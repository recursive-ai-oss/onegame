// Serves a folder, opens a page in headless Chrome with WebGPU, waits for the page to set window.__result and prints it.
// A test tool: nothing here ships.
//   node run-page.mjs --root <dir> [--path /index.html?x=1] [--timeout 300] [--out file.json] [--chrome-flags "..."] [--eval "js"] [--screenshot file.png]
import http from 'node:http';
import fs from 'node:fs';
import path from 'node:path';
import { spawn } from 'node:child_process';

const args = Object.fromEntries(process.argv.slice(2).reduce((a, v, i, all) => (v.startsWith('--') ? [...a, [v.slice(2), all[i + 1]]] : a), []));
const root = path.resolve(args.root);
const timeout = Number(args.timeout || 300) * 1000;
const types = { '.html': 'text/html', '.js': 'text/javascript', '.mjs': 'text/javascript', '.wasm': 'application/wasm', '.json': 'application/json', '.png': 'image/png', '.dat': 'application/octet-stream', '.css': 'text/css', '.yaml': 'text/plain', '.svg': 'image/svg+xml' };

const server = http.createServer((req, res) => {
  const url = new URL(req.url, 'http://x');
  let file = path.join(root, decodeURIComponent(url.pathname));
  if (!file.startsWith(root)) { res.writeHead(403).end(); return; }
  if (fs.existsSync(file) && fs.statSync(file).isDirectory()) file = path.join(file, 'index.html');
  if (!fs.existsSync(file)) { res.writeHead(404).end('not found'); return; }
  res.writeHead(200, { 'content-type': types[path.extname(file)] || 'application/octet-stream', 'cache-control': 'no-store' });
  fs.createReadStream(file).pipe(res);
});
await new Promise(r => server.listen(0, '127.0.0.1', r));
const httpPort = server.address().port;

const dbg = 9300 + Math.floor(Math.random() * 600);
const profile = path.join(process.env.TMPDIR || '/tmp', 'chrome-profile-' + dbg);
const chromeFlags = (args['chrome-flags'] || '').split(' ').filter(Boolean);
const chrome = spawn(process.env.ONEGAME_CHROME || 'google-chrome', [
  ...(process.env.CHROME_HEADED ? [] : ['--headless=new']), ...(process.env.CHROME_LOG ? ['--enable-logging=stderr', '--v=0'] : []), '--no-sandbox', ...(process.env.CHROME_GPU_FLAGS ? process.env.CHROME_GPU_FLAGS.split(' ') : ['--enable-unsafe-webgpu', '--use-webgpu-adapter=swiftshader', '--enable-unsafe-swiftshader', '--ignore-gpu-blocklist']),
  '--remote-debugging-port=' + dbg, '--user-data-dir=' + profile, '--window-size=1280,800', '--hide-scrollbars', '--mute-audio', ...chromeFlags,
  `http://127.0.0.1:${httpPort}${args.path || '/index.html'}`,
], { stdio: process.env.CHROME_LOG ? ['ignore', 'ignore', fs.openSync(process.env.CHROME_LOG, 'w')] : 'ignore' });

const done = (code) => { try { chrome.kill('SIGKILL'); } catch { } server.close(); fs.rmSync(profile, { recursive: true, force: true }); process.exit(code); };
process.on('SIGINT', () => done(130));

let wsUrl;
for (let i = 0; i < 100 && !wsUrl; i++) {
  await new Promise(r => setTimeout(r, 200));
  try { const list = await (await fetch(`http://127.0.0.1:${dbg}/json`)).json(); wsUrl = list.find(t => t.type === 'page')?.webSocketDebuggerUrl; } catch { }
}
if (!wsUrl) { console.error('chrome did not start'); done(2); }
const ws = new WebSocket(wsUrl);
await new Promise(r => ws.onopen = r);
let id = 0; const pending = new Map(); const logs = [];
ws.onmessage = m => {
  const d = JSON.parse(m.data);
  if (d.id && pending.has(d.id)) pending.get(d.id)(d);
  else if (d.method === 'Runtime.consoleAPICalled') logs.push(d.params.type + ': ' + d.params.args.map(a => a.value ?? a.description ?? '').join(' '));
  else if (d.method === 'Log.entryAdded') logs.push('log ' + d.params.entry.level + ': ' + d.params.entry.text);
  else if (d.method === 'Runtime.exceptionThrown') logs.push('exception: ' + (d.params.exceptionDetails.exception?.description || d.params.exceptionDetails.text));
};
const send = (method, params = {}) => new Promise(r => { const i = ++id; pending.set(i, r); ws.send(JSON.stringify({ id: i, method, params })); });
await send('Runtime.enable');
await send('Log.enable');
const start = Date.now();
let result;
if (args.script) {
  // A test script drives the page: it gets the CDP send function, a helper to evaluate JavaScript in the page and a sleep.
  const evalJs = async (expression) => { const r = await send('Runtime.evaluate', { expression, returnByValue: true, awaitPromise: true }); if (r.result?.exceptionDetails) throw new Error(r.result.exceptionDetails.exception?.description || 'eval failed'); return r.result?.result?.value; };
  const mod = await import(path.resolve(args.script));
  try { result = JSON.stringify(await mod.default({ send, evalJs, sleep: (ms) => new Promise(r => setTimeout(r, ms)), logs })); } catch (e) { result = JSON.stringify({ ok: false, error: String(e && e.stack || e) }); }
}
while (!result && Date.now() - start < timeout) {
  await new Promise(r => setTimeout(r, 500));
  const r = await send('Runtime.evaluate', { expression: args.eval || 'window.__result', returnByValue: true });
  if (r.result?.result?.value) { result = r.result.result.value; break; }
}
if (args.screenshot) {
  const shot = await send('Page.captureScreenshot', { format: 'png' });
  if (shot.result?.data) fs.writeFileSync(args.screenshot, Buffer.from(shot.result.data, 'base64'));
}
if (args.out && result) fs.writeFileSync(args.out, typeof result === 'string' ? result : JSON.stringify(result));
if (args['log'] || !result) console.error(logs.join('\n'));
console.log(result ? (typeof result === 'string' ? result.slice(0, Number(args.print || 4000)) : JSON.stringify(result)) : 'TIMEOUT');
done(result ? 0 : 1);
