[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# Assets class

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)  
**Implements:** [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class Assets : IDisposable
```

The asset service: loads files from the game's content folders into the types the engine draws, plays and reads, caches them by path and, in development, swaps an edited file in under the existing handle. Everything here is built on the low-level loaders (OneImage, OneYaml, OneAudio, [`GltfLoader`](../../OneGame.Gltf/OneGame.Gltf/GltfLoader.md), [`FontLibrary`](../../OneGame.Text/OneGame.Text/FontLibrary.md)), which stay usable directly.

A [`GameInstance`](GameInstance.md) owns one ([`GameInstance.Assets`](GameInstance.md#m-assets)); its 2D stage attaches the renderer when it makes one. Files are untrusted: each kind has a size limit checked before the file is read into memory, and decoders run with the same limits the art atlas uses. Reload logic lives here; what watches the files lives in the development control layer, so exports contain no watcher.

## Constructors

| Member | Description |
| --- | --- |
| [`Assets(IAssetSource?)`](#m-ctor-iassetsource) | Creates a service over `source` (files from the default roots when null) and makes it [`Assets.Active`](Assets.md#m-active). |

### <a id="m-ctor-iassetsource"></a>Assets(IAssetSource?)

```csharp
public Assets(IAssetSource? source = null);
```

Creates a service over `source` (files from the default roots when null) and makes it [`Assets.Active`](Assets.md#m-active).

**Parameters**

- `source` ([`IAssetSource`](IAssetSource.md)?): Where content comes from.

## Properties

| Member | Description |
| --- | --- |
| [`Active`](#m-active) | The most recently created service, so tools can find a game's assets without being handed them. |
| [`DataDigest`](#m-datadigest) | A digest over every data asset (path and file digest, in path order): equal when the game runs with the same data files. |
| [`Events`](#m-events) | The last 256 events, oldest first. |
| [`Items`](#m-items) | The loaded assets, in no particular order. |
| [`LastSequence`](#m-lastsequence) | The newest event's sequence number, or 0. |
| [`MaxDataBytes`](#m-maxdatabytes) | Largest YAML file read, in bytes. |
| [`MaxImageBytes`](#m-maximagebytes) | Largest raster or SVG file read, in bytes. |
| [`MaxSoundBytes`](#m-maxsoundbytes) | Largest audio file read, in bytes. |
| [`Renderer`](#m-renderer) | The 2D renderer textures are uploaded with, or null until a stage attached one. |
| [`Source`](#m-source) | Where content is read from. |

### <a id="m-active"></a>Active

```csharp
public static Assets? Active { get; }
```

The most recently created service, so tools can find a game's assets without being handed them.

**Type:** [`Assets`](Assets.md)?

### <a id="m-datadigest"></a>DataDigest

```csharp
public string DataDigest { get; }
```

A digest over every data asset (path and file digest, in path order): equal when the game runs with the same data files.

**Type:** `string`

### <a id="m-events"></a>Events

```csharp
public IReadOnlyList<AssetEvent> Events { get; }
```

The last 256 events, oldest first.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`AssetEvent`](AssetEvent.md)&gt;

### <a id="m-items"></a>Items

```csharp
public IEnumerable<Asset> Items { get; }
```

The loaded assets, in no particular order.

**Type:** [`IEnumerable`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable-1)&lt;[`Asset`](Asset.md)&gt;

### <a id="m-lastsequence"></a>LastSequence

```csharp
public long LastSequence { get; }
```

The newest event's sequence number, or 0.

**Type:** `long`

### <a id="m-maxdatabytes"></a>MaxDataBytes

```csharp
public long MaxDataBytes { get; set; }
```

Largest YAML file read, in bytes. Default 8 MiB.

**Type:** `long`

### <a id="m-maximagebytes"></a>MaxImageBytes

```csharp
public long MaxImageBytes { get; set; }
```

Largest raster or SVG file read, in bytes. Default 32 MiB.

**Type:** `long`

### <a id="m-maxsoundbytes"></a>MaxSoundBytes

```csharp
public long MaxSoundBytes { get; set; }
```

Largest audio file read, in bytes. Default 64 MiB (the audio service also limits clip length).

**Type:** `long`

### <a id="m-renderer"></a>Renderer

```csharp
public Renderer2D? Renderer { get; }
```

The 2D renderer textures are uploaded with, or null until a stage attached one.

**Type:** [`Renderer2D`](../../OneGame.Render2D/OneGame.Render2D/Renderer2D.md)?

### <a id="m-source"></a>Source

```csharp
public IAssetSource Source { get; }
```

Where content is read from.

**Type:** [`IAssetSource`](IAssetSource.md)

## Events

| Member | Description |
| --- | --- |
| [`EventRaised`](#m-eventraised) | Raised for every [`AssetEvent`](AssetEvent.md). |

### <a id="m-eventraised"></a>EventRaised

```csharp
public event Action<AssetEvent>? EventRaised;
```

Raised for every [`AssetEvent`](AssetEvent.md).

**Type:** [`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)&lt;[`AssetEvent`](AssetEvent.md)&gt;?

## Methods

| Member | Description |
| --- | --- |
| [`ApplyPendingData()`](#m-applypendingdata) | Swaps in the data files that changed on disk and were staged by [`Assets.Reload()`](Assets.md#m-reload-asset). |
| [`AttachRenderer(Renderer2D)`](#m-attachrenderer-renderer2d) | Gives textures a renderer to upload with. |
| [`Dispose()`](#m-dispose) | Frees every asset. |
| [`LoadData<T>(string, YamlTypeInfo<T>, bool)`](#m-loaddata-t-string-yamltypeinfo-t-bool) | Reads a YAML file into one of the game's own types through its source-generated metadata (`[YamlSerializable]` context). |
| [`LoadFont(string, FontLibrary)`](#m-loadfont-string-fontlibrary) | Adds a font file to a library. |
| [`LoadModel(string, bool)`](#m-loadmodel-string-bool) | Imports a glTF model (`.gltf` with its buffers and images in the same folder, or `.glb`). |
| [`LoadSound(string, bool)`](#m-loadsound-string-bool) | Decodes an audio clip with the game's audio service ([`AudioService.Active`](../../OneGame.Audio/OneGame.Audio/AudioService.md#m-active)). |
| [`LoadSvg(string, int, int, bool)`](#m-loadsvg-string-int-int-bool) | Loads an SVG drawn at the given pixel size as a texture, so it is crisp at that size. |
| [`LoadTexture(string, TextureFilter, bool)`](#m-loadtexture-string-texturefilter-bool) | Loads a raster image (PNG, JPEG, WebP and the other formats OneImage reads) as a texture. |
| [`Release(Asset)`](#m-release-asset) | Gives one reference back; at zero the asset is freed and forgotten. |
| [`Reload(Asset)`](#m-reload-asset) | Reads an asset's file again and swaps the result in under the same handle. |
| [`Update()`](#m-update) | Finishes background loads that completed. |

### <a id="m-applypendingdata"></a>ApplyPendingData()

```csharp
public int ApplyPendingData();
```

Swaps in the data files that changed on disk and were staged by [`Assets.Reload()`](Assets.md#m-reload-asset). Call it at a boundary: before a new game or a replay starts, never while a recording is running, because a recording made with other data would not replay.

**Returns:** `int` — How many data assets changed.

### <a id="m-attachrenderer-renderer2d"></a>AttachRenderer(Renderer2D)

```csharp
public void AttachRenderer(Renderer2D renderer);
```

Gives textures a renderer to upload with. Textures loaded earlier are uploaded now; when the renderer is a different one, they are rebuilt on it.

**Parameters**

- `renderer` ([`Renderer2D`](../../OneGame.Render2D/OneGame.Render2D/Renderer2D.md)): The renderer.

### <a id="m-dispose"></a>Dispose()

```csharp
public void Dispose();
```

Frees every asset. The service is unusable afterwards.

### <a id="m-loaddata-t-string-yamltypeinfo-t-bool"></a>LoadData&lt;T&gt;(string, YamlTypeInfo&lt;T&gt;, bool)

```csharp
public Asset<T> LoadData<T>(string path, YamlTypeInfo<T> typeInfo, bool background = false) where T : class;
```

Reads a YAML file into one of the game's own types through its source-generated metadata (`[YamlSerializable]` context). Create the context with `UnmappedMemberHandling = Disallow` to reject unknown keys. A reload of a data file is read and checked when the file changes but only swapped in by [`Assets.ApplyPendingData()`](Assets.md#m-applypendingdata), because sim code reads data.

**Type parameters**

- `T`: The data type.

**Parameters**

- `path` (`string`): Path under a content folder, like `data/ships.yaml`.
- `typeInfo` (`YamlTypeInfo<T>`): Generated metadata, like `GameYaml.Default.Ships`.
- `background` (`bool`): Read and parse on another thread and return at once.

**Returns:** [`Asset`](Asset-1.md)`<T>` — The shared handle; [`Asset<T>.Value`](Asset-1.md#m-value) is the data.

**Exceptions**

- [`AssetException`](AssetException.md): The file is missing, too large or does not match the type.

### <a id="m-loadfont-string-fontlibrary"></a>LoadFont(string, FontLibrary)

```csharp
public Asset<LoadedFont> LoadFont(string path, FontLibrary library);
```

Adds a font file to a library. Fonts are loaded once and not hot reloaded.

**Parameters**

- `path` (`string`): Path under a content folder, like `fonts/Body.ttf`.
- `library` ([`FontLibrary`](../../OneGame.Text/OneGame.Text/FontLibrary.md)): The library to add the face to.

**Returns:** [`Asset`](Asset-1.md)&lt;[`LoadedFont`](LoadedFont.md)&gt; — The shared handle; [`Asset<T>.Value`](Asset-1.md#m-value) names the face.

**Exceptions**

- [`AssetException`](AssetException.md): The file is missing, too large or not a usable font.

### <a id="m-loadmodel-string-bool"></a>LoadModel(string, bool)

```csharp
public Asset<GltfModel> LoadModel(string path, bool background = false);
```

Imports a glTF model (`.gltf` with its buffers and images in the same folder, or `.glb`). Only the model file itself is watched for edits.

**Parameters**

- `path` (`string`): Path under a content folder, like `art/ship.glb`.
- `background` (`bool`): Import on another thread and return at once.

**Returns:** [`Asset`](Asset-1.md)&lt;[`GltfModel`](../../OneGame.Gltf/OneGame.Gltf/GltfModel.md)&gt; — The shared handle.

**Exceptions**

- [`AssetException`](AssetException.md): The file is missing, too large, damaged or uses something unsupported.

### <a id="m-loadsound-string-bool"></a>LoadSound(string, bool)

```csharp
public Asset<Sound> LoadSound(string path, bool background = false);
```

Decodes an audio clip with the game's audio service ([`AudioService.Active`](../../OneGame.Audio/OneGame.Audio/AudioService.md#m-active)). A reload replaces [`Asset<T>.Value`](Asset-1.md#m-value); voices already playing keep the old clip.

**Parameters**

- `path` (`string`): Path under a content folder, like `data/hit.ogg`.
- `background` (`bool`): Read the file on another thread; decoding finishes on the caller's thread.

**Returns:** [`Asset`](Asset-1.md)&lt;[`Sound`](../../OneGame.Audio/OneGame.Audio/Sound.md)&gt; — The shared handle.

**Exceptions**

- [`AssetException`](AssetException.md): The file is missing, too large, damaged, or no audio service exists.

### <a id="m-loadsvg-string-int-int-bool"></a>LoadSvg(string, int, int, bool)

```csharp
public Asset<Texture2D> LoadSvg(string path, int width, int height, bool background = false);
```

Loads an SVG drawn at the given pixel size as a texture, so it is crisp at that size. The same file at another size is another asset.

**Parameters**

- `path` (`string`): Path under a content folder, like `art/icon.svg`.
- `width` (`int`): Width in pixels.
- `height` (`int`): Height in pixels.
- `background` (`bool`): Decode on another thread and return at once.

**Returns:** [`Asset`](Asset-1.md)&lt;[`Texture2D`](../../OneGame.Render2D/OneGame.Render2D/Texture2D.md)&gt; — The shared handle.

**Exceptions**

- [`AssetException`](AssetException.md): The file is missing, too large or damaged.

### <a id="m-loadtexture-string-texturefilter-bool"></a>LoadTexture(string, TextureFilter, bool)

```csharp
public Asset<Texture2D> LoadTexture(string path, TextureFilter filter = 1, bool background = false);
```

Loads a raster image (PNG, JPEG, WebP and the other formats OneImage reads) as a texture. Edits to the file show from the next frame in development.

**Parameters**

- `path` (`string`): Path under a content folder, like `art/ship.png`.
- `filter` ([`TextureFilter`](../../OneGame.Render2D/OneGame.Render2D/TextureFilter.md)): How the texture is filtered when scaled; [`TextureFilter.Nearest`](../../OneGame.Render2D/OneGame.Render2D/TextureFilter.md#m-nearest) for pixel art.
- `background` (`bool`): Read and decode on another thread and return at once; see [`Asset.IsReady`](Asset.md#m-isready).

**Returns:** [`Asset`](Asset-1.md)&lt;[`Texture2D`](../../OneGame.Render2D/OneGame.Render2D/Texture2D.md)&gt; — The shared handle; [`Asset<T>.Value`](Asset-1.md#m-value) is the texture.

**Exceptions**

- [`AssetException`](AssetException.md): The file is missing, too large or damaged (not for a background load, which reports through the handle).

### <a id="m-release-asset"></a>Release(Asset)

```csharp
public void Release(Asset asset);
```

Gives one reference back; at zero the asset is freed and forgotten. Same as [`Asset.Release()`](Asset.md#m-release).

**Parameters**

- `asset` ([`Asset`](Asset.md)): The handle.

### <a id="m-reload-asset"></a>Reload(Asset)

```csharp
public bool Reload(Asset asset);
```

Reads an asset's file again and swaps the result in under the same handle. A file that cannot be read or decoded leaves the old content in use and is reported as a `failed` event with [`Asset.Error`](Asset.md#m-error) set. Data assets are staged, not swapped in: see [`Assets.ApplyPendingData()`](Assets.md#m-applypendingdata). Development tools call this when the file changed; nothing in a running game does.

**Parameters**

- `asset` ([`Asset`](Asset.md)): The asset.

**Returns:** `bool` — True when new content was swapped in or staged.

### <a id="m-update"></a>Update()

```csharp
public int Update();
```

Finishes background loads that completed. The host calls this every frame; call it yourself when you load without a host.

**Returns:** `int` — How many assets became ready or failed.
