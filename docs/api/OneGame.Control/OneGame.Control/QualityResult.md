[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# QualityResult record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`QualityResult`](QualityResult.md)&gt;

```csharp
public sealed record QualityResult : IEquatable<QualityResult>
```

Result of `quality`: the level of the 3D renderer and what it means, with what the last frame did.

## Constructors

| Member | Description |
| --- | --- |
| [`QualityResult(string, string[], int, int, int, int, bool, int, bool, int, bool, int, int, int, int, string[])`](#m-ctor-string-string-int-int-int-int-bool-int-bool-int-bool-int-int-int-int-string) | Result of `quality`: the level of the 3D renderer and what it means, with what the last frame did. |

### <a id="m-ctor-string-string-int-int-int-int-bool-int-bool-int-bool-int-int-int-int-string"></a>QualityResult(string, string\[\], int, int, int, int, bool, int, bool, int, bool, int, int, int, int, string\[\])

```csharp
public QualityResult(string Level, string[] Levels, int ShadowTileSize, int MaxCascades, int ShadowFilterRadius, int MaxSamples, bool Bloom, int EnvironmentSize, bool SsaoAllowed, int SsaoSamples, bool SsaoEnabled, int ShadowTiles, int ShadowLights, int Instances, int InstancesDrawn, string[] Passes);
```

Result of `quality`: the level of the 3D renderer and what it means, with what the last frame did.

**Parameters**

- `Level` (`string`): Current level: low, medium, high or ultra.
- `Levels` (`string[]`): All level names.
- `ShadowTileSize` (`int`): Shadow map tile edge in texels.
- `MaxCascades` (`int`): Most cascades per directional light.
- `ShadowFilterRadius` (`int`): Shadow filter radius in texels.
- `MaxSamples` (`int`): Most multisampling samples.
- `Bloom` (`bool`): Whether bloom may run at this level.
- `EnvironmentSize` (`int`): Specular cube face size of environments that follow the level.
- `SsaoAllowed` (`bool`): Whether this level allows ambient occlusion.
- `SsaoSamples` (`int`): Occlusion samples per pixel.
- `SsaoEnabled` (`bool`): Whether the scene asks for ambient occlusion.
- `ShadowTiles` (`int`): Shadow map tiles the last frame drew.
- `ShadowLights` (`int`): Lights that cast shadows in the last frame.
- `Instances` (`int`): Instanced copies in the last frame.
- `InstancesDrawn` (`int`): Instanced copies that survived culling in the last frame.
- `Passes` (`string[]`): Names of the passes the last frame ran, in order (the same list `render_graph` reports).

## Properties

| Member | Description |
| --- | --- |
| [`Bloom`](#m-bloom) | Whether bloom may run at this level. |
| [`EnvironmentSize`](#m-environmentsize) | Specular cube face size of environments that follow the level. |
| [`Instances`](#m-instances) | Instanced copies in the last frame. |
| [`InstancesDrawn`](#m-instancesdrawn) | Instanced copies that survived culling in the last frame. |
| [`Level`](#m-level) | Current level: low, medium, high or ultra. |
| [`Levels`](#m-levels) | All level names. |
| [`MaxCascades`](#m-maxcascades) | Most cascades per directional light. |
| [`MaxSamples`](#m-maxsamples) | Most multisampling samples. |
| [`Passes`](#m-passes) | Names of the passes the last frame ran, in order (the same list `render_graph` reports). |
| [`ShadowFilterRadius`](#m-shadowfilterradius) | Shadow filter radius in texels. |
| [`ShadowLights`](#m-shadowlights) | Lights that cast shadows in the last frame. |
| [`ShadowTileSize`](#m-shadowtilesize) | Shadow map tile edge in texels. |
| [`ShadowTiles`](#m-shadowtiles) | Shadow map tiles the last frame drew. |
| [`SsaoAllowed`](#m-ssaoallowed) | Whether this level allows ambient occlusion. |
| [`SsaoEnabled`](#m-ssaoenabled) | Whether the scene asks for ambient occlusion. |
| [`SsaoSamples`](#m-ssaosamples) | Occlusion samples per pixel. |

### <a id="m-bloom"></a>Bloom

```csharp
public bool Bloom { get; init; }
```

Whether bloom may run at this level.

**Type:** `bool`

### <a id="m-environmentsize"></a>EnvironmentSize

```csharp
public int EnvironmentSize { get; init; }
```

Specular cube face size of environments that follow the level.

**Type:** `int`

### <a id="m-instances"></a>Instances

```csharp
public int Instances { get; init; }
```

Instanced copies in the last frame.

**Type:** `int`

### <a id="m-instancesdrawn"></a>InstancesDrawn

```csharp
public int InstancesDrawn { get; init; }
```

Instanced copies that survived culling in the last frame.

**Type:** `int`

### <a id="m-level"></a>Level

```csharp
public string Level { get; init; }
```

Current level: low, medium, high or ultra.

**Type:** `string`

### <a id="m-levels"></a>Levels

```csharp
public string[] Levels { get; init; }
```

All level names.

**Type:** `string[]`

### <a id="m-maxcascades"></a>MaxCascades

```csharp
public int MaxCascades { get; init; }
```

Most cascades per directional light.

**Type:** `int`

### <a id="m-maxsamples"></a>MaxSamples

```csharp
public int MaxSamples { get; init; }
```

Most multisampling samples.

**Type:** `int`

### <a id="m-passes"></a>Passes

```csharp
public string[] Passes { get; init; }
```

Names of the passes the last frame ran, in order (the same list `render_graph` reports).

**Type:** `string[]`

### <a id="m-shadowfilterradius"></a>ShadowFilterRadius

```csharp
public int ShadowFilterRadius { get; init; }
```

Shadow filter radius in texels.

**Type:** `int`

### <a id="m-shadowlights"></a>ShadowLights

```csharp
public int ShadowLights { get; init; }
```

Lights that cast shadows in the last frame.

**Type:** `int`

### <a id="m-shadowtilesize"></a>ShadowTileSize

```csharp
public int ShadowTileSize { get; init; }
```

Shadow map tile edge in texels.

**Type:** `int`

### <a id="m-shadowtiles"></a>ShadowTiles

```csharp
public int ShadowTiles { get; init; }
```

Shadow map tiles the last frame drew.

**Type:** `int`

### <a id="m-ssaoallowed"></a>SsaoAllowed

```csharp
public bool SsaoAllowed { get; init; }
```

Whether this level allows ambient occlusion.

**Type:** `bool`

### <a id="m-ssaoenabled"></a>SsaoEnabled

```csharp
public bool SsaoEnabled { get; init; }
```

Whether the scene asks for ambient occlusion.

**Type:** `bool`

### <a id="m-ssaosamples"></a>SsaoSamples

```csharp
public int SsaoSamples { get; init; }
```

Occlusion samples per pixel.

**Type:** `int`
