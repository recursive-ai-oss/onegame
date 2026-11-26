[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# FluidSurface2D class

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)  
**Implements:** [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class FluidSurface2D : IDisposable
```

Draws a particle fluid as one smooth body instead of separate dots. Each frame the particles are splatted as discs onto a layer of their own, the layer is blurred, and a threshold turns the blurred field into a filled shape with a rim: neighbouring particles merge, a stream stays one piece, and a pool is a flat sheet. It is view code, fed from a copy of the particle positions (`PhysicsSpace2D.GetFluidPositions`), and allocates nothing per frame once it has been made.

Make one and keep it; call [`FluidSurface2D.Draw()`](FluidSurface2D.md#m-draw-draw2d-readonlyspan-vector2-float-bool-float) every frame from a view. The surface lives on a named layer, which draws in the order layers were made, so call [`FluidSurface2D.Draw()`](FluidSurface2D.md#m-draw-draw2d-readonlyspan-vector2-float-bool-float) at the point of the frame where the liquid should sit between the world behind it and what is in front. The cost is three full-layer passes (blur in two, threshold in one) plus one draw per particle, so on small screens it is cheap and on large ones [`FluidSurfaceStyle.Blur`](FluidSurfaceStyle.md#m-blur) and a smaller layer [`Layer2D.Viewport`](../../OneGame.Render2D/OneGame.Render2D/Layer2D.md#m-viewport) are the levers.

## Constructors

| Member | Description |
| --- | --- |
| [`FluidSurface2D(string, FluidSurfaceStyle?)`](#m-ctor-string-fluidsurfacestyle) | Creates a surface that draws on the layer with this name. |

### <a id="m-ctor-string-fluidsurfacestyle"></a>FluidSurface2D(string, FluidSurfaceStyle?)

```csharp
public FluidSurface2D(string layerName = "fluid", FluidSurfaceStyle? style = null);
```

Creates a surface that draws on the layer with this name.

**Parameters**

- `layerName` (`string`): The layer's name. A layer of this name is made the first time [`FluidSurface2D.Draw()`](FluidSurface2D.md#m-draw-draw2d-readonlyspan-vector2-float-bool-float) runs.
- `style` ([`FluidSurfaceStyle`](FluidSurfaceStyle.md)?): How it looks; null for the defaults. The style can be changed later through [`FluidSurface2D.Style`](FluidSurface2D.md#m-style).

## Properties

| Member | Description |
| --- | --- |
| [`Layer`](#m-layer) | The layer the surface draws on, once [`FluidSurface2D.Draw()`](FluidSurface2D.md#m-draw-draw2d-readonlyspan-vector2-float-bool-float) has run; set its [`Layer2D.Camera`](../../OneGame.Render2D/OneGame.Render2D/Layer2D.md#m-camera) or [`Layer2D.Viewport`](../../OneGame.Render2D/OneGame.Render2D/Layer2D.md#m-viewport) if the liquid needs its own view. |
| [`Style`](#m-style) | The look; change a value any time, it applies to the next frame drawn. |

### <a id="m-layer"></a>Layer

```csharp
public Layer2D? Layer { get; }
```

The layer the surface draws on, once [`FluidSurface2D.Draw()`](FluidSurface2D.md#m-draw-draw2d-readonlyspan-vector2-float-bool-float) has run; set its [`Layer2D.Camera`](../../OneGame.Render2D/OneGame.Render2D/Layer2D.md#m-camera) or [`Layer2D.Viewport`](../../OneGame.Render2D/OneGame.Render2D/Layer2D.md#m-viewport) if the liquid needs its own view.

**Type:** [`Layer2D`](../../OneGame.Render2D/OneGame.Render2D/Layer2D.md)?

### <a id="m-style"></a>Style

```csharp
public FluidSurfaceStyle Style { get; }
```

The look; change a value any time, it applies to the next frame drawn.

**Type:** [`FluidSurfaceStyle`](FluidSurfaceStyle.md)

## Methods

| Member | Description |
| --- | --- |
| [`Dispose()`](#m-dispose) | Releases the GPU objects of the surface's post chain. |
| [`Draw(Draw2D, ReadOnlySpan<Vector2>, float, bool, float)`](#m-draw-draw2d-readonlyspan-vector2-float-bool-float) | Draws the particles as one surface. |

### <a id="m-dispose"></a>Dispose()

```csharp
public void Dispose();
```

Releases the GPU objects of the surface's post chain. The layer stays in the scene and draws unprocessed if drawn to again.

### <a id="m-draw-draw2d-readonlyspan-vector2-float-bool-float"></a>Draw(Draw2D, ReadOnlySpan&lt;Vector2&gt;, float, bool, float)

```csharp
public void Draw(Draw2D d, ReadOnlySpan<Vector2> points, float unit = 1f, bool flipY = true, float z = 0f);
```

Draws the particles as one surface.

**Parameters**

- `d` ([`Draw2D`](Draw2D.md)): The frame's drawing surface, from a view.
- `points` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)&lt;[`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)&gt;): The particle positions (for example from `PhysicsSpace2D.GetFluidPositions`), in sim units with y up when `flipY` is true.
- `unit` (`float`): Layer units per sim unit (pixels per metre when the layer is in pixels). Default 1.
- `flipY` (`bool`): True to draw the sim's y-up positions on a y-down layer.
- `z` (`float`): Draw order of the splats inside the surface's layer.

**Exceptions**

- [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception): Called when no frame is being rendered.
