[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# ScopeHash struct

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ScopeHash`](ScopeHash.md)&gt;

```csharp
public readonly struct ScopeHash : IEquatable<ScopeHash>
```

One scope's state on one node, as listed by [`Cluster.Hashes()`](Cluster.md#m-hashes).

## Constructors

| Member | Description |
| --- | --- |
| [`ScopeHash(string, ScopeId, WorldRole, long, ulong)`](#m-ctor-string-scopeid-worldrole-long-ulong) | One scope's state on one node, as listed by [`Cluster.Hashes()`](Cluster.md#m-hashes). |

### <a id="m-ctor-string-scopeid-worldrole-long-ulong"></a>ScopeHash(string, ScopeId, WorldRole, long, ulong)

```csharp
public ScopeHash(string Node, ScopeId Scope, WorldRole Role, long Tick, ulong Hash);
```

One scope's state on one node, as listed by [`Cluster.Hashes()`](Cluster.md#m-hashes).

**Parameters**

- `Node` (`string`): The node name.
- `Scope` ([`ScopeId`](ScopeId.md)): The scope.
- `Role` ([`WorldRole`](../../OneGame.Core/OneGame.Core/WorldRole.md)): Authority or replica.
- `Tick` (`long`): The world's completed tick count.
- `Hash` (`ulong`): The world hash.

## Properties

| Member | Description |
| --- | --- |
| [`Hash`](#m-hash) | The world hash. |
| [`Node`](#m-node) | The node name. |
| [`Role`](#m-role) | Authority or replica. |
| [`Scope`](#m-scope) | The scope. |
| [`Tick`](#m-tick) | The world's completed tick count. |

### <a id="m-hash"></a>Hash

```csharp
public ulong Hash { get; init; }
```

The world hash.

**Type:** `ulong`

### <a id="m-node"></a>Node

```csharp
public string Node { get; init; }
```

The node name.

**Type:** `string`

### <a id="m-role"></a>Role

```csharp
public WorldRole Role { get; init; }
```

Authority or replica.

**Type:** [`WorldRole`](../../OneGame.Core/OneGame.Core/WorldRole.md)

### <a id="m-scope"></a>Scope

```csharp
public ScopeId Scope { get; init; }
```

The scope.

**Type:** [`ScopeId`](ScopeId.md)

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; init; }
```

The world's completed tick count.

**Type:** `long`
