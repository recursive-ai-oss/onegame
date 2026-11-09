[API reference](../../index.md) › [OneGame.Physics](../index.md) › OneGame.Physics

# PhysicsWheel3D struct

**Namespace:** `OneGame.Physics`  
**Package:** [`RecursiveAI.OneGame.Physics`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`PhysicsWheel3D`](PhysicsWheel3D.md)&gt;

```csharp
public readonly struct PhysicsWheel3D : IEquatable<PhysicsWheel3D>
```

One wheel's view and traction state, in local coordinates.

## Constructors

| Member | Description |
| --- | --- |
| [`PhysicsWheel3D(FVec3, Fixed, bool, FVec3, Fixed, Fixed, Fixed)`](#m-ctor-fvec3-fixed-bool-fvec3-fixed-fixed-fixed) | One wheel's view and traction state, in local coordinates. |

### <a id="m-ctor-fvec3-fixed-bool-fvec3-fixed-fixed-fixed"></a>PhysicsWheel3D(FVec3, Fixed, bool, FVec3, Fixed, Fixed, Fixed)

```csharp
public PhysicsWheel3D(FVec3 Center, Fixed Radius, bool Grounded, FVec3 ContactPoint, Fixed SuspensionLength, Fixed SpinAngle, Fixed SteerAngle);
```

One wheel's view and traction state, in local coordinates.

**Parameters**

- `Center` ([`FVec3`](../../OneGame.Core/OneGame.Core/FVec3.md)): Wheel centre.
- `Radius` ([`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)): Wheel radius.
- `Grounded` (`bool`): Whether the suspension cast touches ground.
- `ContactPoint` ([`FVec3`](../../OneGame.Core/OneGame.Core/FVec3.md)): Ground contact point.
- `SuspensionLength` ([`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)): Current suspension length.
- `SpinAngle` ([`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)): Wheel spin in radians.
- `SteerAngle` ([`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)): Wheel steering in radians.

## Properties

| Member | Description |
| --- | --- |
| [`Center`](#m-center) | Wheel centre. |
| [`ContactPoint`](#m-contactpoint) | Ground contact point. |
| [`Grounded`](#m-grounded) | Whether the suspension cast touches ground. |
| [`Radius`](#m-radius) | Wheel radius. |
| [`SpinAngle`](#m-spinangle) | Wheel spin in radians. |
| [`SteerAngle`](#m-steerangle) | Wheel steering in radians. |
| [`SuspensionLength`](#m-suspensionlength) | Current suspension length. |

### <a id="m-center"></a>Center

```csharp
public FVec3 Center { get; init; }
```

Wheel centre.

**Type:** [`FVec3`](../../OneGame.Core/OneGame.Core/FVec3.md)

### <a id="m-contactpoint"></a>ContactPoint

```csharp
public FVec3 ContactPoint { get; init; }
```

Ground contact point.

**Type:** [`FVec3`](../../OneGame.Core/OneGame.Core/FVec3.md)

### <a id="m-grounded"></a>Grounded

```csharp
public bool Grounded { get; init; }
```

Whether the suspension cast touches ground.

**Type:** `bool`

### <a id="m-radius"></a>Radius

```csharp
public Fixed Radius { get; init; }
```

Wheel radius.

**Type:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)

### <a id="m-spinangle"></a>SpinAngle

```csharp
public Fixed SpinAngle { get; init; }
```

Wheel spin in radians.

**Type:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)

### <a id="m-steerangle"></a>SteerAngle

```csharp
public Fixed SteerAngle { get; init; }
```

Wheel steering in radians.

**Type:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)

### <a id="m-suspensionlength"></a>SuspensionLength

```csharp
public Fixed SuspensionLength { get; init; }
```

Current suspension length.

**Type:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)
