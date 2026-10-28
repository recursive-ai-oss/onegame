[API reference](../../index.md) › [OneGame.Audio](../index.md) › OneGame.Audio

# AudioCoreSettings record

**Namespace:** `OneGame.Audio`  
**Package:** [`RecursiveAI.OneGame.Audio`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`AudioCoreSettings`](AudioCoreSettings.md)&gt;

```csharp
public sealed record AudioCoreSettings : IEquatable<AudioCoreSettings>
```

What a core is built with.

## Constructors

| Member | Description |
| --- | --- |
| [`AudioCoreSettings(int, int, int, int)`](#m-ctor-int-int-int-int) | What a core is built with. |

### <a id="m-ctor-int-int-int-int"></a>AudioCoreSettings(int, int, int, int)

```csharp
public AudioCoreSettings(int SampleRate, int Channels, int MaxVoices, int Buses);
```

What a core is built with.

**Parameters**

- `SampleRate` (`int`): Frames per second.
- `Channels` (`int`): Samples per frame.
- `MaxVoices` (`int`): The most simultaneous voices.
- `Buses` (`int`): The number of buses.

## Properties

| Member | Description |
| --- | --- |
| [`Buses`](#m-buses) | The number of buses. |
| [`Channels`](#m-channels) | Samples per frame. |
| [`LimiterLookaheadMilliseconds`](#m-limiterlookaheadmilliseconds) | Lookahead of the master limiter in milliseconds, 0 to 100. |
| [`MaxBusMeters`](#m-maxbusmeters) | Per-bus loudness meters that can run at once, 0 to 64. |
| [`MaxReverbs`](#m-maxreverbs) | Reverbs that can be active at once, 0 to 16. |
| [`MaxVoices`](#m-maxvoices) | The most simultaneous voices. |
| [`SampleRate`](#m-samplerate) | Frames per second. |

### <a id="m-buses"></a>Buses

```csharp
public int Buses { get; init; }
```

The number of buses.

**Type:** `int`

### <a id="m-channels"></a>Channels

```csharp
public int Channels { get; init; }
```

Samples per frame.

**Type:** `int`

### <a id="m-limiterlookaheadmilliseconds"></a>LimiterLookaheadMilliseconds

```csharp
public int LimiterLookaheadMilliseconds { get; init; }
```

Lookahead of the master limiter in milliseconds, 0 to 100. 0 builds no limiter. The whole output is delayed by this much. Default 0.

**Type:** `int`

### <a id="m-maxbusmeters"></a>MaxBusMeters

```csharp
public int MaxBusMeters { get; init; }
```

Per-bus loudness meters that can run at once, 0 to 64. Each costs about 0.1 MB up front. Default 0.

**Type:** `int`

### <a id="m-maxreverbs"></a>MaxReverbs

```csharp
public int MaxReverbs { get; init; }
```

Reverbs that can be active at once, 0 to 16. Each costs about 0.3 MB up front. Default 2.

**Type:** `int`

### <a id="m-maxvoices"></a>MaxVoices

```csharp
public int MaxVoices { get; init; }
```

The most simultaneous voices.

**Type:** `int`

### <a id="m-samplerate"></a>SampleRate

```csharp
public int SampleRate { get; init; }
```

Frames per second.

**Type:** `int`
