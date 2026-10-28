[API reference](../../index.md) › [OneGame.Render2D](../index.md) › OneGame.Render2D

# PixelRect struct

**Namespace:** `OneGame.Render2D`  
**Package:** [`RecursiveAI.OneGame.Render2D`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`PixelRect`](PixelRect.md)&gt;

```csharp
public readonly struct PixelRect : IEquatable<PixelRect>
```

Whole pixels of a texture or sub-rectangle.

## Constructors

| Member | Description |
| --- | --- |
| [`PixelRect(int, int, int, int)`](#m-ctor-int-int-int-int) | Whole pixels of a texture or sub-rectangle. |

### <a id="m-ctor-int-int-int-int"></a>PixelRect(int, int, int, int)

```csharp
public PixelRect(int X, int Y, int Width, int Height);
```

Whole pixels of a texture or sub-rectangle.

**Parameters**

- `X` (`int`): Left edge in pixels.
- `Y` (`int`): Top edge in pixels.
- `Width` (`int`): Width in pixels.
- `Height` (`int`): Height in pixels.

## Properties

| Member | Description |
| --- | --- |
| [`Height`](#m-height) | Height in pixels. |
| [`Width`](#m-width) | Width in pixels. |
| [`X`](#m-x) | Left edge in pixels. |
| [`Y`](#m-y) | Top edge in pixels. |

### <a id="m-height"></a>Height

```csharp
public int Height { get; init; }
```

Height in pixels.

**Type:** `int`

### <a id="m-width"></a>Width

```csharp
public int Width { get; init; }
```

Width in pixels.

**Type:** `int`

### <a id="m-x"></a>X

```csharp
public int X { get; init; }
```

Left edge in pixels.

**Type:** `int`

### <a id="m-y"></a>Y

```csharp
public int Y { get; init; }
```

Top edge in pixels.

**Type:** `int`
