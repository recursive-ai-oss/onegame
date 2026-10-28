[API reference](../../index.md) › [OneGame.Audio](../index.md) › OneGame.Audio

# AudioUnitBackend class

**Namespace:** `OneGame.Audio`  
**Package:** [`RecursiveAI.OneGame.Audio`](../index.md)  
**Implements:** [`IAudioBackend`](IAudioBackend.md), `IDevicesHolder`

```csharp
public sealed class AudioUnitBackend : IAudioBackend, IDevicesHolder
```

Apple output: RemoteIO on iOS (after the session is set up) and the default output unit on macOS, provided by the device package. On iOS the backend reacts to the session's interruption notifications (calls, Siri, alarms): the device stops, the mixer's clock holds still, and sound resumes where it was when the interruption ends. The app lifecycle stops and starts the device the same way.

## Constructors

| Member | Description |
| --- | --- |
| [`AudioUnitBackend(AudioSessionCategory)`](#m-ctor-audiosessioncategory) | Creates the backend. |

### <a id="m-ctor-audiosessioncategory"></a>AudioUnitBackend(AudioSessionCategory)

```csharp
public AudioUnitBackend(AudioSessionCategory category = 0);
```

Creates the backend.

**Parameters**

- `category` ([`AudioSessionCategory`](AudioSessionCategory.md)): The iOS session category; ignored on macOS.

## Properties

| Member | Description |
| --- | --- |
| [`Category`](#m-category) | The iOS session category this backend sets. |
| [`IsManual`](#m-ismanual) | When true, nothing paces [`IAudioSink.Write()`](IAudioSink.md#m-write-readonlyspan-float) and [`AudioOutput`](AudioOutput.md) runs without a thread; the caller advances it with [`AudioOutput.Advance()`](AudioOutput.md#m-advance-int). |
| [`Name`](#m-name) | A short name for diagnostics, such as `alsa`. |

### <a id="m-category"></a>Category

```csharp
public AudioSessionCategory Category { get; }
```

The iOS session category this backend sets.

**Type:** [`AudioSessionCategory`](AudioSessionCategory.md)

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
| [`SimulateInterruption(bool)`](#m-simulateinterruption-bool) | Posts the same notification the system sends when another app or a call takes the audio session (`AVAudioSessionInterruptionNotification`), so the whole interruption path runs without a phone call. iOS only. |
| [`TryOpen(AudioDeviceOptions, out IAudioSink?, out string)`](#m-tryopen-audiodeviceoptions-iaudiosink-string) | Opens a device. |

### <a id="m-listdevices"></a>ListDevices()

```csharp
public IReadOnlyList<AudioDeviceInfo> ListDevices();
```

Lists output devices. Returns an empty list when the audio system is unavailable.

**Returns:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`AudioDeviceInfo`](AudioDeviceInfo.md)&gt;

### <a id="m-simulateinterruption-bool"></a>SimulateInterruption(bool)

```csharp
public static bool SimulateInterruption(bool began);
```

Posts the same notification the system sends when another app or a call takes the audio session (`AVAudioSessionInterruptionNotification`), so the whole interruption path runs without a phone call. iOS only.

**Parameters**

- `began` (`bool`): `true` for the start of an interruption, `false` for its end.

**Returns:** `bool` — Whether the notification was posted.

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
