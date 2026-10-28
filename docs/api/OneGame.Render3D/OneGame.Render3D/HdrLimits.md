[API reference](../../index.md) › [OneGame.Render3D](../index.md) › OneGame.Render3D

# HdrLimits class

**Namespace:** `OneGame.Render3D`  
**Package:** [`RecursiveAI.OneGame.Render3D`](../index.md)

```csharp
public sealed class HdrLimits
```

Limits applied before a Radiance file allocates anything.

## Constructors

| Member | Description |
| --- | --- |
| [`HdrLimits()`](#m-ctor) |  |

### <a id="m-ctor"></a>HdrLimits()

```csharp
public HdrLimits();
```

## Properties

| Member | Description |
| --- | --- |
| [`MaxDimension`](#m-maxdimension) | Largest width or height in pixels. |
| [`MaxHeaderLine`](#m-maxheaderline) | Longest header line, in bytes. |
| [`MaxHeaderLines`](#m-maxheaderlines) | Most header lines. |
| [`MaxPixels`](#m-maxpixels) | Most pixels in the image. |

### <a id="m-maxdimension"></a>MaxDimension

```csharp
public int MaxDimension { get; init; }
```

Largest width or height in pixels.

**Type:** `int`

### <a id="m-maxheaderline"></a>MaxHeaderLine

```csharp
public int MaxHeaderLine { get; init; }
```

Longest header line, in bytes.

**Type:** `int`

### <a id="m-maxheaderlines"></a>MaxHeaderLines

```csharp
public int MaxHeaderLines { get; init; }
```

Most header lines.

**Type:** `int`

### <a id="m-maxpixels"></a>MaxPixels

```csharp
public long MaxPixels { get; init; }
```

Most pixels in the image.

**Type:** `long`
