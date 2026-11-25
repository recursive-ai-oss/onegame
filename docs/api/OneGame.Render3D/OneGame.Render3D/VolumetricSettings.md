[API reference](../../index.md) › [OneGame.Render3D](../index.md) › OneGame.Render3D

# VolumetricSettings class

**Namespace:** `OneGame.Render3D`  
**Package:** [`RecursiveAI.OneGame.Render3D`](../index.md)

```csharp
public sealed class VolumetricSettings
```

Light shafts (also called god rays or volumetric light): lights scattering through the air, so a sun through a gap in a wall or a flashlight's cone shows as a beam, and objects that block the light leave dark shafts in it. Off by default; with [`VolumetricSettings.Enabled`](VolumetricSettings.md#m-enabled) false (or no light opting in through [`Light3D.Volumetric`](Light3D.md#m-volumetric)) the frame has no extra pass and is identical to one drawn without the feature.

Each opted-in light is integrated along every view ray with single scattering, at a fraction of the frame size and then upsampled edge-aware, using the light's shadow maps (a light without [`Light3D.CastShadows`](Light3D.md#m-castshadows) fills the air evenly). The cost is bounded by the quality level ([`QualitySettings.VolumetricSteps`](QualitySettings.md#m-volumetricsteps), [`QualitySettings.VolumetricScale`](QualitySettings.md#m-volumetricscale)): off at Low. At most [`Renderer3D.MaxVolumetricLights`](Renderer3D.md#m-maxvolumetriclights) lights are used. Shafts are added to the lit scene before bloom and tone mapping, over surfaces, the backdrop and blended surfaces alike; the planar reflection does not show them.

## Constructors

| Member | Description |
| --- | --- |
| [`VolumetricSettings()`](#m-ctor) |  |

### <a id="m-ctor"></a>VolumetricSettings()

```csharp
public VolumetricSettings();
```

## Properties

| Member | Description |
| --- | --- |
| [`Anisotropy`](#m-anisotropy) | Henyey-Greenstein anisotropy g, -0.95 to 0.95 (default 0.5): positive values scatter toward the direction the light travels, so shafts are brightest when you look toward the light; 0 scatters evenly; negative values favour scattering back toward the light's source. |
| [`Density`](#m-density) | How much the air scatters, per world unit along the ray (default 0.02). |
| [`Enabled`](#m-enabled) | Whether shafts are drawn (they still need a level that allows them and at least one opted-in light). |
| [`Intensity`](#m-intensity) | A multiplier on the scattered light (default 1), in the light's own units: a sun of 3 lux and an intensity of 1 gives faint shafts, 5 to 20 bold ones. |
| [`LinkToFog`](#m-linktofog) | When true and [`Scene3D.Fog`](Scene3D.md#m-fog) is on, the shafts fade with the fog's amount at each point of the ray, so a surface lost in fog does not sit behind bright shafts and the two read as one medium. |
| [`MaxDistance`](#m-maxdistance) | Longest stretch of a view ray, from the camera, that is marched (default 60). |

### <a id="m-anisotropy"></a>Anisotropy

```csharp
public float Anisotropy { get; set; }
```

Henyey-Greenstein anisotropy g, -0.95 to 0.95 (default 0.5): positive values scatter toward the direction the light travels, so shafts are brightest when you look toward the light; 0 scatters evenly; negative values favour scattering back toward the light's source.

**Type:** `float`

### <a id="m-density"></a>Density

```csharp
public float Density { get; set; }
```

How much the air scatters, per world unit along the ray (default 0.02). It sets how bright the shafts are and, since the same amount also dims light on its way to the camera, how far they reach: the visible depth is about 1 / Density. Negative and non-finite values are 0.

**Type:** `float`

### <a id="m-enabled"></a>Enabled

```csharp
public bool Enabled { get; set; }
```

Whether shafts are drawn (they still need a level that allows them and at least one opted-in light).

**Type:** `bool`

### <a id="m-intensity"></a>Intensity

```csharp
public float Intensity { get; set; }
```

A multiplier on the scattered light (default 1), in the light's own units: a sun of 3 lux and an intensity of 1 gives faint shafts, 5 to 20 bold ones.

**Type:** `float`

### <a id="m-linktofog"></a>LinkToFog

```csharp
public bool LinkToFog { get; set; }
```

When true and [`Scene3D.Fog`](Scene3D.md#m-fog) is on, the shafts fade with the fog's amount at each point of the ray, so a surface lost in fog does not sit behind bright shafts and the two read as one medium. The fog's own colour is not added here: it is already part of the surfaces. With fog off this has no effect. Fog modes added later follow automatically, since the shader asks the fog for its amount.

**Type:** `bool`

### <a id="m-maxdistance"></a>MaxDistance

```csharp
public float MaxDistance { get; set; }
```

Longest stretch of a view ray, from the camera, that is marched (default 60). Rays that hit a surface stop there; rays into the sky stop here. Keep it at or below [`ShadowSettings.MaxDistance`](ShadowSettings.md#m-maxdistance): a directional light is unshadowed beyond its last cascade, so the air past it would glow evenly. Larger values spread the fixed number of steps over more distance.

**Type:** `float`
