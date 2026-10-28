[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# Prediction class

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)

```csharp
public sealed class Prediction
```

Client-side prediction for one replica. The replica's own world stays exactly the authority's confirmed state. Prediction keeps a second, writable world beside it: commands marked `[Command(Predicted = true)]` run on it the moment they are sent, and whenever authoritative state arrives it is copied over the predicted world, the commands the authority has not yet applied are run again, and the predicted world is simulated forward to where it was (a rollback and resimulation). Views draw [`Prediction.World`](Prediction.md#m-world); sim state is never blended, so any smoothing of a visible correction belongs in view code (see [`Prediction.Reconciling`](Prediction.md#m-reconciling) and [`Prediction.Reconciled`](Prediction.md#m-reconciled)).

Reading [`Prediction.World`](Prediction.md#m-world) (or calling [`Prediction.Advance()`](Prediction.md#m-advance)) first brings the prediction up to date with whatever the replica has received since, so call them from the thread that pumps the node.

## Properties

| Member | Description |
| --- | --- |
| [`CommandsPredicted`](#m-commandspredicted) | Predicted commands run so far. |
| [`CommandsSkipped`](#m-commandsskipped) | Predicted commands that were sent without being predicted because they use a random stream this replica does not receive. |
| [`LeadTicks`](#m-leadticks) | How many ticks ahead of the confirmed state the predicted world is rebuilt to when a frame arrives: [`PredictionOptions.LeadTicks`](PredictionOptions.md#m-leadticks), then what the measured round trips asked for. |
| [`Reconciliations`](#m-reconciliations) | How many times the predicted world was rebuilt from confirmed state and simulated forward. |
| [`ResimulatedTicks`](#m-resimulatedticks) | Ticks simulated again during reconciliations. |
| [`Rollbacks`](#m-rollbacks) | Reconciliations that changed what the view was showing at the same tick: a command the authority rejected, or state it changed that the prediction missed. |
| [`Tick`](#m-tick) | The tick the predicted world is at. |
| [`Unconfirmed`](#m-unconfirmed) | Predicted commands the authority has not yet applied to the confirmed state. |
| [`World`](#m-world) | The predicted world: what the authority will most likely show once the commands in flight are applied. |

### <a id="m-commandspredicted"></a>CommandsPredicted

```csharp
public long CommandsPredicted { get; }
```

Predicted commands run so far.

**Type:** `long`

### <a id="m-commandsskipped"></a>CommandsSkipped

```csharp
public long CommandsSkipped { get; }
```

Predicted commands that were sent without being predicted because they use a random stream this replica does not receive.

**Type:** `long`

### <a id="m-leadticks"></a>LeadTicks

```csharp
public int LeadTicks { get; }
```

How many ticks ahead of the confirmed state the predicted world is rebuilt to when a frame arrives: [`PredictionOptions.LeadTicks`](PredictionOptions.md#m-leadticks), then what the measured round trips asked for.

**Type:** `int`

### <a id="m-reconciliations"></a>Reconciliations

```csharp
public long Reconciliations { get; }
```

How many times the predicted world was rebuilt from confirmed state and simulated forward.

**Type:** `long`

### <a id="m-resimulatedticks"></a>ResimulatedTicks

```csharp
public long ResimulatedTicks { get; }
```

Ticks simulated again during reconciliations.

**Type:** `long`

### <a id="m-rollbacks"></a>Rollbacks

```csharp
public long Rollbacks { get; }
```

Reconciliations that changed what the view was showing at the same tick: a command the authority rejected, or state it changed that the prediction missed.

**Type:** `long`

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; }
```

The tick the predicted world is at.

**Type:** `long`

### <a id="m-unconfirmed"></a>Unconfirmed

```csharp
public int Unconfirmed { get; }
```

Predicted commands the authority has not yet applied to the confirmed state.

**Type:** `int`

### <a id="m-world"></a>World

```csharp
public World World { get; }
```

The predicted world: what the authority will most likely show once the commands in flight are applied. Read it; send commands through its entities or the replica's.

**Type:** [`World`](../../OneGame.Core/OneGame.Core/World.md)

## Events

| Member | Description |
| --- | --- |
| [`Reconciled`](#m-reconciled) | Raised after a reconciliation, with the corrected predicted world. |
| [`Reconciling`](#m-reconciling) | Raised before a reconciliation overwrites the predicted world: it still holds what the view has been showing. |

### <a id="m-reconciled"></a>Reconciled

```csharp
public event Action<Prediction>? Reconciled;
```

Raised after a reconciliation, with the corrected predicted world. A view compares with what it recorded in [`Prediction.Reconciling`](Prediction.md#m-reconciling) and blends the difference in over a few frames.

**Type:** [`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)&lt;[`Prediction`](Prediction.md)&gt;?

### <a id="m-reconciling"></a>Reconciling

```csharp
public event Action<Prediction>? Reconciling;
```

Raised before a reconciliation overwrites the predicted world: it still holds what the view has been showing. A view records the positions it draws here.

**Type:** [`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)&lt;[`Prediction`](Prediction.md)&gt;?

## Methods

| Member | Description |
| --- | --- |
| [`Advance()`](#m-advance) | Simulates one tick on the predicted world, one per local frame, so the view moves while the authority's frames are still on their way. |
| [`Reconcile()`](#m-reconcile) | Brings the predicted world up to date now. |

### <a id="m-advance"></a>Advance()

```csharp
public bool Advance();
```

Simulates one tick on the predicted world, one per local frame, so the view moves while the authority's frames are still on their way. Returns false (and does nothing) while the replica has no state yet, or when the predicted world is already [`PredictionOptions.MaxAheadTicks`](PredictionOptions.md#m-maxaheadticks) ahead.

**Returns:** `bool`

### <a id="m-reconcile"></a>Reconcile()

```csharp
public void Reconcile();
```

Brings the predicted world up to date now. Reading [`Prediction.World`](Prediction.md#m-world) does it already.
