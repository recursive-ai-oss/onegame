[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu.Graph

# PassContext class

**Namespace:** `OneGame.Gpu.Graph`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)

```csharp
public sealed class PassContext
```

What a pass may do while it records: get the physical textures and buffers it declared, and the render or compute pass the graph opened for it.

## Properties

| Member | Description |
| --- | --- |
| [`Compute`](#m-compute) | The compute pass the graph opened for a pass that called [`PassBuilder.Compute()`](PassBuilder.md#m-compute). |
| [`Device`](#m-device) | The device. |
| [`Encoder`](#m-encoder) | The encoder everything records into. |
| [`Frame`](#m-frame) | Number of the frame being recorded. |
| [`PassName`](#m-passname) | Name of the running pass. |
| [`Render`](#m-render) | The render pass the graph opened for a pass that declared targets. |

### <a id="m-compute"></a>Compute

```csharp
public GpuComputePass Compute { get; }
```

The compute pass the graph opened for a pass that called [`PassBuilder.Compute()`](PassBuilder.md#m-compute). Do not end it.

**Type:** [`GpuComputePass`](../OneGame.Gpu/GpuComputePass.md)

### <a id="m-device"></a>Device

```csharp
public GpuDevice Device { get; }
```

The device.

**Type:** [`GpuDevice`](../OneGame.Gpu/GpuDevice.md)

### <a id="m-encoder"></a>Encoder

```csharp
public GpuCommandEncoder Encoder { get; }
```

The encoder everything records into. Use it directly in a custom pass (copies, mip generation, or a renderer that opens its own render passes). In a raster or compute pass the graph has a pass open already; use [`PassContext.Render`](PassContext.md#m-render) or [`PassContext.Compute`](PassContext.md#m-compute).

**Type:** [`GpuCommandEncoder`](../OneGame.Gpu/GpuCommandEncoder.md)

### <a id="m-frame"></a>Frame

```csharp
public int Frame { get; }
```

Number of the frame being recorded.

**Type:** `int`

### <a id="m-passname"></a>PassName

```csharp
public string PassName { get; }
```

Name of the running pass.

**Type:** `string`

### <a id="m-render"></a>Render

```csharp
public GpuRenderPass Render { get; }
```

The render pass the graph opened for a pass that declared targets. Do not end it; the graph does.

**Type:** [`GpuRenderPass`](../OneGame.Gpu/GpuRenderPass.md)

## Methods

| Member | Description |
| --- | --- |
| [`Buffer(GraphBuffer)`](#m-buffer-graphbuffer) | The buffer behind a handle. |
| [`Texture(GraphTexture)`](#m-texture-graphtexture) | The texture behind a handle. |

### <a id="m-buffer-graphbuffer"></a>Buffer(GraphBuffer)

```csharp
public GpuBuffer Buffer(GraphBuffer handle);
```

The buffer behind a handle. The pass must have declared it.

**Parameters**

- `handle` ([`GraphBuffer`](GraphBuffer.md))

**Returns:** [`GpuBuffer`](../OneGame.Gpu/GpuBuffer.md)

**Exceptions**

- [`RenderGraphException`](RenderGraphException.md): The handle is stale, or this pass did not declare it.

### <a id="m-texture-graphtexture"></a>Texture(GraphTexture)

```csharp
public GpuTexture Texture(GraphTexture handle);
```

The texture behind a handle. The pass must have declared it.

**Parameters**

- `handle` ([`GraphTexture`](GraphTexture.md))

**Returns:** [`GpuTexture`](../OneGame.Gpu/GpuTexture.md)

**Exceptions**

- [`RenderGraphException`](RenderGraphException.md): The handle is stale, or this pass did not declare it.
