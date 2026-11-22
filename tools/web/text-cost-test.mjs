// What screen text costs the page: main-thread time per frame with 100 lines of ScreenText against none, at 1x and 4x CPU throttle, for the same
// labels every frame, numbers that change every frame, and labels at a new size every frame. Prints the time per call. An indicative measurement
// (a loaded machine moves it), not a pass/fail check. Run on the dev export of tests/WebGame:
//   node run-page.mjs --root <export folder> --path '/index.html?control=page' --script text-cost-test.mjs
let nextId = 1;

export default async function ({ send, evalJs, sleep }) {
  const call = async (name, args = {}) => {
    const reply = JSON.parse(await evalJs(`__control(${JSON.stringify(JSON.stringify({ jsonrpc: '2.0', id: nextId++, method: 'tools/call', params: { name, arguments: args } }))})`));
    return reply.error ? { error: JSON.stringify(reply.error) } : reply.result;
  };
  for (let i = 0; i < 600 && !(await evalJs('window.__control && window.__onegame && window.__onegame.frames > 3')); i++) await sleep(100);
  await evalJs(`__control(${JSON.stringify(JSON.stringify({ jsonrpc: '2.0', id: 0, method: 'initialize', params: { protocolVersion: '2025-06-18', capabilities: {}, clientInfo: { name: 'text-cost', version: '1' } } }))})`);
  await send('Performance.enable', {});
  const metric = async (name) => (await send('Performance.getMetrics', {})).result.metrics.find((m) => m.name === name).value;
  const perFrame = async (lines, churn, rate, seconds) => {
    await call('command_send', { entity: 'TextLoad', command: 'Set', args: [String(lines), String(churn)] });
    await send('Emulation.setCPUThrottlingRate', { rate });
    await sleep(1500);
    const t0 = await metric('TaskDuration'), f0 = await evalJs('window.__onegame.frames');
    await sleep(seconds * 1000);
    const t1 = await metric('TaskDuration'), f1 = await evalJs('window.__onegame.frames');
    return { ms: ((t1 - t0) * 1000) / Math.max(f1 - f0, 1), frames: f1 - f0 };
  };
  const out = {};
  for (const rate of [1, 4]) {
    const base = await perFrame(0, 0, rate, 5);
    const row = { baselineMsPerFrame: Math.round(base.ms * 100) / 100 };
    for (const [name, churn] of [['same', 0], ['numbers', 1], ['sizes', 2], ['newsizes', 4]]) {
      const r = await perFrame(100, churn, rate, 5);
      row[name] = { msPerFrame: Math.round(r.ms * 100) / 100, msPerCall: Math.round(((r.ms - base.ms) / 100) * 1000) / 1000, frames: r.frames };
    }
    out['throttle' + rate] = row;
  }
  await send('Emulation.setCPUThrottlingRate', { rate: 1 });
  return out;
}
