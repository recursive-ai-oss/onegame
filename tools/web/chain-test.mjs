// Render textures and post chains on WebGPU, in the web export of tests/WebGame: a layer with bloom and a tint pass of the game's own, and a
// render texture shown as a sprite. Needs hardware-adapter flags (see capture-test.mjs).
let nextId = 1;

export default async function ({ evalJs, sleep, logs }) {
  for (let i = 0; i < 300 && !(await evalJs('window.__control && window.__onegame && window.__onegame.frames > 10')); i++) await sleep(100);
  await evalJs(`__control(${JSON.stringify(JSON.stringify({ jsonrpc: '2.0', id: 0, method: 'initialize', params: { protocolVersion: '2025-06-18', capabilities: {}, clientInfo: { name: 'chain-test', version: '1' } } }))})`);
  const checks = [];
  const check = (name, ok, detail) => checks.push({ name, ok: !!ok, ...(ok ? {} : { detail }) });
  const reply = JSON.parse(await evalJs(`__control(${JSON.stringify(JSON.stringify({ jsonrpc: '2.0', id: nextId++, method: 'tools/call', params: { name: 'capture_frame', arguments: { size: '320x200' } } }))})`));
  const img = reply.result.content.find(c => c.type === 'image');
  check('the capture returns a picture', img && img.data.length > 50, reply.result.content.map(c => c.type));
  // Screen pixels of a 320x200 capture: the bright square is at (80..120, 70..110), its halo around it, the texture sprite at (184..216, 24..56)
  // on a part of the frame the swatch and the chain leave alone.
  const px = JSON.parse(await evalJs(`(async () => {
    const bytes = Uint8Array.from(atob(${JSON.stringify(img.data)}), c => c.charCodeAt(0));
    const bmp = await createImageBitmap(new Blob([bytes], { type: 'image/png' }));
    const c = new OffscreenCanvas(bmp.width, bmp.height), g = c.getContext('2d'); g.drawImage(bmp, 0, 0);
    const at = (x, y) => Array.from(g.getImageData(x, y, 1, 1).data);
    return JSON.stringify({ w: bmp.width, h: bmp.height, square: at(100, 90), halo: at(125, 90), far: at(160, 20), badge: at(200, 32), probe: at(106, 32), probeMid: at(120, 40), pre: at(20, 170), post: at(300, 170) });
  })()`));
  check('the picture has the asked size', px.w === 320 && px.h === 200, px);
  // The square is white; the game's pass multiplies red by one half, so it comes out cyan-ish.
  check('the layer\'s square went through the chain (red halved by the game\'s own pass)', px.square[0] > 100 && px.square[0] < 190 && px.square[1] > 230 && px.square[2] > 230, px);
  check('bloom lit the background beside the square', px.halo[1] > px.far[1] + 25, px);
  check('the render texture shows as a sprite in its own colour', px.badge[0] > 230 && px.badge[1] < 40 && px.badge[2] > 230, px);
  check('a texture cleared with BeginTarget in view code shows its colour and leaves the frame alone', px.probe[2] > 230 && px.probe[0] < 40 && px.probeMid[0] > 230 && px.probeMid[1] > 230 && px.probeMid[2] < 40 && px.pre[0] > 230 && px.pre[1] < 40 && px.post[1] > 200 && px.post[0] < 120 && px.badge[0] > 230 && px.square[1] > 230 && px.halo[1] > px.far[1] + 25, px);
  check('no WebGPU validation message', !logs.some(l => /Invalid|validation|No bind group/i.test(l)), logs.filter(l => /Invalid|validation|bind group/i.test(l)).slice(0, 2));
  return { ok: checks.every(c => c.ok), passed: checks.filter(c => c.ok).length, failed: checks.filter(c => !c.ok).length, checks };
}
