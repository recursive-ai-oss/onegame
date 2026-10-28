[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# GltfLight class

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)

```csharp
public sealed class GltfLight
```

A KHR\_lights\_punctual light.

## Constructors

| Member | Description |
| --- | --- |
| [`GltfLight()`](#m-ctor) |  |

### <a id="m-ctor"></a>GltfLight()

```csharp
public GltfLight();
```

## Properties

| Member | Description |
| --- | --- |
| [`Color`](#m-color) | Linear RGB colour. |
| [`InnerConeAngle`](#m-innerconeangle) | Spot inner cone angle in radians. |
| [`Intensity`](#m-intensity) | Lux for directional lights, candela for point and spot lights. |
| [`Name`](#m-name) | The light's name, or empty. |
| [`OuterConeAngle`](#m-outerconeangle) | Spot outer cone angle in radians. |
| [`Range`](#m-range) | Distance beyond which the light has no effect, or null for unlimited. |
| [`Type`](#m-type) | The kind of light. |

### <a id="m-color"></a>Color

```csharp
public Vector3 Color { get; }
```

Linear RGB colour.

**Type:** [`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)

### <a id="m-innerconeangle"></a>InnerConeAngle

```csharp
public float InnerConeAngle { get; }
```

Spot inner cone angle in radians.

**Type:** `float`

### <a id="m-intensity"></a>Intensity

```csharp
public float Intensity { get; }
```

Lux for directional lights, candela for point and spot lights.

**Type:** `float`

### <a id="m-name"></a>Name

```csharp
public string Name { get; }
```

The light's name, or empty.

**Type:** `string`

### <a id="m-outerconeangle"></a>OuterConeAngle

```csharp
public float OuterConeAngle { get; }
```

Spot outer cone angle in radians.

**Type:** `float`

### <a id="m-range"></a>Range

```csharp
public float? Range { get; }
```

Distance beyond which the light has no effect, or null for unlimited.

**Type:** `float?`

### <a id="m-type"></a>Type

```csharp
public GltfLightType Type { get; }
```

The kind of light.

**Type:** [`GltfLightType`](GltfLightType.md)
