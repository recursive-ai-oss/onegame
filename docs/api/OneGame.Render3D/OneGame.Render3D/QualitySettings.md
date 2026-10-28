[API reference](../../index.md) › [OneGame.Render3D](../index.md) › OneGame.Render3D

# QualitySettings struct

**Namespace:** `OneGame.Render3D`  
**Package:** [`RecursiveAI.OneGame.Render3D`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`QualitySettings`](QualitySettings.md)&gt;

```csharp
public readonly struct QualitySettings : IEquatable<QualitySettings>
```

What a [`QualityLevel`](QualityLevel.md) means in resources and work. Features still have to be asked for by the scene; a level only bounds them.

## Constructors

| Member | Description |
| --- | --- |
| [`QualitySettings(QualityLevel, int, int, int, bool, int, bool, int, bool, int)`](#m-ctor-qualitylevel-int-int-int-bool-int-bool-int-bool-int) | What a [`QualityLevel`](QualityLevel.md) means in resources and work. |

### <a id="m-ctor-qualitylevel-int-int-int-bool-int-bool-int-bool-int"></a>QualitySettings(QualityLevel, int, int, int, bool, int, bool, int, bool, int)

```csharp
public QualitySettings(QualityLevel Level, int ShadowTileSize, int MaxCascades, int ShadowFilterRadius, bool Ssao, int SsaoSamples, bool SsaoHalfResolution, int MaxSamples, bool Bloom, int EnvironmentSize);
```

What a [`QualityLevel`](QualityLevel.md) means in resources and work. Features still have to be asked for by the scene; a level only bounds them.

**Parameters**

- `Level` ([`QualityLevel`](QualityLevel.md)): The level these settings belong to.
- `ShadowTileSize` (`int`): Edge of one shadow map tile in texels (one cascade, one spot light, one face of a point light).
- `MaxCascades` (`int`): Most cascades a directional light splits into.
- `ShadowFilterRadius` (`int`): Percentage-closer filter radius in texels: 0 is one hardware-filtered tap, 1 is 3x3 taps, 2 is 5x5.
- `Ssao` (`bool`): Whether ambient occlusion may run.
- `SsaoSamples` (`int`): Occlusion samples per pixel.
- `SsaoHalfResolution` (`bool`): Whether occlusion is computed at half resolution.
- `MaxSamples` (`int`): Most multisampling samples (1, 4 or 8; 2 is not used because many formats lack it); the device may allow fewer.
- `Bloom` (`bool`): Whether bloom may run.
- `EnvironmentSize` (`int`): Face size of the specular cube of environments created without an explicit size.

## Properties

| Member | Description |
| --- | --- |
| [`Bloom`](#m-bloom) | Whether bloom may run. |
| [`EnvironmentSize`](#m-environmentsize) | Face size of the specular cube of environments created without an explicit size. |
| [`Level`](#m-level) | The level these settings belong to. |
| [`MaxCascades`](#m-maxcascades) | Most cascades a directional light splits into. |
| [`MaxSamples`](#m-maxsamples) | Most multisampling samples (1, 4 or 8; 2 is not used because many formats lack it); the device may allow fewer. |
| [`ShadowFilterRadius`](#m-shadowfilterradius) | Percentage-closer filter radius in texels: 0 is one hardware-filtered tap, 1 is 3x3 taps, 2 is 5x5. |
| [`ShadowTileSize`](#m-shadowtilesize) | Edge of one shadow map tile in texels (one cascade, one spot light, one face of a point light). |
| [`Ssao`](#m-ssao) | Whether ambient occlusion may run. |
| [`SsaoHalfResolution`](#m-ssaohalfresolution) | Whether occlusion is computed at half resolution. |
| [`SsaoSamples`](#m-ssaosamples) | Occlusion samples per pixel. |

### <a id="m-bloom"></a>Bloom

```csharp
public bool Bloom { get; init; }
```

Whether bloom may run.

**Type:** `bool`

### <a id="m-environmentsize"></a>EnvironmentSize

```csharp
public int EnvironmentSize { get; init; }
```

Face size of the specular cube of environments created without an explicit size.

**Type:** `int`

### <a id="m-level"></a>Level

```csharp
public QualityLevel Level { get; init; }
```

The level these settings belong to.

**Type:** [`QualityLevel`](QualityLevel.md)

### <a id="m-maxcascades"></a>MaxCascades

```csharp
public int MaxCascades { get; init; }
```

Most cascades a directional light splits into.

**Type:** `int`

### <a id="m-maxsamples"></a>MaxSamples

```csharp
public int MaxSamples { get; init; }
```

Most multisampling samples (1, 4 or 8; 2 is not used because many formats lack it); the device may allow fewer.

**Type:** `int`

### <a id="m-shadowfilterradius"></a>ShadowFilterRadius

```csharp
public int ShadowFilterRadius { get; init; }
```

Percentage-closer filter radius in texels: 0 is one hardware-filtered tap, 1 is 3x3 taps, 2 is 5x5.

**Type:** `int`

### <a id="m-shadowtilesize"></a>ShadowTileSize

```csharp
public int ShadowTileSize { get; init; }
```

Edge of one shadow map tile in texels (one cascade, one spot light, one face of a point light).

**Type:** `int`

### <a id="m-ssao"></a>Ssao

```csharp
public bool Ssao { get; init; }
```

Whether ambient occlusion may run.

**Type:** `bool`

### <a id="m-ssaohalfresolution"></a>SsaoHalfResolution

```csharp
public bool SsaoHalfResolution { get; init; }
```

Whether occlusion is computed at half resolution.

**Type:** `bool`

### <a id="m-ssaosamples"></a>SsaoSamples

```csharp
public int SsaoSamples { get; init; }
```

Occlusion samples per pixel.

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`For(QualityLevel)`](#m-for-qualitylevel) | The settings of a level. |
| [`TryParse(string?, out QualityLevel)`](#m-tryparse-string-qualitylevel) | Parses a level name (case-insensitive); false for an unknown name. |

### <a id="m-for-qualitylevel"></a>For(QualityLevel)

```csharp
public static QualitySettings For(QualityLevel level);
```

The settings of a level.

**Parameters**

- `level` ([`QualityLevel`](QualityLevel.md)): The level.

**Returns:** [`QualitySettings`](QualitySettings.md)

### <a id="m-tryparse-string-qualitylevel"></a>TryParse(string?, out QualityLevel)

```csharp
public static bool TryParse(string? name, out QualityLevel level);
```

Parses a level name (case-insensitive); false for an unknown name.

**Parameters**

- `name` (`string?`): low, medium, high or ultra.
- `level` ([`QualityLevel`](QualityLevel.md)): The level.

**Returns:** `bool`
