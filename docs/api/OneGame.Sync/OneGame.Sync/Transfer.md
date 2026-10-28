[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# Transfer class

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)

```csharp
public sealed class Transfer
```

A running or finished move of entities from one scope to a scope on another node ([`AuthorityScope.BeginTransfer()`](AuthorityScope.md#m-begintransfer-link-scopeid-ienumerable-entityid)).

## Properties

| Member | Description |
| --- | --- |
| [`Destination`](#m-destination) | The scope the entities go to. |
| [`Entities`](#m-entities) | The entities being moved, by their ids in the source scope. |
| [`Id`](#m-id) | The transfer id (source scope in the high 32 bits); 0 until the transfer starts. |
| [`Link`](#m-link) | The link to the destination node. |
| [`NewIds`](#m-newids) | The ids the entities got in the destination scope, in the order of [`Transfer.Entities`](Transfer.md#m-entities); empty until [`TransferState.Complete`](TransferState.md#m-complete). |
| [`Reason`](#m-reason) | Why the transfer was aborted, or a note on the last problem; null otherwise. |
| [`State`](#m-state) | Where the transfer stands. |

### <a id="m-destination"></a>Destination

```csharp
public ScopeId Destination { get; }
```

The scope the entities go to.

**Type:** [`ScopeId`](ScopeId.md)

### <a id="m-entities"></a>Entities

```csharp
public IReadOnlyList<EntityId> Entities { get; }
```

The entities being moved, by their ids in the source scope.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`EntityId`](../../OneGame.Core/OneGame.Core/EntityId.md)&gt;

### <a id="m-id"></a>Id

```csharp
public ulong Id { get; }
```

The transfer id (source scope in the high 32 bits); 0 until the transfer starts.

**Type:** `ulong`

### <a id="m-link"></a>Link

```csharp
public Link Link { get; }
```

The link to the destination node.

**Type:** [`Link`](Link.md)

### <a id="m-newids"></a>NewIds

```csharp
public IReadOnlyList<EntityId> NewIds { get; }
```

The ids the entities got in the destination scope, in the order of [`Transfer.Entities`](Transfer.md#m-entities); empty until [`TransferState.Complete`](TransferState.md#m-complete).

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`EntityId`](../../OneGame.Core/OneGame.Core/EntityId.md)&gt;

### <a id="m-reason"></a>Reason

```csharp
public string? Reason { get; }
```

Why the transfer was aborted, or a note on the last problem; null otherwise.

**Type:** `string?`

### <a id="m-state"></a>State

```csharp
public TransferState State { get; }
```

Where the transfer stands.

**Type:** [`TransferState`](TransferState.md)
