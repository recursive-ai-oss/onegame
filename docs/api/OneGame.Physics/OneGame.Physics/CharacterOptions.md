[API reference](../../index.md) › [OneGame.Physics](../index.md) › OneGame.Physics

# CharacterOptions class

**Namespace:** `OneGame.Physics`  
**Package:** [`RecursiveAI.OneGame.Physics`](../index.md)

```csharp
public sealed class CharacterOptions
```

The shape and abilities of a character: an upright capsule that is moved by collide-and-slide and not by forces.

## Constructors

| Member | Description |
| --- | --- |
| [`CharacterOptions()`](#m-ctor) |  |

### <a id="m-ctor"></a>CharacterOptions()

```csharp
public CharacterOptions();
```

## Properties

| Member | Description |
| --- | --- |
| [`Height`](#m-height) | Total height of the capsule, rounded ends included. |
| [`Layer`](#m-layer) | The layer of its capsule; see [`PhysicsSpace2D.SetLayerCollision()`](PhysicsSpace2D.md#m-setlayercollision-int-int-bool). |
| [`PushStrength`](#m-pushstrength) | How hard it pushes dynamic bodies it walks into (0 does not push them). |
| [`Radius`](#m-radius) | Capsule radius. |
| [`SlopeLimit`](#m-slopelimit) | Steepest slope it can stand on, in degrees from flat. |
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

The layer of its capsule; see [`PhysicsSpace2D.SetLayerCollision()`](PhysicsSpace2D.md#m-setlayercollision-int-int-bool). Default 0.

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

### <a id="m-slopelimit"></a>SlopeLimit

```csharp
public Fixed SlopeLimit { get; set; }
```

Steepest slope it can stand on, in degrees from flat. Default 45.

**Type:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)

### <a id="m-stepoffset"></a>StepOffset

```csharp
public Fixed StepOffset { get; set; }
```

Tallest step it climbs without jumping. Default 0.3.

**Type:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)
