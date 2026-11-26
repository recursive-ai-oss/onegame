[API reference](../../index.md) › [OneGame.Render2D](../index.md) › OneGame.Render2D

# PostChain class

**Namespace:** `OneGame.Render2D`  
**Package:** [`RecursiveAI.OneGame.Render2D`](../index.md)  
**Implements:** [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class PostChain : IDisposable
```

A list of full-screen effects run, in order, over a finished image: the whole scene ([`Renderer2D.DrawScene()`](Renderer2D.md#m-drawscene-scene2d-camera2d-postchain-iscenemarkersink)) or one layer ([`Layer2D.Post`](Layer2D.md#m-post)). Make one with [`Renderer2D.CreatePostChain()`](Renderer2D.md#m-createpostchain), add effects, and hand it over. A chain with no effects costs nothing; each effect costs one full-screen pass (blur is two, bloom is several at reduced size). A chain keeps GPU objects, so make it once and keep it.

## Properties

| Member | Description |
| --- | --- |
| [`PassCount`](#m-passcount) | The number of effects added (a blur or a bloom counts as one). |

### <a id="m-passcount"></a>PassCount

```csharp
public int PassCount { get; }
```

The number of effects added (a blur or a bloom counts as one).

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`AddBloom(float, float, float, float, int, int)`](#m-addbloom-float-float-float-float-int-int) | Adds bloom: the parts of the image brighter than a threshold are blurred and added back, so bright things glow. |
| [`AddBlur(float)`](#m-addblur-float) | Adds a separable Gaussian-like blur (a horizontal and a vertical pass). |
| [`AddFluidSurface(float, float, Color, Color, float, float)`](#m-addfluidsurface-float-float-color-color-float-float) | Adds a liquid surface: the image's alpha is read as a field (particles drawn as soft discs, then blurred) and cut at a level, so separate discs merge into one smooth body with a lighter or darker rim. |
| [`AddGrade(float, float, float, float, Color?, float)`](#m-addgrade-float-float-float-float-color-float) | Adds a colour grade. |
| [`AddMaterial(Material)`](#m-addmaterial-material) | Adds a pass of your own: a pixel shader (HLSL compiled with the game's shaders, made into a [`Material`](Material.md) with [`Renderer2D.CreateMaterial()`](Renderer2D.md#m-creatematerial-shadercode-string)) that runs over the whole image. |
| [`Dispose()`](#m-dispose) | Releases the effects' GPU objects (not the materials passed to [`PostChain.AddMaterial()`](PostChain.md#m-addmaterial-material)). |
| [`UpdateFluidSurface(float, float, Color, Color, float, float)`](#m-updatefluidsurface-float-float-color-color-float-float) | Changes the settings of the liquid surface pass added with [`PostChain.AddFluidSurface()`](PostChain.md#m-addfluidsurface-float-float-color-color-float-float); they apply to the next frame drawn. |

### <a id="m-addbloom-float-float-float-float-int-int"></a>AddBloom(float, float, float, float, int, int)

```csharp
public PostChain AddBloom(float threshold = 0.6f, float intensity = 1f, float radius = 1f, float softness = 0.3f, int blurPasses = 3, int downscale = 2);
```

Adds bloom: the parts of the image brighter than a threshold are blurred and added back, so bright things glow. It works in linear light on an image at reduced size, and adds the glow to what is there instead of replacing it.

**Parameters**

- `threshold` (`float`): Brightness (linear light, 0 to 1) above which a pixel contributes; the brightest colour channel decides.
- `intensity` (`float`): How strongly the glow is added; 1 adds it as found. Above about 1 the glow clips in an 8-bit renderer.
- `radius` (`float`): Spacing of the blur's taps, in pixels of the reduced image; the glow reaches several times this far per blur pass.
- `softness` (`float`): Width of the threshold's ramp in linear brightness; larger values include dimmer pixels gently.
- `blurPasses` (`int`): How many times the image is blurred (each is a horizontal and a vertical pass); more is wider and smoother.
- `downscale` (`int`): The factor the glow image is smaller than the source by (1 = full size, 2 = half, 4 = quarter).

**Returns:** [`PostChain`](PostChain.md) — This chain.

### <a id="m-addblur-float"></a>AddBlur(float)

```csharp
public PostChain AddBlur(float radius);
```

Adds a separable Gaussian-like blur (a horizontal and a vertical pass).

**Parameters**

- `radius` (`float`): Spacing of the blur's taps in pixels; the visible softening reaches about three times this far.

**Returns:** [`PostChain`](PostChain.md) — This chain.

### <a id="m-addfluidsurface-float-float-color-color-float-float"></a>AddFluidSurface(float, float, Color, Color, float, float)

```csharp
public PostChain AddFluidSurface(float threshold, float softness, Color fill, Color rim, float rimWidth = 0.2f, float rimStrength = 1f);
```

Adds a liquid surface: the image's alpha is read as a field (particles drawn as soft discs, then blurred) and cut at a level, so separate discs merge into one smooth body with a lighter or darker rim. Put it after [`PostChain.AddBlur()`](PostChain.md#m-addblur-float); `OneGame.Host.FluidSurface2D` sets up the whole chain. The pass replaces the image's colour with `fill` and `rim`.

**Parameters**

- `threshold` (`float`): The field level where the surface sits, 0 to 1. Lower grows the body and bridges larger gaps; higher shrinks it.
- `softness` (`float`): Half width of the outline's blend, in field units (about 0.04 keeps it smooth without blurring).
- `fill` ([`Color`](../../OneGame.UI/OneGame.UI/Color.md)): The body colour (sRGB, straight alpha).
- `rim` ([`Color`](../../OneGame.UI/OneGame.UI/Color.md)): The colour of the band just inside the outline (sRGB, straight alpha).
- `rimWidth` (`float`): How far inside the outline the rim reaches, in field units.
- `rimStrength` (`float`): 0 for no rim, 1 for the full rim colour at the outline.

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

### <a id="m-addmaterial-material"></a>AddMaterial(Material)

```csharp
public PostChain AddMaterial(Material material);
```

Adds a pass of your own: a pixel shader (HLSL compiled with the game's shaders, made into a [`Material`](Material.md) with [`Renderer2D.CreateMaterial()`](Renderer2D.md#m-creatematerial-shadercode-string)) that runs over the whole image. It reads the image so far as `Tex`/`Samp` (set 1), its parameters as `P[0..7]` (set 2) and the size of the image being drawn as `Viewport` (set 0), and returns premultiplied colour. Change its parameters with [`Material.SetParameter()`](Material.md#m-setparameter-int-vector4) any time; they apply to the next frame drawn. The chain does not own the material: dispose it yourself after the chain.

**Parameters**

- `material` ([`Material`](Material.md)): A material whose shader reads the previous image.

**Returns:** [`PostChain`](PostChain.md) — This chain.

### <a id="m-dispose"></a>Dispose()

```csharp
public void Dispose();
```

Releases the effects' GPU objects (not the materials passed to [`PostChain.AddMaterial()`](PostChain.md#m-addmaterial-material)).

### <a id="m-updatefluidsurface-float-float-color-color-float-float"></a>UpdateFluidSurface(float, float, Color, Color, float, float)

```csharp
public PostChain UpdateFluidSurface(float threshold, float softness, Color fill, Color rim, float rimWidth = 0.2f, float rimStrength = 1f);
```

Changes the settings of the liquid surface pass added with [`PostChain.AddFluidSurface()`](PostChain.md#m-addfluidsurface-float-float-color-color-float-float); they apply to the next frame drawn. Allocates nothing.

**Parameters**

- `threshold` (`float`): The field level where the surface sits, 0 to 1.
- `softness` (`float`): Half width of the outline's blend, in field units.
- `fill` ([`Color`](../../OneGame.UI/OneGame.UI/Color.md)): The body colour (sRGB, straight alpha).
- `rim` ([`Color`](../../OneGame.UI/OneGame.UI/Color.md)): The colour of the band just inside the outline (sRGB, straight alpha).
- `rimWidth` (`float`): How far inside the outline the rim reaches, in field units.
- `rimStrength` (`float`): 0 for no rim, 1 for the full rim colour at the outline.

**Returns:** [`PostChain`](PostChain.md) — This chain.

**Exceptions**

- [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception): The chain has no liquid surface pass.
