[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# PresentMode enum

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)

```csharp
public enum PresentMode : int
```

How a swapchain paces presentation.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-vsync"></a>`Vsync` | `0` | Wait for vertical blank; always available. |
| <a id="m-lowlatency"></a>`LowLatency` | `1` | Wait for vertical blank but replace the queued image with the newest, when supported. |
| <a id="m-immediate"></a>`Immediate` | `2` | Present immediately, which may tear, when supported. |
