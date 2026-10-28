[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# DivergenceReason enum

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)

```csharp
public enum DivergenceReason : int
```

Why a replay stopped.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-startsnapshot"></a>`StartSnapshot` | `0` | The start snapshot could not be loaded into the replay world. |
| <a id="m-starthash"></a>`StartHash` | `1` | The world loaded from the start snapshot does not hash to the recorded start hash. |
| <a id="m-checkpoint"></a>`Checkpoint` | `2` | A checkpoint hash differs from the recorded one. |
| <a id="m-unusable"></a>`Unusable` | `3` | The replay world is not an authority world, or the recording is inconsistent. |
| <a id="m-toolong"></a>`TooLong` | `4` | The recording asks for more ticks than [`ReplayOptions.MaxTicks`](ReplayOptions.md#m-maxticks) allows. Nothing was run. |
