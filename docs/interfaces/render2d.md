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

## Post-processing

`renderer.CreatePostChain().AddBlur(radius).AddGrade(exposure, contrast, saturation, vignette, tint, tintAmount)` and pass it to
`DrawScene`. The scene is drawn offscreen, each effect is one full-screen pass (blur is two), and the last writes to the frame target; the
frame's `Clear` colour fills the scene first so the effects see the background. Effects are materials with built-in shaders, so a chain costs
one pass per effect and two offscreen textures (reused across frames and groups).

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
  *whether*). A missing image draws as a magenta box and counts in `Ui.MissingArt`.
- **Not in the display list yet:** gradients (draw them through `Ui.Fill(rect, gradient)`).

## Stats

`FrameStats`: `DrawCalls`, `Passes`, `Sprites`, `SpritesCulled`, `QuadInstances`, `UiInstances`, `TileChunkQuads`, `ChunkUploads`,
`UploadedBytes`, from `EndFrame` and `renderer.LastFrame`.

## Shaders

The renderer's shaders are HLSL in `src/OneGame.Render2D/Shaders` (`Quad.hlsl`: sprite, blur, grade; `Tile.hlsl`: ids, flat; `Ui.hlsl`). It asks
an `IShaderSource` for `(program, entry)` pairs (`ShaderNames`) and gets SPIR-V and an entry point name. Resource slots are fixed and written
in `Shaders.cs`: set 0 = frame constants + clip table, set 1 = texture + sampler, set 2 = parameters (tile programs: parameters + two pools). Sets
0 and 1 are identical in every program so bind groups are shared.

Until the shader build package ships, the tests compile these same files with the Khronos reference compiler
(`tests/OneGame.Render2D.Tests/Shaders/build.sh`, glslang's HLSL front end plus `spirv-val`) and inject the result through `IShaderSource`. Nothing
compiled that way is in `src/`. **Switching to the generated bindings is one step:** add the shader build package to `OneGame.Render2D.csproj`
(it compiles every `*.hlsl` in the project) and add

```csharp
internal sealed class GeneratedShaders : IShaderSource
{
    public ShaderCode Load(string program, string entry) => (program, entry) switch
    {
        ("Quad", "VSMain") => new(Quad.VSMain.Stage, Quad.VSMain.EntryPoint, Quad.VSMain.SpirV.ToArray()),
        // … one line per pair in ShaderNames (Quad: VSMain PSSprite PSBlur PSGrade; Tile: VSMain PSIds PSFlat; Ui: VSMain PSMain)
        _ => throw new ArgumentException($"no shader {program}.{entry}"),
    };
}
```

and pass it to `Renderer2D.Create`. The pipeline layouts come from the tables in `Shaders.cs`, not from `Bindings.All`, so used-only binding lists
do not matter; a check that `Bindings.All` agrees with those tables per program is worth adding when the package is in.

## Plugging into the game host

The host's render stage hands a stage the device, a target and the frame's display list. A stage over this renderer owns the `Renderer2D`, the
atlases and the `TextQuadBuilder`, and in `Render` does `BeginFrame(target)`, `Ui.Draw(frame.List, …)`, `EndFrame(encoder)`, `Submit`. The
host's targets and swapchain must be `Rgba8UnormSrgb`/`Bgra8UnormSrgb` (`SwapchainDesc.Srgb = true`). Call `ForgetTarget(texture)` for a target
that is destroyed (swapchain resize). The CPU-only `RenderToPixels` path is not provided by this renderer.
