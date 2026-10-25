import { dotnet } from './_framework/dotnet.js';
import * as gpu from './onegame-gpu.js';

const log = document.getElementById('log');
const put = async (R, dst, url) => { const r = await fetch(url); if (!r.ok) throw new Error(url + ' ' + r.status); R.Put(dst, new Uint8Array(await r.arrayBuffer())); };
try {
  const names = (new URLSearchParams(location.search).get('cases') || '').split(',').filter(Boolean);
  const runtime = await dotnet.withDiagnosticTracing(false).create();
  runtime.setModuleImports('onegame-gpu', gpu);
  const config = runtime.getConfig();
  const exports = await runtime.getAssemblyExports(config.mainAssemblyName);
  const R = exports.OneGame.Render3D.Tests.Runner;
  await put(R, '/repo/testdata/render3d/cases.json', '/testdata/render3d/cases.json');
  const cases = (await (await fetch('/testdata/render3d/cases.json')).json()).cases;
  const results = [];
  for (const name of names) {
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
  window.__result = JSON.stringify({ adapter: await gpu.takeMessages(), results });
} catch (e) {
  window.__result = JSON.stringify({ fatal: String(e && e.stack || e) });
  log.textContent = 'fatal: ' + e;
}
