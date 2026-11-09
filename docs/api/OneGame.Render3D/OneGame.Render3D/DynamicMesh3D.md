[API reference](../../index.md) › [OneGame.Render3D](../index.md) › OneGame.Render3D

# DynamicMesh3D class

**Namespace:** `OneGame.Render3D`  
**Package:** [`RecursiveAI.OneGame.Render3D`](../index.md)  
**Implements:** [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class DynamicMesh3D : IDisposable
```

A triangle mesh a game builds and changes while running, such as a streamed voxel chunk. Create it with [`Renderer3D.CreateMesh()`](Renderer3D.md#m-createmesh-material3d-readonlyspan-meshvertex3d-readonlyspan-uint-string), draw it by adding [`DynamicMesh3D.Model`](DynamicMesh3D.md#m-model) to a [`Scene3D`](Scene3D.md) (or through `ModelLibrary.SetMesh`), change it with [`DynamicMesh3D.Update()`](DynamicMesh3D.md#m-update-readonlyspan-meshvertex3d-readonlyspan-uint) and free it with [`DynamicMesh3D.Dispose()`](DynamicMesh3D.md#m-dispose).

Buffers are sized in classes (at most 12.5% slack), so a mesh that changes size a little keeps its buffers and an update is one copy through the device's reused staging memory. A bigger or much smaller mesh gets new buffers and the old ones are released once the GPU has finished with the frames that used them. Nothing waits for the GPU. The mesh uses one shared [`Material3D`](Material3D.md); it is not skinned, morphed or pickable at the triangle level, and it is not thread-safe: call it from the thread that renders.

## Properties

| Member | Description |
| --- | --- |
| [`BoundsMax`](#m-boundsmax) | Opposite corner of the box around the vertices. |
| [`BoundsMin`](#m-boundsmin) | Corner of the box around the vertices, in mesh space. |
| [`GpuBytes`](#m-gpubytes) | Bytes of GPU buffer the mesh holds now (vertex plus index capacity). |
| [`IndexCount`](#m-indexcount) | Number of indices of the current contents. |
| [`Material`](#m-material) | The material the mesh is drawn with. |
| [`Model`](#m-model) | The drawable model: add it to a [`Scene3D`](Scene3D.md) with `Scene3D.Add`. |
| [`Name`](#m-name) | The name given at creation (used to label GPU buffers). |
| [`TriangleCount`](#m-trianglecount) | Number of triangles of the current contents. |
| [`VertexCount`](#m-vertexcount) | Number of vertices of the current contents. |

### <a id="m-boundsmax"></a>BoundsMax

```csharp
public Vector3 BoundsMax { get; }
```

Opposite corner of the box around the vertices.

**Type:** [`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)

### <a id="m-boundsmin"></a>BoundsMin

```csharp
public Vector3 BoundsMin { get; }
```

Corner of the box around the vertices, in mesh space.

**Type:** [`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)

### <a id="m-gpubytes"></a>GpuBytes

```csharp
public long GpuBytes { get; }
```

Bytes of GPU buffer the mesh holds now (vertex plus index capacity).

**Type:** `long`

### <a id="m-indexcount"></a>IndexCount

```csharp
public int IndexCount { get; }
```

Number of indices of the current contents.

**Type:** `int`

### <a id="m-material"></a>Material

```csharp
public Material3D Material { get; }
```

The material the mesh is drawn with. Many meshes share one.

**Type:** [`Material3D`](Material3D.md)

### <a id="m-model"></a>Model

```csharp
public Model3D Model { get; }
```

The drawable model: add it to a [`Scene3D`](Scene3D.md) with `Scene3D.Add`. It stays the same object across updates.

**Type:** [`Model3D`](Model3D.md)

### <a id="m-name"></a>Name

```csharp
public string Name { get; }
```

The name given at creation (used to label GPU buffers).

**Type:** `string`

### <a id="m-trianglecount"></a>TriangleCount

```csharp
public int TriangleCount { get; }
```

Number of triangles of the current contents.

**Type:** `int`

### <a id="m-vertexcount"></a>VertexCount

```csharp
public int VertexCount { get; }
```

Number of vertices of the current contents.

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`Dispose()`](#m-dispose) | Frees the GPU buffers once the GPU has finished with the frames that drew them. |
| [`Update(ReadOnlySpan<MeshVertex3D>, ReadOnlySpan<uint>)`](#m-update-readonlyspan-meshvertex3d-readonlyspan-uint) | Replaces the geometry. |
| [`Validate(ReadOnlySpan<MeshVertex3D>, ReadOnlySpan<uint>)`](#m-validate-readonlyspan-meshvertex3d-readonlyspan-uint) | Checks geometry the way [`DynamicMesh3D.Update()`](DynamicMesh3D.md#m-update-readonlyspan-meshvertex3d-readonlyspan-uint) and [`Renderer3D.CreateMesh()`](Renderer3D.md#m-createmesh-material3d-readonlyspan-meshvertex3d-readonlyspan-uint-string) do, without touching the GPU. |

### <a id="m-dispose"></a>Dispose()

```csharp
public void Dispose();
```

Frees the GPU buffers once the GPU has finished with the frames that drew them. Remove the model from scenes first.

### <a id="m-update-readonlyspan-meshvertex3d-readonlyspan-uint"></a>Update(ReadOnlySpan&lt;MeshVertex3D&gt;, ReadOnlySpan&lt;uint&gt;)

```csharp
public void Update(ReadOnlySpan<MeshVertex3D> vertices, ReadOnlySpan<uint> indices);
```

Replaces the geometry. The next frame shows the new contents.

**Parameters**

- `vertices` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)&lt;[`MeshVertex3D`](MeshVertex3D.md)&gt;): The vertices.
- `indices` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<uint>`): Triangle list indices into `vertices`; a multiple of 3, each below the vertex count.

**Exceptions**

- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): There are no vertices or indices, the index count is not a multiple of 3, or an index is out of range.
- [`ObjectDisposedException`](https://learn.microsoft.com/dotnet/api/system.objectdisposedexception): The mesh was freed.

### <a id="m-validate-readonlyspan-meshvertex3d-readonlyspan-uint"></a>Validate(ReadOnlySpan&lt;MeshVertex3D&gt;, ReadOnlySpan&lt;uint&gt;)

```csharp
public static void Validate(ReadOnlySpan<MeshVertex3D> vertices, ReadOnlySpan<uint> indices);
```

Checks geometry the way [`DynamicMesh3D.Update()`](DynamicMesh3D.md#m-update-readonlyspan-meshvertex3d-readonlyspan-uint) and [`Renderer3D.CreateMesh()`](Renderer3D.md#m-createmesh-material3d-readonlyspan-meshvertex3d-readonlyspan-uint-string) do, without touching the GPU.

**Parameters**

- `vertices` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)&lt;[`MeshVertex3D`](MeshVertex3D.md)&gt;): The vertices.
- `indices` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<uint>`): Triangle list indices.

**Exceptions**

- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): There are no vertices or indices, the index count is not a multiple of 3, or an index is out of range.
