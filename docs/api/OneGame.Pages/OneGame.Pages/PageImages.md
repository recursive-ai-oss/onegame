[API reference](../../index.md) › [OneGame.Pages](../index.md) › OneGame.Pages

# PageImages class

**Namespace:** `OneGame.Pages`  
**Package:** [`RecursiveAI.OneGame.Pages`](../index.md)  
**Implements:** `IImageSource`

```csharp
public sealed class PageImages : IImageSource
```

The images a page can show. Pages name images by address (`<img src="coin.png">`, `background-image: url(coin.png)`); the game registers the bytes under that address and the images live in the game's [`ArtAtlas`](../../OneGame.Atlas/OneGame.Atlas/ArtAtlas.md) next to the UI's art. OneRender never sees pixels, only the handle and size this class hands back.

## Constructors

| Member | Description |
| --- | --- |
| [`PageImages(ArtAtlas)`](#m-ctor-artatlas) | Creates an empty set over the game's art atlas. |

### <a id="m-ctor-artatlas"></a>PageImages(ArtAtlas)

```csharp
public PageImages(ArtAtlas art);
```

Creates an empty set over the game's art atlas.

**Parameters**

- `art` ([`ArtAtlas`](../../OneGame.Atlas/OneGame.Atlas/ArtAtlas.md)): The atlas the images are registered in.

## Fields

| Member | Description |
| --- | --- |
| [`MaxImages`](#m-maximages) | The most images one set holds. |

### <a id="m-maximages"></a>MaxImages

```csharp
public const int MaxImages = 4096;
```

The most images one set holds.

**Type:** `int`

## Properties

| Member | Description |
| --- | --- |
| [`Count`](#m-count) | How many addresses are registered. |

### <a id="m-count"></a>Count

```csharp
public int Count { get; }
```

How many addresses are registered.

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`Add(string, ReadOnlyMemory<byte>)`](#m-add-string-readonlymemory-byte) | Registers an image or SVG file under an address. |
| [`Replace(string, ReadOnlyMemory<byte>)`](#m-replace-string-readonlymemory-byte) | Replaces the pixels behind an address. |
| [`TryGetHandle(string, out ImageHandle)`](#m-trygethandle-string-imagehandle) | The handle of an address, if it is registered. |
| [`TryGetImage(string, out ImageInfo)`](#m-trygetimage-string-imageinfo) |  |

### <a id="m-add-string-readonlymemory-byte"></a>Add(string, ReadOnlyMemory&lt;byte&gt;)

```csharp
public ImageHandle Add(string address, ReadOnlyMemory<byte> data);
```

Registers an image or SVG file under an address.

**Parameters**

- `address` (`string`): The address as the page writes it.
- `data` ([`ReadOnlyMemory`](https://learn.microsoft.com/dotnet/api/system.readonlymemory-1)`<byte>`): The file's bytes (PNG, JPEG, WebP, SVG...).

**Returns:** `ImageHandle` — The handle pages use for it.

**Exceptions**

- [`AtlasAssetException`](../../OneGame.Atlas/OneGame.Atlas/AtlasAssetException.md): The file is damaged, too large, or the set is full.

### <a id="m-replace-string-readonlymemory-byte"></a>Replace(string, ReadOnlyMemory&lt;byte&gt;)

```csharp
public ImageHandle Replace(string address, ReadOnlyMemory<byte> data);
```

Replaces the pixels behind an address. Pages that show it draw the new pixels from the next frame when the size is unchanged (call `PageSet.InvalidateImage`); a new size needs the page to lay out again.

**Parameters**

- `address` (`string`): A registered address.
- `data` ([`ReadOnlyMemory`](https://learn.microsoft.com/dotnet/api/system.readonlymemory-1)`<byte>`): The new file.

**Returns:** `ImageHandle` — The image's handle.

**Exceptions**

- [`KeyNotFoundException`](https://learn.microsoft.com/dotnet/api/system.collections.generic.keynotfoundexception): The address is not registered.
- [`AtlasAssetException`](../../OneGame.Atlas/OneGame.Atlas/AtlasAssetException.md): The file is damaged or too large.

### <a id="m-trygethandle-string-imagehandle"></a>TryGetHandle(string, out ImageHandle)

```csharp
public bool TryGetHandle(string address, out ImageHandle handle);
```

The handle of an address, if it is registered.

**Parameters**

- `address` (`string`): The address.
- `handle` (`ImageHandle`): The handle.

**Returns:** `bool` — True when registered.

### <a id="m-trygetimage-string-imageinfo"></a>TryGetImage(string, out ImageInfo)

```csharp
public bool TryGetImage(string address, out ImageInfo image);
```

Implements `IImageSource.TryGetImage()`.

**Parameters**

- `address` (`string`)
- `image` (`ImageInfo`)

**Returns:** `bool`
