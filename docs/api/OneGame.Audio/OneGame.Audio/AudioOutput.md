[API reference](../../index.md) › [OneGame.Audio](../index.md) › OneGame.Audio

# AudioOutput class

**Namespace:** `OneGame.Audio`  
**Package:** [`RecursiveAI.OneGame.Audio`](../index.md)  
**Implements:** [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class AudioOutput : IDisposable
```

The real-time side of audio: a thread pulls frames from an [`IAudioRenderSource`](IAudioRenderSource.md), limits them and writes them to an [`IAudioSink`](IAudioSink.md). After construction the thread allocates nothing, takes no lock shared with the game thread and lets no exception out. A lost device drops to a silent, real-time-paced mode and is reopened at the same format when it comes back.

## Properties

| Member | Description |
| --- | --- |
| [`AudioThreadAllocatedBytes`](#m-audiothreadallocatedbytes) | Bytes the audio thread allocated after its first few blocks, not counting device opens. |
| [`BackendName`](#m-backendname) | The backend's short name, such as `alsa` or `aaudio`. |
| [`BufferFrames`](#m-bufferframes) | The device's buffer in frames, or 0 with no device. |
| [`DeviceLosses`](#m-devicelosses) | Times an open device was lost. |
| [`DeviceName`](#m-devicename) | The device name in use or being retried; `null` for the system default. |
| [`Format`](#m-format) | The format the mixer must render at. |
| [`FramesRendered`](#m-framesrendered) | Frames produced so far, to a device or not. |
| [`IsDeviceStopped`](#m-isdevicestopped) | Whether the device is stopped, by [`AudioOutput.Suspended`](AudioOutput.md#m-suspended) or because the system took it (a phone call, another app's audio). |
| [`IsPumped`](#m-ispumped) | Whether the page drives this output with [`AudioOutput.Pump()`](AudioOutput.md#m-pump) (no audio thread). |
| [`LastError`](#m-lasterror) | The most recent reason a device failed to open, or `null`. |
| [`LatencyMilliseconds`](#m-latencymilliseconds) | Output latency in milliseconds: audio queued in the device right after a write. |
| [`Loudness`](#m-loudness) | The loudness at the output, after the limiter. |
| [`Metering`](#m-metering) | Whether the output measures loudness. |
| [`Paused`](#m-paused) | Stops sound without stopping the device: it is fed silence and the source is not rendered, so the mixer's timeline holds still. |
| [`PeriodFrames`](#m-periodframes) | The device's write size in frames, or 0 with no device. |
| [`RenderFaults`](#m-renderfaults) | Exceptions the render source threw. |
| [`Reopens`](#m-reopens) | Times a device was opened again after a loss, a failed open or a device change. |
| [`SilentFrames`](#m-silentframes) | Frames produced while no device was open. |
| [`Status`](#m-status) | The current state. |
| [`Suspended`](#m-suspended) | Stops the device, as an app does when it leaves the foreground: the mixer is not rendered (its clock holds still), the device is stopped if its sink supports that, and nothing is written until this is cleared again. |
| [`Suspensions`](#m-suspensions) | Times [`AudioOutput.Suspended`](AudioOutput.md#m-suspended) was set. |
| [`Underruns`](#m-underruns) | Times the device ran dry (the game could not keep up). |

### <a id="m-audiothreadallocatedbytes"></a>AudioThreadAllocatedBytes

```csharp
public long AudioThreadAllocatedBytes { get; }
```

Bytes the audio thread allocated after its first few blocks, not counting device opens. Expected 0.

**Type:** `long`

### <a id="m-backendname"></a>BackendName

```csharp
public string BackendName { get; }
```

The backend's short name, such as `alsa` or `aaudio`.

**Type:** `string`

### <a id="m-bufferframes"></a>BufferFrames

```csharp
public int BufferFrames { get; }
```

The device's buffer in frames, or 0 with no device.

**Type:** `int`

### <a id="m-devicelosses"></a>DeviceLosses

```csharp
public long DeviceLosses { get; }
```

Times an open device was lost.

**Type:** `long`

### <a id="m-devicename"></a>DeviceName

```csharp
public string? DeviceName { get; }
```

The device name in use or being retried; `null` for the system default.

**Type:** `string?`

### <a id="m-format"></a>Format

```csharp
public AudioDeviceFormat Format { get; }
```

The format the mixer must render at. Fixed once [`AudioOutput.Start()`](AudioOutput.md#m-start-iaudiorendersource) is called, including across device loss.

**Type:** [`AudioDeviceFormat`](AudioDeviceFormat.md)

### <a id="m-framesrendered"></a>FramesRendered

```csharp
public long FramesRendered { get; }
```

Frames produced so far, to a device or not.

**Type:** `long`

### <a id="m-isdevicestopped"></a>IsDeviceStopped

```csharp
public bool IsDeviceStopped { get; }
```

Whether the device is stopped, by [`AudioOutput.Suspended`](AudioOutput.md#m-suspended) or because the system took it (a phone call, another app's audio).

**Type:** `bool`

### <a id="m-ispumped"></a>IsPumped

```csharp
public bool IsPumped { get; }
```

Whether the page drives this output with [`AudioOutput.Pump()`](AudioOutput.md#m-pump) (no audio thread).

**Type:** `bool`

### <a id="m-lasterror"></a>LastError

```csharp
public string? LastError { get; }
```

The most recent reason a device failed to open, or `null`.

**Type:** `string?`

### <a id="m-latencymilliseconds"></a>LatencyMilliseconds

```csharp
public double LatencyMilliseconds { get; }
```

Output latency in milliseconds: audio queued in the device right after a write. 0 with no device.

**Type:** `double`

### <a id="m-loudness"></a>Loudness

```csharp
public LoudnessReading Loudness { get; }
```

The loudness at the output, after the limiter. Safe from any thread; updated once per block while sound is rendering (not while paused).

**Type:** [`LoudnessReading`](LoudnessReading.md)

### <a id="m-metering"></a>Metering

```csharp
public bool Metering { get; set; }
```

Whether the output measures loudness. Set before [`AudioOutput.Start()`](AudioOutput.md#m-start-iaudiorendersource); off by default for an output built directly.

**Type:** `bool`

### <a id="m-paused"></a>Paused

```csharp
public bool Paused { get; set; }
```

Stops sound without stopping the device: it is fed silence and the source is not rendered, so the mixer's timeline holds still.

**Type:** `bool`

### <a id="m-periodframes"></a>PeriodFrames

```csharp
public int PeriodFrames { get; }
```

The device's write size in frames, or 0 with no device.

**Type:** `int`

### <a id="m-renderfaults"></a>RenderFaults

```csharp
public long RenderFaults { get; }
```

Exceptions the render source threw. The block is replaced by silence. Expected 0.

**Type:** `long`

### <a id="m-reopens"></a>Reopens

```csharp
public long Reopens { get; }
```

Times a device was opened again after a loss, a failed open or a device change.

**Type:** `long`

### <a id="m-silentframes"></a>SilentFrames

```csharp
public long SilentFrames { get; }
```

Frames produced while no device was open.

**Type:** `long`

### <a id="m-status"></a>Status

```csharp
public AudioOutputStatus Status { get; }
```

The current state.

**Type:** [`AudioOutputStatus`](AudioOutputStatus.md)

### <a id="m-suspended"></a>Suspended

```csharp
public bool Suspended { get; set; }
```

Stops the device, as an app does when it leaves the foreground: the mixer is not rendered (its clock holds still), the device is stopped if its sink supports that, and nothing is written until this is cleared again.

**Type:** `bool`

### <a id="m-suspensions"></a>Suspensions

```csharp
public long Suspensions { get; }
```

Times [`AudioOutput.Suspended`](AudioOutput.md#m-suspended) was set.

**Type:** `long`

### <a id="m-underruns"></a>Underruns

```csharp
public long Underruns { get; }
```

Times the device ran dry (the game could not keep up). Expected 0.

**Type:** `long`

## Methods

| Member | Description |
| --- | --- |
| [`Advance(int)`](#m-advance-int) | Renders exactly `frames` frames through the limiter into the sink on the calling thread. |
| [`Create(IAudioBackend, AudioDeviceOptions, OutputLimiterMode)`](#m-create-iaudiobackend-audiodeviceoptions-outputlimitermode) | Opens the device and returns the output. |
| [`DeviceStatus()`](#m-devicestatus) | Describes the device side for the harness: backend, the device's own state, counters, and what the device has taken from us. |
| [`Dispose()`](#m-dispose) | Stops the thread and closes the device. |
| [`Pump()`](#m-pump) | For pumped backends (a web page, which has no audio thread): renders as many blocks as the device wants right now. |
| [`ResetLoudness()`](#m-resetloudness) | Clears the meter's history (integrated loudness, true peak) at the next block, so a new measurement starts clean. |
| [`SelectDevice(string?)`](#m-selectdevice-string) | Asks the audio thread to switch to another device at the same format. |
| [`Start(IAudioRenderSource)`](#m-start-iaudiorendersource) | Attaches the source and, for a real device, starts the audio thread. |

### <a id="m-advance-int"></a>Advance(int)

```csharp
public void Advance(int frames);
```

Renders exactly `frames` frames through the limiter into the sink on the calling thread. For manual backends such as the capture sink, where tests and the harness control time.

**Parameters**

- `frames` (`int`): Frames to produce.

**Exceptions**

- [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception): The backend is not manual, or no source is attached.

### <a id="m-create-iaudiobackend-audiodeviceoptions-outputlimitermode"></a>Create(IAudioBackend, AudioDeviceOptions, OutputLimiterMode)

```csharp
public static AudioOutput Create(IAudioBackend backend, AudioDeviceOptions options, OutputLimiterMode limiter = 1);
```

Opens the device and returns the output. When no device opens, the output still exists and is silent (see [`AudioOutput.Status`](AudioOutput.md#m-status)); it never throws for a missing device.

**Parameters**

- `backend` ([`IAudioBackend`](IAudioBackend.md)): The audio system to use.
- `options` ([`AudioDeviceOptions`](AudioDeviceOptions.md)): What to ask of the device.
- `limiter` ([`OutputLimiterMode`](OutputLimiterMode.md)): How to keep samples in range.

**Returns:** [`AudioOutput`](AudioOutput.md) — The output, not yet started.

**Exceptions**

- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): The rate is outside 8000..384000 or the channel count outside 1..16.

### <a id="m-devicestatus"></a>DeviceStatus()

```csharp
public AudioDeviceStatus DeviceStatus();
```

Describes the device side for the harness: backend, the device's own state, counters, and what the device has taken from us.

**Returns:** [`AudioDeviceStatus`](AudioDeviceStatus.md) — A snapshot.

### <a id="m-dispose"></a>Dispose()

```csharp
public void Dispose();
```

Stops the thread and closes the device.

### <a id="m-pump"></a>Pump()

```csharp
public void Pump();
```

For pumped backends (a web page, which has no audio thread): renders as many blocks as the device wants right now. The page calls this from a timer; it does nothing for other backends.

### <a id="m-resetloudness"></a>ResetLoudness()

```csharp
public void ResetLoudness();
```

Clears the meter's history (integrated loudness, true peak) at the next block, so a new measurement starts clean.

### <a id="m-selectdevice-string"></a>SelectDevice(string?)

```csharp
public void SelectDevice(string? deviceName);
```

Asks the audio thread to switch to another device at the same format. Takes effect within a block or two; if the new device fails to open the output goes silent and keeps retrying it.

**Parameters**

- `deviceName` (`string?`): A name from [`IAudioBackend.ListDevices()`](IAudioBackend.md#m-listdevices), or `null` for the system default.

### <a id="m-start-iaudiorendersource"></a>Start(IAudioRenderSource)

```csharp
public void Start(IAudioRenderSource source);
```

Attaches the source and, for a real device, starts the audio thread.

**Parameters**

- `source` ([`IAudioRenderSource`](IAudioRenderSource.md)): The mixer. Its rate and channel count must equal [`AudioOutput.Format`](AudioOutput.md#m-format).

**Exceptions**

- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): The source's format differs from [`AudioOutput.Format`](AudioOutput.md#m-format).
- [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception): Already started or disposed.
