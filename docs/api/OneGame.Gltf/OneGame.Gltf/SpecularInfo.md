[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# SpecularInfo class

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)

```csharp
public sealed class SpecularInfo
```

KHR\_materials\_specular.

## Constructors

| Member | Description |
| --- | --- |
| [`SpecularInfo()`](#m-ctor) |  |

### <a id="m-ctor"></a>SpecularInfo()

```csharp
public SpecularInfo();
```

## Properties

| Member | Description |
| --- | --- |
| [`ColorFactor`](#m-colorfactor) | Tint of the specular reflection (linear RGB). |
| [`ColorTexture`](#m-colortexture) | Tint texture (sRGB RGB), or null. |
| [`Factor`](#m-factor) | Strength of the dielectric specular reflection. |
| [`Texture`](#m-texture) | Strength texture (alpha channel), or null. |

### <a id="m-colorfactor"></a>ColorFactor

```csharp
public Vector3 ColorFactor { get; }
```

Tint of the specular reflection (linear RGB).

**Type:** [`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)

### <a id="m-colortexture"></a>ColorTexture

```csharp
public TextureRef? ColorTexture { get; }
```

Tint texture (sRGB RGB), or null.

**Type:** [`TextureRef`](TextureRef.md)?

### <a id="m-factor"></a>Factor

```csharp
public float Factor { get; }
```

Strength of the dielectric specular reflection.

**Type:** `float`

### <a id="m-texture"></a>Texture

```csharp
public TextureRef? Texture { get; }
```

Strength texture (alpha channel), or null.

**Type:** [`TextureRef`](TextureRef.md)?
