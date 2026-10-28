[API reference](../../index.md) › [OneGame.UI](../index.md) › OneGame.UI

# TextRun struct

**Namespace:** `OneGame.UI`  
**Package:** [`RecursiveAI.OneGame.UI`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`TextRun`](TextRun.md)&gt;

```csharp
public readonly struct TextRun : IEquatable<TextRun>
```

A paragraph of text to draw: the string, its font, color and where each line goes. The renderer shapes each line segment.

## Constructors

| Member | Description |
| --- | --- |
| [`TextRun(string, TextStyle, Color, int, int)`](#m-ctor-string-textstyle-color-int-int) | A paragraph of text to draw: the string, its font, color and where each line goes. |

### <a id="m-ctor-string-textstyle-color-int-int"></a>TextRun(string, TextStyle, Color, int, int)

```csharp
public TextRun(string Text, TextStyle Style, Color Color, int FirstLine, int LineCount);
```

A paragraph of text to draw: the string, its font, color and where each line goes. The renderer shapes each line segment.

**Parameters**

- `Text` (`string`): The source string; lines index into it.
- `Style` ([`TextStyle`](TextStyle.md)): The font properties, with sizes in pixels.
- `Color` ([`Color`](Color.md)): The text color.
- `FirstLine` (`int`): Index of the first line in [`DisplayList.Lines`](DisplayList.md#m-lines).
- `LineCount` (`int`): Number of lines.

## Properties

| Member | Description |
| --- | --- |
| [`Color`](#m-color) | The text color. |
| [`FirstLine`](#m-firstline) | Index of the first line in [`DisplayList.Lines`](DisplayList.md#m-lines). |
| [`LineCount`](#m-linecount) | Number of lines. |
| [`Style`](#m-style) | The font properties, with sizes in pixels. |
| [`Text`](#m-text) | The source string; lines index into it. |

### <a id="m-color"></a>Color

```csharp
public Color Color { get; init; }
```

The text color.

**Type:** [`Color`](Color.md)

### <a id="m-firstline"></a>FirstLine

```csharp
public int FirstLine { get; init; }
```

Index of the first line in [`DisplayList.Lines`](DisplayList.md#m-lines).

**Type:** `int`

### <a id="m-linecount"></a>LineCount

```csharp
public int LineCount { get; init; }
```

Number of lines.

**Type:** `int`

### <a id="m-style"></a>Style

```csharp
public TextStyle Style { get; init; }
```

The font properties, with sizes in pixels.

**Type:** [`TextStyle`](TextStyle.md)

### <a id="m-text"></a>Text

```csharp
public string Text { get; init; }
```

The source string; lines index into it.

**Type:** `string`
