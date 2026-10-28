[API reference](../../index.md) › [OneGame.Testing](../index.md) › OneGame.Testing

# TestServer class

**Namespace:** `OneGame.Testing`  
**Package:** [`RecursiveAI.OneGame.Testing`](../index.md)  
**Implements:** [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class TestServer : IDisposable
```

A server under test, with in-process clients on the loopback transport. One call to [`TestServer.Step()`](TestServer.md#m-step-int) runs a server tick and then delivers the frames to every client and records every scope's hash, so a test can compare hash sequences between runs, check that replicas never drift, and reload plugins in the middle of a run.

## Constructors

| Member | Description |
| --- | --- |
| [`TestServer(ServerConfig, ServerSetup?, IPluginLoader?)`](#m-ctor-serverconfig-serversetup-ipluginloader) | Creates and starts a server. |

### <a id="m-ctor-serverconfig-serversetup-ipluginloader"></a>TestServer(ServerConfig, ServerSetup?, IPluginLoader?)

```csharp
public TestServer(ServerConfig config, ServerSetup? setup = null, IPluginLoader? loader = null);
```

Creates and starts a server.

**Parameters**

- `config` ([`ServerConfig`](../../OneGame.Server/OneGame.Server/ServerConfig.md)): The configuration.
- `setup` ([`ServerSetup`](../../OneGame.Server/OneGame.Server/ServerSetup.md)?): The game's own types and world setup, or null.
- `loader` ([`IPluginLoader`](../../OneGame.Server/OneGame.Server/IPluginLoader.md)?): The plugin loader (`new PluginLoader()` from `OneGame.Server.Plugins` on CoreCLR), or null for none.

## Properties

| Member | Description |
| --- | --- |
| [`Clients`](#m-clients) | The connected clients. |
| [`DriftCount`](#m-driftcount) | Drift reports filed by any client's replicas. |
| [`Host`](#m-host) | The server under test. |

### <a id="m-clients"></a>Clients

```csharp
public IReadOnlyList<ServerTestClient> Clients { get; }
```

The connected clients.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`ServerTestClient`](ServerTestClient.md)&gt;

### <a id="m-driftcount"></a>DriftCount

```csharp
public int DriftCount { get; }
```

Drift reports filed by any client's replicas. Must stay empty.

**Type:** `int`

### <a id="m-host"></a>Host

```csharp
public ServerHost Host { get; }
```

The server under test.

**Type:** [`ServerHost`](../../OneGame.Server/OneGame.Server/ServerHost.md)

## Methods

| Member | Description |
| --- | --- |
| [`AddClient(string, ScopeId, Trust?)`](#m-addclient-string-scopeid-trust) | Connects a client that subscribes to a scope, and delivers its first snapshot. |
| [`CheckSync()`](#m-checksync) | Compares every client's replica to the authority right now. |
| [`Dispose()`](#m-dispose) | Stops the server. |
| [`HashHistory(ScopeId)`](#m-hashhistory-scopeid) | The (tick, hash) pairs recorded for a scope, one per [`TestServer.Step()`](TestServer.md#m-step-int). |
| [`Settle()`](#m-settle) | Delivers pending messages both ways without ticking. |
| [`Step(int)`](#m-step-int) | Runs ticks: the server steps, then every client applies the frames. |

### <a id="m-addclient-string-scopeid-trust"></a>AddClient(string, ScopeId, Trust?)

```csharp
public ServerTestClient AddClient(string name, ScopeId scope, Trust? trust = default);
```

Connects a client that subscribes to a scope, and delivers its first snapshot.

**Parameters**

- `name` (`string`): The client's name.
- `scope` ([`ScopeId`](../../OneGame.Sync/OneGame.Sync/ScopeId.md)): The scope to subscribe to.
- `trust` ([`Trust`](../../OneGame.Sync/OneGame.Sync/Trust.md)?): What the client may do; [`Trust.Player()`](../../OneGame.Sync/OneGame.Sync/Trust.md#m-player-scopeid) on the scope when null.

**Returns:** [`ServerTestClient`](ServerTestClient.md)

### <a id="m-checksync"></a>CheckSync()

```csharp
public IReadOnlyList<string> CheckSync();
```

Compares every client's replica to the authority right now. Returns a description of each difference; empty means in sync.

**Returns:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)`<string>`

### <a id="m-dispose"></a>Dispose()

```csharp
public void Dispose();
```

Stops the server.

### <a id="m-hashhistory-scopeid"></a>HashHistory(ScopeId)

```csharp
public IReadOnlyList<(long Tick, ulong Hash)> HashHistory(ScopeId scope);
```

The (tick, hash) pairs recorded for a scope, one per [`TestServer.Step()`](TestServer.md#m-step-int).

**Parameters**

- `scope` ([`ScopeId`](../../OneGame.Sync/OneGame.Sync/ScopeId.md)): The scope.

**Returns:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)`<(long Tick, ulong Hash)>`

### <a id="m-settle"></a>Settle()

```csharp
public void Settle();
```

Delivers pending messages both ways without ticking.

### <a id="m-step-int"></a>Step(int)

```csharp
public void Step(int ticks = 1);
```

Runs ticks: the server steps, then every client applies the frames. The hash of each scope after each tick is recorded.

**Parameters**

- `ticks` (`int`): How many.
