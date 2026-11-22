// A game's own HLSL material, drawn as the first draw of a frame in a web export: the green square of tests/WebGame appears, and the browser
// reports no "No bind group set" validation message. Needs hardware-adapter flags (see capture-test.mjs).
let nextId = 1;

export default async function ({ evalJs, sleep, logs }) {
  for (let i = 0; i < 300 && !(await evalJs('window.__control && window.__onegame && window.__onegame.frames > 10')); i++) await sleep(100);
  await evalJs(`__control(${JSON.stringify(JSON.stringify({ jsonrpc: '2.0', id: 0, method: 'initialize', params: { protocolVersion: '2025-06-18', capabilities: {}, clientInfo: { name: 'material-test', version: '1' } } }))})`);
  const checks = [];
  const check = (name, ok, detail) => checks.push({ name, ok: !!ok, ...(ok ? {} : { detail }) });
  const reply = JSON.parse(await evalJs(`__control(${JSON.stringify(JSON.stringify({ jsonrpc: '2.0', id: nextId++, method: 'tools/call', params: { name: 'capture_frame', arguments: { size: '320x200' } } }))})`));
  const img = reply.result.content.find(c => c.type === 'image');
  check('the capture returns a picture', img && img.data.length > 50, reply.result.content.map(c => c.type));
  // Decode in the page and read two pixels: the square (bottom right of the default camera) and the empty background.
  const px = JSON.parse(await evalJs(`(async () => {
    const bytes = Uint8Array.from(atob(${JSON.stringify(img.data)}), c => c.charCodeAt(0));
    const bmp = await createImageBitmap(new Blob([bytes], { type: 'image/png' }));
    const c = new OffscreenCanvas(bmp.width, bmp.height), g = c.getContext('2d'); g.drawImage(bmp, 0, 0);
    const at = (x, y) => Array.from(g.getImageData(x, y, 1, 1).data);
    return JSON.stringify({ w: bmp.width, h: bmp.height, swatch: at(300, 190), background: at(20, 20) });
  })()`));
  check('the picture has the asked size', px.w === 320 && px.h === 200, px);
  check('the square is drawn in the material\'s colour (green, from the shader\'s parameters)', px.swatch[1] > 200 && px.swatch[0] < 130 && px.swatch[2] < 170, px);
  check('and the background is not', px.background[1] < 60, px);
  check('no bind group validation message', !logs.some(l => /No bind group|Invalid CommandBuffer/.test(l)), logs.filter(l => /bind group|Invalid/.test(l)).slice(0, 2));
  return { ok: checks.every(c => c.ok), passed: checks.filter(c => c.ok).length, failed: checks.filter(c => !c.ok).length, checks };
}
