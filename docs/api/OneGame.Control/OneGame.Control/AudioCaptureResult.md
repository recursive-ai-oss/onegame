[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# AudioCaptureResult record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`AudioCaptureResult`](AudioCaptureResult.md)&gt;

```csharp
public sealed record AudioCaptureResult : IEquatable<AudioCaptureResult>
```

Result of `audio_capture`: what the capture sink holds after the advance.

## Constructors

| Member | Description |
| --- | --- |
| [`AudioCaptureResult(string?, int, double, float, string?, LoudnessStatus, BusLoudnessStatus[])`](#m-ctor-string-int-double-float-string-loudnessstatus-busloudnessstatus) | Result of `audio_capture`: what the capture sink holds after the advance. |

### <a id="m-ctor-string-int-double-float-string-loudnessstatus-busloudnessstatus"></a>AudioCaptureResult(string?, int, double, float, string?, LoudnessStatus, BusLoudnessStatus\[\])

```csharp
public AudioCaptureResult(string? Path, int Frames, double Seconds, float Peak, string? Sha256, LoudnessStatus Loudness, BusLoudnessStatus[] BusLoudness);
```

Result of `audio_capture`: what the capture sink holds after the advance.

**Parameters**

- `Path` (`string?`): The WAV file written under the output folder, or null.
- `Frames` (`int`): Frames recorded since the sink was last cleared.
- `Seconds` (`double`): The same in seconds.
- `Peak` (`float`): The highest sample magnitude recorded.
- `Sha256` (`string?`): Hash of the WAV file, when written.
- `Loudness` ([`LoudnessStatus`](../../OneGame.Audio/OneGame.Audio/LoudnessStatus.md)): The output loudness now.
- `BusLoudness` ([`BusLoudnessStatus`](BusLoudnessStatus.md)\[\]): The loudness of each metered bus, measured before ducking.

## Properties

| Member | Description |
| --- | --- |
| [`BusLoudness`](#m-busloudness) | The loudness of each metered bus, measured before ducking. |
| [`Frames`](#m-frames) | Frames recorded since the sink was last cleared. |
| [`Loudness`](#m-loudness) | The output loudness now. |
| [`Path`](#m-path) | The WAV file written under the output folder, or null. |
| [`Peak`](#m-peak) | The highest sample magnitude recorded. |
| [`Seconds`](#m-seconds) | The same in seconds. |
| [`Sha256`](#m-sha256) | Hash of the WAV file, when written. |

### <a id="m-busloudness"></a>BusLoudness

```csharp
public BusLoudnessStatus[] BusLoudness { get; init; }
```

The loudness of each metered bus, measured before ducking.

**Type:** [`BusLoudnessStatus`](BusLoudnessStatus.md)\[\]

### <a id="m-frames"></a>Frames

```csharp
public int Frames { get; init; }
```

Frames recorded since the sink was last cleared.

**Type:** `int`

### <a id="m-loudness"></a>Loudness

```csharp
public LoudnessStatus Loudness { get; init; }
```

The output loudness now.

**Type:** [`LoudnessStatus`](../../OneGame.Audio/OneGame.Audio/LoudnessStatus.md)

### <a id="m-path"></a>Path

```csharp
public string? Path { get; init; }
```

The WAV file written under the output folder, or null.

**Type:** `string?`

### <a id="m-peak"></a>Peak

```csharp
public float Peak { get; init; }
```

The highest sample magnitude recorded.

**Type:** `float`

### <a id="m-seconds"></a>Seconds

```csharp
public double Seconds { get; init; }
```

The same in seconds.

**Type:** `double`

### <a id="m-sha256"></a>Sha256

```csharp
public string? Sha256 { get; init; }
```

Hash of the WAV file, when written.

**Type:** `string?`
