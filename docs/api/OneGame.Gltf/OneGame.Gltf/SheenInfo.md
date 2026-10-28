[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# SheenInfo class

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)

```csharp
public sealed class SheenInfo
```

KHR\_materials\_sheen.

## Constructors

| Member | Description |
| --- | --- |
| [`SheenInfo()`](#m-ctor) |  |

### <a id="m-ctor"></a>SheenInfo()

```csharp
public SheenInfo();
```

## Properties

| Member | Description |
| --- | --- |
| [`ColorFactor`](#m-colorfactor) | Sheen colour (linear RGB). |
| [`ColorTexture`](#m-colortexture) | Sheen colour texture (sRGB RGB), or null. |
| [`RoughnessFactor`](#m-roughnessfactor) | Sheen roughness. |
| [`RoughnessTexture`](#m-roughnesstexture) | Sheen roughness texture (alpha channel), or null. |

### <a id="m-colorfactor"></a>ColorFactor

```csharp
public Vector3 ColorFactor { get; }
```

Sheen colour (linear RGB).

**Type:** [`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)

### <a id="m-colortexture"></a>ColorTexture

```csharp
public TextureRef? ColorTexture { get; }
```

Sheen colour texture (sRGB RGB), or null.

**Type:** [`TextureRef`](TextureRef.md)?

### <a id="m-roughnessfactor"></a>RoughnessFactor

```csharp
public float RoughnessFactor { get; }
```

Sheen roughness.

**Type:** `float`

### <a id="m-roughnesstexture"></a>RoughnessTexture

```csharp
public TextureRef? RoughnessTexture { get; }
```

Sheen roughness texture (alpha channel), or null.

**Type:** [`TextureRef`](TextureRef.md)?
