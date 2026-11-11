[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# IFrameOverlay interface

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)

```csharp
public interface IFrameOverlay
```

Something drawn on top of the finished frame, after the game's UI and sprites: the development panel and a game's own overlays ([`Render2DStage.AddOverlay()`](Render2DStage.md#m-addoverlay-iframeoverlay)). It sees the frame only through an [`OverlayCanvas`](OverlayCanvas.md), so it can draw text, rectangles and images and nothing else.

## Methods

| Member | Description |
| --- | --- |
| [`Draw(OverlayCanvas)`](#m-draw-overlaycanvas) | Draws this frame's overlay. |

### <a id="m-draw-overlaycanvas"></a>Draw(OverlayCanvas)

```csharp
void Draw(OverlayCanvas canvas);
```

Draws this frame's overlay.

**Parameters**

- `canvas` ([`OverlayCanvas`](OverlayCanvas.md)): Where to draw; valid only during the call.
