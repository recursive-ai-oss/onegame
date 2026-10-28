[API reference](../index.md) › OneGame.Render2D

# OneGame.Render2D

Sprite, tilemap and text renderer.

**Package:** `RecursiveAI.OneGame.Render2D`

```sh
dotnet add package RecursiveAI.OneGame.Render2D
```

## OneGame.Render2D

| Type | Kind | Description |
| --- | --- | --- |
| [`Camera2D`](OneGame.Render2D/Camera2D.md) | struct | The view onto the world: where it looks, how far in, and how it is turned. |
| [`Camera2DRig`](OneGame.Render2D/Camera2DRig.md) | class | A camera that follows a point: optional smoothing, world bounds, zoom, and conversion between screen pixels and world points. |
| [`FrameOptions`](OneGame.Render2D/FrameOptions.md) | struct | Per-frame settings. |
| [`FrameStats`](OneGame.Render2D/FrameStats.md) | struct | What a frame cost, counted when it was recorded. |
| [`GeneratedShaders`](OneGame.Render2D/GeneratedShaders.md) | class | The renderer's own shaders, compiled from `Shaders/*.hlsl` during the build. |
| [`Gradient`](OneGame.Render2D/Gradient.md) | struct | A colour ramp for a filled shape. |
| [`IShaderSource`](OneGame.Render2D/IShaderSource.md) | interface | Supplies the renderer's built-in shaders by program and entry point name. |
| [`Layer2D`](OneGame.Render2D/Layer2D.md) | class | A layer of a [`Scene2D`](OneGame.Render2D/Scene2D.md). |
| [`Material`](OneGame.Render2D/Material.md) | class | A pixel shader with parameters, usable on sprites. |
| [`PixelRect`](OneGame.Render2D/PixelRect.md) | struct | Whole pixels of a texture or sub-rectangle. |
| [`PostChain`](OneGame.Render2D/PostChain.md) | class | A list of full-screen effects run, in order, over a finished scene. |
| [`Render2DModule`](OneGame.Render2D/Render2DModule.md) | static class | Identifies the Render2D assembly; a placeholder until its first feature lands. |
| [`Renderer2D`](OneGame.Render2D/Renderer2D.md) | class | The 2D renderer: sprites, tilemaps, parallax layers, custom materials, post-processing and the UI display list, drawn through the GPU layer. |
| [`Scene2D`](OneGame.Render2D/Scene2D.md) | class | Everything to draw in one frame of the world: sprites and tilemaps on layers. |
| [`ShaderCode`](OneGame.Render2D/ShaderCode.md) | struct | One compiled shader stage: SPIR-V (and, for Metal, Metal Shading Language) and the name of its entry point. |
| [`ShaderNames`](OneGame.Render2D/ShaderNames.md) | static class | The programs and entry points the renderer asks an [`IShaderSource`](OneGame.Render2D/IShaderSource.md) for. |
| [`Sprite`](OneGame.Render2D/Sprite.md) | struct | One sprite to draw this frame. |
| [`SpriteClip`](OneGame.Render2D/SpriteClip.md) | class | A named run of frames of a [`SpriteSheet`](OneGame.Render2D/SpriteSheet.md) played at a rate. |
| [`SpriteFlip`](OneGame.Render2D/SpriteFlip.md) | enum | Mirror flags for a sprite. |
| [`SpriteSheet`](OneGame.Render2D/SpriteSheet.md) | class | The frames of one texture: a grid of equal cells or explicit rectangles, plus named [`SpriteClip`](OneGame.Render2D/SpriteClip.md)s. |
| [`Texture2D`](OneGame.Render2D/Texture2D.md) | class | An image on the GPU that sprites, tilesets and materials draw from. |
| [`TextureFilter`](OneGame.Render2D/TextureFilter.md) | enum | How a texture is filtered when it is drawn at a size other than its own. |
| [`TileMap`](OneGame.Render2D/TileMap.md) | class | A rectangular map of tile ids, stored in chunks of 64 by 64 so a very large map costs memory only where it has tiles. |
| [`TileMapStats`](OneGame.Render2D/TileMapStats.md) | struct | What the tilemap drew last frame; for tests, captures and tuning. |
| [`TileSet`](OneGame.Render2D/TileSet.md) | class | A grid of equally sized tiles in one texture. |
| [`UiCanvas`](OneGame.Render2D/UiCanvas.md) | class | Draws UI shapes, images and text into the current frame, in coordinates of framebuffer pixels (origin top-left, y down). |
| [`UnknownClipException`](OneGame.Render2D/UnknownClipException.md) | class | A clip name that a [`SpriteSheet`](OneGame.Render2D/SpriteSheet.md) does not have. |

## OneGame.Render2D.Shaders

| Type | Kind | Description |
| --- | --- | --- |
| [`Quad`](OneGame.Render2D.Shaders/Quad.md) | static class |  |
| [`Quad.Bindings`](OneGame.Render2D.Shaders/Quad.Bindings.md) | static class |  |
| [`Quad.Bindings.Frame`](OneGame.Render2D.Shaders/Quad.Bindings.Frame.md) | static class |  |
| [`Quad.Bindings.Params`](OneGame.Render2D.Shaders/Quad.Bindings.Params.md) | static class |  |
| [`Quad.Bindings.Samp`](OneGame.Render2D.Shaders/Quad.Bindings.Samp.md) | static class |  |
| [`Quad.Bindings.Tex`](OneGame.Render2D.Shaders/Quad.Bindings.Tex.md) | static class |  |
| [`Quad.Frame`](OneGame.Render2D.Shaders/Quad.Frame.md) | struct |  |
| [`Quad.PSAdditive`](OneGame.Render2D.Shaders/Quad.PSAdditive.md) | static class |  |
| [`Quad.PSBlur`](OneGame.Render2D.Shaders/Quad.PSBlur.md) | static class |  |
| [`Quad.PSGrade`](OneGame.Render2D.Shaders/Quad.PSGrade.md) | static class |  |
| [`Quad.PSSprite`](OneGame.Render2D.Shaders/Quad.PSSprite.md) | static class |  |
| [`Quad.Params`](OneGame.Render2D.Shaders/Quad.Params.md) | struct |  |
| [`Quad.VSMain`](OneGame.Render2D.Shaders/Quad.VSMain.md) | static class |  |
| [`Quad.VSMain.Vertex`](OneGame.Render2D.Shaders/Quad.VSMain.Vertex.md) | struct |  |
| [`Tile`](OneGame.Render2D.Shaders/Tile.md) | static class |  |
| [`Tile.Bindings`](OneGame.Render2D.Shaders/Tile.Bindings.md) | static class |  |
| [`Tile.Bindings.Colors`](OneGame.Render2D.Shaders/Tile.Bindings.Colors.md) | static class |  |
| [`Tile.Bindings.Frame`](OneGame.Render2D.Shaders/Tile.Bindings.Frame.md) | static class |  |
| [`Tile.Bindings.Ids`](OneGame.Render2D.Shaders/Tile.Bindings.Ids.md) | static class |  |
| [`Tile.Bindings.Samp`](OneGame.Render2D.Shaders/Tile.Bindings.Samp.md) | static class |  |
| [`Tile.Bindings.TileParams`](OneGame.Render2D.Shaders/Tile.Bindings.TileParams.md) | static class |  |
| [`Tile.Bindings.Tileset`](OneGame.Render2D.Shaders/Tile.Bindings.Tileset.md) | static class |  |
| [`Tile.Frame`](OneGame.Render2D.Shaders/Tile.Frame.md) | struct |  |
| [`Tile.PSFlat`](OneGame.Render2D.Shaders/Tile.PSFlat.md) | static class |  |
| [`Tile.PSIds`](OneGame.Render2D.Shaders/Tile.PSIds.md) | static class |  |
| [`Tile.TileParams`](OneGame.Render2D.Shaders/Tile.TileParams.md) | struct |  |
| [`Tile.VSMain`](OneGame.Render2D.Shaders/Tile.VSMain.md) | static class |  |
| [`Tile.VSMain.Vertex`](OneGame.Render2D.Shaders/Tile.VSMain.Vertex.md) | struct |  |
| [`Ui`](OneGame.Render2D.Shaders/Ui.md) | static class |  |
| [`Ui.Bindings`](OneGame.Render2D.Shaders/Ui.Bindings.md) | static class |  |
| [`Ui.Bindings.Clips`](OneGame.Render2D.Shaders/Ui.Bindings.Clips.md) | static class |  |
| [`Ui.Bindings.Frame`](OneGame.Render2D.Shaders/Ui.Bindings.Frame.md) | static class |  |
| [`Ui.Bindings.Samp`](OneGame.Render2D.Shaders/Ui.Bindings.Samp.md) | static class |  |
| [`Ui.Bindings.Tex`](OneGame.Render2D.Shaders/Ui.Bindings.Tex.md) | static class |  |
| [`Ui.Frame`](OneGame.Render2D.Shaders/Ui.Frame.md) | struct |  |
| [`Ui.PSMain`](OneGame.Render2D.Shaders/Ui.PSMain.md) | static class |  |
| [`Ui.VSMain`](OneGame.Render2D.Shaders/Ui.VSMain.md) | static class |  |
| [`Ui.VSMain.Vertex`](OneGame.Render2D.Shaders/Ui.VSMain.Vertex.md) | struct |  |
