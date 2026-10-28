[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# GltfSkin class

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)

```csharp
public sealed class GltfSkin
```

A skin: the joints that deform a mesh.

## Constructors

| Member | Description |
| --- | --- |
| [`GltfSkin()`](#m-ctor) |  |

### <a id="m-ctor"></a>GltfSkin()

```csharp
public GltfSkin();
```

## Properties

| Member | Description |
| --- | --- |
| [`InverseBindMatrices`](#m-inversebindmatrices) | One inverse bind matrix per joint (identity when the file has none). |
| [`Joints`](#m-joints) | Node indices of the joints, in the order the vertex `JOINTS_0` values refer to. |
| [`Name`](#m-name) | The skin's name, or empty. |
| [`Skeleton`](#m-skeleton) | Index of the skeleton root node, or -1. |

### <a id="m-inversebindmatrices"></a>InverseBindMatrices

```csharp
public Matrix4x4[] InverseBindMatrices { get; }
```

One inverse bind matrix per joint (identity when the file has none).

**Type:** [`Matrix4x4`](https://learn.microsoft.com/dotnet/api/system.numerics.matrix4x4)\[\]

### <a id="m-joints"></a>Joints

```csharp
public int[] Joints { get; }
```

Node indices of the joints, in the order the vertex `JOINTS_0` values refer to.

**Type:** `int[]`

### <a id="m-name"></a>Name

```csharp
public string Name { get; }
```

The skin's name, or empty.

**Type:** `string`

### <a id="m-skeleton"></a>Skeleton

```csharp
public int Skeleton { get; }
```

Index of the skeleton root node, or -1.

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`ComputeJointMatrices(ReadOnlySpan<Matrix4x4>, int, Span<Matrix4x4>)`](#m-computejointmatrices-readonlyspan-matrix4x4-int-span-matrix4x4) | Computes the matrices the vertex shader multiplies a vertex by: `inverse(meshNodeWorld)`, the joint's world, then its inverse bind matrix (glTF §Skins). |

### <a id="m-computejointmatrices-readonlyspan-matrix4x4-int-span-matrix4x4"></a>ComputeJointMatrices(ReadOnlySpan&lt;Matrix4x4&gt;, int, Span&lt;Matrix4x4&gt;)

```csharp
public void ComputeJointMatrices(ReadOnlySpan<Matrix4x4> world, int meshNode, Span<Matrix4x4> destination);
```

Computes the matrices the vertex shader multiplies a vertex by: `inverse(meshNodeWorld)`, the joint's world, then its inverse bind matrix (glTF §Skins).

**Parameters**

- `world` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)&lt;[`Matrix4x4`](https://learn.microsoft.com/dotnet/api/system.numerics.matrix4x4)&gt;): World matrices of all nodes, for example from [`NodePose.ComputeWorld()`](NodePose.md#m-computeworld-gltfmodel-span-matrix4x4).
- `meshNode` (`int`): The node that carries the mesh and the skin.
- `destination` ([`Span`](https://learn.microsoft.com/dotnet/api/system.span-1)&lt;[`Matrix4x4`](https://learn.microsoft.com/dotnet/api/system.numerics.matrix4x4)&gt;): Receives one matrix per joint; at least [`GltfSkin.Joints`](GltfSkin.md#m-joints) long.
