# Server host and plugins

Source: `src/OneGame.Server` (host, configuration, plugin API; NativeAOT-clean), `src/OneGame.Server.Plugins` (the CoreCLR plugin loader), `src/OneGame.Server.Live` (control surface), `src/OneGame.Server.Runner` (`onegame-server`), `src/OneGame.Testing/TestServer.cs`. Builds on `sync.md` (`Node`, `AuthorityScope`, `FixedStepDriver`, `SaveFile`) and `control.md` (`IControlEndpoint`, MCP listener). Nothing in Sync or the generators changed.

## Host

`new ServerHost(ServerConfig, ServerSetup? setup, IPluginLoader? loader, ServerLog? log)`, then `Start()`.

- One `Node` is authority for every configured scope. Each scope is an `AuthorityScope` over a `World` built by `ServerSetup` (`RegisterEntities` with the game's generated `EntityTypes.Register`, `BuildWorld` for tables and systems) and then given one adapter system that runs plugin systems. Clients connect with `host.Node` as the server end (`Node.ConnectTo`), exactly as in a cluster.
- `StepOnce()` = deliver messages (`Node.Pump`) → step and publish each scope. `Advance(elapsed)` feeds the `FixedStepDriver`; `Run(keepRunning, between)` paces by the clock and calls `between` for live control. Single-threaded; the caller owns the thread. `Stop()` runs every plugin's `Unload` hook and releases it. States: Created → Running → Draining → Stopped.
- No loader (`null`) = a host that can be published NativeAOT: configured plugins show up in `LoadFailures` with code `unsupported`, and the server runs without them.
- Config (`ServerConfig.Load/Parse`, YAML via oneyaml): `name`, `tickRate`, `maxCatchUp`, `live`, `scopes: [{id, seed}]`, `plugins: [{name, path, scopes, settings}]`. Unknown keys, duplicates, and plugin scopes that are not declared are errors with file and line. Relative plugin paths resolve against the file.
- Log: `ServerLog` writes to a `TextWriter` (stderr in the runner) and keeps the last 512 lines as text.

## Clients on the network

The host listens when the configuration has `listen:` entries (reliable UDP and/or WebSocket), accepts each connection with `Node.Accept` under the trust `ServerHost.Authenticate` returns (the configured `trust` by default), and unsubscribes clients that leave. Client links are encrypted and authenticated: the `security:` section of the configuration (`key`, `game`, `insecure`) or `ServerHost.Security` says how, and a server that listens without a key fails to start with `SecurityNotConfiguredException`. `ClientRequest.Key` is the public key the client proved; map it to a player with `PlayerRegistry` so a reconnect is the same player. Flags `--key`, `--game-id` and `--insecure` (development runs only) override the file; see `transport-security.md` section 9. `PumpNetwork()` moves messages between ticks; `Run` calls it every loop. `ServerApp.Run(setup, args)` is the whole `Main` of a dedicated server; `ServerSetup.SeedWorld` creates a scope's starting entities. Control ops `clients` and `hash_at` report connections and the per-tick view hashes clients must match. See `cluster.md`.

## Plugin API (version 1)

A plugin is a class library referencing `OneGame.Server` and the generator, with `[assembly: ServerPlugin(typeof(MyPlugin))]` (the compiler bakes the API version in as a default argument). `MyPlugin : ServerPlugin`:

- `Load(PluginContext)` runs on every load and reload and only registers: `AddEntity<T>()` (a generated entity), `AddSystem(ISimSystem)`, `Setting(key)`, `Log(text)`. Entity commands are the plugin's command handlers. It must not touch worlds; throwing here refuses the plugin and changes nothing.
- `Seed(PluginScope)` runs once per bound scope on the **first** load only, to create initial entities.
- `Unload()` runs before the plugin is replaced or removed; throwing is logged and does not stop the unload.
- Not supported: hot tables (`IHotTable`) from plugins (the world cannot forget one), plugin-defined scopes.
- The API version is read from assembly metadata before any plugin code runs. A version outside `PluginApi.MinimumVersion..Version` is refused with `api_mismatch`.

## Operations on a running host

`LoadPlugin(PluginConfig)`, `ReloadPlugin(name, path?)`, `UnloadPlugin(name)`, `Plugins()`, `ScopeInfos()`, `CreateReplicaWorld(scope)`, `SendCommand(...)`. Failures are `PluginException(code, message)`: `api_mismatch`, `no_entry`, `not_found`, `duplicate`, `load_failed`, `conflict`, `reload_failed`, `bad_setting`, `unsupported`.

- **Types are registered through the host.** Scope registries get one factory per type id that asks the host's table, so no registry, world or system list holds a plugin type directly. Entity types a plugin registers are available to every scope's snapshots and to replica worlds from `CreateReplicaWorld`.
- **Reload**: load the new build and run its `Load` (failure keeps the old build running) → refuse if it dropped a type with live entities, or a recording is running → `AuthorityScope.Save()` on each bound scope → swap factories and systems → `AuthorityScope.Load(save)` (runs the generated migrations; subscribers get a fresh snapshot) → on any failure swap back and restore the save → run the old `Unload`, release, wait for the collector. Entity ids, tick and RNG streams are kept; an unchanged build gives the same hash sequence as a run without the reload. Commands from connections wait in the node's inbound queue until the next tick, so none is lost; commands queued through `SendCommand` since the last tick would be, so reload and unload refuse with `conflict` until the server has stepped.
- **Unload**: its entities are removed (subscribers get a snapshot at once), systems stop, `Unload` runs, the context is released. `UnloadResult.Collected`/`ReloadResult.Collected` say whether the load context was garbage collected (up to 20 forced collections). In-process subscribers keep a plugin's entities alive until they apply the snapshot: `ServerHost.DeliverLocalMessages` (set by `TestServer`) is called first.
- **Faults.** An exception in a plugin system, entity logic or command handler during a tick is attributed to the plugin by its stack (the loader checks which load context owns a frame), the plugin is marked `Faulted` (`PluginInfo.Fault`: phase, type, message, trace head), its systems stop and its entities are removed, and the server keeps ticking. The tick in which it happened is partly applied: do not rely on a recording across it. An exception from non-plugin code propagates. A `Seed` fault quarantines the same way.
- **Loading rules.** The dll is read into memory (the file can be rebuilt while it runs). `OneGame.*`, `System.*`, `Microsoft.*` resolve to the host's copies; everything else loads from the plugin's folder. Keep nothing in statics, threads, timers or events that outlive `Unload`.

## Control

`ServerControl : IControlEndpoint` (served by `LiveServer` over MCP, JSON lines on a Unix socket): `server_info`, `hash`, `step`, `plugin_list`, `plugin_load`, `plugin_reload`, `plugin_unload`, `command_send`, `state_query`, `log_tail`, `shutdown`. Errors are `ControlException` with the plugin codes above.

- `onegame-server --config f.yaml [--live [socket]] [--ticks N] [--manual]` prints `{"live":...}` when listening and `{"stopped":true,...}` at the end; logs go to stderr.
- `onegame server run ...` starts it; `onegame server info|hash|step N|plugins|load <name> <path>|reload <name>|unload <name>|send <entity> <command> [args]|state|log|stop [--socket p]` call the live socket and print the JSON result.
- `TestServer(config, setup, loader)` (OneGame.Testing): `AddClient` (loopback replica), `Step` (server tick then client delivery, recording each scope's hash), `HashHistory`, `CheckSync`, `DriftCount`.

## Consumers

Dedicated-server export uses `ServerHost` directly: a plugin-less game server references `OneGame.Server` (and optionally `OneGame.Server.Live`) and publishes NativeAOT; a plugin host references `OneGame.Server.Plugins` and runs on CoreCLR (`onegame-server` is the ready-made one).

The tool package (`RecursiveAI.OneGame.Cli`) carries `onegame-server` and its libraries in `tools/net10.0/any/server-host/` (published framework-dependent when the tool is packed). `onegame server run` and `onegame export --server --plugins` use that copy when it is beside the tool, and the runner project of a checkout otherwise. `onegame new <name> --server` writes `plugin/` (OneGameKind `Plugin`: a library on `build/Server.targets`) beside `server/`.
