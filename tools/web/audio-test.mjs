// Checks the web audio output in a running solitaire page: the browser's autoplay rule, the first user gesture, sound reaching the audio
// worklet, and the app lifecycle. A test tool: nothing here ships. Used by run-page.mjs:
//   node run-page.mjs --root <exported dev folder> --path "/index.html?args=--no-present%20--seed%201&control=page" --script audio-test.mjs
// Chrome is started with the autoplay policy a normal browser has (--autoplay-policy=document-user-activation-required), so the context
// stays suspended until the script sends a real mouse click through the DevTools protocol (a trusted gesture).
let nextId = 1;

export default async function ({ send, evalJs, sleep }) {
  const call = async (name, args = {}) => {
    const reply = JSON.parse(await evalJs(`__control(${JSON.stringify(JSON.stringify({ jsonrpc: '2.0', id: nextId++, method: 'tools/call', params: { name, arguments: args } }))})`));
    if (reply.error) throw new Error(name + ': ' + JSON.stringify(reply.error));
    const text = reply.result.content[0].text;
    if (reply.result.isError) throw new Error(name + ': ' + text);
    return JSON.parse(text);
  };
  for (let i = 0; i < 300 && !(await evalJs('window.__control && window.__onegame && window.__onegame.frames > 3')); i++) await sleep(100);
  await evalJs(`__control(${JSON.stringify(JSON.stringify({ jsonrpc: '2.0', id: 0, method: 'initialize', params: { protocolVersion: '2025-06-18', capabilities: {}, clientInfo: { name: 'audio-test', version: '1' } } }))})`);

  const steps = [];
  const device = async (args = {}) => (await call('audio', args)).device;
  const note = (step, d, ok, extra = {}) => steps.push({ step, ok, deviceState: d.deviceState, framesRendered: d.framesRendered, framesWritten: d.framesWritten, framesPlayed: d.framesPlayed, peakPlayed: d.peakPlayed, underruns: d.underruns, allocatedBytes: d.audioThreadAllocatedBytes, deviceUnderruns: d.deviceUnderruns, suspended: d.suspended, request: d.request, detail: d.detail, ...extra });
  const tapStock = async () => { await call('ui_tap', { id: 'stock' }); await sleep(250); };
  const mouse = (type, x, y) => send('Input.dispatchMouseEvent', { type, x, y, button: 'left', clickCount: 1 });

  await call('screen_open', { name: 'table' });
  await sleep(600);

  // 1. Before any gesture the browser keeps audio suspended: sounds are dropped, nothing reaches the worklet.
  let d = await device();
  note('fresh page: the output exists and waits for a gesture', d, d.backend === 'webaudio' && d.deviceState === 'awaiting_gesture' && d.framesPlayed === 0 && d.peakPlayed === 0);
  await tapStock(); await tapStock();
  await sleep(500);
  d = await device();
  note('sounds played before any gesture are dropped', d, d.deviceState === 'awaiting_gesture' && d.framesPlayed === 0 && d.peakPlayed === 0 && d.framesRendered > 0, { rendered: d.framesRendered });

  // 2. Asking for audio from a script does not unlock it either (the same rule): the harness sees the state it asked for.
  d = await device({ unlock: true });
  await sleep(300);
  const d2 = await device();
  note('unlock without a user gesture stays suspended', { ...d2, request: d.request }, d2.deviceState === 'awaiting_gesture' && d2.framesPlayed === 0);

  // 3. A real click (trusted input) starts audio.
  const board = await evalJs('(() => { const r = document.getElementById("onegame-canvas").getBoundingClientRect(); return [r.x + r.width / 2, r.y + 8]; })()');
  await mouse('mouseMoved', board[0], board[1]);
  await mouse('mousePressed', board[0], board[1]);
  await sleep(60);
  await mouse('mouseReleased', board[0], board[1]);
  let running = false;
  for (let i = 0; i < 40 && !running; i++) { await sleep(100); running = (await device()).deviceState === 'running'; }
  d = await device();
  note('a real click starts audio', d, d.deviceState === 'running');

  // 4. Sounds now reach the worklet: frames played advance and the peak of what it played is above zero.
  const before = d.framesPlayed;
  await tapStock(); await sleep(400); await tapStock(); await sleep(600);
  d = await device();
  note('sounds reach the device after the gesture', d, d.framesPlayed > before + 20000 && d.peakPlayed > 0.01 && d.underruns === 0, { playedSinceGesture: d.framesPlayed - before });

  // 5. Lifecycle: leaving the foreground suspends the context and the mixer clock; coming back resumes both.
  await call('app_lifecycle', { state: 'paused' });
  await sleep(500);
  d = await device();
  const heldRendered = d.framesRendered, heldPlayed = d.framesPlayed;
  await sleep(700);
  const d3 = await device();
  note('paused: the output stops', d3, d3.suspended === true && d3.framesRendered === heldRendered && d3.framesPlayed === heldPlayed && d3.deviceState === 'suspended');
  await call('app_lifecycle', { state: 'resumed' });
  await sleep(1200);
  d = await device();
  note('resumed: the output runs again', d, d.suspended === false && d.deviceState === 'running' && d.framesPlayed > heldPlayed && d.framesRendered > heldRendered);

  return { ok: steps.every(s => s.ok), steps };
}
