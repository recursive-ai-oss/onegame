[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# GltfMaterial class

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)

```csharp
public sealed class GltfMaterial
```

A metal/rough material with the supported extensions.

## Constructors

| Member | Description |
| --- | --- |
| [`GltfMaterial()`](#m-ctor) |  |

### <a id="m-ctor"></a>GltfMaterial()

```csharp
public GltfMaterial();
```

## Properties

| Member | Description |
| --- | --- |
| [`AlphaCutoff`](#m-alphacutoff) | Cutoff for [`GltfAlphaMode.Mask`](GltfAlphaMode.md#m-mask). |
| [`AlphaMode`](#m-alphamode) | How alpha is used. |
| [`BaseColorFactor`](#m-basecolorfactor) | Linear RGBA multiplier of the base colour. |
| [`BaseColorTexture`](#m-basecolortexture) | Base colour texture (sRGB), or null. |
| [`Clearcoat`](#m-clearcoat) | KHR\_materials\_clearcoat, or null. |
| [`DoubleSided`](#m-doublesided) | Draw back faces too, lit from the other side. |
| [`EmissiveFactor`](#m-emissivefactor) | Linear RGB emission multiplier (before [`GltfMaterial.EmissiveStrength`](GltfMaterial.md#m-emissivestrength)). |
| [`EmissiveStrength`](#m-emissivestrength) | KHR\_materials\_emissive\_strength: multiplies the emission (1 when absent). |
| [`EmissiveTexture`](#m-emissivetexture) | Emission texture (sRGB), or null. |
| [`Ior`](#m-ior) | KHR\_materials\_ior: index of refraction (1.5 when absent). |
| [`MetallicFactor`](#m-metallicfactor) | Metalness multiplier. |
| [`MetallicRoughnessTexture`](#m-metallicroughnesstexture) | Metal (blue) and roughness (green) texture, or null. |
| [`Name`](#m-name) | The material's name, or empty. |
| [`NormalScale`](#m-normalscale) | Scale of the normal map's xy. |
| [`NormalTexture`](#m-normaltexture) | Tangent-space normal map, or null. |
| [`OcclusionStrength`](#m-occlusionstrength) | How strongly occlusion applies (0 to 1). |
| [`OcclusionTexture`](#m-occlusiontexture) | Ambient occlusion map (red channel), or null. |
| [`RoughnessFactor`](#m-roughnessfactor) | Roughness multiplier. |
| [`Sheen`](#m-sheen) | KHR\_materials\_sheen, or null. |
| [`Specular`](#m-specular) | KHR\_materials\_specular, or null. |
| [`Transmission`](#m-transmission) | KHR\_materials\_transmission, or null. |
| [`Unlit`](#m-unlit) | KHR\_materials\_unlit: draw the base colour without lighting. |
| [`Volume`](#m-volume) | KHR\_materials\_volume, or null. |

### <a id="m-alphacutoff"></a>AlphaCutoff

```csharp
public float AlphaCutoff { get; }
```

Cutoff for [`GltfAlphaMode.Mask`](GltfAlphaMode.md#m-mask).

**Type:** `float`

### <a id="m-alphamode"></a>AlphaMode

```csharp
public GltfAlphaMode AlphaMode { get; }
```

How alpha is used.

**Type:** [`GltfAlphaMode`](GltfAlphaMode.md)

### <a id="m-basecolorfactor"></a>BaseColorFactor

```csharp
public Vector4 BaseColorFactor { get; }
```

Linear RGBA multiplier of the base colour.

**Type:** [`Vector4`](https://learn.microsoft.com/dotnet/api/system.numerics.vector4)

### <a id="m-basecolortexture"></a>BaseColorTexture

```csharp
public TextureRef? BaseColorTexture { get; }
```

Base colour texture (sRGB), or null.

**Type:** [`TextureRef`](TextureRef.md)?

### <a id="m-clearcoat"></a>Clearcoat

```csharp
public ClearcoatInfo? Clearcoat { get; }
```

KHR\_materials\_clearcoat, or null.

**Type:** [`ClearcoatInfo`](ClearcoatInfo.md)?

### <a id="m-doublesided"></a>DoubleSided

```csharp
public bool DoubleSided { get; }
```

Draw back faces too, lit from the other side.

**Type:** `bool`

### <a id="m-emissivefactor"></a>EmissiveFactor

```csharp
public Vector3 EmissiveFactor { get; }
```

Linear RGB emission multiplier (before [`GltfMaterial.EmissiveStrength`](GltfMaterial.md#m-emissivestrength)).

**Type:** [`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)

### <a id="m-emissivestrength"></a>EmissiveStrength

```csharp
public float EmissiveStrength { get; }
```

KHR\_materials\_emissive\_strength: multiplies the emission (1 when absent).

**Type:** `float`

### <a id="m-emissivetexture"></a>EmissiveTexture

```csharp
public TextureRef? EmissiveTexture { get; }
```

Emission texture (sRGB), or null.

**Type:** [`TextureRef`](TextureRef.md)?

### <a id="m-ior"></a>Ior

```csharp
public float Ior { get; }
```

KHR\_materials\_ior: index of refraction (1.5 when absent).

**Type:** `float`

### <a id="m-metallicfactor"></a>MetallicFactor

```csharp
public float MetallicFactor { get; }
```

Metalness multiplier.

**Type:** `float`

### <a id="m-metallicroughnesstexture"></a>MetallicRoughnessTexture

```csharp
public TextureRef? MetallicRoughnessTexture { get; }
```

Metal (blue) and roughness (green) texture, or null.

**Type:** [`TextureRef`](TextureRef.md)?

### <a id="m-name"></a>Name

```csharp
public string Name { get; }
```

The material's name, or empty.

**Type:** `string`

### <a id="m-normalscale"></a>NormalScale

```csharp
public float NormalScale { get; }
```

Scale of the normal map's xy.

**Type:** `float`

### <a id="m-normaltexture"></a>NormalTexture

```csharp
public TextureRef? NormalTexture { get; }
```

Tangent-space normal map, or null.

**Type:** [`TextureRef`](TextureRef.md)?

### <a id="m-occlusionstrength"></a>OcclusionStrength

```csharp
public float OcclusionStrength { get; }
```

How strongly occlusion applies (0 to 1).

**Type:** `float`

### <a id="m-occlusiontexture"></a>OcclusionTexture

```csharp
public TextureRef? OcclusionTexture { get; }
```

Ambient occlusion map (red channel), or null.

**Type:** [`TextureRef`](TextureRef.md)?

### <a id="m-roughnessfactor"></a>RoughnessFactor

```csharp
public float RoughnessFactor { get; }
```

Roughness multiplier.

**Type:** `float`

### <a id="m-sheen"></a>Sheen

```csharp
public SheenInfo? Sheen { get; }
```

KHR\_materials\_sheen, or null.

**Type:** [`SheenInfo`](SheenInfo.md)?

### <a id="m-specular"></a>Specular

```csharp
public SpecularInfo? Specular { get; }
```

KHR\_materials\_specular, or null.

**Type:** [`SpecularInfo`](SpecularInfo.md)?

### <a id="m-transmission"></a>Transmission

```csharp
public TransmissionInfo? Transmission { get; }
```

KHR\_materials\_transmission, or null.

**Type:** [`TransmissionInfo`](TransmissionInfo.md)?

### <a id="m-unlit"></a>Unlit

```csharp
public bool Unlit { get; }
```

KHR\_materials\_unlit: draw the base colour without lighting.

**Type:** `bool`

### <a id="m-volume"></a>Volume

```csharp
public VolumeInfo? Volume { get; }
```

KHR\_materials\_volume, or null.

**Type:** [`VolumeInfo`](VolumeInfo.md)?
