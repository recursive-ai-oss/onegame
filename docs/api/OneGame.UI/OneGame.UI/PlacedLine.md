[API reference](../../index.md) › [OneGame.UI](../index.md) › OneGame.UI

# PlacedLine struct

**Namespace:** `OneGame.UI`  
**Package:** [`RecursiveAI.OneGame.UI`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`PlacedLine`](PlacedLine.md)&gt;

```csharp
public readonly struct PlacedLine : IEquatable<PlacedLine>
```

A line of text with its final position.

## Constructors

| Member | Description |
| --- | --- |
| [`PlacedLine(int, int, float, float, float, float)`](#m-ctor-int-int-float-float-float-float) | A line of text with its final position. |

### <a id="m-ctor-int-int-float-float-float-float"></a>PlacedLine(int, int, float, float, float, float)

```csharp
public PlacedLine(int Start, int Length, float X, float Y, float Width, float Height);
```

A line of text with its final position.

**Parameters**

- `Start` (`int`): Index of the first UTF-16 code unit in the run's text.
- `Length` (`int`): Number of code units.
- `X` (`float`): Left edge in screen pixels (text alignment already applied).
- `Y` (`float`): Top edge in screen pixels.
- `Width` (`float`): Advance width in pixels.
- `Height` (`float`): Line height in pixels.

## Properties

| Member | Description |
| --- | --- |
| [`Height`](#m-height) | Line height in pixels. |
| [`Length`](#m-length) | Number of code units. |
| [`Start`](#m-start) | Index of the first UTF-16 code unit in the run's text. |
| [`Width`](#m-width) | Advance width in pixels. |
| [`X`](#m-x) | Left edge in screen pixels (text alignment already applied). |
| [`Y`](#m-y) | Top edge in screen pixels. |

### <a id="m-height"></a>Height

```csharp
public float Height { get; init; }
```

Line height in pixels.

**Type:** `float`

### <a id="m-length"></a>Length

```csharp
public int Length { get; init; }
```

Number of code units.

**Type:** `int`

### <a id="m-start"></a>Start

```csharp
public int Start { get; init; }
```

Index of the first UTF-16 code unit in the run's text.

**Type:** `int`

### <a id="m-width"></a>Width

```csharp
public float Width { get; init; }
```

Advance width in pixels.

**Type:** `float`

### <a id="m-x"></a>X

```csharp
public float X { get; init; }
```

Left edge in screen pixels (text alignment already applied).

**Type:** `float`

### <a id="m-y"></a>Y

```csharp
public float Y { get; init; }
```

Top edge in screen pixels.

**Type:** `float`
