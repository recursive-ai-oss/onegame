[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# RemoteSession class

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)

```csharp
public sealed class RemoteSession
```

A game's connection to a server: a node holding one replica scope, reached through a transport connection ([`Node.ConnectVia()`](../../OneGame.Sync/OneGame.Sync/Node.md#m-connectvia-imessagechannel-string)). [`RemoteSession.Pump()`](RemoteSession.md#m-pump) does all the work, on the caller's thread.

## Constructors

| Member | Description |
| --- | --- |
| [`RemoteSession(ConnectOptions, World, IReadOnlyList<World>?)`](#m-ctor-connectoptions-world-ireadonlylist-world) | Connects an empty replica world to a server and subscribes to the scope. |

### <a id="m-ctor-connectoptions-world-ireadonlylist-world"></a>RemoteSession(ConnectOptions, World, IReadOnlyList&lt;World&gt;?)

```csharp
public RemoteSession(ConnectOptions options, World replicaWorld, IReadOnlyList<World>? extraWorlds = null);
```

Connects an empty replica world to a server and subscribes to the scope. Pump it to make progress.

**Parameters**

- `options` ([`ConnectOptions`](ConnectOptions.md)): Where to connect.
- `replicaWorld` ([`World`](../../OneGame.Core/OneGame.Core/World.md)): An empty replica world with the server's entity types, tables and systems.
- `extraWorlds` ([`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`World`](../../OneGame.Core/OneGame.Core/World.md)&gt;?): Empty replica worlds, one for each of [`ConnectOptions.AlsoScopes`](ConnectOptions.md#m-alsoscopes), in order.

## Properties

| Member | Description |
| --- | --- |
| [`Connection`](#m-connection) | The transport connection. |
| [`DriftReports`](#m-driftreports) | Drift reports the replica filed. |
| [`IsClosed`](#m-isclosed) | True once the transport connection has closed and the session will not try again (the server refused it, the window ran out, or reconnecting is off). |
| [`IsReconnecting`](#m-isreconnecting) | True while the connection is lost and the session is trying to get it back ([`ConnectOptions.Reconnect`](ConnectOptions.md#m-reconnect)). |
| [`Link`](#m-link) | The link to the server, for subscribing to more scopes later. |
| [`Node`](#m-node) | The client's node. |
| [`Options`](#m-options) | The options it was created with. |
| [`Reconnects`](#m-reconnects) | How many times the session connected again after losing the server. |
| [`Replica`](#m-replica) | The replica of the server's scope; its world is the game's world. |
| [`Replicas`](#m-replicas) | Every replica this session follows, the main one first. |
| [`Transport`](#m-transport) | `udp` or `ws`. |

### <a id="m-connection"></a>Connection

```csharp
public INetConnection Connection { get; }
```

The transport connection.

**Type:** [`INetConnection`](../../OneGame.Net/OneGame.Net/INetConnection.md)

### <a id="m-driftreports"></a>DriftReports

```csharp
public IReadOnlyList<DriftReport> DriftReports { get; }
```

Drift reports the replica filed.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`DriftReport`](../../OneGame.Sync/OneGame.Sync/DriftReport.md)&gt;

### <a id="m-isclosed"></a>IsClosed

```csharp
public bool IsClosed { get; }
```

True once the transport connection has closed and the session will not try again (the server refused it, the window ran out, or reconnecting is off).

**Type:** `bool`

### <a id="m-isreconnecting"></a>IsReconnecting

```csharp
public bool IsReconnecting { get; }
```

True while the connection is lost and the session is trying to get it back ([`ConnectOptions.Reconnect`](ConnectOptions.md#m-reconnect)).

**Type:** `bool`

### <a id="m-link"></a>Link

```csharp
public Link Link { get; }
```

The link to the server, for subscribing to more scopes later.

**Type:** [`Link`](../../OneGame.Sync/OneGame.Sync/Link.md)

### <a id="m-node"></a>Node

```csharp
public Node Node { get; }
```

The client's node.

**Type:** [`Node`](../../OneGame.Sync/OneGame.Sync/Node.md)

### <a id="m-options"></a>Options

```csharp
public ConnectOptions Options { get; }
```

The options it was created with.

**Type:** [`ConnectOptions`](ConnectOptions.md)

### <a id="m-reconnects"></a>Reconnects

```csharp
public int Reconnects { get; }
```

How many times the session connected again after losing the server.

**Type:** `int`

### <a id="m-replica"></a>Replica

```csharp
public ReplicaScope Replica { get; }
```

The replica of the server's scope; its world is the game's world.

**Type:** [`ReplicaScope`](../../OneGame.Sync/OneGame.Sync/ReplicaScope.md)

### <a id="m-replicas"></a>Replicas

```csharp
public IReadOnlyList<ReplicaScope> Replicas { get; }
```

Every replica this session follows, the main one first.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`ReplicaScope`](../../OneGame.Sync/OneGame.Sync/ReplicaScope.md)&gt;

### <a id="m-transport"></a>Transport

```csharp
public string Transport { get; }
```

`udp` or `ws`.

**Type:** `string`

## Methods

| Member | Description |
| --- | --- |
| [`ParseConditions(string)`](#m-parseconditions-string) | Reads `latency=100,jitter=10,loss=0.02,duplicate=0.01,reorder=0.05,bandwidth=125000,seed=7`. |
| [`Pump()`](#m-pump) | Moves messages both ways and applies what arrived. |
| [`Status()`](#m-status) | Describes the connection and the replica now. |

### <a id="m-parseconditions-string"></a>ParseConditions(string)

```csharp
public static NetworkConditions ParseConditions(string text);
```

Reads `latency=100,jitter=10,loss=0.02,duplicate=0.01,reorder=0.05,bandwidth=125000,seed=7`.

**Parameters**

- `text` (`string`)

**Returns:** [`NetworkConditions`](../../OneGame.Net/OneGame.Net/NetworkConditions.md)

**Exceptions**

- [`FormatException`](https://learn.microsoft.com/dotnet/api/system.formatexception): The text is not a list of known settings.

### <a id="m-pump"></a>Pump()

```csharp
public void Pump();
```

Moves messages both ways and applies what arrived. Cheap when nothing did.

### <a id="m-status"></a>Status()

```csharp
public NetStatus Status();
```

Describes the connection and the replica now.

**Returns:** [`NetStatus`](NetStatus.md)
