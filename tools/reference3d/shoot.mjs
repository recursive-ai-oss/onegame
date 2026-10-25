// Renders the reference images of testdata/render3d/cases.json with the Khronos glTF Sample Viewer, used as a black box: its web page is
// loaded in headless Chrome, driven through its controls, and its own "Download as .png" button saves the canvas.
//
// Each case gets a copy of its model with a camera (and, for lit cases, KHR_lights_punctual lights and a baked animation pose) added to the
// glTF JSON, so the engine and the viewer see exactly the same camera. Test tooling only; nothing here ships.
//
//   npm install puppeteer-core
//   node shoot.mjs --cases ../../testdata/render3d/cases.json --models <samples dir> --env <hdr dir> --out <dir> [--only name] [--chrome path]
import puppeteer from 'puppeteer-core';
import http from 'http';
import fs from 'fs';
import path from 'path';

const args = process.argv.slice(2);
const opt = (k, d) => { const i = args.indexOf(k); return i >= 0 ? args[i + 1] : d; };
const spec = JSON.parse(fs.readFileSync(opt('--cases'), 'utf8'));
const modelsDir = path.resolve(opt('--models'));
const envDir = path.resolve(opt('--env'));
const outDir = path.resolve(opt('--out'));
const only = opt('--only');
const chrome = opt('--chrome', '/usr/bin/google-chrome');
const [W, H] = spec.size;
const PANEL = 384; // width of the viewer's side panel: the canvas is the rest of the window
fs.mkdirSync(outDir, { recursive: true });

function lookAtMatrix(eye, target, up) {
  const sub = (a, b) => a.map((v, i) => v - b[i]);
  const cross = (a, b) => [a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0]];
  const norm = (a) => { const l = Math.hypot(...a); return a.map((v) => v / l); };
  const z = norm(sub(eye, target));
  const x = norm(cross(up, z));
  const y = cross(z, x);
  return [...x, 0, ...y, 0, ...z, 0, ...eye, 1]; // glTF matrices are column-major
}

function quatToMat(q, t, s) {
  const [x, y, z, w] = q;
  return [
    (1 - 2 * (y * y + z * z)) * s[0], 2 * (x * y + z * w) * s[0], 2 * (x * z - y * w) * s[0], 0,
    2 * (x * y - z * w) * s[1], (1 - 2 * (x * x + z * z)) * s[1], 2 * (y * z + x * w) * s[1], 0,
    2 * (x * z + y * w) * s[2], 2 * (y * z - x * w) * s[2], (1 - 2 * (x * x + y * y)) * s[2], 0,
    t[0], t[1], t[2], 1,
  ];
}

function directionToQuat(dir) {
  // Rotation taking the glTF light axis (0, 0, -1) to dir.
  const d = Math.hypot(...dir);
  const v = dir.map((c) => c / d);
  const from = [0, 0, -1];
  const dot = from[0] * v[0] + from[1] * v[1] + from[2] * v[2];
  if (dot < -0.999999) return [1, 0, 0, 0];
  const axis = [from[1] * v[2] - from[2] * v[1], from[2] * v[0] - from[0] * v[2], from[0] * v[1] - from[1] * v[0]];
  const q = [axis[0], axis[1], axis[2], 1 + dot];
  const l = Math.hypot(...q);
  return q.map((c) => c / l);
}

function readAccessor(json, buffers, index) {
  const a = json.accessors[index];
  const bv = json.bufferViews[a.bufferView];
  const comps = { SCALAR: 1, VEC3: 3, VEC4: 4 }[a.type];
  const buf = buffers[bv.buffer];
  const base = (bv.byteOffset || 0) + (a.byteOffset || 0);
  const stride = bv.byteStride || comps * 4;
  const out = [];
  for (let i = 0; i < a.count; i++) {
    const row = [];
    for (let c = 0; c < comps; c++) row.push(buf.readFloatLE(base + i * stride + c * 4));
    out.push(row);
  }
  return out;
}

function bakePose(json, dir, anim, time) {
  const buffers = json.buffers.map((b) => fs.readFileSync(path.join(dir, b.uri)));
  const clip = json.animations.find((a) => a.name === anim);
  for (const ch of clip.channels) {
    const s = clip.samplers[ch.sampler];
    const times = readAccessor(json, buffers, s.input).map((r) => r[0]);
    const vals = readAccessor(json, buffers, s.output);
    const t = Math.min(Math.max(time, times[0]), times[times.length - 1]);
    let k = 0;
    while (k + 2 < times.length && times[k + 1] < t) k++;
    const f = times[k + 1] === times[k] ? 0 : (t - times[k]) / (times[k + 1] - times[k]);
    let v;
    if (ch.target.path === 'rotation') {
      const a = vals[k];
      let b = vals[k + 1];
      let dot = a[0] * b[0] + a[1] * b[1] + a[2] * b[2] + a[3] * b[3];
      if (dot < 0) { b = b.map((c) => -c); dot = -dot; }
      if (dot > 0.9995) {
        v = a.map((c, i) => c + (b[i] - c) * f);
      } else {
        const th = Math.acos(dot);
        const sa = Math.sin((1 - f) * th) / Math.sin(th);
        const sb = Math.sin(f * th) / Math.sin(th);
        v = a.map((c, i) => c * sa + b[i] * sb);
      }
      const l = Math.hypot(...v);
      v = v.map((c) => c / l);
    } else {
      v = vals[k].map((c, i) => c + (vals[k + 1][i] - c) * f);
    }
    json.nodes[ch.target.node][ch.target.path] = v;
  }
  delete json.animations;
}

function prepare(c) {
  const file = path.join(modelsDir, c.model);
  const dir = path.dirname(file);
  const json = JSON.parse(fs.readFileSync(file, 'utf8'));
  if (c.pose) bakePose(json, dir, c.pose.animation, c.pose.time);
  json.cameras = [{ type: 'perspective', perspective: { yfov: c.camera.yfov, znear: c.camera.near, zfar: c.camera.far, aspectRatio: W / H } }];
  json.nodes.push({ name: 'reference camera', camera: 0, matrix: lookAtMatrix(c.camera.eye, c.camera.target, c.camera.up) });
  const scene = json.scenes[json.scene || 0];
  scene.nodes.push(json.nodes.length - 1);
  if (c.lights?.length) {
    json.extensionsUsed = [...(json.extensionsUsed || []), 'KHR_lights_punctual'];
    json.extensions = { ...(json.extensions || {}), KHR_lights_punctual: { lights: [] } };
    for (const l of c.lights) {
      const def = { type: l.type, color: l.color, intensity: l.intensity };
      if (l.range) def.range = l.range;
      if (l.type === 'spot') def.spot = { innerConeAngle: l.inner, outerConeAngle: l.outer };
      json.extensions.KHR_lights_punctual.lights.push(def);
      const node = { name: l.type + ' light', extensions: { KHR_lights_punctual: { light: json.extensions.KHR_lights_punctual.lights.length - 1 } } };
      if (l.position) node.translation = l.position;
      if (l.direction) node.rotation = directionToQuat(l.direction);
      json.nodes.push(node);
      scene.nodes.push(json.nodes.length - 1);
    }
  }
  return { json, dir };
}

async function shoot(c) {
  const { json, dir } = prepare(c);
  const body = Buffer.from(JSON.stringify(json));
  const server = http.createServer((req, res) => {
    const url = decodeURIComponent(req.url.split('?')[0]);
    if (url === '/reference.gltf') { res.writeHead(200, { 'Access-Control-Allow-Origin': '*', 'Content-Type': 'model/gltf+json' }); res.end(body); return; }
    if (url === '/environment.hdr') { res.writeHead(200, { 'Access-Control-Allow-Origin': '*' }); fs.createReadStream(path.join(envDir, c.env + '_1k.hdr')).pipe(res); return; }
    const p = path.join(dir, url);
    if (!p.startsWith(dir) || !fs.existsSync(p)) { res.writeHead(404); res.end(); return; }
    res.writeHead(200, { 'Access-Control-Allow-Origin': '*' });
    fs.createReadStream(p).pipe(res);
  }).listen(0);
  const port = server.address().port;
  const dl = path.join(outDir, 'download-' + c.name);
  fs.mkdirSync(dl, { recursive: true });
  const browser = await puppeteer.launch({
    executablePath: chrome, headless: 'new',
    args: ['--no-sandbox', '--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--ignore-gpu-blocklist', '--disable-web-security'],
    userDataDir: path.join(outDir, 'chrome-' + c.name),
  });
  try {
    const page = await browser.newPage();
    await page.setViewport({ width: W + PANEL, height: H });
    const cdp = await page.createCDPSession();
    await cdp.send('Browser.setDownloadBehavior', { behavior: 'allow', downloadPath: dl });
    await page.goto(`https://github.khronos.org/glTF-Sample-Viewer-Release/?model=http://localhost:${port}/reference.gltf`, { waitUntil: 'networkidle2', timeout: 180000 });
    await new Promise((r) => setTimeout(r, 4000));
    const click = (t) => page.evaluate((text) => { for (const e of document.querySelectorAll('*')) if (e.children.length === 0 && e.textContent.trim() === text) { e.click(); return true; } return false; }, t);

    await click('Models');
    await new Promise((r) => setTimeout(r, 500));
    for (const s of await page.$$('select')) {
      const texts = await s.evaluate((e) => [...e.options].map((o) => o.text));
      if (texts.includes('User Camera')) {
        // The viewer ignores a programmatic value change; a key press on the focused control is a real change. Option 1 is the camera added above.
        await s.focus();
        await page.keyboard.press('ArrowDown');
        await page.keyboard.press('Enter');
      }
    }
    await click('Display');
    await new Promise((r) => setTimeout(r, 500));
    if (c.env) {
      const input = await page.$('input[type=file]');
      await input.uploadFile(path.join(envDir, c.env + '_1k.hdr'));
      await new Promise((r) => setTimeout(r, 5000));
    }
    const toggles = await page.$$('input[type=checkbox]');
    const set = async (i, on) => { if ((await toggles[i].evaluate((e) => e.checked)) !== on) await toggles[i].evaluate((e) => e.click()); };
    await set(0, !!c.ibl);          // Image Based
    await set(1, true);             // Punctual Lighting
    await set(2, false);            // Environment Map as background: off, so the backdrop is one flat colour
    await page.evaluate((hex) => {
      const e = document.querySelector('input[type=color]');
      Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value').set.call(e, hex);
      e.dispatchEvent(new Event('input', { bubbles: true }));
      e.dispatchEvent(new Event('change', { bubbles: true }));
    }, spec.background.color);
    await new Promise((r) => setTimeout(r, 4000));
    for (const b of await page.$$('button')) {
      if ((await b.evaluate((e) => e.textContent.trim())) === 'Download as .png') { await b.click(); break; }
    }
    await new Promise((r) => setTimeout(r, 3000));
    const files = fs.readdirSync(dl);
    if (!files.length) throw new Error('no download for ' + c.name);
    fs.copyFileSync(path.join(dl, files[0]), path.join(outDir, c.name + '.png'));
    if (args.includes('--page')) await page.screenshot({ path: path.join(outDir, c.name + '-page.png') });
  } finally {
    await browser.close();
    server.close();
    fs.rmSync(dl, { recursive: true, force: true });
    fs.rmSync(path.join(outDir, 'chrome-' + c.name), { recursive: true, force: true });
  }
}

for (const c of spec.cases) {
  if (only && c.name !== only) continue;
  console.log('shooting', c.name);
  await shoot(c);
}
