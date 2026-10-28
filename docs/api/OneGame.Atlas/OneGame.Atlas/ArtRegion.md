[API reference](../../index.md) › [OneGame.Atlas](../index.md) › OneGame.Atlas

# ArtRegion struct

**Namespace:** `OneGame.Atlas`  
**Package:** [`RecursiveAI.OneGame.Atlas`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ArtRegion`](ArtRegion.md)&gt;

```csharp
public readonly struct ArtRegion : IEquatable<ArtRegion>
```

Where one rasterized SVG or image sits in the atlas.

## Constructors

| Member | Description |
| --- | --- |
| [`ArtRegion(int, int, int, int, int, float, float, float, float, NineSlice, int)`](#m-ctor-int-int-int-int-int-float-float-float-float-nineslice-int) | Where one rasterized SVG or image sits in the atlas. |

### <a id="m-ctor-int-int-int-int-int-float-float-float-float-nineslice-int"></a>ArtRegion(int, int, int, int, int, float, float, float, float, NineSlice, int)

```csharp
public ArtRegion(int Page, int X, int Y, int Width, int Height, float U0, float V0, float U1, float V1, NineSlice Slice, int Standalone = 0);
```

Where one rasterized SVG or image sits in the atlas.

**Parameters**

- `Page` (`int`): Page index.
- `X` (`int`): Left edge in page pixels.
- `Y` (`int`): Top edge in page pixels.
- `Width` (`int`): Width in pixels.
- `Height` (`int`): Height in pixels.
- `U0` (`float`): Left texture coordinate (X / page width).
- `V0` (`float`): Top texture coordinate (Y / page height).
- `U1` (`float`): Right texture coordinate.
- `V1` (`float`): Bottom texture coordinate.
- `Slice` ([`NineSlice`](NineSlice.md)): Nine-slice insets in pixels of this raster (the registered insets scaled to this size); zero when not sliced.
- `Standalone` (`int`): Zero when the raster sits on page `Page`. Otherwise the handle of a standalone texture (see [`ArtAtlas.TryGetStandalone()`](ArtAtlas.md#m-trygetstandalone-int-standaloneart)): `Page` is -1, the rectangle is the whole texture and the texture coordinates run 0 to 1.

## Properties

| Member | Description |
| --- | --- |
| [`Height`](#m-height) | Height in pixels. |
| [`Page`](#m-page) | Page index. |
| [`Slice`](#m-slice) | Nine-slice insets in pixels of this raster (the registered insets scaled to this size); zero when not sliced. |
| [`Standalone`](#m-standalone) | Zero when the raster sits on page `Page`. |
| [`U0`](#m-u0) | Left texture coordinate (X / page width). |
| [`U1`](#m-u1) | Right texture coordinate. |
| [`V0`](#m-v0) | Top texture coordinate (Y / page height). |
| [`V1`](#m-v1) | Bottom texture coordinate. |
| [`Width`](#m-width) | Width in pixels. |
| [`X`](#m-x) | Left edge in page pixels. |
| [`Y`](#m-y) | Top edge in page pixels. |

### <a id="m-height"></a>Height

```csharp
public int Height { get; init; }
```

Height in pixels.

**Type:** `int`

### <a id="m-page"></a>Page

```csharp
public int Page { get; init; }
```

Page index.

**Type:** `int`

### <a id="m-slice"></a>Slice

```csharp
public NineSlice Slice { get; init; }
```

Nine-slice insets in pixels of this raster (the registered insets scaled to this size); zero when not sliced.

**Type:** [`NineSlice`](NineSlice.md)

### <a id="m-standalone"></a>Standalone

```csharp
public int Standalone { get; init; }
```

Zero when the raster sits on page `Page`. Otherwise the handle of a standalone texture (see [`ArtAtlas.TryGetStandalone()`](ArtAtlas.md#m-trygetstandalone-int-standaloneart)): `Page` is -1, the rectangle is the whole texture and the texture coordinates run 0 to 1.

**Type:** `int`

### <a id="m-u0"></a>U0

```csharp
public float U0 { get; init; }
```

Left texture coordinate (X / page width).

**Type:** `float`

### <a id="m-u1"></a>U1

```csharp
public float U1 { get; init; }
```

Right texture coordinate.

**Type:** `float`

### <a id="m-v0"></a>V0

```csharp
public float V0 { get; init; }
```

Top texture coordinate (Y / page height).

**Type:** `float`

### <a id="m-v1"></a>V1

```csharp
public float V1 { get; init; }
```

Bottom texture coordinate.

**Type:** `float`

### <a id="m-width"></a>Width

```csharp
public int Width { get; init; }
```

Width in pixels.

**Type:** `int`

### <a id="m-x"></a>X

```csharp
public int X { get; init; }
```

Left edge in page pixels.

**Type:** `int`

### <a id="m-y"></a>Y

```csharp
public int Y { get; init; }
```

Top edge in page pixels.

**Type:** `int`
