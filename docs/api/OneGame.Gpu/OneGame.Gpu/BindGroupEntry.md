[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# BindGroupEntry struct

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`BindGroupEntry`](BindGroupEntry.md)&gt;

```csharp
public readonly struct BindGroupEntry : IEquatable<BindGroupEntry>
```

One resource bound at a binding index of a bind group.

## Properties

| Member | Description |
| --- | --- |
| [`Binding`](#m-binding) | Binding index inside the group. |
| [`Buffer`](#m-buffer) | Bound buffer, for buffer kinds. |
| [`Buffers`](#m-buffers) | Buffers of a resource array; one per element, each visible from offset 0 for [`BindGroupEntry.Size`](BindGroupEntry.md#m-size) bytes. |
| [`Offset`](#m-offset) | Byte offset into [`BindGroupEntry.Buffer`](BindGroupEntry.md#m-buffer). |
| [`Sampler`](#m-sampler) | Bound sampler, for sampler kinds. |
| [`Samplers`](#m-samplers) | Samplers of a resource array; one per element. |
| [`Size`](#m-size) | Bytes visible to the shader; 0 means the rest of the buffer. |
| [`Texture`](#m-texture) | Bound texture, for texture kinds. |
| [`Textures`](#m-textures) | Textures of a resource array (a binding whose count is more than 1); one per element. |
| [`View`](#m-view) | Part of [`BindGroupEntry.Texture`](BindGroupEntry.md#m-texture) (or of each of [`BindGroupEntry.Textures`](BindGroupEntry.md#m-textures)) the shader sees; null means the whole texture. |

### <a id="m-binding"></a>Binding

```csharp
public uint Binding { get; init; }
```

Binding index inside the group.

**Type:** `uint`

### <a id="m-buffer"></a>Buffer

```csharp
public GpuBuffer? Buffer { get; init; }
```

Bound buffer, for buffer kinds.

**Type:** [`GpuBuffer`](GpuBuffer.md)?

### <a id="m-buffers"></a>Buffers

```csharp
public IReadOnlyList<GpuBuffer>? Buffers { get; init; }
```

Buffers of a resource array; one per element, each visible from offset 0 for [`BindGroupEntry.Size`](BindGroupEntry.md#m-size) bytes.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`GpuBuffer`](GpuBuffer.md)&gt;?

### <a id="m-offset"></a>Offset

```csharp
public ulong Offset { get; init; }
```

Byte offset into [`BindGroupEntry.Buffer`](BindGroupEntry.md#m-buffer).

**Type:** `ulong`

### <a id="m-sampler"></a>Sampler

```csharp
public GpuSampler? Sampler { get; init; }
```

Bound sampler, for sampler kinds.

**Type:** [`GpuSampler`](GpuSampler.md)?

### <a id="m-samplers"></a>Samplers

```csharp
public IReadOnlyList<GpuSampler>? Samplers { get; init; }
```

Samplers of a resource array; one per element.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`GpuSampler`](GpuSampler.md)&gt;?

### <a id="m-size"></a>Size

```csharp
public ulong Size { get; init; }
```

Bytes visible to the shader; 0 means the rest of the buffer.

**Type:** `ulong`

### <a id="m-texture"></a>Texture

```csharp
public GpuTexture? Texture { get; init; }
```

Bound texture, for texture kinds.

**Type:** [`GpuTexture`](GpuTexture.md)?

### <a id="m-textures"></a>Textures

```csharp
public IReadOnlyList<GpuTexture>? Textures { get; init; }
```

Textures of a resource array (a binding whose count is more than 1); one per element.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`GpuTexture`](GpuTexture.md)&gt;?

### <a id="m-view"></a>View

```csharp
public TextureViewDesc? View { get; init; }
```

Part of [`BindGroupEntry.Texture`](BindGroupEntry.md#m-texture) (or of each of [`BindGroupEntry.Textures`](BindGroupEntry.md#m-textures)) the shader sees; null means the whole texture.

**Type:** [`TextureViewDesc`](TextureViewDesc.md)?

## Methods

| Member | Description |
| --- | --- |
| [`ForBuffer(uint, GpuBuffer)`](#m-forbuffer-uint-gpubuffer) | Binds a whole buffer. |
| [`ForBuffer(uint, GpuBuffer, ulong, ulong)`](#m-forbuffer-uint-gpubuffer-ulong-ulong) | Binds a range of a buffer. |
| [`ForBuffers(uint, IReadOnlyList<GpuBuffer>, ulong)`](#m-forbuffers-uint-ireadonlylist-gpubuffer-ulong) | Binds an array of buffers, each seen from offset 0 for `size` bytes (0 means all). |
| [`ForSampler(uint, GpuSampler)`](#m-forsampler-uint-gpusampler) | Binds a sampler. |
| [`ForSamplers(uint, IReadOnlyList<GpuSampler>)`](#m-forsamplers-uint-ireadonlylist-gpusampler) | Binds an array of samplers; the count must equal the binding's declared count. |
| [`ForTexture(uint, GpuTexture)`](#m-fortexture-uint-gputexture) | Binds a texture. |
| [`ForTexture(uint, GpuTexture, in TextureViewDesc)`](#m-fortexture-uint-gputexture-textureviewdesc) | Binds part of a texture: chosen mip levels or layers, or a different presentation. |
| [`ForTextures(uint, IReadOnlyList<GpuTexture>)`](#m-fortextures-uint-ireadonlylist-gputexture) | Binds an array of textures; the count must equal the binding's declared count. |

### <a id="m-forbuffer-uint-gpubuffer"></a>ForBuffer(uint, GpuBuffer)

```csharp
public static BindGroupEntry ForBuffer(uint binding, GpuBuffer buffer);
```

Binds a whole buffer.

**Parameters**

- `binding` (`uint`)
- `buffer` ([`GpuBuffer`](GpuBuffer.md))

**Returns:** [`BindGroupEntry`](BindGroupEntry.md)

### <a id="m-forbuffer-uint-gpubuffer-ulong-ulong"></a>ForBuffer(uint, GpuBuffer, ulong, ulong)

```csharp
public static BindGroupEntry ForBuffer(uint binding, GpuBuffer buffer, ulong offset, ulong size);
```

Binds a range of a buffer.

**Parameters**

- `binding` (`uint`)
- `buffer` ([`GpuBuffer`](GpuBuffer.md))
- `offset` (`ulong`)
- `size` (`ulong`)

**Returns:** [`BindGroupEntry`](BindGroupEntry.md)

### <a id="m-forbuffers-uint-ireadonlylist-gpubuffer-ulong"></a>ForBuffers(uint, IReadOnlyList&lt;GpuBuffer&gt;, ulong)

```csharp
public static BindGroupEntry ForBuffers(uint binding, IReadOnlyList<GpuBuffer> buffers, ulong size = 0);
```

Binds an array of buffers, each seen from offset 0 for `size` bytes (0 means all).

**Parameters**

- `binding` (`uint`)
- `buffers` ([`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`GpuBuffer`](GpuBuffer.md)&gt;)
- `size` (`ulong`)

**Returns:** [`BindGroupEntry`](BindGroupEntry.md)

### <a id="m-forsampler-uint-gpusampler"></a>ForSampler(uint, GpuSampler)

```csharp
public static BindGroupEntry ForSampler(uint binding, GpuSampler sampler);
```

Binds a sampler.

**Parameters**

- `binding` (`uint`)
- `sampler` ([`GpuSampler`](GpuSampler.md))

**Returns:** [`BindGroupEntry`](BindGroupEntry.md)

### <a id="m-forsamplers-uint-ireadonlylist-gpusampler"></a>ForSamplers(uint, IReadOnlyList&lt;GpuSampler&gt;)

```csharp
public static BindGroupEntry ForSamplers(uint binding, IReadOnlyList<GpuSampler> samplers);
```

Binds an array of samplers; the count must equal the binding's declared count.

**Parameters**

- `binding` (`uint`)
- `samplers` ([`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`GpuSampler`](GpuSampler.md)&gt;)

**Returns:** [`BindGroupEntry`](BindGroupEntry.md)

### <a id="m-fortexture-uint-gputexture"></a>ForTexture(uint, GpuTexture)

```csharp
public static BindGroupEntry ForTexture(uint binding, GpuTexture texture);
```

Binds a texture.

**Parameters**

- `binding` (`uint`)
- `texture` ([`GpuTexture`](GpuTexture.md))

**Returns:** [`BindGroupEntry`](BindGroupEntry.md)

### <a id="m-fortexture-uint-gputexture-textureviewdesc"></a>ForTexture(uint, GpuTexture, in TextureViewDesc)

```csharp
public static BindGroupEntry ForTexture(uint binding, GpuTexture texture, in TextureViewDesc view);
```

Binds part of a texture: chosen mip levels or layers, or a different presentation.

**Parameters**

- `binding` (`uint`)
- `texture` ([`GpuTexture`](GpuTexture.md))
- `view` ([`TextureViewDesc`](TextureViewDesc.md))

**Returns:** [`BindGroupEntry`](BindGroupEntry.md)

### <a id="m-fortextures-uint-ireadonlylist-gputexture"></a>ForTextures(uint, IReadOnlyList&lt;GpuTexture&gt;)

```csharp
public static BindGroupEntry ForTextures(uint binding, IReadOnlyList<GpuTexture> textures);
```

Binds an array of textures; the count must equal the binding's declared count.

**Parameters**

- `binding` (`uint`)
- `textures` ([`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`GpuTexture`](GpuTexture.md)&gt;)

**Returns:** [`BindGroupEntry`](BindGroupEntry.md)
