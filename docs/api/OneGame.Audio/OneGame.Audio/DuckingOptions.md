[API reference](../../index.md) › [OneGame.Audio](../index.md) › OneGame.Audio

# DuckingOptions struct

**Namespace:** `OneGame.Audio`  
**Package:** [`RecursiveAI.OneGame.Audio`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`DuckingOptions`](DuckingOptions.md)&gt;

```csharp
public readonly struct DuckingOptions : IEquatable<DuckingOptions>
```

How one bus is turned down by the level of another, for example music under spoken lines. The bus gain moves exponentially toward its goal, so the attack and release times are time constants: after one release time 63% of the way back is done, after four times 98%, and it never quite arrives. The watched level is taken after the watched bus's own gain and mute, so muting or fading it also stops the ducking.

## Constructors

| Member | Description |
| --- | --- |
| [`DuckingOptions()`](#m-ctor) | Creates the defaults: duck 12 dB when the watched bus passes -30 dBFS, 10 ms attack, 250 ms release. |

### <a id="m-ctor"></a>DuckingOptions()

```csharp
public DuckingOptions();
```

Creates the defaults: duck 12 dB when the watched bus passes -30 dBFS, 10 ms attack, 250 ms release.

## Properties

| Member | Description |
| --- | --- |
| [`AttackMs`](#m-attackms) | Time constant, in milliseconds, of the gain falling to the ducked level. |
| [`Enabled`](#m-enabled) | False for [`DuckingOptions.None`](DuckingOptions.md#m-none); the other values are then ignored. |
| [`None`](#m-none) | Settings that mean no ducking; [`AudioService.SetDucking()`](AudioService.md#m-setducking-audiobus-audiobus-duckingoptions) with them removes the link. |
| [`ReductionDb`](#m-reductiondb) | How far the target is turned down, in dB (positive). |
| [`ReleaseMs`](#m-releasems) | Time constant, in milliseconds, of the gain recovering once the watched bus is quiet. |
| [`ThresholdDb`](#m-thresholddb) | Level of the watched bus, in dBFS, above which ducking starts. |

### <a id="m-attackms"></a>AttackMs

```csharp
public float AttackMs { get; init; }
```

Time constant, in milliseconds, of the gain falling to the ducked level.

**Type:** `float`

### <a id="m-enabled"></a>Enabled

```csharp
public bool Enabled { get; init; }
```

False for [`DuckingOptions.None`](DuckingOptions.md#m-none); the other values are then ignored. Default true.

**Type:** `bool`

### <a id="m-none"></a>None

```csharp
public static DuckingOptions None { get; }
```

Settings that mean no ducking; [`AudioService.SetDucking()`](AudioService.md#m-setducking-audiobus-audiobus-duckingoptions) with them removes the link.

**Type:** [`DuckingOptions`](DuckingOptions.md)

### <a id="m-reductiondb"></a>ReductionDb

```csharp
public float ReductionDb { get; init; }
```

How far the target is turned down, in dB (positive).

**Type:** `float`

### <a id="m-releasems"></a>ReleaseMs

```csharp
public float ReleaseMs { get; init; }
```

Time constant, in milliseconds, of the gain recovering once the watched bus is quiet.

**Type:** `float`

### <a id="m-thresholddb"></a>ThresholdDb

```csharp
public float ThresholdDb { get; init; }
```

Level of the watched bus, in dBFS, above which ducking starts.

**Type:** `float`
