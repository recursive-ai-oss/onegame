// Starts a onegame game in a page. Query options: ?args=--size%201280x720 passes command-line arguments, ?control=ws://host:port/ lets a
// development build take commands from a tool (see the web section of the export guide).
import { dotnet } from './_framework/dotnet.js';
import * as gpu from './onegame-gpu.js';
import * as platform from './onegame-platform.js';
import * as audio from './onegame-audio.js';

const status = document.getElementById('onegame-status');
window.__onegame = { error: null, startedMs: performance.now() };
window.__gpu = gpu;           // for tools: GPU validation messages (takeMessages)
window.__platform = platform; // for tools: the gamepad test hook (injectGamepads)
window.__audio = audio;       // for tools: the audio output's state and counters
try {
  const query = new URLSearchParams(location.search);
  const args = (query.get('args') || '').split(' ').filter(Boolean);
  if (query.get('control')) args.push('--control', query.get('control'));
  await platform.storageOpen('onegame-files');  // saved files are loaded from IndexedDB before the program starts, so reads never wait
  const runtime = await dotnet.withApplicationArguments(...args).create();
  runtime.setModuleImports('onegame-gpu', gpu);
  runtime.setModuleImports('onegame-platform', platform);
  runtime.setModuleImports('onegame-audio', audio);
  const exports = await runtime.getAssemblyExports('OneGame.Platform');
  window.__control = (message) => exports.OneGame.Platform.BrowserPage.Control(message);  // development builds: one MCP message in, the answer out
  await runtime.runMain();
  window.__onegame.mainReturnedMs = performance.now();
  status.remove();
} catch (e) {
  window.__onegame.error = String((e && e.stack) || e);
  status.textContent = 'The game could not start: ' + e;
}
