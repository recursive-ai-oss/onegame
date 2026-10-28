[API reference](../../index.md) › [OneGame.Render2D](../index.md) › OneGame.Render2D

# TileSet class

**Namespace:** `OneGame.Render2D`  
**Package:** [`RecursiveAI.OneGame.Render2D`](../index.md)  
**Implements:** [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class TileSet : IDisposable
```

A grid of equally sized tiles in one texture. Tile id 0 means "empty"; id n is the n-th cell counted left to right, top to bottom, starting at 1.

## Properties

| Member | Description |
| --- | --- |
| [`Columns`](#m-columns) | Tiles per row. |
| [`Rows`](#m-rows) | Rows of tiles. |
| [`Texture`](#m-texture) | The tile image. |
| [`TileCount`](#m-tilecount) | The largest valid tile id. |
| [`TileHeight`](#m-tileheight) | Height of one tile in texels. |
| [`TileWidth`](#m-tilewidth) | Width of one tile in texels. |

### <a id="m-columns"></a>Columns

```csharp
public int Columns { get; }
```

Tiles per row.

**Type:** `int`

### <a id="m-rows"></a>Rows

```csharp
public int Rows { get; }
```

Rows of tiles.

**Type:** `int`

### <a id="m-texture"></a>Texture

```csharp
public Texture2D Texture { get; }
```

The tile image.

**Type:** [`Texture2D`](Texture2D.md)

### <a id="m-tilecount"></a>TileCount

```csharp
public int TileCount { get; }
```

The largest valid tile id.

**Type:** `int`

### <a id="m-tileheight"></a>TileHeight

```csharp
public int TileHeight { get; }
```

Height of one tile in texels.

**Type:** `int`

### <a id="m-tilewidth"></a>TileWidth

```csharp
public int TileWidth { get; }
```

Width of one tile in texels.

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`Dispose()`](#m-dispose) | Releases the texture. |

### <a id="m-dispose"></a>Dispose()

```csharp
public void Dispose();
```

Releases the texture.
