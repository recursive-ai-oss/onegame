// Synthetic-event tests of onegame-platform.js (the browser half of the platform layer): gamepad polling through the test hook, input-method
// composition on the hidden text field, keyboard, pointer and touch. Used by run-page.mjs:
// node run-page.mjs --root <folder with onegame-platform.js and platform-test.html> --path /platform-test.html --script platform-test.mjs
export default async function ({ evalJs, sleep }) {
  for (let i = 0; i < 100 && !(await evalJs('window.__ready === true')); i++) await sleep(50);
  const drain = async () => { const t = await evalJs('__p.drain()'); return t ? JSON.parse(t) : []; };
  const checks = [];
  const check = (name, ok, detail) => checks.push({ name, ok: !!ok, ...(ok ? {} : { detail }) });
  await drain();

  // Gamepad: connect, press south (standard button 0), push the left stick, pull the left trigger, release, disconnect.
  await evalJs(`window.__pad = { index: 0, id: 'Test Pad', connected: true, buttons: Array.from({ length: 17 }, () => ({ pressed: false, value: 0 })), axes: [0, 0, 0, 0] };
                __p.injectGamepads(() => window.__pad ? [window.__pad] : []);`);
  let ev = await drain();
  check('gamepad connects with its name', ev.some(e => e.t === 'gc' && e.id === 0 && e.name === 'Test Pad'), ev);
  await evalJs('window.__pad.buttons[0] = { pressed: true, value: 1 }; window.__pad.axes[0] = -0.75; window.__pad.buttons[6] = { pressed: true, value: 0.5 };');
  ev = await drain();
  check('button 0 press', ev.some(e => e.t === 'gb' && e.b === 0 && e.down), ev);
  check('left stick x axis', ev.some(e => e.t === 'ga' && e.axis === 0 && e.v === -0.75), ev);
  check('left trigger is axis 4', ev.some(e => e.t === 'ga' && e.axis === 4 && e.v === 0.5), ev);
  await evalJs('window.__pad.buttons[0] = { pressed: false, value: 0 };');
  ev = await drain();
  check('button 0 release', ev.some(e => e.t === 'gb' && e.b === 0 && !e.down), ev);
  await evalJs('window.__pad = null;');
  ev = await drain();
  check('gamepad disconnects', ev.some(e => e.t === 'gd' && e.id === 0), ev);

  // Input method: composition updates become pre-edit events and the finished text a text input.
  await evalJs(`__p.setTextInput(true, 10, 10, 100, 20); window.__in = document.querySelector('input'); window.__in.focus();
                window.__in.dispatchEvent(new CompositionEvent('compositionstart', { data: '' }));
                window.__in.dispatchEvent(new CompositionEvent('compositionupdate', { data: 'にほ' }));`);
  ev = await drain();
  check('IME pre-edit text', ev.some(e => e.t === 'tc' && e.text === 'にほ'), ev);
  await evalJs(`window.__in.dispatchEvent(new CompositionEvent('compositionend', { data: '日本' }));`);
  ev = await drain();
  check('IME end clears the pre-edit and commits the text', ev.some(e => e.t === 'tc' && e.text === '') && ev.some(e => e.t === 'ti' && e.text === '日本'), ev);
  // Keys that arrive while composing belong to the input method.
  await evalJs(`window.__in.dispatchEvent(new CompositionEvent('compositionstart', { data: '' }));
                window.dispatchEvent(new KeyboardEvent('keydown', { code: 'KeyA', key: 'Process', keyCode: 229 }));
                window.__in.dispatchEvent(new CompositionEvent('compositionend', { data: '' }));`);
  ev = await drain();
  check('keys during composition are not game keys', !ev.some(e => e.t === 'kd'), ev);

  // Keyboard and pointer, including touch delivered as a primary pointer.
  await evalJs(`__p.setTextInput(false, 0, 0, 0, 0);
                window.dispatchEvent(new KeyboardEvent('keydown', { code: 'KeyU', key: 'u', shiftKey: true }));
                window.dispatchEvent(new KeyboardEvent('keyup', { code: 'KeyU', key: 'u' }));
                const c = document.getElementById('c');
                c.dispatchEvent(new PointerEvent('pointerdown', { pointerId: 1, pointerType: 'touch', clientX: 20, clientY: 30, button: 0, bubbles: true }));
                c.dispatchEvent(new PointerEvent('pointerup', { pointerId: 1, pointerType: 'touch', clientX: 20, clientY: 30, button: 0, bubbles: true }));
                c.dispatchEvent(new WheelEvent('wheel', { clientX: 5, clientY: 5, deltaY: 100, deltaMode: 0, cancelable: true, bubbles: true }));`);
  ev = await drain();
  check('key down with shift', ev.some(e => e.t === 'kd' && e.code === 'KeyU' && (e.m & 1) === 1), ev);
  check('key up', ev.some(e => e.t === 'ku' && e.code === 'KeyU'), ev);
  check('touch becomes a left-button press at its position', ev.some(e => e.t === 'pd' && e.b === 0 && e.x === 20 && e.y === 30), ev);
  check('touch release', ev.some(e => e.t === 'pu' && e.x === 20 && e.y === 30), ev);
  check('wheel scrolls down as negative dy', ev.some(e => e.t === 'wh' && e.dy === -1), ev);
  return { ok: checks.every(c => c.ok), passed: checks.filter(c => c.ok).length, failed: checks.filter(c => !c.ok).length, checks };
}
