[API reference](../index.md) › OneGame.Atlas

# OneGame.Atlas

Atlas pages and the SVG and image atlas for the 2D renderer.

**Package:** `RecursiveAI.OneGame.Atlas`

```sh
dotnet add package RecursiveAI.OneGame.Atlas
```

## OneGame.Atlas

| Type | Kind | Description |
| --- | --- | --- |
| [`ArtAtlas`](OneGame.Atlas/ArtAtlas.md) | class | SVG and image atlas. |
| [`ArtLimits`](OneGame.Atlas/ArtLimits.md) | class | Budgets that bound what a hostile SVG or image can make the atlas do. |
| [`ArtPreload`](OneGame.Atlas/ArtPreload.md) | class | Progress of one [`ArtAtlas.Preload()`](OneGame.Atlas/ArtAtlas.md#m-preload-ienumerable-string-float) call. |
| [`ArtRegion`](OneGame.Atlas/ArtRegion.md) | struct | Where one rasterized SVG or image sits in the atlas. |
| [`AtlasAssetException`](OneGame.Atlas/AtlasAssetException.md) | class | A registered asset was refused: it is damaged or breaks a size budget. |
| [`AtlasFormat`](OneGame.Atlas/AtlasFormat.md) | enum | The pixel layout of an atlas page. |
| [`AtlasOptions`](OneGame.Atlas/AtlasOptions.md) | class | How an atlas is sized. |
| [`AtlasPage`](OneGame.Atlas/AtlasPage.md) | class | One texture-sized page of pixels in system memory. |
| [`AtlasPages`](OneGame.Atlas/AtlasPages.md) | class | A growing set of equal-sized pages with a shelf packer. |
| [`AtlasRect`](OneGame.Atlas/AtlasRect.md) | struct | An integer rectangle in page pixels, origin top-left. |
| [`AtlasSlot`](OneGame.Atlas/AtlasSlot.md) | struct | A rectangle reserved on a page. |
| [`AtlasStatus`](OneGame.Atlas/AtlasStatus.md) | enum | The outcome of a lookup that may rasterize. |
| [`NineSlice`](OneGame.Atlas/NineSlice.md) | struct | Insets, in pixels, that cut a frame into nine parts: the corners keep their size, the edges stretch along one axis, the middle in both. |
| [`StandaloneArt`](OneGame.Atlas/StandaloneArt.md) | class | A raster too big for the shared pages: it has a texture of its own, at full resolution. |
