[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# ConnectOptions record

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ConnectOptions`](ConnectOptions.md)&gt;

```csharp
public sealed record ConnectOptions : IEquatable<ConnectOptions>
```

Where a game connects instead of running its own world: a server address and how.

## Constructors

| Member | Description |
| --- | --- |
| [`ConnectOptions()`](#m-ctor) |  |

### <a id="m-ctor"></a>ConnectOptions()

```csharp
public ConnectOptions();
```

> **Obsolete.** Constructors of types with required members are not supported in this version of your compiler.

## Properties

| Member | Description |
| --- | --- |
| [`Address`](#m-address) | `udp://host:port` (reliable UDP) or `ws://host:port` (WebSocket). |
| [`AlsoScopes`](#m-alsoscopes) | More scopes to follow over the same connection, for a server that serves several (its own, and others it relays). |
| [`Conditions`](#m-conditions) | Network conditions to apply to this client's traffic (latency, loss, ...), or null for the real network as it is. |
| [`Name`](#m-name) | The name this client has in reports. |
| [`Reconnect`](#m-reconnect) | When the connection closes for any reason but a refusal or a version mismatch (the server died, restarted or timed out), keep trying to connect again and resubscribe to every followed scope; the replicas are rebuilt from fresh snapshots. |
| [`ReconnectWindowMs`](#m-reconnectwindowms) | How long to keep trying after the connection was lost before the session counts as closed for good. |
| [`Scope`](#m-scope) | The scope to subscribe to. |

### <a id="m-address"></a>Address

```csharp
public string Address { get; init; }
```

`udp://host:port` (reliable UDP) or `ws://host:port` (WebSocket).

**Type:** `string`

### <a id="m-alsoscopes"></a>AlsoScopes

```csharp
public IReadOnlyList<uint> AlsoScopes { get; init; }
```

More scopes to follow over the same connection, for a server that serves several (its own, and others it relays). Their replicas are in [`RemoteSession.Replicas`](RemoteSession.md#m-replicas) after the first.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)`<uint>`

### <a id="m-conditions"></a>Conditions

```csharp
public NetworkConditions? Conditions { get; init; }
```

Network conditions to apply to this client's traffic (latency, loss, ...), or null for the real network as it is.

**Type:** [`NetworkConditions`](../../OneGame.Net/OneGame.Net/NetworkConditions.md)?

### <a id="m-name"></a>Name

```csharp
public string Name { get; init; }
```

The name this client has in reports.

**Type:** `string`

### <a id="m-reconnect"></a>Reconnect

```csharp
public bool Reconnect { get; init; }
```

When the connection closes for any reason but a refusal or a version mismatch (the server died, restarted or timed out), keep trying to connect again and resubscribe to every followed scope; the replicas are rebuilt from fresh snapshots. On by default.

**Type:** `bool`

### <a id="m-reconnectwindowms"></a>ReconnectWindowMs

```csharp
public int ReconnectWindowMs { get; init; }
```

How long to keep trying after the connection was lost before the session counts as closed for good. Attempts back off from 250 ms to 2 s.

**Type:** `int`

### <a id="m-scope"></a>Scope

```csharp
public uint Scope { get; init; }
```

The scope to subscribe to.

**Type:** `uint`
