[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# Draw2D class

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)

```csharp
public sealed class Draw2D
```

Immediate-mode 2D drawing for view code: call it every frame to say what to show, and the stage batches it onto the same [`Scene2D`](../../OneGame.Render2D/OneGame.Render2D/Scene2D.md) and [`Renderer2D`](../../OneGame.Render2D/OneGame.Render2D/Renderer2D.md) a hand-built stage would use. Nothing is retained between frames except the camera. World coordinates are the renderer's: +X right, +Y down, one unit is one pixel at camera zoom 1. Draw order is by `z` (lower is behind), then call order; text is drawn over everything else, in call order. Steady frames allocate nothing (text strings that the view builds each frame are the view's own allocation).

Reach it as the parameter of an [`IView2D`](IView2D.md) entity's `Draw` method. It is view-side: read state, never write it. Use [`Draw2D.Time`](Draw2D.md#m-time) (not a clock) to animate so captures and replays repeat exactly. For more control drop down to [`Draw2D.Scene`](Draw2D.md#m-scene) and [`Draw2D.Renderer`](Draw2D.md#m-renderer).

**Example**

A sprite that moves with its synced position, animated from a sheet, with a camera that follows it and a text label:

```csharp
private SpriteSheet? _sheet;

[View]
public void Draw(Draw2D d)
{
    if (_sheet is null)
    {
        _sheet = SpriteSheet.Grid(d.Texture("art/hero.png"), 32, 32);
        _sheet.AddClip("walk", firstFrame: 0, frameCount: 4, fps: 8);
    }

    var at = new Vector2(X.ToSingle(), Y.ToSingle());
    d.Camera.Zoom = 2;
    d.Camera.Smoothing = 0.15f;
    d.Follow(at);                                           // eases the camera toward the hero
    d.FillRect(new Rect(-200, 40, 400, 8), Color.Rgb(0x335533));   // the ground
    d.Sprite(_sheet, "walk", at, scale: 1);                // plays the clip from d.Time
    d.Text("hero", at - new Vector2(0, 24), size: 12, anchor: new Vector2(0.5f, 1));
}
```

## Properties

| Member | Description |
| --- | --- |
| [`Alpha`](#m-alpha) | How far past [`Draw2D.Tick`](Draw2D.md#m-tick) the frame is, from 0 to just under 1, for blending a moving thing between ticks. |
| [`Assets`](#m-assets) | The game's assets, where [`Draw2D.Texture()`](Draw2D.md#m-texture-string-texturefilter) and the path overloads of [`Draw2D.Sprite()`](Draw2D.md#m-sprite-string-vector2-float-float-color-vector2-spriteflip-float) load from. |
| [`Camera`](#m-camera) | The camera. |
| [`DeltaTime`](#m-deltatime) | View time since the previous drawn frame, in seconds; zero when the same moment is drawn twice or time went backwards. |
| [`Describer`](#m-describer) | Draws the shapes of entities that cannot reference the host (a shared rules assembly): called with the world being drawn, after the [`IView2D`](IView2D.md) entities. |
| [`Entity`](#m-entity) | The entity being asked to draw, or default for drawing done outside an entity. |
| [`HasDrawn`](#m-hasdrawn) | True once a frame has been drawn through this object. |
| [`Height`](#m-height) | Target height in pixels. |
| [`Layer`](#m-layer) | The layer new sprites go on; the scene's default layer at the start of every frame. |
| [`ParticleEmitters`](#m-particleemitters) | The emitters handed to [`Draw2D.Particles()`](Draw2D.md#m-particles-particleemitter-vector2-texture2d-float) in the last drawn frame, in call order. |
| [`Renderer`](#m-renderer) | The renderer drawing this frame; use it to create textures ([`Renderer2D.CreateTexture()`](../../OneGame.Render2D/OneGame.Render2D/Renderer2D.md#m-createtexture-int-int-readonlyspan-byte-texturefilter)). |
| [`Scene`](#m-scene) | The low-level scene this frame is collected in. |
| [`SpriteCount`](#m-spritecount) | The number of sprites queued so far this frame (shapes count; text does not). |
| [`TextCount`](#m-textcount) | The number of text lines queued so far this frame. |
| [`Tick`](#m-tick) | The simulation tick being shown. |
| [`Time`](#m-time) | View time in seconds: the shown tick and fraction at the simulation's rate. |
| [`Width`](#m-width) | Target width in pixels. |

### <a id="m-alpha"></a>Alpha

```csharp
public Fixed Alpha { get; }
```

How far past [`Draw2D.Tick`](Draw2D.md#m-tick) the frame is, from 0 to just under 1, for blending a moving thing between ticks.

**Type:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)

### <a id="m-assets"></a>Assets

```csharp
public Assets? Assets { get; }
```

The game's assets, where [`Draw2D.Texture()`](Draw2D.md#m-texture-string-texturefilter) and the path overloads of [`Draw2D.Sprite()`](Draw2D.md#m-sprite-string-vector2-float-float-color-vector2-spriteflip-float) load from. Null when the stage was made without them.

**Type:** [`Assets`](Assets.md)?

### <a id="m-camera"></a>Camera

```csharp
public Camera2DRig Camera { get; }
```

The camera. It remembers where it is between frames; call [`Draw2D.Follow()`](Draw2D.md#m-follow-vector2) or set its properties from view code.

**Type:** [`Camera2DRig`](../../OneGame.Render2D/OneGame.Render2D/Camera2DRig.md)

### <a id="m-deltatime"></a>DeltaTime

```csharp
public float DeltaTime { get; }
```

View time since the previous drawn frame, in seconds; zero when the same moment is drawn twice or time went backwards.

**Type:** `float`

### <a id="m-describer"></a>Describer

```csharp
public Action<World, Draw2D>? Describer { get; set; }
```

Draws the shapes of entities that cannot reference the host (a shared rules assembly): called with the world being drawn, after the [`IView2D`](IView2D.md) entities. It may read any state and writes only into the [`Draw2D`](Draw2D.md).

**Type:** [`Action`](https://learn.microsoft.com/dotnet/api/system.action-2)&lt;[`World`](../../OneGame.Core/OneGame.Core/World.md), [`Draw2D`](Draw2D.md)&gt;?

### <a id="m-entity"></a>Entity

```csharp
public EntityId Entity { get; }
```

The entity being asked to draw, or default for drawing done outside an entity.

**Type:** [`EntityId`](../../OneGame.Core/OneGame.Core/EntityId.md)

### <a id="m-hasdrawn"></a>HasDrawn

```csharp
public bool HasDrawn { get; }
```

True once a frame has been drawn through this object.

**Type:** `bool`

### <a id="m-height"></a>Height

```csharp
public int Height { get; }
```

Target height in pixels.

**Type:** `int`

### <a id="m-layer"></a>Layer

```csharp
public Layer2D Layer { get; set; }
```

The layer new sprites go on; the scene's default layer at the start of every frame.

**Type:** [`Layer2D`](../../OneGame.Render2D/OneGame.Render2D/Layer2D.md)

### <a id="m-particleemitters"></a>ParticleEmitters

```csharp
public IReadOnlyList<ParticleEmitter> ParticleEmitters { get; }
```

The emitters handed to [`Draw2D.Particles()`](Draw2D.md#m-particles-particleemitter-vector2-texture2d-float) in the last drawn frame, in call order. The harness reads it.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`ParticleEmitter`](../../OneGame.Particles/OneGame.Particles/ParticleEmitter.md)&gt;

### <a id="m-renderer"></a>Renderer

```csharp
public Renderer2D? Renderer { get; }
```

The renderer drawing this frame; use it to create textures ([`Renderer2D.CreateTexture()`](../../OneGame.Render2D/OneGame.Render2D/Renderer2D.md#m-createtexture-int-int-readonlyspan-byte-texturefilter)). Null before the first frame.

**Type:** [`Renderer2D`](../../OneGame.Render2D/OneGame.Render2D/Renderer2D.md)?

### <a id="m-scene"></a>Scene

```csharp
public Scene2D Scene { get; }
```

The low-level scene this frame is collected in. Use it to drop down a layer: add layers, tilemaps or materials alongside what the calls here draw.

**Type:** [`Scene2D`](../../OneGame.Render2D/OneGame.Render2D/Scene2D.md)

### <a id="m-spritecount"></a>SpriteCount

```csharp
public int SpriteCount { get; }
```

The number of sprites queued so far this frame (shapes count; text does not).

**Type:** `int`

### <a id="m-textcount"></a>TextCount

```csharp
public int TextCount { get; }
```

The number of text lines queued so far this frame.

**Type:** `int`

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; }
```

The simulation tick being shown.

**Type:** `long`

### <a id="m-time"></a>Time

```csharp
public float Time { get; }
```

View time in seconds: the shown tick and fraction at the simulation's rate. Animations read this, so they are smooth in play, repeat exactly in captures and replays, and stand still while the game is paused. It is a view value; never store it in synced state.

**Type:** `float`

### <a id="m-width"></a>Width

```csharp
public int Width { get; }
```

Target width in pixels.

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`FillCircle(Vector2, float, Color, float)`](#m-fillcircle-vector2-float-color-float) | Fills a circle. |
| [`FillRect(Rect, Color, float, float)`](#m-fillrect-rect-color-float-float) | Fills a rectangle. |
| [`Follow(Vector2)`](#m-follow-vector2) | The camera moves toward a point, by its smoothing, using this frame's [`Draw2D.DeltaTime`](Draw2D.md#m-deltatime). |
| [`Line(Vector2, Vector2, Color, float, float)`](#m-line-vector2-vector2-color-float-float) | Draws a line with square ends. |
| [`Particles(ParticleEmitter, Vector2, Texture2D?, float)`](#m-particles-particleemitter-vector2-texture2d-float) | Shows a particle emitter at a point. |
| [`Particles(ParticleEmitter, Vector2, string, float)`](#m-particles-particleemitter-vector2-string-float) | Shows a particle emitter at a point, with an image loaded from the game's content folders; see [`Draw2D.Particles()`](Draw2D.md#m-particles-particleemitter-vector2-texture2d-float). |
| [`ScreenText(string, Vector2, float, Color?, Vector2?, string?, int)`](#m-screentext-string-vector2-float-color-vector2-string-int) | Draws one line of text at a screen pixel, unaffected by the camera. |
| [`ScreenToWorld(Vector2)`](#m-screentoworld-vector2) | The world point under a screen pixel this frame. |
| [`Sprite(in Sprite)`](#m-sprite-sprite) | Queues a fully specified low-level sprite on the current layer. |
| [`Sprite(Texture2D, Vector2, float, float, Color?, Vector2?, SpriteFlip, float)`](#m-sprite-texture2d-vector2-float-float-color-vector2-spriteflip-float) | Draws a whole texture. |
| [`Sprite(string, Vector2, float, float, Color?, Vector2?, SpriteFlip, float)`](#m-sprite-string-vector2-float-float-color-vector2-spriteflip-float) | Draws a whole image loaded from the game's content folders; see [`Draw2D.Texture()`](Draw2D.md#m-texture-string-texturefilter). |
| [`Sprite(SpriteSheet, int, Vector2, float, float, Color?, Vector2?, SpriteFlip, float)`](#m-sprite-spritesheet-int-vector2-float-float-color-vector2-spriteflip-float) | Draws one frame of a sprite sheet. |
| [`Sprite(Texture2D, PixelRect, Vector2, float, float, Color?, Vector2?, SpriteFlip, float)`](#m-sprite-texture2d-pixelrect-vector2-float-float-color-vector2-spriteflip-float) | Draws part of a texture. |
| [`Sprite(SpriteSheet, SpriteClip, Vector2, float, float, Color?, Vector2?, SpriteFlip, float, float?)`](#m-sprite-spritesheet-spriteclip-vector2-float-float-color-vector2-spriteflip-float-float) | Draws a clip of a sprite sheet at the current view time. |
| [`Sprite(SpriteSheet, string, Vector2, float, float, Color?, Vector2?, SpriteFlip, float, float?)`](#m-sprite-spritesheet-string-vector2-float-float-color-vector2-spriteflip-float-float) | Draws a named clip of a sprite sheet at the current view time. |
| [`StrokeCircle(Vector2, float, Color, float, float)`](#m-strokecircle-vector2-float-color-float-float) | Draws a circle's outline as short straight segments, enough of them to look round at the current zoom. |
| [`StrokeRect(Rect, Color, float, float)`](#m-strokerect-rect-color-float-float) | Draws a rectangle's outline, inside its bounds. |
| [`Text(string, Vector2, float, Color?, Vector2?, string?, int)`](#m-text-string-vector2-float-color-vector2-string-int) | Draws one line of text at a world point. |
| [`Texture(string, TextureFilter)`](#m-texture-string-texturefilter) | A texture loaded from the game's content folders (`art/ship.png`), cached after the first call, so it is cheap to ask for every frame. |
| [`WorldToScreen(Vector2)`](#m-worldtoscreen-vector2) | Where a world point lands on the screen this frame, in pixels. |

### <a id="m-fillcircle-vector2-float-color-float"></a>FillCircle(Vector2, float, Color, float)

```csharp
public void FillCircle(Vector2 center, float radius, Color color, float z = 0f);
```

Fills a circle.

**Parameters**

- `center` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)): Centre in world units.
- `radius` (`float`): Radius in world units.
- `color` ([`Color`](../../OneGame.UI/OneGame.UI/Color.md)): Fill colour, straight alpha.
- `z` (`float`): Draw order inside the layer.

### <a id="m-fillrect-rect-color-float-float"></a>FillRect(Rect, Color, float, float)

```csharp
public void FillRect(Rect bounds, Color color, float rotation = 0f, float z = 0f);
```

Fills a rectangle.

**Parameters**

- `bounds` ([`Rect`](../../OneGame.UI/OneGame.UI/Rect.md)): The rectangle in world units.
- `color` ([`Color`](../../OneGame.UI/OneGame.UI/Color.md)): Fill colour, straight alpha.
- `rotation` (`float`): Radians about the rectangle's centre.
- `z` (`float`): Draw order inside the layer.

### <a id="m-follow-vector2"></a>Follow(Vector2)

```csharp
public void Follow(Vector2 target);
```

The camera moves toward a point, by its smoothing, using this frame's [`Draw2D.DeltaTime`](Draw2D.md#m-deltatime).

**Parameters**

- `target` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)): The world point to centre on.

### <a id="m-line-vector2-vector2-color-float-float"></a>Line(Vector2, Vector2, Color, float, float)

```csharp
public void Line(Vector2 from, Vector2 to, Color color, float thickness = 1f, float z = 0f);
```

Draws a line with square ends.

**Parameters**

- `from` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)): Start in world units.
- `to` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)): End in world units.
- `color` ([`Color`](../../OneGame.UI/OneGame.UI/Color.md)): Line colour, straight alpha.
- `thickness` (`float`): Line width in world units.
- `z` (`float`): Draw order inside the layer.

### <a id="m-particles-particleemitter-vector2-texture2d-float"></a>Particles(ParticleEmitter, Vector2, Texture2D?, float)

```csharp
public void Particles(ParticleEmitter emitter, Vector2 position, Texture2D? texture = null, float z = 0f);
```

Shows a particle emitter at a point. Call it every frame from view code, with the emitter you keep in a field: the first call of a frame moves it to `position`, steps it by the frame's [`Draw2D.DeltaTime`](Draw2D.md#m-deltatime) and draws every live particle as a sprite on the current [`Draw2D.Layer`](Draw2D.md#m-layer), so the camera and `z` apply as for any sprite. Additive emitters use [`Renderer2D.AdditiveMaterial`](../../OneGame.Render2D/OneGame.Render2D/Renderer2D.md#m-additivematerial). Time comes from the shown tick, so captures and replays repeat exactly, and a paused game holds its particles still. An emitter that is not passed in a frame neither moves nor draws. Steady frames allocate nothing.

**Parameters**

- `emitter` ([`ParticleEmitter`](../../OneGame.Particles/OneGame.Particles/ParticleEmitter.md)): The emitter. Its settings should use [`ParticleSpace.Plane`](../../OneGame.Particles/OneGame.Particles/ParticleSpace.md#m-plane).
- `position` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)): Where it is, in world units.
- `texture` ([`Texture2D`](../../OneGame.Render2D/OneGame.Render2D/Texture2D.md)?): The image, or the sheet its settings describe; null draws a soft round spot.
- `z` (`float`): Draw order inside the layer: lower is behind.

### <a id="m-particles-particleemitter-vector2-string-float"></a>Particles(ParticleEmitter, Vector2, string, float)

```csharp
public void Particles(ParticleEmitter emitter, Vector2 position, string texturePath, float z = 0f);
```

Shows a particle emitter at a point, with an image loaded from the game's content folders; see [`Draw2D.Particles()`](Draw2D.md#m-particles-particleemitter-vector2-texture2d-float).

**Parameters**

- `emitter` ([`ParticleEmitter`](../../OneGame.Particles/OneGame.Particles/ParticleEmitter.md)): The emitter.
- `position` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)): Where it is, in world units.
- `texturePath` (`string`): Path under a content folder, like `art/spark.png`.
- `z` (`float`): Draw order inside the layer: lower is behind.

### <a id="m-screentext-string-vector2-float-color-vector2-string-int"></a>ScreenText(string, Vector2, float, Color?, Vector2?, string?, int)

```csharp
public void ScreenText(string text, Vector2 position, float size = 16f, Color? color = default, Vector2? anchor = default, string? family = null, int weight = 400);
```

Draws one line of text at a screen pixel, unaffected by the camera.

**Parameters**

- `text` (`string`): The string. Newlines are not broken.
- `position` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)): Where the anchor of the text goes, in framebuffer pixels.
- `size` (`float`): Font size in pixels.
- `color` ([`Color`](../../OneGame.UI/OneGame.UI/Color.md)?): Text colour, straight alpha; white by default.
- `anchor` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)?): The point of the text placed at `position`, as a fraction of its size: (0,0) top-left (default).
- `family` (`string?`): A font family the game added in `DefineFonts`, or null for the first.
- `weight` (`int`): Font weight, 400 regular.

### <a id="m-screentoworld-vector2"></a>ScreenToWorld(Vector2)

```csharp
public Vector2 ScreenToWorld(Vector2 screen);
```

The world point under a screen pixel this frame.

**Parameters**

- `screen` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)): Framebuffer pixels, origin top-left.

**Returns:** [`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2) — A world point.

### <a id="m-sprite-sprite"></a>Sprite(in Sprite)

```csharp
public void Sprite(in Sprite sprite);
```

Queues a fully specified low-level sprite on the current layer.

**Parameters**

- `sprite` ([`Sprite`](../../OneGame.Render2D/OneGame.Render2D/Sprite.md)): The sprite.

### <a id="m-sprite-texture2d-vector2-float-float-color-vector2-spriteflip-float"></a>Sprite(Texture2D, Vector2, float, float, Color?, Vector2?, SpriteFlip, float)

```csharp
public void Sprite(Texture2D texture, Vector2 position, float rotation = 0f, float scale = 1f, Color? tint = default, Vector2? origin = default, SpriteFlip flip = 0, float z = 0f);
```

Draws a whole texture.

**Parameters**

- `texture` ([`Texture2D`](../../OneGame.Render2D/OneGame.Render2D/Texture2D.md)): The image, from [`Renderer2D.CreateTexture()`](../../OneGame.Render2D/OneGame.Render2D/Renderer2D.md#m-createtexture-int-int-readonlyspan-byte-texturefilter).
- `position` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)): Where its origin goes, in world units.
- `rotation` (`float`): Radians about the origin; positive turns clockwise on screen.
- `scale` (`float`): World units per texture pixel.
- `tint` ([`Color`](../../OneGame.UI/OneGame.UI/Color.md)?): Colour multiplied into the texture; white (the default) draws it unchanged.
- `origin` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)?): The point of the sprite placed at `position`, as a fraction of its size; the centre by default.
- `flip` ([`SpriteFlip`](../../OneGame.Render2D/OneGame.Render2D/SpriteFlip.md)): Mirroring.
- `z` (`float`): Draw order inside the layer: lower is behind.

### <a id="m-sprite-string-vector2-float-float-color-vector2-spriteflip-float"></a>Sprite(string, Vector2, float, float, Color?, Vector2?, SpriteFlip, float)

```csharp
public void Sprite(string path, Vector2 position, float rotation = 0f, float scale = 1f, Color? tint = default, Vector2? origin = default, SpriteFlip flip = 0, float z = 0f);
```

Draws a whole image loaded from the game's content folders; see [`Draw2D.Texture()`](Draw2D.md#m-texture-string-texturefilter). The other parameters are those of [`Draw2D.Sprite()`](Draw2D.md#m-sprite-texture2d-vector2-float-float-color-vector2-spriteflip-float).

**Parameters**

- `path` (`string`): Path under a content folder, like `art/ship.png`.
- `position` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)): Where its origin goes, in world units.
- `rotation` (`float`): Radians about the origin; positive turns clockwise on screen.
- `scale` (`float`): World units per texture pixel.
- `tint` ([`Color`](../../OneGame.UI/OneGame.UI/Color.md)?): Colour multiplied into the texture.
- `origin` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)?): The point of the sprite placed at `position`, as a fraction of its size; the centre by default.
- `flip` ([`SpriteFlip`](../../OneGame.Render2D/OneGame.Render2D/SpriteFlip.md)): Mirroring.
- `z` (`float`): Draw order inside the layer.

### <a id="m-sprite-spritesheet-int-vector2-float-float-color-vector2-spriteflip-float"></a>Sprite(SpriteSheet, int, Vector2, float, float, Color?, Vector2?, SpriteFlip, float)

```csharp
public void Sprite(SpriteSheet sheet, int frame, Vector2 position, float rotation = 0f, float scale = 1f, Color? tint = default, Vector2? origin = default, SpriteFlip flip = 0, float z = 0f);
```

Draws one frame of a sprite sheet.

**Parameters**

- `sheet` ([`SpriteSheet`](../../OneGame.Render2D/OneGame.Render2D/SpriteSheet.md)): The sheet.
- `frame` (`int`): The frame number.
- `position` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)): Where its origin goes, in world units.
- `rotation` (`float`): Radians about the origin; positive turns clockwise on screen.
- `scale` (`float`): World units per texture pixel.
- `tint` ([`Color`](../../OneGame.UI/OneGame.UI/Color.md)?): Colour multiplied into the texture.
- `origin` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)?): The point of the sprite placed at `position`, as a fraction of its size; the centre by default.
- `flip` ([`SpriteFlip`](../../OneGame.Render2D/OneGame.Render2D/SpriteFlip.md)): Mirroring.
- `z` (`float`): Draw order inside the layer: lower is behind.

### <a id="m-sprite-texture2d-pixelrect-vector2-float-float-color-vector2-spriteflip-float"></a>Sprite(Texture2D, PixelRect, Vector2, float, float, Color?, Vector2?, SpriteFlip, float)

```csharp
public void Sprite(Texture2D texture, PixelRect source, Vector2 position, float rotation = 0f, float scale = 1f, Color? tint = default, Vector2? origin = default, SpriteFlip flip = 0, float z = 0f);
```

Draws part of a texture.

**Parameters**

- `texture` ([`Texture2D`](../../OneGame.Render2D/OneGame.Render2D/Texture2D.md)): The image.
- `source` ([`PixelRect`](../../OneGame.Render2D/OneGame.Render2D/PixelRect.md)): The part to draw, in texture pixels.
- `position` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)): Where its origin goes, in world units.
- `rotation` (`float`): Radians about the origin; positive turns clockwise on screen.
- `scale` (`float`): World units per texture pixel.
- `tint` ([`Color`](../../OneGame.UI/OneGame.UI/Color.md)?): Colour multiplied into the texture.
- `origin` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)?): The point of the sprite placed at `position`, as a fraction of its size; the centre by default.
- `flip` ([`SpriteFlip`](../../OneGame.Render2D/OneGame.Render2D/SpriteFlip.md)): Mirroring.
- `z` (`float`): Draw order inside the layer: lower is behind.

### <a id="m-sprite-spritesheet-spriteclip-vector2-float-float-color-vector2-spriteflip-float-float"></a>Sprite(SpriteSheet, SpriteClip, Vector2, float, float, Color?, Vector2?, SpriteFlip, float, float?)

```csharp
public void Sprite(SpriteSheet sheet, SpriteClip clip, Vector2 position, float rotation = 0f, float scale = 1f, Color? tint = default, Vector2? origin = default, SpriteFlip flip = 0, float z = 0f, float? clipTime = default);
```

Draws a clip of a sprite sheet at the current view time.

**Parameters**

- `sheet` ([`SpriteSheet`](../../OneGame.Render2D/OneGame.Render2D/SpriteSheet.md)): The sheet.
- `clip` ([`SpriteClip`](../../OneGame.Render2D/OneGame.Render2D/SpriteClip.md)): The clip, from [`SpriteSheet.AddClip()`](../../OneGame.Render2D/OneGame.Render2D/SpriteSheet.md#m-addclip-string-int-int-float-bool).
- `position` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)): Where its origin goes, in world units.
- `rotation` (`float`): Radians about the origin; positive turns clockwise on screen.
- `scale` (`float`): World units per texture pixel.
- `tint` ([`Color`](../../OneGame.UI/OneGame.UI/Color.md)?): Colour multiplied into the texture.
- `origin` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)?): The point of the sprite placed at `position`, as a fraction of its size; the centre by default.
- `flip` ([`SpriteFlip`](../../OneGame.Render2D/OneGame.Render2D/SpriteFlip.md)): Mirroring.
- `z` (`float`): Draw order inside the layer: lower is behind.
- `clipTime` (`float?`): Seconds into the clip; the view's [`Draw2D.Time`](Draw2D.md#m-time) when null.

### <a id="m-sprite-spritesheet-string-vector2-float-float-color-vector2-spriteflip-float-float"></a>Sprite(SpriteSheet, string, Vector2, float, float, Color?, Vector2?, SpriteFlip, float, float?)

```csharp
public void Sprite(SpriteSheet sheet, string clip, Vector2 position, float rotation = 0f, float scale = 1f, Color? tint = default, Vector2? origin = default, SpriteFlip flip = 0, float z = 0f, float? clipTime = default);
```

Draws a named clip of a sprite sheet at the current view time.

**Parameters**

- `sheet` ([`SpriteSheet`](../../OneGame.Render2D/OneGame.Render2D/SpriteSheet.md)): The sheet.
- `clip` (`string`): The clip's name.
- `position` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)): Where its origin goes, in world units.
- `rotation` (`float`): Radians about the origin; positive turns clockwise on screen.
- `scale` (`float`): World units per texture pixel.
- `tint` ([`Color`](../../OneGame.UI/OneGame.UI/Color.md)?): Colour multiplied into the texture.
- `origin` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)?): The point of the sprite placed at `position`, as a fraction of its size; the centre by default.
- `flip` ([`SpriteFlip`](../../OneGame.Render2D/OneGame.Render2D/SpriteFlip.md)): Mirroring.
- `z` (`float`): Draw order inside the layer: lower is behind.
- `clipTime` (`float?`): Seconds into the clip; the view's [`Draw2D.Time`](Draw2D.md#m-time) when null, so every thing plays in step. Pass `Time - startedAt` to play from a moment.

**Exceptions**

- [`UnknownClipException`](../../OneGame.Render2D/OneGame.Render2D/UnknownClipException.md): The sheet has no clip of that name.

### <a id="m-strokecircle-vector2-float-color-float-float"></a>StrokeCircle(Vector2, float, Color, float, float)

```csharp
public void StrokeCircle(Vector2 center, float radius, Color color, float thickness = 1f, float z = 0f);
```

Draws a circle's outline as short straight segments, enough of them to look round at the current zoom.

**Parameters**

- `center` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)): Centre in world units.
- `radius` (`float`): Radius in world units; the line is centred on it.
- `color` ([`Color`](../../OneGame.UI/OneGame.UI/Color.md)): Line colour, straight alpha.
- `thickness` (`float`): Line width in world units.
- `z` (`float`): Draw order inside the layer.

### <a id="m-strokerect-rect-color-float-float"></a>StrokeRect(Rect, Color, float, float)

```csharp
public void StrokeRect(Rect bounds, Color color, float thickness = 1f, float z = 0f);
```

Draws a rectangle's outline, inside its bounds.

**Parameters**

- `bounds` ([`Rect`](../../OneGame.UI/OneGame.UI/Rect.md)): The rectangle in world units.
- `color` ([`Color`](../../OneGame.UI/OneGame.UI/Color.md)): Line colour, straight alpha.
- `thickness` (`float`): Line width in world units.
- `z` (`float`): Draw order inside the layer.

### <a id="m-text-string-vector2-float-color-vector2-string-int"></a>Text(string, Vector2, float, Color?, Vector2?, string?, int)

```csharp
public void Text(string text, Vector2 position, float size = 16f, Color? color = default, Vector2? anchor = default, string? family = null, int weight = 400);
```

Draws one line of text at a world point. The size scales with the camera zoom; text is not rotated, and is drawn over sprites and shapes.

**Parameters**

- `text` (`string`): The string. Newlines are not broken.
- `position` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)): Where the anchor of the text goes, in world units.
- `size` (`float`): Font size in world units (pixels at zoom 1).
- `color` ([`Color`](../../OneGame.UI/OneGame.UI/Color.md)?): Text colour, straight alpha; white by default.
- `anchor` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)?): The point of the text placed at `position`, as a fraction of its size: (0,0) top-left (default), (0.5,0.5) centred.
- `family` (`string?`): A font family the game added in `DefineFonts`, or null for the first.
- `weight` (`int`): Font weight, 400 regular.

### <a id="m-texture-string-texturefilter"></a>Texture(string, TextureFilter)

```csharp
public Texture2D Texture(string path, TextureFilter filter = 1);
```

A texture loaded from the game's content folders (`art/ship.png`), cached after the first call, so it is cheap to ask for every frame. The texture is updated in place when the file changes during development, so sprites and [`SpriteSheet`](../../OneGame.Render2D/OneGame.Render2D/SpriteSheet.md)s over it show the new image.

**Parameters**

- `path` (`string`): Path under a content folder.
- `filter` ([`TextureFilter`](../../OneGame.Render2D/OneGame.Render2D/TextureFilter.md)): How it is filtered when scaled; [`TextureFilter.Nearest`](../../OneGame.Render2D/OneGame.Render2D/TextureFilter.md#m-nearest) for pixel art.

**Returns:** [`Texture2D`](../../OneGame.Render2D/OneGame.Render2D/Texture2D.md) — The texture.

**Exceptions**

- [`AssetException`](AssetException.md): The file is missing or damaged.
- [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception): The stage has no [`Draw2D.Assets`](Draw2D.md#m-assets).

### <a id="m-worldtoscreen-vector2"></a>WorldToScreen(Vector2)

```csharp
public Vector2 WorldToScreen(Vector2 world);
```

Where a world point lands on the screen this frame, in pixels.

**Parameters**

- `world` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)): A world point.

**Returns:** [`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2) — Framebuffer pixels, origin top-left.
