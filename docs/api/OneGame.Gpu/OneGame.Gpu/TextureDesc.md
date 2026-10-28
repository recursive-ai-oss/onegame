[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# TextureDesc struct

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`TextureDesc`](TextureDesc.md)&gt;

```csharp
public readonly struct TextureDesc : IEquatable<TextureDesc>
```

Describes a texture to create: 2D, 2D array, cube, cube array or 3D.

## Constructors

| Member | Description |
| --- | --- |
| [`TextureDesc(uint, uint, GpuFormat, TextureUsage)`](#m-ctor-uint-uint-gpuformat-textureusage) | Describes a texture to create: 2D, 2D array, cube, cube array or 3D. |

### <a id="m-ctor-uint-uint-gpuformat-textureusage"></a>TextureDesc(uint, uint, GpuFormat, TextureUsage)

```csharp
public TextureDesc(uint Width, uint Height, GpuFormat Format, TextureUsage Usage);
```

Describes a texture to create: 2D, 2D array, cube, cube array or 3D.

**Parameters**

- `Width` (`uint`): Width of mip level 0 in texels; at least 1.
- `Height` (`uint`): Height of mip level 0 in texels; at least 1.
- `Format` ([`GpuFormat`](GpuFormat.md)): Pixel format. The `Srgb` formats decode to linear light when sampled and encode when rendered into; the others store values as they are.
- `Usage` ([`TextureUsage`](TextureUsage.md)): Every use the texture will have, combined: a sampled image you upload to is `Sampled | CopyDestination`; an offscreen target you read back is `RenderTarget | CopySource`.

## Properties

| Member | Description |
| --- | --- |
| [`ArrayLayers`](#m-arraylayers) | Number of array layers. |
| [`Depth`](#m-depth) | Depth in texels of a 3D texture; 1 otherwise. |
| [`Dimension`](#m-dimension) | Shape of the texture; 2D by default. |
| [`Format`](#m-format) | Pixel format. |
| [`Height`](#m-height) | Height of mip level 0 in texels; at least 1. |
| [`Label`](#m-label) | Optional name shown in debugging tools. |
| [`MipLevels`](#m-miplevels) | Number of mip levels; 1 by default. |
| [`SampleCount`](#m-samplecount) | Samples per texel for multisampled render targets (1, 2, 4 or 8). |
| [`Usage`](#m-usage) | Every use the texture will have, combined: a sampled image you upload to is `Sampled \| CopyDestination`; an offscreen target you read back is `RenderTarget \| CopySource`. |
| [`Width`](#m-width) | Width of mip level 0 in texels; at least 1. |

### <a id="m-arraylayers"></a>ArrayLayers

```csharp
public uint ArrayLayers { get; init; }
```

Number of array layers. More than 1 makes a 2D texture an array that shaders sample as a 2D array. A cube texture has a multiple of 6 (6 faces per cube).

**Type:** `uint`

### <a id="m-depth"></a>Depth

```csharp
public uint Depth { get; init; }
```

Depth in texels of a 3D texture; 1 otherwise.

**Type:** `uint`

### <a id="m-dimension"></a>Dimension

```csharp
public TextureDimension Dimension { get; init; }
```

Shape of the texture; 2D by default.

**Type:** [`TextureDimension`](TextureDimension.md)

### <a id="m-format"></a>Format

```csharp
public GpuFormat Format { get; init; }
```

Pixel format. The `Srgb` formats decode to linear light when sampled and encode when rendered into; the others store values as they are.

**Type:** [`GpuFormat`](GpuFormat.md)

### <a id="m-height"></a>Height

```csharp
public uint Height { get; init; }
```

Height of mip level 0 in texels; at least 1.

**Type:** `uint`

### <a id="m-label"></a>Label

```csharp
public string? Label { get; init; }
```

Optional name shown in debugging tools.

**Type:** `string?`

### <a id="m-miplevels"></a>MipLevels

```csharp
public uint MipLevels { get; init; }
```

Number of mip levels; 1 by default. [`GpuTexture.FullMipCount()`](GpuTexture.md#m-fullmipcount-uint-uint) computes the full chain.

**Type:** `uint`

### <a id="m-samplecount"></a>SampleCount

```csharp
public uint SampleCount { get; init; }
```

Samples per texel for multisampled render targets (1, 2, 4 or 8).

**Type:** `uint`

### <a id="m-usage"></a>Usage

```csharp
public TextureUsage Usage { get; init; }
```

Every use the texture will have, combined: a sampled image you upload to is `Sampled | CopyDestination`; an offscreen target you read back is `RenderTarget | CopySource`.

**Type:** [`TextureUsage`](TextureUsage.md)

### <a id="m-width"></a>Width

```csharp
public uint Width { get; init; }
```

Width of mip level 0 in texels; at least 1.

**Type:** `uint`
