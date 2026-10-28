[API reference](../../index.md) › [OneGame.Text](../index.md) › OneGame.Text

# TextQuadBuilder class

**Namespace:** `OneGame.Text`  
**Package:** [`RecursiveAI.OneGame.Text`](../index.md)

```csharp
public sealed class TextQuadBuilder
```

Turns the text runs of a display list into glyph quads. Shaping comes from the measurer's cache (the same shaping that sized the lines), glyphs from the atlas. Quads sit on whole pixels vertically and on quarter pixels horizontally.

## Constructors

| Member | Description |
| --- | --- |
| [`TextQuadBuilder(FontTextMeasurer, GlyphAtlas)`](#m-ctor-fonttextmeasurer-glyphatlas) | Creates a builder. |

### <a id="m-ctor-fonttextmeasurer-glyphatlas"></a>TextQuadBuilder(FontTextMeasurer, GlyphAtlas)

```csharp
public TextQuadBuilder(FontTextMeasurer measurer, GlyphAtlas atlas);
```

Creates a builder.

**Parameters**

- `measurer` ([`FontTextMeasurer`](FontTextMeasurer.md)): The measurer the UI laid out with. Using another measurer gives glyphs that do not match the lines.
- `atlas` ([`GlyphAtlas`](GlyphAtlas.md)): The atlas to rasterize into.

## Methods

| Member | Description |
| --- | --- |
| [`Build(DisplayList, int, List<GlyphQuad>)`](#m-build-displaylist-int-list-glyphquad) | Appends the quads of one text run. |
| [`BuildAll(DisplayList, List<GlyphQuad>)`](#m-buildall-displaylist-list-glyphquad) | Appends the quads of every text run of a list, in run order. |
| [`BuildLine(string, in TextStyle, float, float, Color, List<GlyphQuad>)`](#m-buildline-string-textstyle-float-float-color-list-glyphquad) | Appends the quads of one line of text placed with its top-left corner at a point. |

### <a id="m-build-displaylist-int-list-glyphquad"></a>Build(DisplayList, int, List&lt;GlyphQuad&gt;)

```csharp
public int Build(DisplayList list, int runIndex, List<GlyphQuad> quads);
```

Appends the quads of one text run.

**Parameters**

- `list` ([`DisplayList`](../../OneGame.UI/OneGame.UI/DisplayList.md)): The display list.
- `runIndex` (`int`): The [`DrawCommand.TextRun`](../../OneGame.UI/OneGame.UI/DrawCommand.md#m-textrun) of a `Text` command.
- `quads` ([`List`](https://learn.microsoft.com/dotnet/api/system.collections.generic.list-1)&lt;[`GlyphQuad`](GlyphQuad.md)&gt;): Receives the quads, in draw order.

**Returns:** `int` — How many glyphs were not drawn because the atlas refused them (too large, or every page in use this frame); 0 normally.

### <a id="m-buildall-displaylist-list-glyphquad"></a>BuildAll(DisplayList, List&lt;GlyphQuad&gt;)

```csharp
public int BuildAll(DisplayList list, List<GlyphQuad> quads);
```

Appends the quads of every text run of a list, in run order.

**Parameters**

- `list` ([`DisplayList`](../../OneGame.UI/OneGame.UI/DisplayList.md)): The display list.
- `quads` ([`List`](https://learn.microsoft.com/dotnet/api/system.collections.generic.list-1)&lt;[`GlyphQuad`](GlyphQuad.md)&gt;): Receives the quads.

**Returns:** `int` — How many glyphs were refused.

### <a id="m-buildline-string-textstyle-float-float-color-list-glyphquad"></a>BuildLine(string, in TextStyle, float, float, Color, List&lt;GlyphQuad&gt;)

```csharp
public int BuildLine(string text, in TextStyle style, float x, float y, Color color, List<GlyphQuad> quads);
```

Appends the quads of one line of text placed with its top-left corner at a point. Newlines are not broken; the whole string is one line.

**Parameters**

- `text` (`string`): The text.
- `style` ([`TextStyle`](../../OneGame.UI/OneGame.UI/TextStyle.md)): Family, size and spacing.
- `x` (`float`): Left edge in screen pixels.
- `y` (`float`): Top of the line box in screen pixels.
- `color` ([`Color`](../../OneGame.UI/OneGame.UI/Color.md)): Text color (straight alpha).
- `quads` ([`List`](https://learn.microsoft.com/dotnet/api/system.collections.generic.list-1)&lt;[`GlyphQuad`](GlyphQuad.md)&gt;): Receives the quads, in draw order.

**Returns:** `int` — How many glyphs the atlas refused.
