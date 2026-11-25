// Like run-page.mjs, but the browser is Safari, driven through safaridriver (W3C WebDriver). Serves a folder, opens a page, and runs a test script
// that has the same shape as the Chrome ones: default export ({ evalJs, sleep, logs, driver }). The scripts that only use evalJs, sleep and logs
// (title, capture, blur, chain, material) run unchanged. `send` (Chrome DevTools) does not exist here: a script that needs emulation or raw input
// uses `driver` (W3C commands: window rect, pointer actions, screenshots) instead. Console output reaches `logs` through a hook this tool writes into the
// served index.html. A test tool: nothing here ships.
//   safaridriver -p 4444 &   (Safari needs "Allow Remote Automation" and an unlocked desktop session: it runs no animation frames for a hidden page)
//   node run-page-safari.mjs --root <dir> [--path /index.html?x=1] [--script file.mjs] [--timeout 300] [--screenshot file.png] [--print 3000]
import http from 'node:http';
import fs from 'node:fs';
import path from 'node:path';
import { spawnSync } from 'node:child_process';

const args = Object.fromEntries(process.argv.slice(2).reduce((a, v, i, all) => (v.startsWith('--') ? [...a, [v.slice(2), all[i + 1]]] : a), []));
const root = path.resolve(args.root);
const timeout = Number(args.timeout || 300) * 1000;
const driverUrl = process.env.WEBDRIVER_URL || 'http://127.0.0.1:4444';
const types = { '.html': 'text/html', '.js': 'text/javascript', '.mjs': 'text/javascript', '.wasm': 'application/wasm', '.json': 'application/json', '.png': 'image/png', '.css': 'text/css', '.svg': 'image/svg+xml' };

// Installed first thing in the page itself, because the game keeps its own reference to console.log when it starts.
const hook = `<script>if (!window.__ogLogs) { window.__ogLogs = []; const push = (k, a) => window.__ogLogs.push(k + ': ' + Array.from(a).map(x => { try { return typeof x === 'string' ? x : (x && x.stack) || JSON.stringify(x); } catch { return String(x); } }).join(' '));
  for (const k of ['log', 'info', 'warn', 'error', 'debug']) { const o = console[k].bind(console); console[k] = (...a) => { push(k, a); o(...a); }; }
  window.addEventListener('error', e => window.__ogLogs.push('exception: ' + e.message));
  window.addEventListener('unhandledrejection', e => window.__ogLogs.push('exception: ' + e.reason)); }</script>`;
// --mounts "/testdata=<dir>,/models=<dir>" serves more folders under URL prefixes (the 3D runner page fetches its test data that way).
const mounts = (args.mounts || '').split(',').filter(Boolean).map(m => { const [prefix, dir] = m.split('='); return [prefix, path.resolve(dir)]; });
const server = http.createServer((req, res) => {
  const url = new URL(req.url, 'http://x');
  const mount = mounts.find(([prefix]) => url.pathname.startsWith(prefix + '/'));
  let file = mount ? path.join(mount[1], decodeURIComponent(url.pathname.slice(mount[0].length))) : path.join(root, decodeURIComponent(url.pathname));
  if (!mount && !file.startsWith(root)) { res.writeHead(403).end(); return; }
  if (fs.existsSync(file) && fs.statSync(file).isDirectory()) file = path.join(file, 'index.html');
  if (!fs.existsSync(file)) { res.writeHead(404).end('not found'); return; }
  res.writeHead(200, { 'content-type': types[path.extname(file)] || 'application/octet-stream', 'cache-control': 'no-store' });
  if (path.extname(file) === '.html') { res.end(fs.readFileSync(file, 'utf8').replace(/<head>/i, '<head>' + hook)); return; }
  fs.createReadStream(file).pipe(res);
});
await new Promise(r => server.listen(0, '127.0.0.1', r));
const httpPort = server.address().port;

let session = null;
const wd = async (method, p, body) => {
  const r = await fetch(`${driverUrl}${p}`, { method, headers: { 'content-type': 'application/json' }, body: body === undefined ? undefined : JSON.stringify(body), signal: AbortSignal.timeout(Number(process.env.WEBDRIVER_COMMAND_MS || 90000)) });
  const j = await r.json();
  if (j.value?.error) throw new Error(`${j.value.error}: ${j.value.message}`);
  return j.value;
};
const done = async (code) => {
  try { if (session) await wd('DELETE', `/session/${session}`); } catch { }
  server.close();
  process.exit(code);
};
process.on('SIGINT', () => done(130));
process.on('uncaughtException', (e) => { console.error(String(e && e.stack || e)); done(2); });
process.on('unhandledRejection', (e) => { console.error(String(e && e.stack || e)); done(2); });

const created = await wd('POST', '/session', { capabilities: { alwaysMatch: { browserName: 'safari', pageLoadStrategy: 'none' } } });
session = created.sessionId;
const s = (p) => `/session/${session}${p}`;
await wd('POST', s('/timeouts'), { script: 600000, pageLoad: 120000 });
// Safari stops animation frames for a window that is not in front, so the game would never draw.
// (`open -a` needs no Apple-event permission; osascript would wait for a prompt nobody can answer over ssh.)
if (process.platform === 'darwin') spawnSync('open', ['-a', 'Safari'], { timeout: 10000 });
await wd('POST', s('/url'), { url: `http://127.0.0.1:${httpPort}${args.path || '/index.html'}` });

// With pageLoadStrategy "none" the navigation call returns at once; the new document replaces about:blank some seconds later.
for (let i = 0; i < 600; i++) {
  const href = await wd('POST', s('/execute/sync'), { script: 'return location.href', args: [] });
  if (href.startsWith(`http://127.0.0.1:${httpPort}`)) break;
  await new Promise(r => setTimeout(r, 200));
}

const logs = [];
const pull = async () => { const n = await wd('POST', s('/execute/sync'), { script: 'const l = window.__ogLogs.splice(0); return l;', args: [] }); logs.push(...n.map(l => l.replace(/^log: /, 'log: '))); };
const evalJs = async (expression) => {
  const r = await wd('POST', s('/execute/async'), {
    script: `const done = arguments[arguments.length - 1]; const src = ${JSON.stringify(expression)}; let f; try { f = new Function('return (async () => (' + src + '\\n))()'); } catch { f = () => (0, eval)(src); } Promise.resolve().then(f).then(v => done({ v }), e => done({ e: String(e && e.stack || e) }));`, args: [],
  });
  await pull();
  if (r.e) throw new Error(r.e);
  return r.v;
};
const sleep = (ms) => new Promise(r => setTimeout(r, ms));
const timer = setInterval(() => pull().catch(() => { }), 250);

let result;
if (args.script) {
  const mod = await import(path.resolve(args.script));
  const watchdog = new Promise((_, rej) => setTimeout(() => rej(new Error('script timed out')), timeout));
  try { result = JSON.stringify(await Promise.race([mod.default({ evalJs, sleep, logs, driver: { wd, s } }), watchdog])); } catch (e) { result = JSON.stringify({ ok: false, error: String(e && e.stack || e) }); }
}
// No script: wait for the page to publish window.__result (the runner pages do), as run-page.mjs does.
for (const start = Date.now(); !result && !args.script && Date.now() - start < timeout;) {
  await sleep(500);
  const r = await wd('POST', s('/execute/sync'), { script: 'return window.__result || null', args: [] }).catch(() => null);
  if (r) result = typeof r === 'string' ? r : JSON.stringify(r);
}
clearInterval(timer);
if (!result || args.log) console.error(logs.join('\n'));
console.log(result ? result.slice(0, Number(args.print || 4000)) : 'TIMEOUT');
if (args.screenshot) {
  try { fs.writeFileSync(args.screenshot, Buffer.from(await wd('GET', s('/screenshot')), 'base64')); } catch (e) { console.error('screenshot failed: ' + e.message); }
}
await done(result ? 0 : 1);
