[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# GltfPrimitive class

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)

```csharp
public sealed class GltfPrimitive
```

One drawable piece of a mesh: vertex streams, indices and a material.

## Constructors

| Member | Description |
| --- | --- |
| [`GltfPrimitive()`](#m-ctor) |  |

### <a id="m-ctor"></a>GltfPrimitive()

```csharp
public GltfPrimitive();
```

## Fields

| Member | Description |
| --- | --- |
| [`PackedVertexSize`](#m-packedvertexsize) | Bytes per vertex of [`GltfPrimitive.PackVertices()`](GltfPrimitive.md#m-packvertices). |

### <a id="m-packedvertexsize"></a>PackedVertexSize

```csharp
public const int PackedVertexSize = 96;
```

Bytes per vertex of [`GltfPrimitive.PackVertices()`](GltfPrimitive.md#m-packvertices).

**Type:** `int`

## Properties

| Member | Description |
| --- | --- |
| [`Bounds`](#m-bounds) | The position bounds of the base mesh (morph targets not applied). |
| [`Colors`](#m-colors) | Vertex colours (RGBA), or null. |
| [`HasNormals`](#m-hasnormals) | True when the file supplied normals or they were generated (false for points and lines without normals). |
| [`Indices`](#m-indices) | The shape indices into the vertex streams; always present. |
| [`Joints`](#m-joints) | Joint indices, or null when the primitive is not skinned. |
| [`Material`](#m-material) | Index of the material, or -1 for the glTF default material. |
| [`MorphTargets`](#m-morphtargets) | Morph targets (deltas), all with one entry per vertex. |
| [`Normals`](#m-normals) | Vertex normals; always present (flat for generated ones, or (0,0,1) for points and lines without any). |
| [`NormalsGenerated`](#m-normalsgenerated) | True when the normals were generated flat, which also split shared vertices. |
| [`Positions`](#m-positions) | Vertex positions. |
| [`Tangents`](#m-tangents) | Tangents (xyz and a sign in w), or null. |
| [`TangentsGenerated`](#m-tangentsgenerated) | True when the tangents were generated. |
| [`Topology`](#m-topology) | How [`GltfPrimitive.Indices`](GltfPrimitive.md#m-indices) form shapes. |
| [`Uv0`](#m-uv0) | First texture coordinate set, or null. |
| [`Uv1`](#m-uv1) | Second texture coordinate set, or null. |
| [`VertexCount`](#m-vertexcount) | The number of vertices. |
| [`Weights`](#m-weights) | Joint weights, or null when the primitive is not skinned. |

### <a id="m-bounds"></a>Bounds

```csharp
public GltfBounds Bounds { get; }
```

The position bounds of the base mesh (morph targets not applied).

**Type:** [`GltfBounds`](GltfBounds.md)

### <a id="m-colors"></a>Colors

```csharp
public Vector4[]? Colors { get; }
```

Vertex colours (RGBA), or null.

**Type:** [`Vector4`](https://learn.microsoft.com/dotnet/api/system.numerics.vector4)\[\]?

### <a id="m-hasnormals"></a>HasNormals

```csharp
public bool HasNormals { get; }
```

True when the file supplied normals or they were generated (false for points and lines without normals).

**Type:** `bool`

### <a id="m-indices"></a>Indices

```csharp
public uint[] Indices { get; }
```

The shape indices into the vertex streams; always present.

**Type:** `uint[]`

### <a id="m-joints"></a>Joints

```csharp
public JointIndices[]? Joints { get; }
```

Joint indices, or null when the primitive is not skinned.

**Type:** [`JointIndices`](JointIndices.md)\[\]?

### <a id="m-material"></a>Material

```csharp
public int Material { get; }
```

Index of the material, or -1 for the glTF default material.

**Type:** `int`

### <a id="m-morphtargets"></a>MorphTargets

```csharp
public MorphTarget[] MorphTargets { get; }
```

Morph targets (deltas), all with one entry per vertex.

**Type:** [`MorphTarget`](MorphTarget.md)\[\]

### <a id="m-normals"></a>Normals

```csharp
public Vector3[] Normals { get; }
```

Vertex normals; always present (flat for generated ones, or (0,0,1) for points and lines without any).

**Type:** [`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)\[\]

### <a id="m-normalsgenerated"></a>NormalsGenerated

```csharp
public bool NormalsGenerated { get; }
```

True when the normals were generated flat, which also split shared vertices.

**Type:** `bool`

### <a id="m-positions"></a>Positions

```csharp
public Vector3[] Positions { get; }
```

Vertex positions.

**Type:** [`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)\[\]

### <a id="m-tangents"></a>Tangents

```csharp
public Vector4[]? Tangents { get; }
```

Tangents (xyz and a sign in w), or null.

**Type:** [`Vector4`](https://learn.microsoft.com/dotnet/api/system.numerics.vector4)\[\]?

### <a id="m-tangentsgenerated"></a>TangentsGenerated

```csharp
public bool TangentsGenerated { get; }
```

True when the tangents were generated.

**Type:** `bool`

### <a id="m-topology"></a>Topology

```csharp
public GltfTopology Topology { get; }
```

How [`GltfPrimitive.Indices`](GltfPrimitive.md#m-indices) form shapes.

**Type:** [`GltfTopology`](GltfTopology.md)

### <a id="m-uv0"></a>Uv0

```csharp
public Vector2[]? Uv0 { get; }
```

First texture coordinate set, or null.

**Type:** [`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)\[\]?

### <a id="m-uv1"></a>Uv1

```csharp
public Vector2[]? Uv1 { get; }
```

Second texture coordinate set, or null.

**Type:** [`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)\[\]?

### <a id="m-vertexcount"></a>VertexCount

```csharp
public int VertexCount { get; }
```

The number of vertices.

**Type:** `int`

### <a id="m-weights"></a>Weights

```csharp
public Vector4[]? Weights { get; }
```

Joint weights, or null when the primitive is not skinned.

**Type:** [`Vector4`](https://learn.microsoft.com/dotnet/api/system.numerics.vector4)\[\]?

## Methods

| Member | Description |
| --- | --- |
| [`PackIndices(out IndexWidth)`](#m-packindices-indexwidth) | Packs the indices as 16-bit when every vertex index fits, else 32-bit, little-endian. |
| [`PackVertices()`](#m-packvertices) | Packs the vertices into the fixed 96-byte little-endian layout documented in the interface description. |

### <a id="m-packindices-indexwidth"></a>PackIndices(out IndexWidth)

```csharp
public byte[] PackIndices(out IndexWidth width);
```

Packs the indices as 16-bit when every vertex index fits, else 32-bit, little-endian.

**Parameters**

- `width` ([`IndexWidth`](IndexWidth.md)): The chosen width.

**Returns:** `byte[]` — The index bytes.

### <a id="m-packvertices"></a>PackVertices()

```csharp
public byte[] PackVertices();
```

Packs the vertices into the fixed 96-byte little-endian layout documented in the interface description.

**Returns:** `byte[]` — VertexCount times [`GltfPrimitive.PackedVertexSize`](GltfPrimitive.md#m-packedvertexsize) bytes.
