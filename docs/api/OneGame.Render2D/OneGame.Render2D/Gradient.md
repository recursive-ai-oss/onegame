[API reference](../../index.md) › [OneGame.Render2D](../index.md) › OneGame.Render2D

# Gradient struct

**Namespace:** `OneGame.Render2D`  
**Package:** [`RecursiveAI.OneGame.Render2D`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`Gradient`](Gradient.md)&gt;

```csharp
public readonly struct Gradient : IEquatable<Gradient>
```

A colour ramp for a filled shape. Positions are in the same coordinates as the shape's bounds, before any transform.

## Constructors

| Member | Description |
| --- | --- |
| [`Gradient(Color, Color, Vector2, Vector2, float)`](#m-ctor-color-color-vector2-vector2-float) | A colour ramp for a filled shape. |

### <a id="m-ctor-color-color-vector2-vector2-float"></a>Gradient(Color, Color, Vector2, Vector2, float)

```csharp
public Gradient(Color From, Color To, Vector2 Start, Vector2 End, float Radius);
```

A colour ramp for a filled shape. Positions are in the same coordinates as the shape's bounds, before any transform.

**Parameters**

- `From` ([`Color`](../../OneGame.UI/OneGame.UI/Color.md)): The colour at the start (or the centre), sRGB straight alpha.
- `To` ([`Color`](../../OneGame.UI/OneGame.UI/Color.md)): The colour at the end (or the rim).
- `Start` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)): The start point of a linear ramp, or the centre of a radial one.
- `End` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)): The end point of a linear ramp (unused for radial).
- `Radius` (`float`): Greater than zero makes the ramp radial with this radius around `Start`.

## Properties

| Member | Description |
| --- | --- |
| [`End`](#m-end) | The end point of a linear ramp (unused for radial). |
| [`From`](#m-from) | The colour at the start (or the centre), sRGB straight alpha. |
| [`Radius`](#m-radius) | Greater than zero makes the ramp radial with this radius around `Start`. |
| [`Start`](#m-start) | The start point of a linear ramp, or the centre of a radial one. |
| [`To`](#m-to) | The colour at the end (or the rim). |

### <a id="m-end"></a>End

```csharp
public Vector2 End { get; init; }
```

The end point of a linear ramp (unused for radial).

**Type:** [`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="m-from"></a>From

```csharp
public Color From { get; init; }
```

The colour at the start (or the centre), sRGB straight alpha.

**Type:** [`Color`](../../OneGame.UI/OneGame.UI/Color.md)

### <a id="m-radius"></a>Radius

```csharp
public float Radius { get; init; }
```

Greater than zero makes the ramp radial with this radius around `Start`.

**Type:** `float`

### <a id="m-start"></a>Start

```csharp
public Vector2 Start { get; init; }
```

The start point of a linear ramp, or the centre of a radial one.

**Type:** [`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="m-to"></a>To

```csharp
public Color To { get; init; }
```

The colour at the end (or the rim).

**Type:** [`Color`](../../OneGame.UI/OneGame.UI/Color.md)

## Methods

| Member | Description |
| --- | --- |
| [`Linear(Color, Color, Vector2, Vector2)`](#m-linear-color-color-vector2-vector2) | A ramp along the line from `start` to `end`. |
| [`Radial(Color, Color, Vector2, float)`](#m-radial-color-color-vector2-float) | A ramp from the centre outwards. |

### <a id="m-linear-color-color-vector2-vector2"></a>Linear(Color, Color, Vector2, Vector2)

```csharp
public static Gradient Linear(Color from, Color to, Vector2 start, Vector2 end);
```

A ramp along the line from `start` to `end`.

**Parameters**

- `from` ([`Color`](../../OneGame.UI/OneGame.UI/Color.md)): Colour at the start.
- `to` ([`Color`](../../OneGame.UI/OneGame.UI/Color.md)): Colour at the end.
- `start` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)): Start point.
- `end` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)): End point.

**Returns:** [`Gradient`](Gradient.md) — The gradient.

### <a id="m-radial-color-color-vector2-float"></a>Radial(Color, Color, Vector2, float)

```csharp
public static Gradient Radial(Color inner, Color outer, Vector2 center, float radius);
```

A ramp from the centre outwards.

**Parameters**

- `inner` ([`Color`](../../OneGame.UI/OneGame.UI/Color.md)): Colour at the centre.
- `outer` ([`Color`](../../OneGame.UI/OneGame.UI/Color.md)): Colour at `radius` and beyond.
- `center` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)): The centre.
- `radius` (`float`): Distance at which the ramp ends.

**Returns:** [`Gradient`](Gradient.md) — The gradient.
