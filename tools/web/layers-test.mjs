// Passes whose first draw is a material of the game's own (a layer into a render texture, a layer with a post chain), in the web export of tests/WebGame:
// each must draw, and the browser must report no "No bind group set" validation message. Needs hardware-adapter flags (see capture-test.mjs).
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
    return JSON.stringify({ w: bmp.width, h: bmp.height, post: at(220, 40), into: at(260, 40), background: at(20, 20) });
  })()`));
  check('the picture has the asked size', px.w === 320 && px.h === 200, px);
  // The material multiplies white by (1, 0.5, 0): orange (half green is 188 once encoded).
  check('a layer with a post chain draws a material first', px.post[0] > 200 && px.post[1] > 150 && px.post[1] < 220 && px.post[2] < 60, px);
  check('a layer into a render texture draws a material first', px.into[0] > 200 && px.into[1] > 150 && px.into[1] < 220 && px.into[2] < 60, px);
  check('no bind group validation message', !logs.some(l => /No bind group|Invalid CommandBuffer/.test(l)), logs.filter(l => /bind group|Invalid/.test(l)).slice(0, 2));
  return { ok: checks.every(c => c.ok), passed: checks.filter(c => c.ok).length, failed: checks.filter(c => !c.ok).length, checks };
}
