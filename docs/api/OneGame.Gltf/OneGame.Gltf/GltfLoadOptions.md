[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# GltfLoadOptions class

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)

```csharp
public sealed class GltfLoadOptions
```

How a file is imported. Every property has a default, so `null` or `new GltfLoadOptions()` is the normal choice; set [`GltfLoadOptions.Limits`](GltfLoadOptions.md#m-limits) when loading content from a server and [`GltfLoadOptions.DecodeImages`](GltfLoadOptions.md#m-decodeimages) to false when only the geometry matters.

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
| [`DecodeImages`](#m-decodeimages) | Decode image pixels (PNG and JPEG, through the image library). |
| [`GenerateNormals`](#m-generatenormals) | Generate flat (per-face) normals for triangle primitives that have none, as the glTF specification requires. |
| [`Limits`](#m-limits) | The limits to enforce. |
| [`Tangents`](#m-tangents) | When to generate tangents. |

### <a id="m-decodeimages"></a>DecodeImages

```csharp
public bool DecodeImages { get; init; }
```

Decode image pixels (PNG and JPEG, through the image library). When false only each image's size is read, which is much faster for a tool that lists or measures models.

**Type:** `bool`

### <a id="m-generatenormals"></a>GenerateNormals

```csharp
public bool GenerateNormals { get; init; }
```

Generate flat (per-face) normals for triangle primitives that have none, as the glTF specification requires. Turn off to keep the data as authored.

**Type:** `bool`

### <a id="m-limits"></a>Limits

```csharp
public GltfLimits? Limits { get; init; }
```

The limits to enforce. Null means the defaults, which suit untrusted content: a file that exceeds one is refused with a [`GltfException`](GltfException.md) before anything large is allocated.

**Type:** [`GltfLimits`](GltfLimits.md)?

### <a id="m-tangents"></a>Tangents

```csharp
public TangentMode Tangents { get; init; }
```

When to generate tangents.

**Type:** [`TangentMode`](TangentMode.md)
