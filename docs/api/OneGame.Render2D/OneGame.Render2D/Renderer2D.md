[API reference](../../index.md) › [OneGame.Render2D](../index.md) › OneGame.Render2D

# Renderer2D class

**Namespace:** `OneGame.Render2D`  
**Package:** [`RecursiveAI.OneGame.Render2D`](../index.md)  
**Implements:** [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class Renderer2D : IDisposable
```

The 2D renderer: sprites, tilemaps, parallax layers, custom materials, post-processing and the UI display list, drawn through the GPU layer. A frame is built with [`Renderer2D.BeginFrame()`](Renderer2D.md#m-beginframe-gputexture-frameoptions), any number of draw calls, and [`Renderer2D.EndFrame()`](Renderer2D.md#m-endframe-gpucommandencoder), which uploads everything once and records the passes into an encoder the caller then submits. Colours are sRGB in the API; blending is linear and premultiplied. See the renderer's interface document for the full contract.

## Properties

| Member | Description |
| --- | --- |
| [`AdditiveMaterial`](#m-additivematerial) | A built-in material that adds a sprite's colour to what is already on the target instead of blending it over: glows, sparks and fire. |
| [`Device`](#m-device) | The GPU device. |
| [`FrameNumber`](#m-framenumber) | The number of the frame being recorded; atlases take it as their clock, so every user of an atlas in a frame must pass the same one. |
| [`LastFrame`](#m-lastframe) | Stats of the last frame ended. |
| [`TargetFormat`](#m-targetformat) | The format of the targets this renderer draws into. |
| [`Ui`](#m-ui) | The UI drawing surface for the current frame. |

### <a id="m-additivematerial"></a>AdditiveMaterial

```csharp
public Material AdditiveMaterial { get; }
```

A built-in material that adds a sprite's colour to what is already on the target instead of blending it over: glows, sparks and fire. Set it as [`Sprite.Material`](Sprite.md#m-material). The sprite's alpha scales its brightness; the colour is premultiplied as for any sprite.

**Type:** [`Material`](Material.md)

### <a id="m-device"></a>Device

```csharp
public GpuDevice Device { get; }
```

The GPU device.

**Type:** [`GpuDevice`](../../OneGame.Gpu/OneGame.Gpu/GpuDevice.md)

### <a id="m-framenumber"></a>FrameNumber

```csharp
public int FrameNumber { get; }
```

The number of the frame being recorded; atlases take it as their clock, so every user of an atlas in a frame must pass the same one.

**Type:** `int`

### <a id="m-lastframe"></a>LastFrame

```csharp
public FrameStats LastFrame { get; }
```

Stats of the last frame ended.

**Type:** [`FrameStats`](FrameStats.md)

### <a id="m-targetformat"></a>TargetFormat

```csharp
public GpuFormat TargetFormat { get; }
```

The format of the targets this renderer draws into.

**Type:** [`GpuFormat`](../../OneGame.Gpu/OneGame.Gpu/GpuFormat.md)

### <a id="m-ui"></a>Ui

```csharp
public UiCanvas Ui { get; }
```

The UI drawing surface for the current frame.

**Type:** [`UiCanvas`](UiCanvas.md)

## Methods

| Member | Description |
| --- | --- |
| [`BeginFrame(GpuTexture, in FrameOptions)`](#m-beginframe-gputexture-frameoptions) | Starts a frame that draws into `target`. |
| [`Create(GpuDevice, GpuFormat, IShaderSource)`](#m-create-gpudevice-gpuformat-ishadersource) | Makes a renderer that draws into targets of `targetFormat`. |
| [`CreateMaterial(ShaderCode, string?)`](#m-creatematerial-shadercode-string) | Creates a material from a custom pixel shader that draws sprites. |
| [`CreatePostChain()`](#m-createpostchain) | Makes an empty post-processing chain. |
| [`CreateTexture(int, int, ReadOnlySpan<byte>, TextureFilter)`](#m-createtexture-int-int-readonlyspan-byte-texturefilter) | Creates a texture from straight-alpha sRGB pixels (R, G, B, A bytes, top row first). |
| [`CreateTileSet(int, int, int, int, ReadOnlySpan<byte>, TextureFilter)`](#m-createtileset-int-int-int-int-readonlyspan-byte-texturefilter) | Creates a tile set from a grid of tiles. |
| [`Dispose()`](#m-dispose) | Releases every GPU object the renderer made. |
| [`DrawScene(Scene2D, in Camera2D, PostChain?)`](#m-drawscene-scene2d-camera2d-postchain) | Draws a scene's layers back to front through `camera`. |
| [`EndFrame(GpuCommandEncoder)`](#m-endframe-gpucommandencoder) | Uploads everything the frame drew and records its passes into `encoder`. |
| [`ForgetTarget(GpuTexture)`](#m-forgettarget-gputexture) | Forgets what the renderer keeps for a target texture (its constants and bind group). |
| [`ReplaceMaterialShader(Material, ShaderCode)`](#m-replacematerialshader-material-shadercode) | Replaces the pixel shader of a material made by [`Renderer2D.CreateMaterial()`](Renderer2D.md#m-creatematerial-shadercode-string) without touching its parameters, so a shader edited while the game runs shows from the next frame on. |
| [`ReplaceTexture(Texture2D, int, int, ReadOnlySpan<byte>, TextureFilter)`](#m-replacetexture-texture2d-int-int-readonlyspan-byte-texturefilter) | Replaces the pixels of a texture made by [`Renderer2D.CreateTexture()`](Renderer2D.md#m-createtexture-int-int-readonlyspan-byte-texturefilter), keeping the same [`Texture2D`](Texture2D.md) object so everything that holds it draws the new image from the next frame on. |

### <a id="m-beginframe-gputexture-frameoptions"></a>BeginFrame(GpuTexture, in FrameOptions)

```csharp
public void BeginFrame(GpuTexture target, in FrameOptions options = default);
```

Starts a frame that draws into `target`. The target needs `RenderTarget` usage and the renderer's format.

**Parameters**

- `target` ([`GpuTexture`](../../OneGame.Gpu/OneGame.Gpu/GpuTexture.md)): The texture to draw into: an offscreen texture or a swapchain image.
- `options` ([`FrameOptions`](FrameOptions.md)): Per-frame settings.

### <a id="m-create-gpudevice-gpuformat-ishadersource"></a>Create(GpuDevice, GpuFormat, IShaderSource)

```csharp
public static Renderer2D Create(GpuDevice device, GpuFormat targetFormat, IShaderSource shaders);
```

Makes a renderer that draws into targets of `targetFormat`.

**Parameters**

- `device` ([`GpuDevice`](../../OneGame.Gpu/OneGame.Gpu/GpuDevice.md)): The GPU device. It must outlive the renderer.
- `targetFormat` ([`GpuFormat`](../../OneGame.Gpu/OneGame.Gpu/GpuFormat.md)): An sRGB colour format (`Rgba8UnormSrgb` or `Bgra8UnormSrgb`) or `Rgba16Float`: the shaders output linear light and rely on the target to encode it.
- `shaders` ([`IShaderSource`](IShaderSource.md)): The compiled built-in shaders.

**Returns:** [`Renderer2D`](Renderer2D.md) — The renderer.

### <a id="m-creatematerial-shadercode-string"></a>CreateMaterial(ShaderCode, string?)

```csharp
public Material CreateMaterial(ShaderCode pixelShader, string? label = null);
```

Creates a material from a custom pixel shader that draws sprites. See the renderer's interface document for what the shader receives.

**Parameters**

- `pixelShader` ([`ShaderCode`](ShaderCode.md)): The compiled pixel stage.
- `label` (`string?`): A name for debugging.

**Returns:** [`Material`](Material.md) — The material.

### <a id="m-createpostchain"></a>CreatePostChain()

```csharp
public PostChain CreatePostChain();
```

Makes an empty post-processing chain. Add effects, then pass it to [`Renderer2D.DrawScene()`](Renderer2D.md#m-drawscene-scene2d-camera2d-postchain).

**Returns:** [`PostChain`](PostChain.md) — The chain.

### <a id="m-createtexture-int-int-readonlyspan-byte-texturefilter"></a>CreateTexture(int, int, ReadOnlySpan&lt;byte&gt;, TextureFilter)

```csharp
public Texture2D CreateTexture(int width, int height, ReadOnlySpan<byte> rgba, TextureFilter filter = 1);
```

Creates a texture from straight-alpha sRGB pixels (R, G, B, A bytes, top row first). The renderer premultiplies in linear light.

**Parameters**

- `width` (`int`): Width in pixels.
- `height` (`int`): Height in pixels.
- `rgba` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`): Exactly `width * height * 4` bytes.
- `filter` ([`TextureFilter`](TextureFilter.md)): How the texture is filtered when scaled.

**Returns:** [`Texture2D`](Texture2D.md) — The texture.

### <a id="m-createtileset-int-int-int-int-readonlyspan-byte-texturefilter"></a>CreateTileSet(int, int, int, int, ReadOnlySpan&lt;byte&gt;, TextureFilter)

```csharp
public TileSet CreateTileSet(int tileWidth, int tileHeight, int columns, int rows, ReadOnlySpan<byte> rgba, TextureFilter filter = 0);
```

Creates a tile set from a grid of tiles.

**Parameters**

- `tileWidth` (`int`): Width of one tile in texels.
- `tileHeight` (`int`): Height of one tile in texels.
- `columns` (`int`): Tiles per row.
- `rows` (`int`): Rows of tiles.
- `rgba` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`): Straight-alpha sRGB pixels of the whole grid, `columns * tileWidth` by `rows * tileHeight`.
- `filter` ([`TextureFilter`](TextureFilter.md)): Filtering; use [`TextureFilter.Nearest`](TextureFilter.md#m-nearest) for pixel art.

**Returns:** [`TileSet`](TileSet.md) — The tile set.

### <a id="m-dispose"></a>Dispose()

```csharp
public void Dispose();
```

Releases every GPU object the renderer made. Dispose the textures, tile sets and materials you created yourself first or after; they are independent.

### <a id="m-drawscene-scene2d-camera2d-postchain"></a>DrawScene(Scene2D, in Camera2D, PostChain?)

```csharp
public void DrawScene(Scene2D scene, in Camera2D camera, PostChain? post = null);
```

Draws a scene's layers back to front through `camera`. With a post chain the scene goes into an offscreen image first, the chain runs over it, and the result lands on the current target; anything drawn after (usually the UI) is not post-processed.

**Parameters**

- `scene` ([`Scene2D`](Scene2D.md)): The layers, sprites and tilemaps.
- `camera` ([`Camera2D`](Camera2D.md)): The view.
- `post` ([`PostChain`](PostChain.md)?): Effects to apply to the finished scene, or null.

### <a id="m-endframe-gpucommandencoder"></a>EndFrame(GpuCommandEncoder)

```csharp
public FrameStats EndFrame(GpuCommandEncoder encoder);
```

Uploads everything the frame drew and records its passes into `encoder`. Submit the encoder afterwards.

**Parameters**

- `encoder` ([`GpuCommandEncoder`](../../OneGame.Gpu/OneGame.Gpu/GpuCommandEncoder.md)): The command encoder to record into.

**Returns:** [`FrameStats`](FrameStats.md) — What the frame cost.

### <a id="m-forgettarget-gputexture"></a>ForgetTarget(GpuTexture)

```csharp
public void ForgetTarget(GpuTexture texture);
```

Forgets what the renderer keeps for a target texture (its constants and bind group). Call it when a target is destroyed, for example on swapchain resize.

**Parameters**

- `texture` ([`GpuTexture`](../../OneGame.Gpu/OneGame.Gpu/GpuTexture.md)): The target.

### <a id="m-replacematerialshader-material-shadercode"></a>ReplaceMaterialShader(Material, ShaderCode)

```csharp
public void ReplaceMaterialShader(Material material, ShaderCode pixelShader);
```

Replaces the pixel shader of a material made by [`Renderer2D.CreateMaterial()`](Renderer2D.md#m-creatematerial-shadercode-string) without touching its parameters, so a shader edited while the game runs shows from the next frame on. The new shader must keep the interface the material was made with (same resources and vertex inputs); it is the caller's job to check that, for example with `InterfaceCompatibility.Compare`.

**Parameters**

- `material` ([`Material`](Material.md)): A material from [`Renderer2D.CreateMaterial()`](Renderer2D.md#m-creatematerial-shadercode-string).
- `pixelShader` ([`ShaderCode`](ShaderCode.md)): The new compiled pixel stage.

**Exceptions**

- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): The shader is not a pixel shader, or the material is one of the renderer's own.

### <a id="m-replacetexture-texture2d-int-int-readonlyspan-byte-texturefilter"></a>ReplaceTexture(Texture2D, int, int, ReadOnlySpan&lt;byte&gt;, TextureFilter)

```csharp
public void ReplaceTexture(Texture2D texture, int width, int height, ReadOnlySpan<byte> rgba, TextureFilter filter = 1);
```

Replaces the pixels of a texture made by [`Renderer2D.CreateTexture()`](Renderer2D.md#m-createtexture-int-int-readonlyspan-byte-texturefilter), keeping the same [`Texture2D`](Texture2D.md) object so everything that holds it draws the new image from the next frame on. The size may change. Waits for the device, so it is meant for development reloads and loading screens, not for every frame.

**Parameters**

- `texture` ([`Texture2D`](Texture2D.md)): A texture made by this renderer's `CreateTexture`.
- `width` (`int`): New width in pixels.
- `height` (`int`): New height in pixels.
- `rgba` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`): Exactly `width * height * 4` straight-alpha sRGB bytes.
- `filter` ([`TextureFilter`](TextureFilter.md)): How the texture is filtered when scaled.
