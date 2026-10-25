# Atlases: glyphs, SVG and images for the 2D renderer

Two projects hold the CPU side of text and vector art. `OneGame.Atlas` has the page and packing types and the SVG/image atlas (it
references OneImage and OneImage.Svg). `OneGame.Text` has the font library, the UI text measurer, the glyph atlas and the quad
builder (it references OneFont, `OneGame.UI` and `OneGame.Atlas`). Neither touches the GPU: the renderer uploads pages and draws quads.

## Pages

`AtlasPages` (one per atlas) owns equal-sized square pages created on demand, up to `AtlasOptions.MaxPages` (default 4 of 1024 px).
Each `AtlasPage` has `Width`, `Height`, `Format`, `Stride`, `Pixels` (row-major, top row first, tightly packed), `Version` and a dirty
rectangle.

| Atlas | Format | Texel meaning |
|---|---|---|
| glyphs (`GlyphAtlas.Pages`) | `R8` | coverage, 0 to 255. Not alpha-premultiplied: there is no colour. |
| SVG and images (`ArtAtlas.Pages`) | `Rgba8Premultiplied` | R, G, B already multiplied by A. Not sRGB-converted: sample as stored. |

**Uploading.** Every frame, for each page index `0..PageCount-1`: create the texture when the index is new (a new page starts fully
dirty); `rect = page.TakeDirty()`; if `!rect.IsEmpty`, copy the rows of `rect` (`page.RowOf(rect, row)`) to the texture. Keep one texture
per page index. When `pages.Epoch` changes, a page was cleared (see below); the dirty rectangle then covers the whole page, so the same
loop is enough, but quads built earlier must be rebuilt.

**Frames and eviction.** Call `BeginFrame(n)` on the atlas once per frame with a growing number before looking anything up. Entries are
never moved. When every page is full the least recently used page is cleared as a whole and its entries are forgotten (the next lookup
rasterizes them again). A page that served a lookup in the current frame is never cleared, so within one frame glyphs already handed
out stay valid; when nothing can be cleared the lookup returns `AtlasStatus.Full` and the caller skips that glyph for the frame.

**Padding.** Every entry has a transparent 1 px border (zeroed), so bilinear sampling inside the entry's rectangle never reads a
neighbour. Texture coordinates `U0,V0,U1,V1` are the entry's own pixels divided by the page size, without the padding.

## Text: glyph records and quads

Representation: **coverage bitmaps at the exact pixel size**, not SDF. UI text is small, upright and drawn at the player's UI scale, and
hinting-free exact-area coverage looks right at those sizes where a distance field rounds corners; large decorative text can use an
SDF path later without changing these types. Glyphs are rasterized at 1/64 px size resolution and four horizontal subpixel positions
(`GlyphAtlas.SubpixelSteps`); vertically the baseline is rounded to a whole pixel.

`GlyphAtlas.TryGet(faceId, glyphId, size, subpixelStep, out GlyphInfo)` returns `GlyphInfo`: `Page`, `X`, `Y`, `Width`, `Height` (pixels
in the page), `U0 V0 U1 V1`, and `OffsetX`, `OffsetY` (bitmap top-left relative to the pen's whole-pixel position on the baseline, y
down). `Width == 0` means no ink (a space): draw nothing.

Normally the renderer does not call this: `TextQuadBuilder(measurer, glyphAtlas).Build(list, runIndex, quads)` (or `BuildAll`) turns the
`TextRun`s of a `DisplayList` into `GlyphQuad`s: `Page`, screen rectangle `X0 Y0 X1 Y1` (whole pixels), `U0 V0 U1 V1`, and `Color`
(straight RGBA from the run). It returns the number of glyphs the atlas refused (0 normally). Quads are in the display list's screen
space; clip, transform and opacity commands apply to them like any other command.

**What the renderer does with a quad:** bind the page texture for `Page`; fragment colour = `Color.rgb`, alpha = `Color.a ×
texture.r`; blend source-over with straight alpha (`SrcAlpha, OneMinusSrcAlpha`). Sample with nearest or bilinear filtering at exactly
1:1 scale; under a `PushTransform` that scales or rotates, use bilinear. Cache the quads while `list.Version` and `pages.Epoch` are both
unchanged.

**Measurement and drawing share one shaping.** `FontTextMeasurer` implements `ITextMeasurer`. The line breaks, line widths and the glyph
positions of `Place`/`TextQuadBuilder` come from the same cached shaping (OneFont: bidi, fallback, ligatures, kerning; line breaks at
UAX #14 opportunities; CSS whitespace collapsing in `normal`/`nowrap`, preserved in `pre`). A `PlacedLine`'s `Start`/`Length` index the
run's string; the renderer never re-wraps. Glyphs of a line are in display order (UAX #9 L2) with x from the line's left end.

## SVG and images

`ArtAtlas.AddSvg(id, bytes, NineSlice slice = default)` and `AddImage(...)` register a source under the id that display list `Image`
commands carry in `Resource`. Registration reads the size only; a damaged or oversized file throws `AtlasAssetException`. Rasterizing is
lazy:

- `TryGetScaled(id, list.Scale, out ArtRegion)`: an SVG at its natural size × the UI scale (rounded to whole pixels), re-rasterized the
  first time a new scale is asked for; a raster image at its own size.
- `TryGet(id, widthPx, heightPx, out ArtRegion)`: an SVG at exactly that size (use the element's pixel box for stretched art).
- `TryGetFitted(id, boxWidthPx, boxHeightPx, scale, out ArtRegion)`: what the renderer calls for an image drawn into a box. An SVG in a box over `StandaloneThreshold` on a side whose size is more than a pixel off its natural size times `scale` is rasterized at the box size; everything else is `TryGetScaled`. A refused exact size falls back to the scaled one.

**Standalone textures.** A raster with a side over `ArtLimits.StandaloneThreshold` (default 512, never more than a page holds) is not packed: `region.Standalone` is a non-zero handle, `Page` is -1, the rectangle is the whole texture and `U0 V0 U1 V1` are `0 0 1 1`.
`TryGetStandalone(handle, out StandaloneArt)` gives its size and its pixels (premultiplied RGBA, tightly packed). The renderer creates one texture per handle, writes `Pixels`, calls `MarkUploaded()` (which drops the CPU copy, so each large raster is held once) and draws the region like a page entry.
After each frame's lookups call `TakeReleased(list)` and dispose the textures of the handles it returns (the atlas gave them up to stay inside `MaxStandaloneBytes`, or the resource was replaced). `StandaloneCount` and `StandaloneBytes` report what is held.
Eviction is least recently used, and a texture looked up in the current frame (`BeginFrame`) is never evicted: when the frame itself needs more than the budget the lookup returns `AtlasStatus.Full` and the caller skips that image for the frame.

`ArtRegion`: `Page`, `X Y Width Height`, `U0 V0 U1 V1`, `Slice`, `Standalone`. The renderer draws the region as a textured quad over the command's
`Bounds`, tinted by `Color` (multiply the premultiplied texel by the tint, alpha included), blending source-over premultiplied
(`One, OneMinusSrcAlpha`).

**Nine-slice.** `NineSlice(Left, Top, Right, Bottom)` is registered with the source, in the source's own pixels; `ArtRegion.Slice` is
that scaled to the raster's pixel size (so a 2× raster has 2× insets). The text form is `"l t r b"` (`ToString`, `NineSlice.TryParse`; one
or two numbers are accepted). A non-zero `DrawCommand.Slice` (insets in screen pixels) draws the region as nine parts: corners at their
size, edges stretched along one axis, the middle in both, with texture coordinates inside the region's own `U/V` rectangle.

## Budgets (checked before any decode, rasterization or allocation)

| Budget | Default | On breach |
|---|---|---|
| SVG file / image file | 4 MiB / 32 MiB | `AtlasAssetException` at registration |
| SVG natural size per side | 16384 | `AtlasAssetException` at registration |
| Raster image size | 8192 per side, 16 Mpx | `AtlasAssetException` at registration |
| Registered resources | 4096 | `AtlasAssetException` |
| One raster (`TryGet*`) | 8192 per side, 16 Mpx, and inside `MaxStandaloneBytes` | `AtlasStatus.TooLarge` |
| Standalone textures held at once | 256 MiB (`MaxStandaloneBytes`) | least recently used released; `AtlasStatus.Full` if the current frame needs more |
| Glyph request | font size ≤ 512 px, bitmap ≤ 640 px per side and 262,144 px | `AtlasStatus.TooLarge` |
| Text length | 1,048,576 UTF-16 units | `TextLimitException` |
| Font file / faces | 64 MiB / 256 | `TextLimitException` |

A font that is not a usable font throws OneFont's `FontFormatException` from `FontLibrary.Add`. An asset that decodes badly later is
remembered as `AtlasStatus.Invalid` and not retried. All of these classes are for the UI thread only.

## Fonts

`FontLibrary.Add(bytes, family?)` loads a face and returns its id; `AddFallback(id)` appends it to every stack after the families a style
names; `SetAlias("sans-serif", "Family")` maps generic names. A `font-family` list is resolved left to right, then the fallbacks. Per
family the face is chosen by slant, then nearest weight. Line height defaults to the first face's ascent + descent + line gap.
