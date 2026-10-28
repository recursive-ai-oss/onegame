[API reference](../../index.md) › [OneGame.Text](../index.md) › OneGame.Text

# FontTextMeasurer class

**Namespace:** `OneGame.Text`  
**Package:** [`RecursiveAI.OneGame.Text`](../index.md)  
**Implements:** [`ITextMeasurer`](../../OneGame.UI/OneGame.UI/ITextMeasurer.md)

```csharp
public sealed class FontTextMeasurer : ITextMeasurer
```

The UI's text measurer, backed by the font library: shapes with OneFont (bidi, fallback, ligatures, kerning), breaks lines at the opportunities of UAX \#14 and measures with the same advances the glyphs are drawn with. Results are cached per string and style, so repeated layout of unchanged text shapes nothing and allocates nothing. Not thread safe.

## Constructors

| Member | Description |
| --- | --- |
| [`FontTextMeasurer(FontLibrary)`](#m-ctor-fontlibrary) | Creates a measurer over a library. |

### <a id="m-ctor-fontlibrary"></a>FontTextMeasurer(FontLibrary)

```csharp
public FontTextMeasurer(FontLibrary library);
```

Creates a measurer over a library.

**Parameters**

- `library` ([`FontLibrary`](FontLibrary.md)): The fonts to use.

## Fields

| Member | Description |
| --- | --- |
| [`MaxFontSize`](#m-maxfontsize) | The largest font size accepted, in pixels. |
| [`MaxTextLength`](#m-maxtextlength) | The longest string accepted, in UTF-16 code units. |

### <a id="m-maxfontsize"></a>MaxFontSize

```csharp
public const float MaxFontSize = 4096f;
```

The largest font size accepted, in pixels.

**Type:** `float`

### <a id="m-maxtextlength"></a>MaxTextLength

```csharp
public const int MaxTextLength = 1048576;
```

The longest string accepted, in UTF-16 code units.

**Type:** `int`

## Properties

| Member | Description |
| --- | --- |
| [`Library`](#m-library) | Gets the font library this measurer shapes with. |

### <a id="m-library"></a>Library

```csharp
public FontLibrary Library { get; }
```

Gets the font library this measurer shapes with.

**Type:** [`FontLibrary`](FontLibrary.md)

## Methods

| Member | Description |
| --- | --- |
| [`Layout(string, in TextStyle, float, List<TextLine>)`](#m-layout-string-textstyle-float-list-textline) | Breaks `text` into lines at `maxWidth`, appending to `lines`; the lines agree with [`ITextMeasurer.Measure()`](../../OneGame.UI/OneGame.UI/ITextMeasurer.md#m-measure-string-textstyle-float). |
| [`Measure(string, in TextStyle, float)`](#m-measure-string-textstyle-float) | Measures `text` wrapped at `maxWidth`. |
| [`Place(string, in TextStyle, int, int, List<PositionedGlyph>)`](#m-place-string-textstyle-int-int-list-positionedglyph) | Appends the glyphs of one laid-out line, in display order, with x measured from the line's left end and y from its baseline. |
| [`Place(string, in TextStyle, int, int, float, List<PositionedGlyph>)`](#m-place-string-textstyle-int-int-float-list-positionedglyph) | Like [`FontTextMeasurer.Place()`](FontTextMeasurer.md#m-place-string-textstyle-int-int-list-positionedglyph), for a line box of a given height. |

### <a id="m-layout-string-textstyle-float-list-textline"></a>Layout(string, in TextStyle, float, List&lt;TextLine&gt;)

```csharp
public void Layout(string text, in TextStyle style, float maxWidth, List<TextLine> lines);
```

Breaks `text` into lines at `maxWidth`, appending to `lines`; the lines agree with [`ITextMeasurer.Measure()`](../../OneGame.UI/OneGame.UI/ITextMeasurer.md#m-measure-string-textstyle-float).

**Parameters**

- `text` (`string`)
- `style` ([`TextStyle`](../../OneGame.UI/OneGame.UI/TextStyle.md))
- `maxWidth` (`float`)
- `lines` ([`List`](https://learn.microsoft.com/dotnet/api/system.collections.generic.list-1)&lt;[`TextLine`](../../OneGame.UI/OneGame.UI/TextLine.md)&gt;)

**Exceptions**

- [`TextLimitException`](TextLimitException.md): The text is longer than [`FontTextMeasurer.MaxTextLength`](FontTextMeasurer.md#m-maxtextlength).
- [`FontNotFoundException`](FontNotFoundException.md): The library holds no faces.

### <a id="m-measure-string-textstyle-float"></a>Measure(string, in TextStyle, float)

```csharp
public TextSize Measure(string text, in TextStyle style, float maxWidth);
```

Measures `text` wrapped at `maxWidth`. A `PositiveInfinity` width means no wrapping (max-content); zero means break at every opportunity (min-content, the widest unbreakable run). A run wider than the limit overflows on its own line.

**Parameters**

- `text` (`string`)
- `style` ([`TextStyle`](../../OneGame.UI/OneGame.UI/TextStyle.md))
- `maxWidth` (`float`)

**Returns:** [`TextSize`](../../OneGame.UI/OneGame.UI/TextSize.md)

**Exceptions**

- [`TextLimitException`](TextLimitException.md): The text is longer than [`FontTextMeasurer.MaxTextLength`](FontTextMeasurer.md#m-maxtextlength).
- [`FontNotFoundException`](FontNotFoundException.md): The library holds no faces.

### <a id="m-place-string-textstyle-int-int-list-positionedglyph"></a>Place(string, in TextStyle, int, int, List&lt;PositionedGlyph&gt;)

```csharp
public float Place(string text, in TextStyle style, int start, int length, List<PositionedGlyph> into);
```

Appends the glyphs of one laid-out line, in display order, with x measured from the line's left end and y from its baseline.

**Parameters**

- `text` (`string`): The text the line came from.
- `style` ([`TextStyle`](../../OneGame.UI/OneGame.UI/TextStyle.md)): The style it was laid out with.
- `start` (`int`): The line's `Start`.
- `length` (`int`): The line's `Length`.
- `into` ([`List`](https://learn.microsoft.com/dotnet/api/system.collections.generic.list-1)&lt;[`PositionedGlyph`](PositionedGlyph.md)&gt;): Receives the glyphs.

**Returns:** `float` — The baseline's distance from the top of a line box of the style's line height.

### <a id="m-place-string-textstyle-int-int-float-list-positionedglyph"></a>Place(string, in TextStyle, int, int, float, List&lt;PositionedGlyph&gt;)

```csharp
public float Place(string text, in TextStyle style, int start, int length, float lineHeight, List<PositionedGlyph> into);
```

Like [`FontTextMeasurer.Place()`](FontTextMeasurer.md#m-place-string-textstyle-int-int-list-positionedglyph), for a line box of a given height.

**Parameters**

- `text` (`string`): The text the line came from.
- `style` ([`TextStyle`](../../OneGame.UI/OneGame.UI/TextStyle.md)): The style it was laid out with.
- `start` (`int`): The line's `Start`.
- `length` (`int`): The line's `Length`.
- `lineHeight` (`float`): The line's `Height`; 0 for the style's own.
- `into` ([`List`](https://learn.microsoft.com/dotnet/api/system.collections.generic.list-1)&lt;[`PositionedGlyph`](PositionedGlyph.md)&gt;): Receives the glyphs.

**Returns:** `float` — The baseline's distance from the top of the line box.
