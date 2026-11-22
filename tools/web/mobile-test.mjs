// The canvas on a phone: its CSS box is the visible area (innerWidth by innerHeight), so the picture is not stretched against the drawing
// surface, and the safe area reaches the game. Run on the export of tests/WebGame:
//   node run-page.mjs --root <export folder> --path '/index.html' --script mobile-test.mjs
export default async function ({ send, evalJs, sleep, logs }) {
  for (let i = 0; i < 300 && !(await evalJs('window.__onegame && window.__onegame.frames > 5')); i++) await sleep(100);
  const checks = [];
  const check = (name, ok, detail) => checks.push({ name, ok: !!ok, ...(ok ? {} : { detail }) });
  const measure = async () => JSON.parse(await evalJs(`(() => { const c = document.querySelector('canvas'), r = c.getBoundingClientRect(); return JSON.stringify({ iw: innerWidth, ih: innerHeight, dpr: devicePixelRatio, left: r.left, top: r.top, cssW: r.width, cssH: r.height, bufW: c.width, bufH: c.height }); })()`));
  const lastWindow = () => [...logs].reverse().find(l => l.includes('window '))?.replace(/^log: /, '');

  await send('Emulation.setDeviceMetricsOverride', { width: 390, height: 844, deviceScaleFactor: 3, mobile: true, screenWidth: 390, screenHeight: 844 });
  await sleep(600);
  let m = await measure();
  check('phone: the canvas box is the visible area', m.cssW === m.iw && m.cssH === m.ih && m.left === 0 && m.top === 0, m);
  check('phone: the drawing surface is that box at the pixel ratio', m.bufW === m.iw * 3 && m.bufH === m.ih * 3, m);
  check('phone: the game sees that size', lastWindow()?.startsWith('window 1170x2532 scale=3'), lastWindow());

  // A browser whose toolbar is out reports a smaller innerHeight than the unit 100vh stands for. Emulate that by changing what the page reads,
  // then telling it the viewport changed, as the browser does.
  await evalJs(`Object.defineProperty(window, 'innerHeight', { configurable: true, get: () => 700 }); visualViewport.dispatchEvent(new Event('resize')); true`);
  await sleep(600);
  m = await measure();
  check('toolbar out: the canvas follows innerHeight, not 100vh', m.cssH === 700 && m.bufH === 2100, m);
  check('toolbar out: the surface and the box have the same shape (no stretch)', Math.abs(m.cssH * m.dpr - m.bufH) < 0.5 && Math.abs(m.cssW * m.dpr - m.bufW) < 0.5, m);
  check('toolbar out: the game sees the new height', lastWindow()?.startsWith('window 1170x2100 scale=3'), lastWindow());

  // A tap lands where the finger is on the visible picture: the middle of the box is the middle of the surface.
  await send('Emulation.setTouchEmulationEnabled', { enabled: true, maxTouchPoints: 5 });
  const mark = logs.length;
  await send('Input.dispatchTouchEvent', { type: 'touchStart', touchPoints: [{ x: 195, y: 350, id: 1 }] });
  await send('Input.dispatchTouchEvent', { type: 'touchEnd', touchPoints: [] });
  await sleep(600);
  const down = logs.slice(mark).map(l => /edge frame=\d+ down id=(\d+) x=([\d.]+) y=([\d.]+)/.exec(l)).find(Boolean);
  check('toolbar out: a tap at the middle of the visible area is the middle of the surface', down && Math.abs(down[2] - 585) < 0.5 && Math.abs(down[3] - 1050) < 0.5, down && down.slice(0, 4));

  // Safe area: the page asks for the whole screen (viewport-fit=cover); the browser's insets reach the game as a change.
  await evalJs(`Object.defineProperty(window, 'innerHeight', { configurable: true, get: () => 844 }); true`);
  await send('Emulation.setSafeAreaInsetsOverride', { insets: { top: 47, bottom: 34, left: 0, right: 0 } });
  await sleep(300);
  await evalJs(`window.dispatchEvent(new Event('resize')); true`);
  await sleep(600);
  check('the safe area insets arrive in device pixels', lastWindow()?.endsWith('safe=0,141,0,102'), lastWindow());
  await send('Emulation.setSafeAreaInsetsOverride', { insets: { top: 0, bottom: 0, left: 0, right: 0 } });
  await evalJs(`window.dispatchEvent(new Event('resize')); true`);
  await sleep(600);
  check('and a change back to none is reported too', lastWindow()?.endsWith('safe=0,0,0,0'), lastWindow());

  // The page keeps its own title.
  const title = await evalJs('document.title');
  check('the page title is the game name as written', title === 'Web Fixture', title);
  return { ok: checks.every(c => c.ok), passed: checks.filter(c => c.ok).length, failed: checks.filter(c => !c.ok).length, checks };
}
