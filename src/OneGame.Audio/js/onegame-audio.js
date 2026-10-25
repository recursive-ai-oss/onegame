// Web Audio output for onegame. The game's mixer runs in WebAssembly on the page's main thread (a page has no other thread to give it);
// each timer tick it renders the frames the device wants and hands them here, and an AudioWorklet plays them. The worklet reports back
// how many frames it has played, how many times it ran dry, and the peak of what it played, which is what the harness reports.
//
// Browsers keep an AudioContext suspended until the user has interacted with the page (W3C Web Audio API, "autoplay policy"). Until then
// frames are dropped at real-time pace, so the game's clock and sounds behave the same and nothing is played; the first user gesture
// resumes the context and sound starts from that moment.

const processorSource = `
class OneGameOutput extends AudioWorkletProcessor {
  constructor(options) {
    super();
    this.channels = options.processorOptions.channels;
    this.queue = [];
    this.pos = 0;
    this.consumed = 0;
    this.underruns = 0;
    this.peak = 0;
    this.started = false;
    this.starved = false;
    this.quanta = 0;
    this.port.onmessage = (e) => {
      if (e.data === 'flush') { this.queue = []; this.pos = 0; } else { this.queue.push(e.data); }
    };
  }
  process(inputs, outputs) {
    const out = outputs[0];
    const frames = out[0].length;
    const ch = this.channels;
    let i = 0;
    while (i < frames && this.queue.length > 0) {
      const buf = this.queue[0];
      const avail = buf.length / ch - this.pos;
      const n = Math.min(avail, frames - i);
      for (let c = 0; c < ch && c < out.length; c++) {
        const dst = out[c];
        for (let k = 0; k < n; k++) {
          const s = buf[(this.pos + k) * ch + c];
          dst[i + k] = s;
          const a = s < 0 ? -s : s;
          if (a > this.peak) this.peak = a;
        }
      }
      i += n; this.pos += n; this.consumed += n;
      if (this.pos * ch >= buf.length) { this.queue.shift(); this.pos = 0; }
    }
    if (i > 0) this.started = true;
    if (i < frames) {
      // Fewer frames than the device asked for: a dry spell (counted once per run of dry quanta, and only after sound has begun).
      if (this.started && !this.starved) { this.underruns++; this.starved = true; }
    } else {
      this.starved = false;
    }
    if (++this.quanta % 8 === 0) this.port.postMessage({ consumed: this.consumed, underruns: this.underruns, peak: this.peak });
    return true;
  }
}
registerProcessor('onegame-output', OneGameOutput);
`;

let A = null;   // the one output

function newState() {
  return {
    ctx: null, node: null, ready: false, error: '', rate: 0, channels: 2, targetFrames: 0,
    sent: 0, dropped: 0, consumed: 0, underruns: 0, peak: 0, lag: 0, lagAt: 0,
    wantActive: true, gesture: false, gestureEvents: 0, resumeRequests: 0, timer: 0, unlockLog: '',
  };
}

function hasGesture() {
  return A.gesture || (typeof navigator !== 'undefined' && navigator.userActivation && navigator.userActivation.hasBeenActive);
}

function tryResume() {
  if (!A || !A.ctx || !A.wantActive || A.ctx.state === 'running' || A.ctx.state === 'closed') return;
  A.resumeRequests++;
  A.ctx.resume().catch((e) => { A.unlockLog = String(e); });
}

function onGesture(e) {
  if (!A) return;
  // Only real (trusted) input counts as a gesture; a script-made event does not unlock audio.
  if (e && e.isTrusted === false) return;
  A.gesture = true;
  A.gestureEvents++;
  tryResume();
}

// Creates the context and the worklet; returns '' or the reason it is unavailable. The context starts suspended under the autoplay policy.
export function create(rate, channels, latencyMs) {
  if (A) return '';
  const Ctx = globalThis.AudioContext || globalThis.webkitAudioContext;
  if (!Ctx || !globalThis.AudioWorkletNode) return 'this browser has no AudioContext/AudioWorklet';
  A = newState();
  A.channels = channels;
  try {
    A.ctx = new Ctx({ sampleRate: rate, latencyHint: 'interactive' });
  } catch (e) {
    try { A.ctx = new Ctx({ latencyHint: 'interactive' }); } catch (e2) { const m = String(e2); A = null; return m; }
  }
  A.rate = A.ctx.sampleRate;
  A.targetFrames = Math.max(latencyMs, 80) * A.rate / 1000 | 0;
  const url = URL.createObjectURL(new Blob([processorSource], { type: 'text/javascript' }));
  A.ctx.audioWorklet.addModule(url).then(() => {
    A.node = new AudioWorkletNode(A.ctx, 'onegame-output', { numberOfInputs: 0, numberOfOutputs: 1, outputChannelCount: [channels], processorOptions: { channels } });
    A.node.port.onmessage = (e) => { A.consumed = e.data.consumed; A.underruns = e.data.underruns; A.peak = e.data.peak; };
    A.node.connect(A.ctx.destination);
    A.ready = true;
    URL.revokeObjectURL(url);
  }).catch((e) => { A.error = String(e); });
  for (const type of ['pointerdown', 'pointerup', 'mousedown', 'touchend', 'keydown', 'click']) {
    window.addEventListener(type, onGesture, { capture: true, passive: true });
  }
  A.ctx.addEventListener('statechange', () => { A.unlockLog = 'state:' + A.ctx.state; });
  return '';
}

export function rate() { return A ? A.rate : 0; }

export function targetFrames() { return A ? A.targetFrames : 0; }

function playing() { return A && A.ready && A.ctx.state === 'running' && A.wantActive; }

// Frames waiting to be played. While the context can't play (autoplay not yet allowed, or suspended by the app), frames are dropped and
// this counts them down at the sample rate, so the mixer keeps real-time pace without anything being queued for later.
export function queued() {
  if (!A) return 0;
  const now = performance.now();
  if (playing()) {
    A.lag = 0; A.lagAt = now;
    return Math.max(0, A.sent - A.dropped - A.consumed);
  }
  A.lag = Math.max(0, A.lag - (now - A.lagAt) * A.rate / 1000);
  A.lagAt = now;
  return A.lag | 0;
}

// Takes interleaved float32 bytes. Returns true when they were queued for the speakers and false when they were dropped.
export function write(view) {
  if (!A) return false;
  const copy = view.slice();   // the managed buffer is only valid during this call
  const frames = copy.byteLength / 4 / A.channels | 0;
  if (!playing()) {
    A.dropped += frames; A.sent += frames;
    if (A.lagAt === 0) A.lagAt = performance.now();
    A.lag += frames;
    return false;
  }
  A.lagAt = performance.now();
  A.sent += frames;
  const f32 = new Float32Array(copy.buffer, copy.byteOffset, copy.byteLength / 4);
  A.node.port.postMessage(f32, [f32.buffer]);
  return true;
}

// The app lifecycle: false suspends the context (the hardware is released), true resumes it if the user has already interacted.
export function setActive(active) {
  if (!A) return;
  A.wantActive = active;
  if (active) {
    if (hasGesture()) tryResume();
  } else {
    A.ctx.suspend().catch(() => {});
    if (A.node) A.node.port.postMessage('flush');
    A.dropped += Math.max(0, A.sent - A.dropped - A.consumed);   // what was queued is thrown away with the flush
  }
}

// Asks the browser to start audio now. It works only when the page has had a user gesture; the answer is the context's state.
export function unlock() {
  if (!A) return 'none';
  A.wantActive = true;
  A.resumeRequests++;
  A.ctx.resume().catch((e) => { A.unlockLog = String(e); });
  return A.ctx.state;
}

// 'initializing' | 'awaiting_gesture' | 'running' | 'suspended' | 'closed' | 'unavailable'
export function state() {
  if (!A) return 'unavailable';
  if (A.error) return 'unavailable';
  if (A.ctx.state === 'closed') return 'closed';
  if (!A.ready) return 'initializing';
  if (A.ctx.state === 'running') return A.wantActive ? 'running' : 'suspended';
  if (!A.wantActive) return 'suspended';
  return hasGesture() ? 'suspended' : 'awaiting_gesture';
}

// "key=value; ..." for the report's detail line.
export function stats() {
  if (!A) return 'no output';
  const c = A.ctx;
  const ms = (v) => Math.round((v || 0) * 1000 * 100) / 100;
  return `contextState=${c.state}; gestureSeen=${!!hasGesture()}; gestureEvents=${A.gestureEvents}; resumeRequests=${A.resumeRequests}; ` +
    `sampleRate=${c.sampleRate}; baseLatencyMs=${ms(c.baseLatency)}; outputLatencyMs=${ms(c.outputLatency)}; ` +
    `sentFrames=${A.sent}; droppedFrames=${A.dropped}` + (A.error ? `; error=${A.error}` : '');
}

export function consumed() { return A ? A.consumed : 0; }

export function underruns() { return A ? A.underruns : 0; }

export function peak() { return A ? A.peak : 0; }

// Calls back every few milliseconds; the managed side renders what is wanted. A page timer is the only clock a single-threaded runtime has.
export function startTimer(callback, ms) {
  if (!A || A.timer) return;
  A.timer = setInterval(callback, ms);
}

export function close() {
  if (!A) return;
  if (A.timer) clearInterval(A.timer);
  for (const type of ['pointerdown', 'pointerup', 'mousedown', 'touchend', 'keydown', 'click']) window.removeEventListener(type, onGesture, { capture: true });
  try { A.ctx.close(); } catch (e) { /* already closed */ }
  A = null;
}
