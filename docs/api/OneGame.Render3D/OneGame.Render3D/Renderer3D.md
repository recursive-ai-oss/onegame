[API reference](../../index.md) › [OneGame.Render3D](../index.md) › OneGame.Render3D

# Renderer3D class

**Namespace:** `OneGame.Render3D`  
**Package:** [`RecursiveAI.OneGame.Render3D`](../index.md)  
**Implements:** [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class Renderer3D : IDisposable
```

The physically based 3D renderer. It uploads glTF models ([`Renderer3D.Upload()`](Renderer3D.md#m-upload-gltfmodel-int)) and adds a frame to a render graph ([`Renderer3D.Render()`](Renderer3D.md#m-render-rendergraph-scene3d-graphtexture-int-int-gpuformat)): cluster light assignment, one forward pass for opaque, masked and blended surfaces plus the backdrop, a bloom chain, and tone mapping into the target. See `docs/interfaces/render3d.md`.

## Constructors

| Member | Description |
| --- | --- |
| [`Renderer3D(GpuDevice)`](#m-ctor-gpudevice) | Creates a renderer on a device. |

### <a id="m-ctor-gpudevice"></a>Renderer3D(GpuDevice)

```csharp
public Renderer3D(GpuDevice device);
```

Creates a renderer on a device.

**Parameters**

- `device` ([`GpuDevice`](../../OneGame.Gpu/OneGame.Gpu/GpuDevice.md)): The graphics device. It must outlive the renderer.

## Fields

| Member | Description |
| --- | --- |
| [`DepthFormat`](#m-depthformat) | Format of the depth buffer. |
| [`DepthSlices`](#m-depthslices) | Depth slices of the light grid. |
| [`HdrFormat`](#m-hdrformat) | Format of the HDR image the scene is lit into. |
| [`MaxLights`](#m-maxlights) | Most punctual lights one frame uses; extra lights are ignored. |
| [`MaxLightsPerCluster`](#m-maxlightspercluster) | Most lights one cluster can hold; the rest are dropped. |
| [`MaxShadowLights`](#m-maxshadowlights) | Most lights that cast shadows in one frame. |
| [`MaxShadowTiles`](#m-maxshadowtiles) | Most shadow map tiles (cascades, spot maps, point light faces) in one frame. |
| [`TileSize`](#m-tilesize) | Screen tile size of the light grid, in pixels. |

### <a id="m-depthformat"></a>DepthFormat

```csharp
public const GpuFormat DepthFormat = 13;
```

Format of the depth buffer.

**Type:** [`GpuFormat`](../../OneGame.Gpu/OneGame.Gpu/GpuFormat.md)

### <a id="m-depthslices"></a>DepthSlices

```csharp
public const int DepthSlices = 24;
```

Depth slices of the light grid.

**Type:** `int`

### <a id="m-hdrformat"></a>HdrFormat

```csharp
public const GpuFormat HdrFormat = 8;
```

Format of the HDR image the scene is lit into.

**Type:** [`GpuFormat`](../../OneGame.Gpu/OneGame.Gpu/GpuFormat.md)

### <a id="m-maxlights"></a>MaxLights

```csharp
public const int MaxLights = 512;
```

Most punctual lights one frame uses; extra lights are ignored.

**Type:** `int`

### <a id="m-maxlightspercluster"></a>MaxLightsPerCluster

```csharp
public const int MaxLightsPerCluster = 63;
```

Most lights one cluster can hold; the rest are dropped.

**Type:** `int`

### <a id="m-maxshadowlights"></a>MaxShadowLights

```csharp
public const int MaxShadowLights = 8;
```

Most lights that cast shadows in one frame.

**Type:** `int`

### <a id="m-maxshadowtiles"></a>MaxShadowTiles

```csharp
public const int MaxShadowTiles = 16;
```

Most shadow map tiles (cascades, spot maps, point light faces) in one frame.

**Type:** `int`

### <a id="m-tilesize"></a>TileSize

```csharp
public const int TileSize = 64;
```

Screen tile size of the light grid, in pixels.

**Type:** `int`

## Properties

| Member | Description |
| --- | --- |
| [`BrdfLut`](#m-brdflut) | The split-sum environment BRDF table (`Rgba16Float`, the first two channels; n.v across, roughness down); filled on first use. |
| [`Device`](#m-device) | The device this renderer draws with. |
| [`LastStats`](#m-laststats) | What the last [`Renderer3D.Render()`](Renderer3D.md#m-render-rendergraph-scene3d-graphtexture-int-int-gpuformat) call prepared. |
| [`SoftSpot`](#m-softspot) | A round white spot that fades smoothly to nothing at its edge: what billboards show when their batch has no texture. |

### <a id="m-brdflut"></a>BrdfLut

```csharp
public GpuTexture BrdfLut { get; }
```

The split-sum environment BRDF table (`Rgba16Float`, the first two channels; n.v across, roughness down); filled on first use.

**Type:** [`GpuTexture`](../../OneGame.Gpu/OneGame.Gpu/GpuTexture.md)

### <a id="m-device"></a>Device

```csharp
public GpuDevice Device { get; }
```

The device this renderer draws with.

**Type:** [`GpuDevice`](../../OneGame.Gpu/OneGame.Gpu/GpuDevice.md)

### <a id="m-laststats"></a>LastStats

```csharp
public FrameStats3D LastStats { get; }
```

What the last [`Renderer3D.Render()`](Renderer3D.md#m-render-rendergraph-scene3d-graphtexture-int-int-gpuformat) call prepared.

**Type:** [`FrameStats3D`](FrameStats3D.md)

### <a id="m-softspot"></a>SoftSpot

```csharp
public BillboardTexture3D SoftSpot { get; }
```

A round white spot that fades smoothly to nothing at its edge: what billboards show when their batch has no texture.

**Type:** [`BillboardTexture3D`](BillboardTexture3D.md)

## Methods

| Member | Description |
| --- | --- |
| [`CreateBillboardTexture(int, int, ReadOnlySpan<byte>, string?)`](#m-createbillboardtexture-int-int-readonlyspan-byte-string) | Uploads an image for billboards. |
| [`CreateEnvironment(HdrImage, int)`](#m-createenvironment-hdrimage-int) | Makes an environment from an equirectangular panorama (see [`HdrImage`](HdrImage.md) for the orientation). |
| [`Dispose()`](#m-dispose) | Releases everything the renderer created. |
| [`PrepareEnvironment(EnvironmentMap)`](#m-prepareenvironment-environmentmap) | Runs the filtering of an environment now, on its own graph, and waits for it. |
| [`Render(RenderGraph, Scene3D, GraphTexture, int, int, GpuFormat)`](#m-render-rendergraph-scene3d-graphtexture-int-int-gpuformat) | Adds this frame's passes to a render graph that has begun a frame: shadow maps, the depth prepass and ambient occlusion when the scene asks for them, light clustering, the forward pass, bloom and tone mapping into `target`. |
| [`Upload(GltfModel, int)`](#m-upload-gltfmodel-int) | Uploads a glTF model: vertex and index buffers, materials, and textures with generated mip levels (colour textures as sRGB). |

### <a id="m-createbillboardtexture-int-int-readonlyspan-byte-string"></a>CreateBillboardTexture(int, int, ReadOnlySpan&lt;byte&gt;, string?)

```csharp
public BillboardTexture3D CreateBillboardTexture(int width, int height, ReadOnlySpan<byte> rgba, string? label = null);
```

Uploads an image for billboards.

**Parameters**

- `width` (`int`): Width in pixels.
- `height` (`int`): Height in pixels.
- `rgba` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`): Straight-alpha sRGB bytes (R, G, B, A), top row first. They are premultiplied in linear light here, so filtering and blending are exact.
- `label` (`string?`): A name for debugging.

**Returns:** [`BillboardTexture3D`](BillboardTexture3D.md) — The texture. Dispose it before the renderer.

**Exceptions**

- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): The sizes are not positive or the data is not `width * height * 4` bytes.

### <a id="m-createenvironment-hdrimage-int"></a>CreateEnvironment(HdrImage, int)

```csharp
public EnvironmentMap CreateEnvironment(HdrImage image, int size = 0);
```

Makes an environment from an equirectangular panorama (see [`HdrImage`](HdrImage.md) for the orientation). The cubes are allocated now and filled by compute passes the first time a frame uses the environment, or by [`Renderer3D.PrepareEnvironment()`](Renderer3D.md#m-prepareenvironment-environmentmap).

**Parameters**

- `image` ([`HdrImage`](HdrImage.md)): The panorama, linear radiance.
- `size` (`int`): Face size of the specular cube, a power of two from 16 to 1024; 0 (the default) follows the scene's quality level.

**Returns:** [`EnvironmentMap`](EnvironmentMap.md) — The environment. Dispose it before the renderer.

### <a id="m-dispose"></a>Dispose()

```csharp
public void Dispose();
```

Releases everything the renderer created. Models and environments are disposed separately, before this.

### <a id="m-prepareenvironment-environmentmap"></a>PrepareEnvironment(EnvironmentMap)

```csharp
public void PrepareEnvironment(EnvironmentMap environment);
```

Runs the filtering of an environment now, on its own graph, and waits for it. Frames do this by themselves; tests and tools use it to inspect the cubes.

**Parameters**

- `environment` ([`EnvironmentMap`](EnvironmentMap.md)): The environment to filter.

### <a id="m-render-rendergraph-scene3d-graphtexture-int-int-gpuformat"></a>Render(RenderGraph, Scene3D, GraphTexture, int, int, GpuFormat)

```csharp
public void Render(RenderGraph graph, Scene3D scene, GraphTexture target, int width, int height, GpuFormat targetFormat);
```

Adds this frame's passes to a render graph that has begun a frame: shadow maps, the depth prepass and ambient occlusion when the scene asks for them, light clustering, the forward pass, bloom and tone mapping into `target`. The target's earlier contents are replaced; a later pass that draws over it (the 2D renderer) must declare `ReadWrite`, not `Write`, or the graph drops this work as overwritten.

**Parameters**

- `graph` ([`RenderGraph`](../../OneGame.Gpu/OneGame.Gpu.Graph/RenderGraph.md)): The graph, after `BeginFrame`.
- `scene` ([`Scene3D`](Scene3D.md)): What to draw.
- `target` ([`GraphTexture`](../../OneGame.Gpu/OneGame.Gpu.Graph/GraphTexture.md)): The image to draw into: an sRGB 8-bit, 8-bit UNorm or 16-bit float colour target of the given size.
- `width` (`int`): Target width in pixels.
- `height` (`int`): Target height in pixels.
- `targetFormat` ([`GpuFormat`](../../OneGame.Gpu/OneGame.Gpu/GpuFormat.md)): Format of the target texture.

### <a id="m-upload-gltfmodel-int"></a>Upload(GltfModel, int)

```csharp
public Model3D Upload(GltfModel model, int scene = -1);
```

Uploads a glTF model: vertex and index buffers, materials, and textures with generated mip levels (colour textures as sRGB).

**Parameters**

- `model` ([`GltfModel`](../../OneGame.Gltf/OneGame.Gltf/GltfModel.md)): The imported model.
- `scene` (`int`): Scene to draw; -1 for the model's default scene.

**Returns:** [`Model3D`](Model3D.md) — The GPU model. Dispose it before the renderer.
