[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# IRenderStage interface

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)

```csharp
public interface IRenderStage
```

Draws one frame. The host owns the window, the swapchain and the offscreen targets and hands the stage a texture; a renderer replaces the default by overriding [`GameDefinition.CreateRenderStage()`](GameDefinition.md#m-createrenderstage-hostresources).

## Properties

| Member | Description |
| --- | --- |
| [`Graph`](#m-graph) | The render graph the stage draws with, once it has drawn a frame; null for stages that do not use one. |
| [`Name`](#m-name) | A short name shown in harness output. |
| [`RequiresSrgb`](#m-requiressrgb) | True when the stage draws into sRGB targets (the host then makes its swapchain and capture textures sRGB). |

### <a id="m-graph"></a>Graph

```csharp
RenderGraph? Graph { get; }
```

The render graph the stage draws with, once it has drawn a frame; null for stages that do not use one.

**Type:** [`RenderGraph`](../../OneGame.Gpu/OneGame.Gpu.Graph/RenderGraph.md)?

### <a id="m-name"></a>Name

```csharp
string Name { get; }
```

A short name shown in harness output.

**Type:** `string`

### <a id="m-requiressrgb"></a>RequiresSrgb

```csharp
bool RequiresSrgb { get; }
```

True when the stage draws into sRGB targets (the host then makes its swapchain and capture textures sRGB).

**Type:** `bool`

## Methods

| Member | Description |
| --- | --- |
| [`PaintOver(Span<byte>, in FrameInfo)`](#m-paintover-span-byte-frameinfo) | Runs on the RGBA8 pixels read back from a captured GPU frame, after [`IRenderStage.Render()`](IRenderStage.md#m-render-gpudevice-gputexture-frameinfo). |
| [`ReleaseTargets(GpuTexture?)`](#m-releasetargets-gputexture) | The host is about to destroy a target (a capture texture, or all swapchain images on resize). |
| [`Render(GpuDevice, GpuTexture, in FrameInfo)`](#m-render-gpudevice-gputexture-frameinfo) | Records and submits the draw for one frame into `target` (a swapchain image or an offscreen texture). |
| [`RenderToPixels(Span<byte>, in FrameInfo)`](#m-rendertopixels-span-byte-frameinfo) | Produces the whole frame on the CPU as RGBA8, row-major, top row first. |

### <a id="m-paintover-span-byte-frameinfo"></a>PaintOver(Span&lt;byte&gt;, in FrameInfo)

```csharp
void PaintOver(Span<byte> rgba, in FrameInfo frame);
```

Runs on the RGBA8 pixels read back from a captured GPU frame, after [`IRenderStage.Render()`](IRenderStage.md#m-render-gpudevice-gputexture-frameinfo). Stages that draw everything on the GPU leave it empty.

**Parameters**

- `rgba` ([`Span`](https://learn.microsoft.com/dotnet/api/system.span-1)`<byte>`)
- `frame` ([`FrameInfo`](FrameInfo.md))

### <a id="m-releasetargets-gputexture"></a>ReleaseTargets(GpuTexture?)

```csharp
void ReleaseTargets(GpuTexture? only);
```

The host is about to destroy a target (a capture texture, or all swapchain images on resize). Stages that cache per-target state drop it. `only` null means every target.

**Parameters**

- `only` ([`GpuTexture`](../../OneGame.Gpu/OneGame.Gpu/GpuTexture.md)?)

### <a id="m-render-gpudevice-gputexture-frameinfo"></a>Render(GpuDevice, GpuTexture, in FrameInfo)

```csharp
void Render(GpuDevice device, GpuTexture target, in FrameInfo frame);
```

Records and submits the draw for one frame into `target` (a swapchain image or an offscreen texture).

**Parameters**

- `device` ([`GpuDevice`](../../OneGame.Gpu/OneGame.Gpu/GpuDevice.md))
- `target` ([`GpuTexture`](../../OneGame.Gpu/OneGame.Gpu/GpuTexture.md))
- `frame` ([`FrameInfo`](FrameInfo.md))

### <a id="m-rendertopixels-span-byte-frameinfo"></a>RenderToPixels(Span&lt;byte&gt;, in FrameInfo)

```csharp
void RenderToPixels(Span<byte> rgba, in FrameInfo frame);
```

Produces the whole frame on the CPU as RGBA8, row-major, top row first. Used for captures on machines with no usable GPU.

**Parameters**

- `rgba` ([`Span`](https://learn.microsoft.com/dotnet/api/system.span-1)`<byte>`)
- `frame` ([`FrameInfo`](FrameInfo.md))
