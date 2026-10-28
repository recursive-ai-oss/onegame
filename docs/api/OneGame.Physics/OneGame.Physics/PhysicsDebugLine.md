[API reference](../../index.md) › [OneGame.Physics](../index.md) › OneGame.Physics

# PhysicsDebugLine struct

**Namespace:** `OneGame.Physics`  
**Package:** [`RecursiveAI.OneGame.Physics`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`PhysicsDebugLine`](PhysicsDebugLine.md)&gt;

```csharp
public readonly struct PhysicsDebugLine : IEquatable<PhysicsDebugLine>
```

A line of debug geometry in world units.

## Constructors

| Member | Description |
| --- | --- |
| [`PhysicsDebugLine(Vector2, Vector2, uint, float)`](#m-ctor-vector2-vector2-uint-float) | A line of debug geometry in world units. |

### <a id="m-ctor-vector2-vector2-uint-float"></a>PhysicsDebugLine(Vector2, Vector2, uint, float)

```csharp
public PhysicsDebugLine(Vector2 From, Vector2 To, uint Rgba, float Width);
```

A line of debug geometry in world units.

**Parameters**

- `From` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)): First end.
- `To` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)): Second end.
- `Rgba` (`uint`): Colour as `0xRRGGBBAA`.
- `Width` (`float`): Thickness in world units.

## Properties

| Member | Description |
| --- | --- |
| [`From`](#m-from) | First end. |
| [`Rgba`](#m-rgba) | Colour as `0xRRGGBBAA`. |
| [`To`](#m-to) | Second end. |
| [`Width`](#m-width) | Thickness in world units. |

### <a id="m-from"></a>From

```csharp
public Vector2 From { get; init; }
```

First end.

**Type:** [`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="m-rgba"></a>Rgba

```csharp
public uint Rgba { get; init; }
```

Colour as `0xRRGGBBAA`.

**Type:** `uint`

### <a id="m-to"></a>To

```csharp
public Vector2 To { get; init; }
```

Second end.

**Type:** [`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="m-width"></a>Width

```csharp
public float Width { get; init; }
```

Thickness in world units.

**Type:** `float`
