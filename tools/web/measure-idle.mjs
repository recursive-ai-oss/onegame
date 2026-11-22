// Measures main-thread busy time (Performance.getMetrics TaskDuration) of a silent page over a window, at 1x and 4x CPU throttle.
//   node run-page.mjs --root <export> --path "/index.html?args=--no-present" --script measure-idle.mjs
export default async function ({ send, evalJs, sleep }) {
  for (let i = 0; i < 600 && !(await evalJs('window.__onegame && window.__onegame.mainReturnedMs ? 1 : 0')); i++) await sleep(100);
  await sleep(3000);
  await send('Performance.enable', {});
  const metric = async (name) => (await send("Performance.getMetrics", {})).result.metrics.find((m) => m.name === name).value;
  const out = {};
  for (const rate of [1, 4]) {
    await send('Emulation.setCPUThrottlingRate', { rate });
    await sleep(1500);
    const t0 = await metric('TaskDuration'); const w0 = Date.now();
    await sleep(6000);
    const t1 = await metric('TaskDuration'); const w1 = Date.now();
    out['throttle' + rate] = { busyPct: Math.round(((t1 - t0) * 1000 / (w1 - w0)) * 1000) / 10, taskSeconds: Math.round((t1 - t0) * 1000) / 1000, wallMs: w1 - w0 };
  }
  out.audio = await evalJs('(() => { try { return JSON.stringify(window.__onegame.audio || null); } catch (e) { return String(e); } })()');
  return out;
}
