// The ui_scale control operation in a browser: an override changes the scale the game lays out at, and `reset` follows the window again. Scenario
// isolation (the runner clearing the override before each scenario) is host code, `ScenarioRunner.RunScenario`, which is not browser-specific;
// this checks the page's half. Run on the export of tests/WebGame, with run-page.mjs or run-page-safari.mjs:
//   --path '/index.html?control=page' --script ui-scale-test.mjs
let nextId = 1;

export default async function ({ evalJs, sleep }) {
  for (let i = 0; i < 300 && !(await evalJs('window.__control && window.__onegame && window.__onegame.frames > 5')); i++) await sleep(100);
  await evalJs(`__control(${JSON.stringify(JSON.stringify({ jsonrpc: '2.0', id: 0, method: 'initialize', params: { protocolVersion: '2025-06-18', capabilities: {}, clientInfo: { name: 'ui-scale-test', version: '1' } } }))})`);
  const checks = [];
  const check = (name, ok, detail) => checks.push({ name, ok: !!ok, ...(ok ? {} : { detail }) });
  const call = async (name, args = {}) => {
    const reply = JSON.parse(await evalJs(`__control(${JSON.stringify(JSON.stringify({ jsonrpc: '2.0', id: nextId++, method: 'tools/call', params: { name, arguments: args } }))})`));
    const text = reply.result?.content?.find(c => c.type === 'text')?.text;
    return text ? JSON.parse(text) : reply;
  };
  const info = () => call('info');
  const base = await info();
  check('no override at the start', base.scaleOverride == null, base);
  await call('ui_scale', { scale: 2 });
  await sleep(300);
  let now = await info();
  check('an override of 2 is in effect', now.scale === 2 && now.scaleOverride === 2, now);
  await call('ui_scale', { reset: true });
  await sleep(300);
  now = await info();
  check('reset follows the window again', now.scaleOverride == null && now.scale === base.scale, { base, now });
  return { ok: checks.every(c => c.ok), passed: checks.filter(c => c.ok).length, failed: checks.filter(c => !c.ok).length, checks };
}
