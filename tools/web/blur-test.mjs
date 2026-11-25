// Backdrop blur on WebGPU, in the web export of tests/WebGame: a capture of the bright square with and without the full-window blurring panel
// (Swatch.SetGlass). The sharpest horizontal step of the green channel must fall a lot, and nothing may be reported by the validation layer.
// Needs hardware-adapter flags (see capture-test.mjs).
let nextId = 1;

export default async function ({ evalJs, sleep, logs }) {
  for (let i = 0; i < 300 && !(await evalJs('window.__control && window.__onegame && window.__onegame.frames > 10')); i++) await sleep(100);
  await evalJs(`__control(${JSON.stringify(JSON.stringify({ jsonrpc: '2.0', id: 0, method: 'initialize', params: { protocolVersion: '2025-06-18', capabilities: {}, clientInfo: { name: 'blur-test', version: '1' } } }))})`);
  const checks = [];
  const check = (name, ok, detail) => checks.push({ name, ok: !!ok, ...(ok ? {} : { detail }) });
  const call = async (name, args = {}) => JSON.parse(await evalJs(`__control(${JSON.stringify(JSON.stringify({ jsonrpc: '2.0', id: nextId++, method: 'tools/call', params: { name, arguments: args } }))})`));
  const sharpest = async (reply) => {
    const img = reply.result.content.find(c => c.type === 'image');
    return JSON.parse(await evalJs(`(async () => {
      const bytes = Uint8Array.from(atob(${JSON.stringify(img.data)}), c => c.charCodeAt(0));
      const bmp = await createImageBitmap(new Blob([bytes], { type: 'image/png' }));
      const c = new OffscreenCanvas(bmp.width, bmp.height), g = c.getContext('2d'); g.drawImage(bmp, 0, 0);
      const d = g.getImageData(0, 0, bmp.width, bmp.height).data;
      let max = 0;
      for (let y = 0; y < bmp.height; y++) for (let x = 0; x + 1 < bmp.width; x++) max = Math.max(max, Math.abs(d[(y * bmp.width + x + 1) * 4 + 1] - d[(y * bmp.width + x) * 4 + 1]));
      return JSON.stringify({ w: bmp.width, max });
    })()`));
  };
  const sharp = await sharpest(await call('capture_frame', { size: '320x200' }));
  const sent = await call('command_send', { entity: 'Swatch', command: 'SetGlass', args: ['true'] });
  check('the command was accepted', !sent.error && !sent.result?.isError, sent);
  await sleep(500);
  const blurred = await sharpest(await call('capture_frame', { size: '320x200' }));
  check('the square has a sharp edge without the panel', sharp.max > 100, sharp);
  check('the panel softens every edge behind it', blurred.max < sharp.max * 0.4, { sharp, blurred });
  check('no WebGPU validation message', !logs.some(l => /Invalid|validation|No bind group/i.test(l)), logs.filter(l => /Invalid|validation|bind group/i.test(l)).slice(0, 2));
  return { ok: checks.every(c => c.ok), passed: checks.filter(c => c.ok).length, failed: checks.filter(c => !c.ok).length, checks };
}
