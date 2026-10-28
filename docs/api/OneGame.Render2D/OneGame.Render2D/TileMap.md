[API reference](../../index.md) › [OneGame.Render2D](../index.md) › OneGame.Render2D

# TileMap class

**Namespace:** `OneGame.Render2D`  
**Package:** [`RecursiveAI.OneGame.Render2D`](../index.md)  
**Implements:** [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class TileMap : IDisposable
```

A rectangular map of tile ids, stored in chunks of 64 by 64 so a very large map costs memory only where it has tiles. Zoomed out, the renderer draws from coarser levels (one averaged colour per 2^n by 2^n tiles) instead of drawing every tile, so the work depends on the screen and not on the map. World +Y is down: tile (0, 0) is at [`TileMap.Origin`](TileMap.md#m-origin) and tile (x, y) covers [`TileMap.TileSize`](TileMap.md#m-tilesize) world units to the right and down.

## Constructors

| Member | Description |
| --- | --- |
| [`TileMap(TileSet, int, int, float)`](#m-ctor-tileset-int-int-float) | Makes an empty map. |

### <a id="m-ctor-tileset-int-int-float"></a>TileMap(TileSet, int, int, float)

```csharp
public TileMap(TileSet tileSet, int width, int height, float tileSize);
```

Makes an empty map.

**Parameters**

- `tileSet` ([`TileSet`](TileSet.md)): The tiles to draw with.
- `width` (`int`): Width in tiles, 1 to [`TileMap.MaxSize`](TileMap.md#m-maxsize).
- `height` (`int`): Height in tiles, 1 to [`TileMap.MaxSize`](TileMap.md#m-maxsize).
- `tileSize` (`float`): World units per tile edge.

## Fields

| Member | Description |
| --- | --- |
| [`ChunkSize`](#m-chunksize) | Tiles per chunk edge. |
| [`MaxSize`](#m-maxsize) | The largest width or height in tiles. |

### <a id="m-chunksize"></a>ChunkSize

```csharp
public const int ChunkSize = 64;
```

Tiles per chunk edge.

**Type:** `int`

### <a id="m-maxsize"></a>MaxSize

```csharp
public const int MaxSize = 65536;
```

The largest width or height in tiles.

**Type:** `int`

## Properties

| Member | Description |
| --- | --- |
| [`Height`](#m-height) | Height in tiles. |
| [`LastStats`](#m-laststats) | What the last [`Renderer2D.DrawScene()`](Renderer2D.md#m-drawscene-scene2d-camera2d-postchain) drew for this map. |
| [`LodThreshold`](#m-lodthreshold) | Once a tile would be smaller than this many pixels on screen, the renderer switches to the next coarser level. |
| [`MaxLevel`](#m-maxlevel) | The coarsest level of detail the map can be drawn at. |
| [`Origin`](#m-origin) | The world position of the map's top-left corner. |
| [`PoolChunks`](#m-poolchunks) | Chunks the GPU keeps resident for this map. |
| [`TileSet`](#m-tileset) | The tiles this map draws with. |
| [`TileSize`](#m-tilesize) | World units per tile edge. |
| [`Width`](#m-width) | Width in tiles. |

### <a id="m-height"></a>Height

```csharp
public int Height { get; }
```

Height in tiles.

**Type:** `int`

### <a id="m-laststats"></a>LastStats

```csharp
public TileMapStats LastStats { get; }
```

What the last [`Renderer2D.DrawScene()`](Renderer2D.md#m-drawscene-scene2d-camera2d-postchain) drew for this map.

**Type:** [`TileMapStats`](TileMapStats.md)

### <a id="m-lodthreshold"></a>LodThreshold

```csharp
public float LodThreshold { get; set; }
```

Once a tile would be smaller than this many pixels on screen, the renderer switches to the next coarser level. 4 by default.

**Type:** `float`

### <a id="m-maxlevel"></a>MaxLevel

```csharp
public int MaxLevel { get; }
```

The coarsest level of detail the map can be drawn at.

**Type:** `int`

### <a id="m-origin"></a>Origin

```csharp
public Vector2 Origin { get; set; }
```

The world position of the map's top-left corner.

**Type:** [`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="m-poolchunks"></a>PoolChunks

```csharp
public int PoolChunks { get; init; }
```

Chunks the GPU keeps resident for this map. It must cover the chunks visible at once (about 135 on a 4K screen at the default threshold).

**Type:** `int`

### <a id="m-tileset"></a>TileSet

```csharp
public TileSet TileSet { get; }
```

The tiles this map draws with.

**Type:** [`TileSet`](TileSet.md)

### <a id="m-tilesize"></a>TileSize

```csharp
public float TileSize { get; }
```

World units per tile edge.

**Type:** `float`

### <a id="m-width"></a>Width

```csharp
public int Width { get; }
```

Width in tiles.

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`Dispose()`](#m-dispose) | Releases the GPU memory of this map. |
| [`Get(int, int)`](#m-get-int-int) | Reads a tile id; 0 outside the map. |
| [`Set(int, int, ushort)`](#m-set-int-int-ushort) | Writes a tile id. |

### <a id="m-dispose"></a>Dispose()

```csharp
public void Dispose();
```

Releases the GPU memory of this map.

### <a id="m-get-int-int"></a>Get(int, int)

```csharp
public ushort Get(int x, int y);
```

Reads a tile id; 0 outside the map.

**Parameters**

- `x` (`int`): Column.
- `y` (`int`): Row.

**Returns:** `ushort` — The tile id.

### <a id="m-set-int-int-ushort"></a>Set(int, int, ushort)

```csharp
public void Set(int x, int y, ushort id);
```

Writes a tile id.

**Parameters**

- `x` (`int`): Column.
- `y` (`int`): Row.
- `id` (`ushort`): The tile id: 0 for empty, otherwise 1 to [`TileSet.TileCount`](TileSet.md#m-tilecount).
