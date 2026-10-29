// Runs the published probe under node: node run.mjs <publish folder>/wwwroot
import path from 'node:path';
import { pathToFileURL } from 'node:url';
const { dotnet } = await import(pathToFileURL(path.resolve(process.argv[2], '_framework/dotnet.js')).href);
const { runMain } = await dotnet.withDiagnosticTracing(false).create();
await runMain();
