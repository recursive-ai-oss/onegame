[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# NodePose class

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)

```csharp
public sealed class NodePose
```

The animatable state of every node: translation, rotation, scale and morph weights.

## Properties

| Member | Description |
| --- | --- |
| [`Rotation`](#m-rotation) | Local rotation per node. |
| [`Scale`](#m-scale) | Local scale per node. |
| [`Translation`](#m-translation) | Local translation per node. |
| [`Weights`](#m-weights) | Morph weights per node (empty when the node has no morph targets). |

### <a id="m-rotation"></a>Rotation

```csharp
public Quaternion[] Rotation { get; }
```

Local rotation per node.

**Type:** [`Quaternion`](https://learn.microsoft.com/dotnet/api/system.numerics.quaternion)\[\]

### <a id="m-scale"></a>Scale

```csharp
public Vector3[] Scale { get; }
```

Local scale per node.

**Type:** [`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)\[\]

### <a id="m-translation"></a>Translation

```csharp
public Vector3[] Translation { get; }
```

Local translation per node.

**Type:** [`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)\[\]

### <a id="m-weights"></a>Weights

```csharp
public float[][] Weights { get; }
```

Morph weights per node (empty when the node has no morph targets).

**Type:** `float[][]`

## Methods

| Member | Description |
| --- | --- |
| [`ComputeWorld(GltfModel, Span<Matrix4x4>)`](#m-computeworld-gltfmodel-span-matrix4x4) | Computes every node's transform to the model's root space. |

### <a id="m-computeworld-gltfmodel-span-matrix4x4"></a>ComputeWorld(GltfModel, Span&lt;Matrix4x4&gt;)

```csharp
public void ComputeWorld(GltfModel model, Span<Matrix4x4> world);
```

Computes every node's transform to the model's root space.

**Parameters**

- `model` ([`GltfModel`](GltfModel.md)): The model this pose came from.
- `world` ([`Span`](https://learn.microsoft.com/dotnet/api/system.span-1)&lt;[`Matrix4x4`](https://learn.microsoft.com/dotnet/api/system.numerics.matrix4x4)&gt;): Receives one matrix per node (row-vector convention: child = local \* parent).
