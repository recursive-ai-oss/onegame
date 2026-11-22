[API reference](../../index.md) › [OneGame.Render2D](../index.md) › OneGame.Render2D

# ISceneMarkerSink interface

**Namespace:** `OneGame.Render2D`  
**Package:** [`RecursiveAI.OneGame.Render2D`](../index.md)

```csharp
public interface ISceneMarkerSink
```

Receives the markers a [`Scene2D`](Scene2D.md) holds (see [`Scene2D.Mark()`](Scene2D.md#m-mark-layer2d-float-int)) at their place in the draw order, while [`Renderer2D.DrawScene()`](Renderer2D.md#m-drawscene-scene2d-camera2d-postchain-iscenemarkersink) runs. The host's immediate-mode text uses it to interleave with sprites.

## Methods

| Member | Description |
| --- | --- |
| [`DrawMarker(int, Layer2D, in Matrix3x2)`](#m-drawmarker-int-layer2d-matrix3x2) | Called when a marker's turn comes; draw whatever it stands for on [`Renderer2D.Ui`](Renderer2D.md#m-ui). |

### <a id="m-drawmarker-int-layer2d-matrix3x2"></a>DrawMarker(int, Layer2D, in Matrix3x2)

```csharp
void DrawMarker(int id, Layer2D layer, in Matrix3x2 view);
```

Called when a marker's turn comes; draw whatever it stands for on [`Renderer2D.Ui`](Renderer2D.md#m-ui).

**Parameters**

- `id` (`int`): The number given to [`Scene2D.Mark()`](Scene2D.md#m-mark-layer2d-float-int).
- `layer` ([`Layer2D`](Layer2D.md)): The layer the marker was put on.
- `view` ([`Matrix3x2`](https://learn.microsoft.com/dotnet/api/system.numerics.matrix3x2)): That layer's matrix to framebuffer pixels, as [`Layer2D.View()`](Layer2D.md#m-view-camera2d-vector2) gives it.
