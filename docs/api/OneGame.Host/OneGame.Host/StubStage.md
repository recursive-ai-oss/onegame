[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# StubStage class

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)  
**Implements:** [`IRenderStage`](IRenderStage.md)

```csharp
public sealed class StubStage : IRenderStage
```

The stand-in stage: the GPU clears the target to a flat color, and for captures the UI's rectangles, borders and text lines are painted over the read-back pixels by a small deterministic CPU rasterizer. It exists so frames can be captured and compared before the 2D renderer lands; a window shows only the clear color.

## Constructors

| Member | Description |
| --- | --- |
| [`StubStage()`](#m-ctor) |  |

### <a id="m-ctor"></a>StubStage()

```csharp
public StubStage();
```

## Fields

| Member | Description |
| --- | --- |
| [`Clear`](#m-clear) | The clear color (RGBA). |

### <a id="m-clear"></a>Clear

```csharp
public static readonly (byte R, byte G, byte B, byte A) Clear;
```

The clear color (RGBA).

**Type:** `(byte R, byte G, byte B, byte A)`

## Properties

| Member | Description |
| --- | --- |
| [`Name`](#m-name) | A short name shown in harness output. |

### <a id="m-name"></a>Name

```csharp
public string Name { get; }
```

A short name shown in harness output.

**Type:** `string`

## Methods

| Member | Description |
| --- | --- |
| [`PaintOver(Span<byte>, in FrameInfo)`](#m-paintover-span-byte-frameinfo) | Runs on the RGBA8 pixels read back from a captured GPU frame, after [`IRenderStage.Render()`](IRenderStage.md#m-render-gpudevice-gputexture-frameinfo). |
| [`Render(GpuDevice, GpuTexture, in FrameInfo)`](#m-render-gpudevice-gputexture-frameinfo) | Records and submits the draw for one frame into `target` (a swapchain image or an offscreen texture). |
| [`RenderToPixels(Span<byte>, in FrameInfo)`](#m-rendertopixels-span-byte-frameinfo) | Produces the whole frame on the CPU as RGBA8, row-major, top row first. |

### <a id="m-paintover-span-byte-frameinfo"></a>PaintOver(Span&lt;byte&gt;, in FrameInfo)

```csharp
public void PaintOver(Span<byte> rgba, in FrameInfo frame);
```

Runs on the RGBA8 pixels read back from a captured GPU frame, after [`IRenderStage.Render()`](IRenderStage.md#m-render-gpudevice-gputexture-frameinfo). Stages that draw everything on the GPU leave it empty.

**Parameters**

- `rgba` ([`Span`](https://learn.microsoft.com/dotnet/api/system.span-1)`<byte>`)
- `frame` ([`FrameInfo`](FrameInfo.md))

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
