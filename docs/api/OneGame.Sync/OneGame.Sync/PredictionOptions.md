[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# PredictionOptions class

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)

```csharp
public sealed class PredictionOptions
```

Settings for a replica's prediction ([`ReplicaScope.EnablePrediction()`](ReplicaScope.md#m-enableprediction-world-predictionoptions)).

## Constructors

| Member | Description |
| --- | --- |
| [`PredictionOptions()`](#m-ctor) |  |

### <a id="m-ctor"></a>PredictionOptions()

```csharp
public PredictionOptions();
```

## Properties

| Member | Description |
| --- | --- |
| [`AutoLead`](#m-autolead) | Measure the lead instead of trusting [`PredictionOptions.LeadTicks`](PredictionOptions.md#m-leadticks) alone. |
| [`DetectRollbacks`](#m-detectrollbacks) | Hash the predicted world before and after each reconciliation to count [`Prediction.Rollbacks`](Prediction.md#m-rollbacks) (reconciliations that changed what a view was showing). |
| [`LeadTicks`](#m-leadticks) | How many ticks the predicted world runs ahead of the last confirmed tick, at least. |
| [`MaxAheadTicks`](#m-maxaheadticks) | The predicted world stops advancing when it is this many ticks ahead of the confirmed state (a stalled link must not run the prediction away). |
| [`MaxLeadTicks`](#m-maxleadticks) | The most the measured lead may grow to. |
| [`MaxUnconfirmed`](#m-maxunconfirmed) | The most unanswered predicted commands kept for resimulation; older ones are given up on (the authority's state still arrives). |

### <a id="m-autolead"></a>AutoLead

```csharp
public bool AutoLead { get; init; }
```

Measure the lead instead of trusting [`PredictionOptions.LeadTicks`](PredictionOptions.md#m-leadticks) alone. When the authority answers a command, the tick it ran in is known exactly; if the predicted world ran that command earlier or later than the authority did, the lead moves by the difference (it follows the 75th percentile of the last 32 answers: up at once, down one tick per 8 answers). [`PredictionOptions.LeadTicks`](PredictionOptions.md#m-leadticks) is where it starts.

**Type:** `bool`

### <a id="m-detectrollbacks"></a>DetectRollbacks

```csharp
public bool DetectRollbacks { get; init; }
```

Hash the predicted world before and after each reconciliation to count [`Prediction.Rollbacks`](Prediction.md#m-rollbacks) (reconciliations that changed what a view was showing). Costs two world hashes per reconciliation; allocation-free.

**Type:** `bool`

### <a id="m-leadticks"></a>LeadTicks

```csharp
public int LeadTicks { get; init; }
```

How many ticks the predicted world runs ahead of the last confirmed tick, at least. A command sent now reaches the authority after about half a round trip and its effect comes back after the rest, so a lead near the round trip in ticks makes the predicted world show what the authority will show when the command's effect arrives. With [`PredictionOptions.AutoLead`](PredictionOptions.md#m-autolead) this is only the starting value.

**Type:** `int`

### <a id="m-maxaheadticks"></a>MaxAheadTicks

```csharp
public int MaxAheadTicks { get; init; }
```

The predicted world stops advancing when it is this many ticks ahead of the confirmed state (a stalled link must not run the prediction away).

**Type:** `int`

### <a id="m-maxleadticks"></a>MaxLeadTicks

```csharp
public int MaxLeadTicks { get; init; }
```

The most the measured lead may grow to.

**Type:** `int`

### <a id="m-maxunconfirmed"></a>MaxUnconfirmed

```csharp
public int MaxUnconfirmed { get; init; }
```

The most unanswered predicted commands kept for resimulation; older ones are given up on (the authority's state still arrives).

**Type:** `int`
