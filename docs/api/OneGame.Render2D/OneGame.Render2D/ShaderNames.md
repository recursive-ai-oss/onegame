[API reference](../../index.md) › [OneGame.Render2D](../index.md) › OneGame.Render2D

# ShaderNames static class

**Namespace:** `OneGame.Render2D`  
**Package:** [`RecursiveAI.OneGame.Render2D`](../index.md)

```csharp
public static class ShaderNames
```

The programs and entry points the renderer asks an [`IShaderSource`](IShaderSource.md) for.

## Fields

| Member | Description |
| --- | --- |
| [`Additive`](#m-additive) | Quad pixel entry point: texture times tint, added to the target instead of blended over it. |
| [`Blur`](#m-blur) | Quad pixel entry point: separable blur. |
| [`Grade`](#m-grade) | Quad pixel entry point: colour grade. |
| [`Quad`](#m-quad) | Program `Quad` (Quad.hlsl): sprites, composites and post passes. |
| [`Sprite`](#m-sprite) | Quad pixel entry point: texture times tint. |
| [`Tile`](#m-tile) | Program `Tile` (Tile.hlsl): tilemap chunks. |
| [`TileFlat`](#m-tileflat) | Tile pixel entry point: flat colours from the far-zoom pool. |
| [`TileIds`](#m-tileids) | Tile pixel entry point: tile ids from the near-zoom pool. |
| [`Ui`](#m-ui) | Program `Ui` (Ui.hlsl): UI primitives. |
| [`UiPixel`](#m-uipixel) | Ui pixel entry point. |
| [`Vertex`](#m-vertex) | Vertex entry point shared by every program. |

### <a id="m-additive"></a>Additive

```csharp
public const string Additive = "PSAdditive";
```

Quad pixel entry point: texture times tint, added to the target instead of blended over it.

**Type:** `string`

### <a id="m-blur"></a>Blur

```csharp
public const string Blur = "PSBlur";
```

Quad pixel entry point: separable blur.

**Type:** `string`

### <a id="m-grade"></a>Grade

```csharp
public const string Grade = "PSGrade";
```

Quad pixel entry point: colour grade.

**Type:** `string`

### <a id="m-quad"></a>Quad

```csharp
public const string Quad = "Quad";
```

Program `Quad` (Quad.hlsl): sprites, composites and post passes.

**Type:** `string`

### <a id="m-sprite"></a>Sprite

```csharp
public const string Sprite = "PSSprite";
```

Quad pixel entry point: texture times tint.

**Type:** `string`

### <a id="m-tile"></a>Tile

```csharp
public const string Tile = "Tile";
```

Program `Tile` (Tile.hlsl): tilemap chunks.

**Type:** `string`

### <a id="m-tileflat"></a>TileFlat

```csharp
public const string TileFlat = "PSFlat";
```

Tile pixel entry point: flat colours from the far-zoom pool.

**Type:** `string`

### <a id="m-tileids"></a>TileIds

```csharp
public const string TileIds = "PSIds";
```

Tile pixel entry point: tile ids from the near-zoom pool.

**Type:** `string`

### <a id="m-ui"></a>Ui

```csharp
public const string Ui = "Ui";
```

Program `Ui` (Ui.hlsl): UI primitives.

**Type:** `string`

### <a id="m-uipixel"></a>UiPixel

```csharp
public const string UiPixel = "PSMain";
```

Ui pixel entry point.

**Type:** `string`

### <a id="m-vertex"></a>Vertex

```csharp
public const string Vertex = "VSMain";
```

Vertex entry point shared by every program.

**Type:** `string`
