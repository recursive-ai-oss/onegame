[API reference](../../index.md) › [OneGame.Platform](../index.md) › OneGame.Platform

# GestureRegion struct

**Namespace:** `OneGame.Platform`  
**Package:** [`RecursiveAI.OneGame.Platform`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`GestureRegion`](GestureRegion.md)&gt;

```csharp
public readonly struct GestureRegion : IEquatable<GestureRegion>
```

A rectangle in window pixels, with the origin at the window's top-left corner.

## Constructors

| Member | Description |
| --- | --- |
| [`GestureRegion(int, int, int, int)`](#m-ctor-int-int-int-int) | A rectangle in window pixels, with the origin at the window's top-left corner. |

### <a id="m-ctor-int-int-int-int"></a>GestureRegion(int, int, int, int)

```csharp
public GestureRegion(int X, int Y, int Width, int Height);
```

A rectangle in window pixels, with the origin at the window's top-left corner.

**Parameters**

- `X` (`int`): Left edge.
- `Y` (`int`): Top edge.
- `Width` (`int`): Width.
- `Height` (`int`): Height.

## Properties

| Member | Description |
| --- | --- |
| [`Height`](#m-height) | Height. |
| [`IsEmpty`](#m-isempty) | Whether the rectangle covers anything. |
| [`Width`](#m-width) | Width. |
| [`X`](#m-x) | Left edge. |
| [`Y`](#m-y) | Top edge. |

### <a id="m-height"></a>Height

```csharp
public int Height { get; init; }
```

Height.

**Type:** `int`

### <a id="m-isempty"></a>IsEmpty

```csharp
public bool IsEmpty { get; }
```

Whether the rectangle covers anything.

**Type:** `bool`

### <a id="m-width"></a>Width

```csharp
public int Width { get; init; }
```

Width.

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
