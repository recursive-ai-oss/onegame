[API reference](../../index.md) › [OneGame.Testing](../index.md) › OneGame.Testing

# Determinism static class

**Namespace:** `OneGame.Testing`  
**Package:** [`RecursiveAI.OneGame.Testing`](../index.md)

```csharp
public static class Determinism
```

Checks that a scripted run is reproducible: twice from scratch, through a snapshot, and through a recording.

## Nested types

| Type | Description |
| --- | --- |
| [`Determinism.Drive`](Determinism.Drive.md) | Does something to the game before frame number `frame` runs: send a command, click, press a key. |

## Methods

| Member | Description |
| --- | --- |
| [`Replay(Func<GameDefinition>, Determinism.Drive, int, TestOptions?)`](#m-replay-func-gamedefinition-determinism-drive-int-testoptions) | Records the script, replays the recording in a new game and compares every tick. |
| [`RunTwice(Func<GameDefinition>, Determinism.Drive, int, TestOptions?)`](#m-runtwice-func-gamedefinition-determinism-drive-int-testoptions) | Runs the script twice in fresh games and compares the state hash after every frame. |
| [`SnapshotRestore(Func<GameDefinition>, Determinism.Drive, int, int, TestOptions?)`](#m-snapshotrestore-func-gamedefinition-determinism-drive-int-int-testoptions) | Runs the script straight through, and again with a snapshot taken after `splitFrame` frames and restored into a new game that runs the rest. |

### <a id="m-replay-func-gamedefinition-determinism-drive-int-testoptions"></a>Replay(Func&lt;GameDefinition&gt;, Determinism.Drive, int, TestOptions?)

```csharp
public static DeterminismReport Replay(Func<GameDefinition> make, Determinism.Drive drive, int frames, TestOptions? options = null);
```

Records the script, replays the recording in a new game and compares every tick.

**Parameters**

- `make` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-1)&lt;[`GameDefinition`](../../OneGame.Host/OneGame.Host/GameDefinition.md)&gt;)
- `drive` (`Determinism.`[`Drive`](Determinism.Drive.md))
- `frames` (`int`)
- `options` ([`TestOptions`](TestOptions.md)?)

**Returns:** [`DeterminismReport`](DeterminismReport.md)

### <a id="m-runtwice-func-gamedefinition-determinism-drive-int-testoptions"></a>RunTwice(Func&lt;GameDefinition&gt;, Determinism.Drive, int, TestOptions?)

```csharp
public static DeterminismReport RunTwice(Func<GameDefinition> make, Determinism.Drive drive, int frames, TestOptions? options = null);
```

Runs the script twice in fresh games and compares the state hash after every frame.

**Parameters**

- `make` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-1)&lt;[`GameDefinition`](../../OneGame.Host/OneGame.Host/GameDefinition.md)&gt;)
- `drive` (`Determinism.`[`Drive`](Determinism.Drive.md))
- `frames` (`int`)
- `options` ([`TestOptions`](TestOptions.md)?)

**Returns:** [`DeterminismReport`](DeterminismReport.md)

### <a id="m-snapshotrestore-func-gamedefinition-determinism-drive-int-int-testoptions"></a>SnapshotRestore(Func&lt;GameDefinition&gt;, Determinism.Drive, int, int, TestOptions?)

```csharp
public static DeterminismReport SnapshotRestore(Func<GameDefinition> make, Determinism.Drive drive, int frames, int splitFrame, TestOptions? options = null);
```

Runs the script straight through, and again with a snapshot taken after `splitFrame` frames and restored into a new game that runs the rest. The hashes after the split must match.

**Parameters**

- `make` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-1)&lt;[`GameDefinition`](../../OneGame.Host/OneGame.Host/GameDefinition.md)&gt;)
- `drive` (`Determinism.`[`Drive`](Determinism.Drive.md))
- `frames` (`int`)
- `splitFrame` (`int`)
- `options` ([`TestOptions`](TestOptions.md)?)

**Returns:** [`DeterminismReport`](DeterminismReport.md)
