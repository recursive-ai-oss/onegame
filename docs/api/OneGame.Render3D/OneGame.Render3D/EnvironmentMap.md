[API reference](../../index.md) › [OneGame.Render3D](../index.md) › OneGame.Render3D

# EnvironmentMap class

**Namespace:** `OneGame.Render3D`  
**Package:** [`RecursiveAI.OneGame.Render3D`](../index.md)  
**Implements:** [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class EnvironmentMap : IDisposable
```

An environment prepared for lighting: a GGX-prefiltered specular cube (roughness grows with the mip level, mip 0 is the sharp environment) and a cosine-convolved diffuse cube. Create one with [`Renderer3D.CreateEnvironment()`](Renderer3D.md#m-createenvironment-hdrimage-int); it is filtered on the GPU, on the render graph, the first time it is used.

## Properties

| Member | Description |
| --- | --- |
| [`FollowsQuality`](#m-followsquality) | True when the size was left to the renderer: the cube is rebuilt at [`QualitySettings.EnvironmentSize`](QualitySettings.md#m-environmentsize) whenever the scene's quality level changes it. |
| [`Irradiance`](#m-irradiance) | The diffuse cube: mean radiance over the cosine-weighted hemisphere, that is irradiance divided by pi (`Rgba16Float`). |
| [`IsBuilt`](#m-isbuilt) | True once the filtering passes have been recorded for submission. |
| [`Size`](#m-size) | Face size in texels of the specular cube's top mip. |
| [`Specular`](#m-specular) | The prefiltered specular cube (`Rgba16Float`, six faces, full mip chain). |
| [`SpecularMipCount`](#m-specularmipcount) | Number of mip levels of the specular cube (roughness = mip / (count - 1)). |

### <a id="m-followsquality"></a>FollowsQuality

```csharp
public bool FollowsQuality { get; }
```

True when the size was left to the renderer: the cube is rebuilt at [`QualitySettings.EnvironmentSize`](QualitySettings.md#m-environmentsize) whenever the scene's quality level changes it.

**Type:** `bool`

### <a id="m-irradiance"></a>Irradiance

```csharp
public GpuTexture Irradiance { get; }
```

The diffuse cube: mean radiance over the cosine-weighted hemisphere, that is irradiance divided by pi (`Rgba16Float`).

**Type:** [`GpuTexture`](../../OneGame.Gpu/OneGame.Gpu/GpuTexture.md)

### <a id="m-isbuilt"></a>IsBuilt

```csharp
public bool IsBuilt { get; }
```

True once the filtering passes have been recorded for submission.

**Type:** `bool`

### <a id="m-size"></a>Size

```csharp
public int Size { get; }
```

Face size in texels of the specular cube's top mip.

**Type:** `int`

### <a id="m-specular"></a>Specular

```csharp
public GpuTexture Specular { get; }
```

The prefiltered specular cube (`Rgba16Float`, six faces, full mip chain).

**Type:** [`GpuTexture`](../../OneGame.Gpu/OneGame.Gpu/GpuTexture.md)

### <a id="m-specularmipcount"></a>SpecularMipCount

```csharp
public int SpecularMipCount { get; }
```

Number of mip levels of the specular cube (roughness = mip / (count - 1)).

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`Dispose()`](#m-dispose) |  |

### <a id="m-dispose"></a>Dispose()

```csharp
public void Dispose();
```
