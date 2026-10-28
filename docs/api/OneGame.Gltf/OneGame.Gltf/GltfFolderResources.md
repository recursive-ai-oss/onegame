[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# GltfFolderResources class

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)  
**Implements:** [`IGltfResources`](IGltfResources.md)

```csharp
public sealed class GltfFolderResources : IGltfResources
```

Reads external files from a folder and refuses to leave it.

## Constructors

| Member | Description |
| --- | --- |
| [`GltfFolderResources(string)`](#m-ctor-string) | Creates a resolver rooted at a folder. |

### <a id="m-ctor-string"></a>GltfFolderResources(string)

```csharp
public GltfFolderResources(string root);
```

Creates a resolver rooted at a folder.

**Parameters**

- `root` (`string`): The folder that holds the .gltf file.

## Methods

| Member | Description |
| --- | --- |
| [`Read(string, long)`](#m-read-string-long) | Reads a file. |

### <a id="m-read-string-long"></a>Read(string, long)

```csharp
public byte[]? Read(string relativeUri, long maxBytes);
```

Reads a file. The uri has been checked by [`GltfUri.CheckRelative()`](GltfUri.md#m-checkrelative-string) already.

**Parameters**

- `relativeUri` (`string`): The decoded relative path, with forward slashes.
- `maxBytes` (`long`): The most bytes the caller accepts; reading more must fail rather than allocate.

**Returns:** `byte[]?` — The bytes, or null when the file does not exist.
