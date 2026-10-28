[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# IGltfResources interface

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)

```csharp
public interface IGltfResources
```

Supplies the external files (buffers and images) a `.gltf` names.

## Methods

| Member | Description |
| --- | --- |
| [`Read(string, long)`](#m-read-string-long) | Reads a file. |

### <a id="m-read-string-long"></a>Read(string, long)

```csharp
byte[]? Read(string relativeUri, long maxBytes);
```

Reads a file. The uri has been checked by [`GltfUri.CheckRelative()`](GltfUri.md#m-checkrelative-string) already.

**Parameters**

- `relativeUri` (`string`): The decoded relative path, with forward slashes.
- `maxBytes` (`long`): The most bytes the caller accepts; reading more must fail rather than allocate.

**Returns:** `byte[]?` — The bytes, or null when the file does not exist.
