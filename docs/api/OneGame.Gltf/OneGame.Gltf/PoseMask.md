[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# PoseMask class

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)

```csharp
public sealed class PoseMask
```

Per-node weights that limit how much of a blend applies: for example only the upper body of a character.

## Properties

| Member | Description |
| --- | --- |
| [`this[int]`](#m-item-int) | The weight of a node, 0 to 1. |

### <a id="m-item-int"></a>this\[int\]

```csharp
public float this[int node] { get; }
```

The weight of a node, 0 to 1.

**Parameters**

- `node` (`int`)

**Type:** `float`

## Methods

| Member | Description |
| --- | --- |
| [`FromWeights(float[])`](#m-fromweights-float) | A mask from explicit weights, one per node. |
| [`Subtree(GltfModel, int, float)`](#m-subtree-gltfmodel-int-float) | A mask that covers a node and everything below it. |

### <a id="m-fromweights-float"></a>FromWeights(float\[\])

```csharp
public static PoseMask FromWeights(float[] weights);
```

A mask from explicit weights, one per node.

**Parameters**

- `weights` (`float[]`): Values from 0 to 1.

**Returns:** [`PoseMask`](PoseMask.md)

### <a id="m-subtree-gltfmodel-int-float"></a>Subtree(GltfModel, int, float)

```csharp
public static PoseMask Subtree(GltfModel model, int node, float weight = 1f);
```

A mask that covers a node and everything below it.

**Parameters**

- `model` ([`GltfModel`](GltfModel.md)): The model.
- `node` (`int`): The top node of the part to cover.
- `weight` (`float`): The weight of the covered nodes; the rest are 0.

**Returns:** [`PoseMask`](PoseMask.md)
