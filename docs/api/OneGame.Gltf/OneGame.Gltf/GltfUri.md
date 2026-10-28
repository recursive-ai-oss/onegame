[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# GltfUri static class

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)

```csharp
public static class GltfUri
```

The uri rules: data uris are decoded in memory, everything else must be a plain relative path.

## Methods

| Member | Description |
| --- | --- |
| [`CheckRelative(string)`](#m-checkrelative-string) | Decodes percent escapes and refuses anything that could reach outside the model's folder. |

### <a id="m-checkrelative-string"></a>CheckRelative(string)

```csharp
public static string CheckRelative(string uri);
```

Decodes percent escapes and refuses anything that could reach outside the model's folder.

**Parameters**

- `uri` (`string`): The uri as written in the file.

**Returns:** `string` — The relative path with forward slashes.

**Exceptions**

- [`GltfException`](GltfException.md): `uri_not_allowed` for a scheme, an absolute path, a drive, a NUL, a backslash or a `..` segment.
