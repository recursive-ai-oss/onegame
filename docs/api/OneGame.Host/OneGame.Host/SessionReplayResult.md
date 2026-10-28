[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# SessionReplayResult record

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`SessionReplayResult`](SessionReplayResult.md)&gt;

```csharp
public sealed record SessionReplayResult : IEquatable<SessionReplayResult>
```

The outcome of replaying a [`SessionRecording`](SessionRecording.md).

## Constructors

| Member | Description |
| --- | --- |
| [`SessionReplayResult(bool, long, int, ulong, int, Divergence?)`](#m-ctor-bool-long-int-ulong-int-divergence) | The outcome of replaying a [`SessionRecording`](SessionRecording.md). |

### <a id="m-ctor-bool-long-int-ulong-int-divergence"></a>SessionReplayResult(bool, long, int, ulong, int, Divergence?)

```csharp
public SessionReplayResult(bool Ok, long TicksRun, int CheckpointsVerified, ulong FinalHash, int Segment, Divergence? Divergence);
```

The outcome of replaying a [`SessionRecording`](SessionRecording.md).

**Parameters**

- `Ok` (`bool`): True when every checkpoint of every segment matched.
- `TicksRun` (`long`): Ticks simulated over all segments.
- `CheckpointsVerified` (`int`): Checkpoints that matched.
- `FinalHash` (`ulong`): The hash of the last world the replay reached.
- `Segment` (`int`): Index of the segment that diverged, or -1.
- `Divergence` ([`Divergence`](../../OneGame.Sync/OneGame.Sync/Divergence.md)?): The first mismatch, or null.

## Properties

| Member | Description |
| --- | --- |
| [`CheckpointsVerified`](#m-checkpointsverified) | Checkpoints that matched. |
| [`Divergence`](#m-divergence) | The first mismatch, or null. |
| [`FinalHash`](#m-finalhash) | The hash of the last world the replay reached. |
| [`Ok`](#m-ok) | True when every checkpoint of every segment matched. |
| [`Segment`](#m-segment) | Index of the segment that diverged, or -1. |
| [`TicksRun`](#m-ticksrun) | Ticks simulated over all segments. |

### <a id="m-checkpointsverified"></a>CheckpointsVerified

```csharp
public int CheckpointsVerified { get; init; }
```

Checkpoints that matched.

**Type:** `int`

### <a id="m-divergence"></a>Divergence

```csharp
public Divergence? Divergence { get; init; }
```

The first mismatch, or null.

**Type:** [`Divergence`](../../OneGame.Sync/OneGame.Sync/Divergence.md)?

### <a id="m-finalhash"></a>FinalHash

```csharp
public ulong FinalHash { get; init; }
```

The hash of the last world the replay reached.

**Type:** `ulong`

### <a id="m-ok"></a>Ok

```csharp
public bool Ok { get; init; }
```

True when every checkpoint of every segment matched.

**Type:** `bool`

### <a id="m-segment"></a>Segment

```csharp
public int Segment { get; init; }
```

Index of the segment that diverged, or -1.

**Type:** `int`

### <a id="m-ticksrun"></a>TicksRun

```csharp
public long TicksRun { get; init; }
```

Ticks simulated over all segments.

**Type:** `long`
