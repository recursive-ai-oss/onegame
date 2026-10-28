[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# GltfTexture class

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)

```csharp
public sealed class GltfTexture
```

A texture: an image with a sampler.

## Constructors

| Member | Description |
| --- | --- |
| [`GltfTexture()`](#m-ctor) |  |

### <a id="m-ctor"></a>GltfTexture()

```csharp
public GltfTexture();
```

## Properties

| Member | Description |
| --- | --- |
| [`Name`](#m-name) | The texture's name, or empty. |
| [`Sampler`](#m-sampler) | Index into [`GltfModel.Samplers`](GltfModel.md#m-samplers), or -1 for the default (repeat, linear). |
| [`Source`](#m-source) | Index into [`GltfModel.Images`](GltfModel.md#m-images), or -1 when the source needs an unsupported extension. |

### <a id="m-name"></a>Name

```csharp
public string Name { get; }
```

The texture's name, or empty.

**Type:** `string`

### <a id="m-sampler"></a>Sampler

```csharp
public int Sampler { get; }
```

Index into [`GltfModel.Samplers`](GltfModel.md#m-samplers), or -1 for the default (repeat, linear).

**Type:** `int`

### <a id="m-source"></a>Source

```csharp
public int Source { get; }
```

Index into [`GltfModel.Images`](GltfModel.md#m-images), or -1 when the source needs an unsupported extension.

**Type:** `int`
