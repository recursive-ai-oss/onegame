[API reference](../../index.md) › [OneGame.Audio](../index.md) › OneGame.Audio

# SinkWriteResult enum

**Namespace:** `OneGame.Audio`  
**Package:** [`RecursiveAI.OneGame.Audio`](../index.md)

```csharp
public enum SinkWriteResult : int
```

The outcome of one [`IAudioSink.Write()`](IAudioSink.md#m-write-readonlyspan-float).

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-ok"></a>`Ok` | `0` | All frames were accepted. |
| <a id="m-underrun"></a>`Underrun` | `1` | The device had run dry before this write and recovered; all frames were accepted. |
| <a id="m-lost"></a>`Lost` | `2` | The device is gone. The sink must be discarded. |
