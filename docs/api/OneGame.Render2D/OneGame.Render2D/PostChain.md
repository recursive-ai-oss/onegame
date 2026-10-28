[API reference](../../index.md) › [OneGame.Render2D](../index.md) › OneGame.Render2D

# PostChain class

**Namespace:** `OneGame.Render2D`  
**Package:** [`RecursiveAI.OneGame.Render2D`](../index.md)  
**Implements:** [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class PostChain : IDisposable
```

A list of full-screen effects run, in order, over a finished scene. Make one with [`Renderer2D.CreatePostChain()`](Renderer2D.md#m-createpostchain), add effects, and pass it to [`Renderer2D.DrawScene()`](Renderer2D.md#m-drawscene-scene2d-camera2d-postchain). The effects are ordinary materials with built-in shaders, so a chain costs one full-screen pass per effect.

## Properties

| Member | Description |
| --- | --- |
| [`PassCount`](#m-passcount) | The number of full-screen passes. |

### <a id="m-passcount"></a>PassCount

```csharp
public int PassCount { get; }
```

The number of full-screen passes.

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`AddBlur(float)`](#m-addblur-float) | Adds a separable Gaussian-like blur (a horizontal and a vertical pass). |
| [`AddGrade(float, float, float, float, Color?, float)`](#m-addgrade-float-float-float-float-color-float) | Adds a colour grade. |
| [`Dispose()`](#m-dispose) | Releases the effects' GPU objects. |

### <a id="m-addblur-float"></a>AddBlur(float)

```csharp
public PostChain AddBlur(float radius);
```

Adds a separable Gaussian-like blur (a horizontal and a vertical pass).

**Parameters**

- `radius` (`float`): Spacing of the blur's taps in pixels; the visible softening reaches about three times this far.

**Returns:** [`PostChain`](PostChain.md) — This chain.

### <a id="m-addgrade-float-float-float-float-color-float"></a>AddGrade(float, float, float, float, Color?, float)

```csharp
public PostChain AddGrade(float exposure = 0f, float contrast = 1f, float saturation = 1f, float vignette = 0f, Color? tint = default, float tintAmount = 0f);
```

Adds a colour grade.

**Parameters**

- `exposure` (`float`): Brightness change in stops (0 leaves it, +1 doubles the light).
- `contrast` (`float`): 1 leaves contrast alone, above 1 increases it.
- `saturation` (`float`): 1 leaves colour, 0 is grey, above 1 is more vivid.
- `vignette` (`float`): 0 for none; 1 darkens the corners fully.
- `tint` ([`Color`](../../OneGame.UI/OneGame.UI/Color.md)?): A colour to multiply in, with `tintAmount`; sRGB.
- `tintAmount` (`float`): 0 for no tint, 1 for the full tint.

**Returns:** [`PostChain`](PostChain.md) — This chain.

### <a id="m-dispose"></a>Dispose()

```csharp
public void Dispose();
```

Releases the effects' GPU objects.
