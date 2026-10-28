[API reference](../../index.md) › [OneGame.UI](../index.md) › OneGame.UI

# TextLine struct

**Namespace:** `OneGame.UI`  
**Package:** [`RecursiveAI.OneGame.UI`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`TextLine`](TextLine.md)&gt;

```csharp
public readonly struct TextLine : IEquatable<TextLine>
```

One line of laid-out text.

## Constructors

| Member | Description |
| --- | --- |
| [`TextLine(int, int, float, float, float)`](#m-ctor-int-int-float-float-float) | One line of laid-out text. |

### <a id="m-ctor-int-int-float-float-float"></a>TextLine(int, int, float, float, float)

```csharp
public TextLine(int Start, int Length, float Width, float Y, float Height);
```

One line of laid-out text.

**Parameters**

- `Start` (`int`): Index of the first UTF-16 code unit of the line in the source string.
- `Length` (`int`): Number of code units in the line, without trailing spaces or the line break.
- `Width` (`float`): Advance width of the line in pixels.
- `Y` (`float`): Top of the line relative to the first line, in pixels.
- `Height` (`float`): Height of the line in pixels.

## Properties

| Member | Description |
| --- | --- |
| [`Height`](#m-height) | Height of the line in pixels. |
| [`Length`](#m-length) | Number of code units in the line, without trailing spaces or the line break. |
| [`Start`](#m-start) | Index of the first UTF-16 code unit of the line in the source string. |
| [`Width`](#m-width) | Advance width of the line in pixels. |
| [`Y`](#m-y) | Top of the line relative to the first line, in pixels. |

### <a id="m-height"></a>Height

```csharp
public float Height { get; init; }
```

Height of the line in pixels.

**Type:** `float`

### <a id="m-length"></a>Length

```csharp
public int Length { get; init; }
```

Number of code units in the line, without trailing spaces or the line break.

**Type:** `int`

### <a id="m-start"></a>Start

```csharp
public int Start { get; init; }
```

Index of the first UTF-16 code unit of the line in the source string.

**Type:** `int`

### <a id="m-width"></a>Width

```csharp
public float Width { get; init; }
```

Advance width of the line in pixels.

**Type:** `float`

### <a id="m-y"></a>Y

```csharp
public float Y { get; init; }
```

Top of the line relative to the first line, in pixels.

**Type:** `float`
