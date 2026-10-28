[API reference](../../index.md) › [OneGame.Testing](../index.md) › OneGame.Testing

# NodeState record

**Namespace:** `OneGame.Testing`  
**Package:** [`RecursiveAI.OneGame.Testing`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`NodeState`](NodeState.md)&gt;

```csharp
public sealed record NodeState : IEquatable<NodeState>
```

One node as recorded in `cluster.json`.

## Constructors

| Member | Description |
| --- | --- |
| [`NodeState(string, string, int, string, string, EndpointState[], string?, string?, bool)`](#m-ctor-string-string-int-string-string-endpointstate-string-string-bool) | One node as recorded in `cluster.json`. |

### <a id="m-ctor-string-string-int-string-string-endpointstate-string-string-bool"></a>NodeState(string, string, int, string, string, EndpointState\[\], string?, string?, bool)

```csharp
public NodeState(string Name, string Role, int Pid, string Socket, string Log, EndpointState[] Listen, string? Connect, string? Transport, bool Release = false);
```

One node as recorded in `cluster.json`.

**Parameters**

- `Name` (`string`): The node's name.
- `Role` (`string`): `server` or `client`.
- `Pid` (`int`): The process id.
- `Socket` (`string`): The live-control socket.
- `Log` (`string`): The file the process writes its output to.
- `Listen` ([`EndpointState`](EndpointState.md)\[\]): Servers: where it accepts clients.
- `Connect` (`string?`): Clients: the server node they are connected to.
- `Transport` (`string?`): Clients: the transport they use.
- `Release` (`bool`): Clients: an exported release build with no live control; its result is read from its output when it exits.

## Properties

| Member | Description |
| --- | --- |
| [`Connect`](#m-connect) | Clients: the server node they are connected to. |
| [`Listen`](#m-listen) | Servers: where it accepts clients. |
| [`Log`](#m-log) | The file the process writes its output to. |
| [`Name`](#m-name) | The node's name. |
| [`Pid`](#m-pid) | The process id. |
| [`Release`](#m-release) | Clients: an exported release build with no live control; its result is read from its output when it exits. |
| [`Role`](#m-role) | `server` or `client`. |
| [`Socket`](#m-socket) | The live-control socket. |
| [`Transport`](#m-transport) | Clients: the transport they use. |

### <a id="m-connect"></a>Connect

```csharp
public string? Connect { get; init; }
```

Clients: the server node they are connected to.

**Type:** `string?`

### <a id="m-listen"></a>Listen

```csharp
public EndpointState[] Listen { get; init; }
```

Servers: where it accepts clients.

**Type:** [`EndpointState`](EndpointState.md)\[\]

### <a id="m-log"></a>Log

```csharp
public string Log { get; init; }
```

The file the process writes its output to.

**Type:** `string`

### <a id="m-name"></a>Name

```csharp
public string Name { get; init; }
```

The node's name.

**Type:** `string`

### <a id="m-pid"></a>Pid

```csharp
public int Pid { get; init; }
```

The process id.

**Type:** `int`

### <a id="m-release"></a>Release

```csharp
public bool Release { get; init; }
```

Clients: an exported release build with no live control; its result is read from its output when it exits.

**Type:** `bool`

### <a id="m-role"></a>Role

```csharp
public string Role { get; init; }
```

`server` or `client`.

**Type:** `string`

### <a id="m-socket"></a>Socket

```csharp
public string Socket { get; init; }
```

The live-control socket.

**Type:** `string`

### <a id="m-transport"></a>Transport

```csharp
public string? Transport { get; init; }
```

Clients: the transport they use.

**Type:** `string?`
