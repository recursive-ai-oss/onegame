[API reference](../../index.md) › [OneGame.Audio](../index.md) › OneGame.Audio

# CaptureAudioBackend class

**Namespace:** `OneGame.Audio`  
**Package:** [`RecursiveAI.OneGame.Audio`](../index.md)  
**Implements:** [`IAudioBackend`](IAudioBackend.md), `IDevicesHolder`, [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class CaptureAudioBackend : IAudioBackend, IDevicesHolder, IDisposable
```

A backend with no device: it records the samples the output stage would have sent to the speakers. It is manual, so time moves only when [`AudioOutput.Advance()`](AudioOutput.md#m-advance-int) is called, which makes captures exact and repeatable. Tests and the harness use it.

## Constructors

| Member | Description |
| --- | --- |
| [`CaptureAudioBackend()`](#m-ctor) | Creates the backend. |

### <a id="m-ctor"></a>CaptureAudioBackend()

```csharp
public CaptureAudioBackend();
```

Creates the backend.

## Properties

| Member | Description |
| --- | --- |
| [`IsManual`](#m-ismanual) | When true, nothing paces [`IAudioSink.Write()`](IAudioSink.md#m-write-readonlyspan-float) and [`AudioOutput`](AudioOutput.md) runs without a thread; the caller advances it with [`AudioOutput.Advance()`](AudioOutput.md#m-advance-int). |
| [`Name`](#m-name) | A short name for diagnostics, such as `alsa`. |
| [`Sink`](#m-sink) | The recording, or `null` before an output has been created on this backend. |

### <a id="m-ismanual"></a>IsManual

```csharp
public bool IsManual { get; }
```

When true, nothing paces [`IAudioSink.Write()`](IAudioSink.md#m-write-readonlyspan-float) and [`AudioOutput`](AudioOutput.md) runs without a thread; the caller advances it with [`AudioOutput.Advance()`](AudioOutput.md#m-advance-int).

**Type:** `bool`

### <a id="m-name"></a>Name

```csharp
public string Name { get; }
```

A short name for diagnostics, such as `alsa`.

**Type:** `string`

### <a id="m-sink"></a>Sink

```csharp
public CaptureSink? Sink { get; }
```

The recording, or `null` before an output has been created on this backend.

**Type:** [`CaptureSink`](CaptureSink.md)?

## Methods

| Member | Description |
| --- | --- |
| [`Dispose()`](#m-dispose) |  |
| [`ListDevices()`](#m-listdevices) | Lists output devices. |
| [`TryOpen(AudioDeviceOptions, out IAudioSink?, out string)`](#m-tryopen-audiodeviceoptions-iaudiosink-string) | Opens a device. |

### <a id="m-dispose"></a>Dispose()

```csharp
public void Dispose();
```

### <a id="m-listdevices"></a>ListDevices()

```csharp
public IReadOnlyList<AudioDeviceInfo> ListDevices();
```

Lists output devices. Returns an empty list when the audio system is unavailable.

**Returns:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`AudioDeviceInfo`](AudioDeviceInfo.md)&gt;

### <a id="m-tryopen-audiodeviceoptions-iaudiosink-string"></a>TryOpen(AudioDeviceOptions, out IAudioSink?, out string)

```csharp
public bool TryOpen(AudioDeviceOptions options, out IAudioSink? sink, out string reason);
```

Opens a device. Never throws: failure returns false with a reason.

**Parameters**

- `options` ([`AudioDeviceOptions`](AudioDeviceOptions.md))
- `sink` ([`IAudioSink`](IAudioSink.md)?)
- `reason` (`string`)

**Returns:** `bool`
