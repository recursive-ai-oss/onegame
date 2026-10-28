[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# ReplayResult class

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)

```csharp
public sealed class ReplayResult
```

The outcome of [`Replayer.Replay()`](Replayer.md#m-replay-recording-world-replayoptions).

## Properties

| Member | Description |
| --- | --- |
| [`CheckpointsVerified`](#m-checkpointsverified) | Checkpoints that matched. |
| [`Divergence`](#m-divergence) | The first mismatch, or null. |
| [`FinalHash`](#m-finalhash) | The replay world's hash when it stopped. |
| [`Ok`](#m-ok) | True when every checkpoint matched. |
| [`TicksRun`](#m-ticksrun) | Ticks simulated. |

### <a id="m-checkpointsverified"></a>CheckpointsVerified

```csharp
public int CheckpointsVerified { get; }
```

Checkpoints that matched.

**Type:** `int`

### <a id="m-divergence"></a>Divergence

```csharp
public Divergence? Divergence { get; }
```

The first mismatch, or null.

**Type:** [`Divergence`](Divergence.md)?

### <a id="m-finalhash"></a>FinalHash

```csharp
public ulong FinalHash { get; }
```

The replay world's hash when it stopped.

**Type:** `ulong`

### <a id="m-ok"></a>Ok

```csharp
public bool Ok { get; }
```

True when every checkpoint matched.

**Type:** `bool`

### <a id="m-ticksrun"></a>TicksRun

```csharp
public long TicksRun { get; }
```

Ticks simulated.

**Type:** `long`

## Methods

| Member | Description |
| --- | --- |
| [`ToJson()`](#m-tojson) | The result as JSON. |

### <a id="m-tojson"></a>ToJson()

```csharp
public string ToJson();
```

The result as JSON.

**Returns:** `string`
