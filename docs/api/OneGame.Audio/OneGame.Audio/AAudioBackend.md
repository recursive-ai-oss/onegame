[API reference](../../index.md) › [OneGame.Audio](../index.md) › OneGame.Audio

# AAudioBackend class

**Namespace:** `OneGame.Audio`  
**Package:** [`RecursiveAI.OneGame.Audio`](../index.md)  
**Implements:** [`IAudioBackend`](IAudioBackend.md), `IDevicesHolder`

```csharp
public sealed class AAudioBackend : IAudioBackend, IDevicesHolder
```

Android output through AAudio (Android 8.0 and later), provided by the device package: shared low-latency float streams driven by the system's data callback, with the device clock, xrun counts and reopening on a route change reported by the package.

## Constructors

| Member | Description |
| --- | --- |
| [`AAudioBackend()`](#m-ctor) |  |

### <a id="m-ctor"></a>AAudioBackend()

```csharp
public AAudioBackend();
```

## Properties

| Member | Description |
| --- | --- |
| [`IsManual`](#m-ismanual) | When true, nothing paces [`IAudioSink.Write()`](IAudioSink.md#m-write-readonlyspan-float) and [`AudioOutput`](AudioOutput.md) runs without a thread; the caller advances it with [`AudioOutput.Advance()`](AudioOutput.md#m-advance-int). |
| [`Name`](#m-name) | A short name for diagnostics, such as `alsa`. |

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

## Methods

| Member | Description |
| --- | --- |
| [`ListDevices()`](#m-listdevices) | Lists output devices. |
| [`TryOpen(AudioDeviceOptions, out IAudioSink?, out string)`](#m-tryopen-audiodeviceoptions-iaudiosink-string) | Opens a device. |

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
