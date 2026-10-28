[API reference](../../index.md) › [OneGame.Audio](../index.md) › OneGame.Audio

# AudioDeviceOptions record

**Namespace:** `OneGame.Audio`  
**Package:** [`RecursiveAI.OneGame.Audio`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`AudioDeviceOptions`](AudioDeviceOptions.md)&gt;

```csharp
public sealed record AudioDeviceOptions : IEquatable<AudioDeviceOptions>
```

What the game asks of an output device. The device may deliver a different buffer size; rate and channel count are always honoured (the backend converts when the hardware can't).

## Constructors

| Member | Description |
| --- | --- |
| [`AudioDeviceOptions()`](#m-ctor) |  |

### <a id="m-ctor"></a>AudioDeviceOptions()

```csharp
public AudioDeviceOptions();
```

## Properties

| Member | Description |
| --- | --- |
| [`Channels`](#m-channels) | Output channel count. |
| [`DeviceName`](#m-devicename) | Backend-specific device name from [`IAudioBackend.ListDevices()`](IAudioBackend.md#m-listdevices); `null` selects the system default. |
| [`LatencyMilliseconds`](#m-latencymilliseconds) | Requested total device buffer in milliseconds. |
| [`SampleRate`](#m-samplerate) | Preferred sample rate in Hz. |

### <a id="m-channels"></a>Channels

```csharp
public int Channels { get; init; }
```

Output channel count. Default 2.

**Type:** `int`

### <a id="m-devicename"></a>DeviceName

```csharp
public string? DeviceName { get; init; }
```

Backend-specific device name from [`IAudioBackend.ListDevices()`](IAudioBackend.md#m-listdevices); `null` selects the system default.

**Type:** `string?`

### <a id="m-latencymilliseconds"></a>LatencyMilliseconds

```csharp
public int LatencyMilliseconds { get; init; }
```

Requested total device buffer in milliseconds. Smaller is lower latency and more underrun risk. Default 40.

**Type:** `int`

### <a id="m-samplerate"></a>SampleRate

```csharp
public int SampleRate { get; init; }
```

Preferred sample rate in Hz. Default 48000.

**Type:** `int`
