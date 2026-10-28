[API reference](../../index.md) › [OneGame.Server](../index.md) › OneGame.Server

# ServerHost class

**Namespace:** `OneGame.Server`  
**Package:** [`RecursiveAI.OneGame.Server`](../index.md)

```csharp
public sealed class ServerHost
```

A headless game server: one node hosting one or more authority scopes on a fixed-step driver, with no window, GPU or audio. It starts from a [`ServerConfig`](ServerConfig.md), loads plugins through an [`IPluginLoader`](IPluginLoader.md) (when the host has one), keeps ticking when a plugin fails, and can load, hot-reload and unload plugins while it runs. Everything runs on the thread that calls [`ServerHost.StepOnce()`](ServerHost.md#m-steponce), [`ServerHost.Advance()`](ServerHost.md#m-advance-timespan) or [`ServerHost.Run()`](ServerHost.md#m-run-func-bool-action): plugin operations are plain calls between ticks.

## Constructors

| Member | Description |
| --- | --- |
| [`ServerHost(ServerConfig, ServerSetup?, IPluginLoader?, ServerLog?)`](#m-ctor-serverconfig-serversetup-ipluginloader-serverlog) | Creates a server. |

### <a id="m-ctor-serverconfig-serversetup-ipluginloader-serverlog"></a>ServerHost(ServerConfig, ServerSetup?, IPluginLoader?, ServerLog?)

```csharp
public ServerHost(ServerConfig config, ServerSetup? setup = null, IPluginLoader? loader = null, ServerLog? log = null);
```

Creates a server. Nothing runs until [`ServerHost.Start()`](ServerHost.md#m-start).

**Parameters**

- `config` ([`ServerConfig`](ServerConfig.md)): The configuration.
- `setup` ([`ServerSetup`](ServerSetup.md)?): The game's own entity types and world setup, or null for a server that only runs plugins.
- `loader` ([`IPluginLoader`](IPluginLoader.md)?): Loads plugins. Null for a host that cannot (a NativeAOT build): configured plugins are then reported as failures.
- `log` ([`ServerLog`](ServerLog.md)?): The log; defaults to one that keeps entries and writes nowhere.

## Properties

| Member | Description |
| --- | --- |
| [`Authenticate`](#m-authenticate) | Decides what a newly connected network client may do, or refuses it by returning null. |
| [`Config`](#m-config) | The configuration. |
| [`DeliverLocalMessages`](#m-deliverlocalmessages) | Delivers pending messages to clients that live in this process (tests). |
| [`Driver`](#m-driver) | The fixed-step driver that turns elapsed time into ticks. |
| [`Endpoints`](#m-endpoints) | The addresses the server listens on, with the ports the system picked for configured port 0. |
| [`LoadFailures`](#m-loadfailures) | Plugins that were configured but failed to load. |
| [`Log`](#m-log) | The log. |
| [`Node`](#m-node) | The node that is authority for every hosted scope. |
| [`Scopes`](#m-scopes) | The hosted scopes in id order of declaration. |
| [`State`](#m-state) | Where the server is in its life. |
| [`StopRequested`](#m-stoprequested) | True once a stop was requested (the control surface's `shutdown`). |
| [`Tick`](#m-tick) | Ticks run so far. |

### <a id="m-authenticate"></a>Authenticate

```csharp
public Func<ClientRequest, Trust?>? Authenticate { get; set; }
```

Decides what a newly connected network client may do, or refuses it by returning null. The default gives every client the configured `trust` (read and command every hosted scope unless the configuration says otherwise). The transport does not authenticate yet, so the request carries only the remote address. Set it before [`ServerHost.Start()`](ServerHost.md#m-start).

**Type:** [`Func`](https://learn.microsoft.com/dotnet/api/system.func-2)&lt;[`ClientRequest`](ClientRequest.md), [`Trust`](../../OneGame.Sync/OneGame.Sync/Trust.md)?&gt;?

### <a id="m-config"></a>Config

```csharp
public ServerConfig Config { get; }
```

The configuration.

**Type:** [`ServerConfig`](ServerConfig.md)

### <a id="m-deliverlocalmessages"></a>DeliverLocalMessages

```csharp
public Action? DeliverLocalMessages { get; set; }
```

Delivers pending messages to clients that live in this process (tests). A plugin's load context cannot be collected while a local replica still holds one of its entities, so reload and unload call this before they wait for the collector. Remote clients need nothing.

**Type:** [`Action`](https://learn.microsoft.com/dotnet/api/system.action)?

### <a id="m-driver"></a>Driver

```csharp
public FixedStepDriver Driver { get; }
```

The fixed-step driver that turns elapsed time into ticks.

**Type:** [`FixedStepDriver`](../../OneGame.Sync/OneGame.Sync/FixedStepDriver.md)

### <a id="m-endpoints"></a>Endpoints

```csharp
public IReadOnlyList<ListenEndpoint> Endpoints { get; }
```

The addresses the server listens on, with the ports the system picked for configured port 0. Filled by [`ServerHost.Start()`](ServerHost.md#m-start).

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`ListenEndpoint`](ListenEndpoint.md)&gt;

### <a id="m-loadfailures"></a>LoadFailures

```csharp
public IReadOnlyList<PluginLoadFailure> LoadFailures { get; }
```

Plugins that were configured but failed to load.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`PluginLoadFailure`](PluginLoadFailure.md)&gt;

### <a id="m-log"></a>Log

```csharp
public ServerLog Log { get; }
```

The log.

**Type:** [`ServerLog`](ServerLog.md)

### <a id="m-node"></a>Node

```csharp
public Node Node { get; }
```

The node that is authority for every hosted scope. Clients connect to it with [`Node.ConnectTo()`](../../OneGame.Sync/OneGame.Sync/Node.md#m-connectto-node-trust-string).

**Type:** [`Node`](../../OneGame.Sync/OneGame.Sync/Node.md)

### <a id="m-scopes"></a>Scopes

```csharp
public IReadOnlyList<ServerScope> Scopes { get; }
```

The hosted scopes in id order of declaration.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`ServerScope`](ServerScope.md)&gt;

### <a id="m-state"></a>State

```csharp
public ServerState State { get; }
```

Where the server is in its life.

**Type:** [`ServerState`](ServerState.md)

### <a id="m-stoprequested"></a>StopRequested

```csharp
public bool StopRequested { get; }
```

True once a stop was requested (the control surface's `shutdown`).

**Type:** `bool`

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; }
```

Ticks run so far.

**Type:** `long`

## Events

| Member | Description |
| --- | --- |
| [`ClientLeft`](#m-clientleft) | Raised when a network client's connection closed (it left, timed out or was dropped), after the messages it sent before that were handled. |

### <a id="m-clientleft"></a>ClientLeft

```csharp
public event Action<Connection>? ClientLeft;
```

Raised when a network client's connection closed (it left, timed out or was dropped), after the messages it sent before that were handled. [`Connection.Trust`](../../OneGame.Sync/OneGame.Sync/Connection.md#m-trust) still says who it was, so a game can remove that player's entities.

**Type:** [`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)&lt;[`Connection`](../../OneGame.Sync/OneGame.Sync/Connection.md)&gt;?

## Methods

| Member | Description |
| --- | --- |
| [`Advance(TimeSpan)`](#m-advance-timespan) | Feeds elapsed time to the fixed-step driver and runs the ticks that are due. |
| [`Clients()`](#m-clients) | The network clients that are connected now. |
| [`CreateReplicaWorld(ScopeId)`](#m-createreplicaworld-scopeid) | Creates a replica world for a scope with the same entity types, tables and systems as the authority, for in-process clients and tests. |
| [`FindScope(ScopeId)`](#m-findscope-scopeid) | The scope with this id, or null. |
| [`HashesAt(long)`](#m-hashesat-long) | The recorded hash of every scope at `tick`; the hash is null for a tick that is not recorded (see `hashHistory` in the configuration). |
| [`LoadPlugin(PluginConfig)`](#m-loadplugin-pluginconfig) | Loads a plugin. |
| [`Plugins()`](#m-plugins) | Snapshots of every loaded plugin, in load order. |
| [`PumpNetwork()`](#m-pumpnetwork) | Moves messages between the node and its network clients without ticking: accepts connections, reads what arrived (it is handled at the next tick) and sends what the last tick published. |
| [`ReloadPlugin(string, string?)`](#m-reloadplugin-string-string) | Replaces a running plugin with a new build of it. |
| [`RequestStop()`](#m-requeststop) | Asks [`ServerHost.Run()`](ServerHost.md#m-run-func-bool-action) to finish its current tick and stop. |
| [`Run(Func<bool>?, Action?)`](#m-run-func-bool-action) | Runs in real time until [`ServerHost.RequestStop()`](ServerHost.md#m-requeststop) or `keepRunning` returns false. |
| [`ScopeInfos()`](#m-scopeinfos) | Snapshots of every scope. |
| [`SendCommand(ScopeId, EntityId, string, string[])`](#m-sendcommand-scopeid-entityid-string-string) | Sends a command to an entity from the host, as trusted input: it is queued for the next tick. |
| [`Start()`](#m-start) | Builds the scopes and loads the configured plugins. |
| [`Step(int)`](#m-step-int) | Runs `ticks` ticks. |
| [`StepOnce()`](#m-steponce) | Runs one tick: delivers arrived messages, steps and publishes every scope. |
| [`Stop()`](#m-stop) | Stops ticking, gives every plugin its [`ServerPlugin.Unload()`](ServerPlugin.md#m-unload) call (errors are logged) and releases it. |
| [`UnloadPlugin(string)`](#m-unloadplugin-string) | Removes a plugin: its entities leave the scopes, its systems stop, [`ServerPlugin.Unload()`](ServerPlugin.md#m-unload) runs, and its load context is released and collected. |

### <a id="m-advance-timespan"></a>Advance(TimeSpan)

```csharp
public int Advance(TimeSpan elapsed);
```

Feeds elapsed time to the fixed-step driver and runs the ticks that are due. Returns how many ran.

**Parameters**

- `elapsed` ([`TimeSpan`](https://learn.microsoft.com/dotnet/api/system.timespan)): Time since the previous call.

**Returns:** `int`

### <a id="m-clients"></a>Clients()

```csharp
public ClientInfo[] Clients();
```

The network clients that are connected now.

**Returns:** [`ClientInfo`](ClientInfo.md)\[\]

### <a id="m-createreplicaworld-scopeid"></a>CreateReplicaWorld(ScopeId)

```csharp
public World CreateReplicaWorld(ScopeId scope);
```

Creates a replica world for a scope with the same entity types, tables and systems as the authority, for in-process clients and tests. Plugin types are created through the host, so the replica follows reloads.

**Parameters**

- `scope` ([`ScopeId`](../../OneGame.Sync/OneGame.Sync/ScopeId.md)): The scope id.

**Returns:** [`World`](../../OneGame.Core/OneGame.Core/World.md)

**Exceptions**

- [`PluginException`](PluginException.md): No such scope.

### <a id="m-findscope-scopeid"></a>FindScope(ScopeId)

```csharp
public ServerScope? FindScope(ScopeId id);
```

The scope with this id, or null.

**Parameters**

- `id` ([`ScopeId`](../../OneGame.Sync/OneGame.Sync/ScopeId.md)): The scope id.

**Returns:** [`ServerScope`](ServerScope.md)?

### <a id="m-hashesat-long"></a>HashesAt(long)

```csharp
public ScopeHashAt[] HashesAt(long tick);
```

The recorded hash of every scope at `tick`; the hash is null for a tick that is not recorded (see `hashHistory` in the configuration).

**Parameters**

- `tick` (`long`): The scope tick.

**Returns:** [`ScopeHashAt`](ScopeHashAt.md)\[\]

### <a id="m-loadplugin-pluginconfig"></a>LoadPlugin(PluginConfig)

```csharp
public PluginInfo LoadPlugin(PluginConfig spec);
```

Loads a plugin. Errors while loading (missing files, API mismatch, an exception in [`ServerPlugin.Load()`](ServerPlugin.md#m-load-plugincontext), clashing type ids) refuse the plugin and change nothing. An exception while seeding quarantines the plugin: it stays listed as faulted.

**Parameters**

- `spec` ([`PluginConfig`](PluginConfig.md)): The name, path, scopes and settings.

**Returns:** [`PluginInfo`](PluginInfo.md) — The loaded plugin.

**Exceptions**

- [`PluginException`](PluginException.md): The plugin was refused.

### <a id="m-plugins"></a>Plugins()

```csharp
public IReadOnlyList<PluginInfo> Plugins();
```

Snapshots of every loaded plugin, in load order.

**Returns:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`PluginInfo`](PluginInfo.md)&gt;

### <a id="m-pumpnetwork"></a>PumpNetwork()

```csharp
public void PumpNetwork();
```

Moves messages between the node and its network clients without ticking: accepts connections, reads what arrived (it is handled at the next tick) and sends what the last tick published. [`ServerHost.Run()`](ServerHost.md#m-run-func-bool-action) calls it every loop; a caller that drives [`ServerHost.StepOnce()`](ServerHost.md#m-steponce) itself should call it between ticks so the transports keep acknowledging and the clients stay connected.

### <a id="m-reloadplugin-string-string"></a>ReloadPlugin(string, string?)

```csharp
public ReloadResult ReloadPlugin(string name, string? path = null);
```

Replaces a running plugin with a new build of it. The new build loads first (a failure leaves the old one running). Then each bound scope is saved, the new build's types and systems are swapped in, and the save is restored through the entity types' migrations, so entity ids and state stay and replicas get a fresh snapshot. If the restore fails the old build is put back.

**Parameters**

- `name` (`string`): The plugin's name.
- `path` (`string?`): A new location, or null to load again from where it was.

**Returns:** [`ReloadResult`](ReloadResult.md)

**Exceptions**

- [`PluginException`](PluginException.md): The reload was refused; the old build is still running.

### <a id="m-requeststop"></a>RequestStop()

```csharp
public void RequestStop();
```

Asks [`ServerHost.Run()`](ServerHost.md#m-run-func-bool-action) to finish its current tick and stop.

### <a id="m-run-func-bool-action"></a>Run(Func&lt;bool&gt;?, Action?)

```csharp
public void Run(Func<bool>? keepRunning = null, Action? between = null);
```

Runs in real time until [`ServerHost.RequestStop()`](ServerHost.md#m-requeststop) or `keepRunning` returns false. `between` runs once per loop and is where live control requests are served; it should wait briefly (a millisecond or two) when it has nothing to do.

**Parameters**

- `keepRunning` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-1)`<bool>?`): Checked every loop; null runs until a stop is requested.
- `between` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action)?): Called between ticks, or null to sleep a millisecond.

### <a id="m-scopeinfos"></a>ScopeInfos()

```csharp
public IReadOnlyList<ScopeInfo> ScopeInfos();
```

Snapshots of every scope.

**Returns:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`ScopeInfo`](ScopeInfo.md)&gt;

### <a id="m-sendcommand-scopeid-entityid-string-string"></a>SendCommand(ScopeId, EntityId, string, string\[\])

```csharp
public CommandStatus SendCommand(ScopeId scope, EntityId entity, string command, params string[] args);
```

Sends a command to an entity from the host, as trusted input: it is queued for the next tick. Arguments are text, as in scenarios.

**Parameters**

- `scope` ([`ScopeId`](../../OneGame.Sync/OneGame.Sync/ScopeId.md)): The scope.
- `entity` ([`EntityId`](../../OneGame.Core/OneGame.Core/EntityId.md)): The entity id.
- `command` (`string`): The command name.
- `args` (`string[]`): The arguments as text.

**Returns:** [`CommandStatus`](../../OneGame.Core/OneGame.Core/CommandStatus.md)

**Exceptions**

- [`PluginException`](PluginException.md): No such scope or entity.

### <a id="m-start"></a>Start()

```csharp
public void Start();
```

Builds the scopes and loads the configured plugins. A plugin that fails to load is logged and listed in [`ServerHost.LoadFailures`](ServerHost.md#m-loadfailures); the server starts without it.

**Exceptions**

- [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception): The server was already started.

### <a id="m-step-int"></a>Step(int)

```csharp
public void Step(int ticks);
```

Runs `ticks` ticks.

**Parameters**

- `ticks` (`int`): How many.

### <a id="m-steponce"></a>StepOnce()

```csharp
public void StepOnce();
```

Runs one tick: delivers arrived messages, steps and publishes every scope. A plugin that throws is contained.

**Exceptions**

- [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception): The server is not running.

### <a id="m-stop"></a>Stop()

```csharp
public void Stop();
```

Stops ticking, gives every plugin its [`ServerPlugin.Unload()`](ServerPlugin.md#m-unload) call (errors are logged) and releases it. The scopes' state stays readable.

### <a id="m-unloadplugin-string"></a>UnloadPlugin(string)

```csharp
public UnloadResult UnloadPlugin(string name);
```

Removes a plugin: its entities leave the scopes, its systems stop, [`ServerPlugin.Unload()`](ServerPlugin.md#m-unload) runs, and its load context is released and collected.

**Parameters**

- `name` (`string`): The plugin's name.

**Returns:** [`UnloadResult`](UnloadResult.md)

**Exceptions**

- [`PluginException`](PluginException.md): No such plugin.
