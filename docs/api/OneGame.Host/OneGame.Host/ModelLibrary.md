[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# ModelLibrary class

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)  
**Implements:** [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class ModelLibrary : IDisposable
```

The models a game's entities can show, by name. Each keeps its imported glTF data, which picking reads without a graphics device, and its GPU copy, which is uploaded the first time a frame draws it.

## Constructors

| Member | Description |
| --- | --- |
| [`ModelLibrary()`](#m-ctor) |  |

### <a id="m-ctor"></a>ModelLibrary()

```csharp
public ModelLibrary();
```

## Properties

| Member | Description |
| --- | --- |
| [`Ids`](#m-ids) | The model names in the order they were added. |
| [`this[string]`](#m-item-string) | The imported model with this name. |

### <a id="m-ids"></a>Ids

```csharp
public IEnumerable<string> Ids { get; }
```

The model names in the order they were added.

**Type:** [`IEnumerable`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable-1)`<string>`

### <a id="m-item-string"></a>this\[string\]

```csharp
public GltfModel this[string id] { get; }
```

The imported model with this name.

**Parameters**

- `id` (`string`)

**Type:** [`GltfModel`](../../OneGame.Gltf/OneGame.Gltf/GltfModel.md)

**Exceptions**

- [`UnknownModelException`](UnknownModelException.md): There is none.

## Methods

| Member | Description |
| --- | --- |
| [`Add(string, GltfModel)`](#m-add-string-gltfmodel) | Adds a model under a name. |
| [`Contains(string)`](#m-contains-string) | Whether a model or mesh with this name is in the library. |
| [`Dispose()`](#m-dispose) | Releases the GPU copies. |
| [`Remove(string)`](#m-remove-string) | Takes a model or mesh out of the library. |
| [`Replace(string, GltfModel)`](#m-replace-string-gltfmodel) | Replaces the model named `id`, or adds it. |
| [`SetMesh(string, Material3D, ReadOnlySpan<MeshVertex3D>, ReadOnlySpan<uint>)`](#m-setmesh-string-material3d-readonlyspan-meshvertex3d-readonlyspan-uint) | Adds a mesh built at run time under a name, or replaces the one with that name; entities draw it with `SceneWriter.Model` like any model, and the next frame shows the new geometry. |

### <a id="m-add-string-gltfmodel"></a>Add(string, GltfModel)

```csharp
public void Add(string id, GltfModel model);
```

Adds a model under a name.

**Parameters**

- `id` (`string`): The name entities use.
- `model` ([`GltfModel`](../../OneGame.Gltf/OneGame.Gltf/GltfModel.md)): The imported model.

**Exceptions**

- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): The name is taken.

### <a id="m-contains-string"></a>Contains(string)

```csharp
public bool Contains(string id);
```

Whether a model or mesh with this name is in the library.

**Parameters**

- `id` (`string`): The name.

**Returns:** `bool`

### <a id="m-dispose"></a>Dispose()

```csharp
public void Dispose();
```

Releases the GPU copies. The renderer that made them must still be alive.

### <a id="m-remove-string"></a>Remove(string)

```csharp
public bool Remove(string id);
```

Takes a model or mesh out of the library. Its GPU memory is released when the next frame is built (and the device frees it once the frames that used it have finished). Entities still drawing the name draw nothing until it is added again.

**Parameters**

- `id` (`string`): The name.

**Returns:** `bool` — False when there was none.

### <a id="m-replace-string-gltfmodel"></a>Replace(string, GltfModel)

```csharp
public void Replace(string id, GltfModel model);
```

Replaces the model named `id`, or adds it. Entities drawing the name show the new model from the next frame.

**Parameters**

- `id` (`string`): The name entities use.
- `model` ([`GltfModel`](../../OneGame.Gltf/OneGame.Gltf/GltfModel.md)): The new imported model.

### <a id="m-setmesh-string-material3d-readonlyspan-meshvertex3d-readonlyspan-uint"></a>SetMesh(string, Material3D, ReadOnlySpan&lt;MeshVertex3D&gt;, ReadOnlySpan&lt;uint&gt;)

```csharp
public void SetMesh(string id, Material3D material, ReadOnlySpan<MeshVertex3D> vertices, ReadOnlySpan<uint> indices);
```

Adds a mesh built at run time under a name, or replaces the one with that name; entities draw it with `SceneWriter.Model` like any model, and the next frame shows the new geometry. The vertices are copied (into pooled memory) and uploaded when the next frame is built, so many calls in one frame cost one upload each, and the spans need not outlive the call. See [`DynamicMesh3D`](../../OneGame.Render3D/OneGame.Render3D/DynamicMesh3D.md) for how buffers are reused. Empty geometry keeps the name but draws nothing. Call it from the thread that renders. Meshes cannot be picked.

**Parameters**

- `id` (`string`): The name entities use.
- `material` ([`Material3D`](../../OneGame.Render3D/OneGame.Render3D/Material3D.md)): The shared material, from [`Renderer3D.CreateMaterial()`](../../OneGame.Render3D/OneGame.Render3D/Renderer3D.md#m-creatematerial-string-materialalpha3d-bool-vector4-float) or a model's materials.
- `vertices` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)&lt;[`MeshVertex3D`](../../OneGame.Render3D/OneGame.Render3D/MeshVertex3D.md)&gt;): The vertices.
- `indices` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<uint>`): Triangle list indices; a multiple of 3, each below the vertex count.

**Exceptions**

- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): The geometry is invalid, or `id` names an imported model (remove it first).
