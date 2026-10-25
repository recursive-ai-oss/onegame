// A browser client of a native Lanterns island server over WebSocket: the page connects with --connect, and this script checks that the
// replica holds exactly what the server says that audience must hold at the same tick (hash_at, the server's per-audience views).
// Environment: LANTERNS_SOCK is the server's live-control Unix socket (printed in its ready line).
import net from 'node:net';

function serverSession(sock) {
  const conn = net.createConnection(sock);
  let buf = '', next = 1;
  const waiting = new Map();
  conn.on('data', (d) => {
    buf += d.toString();
    let i;
    while ((i = buf.indexOf('\n')) >= 0) {
      const line = buf.slice(0, i); buf = buf.slice(i + 1);
      if (!line.trim()) continue;
      const m = JSON.parse(line);
      if (m.id !== undefined && waiting.has(m.id)) { waiting.get(m.id)(m); waiting.delete(m.id); }
    }
  });
  const rpc = (method, params) => new Promise((resolve) => { const id = next++; waiting.set(id, resolve); conn.write(JSON.stringify({ jsonrpc: '2.0', id, method, params }) + '\n'); });
  return {
    async init() { await rpc('initialize', { protocolVersion: '2025-06-18', capabilities: {}, clientInfo: { name: 'web-test', version: '1' } }); },
    async call(name, args = {}) { const r = await rpc('tools/call', { name, arguments: args }); return JSON.parse(r.result.content[0].text); },
    close() { conn.end(); },
  };
}

let nextId = 1;
export default async function ({ evalJs, sleep }) {
  const page = async (name, args = {}) => {
    const reply = JSON.parse(await evalJs(`__control(${JSON.stringify(JSON.stringify({ jsonrpc: '2.0', id: nextId++, method: 'tools/call', params: { name, arguments: args } }))})`));
    if (reply.error) throw new Error(name + ': ' + JSON.stringify(reply.error));
    const text = reply.result.content[0].text;
    if (reply.result.isError) throw new Error(name + ': ' + text);
    return JSON.parse(text);
  };
  for (let i = 0; i < 300 && !(await evalJs('window.__control && window.__onegame && window.__onegame.frames > 3')); i++) await sleep(100);
  await evalJs(`__control(${JSON.stringify(JSON.stringify({ jsonrpc: '2.0', id: 0, method: 'initialize', params: { protocolVersion: '2025-06-18', capabilities: {}, clientInfo: { name: 'web-test', version: '1' } } }))})`);
  const server = serverSession(process.env.LANTERNS_SOCK);
  await server.init();
  // Pump the connection until the replica has run for a while on the server's ticks.
  let status = null;
  for (let i = 0; i < 100; i++) {
    try { status = await page('net_status'); } catch { status = null; }
    if (status && status.tick > 120) break;
    await sleep(200);
  }
  const checks = [];
  // Hash at one tick must be compared at the same tick on both sides: hold the page still (it keeps drawing), read the replica's tick and view hash.
  await evalJs(`__control(${JSON.stringify(JSON.stringify({ jsonrpc: '2.0', id: nextId++, method: 'tools/call', params: { name: 'info', arguments: {} } }))})`);
  const samples = [];
  for (let k = 0; k < 5; k++) {
    await sleep(500);
    const st = await page('net_status');
    const tick = st.tick, hash = st.hash;
    const at = await server.call('hash_at', { tick });
    const scope = at.find(s => s.scope === (st.scope ?? 1)) ?? at[0];
    const matched = !!scope && (scope.hash === hash || (scope.views ?? []).includes(hash));
    samples.push({ tick, replicaHash: hash, serverHash: scope?.hash ?? null, serverViews: scope?.views ?? [], matched, drift: st.drift ?? null });
  }
  checks.push({ name: 'replica view hash equals the server audience view at the same tick (5 samples)', ok: samples.every(s => s.matched) });
  checks.push({ name: 'the page reports zero drift', ok: samples.every(s => !s.drift) });
  const clients = await server.call('clients');
  checks.push({ name: 'the server lists the browser as a WebSocket client', ok: Array.isArray(clients) && clients.some(c => c.transport === 'ws'), clients: clients.map(c => ({ name: c.name, transport: c.transport, state: c.state })) });
  server.close();
  return { ok: checks.every(c => c.ok), checks, samples, status };
}
