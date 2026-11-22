// Pointer positions in a real browser: the game logs each pointer edge it sees in a frame ("edge frame=N kind id=I x=X y=Y", from tests/WebGame).
// A press followed by a long move in the same frame must report the press where it happened, a finger that moves and lifts in one frame must
// report the lift where it lifted, and positions keep their fraction. Run on the export of tests/WebGame:
//   node run-page.mjs --root <export folder> --path '/index.html' --script pointer-test.mjs
export default async function ({ send, evalJs, sleep, logs }) {
  for (let i = 0; i < 300 && !(await evalJs('window.__onegame && window.__onegame.frames > 5')); i++) await sleep(100);
  const checks = [];
  const check = (name, ok, detail) => checks.push({ name, ok: !!ok, ...(ok ? {} : { detail }) });
  const edges = () => logs.filter(l => l.includes('edge frame=')).map(l => {
    const m = /edge frame=(\d+) (\w+) id=(-?\d+) x=(-?[\d.]+) y=(-?[\d.]+)/.exec(l);
    return m && { frame: +m[1], kind: m[2], id: +m[3], x: +m[4], y: +m[5] };
  }).filter(Boolean);
  // Holds the page's main thread, so everything sent meanwhile is delivered to one frame.
  const hold = () => send('Runtime.evaluate', { expression: 'const t = performance.now(); while (performance.now() - t < 200) {}' });
  const since = (n) => edges().slice(n);

  // Mouse: press at (200.5, 150.25), then 60 px to the right, all in one frame.
  let before = edges().length;
  hold(); await sleep(20);
  const mouse = (type, x, y, extra = {}) => send('Input.dispatchMouseEvent', { type, x, y, button: type === 'mouseMoved' ? 'none' : 'left', clickCount: type === 'mouseMoved' ? 0 : 1, ...extra });
  await Promise.all([mouse('mouseMoved', 200.5, 150.25), mouse('mousePressed', 200.5, 150.25), mouse('mouseMoved', 260.5, 150.25, { button: 'left', buttons: 1 })]);
  await sleep(500);
  let got = since(before);
  const down = got.find(e => e.kind === 'down' && e.id === -1);
  const lastMove = [...got].reverse().find(e => e.kind === 'move');
  check('a mouse press keeps its own position when the pointer moves 60 px in the same frame', down && Math.abs(down.x - 200.5) < 0.01 && Math.abs(down.y - 150.25) < 0.01, got);
  check('the move after it is listed too, further on', lastMove && lastMove.x > 255, got);
  check('press and move arrived in one frame', down && lastMove && down.frame === lastMove.frame, got);
  await mouse('mouseReleased', 260.5, 150.25);
  await sleep(300);

  // Touch: a finger moves and lifts in the same frame; the lift is where the finger was last, not where the previous frame saw it.
  await send('Emulation.setTouchEmulationEnabled', { enabled: true, maxTouchPoints: 5 });
  await send('Input.dispatchTouchEvent', { type: 'touchStart', touchPoints: [{ x: 100, y: 100, id: 1 }] });
  await sleep(300);
  before = edges().length;
  hold(); await sleep(20);
  await Promise.all([
    send('Input.dispatchTouchEvent', { type: 'touchMove', touchPoints: [{ x: 180.5, y: 140.75, id: 1 }] }),
    send('Input.dispatchTouchEvent', { type: 'touchEnd', touchPoints: [] }),
  ]);
  await sleep(500);
  got = since(before);
  const up = got.find(e => e.kind === 'up' && e.id >= 0);
  const mv = got.find(e => e.kind === 'move' && e.id >= 0);
  check('a finger that moves and lifts in one frame reports the lift at the end of the move', up && Math.abs(up.x - 180.5) < 0.01 && Math.abs(up.y - 140.75) < 0.01, got);
  check('and the move before it', mv && Math.abs(mv.x - 180.5) < 0.01, got);

  // Touch: down and up inside one frame is still two edges.
  before = edges().length;
  hold(); await sleep(20);
  await send('Input.dispatchTouchEvent', { type: 'touchStart', touchPoints: [{ x: 321.25, y: 210.5, id: 2 }] });
  await send('Input.dispatchTouchEvent', { type: 'touchEnd', touchPoints: [] });
  await sleep(500);
  got = since(before);
  const d2 = got.find(e => e.kind === 'down' && e.id >= 0), u2 = got.find(e => e.kind === 'up' && e.id >= 0);
  check('a tap that began and ended in one frame is a down and an up at its position', d2 && u2 && Math.abs(d2.x - 321.25) < 0.01 && Math.abs(u2.y - 210.5) < 0.01, got);
  return { ok: checks.every(c => c.ok), passed: checks.filter(c => c.ok).length, failed: checks.filter(c => !c.ok).length, checks };
}
