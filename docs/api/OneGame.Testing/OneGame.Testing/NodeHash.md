[API reference](../../index.md) › [OneGame.Testing](../index.md) › OneGame.Testing

# NodeHash record

**Namespace:** `OneGame.Testing`  
**Package:** [`RecursiveAI.OneGame.Testing`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`NodeHash`](NodeHash.md)&gt;

```csharp
public sealed record NodeHash : IEquatable<NodeHash>
```

One scope's tick and hash on one node.

## Constructors

| Member | Description |
| --- | --- |
| [`NodeHash(string, string, uint, long, string)`](#m-ctor-string-string-uint-long-string) | One scope's tick and hash on one node. |

### <a id="m-ctor-string-string-uint-long-string"></a>NodeHash(string, string, uint, long, string)

```csharp
public NodeHash(string Node, string Role, uint Scope, long Tick, string Hash);
```

One scope's tick and hash on one node.

**Parameters**

- `Node` (`string`): The node.
- `Role` (`string`): `server` or `client`.
- `Scope` (`uint`): The scope id.
- `Tick` (`long`): The scope's tick.
- `Hash` (`string`): The state hash (16 hex digits).

## Properties

| Member | Description |
| --- | --- |
| [`Hash`](#m-hash) | The state hash (16 hex digits). |
| [`Node`](#m-node) | The node. |
| [`Role`](#m-role) | `server` or `client`. |
| [`Scope`](#m-scope) | The scope id. |
| [`Tick`](#m-tick) | The scope's tick. |

### <a id="m-hash"></a>Hash

```csharp
public string Hash { get; init; }
```

The state hash (16 hex digits).

**Type:** `string`

### <a id="m-node"></a>Node

```csharp
public string Node { get; init; }
```

The node.

**Type:** `string`

### <a id="m-role"></a>Role

```csharp
public string Role { get; init; }
```

`server` or `client`.

**Type:** `string`

### <a id="m-scope"></a>Scope

```csharp
public uint Scope { get; init; }
```

The scope id.

**Type:** `uint`

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; init; }
```

The scope's tick.

**Type:** `long`
