[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# GltfMesh class

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)

```csharp
public sealed class GltfMesh
```

A mesh: one or more primitives sharing a node and morph weights.

## Constructors

| Member | Description |
| --- | --- |
| [`GltfMesh()`](#m-ctor) |  |

### <a id="m-ctor"></a>GltfMesh()

```csharp
public GltfMesh();
```

## Properties

| Member | Description |
| --- | --- |
| [`Bounds`](#m-bounds) | The union of the primitives' bounds. |
| [`DefaultWeights`](#m-defaultweights) | Default morph weights, one per target (zeros when the file gives none). |
| [`MorphTargetCount`](#m-morphtargetcount) | The number of morph targets (the same for every primitive). |
| [`Name`](#m-name) | The mesh's name, or empty. |
| [`Primitives`](#m-primitives) | The primitives. |

### <a id="m-bounds"></a>Bounds

```csharp
public GltfBounds Bounds { get; }
```

The union of the primitives' bounds.

**Type:** [`GltfBounds`](GltfBounds.md)

### <a id="m-defaultweights"></a>DefaultWeights

```csharp
public float[] DefaultWeights { get; }
```

Default morph weights, one per target (zeros when the file gives none).

**Type:** `float[]`

### <a id="m-morphtargetcount"></a>MorphTargetCount

```csharp
public int MorphTargetCount { get; }
```

The number of morph targets (the same for every primitive).

**Type:** `int`

### <a id="m-name"></a>Name

```csharp
public string Name { get; }
```

The mesh's name, or empty.

**Type:** `string`

### <a id="m-primitives"></a>Primitives

```csharp
public GltfPrimitive[] Primitives { get; }
```

The primitives.

**Type:** [`GltfPrimitive`](GltfPrimitive.md)\[\]
