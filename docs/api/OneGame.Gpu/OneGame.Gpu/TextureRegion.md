[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# TextureRegion struct

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`TextureRegion`](TextureRegion.md)&gt;

```csharp
public readonly struct TextureRegion : IEquatable<TextureRegion>
```

Where a texture region is: a mip level, a layer, a box inside it.

## Constructors

| Member | Description |
| --- | --- |
| [`TextureRegion(uint, uint, uint, uint, uint, uint, uint, uint)`](#m-ctor-uint-uint-uint-uint-uint-uint-uint-uint) | Where a texture region is: a mip level, a layer, a box inside it. |

### <a id="m-ctor-uint-uint-uint-uint-uint-uint-uint-uint"></a>TextureRegion(uint, uint, uint, uint, uint, uint, uint, uint)

```csharp
public TextureRegion(uint MipLevel, uint Layer, uint X, uint Y, uint Z, uint Width, uint Height, uint Depth = 1);
```

Where a texture region is: a mip level, a layer, a box inside it.

**Parameters**

- `MipLevel` (`uint`): Mip level.
- `Layer` (`uint`): Array layer (cube face index for cube textures); 0 for 3D textures.
- `X` (`uint`): Left edge in texels.
- `Y` (`uint`): Top edge in texels.
- `Z` (`uint`): Front slice of a 3D texture; 0 otherwise.
- `Width` (`uint`): Width in texels.
- `Height` (`uint`): Height in texels.
- `Depth` (`uint`): Slices of a 3D texture; 1 otherwise.

## Properties

| Member | Description |
| --- | --- |
| [`Depth`](#m-depth) | Slices of a 3D texture; 1 otherwise. |
| [`Height`](#m-height) | Height in texels. |
| [`Layer`](#m-layer) | Array layer (cube face index for cube textures); 0 for 3D textures. |
| [`MipLevel`](#m-miplevel) | Mip level. |
| [`Width`](#m-width) | Width in texels. |
| [`X`](#m-x) | Left edge in texels. |
| [`Y`](#m-y) | Top edge in texels. |
| [`Z`](#m-z) | Front slice of a 3D texture; 0 otherwise. |

### <a id="m-depth"></a>Depth

```csharp
public uint Depth { get; init; }
```

Slices of a 3D texture; 1 otherwise.

**Type:** `uint`

### <a id="m-height"></a>Height

```csharp
public uint Height { get; init; }
```

Height in texels.

**Type:** `uint`

### <a id="m-layer"></a>Layer

```csharp
public uint Layer { get; init; }
```

Array layer (cube face index for cube textures); 0 for 3D textures.

**Type:** `uint`

### <a id="m-miplevel"></a>MipLevel

```csharp
public uint MipLevel { get; init; }
```

Mip level.

**Type:** `uint`

### <a id="m-width"></a>Width

```csharp
public uint Width { get; init; }
```

Width in texels.

**Type:** `uint`

### <a id="m-x"></a>X

```csharp
public uint X { get; init; }
```

Left edge in texels.

**Type:** `uint`

### <a id="m-y"></a>Y

```csharp
public uint Y { get; init; }
```

Top edge in texels.

**Type:** `uint`

### <a id="m-z"></a>Z

```csharp
public uint Z { get; init; }
```

Front slice of a 3D texture; 0 otherwise.

**Type:** `uint`

## Methods

| Member | Description |
| --- | --- |
| [`Rect(uint, uint, uint, uint, uint, uint)`](#m-rect-uint-uint-uint-uint-uint-uint) | A 2D rectangle of one layer. |

### <a id="m-rect-uint-uint-uint-uint-uint-uint"></a>Rect(uint, uint, uint, uint, uint, uint)

```csharp
public static TextureRegion Rect(uint mip, uint layer, uint x, uint y, uint width, uint height);
```

A 2D rectangle of one layer.

**Parameters**

- `mip` (`uint`)
- `layer` (`uint`)
- `x` (`uint`)
- `y` (`uint`)
- `width` (`uint`)
- `height` (`uint`)

**Returns:** [`TextureRegion`](TextureRegion.md)
