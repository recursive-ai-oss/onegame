[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# VolumetricLightInfo record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`VolumetricLightInfo`](VolumetricLightInfo.md)&gt;

```csharp
public sealed record VolumetricLightInfo : IEquatable<VolumetricLightInfo>
```

One light as `volumetric` reports it.

## Constructors

| Member | Description |
| --- | --- |
| [`VolumetricLightInfo(int, string, bool, double, bool)`](#m-ctor-int-string-bool-double-bool) | One light as `volumetric` reports it. |

### <a id="m-ctor-int-string-bool-double-bool"></a>VolumetricLightInfo(int, string, bool, double, bool)

```csharp
public VolumetricLightInfo(int Index, string Kind, bool Volumetric, double Intensity, bool CastShadows);
```

One light as `volumetric` reports it.

**Parameters**

- `Index` (`int`): Position in the scene's light list.
- `Kind` (`string`): directional, point or spot.
- `Volumetric` (`bool`): Whether the light opts in to light shafts.
- `Intensity` (`double`): The light's own shaft multiplier.
- `CastShadows` (`bool`): Whether the light has shadow maps (shafts are broken up by them).

## Properties

| Member | Description |
| --- | --- |
| [`CastShadows`](#m-castshadows) | Whether the light has shadow maps (shafts are broken up by them). |
| [`Index`](#m-index) | Position in the scene's light list. |
| [`Intensity`](#m-intensity) | The light's own shaft multiplier. |
| [`Kind`](#m-kind) | directional, point or spot. |
| [`Volumetric`](#m-volumetric) | Whether the light opts in to light shafts. |

### <a id="m-castshadows"></a>CastShadows

```csharp
public bool CastShadows { get; init; }
```

Whether the light has shadow maps (shafts are broken up by them).

**Type:** `bool`

### <a id="m-index"></a>Index

```csharp
public int Index { get; init; }
```

Position in the scene's light list.

**Type:** `int`

### <a id="m-intensity"></a>Intensity

```csharp
public double Intensity { get; init; }
```

The light's own shaft multiplier.

**Type:** `double`

### <a id="m-kind"></a>Kind

```csharp
public string Kind { get; init; }
```

directional, point or spot.

**Type:** `string`

### <a id="m-volumetric"></a>Volumetric

```csharp
public bool Volumetric { get; init; }
```

Whether the light opts in to light shafts.

**Type:** `bool`
