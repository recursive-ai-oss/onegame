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
| [`Dispose()`](#m-dispose) | Releases the GPU copies. |

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

### <a id="m-dispose"></a>Dispose()

```csharp
public void Dispose();
```

Releases the GPU copies. The renderer that made them must still be alive.
