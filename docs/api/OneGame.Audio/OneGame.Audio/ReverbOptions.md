[API reference](../../index.md) › [OneGame.Audio](../index.md) › OneGame.Audio

# ReverbOptions struct

**Namespace:** `OneGame.Audio`  
**Package:** [`RecursiveAI.OneGame.Audio`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ReverbOptions`](ReverbOptions.md)&gt;

```csharp
public readonly struct ReverbOptions : IEquatable<ReverbOptions>
```

A reverb insert on a bus: the bus signal becomes `Dry * signal + Wet * reverb`.

## Constructors

| Member | Description |
| --- | --- |
| [`ReverbOptions()`](#m-ctor) | Creates a medium room: 1.5 s decay, wet 0.25. |

### <a id="m-ctor"></a>ReverbOptions()

```csharp
public ReverbOptions();
```

Creates a medium room: 1.5 s decay, wet 0.25.

## Properties

| Member | Description |
| --- | --- |
| [`Damping`](#m-damping) | High-frequency absorption, 0 to 1. |
| [`DecaySeconds`](#m-decayseconds) | Time for the tail to fall 60 dB at low frequencies, in seconds (0.1 to 20). |
| [`Dry`](#m-dry) | Linear gain of the unprocessed signal, 0 to 4. |
| [`PreDelayMilliseconds`](#m-predelaymilliseconds) | Delay before the tail starts, in milliseconds. |
| [`RoomSize`](#m-roomsize) | Apparent room size, 0 to 1. |
| [`Wet`](#m-wet) | Linear gain of the reverberated signal, 0 to 4. |

### <a id="m-damping"></a>Damping

```csharp
public float Damping { get; init; }
```

High-frequency absorption, 0 to 1. Default 0.4.

**Type:** `float`

### <a id="m-decayseconds"></a>DecaySeconds

```csharp
public float DecaySeconds { get; init; }
```

Time for the tail to fall 60 dB at low frequencies, in seconds (0.1 to 20). Default 1.5.

**Type:** `float`

### <a id="m-dry"></a>Dry

```csharp
public float Dry { get; init; }
```

Linear gain of the unprocessed signal, 0 to 4. Default 1.

**Type:** `float`

### <a id="m-predelaymilliseconds"></a>PreDelayMilliseconds

```csharp
public float PreDelayMilliseconds { get; init; }
```

Delay before the tail starts, in milliseconds. Default 0.

**Type:** `float`

### <a id="m-roomsize"></a>RoomSize

```csharp
public float RoomSize { get; init; }
```

Apparent room size, 0 to 1. Default 0.5.

**Type:** `float`

### <a id="m-wet"></a>Wet

```csharp
public float Wet { get; init; }
```

Linear gain of the reverberated signal, 0 to 4. Default 0.25.

**Type:** `float`
