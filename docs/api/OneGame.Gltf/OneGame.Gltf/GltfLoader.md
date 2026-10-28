[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# GltfLoader static class

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)

```csharp
public static class GltfLoader
```

Imports glTF 2.0 files (`.gltf` and `.glb`) into a [`GltfModel`](GltfModel.md): meshes, metal/rough materials, textures, skins, animations, cameras and punctual lights. The import is CPU-side and does not need a graphics device; hand the model to `Renderer3D.Upload` to draw it, or read its meshes directly. Input is untrusted: every count and size is checked against [`GltfLimits`](GltfLimits.md) before anything is allocated, and a bad file throws [`GltfException`](GltfException.md) rather than crashing.

**Example**

```csharp
GltfModel model = GltfLoader.LoadFile("models/Box.glb");
Console.WriteLine($"{model.Meshes.Length} meshes, {model.Materials.Length} materials");
foreach (var warning in model.Warnings)
{
    Console.WriteLine($"{warning.Code}: {warning.Message}");   // something unsupported was skipped
}
```

## Methods

| Member | Description |
| --- | --- |
| [`Load(ReadOnlyMemory<byte>, GltfLoadOptions?, IGltfResources?)`](#m-load-readonlymemory-byte-gltfloadoptions-igltfresources) | Imports a file already in memory. |
| [`LoadFile(string, GltfLoadOptions?)`](#m-loadfile-string-gltfloadoptions) | Imports a file from disk; external buffers and images resolve inside the file's folder. |

### <a id="m-load-readonlymemory-byte-gltfloadoptions-igltfresources"></a>Load(ReadOnlyMemory&lt;byte&gt;, GltfLoadOptions?, IGltfResources?)

```csharp
public static GltfModel Load(ReadOnlyMemory<byte> file, GltfLoadOptions? options = null, IGltfResources? resources = null);
```

Imports a file already in memory.

**Parameters**

- `file` ([`ReadOnlyMemory`](https://learn.microsoft.com/dotnet/api/system.readonlymemory-1)`<byte>`): The bytes of a `.gltf` (JSON) or `.glb` file; the kind is detected from the first bytes.
- `options` ([`GltfLoadOptions`](GltfLoadOptions.md)?): Options, or null for the defaults.
- `resources` ([`IGltfResources`](IGltfResources.md)?): Resolves external buffers and images; null refuses every external uri.

**Returns:** [`GltfModel`](GltfModel.md) — The model.

**Exceptions**

- [`GltfException`](GltfException.md): The file is damaged, over a limit or uses something unsupported.

### <a id="m-loadfile-string-gltfloadoptions"></a>LoadFile(string, GltfLoadOptions?)

```csharp
public static GltfModel LoadFile(string path, GltfLoadOptions? options = null);
```

Imports a file from disk; external buffers and images resolve inside the file's folder.

**Parameters**

- `path` (`string`): The path of the `.gltf` or `.glb` file.
- `options` ([`GltfLoadOptions`](GltfLoadOptions.md)?): Options, or null for the defaults.

**Returns:** [`GltfModel`](GltfModel.md) — The model.

**Exceptions**

- [`GltfException`](GltfException.md): The file is damaged, over a limit, missing or uses something unsupported.
