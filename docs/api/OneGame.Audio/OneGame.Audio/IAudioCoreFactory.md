[API reference](../../index.md) › [OneGame.Audio](../index.md) › OneGame.Audio

# IAudioCoreFactory interface

**Namespace:** `OneGame.Audio`  
**Package:** [`RecursiveAI.OneGame.Audio`](../index.md)

```csharp
public interface IAudioCoreFactory
```

Creates the mixer once the device has fixed the sample rate and channel count.

## Methods

| Member | Description |
| --- | --- |
| [`Create(AudioCoreSettings)`](#m-create-audiocoresettings) | Builds a core with the full settings. |
| [`Create(int, int, int, int)`](#m-create-int-int-int-int) | Builds a core that renders at the given format. |

### <a id="m-create-audiocoresettings"></a>Create(AudioCoreSettings)

```csharp
IAudioCore Create(AudioCoreSettings settings);
```

Builds a core with the full settings. The default ignores the effect settings and calls the four-number overload.

**Parameters**

- `settings` ([`AudioCoreSettings`](AudioCoreSettings.md)): The format, sizes and effect capacity.

**Returns:** [`IAudioCore`](IAudioCore.md) — The core.

### <a id="m-create-int-int-int-int"></a>Create(int, int, int, int)

```csharp
IAudioCore Create(int sampleRate, int channels, int maxVoices, int buses);
```

Builds a core that renders at the given format.

**Parameters**

- `sampleRate` (`int`): Frames per second.
- `channels` (`int`): Samples per frame.
- `maxVoices` (`int`): The most simultaneous voices.
- `buses` (`int`): The number of buses.

**Returns:** [`IAudioCore`](IAudioCore.md) — The core.
