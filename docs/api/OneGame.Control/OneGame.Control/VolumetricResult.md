[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# VolumetricResult record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`VolumetricResult`](VolumetricResult.md)&gt;

```csharp
public sealed record VolumetricResult : IEquatable<VolumetricResult>
```

Result of `volumetric`: the scene's light shaft settings and what the quality level and the last frame did with them.

## Constructors

| Member | Description |
| --- | --- |
| [`VolumetricResult(bool, double, double, double, double, bool, bool, int, double, bool, VolumetricLightInfo[])`](#m-ctor-bool-double-double-double-double-bool-bool-int-double-bool-volumetriclightinfo) | Result of `volumetric`: the scene's light shaft settings and what the quality level and the last frame did with them. |

### <a id="m-ctor-bool-double-double-double-double-bool-bool-int-double-bool-volumetriclightinfo"></a>VolumetricResult(bool, double, double, double, double, bool, bool, int, double, bool, VolumetricLightInfo\[\])

```csharp
public VolumetricResult(bool Enabled, double Density, double Intensity, double Anisotropy, double MaxDistance, bool LinkToFog, bool Allowed, int Steps, double Scale, bool Active, VolumetricLightInfo[] Lights);
```

Result of `volumetric`: the scene's light shaft settings and what the quality level and the last frame did with them.

**Parameters**

- `Enabled` (`bool`): Whether the scene asks for light shafts.
- `Density` (`double`): Scattering per world unit.
- `Intensity` (`double`): Scene-wide multiplier on the scattered light.
- `Anisotropy` (`double`): Henyey-Greenstein g, -0.95 to 0.95.
- `MaxDistance` (`double`): Longest marched stretch of a view ray.
- `LinkToFog` (`bool`): Whether shafts fade with the scene's fog.
- `Allowed` (`bool`): Whether the current quality level allows shafts (Low does not).
- `Steps` (`int`): Samples per ray at the current level.
- `Scale` (`double`): Size of the marched image as a share of the frame at the current level.
- `Active` (`bool`): Whether the last frame ran the shaft passes.
- `Lights` ([`VolumetricLightInfo`](VolumetricLightInfo.md)\[\]): Every light of the scene.

## Properties

| Member | Description |
| --- | --- |
| [`Active`](#m-active) | Whether the last frame ran the shaft passes. |
| [`Allowed`](#m-allowed) | Whether the current quality level allows shafts (Low does not). |
| [`Anisotropy`](#m-anisotropy) | Henyey-Greenstein g, -0.95 to 0.95. |
| [`Density`](#m-density) | Scattering per world unit. |
| [`Enabled`](#m-enabled) | Whether the scene asks for light shafts. |
| [`Intensity`](#m-intensity) | Scene-wide multiplier on the scattered light. |
| [`Lights`](#m-lights) | Every light of the scene. |
| [`LinkToFog`](#m-linktofog) | Whether shafts fade with the scene's fog. |
| [`MaxDistance`](#m-maxdistance) | Longest marched stretch of a view ray. |
| [`Scale`](#m-scale) | Size of the marched image as a share of the frame at the current level. |
| [`Steps`](#m-steps) | Samples per ray at the current level. |

### <a id="m-active"></a>Active

```csharp
public bool Active { get; init; }
```

Whether the last frame ran the shaft passes.

**Type:** `bool`

### <a id="m-allowed"></a>Allowed

```csharp
public bool Allowed { get; init; }
```

Whether the current quality level allows shafts (Low does not).

**Type:** `bool`

### <a id="m-anisotropy"></a>Anisotropy

```csharp
public double Anisotropy { get; init; }
```

Henyey-Greenstein g, -0.95 to 0.95.

**Type:** `double`

### <a id="m-density"></a>Density

```csharp
public double Density { get; init; }
```

Scattering per world unit.

**Type:** `double`

### <a id="m-enabled"></a>Enabled

```csharp
public bool Enabled { get; init; }
```

Whether the scene asks for light shafts.

**Type:** `bool`

### <a id="m-intensity"></a>Intensity

```csharp
public double Intensity { get; init; }
```

Scene-wide multiplier on the scattered light.

**Type:** `double`

### <a id="m-lights"></a>Lights

```csharp
public VolumetricLightInfo[] Lights { get; init; }
```

Every light of the scene.

**Type:** [`VolumetricLightInfo`](VolumetricLightInfo.md)\[\]

### <a id="m-linktofog"></a>LinkToFog

```csharp
public bool LinkToFog { get; init; }
```

Whether shafts fade with the scene's fog.

**Type:** `bool`

### <a id="m-maxdistance"></a>MaxDistance

```csharp
public double MaxDistance { get; init; }
```

Longest marched stretch of a view ray.

**Type:** `double`

### <a id="m-scale"></a>Scale

```csharp
public double Scale { get; init; }
```

Size of the marched image as a share of the frame at the current level.

**Type:** `double`

### <a id="m-steps"></a>Steps

```csharp
public int Steps { get; init; }
```

Samples per ray at the current level.

**Type:** `int`
