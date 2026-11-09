[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# GpuTimingStatus enum

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)

```csharp
public enum GpuTimingStatus : int
```

Whether GPU pass times can be read.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-none"></a>`None` | `0` | No graphics device, or the stage has not drawn a frame yet. |
| <a id="m-unsupported"></a>`Unsupported` | `1` | The backend cannot time passes (Metal and WebGPU). |
| <a id="m-ready"></a>`Ready` | `2` | Times are available (a frame or two behind). |
