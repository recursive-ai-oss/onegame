[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# AcquireStatus enum

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)

```csharp
public enum AcquireStatus : int
```

Result of acquiring the next swapchain image.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-ready"></a>`Ready` | `0` | An image was acquired. |
| <a id="m-skipped"></a>`Skipped` | `1` | The surface has no drawable area right now (minimized), or no image became free within [`GpuSwapchain.AcquireTimeout`](GpuSwapchain.md#m-acquiretimeout). Skip the frame. |
