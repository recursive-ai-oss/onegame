[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# EscrowTrade class

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)

```csharp
public sealed class EscrowTrade
```

A trade between two scopes, as the coordinator records it.

## Properties

| Member | Description |
| --- | --- |
| [`IsFinal`](#m-isfinal) | True once the trade can no longer change. |
| [`Legs`](#m-legs) | The two legs. |
| [`OpenedTick`](#m-openedtick) | The coordinator tick the trade was opened in. |
| [`State`](#m-state) | Where the trade stands. |
| [`StateTick`](#m-statetick) | The coordinator tick of the last change. |
| [`Tx`](#m-tx) | The transaction id, chosen by whoever opened the trade; unique. |

### <a id="m-isfinal"></a>IsFinal

```csharp
public bool IsFinal { get; }
```

True once the trade can no longer change.

**Type:** `bool`

### <a id="m-legs"></a>Legs

```csharp
public IReadOnlyList<EscrowLeg> Legs { get; }
```

The two legs.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`EscrowLeg`](EscrowLeg.md)&gt;

### <a id="m-openedtick"></a>OpenedTick

```csharp
public long OpenedTick { get; }
```

The coordinator tick the trade was opened in.

**Type:** `long`

### <a id="m-state"></a>State

```csharp
public EscrowState State { get; }
```

Where the trade stands.

**Type:** [`EscrowState`](EscrowState.md)

### <a id="m-statetick"></a>StateTick

```csharp
public long StateTick { get; }
```

The coordinator tick of the last change. Hosts act on a decision only once a save at or after this tick exists.

**Type:** `long`

### <a id="m-tx"></a>Tx

```csharp
public ulong Tx { get; }
```

The transaction id, chosen by whoever opened the trade; unique.

**Type:** `ulong`

## Methods

| Member | Description |
| --- | --- |
| [`DeliveryTarget(int)`](#m-deliverytarget-int) | The scope and owner a leg's value goes to: the counterparty after a commit, the giver after a cancel. |

### <a id="m-deliverytarget-int"></a>DeliveryTarget(int)

```csharp
public (ScopeId Scope, uint Owner) DeliveryTarget(int leg);
```

The scope and owner a leg's value goes to: the counterparty after a commit, the giver after a cancel.

**Parameters**

- `leg` (`int`)

**Returns:** ([`ScopeId`](ScopeId.md) `Scope, uint Owner)`
