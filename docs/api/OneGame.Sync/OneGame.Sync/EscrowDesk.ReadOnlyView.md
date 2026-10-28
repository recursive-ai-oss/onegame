[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# EscrowDesk.ReadOnlyView struct

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)  
**Declared in:** [`EscrowDesk`](EscrowDesk.md)

```csharp
public readonly struct EscrowDesk.ReadOnlyView
```

A read-only window onto the synced state, for views.

## Properties

| Member | Description |
| --- | --- |
| [`Coordinator`](#m-coordinator) | The synced value of `Coordinator`. |
| [`Id`](#m-id) | The entity's id. |

### <a id="m-coordinator"></a>Coordinator

```csharp
public uint Coordinator { get; }
```

The synced value of `Coordinator`.

**Type:** `uint`

### <a id="m-id"></a>Id

```csharp
public EntityId Id { get; }
```

The entity's id.

**Type:** [`EntityId`](../../OneGame.Core/OneGame.Core/EntityId.md)
