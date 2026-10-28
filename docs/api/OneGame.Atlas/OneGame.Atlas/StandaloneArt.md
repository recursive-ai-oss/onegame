[API reference](../../index.md) › [OneGame.Atlas](../index.md) › OneGame.Atlas

# StandaloneArt class

**Namespace:** `OneGame.Atlas`  
**Package:** [`RecursiveAI.OneGame.Atlas`](../index.md)

```csharp
public sealed class StandaloneArt
```

A raster too big for the shared pages: it has a texture of its own, at full resolution. The renderer uploads [`StandaloneArt.Pixels`](StandaloneArt.md#m-pixels) once, calls [`StandaloneArt.MarkUploaded()`](StandaloneArt.md#m-markuploaded), and drops the texture when the atlas reports the handle as released.

## Properties

| Member | Description |
| --- | --- |
| [`Bytes`](#m-bytes) | Gets the bytes this texture takes on the GPU. |
| [`Handle`](#m-handle) | Gets the handle carried by [`ArtRegion.Standalone`](ArtRegion.md#m-standalone). |
| [`Height`](#m-height) | Gets the height in pixels. |
| [`NeedsUpload`](#m-needsupload) | Gets whether the pixels still wait for the upload. |
| [`Pixels`](#m-pixels) | Gets the premultiplied RGBA rows, tightly packed, until [`StandaloneArt.MarkUploaded()`](StandaloneArt.md#m-markuploaded); empty afterwards. |
| [`Width`](#m-width) | Gets the width in pixels. |

### <a id="m-bytes"></a>Bytes

```csharp
public long Bytes { get; }
```

Gets the bytes this texture takes on the GPU.

**Type:** `long`

### <a id="m-handle"></a>Handle

```csharp
public int Handle { get; }
```

Gets the handle carried by [`ArtRegion.Standalone`](ArtRegion.md#m-standalone).

**Type:** `int`

### <a id="m-height"></a>Height

```csharp
public int Height { get; }
```

Gets the height in pixels.

**Type:** `int`

### <a id="m-needsupload"></a>NeedsUpload

```csharp
public bool NeedsUpload { get; }
```

Gets whether the pixels still wait for the upload.

**Type:** `bool`

### <a id="m-pixels"></a>Pixels

```csharp
public ReadOnlySpan<byte> Pixels { get; }
```

Gets the premultiplied RGBA rows, tightly packed, until [`StandaloneArt.MarkUploaded()`](StandaloneArt.md#m-markuploaded); empty afterwards.

**Type:** [`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`

### <a id="m-width"></a>Width

```csharp
public int Width { get; }
```

Gets the width in pixels.

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`MarkUploaded()`](#m-markuploaded) | Drops the CPU copy once it is on the GPU, so each large raster is held in memory once. |

### <a id="m-markuploaded"></a>MarkUploaded()

```csharp
public void MarkUploaded();
```

Drops the CPU copy once it is on the GPU, so each large raster is held in memory once.
