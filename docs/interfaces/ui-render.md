# UI to renderer: the display list

`OneGame.UI` never draws. Each frame `UiTree.Frame()` returns a `DisplayList` (the same instance every time) that a renderer replays. It needs no GPU to produce, so it can be inspected, compared and captured in tests.

```csharp
var ui = new UiTree(textMeasurer);            // ITextMeasurer: see "Text" below
ui.SetViewport(widthPx, heightPx, uiScale);   // pixels, and pixels per UI unit
DisplayList list = ui.Frame();                // cheap and allocation-free when nothing changed
if (list.Version != lastVersion) Record(list);
```

## Contract

- **Coordinates** are screen pixels, origin top-left, y down. Layout resolves the `ui` unit (1 unit = `Scale` pixels) before commands are produced, so commands are always in pixels. `list.Scale` is given so SVG art can be rasterized at the right size.
- **Order** is paint order: replay front to back in list order, with blending. Per element: shadow, background color, background image, border, text, then the children (ordered by `z-index`, ties by document order), then the outline.
- **`Version`** increases whenever the commands change. `Width`/`Height` are the screen size.
- **Stack commands** nest and always balance: `PushClip`/`PopClip`, `PushTransform`/`PopTransform`, `PushOpacity`/`PopOpacity`.

## Commands (`DrawCommand`, field use by `Kind`)

| Kind | Fields | Meaning |
|---|---|---|
| `Rect` | `Bounds`, `Color`, `Radii` | Filled rounded rectangle (the border box). |
| `Border` | `Bounds`, `Color`, `Widths` (left, top, right, bottom), `Radii` | Stroke along the inside of `Bounds`. Also used for outlines (then `Bounds` is already grown). |
| `Shadow` | `Bounds`, `Color`, `Radii`, `Offset`, `Value` (blur radius), `Spread` | Blurred shadow of the rounded box; not drawn under the box itself. |
| `Image` | `Bounds`, `Resource`, `Color` (tint, white = none), `Radii`, `Slice` | `Resource` is an asset id for a raster image or an SVG; the renderer resolves it. A non-zero `Slice` gives nine-slice insets in pixels. |
| `Text` | `TextRun` (index into `list.TextRuns`), `Bounds` (content box), `Color` | See below. |
| `PushClip` | `Bounds`, `Radii` | Intersect the clip with a rounded rectangle (the padding box of a scroll or `overflow: hidden` element). |
| `PushTransform` | `Transform` (`Matrix3x2`, row vectors) | Applies to everything until the pop. The matrix already includes the pivot, so it maps absolute screen coordinates to absolute screen coordinates. Translate, scale and rotate compose about the box center. |
| `PushOpacity` | `Value` (0..1) | Everything until the pop is drawn as a group, then blended with this opacity. |

`Source` names the element that produced a command; renderers ignore it, capture tools use it to annotate frames.

`DisplayList.ToText()` prints the list one command per line (nested by indent). Tests compare against it.

## Text

The UI breaks lines (through `ITextMeasurer`) and positions them; the renderer shapes each line segment at draw time.

- `TextRun`: `Text` (the whole string), `Style` (`TextStyle`: family, size in pixels, weight, italic, line height in pixels or 0 for the font's own, letter spacing, white-space), `Color`, `FirstLine`, `LineCount`.
- `list.Lines[FirstLine .. FirstLine + LineCount)`: `PlacedLine(Start, Length, X, Y, Width, Height)`. `Start`/`Length` index UTF-16 code units of `Text`; `X`/`Y` are the line's top-left in screen pixels with `text-align` applied; `Width` is the measurer's advance for the line (the renderer may use it for ellipsis or justification, but must not re-wrap).
- Clip, transform and opacity apply to text like any other command.

### `ITextMeasurer` (what the real text system implements)

```csharp
TextSize Measure(string text, in TextStyle style, float maxWidth);
void Layout(string text, in TextStyle style, float maxWidth, List<TextLine> lines);
```

`maxWidth` is `+Infinity` for no wrapping (max-content) and `0` for the min-content width (break at every opportunity; an unbreakable run overflows). `Layout` and `Measure` must agree, and the result must be a pure function of the arguments: layout caches on it. `TextLine.Y` is relative to the first line. The fit with the font library: break opportunities from `TextSegmentation.GetLineBreakOpportunities`, advances from `TextShaping.ShapeRun` (per bidi run, in visual order), line height from face metrics when `LineHeight` is 0. `MonospaceTextMeasurer` (6 px per character at 10 px font size, 12 px lines) is the deterministic stand-in for headless runs and tests. Set `ui.Measurer` when the real one is ready; all layout is redone.

## Not done by the UI

Scroll bars, text selection and carets, focus rings (a style rule gives an `outline`), and animation. `transition` is stored as data only.
