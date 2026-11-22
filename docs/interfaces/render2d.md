# render2d — the 2D renderer

`OneGame.Render2D` draws sprites, tilemaps, parallax layers, custom-shaded sprites, post-processing and the UI display list through the GPU
layer (`gpu.md`). It has no Vulkan concepts, draws into any `RenderTarget` texture (a swapchain image or an offscreen one), and reads back
through the same device for captures.

```csharp
var renderer = Renderer2D.Create(device, GpuFormat.Rgba8UnormSrgb, shaders);     // shaders: IShaderSource (see "Shaders")
var tex = renderer.CreateTexture(w, h, rgbaStraightSrgb);                          // straight alpha, sRGB bytes
var scene = new Scene2D();                                                         // layers, sprites, tilemaps
scene.Draw(scene.DefaultLayer, Sprite.Create(tex, new Vector2(10, 20)) with { Rotation = 0.3f, Tint = Color.White });

renderer.BeginFrame(target, new FrameOptions { Clear = new Color(20, 24, 30, 255) });
renderer.DrawScene(scene, camera, postChain);                                      // post chain optional
renderer.Ui.Draw(displayList, textQuadBuilder, glyphAtlas, artAtlas);              // after the world, so UI is not post-processed
var enc = device.BeginCommands();
FrameStats stats = renderer.EndFrame(enc);                                         // uploads once, records every pass
device.Submit(enc);
```

A frame is built first and recorded at `EndFrame`: nothing is written to the GPU before it, instance data and parameters go up in one write
each, and the passes are recorded in the order the draw calls were made. Steady frames allocate nothing (a test measures it).

## Conventions

- **Pixels.** The target is in framebuffer pixels, origin top-left, y down. HiDPI is the caller's: the UI list is already in physical pixels
  (`list.Scale` is only for art), and `Camera2D.Zoom` is physical pixels per world unit, so a HiDPI game multiplies its zoom by the display scale.
- **World.** +X right, +Y down, rotation in radians, positive clockwise on screen. `Camera2D.Position` is the world point at the screen centre;
  a camera with positive `Rotation` turns the view clockwise.
- **Colour.** The API takes sRGB colours with straight alpha (`OneGame.UI.Color`). Shaders work in linear light: vertex colours are decoded
  in the shader, textures are sampled from `Rgba8UnormSrgb` (decoded by the hardware), and the target must be an sRGB format (or
  `Rgba16Float`) so the hardware encodes on write; `Create` rejects other formats. All blending is linear and **premultiplied**
  (`One, OneMinusSrcAlpha`); every shader outputs `rgb * a, a`.
- **Texture upload.** `CreateTexture` takes straight-alpha sRGB bytes and premultiplies in linear light (decode, multiply by alpha, encode),
  so filtering and blending are exact. Atlas pages arrive premultiplied in encoded space and are used as stored: edge pixels with partial alpha
  carry a small error (a slightly brighter fringe). Glyph pages are coverage (`R8Unorm`) multiplied into the text colour.
- **Clear colour** is decoded to linear before it is written to the sRGB target, so `Clear = Color(r, g, b)` shows exactly those bytes.

## Sprites, layers, camera

`Sprite` (`Texture`, `Source` pixel rectangle, `Position`, `Pivot`, `Scale`, `Rotation`, `Tint`, `Z`, `Flip`, `Material`) is a value; make one
with `Sprite.Create(texture, position)`. `Scene2D` holds layers (`AddLayer(name, parallax)`; the first is `DefaultLayer`) and the entries of
the frame; `Clear()` between frames. Draw order is layer, then `Z`, then submission order. A run of consecutive entries with the same texture and
material is one draw call; 10,000 sprites from one atlas are one draw (tested). Sprites wholly off screen are skipped before upload
(`FrameStats.SpritesCulled`). A layer follows the camera position by its `Parallax` factor (1 = the world, 0 = fixed to the screen); zoom and
rotation apply to every layer. `Camera2D.PixelSnap` rounds each sprite's pivot to a whole pixel for crisp pixel art.

## Tilemaps

`TileSet` = a grid of equal tiles in one texture (`Renderer2D.CreateTileSet`); tile id 0 is empty, id n is cell n−1 left to right, top to
bottom. `TileMap(tileSet, width, height, tileSize)` stores ids in 64×64 chunks (memory only where tiles exist; up to 65,536 per side) and is
drawn with `scene.Draw(layer, map, z)`. `Origin` places its top-left corner in the world.

**Level of detail.** Near, a chunk is one quad, and the pixel shader looks up the tile id under each pixel in a pool of chunk layers
(`R32Uint` array) and samples the tileset. When a tile would be smaller than `LodThreshold` pixels (default 4), the map is drawn from level n:
one premultiplied colour per 2^n × 2^n tiles (the mean of the tiles beneath, averaged in linear light), held in a second pool. Coarse levels
are built lazily from the level below and rebuilt only for chunks whose tiles changed. So work per frame is bounded by the screen: the
visible chunks are at most about (width/256 + 2) × (height/256 + 2), one draw call, regardless of map size. `PoolChunks` (default 256) is the
number of chunks kept on the GPU per map (least recently used replaced); `TileMap.LastStats` reports level, quads, uploads and dropped chunks.
Animated tiles are not implemented.

## Custom materials

`renderer.CreateMaterial(ShaderCode pixelShader)` makes a sprite pixel shader; set `Sprite.Material`. The vertex stage is the renderer's, so
the shader takes its output and these resources:

```hlsl
[[vk::binding(0, 1)]] Texture2D Tex;           // the sprite's texture
[[vk::binding(1, 1)]] SamplerState Samp;
[[vk::binding(0, 2)]] cbuffer Params { float4 P[8]; };   // material.SetParameter(slot, value)
struct QuadOut { float4 Pos : SV_Position; [[vk::location(0)]] float2 Uv : TEXCOORD0; [[vk::location(1)]] float4 Color : TEXCOORD1; };
```

`Uv` is in texture coordinates, `Color` is the tint already in linear light and premultiplied. Return premultiplied linear colour. A
parameter changed during the frame being built is seen by every sprite of that material in the frame (one value per frame). Declare a
`P[n]` smaller than 8 if you like; the buffer is 8 × float4. See `tests/OneGame.Render2D.Tests/Shaders/Ripple.hlsl`.

`renderer.ReplaceMaterialShader(material, newPixelShader)` swaps a material's pixel shader in place: parameters, bind groups and every sprite
that uses it stay, and the next frame draws with the new code. The new shader must keep the interface the material was made with; checking that
is the caller's job (`InterfaceCompatibility.Compare`, which the host's hot reload runs). The old pipeline is released after a device wait, so
use it for development reloads, not per frame. Only materials from `CreateMaterial` can be replaced.

## Additive blending

`renderer.AdditiveMaterial` is a built-in material (pixel entry `PSAdditive` in `Quad.hlsl`): it outputs the premultiplied colour with alpha 0, so with the renderer's `One, OneMinusSrcAlpha` blend the colour is added to the target and the sprite's alpha only scales its brightness. Set it as `Sprite.Material`. Additive sprites batch with each other as any run of equal texture and material; order between them does not matter. Particles use it (`particles.md`).

## Render textures

`renderer.CreateRenderTexture(width, height, filter)` makes a `RenderTexture2D`: an image in the renderer's target format (so every shader and pipeline of the renderer draws into it), 1 to 16384 pixels a side. Its `Texture` is an ordinary `Texture2D` (made like `WrapTexture`, so sprites, tiles and the UI draw it). Two ways to draw into one, both between `BeginFrame` and `EndFrame`:

- `BeginTarget(texture, clear)` ... `EndTarget()`: everything drawn between (scenes, sprites, text, UI primitives) lands in the texture, in its pixels (the frame's size, the UI canvas's root clip and `FullScreen` quads follow the texture). `clear` is the colour to clear to first, or null to keep the contents; a texture never drawn into is cleared to transparent either way. Targets do not nest.
- `Layer2D.Target` (with `TargetClear`): a layer draws into the texture instead of the frame; `DrawScene` does the begin and end around the layer's entries. The layer's camera, viewport and screen-space rules use the texture's size. The texture is cleared by the first layer that draws into it in a frame (`TargetClear` null: never); a layer with no entries touches nothing.

Order is the order of the frame's passes: a texture is written by the passes recorded when it was drawn into and read by those recorded after, and the backend moves it between render-target and sampled use. A target that was only cleared still gets its pass, before the passes that sample it. Sampling a texture while it is the target throws `InvalidOperationException`. Textures are not part of the frame's render graph resources: `Render2DStage` runs the whole 2D frame as one graph pass (`render2d`), and the passes inside it are the renderer's own, with pooled intermediate images.

## Post-processing

`renderer.CreatePostChain()` then `AddBlur(radius)`, `AddGrade(...)`, `AddBloom(...)` and `AddMaterial(material)`, in order. A chain applies to the whole scene (`DrawScene(scene, camera, chain)`) or to one layer (`Layer2D.Post`).

- **Scene chain.** The scene is drawn offscreen (cleared to the frame's `Clear` colour so the effects see the background), each effect is one pass, and the last writes to the frame target; anything drawn after (usually the UI) is not post-processed.
- **Layer chain.** The layer is drawn into an image of its own (transparent where nothing is drawn), the chain runs over it, and the last pass blends the result, premultiplied, onto the layer's destination (the scene image or the layer's `Target`) inside the layer's scissor box. Layers above it are untouched. Text drawn on the layer (markers) goes into the layer's image too.
- **Effects.** Blur is two passes. Grade is one. Bloom: a bright pass (`PSBright`: the brightest unpremultiplied channel against a threshold with a soft ramp, in linear light) into an image `downscale` times smaller, `blurPasses` pairs of horizontal and vertical blurs, then the source is drawn to the destination and the glow added (the additive pipeline); the intensity is a gain in the bright pass, so an 8-bit renderer clips a glow stronger than 1. `AddMaterial` runs a material of the game's own over the image so far; the chain does not own it. Its shader sees the image as `Tex`/`Samp` (set 1), `P[0..7]` (set 2) and the image size as `Viewport` (set 0).
- **Cost.** Images are pooled and reused across frames and groups; nothing is allocated in steady state. A chain with no effects, and no chain, add no pass and leave the output pixel-identical. Blur and bloom set their taps from the image size when the frame is built, so one chain used on images of two sizes in one frame is not supported.

## UI

`renderer.Ui` replays a `DisplayList` (`ui-render.md`) with `Draw(list, textQuadBuilder, glyphAtlas, artAtlas)` and also draws single
primitives for HUDs and tools: `Fill` (solid or `Gradient.Linear/Radial`), `Border`, `Shadow`, `Image`, and the stacks `PushClip` /
`PushTransform` / `PushOpacity` with their pops. Stacks must balance by `EndFrame` (`InvalidOperationException` otherwise).

- **Primitives** are one instanced pixel shader (`Ui.hlsl`): a rounded box with a radius per corner, anti-aliased from its signed distance
  (one pixel ramp, correct under any transform); borders with a width per side; shadows (a logistic curve with the spread of a Gaussian,
  with the element's own box left clear); images; glyphs. 128 bytes of instance data each. Consecutive primitives with the same texture
  and clip are one draw.
- **Clips** cut with the scissor rectangle (rounded to whole pixels) and, when the clip has rounded corners or sits under a rotation or skew,
  also with an analytic rounded-box test per pixel against a clip table (4,096 entries a frame; nested clips chain up to four deep).
- **Transforms** are absolute matrices as in the display list; a nested push composes as `new * current`. Shapes keep their sharpness.
- **Opacity** at `PushOpacity` renders the group into an offscreen image the size of the target and blends it with the opacity at the pop
  (overlapping children do not show through each other). Opacity ≥ 0.999 costs nothing; opacity ≤ 0 skips the group's commands.
- **Atlases.** Pages are looked up through `GlyphAtlas`/`ArtAtlas`/`TextQuadBuilder` (`atlas.md`) and uploaded in `Draw` after the frame's
  lookups. The GPU layer writes whole mip levels, so a page that changed goes up whole (the dirty rectangle only tells the renderer
  *whether*). A missing image draws as a magenta box and counts in `Ui.MissingArt`; a refused one (`atlas.md`, "Refusals are loud") also counts in `ArtAtlas.Refusals`.
- **Not in the display list yet:** gradients (draw them through `Ui.Fill(rect, gradient)`).

## Stats

`FrameStats`: `DrawCalls`, `Passes`, `Sprites`, `SpritesCulled`, `QuadInstances`, `UiInstances`, `TileChunkQuads`, `ChunkUploads`,
`UploadedBytes`, from `EndFrame` and `renderer.LastFrame`.

## Shaders

The renderer's shaders are HLSL in `src/OneGame.Render2D/Shaders` (`Quad.hlsl`: sprite, blur, grade; `Tile.hlsl`: ids, flat; `Ui.hlsl`). It asks
an `IShaderSource` for `(program, entry)` pairs (`ShaderNames`) and gets a `ShaderCode`: the entry point with its SPIR-V, MSL and WGSL forms and layout, so the same code runs on every backend. Resource slots are fixed by the HLSL: set 0 = frame constants + clip table, set 1 = texture + sampler, set 2 = parameters (tile programs: parameters + two pools). Sets
0 and 1 are identical in every program so bind groups are shared.

The shaders are compiled during `dotnet build` by the shader build package (`RecursiveAI.OneShader.Build`, referenced by `OneGame.Render2D.csproj`),
which generates the C# bindings (`OneGame.Render2D.Shaders.Quad|Tile|Ui`). `GeneratedShaders` (`GeneratedShaders.Instance`) maps the eleven
`(program, entry)` pairs of `ShaderNames` onto the generated `Stage`/`EntryPoint`/`SpirV`; it is the default `IShaderSource` of the host, the test
cluster and every test, and the compiled shaders are embedded in the assembly, so nothing is read from disk. A custom `IShaderSource` can still be passed to
`Renderer2D.Create` or `GameOptions.Shaders`. A custom sprite material is HLSL in your own project compiled the same way (see
`tests/OneGame.Render2D.Tests/Shaders/Ripple.hlsl`); pass its `PSMain.SpirV` and `EntryPoint` to `CreateMaterial`.

The pipeline layouts and the two instance vertex layouts are derived from the generated entry layouts in `Shaders.cs` (set 0 is widened to both stages, so bind groups carry across programs; the vertex overrides say the colours are packed `UNorm8x4` in 48- and 128-byte instances). The GPU layer keeps its own `ShaderStage`, `ResourceKind` and `VertexFormat` enums, and
`GeneratedShaders` maps the stage.

## Shader hot reload (development builds)

A game that draws its own materials registers each one in its render stage: derive from `Render2DStage`, create the material in
`RendererCreated(Renderer2D)`, draw with it in `DrawOverUi(Renderer2D, in FrameInfo)`, and register it:

```csharp
HotShaders.Register(new HotShader("glow", "Shaders/Glow.hlsl", Glow.PSMain.EntryPoint, Glow.Reflection,
    spirv => renderer.ReplaceMaterialShader(material, new ShaderCode(ShaderStage.Pixel, Glow.PSMain.EntryPoint, spirv))));
```

`Glow.Reflection` is the interface the C# was built with (generated by the shader build package). A development build (`OneGame.Live`,
`OneGameDev`) polls the registered files and, when one changes, recompiles it with `ShaderCompiler.CompileFile`, compares the result with that
baseline and calls the delegate only when it is compatible. A compile error or an interface change leaves the running shader alone and is
reported by the `shaders` control operation and on standard error. Release builds register the shader and nothing else: the watcher and the
compiler call are in `OneGame.Control`, which they do not reference. Built-in shaders (`Quad`, `Tile`, `Ui`) are not reloaded.

## The happy path: draw from view code

The layer above `Scene2D` for games that just want things on screen. `Render2DStage` (the default stage) asks every entity that implements
`IView2D` to draw each frame, then `Draw2D.Describer(world, draw)` for entities that cannot reference the host, and shows the result under the
UI. No stage of your own:

```csharp
[Entity]
public partial class Hero : Entity, IView2D
{
    [Synced] public partial Fixed X { get; private set; }
    [Command] public void Go(Fixed speed) => Speed = speed;            // sim state is fixed-point; the animation is view only

    private SpriteSheet? _sheet;
    [View]
    public void Draw(Draw2D d)
    {
        _sheet ??= MySheet(d.Renderer!);                                // d.Renderer.CreateTexture(...) -> SpriteSheet.Grid(tex, 16, 16) + AddClip(...)
        var at = new Vector2(X.ToSingle(), 0);
        d.Follow(at);                                                   // camera
        d.Sprite(_sheet, "walk", at, scale: 2, flip: SpriteFlip.Horizontal);
        d.Text("hero", at - new Vector2(0, 24), 12, anchor: new Vector2(0.5f, 1));
    }
}
```

- **`Draw2D`** (`OneGame.Host`): `Sprite` (texture, source rect, sheet frame, sheet clip, or a low-level `Sprite`; each with rotation, `scale`,
  `tint`, `origin` as a fraction of the sprite, `flip`, `z`), `FillRect`, `StrokeRect`, `Line`, `FillCircle`, `StrokeCircle`, `Text` (world
  position, size in world units) and `ScreenText` (pixels). Order is layer, then `z`, then call order, and text is in that order like a sprite (each text is a marker in the `Scene2D`, handed back to `Draw2D` as `DrawScene` reaches it; sprites and text that do not interleave batch as before). Shapes are sprites of a
  1x1 and a 128 px disc texture the stage makes, so they batch with everything else; circles are exact to about a pixel up to a few hundred
  pixels across. Text is one line (no wrapping), unrotated, in the game's fonts (`family` null = the first); it reaches the screen through
  `UiCanvas.Text`, which uses the UI's glyph atlas; the atlas pages those calls change are uploaded once, at the end of the frame.
- **Layer cameras.** A `Layer2D` may have its own `Camera` (replacing the one `DrawScene` is given), a `Viewport` (a framebuffer rectangle: the camera is centred on it, drawing is scissored to it) and `ScreenSpace` (framebuffer pixels, no camera). `Layer2D.View` is the layer's matrix to pixels. `Draw2D.ScreenLayer` and `GetLayer(name)` make persistent layers; `WorldToScreen(layer, p)`/`ScreenToWorld(layer, p)` convert through them. `WarmUpText(chars, size)` rasterizes glyphs ahead of time.
- **Render textures and post chains.** `d.RenderTexture(name, w, h)` keeps a `RenderTexture2D` by name; `d.RenderLayer(name, texture, clear)` makes a layer (pixel coordinates) that draws into it; `d.Post` and `Layer2D.Post` take a chain from `d.CreatePostChain()`; see "Render textures" and "Post-processing" below.
- **View time.** `d.Time` is `(Tick + Alpha) / 60` seconds, `d.DeltaTime` the step since the previous drawn frame. Clips read it, so animation is smooth
  in play, identical in captures and replays, and frozen while the sim is paused. Pass `clipTime: d.Time - startedAt` to play from a moment.
- **`SpriteSheet`** (`OneGame.Render2D`): `Grid(texture, w, h, margin, spacing)` or `Frames(texture, rects)`; `AddClip(name, first, count, fps,
  loop)` or with an explicit frame list; `SpriteClip.FrameAt(seconds)` is a pure function. An unknown clip throws `UnknownClipException`.
- **`Camera2DRig`** (`d.Camera`): `Zoom`, `Rotation`, `Bounds`, `Smoothing` (time constant in seconds; 0 = exact), `Follow(target, dt)` /
  `d.Follow(target)`, `Snap`, `WorldToScreen`, `ScreenToWorld`, and `Current(size)` giving the low-level `Camera2D`. Smoothing makes the camera
  depend on how many frames were drawn, so a capture taken after a jump is not the same as one taken after every tick; keep it at 0 where captures
  must repeat (the clip and sheet maths are unaffected).
- **Dropping down.** `d.Scene` is the `Scene2D` of the frame, `d.Layer` the layer new sprites go on (`d.Scene.AddLayer(...)` for parallax), and
  `d.Sprite(in Sprite)` takes any hand-built sprite with a material. Tilemaps still go through `DrawOverUi` or the stage's own
  `Renderer2D`, alongside.
- **Drawing from files.** `d.Sprite("art/ship.png", pos, ...)` and `d.Texture(path, filter)` load through the game's `Assets` (`assets.md`), cached by path, cheap every frame. The texture is updated in place when the file changes in development, so a sprite, or a grid `SpriteSheet` over it, shows the new picture from the next frame; a grid sheet is cut again when the size changes (`Resized`; clip frames past the new count show the last frame), while a sheet of explicit frames keeps its rectangles. A missing or damaged file throws `AssetException`.
- **Textures** are the renderer's (`d.Renderer.CreateTexture`); a texture belongs to one renderer, so a game with several targets of different
  formats creates its sheet per renderer.
- **Rules.** Draw code is view code: mark it `[View]`; the analyzers keep it from writing synced state, and `Draw2D` has nothing that does. Keep
  GPU objects in view fields, never synced state. Steady frames allocate nothing (a test measures 60 frames); strings built per frame are yours.
- **Harness.** Captures and the scenario `capture` expectation show it with the UI. The `draw_view` operation reports the last frame's sprite and
  text counts, tick, view time and camera, and converts `screen` [x,y] to a world point and `world` [x,y] to a pixel.
  `tests/Draw2DGame` is the worked example (a walking hero, scenario `01-walk.yaml` with capture references).

## Plugging into the game host

The host's render stage hands a stage the device, a target and the frame's display list. A stage over this renderer owns the `Renderer2D`, the
atlases and the `TextQuadBuilder`, and in `Render` does `BeginFrame(target)`, `Ui.Draw(frame.List, …)`, `EndFrame(encoder)`, `Submit`. The
host's targets and swapchain must be `Rgba8UnormSrgb`/`Bgra8UnormSrgb` (`SwapchainDesc.Srgb = true`). Call `ForgetTarget(texture)` for a target
that is destroyed (swapchain resize). The CPU-only `RenderToPixels` path is not provided by this renderer.

### Drawing above the UI from a custom stage

A stage that subclasses `Render2DStage` has two ways to draw over the UI, both inside the stage's one 2D batch (no extra pass, nothing allocated per frame):

- `DrawOverUi(renderer, frame)` for sprites, tiles and shapes through the `Renderer2D` (`renderer.DrawScene`, `renderer.Ui.Fill/Image`).
- **Overlays** for text and rectangles through the shared fonts and glyph atlas: implement `IFrameOverlay` and call `stage.AddOverlay(overlay)` (`RemoveOverlay` undoes it). `Draw(OverlayCanvas)` gets `Fill`, `Outline`, `Image`, `Text` and `Measure` in framebuffer pixels (`Width`, `Height`, `Scale` say how big the frame is) and nothing else, so an overlay stays view code. Overlays draw in the order added, after the UI and `DrawOverUi`, and show in captures.

`Render2DStage.Overlay` is a separate slot held by the development panel (`DebugOverlay`, installed by the control layer); it draws last, over every overlay in the chain. A game's overlays and the panel therefore never replace each other, and the panel stays on top. Setting `Overlay` yourself replaces the panel; use `AddOverlay`.

### GPU images as textures

`renderer.WrapTexture(gpuTexture)` makes a `Texture2D` over an image someone else owns (for example `Offscreen3D.Target`, made with `sampled: true`), with no copy and no readback. Sprites, tile sets, `Ui.Image` and `OverlayCanvas.Image` draw it like any texture.

- **Ownership.** `Texture2D.Dispose` frees the sampler and bind group only. The owner disposes the image after the wrapper and after the last frame that drew it.
- **Resize or recreate.** `renderer.RebindTexture(texture, newImage)` points the same `Texture2D` at the new image (any size or format that `WrapTexture` accepts); sprites holding it draw it from the next frame. It waits for the device, so it is for resizes, not per frame. `Texture2D.Width/Height` follow.
- **Colour.** Shaders sample linear light and treat texels as premultiplied. A `*Srgb` image (`Offscreen3D`'s default) is decoded by the hardware; `Rgba8Unorm` and `Rgba16Float` are taken as linear. The image's format does not need to match the target's, so a windowed capture in the swapchain's format draws it the same way. An opaque image needs nothing more; an image with partial alpha must be premultiplied already.
- **Order.** Render into the image before the frame that samples it (a submitted `Offscreen3D.Render`, or an earlier graph pass). The backend's whole-resource tracker moves it between render-target and sampled use; the 2D pass does not declare the read, so rendering and sampling the same image inside one graph pass is not supported.
- **Refused** with `ArgumentException`: images without `TextureUsage.Sampled`, array, cube, 3D, multisampled and depth images.
- **Backends.** The code uses only sampled-texture binding in the GPU layer, so it is not Vulkan-specific; it compiles for Metal and WebGPU, and its behaviour there is unchecked (the pixel-equality tests ran on Vulkan).
