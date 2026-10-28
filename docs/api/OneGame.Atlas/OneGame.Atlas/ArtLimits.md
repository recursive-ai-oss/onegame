[API reference](../../index.md) › [OneGame.Atlas](../index.md) › OneGame.Atlas

# ArtLimits class

**Namespace:** `OneGame.Atlas`  
**Package:** [`RecursiveAI.OneGame.Atlas`](../index.md)

```csharp
public sealed class ArtLimits
```

Budgets that bound what a hostile SVG or image can make the atlas do.

## Constructors

| Member | Description |
| --- | --- |
| [`ArtLimits()`](#m-ctor) |  |

### <a id="m-ctor"></a>ArtLimits()

```csharp
public ArtLimits();
```

## Properties

| Member | Description |
| --- | --- |
| [`MaxImageBytes`](#m-maximagebytes) | Gets the largest raster image file in bytes. |
| [`MaxRasterDimension`](#m-maxrasterdimension) | Gets the largest width or height of one raster in pixels, and so the largest natural size of a raster image. |
| [`MaxRasterPixels`](#m-maxrasterpixels) | Gets the largest pixel count of one raster. |
| [`MaxResources`](#m-maxresources) | Gets how many resources may be registered. |
| [`MaxStandaloneBytes`](#m-maxstandalonebytes) | Gets the most texel memory in bytes that standalone textures may hold at once. |
| [`MaxSvgBytes`](#m-maxsvgbytes) | Gets the largest SVG file in bytes. |
| [`MaxSvgNaturalDimension`](#m-maxsvgnaturaldimension) | Gets the natural size limit of a registered SVG per side. |
| [`StandaloneThreshold`](#m-standalonethreshold) | Gets the width or height above which a raster gets a texture of its own instead of a place on a shared page. |

### <a id="m-maximagebytes"></a>MaxImageBytes

```csharp
public int MaxImageBytes { get; init; }
```

Gets the largest raster image file in bytes. Default 32 MiB.

**Type:** `int`

### <a id="m-maxrasterdimension"></a>MaxRasterDimension

```csharp
public int MaxRasterDimension { get; init; }
```

Gets the largest width or height of one raster in pixels, and so the largest natural size of a raster image. Default 8192.

**Type:** `int`

### <a id="m-maxrasterpixels"></a>MaxRasterPixels

```csharp
public long MaxRasterPixels { get; init; }
```

Gets the largest pixel count of one raster. Default 16,777,216 (4096 × 4096, 64 MiB of texels).

**Type:** `long`

### <a id="m-maxresources"></a>MaxResources

```csharp
public int MaxResources { get; init; }
```

Gets how many resources may be registered. Default 4096.

**Type:** `int`

### <a id="m-maxstandalonebytes"></a>MaxStandaloneBytes

```csharp
public long MaxStandaloneBytes { get; init; }
```

Gets the most texel memory in bytes that standalone textures may hold at once. When a new one would not fit, the least recently used ones that were not drawn this frame are released first; if the frame itself needs more, the lookup returns [`AtlasStatus.Full`](AtlasStatus.md#m-full). Default 256 MiB.

**Type:** `long`

### <a id="m-maxsvgbytes"></a>MaxSvgBytes

```csharp
public int MaxSvgBytes { get; init; }
```

Gets the largest SVG file in bytes. Default 4 MiB.

**Type:** `int`

### <a id="m-maxsvgnaturaldimension"></a>MaxSvgNaturalDimension

```csharp
public int MaxSvgNaturalDimension { get; init; }
```

Gets the natural size limit of a registered SVG per side. The natural size only seeds the scale; the raster limit applies when drawing. Default 16384.

**Type:** `int`

### <a id="m-standalonethreshold"></a>StandaloneThreshold

```csharp
public int StandaloneThreshold { get; init; }
```

Gets the width or height above which a raster gets a texture of its own instead of a place on a shared page. Default 512; never more than a page can hold.

**Type:** `int`
