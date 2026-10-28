[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# GltfNode class

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)

```csharp
public sealed class GltfNode
```

A node in the transform tree.

## Constructors

| Member | Description |
| --- | --- |
| [`GltfNode()`](#m-ctor) |  |

### <a id="m-ctor"></a>GltfNode()

```csharp
public GltfNode();
```

## Properties

| Member | Description |
| --- | --- |
| [`Camera`](#m-camera) | Index of the camera at this node, or -1. |
| [`Children`](#m-children) | Indices of the children. |
| [`Depth`](#m-depth) | Distance from a root (a root is 0). |
| [`FromMatrix`](#m-frommatrix) | True when the file gave a `matrix`; such a node cannot be animated. |
| [`Light`](#m-light) | Index of the punctual light at this node, or -1. |
| [`Local`](#m-local) | The exact local transform, in `System.Numerics` row-vector convention. |
| [`Mesh`](#m-mesh) | Index of the mesh drawn at this node, or -1. |
| [`Name`](#m-name) | The node's name, or empty. |
| [`Parent`](#m-parent) | Index of the parent node, or -1 for a root. |
| [`Rotation`](#m-rotation) | Local rotation (the rest pose). |
| [`Scale`](#m-scale) | Local scale (the rest pose). |
| [`Skin`](#m-skin) | Index of the skin applied to the mesh, or -1. |
| [`Translation`](#m-translation) | Local translation (the rest pose). |
| [`Weights`](#m-weights) | Default morph weights that override the mesh's, or null. |
| [`World`](#m-world) | The rest-pose transform from the node to the model's root space. |

### <a id="m-camera"></a>Camera

```csharp
public int Camera { get; }
```

Index of the camera at this node, or -1.

**Type:** `int`

### <a id="m-children"></a>Children

```csharp
public int[] Children { get; }
```

Indices of the children.

**Type:** `int[]`

### <a id="m-depth"></a>Depth

```csharp
public int Depth { get; }
```

Distance from a root (a root is 0).

**Type:** `int`

### <a id="m-frommatrix"></a>FromMatrix

```csharp
public bool FromMatrix { get; }
```

True when the file gave a `matrix`; such a node cannot be animated.

**Type:** `bool`

### <a id="m-light"></a>Light

```csharp
public int Light { get; }
```

Index of the punctual light at this node, or -1.

**Type:** `int`

### <a id="m-local"></a>Local

```csharp
public Matrix4x4 Local { get; }
```

The exact local transform, in `System.Numerics` row-vector convention.

**Type:** [`Matrix4x4`](https://learn.microsoft.com/dotnet/api/system.numerics.matrix4x4)

### <a id="m-mesh"></a>Mesh

```csharp
public int Mesh { get; }
```

Index of the mesh drawn at this node, or -1.

**Type:** `int`

### <a id="m-name"></a>Name

```csharp
public string Name { get; }
```

The node's name, or empty.

**Type:** `string`

### <a id="m-parent"></a>Parent

```csharp
public int Parent { get; }
```

Index of the parent node, or -1 for a root.

**Type:** `int`

### <a id="m-rotation"></a>Rotation

```csharp
public Quaternion Rotation { get; }
```

Local rotation (the rest pose).

**Type:** [`Quaternion`](https://learn.microsoft.com/dotnet/api/system.numerics.quaternion)

### <a id="m-scale"></a>Scale

```csharp
public Vector3 Scale { get; }
```

Local scale (the rest pose).

**Type:** [`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)

### <a id="m-skin"></a>Skin

```csharp
public int Skin { get; }
```

Index of the skin applied to the mesh, or -1.

**Type:** `int`

### <a id="m-translation"></a>Translation

```csharp
public Vector3 Translation { get; }
```

Local translation (the rest pose).

**Type:** [`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)

### <a id="m-weights"></a>Weights

```csharp
public float[]? Weights { get; }
```

Default morph weights that override the mesh's, or null.

**Type:** `float[]?`

### <a id="m-world"></a>World

```csharp
public Matrix4x4 World { get; }
```

The rest-pose transform from the node to the model's root space.

**Type:** [`Matrix4x4`](https://learn.microsoft.com/dotnet/api/system.numerics.matrix4x4)
