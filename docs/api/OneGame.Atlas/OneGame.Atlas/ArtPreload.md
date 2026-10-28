[API reference](../../index.md) › [OneGame.Atlas](../index.md) › OneGame.Atlas

# ArtPreload class

**Namespace:** `OneGame.Atlas`  
**Package:** [`RecursiveAI.OneGame.Atlas`](../index.md)

```csharp
public sealed class ArtPreload
```

Progress of one [`ArtAtlas.Preload()`](ArtAtlas.md#m-preload-ienumerable-string-float) call. Safe to read from any thread.

## Constructors

| Member | Description |
| --- | --- |
| [`ArtPreload()`](#m-ctor) |  |

### <a id="m-ctor"></a>ArtPreload()

```csharp
public ArtPreload();
```

## Properties

| Member | Description |
| --- | --- |
| [`Failed`](#m-failed) | Gets how many finished without art in the atlas (unknown id, too large, damaged, or no room). |
| [`Finished`](#m-finished) | Gets how many are finished: in the atlas, already there, or given up on. |
| [`IsComplete`](#m-iscomplete) | Gets whether every requested raster is finished. |
| [`Total`](#m-total) | Gets how many rasters were asked for. |

### <a id="m-failed"></a>Failed

```csharp
public int Failed { get; }
```

Gets how many finished without art in the atlas (unknown id, too large, damaged, or no room). A draw of those behaves as without preload.

**Type:** `int`

### <a id="m-finished"></a>Finished

```csharp
public int Finished { get; }
```

Gets how many are finished: in the atlas, already there, or given up on.

**Type:** `int`

### <a id="m-iscomplete"></a>IsComplete

```csharp
public bool IsComplete { get; }
```

Gets whether every requested raster is finished.

**Type:** `bool`

### <a id="m-total"></a>Total

```csharp
public int Total { get; }
```

Gets how many rasters were asked for.

**Type:** `int`
