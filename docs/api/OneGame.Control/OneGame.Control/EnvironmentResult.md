[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# EnvironmentResult record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`EnvironmentResult`](EnvironmentResult.md)&gt;

```csharp
public sealed record EnvironmentResult : IEquatable<EnvironmentResult>
```

Result of `environment`: the scene's distance fog and environment crossfade.

## Constructors

| Member | Description |
| --- | --- |
| [`EnvironmentResult(bool, double[], double, double, double, bool, double, double, double)`](#m-ctor-bool-double-double-double-double-bool-double-double-double) | Result of `environment`: the scene's distance fog and environment crossfade. |

### <a id="m-ctor-bool-double-double-double-double-bool-double-double-double"></a>EnvironmentResult(bool, double\[\], double, double, double, bool, double, double, double)

```csharp
public EnvironmentResult(bool FogEnabled, double[] FogColor, double FogStart, double FogEnd, double FogMaxDensity, bool HasNextMap, double Blend, double Intensity, double Yaw);
```

Result of `environment`: the scene's distance fog and environment crossfade.

**Parameters**

- `FogEnabled` (`bool`): Whether fog is applied.
- `FogColor` (`double[]`): Linear fog colour (r, g, b).
- `FogStart` (`double`): Distance where fog begins.
- `FogEnd` (`double`): Distance where fog reaches its maximum.
- `FogMaxDensity` (`double`): Largest share of the fog colour a surface shows.
- `HasNextMap` (`bool`): Whether the scene has a second environment to fade to.
- `Blend` (`double`): Crossfade weight, 0 (first map) to 1 (second map).
- `Intensity` (`double`): Image-based light scale.
- `Yaw` (`double`): Environment turn about +Y in radians.

## Properties

| Member | Description |
| --- | --- |
| [`Blend`](#m-blend) | Crossfade weight, 0 (first map) to 1 (second map). |
| [`FogColor`](#m-fogcolor) | Linear fog colour (r, g, b). |
| [`FogEnabled`](#m-fogenabled) | Whether fog is applied. |
| [`FogEnd`](#m-fogend) | Distance where fog reaches its maximum. |
| [`FogMaxDensity`](#m-fogmaxdensity) | Largest share of the fog colour a surface shows. |
| [`FogStart`](#m-fogstart) | Distance where fog begins. |
| [`HasNextMap`](#m-hasnextmap) | Whether the scene has a second environment to fade to. |
| [`Intensity`](#m-intensity) | Image-based light scale. |
| [`Yaw`](#m-yaw) | Environment turn about +Y in radians. |

### <a id="m-blend"></a>Blend

```csharp
public double Blend { get; init; }
```

Crossfade weight, 0 (first map) to 1 (second map).

**Type:** `double`

### <a id="m-fogcolor"></a>FogColor

```csharp
public double[] FogColor { get; init; }
```

Linear fog colour (r, g, b).

**Type:** `double[]`

### <a id="m-fogenabled"></a>FogEnabled

```csharp
public bool FogEnabled { get; init; }
```

Whether fog is applied.

**Type:** `bool`

### <a id="m-fogend"></a>FogEnd

```csharp
public double FogEnd { get; init; }
```

Distance where fog reaches its maximum.

**Type:** `double`

### <a id="m-fogmaxdensity"></a>FogMaxDensity

```csharp
public double FogMaxDensity { get; init; }
```

Largest share of the fog colour a surface shows.

**Type:** `double`

### <a id="m-fogstart"></a>FogStart

```csharp
public double FogStart { get; init; }
```

Distance where fog begins.

**Type:** `double`

### <a id="m-hasnextmap"></a>HasNextMap

```csharp
public bool HasNextMap { get; init; }
```

Whether the scene has a second environment to fade to.

**Type:** `bool`

### <a id="m-intensity"></a>Intensity

```csharp
public double Intensity { get; init; }
```

Image-based light scale.

**Type:** `double`

### <a id="m-yaw"></a>Yaw

```csharp
public double Yaw { get; init; }
```

Environment turn about +Y in radians.

**Type:** `double`
