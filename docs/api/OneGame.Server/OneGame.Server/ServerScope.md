[API reference](../../index.md) › [OneGame.Server](../index.md) › OneGame.Server

# ServerScope class

**Namespace:** `OneGame.Server`  
**Package:** [`RecursiveAI.OneGame.Server`](../index.md)

```csharp
public sealed class ServerScope
```

One hosted scope.

## Properties

| Member | Description |
| --- | --- |
| [`Authority`](#m-authority) | The authority half: world, subscribers, outcomes. |
| [`Id`](#m-id) | The scope id. |
| [`World`](#m-world) | The authority world. |

### <a id="m-authority"></a>Authority

```csharp
public AuthorityScope Authority { get; }
```

The authority half: world, subscribers, outcomes.

**Type:** [`AuthorityScope`](../../OneGame.Sync/OneGame.Sync/AuthorityScope.md)

### <a id="m-id"></a>Id

```csharp
public ScopeId Id { get; }
```

The scope id.

**Type:** [`ScopeId`](../../OneGame.Sync/OneGame.Sync/ScopeId.md)

### <a id="m-world"></a>World

```csharp
public World World { get; }
```

The authority world.

**Type:** [`World`](../../OneGame.Core/OneGame.Core/World.md)
