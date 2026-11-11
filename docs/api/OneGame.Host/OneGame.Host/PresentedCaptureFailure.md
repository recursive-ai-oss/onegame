[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# PresentedCaptureFailure enum

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)

```csharp
public enum PresentedCaptureFailure : int
```

Why a presented frame could not be captured.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-none"></a>`None` | `0` | No failure. |
| <a id="m-nowindow"></a>`NoWindow` | `1` | The game has no native window to present to. |
| <a id="m-minimized"></a>`Minimized` | `2` | The window is minimized, so nothing is drawn. |
| <a id="m-windownotdrawn"></a>`WindowNotDrawn` | `3` | The window system has stopped drawing the window (covered or hidden); drawing waits so the sim and network keep running. |
| <a id="m-nobackbuffer"></a>`NoBackbuffer` | `4` | The swapchain gave no free image within its acquire timeout. |
| <a id="m-surfacelost"></a>`SurfaceLost` | `5` | The window system took the surface; a new swapchain is made on the next frame. |
