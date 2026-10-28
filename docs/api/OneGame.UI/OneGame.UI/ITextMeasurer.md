[API reference](../../index.md) › [OneGame.UI](../index.md) › OneGame.UI

# ITextMeasurer interface

**Namespace:** `OneGame.UI`  
**Package:** [`RecursiveAI.OneGame.UI`](../index.md)

```csharp
public interface ITextMeasurer
```

Measures and breaks text for layout. The UI never shapes text itself: the real implementation (shaping, bidi, fallback, line breaking) lives with the text system, and the renderer shapes each returned line again at draw time.

## Methods

| Member | Description |
| --- | --- |
| [`Layout(string, in TextStyle, float, List<TextLine>)`](#m-layout-string-textstyle-float-list-textline) | Breaks `text` into lines at `maxWidth`, appending to `lines`; the lines agree with [`ITextMeasurer.Measure()`](ITextMeasurer.md#m-measure-string-textstyle-float). |
| [`Measure(string, in TextStyle, float)`](#m-measure-string-textstyle-float) | Measures `text` wrapped at `maxWidth`. |

### <a id="m-layout-string-textstyle-float-list-textline"></a>Layout(string, in TextStyle, float, List&lt;TextLine&gt;)

```csharp
void Layout(string text, in TextStyle style, float maxWidth, List<TextLine> lines);
```

Breaks `text` into lines at `maxWidth`, appending to `lines`; the lines agree with [`ITextMeasurer.Measure()`](ITextMeasurer.md#m-measure-string-textstyle-float).

**Parameters**

- `text` (`string`)
- `style` ([`TextStyle`](TextStyle.md))
- `maxWidth` (`float`)
- `lines` ([`List`](https://learn.microsoft.com/dotnet/api/system.collections.generic.list-1)&lt;[`TextLine`](TextLine.md)&gt;)

### <a id="m-measure-string-textstyle-float"></a>Measure(string, in TextStyle, float)

```csharp
TextSize Measure(string text, in TextStyle style, float maxWidth);
```

Measures `text` wrapped at `maxWidth`. A `PositiveInfinity` width means no wrapping (max-content); zero means break at every opportunity (min-content, the widest unbreakable run). A run wider than the limit overflows on its own line.

**Parameters**

- `text` (`string`)
- `style` ([`TextStyle`](TextStyle.md))
- `maxWidth` (`float`)

**Returns:** [`TextSize`](TextSize.md)
