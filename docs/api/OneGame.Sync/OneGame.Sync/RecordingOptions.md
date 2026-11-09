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
| [`TableDiagnostics`](#m-tablediagnostics) | Retains optional named table snapshots at checkpoints for detailed replay differences. |

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

### <a id="m-tablediagnostics"></a>TableDiagnostics

```csharp
public bool TableDiagnostics { get; init; }
```

Retains optional named table snapshots at checkpoints for detailed replay differences. Allocates and increases recording size; default false.

**Type:** `bool`
