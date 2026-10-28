[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# GpuRenderPass class

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)

```csharp
public abstract class GpuRenderPass
```

Records draws inside a render pass.

## Constructors

| Member | Description |
| --- | --- |
| [`GpuRenderPass()`](#m-ctor) |  |

### <a id="m-ctor"></a>GpuRenderPass()

```csharp
protected GpuRenderPass();
```

## Methods

| Member | Description |
| --- | --- |
| [`Draw(uint, uint, uint, uint)`](#m-draw-uint-uint-uint-uint) | Draws vertices. |
| [`DrawIndexed(uint, uint, uint, int, uint)`](#m-drawindexed-uint-uint-uint-int-uint) | Draws indexed vertices. |
| [`DrawIndexedIndirect(GpuBuffer, ulong, uint)`](#m-drawindexedindirect-gpubuffer-ulong-uint) | Draws indexed with arguments read from a buffer ([`DrawIndexedIndirectArgs`](DrawIndexedIndirectArgs.md), 20 bytes each, packed). |
| [`DrawIndirect(GpuBuffer, ulong, uint)`](#m-drawindirect-gpubuffer-ulong-uint) | Draws with arguments read from a buffer ([`DrawIndirectArgs`](DrawIndirectArgs.md), 16 bytes each, packed). |
| [`EndPass()`](#m-endpass) | Ends the pass. |
| [`SetBindGroup(uint, GpuBindGroup)`](#m-setbindgroup-uint-gpubindgroup) | Binds a bind group at a set index of the current pipeline layout. |
| [`SetBindGroup(uint, GpuBindGroup, ReadOnlySpan<uint>)`](#m-setbindgroup-uint-gpubindgroup-readonlyspan-uint) | Binds a bind group whose layout has dynamic bindings. |
| [`SetIndexBuffer(GpuBuffer, IndexFormat, ulong)`](#m-setindexbuffer-gpubuffer-indexformat-ulong) | Binds the index buffer. |
| [`SetPipeline(GpuGraphicsPipeline)`](#m-setpipeline-gpugraphicspipeline) | Sets the pipeline for the following draws. |
| [`SetPushConstants(uint, ReadOnlySpan<byte>)`](#m-setpushconstants-uint-readonlyspan-byte) | Writes push constants (up to the size the pipeline layout declared) visible to the following draws. |
| [`SetScissor(int, int, uint, uint)`](#m-setscissor-int-int-uint-uint) | Sets the scissor rectangle in pixels. |
| [`SetVertexBuffer(uint, GpuBuffer, ulong)`](#m-setvertexbuffer-uint-gpubuffer-ulong) | Binds a vertex buffer to a vertex buffer slot. |
| [`SetViewport(float, float, float, float, float, float)`](#m-setviewport-float-float-float-float-float-float) | Sets the viewport in pixels. |

### <a id="m-draw-uint-uint-uint-uint"></a>Draw(uint, uint, uint, uint)

```csharp
public abstract void Draw(uint vertexCount, uint instanceCount = 1, uint firstVertex = 0, uint firstInstance = 0);
```

Draws vertices.

**Parameters**

- `vertexCount` (`uint`)
- `instanceCount` (`uint`)
- `firstVertex` (`uint`)
- `firstInstance` (`uint`)

### <a id="m-drawindexed-uint-uint-uint-int-uint"></a>DrawIndexed(uint, uint, uint, int, uint)

```csharp
public abstract void DrawIndexed(uint indexCount, uint instanceCount = 1, uint firstIndex = 0, int baseVertex = 0, uint firstInstance = 0);
```

Draws indexed vertices.

**Parameters**

- `indexCount` (`uint`)
- `instanceCount` (`uint`)
- `firstIndex` (`uint`)
- `baseVertex` (`int`)
- `firstInstance` (`uint`)

### <a id="m-drawindexedindirect-gpubuffer-ulong-uint"></a>DrawIndexedIndirect(GpuBuffer, ulong, uint)

```csharp
public abstract void DrawIndexedIndirect(GpuBuffer arguments, ulong offset, uint drawCount = 1);
```

Draws indexed with arguments read from a buffer ([`DrawIndexedIndirectArgs`](DrawIndexedIndirectArgs.md), 20 bytes each, packed). See [`GpuRenderPass.DrawIndirect()`](GpuRenderPass.md#m-drawindirect-gpubuffer-ulong-uint).

**Parameters**

- `arguments` ([`GpuBuffer`](GpuBuffer.md))
- `offset` (`ulong`)
- `drawCount` (`uint`)

### <a id="m-drawindirect-gpubuffer-ulong-uint"></a>DrawIndirect(GpuBuffer, ulong, uint)

```csharp
public abstract void DrawIndirect(GpuBuffer arguments, ulong offset, uint drawCount = 1);
```

Draws with arguments read from a buffer ([`DrawIndirectArgs`](DrawIndirectArgs.md), 16 bytes each, packed). The buffer needs [`BufferUsage.Indirect`](BufferUsage.md#m-indirect). With `drawCount` above 1, that many consecutive records are drawn.

**Parameters**

- `arguments` ([`GpuBuffer`](GpuBuffer.md))
- `offset` (`ulong`)
- `drawCount` (`uint`)

### <a id="m-endpass"></a>EndPass()

```csharp
public abstract void EndPass();
```

Ends the pass.

### <a id="m-setbindgroup-uint-gpubindgroup"></a>SetBindGroup(uint, GpuBindGroup)

```csharp
public void SetBindGroup(uint setIndex, GpuBindGroup group);
```

Binds a bind group at a set index of the current pipeline layout.

**Parameters**

- `setIndex` (`uint`)
- `group` ([`GpuBindGroup`](GpuBindGroup.md))

### <a id="m-setbindgroup-uint-gpubindgroup-readonlyspan-uint"></a>SetBindGroup(uint, GpuBindGroup, ReadOnlySpan&lt;uint&gt;)

```csharp
public abstract void SetBindGroup(uint setIndex, GpuBindGroup group, ReadOnlySpan<uint> dynamicOffsets);
```

Binds a bind group whose layout has dynamic bindings. `dynamicOffsets` holds one byte offset per dynamic binding, in binding order, each a multiple of [`GpuDevice.DynamicOffsetAlignment`](GpuDevice.md#m-dynamicoffsetalignment).

**Parameters**

- `setIndex` (`uint`)
- `group` ([`GpuBindGroup`](GpuBindGroup.md))
- `dynamicOffsets` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<uint>`)

### <a id="m-setindexbuffer-gpubuffer-indexformat-ulong"></a>SetIndexBuffer(GpuBuffer, IndexFormat, ulong)

```csharp
public abstract void SetIndexBuffer(GpuBuffer buffer, IndexFormat format, ulong offset = 0);
```

Binds the index buffer.

**Parameters**

- `buffer` ([`GpuBuffer`](GpuBuffer.md))
- `format` ([`IndexFormat`](IndexFormat.md))
- `offset` (`ulong`)

### <a id="m-setpipeline-gpugraphicspipeline"></a>SetPipeline(GpuGraphicsPipeline)

```csharp
public abstract void SetPipeline(GpuGraphicsPipeline pipeline);
```

Sets the pipeline for the following draws.

**Parameters**

- `pipeline` ([`GpuGraphicsPipeline`](GpuGraphicsPipeline.md))

### <a id="m-setpushconstants-uint-readonlyspan-byte"></a>SetPushConstants(uint, ReadOnlySpan&lt;byte&gt;)

```csharp
public abstract void SetPushConstants(uint offset, ReadOnlySpan<byte> data);
```

Writes push constants (up to the size the pipeline layout declared) visible to the following draws.

**Parameters**

- `offset` (`uint`)
- `data` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`)

### <a id="m-setscissor-int-int-uint-uint"></a>SetScissor(int, int, uint, uint)

```csharp
public abstract void SetScissor(int x, int y, uint width, uint height);
```

Sets the scissor rectangle in pixels.

**Parameters**

- `x` (`int`)
- `y` (`int`)
- `width` (`uint`)
- `height` (`uint`)

### <a id="m-setvertexbuffer-uint-gpubuffer-ulong"></a>SetVertexBuffer(uint, GpuBuffer, ulong)

```csharp
public abstract void SetVertexBuffer(uint slot, GpuBuffer buffer, ulong offset = 0);
```

Binds a vertex buffer to a vertex buffer slot.

**Parameters**

- `slot` (`uint`)
- `buffer` ([`GpuBuffer`](GpuBuffer.md))
- `offset` (`ulong`)

### <a id="m-setviewport-float-float-float-float-float-float"></a>SetViewport(float, float, float, float, float, float)

```csharp
public abstract void SetViewport(float x, float y, float width, float height, float minDepth = 0f, float maxDepth = 1f);
```

Sets the viewport in pixels. Clip space has +Y up and depth 0..1 on every backend.

**Parameters**

- `x` (`float`)
- `y` (`float`)
- `width` (`float`)
- `height` (`float`)
- `minDepth` (`float`)
- `maxDepth` (`float`)
