[API reference](../../index.md) › [OneGame.Platform](../index.md) › OneGame.Platform

# ScreenRect struct

**Namespace:** `OneGame.Platform`  
**Package:** [`RecursiveAI.OneGame.Platform`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ScreenRect`](ScreenRect.md)&gt;

```csharp
public readonly struct ScreenRect : IEquatable<ScreenRect>
```

A rectangle in screen pixels, such as the usable area of a monitor.

## Constructors

| Member | Description |
| --- | --- |
| [`ScreenRect(int, int, int, int)`](#m-ctor-int-int-int-int) | A rectangle in screen pixels, such as the usable area of a monitor. |

### <a id="m-ctor-int-int-int-int"></a>ScreenRect(int, int, int, int)

```csharp
public ScreenRect(int X, int Y, int Width, int Height);
```

A rectangle in screen pixels, such as the usable area of a monitor.

**Parameters**

- `X` (`int`): Left edge.
- `Y` (`int`): Top edge.
- `Width` (`int`): Width in pixels.
- `Height` (`int`): Height in pixels.

## Properties

| Member | Description |
| --- | --- |
| [`Bottom`](#m-bottom) | The bottom edge (exclusive). |
| [`Height`](#m-height) | Height in pixels. |
| [`Right`](#m-right) | The right edge (exclusive). |
| [`Width`](#m-width) | Width in pixels. |
| [`X`](#m-x) | Left edge. |
| [`Y`](#m-y) | Top edge. |

### <a id="m-bottom"></a>Bottom

```csharp
public int Bottom { get; }
```

The bottom edge (exclusive).

**Type:** `int`

### <a id="m-height"></a>Height

```csharp
public int Height { get; init; }
```

Height in pixels.

**Type:** `int`

### <a id="m-right"></a>Right

```csharp
public int Right { get; }
```

The right edge (exclusive).

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

Left edge.

**Type:** `int`

### <a id="m-y"></a>Y

```csharp
public int Y { get; init; }
```

Top edge.

**Type:** `int`
