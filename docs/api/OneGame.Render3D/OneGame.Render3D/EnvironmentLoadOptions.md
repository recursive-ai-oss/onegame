[API reference](../../index.md) › [OneGame.Render3D](../index.md) › OneGame.Render3D

# EnvironmentLoadOptions class

**Namespace:** `OneGame.Render3D`  
**Package:** [`RecursiveAI.OneGame.Render3D`](../index.md)

```csharp
public sealed class EnvironmentLoadOptions
```

Tuning for [`Renderer3D.CreateEnvironmentAsync()`](Renderer3D.md#m-createenvironmentasync-hdrimage-environmentloadoptions). The defaults suit a game that holds 60 frames per second.

## Constructors

| Member | Description |
| --- | --- |
| [`EnvironmentLoadOptions()`](#m-ctor) |  |

### <a id="m-ctor"></a>EnvironmentLoadOptions()

```csharp
public EnvironmentLoadOptions();
```

## Properties

| Member | Description |
| --- | --- |
| [`Background`](#m-background) | Convert on a thread-pool thread. |
| [`ConvertPixelsPerFrame`](#m-convertpixelsperframe) | Panorama pixels converted per frame when there is no worker thread (a browser). |
| [`GpuBudget`](#m-gpubudget) | The most filtering work one frame takes on, in filter samples (a texel of one face of one mip costs 512). |
| [`Size`](#m-size) | Face size of the specular cube, a power of two from 16 to 1024; 0 (the default) follows the scene's quality level. |
| [`UploadBytesPerFrame`](#m-uploadbytesperframe) | The most panorama bytes copied to the GPU in one frame. |

### <a id="m-background"></a>Background

```csharp
public bool Background { get; init; }
```

Convert on a thread-pool thread. Ignored (false) in a browser, where one thread runs everything. Default true.

**Type:** `bool`

### <a id="m-convertpixelsperframe"></a>ConvertPixelsPerFrame

```csharp
public int ConvertPixelsPerFrame { get; init; }
```

Panorama pixels converted per frame when there is no worker thread (a browser). Default 262144.

**Type:** `int`

### <a id="m-gpubudget"></a>GpuBudget

```csharp
public long GpuBudget { get; init; }
```

The most filtering work one frame takes on, in filter samples (a texel of one face of one mip costs 512). Each frame adds passes until the next would pass this, but always at least one. Lower it to spread the load over more frames. Default 40 million.

**Type:** `long`

### <a id="m-size"></a>Size

```csharp
public int Size { get; init; }
```

Face size of the specular cube, a power of two from 16 to 1024; 0 (the default) follows the scene's quality level.

**Type:** `int`

### <a id="m-uploadbytesperframe"></a>UploadBytesPerFrame

```csharp
public int UploadBytesPerFrame { get; init; }
```

The most panorama bytes copied to the GPU in one frame. Default 1 MiB.

**Type:** `int`
