[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# GltfImage class

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)

```csharp
public sealed class GltfImage
```

An image of the model.

## Constructors

| Member | Description |
| --- | --- |
| [`GltfImage()`](#m-ctor) |  |

### <a id="m-ctor"></a>GltfImage()

```csharp
public GltfImage();
```

## Properties

| Member | Description |
| --- | --- |
| [`Format`](#m-format) | The format found in the bytes: `png`, `jpeg` or `unknown`. |
| [`Height`](#m-height) | Height in pixels (0 when unknown). |
| [`MimeType`](#m-mimetype) | The mime type as written in the file, or empty. |
| [`Name`](#m-name) | The image's name, or empty. |
| [`Pixels`](#m-pixels) | Straight-alpha RGBA8 pixels, top row first, or null. |
| [`Status`](#m-status) | What happened to the image. |
| [`UsedAsColor`](#m-usedascolor) | True when a material uses the image as colour (sRGB): base colour, emissive, sheen or specular colour. |
| [`UsedAsData`](#m-usedasdata) | True when a material uses the image as data (linear): normals, occlusion, metal/rough, clearcoat and the like. |
| [`Width`](#m-width) | Width in pixels (0 when unknown). |

### <a id="m-format"></a>Format

```csharp
public string Format { get; }
```

The format found in the bytes: `png`, `jpeg` or `unknown`.

**Type:** `string`

### <a id="m-height"></a>Height

```csharp
public int Height { get; }
```

Height in pixels (0 when unknown).

**Type:** `int`

### <a id="m-mimetype"></a>MimeType

```csharp
public string MimeType { get; }
```

The mime type as written in the file, or empty.

**Type:** `string`

### <a id="m-name"></a>Name

```csharp
public string Name { get; }
```

The image's name, or empty.

**Type:** `string`

### <a id="m-pixels"></a>Pixels

```csharp
public byte[]? Pixels { get; }
```

Straight-alpha RGBA8 pixels, top row first, or null.

**Type:** `byte[]?`

### <a id="m-status"></a>Status

```csharp
public ImageStatus Status { get; }
```

What happened to the image.

**Type:** [`ImageStatus`](ImageStatus.md)

### <a id="m-usedascolor"></a>UsedAsColor

```csharp
public bool UsedAsColor { get; }
```

True when a material uses the image as colour (sRGB): base colour, emissive, sheen or specular colour.

**Type:** `bool`

### <a id="m-usedasdata"></a>UsedAsData

```csharp
public bool UsedAsData { get; }
```

True when a material uses the image as data (linear): normals, occlusion, metal/rough, clearcoat and the like.

**Type:** `bool`

### <a id="m-width"></a>Width

```csharp
public int Width { get; }
```

Width in pixels (0 when unknown).

**Type:** `int`
