[API reference](../../index.md) › [OneGame.Audio](../index.md) › OneGame.Audio

# AudioServiceOptions record

**Namespace:** `OneGame.Audio`  
**Package:** [`RecursiveAI.OneGame.Audio`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`AudioServiceOptions`](AudioServiceOptions.md)&gt;

```csharp
public sealed record AudioServiceOptions : IEquatable<AudioServiceOptions>
```

Settings for [`AudioService.Create()`](AudioService.md#m-create-audioserviceoptions).

## Constructors

| Member | Description |
| --- | --- |
| [`AudioServiceOptions()`](#m-ctor) |  |

### <a id="m-ctor"></a>AudioServiceOptions()

```csharp
public AudioServiceOptions();
```

## Properties

| Member | Description |
| --- | --- |
| [`Backend`](#m-backend) | The audio system. |
| [`BusCount`](#m-buscount) | Number of buses; at least 3. |
| [`Capture`](#m-capture) | Records instead of playing: no device, no thread, and time moves only with [`AudioService.Advance()`](AudioService.md#m-advance-int). |
| [`CoreFactory`](#m-corefactory) | The mixer. |
| [`Device`](#m-device) | The device to ask for. |
| [`Limiter`](#m-limiter) | How the output keeps samples in range. |
| [`LimiterLookaheadMilliseconds`](#m-limiterlookaheadmilliseconds) | The master limiter's lookahead in milliseconds, 1 to 100; only used when [`AudioServiceOptions.MasterLimiter`](AudioServiceOptions.md#m-masterlimiter) is set. |
| [`MasterLimiter`](#m-masterlimiter) | A lookahead limiter on the master output: no sample passes the ceiling, and it reduces gain gently before a peak instead of clipping it. |
| [`MaxBusMeters`](#m-maxbusmeters) | Buses whose loudness can be measured at once, 0 to 64 (see [`AudioService.EnableBusMeter()`](AudioService.md#m-enablebusmeter-audiobus)). |
| [`MaxClipSeconds`](#m-maxclipseconds) | The longest file [`AudioService.LoadSound()`](AudioService.md#m-loadsound-stream-string) decodes into memory, in seconds; longer files must be streamed as music. |
| [`MaxReverbs`](#m-maxreverbs) | Reverbs that can be active at once across all buses, 0 to 16. |
| [`MaxVoices`](#m-maxvoices) | Most simultaneous voices. |
| [`Metering`](#m-metering) | Whether the output measures loudness (EBU R 128), readable as [`AudioService.Loudness`](AudioService.md#m-loudness). |
| [`PauseOnFocusLoss`](#m-pauseonfocusloss) | Whether [`AudioService.SetFocused()`](AudioService.md#m-setfocused-bool) with false silences the game. |

### <a id="m-backend"></a>Backend

```csharp
public IAudioBackend? Backend { get; init; }
```

The audio system. `null` picks the operating system's own (ALSA on Linux, WASAPI on Windows) and falls back to silence elsewhere.

**Type:** [`IAudioBackend`](IAudioBackend.md)?

### <a id="m-buscount"></a>BusCount

```csharp
public int BusCount { get; init; }
```

Number of buses; at least 3. Default 8.

**Type:** `int`

### <a id="m-capture"></a>Capture

```csharp
public bool Capture { get; init; }
```

Records instead of playing: no device, no thread, and time moves only with [`AudioService.Advance()`](AudioService.md#m-advance-int). See [`AudioService.Capture`](AudioService.md#m-capture). Overrides [`AudioServiceOptions.Backend`](AudioServiceOptions.md#m-backend).

**Type:** `bool`

### <a id="m-corefactory"></a>CoreFactory

```csharp
public IAudioCoreFactory? CoreFactory { get; init; }
```

The mixer. `null` uses [`OneAudioCore`](OneAudioCore.md): oneaudio's mixer with WAV, Ogg Vorbis, Opus, FLAC and MP3 decoding.

**Type:** [`IAudioCoreFactory`](IAudioCoreFactory.md)?

### <a id="m-device"></a>Device

```csharp
public AudioDeviceOptions Device { get; init; }
```

The device to ask for.

**Type:** [`AudioDeviceOptions`](AudioDeviceOptions.md)

### <a id="m-limiter"></a>Limiter

```csharp
public OutputLimiterMode Limiter { get; init; }
```

How the output keeps samples in range. Default soft knee.

**Type:** [`OutputLimiterMode`](OutputLimiterMode.md)

### <a id="m-limiterlookaheadmilliseconds"></a>LimiterLookaheadMilliseconds

```csharp
public int LimiterLookaheadMilliseconds { get; init; }
```

The master limiter's lookahead in milliseconds, 1 to 100; only used when [`AudioServiceOptions.MasterLimiter`](AudioServiceOptions.md#m-masterlimiter) is set. Default 5.

**Type:** `int`

### <a id="m-masterlimiter"></a>MasterLimiter

```csharp
public LimiterOptions? MasterLimiter { get; init; }
```

A lookahead limiter on the master output: no sample passes the ceiling, and it reduces gain gently before a peak instead of clipping it. `null` (the default) builds none. A limiter delays all audio by [`AudioServiceOptions.LimiterLookaheadMilliseconds`](AudioServiceOptions.md#m-limiterlookaheadmilliseconds) for the life of the service, and can be retuned or switched off with [`AudioService.SetMasterLimiter()`](AudioService.md#m-setmasterlimiter-limiteroptions) but not added later.

**Type:** [`LimiterOptions`](LimiterOptions.md)?

### <a id="m-maxbusmeters"></a>MaxBusMeters

```csharp
public int MaxBusMeters { get; init; }
```

Buses whose loudness can be measured at once, 0 to 64 (see [`AudioService.EnableBusMeter()`](AudioService.md#m-enablebusmeter-audiobus)). Each costs about 0.1 MB, built at startup. When [`AudioServiceOptions.Metering`](AudioServiceOptions.md#m-metering) is on, the music, sfx and ui buses (as many as fit) are metered from the start. Default 3.

**Type:** `int`

### <a id="m-maxclipseconds"></a>MaxClipSeconds

```csharp
public double MaxClipSeconds { get; init; }
```

The longest file [`AudioService.LoadSound()`](AudioService.md#m-loadsound-stream-string) decodes into memory, in seconds; longer files must be streamed as music. Default 120.

**Type:** `double`

### <a id="m-maxreverbs"></a>MaxReverbs

```csharp
public int MaxReverbs { get; init; }
```

Reverbs that can be active at once across all buses, 0 to 16. Each is built at startup and costs about 0.3 MB. Default 2.

**Type:** `int`

### <a id="m-maxvoices"></a>MaxVoices

```csharp
public int MaxVoices { get; init; }
```

Most simultaneous voices. Default 64.

**Type:** `int`

### <a id="m-metering"></a>Metering

```csharp
public bool Metering { get; init; }
```

Whether the output measures loudness (EBU R 128), readable as [`AudioService.Loudness`](AudioService.md#m-loudness). The cost is small. Default true.

**Type:** `bool`

### <a id="m-pauseonfocusloss"></a>PauseOnFocusLoss

```csharp
public bool PauseOnFocusLoss { get; init; }
```

Whether [`AudioService.SetFocused()`](AudioService.md#m-setfocused-bool) with false silences the game. Default true.

**Type:** `bool`
