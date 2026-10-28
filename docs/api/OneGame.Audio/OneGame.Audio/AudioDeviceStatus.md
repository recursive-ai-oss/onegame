[API reference](../../index.md) › [OneGame.Audio](../index.md) › OneGame.Audio

# AudioDeviceStatus record

**Namespace:** `OneGame.Audio`  
**Package:** [`RecursiveAI.OneGame.Audio`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`AudioDeviceStatus`](AudioDeviceStatus.md)&gt;

```csharp
public sealed record AudioDeviceStatus : IEquatable<AudioDeviceStatus>
```

The device section of the `audio` report.

## Constructors

| Member | Description |
| --- | --- |
| [`AudioDeviceStatus(string, string, string?, string?, string?, bool, bool, int, int, double, long, long, long, long, long, long, long, long, float, long, string?, string?)`](#m-ctor-string-string-string-string-string-bool-bool-int-int-double-long-long-long-long-long-long-long-long-float-long-string-string) | The device section of the `audio` report. |

### <a id="m-ctor-string-string-string-string-string-bool-bool-int-int-double-long-long-long-long-long-long-long-long-float-long-string-string"></a>AudioDeviceStatus(string, string, string?, string?, string?, bool, bool, int, int, double, long, long, long, long, long, long, long, long, float, long, string?, string?)

```csharp
public AudioDeviceStatus(string Backend, string State, string? DeviceState, string? Device, string? Error, bool Suspended, bool Paused, int BufferFrames, int PeriodFrames, double LatencyMs, long Underruns, long DeviceUnderruns, long Losses, long Reopens, long Suspensions, long FramesRendered, long FramesWritten, long FramesPlayed, float PeakPlayed, long AudioThreadAllocatedBytes, string? Detail, string? Request = null);
```

The device section of the `audio` report.

**Parameters**

- `Backend` (`string`): The operating-system API, such as `aaudio`, `audiounit`, `webaudio`.
- `State` (`string`): The output's state (`Running`, `NoDevice`, ...).
- `DeviceState` (`string?`): The device's own state from the sink, or null for sinks that don't say.
- `Device` (`string?`): The device name asked for; null for the system default.
- `Error` (`string?`): The last reason a device failed to open, or null.
- `Suspended` (`bool`): Whether the app lifecycle has the device stopped.
- `Paused` (`bool`): Whether the output is feeding silence (focus loss or pause).
- `BufferFrames` (`int`): The device buffer in frames.
- `PeriodFrames` (`int`): The write size in frames.
- `LatencyMs` (`double`): Audio queued in the device after the last write, in milliseconds.
- `Underruns` (`long`): Times the device ran dry, as seen by the output.
- `DeviceUnderruns` (`long`): Underruns the device counted itself.
- `Losses` (`long`): Times the device was lost.
- `Reopens` (`long`): Times a device was opened again.
- `Suspensions` (`long`): Times the lifecycle stopped the device.
- `FramesRendered` (`long`): Frames the mixer produced.
- `FramesWritten` (`long`): Frames handed to the device.
- `FramesPlayed` (`long`): Frames the device took.
- `PeakPlayed` (`float`): Highest absolute sample among frames the device took.
- `AudioThreadAllocatedBytes` (`long`): Bytes the audio thread allocated since warm-up. Expected 0.
- `Detail` (`string?`): Platform extras (route, session, autoplay), or null.
- `Request` (`string?`): What a device request in the same `audio` call came to (unlock, interrupt, suspend), or null.

## Properties

| Member | Description |
| --- | --- |
| [`AudioThreadAllocatedBytes`](#m-audiothreadallocatedbytes) | Bytes the audio thread allocated since warm-up. |
| [`Backend`](#m-backend) | The operating-system API, such as `aaudio`, `audiounit`, `webaudio`. |
| [`BufferFrames`](#m-bufferframes) | The device buffer in frames. |
| [`Detail`](#m-detail) | Platform extras (route, session, autoplay), or null. |
| [`Device`](#m-device) | The device name asked for; null for the system default. |
| [`DeviceState`](#m-devicestate) | The device's own state from the sink, or null for sinks that don't say. |
| [`DeviceUnderruns`](#m-deviceunderruns) | Underruns the device counted itself. |
| [`Error`](#m-error) | The last reason a device failed to open, or null. |
| [`FramesPlayed`](#m-framesplayed) | Frames the device took. |
| [`FramesRendered`](#m-framesrendered) | Frames the mixer produced. |
| [`FramesWritten`](#m-frameswritten) | Frames handed to the device. |
| [`LatencyMs`](#m-latencyms) | Audio queued in the device after the last write, in milliseconds. |
| [`Losses`](#m-losses) | Times the device was lost. |
| [`Paused`](#m-paused) | Whether the output is feeding silence (focus loss or pause). |
| [`PeakPlayed`](#m-peakplayed) | Highest absolute sample among frames the device took. |
| [`PeriodFrames`](#m-periodframes) | The write size in frames. |
| [`Reopens`](#m-reopens) | Times a device was opened again. |
| [`Request`](#m-request) | What a device request in the same `audio` call came to (unlock, interrupt, suspend), or null. |
| [`State`](#m-state) | The output's state (`Running`, `NoDevice`, ...). |
| [`Suspended`](#m-suspended) | Whether the app lifecycle has the device stopped. |
| [`Suspensions`](#m-suspensions) | Times the lifecycle stopped the device. |
| [`Underruns`](#m-underruns) | Times the device ran dry, as seen by the output. |

### <a id="m-audiothreadallocatedbytes"></a>AudioThreadAllocatedBytes

```csharp
public long AudioThreadAllocatedBytes { get; init; }
```

Bytes the audio thread allocated since warm-up. Expected 0.

**Type:** `long`

### <a id="m-backend"></a>Backend

```csharp
public string Backend { get; init; }
```

The operating-system API, such as `aaudio`, `audiounit`, `webaudio`.

**Type:** `string`

### <a id="m-bufferframes"></a>BufferFrames

```csharp
public int BufferFrames { get; init; }
```

The device buffer in frames.

**Type:** `int`

### <a id="m-detail"></a>Detail

```csharp
public string? Detail { get; init; }
```

Platform extras (route, session, autoplay), or null.

**Type:** `string?`

### <a id="m-device"></a>Device

```csharp
public string? Device { get; init; }
```

The device name asked for; null for the system default.

**Type:** `string?`

### <a id="m-devicestate"></a>DeviceState

```csharp
public string? DeviceState { get; init; }
```

The device's own state from the sink, or null for sinks that don't say.

**Type:** `string?`

### <a id="m-deviceunderruns"></a>DeviceUnderruns

```csharp
public long DeviceUnderruns { get; init; }
```

Underruns the device counted itself.

**Type:** `long`

### <a id="m-error"></a>Error

```csharp
public string? Error { get; init; }
```

The last reason a device failed to open, or null.

**Type:** `string?`

### <a id="m-framesplayed"></a>FramesPlayed

```csharp
public long FramesPlayed { get; init; }
```

Frames the device took.

**Type:** `long`

### <a id="m-framesrendered"></a>FramesRendered

```csharp
public long FramesRendered { get; init; }
```

Frames the mixer produced.

**Type:** `long`

### <a id="m-frameswritten"></a>FramesWritten

```csharp
public long FramesWritten { get; init; }
```

Frames handed to the device.

**Type:** `long`

### <a id="m-latencyms"></a>LatencyMs

```csharp
public double LatencyMs { get; init; }
```

Audio queued in the device after the last write, in milliseconds.

**Type:** `double`

### <a id="m-losses"></a>Losses

```csharp
public long Losses { get; init; }
```

Times the device was lost.

**Type:** `long`

### <a id="m-paused"></a>Paused

```csharp
public bool Paused { get; init; }
```

Whether the output is feeding silence (focus loss or pause).

**Type:** `bool`

### <a id="m-peakplayed"></a>PeakPlayed

```csharp
public float PeakPlayed { get; init; }
```

Highest absolute sample among frames the device took.

**Type:** `float`

### <a id="m-periodframes"></a>PeriodFrames

```csharp
public int PeriodFrames { get; init; }
```

The write size in frames.

**Type:** `int`

### <a id="m-reopens"></a>Reopens

```csharp
public long Reopens { get; init; }
```

Times a device was opened again.

**Type:** `long`

### <a id="m-request"></a>Request

```csharp
public string? Request { get; init; }
```

What a device request in the same `audio` call came to (unlock, interrupt, suspend), or null.

**Type:** `string?`

### <a id="m-state"></a>State

```csharp
public string State { get; init; }
```

The output's state (`Running`, `NoDevice`, ...).

**Type:** `string`

### <a id="m-suspended"></a>Suspended

```csharp
public bool Suspended { get; init; }
```

Whether the app lifecycle has the device stopped.

**Type:** `bool`

### <a id="m-suspensions"></a>Suspensions

```csharp
public long Suspensions { get; init; }
```

Times the lifecycle stopped the device.

**Type:** `long`

### <a id="m-underruns"></a>Underruns

```csharp
public long Underruns { get; init; }
```

Times the device ran dry, as seen by the output.

**Type:** `long`
