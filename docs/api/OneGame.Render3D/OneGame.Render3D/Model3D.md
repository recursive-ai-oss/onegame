[API reference](../../index.md) › [OneGame.Render3D](../index.md) › OneGame.Render3D

# Model3D class

**Namespace:** `OneGame.Render3D`  
**Package:** [`RecursiveAI.OneGame.Render3D`](../index.md)  
**Implements:** [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class Model3D : IDisposable
```

A glTF model uploaded to the GPU: meshes, materials and textures. Create it with [`Renderer3D.Upload()`](Renderer3D.md#m-upload-gltfmodel-int).

## Properties

| Member | Description |
| --- | --- |
| [`Materials`](#m-materials) | The materials of the model, in glTF order (the last one is the default material primitives without a material use). |
| [`Scene`](#m-scene) | The glTF scene being drawn. |
| [`Source`](#m-source) | The imported model. |
| [`TriangleCount`](#m-trianglecount) | Number of triangles drawn per frame at rest. |

### <a id="m-materials"></a>Materials

```csharp
public IReadOnlyList<Material3D> Materials { get; }
```

The materials of the model, in glTF order (the last one is the default material primitives without a material use).

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`Material3D`](Material3D.md)&gt;

### <a id="m-scene"></a>Scene

```csharp
public int Scene { get; }
```

The glTF scene being drawn.

**Type:** `int`

### <a id="m-source"></a>Source

```csharp
public GltfModel Source { get; }
```

The imported model.

**Type:** [`GltfModel`](../../OneGame.Gltf/OneGame.Gltf/GltfModel.md)

### <a id="m-trianglecount"></a>TriangleCount

```csharp
public int TriangleCount { get; }
```

Number of triangles drawn per frame at rest.

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`Dispose()`](#m-dispose) |  |
| [`FindMaterial(string)`](#m-findmaterial-string) | The first material with this name (the glTF material name), or null. |
| [`SetStyle(MaterialStyle3D)`](#m-setstyle-materialstyle3d) | Sets the style of every material of the model. |

### <a id="m-dispose"></a>Dispose()

```csharp
public void Dispose();
```

Implements [`IDisposable.Dispose()`](https://learn.microsoft.com/dotnet/api/system.idisposable.dispose).

### <a id="m-findmaterial-string"></a>FindMaterial(string)

```csharp
public Material3D? FindMaterial(string name);
```

The first material with this name (the glTF material name), or null. Use it to opt a loaded file's materials in to [`Material3D.PlanarReflection`](Material3D.md#m-planarreflection) or [`Material3D.UvScroll`](Material3D.md#m-uvscroll).

**Parameters**

- `name` (`string`): The material name, compared exactly.

**Returns:** [`Material3D`](Material3D.md)?

### <a id="m-setstyle-materialstyle3d"></a>SetStyle(MaterialStyle3D)

```csharp
public void SetStyle(MaterialStyle3D style);
```

Sets the style of every material of the model.

**Parameters**

- `style` ([`MaterialStyle3D`](MaterialStyle3D.md)): The lighting model.
