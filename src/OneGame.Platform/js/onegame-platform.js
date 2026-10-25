// The browser side of the platform layer: one canvas, its size and pixel ratio, pointer, touch, keyboard, text input (including
// input methods) through a hidden text field, the Gamepad API, visibility, and the animation-frame loop.
// Events are queued here and handed to the managed side as one JSON text per frame.
// References: HTML Living Standard (UI events, the Gamepad API through the W3C Gamepad spec, requestAnimationFrame).

let canvas = null, input = null;
const queue = [];
let textActive = false;
let composing = false;
const pads = new Map();      // index -> { id, buttons: [bool], axes: [number] }
let injected = null;         // test hook: replaces navigator.getGamepads

const MODS = (e) => (e.shiftKey ? 1 : 0) | (e.ctrlKey ? 2 : 0) | (e.altKey ? 4 : 0) | (e.metaKey ? 8 : 0) | (e.getModifierState && e.getModifierState('CapsLock') ? 16 : 0) | (e.getModifierState && e.getModifierState('NumLock') ? 32 : 0);
const push = (o) => { if (queue.length < 20000) queue.push(o); };
const pixels = (v) => Math.round(v * (window.devicePixelRatio || 1));

function measure() {
  const dpr = window.devicePixelRatio || 1;
  return { w: Math.max(1, Math.round(window.innerWidth * dpr)), h: Math.max(1, Math.round(window.innerHeight * dpr)), s: dpr };
}

export function init(canvasId) {
  canvas = document.getElementById(canvasId);
  if (!canvas) throw new Error('No canvas element with id "' + canvasId + '".');
  Object.assign(document.body.style, { margin: '0', overflow: 'hidden', background: '#000' });
  Object.assign(canvas.style, { width: '100vw', height: '100vh', display: 'block', touchAction: 'none', outline: 'none' });
  canvas.tabIndex = 0;
  const m = measure();
  canvas.width = m.w; canvas.height = m.h;

  input = document.createElement('input');
  input.type = 'text'; input.autocomplete = 'off'; input.autocapitalize = 'off'; input.spellcheck = false;
  input.setAttribute('aria-hidden', 'true');
  Object.assign(input.style, { position: 'fixed', left: '0', top: '0', width: '1px', height: '1px', opacity: '0', border: '0', padding: '0', pointerEvents: 'none' });
  document.body.appendChild(input);

  window.addEventListener('resize', () => { const n = measure(); push({ t: 'rs', w: n.w, h: n.h, s: n.s }); });
  document.addEventListener('visibilitychange', () => push({ t: 'vis', hidden: document.hidden }));
  window.addEventListener('focus', () => push({ t: 'fc', f: true }));
  window.addEventListener('blur', () => push({ t: 'fc', f: false }));
  window.addEventListener('beforeunload', () => push({ t: 'close' }));

  const rel = (e) => ({ x: pixels(e.clientX - canvas.getBoundingClientRect().left), y: pixels(e.clientY - canvas.getBoundingClientRect().top) });
  canvas.addEventListener('pointermove', (e) => {
    const p = rel(e);
    push({ t: 'pm', x: p.x, y: p.y, dx: pixels(e.movementX || 0), dy: pixels(e.movementY || 0), lock: document.pointerLockElement === canvas, m: MODS(e) });
  });
  canvas.addEventListener('pointerdown', (e) => {
    canvas.focus({ preventScroll: true });
    try { canvas.setPointerCapture(e.pointerId); } catch { }
    const p = rel(e);
    push({ t: 'pm', x: p.x, y: p.y, dx: 0, dy: 0, m: MODS(e) });
    push({ t: 'pd', b: e.pointerType === 'touch' ? 0 : e.button, x: p.x, y: p.y, m: MODS(e) });
    e.preventDefault();
  });
  canvas.addEventListener('pointerup', (e) => {
    const p = rel(e);
    push({ t: 'pu', b: e.pointerType === 'touch' ? 0 : e.button, x: p.x, y: p.y, m: MODS(e) });
    try { canvas.releasePointerCapture(e.pointerId); } catch { }
  });
  canvas.addEventListener('pointercancel', (e) => { const p = rel(e); push({ t: 'pu', b: 0, x: p.x, y: p.y, m: MODS(e) }); });
  canvas.addEventListener('pointerenter', () => push({ t: 'pi', inside: true }));
  canvas.addEventListener('pointerleave', () => push({ t: 'pi', inside: false }));
  canvas.addEventListener('contextmenu', (e) => e.preventDefault());
  canvas.addEventListener('wheel', (e) => {
    const p = rel(e);
    const k = e.deltaMode === 0 ? 100 : e.deltaMode === 1 ? 1 : 1 / 3;
    push({ t: 'wh', x: p.x, y: p.y, dx: -e.deltaX / k, dy: -e.deltaY / k, m: MODS(e) });
    e.preventDefault();
  }, { passive: false });

  const keyHandler = (down) => (e) => {
    if (e.isComposing || e.key === 'Process' || e.keyCode === 229) return;
    push({ t: down ? 'kd' : 'ku', code: e.code, rep: e.repeat, m: MODS(e) });
    const shortcut = e.ctrlKey || e.metaKey || e.altKey || /^F\d+$/.test(e.code);
    if (down && !shortcut && !textActive && /^(Space|Arrow|Tab|Backspace|Page|Home|End|Enter|Slash|Quote)/.test(e.code)) e.preventDefault();
  };
  window.addEventListener('keydown', keyHandler(true));
  window.addEventListener('keyup', keyHandler(false));

  input.addEventListener('compositionstart', () => { composing = true; });
  input.addEventListener('compositionupdate', (e) => {
    const t = e.data || '';
    push({ t: 'tc', text: t, cs: t.length, ce: t.length });
  });
  input.addEventListener('compositionend', (e) => {
    composing = false;
    push({ t: 'tc', text: '', cs: 0, ce: 0 });
    if (e.data) push({ t: 'ti', text: e.data });
    input.value = '';
  });
  input.addEventListener('input', (e) => {
    if (composing || e.isComposing) return;
    if (e.data) push({ t: 'ti', text: e.data });
    input.value = '';
  });
  window.addEventListener('paste', (e) => { const t = e.clipboardData && e.clipboardData.getData('text'); if (t) push({ t: 'cb', text: t }); });

  document.addEventListener('pointerlockchange', () => push({ t: 'lock', on: document.pointerLockElement === canvas }));
  canvas.focus({ preventScroll: true });
  return JSON.stringify(m);
}

export function size() { return JSON.stringify(measure()); }

// ---- gamepads (standard mapping: W3C Gamepad spec, section "Standard Gamepad") ----
const BUTTON_MAP = [0, 1, 2, 3, 4, 5, -1, -1, 6, 7, 9, 10, 11, 12, 13, 14, 8];   // standard button index -> GamepadButton ordinal; 6 and 7 are triggers
function pollGamepads() {
  const list = injected ? injected() : (navigator.getGamepads ? navigator.getGamepads() : []);
  const seen = new Set();
  for (const g of list) {
    if (!g || !g.connected) continue;
    seen.add(g.index);
    let st = pads.get(g.index);
    if (!st) {
      st = { buttons: [], axes: [0, 0, 0, 0, 0, 0] };
      pads.set(g.index, st);
      push({ t: 'gc', id: g.index, name: g.id });
    }
    g.buttons.forEach((b, i) => {
      const down = typeof b === 'object' ? b.pressed : !!b;
      const value = typeof b === 'object' ? b.value : (b ? 1 : 0);
      if (i === 6 || i === 7) {
        const axis = i === 6 ? 4 : 5;
        if (Math.abs(st.axes[axis] - value) > 0.01) { st.axes[axis] = value; push({ t: 'ga', id: g.index, axis, v: value }); }
      } else if (i < BUTTON_MAP.length && st.buttons[i] !== down) {
        st.buttons[i] = down;
        push({ t: 'gb', id: g.index, b: BUTTON_MAP[i], down });
      }
    });
    for (let a = 0; a < Math.min(4, g.axes.length); a++) {
      const v = g.axes[a];
      if (Math.abs(st.axes[a] - v) > 0.01) { st.axes[a] = v; push({ t: 'ga', id: g.index, axis: a, v }); }
    }
  }
  for (const idx of [...pads.keys()]) if (!seen.has(idx)) { pads.delete(idx); push({ t: 'gd', id: idx }); }
}

// Tests replace the gamepad source with a function returning Gamepad-shaped objects.
export function injectGamepads(fn) { injected = fn; }

export function drain() {
  pollGamepads();
  if (queue.length === 0) return '';
  const out = JSON.stringify(queue);
  queue.length = 0;
  return out;
}

export function setTitle(t) { document.title = t; }
export function setCursorHidden(h) { canvas.style.cursor = h ? 'none' : 'default'; }
export function setCursorLocked(l) { if (l) canvas.requestPointerLock && canvas.requestPointerLock(); else if (document.pointerLockElement) document.exitPointerLock(); }
export function setFullscreen(f) { if (f) (canvas.requestFullscreen || (() => { })).call(canvas); else if (document.fullscreenElement) document.exitFullscreen(); }
export function setTextInput(active, x, y, w, h) {
  textActive = active;
  if (active) {
    const dpr = window.devicePixelRatio || 1;
    Object.assign(input.style, { left: (x / dpr) + 'px', top: (y / dpr) + 'px', width: Math.max(1, w / dpr) + 'px', height: Math.max(1, h / dpr) + 'px' });
    input.focus({ preventScroll: true });
  } else {
    input.blur(); input.value = '';
    canvas.focus({ preventScroll: true });
  }
}
export function clipboardWrite(text) { try { navigator.clipboard.writeText(text); } catch { } }

// The animation-frame loop: calls back with the time in milliseconds until stop() is called.
let running = false;
export function startFrames(callback) {
  running = true;
  const tick = (t) => {
    if (!running) return;
    callback(t);
    // Page-level counters for tools: how many frames ran and when the first one finished (milliseconds since navigation start).
    const o = (window.__onegame = window.__onegame || {});
    o.frames = (o.frames || 0) + 1;
    if (o.frames === 1) o.firstFrameMs = performance.now();
    requestAnimationFrame(tick);
  };
  requestAnimationFrame(tick);
}
export function stopFrames() { running = false; }

// ---- WebSocket (binary messages), polled by the managed side ----
const sockets = [];
export function wsOpen(url) {
  const s = { ws: null, inbox: [], state: 0, code: 0 };
  try {
    const ws = new WebSocket(url);
    ws.binaryType = 'arraybuffer';
    ws.onopen = () => { s.state = 1; };
    ws.onmessage = (e) => { if (e.data instanceof ArrayBuffer) s.inbox.push(new Uint8Array(e.data)); };
    ws.onerror = () => { };
    ws.onclose = (e) => { s.state = 3; s.code = e.code; };
    s.ws = ws;
  } catch { s.state = 3; s.code = 1006; }
  sockets.push(s);
  return sockets.length - 1;
}
export function wsState(id) { return sockets[id].state; }
export function wsCode(id) { return sockets[id].code; }
export function wsPending(id) { const q = sockets[id].inbox; return q.length ? q[0].length : -1; }
export function wsReceive(id, dest) { const m = sockets[id].inbox.shift(); dest.set(m); return m.length; }
export function wsSend(id, data) { const s = sockets[id]; if (s.state === 1) s.ws.send(data.slice()); }
export function wsClose(id) { const s = sockets[id]; if (s.ws && s.state < 2) { s.state = 2; s.ws.close(1000); } }
