[API reference](../../index.md) › [OneGame.Physics](../index.md) › OneGame.Physics

# CharacterOptions3D class

**Namespace:** `OneGame.Physics`  
**Package:** [`RecursiveAI.OneGame.Physics`](../index.md)

```csharp
public sealed class CharacterOptions3D
```

The shape and abilities of a character: an upright capsule that is moved by collide-and-slide and not by forces.

## Constructors

| Member | Description |
| --- | --- |
| [`CharacterOptions3D()`](#m-ctor) |  |

### <a id="m-ctor"></a>CharacterOptions3D()

```csharp
public CharacterOptions3D();
```

## Properties

| Member | Description |
| --- | --- |
| [`Height`](#m-height) | Total height of the capsule, rounded ends included. |
| [`Layer`](#m-layer) | The layer of its capsule; see [`PhysicsSpace3D.SetLayerCollision()`](PhysicsSpace3D.md#m-setlayercollision-int-int-bool). |
| [`PushStrength`](#m-pushstrength) | How hard it pushes dynamic bodies it walks into (0 does not push them). |
| [`Radius`](#m-radius) | Capsule radius. |
| [`SlopeLimitRadians`](#m-slopelimitradians) | Steepest slope in radians from flat. |
| [`StepOffset`](#m-stepoffset) | Tallest step it climbs without jumping. |

### <a id="m-height"></a>Height

```csharp
public Fixed Height { get; set; }
```

Total height of the capsule, rounded ends included. Default 1.8.

**Type:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)

### <a id="m-layer"></a>Layer

```csharp
public int Layer { get; set; }
```

The layer of its capsule; see [`PhysicsSpace3D.SetLayerCollision()`](PhysicsSpace3D.md#m-setlayercollision-int-int-bool). Default 0.

**Type:** `int`

### <a id="m-pushstrength"></a>PushStrength

```csharp
public Fixed PushStrength { get; set; }
```

How hard it pushes dynamic bodies it walks into (0 does not push them). Default 1.

**Type:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)

### <a id="m-radius"></a>Radius

```csharp
public Fixed Radius { get; set; }
```

Capsule radius. Default 0.3.

**Type:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)

### <a id="m-slopelimitradians"></a>SlopeLimitRadians

```csharp
public Fixed SlopeLimitRadians { get; set; }
```

Steepest slope in radians from flat. Default pi/4; passed to physics without a degree round trip.

**Type:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)

### <a id="m-stepoffset"></a>StepOffset

```csharp
public Fixed StepOffset { get; set; }
```

Tallest step it climbs without jumping. Default 0.3.

**Type:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)
