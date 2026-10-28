[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# VolumeInfo class

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)

```csharp
public sealed class VolumeInfo
```

KHR\_materials\_volume.

## Constructors

| Member | Description |
| --- | --- |
| [`VolumeInfo()`](#m-ctor) |  |

### <a id="m-ctor"></a>VolumeInfo()

```csharp
public VolumeInfo();
```

## Properties

| Member | Description |
| --- | --- |
| [`AttenuationColor`](#m-attenuationcolor) | Colour light has after travelling [`VolumeInfo.AttenuationDistance`](VolumeInfo.md#m-attenuationdistance) (linear RGB). |
| [`AttenuationDistance`](#m-attenuationdistance) | Distance at which light is attenuated to [`VolumeInfo.AttenuationColor`](VolumeInfo.md#m-attenuationcolor); infinity when absent. |
| [`ThicknessFactor`](#m-thicknessfactor) | Thickness of the volume in model units. |
| [`ThicknessTexture`](#m-thicknesstexture) | Thickness texture (green channel), or null. |

### <a id="m-attenuationcolor"></a>AttenuationColor

```csharp
public Vector3 AttenuationColor { get; }
```

Colour light has after travelling [`VolumeInfo.AttenuationDistance`](VolumeInfo.md#m-attenuationdistance) (linear RGB).

**Type:** [`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)

### <a id="m-attenuationdistance"></a>AttenuationDistance

```csharp
public float AttenuationDistance { get; }
```

Distance at which light is attenuated to [`VolumeInfo.AttenuationColor`](VolumeInfo.md#m-attenuationcolor); infinity when absent.

**Type:** `float`

### <a id="m-thicknessfactor"></a>ThicknessFactor

```csharp
public float ThicknessFactor { get; }
```

Thickness of the volume in model units.

**Type:** `float`

### <a id="m-thicknesstexture"></a>ThicknessTexture

```csharp
public TextureRef? ThicknessTexture { get; }
```

Thickness texture (green channel), or null.

**Type:** [`TextureRef`](TextureRef.md)?
