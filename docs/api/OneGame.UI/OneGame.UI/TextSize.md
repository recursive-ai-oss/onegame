[API reference](../../index.md) › [OneGame.UI](../index.md) › OneGame.UI

# TextSize struct

**Namespace:** `OneGame.UI`  
**Package:** [`RecursiveAI.OneGame.UI`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`TextSize`](TextSize.md)&gt;

```csharp
public readonly struct TextSize : IEquatable<TextSize>
```

The size of laid-out text.

## Constructors

| Member | Description |
| --- | --- |
| [`TextSize(float, float, int)`](#m-ctor-float-float-int) | The size of laid-out text. |

### <a id="m-ctor-float-float-int"></a>TextSize(float, float, int)

```csharp
public TextSize(float Width, float Height, int LineCount);
```

The size of laid-out text.

**Parameters**

- `Width` (`float`): Width of the widest line in pixels.
- `Height` (`float`): Total height in pixels.
- `LineCount` (`int`): Number of lines.

## Properties

| Member | Description |
| --- | --- |
| [`Height`](#m-height) | Total height in pixels. |
| [`LineCount`](#m-linecount) | Number of lines. |
| [`Width`](#m-width) | Width of the widest line in pixels. |

### <a id="m-height"></a>Height

```csharp
public float Height { get; init; }
```

Total height in pixels.

**Type:** `float`

### <a id="m-linecount"></a>LineCount

```csharp
public int LineCount { get; init; }
```

Number of lines.

**Type:** `int`

### <a id="m-width"></a>Width

```csharp
public float Width { get; init; }
```

Width of the widest line in pixels.

**Type:** `float`
