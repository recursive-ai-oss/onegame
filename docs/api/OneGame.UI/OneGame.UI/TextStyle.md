[API reference](../../index.md) › [OneGame.UI](../index.md) › OneGame.UI

# TextStyle struct

**Namespace:** `OneGame.UI`  
**Package:** [`RecursiveAI.OneGame.UI`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`TextStyle`](TextStyle.md)&gt;

```csharp
public readonly struct TextStyle : IEquatable<TextStyle>
```

The resolved font properties that decide how a piece of text is measured and wrapped.

## Constructors

| Member | Description |
| --- | --- |
| [`TextStyle(string, float, int, bool, float, float, WhiteSpace)`](#m-ctor-string-float-int-bool-float-float-whitespace) | The resolved font properties that decide how a piece of text is measured and wrapped. |

### <a id="m-ctor-string-float-int-bool-float-float-whitespace"></a>TextStyle(string, float, int, bool, float, float, WhiteSpace)

```csharp
public TextStyle(string Family, float Size, int Weight, bool Italic, float LineHeight, float LetterSpacing, WhiteSpace WhiteSpace);
```

The resolved font properties that decide how a piece of text is measured and wrapped.

**Parameters**

- `Family` (`string`): The `font-family` list.
- `Size` (`float`): Font size in pixels (after UI scale).
- `Weight` (`int`): Weight, 100..900.
- `Italic` (`bool`): True for italic.
- `LineHeight` (`float`): Line pitch in pixels, or 0 for the font's own line height.
- `LetterSpacing` (`float`): Extra advance per character in pixels.
- `WhiteSpace` ([`WhiteSpace`](WhiteSpace.md)): Wrapping mode.

## Properties

| Member | Description |
| --- | --- |
| [`Family`](#m-family) | The `font-family` list. |
| [`Italic`](#m-italic) | True for italic. |
| [`LetterSpacing`](#m-letterspacing) | Extra advance per character in pixels. |
| [`LineHeight`](#m-lineheight) | Line pitch in pixels, or 0 for the font's own line height. |
| [`Size`](#m-size) | Font size in pixels (after UI scale). |
| [`Weight`](#m-weight) | Weight, 100..900. |
| [`WhiteSpace`](#m-whitespace) | Wrapping mode. |

### <a id="m-family"></a>Family

```csharp
public string Family { get; init; }
```

The `font-family` list.

**Type:** `string`

### <a id="m-italic"></a>Italic

```csharp
public bool Italic { get; init; }
```

True for italic.

**Type:** `bool`

### <a id="m-letterspacing"></a>LetterSpacing

```csharp
public float LetterSpacing { get; init; }
```

Extra advance per character in pixels.

**Type:** `float`

### <a id="m-lineheight"></a>LineHeight

```csharp
public float LineHeight { get; init; }
```

Line pitch in pixels, or 0 for the font's own line height.

**Type:** `float`

### <a id="m-size"></a>Size

```csharp
public float Size { get; init; }
```

Font size in pixels (after UI scale).

**Type:** `float`

### <a id="m-weight"></a>Weight

```csharp
public int Weight { get; init; }
```

Weight, 100..900.

**Type:** `int`

### <a id="m-whitespace"></a>WhiteSpace

```csharp
public WhiteSpace WhiteSpace { get; init; }
```

Wrapping mode.

**Type:** [`WhiteSpace`](WhiteSpace.md)
