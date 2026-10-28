[API reference](../../index.md) › [OneGame.Audio](../index.md) › OneGame.Audio

# BusStatus record

**Namespace:** `OneGame.Audio`  
**Package:** [`RecursiveAI.OneGame.Audio`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`BusStatus`](BusStatus.md)&gt;

```csharp
public sealed record BusStatus : IEquatable<BusStatus>
```

One bus as the service set it up.

## Constructors

| Member | Description |
| --- | --- |
| [`BusStatus(int, string?, float, bool, FilterStatus?, FilterStatus?, ReverbOptions?, DuckingStatus?, LoudnessStatus?, bool)`](#m-ctor-int-string-float-bool-filterstatus-filterstatus-reverboptions-duckingstatus-loudnessstatus-bool) | One bus as the service set it up. |

### <a id="m-ctor-int-string-float-bool-filterstatus-filterstatus-reverboptions-duckingstatus-loudnessstatus-bool"></a>BusStatus(int, string?, float, bool, FilterStatus?, FilterStatus?, ReverbOptions?, DuckingStatus?, LoudnessStatus?, bool)

```csharp
public BusStatus(int Index, string? Name, float Volume, bool Muted, FilterStatus? LowPass, FilterStatus? HighPass, ReverbOptions? Reverb, DuckingStatus? Ducking, LoudnessStatus? Loudness = null, bool Metered = false);
```

One bus as the service set it up.

**Parameters**

- `Index` (`int`): The bus index.
- `Name` (`string?`): `music`, `sfx` or `ui` for the named buses, otherwise null.
- `Volume` (`float`): The bus volume (before the master volume).
- `Muted` (`bool`): Whether it is muted.
- `LowPass` ([`FilterStatus`](FilterStatus.md)?): Its low-pass, or null.
- `HighPass` ([`FilterStatus`](FilterStatus.md)?): Its high-pass, or null.
- `Reverb` ([`ReverbOptions`](ReverbOptions.md)?): Its reverb, or null.
- `Ducking` ([`DuckingStatus`](DuckingStatus.md)?): What ducks it, or null.
- `Loudness` ([`LoudnessStatus`](LoudnessStatus.md)?): Its loudness (see [`AudioService.GetBusLoudness()`](AudioService.md#m-getbusloudness-audiobus)), or null when the bus is not metered.
- `Metered` (`bool`): Whether a loudness meter is assigned to the bus (a reading follows at the next block).

## Properties

| Member | Description |
| --- | --- |
| [`Ducking`](#m-ducking) | What ducks it, or null. |
| [`HighPass`](#m-highpass) | Its high-pass, or null. |
| [`Index`](#m-index) | The bus index. |
| [`Loudness`](#m-loudness) | Its loudness (see [`AudioService.GetBusLoudness()`](AudioService.md#m-getbusloudness-audiobus)), or null when the bus is not metered. |
| [`LowPass`](#m-lowpass) | Its low-pass, or null. |
| [`Metered`](#m-metered) | Whether a loudness meter is assigned to the bus (a reading follows at the next block). |
| [`Muted`](#m-muted) | Whether it is muted. |
| [`Name`](#m-name) | `music`, `sfx` or `ui` for the named buses, otherwise null. |
| [`Reverb`](#m-reverb) | Its reverb, or null. |
| [`Volume`](#m-volume) | The bus volume (before the master volume). |

### <a id="m-ducking"></a>Ducking

```csharp
public DuckingStatus? Ducking { get; init; }
```

What ducks it, or null.

**Type:** [`DuckingStatus`](DuckingStatus.md)?

### <a id="m-highpass"></a>HighPass

```csharp
public FilterStatus? HighPass { get; init; }
```

Its high-pass, or null.

**Type:** [`FilterStatus`](FilterStatus.md)?

### <a id="m-index"></a>Index

```csharp
public int Index { get; init; }
```

The bus index.

**Type:** `int`

### <a id="m-loudness"></a>Loudness

```csharp
public LoudnessStatus? Loudness { get; init; }
```

Its loudness (see [`AudioService.GetBusLoudness()`](AudioService.md#m-getbusloudness-audiobus)), or null when the bus is not metered.

**Type:** [`LoudnessStatus`](LoudnessStatus.md)?

### <a id="m-lowpass"></a>LowPass

```csharp
public FilterStatus? LowPass { get; init; }
```

Its low-pass, or null.

**Type:** [`FilterStatus`](FilterStatus.md)?

### <a id="m-metered"></a>Metered

```csharp
public bool Metered { get; init; }
```

Whether a loudness meter is assigned to the bus (a reading follows at the next block).

**Type:** `bool`

### <a id="m-muted"></a>Muted

```csharp
public bool Muted { get; init; }
```

Whether it is muted.

**Type:** `bool`

### <a id="m-name"></a>Name

```csharp
public string? Name { get; init; }
```

`music`, `sfx` or `ui` for the named buses, otherwise null.

**Type:** `string?`

### <a id="m-reverb"></a>Reverb

```csharp
public ReverbOptions? Reverb { get; init; }
```

Its reverb, or null.

**Type:** [`ReverbOptions`](ReverbOptions.md)?

### <a id="m-volume"></a>Volume

```csharp
public float Volume { get; init; }
```

The bus volume (before the master volume).

**Type:** `float`
