[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# GpuCommandEncoder class

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)

```csharp
public abstract class GpuCommandEncoder
```

Records a sequence of copy, render and compute work, then is handed to [`GpuDevice.Submit()`](GpuDevice.md#m-submit-gpucommandencoder). Encoders are reused after submission; do not touch one afterwards.

## Constructors

| Member | Description |
| --- | --- |
| [`GpuCommandEncoder()`](#m-ctor) |  |

### <a id="m-ctor"></a>GpuCommandEncoder()

```csharp
protected GpuCommandEncoder();
```

## Methods

| Member | Description |
| --- | --- |
| [`BeginComputePass()`](#m-begincomputepass) | Begins a compute pass. |
| [`BeginPassTiming(in PassTiming)`](#m-beginpasstiming-passtiming) | Times the render and compute passes begun after this call: pass `k` (counting from 0) writes its start and end to queries `First + 2k` and `First + 2k + 1`, up to [`PassTiming.MaxPasses`](PassTiming.md#m-maxpasses) passes. |
| [`BeginRenderPass(in ColorAttachment, DepthAttachment?)`](#m-beginrenderpass-colorattachment-depthattachment) | Begins a render pass into one color attachment, with an optional depth attachment. |
| [`BeginRenderPass(ReadOnlySpan<ColorAttachment>, DepthAttachment?)`](#m-beginrenderpass-readonlyspan-colorattachment-depthattachment) | Begins a render pass into several color attachments, with an optional depth attachment. |
| [`CopyBuffer(GpuBuffer, ulong, GpuBuffer, ulong, ulong)`](#m-copybuffer-gpubuffer-ulong-gpubuffer-ulong-ulong) | Copies bytes between buffers. |
| [`CopyBufferToTexture(GpuBuffer, ulong, GpuTexture, uint, uint)`](#m-copybuffertotexture-gpubuffer-ulong-gputexture-uint-uint) | Copies tightly packed rows from a buffer into a texture mip level. |
| [`CopyTexture(GpuTexture, GpuTexture)`](#m-copytexture-gputexture-gputexture) | Copies every mip level and layer of one texture into another of the same size, format, dimension, mip count and layer count. |
| [`CopyTextureToBuffer(GpuTexture, uint, uint, GpuBuffer, ulong)`](#m-copytexturetobuffer-gputexture-uint-uint-gpubuffer-ulong) | Copies a texture mip level into a buffer with tightly packed rows. |
| [`EndPassTiming()`](#m-endpasstiming) | Stops timing passes and returns how many were timed since [`GpuCommandEncoder.BeginPassTiming()`](GpuCommandEncoder.md#m-beginpasstiming-passtiming). |
| [`GenerateMips(GpuTexture)`](#m-generatemips-gputexture) | Fills mip levels 1 and up of every layer (and every slice of a volume) by repeatedly halving level 0 with linear filtering. |

### <a id="m-begincomputepass"></a>BeginComputePass()

```csharp
public abstract GpuComputePass BeginComputePass();
```

Begins a compute pass.

**Returns:** [`GpuComputePass`](GpuComputePass.md)

### <a id="m-beginpasstiming-passtiming"></a>BeginPassTiming(in PassTiming)

```csharp
public abstract void BeginPassTiming(in PassTiming timing);
```

Times the render and compute passes begun after this call: pass `k` (counting from 0) writes its start and end to queries `First + 2k` and `First + 2k + 1`, up to [`PassTiming.MaxPasses`](PassTiming.md#m-maxpasses) passes. Call [`GpuCommandEncoder.EndPassTiming()`](GpuCommandEncoder.md#m-endpasstiming) to stop.

**Parameters**

- `timing` ([`PassTiming`](PassTiming.md))

### <a id="m-beginrenderpass-colorattachment-depthattachment"></a>BeginRenderPass(in ColorAttachment, DepthAttachment?)

```csharp
public GpuRenderPass BeginRenderPass(in ColorAttachment color, DepthAttachment? depth = default);
```

Begins a render pass into one color attachment, with an optional depth attachment. End it before recording anything else.

**Parameters**

- `color` ([`ColorAttachment`](ColorAttachment.md))
- `depth` ([`DepthAttachment`](DepthAttachment.md)?)

**Returns:** [`GpuRenderPass`](GpuRenderPass.md)

### <a id="m-beginrenderpass-readonlyspan-colorattachment-depthattachment"></a>BeginRenderPass(ReadOnlySpan&lt;ColorAttachment&gt;, DepthAttachment?)

```csharp
public abstract GpuRenderPass BeginRenderPass(ReadOnlySpan<ColorAttachment> colors, DepthAttachment? depth = default);
```

Begins a render pass into several color attachments, with an optional depth attachment.

**Parameters**

- `colors` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)&lt;[`ColorAttachment`](ColorAttachment.md)&gt;)
- `depth` ([`DepthAttachment`](DepthAttachment.md)?)

**Returns:** [`GpuRenderPass`](GpuRenderPass.md)

### <a id="m-copybuffer-gpubuffer-ulong-gpubuffer-ulong-ulong"></a>CopyBuffer(GpuBuffer, ulong, GpuBuffer, ulong, ulong)

```csharp
public abstract void CopyBuffer(GpuBuffer source, ulong sourceOffset, GpuBuffer destination, ulong destinationOffset, ulong size);
```

Copies bytes between buffers.

**Parameters**

- `source` ([`GpuBuffer`](GpuBuffer.md))
- `sourceOffset` (`ulong`)
- `destination` ([`GpuBuffer`](GpuBuffer.md))
- `destinationOffset` (`ulong`)
- `size` (`ulong`)

### <a id="m-copybuffertotexture-gpubuffer-ulong-gputexture-uint-uint"></a>CopyBufferToTexture(GpuBuffer, ulong, GpuTexture, uint, uint)

```csharp
public abstract void CopyBufferToTexture(GpuBuffer source, ulong sourceOffset, GpuTexture destination, uint mipLevel, uint layer);
```

Copies tightly packed rows from a buffer into a texture mip level.

**Parameters**

- `source` ([`GpuBuffer`](GpuBuffer.md))
- `sourceOffset` (`ulong`)
- `destination` ([`GpuTexture`](GpuTexture.md))
- `mipLevel` (`uint`)
- `layer` (`uint`)

### <a id="m-copytexture-gputexture-gputexture"></a>CopyTexture(GpuTexture, GpuTexture)

```csharp
public abstract void CopyTexture(GpuTexture source, GpuTexture destination);
```

Copies every mip level and layer of one texture into another of the same size, format, dimension, mip count and layer count. Neither may be multisampled. The source needs [`TextureUsage.CopySource`](TextureUsage.md#m-copysource) and the destination [`TextureUsage.CopyDestination`](TextureUsage.md#m-copydestination).

**Parameters**

- `source` ([`GpuTexture`](GpuTexture.md))
- `destination` ([`GpuTexture`](GpuTexture.md))

### <a id="m-copytexturetobuffer-gputexture-uint-uint-gpubuffer-ulong"></a>CopyTextureToBuffer(GpuTexture, uint, uint, GpuBuffer, ulong)

```csharp
public abstract void CopyTextureToBuffer(GpuTexture source, uint mipLevel, uint layer, GpuBuffer destination, ulong destinationOffset);
```

Copies a texture mip level into a buffer with tightly packed rows.

**Parameters**

- `source` ([`GpuTexture`](GpuTexture.md))
- `mipLevel` (`uint`)
- `layer` (`uint`)
- `destination` ([`GpuBuffer`](GpuBuffer.md))
- `destinationOffset` (`ulong`)

### <a id="m-endpasstiming"></a>EndPassTiming()

```csharp
public abstract uint EndPassTiming();
```

Stops timing passes and returns how many were timed since [`GpuCommandEncoder.BeginPassTiming()`](GpuCommandEncoder.md#m-beginpasstiming-passtiming).

**Returns:** `uint`

### <a id="m-generatemips-gputexture"></a>GenerateMips(GpuTexture)

```csharp
public abstract void GenerateMips(GpuTexture texture);
```

Fills mip levels 1 and up of every layer (and every slice of a volume) by repeatedly halving level 0 with linear filtering. Not for depth textures.

**Parameters**

- `texture` ([`GpuTexture`](GpuTexture.md))
