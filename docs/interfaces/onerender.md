# onerender — OneRender pages as game screens

`OneGame.Pages` shows a [OneRender](https://github.com/recursive-ai-oss/onerender-csharp) page (HTML and CSS through OneHtml, or a `StyledTree` built in C#) as a screen of a game, drawn through the render graph over the `OneGame.UI` tree and the game's 2D pass. OneRender does layout, style, animation and input handling and produces a display list (format 1.3); this layer draws that list, feeds it input and time, and exposes it to the harness. The adapter is view-only: it reads nothing from the simulation, writes nothing to it, and changes no state hash. `OneGame.UI` stays; a game can use either, or both.

```csharp
var fonts = new PageFonts(game.Fonts);                          // the same OneFont faces as OneGame.Text
fonts.Add(File.ReadAllBytes("fonts/Body.ttf"), "Body");         // the page names it: font: 16px Body
var images = new PageImages(game.Art);                          // addresses a page can name
images.Add("coin.png", File.ReadAllBytes("art/coin.png"));

game.Pages.Show("menu", PageScreen.FromHtml("""<button id=start>Play</button>""", new PageOptions(fonts)
{
    Images = images,
    Setup = s => s.Interactive!.Events.AddEventListener(      // runs on every (re)creation: listeners survive a resize
        EventTarget.Of(s.Interactive.FindElement("start")), "click", e => Meter.Of(game.World).Add(1)),
}));
```

DOM handlers are OneRender's own `OneRender.Interaction.EventListener` registered with `page.Events.AddEventListener`. The adapter has no handler type of its own. A handler runs from input the game receives, so it is view code: it reads state and sends commands.

## Types (`OneGame.Pages`)

| Type | Role |
|---|---|
| `PageFonts` | A OneRender `FontCatalog` over the game's `FontLibrary`. `Add(bytes, family)` loads a face once and registers the same `OneFont.FontFace` in both. `Provider` is the `IFontProvider` pages take. |
| `PageImages` | OneRender's `IImageSource`. `Add(address, bytes)` registers a raster or SVG file in the game's `ArtAtlas` and returns the `ImageHandle` pages use; `Replace(address, bytes)` swaps the pixels. |
| `PageOptions` | Fonts, images and `Setup(PageScreen)`, which runs each time the page is created. |
| `PageScreen` | `FromHtml(html, options)` / `FromTree(tree, options)`. `Visible`, `Order`, `Interactive` (`InteractivePage`) or `Live` (`LivePage`), `Viewport`, `Creations`, `Has/TextOf/BoundsOf/StatesOf/Click(name)`, `Covers(x, y)`, `Refresh()`. |
| `PageSet` | `game.Pages`: `Show(name, screen)`, `Get`, `Remove`, `SetVisible`, `Screens` (shown, back to front), `Scopes`, `Refresh()`; the host drives `Prepare`, `Advance` and the input methods. |
| `PageScopes` | `Layer(n, PageScopes.Under \| Over)` and `Additive(n)` for `--host-scope: n`. |
| `PageRenderer` | The GPU side. `PageRenderer.Create(device, targetFormat, glyphAtlas, artAtlas)`; `AddPasses(graph, target, screens, scopes, PageFrame)` adds the passes for one layer; `Stats` (`PageStats`) says what was drawn. `Render2DStage` owns one per target format and calls it. |

`PageScreen.Creations` counts how often the page was made. An HTML page cannot resize, so it is created again when the window size or UI scale changes (the viewport is `width / scale` CSS px and OneRender's `DevicePixelRatio` is the scale); focus, scroll and field values are lost then, and `Setup` runs again. A builder page resizes in place.

## Drawing

`Render2DStage` draws pages when `Pages` is set (`GameInstance` sets it). Order in a frame: the `PageScopes.Under` layer (only when some scope is bound to it), the 2D pass (sprites, `Draw2D`, the `OneGame.UI` tree), then every other page content (layer 0). So page content normally covers the game, and a scope bound to `Under` sits beneath the UI. The debug overlay draws in the 2D pass, below pages. The CPU fallback used on machines with no GPU draws no pages.

- **Colour.** Pages blend in encoded sRGB as CSS does, in transient `Rgba8Unorm` layers, and one last pass converts and lays the result over the game's sRGB target. Opaque page pixels come out as in a browser and match OneRender's reference rasterizer; translucent ones blend in linear light with the game's pixels at that last step.
- **Items.** All of format 1.3: rectangles, rounded rectangles with elliptical corners, paths, borders (solid, dashed, dotted, double, groove, ridge, inset, outset, per-side colours), linear/radial/conic and repeating gradients, outer and inset box shadows, images (smooth, pixelated, tiled, nine-slice), glyph runs, inline SVG, rectangle/rounded/path clips, 2D and 3D transforms, groups (opacity and every blend mode, plus-lighter), masks, the filter functions, backdrop filters, host scopes. A list with a newer `RequiredMinor` throws `NotSupportedException`.
- **Passes.** Rectangles, borders, shadows, gradients, images and glyphs are instanced quads of one shader (`Shaders/Page.hlsl`). A path, mask, filter, backdrop filter, non-normal blend, 3D transform, path clip or group with opacity below one opens a layer, drawn and composited by passes named `page.layer`, `page.composite`, `page.blend`, `page.mask`, `page.filter`, `page.blur`, `page.shadow`, `page.backdrop`, `page.project`, `page.add`, `page.present`. They appear in the `render_graph` operation and share transient memory through the graph's pool. A group with one plain leaf folds its opacity into the leaf and opens no layer.
- **Atlases.** Glyphs go through the game's `GlyphAtlas` (OneRender's faces are registered by their `FaceKey`; quarter-pixel across, whole pixels down, as UI text); images and SVG through the `ArtAtlas` (SVG is rasterized at the size it is drawn, `currentColor` resolved to the item's colour, alpha ignored). `PageImages.Replace` takes effect on the next frame (pixels are read every frame); call `PageSet.Refresh()` when the image's size changed. Gradients use a 1024 × 256 ramp texture, path coverage bitmaps are cached (256 entries, four idle frames). Atlas clocks use `Renderer2D.FrameNumber`.
- **Approximations.** Dashed and dotted borders run along each side's axis and start at a different phase than the reference rasterizer. Box shadows use a logistic falloff of the signed distance (sigma = blur / 2), not an exact Gaussian. Path coverage is eight sub-scanlines. Colour-emoji bitmaps are not drawn. Test pages stay within 8/255 of the reference rasterizer except on text, dashes and shadow edges (maximum 58–204 on single edge pixels, under 3 % of pixels above 8).
- **Allocation.** A steady frame (no edits) allocates 0 B in the adapter (`AFrameThatChangesNothingAllocatesNothing`). OneRender's own cost is in its `docs/game-hosts.md`.

## Time

`GameInstance` advances the page clock with the same view-time delta the tweens get (`(tick + FrameAlpha) / TickRate`), passing the sum in milliseconds to `InteractivePage.Advance`. CSS transitions and animations therefore move with the simulation's ticks: a stepped or recorded run shows the same frames, and `tweens` with `advance: N` moves pages too, which is how a capture looks at a transition's midpoint. Builder pages flush on each advance. A page created at clock *t* starts its animations at *t*.

## Input

`GameInstance` offers pointer, wheel, key and text events to the shown screens, topmost first, before `OneGame.UI` sees them.

| Event | HTML page | Builder page |
|---|---|---|
| pointer move / down / up | `HandlePointer`: hover, active, click, focus. Taken when an element other than `html`/`body` is under the pointer, so a transparent page lets clicks through | none |
| wheel | `HandleWheel` (scroll containers); taken like pointer events | none |
| key | `HandleKey`; taken only while an element of the page has focus | none |
| text | `InsertText` into the focused field | none |

A builder page draws and can be found by name (`Has`); it takes no input yet, so a game drives its tree itself.

## Host scopes

`--host-scope: N` (or `StyleDeclaration.SetHostScope(N)`) marks a subtree; `game.Pages.Scopes` gives N its meaning. Unbound, the scope is ignored.

| Binding | Effect |
|---|---|
| `Layer(n, PageScopes.Under)` | the subtree is drawn before the 2D pass, so sprites and `OneGame.UI` draw over it. Enclosing transforms and clips apply; enclosing groups, filters and masks do not. |
| `Layer(n, PageScopes.Over)` | the default: drawn with the page, over the 2D pass |
| `Additive(n)` | drawn into its own layer and added to what is below (`dst + src`), for glows |

## Harness

The `ui_query`, `ui_click` and `ui_invoke` operations, and the scenario steps `click:`, `invoke:` and `ui:` that use them, find page elements **by name** (an HTML `id`, a form control's `name`, or `#name`) when the `OneGame.UI` tree has no element of that name. A query returns `kind: "page"`, `id`, `screen`, `text` (a field's value for `input`/`textarea`), `states` (`hover active focus`) and `bounds` in device pixels. `ui_click` sends real pointer events through the platform queue at the element's centre, so hit testing, hover, focus, transitions and handlers run as for a player; it fails with `obscured` when the centre is not over the page. `ui_invoke` calls OneRender's `Click`. `type:` goes to the focused page field. `pages` lists the screens (name, kind, order, visible, creations, viewport, animating) and the renderer's counters (passes, instances, layers, dropped, missingImages, refusedGlyphs, pathBitmaps, artPages, glyphPages). Captures include pages. `ui_tree` and `ui_hit` do not look into pages.

## Limits

OneRender bounds its own input (depth, items, scopes, SVG size). The adapter adds: 512 SVG keys per renderer, 4096 images per `PageImages`, 1,048,576 draw instances and 4096 clips per plan, 256 gradient-ramp rows kept per frame (the table restarts when nearly full), 4096 external font faces. Excess is skipped and counted in `PageStats.Dropped`, never thrown from the draw path.

## Not built (follow-ups)

Material scopes (a 2D material's pixel shader over a subtree); gamepad and spatial focus navigation; clipboard wiring and IME composition for page fields; pointer, key and name-based click for builder pages; `ui_tree` and `ui_hit` for pages; dirty-region redraw (the walker rebuilds the plan every frame); Metal and WebGPU runs (the shader compiles for both; only Vulkan was run); an `Under` layer on the CPU fallback.
