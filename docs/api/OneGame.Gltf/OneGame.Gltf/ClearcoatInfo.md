[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# ClearcoatInfo class

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)

```csharp
public sealed class ClearcoatInfo
```

KHR\_materials\_clearcoat.

## Constructors

| Member | Description |
| --- | --- |
| [`ClearcoatInfo()`](#m-ctor) |  |

### <a id="m-ctor"></a>ClearcoatInfo()

```csharp
public ClearcoatInfo();
```

## Properties

| Member | Description |
| --- | --- |
| [`Factor`](#m-factor) | Clearcoat strength. |
| [`NormalScale`](#m-normalscale) | Scale of the coat normal map. |
| [`NormalTexture`](#m-normaltexture) | Normal map for the coat, or null. |
| [`RoughnessFactor`](#m-roughnessfactor) | Clearcoat roughness. |
| [`RoughnessTexture`](#m-roughnesstexture) | Roughness texture (green channel), or null. |
| [`Texture`](#m-texture) | Strength texture (red channel), or null. |

### <a id="m-factor"></a>Factor

```csharp
public float Factor { get; }
```

Clearcoat strength.

**Type:** `float`

### <a id="m-normalscale"></a>NormalScale

```csharp
public float NormalScale { get; }
```

Scale of the coat normal map.

**Type:** `float`

### <a id="m-normaltexture"></a>NormalTexture

```csharp
public TextureRef? NormalTexture { get; }
```

Normal map for the coat, or null.

**Type:** [`TextureRef`](TextureRef.md)?

### <a id="m-roughnessfactor"></a>RoughnessFactor

```csharp
public float RoughnessFactor { get; }
```

Clearcoat roughness.

**Type:** `float`

### <a id="m-roughnesstexture"></a>RoughnessTexture

```csharp
public TextureRef? RoughnessTexture { get; }
```

Roughness texture (green channel), or null.

**Type:** [`TextureRef`](TextureRef.md)?

### <a id="m-texture"></a>Texture

```csharp
public TextureRef? Texture { get; }
```

Strength texture (red channel), or null.

**Type:** [`TextureRef`](TextureRef.md)?
