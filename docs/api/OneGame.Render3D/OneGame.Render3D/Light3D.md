[API reference](../../index.md) › [OneGame.Render3D](../index.md) › OneGame.Render3D

# Light3D class

**Namespace:** `OneGame.Render3D`  
**Package:** [`RecursiveAI.OneGame.Render3D`](../index.md)

```csharp
public sealed class Light3D
```

A punctual light. Intensities are scene-referred: a directional light of E lux lights a white diffuse surface facing it to radiance E/pi, a point light of I candela at distance d gives illuminance I/d^2. The camera's exposure maps that to the display.

## Constructors

| Member | Description |
| --- | --- |
| [`Light3D()`](#m-ctor) |  |

### <a id="m-ctor"></a>Light3D()

```csharp
public Light3D();
```

## Properties

| Member | Description |
| --- | --- |
| [`CastShadows`](#m-castshadows) | Whether the light casts shadows. |
| [`Color`](#m-color) | Linear RGB colour. |
| [`Direction`](#m-direction) | Direction the light shines in (directional and spot); it need not be normalized. |
| [`InnerConeAngle`](#m-innerconeangle) | Spot: angle in radians from the axis inside which the light is at full strength. |
| [`Intensity`](#m-intensity) | Lux (directional) or candela (point, spot). |
| [`Kind`](#m-kind) | Kind of light. |
| [`OuterConeAngle`](#m-outerconeangle) | Spot: angle in radians from the axis at which the light reaches zero. |
| [`Position`](#m-position) | Position (point and spot). |
| [`Range`](#m-range) | Distance at which a point or spot light is cut off with a smooth window; 0 means no limit. |

### <a id="m-castshadows"></a>CastShadows

```csharp
public bool CastShadows { get; set; }
```

Whether the light casts shadows. A directional light uses cascaded shadow maps, a spot light one map, a point light six. At most [`Renderer3D.MaxShadowLights`](Renderer3D.md#m-maxshadowlights) lights and [`Renderer3D.MaxShadowTiles`](Renderer3D.md#m-maxshadowtiles) maps are used per frame, in light order.

**Type:** `bool`

### <a id="m-color"></a>Color

```csharp
public Vector3 Color { get; set; }
```

Linear RGB colour.

**Type:** [`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)

### <a id="m-direction"></a>Direction

```csharp
public Vector3 Direction { get; set; }
```

Direction the light shines in (directional and spot); it need not be normalized.

**Type:** [`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)

### <a id="m-innerconeangle"></a>InnerConeAngle

```csharp
public float InnerConeAngle { get; set; }
```

Spot: angle in radians from the axis inside which the light is at full strength.

**Type:** `float`

### <a id="m-intensity"></a>Intensity

```csharp
public float Intensity { get; set; }
```

Lux (directional) or candela (point, spot).

**Type:** `float`

### <a id="m-kind"></a>Kind

```csharp
public LightKind Kind { get; set; }
```

Kind of light.

**Type:** [`LightKind`](LightKind.md)

### <a id="m-outerconeangle"></a>OuterConeAngle

```csharp
public float OuterConeAngle { get; set; }
```

Spot: angle in radians from the axis at which the light reaches zero.

**Type:** `float`

### <a id="m-position"></a>Position

```csharp
public Vector3 Position { get; set; }
```

Position (point and spot).

**Type:** [`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)

### <a id="m-range"></a>Range

```csharp
public float Range { get; set; }
```

Distance at which a point or spot light is cut off with a smooth window; 0 means no limit.

**Type:** `float`

## Methods

| Member | Description |
| --- | --- |
| [`Directional(Vector3, Vector3, float)`](#m-directional-vector3-vector3-float) | A directional light: parallel rays, like the sun. |
| [`FromGltf(GltfLight, Matrix4x4)`](#m-fromgltf-gltflight-matrix4x4) | The light a glTF file describes, placed by the world matrix of its node (glTF lights shine down the node's -Z axis). |
| [`Point(Vector3, Vector3, float, float)`](#m-point-vector3-vector3-float-float) | A point light: radiates equally in every direction. |
| [`Spot(Vector3, Vector3, Vector3, float, float, float, float)`](#m-spot-vector3-vector3-vector3-float-float-float-float) | A spot light: a point light limited to a cone. |

### <a id="m-directional-vector3-vector3-float"></a>Directional(Vector3, Vector3, float)

```csharp
public static Light3D Directional(Vector3 direction, Vector3 color, float lux);
```

A directional light: parallel rays, like the sun.

**Parameters**

- `direction` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)): The way the light travels (from the light toward the scene); need not be normalized.
- `color` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)): Linear RGB, each channel 0 to 1.
- `lux` (`float`): Intensity in lux (the `onegame view` default is 3).

**Returns:** [`Light3D`](Light3D.md)

### <a id="m-fromgltf-gltflight-matrix4x4"></a>FromGltf(GltfLight, Matrix4x4)

```csharp
public static Light3D FromGltf(GltfLight light, Matrix4x4 nodeWorld);
```

The light a glTF file describes, placed by the world matrix of its node (glTF lights shine down the node's -Z axis).

**Parameters**

- `light` ([`GltfLight`](../../OneGame.Gltf/OneGame.Gltf/GltfLight.md))
- `nodeWorld` ([`Matrix4x4`](https://learn.microsoft.com/dotnet/api/system.numerics.matrix4x4))

**Returns:** [`Light3D`](Light3D.md)

### <a id="m-point-vector3-vector3-float-float"></a>Point(Vector3, Vector3, float, float)

```csharp
public static Light3D Point(Vector3 position, Vector3 color, float candela, float range = 0f);
```

A point light: radiates equally in every direction.

**Parameters**

- `position` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)): World position.
- `color` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)): Linear RGB.
- `candela` (`float`): Intensity in candela; the lit surface receives candela divided by distance squared.
- `range` (`float`): Distance in world units at which the light fades to zero; 0 for no limit.

**Returns:** [`Light3D`](Light3D.md)

### <a id="m-spot-vector3-vector3-vector3-float-float-float-float"></a>Spot(Vector3, Vector3, Vector3, float, float, float, float)

```csharp
public static Light3D Spot(Vector3 position, Vector3 direction, Vector3 color, float candela, float innerCone, float outerCone, float range = 0f);
```

A spot light: a point light limited to a cone.

**Parameters**

- `position` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)): World position.
- `direction` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)): The cone's axis, from the light toward the scene.
- `color` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)): Linear RGB.
- `candela` (`float`): Intensity in candela.
- `innerCone` (`float`): Angle in radians from the axis inside which the light is at full strength.
- `outerCone` (`float`): Angle in radians from the axis at which it reaches zero; at least `innerCone`.
- `range` (`float`): Distance at which the light fades to zero; 0 for no limit.

**Returns:** [`Light3D`](Light3D.md)
