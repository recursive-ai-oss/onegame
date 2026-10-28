[API reference](../../index.md) › [OneGame.Audio](../index.md) › OneGame.Audio

# OneAudioCore.Factory class

**Namespace:** `OneGame.Audio`  
**Package:** [`RecursiveAI.OneGame.Audio`](../index.md)  
**Declared in:** [`OneAudioCore`](OneAudioCore.md)  
**Implements:** [`IAudioCoreFactory`](IAudioCoreFactory.md)

```csharp
public sealed class OneAudioCore.Factory : IAudioCoreFactory
```

Builds [`OneAudioCore`](OneAudioCore.md)s; the default for [`AudioServiceOptions.CoreFactory`](AudioServiceOptions.md#m-corefactory).

## Constructors

| Member | Description |
| --- | --- |
| [`Factory(double)`](#m-ctor-double) | Builds [`OneAudioCore`](OneAudioCore.md)s; the default for [`AudioServiceOptions.CoreFactory`](AudioServiceOptions.md#m-corefactory). |

### <a id="m-ctor-double"></a>Factory(double)

```csharp
public Factory(double maxClipSeconds = 120);
```

Builds [`OneAudioCore`](OneAudioCore.md)s; the default for [`AudioServiceOptions.CoreFactory`](AudioServiceOptions.md#m-corefactory).

**Parameters**

- `maxClipSeconds` (`double`): The longest clip decoded into memory.

## Methods

| Member | Description |
| --- | --- |
| [`Create(AudioCoreSettings)`](#m-create-audiocoresettings) | Builds a core with the full settings. |
| [`Create(int, int, int, int)`](#m-create-int-int-int-int) | Builds a core that renders at the given format. |

### <a id="m-create-audiocoresettings"></a>Create(AudioCoreSettings)

```csharp
public IAudioCore Create(AudioCoreSettings settings);
```

Builds a core with the full settings. The default ignores the effect settings and calls the four-number overload.

**Parameters**

- `settings` ([`AudioCoreSettings`](AudioCoreSettings.md)): The format, sizes and effect capacity.

**Returns:** [`IAudioCore`](IAudioCore.md) — The core.

### <a id="m-create-int-int-int-int"></a>Create(int, int, int, int)

```csharp
public IAudioCore Create(int sampleRate, int channels, int maxVoices, int buses);
```

Builds a core that renders at the given format.

**Parameters**

- `sampleRate` (`int`): Frames per second.
- `channels` (`int`): Samples per frame.
- `maxVoices` (`int`): The most simultaneous voices.
- `buses` (`int`): The number of buses.

**Returns:** [`IAudioCore`](IAudioCore.md) — The core.
