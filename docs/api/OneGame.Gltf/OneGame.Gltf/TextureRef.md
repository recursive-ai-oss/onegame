[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# TextureRef class

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)

```csharp
public sealed class TextureRef
```

A reference from a material slot to a texture.

## Constructors

| Member | Description |
| --- | --- |
| [`TextureRef()`](#m-ctor) |  |

### <a id="m-ctor"></a>TextureRef()

```csharp
public TextureRef();
```

## Properties

| Member | Description |
| --- | --- |
| [`TexCoord`](#m-texcoord) | Which texture coordinate set to use (0 or 1 are drawn; others are kept as written). |
| [`Texture`](#m-texture) | Index into [`GltfModel.Textures`](GltfModel.md#m-textures). |
| [`Transform`](#m-transform) | The KHR\_texture\_transform, or null. |
| [`UvMatrix`](#m-uvmatrix) | The coordinate-transform matrix (identity without an extension), `uv' = uv * M`. |

### <a id="m-texcoord"></a>TexCoord

```csharp
public int TexCoord { get; }
```

Which texture coordinate set to use (0 or 1 are drawn; others are kept as written).

**Type:** `int`

### <a id="m-texture"></a>Texture

```csharp
public int Texture { get; }
```

Index into [`GltfModel.Textures`](GltfModel.md#m-textures).

**Type:** `int`

### <a id="m-transform"></a>Transform

```csharp
public TextureTransform? Transform { get; }
```

The KHR\_texture\_transform, or null.

**Type:** [`TextureTransform`](TextureTransform.md)?

### <a id="m-uvmatrix"></a>UvMatrix

```csharp
public Matrix3x2 UvMatrix { get; }
```

The coordinate-transform matrix (identity without an extension), `uv' = uv * M`.

**Type:** [`Matrix3x2`](https://learn.microsoft.com/dotnet/api/system.numerics.matrix3x2)
