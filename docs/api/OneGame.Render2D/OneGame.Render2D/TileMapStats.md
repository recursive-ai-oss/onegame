[API reference](../../index.md) › [OneGame.Render2D](../index.md) › OneGame.Render2D

# TileMapStats struct

**Namespace:** `OneGame.Render2D`  
**Package:** [`RecursiveAI.OneGame.Render2D`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`TileMapStats`](TileMapStats.md)&gt;

```csharp
public readonly struct TileMapStats : IEquatable<TileMapStats>
```

What the tilemap drew last frame; for tests, captures and tuning.

## Constructors

| Member | Description |
| --- | --- |
| [`TileMapStats(int, int, int, int)`](#m-ctor-int-int-int-int) | What the tilemap drew last frame; for tests, captures and tuning. |

### <a id="m-ctor-int-int-int-int"></a>TileMapStats(int, int, int, int)

```csharp
public TileMapStats(int Level, int ChunkQuads, int ChunkUploads, int DroppedChunks);
```

What the tilemap drew last frame; for tests, captures and tuning.

**Parameters**

- `Level` (`int`): The level of detail drawn: 0 is individual tiles, level n is one colour per 2^n by 2^n tiles.
- `ChunkQuads` (`int`): Chunk quads drawn.
- `ChunkUploads` (`int`): Chunks uploaded to the GPU this frame.
- `DroppedChunks` (`int`): Visible chunks that did not fit in the GPU pool (the map's pool is too small for this zoom).

## Properties

| Member | Description |
| --- | --- |
| [`ChunkQuads`](#m-chunkquads) | Chunk quads drawn. |
| [`ChunkUploads`](#m-chunkuploads) | Chunks uploaded to the GPU this frame. |
| [`DroppedChunks`](#m-droppedchunks) | Visible chunks that did not fit in the GPU pool (the map's pool is too small for this zoom). |
| [`Level`](#m-level) | The level of detail drawn: 0 is individual tiles, level n is one colour per 2^n by 2^n tiles. |

### <a id="m-chunkquads"></a>ChunkQuads

```csharp
public int ChunkQuads { get; init; }
```

Chunk quads drawn.

**Type:** `int`

### <a id="m-chunkuploads"></a>ChunkUploads

```csharp
public int ChunkUploads { get; init; }
```

Chunks uploaded to the GPU this frame.

**Type:** `int`

### <a id="m-droppedchunks"></a>DroppedChunks

```csharp
public int DroppedChunks { get; init; }
```

Visible chunks that did not fit in the GPU pool (the map's pool is too small for this zoom).

**Type:** `int`

### <a id="m-level"></a>Level

```csharp
public int Level { get; init; }
```

The level of detail drawn: 0 is individual tiles, level n is one colour per 2^n by 2^n tiles.

**Type:** `int`
