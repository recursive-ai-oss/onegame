[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# Render2DStage class

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)  
**Implements:** [`IRenderStage`](IRenderStage.md), [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public class Render2DStage : IRenderStage, IDisposable
```

The default render stage: the 2D renderer draws the UI display list into the host's target. The renderer is made on first use for the target's format (which must be an sRGB format). With no usable GPU the host falls back to [`DisplayListRaster`](DisplayListRaster.md).

## Constructors

| Member | Description |
| --- | --- |
| [`Render2DStage(HostResources)`](#m-ctor-hostresources) | Creates the stage over the game's fonts, art and shaders. |

### <a id="m-ctor-hostresources"></a>Render2DStage(HostResources)

```csharp
public Render2DStage(HostResources resources);
```

Creates the stage over the game's fonts, art and shaders.

**Parameters**

- `resources` ([`HostResources`](HostResources.md))

**Exceptions**

- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): The resources have no fonts or no shaders.

## Properties

| Member | Description |
| --- | --- |
| [`Draw`](#m-draw) | Immediate-mode 2D drawing. |
| [`Graph`](#m-graph) | The render graph the stage draws with, once it has drawn a frame; null for stages that do not use one. |
| [`HotShaders`](#m-hotshaders) | Where this stage registers materials whose shader files may be edited while the game runs. |
| [`Name`](#m-name) | A short name shown in harness output. |
| [`Overlay`](#m-overlay) | Drawn over everything else in each frame, captures included; null for none. |
| [`PageStats`](#m-pagestats) | What the page renderer drew in the last frame (the first format's renderer). |
| [`Pages`](#m-pages) | The page screens to draw (`OneGame.Pages`): host-scope layer [`PageScopes.Under`](../../OneGame.Pages/OneGame.Pages/PageScopes.md#m-under) before the 2D pass and the UI, everything else after them. |
| [`RequiresSrgb`](#m-requiressrgb) | True when the stage draws into sRGB targets (the host then makes its swapchain and capture textures sRGB). |

### <a id="m-draw"></a>Draw

```csharp
public Draw2D Draw { get; }
```

Immediate-mode 2D drawing. Every frame, before the UI, the stage asks each [`IView2D`](IView2D.md) entity of the world to draw into it (and then [`Draw2D.Describer`](Draw2D.md#m-describer), for entities that cannot reference the host), and shows the result under the UI.

**Type:** [`Draw2D`](Draw2D.md)

### <a id="m-graph"></a>Graph

```csharp
public RenderGraph? Graph { get; }
```

The render graph the stage draws with, once it has drawn a frame; null for stages that do not use one.

**Type:** [`RenderGraph`](../../OneGame.Gpu/OneGame.Gpu.Graph/RenderGraph.md)?

### <a id="m-hotshaders"></a>HotShaders

```csharp
protected HotShaders HotShaders { get; }
```

Where this stage registers materials whose shader files may be edited while the game runs.

**Type:** [`HotShaders`](HotShaders.md)

### <a id="m-name"></a>Name

```csharp
public virtual string Name { get; }
```

A short name shown in harness output.

**Type:** `string`

### <a id="m-overlay"></a>Overlay

```csharp
public IFrameOverlay? Overlay { get; set; }
```

Drawn over everything else in each frame, captures included; null for none. Development builds set it; a release build never does.

**Type:** [`IFrameOverlay`](IFrameOverlay.md)?

### <a id="m-pagestats"></a>PageStats

```csharp
public PageStats PageStats { get; }
```

What the page renderer drew in the last frame (the first format's renderer).

**Type:** [`PageStats`](../../OneGame.Pages/OneGame.Pages/PageStats.md)

### <a id="m-pages"></a>Pages

```csharp
public PageSet? Pages { get; set; }
```

The page screens to draw (`OneGame.Pages`): host-scope layer [`PageScopes.Under`](../../OneGame.Pages/OneGame.Pages/PageScopes.md#m-under) before the 2D pass and the UI, everything else after them. The host sets it; null draws no pages.

**Type:** [`PageSet`](../../OneGame.Pages/OneGame.Pages/PageSet.md)?

### <a id="m-requiressrgb"></a>RequiresSrgb

```csharp
public bool RequiresSrgb { get; }
```

True when the stage draws into sRGB targets (the host then makes its swapchain and capture textures sRGB).

**Type:** `bool`

## Methods

| Member | Description |
| --- | --- |
| [`AddScenePasses(GpuDevice, RenderGraph, GraphTexture, GpuTexture, in FrameInfo)`](#m-addscenepasses-gpudevice-rendergraph-graphtexture-gputexture-frameinfo) | Adds passes that draw into the target before the 2D renderer and the UI, for a stage that draws a 3D scene. |
| [`Dispose()`](#m-dispose) |  |
| [`DrawOverUi(Renderer2D, in FrameInfo)`](#m-drawoverui-renderer2d-frameinfo) | Called every frame after the UI is drawn, to add the game's own sprites and tiles. |
| [`PaintOver(Span<byte>, in FrameInfo)`](#m-paintover-span-byte-frameinfo) | Runs on the RGBA8 pixels read back from a captured GPU frame, after [`IRenderStage.Render()`](IRenderStage.md#m-render-gpudevice-gputexture-frameinfo). |
| [`ReleaseTargets(GpuTexture?)`](#m-releasetargets-gputexture) | The host is about to destroy a target (a capture texture, or all swapchain images on resize). |
| [`Render(GpuDevice, GpuTexture, in FrameInfo)`](#m-render-gpudevice-gputexture-frameinfo) | Records and submits the draw for one frame into `target` (a swapchain image or an offscreen texture). |
| [`RenderToPixels(Span<byte>, in FrameInfo)`](#m-rendertopixels-span-byte-frameinfo) | Produces the whole frame on the CPU as RGBA8, row-major, top row first. |
| [`RendererCreated(Renderer2D)`](#m-renderercreated-renderer2d) | Called once for each renderer the stage creates (one per target format). |

### <a id="m-addscenepasses-gpudevice-rendergraph-graphtexture-gputexture-frameinfo"></a>AddScenePasses(GpuDevice, RenderGraph, GraphTexture, GpuTexture, in FrameInfo)

```csharp
protected virtual bool AddScenePasses(GpuDevice device, RenderGraph graph, GraphTexture target, GpuTexture targetTexture, in FrameInfo frame);
```

Adds passes that draw into the target before the 2D renderer and the UI, for a stage that draws a 3D scene. Return true when it did; the 2D pass then keeps the target's contents instead of clearing it.

**Parameters**

- `device` ([`GpuDevice`](../../OneGame.Gpu/OneGame.Gpu/GpuDevice.md)): The graphics device.
- `graph` ([`RenderGraph`](../../OneGame.Gpu/OneGame.Gpu.Graph/RenderGraph.md)): The frame's render graph, already begun.
- `target` ([`GraphTexture`](../../OneGame.Gpu/OneGame.Gpu.Graph/GraphTexture.md)): The target as a graph resource.
- `targetTexture` ([`GpuTexture`](../../OneGame.Gpu/OneGame.Gpu/GpuTexture.md)): The target texture.
- `frame` ([`FrameInfo`](FrameInfo.md)): The frame being drawn.

**Returns:** `bool`

### <a id="m-dispose"></a>Dispose()

```csharp
public virtual void Dispose();
```

Implements [`IDisposable.Dispose()`](https://learn.microsoft.com/dotnet/api/system.idisposable.dispose).

### <a id="m-drawoverui-renderer2d-frameinfo"></a>DrawOverUi(Renderer2D, in FrameInfo)

```csharp
protected virtual void DrawOverUi(Renderer2D renderer, in FrameInfo frame);
```

Called every frame after the UI is drawn, to add the game's own sprites and tiles.

**Parameters**

- `renderer` ([`Renderer2D`](../../OneGame.Render2D/OneGame.Render2D/Renderer2D.md)): The renderer drawing this frame.
- `frame` ([`FrameInfo`](FrameInfo.md)): The frame being drawn.

### <a id="m-paintover-span-byte-frameinfo"></a>PaintOver(Span&lt;byte&gt;, in FrameInfo)

```csharp
public void PaintOver(Span<byte> rgba, in FrameInfo frame);
```

Runs on the RGBA8 pixels read back from a captured GPU frame, after [`IRenderStage.Render()`](IRenderStage.md#m-render-gpudevice-gputexture-frameinfo). Stages that draw everything on the GPU leave it empty.

**Parameters**

- `rgba` ([`Span`](https://learn.microsoft.com/dotnet/api/system.span-1)`<byte>`)
- `frame` ([`FrameInfo`](FrameInfo.md))

### <a id="m-releasetargets-gputexture"></a>ReleaseTargets(GpuTexture?)

```csharp
public void ReleaseTargets(GpuTexture? only);
```

The host is about to destroy a target (a capture texture, or all swapchain images on resize). Stages that cache per-target state drop it. `only` null means every target.

**Parameters**

- `only` ([`GpuTexture`](../../OneGame.Gpu/OneGame.Gpu/GpuTexture.md)?)

### <a id="m-render-gpudevice-gputexture-frameinfo"></a>Render(GpuDevice, GpuTexture, in FrameInfo)

```csharp
public void Render(GpuDevice device, GpuTexture target, in FrameInfo frame);
```

Records and submits the draw for one frame into `target` (a swapchain image or an offscreen texture).

**Parameters**

- `device` ([`GpuDevice`](../../OneGame.Gpu/OneGame.Gpu/GpuDevice.md))
- `target` ([`GpuTexture`](../../OneGame.Gpu/OneGame.Gpu/GpuTexture.md))
- `frame` ([`FrameInfo`](FrameInfo.md))

### <a id="m-rendertopixels-span-byte-frameinfo"></a>RenderToPixels(Span&lt;byte&gt;, in FrameInfo)

```csharp
public void RenderToPixels(Span<byte> rgba, in FrameInfo frame);
```

Produces the whole frame on the CPU as RGBA8, row-major, top row first. Used for captures on machines with no usable GPU.

**Parameters**

- `rgba` ([`Span`](https://learn.microsoft.com/dotnet/api/system.span-1)`<byte>`)
- `frame` ([`FrameInfo`](FrameInfo.md))

### <a id="m-renderercreated-renderer2d"></a>RendererCreated(Renderer2D)

```csharp
protected virtual void RendererCreated(Renderer2D renderer);
```

Called once for each renderer the stage creates (one per target format). A game that draws its own sprites creates its materials here and registers them with [`Render2DStage.HotShaders`](Render2DStage.md#m-hotshaders).

**Parameters**

- `renderer` ([`Renderer2D`](../../OneGame.Render2D/OneGame.Render2D/Renderer2D.md)): The new renderer.
