[API reference](../../index.md) › [OneGame.UI](../index.md) › OneGame.UI

# DrawCommand struct

**Namespace:** `OneGame.UI`  
**Package:** [`RecursiveAI.OneGame.UI`](../index.md)

```csharp
public struct DrawCommand
```

One drawing instruction. Which fields matter depends on [`DrawCommand.Kind`](DrawCommand.md#m-kind); the others are default.

## Properties

| Member | Description |
| --- | --- |
| [`Bounds`](#m-bounds) | The rectangle in screen pixels, in the coordinates in effect before the current transform. |
| [`Color`](#m-color) | The fill, stroke, tint, shadow or text color. |
| [`Kind`](#m-kind) | What to draw. |
| [`Offset`](#m-offset) | Shadow offset. |
| [`Radii`](#m-radii) | Corner radii of [`DrawCommand.Bounds`](DrawCommand.md#m-bounds). |
| [`Repeat`](#m-repeat) | For an image with a [`DrawCommand.TileSize`](DrawCommand.md#m-tilesize): the axes along which the tile repeats. |
| [`Resource`](#m-resource) | The asset id of an image or SVG. |
| [`Slice`](#m-slice) | Nine-slice insets of an image. |
| [`Source`](#m-source) | The element that produced the command, for debugging and capture annotations. |
| [`Spread`](#m-spread) | Shadow spread in pixels. |
| [`TextRun`](#m-textrun) | Index into [`DisplayList.TextRuns`](DisplayList.md#m-textruns). |
| [`TileOrigin`](#m-tileorigin) | For an image with a [`DrawCommand.TileSize`](DrawCommand.md#m-tilesize): the top-left corner in pixels of the tile at index (0, 0). |
| [`TileSize`](#m-tilesize) | For an image: the size in pixels of one tile (or of the single placed image). |
| [`Transform`](#m-transform) | The 2D transform of [`DrawKind.PushTransform`](DrawKind.md#m-pushtransform), applied to row vectors (x, y, 1). |
| [`Value`](#m-value) | Shadow blur radius in pixels, or the opacity (0..1) of a group. |
| [`Widths`](#m-widths) | Border thickness per side. |

### <a id="m-bounds"></a>Bounds

```csharp
public Rect Bounds { get; init; }
```

The rectangle in screen pixels, in the coordinates in effect before the current transform.

**Type:** [`Rect`](Rect.md)

### <a id="m-color"></a>Color

```csharp
public Color Color { get; init; }
```

The fill, stroke, tint, shadow or text color.

**Type:** [`Color`](Color.md)

### <a id="m-kind"></a>Kind

```csharp
public DrawKind Kind { get; init; }
```

What to draw.

**Type:** [`DrawKind`](DrawKind.md)

### <a id="m-offset"></a>Offset

```csharp
public Vector2 Offset { get; init; }
```

Shadow offset.

**Type:** [`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="m-radii"></a>Radii

```csharp
public CornerRadii Radii { get; init; }
```

Corner radii of [`DrawCommand.Bounds`](DrawCommand.md#m-bounds).

**Type:** [`CornerRadii`](CornerRadii.md)

### <a id="m-repeat"></a>Repeat

```csharp
public TileAxes Repeat { get; init; }
```

For an image with a [`DrawCommand.TileSize`](DrawCommand.md#m-tilesize): the axes along which the tile repeats. Where it does not repeat, nothing is drawn outside the one tile.

**Type:** [`TileAxes`](TileAxes.md)

### <a id="m-resource"></a>Resource

```csharp
public string? Resource { get; init; }
```

The asset id of an image or SVG.

**Type:** `string?`

### <a id="m-slice"></a>Slice

```csharp
public Edges Slice { get; init; }
```

Nine-slice insets of an image.

**Type:** [`Edges`](Edges.md)

### <a id="m-source"></a>Source

```csharp
public Element? Source { get; init; }
```

The element that produced the command, for debugging and capture annotations. Renderers ignore it.

**Type:** [`Element`](Element.md)?

### <a id="m-spread"></a>Spread

```csharp
public float Spread { get; init; }
```

Shadow spread in pixels.

**Type:** `float`

### <a id="m-textrun"></a>TextRun

```csharp
public int TextRun { get; init; }
```

Index into [`DisplayList.TextRuns`](DisplayList.md#m-textruns).

**Type:** `int`

### <a id="m-tileorigin"></a>TileOrigin

```csharp
public Vector2 TileOrigin { get; init; }
```

For an image with a [`DrawCommand.TileSize`](DrawCommand.md#m-tilesize): the top-left corner in pixels of the tile at index (0, 0). Repeating axes continue the pattern in both directions from it.

**Type:** [`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="m-tilesize"></a>TileSize

```csharp
public Vector2 TileSize { get; init; }
```

For an image: the size in pixels of one tile (or of the single placed image). Zero means the image fills [`DrawCommand.Bounds`](DrawCommand.md#m-bounds) (stretched or nine-sliced).

**Type:** [`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="m-transform"></a>Transform

```csharp
public Matrix3x2 Transform { get; init; }
```

The 2D transform of [`DrawKind.PushTransform`](DrawKind.md#m-pushtransform), applied to row vectors (x, y, 1).

**Type:** [`Matrix3x2`](https://learn.microsoft.com/dotnet/api/system.numerics.matrix3x2)

### <a id="m-value"></a>Value

```csharp
public float Value { get; init; }
```

Shadow blur radius in pixels, or the opacity (0..1) of a group.

**Type:** `float`

### <a id="m-widths"></a>Widths

```csharp
public Edges Widths { get; init; }
```

Border thickness per side.

**Type:** [`Edges`](Edges.md)
