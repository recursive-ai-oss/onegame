// The control channel of a page: capture_frame answers while the page keeps drawing, and after a frame that threw (a view with a bug) the
// page goes on drawing and captures work again. Run on the dev export of tests/WebGame:
//   CHROME_GPU_FLAGS='--enable-unsafe-webgpu --enable-features=Vulkan --use-angle=vulkan --ignore-gpu-blocklist --disable-vulkan-surface' \
//     node run-page.mjs --root <export folder> --path '/index.html?control=page' --script capture-test.mjs
// (A page that presents needs a hardware adapter flags; the software one cannot read back while a canvas is configured.)
let nextId = 1;

export default async function ({ send, evalJs, sleep, logs }) {
  const call = async (name, args = {}) => {
    let reply;
    try { reply = JSON.parse(await evalJs(`__control(${JSON.stringify(JSON.stringify({ jsonrpc: '2.0', id: nextId++, method: 'tools/call', params: { name, arguments: args } }))})`)); } catch (e) { return { error: String(e.message || e) }; }
    if (reply.error) return { error: JSON.stringify(reply.error) };
    const text = reply.result.content[0].text;
    if (reply.result.isError) return { error: text };
    try { return JSON.parse(text); } catch { return { text }; }
  };
  for (let i = 0; i < 200 && !(await evalJs('window.__control && window.__onegame && window.__onegame.frames > 3')); i++) await sleep(100);
  await evalJs(`__control(${JSON.stringify(JSON.stringify({ jsonrpc: '2.0', id: 0, method: 'initialize', params: { protocolVersion: '2025-06-18', capabilities: {}, clientInfo: { name: 'capture-test', version: '1' } } }))})`);
  const checks = [];
  const check = (name, ok, detail) => checks.push({ name, ok: !!ok, ...(ok ? {} : { detail }) });
  const frames = () => evalJs('window.__onegame.frames');

  // Captures while the page runs, one after another and two at once.
  const first = await call('capture_frame', { size: '320x200' });
  check('a capture answers while the page runs', first.width === 320 && first.height === 200, first);
  const both = await Promise.all([call('capture_frame', { size: '160x100' }), call('capture_frame', { size: '200x120' })]);
  check('two captures at once both answer', both.every(r => !r.error), both);

  // A frame that throws: the loop goes on.
  const sent = await call('command_send', { entity: 'Swatch', command: 'Break', args: ['true'] });
  check('the command is accepted', !sent.error, sent);
  await sleep(1500);
  const f0 = await frames();
  await sleep(500);
  const f1 = await frames();
  const errors = await evalJs('window.__onegame.frameErrors || 0');
  check('a view that throws does not stop the page frames', f1 > f0 + 5, { f0, f1 });
  check('the failing frames are counted', errors > 0, errors);
  const broken = await call('capture_frame', { size: '160x100' });
  check('a capture of a failing view says what failed, not that a frame was never executed', broken.error && /broken on purpose/.test(broken.error) && !/never executed/.test(broken.error), broken);

  await call('command_send', { entity: 'Swatch', command: 'Break', args: ['false'] });
  await sleep(800);
  const again = await call('capture_frame', { size: '320x200' });
  check('after the view works again, captures work again', again.width === 320 && !again.error, again);
  const after = await frames();
  await sleep(300);
  check('and the page still draws', (await frames()) > after, after);
  check('no frame failed with "never executed" at any point', !logs.some(l => /never executed/.test(l)), logs.filter(l => /never executed/.test(l)).slice(0, 2));
  return { ok: checks.every(c => c.ok), passed: checks.filter(c => c.ok).length, failed: checks.filter(c => !c.ok).length, checks };
}
