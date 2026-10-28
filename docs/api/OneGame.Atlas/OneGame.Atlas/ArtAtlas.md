[API reference](../../index.md) › [OneGame.Atlas](../index.md) › OneGame.Atlas

# ArtAtlas class

**Namespace:** `OneGame.Atlas`  
**Package:** [`RecursiveAI.OneGame.Atlas`](../index.md)

```csharp
public sealed class ArtAtlas
```

SVG and image atlas. Sources are registered once under an id (the id the UI's `Image` commands carry) and rasterized lazily at the pixel size asked for: an SVG is rendered at exactly that size, so it is crisp at any UI scale and is re-rasterized when the scale changes; a raster image is decoded at its own size. Pages are [`AtlasFormat.Rgba8Premultiplied`](AtlasFormat.md#m-rgba8premultiplied).

**Example**

Register art in `GameDefinition.DefineArt`, then show it by id:

```csharp
public override void DefineArt(ArtAtlas art) =>
    art.AddSvg("icons/save", File.ReadAllBytes("art/save.svg"));

// In a screen: Ui.Image("icons/save", naturalWidth: 24, naturalHeight: 24)
```

## Constructors

| Member | Description |
| --- | --- |
| [`ArtAtlas(AtlasOptions?, ArtLimits?)`](#m-ctor-atlasoptions-artlimits) | Creates an empty atlas. |

### <a id="m-ctor-atlasoptions-artlimits"></a>ArtAtlas(AtlasOptions?, ArtLimits?)

```csharp
public ArtAtlas(AtlasOptions? options = null, ArtLimits? limits = null);
```

Creates an empty atlas.

**Parameters**

- `options` ([`AtlasOptions`](AtlasOptions.md)?): Page sizes; the defaults when `null`.
- `limits` ([`ArtLimits`](ArtLimits.md)?): Budgets; the defaults when `null`.

## Properties

| Member | Description |
| --- | --- |
| [`BackgroundThreads`](#m-backgroundthreads) | Gets or sets whether preload may rasterize on worker threads. |
| [`Pages`](#m-pages) | Gets the pages. |
| [`PreloadFrameBudget`](#m-preloadframebudget) | Gets or sets the time [`ArtAtlas.BeginFrame()`](ArtAtlas.md#m-beginframe-long) may spend rasterizing preload work when [`ArtAtlas.BackgroundThreads`](ArtAtlas.md#m-backgroundthreads) is false (at least one item is always done). |
| [`PreloadRasterizations`](#m-preloadrasterizations) | Gets how many rasterizations ran for [`ArtAtlas.Preload()`](ArtAtlas.md#m-preload-ienumerable-string-float), on a worker thread or in [`ArtAtlas.PumpPreload()`](ArtAtlas.md#m-pumppreload). |
| [`StandaloneBytes`](#m-standalonebytes) | Gets the GPU bytes the standalone textures take; never above [`ArtLimits.MaxStandaloneBytes`](ArtLimits.md#m-maxstandalonebytes). |
| [`StandaloneCount`](#m-standalonecount) | Gets how many standalone textures exist. |
| [`SyncRasterizations`](#m-syncrasterizations) | Gets how many rasterizations ran inside a lookup ([`ArtAtlas.TryGet()`](ArtAtlas.md#m-tryget-string-int-int-artregion) and its variants), that is on the caller's thread, because the art was not ready. |

### <a id="m-backgroundthreads"></a>BackgroundThreads

```csharp
public bool BackgroundThreads { get; set; }
```

Gets or sets whether preload may rasterize on worker threads. The default is true everywhere threads exist and false in the browser (WebAssembly without threads), where preload work is done a little at a time by [`ArtAtlas.PumpPreload()`](ArtAtlas.md#m-pumppreload) instead.

**Type:** `bool`

### <a id="m-pages"></a>Pages

```csharp
public AtlasPages Pages { get; }
```

Gets the pages. The renderer uploads [`AtlasPage.TakeDirty()`](AtlasPage.md#m-takedirty) of each and keeps one texture per page.

**Type:** [`AtlasPages`](AtlasPages.md)

### <a id="m-preloadframebudget"></a>PreloadFrameBudget

```csharp
public TimeSpan PreloadFrameBudget { get; set; }
```

Gets or sets the time [`ArtAtlas.BeginFrame()`](ArtAtlas.md#m-beginframe-long) may spend rasterizing preload work when [`ArtAtlas.BackgroundThreads`](ArtAtlas.md#m-backgroundthreads) is false (at least one item is always done). Default 2 ms.

**Type:** [`TimeSpan`](https://learn.microsoft.com/dotnet/api/system.timespan)

### <a id="m-preloadrasterizations"></a>PreloadRasterizations

```csharp
public long PreloadRasterizations { get; }
```

Gets how many rasterizations ran for [`ArtAtlas.Preload()`](ArtAtlas.md#m-preload-ienumerable-string-float), on a worker thread or in [`ArtAtlas.PumpPreload()`](ArtAtlas.md#m-pumppreload).

**Type:** `long`

### <a id="m-standalonebytes"></a>StandaloneBytes

```csharp
public long StandaloneBytes { get; }
```

Gets the GPU bytes the standalone textures take; never above [`ArtLimits.MaxStandaloneBytes`](ArtLimits.md#m-maxstandalonebytes).

**Type:** `long`

### <a id="m-standalonecount"></a>StandaloneCount

```csharp
public int StandaloneCount { get; }
```

Gets how many standalone textures exist.

**Type:** `int`

### <a id="m-syncrasterizations"></a>SyncRasterizations

```csharp
public long SyncRasterizations { get; }
```

Gets how many rasterizations ran inside a lookup ([`ArtAtlas.TryGet()`](ArtAtlas.md#m-tryget-string-int-int-artregion) and its variants), that is on the caller's thread, because the art was not ready.

**Type:** `long`

## Methods

| Member | Description |
| --- | --- |
| [`AddImage(string, ReadOnlyMemory<byte>, NineSlice)`](#m-addimage-string-readonlymemory-byte-nineslice) | Registers a raster image (PNG, JPEG, WebP and the other formats OneImage reads). |
| [`AddSvg(string, ReadOnlyMemory<byte>, NineSlice)`](#m-addsvg-string-readonlymemory-byte-nineslice) | Registers an SVG document. |
| [`BeginFrame(long)`](#m-beginframe-long) | Starts a frame; see [`AtlasPages.BeginFrame()`](AtlasPages.md#m-beginframe-long). |
| [`CompletePreload()`](#m-completepreload) | Finishes every queued preload (the queue on the calling thread, work already on a worker by waiting for it) and stores the results: a loading screen's last step. |
| [`Preload(IEnumerable<string>, float)`](#m-preload-ienumerable-string-float) | Starts rasterizing resources ahead of their first draw, at their natural size times `scale` (the display list's UI scale), the size [`ArtAtlas.TryGetScaled()`](ArtAtlas.md#m-trygetscaled-string-float-artregion) asks for. |
| [`PreloadFitted(string, float, float, float)`](#m-preloadfitted-string-float-float-float) | Starts rasterizing one resource for a box of the given pixel size, the size [`ArtAtlas.TryGetFitted()`](ArtAtlas.md#m-trygetfitted-string-float-float-float-artregion) would pick; see [`ArtAtlas.Preload()`](ArtAtlas.md#m-preload-ienumerable-string-float). |
| [`PumpPreload()`](#m-pumppreload) | Stores finished preload results in the pages (a memory copy per item), and, when [`ArtAtlas.BackgroundThreads`](ArtAtlas.md#m-backgroundthreads) is false, rasterizes queued work for up to [`ArtAtlas.PreloadFrameBudget`](ArtAtlas.md#m-preloadframebudget) first. |
| [`TakeReleased(List<int>)`](#m-takereleased-list-int) | Moves the handles released since the last call into `handles` so the renderer can free their textures. |
| [`TryGet(string, int, int, out ArtRegion)`](#m-tryget-string-int-int-artregion) | Looks up (rasterizing on first use) the resource at an exact pixel size. |
| [`TryGetFitted(string, float, float, float, out ArtRegion)`](#m-trygetfitted-string-float-float-float-artregion) | Looks up the resource for drawing into a box of the given pixel size. |
| [`TryGetNaturalSize(string, out int, out int)`](#m-trygetnaturalsize-string-int-int) | Returns the natural size of a registered resource: the SVG's own size, or the image's pixel size. |
| [`TryGetScaled(string, float, out ArtRegion)`](#m-trygetscaled-string-float-artregion) | Looks up (rasterizing on first use) the resource at its natural size times `scale`, rounded to whole pixels. |
| [`TryGetStandalone(int, out StandaloneArt)`](#m-trygetstandalone-int-standaloneart) | Returns the standalone texture behind [`ArtRegion.Standalone`](ArtRegion.md#m-standalone). |

### <a id="m-addimage-string-readonlymemory-byte-nineslice"></a>AddImage(string, ReadOnlyMemory&lt;byte&gt;, NineSlice)

```csharp
public void AddImage(string id, ReadOnlyMemory<byte> data, NineSlice slice = default);
```

Registers a raster image (PNG, JPEG, WebP and the other formats OneImage reads).

**Parameters**

- `id` (`string`): The resource id used by display list `Image` commands.
- `data` ([`ReadOnlyMemory`](https://learn.microsoft.com/dotnet/api/system.readonlymemory-1)`<byte>`): The file.
- `slice` ([`NineSlice`](NineSlice.md)): Nine-slice insets in image pixels, or zero.

**Exceptions**

- [`AtlasAssetException`](AtlasAssetException.md): The file is too large, damaged, or larger than [`ArtLimits.MaxRasterDimension`](ArtLimits.md#m-maxrasterdimension).

### <a id="m-addsvg-string-readonlymemory-byte-nineslice"></a>AddSvg(string, ReadOnlyMemory&lt;byte&gt;, NineSlice)

```csharp
public void AddSvg(string id, ReadOnlyMemory<byte> data, NineSlice slice = default);
```

Registers an SVG document. Nothing is rasterized yet; the size is read to seed scaling.

**Parameters**

- `id` (`string`): The resource id used by display list `Image` commands.
- `data` ([`ReadOnlyMemory`](https://learn.microsoft.com/dotnet/api/system.readonlymemory-1)`<byte>`): The file.
- `slice` ([`NineSlice`](NineSlice.md)): Nine-slice insets in the SVG's own pixels (its natural size), or zero.

**Exceptions**

- [`AtlasAssetException`](AtlasAssetException.md): The file is too large, damaged, or its natural size is beyond the limits.

### <a id="m-beginframe-long"></a>BeginFrame(long)

```csharp
public void BeginFrame(long frame);
```

Starts a frame; see [`AtlasPages.BeginFrame()`](AtlasPages.md#m-beginframe-long).

**Parameters**

- `frame` (`long`): A number that grows with every frame.

### <a id="m-completepreload"></a>CompletePreload()

```csharp
public void CompletePreload();
```

Finishes every queued preload (the queue on the calling thread, work already on a worker by waiting for it) and stores the results: a loading screen's last step. UI thread only.

### <a id="m-preload-ienumerable-string-float"></a>Preload(IEnumerable&lt;string&gt;, float)

```csharp
public ArtPreload Preload(IEnumerable<string> ids, float scale);
```

Starts rasterizing resources ahead of their first draw, at their natural size times `scale` (the display list's UI scale), the size [`ArtAtlas.TryGetScaled()`](ArtAtlas.md#m-trygetscaled-string-float-artregion) asks for. The decoding and rasterizing run on worker threads; the pages are only touched on the UI thread, in [`ArtAtlas.BeginFrame()`](ArtAtlas.md#m-beginframe-long) or [`ArtAtlas.PumpPreload()`](ArtAtlas.md#m-pumppreload). Until the result is stored, a draw behaves as without preload: it rasterizes on the spot. Unknown ids, sizes beyond the budgets and art already present count as finished.

**Parameters**

- `ids` ([`IEnumerable`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable-1)`<string>`): The resource ids.
- `scale` (`float`): The UI scale the art will be drawn at.

**Returns:** [`ArtPreload`](ArtPreload.md) — A handle to watch progress.

### <a id="m-preloadfitted-string-float-float-float"></a>PreloadFitted(string, float, float, float)

```csharp
public ArtPreload PreloadFitted(string id, float width, float height, float scale);
```

Starts rasterizing one resource for a box of the given pixel size, the size [`ArtAtlas.TryGetFitted()`](ArtAtlas.md#m-trygetfitted-string-float-float-float-artregion) would pick; see [`ArtAtlas.Preload()`](ArtAtlas.md#m-preload-ienumerable-string-float).

**Parameters**

- `id` (`string`): The resource id.
- `width` (`float`): Box width in pixels.
- `height` (`float`): Box height in pixels.
- `scale` (`float`): The display list's UI scale.

**Returns:** [`ArtPreload`](ArtPreload.md) — A handle to watch progress.

### <a id="m-pumppreload"></a>PumpPreload()

```csharp
public void PumpPreload();
```

Stores finished preload results in the pages (a memory copy per item), and, when [`ArtAtlas.BackgroundThreads`](ArtAtlas.md#m-backgroundthreads) is false, rasterizes queued work for up to [`ArtAtlas.PreloadFrameBudget`](ArtAtlas.md#m-preloadframebudget) first. [`ArtAtlas.BeginFrame()`](ArtAtlas.md#m-beginframe-long) calls it; call it directly to make progress outside a frame. UI thread only.

### <a id="m-takereleased-list-int"></a>TakeReleased(List&lt;int&gt;)

```csharp
public void TakeReleased(List<int> handles);
```

Moves the handles released since the last call into `handles` so the renderer can free their textures.

**Parameters**

- `handles` ([`List`](https://learn.microsoft.com/dotnet/api/system.collections.generic.list-1)`<int>`): Receives the handles.

### <a id="m-tryget-string-int-int-artregion"></a>TryGet(string, int, int, out ArtRegion)

```csharp
public AtlasStatus TryGet(string id, int width, int height, out ArtRegion region);
```

Looks up (rasterizing on first use) the resource at an exact pixel size. A raster image is always stored at its own size.

**Parameters**

- `id` (`string`): The resource id.
- `width` (`int`): Width in pixels.
- `height` (`int`): Height in pixels.
- `region` ([`ArtRegion`](ArtRegion.md)): The entry when the result is [`AtlasStatus.Ok`](AtlasStatus.md#m-ok).

**Returns:** [`AtlasStatus`](AtlasStatus.md) — The status. Sizes beyond the budget return [`AtlasStatus.TooLarge`](AtlasStatus.md#m-toolarge) before anything is decoded or allocated.

### <a id="m-trygetfitted-string-float-float-float-artregion"></a>TryGetFitted(string, float, float, float, out ArtRegion)

```csharp
public AtlasStatus TryGetFitted(string id, float width, float height, float scale, out ArtRegion region);
```

Looks up the resource for drawing into a box of the given pixel size. An SVG whose box is larger than [`ArtLimits.StandaloneThreshold`](ArtLimits.md#m-standalonethreshold) on a side is rasterized at the box's own size when that differs from its natural size times `scale` by more than a pixel, so a backdrop stretched over a panel stays sharp. Smaller boxes (icons, cards) and raster images use [`ArtAtlas.TryGetScaled()`](ArtAtlas.md#m-trygetscaled-string-float-artregion), which keeps the shared pages from filling with one raster per layout size. When the exact size is refused for size or memory, the scaled lookup is the fallback.

**Parameters**

- `id` (`string`): The resource id.
- `width` (`float`): Box width in pixels.
- `height` (`float`): Box height in pixels.
- `scale` (`float`): The display list's UI scale.
- `region` ([`ArtRegion`](ArtRegion.md)): The entry when the result is [`AtlasStatus.Ok`](AtlasStatus.md#m-ok).

**Returns:** [`AtlasStatus`](AtlasStatus.md) — The status.

### <a id="m-trygetnaturalsize-string-int-int"></a>TryGetNaturalSize(string, out int, out int)

```csharp
public bool TryGetNaturalSize(string id, out int width, out int height);
```

Returns the natural size of a registered resource: the SVG's own size, or the image's pixel size.

**Parameters**

- `id` (`string`): The resource id.
- `width` (`int`): Natural width.
- `height` (`int`): Natural height.

**Returns:** `bool` — False when the id is unknown.

### <a id="m-trygetscaled-string-float-artregion"></a>TryGetScaled(string, float, out ArtRegion)

```csharp
public AtlasStatus TryGetScaled(string id, float scale, out ArtRegion region);
```

Looks up (rasterizing on first use) the resource at its natural size times `scale`, rounded to whole pixels.

**Parameters**

- `id` (`string`): The resource id.
- `scale` (`float`): Pixels per natural pixel: the display list's UI scale. A raster image ignores it.
- `region` ([`ArtRegion`](ArtRegion.md)): The entry when the result is [`AtlasStatus.Ok`](AtlasStatus.md#m-ok).

**Returns:** [`AtlasStatus`](AtlasStatus.md) — The status.

### <a id="m-trygetstandalone-int-standaloneart"></a>TryGetStandalone(int, out StandaloneArt)

```csharp
public bool TryGetStandalone(int handle, out StandaloneArt art);
```

Returns the standalone texture behind [`ArtRegion.Standalone`](ArtRegion.md#m-standalone).

**Parameters**

- `handle` (`int`): The handle from a region.
- `art` ([`StandaloneArt`](StandaloneArt.md)): The texture's pixels and size.

**Returns:** `bool` — False when the handle is unknown or was released.
