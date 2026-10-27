# onerender — OneRender pages as game screens

`OneGame.Pages` draws a [OneRender](https://github.com/recursive-ai-oss/onerender-csharp) page (a `StyledTree` built in C#, or HTML and CSS through OneHtml) as a screen of a game, on the render graph, next to or instead of `OneGame.UI` screens. OneRender does layout, style, animation and input handling and produces a display list (format 1.3); this layer draws that list, feeds it input and time, and exposes it to the harness. The adapter is view-only: it reads nothing from the simulation, writes nothing to it, and changes no state hash.

```csharp
var fonts = new PageFonts(game.Fonts);                         // OneFont faces shared with OneGame.Text
fonts.Add(File.ReadAllBytes("fonts/Body.ttf"), "Body");
var images = new PageImages(game.Art);                          // image/SVG addresses a page can use
images.Add("coin.png", File.ReadAllBytes("art/coin.png"));

var screen = PageScreen.FromHtml("""<button name=start>Play</button>""", new PageOptions(fonts) { Images = images });
game.Pages.Show("menu", screen);                                // drawn from the next frame; input goes to it first
screen.Interactive!.Events.AddEventListener(EventTarget.Of(screen.Interactive.FindElement("start")), "click", e => game.OpenScreen("play"));
```

A builder page is `PageScreen.FromTree(tree, options)`. Both kinds expose the OneRender objects (`Live`, `Interactive`) for everything the wrappers do not cover.

## Types (`OneGame.Pages`)

| Type | Role |
|---|---|
| `PageFonts` | A OneRender `FontCatalog` plus the game's `FontLibrary`; `Add` loads each face once and puts the same `OneFont.FontFace` object in both, so a page and an `OneGame.UI` screen shape with the same faces. `Provider` is the `IFontProvider` pages take. |
| `PageImages` | `IImageSource` for pages. `Add(address, bytes)` registers a raster file or SVG in the game's `ArtAtlas` and hands out an `ImageHandle`; `Replace(address, bytes)` swaps the pixels and returns the number of items to redraw (`InvalidateImage` on every page that shows it). |
| `PageScreen` | One page: `FromTree` / `FromHtml`, `Visible`, `Order`, `Viewport` (CSS px), `FindByName`, `Click(name)`, `TextOf(name)`, input entry points, `Advance(viewSeconds)`. |
| `PageSet` | `game.Pages`: the shown screens in draw order, `Show/Hide/Remove/Get`, `Scopes`, input routing and the clock. |
| `PageScopes` | Binds host-scope numbers (`--host-scope: 3`) to a draw layer, a built-in blend (`Additive`) or a `Material`. |
| `PageRenderer` | The GPU side: builds the graph passes of a screen into a render graph. One per device and target format; `Render2DStage` owns one. |

## Drawing

`PageRenderer.AddPasses(graph, target, screenOrSet, layer, in PageFrame)` adds the passes that draw one layer of the visible screens onto `target` (a graph texture, loaded, not cleared). `Render2DStage` calls it for every layer in ascending order after the world and the `OneGame.UI` pass, so pages are on top; a stage that wants the UI around a 2D pass calls it itself between its own passes (`Render2DStage.DrawPagesAt`, see below).

- **Colour.** Pages blend in encoded sRGB as CSS does, so a page looks as in a browser and matches OneRender's reference rasterizer; the game's targets are sRGB and blend in linear light. A page is drawn into transient `Rgba8Unorm` layers and converted once when it is laid over the target. Opaque pixels round-trip exactly; translucent page pixels over the game blend linearly at that last step.
- **Units.** The page lays out in CSS px; `PageFrame.Scale` is device pixels per CSS px (the game's UI scale), so the viewport is `width / scale` CSS px and OneRender's `DevicePixelRatio` is the scale.
- **Items.** Every item kind of format 1.3 is drawn: rectangles, rounded rectangles with elliptical corners, paths, borders (solid, dashed, dotted, double, groove, ridge, inset, outset, per-side colours and widths), linear/radial/conic gradients (repeating, any stop count), box shadows (outer and inset), images (smooth and pixelated, tiled), glyph runs (including blur), inline SVG, rectangle/rounded/path clips, 2D transforms, 3D transforms with perspective, groups (opacity and all blend modes including plus-lighter), masks (alpha and luminance layers with the four composite operators), filters (all ten operations), backdrop filters and host scopes. A list whose `RequiredMinor` is above 3 throws `NotSupportedException` naming the version.
- **Where the work goes.** Rectangles, borders, shadows, gradients, images and glyphs are instanced quads of one shader into the current layer. A path, a mask, a filter, a backdrop filter, a non-normal blend, a 3D transform or a group with opacity below one opens a layer: a transient texture (`page.layer`), drawn by its own pass and composited into its parent by another pass (`page.blend`, `page.mask`, `page.filter`, `page.blur`, `page.backdrop`, `page.project`). All of them are visible in the `render_graph` operation and share memory through the graph's pool.
- **Atlases.** Glyph runs go through the game's `GlyphAtlas`, keyed by OneRender's `FaceKey`, size × scale and glyph id (the same face object as `OneGame.Text` uses). Images and SVG go through the `ArtAtlas`: an image by its handle, an SVG by its `SvgKey` and size × scale. Gradients use a ramp texture, one row per distinct gradient.
- **Approximations.** Glyph positions are quantised to a quarter pixel across and whole pixels down, as in the UI's text. Box shadows use the same analytic falloff as the UI (sigma = blur / 2) rather than an exact Gaussian. Dashed and dotted borders run along each side's axis instead of following rounded corners. Colour emoji bitmaps are drawn from outlines only. The test page compares against the reference rasterizer within the tolerance stated in `tests/OneGame.Pages.Tests`.
- **Allocation.** A steady frame (no edits) allocates 0 B in the adapter; OneRender itself allocates 0 B on its builder path and for the usual interactive edits (see OneRender's `docs/game-hosts.md`). Layers, bind groups and pipelines are pooled and cached by the physical texture.

## Time

`PageSet.Advance(viewSeconds)` is called by the host once per frame with the game's **view time** (`(tick + FrameAlpha) / TickRate`, the clock `game.Tweens` runs on) and passes `viewSeconds * 1000` to `InteractivePage.Advance` as `nowMs`. CSS transitions and animations therefore advance with the simulation's ticks: a stepped or recorded run shows the same frames. `game.AdvanceView(seconds)` moves page clocks too, which is how a capture looks at a transition's mid-point.

## Input

`GameInstance` offers every platform event to the shown screens, topmost first, before `OneGame.UI` sees it. A screen consumes pointer events only where its page has an element under the pointer that is not the root (a transparent page lets clicks through); keys and text go to the focused page.

| Platform event | HTML page (`InteractivePage`) | Builder page (`LivePage`) |
|---|---|---|
| pointer move / down / up | `HandlePointer` (hover, active, click, focus, selection) | hit test; `:hover`/`:active` set on the node; a registered `OnClick(name, handler)` fires on release over the same node |
| wheel | `HandleWheel` | scrolls the nearest scroll container the hit element sits in |
| key down / up | `HandleKey` | Tab/Shift+Tab/arrows move focus among registered click targets, Enter and Space activate |
| text input | `InsertText` on the focused editable element | ignored |
| IME composition | `CompositionStart/Update/End` | ignored |
| clipboard | `PageScreen.Clipboard` (set from the platform's) | none |
| gamepad d-pad / confirm / cancel | focus movement (spatial among `GetTabOrder()` elements by bounding box), `Click` on the focused element, `Escape` key event | as keys |

OneRender has Tab-order focus but no spatial focus; the adapter adds directional moves for HTML pages from element rectangles. The text-input area for the platform's input method comes from `InteractivePage.TryGetCaretRect`.

## Host scopes

`--host-scope: N` (or `StyleDeclaration.SetHostScope(N)`) marks an element's subtree. `game.Pages.Scopes` decides what N means:

| Binding | Effect |
|---|---|
| `Scopes.Layer(n, layer)` | the subtree is drawn when `layer` is drawn instead of with layer 0, so a game can put 2D or 3D passes between page layers. Enclosing transforms and clips apply; enclosing groups, filters and masks do not. |
| `Scopes.Additive(n)` | the subtree is drawn into its own layer and added to its parent (`dst + src`), for glows. |
| `Scopes.Material(n, material)` | the subtree is drawn to a layer, passed through the `Renderer2D.Material`'s pixel shader (`Tex` is the layer, parameters as for sprites) and composited. |
| unbound | drawn as if the scope were not there. |

## Harness

All `ui` operations and scenario steps that take an id work on page elements by **name**: `FindByName` for builder pages (`StyledTree.SetName`), the element's `name`/`id` attribute through `InteractivePage.FindElement` for HTML. `ui_tree`, `ui_query`, `ui_click`, `ui_tap`, `ui_invoke`, `ui_hit` look in `OneGame.UI` first, then in the pages. `#name` selectors match page elements by name (no other selector syntax). A match reports `kind`, `id`, `text`, `states` (`hover active focus disabled hidden`), `bounds` and `screen` in physical pixels. `click:` sends real pointer events at the element's centre through `HandlePointer`, so hover and focus states, hit testing and transitions run as for a player; `invoke:` calls OneRender's `Click`. `ui:` expectations (`count`, `text`, `contains`, `visible`, `enabled`, `focused`) read the same data. `pages` lists the shown screens (name, kind, viewport, generation, whether animating, items, layers, atlas counters); captures include pages.

## Limits

OneRender bounds its own input (depth, items, scopes, SVG size). The adapter adds: 512 distinct SVG keys per renderer, 4096 images per `PageImages`, 64 layers and 4096 gradient rows per frame, 1,048,576 draw instances per frame; excess is skipped and counted in `pages` (`dropped`), never thrown from the draw path.
