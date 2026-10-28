[API reference](../../index.md) › [OneGame.Audio](../index.md) › OneGame.Audio

# AudioStatus record

**Namespace:** `OneGame.Audio`  
**Package:** [`RecursiveAI.OneGame.Audio`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`AudioStatus`](AudioStatus.md)&gt;

```csharp
public sealed record AudioStatus : IEquatable<AudioStatus>
```

The mixing state, from [`AudioService.Describe()`](AudioService.md#m-describe).

## Constructors

| Member | Description |
| --- | --- |
| [`AudioStatus(int, int, string, bool, float, bool, int, BusStatus[], LimiterStatus?, LoudnessStatus, MusicStatus?, int, string?, AudioDeviceStatus?)`](#m-ctor-int-int-string-bool-float-bool-int-busstatus-limiterstatus-loudnessstatus-musicstatus-int-string-audiodevicestatus) | The mixing state, from [`AudioService.Describe()`](AudioService.md#m-describe). |

### <a id="m-ctor-int-int-string-bool-float-bool-int-busstatus-limiterstatus-loudnessstatus-musicstatus-int-string-audiodevicestatus"></a>AudioStatus(int, int, string, bool, float, bool, int, BusStatus\[\], LimiterStatus?, LoudnessStatus, MusicStatus?, int, string?, AudioDeviceStatus?)

```csharp
public AudioStatus(int SampleRate, int Channels, string Output, bool Capture, float MasterVolume, bool Muted, int ActiveVoices, BusStatus[] Buses, LimiterStatus? Limiter, LoudnessStatus Loudness, MusicStatus? Music, int FreeBusMeters = 0, string? MeterRequest = null, AudioDeviceStatus? Device = null);
```

The mixing state, from [`AudioService.Describe()`](AudioService.md#m-describe).

**Parameters**

- `SampleRate` (`int`): Output frames per second.
- `Channels` (`int`): Output channels.
- `Output` (`string`): The output's status (`Running`, `NoDevice`, ...).
- `Capture` (`bool`): Whether the service records instead of playing.
- `MasterVolume` (`float`): The master volume.
- `Muted` (`bool`): Whether everything is muted.
- `ActiveVoices` (`int`): Voices playing now.
- `Buses` ([`BusStatus`](BusStatus.md)\[\]): Every bus.
- `Limiter` ([`LimiterStatus`](LimiterStatus.md)?): The master limiter, or null when the service has none.
- `Loudness` ([`LoudnessStatus`](LoudnessStatus.md)): The output loudness.
- `Music` ([`MusicStatus`](MusicStatus.md)?): The music track, or null when none plays.
- `FreeBusMeters` (`int`): Loudness meter slots not in use.
- `MeterRequest` (`string?`): What a meter request in the same call came to (the `audio` operation's `enableMeter`/`disableMeter`): metered, no\_free\_slot, queue\_full, unavailable or disabled; null otherwise.
- `Device` ([`AudioDeviceStatus`](AudioDeviceStatus.md)?): The device side: backend, its state, underruns, and the frames and peak the device has taken.

## Properties

| Member | Description |
| --- | --- |
| [`ActiveVoices`](#m-activevoices) | Voices playing now. |
| [`Buses`](#m-buses) | Every bus. |
| [`Capture`](#m-capture) | Whether the service records instead of playing. |
| [`Channels`](#m-channels) | Output channels. |
| [`Device`](#m-device) | The device side: backend, its state, underruns, and the frames and peak the device has taken. |
| [`FreeBusMeters`](#m-freebusmeters) | Loudness meter slots not in use. |
| [`Limiter`](#m-limiter) | The master limiter, or null when the service has none. |
| [`Loudness`](#m-loudness) | The output loudness. |
| [`MasterVolume`](#m-mastervolume) | The master volume. |
| [`MeterRequest`](#m-meterrequest) | What a meter request in the same call came to (the `audio` operation's `enableMeter`/`disableMeter`): metered, no\_free\_slot, queue\_full, unavailable or disabled; null otherwise. |
| [`Music`](#m-music) | The music track, or null when none plays. |
| [`Muted`](#m-muted) | Whether everything is muted. |
| [`Output`](#m-output) | The output's status (`Running`, `NoDevice`, ...). |
| [`SampleRate`](#m-samplerate) | Output frames per second. |

### <a id="m-activevoices"></a>ActiveVoices

```csharp
public int ActiveVoices { get; init; }
```

Voices playing now.

**Type:** `int`

### <a id="m-buses"></a>Buses

```csharp
public BusStatus[] Buses { get; init; }
```

Every bus.

**Type:** [`BusStatus`](BusStatus.md)\[\]

### <a id="m-capture"></a>Capture

```csharp
public bool Capture { get; init; }
```

Whether the service records instead of playing.

**Type:** `bool`

### <a id="m-channels"></a>Channels

```csharp
public int Channels { get; init; }
```

Output channels.

**Type:** `int`

### <a id="m-device"></a>Device

```csharp
public AudioDeviceStatus? Device { get; init; }
```

The device side: backend, its state, underruns, and the frames and peak the device has taken.

**Type:** [`AudioDeviceStatus`](AudioDeviceStatus.md)?

### <a id="m-freebusmeters"></a>FreeBusMeters

```csharp
public int FreeBusMeters { get; init; }
```

Loudness meter slots not in use.

**Type:** `int`

### <a id="m-limiter"></a>Limiter

```csharp
public LimiterStatus? Limiter { get; init; }
```

The master limiter, or null when the service has none.

**Type:** [`LimiterStatus`](LimiterStatus.md)?

### <a id="m-loudness"></a>Loudness

```csharp
public LoudnessStatus Loudness { get; init; }
```

The output loudness.

**Type:** [`LoudnessStatus`](LoudnessStatus.md)

### <a id="m-mastervolume"></a>MasterVolume

```csharp
public float MasterVolume { get; init; }
```

The master volume.

**Type:** `float`

### <a id="m-meterrequest"></a>MeterRequest

```csharp
public string? MeterRequest { get; init; }
```

What a meter request in the same call came to (the `audio` operation's `enableMeter`/`disableMeter`): metered, no\_free\_slot, queue\_full, unavailable or disabled; null otherwise.

**Type:** `string?`

### <a id="m-music"></a>Music

```csharp
public MusicStatus? Music { get; init; }
```

The music track, or null when none plays.

**Type:** [`MusicStatus`](MusicStatus.md)?

### <a id="m-muted"></a>Muted

```csharp
public bool Muted { get; init; }
```

Whether everything is muted.

**Type:** `bool`

### <a id="m-output"></a>Output

```csharp
public string Output { get; init; }
```

The output's status (`Running`, `NoDevice`, ...).

**Type:** `string`

### <a id="m-samplerate"></a>SampleRate

```csharp
public int SampleRate { get; init; }
```

Output frames per second.

**Type:** `int`
