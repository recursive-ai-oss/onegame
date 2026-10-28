[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# FollowTarget record

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`FollowTarget`](FollowTarget.md)&gt;

```csharp
public sealed record FollowTarget : IEquatable<FollowTarget>
```

Where a node should subscribe to follow entities that moved to another scope: a link to the node that holds it and an empty replica world.

## Constructors

| Member | Description |
| --- | --- |
| [`FollowTarget(Link, World)`](#m-ctor-link-world) | Where a node should subscribe to follow entities that moved to another scope: a link to the node that holds it and an empty replica world. |

### <a id="m-ctor-link-world"></a>FollowTarget(Link, World)

```csharp
public FollowTarget(Link Link, World World);
```

Where a node should subscribe to follow entities that moved to another scope: a link to the node that holds it and an empty replica world.

**Parameters**

- `Link` ([`Link`](Link.md)): A link from this node to the destination scope's authority (or a relay of it), already trusted to read it.
- `World` ([`World`](../../OneGame.Core/OneGame.Core/World.md)): An empty replica world with the same registry and tables as the destination's.

## Properties

| Member | Description |
| --- | --- |
| [`Link`](#m-link) | A link from this node to the destination scope's authority (or a relay of it), already trusted to read it. |
| [`World`](#m-world) | An empty replica world with the same registry and tables as the destination's. |

### <a id="m-link"></a>Link

```csharp
public Link Link { get; init; }
```

A link from this node to the destination scope's authority (or a relay of it), already trusted to read it.

**Type:** [`Link`](Link.md)

### <a id="m-world"></a>World

```csharp
public World World { get; init; }
```

An empty replica world with the same registry and tables as the destination's.

**Type:** [`World`](../../OneGame.Core/OneGame.Core/World.md)
