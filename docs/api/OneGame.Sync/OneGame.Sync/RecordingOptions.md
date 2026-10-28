[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# RecordingOptions class

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)

```csharp
public sealed class RecordingOptions
```

How a recording samples the run.

## Constructors

| Member | Description |
| --- | --- |
| [`RecordingOptions()`](#m-ctor) |  |

### <a id="m-ctor"></a>RecordingOptions()

```csharp
public RecordingOptions();
```

## Properties

| Member | Description |
| --- | --- |
| [`CheckpointInterval`](#m-checkpointinterval) | Ticks between hash checkpoints. |
| [`DetailLimit`](#m-detaillimit) | Checkpoints keep a per-entity digest only for worlds with at most this many entities. |

### <a id="m-checkpointinterval"></a>CheckpointInterval

```csharp
public int CheckpointInterval { get; init; }
```

Ticks between hash checkpoints. 1 pins any divergence to its exact tick; larger values name the first checkpoint after it. The last tick always gets one.

**Type:** `int`

### <a id="m-detaillimit"></a>DetailLimit

```csharp
public int DetailLimit { get; init; }
```

Checkpoints keep a per-entity digest only for worlds with at most this many entities.

**Type:** `int`
