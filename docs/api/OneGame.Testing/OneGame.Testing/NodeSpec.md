[API reference](../../index.md) › [OneGame.Testing](../index.md) › OneGame.Testing

# NodeSpec record

**Namespace:** `OneGame.Testing`  
**Package:** [`RecursiveAI.OneGame.Testing`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`NodeSpec`](NodeSpec.md)&gt;

```csharp
public sealed record NodeSpec : IEquatable<NodeSpec>
```

One process of a cluster.

## Constructors

| Member | Description |
| --- | --- |
| [`NodeSpec()`](#m-ctor) |  |

### <a id="m-ctor"></a>NodeSpec()

```csharp
public NodeSpec();
```

> **Obsolete.** Constructors of types with required members are not supported in this version of your compiler.

## Properties

| Member | Description |
| --- | --- |
| [`Args`](#m-args) | Extra command-line arguments, before the ones the harness adds. |
| [`Connect`](#m-connect) | Clients: the server node to connect to. |
| [`Exe`](#m-exe) | The program to start: an executable, or a `.dll` that is run with `dotnet`. |
| [`Listen`](#m-listen) | Servers: what to listen on, `udp`, `ws` or `udp+ws`. |
| [`Manual`](#m-manual) | Servers: tick only on the `step` operation, so a test decides exactly when time passes. |
| [`Name`](#m-name) | The node's name; unique. |
| [`Net`](#m-net) | Clients: network conditions applied to this client's traffic, as `latency=100,loss=0.02`. |
| [`Release`](#m-release) | Clients: an exported release build, which has no live control. |
| [`Role`](#m-role) | Server or client. |
| [`Scope`](#m-scope) | Clients: the scope to subscribe to. |
| [`Scopes`](#m-scopes) | Servers: the scopes to host (scope 1 with seed 1 when empty). |
| [`TickRate`](#m-tickrate) | Servers: ticks per second; the server's default when 0. |
| [`Transport`](#m-transport) | Clients: the transport to reach it by, `udp` or `ws`. |

### <a id="m-args"></a>Args

```csharp
public IReadOnlyList<string> Args { get; init; }
```

Extra command-line arguments, before the ones the harness adds.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)`<string>`

### <a id="m-connect"></a>Connect

```csharp
public string? Connect { get; init; }
```

Clients: the server node to connect to.

**Type:** `string?`

### <a id="m-exe"></a>Exe

```csharp
public string Exe { get; init; }
```

The program to start: an executable, or a `.dll` that is run with `dotnet`. Relative paths resolve against the spec file.

**Type:** `string`

### <a id="m-listen"></a>Listen

```csharp
public string Listen { get; init; }
```

Servers: what to listen on, `udp`, `ws` or `udp+ws`. Ports are picked by the system.

**Type:** `string`

### <a id="m-manual"></a>Manual

```csharp
public bool Manual { get; init; }
```

Servers: tick only on the `step` operation, so a test decides exactly when time passes.

**Type:** `bool`

### <a id="m-name"></a>Name

```csharp
public string Name { get; init; }
```

The node's name; unique. Scenarios and operations address it by this.

**Type:** `string`

### <a id="m-net"></a>Net

```csharp
public string? Net { get; init; }
```

Clients: network conditions applied to this client's traffic, as `latency=100,loss=0.02`.

**Type:** `string?`

### <a id="m-release"></a>Release

```csharp
public bool Release { get; init; }
```

Clients: an exported release build, which has no live control. It runs on its own (give it `--until-tick N` in [`NodeSpec.Args`](NodeSpec.md#m-args)), prints its result when it stops, and the cluster reads that result to compare it with its server.

**Type:** `bool`

### <a id="m-role"></a>Role

```csharp
public NodeRole Role { get; init; }
```

Server or client.

**Type:** [`NodeRole`](NodeRole.md)

### <a id="m-scope"></a>Scope

```csharp
public uint Scope { get; init; }
```

Clients: the scope to subscribe to.

**Type:** `uint`

### <a id="m-scopes"></a>Scopes

```csharp
public IReadOnlyList<ScopeSpec> Scopes { get; init; }
```

Servers: the scopes to host (scope 1 with seed 1 when empty).

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`ScopeSpec`](ScopeSpec.md)&gt;

### <a id="m-tickrate"></a>TickRate

```csharp
public int TickRate { get; init; }
```

Servers: ticks per second; the server's default when 0.

**Type:** `int`

### <a id="m-transport"></a>Transport

```csharp
public string Transport { get; init; }
```

Clients: the transport to reach it by, `udp` or `ws`.

**Type:** `string`
