[API reference](../../index.md) › [OneGame.Audio](../index.md) › OneGame.Audio

# AudioOutputStatus enum

**Namespace:** `OneGame.Audio`  
**Package:** [`RecursiveAI.OneGame.Audio`](../index.md)

```csharp
public enum AudioOutputStatus : int
```

Where an [`AudioOutput`](AudioOutput.md) is in its life.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-created"></a>`Created` | `0` | Created, no source attached yet. |
| <a id="m-running"></a>`Running` | `1` | Samples are going to a device (or a capture sink). |
| <a id="m-nodevice"></a>`NoDevice` | `2` | No device is open. The output keeps rendering at real-time pace and discards the samples, so the game behaves the same without sound, and retries opening in the background. |
| <a id="m-stopped"></a>`Stopped` | `3` | Disposed. |
