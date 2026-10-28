[API reference](../../index.md) › [OneGame.Render2D](../index.md) › OneGame.Render2D

# FrameStats struct

**Namespace:** `OneGame.Render2D`  
**Package:** [`RecursiveAI.OneGame.Render2D`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`FrameStats`](FrameStats.md)&gt;

```csharp
public readonly struct FrameStats : IEquatable<FrameStats>
```

What a frame cost, counted when it was recorded.

## Constructors

| Member | Description |
| --- | --- |
| [`FrameStats(int, int, int, int, int, int, int, int, long)`](#m-ctor-int-int-int-int-int-int-int-int-long) | What a frame cost, counted when it was recorded. |

### <a id="m-ctor-int-int-int-int-int-int-int-int-long"></a>FrameStats(int, int, int, int, int, int, int, int, long)

```csharp
public FrameStats(int DrawCalls, int Passes, int Sprites, int SpritesCulled, int QuadInstances, int UiInstances, int TileChunkQuads, int ChunkUploads, long UploadedBytes);
```

What a frame cost, counted when it was recorded.

**Parameters**

- `DrawCalls` (`int`): Draw calls recorded.
- `Passes` (`int`): Render passes begun (each offscreen group or post-processing step is one more).
- `Sprites` (`int`): Sprites drawn.
- `SpritesCulled` (`int`): Sprites skipped because they were off screen.
- `QuadInstances` (`int`): Quad instances uploaded (sprites, tile chunks, composites).
- `UiInstances` (`int`): UI primitives uploaded.
- `TileChunkQuads` (`int`): Tilemap chunk quads drawn.
- `ChunkUploads` (`int`): Tilemap chunks uploaded to the GPU.
- `UploadedBytes` (`long`): Bytes of instance and clip data written.

## Properties

| Member | Description |
| --- | --- |
| [`ChunkUploads`](#m-chunkuploads) | Tilemap chunks uploaded to the GPU. |
| [`DrawCalls`](#m-drawcalls) | Draw calls recorded. |
| [`Passes`](#m-passes) | Render passes begun (each offscreen group or post-processing step is one more). |
| [`QuadInstances`](#m-quadinstances) | Quad instances uploaded (sprites, tile chunks, composites). |
| [`Sprites`](#m-sprites) | Sprites drawn. |
| [`SpritesCulled`](#m-spritesculled) | Sprites skipped because they were off screen. |
| [`TileChunkQuads`](#m-tilechunkquads) | Tilemap chunk quads drawn. |
| [`UiInstances`](#m-uiinstances) | UI primitives uploaded. |
| [`UploadedBytes`](#m-uploadedbytes) | Bytes of instance and clip data written. |

### <a id="m-chunkuploads"></a>ChunkUploads

```csharp
public int ChunkUploads { get; init; }
```

Tilemap chunks uploaded to the GPU.

**Type:** `int`

### <a id="m-drawcalls"></a>DrawCalls

```csharp
public int DrawCalls { get; init; }
```

Draw calls recorded.

**Type:** `int`

### <a id="m-passes"></a>Passes

```csharp
public int Passes { get; init; }
```

Render passes begun (each offscreen group or post-processing step is one more).

**Type:** `int`

### <a id="m-quadinstances"></a>QuadInstances

```csharp
public int QuadInstances { get; init; }
```

Quad instances uploaded (sprites, tile chunks, composites).

**Type:** `int`

### <a id="m-sprites"></a>Sprites

```csharp
public int Sprites { get; init; }
```

Sprites drawn.

**Type:** `int`

### <a id="m-spritesculled"></a>SpritesCulled

```csharp
public int SpritesCulled { get; init; }
```

Sprites skipped because they were off screen.

**Type:** `int`

### <a id="m-tilechunkquads"></a>TileChunkQuads

```csharp
public int TileChunkQuads { get; init; }
```

Tilemap chunk quads drawn.

**Type:** `int`

### <a id="m-uiinstances"></a>UiInstances

```csharp
public int UiInstances { get; init; }
```

UI primitives uploaded.

**Type:** `int`

### <a id="m-uploadedbytes"></a>UploadedBytes

```csharp
public long UploadedBytes { get; init; }
```

Bytes of instance and clip data written.

**Type:** `long`
