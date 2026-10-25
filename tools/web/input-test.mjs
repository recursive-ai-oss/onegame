// Drives a running solitaire page with real browser input (Chrome DevTools Protocol Input.* events) and checks the game state through the
// page's control function. Used by run-page.mjs: node run-page.mjs --root <exported folder> --path "/index.html?args=--no-present%20--seed%201&control=page" --script input-test.mjs
let nextId = 1;

export default async function ({ send, evalJs, sleep }) {
  const call = async (name, args = {}) => {
    const reply = JSON.parse(await evalJs(`__control(${JSON.stringify(JSON.stringify({ jsonrpc: '2.0', id: nextId++, method: 'tools/call', params: { name, arguments: args } }))})`));
    if (reply.error) throw new Error(name + ': ' + JSON.stringify(reply.error));
    const text = reply.result.content[0].text;
    if (reply.result.isError) throw new Error(name + ': ' + text);
    return JSON.parse(text);
  };
  for (let i = 0; i < 200 && !(await evalJs('window.__control && window.__onegame && window.__onegame.frames > 3')); i++) await sleep(100);
  await evalJs(`__control(${JSON.stringify(JSON.stringify({ jsonrpc: '2.0', id: 0, method: 'initialize', params: { protocolVersion: '2025-06-18', capabilities: {}, clientInfo: { name: 'input-test', version: '1' } } }))})`);
  const steps = [];
  const mouse = (type, x, y, extra = {}) => send('Input.dispatchMouseEvent', { type, x, y, button: 'left', clickCount: 1, ...extra });
  const key = async (k, code, vk) => {
    await send('Input.dispatchKeyEvent', { type: 'rawKeyDown', key: k, code, windowsVirtualKeyCode: vk });
    await send('Input.dispatchKeyEvent', { type: 'keyUp', key: k, code, windowsVirtualKeyCode: vk });
  };
  const hash = async () => (await call('hash')).hash;
  const moves = async () => Number(await call('state_query', { entity: 'Table', property: 'Moves' }).then(v => (typeof v === 'object' && v !== null ? v.value ?? v : v)));
  // (the game runs live: the clock advances ticks between our calls, so only commands and hashes of a still table are compared)
  await call('screen_open', { name: 'table' });
  await sleep(300);
  const start = await hash();
  const info = await call('info');
  const stock = (await call('ui_query', { selector: '#stock' }));
  const box = stock[0].screen;  // window pixels
  const cx = Math.round(box.x + box.w / 2), cy = Math.round(box.y + box.h / 2);
  // Pointer: click the stock pile with real mouse events; the table draws a card (Moves goes up).
  const before = await moves();
  await mouse('mouseMoved', cx, cy, { button: 'none', clickCount: 0 });
  await sleep(100);
  await mouse('mousePressed', cx, cy);
  await sleep(60);
  await mouse('mouseReleased', cx, cy);
  await sleep(400);
  const afterClick = await moves();
  steps.push({ step: 'pointer click on #stock', at: [cx, cy], movesBefore: before, movesAfter: afterClick, ok: afterClick === before + 1 });
  // Keyboard: 'u' is Undo; the table returns to the dealt state (Moves back).
  await key('u', 'KeyU', 85);
  await sleep(400);
  const afterUndo = await moves();
  steps.push({ step: 'keyboard U (undo)', movesAfter: afterUndo, ok: afterUndo === before });
  // Keyboard focus navigation: Tab moves focus to a UI element.
  await key('Tab', 'Tab', 9);
  await sleep(300);
  const focused = await call('ui_query', { selector: ':focus' });
  const n = focused.length;
  steps.push({ step: 'keyboard Tab focuses an element', focusedElements: n, ok: n >= 1 });
  return { ok: steps.every(s => s.ok), info: { screen: info.screen, tick: info.tick }, startHash: start, steps };
}
