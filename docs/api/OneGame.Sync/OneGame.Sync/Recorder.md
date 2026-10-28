[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# Recorder class

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)

```csharp
public sealed class Recorder
```

Captures a run on an [`AuthorityScope`](AuthorityScope.md). Create one with [`AuthorityScope.StartRecording()`](AuthorityScope.md#m-startrecording-recordingoptions).

## Properties

| Member | Description |
| --- | --- |
| [`Recording`](#m-recording) | The recording so far. |

### <a id="m-recording"></a>Recording

```csharp
public Recording Recording { get; }
```

The recording so far. Complete once [`Recorder.Stop()`](Recorder.md#m-stop) has been called.

**Type:** [`Recording`](Recording.md)

## Methods

| Member | Description |
| --- | --- |
| [`Stop()`](#m-stop) | Ends the recording (adding a final checkpoint) and returns it. |

### <a id="m-stop"></a>Stop()

```csharp
public Recording Stop();
```

Ends the recording (adding a final checkpoint) and returns it.

**Returns:** [`Recording`](Recording.md)
