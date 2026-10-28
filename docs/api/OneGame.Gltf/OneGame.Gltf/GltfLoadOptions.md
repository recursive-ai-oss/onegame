[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# GltfLoadOptions class

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)

```csharp
public sealed class GltfLoadOptions
```

How a file is imported.

## Constructors

| Member | Description |
| --- | --- |
| [`GltfLoadOptions()`](#m-ctor) |  |

### <a id="m-ctor"></a>GltfLoadOptions()

```csharp
public GltfLoadOptions();
```

## Properties

| Member | Description |
| --- | --- |
| [`DecodeImages`](#m-decodeimages) | Decode image pixels. |
| [`GenerateNormals`](#m-generatenormals) | Generate flat normals for triangle primitives that have none. |
| [`Limits`](#m-limits) | The limits to enforce. |
| [`Tangents`](#m-tangents) | When to generate tangents. |

### <a id="m-decodeimages"></a>DecodeImages

```csharp
public bool DecodeImages { get; init; }
```

Decode image pixels. When false only each image's size is read.

**Type:** `bool`

### <a id="m-generatenormals"></a>GenerateNormals

```csharp
public bool GenerateNormals { get; init; }
```

Generate flat normals for triangle primitives that have none.

**Type:** `bool`

### <a id="m-limits"></a>Limits

```csharp
public GltfLimits? Limits { get; init; }
```

The limits to enforce. Null means the defaults.

**Type:** [`GltfLimits`](GltfLimits.md)?

### <a id="m-tangents"></a>Tangents

```csharp
public TangentMode Tangents { get; init; }
```

When to generate tangents.

**Type:** [`TangentMode`](TangentMode.md)
