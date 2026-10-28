[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# GpuDevice class

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)  
**Implements:** [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public abstract class GpuDevice : IDisposable
```

A graphics device: creates resources, records and submits work. Methods are not thread-safe; use one device from one thread. Work runs in submission order. Resources must not be disposed while an unsubmitted encoder still uses them.

Conventions that hold on every backend: clip space has +Y up and depth 0..1; framebuffer origin is the top-left; resource states and barriers are handled by the backend.

**Example**

Clear an offscreen image and read it back. The device is the low-level layer; most games draw through the 2D and 3D renderers, which use it.

```csharp
using var device = GpuBackends.CreateDevice();
using var target = device.CreateTexture(new TextureDesc(8, 8, GpuFormat.Rgba8Unorm, TextureUsage.RenderTarget | TextureUsage.CopySource));

var commands = device.BeginCommands();
commands.BeginRenderPass(ColorAttachment.Clear(target, 1f, 0.5f, 0f, 1f)).EndPass();
device.Submit(commands);

var pixels = new byte[8 * 8 * 4];
device.ReadTexture(target, mipLevel: 0, layer: 0, pixels);   // waits for the GPU; rows are tightly packed
```

## Constructors

| Member | Description |
| --- | --- |
| [`GpuDevice()`](#m-ctor) |  |

### <a id="m-ctor"></a>GpuDevice()

```csharp
protected GpuDevice();
```

## Fields

| Member | Description |
| --- | --- |
| [`DynamicOffsetAlignment`](#m-dynamicoffsetalignment) | Dynamic buffer offsets must be multiples of this many bytes. |
| [`MaxFramesInFlight`](#m-maxframesinflight) | Number of submissions allowed in flight before [`GpuDevice.Submit()`](GpuDevice.md#m-submit-gpucommandencoder) waits. |
| [`MaxPushConstantBytes`](#m-maxpushconstantbytes) | Most bytes of push constants a pipeline layout may declare. |

### <a id="m-dynamicoffsetalignment"></a>DynamicOffsetAlignment

```csharp
public const uint DynamicOffsetAlignment = 256;
```

Dynamic buffer offsets must be multiples of this many bytes. 256 is valid on every backend.

**Type:** `uint`

### <a id="m-maxframesinflight"></a>MaxFramesInFlight

```csharp
public const int MaxFramesInFlight = 2;
```

Number of submissions allowed in flight before [`GpuDevice.Submit()`](GpuDevice.md#m-submit-gpucommandencoder) waits.

**Type:** `int`

### <a id="m-maxpushconstantbytes"></a>MaxPushConstantBytes

```csharp
public const uint MaxPushConstantBytes = 128;
```

Most bytes of push constants a pipeline layout may declare. 128 is guaranteed on every backend.

**Type:** `uint`

## Properties

| Member | Description |
| --- | --- |
| [`Adapter`](#m-adapter) | The adapter this device runs on. |
| [`Backend`](#m-backend) | The graphics API this device runs on. |
| [`Capabilities`](#m-capabilities) | Optional features and limits of this device. |

### <a id="m-adapter"></a>Adapter

```csharp
public abstract GpuAdapterInfo Adapter { get; }
```

The adapter this device runs on.

**Type:** [`GpuAdapterInfo`](GpuAdapterInfo.md)

### <a id="m-backend"></a>Backend

```csharp
public abstract GpuBackend Backend { get; }
```

The graphics API this device runs on.

**Type:** [`GpuBackend`](GpuBackend.md)

### <a id="m-capabilities"></a>Capabilities

```csharp
public abstract GpuCapabilities Capabilities { get; }
```

Optional features and limits of this device.

**Type:** [`GpuCapabilities`](GpuCapabilities.md)

## Methods

| Member | Description |
| --- | --- |
| [`BeginCommands()`](#m-begincommands) | Starts recording commands. |
| [`BytesPerTexel(GpuFormat)`](#m-bytespertexel-gpuformat) | Bytes per texel of a format. |
| [`CreateBindGroup(GpuPipelineLayout, uint, ReadOnlySpan<BindGroupEntry>)`](#m-createbindgroup-gpupipelinelayout-uint-readonlyspan-bindgroupentry) | Creates a bind group for one set of a pipeline layout. |
| [`CreateBuffer(in BufferDesc)`](#m-createbuffer-bufferdesc) | Creates a buffer. |
| [`CreateComputePipeline(ComputePipelineDesc)`](#m-createcomputepipeline-computepipelinedesc) | Creates a compute pipeline. |
| [`CreateGraphicsPipeline(GraphicsPipelineDesc)`](#m-creategraphicspipeline-graphicspipelinedesc) | Creates a graphics pipeline: shaders, vertex layout, blend, depth and target formats fixed together. |
| [`CreatePipelineLayout(ReadOnlySpan<ResourceBinding>)`](#m-createpipelinelayout-readonlyspan-resourcebinding) | Creates a pipeline layout from the resource bindings of the pipeline's shaders. |
| [`CreatePipelineLayout(ReadOnlySpan<ResourceBinding>, in PushConstantRange)`](#m-createpipelinelayout-readonlyspan-resourcebinding-pushconstantrange) | Creates a pipeline layout like [`GpuDevice.CreatePipelineLayout()`](GpuDevice.md#m-createpipelinelayout-readonlyspan-resourcebinding) that also declares push constants. |
| [`CreateSampler(in SamplerDesc)`](#m-createsampler-samplerdesc) | Creates a sampler: how a shader filters and wraps texture reads. |
| [`CreateShader(in ShaderSource)`](#m-createshader-shadersource) | Creates a shader from a compiled entry point, using the form this backend needs (SPIR-V for Vulkan, Metal Shading Language for Metal). |
| [`CreateShader(ShaderStage, ReadOnlySpan<byte>, string)`](#m-createshader-shaderstage-readonlyspan-byte-string) | Creates a shader from SPIR-V code (4-byte aligned words, little-endian). |
| [`CreateSwapchain(in SurfaceSource, in SwapchainDesc)`](#m-createswapchain-surfacesource-swapchaindesc) | Creates a swapchain: the images presented to a native window. |
| [`CreateTexture(in TextureDesc)`](#m-createtexture-texturedesc) | Creates a texture. |
| [`CreateTimestampSet(uint)`](#m-createtimestampset-uint) | Creates a set of timestamp queries for timing passes. |
| [`Dispose()`](#m-dispose) | Waits for the GPU and releases the device and everything created from it. |
| [`LevelByteSize(GpuFormat, uint, uint, uint)`](#m-levelbytesize-gpuformat-uint-uint-uint) | Bytes needed for one tightly packed mip level of a texture. |
| [`LevelByteSize(GpuFormat, uint, uint, uint, uint)`](#m-levelbytesize-gpuformat-uint-uint-uint-uint) | Bytes needed for one tightly packed mip level of a volume texture. |
| [`ReadBuffer(GpuBuffer, ulong, Span<byte>)`](#m-readbuffer-gpubuffer-ulong-span-byte) | Waits for all submitted work, then copies buffer bytes to `destination`. |
| [`ReadBufferAsync(GpuBuffer, ulong, Memory<byte>)`](#m-readbufferasync-gpubuffer-ulong-memory-byte) | Like [`GpuDevice.ReadBuffer()`](GpuDevice.md#m-readbuffer-gpubuffer-ulong-span-byte), for backends that cannot block (the browser). |
| [`ReadTexture(GpuTexture, uint, uint, Span<byte>)`](#m-readtexture-gputexture-uint-uint-span-byte) | Waits for all submitted work, then copies a texture mip level into `destination` with tightly packed rows. |
| [`ReadTextureAsync(GpuTexture, uint, uint, Memory<byte>)`](#m-readtextureasync-gputexture-uint-uint-memory-byte) | Like [`GpuDevice.ReadTexture()`](GpuDevice.md#m-readtexture-gputexture-uint-uint-span-byte), for backends that cannot block (the browser). |
| [`Submit(GpuCommandEncoder)`](#m-submit-gpucommandencoder) | Queues recorded commands. |
| [`TakeValidationMessages()`](#m-takevalidationmessages) | Returns and clears the warnings and errors the validation layer reported since the last call. |
| [`TryReadTimestamps(GpuTimestampSet, uint, Span<ulong>)`](#m-tryreadtimestamps-gputimestampset-uint-span-ulong) | Reads timestamps as nanoseconds since an arbitrary start, without waiting. |
| [`WaitIdle()`](#m-waitidle) | Blocks until the GPU has finished everything submitted. |
| [`WriteBuffer(GpuBuffer, ulong, ReadOnlySpan<byte>)`](#m-writebuffer-gpubuffer-ulong-readonlyspan-byte) | Queues a copy of bytes into a buffer; it runs before the next submitted commands. |
| [`WriteTexture(GpuTexture, in TextureRegion, ReadOnlySpan<byte>)`](#m-writetexture-gputexture-textureregion-readonlyspan-byte) | Queues a copy of tightly packed texel rows into a box of a texture (a sub-rectangle of a mip level, a range of slices of a volume). |
| [`WriteTexture(GpuTexture, uint, uint, ReadOnlySpan<byte>)`](#m-writetexture-gputexture-uint-uint-readonlyspan-byte) | Queues a copy of tightly packed rows into a whole texture mip level (all slices of a volume); it runs before the next submitted commands. |

### <a id="m-begincommands"></a>BeginCommands()

```csharp
public abstract GpuCommandEncoder BeginCommands();
```

Starts recording commands. Nothing runs until the encoder is passed to [`GpuDevice.Submit()`](GpuDevice.md#m-submit-gpucommandencoder).

**Returns:** [`GpuCommandEncoder`](GpuCommandEncoder.md)

### <a id="m-bytespertexel-gpuformat"></a>BytesPerTexel(GpuFormat)

```csharp
public static uint BytesPerTexel(GpuFormat format);
```

Bytes per texel of a format.

**Parameters**

- `format` ([`GpuFormat`](GpuFormat.md))

**Returns:** `uint`

### <a id="m-createbindgroup-gpupipelinelayout-uint-readonlyspan-bindgroupentry"></a>CreateBindGroup(GpuPipelineLayout, uint, ReadOnlySpan&lt;BindGroupEntry&gt;)

```csharp
public abstract GpuBindGroup CreateBindGroup(GpuPipelineLayout layout, uint setIndex, ReadOnlySpan<BindGroupEntry> entries);
```

Creates a bind group for one set of a pipeline layout. Entries must cover every binding of that set.

**Parameters**

- `layout` ([`GpuPipelineLayout`](GpuPipelineLayout.md))
- `setIndex` (`uint`)
- `entries` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)&lt;[`BindGroupEntry`](BindGroupEntry.md)&gt;)

**Returns:** [`GpuBindGroup`](GpuBindGroup.md)

### <a id="m-createbuffer-bufferdesc"></a>CreateBuffer(in BufferDesc)

```csharp
public abstract GpuBuffer CreateBuffer(in BufferDesc desc);
```

Creates a buffer. Contents are undefined until written with [`GpuDevice.WriteBuffer()`](GpuDevice.md#m-writebuffer-gpubuffer-ulong-readonlyspan-byte).

**Parameters**

- `desc` ([`BufferDesc`](BufferDesc.md)): Size and allowed uses.

**Returns:** [`GpuBuffer`](GpuBuffer.md)

**Exceptions**

- [`GpuOutOfMemoryException`](GpuOutOfMemoryException.md): The device has no memory left.

### <a id="m-createcomputepipeline-computepipelinedesc"></a>CreateComputePipeline(ComputePipelineDesc)

```csharp
public abstract GpuComputePipeline CreateComputePipeline(ComputePipelineDesc desc);
```

Creates a compute pipeline.

**Parameters**

- `desc` ([`ComputePipelineDesc`](ComputePipelineDesc.md)): The compute shader and layout.

**Returns:** [`GpuComputePipeline`](GpuComputePipeline.md)

### <a id="m-creategraphicspipeline-graphicspipelinedesc"></a>CreateGraphicsPipeline(GraphicsPipelineDesc)

```csharp
public abstract GpuGraphicsPipeline CreateGraphicsPipeline(GraphicsPipelineDesc desc);
```

Creates a graphics pipeline: shaders, vertex layout, blend, depth and target formats fixed together. Create pipelines once and reuse them.

**Parameters**

- `desc` ([`GraphicsPipelineDesc`](GraphicsPipelineDesc.md)): The pipeline description.

**Returns:** [`GpuGraphicsPipeline`](GpuGraphicsPipeline.md)

**Exceptions**

- [`GpuException`](GpuException.md): The shaders and layout do not agree, or the backend rejected the description.

### <a id="m-createpipelinelayout-readonlyspan-resourcebinding"></a>CreatePipelineLayout(ReadOnlySpan&lt;ResourceBinding&gt;)

```csharp
public abstract GpuPipelineLayout CreatePipelineLayout(ReadOnlySpan<ResourceBinding> bindings);
```

Creates a pipeline layout from the resource bindings of the pipeline's shaders. Entries from several shaders may repeat; identical ones merge and their stages combine.

**Parameters**

- `bindings` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)&lt;[`ResourceBinding`](ResourceBinding.md)&gt;)

**Returns:** [`GpuPipelineLayout`](GpuPipelineLayout.md)

### <a id="m-createpipelinelayout-readonlyspan-resourcebinding-pushconstantrange"></a>CreatePipelineLayout(ReadOnlySpan&lt;ResourceBinding&gt;, in PushConstantRange)

```csharp
public abstract GpuPipelineLayout CreatePipelineLayout(ReadOnlySpan<ResourceBinding> bindings, in PushConstantRange pushConstants);
```

Creates a pipeline layout like [`GpuDevice.CreatePipelineLayout()`](GpuDevice.md#m-createpipelinelayout-readonlyspan-resourcebinding) that also declares push constants.

**Parameters**

- `bindings` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)&lt;[`ResourceBinding`](ResourceBinding.md)&gt;)
- `pushConstants` ([`PushConstantRange`](PushConstantRange.md))

**Returns:** [`GpuPipelineLayout`](GpuPipelineLayout.md)

### <a id="m-createsampler-samplerdesc"></a>CreateSampler(in SamplerDesc)

```csharp
public abstract GpuSampler CreateSampler(in SamplerDesc desc);
```

Creates a sampler: how a shader filters and wraps texture reads.

**Parameters**

- `desc` ([`SamplerDesc`](SamplerDesc.md)): Filters, addressing and level-of-detail range.

**Returns:** [`GpuSampler`](GpuSampler.md)

### <a id="m-createshader-shadersource"></a>CreateShader(in ShaderSource)

```csharp
public virtual GpuShader CreateShader(in ShaderSource source);
```

Creates a shader from a compiled entry point, using the form this backend needs (SPIR-V for Vulkan, Metal Shading Language for Metal). Prefer this over the SPIR-V overload: it works on every backend.

**Parameters**

- `source` ([`ShaderSource`](ShaderSource.md))

**Returns:** [`GpuShader`](GpuShader.md)

**Exceptions**

- [`GpuUnsupportedException`](GpuUnsupportedException.md): The source has no code for this backend.

### <a id="m-createshader-shaderstage-readonlyspan-byte-string"></a>CreateShader(ShaderStage, ReadOnlySpan&lt;byte&gt;, string)

```csharp
public abstract GpuShader CreateShader(ShaderStage stage, ReadOnlySpan<byte> spirv, string entryPoint);
```

Creates a shader from SPIR-V code (4-byte aligned words, little-endian).

**Parameters**

- `stage` ([`ShaderStage`](ShaderStage.md)): The shader stage.
- `spirv` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`): The SPIR-V module.
- `entryPoint` (`string`): The entry point's name.

**Returns:** [`GpuShader`](GpuShader.md)

### <a id="m-createswapchain-surfacesource-swapchaindesc"></a>CreateSwapchain(in SurfaceSource, in SwapchainDesc)

```csharp
public abstract GpuSwapchain CreateSwapchain(in SurfaceSource surface, in SwapchainDesc desc);
```

Creates a swapchain: the images presented to a native window.

**Parameters**

- `surface` ([`SurfaceSource`](SurfaceSource.md)): The window, from the platform layer.
- `desc` ([`SwapchainDesc`](SwapchainDesc.md)): Initial size, format and presentation pacing.

**Returns:** [`GpuSwapchain`](GpuSwapchain.md)

**Exceptions**

- [`GpuUnsupportedException`](GpuUnsupportedException.md): The adapter cannot present to this window.

### <a id="m-createtexture-texturedesc"></a>CreateTexture(in TextureDesc)

```csharp
public abstract GpuTexture CreateTexture(in TextureDesc desc);
```

Creates a texture. Contents are undefined until written ([`GpuDevice.WriteTexture()`](GpuDevice.md#m-writetexture-gputexture-uint-uint-readonlyspan-byte)) or rendered into.

**Parameters**

- `desc` ([`TextureDesc`](TextureDesc.md)): Size, format, mip levels and allowed uses.

**Returns:** [`GpuTexture`](GpuTexture.md)

**Exceptions**

- [`GpuOutOfMemoryException`](GpuOutOfMemoryException.md): The device has no memory left.
- [`GpuUnsupportedException`](GpuUnsupportedException.md): The format or usage is not supported by this device.

### <a id="m-createtimestampset-uint"></a>CreateTimestampSet(uint)

```csharp
public abstract GpuTimestampSet CreateTimestampSet(uint count);
```

Creates a set of timestamp queries for timing passes.

**Parameters**

- `count` (`uint`)

**Returns:** [`GpuTimestampSet`](GpuTimestampSet.md)

**Exceptions**

- [`GpuUnsupportedException`](GpuUnsupportedException.md): The device cannot time passes ([`GpuCapabilities.Timestamps`](GpuCapabilities.md#m-timestamps) is false).

### <a id="m-dispose"></a>Dispose()

```csharp
public abstract void Dispose();
```

Waits for the GPU and releases the device and everything created from it.

### <a id="m-levelbytesize-gpuformat-uint-uint-uint"></a>LevelByteSize(GpuFormat, uint, uint, uint)

```csharp
public static ulong LevelByteSize(GpuFormat format, uint width, uint height, uint mipLevel);
```

Bytes needed for one tightly packed mip level of a texture.

**Parameters**

- `format` ([`GpuFormat`](GpuFormat.md))
- `width` (`uint`)
- `height` (`uint`)
- `mipLevel` (`uint`)

**Returns:** `ulong`

### <a id="m-levelbytesize-gpuformat-uint-uint-uint-uint"></a>LevelByteSize(GpuFormat, uint, uint, uint, uint)

```csharp
public static ulong LevelByteSize(GpuFormat format, uint width, uint height, uint depth, uint mipLevel);
```

Bytes needed for one tightly packed mip level of a volume texture.

**Parameters**

- `format` ([`GpuFormat`](GpuFormat.md))
- `width` (`uint`)
- `height` (`uint`)
- `depth` (`uint`)
- `mipLevel` (`uint`)

**Returns:** `ulong`

### <a id="m-readbuffer-gpubuffer-ulong-span-byte"></a>ReadBuffer(GpuBuffer, ulong, Span&lt;byte&gt;)

```csharp
public abstract void ReadBuffer(GpuBuffer buffer, ulong offset, Span<byte> destination);
```

Waits for all submitted work, then copies buffer bytes to `destination`. Blocks, so keep it for tools and tests.

**Parameters**

- `buffer` ([`GpuBuffer`](GpuBuffer.md)): The source; it needs [`BufferUsage.CopySource`](BufferUsage.md#m-copysource).
- `offset` (`ulong`): Byte offset into the buffer.
- `destination` ([`Span`](https://learn.microsoft.com/dotnet/api/system.span-1)`<byte>`): Receives the bytes; its length is the amount read.

### <a id="m-readbufferasync-gpubuffer-ulong-memory-byte"></a>ReadBufferAsync(GpuBuffer, ulong, Memory&lt;byte&gt;)

```csharp
public virtual ValueTask ReadBufferAsync(GpuBuffer buffer, ulong offset, Memory<byte> destination);
```

Like [`GpuDevice.ReadBuffer()`](GpuDevice.md#m-readbuffer-gpubuffer-ulong-span-byte), for backends that cannot block (the browser). Native backends complete before returning.

**Parameters**

- `buffer` ([`GpuBuffer`](GpuBuffer.md))
- `offset` (`ulong`)
- `destination` ([`Memory`](https://learn.microsoft.com/dotnet/api/system.memory-1)`<byte>`)

**Returns:** [`ValueTask`](https://learn.microsoft.com/dotnet/api/system.threading.tasks.valuetask)

### <a id="m-readtexture-gputexture-uint-uint-span-byte"></a>ReadTexture(GpuTexture, uint, uint, Span&lt;byte&gt;)

```csharp
public abstract void ReadTexture(GpuTexture texture, uint mipLevel, uint layer, Span<byte> destination);
```

Waits for all submitted work, then copies a texture mip level into `destination` with tightly packed rows. Blocks, so keep it for tools and tests.

**Parameters**

- `texture` ([`GpuTexture`](GpuTexture.md)): The source; it needs [`TextureUsage.CopySource`](TextureUsage.md#m-copysource).
- `mipLevel` (`uint`): The mip level.
- `layer` (`uint`): The array layer or cube face.
- `destination` ([`Span`](https://learn.microsoft.com/dotnet/api/system.span-1)`<byte>`): Receives [`GpuDevice.LevelByteSize()`](GpuDevice.md#m-levelbytesize-gpuformat-uint-uint-uint) bytes.

### <a id="m-readtextureasync-gputexture-uint-uint-memory-byte"></a>ReadTextureAsync(GpuTexture, uint, uint, Memory&lt;byte&gt;)

```csharp
public virtual ValueTask ReadTextureAsync(GpuTexture texture, uint mipLevel, uint layer, Memory<byte> destination);
```

Like [`GpuDevice.ReadTexture()`](GpuDevice.md#m-readtexture-gputexture-uint-uint-span-byte), for backends that cannot block (the browser). Native backends complete before returning.

**Parameters**

- `texture` ([`GpuTexture`](GpuTexture.md))
- `mipLevel` (`uint`)
- `layer` (`uint`)
- `destination` ([`Memory`](https://learn.microsoft.com/dotnet/api/system.memory-1)`<byte>`)

**Returns:** [`ValueTask`](https://learn.microsoft.com/dotnet/api/system.threading.tasks.valuetask)

### <a id="m-submit-gpucommandencoder"></a>Submit(GpuCommandEncoder)

```csharp
public abstract void Submit(GpuCommandEncoder commands);
```

Queues recorded commands. Pending uploads run first. Waits when too many submissions are still in flight.

**Parameters**

- `commands` ([`GpuCommandEncoder`](GpuCommandEncoder.md)): The encoder from [`GpuDevice.BeginCommands()`](GpuDevice.md#m-begincommands); it is finished by this call and cannot be reused.

**Exceptions**

- [`GpuDeviceLostException`](GpuDeviceLostException.md): The driver reset or the GPU was removed.

### <a id="m-takevalidationmessages"></a>TakeValidationMessages()

```csharp
public abstract string[] TakeValidationMessages();
```

Returns and clears the warnings and errors the validation layer reported since the last call. Empty when validation is off.

**Returns:** `string[]`

### <a id="m-tryreadtimestamps-gputimestampset-uint-span-ulong"></a>TryReadTimestamps(GpuTimestampSet, uint, Span&lt;ulong&gt;)

```csharp
public abstract bool TryReadTimestamps(GpuTimestampSet timestamps, uint first, Span<ulong> nanoseconds);
```

Reads timestamps as nanoseconds since an arbitrary start, without waiting. Returns false when the passes that write them have not finished (nothing is written to `nanoseconds` then).

**Parameters**

- `timestamps` ([`GpuTimestampSet`](GpuTimestampSet.md))
- `first` (`uint`)
- `nanoseconds` ([`Span`](https://learn.microsoft.com/dotnet/api/system.span-1)`<ulong>`)

**Returns:** `bool`

### <a id="m-waitidle"></a>WaitIdle()

```csharp
public abstract void WaitIdle();
```

Blocks until the GPU has finished everything submitted.

### <a id="m-writebuffer-gpubuffer-ulong-readonlyspan-byte"></a>WriteBuffer(GpuBuffer, ulong, ReadOnlySpan&lt;byte&gt;)

```csharp
public abstract void WriteBuffer(GpuBuffer buffer, ulong offset, ReadOnlySpan<byte> data);
```

Queues a copy of bytes into a buffer; it runs before the next submitted commands.

**Parameters**

- `buffer` ([`GpuBuffer`](GpuBuffer.md)): The destination; it needs [`BufferUsage.CopyDestination`](BufferUsage.md#m-copydestination).
- `offset` (`ulong`): Byte offset into the buffer.
- `data` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`): The bytes.

### <a id="m-writetexture-gputexture-textureregion-readonlyspan-byte"></a>WriteTexture(GpuTexture, in TextureRegion, ReadOnlySpan&lt;byte&gt;)

```csharp
public abstract void WriteTexture(GpuTexture texture, in TextureRegion region, ReadOnlySpan<byte> data);
```

Queues a copy of tightly packed texel rows into a box of a texture (a sub-rectangle of a mip level, a range of slices of a volume). It runs before the next submitted commands. `data` holds exactly width × height × depth texels.

**Parameters**

- `texture` ([`GpuTexture`](GpuTexture.md))
- `region` ([`TextureRegion`](TextureRegion.md))
- `data` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`)

### <a id="m-writetexture-gputexture-uint-uint-readonlyspan-byte"></a>WriteTexture(GpuTexture, uint, uint, ReadOnlySpan&lt;byte&gt;)

```csharp
public abstract void WriteTexture(GpuTexture texture, uint mipLevel, uint layer, ReadOnlySpan<byte> data);
```

Queues a copy of tightly packed rows into a whole texture mip level (all slices of a volume); it runs before the next submitted commands.

**Parameters**

- `texture` ([`GpuTexture`](GpuTexture.md)): The destination; it needs [`TextureUsage.CopyDestination`](TextureUsage.md#m-copydestination).
- `mipLevel` (`uint`): The mip level.
- `layer` (`uint`): The array layer or cube face.
- `data` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`): Exactly [`GpuDevice.LevelByteSize()`](GpuDevice.md#m-levelbytesize-gpuformat-uint-uint-uint) bytes for the level.
