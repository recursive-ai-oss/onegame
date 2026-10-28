[API reference](../../index.md) › [OneGame.Text](../index.md) › OneGame.Text

# GlyphInfo struct

**Namespace:** `OneGame.Text`  
**Package:** [`RecursiveAI.OneGame.Text`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`GlyphInfo`](GlyphInfo.md)&gt;

```csharp
public readonly struct GlyphInfo : IEquatable<GlyphInfo>
```

Where one rasterized glyph sits in the atlas, and how to place it. A glyph with no ink (a space) has [`GlyphInfo.Width`](GlyphInfo.md#m-width) 0 and no atlas entry.

## Constructors

| Member | Description |
| --- | --- |
| [`GlyphInfo(int, int, int, int, int, float, float, float, float, int, int)`](#m-ctor-int-int-int-int-int-float-float-float-float-int-int) | Where one rasterized glyph sits in the atlas, and how to place it. |

### <a id="m-ctor-int-int-int-int-int-float-float-float-float-int-int"></a>GlyphInfo(int, int, int, int, int, float, float, float, float, int, int)

```csharp
public GlyphInfo(int Page, int X, int Y, int Width, int Height, float U0, float V0, float U1, float V1, int OffsetX, int OffsetY);
```

Where one rasterized glyph sits in the atlas, and how to place it. A glyph with no ink (a space) has [`GlyphInfo.Width`](GlyphInfo.md#m-width) 0 and no atlas entry.

**Parameters**

- `Page` (`int`): Page index.
- `X` (`int`): Left edge in page pixels.
- `Y` (`int`): Top edge in page pixels.
- `Width` (`int`): Bitmap width in pixels.
- `Height` (`int`): Bitmap height in pixels.
- `U0` (`float`): Left texture coordinate.
- `V0` (`float`): Top texture coordinate.
- `U1` (`float`): Right texture coordinate.
- `V1` (`float`): Bottom texture coordinate.
- `OffsetX` (`int`): Distance from the whole-pixel pen position to the bitmap's left edge (the subpixel shift is already inside the bitmap).
- `OffsetY` (`int`): Distance from the baseline to the bitmap's top edge, y down (negative above the baseline).

## Properties

| Member | Description |
| --- | --- |
| [`Height`](#m-height) | Bitmap height in pixels. |
| [`OffsetX`](#m-offsetx) | Distance from the whole-pixel pen position to the bitmap's left edge (the subpixel shift is already inside the bitmap). |
| [`OffsetY`](#m-offsety) | Distance from the baseline to the bitmap's top edge, y down (negative above the baseline). |
| [`Page`](#m-page) | Page index. |
| [`U0`](#m-u0) | Left texture coordinate. |
| [`U1`](#m-u1) | Right texture coordinate. |
| [`V0`](#m-v0) | Top texture coordinate. |
| [`V1`](#m-v1) | Bottom texture coordinate. |
| [`Width`](#m-width) | Bitmap width in pixels. |
| [`X`](#m-x) | Left edge in page pixels. |
| [`Y`](#m-y) | Top edge in page pixels. |

### <a id="m-height"></a>Height

```csharp
public int Height { get; init; }
```

Bitmap height in pixels.

**Type:** `int`

### <a id="m-offsetx"></a>OffsetX

```csharp
public int OffsetX { get; init; }
```

Distance from the whole-pixel pen position to the bitmap's left edge (the subpixel shift is already inside the bitmap).

**Type:** `int`

### <a id="m-offsety"></a>OffsetY

```csharp
public int OffsetY { get; init; }
```

Distance from the baseline to the bitmap's top edge, y down (negative above the baseline).

**Type:** `int`

### <a id="m-page"></a>Page

```csharp
public int Page { get; init; }
```

Page index.

**Type:** `int`

### <a id="m-u0"></a>U0

```csharp
public float U0 { get; init; }
```

Left texture coordinate.

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

Top texture coordinate.

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

Bitmap width in pixels.

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
