import { dotnet } from './_framework/dotnet.js';
import * as gpu from './_content/RecursiveAI.OneGpu.WebGpu/onegpu.js';   // the script the WebGpu package publishes; a page that imports it must also register it (below)

const log = document.getElementById('log');
const put = async (R, dst, url) => { const r = await fetch(url); if (!r.ok) throw new Error(url + ' ' + r.status); R.Put(dst, new Uint8Array(await r.arrayBuffer())); };
try {
  const names = (new URLSearchParams(location.search).get('cases') || '').split(',').filter(Boolean);
  const runtime = await dotnet.withDiagnosticTracing(false).create();
  runtime.setModuleImports('onegpu', gpu);
  const config = runtime.getConfig();
  const exports = await runtime.getAssemblyExports(config.mainAssemblyName);
  const R = exports.OneGame.Render3D.Tests.Runner;
  await put(R, '/repo/testdata/render3d/cases.json', '/testdata/render3d/cases.json');
  const cases = (await (await fetch('/testdata/render3d/cases.json')).json()).cases;
  const results = [];
  if (new URLSearchParams(location.search).get('layers')) results.push(JSON.parse(await R.RunLayers()));
  if (new URLSearchParams(location.search).get('fog')) {
    const r = JSON.parse(await R.RunFog());
    window.__result = JSON.stringify({ adapter: gpu.takeMessages(0), results: [r] });
    log.textContent = 'fog: ' + (r.ok ? 'ok' : 'FAIL');
    throw new Error('__done');
  }
  for (const name of names) {
    if (name === 'envload') {   // self-contained: an asynchronous environment against the synchronous one
      const started = performance.now();
      const r = JSON.parse(await R.RunEnvironmentLoad());
      r.ms = Math.round(performance.now() - started);
      results.push(r);
      continue;
    }
    if (name === 'skinned') {   // a crowd in one skinned batch against the same models drawn one by one
      for (const f of await (await fetch('/__list?path=/models/Fox/glTF')).json()) await put(R, '/models/Fox/glTF/' + f, '/models/Fox/glTF/' + f);
      const started = performance.now();
      const r = JSON.parse(await R.RunSkinnedInstances());
      r.ms = Math.round(performance.now() - started);
      results.push(r);
      continue;
    }
    if (name === 'shafts') {   // light shafts against the software rasterizer's reference
      await put(R, '/ref/shafts_slit.png', '/testdata/render3d/m4/shafts_slit.png');
      const started = performance.now();
      const r = JSON.parse(await R.RunShafts());
      r.ms = Math.round(performance.now() - started);
      results.push(r);
      continue;
    }
    const c = cases.find(x => x.name === name);
    const dir = c.model.split('/').slice(0, -2).join('/') + '/' + c.model.split('/').slice(-2, -1)[0];   // <Model>/glTF
    const files = await (await fetch('/__list?path=' + encodeURIComponent('/models/' + dir))).json();
    for (const f of files) await put(R, '/models/' + dir + '/' + f, '/models/' + dir + '/' + f);
    if (c.env) await put(R, '/env/' + c.env + '_1k.hdr', '/env/' + c.env + '_1k.hdr');
    await put(R, '/ref/' + name + '.png', '/testdata/render3d/ref/' + name + '.png');
    const started = performance.now();
    const r = JSON.parse(await R.Run(name));
    r.ms = Math.round(performance.now() - started);
    results.push(r);
    log.textContent = results.map(x => x.name + ': ' + (x.ok ? 'ok' : 'FAIL')).join('\n');
  }
  window.__result = JSON.stringify({ adapter: gpu.takeMessages(0), results });
} catch (e) {
  if (!String(e).includes('__done')) {
    window.__result = JSON.stringify({ fatal: String(e && e.stack || e) });
    log.textContent = 'fatal: ' + e;
  }
}
