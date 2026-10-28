[API reference](../../index.md) › [OneGame.UI](../index.md) › OneGame.UI

# Rect struct

**Namespace:** `OneGame.UI`  
**Package:** [`RecursiveAI.OneGame.UI`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`Rect`](Rect.md)&gt;

```csharp
public readonly struct Rect : IEquatable<Rect>
```

An axis-aligned rectangle in pixels.

## Constructors

| Member | Description |
| --- | --- |
| [`Rect(float, float, float, float)`](#m-ctor-float-float-float-float) | An axis-aligned rectangle in pixels. |

### <a id="m-ctor-float-float-float-float"></a>Rect(float, float, float, float)

```csharp
public Rect(float X, float Y, float Width, float Height);
```

An axis-aligned rectangle in pixels.

**Parameters**

- `X` (`float`): Left edge.
- `Y` (`float`): Top edge.
- `Width` (`float`): Width.
- `Height` (`float`): Height.

## Properties

| Member | Description |
| --- | --- |
| [`Bottom`](#m-bottom) | The bottom edge. |
| [`Height`](#m-height) | Height. |
| [`Right`](#m-right) | The right edge. |
| [`Width`](#m-width) | Width. |
| [`X`](#m-x) | Left edge. |
| [`Y`](#m-y) | Top edge. |

### <a id="m-bottom"></a>Bottom

```csharp
public float Bottom { get; }
```

The bottom edge.

**Type:** `float`

### <a id="m-height"></a>Height

```csharp
public float Height { get; init; }
```

Height.

**Type:** `float`

### <a id="m-right"></a>Right

```csharp
public float Right { get; }
```

The right edge.

**Type:** `float`

### <a id="m-width"></a>Width

```csharp
public float Width { get; init; }
```

Width.

**Type:** `float`

### <a id="m-x"></a>X

```csharp
public float X { get; init; }
```

Left edge.

**Type:** `float`

### <a id="m-y"></a>Y

```csharp
public float Y { get; init; }
```

Top edge.

**Type:** `float`

## Methods

| Member | Description |
| --- | --- |
| [`Contains(float, float)`](#m-contains-float-float) | True when the point lies inside (left/top inclusive, right/bottom exclusive). |
| [`Intersect(Rect)`](#m-intersect-rect) | The overlap with `other`, or an empty rectangle. |

### <a id="m-contains-float-float"></a>Contains(float, float)

```csharp
public bool Contains(float x, float y);
```

True when the point lies inside (left/top inclusive, right/bottom exclusive).

**Parameters**

- `x` (`float`)
- `y` (`float`)

**Returns:** `bool`

### <a id="m-intersect-rect"></a>Intersect(Rect)

```csharp
public Rect Intersect(Rect other);
```

The overlap with `other`, or an empty rectangle.

**Parameters**

- `other` ([`Rect`](Rect.md))

**Returns:** [`Rect`](Rect.md)
