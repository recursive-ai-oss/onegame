[API reference](../../index.md) › [OneGame.Text](../index.md) › OneGame.Text

# GlyphLimits class

**Namespace:** `OneGame.Text`  
**Package:** [`RecursiveAI.OneGame.Text`](../index.md)

```csharp
public sealed class GlyphLimits
```

Budgets for glyph rasterization.

## Constructors

| Member | Description |
| --- | --- |
| [`GlyphLimits()`](#m-ctor) |  |

### <a id="m-ctor"></a>GlyphLimits()

```csharp
public GlyphLimits();
```

## Properties

| Member | Description |
| --- | --- |
| [`MaxBitmapDimension`](#m-maxbitmapdimension) | Gets the largest glyph bitmap edge in pixels. |
| [`MaxBitmapPixels`](#m-maxbitmappixels) | Gets the largest glyph bitmap in pixels. |
| [`MaxFontSize`](#m-maxfontsize) | Gets the largest font size that is rasterized, in pixels. |

### <a id="m-maxbitmapdimension"></a>MaxBitmapDimension

```csharp
public int MaxBitmapDimension { get; init; }
```

Gets the largest glyph bitmap edge in pixels. Default 640.

**Type:** `int`

### <a id="m-maxbitmappixels"></a>MaxBitmapPixels

```csharp
public int MaxBitmapPixels { get; init; }
```

Gets the largest glyph bitmap in pixels. Default 262,144.

**Type:** `int`

### <a id="m-maxfontsize"></a>MaxFontSize

```csharp
public float MaxFontSize { get; init; }
```

Gets the largest font size that is rasterized, in pixels. Default 512.

**Type:** `float`
