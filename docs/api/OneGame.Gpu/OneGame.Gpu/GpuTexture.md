[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# GpuTexture class

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)  
**Inherits:** [`GpuObject`](GpuObject.md)

```csharp
public abstract class GpuTexture : GpuObject
```

A 2D image, 2D array, cube map, cube array or 3D volume.

## Constructors

| Member | Description |
| --- | --- |
| [`GpuTexture()`](#m-ctor) |  |

### <a id="m-ctor"></a>GpuTexture()

```csharp
protected GpuTexture();
```

## Properties

| Member | Description |
| --- | --- |
| [`ArrayLayers`](#m-arraylayers) | Number of array layers (6 per cube for cube textures). |
| [`Depth`](#m-depth) | Depth in texels of a 3D texture; 1 otherwise. |
| [`Dimension`](#m-dimension) | Shape of the texture. |
| [`Format`](#m-format) | Pixel format. |
| [`Height`](#m-height) | Height in texels. |
| [`MipLevels`](#m-miplevels) | Number of mip levels. |
| [`SampleCount`](#m-samplecount) | Samples per texel. |
| [`Usage`](#m-usage) | Allowed uses. |
| [`Width`](#m-width) | Width in texels. |

### <a id="m-arraylayers"></a>ArrayLayers

```csharp
public uint ArrayLayers { get; protected init; }
```

Number of array layers (6 per cube for cube textures).

**Type:** `uint`

### <a id="m-depth"></a>Depth

```csharp
public uint Depth { get; protected init; }
```

Depth in texels of a 3D texture; 1 otherwise.

**Type:** `uint`

### <a id="m-dimension"></a>Dimension

```csharp
public TextureDimension Dimension { get; protected init; }
```

Shape of the texture.

**Type:** [`TextureDimension`](TextureDimension.md)

### <a id="m-format"></a>Format

```csharp
public GpuFormat Format { get; protected init; }
```

Pixel format.

**Type:** [`GpuFormat`](GpuFormat.md)

### <a id="m-height"></a>Height

```csharp
public uint Height { get; protected init; }
```

Height in texels.

**Type:** `uint`

### <a id="m-miplevels"></a>MipLevels

```csharp
public uint MipLevels { get; protected init; }
```

Number of mip levels.

**Type:** `uint`

### <a id="m-samplecount"></a>SampleCount

```csharp
public uint SampleCount { get; protected init; }
```

Samples per texel.

**Type:** `uint`

### <a id="m-usage"></a>Usage

```csharp
public TextureUsage Usage { get; protected init; }
```

Allowed uses.

**Type:** [`TextureUsage`](TextureUsage.md)

### <a id="m-width"></a>Width

```csharp
public uint Width { get; protected init; }
```

Width in texels.

**Type:** `uint`

## Methods

| Member | Description |
| --- | --- |
| [`FullMipCount(uint, uint)`](#m-fullmipcount-uint-uint) | Number of levels in a full mip chain for the given size. |
| [`FullMipCount(uint, uint, uint)`](#m-fullmipcount-uint-uint-uint) | Number of levels in a full mip chain for a volume of the given size. |

### <a id="m-fullmipcount-uint-uint"></a>FullMipCount(uint, uint)

```csharp
public static uint FullMipCount(uint width, uint height);
```

Number of levels in a full mip chain for the given size.

**Parameters**

- `width` (`uint`)
- `height` (`uint`)

**Returns:** `uint`

### <a id="m-fullmipcount-uint-uint-uint"></a>FullMipCount(uint, uint, uint)

```csharp
public static uint FullMipCount(uint width, uint height, uint depth);
```

Number of levels in a full mip chain for a volume of the given size.

**Parameters**

- `width` (`uint`)
- `height` (`uint`)
- `depth` (`uint`)

**Returns:** `uint`
