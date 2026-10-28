[API reference](../../index.md) › [OneGame.Text](../index.md) › OneGame.Text

# PositionedGlyph struct

**Namespace:** `OneGame.Text`  
**Package:** [`RecursiveAI.OneGame.Text`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`PositionedGlyph`](PositionedGlyph.md)&gt;

```csharp
public readonly struct PositionedGlyph : IEquatable<PositionedGlyph>
```

One glyph with its pen position, ready to look up in the glyph atlas.

## Constructors

| Member | Description |
| --- | --- |
| [`PositionedGlyph(int, int, float, float, float)`](#m-ctor-int-int-float-float-float) | One glyph with its pen position, ready to look up in the glyph atlas. |

### <a id="m-ctor-int-int-float-float-float"></a>PositionedGlyph(int, int, float, float, float)

```csharp
public PositionedGlyph(int FaceId, int GlyphId, float X, float Y, float Advance);
```

One glyph with its pen position, ready to look up in the glyph atlas.

**Parameters**

- `FaceId` (`int`): Face id in the [`FontLibrary`](FontLibrary.md).
- `GlyphId` (`int`): Glyph index in that face.
- `X` (`float`): Horizontal origin in pixels, from the left end of the line.
- `Y` (`float`): Vertical origin in pixels from the baseline, y down (shaper offsets only; usually 0).
- `Advance` (`float`): How far the pen moves after this glyph, in pixels (letter spacing is added once per character cluster, not included here).

## Properties

| Member | Description |
| --- | --- |
| [`Advance`](#m-advance) | How far the pen moves after this glyph, in pixels (letter spacing is added once per character cluster, not included here). |
| [`FaceId`](#m-faceid) | Face id in the [`FontLibrary`](FontLibrary.md). |
| [`GlyphId`](#m-glyphid) | Glyph index in that face. |
| [`X`](#m-x) | Horizontal origin in pixels, from the left end of the line. |
| [`Y`](#m-y) | Vertical origin in pixels from the baseline, y down (shaper offsets only; usually 0). |

### <a id="m-advance"></a>Advance

```csharp
public float Advance { get; init; }
```

How far the pen moves after this glyph, in pixels (letter spacing is added once per character cluster, not included here).

**Type:** `float`

### <a id="m-faceid"></a>FaceId

```csharp
public int FaceId { get; init; }
```

Face id in the [`FontLibrary`](FontLibrary.md).

**Type:** `int`

### <a id="m-glyphid"></a>GlyphId

```csharp
public int GlyphId { get; init; }
```

Glyph index in that face.

**Type:** `int`

### <a id="m-x"></a>X

```csharp
public float X { get; init; }
```

Horizontal origin in pixels, from the left end of the line.

**Type:** `float`

### <a id="m-y"></a>Y

```csharp
public float Y { get; init; }
```

Vertical origin in pixels from the baseline, y down (shaper offsets only; usually 0).

**Type:** `float`
