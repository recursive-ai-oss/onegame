[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# EscrowLeg class

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)

```csharp
public sealed class EscrowLeg
```

One direction of a trade: [`EscrowLeg.Giver`](EscrowLeg.md#m-giver) on [`EscrowLeg.Scope`](EscrowLeg.md#m-scope) gives [`EscrowLeg.Amount`](EscrowLeg.md#m-amount) of [`EscrowLeg.Kind`](EscrowLeg.md#m-kind) to [`EscrowLeg.Receiver`](EscrowLeg.md#m-receiver) on the other leg's scope.

## Constructors

| Member | Description |
| --- | --- |
| [`EscrowLeg()`](#m-ctor) |  |

### <a id="m-ctor"></a>EscrowLeg()

```csharp
public EscrowLeg();
```

## Properties

| Member | Description |
| --- | --- |
| [`Amount`](#m-amount) | How much; positive. |
| [`Delivery`](#m-delivery) | Whether the value was handed on. |
| [`Giver`](#m-giver) | Who gives. |
| [`Hold`](#m-hold) | Whether the value was taken from the giver. |
| [`Kind`](#m-kind) | What kind of value (the game's own numbering). |
| [`Receiver`](#m-receiver) | Who receives, on the other leg's scope. |
| [`Scope`](#m-scope) | The scope that holds the giver. |

### <a id="m-amount"></a>Amount

```csharp
public long Amount { get; set; }
```

How much; positive.

**Type:** `long`

### <a id="m-delivery"></a>Delivery

```csharp
public EscrowDelivery Delivery { get; }
```

Whether the value was handed on.

**Type:** [`EscrowDelivery`](EscrowDelivery.md)

### <a id="m-giver"></a>Giver

```csharp
public uint Giver { get; set; }
```

Who gives.

**Type:** `uint`

### <a id="m-hold"></a>Hold

```csharp
public EscrowHold Hold { get; }
```

Whether the value was taken from the giver.

**Type:** [`EscrowHold`](EscrowHold.md)

### <a id="m-kind"></a>Kind

```csharp
public uint Kind { get; set; }
```

What kind of value (the game's own numbering).

**Type:** `uint`

### <a id="m-receiver"></a>Receiver

```csharp
public uint Receiver { get; set; }
```

Who receives, on the other leg's scope.

**Type:** `uint`

### <a id="m-scope"></a>Scope

```csharp
public ScopeId Scope { get; set; }
```

The scope that holds the giver.

**Type:** [`ScopeId`](ScopeId.md)
