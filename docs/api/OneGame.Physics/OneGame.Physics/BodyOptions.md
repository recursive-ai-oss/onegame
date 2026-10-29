[API reference](../../index.md) › [OneGame.Physics](../index.md) › OneGame.Physics

# BodyOptions class

**Namespace:** `OneGame.Physics`  
**Package:** [`RecursiveAI.OneGame.Physics`](../index.md)

```csharp
public sealed class BodyOptions
```

What a new body is made of and how it behaves. Everything has a usable default: a dynamic body of density 1, friction 0.6, no bounce.

## Constructors

| Member | Description |
| --- | --- |
| [`BodyOptions()`](#m-ctor) |  |

### <a id="m-ctor"></a>BodyOptions()

```csharp
public BodyOptions();
```

## Properties

| Member | Description |
| --- | --- |
| [`Angle`](#m-angle) | Starting rotation, in radians, counter-clockwise. |
| [`AngularDamping`](#m-angulardamping) | Slows the body's rotation by this fraction per second. |
| [`Density`](#m-density) | Mass per area. |
| [`FixedRotation`](#m-fixedrotation) | Whether the body never rotates (upright things). |
| [`Friction`](#m-friction) | Sliding grip, 0 (ice) and up. |
| [`GravityScale`](#m-gravityscale) | Multiplies gravity for this body. |
| [`IsBullet`](#m-isbullet) | Whether fast small bodies are swept against other moving bodies too, so they cannot tunnel through them. |
| [`IsTrigger`](#m-istrigger) | A trigger detects overlaps ([`PhysicsEventKind.TriggerEnter`](PhysicsEventKind.md#m-triggerenter)/[`PhysicsEventKind.TriggerExit`](PhysicsEventKind.md#m-triggerexit)) and has no contact response: things pass through it. |
| [`Kind`](#m-kind) | Static, kinematic or dynamic. |
| [`Layer`](#m-layer) | Which of the 32 layers the body is on; see [`PhysicsSpace2D.SetLayerCollision()`](PhysicsSpace2D.md#m-setlayercollision-int-int-bool). |
| [`LinearDamping`](#m-lineardamping) | Slows the body's linear motion by this fraction per second. |
| [`Restitution`](#m-restitution) | Bounciness, 0 (none) to 1 (perfect). |
| [`Velocity`](#m-velocity) | Starting velocity. |

### <a id="m-angle"></a>Angle

```csharp
public Fixed Angle { get; set; }
```

Starting rotation, in radians, counter-clockwise.

**Type:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)

### <a id="m-angulardamping"></a>AngularDamping

```csharp
public Fixed AngularDamping { get; set; }
```

Slows the body's rotation by this fraction per second. Default 0.

**Type:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)

### <a id="m-density"></a>Density

```csharp
public Fixed Density { get; set; }
```

Mass per area. Zero gives a body that has unit mass whatever its size. Default 1.

**Type:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)

### <a id="m-fixedrotation"></a>FixedRotation

```csharp
public bool FixedRotation { get; set; }
```

Whether the body never rotates (upright things).

**Type:** `bool`

### <a id="m-friction"></a>Friction

```csharp
public Fixed Friction { get; set; }
```

Sliding grip, 0 (ice) and up. Contacts use the geometric mean of the two bodies. Default 0.6.

**Type:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)

### <a id="m-gravityscale"></a>GravityScale

```csharp
public Fixed GravityScale { get; set; }
```

Multiplies gravity for this body. Default 1.

**Type:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)

### <a id="m-isbullet"></a>IsBullet

```csharp
public bool IsBullet { get; set; }
```

Whether fast small bodies are swept against other moving bodies too, so they cannot tunnel through them.

**Type:** `bool`

### <a id="m-istrigger"></a>IsTrigger

```csharp
public bool IsTrigger { get; set; }
```

A trigger detects overlaps ([`PhysicsEventKind.TriggerEnter`](PhysicsEventKind.md#m-triggerenter)/[`PhysicsEventKind.TriggerExit`](PhysicsEventKind.md#m-triggerexit)) and has no contact response: things pass through it.

**Type:** `bool`

### <a id="m-kind"></a>Kind

```csharp
public BodyKind Kind { get; set; }
```

Static, kinematic or dynamic. Default dynamic.

**Type:** [`BodyKind`](BodyKind.md)

### <a id="m-layer"></a>Layer

```csharp
public int Layer { get; set; }
```

Which of the 32 layers the body is on; see [`PhysicsSpace2D.SetLayerCollision()`](PhysicsSpace2D.md#m-setlayercollision-int-int-bool). Default 0.

**Type:** `int`

### <a id="m-lineardamping"></a>LinearDamping

```csharp
public Fixed LinearDamping { get; set; }
```

Slows the body's linear motion by this fraction per second. Default 0.

**Type:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)

### <a id="m-restitution"></a>Restitution

```csharp
public Fixed Restitution { get; set; }
```

Bounciness, 0 (none) to 1 (perfect). Default 0.

**Type:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)

### <a id="m-velocity"></a>Velocity

```csharp
public FVec2 Velocity { get; set; }
```

Starting velocity.

**Type:** [`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md)
