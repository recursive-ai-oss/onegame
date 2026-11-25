// The web host's viewport, pointer and safe-area handling in Safari, through WebDriver (run-page-safari.mjs). The Chrome versions (mobile-test,
// pointer-test) emulate a phone with DevTools commands, which Safari does not have, so this one uses what the desktop browser offers: resizing the
// window, W3C pointer actions, and the page's own measurements. Run on the export of tests/WebGame:
//   node run-page-safari.mjs --root <export folder> --path '/index.html' --script safari-viewport-test.mjs
export default async function ({ evalJs, sleep, logs, driver }) {
  for (let i = 0; i < 300 && !(await evalJs('window.__onegame && window.__onegame.frames > 5')); i++) await sleep(100);
  const checks = [];
  const check = (name, ok, detail) => checks.push({ name, ok: !!ok, ...(ok ? {} : { detail }) });
  const result = (extra = {}) => ({ ok: checks.every(c => c.ok) && !extra.blocked, passed: checks.filter(c => c.ok).length, failed: checks.filter(c => !c.ok).length, checks, ...extra });
  // Safari does not run animation frames for a hidden page (a locked screen, a window behind others), so the game never draws. Say so, and
  // run the checks that need no frame.
  if (!(await evalJs('window.__onegame && window.__onegame.frames > 5'))) {
    check('the page asks for viewport-fit=cover', await evalJs(`/viewport-fit=cover/.test(document.querySelector('meta[name=viewport]')?.content || '')`));
    check('the page title is the game name as written', (await evalJs('document.title')) === 'Web Fixture', await evalJs('document.title'));
    check('the canvas exists and the game started without an error', await evalJs('!!document.querySelector("canvas") && window.__onegame.error === null'));
    check('WebGPU is offered', await evalJs('!!(await navigator.gpu.requestAdapter())'));
    return result({ blocked: 'no animation frame ran: ' + JSON.stringify(await evalJs('[document.visibilityState, document.hasFocus()]')) });
  }
  const measure = async () => JSON.parse(await evalJs(`(() => { const c = document.querySelector('canvas'), r = c.getBoundingClientRect(), v = window.visualViewport;
    return JSON.stringify({ iw: innerWidth, ih: innerHeight, vw: v.width, vh: v.height, dpr: devicePixelRatio, left: r.left, top: r.top, cssW: r.width, cssH: r.height, bufW: c.width, bufH: c.height }); })()`));
  const lastWindow = () => [...logs].reverse().find(l => l.includes('window '))?.replace(/^log: /, '');
  const edges = () => logs.filter(l => l.includes('edge frame=')).map(l => {
    const m = /edge frame=(\d+) (\w+) id=(-?\d+) x=(-?[\d.]+) y=(-?[\d.]+)/.exec(l);
    return m && { frame: +m[1], kind: m[2], id: +m[3], x: +m[4], y: +m[5] };
  }).filter(Boolean);
  const { wd, s } = driver;

  // The canvas box is the visible area, at the start and after the window changes size.
  let m = await measure();
  check('the canvas box is the visual viewport', m.cssW === m.vw && m.cssH === m.vh && m.left === 0 && m.top === 0, m);
  check('the drawing surface is that box at the pixel ratio', Math.abs(m.bufW - m.cssW * m.dpr) <= 1 && Math.abs(m.bufH - m.cssH * m.dpr) <= 1, m);
  check('the game sees that size', lastWindow()?.startsWith(`window ${m.bufW}x${m.bufH} `), { window: lastWindow(), m });
  const rect = await wd('GET', s('/window/rect'));
  for (const [w, h] of [[900, 640], [700, 800]]) {
    await wd('POST', s('/window/rect'), { x: rect.x, y: rect.y, width: w, height: h });
    await sleep(1200);
    m = await measure();
    check(`after resizing to ${w}x${h}: the box follows the visual viewport`, m.cssW === m.vw && m.cssH === m.vh && m.vw < w + 1 && m.vw > w / 2, m);
    check(`after resizing to ${w}x${h}: the surface has the same shape as the box`, Math.abs(m.bufW - m.cssW * m.dpr) <= 1 && Math.abs(m.bufH - m.cssH * m.dpr) <= 1, m);
    check(`after resizing to ${w}x${h}: the game sees the new size`, lastWindow()?.startsWith(`window ${m.bufW}x${m.bufH} `), { window: lastWindow(), m });
  }

  // A press at a known place in the visible area arrives at that place times the pixel ratio.
  const before = edges().length;
  const at = { x: 321, y: 207 };
  await wd('POST', s('/actions'), { actions: [{ type: 'pointer', id: 'mouse', parameters: { pointerType: 'mouse' }, actions: [
    { type: 'pointerMove', duration: 0, x: at.x, y: at.y, origin: 'viewport' }, { type: 'pointerDown', button: 0 }, { type: 'pause', duration: 50 }, { type: 'pointerUp', button: 0 }] }] });
  await sleep(600);
  const got = edges().slice(before);
  const down = got.find(e => e.kind === 'down');
  m = await measure();
  check('a mouse press lands at its CSS position times the pixel ratio', down && Math.abs(down.x - at.x * m.dpr) < 1 && Math.abs(down.y - at.y * m.dpr) < 1, { got, dpr: m.dpr });

  // Safe area: the page asks for the whole screen, and a desktop window has no insets; the game must say so (not leave it undefined).
  check('the page asks for viewport-fit=cover', await evalJs(`/viewport-fit=cover/.test(document.querySelector('meta[name=viewport]')?.content || '')`), await evalJs(`document.querySelector('meta[name=viewport]')?.content`));
  check('the game holds the safe area from the start (none on a desktop window)', logs.some(l => /window \d+x\d+ scale=[\d.]+ safe=0,0,0,0/.test(l)), lastWindow());
  const env = await evalJs(`(() => { const p = document.createElement('div'); p.style.cssText = 'position:fixed;visibility:hidden;padding-top:env(safe-area-inset-top, 7px)'; document.body.appendChild(p); const v = getComputedStyle(p).paddingTop; p.remove(); return v; })()`);
  check('env(safe-area-inset-top) resolves to a length', /^\d+(\.\d+)?px$/.test(env), env);

  check('the page title is the game name as written', (await evalJs('document.title')) === 'Web Fixture', await evalJs('document.title'));
  check('no exception on the page', !logs.some(l => l.startsWith('exception:')), logs.filter(l => l.startsWith('exception:')).slice(0, 3));
  return result();
}
