[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu.Graph

# PassBuilder class

**Namespace:** `OneGame.Gpu.Graph`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)

```csharp
public sealed class PassBuilder
```

Declares what one pass touches. Only valid inside [`IGraphPass.Setup()`](IGraphPass.md#m-setup-passbuilder) (or the setup delegate).

## Methods

| Member | Description |
| --- | --- |
| [`ColorTarget(GraphTexture, LoadOp, float, float, float, float, StoreOp, uint, uint, GraphTexture)`](#m-colortarget-graphtexture-loadop-float-float-float-float-storeop-uint-uint-graphtexture) | Renders into a color target (slot order is declaration order, up to 8). |
| [`Compute()`](#m-compute) | Makes this a compute pass: the graph opens a compute pass and hands it over as [`PassContext.Compute`](PassContext.md#m-compute). |
| [`CopyDestination(GraphBuffer)`](#m-copydestination-graphbuffer) | The pass copies into the buffer. |
| [`CopyDestination(GraphTexture)`](#m-copydestination-graphtexture) | The pass copies into the texture (all of it, or part: what was there before is kept). |
| [`CopySource(GraphTexture)`](#m-copysource-graphtexture) | The pass copies from the texture. |
| [`DepthTarget(GraphTexture, LoadOp, float, StoreOp, uint, uint)`](#m-depthtarget-graphtexture-loadop-float-storeop-uint-uint) | Renders with a depth target (one per pass). |
| [`Read(GraphTexture)`](#m-read-graphtexture) | The pass samples the texture in a shader. |
| [`Read(GraphBuffer, BufferRead)`](#m-read-graphbuffer-bufferread) | The pass reads the buffer in the given way. |
| [`ReadStorage(GraphBuffer)`](#m-readstorage-graphbuffer) | The pass reads the buffer as a storage buffer. |
| [`ReadStorage(GraphTexture)`](#m-readstorage-graphtexture) | The pass reads the texture as a storage image. |
| [`ReadWrite(GraphBuffer)`](#m-readwrite-graphbuffer) | A custom pass reads and modifies the buffer. |
| [`ReadWrite(GraphTexture)`](#m-readwrite-graphtexture) | A custom pass reads and modifies the texture (draws over it, generates its mips). |
| [`SideEffect()`](#m-sideeffect) | Marks the pass as one that must run even if nothing reads its results (readback, presentation, a debug overlay). |
| [`Write(GraphTexture)`](#m-write-graphtexture) | A custom pass writes every texel of the texture without reading it (for example, the 2D renderer clears the target, then draws). |
| [`WriteStorage(GraphBuffer)`](#m-writestorage-graphbuffer) | The pass writes the buffer as a storage buffer. |
| [`WriteStorage(GraphTexture)`](#m-writestorage-graphtexture) | The pass writes the texture as a storage image. |

### <a id="m-colortarget-graphtexture-loadop-float-float-float-float-storeop-uint-uint-graphtexture"></a>ColorTarget(GraphTexture, LoadOp, float, float, float, float, StoreOp, uint, uint, GraphTexture)

```csharp
public void ColorTarget(GraphTexture target, LoadOp load = 1, float r = 0f, float g = 0f, float b = 0f, float a = 1f, StoreOp store = 0, uint mip = 0, uint layer = 0, GraphTexture resolve = default);
```

Renders into a color target (slot order is declaration order, up to 8). With [`LoadOp.Load`](../OneGame.Gpu/LoadOp.md#m-load) the pass also reads the target, so an earlier pass must have written it.

**Parameters**

- `target` ([`GraphTexture`](GraphTexture.md)): The texture.
- `load` ([`LoadOp`](../OneGame.Gpu/LoadOp.md)): What happens to its contents when the pass starts.
- `r` (`float`): Clear red.
- `g` (`float`): Clear green.
- `b` (`float`): Clear blue.
- `a` (`float`): Clear alpha.
- `store` ([`StoreOp`](../OneGame.Gpu/StoreOp.md)): Whether the result is kept; [`StoreOp.Discard`](../OneGame.Gpu/StoreOp.md#m-discard) is for multisampled targets that are only resolved.
- `mip` (`uint`): Mip level rendered into.
- `layer` (`uint`): Array layer or cube face rendered into.
- `resolve` ([`GraphTexture`](GraphTexture.md)): A single-sample texture that receives the resolved image of a multisampled `target`.

### <a id="m-compute"></a>Compute()

```csharp
public void Compute();
```

Makes this a compute pass: the graph opens a compute pass and hands it over as [`PassContext.Compute`](PassContext.md#m-compute).

### <a id="m-copydestination-graphbuffer"></a>CopyDestination(GraphBuffer)

```csharp
public void CopyDestination(GraphBuffer buffer);
```

The pass copies into the buffer.

**Parameters**

- `buffer` ([`GraphBuffer`](GraphBuffer.md))

### <a id="m-copydestination-graphtexture"></a>CopyDestination(GraphTexture)

```csharp
public void CopyDestination(GraphTexture texture);
```

The pass copies into the texture (all of it, or part: what was there before is kept).

**Parameters**

- `texture` ([`GraphTexture`](GraphTexture.md))

### <a id="m-copysource-graphtexture"></a>CopySource(GraphTexture)

```csharp
public void CopySource(GraphTexture texture);
```

The pass copies from the texture.

**Parameters**

- `texture` ([`GraphTexture`](GraphTexture.md))

### <a id="m-depthtarget-graphtexture-loadop-float-storeop-uint-uint"></a>DepthTarget(GraphTexture, LoadOp, float, StoreOp, uint, uint)

```csharp
public void DepthTarget(GraphTexture target, LoadOp load = 1, float clearDepth = 1f, StoreOp store = 0, uint mip = 0, uint layer = 0);
```

Renders with a depth target (one per pass). With [`LoadOp.Load`](../OneGame.Gpu/LoadOp.md#m-load) the pass also reads it.

**Parameters**

- `target` ([`GraphTexture`](GraphTexture.md)): A depth texture.
- `load` ([`LoadOp`](../OneGame.Gpu/LoadOp.md)): What happens to its contents when the pass starts.
- `clearDepth` (`float`): Depth to clear to.
- `store` ([`StoreOp`](../OneGame.Gpu/StoreOp.md)): Whether the result is kept (shadow maps keep it; a scene's depth buffer may discard it).
- `mip` (`uint`): Mip level rendered into.
- `layer` (`uint`): Array layer rendered into (a shadow cascade).

### <a id="m-read-graphtexture"></a>Read(GraphTexture)

```csharp
public void Read(GraphTexture texture);
```

The pass samples the texture in a shader.

**Parameters**

- `texture` ([`GraphTexture`](GraphTexture.md))

### <a id="m-read-graphbuffer-bufferread"></a>Read(GraphBuffer, BufferRead)

```csharp
public void Read(GraphBuffer buffer, BufferRead how);
```

The pass reads the buffer in the given way.

**Parameters**

- `buffer` ([`GraphBuffer`](GraphBuffer.md))
- `how` ([`BufferRead`](BufferRead.md))

### <a id="m-readstorage-graphbuffer"></a>ReadStorage(GraphBuffer)

```csharp
public void ReadStorage(GraphBuffer buffer);
```

The pass reads the buffer as a storage buffer.

**Parameters**

- `buffer` ([`GraphBuffer`](GraphBuffer.md))

### <a id="m-readstorage-graphtexture"></a>ReadStorage(GraphTexture)

```csharp
public void ReadStorage(GraphTexture texture);
```

The pass reads the texture as a storage image.

**Parameters**

- `texture` ([`GraphTexture`](GraphTexture.md))

### <a id="m-readwrite-graphbuffer"></a>ReadWrite(GraphBuffer)

```csharp
public void ReadWrite(GraphBuffer buffer);
```

A custom pass reads and modifies the buffer.

**Parameters**

- `buffer` ([`GraphBuffer`](GraphBuffer.md))

### <a id="m-readwrite-graphtexture"></a>ReadWrite(GraphTexture)

```csharp
public void ReadWrite(GraphTexture texture);
```

A custom pass reads and modifies the texture (draws over it, generates its mips).

**Parameters**

- `texture` ([`GraphTexture`](GraphTexture.md))

### <a id="m-sideeffect"></a>SideEffect()

```csharp
public void SideEffect();
```

Marks the pass as one that must run even if nothing reads its results (readback, presentation, a debug overlay).

### <a id="m-write-graphtexture"></a>Write(GraphTexture)

```csharp
public void Write(GraphTexture texture);
```

A custom pass writes every texel of the texture without reading it (for example, the 2D renderer clears the target, then draws). Everything written before is dead.

**Parameters**

- `texture` ([`GraphTexture`](GraphTexture.md))

### <a id="m-writestorage-graphbuffer"></a>WriteStorage(GraphBuffer)

```csharp
public void WriteStorage(GraphBuffer buffer);
```

The pass writes the buffer as a storage buffer.

**Parameters**

- `buffer` ([`GraphBuffer`](GraphBuffer.md))

### <a id="m-writestorage-graphtexture"></a>WriteStorage(GraphTexture)

```csharp
public void WriteStorage(GraphTexture texture);
```

The pass writes the texture as a storage image. What was there before is kept for passes that only write part of it.

**Parameters**

- `texture` ([`GraphTexture`](GraphTexture.md))
