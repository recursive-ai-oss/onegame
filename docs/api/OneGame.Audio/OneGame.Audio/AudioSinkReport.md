[API reference](../../index.md) › [OneGame.Audio](../index.md) › OneGame.Audio

# AudioSinkReport struct

**Namespace:** `OneGame.Audio`  
**Package:** [`RecursiveAI.OneGame.Audio`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`AudioSinkReport`](AudioSinkReport.md)&gt;

```csharp
public readonly struct AudioSinkReport : IEquatable<AudioSinkReport>
```

What a sink can say about the device behind it, measured on the device's side of the hand-over.

## Constructors

| Member | Description |
| --- | --- |
| [`AudioSinkReport(string, long, long, float, long, string?)`](#m-ctor-string-long-long-float-long-string) | What a sink can say about the device behind it, measured on the device's side of the hand-over. |

### <a id="m-ctor-string-long-long-float-long-string"></a>AudioSinkReport(string, long, long, float, long, string?)

```csharp
public AudioSinkReport(string State, long FramesWritten, long FramesPlayed, float PeakPlayed, long DeviceUnderruns, string? Detail);
```

What a sink can say about the device behind it, measured on the device's side of the hand-over.

**Parameters**

- `State` (`string`): The device's own state, in a few lowercase words (for example `started`, `paused`, `awaiting_gesture`).
- `FramesWritten` (`long`): Frames handed to the device since it was opened.
- `FramesPlayed` (`long`): Frames the device has taken from us (its read position). Advancing means the speaker path is running.
- `PeakPlayed` (`float`): The highest absolute sample value among frames the device has taken; 0 means everything it played was silence.
- `DeviceUnderruns` (`long`): Underruns the device itself counted (for devices that count them).
- `Detail` (`string?`): Platform extras such as the audio route, session category or autoplay state; `null` if none.

## Properties

| Member | Description |
| --- | --- |
| [`Detail`](#m-detail) | Platform extras such as the audio route, session category or autoplay state; `null` if none. |
| [`DeviceUnderruns`](#m-deviceunderruns) | Underruns the device itself counted (for devices that count them). |
| [`FramesPlayed`](#m-framesplayed) | Frames the device has taken from us (its read position). |
| [`FramesWritten`](#m-frameswritten) | Frames handed to the device since it was opened. |
| [`PeakPlayed`](#m-peakplayed) | The highest absolute sample value among frames the device has taken; 0 means everything it played was silence. |
| [`State`](#m-state) | The device's own state, in a few lowercase words (for example `started`, `paused`, `awaiting_gesture`). |

### <a id="m-detail"></a>Detail

```csharp
public string? Detail { get; init; }
```

Platform extras such as the audio route, session category or autoplay state; `null` if none.

**Type:** `string?`

### <a id="m-deviceunderruns"></a>DeviceUnderruns

```csharp
public long DeviceUnderruns { get; init; }
```

Underruns the device itself counted (for devices that count them).

**Type:** `long`

### <a id="m-framesplayed"></a>FramesPlayed

```csharp
public long FramesPlayed { get; init; }
```

Frames the device has taken from us (its read position). Advancing means the speaker path is running.

**Type:** `long`

### <a id="m-frameswritten"></a>FramesWritten

```csharp
public long FramesWritten { get; init; }
```

Frames handed to the device since it was opened.

**Type:** `long`

### <a id="m-peakplayed"></a>PeakPlayed

```csharp
public float PeakPlayed { get; init; }
```

The highest absolute sample value among frames the device has taken; 0 means everything it played was silence.

**Type:** `float`

### <a id="m-state"></a>State

```csharp
public string State { get; init; }
```

The device's own state, in a few lowercase words (for example `started`, `paused`, `awaiting_gesture`).

**Type:** `string`
