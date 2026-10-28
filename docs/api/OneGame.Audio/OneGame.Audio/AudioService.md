[API reference](../../index.md) › [OneGame.Audio](../index.md) › OneGame.Audio

# AudioService class

**Namespace:** `OneGame.Audio`  
**Package:** [`RecursiveAI.OneGame.Audio`](../index.md)  
**Implements:** [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class AudioService : IDisposable
```

The game-facing audio service: load clips, play sounds and music on buses, set volumes, crossfade, and pump events. Everything here is called from the game thread. It owns the device output and the mixer, and keeps working (silently) when there is no device.

## Properties

| Member | Description |
| --- | --- |
| [`Active`](#m-active) | The most recently created service that is still alive, so the harness can reach a game's audio without the game handing it over. |
| [`Capture`](#m-capture) | The recording, when created with [`AudioServiceOptions.Capture`](AudioServiceOptions.md#m-capture); otherwise `null`. |
| [`Core`](#m-core) | The mixer. |
| [`CurrentMusic`](#m-currentmusic) | The playing music voice, or an invalid id. |
| [`Format`](#m-format) | The format everything renders at: the device's own, or the requested one with no device. |
| [`FrameClock`](#m-frameclock) | Tells the log which game frame it is, so a test can ask for sounds played within a window of frames. |
| [`FreeBusMeters`](#m-freebusmeters) | Meter slots not yet used by a bus (see [`AudioServiceOptions.MaxBusMeters`](AudioServiceOptions.md#m-maxbusmeters)). |
| [`FreeReverbSlots`](#m-freereverbslots) | Reverbs that can still be put on buses that have none. |
| [`Loudness`](#m-loudness) | The loudness at the output after the limiter, as the speakers get it: momentary, short-term, gated integrated (EBU R 128) and true peak. |
| [`MasterVolume`](#m-mastervolume) | The master volume, 0 and up; scales every bus. |
| [`Muted`](#m-muted) | Silences everything without changing any volume. |
| [`Output`](#m-output) | The device output, for status, latency and counters. |
| [`Paused`](#m-paused) | Stops sound and holds the mixer's clock, for a pause menu or a lost window. |

### <a id="m-active"></a>Active

```csharp
public static AudioService? Active { get; }
```

The most recently created service that is still alive, so the harness can reach a game's audio without the game handing it over. `null` when none.

**Type:** [`AudioService`](AudioService.md)?

### <a id="m-capture"></a>Capture

```csharp
public CaptureSink? Capture { get; }
```

The recording, when created with [`AudioServiceOptions.Capture`](AudioServiceOptions.md#m-capture); otherwise `null`.

**Type:** [`CaptureSink`](CaptureSink.md)?

### <a id="m-core"></a>Core

```csharp
public IAudioCore Core { get; }
```

The mixer.

**Type:** [`IAudioCore`](IAudioCore.md)

### <a id="m-currentmusic"></a>CurrentMusic

```csharp
public VoiceId CurrentMusic { get; }
```

The playing music voice, or an invalid id.

**Type:** [`VoiceId`](VoiceId.md)

### <a id="m-format"></a>Format

```csharp
public AudioDeviceFormat Format { get; }
```

The format everything renders at: the device's own, or the requested one with no device.

**Type:** [`AudioDeviceFormat`](AudioDeviceFormat.md)

### <a id="m-frameclock"></a>FrameClock

```csharp
public Func<long>? FrameClock { get; set; }
```

Tells the log which game frame it is, so a test can ask for sounds played within a window of frames. The control layer binds it to the game's frame counter when the game's control surface is created or given the service (`ControlSurface.Audio`); a game never needs to. Sounds played while no clock is bound are stamped -1.

**Type:** [`Func`](https://learn.microsoft.com/dotnet/api/system.func-1)`<long>?`

### <a id="m-freebusmeters"></a>FreeBusMeters

```csharp
public int FreeBusMeters { get; }
```

Meter slots not yet used by a bus (see [`AudioServiceOptions.MaxBusMeters`](AudioServiceOptions.md#m-maxbusmeters)).

**Type:** `int`

### <a id="m-freereverbslots"></a>FreeReverbSlots

```csharp
public int FreeReverbSlots { get; }
```

Reverbs that can still be put on buses that have none.

**Type:** `int`

### <a id="m-loudness"></a>Loudness

```csharp
public LoudnessReading Loudness { get; }
```

The loudness at the output after the limiter, as the speakers get it: momentary, short-term, gated integrated (EBU R 128) and true peak. All -infinity until sound has rendered, and always so when [`AudioServiceOptions.Metering`](AudioServiceOptions.md#m-metering) is off.

**Type:** [`LoudnessReading`](LoudnessReading.md)

### <a id="m-mastervolume"></a>MasterVolume

```csharp
public float MasterVolume { get; set; }
```

The master volume, 0 and up; scales every bus.

**Type:** `float`

### <a id="m-muted"></a>Muted

```csharp
public bool Muted { get; set; }
```

Silences everything without changing any volume.

**Type:** `bool`

### <a id="m-output"></a>Output

```csharp
public AudioOutput Output { get; }
```

The device output, for status, latency and counters.

**Type:** [`AudioOutput`](AudioOutput.md)

### <a id="m-paused"></a>Paused

```csharp
public bool Paused { get; set; }
```

Stops sound and holds the mixer's clock, for a pause menu or a lost window. Voices resume where they were.

**Type:** `bool`

## Events

| Member | Description |
| --- | --- |
| [`EventRaised`](#m-eventraised) | Raised from [`AudioService.Update()`](AudioService.md#m-update) for each event the audio thread produced. |

### <a id="m-eventraised"></a>EventRaised

```csharp
public event Action<AudioEvent>? EventRaised;
```

Raised from [`AudioService.Update()`](AudioService.md#m-update) for each event the audio thread produced.

**Type:** [`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)&lt;[`AudioEvent`](AudioEvent.md)&gt;?

## Methods

| Member | Description |
| --- | --- |
| [`Advance(int)`](#m-advance-int) | Capture mode only: feeds streams, then renders exactly `frames` frames into [`AudioService.Capture`](AudioService.md#m-capture). |
| [`ClearBusFilter(AudioBus, AudioFilterKind, double)`](#m-clearbusfilter-audiobus-audiofilterkind-double) | Turns a bus filter off. |
| [`ClearBusReverb(AudioBus)`](#m-clearbusreverb-audiobus) | Removes a bus's reverb. |
| [`ClearDucking(AudioBus)`](#m-clearducking-audiobus) | Removes the ducking of a bus. |
| [`ClearPlayLog()`](#m-clearplaylog) | Empties the log. |
| [`ClearVoiceFilter(VoiceId, AudioFilterKind, double)`](#m-clearvoicefilter-voiceid-audiofilterkind-double) | Turns a voice filter off. |
| [`Create(AudioServiceOptions?)`](#m-create-audioserviceoptions) | Opens the device, builds the mixer at the device's format and starts audio. |
| [`DefaultBackend()`](#m-defaultbackend) | The backend for this operating system, or one that never opens a device. |
| [`Describe()`](#m-describe) | Describes the mixing state: every bus (volume, mute, filters, reverb, ducking), the master limiter, the loudness and the music track. |
| [`DisableBusMeter(AudioBus)`](#m-disablebusmeter-audiobus) | Stops measuring a bus and frees its meter slot. |
| [`Dispose()`](#m-dispose) | Stops audio, the pump thread and the device. |
| [`EnableBusMeter(AudioBus)`](#m-enablebusmeter-audiobus) | Starts measuring a bus's loudness (EBU R 128) so [`AudioService.GetBusLoudness()`](AudioService.md#m-getbusloudness-audiobus) can read it. |
| [`GetBusLoudness(AudioBus)`](#m-getbusloudness-audiobus) | Reads a bus's loudness: momentary, short-term, gated integrated and true peak, with silence as -infinity (as [`AudioService.Loudness`](AudioService.md#m-loudness)). |
| [`GetBusVolume(AudioBus)`](#m-getbusvolume-audiobus) | Reads back a bus's volume. |
| [`GetPlayLog()`](#m-getplaylog) | The sounds played so far, oldest first, up to the last 4096. |
| [`GetStreamPosition(VoiceId)`](#m-getstreamposition-voiceid) | Where a streamed track is. |
| [`IsBusMetered(AudioBus)`](#m-isbusmetered-audiobus) | Whether a meter is assigned to a bus. |
| [`LoadSound(Stream, string)`](#m-loadsound-stream-string) | Decodes a whole audio file into memory. |
| [`Play(Sound, PlayOptions)`](#m-play-sound-playoptions) | Starts a sound. |
| [`PlayMusic(Stream, MusicOptions, string)`](#m-playmusic-stream-musicoptions-string) | Starts a streamed music track on the music bus. |
| [`ResetLoudness()`](#m-resetloudness) | Starts the integrated loudness and true peak over, for measuring a scene from its beginning. |
| [`SetBusFilter(AudioBus, AudioFilterKind, float, float, double)`](#m-setbusfilter-audiobus-audiofilterkind-float-float-double) | Turns on or retunes a low-pass or high-pass filter (12 dB per octave) on a bus. |
| [`SetBusMuted(AudioBus, bool)`](#m-setbusmuted-audiobus-bool) | Mutes or unmutes a bus without changing its volume. |
| [`SetBusReverb(AudioBus, ReverbOptions, double)`](#m-setbusreverb-audiobus-reverboptions-double) | Puts a reverb on a bus, or retunes the one there. |
| [`SetBusVolume(AudioBus, float)`](#m-setbusvolume-audiobus-float) | Sets a bus's volume (linear, 0 and up). |
| [`SetDucking(AudioBus, AudioBus, DuckingOptions)`](#m-setducking-audiobus-audiobus-duckingoptions) | Turns `target` down while `watched` is loud, for example music under speech. |
| [`SetFocused(bool)`](#m-setfocused-bool) | Tells the service whether the game window has focus. |
| [`SetMasterLimiter(LimiterOptions?)`](#m-setmasterlimiter-limiteroptions) | Retunes the master limiter, or switches it off with `null`. |
| [`SetPan(VoiceId, float)`](#m-setpan-voiceid-float) | Changes a playing voice's stereo position. |
| [`SetPitch(VoiceId, float, double)`](#m-setpitch-voiceid-float-double) | Changes a playing voice's speed and pitch. |
| [`SetSuspended(bool)`](#m-setsuspended-bool) | Tells the service the app left (`true`) or returned to the foreground. |
| [`SetVoiceFilter(VoiceId, AudioFilterKind, float, float, double)`](#m-setvoicefilter-voiceid-audiofilterkind-float-float-double) | Filters one voice before its volume and pan, for example a muffling low-pass on a sound behind a wall. |
| [`SetVolume(VoiceId, float, double)`](#m-setvolume-voiceid-float-double) | Changes a playing voice's volume. |
| [`Stop(VoiceId, double)`](#m-stop-voiceid-double) | Stops a voice. |
| [`StopAll(double)`](#m-stopall-double) | Stops every voice, music included. |
| [`StopMusic(double)`](#m-stopmusic-double) | Fades out and stops the current music. |
| [`TryEnableBusMeter(AudioBus)`](#m-tryenablebusmeter-audiobus) | Like [`AudioService.EnableBusMeter()`](AudioService.md#m-enablebusmeter-audiobus), and says why a meter did not start. |
| [`Update()`](#m-update) | Call once per game frame: drains the mixer's events, forgets a music track that ended and raises [`AudioService.EventRaised`](AudioService.md#m-eventraised). |

### <a id="m-advance-int"></a>Advance(int)

```csharp
public void Advance(int frames);
```

Capture mode only: feeds streams, then renders exactly `frames` frames into [`AudioService.Capture`](AudioService.md#m-capture).

**Parameters**

- `frames` (`int`): Frames to render.

**Exceptions**

- [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception): The service is not in capture mode.

### <a id="m-clearbusfilter-audiobus-audiofilterkind-double"></a>ClearBusFilter(AudioBus, AudioFilterKind, double)

```csharp
public void ClearBusFilter(AudioBus bus, AudioFilterKind kind, double glideSeconds = 0.02);
```

Turns a bus filter off.

**Parameters**

- `bus` ([`AudioBus`](AudioBus.md)): The bus.
- `kind` ([`AudioFilterKind`](AudioFilterKind.md)): Which filter.
- `glideSeconds` (`double`): Time to glide to transparent first. Default 0.02.

### <a id="m-clearbusreverb-audiobus"></a>ClearBusReverb(AudioBus)

```csharp
public void ClearBusReverb(AudioBus bus);
```

Removes a bus's reverb. The tail stops at once; to fade it, lower [`ReverbOptions.Wet`](ReverbOptions.md#m-wet) first.

**Parameters**

- `bus` ([`AudioBus`](AudioBus.md)): The bus.

### <a id="m-clearducking-audiobus"></a>ClearDucking(AudioBus)

```csharp
public void ClearDucking(AudioBus target);
```

Removes the ducking of a bus.

**Parameters**

- `target` ([`AudioBus`](AudioBus.md)): The bus.

### <a id="m-clearplaylog"></a>ClearPlayLog()

```csharp
public void ClearPlayLog();
```

Empties the log. Sequence numbers keep counting.

### <a id="m-clearvoicefilter-voiceid-audiofilterkind-double"></a>ClearVoiceFilter(VoiceId, AudioFilterKind, double)

```csharp
public void ClearVoiceFilter(VoiceId voice, AudioFilterKind kind, double glideSeconds = 0.02);
```

Turns a voice filter off.

**Parameters**

- `voice` ([`VoiceId`](VoiceId.md)): The voice.
- `kind` ([`AudioFilterKind`](AudioFilterKind.md)): Which filter.
- `glideSeconds` (`double`): Time to glide to transparent first. Default 0.02.

### <a id="m-create-audioserviceoptions"></a>Create(AudioServiceOptions?)

```csharp
public static AudioService Create(AudioServiceOptions? options = null);
```

Opens the device, builds the mixer at the device's format and starts audio. A missing or failing device is not an error: [`AudioOutput.Status`](AudioOutput.md#m-status) reports [`AudioOutputStatus.NoDevice`](AudioOutputStatus.md#m-nodevice) and the game runs silently.

**Parameters**

- `options` ([`AudioServiceOptions`](AudioServiceOptions.md)?): Settings; defaults are fine for most games.

**Returns:** [`AudioService`](AudioService.md) — The service.

**Exceptions**

- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): A setting is out of range.

### <a id="m-defaultbackend"></a>DefaultBackend()

```csharp
public static IAudioBackend DefaultBackend();
```

The backend for this operating system, or one that never opens a device.

**Returns:** [`IAudioBackend`](IAudioBackend.md) — A backend.

### <a id="m-describe"></a>Describe()

```csharp
public AudioStatus Describe();
```

Describes the mixing state: every bus (volume, mute, filters, reverb, ducking), the master limiter, the loudness and the music track. This is what the harness reports.

**Returns:** [`AudioStatus`](AudioStatus.md) — A snapshot.

### <a id="m-disablebusmeter-audiobus"></a>DisableBusMeter(AudioBus)

```csharp
public bool DisableBusMeter(AudioBus bus);
```

Stops measuring a bus and frees its meter slot.

**Parameters**

- `bus` ([`AudioBus`](AudioBus.md)): The bus.

**Returns:** `bool` — False when the command queue was full (the meter keeps running).

**Exceptions**

- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): The bus is out of range.

### <a id="m-dispose"></a>Dispose()

```csharp
public void Dispose();
```

Stops audio, the pump thread and the device.

### <a id="m-enablebusmeter-audiobus"></a>EnableBusMeter(AudioBus)

```csharp
public bool EnableBusMeter(AudioBus bus);
```

Starts measuring a bus's loudness (EBU R 128) so [`AudioService.GetBusLoudness()`](AudioService.md#m-getbusloudness-audiobus) can read it. The music, sfx and ui buses are metered at startup; use this for others or after [`AudioService.DisableBusMeter()`](AudioService.md#m-disablebusmeter-audiobus). A bus is measured as it leaves the bus: after its sounds, filters, reverb, volume and mute, but before ducking and before the master volume and limiter, so the reading is what the bus itself puts out.

**Parameters**

- `bus` ([`AudioBus`](AudioBus.md)): The bus.

**Returns:** `bool` — True when the bus is metered or is about to be. False when no meter slot is free (see [`AudioService.FreeBusMeters`](AudioService.md#m-freebusmeters)), metering is off or the sample rate is too low, or the command queue was full; nothing changed then. It never throws for a lack of slots. [`AudioService.TryEnableBusMeter()`](AudioService.md#m-tryenablebusmeter-audiobus) says which.

**Exceptions**

- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): The bus is out of range.

### <a id="m-getbusloudness-audiobus"></a>GetBusLoudness(AudioBus)

```csharp
public LoudnessReading? GetBusLoudness(AudioBus bus);
```

Reads a bus's loudness: momentary, short-term, gated integrated and true peak, with silence as -infinity (as [`AudioService.Loudness`](AudioService.md#m-loudness)). Ducking, the master volume and the limiter do not change it; use [`AudioService.Loudness`](AudioService.md#m-loudness) for what the speakers get.

**Parameters**

- `bus` ([`AudioBus`](AudioBus.md)): The bus.

**Returns:** [`LoudnessReading`](LoudnessReading.md)? — The reading, or `null` when the bus is not metered (yet: a meter starts at the next block).

**Exceptions**

- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): The bus is out of range.

### <a id="m-getbusvolume-audiobus"></a>GetBusVolume(AudioBus)

```csharp
public float GetBusVolume(AudioBus bus);
```

Reads back a bus's volume.

**Parameters**

- `bus` ([`AudioBus`](AudioBus.md)): The bus.

**Returns:** `float` — The volume set by [`AudioService.SetBusVolume()`](AudioService.md#m-setbusvolume-audiobus-float).

### <a id="m-getplaylog"></a>GetPlayLog()

```csharp
public IReadOnlyList<PlayedSound> GetPlayLog();
```

The sounds played so far, oldest first, up to the last 4096. Works with every backend, with or without a device, so tests can check what a game played without listening.

**Returns:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`PlayedSound`](PlayedSound.md)&gt; — A copy of the log.

### <a id="m-getstreamposition-voiceid"></a>GetStreamPosition(VoiceId)

```csharp
public StreamPosition? GetStreamPosition(VoiceId voice);
```

Where a streamed track is. Positions are source time, ahead of the speakers by the mixer's and device's buffering.

**Parameters**

- `voice` ([`VoiceId`](VoiceId.md)): A voice from [`AudioService.PlayMusic()`](AudioService.md#m-playmusic-stream-musicoptions-string).

**Returns:** [`StreamPosition`](StreamPosition.md)? — The position and length, or `null` when the voice is not a live stream.

### <a id="m-isbusmetered-audiobus"></a>IsBusMetered(AudioBus)

```csharp
public bool IsBusMetered(AudioBus bus);
```

Whether a meter is assigned to a bus. A reading follows at the next block.

**Parameters**

- `bus` ([`AudioBus`](AudioBus.md)): The bus.

**Returns:** `bool` — True when the bus is metered or its meter is about to start.

**Exceptions**

- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): The bus is out of range.

### <a id="m-loadsound-stream-string"></a>LoadSound(Stream, string)

```csharp
public Sound LoadSound(Stream data, string name = "sound");
```

Decodes a whole audio file into memory.

**Parameters**

- `data` ([`Stream`](https://learn.microsoft.com/dotnet/api/system.io.stream)): The encoded file; read to the end and not kept.
- `name` (`string`): A label for diagnostics.

**Returns:** [`Sound`](Sound.md) — The clip.

### <a id="m-play-sound-playoptions"></a>Play(Sound, PlayOptions)

```csharp
public VoiceId Play(Sound sound, PlayOptions options = default);
```

Starts a sound.

**Parameters**

- `sound` ([`Sound`](Sound.md)): A clip from [`AudioService.LoadSound()`](AudioService.md#m-loadsound-stream-string).
- `options` ([`PlayOptions`](PlayOptions.md)): How to play it; defaults to a one-shot on the sfx bus.

**Returns:** [`VoiceId`](VoiceId.md) — The voice, or an invalid id when the command queue was full (a dropped sound).

### <a id="m-playmusic-stream-musicoptions-string"></a>PlayMusic(Stream, MusicOptions, string)

```csharp
public VoiceId PlayMusic(Stream data, MusicOptions options = default, string name = "music");
```

Starts a streamed music track on the music bus. If music is already playing it is replaced, with a crossfade when asked for.

**Parameters**

- `data` ([`Stream`](https://learn.microsoft.com/dotnet/api/system.io.stream)): The encoded file; the stream is owned by the track and closed when it ends.
- `options` ([`MusicOptions`](MusicOptions.md)): Volume, looping and crossfade.
- `name` (`string`): The name the play log records for the track.

**Returns:** [`VoiceId`](VoiceId.md) — The voice, or an invalid id when the command queue was full.

### <a id="m-resetloudness"></a>ResetLoudness()

```csharp
public void ResetLoudness();
```

Starts the integrated loudness and true peak over, for measuring a scene from its beginning. Metered buses start over too.

### <a id="m-setbusfilter-audiobus-audiofilterkind-float-float-double"></a>SetBusFilter(AudioBus, AudioFilterKind, float, float, double)

```csharp
public void SetBusFilter(AudioBus bus, AudioFilterKind kind, float cutoffHz, float q = 0f, double glideSeconds = 0.02);
```

Turns on or retunes a low-pass or high-pass filter (12 dB per octave) on a bus. A low-pass and a high-pass together make a band-pass. Changes glide, so sweeps make no clicks.

**Parameters**

- `bus` ([`AudioBus`](AudioBus.md)): The bus.
- `kind` ([`AudioFilterKind`](AudioFilterKind.md)): Which filter.
- `cutoffHz` (`float`): The corner frequency, where the level is 3 dB down with the default `q`.
- `q` (`float`): Resonance; 0 (default) is flat. Values above 0.71 add a peak at the corner.
- `glideSeconds` (`double`): Time to glide to the new setting. Default 0.02.

**Exceptions**

- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): The bus is out of range, the cutoff is not above 0, or Q is negative.

### <a id="m-setbusmuted-audiobus-bool"></a>SetBusMuted(AudioBus, bool)

```csharp
public void SetBusMuted(AudioBus bus, bool muted);
```

Mutes or unmutes a bus without changing its volume.

**Parameters**

- `bus` ([`AudioBus`](AudioBus.md)): The bus.
- `muted` (`bool`): Whether to silence it.

**Exceptions**

- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): The bus index is not below [`AudioServiceOptions.BusCount`](AudioServiceOptions.md#m-buscount).

### <a id="m-setbusreverb-audiobus-reverboptions-double"></a>SetBusReverb(AudioBus, ReverbOptions, double)

```csharp
public bool SetBusReverb(AudioBus bus, ReverbOptions options, double glideSeconds = 0.05);
```

Puts a reverb on a bus, or retunes the one there. The tail keeps ringing after the bus's sounds stop. At most [`AudioServiceOptions.MaxReverbs`](AudioServiceOptions.md#m-maxreverbs) buses can have one at once.

**Parameters**

- `bus` ([`AudioBus`](AudioBus.md)): The bus.
- `options` ([`ReverbOptions`](ReverbOptions.md)): Room size, decay, damping, pre-delay and the wet/dry mix.
- `glideSeconds` (`double`): Time changes take. Default 0.05.

**Returns:** `bool` — `false` when every reverb is already in use by other buses (check [`AudioService.FreeReverbSlots`](AudioService.md#m-freereverbslots); see [`AudioServiceOptions.MaxReverbs`](AudioServiceOptions.md#m-maxreverbs)) or the command queue was full; nothing changed then. It never throws for a lack of slots.

**Exceptions**

- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): The bus is out of range.
- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): A reverb setting is out of range.

### <a id="m-setbusvolume-audiobus-float"></a>SetBusVolume(AudioBus, float)

```csharp
public void SetBusVolume(AudioBus bus, float volume);
```

Sets a bus's volume (linear, 0 and up). The effective gain is this times the master volume.

**Parameters**

- `bus` ([`AudioBus`](AudioBus.md)): The bus.
- `volume` (`float`): The volume.

**Exceptions**

- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): The bus index is not below [`AudioServiceOptions.BusCount`](AudioServiceOptions.md#m-buscount).

### <a id="m-setducking-audiobus-audiobus-duckingoptions"></a>SetDucking(AudioBus, AudioBus, DuckingOptions)

```csharp
public void SetDucking(AudioBus target, AudioBus watched, DuckingOptions settings = default);
```

Turns `target` down while `watched` is loud, for example music under speech.

**Parameters**

- `target` ([`AudioBus`](AudioBus.md)): The bus that is turned down.
- `watched` ([`AudioBus`](AudioBus.md)): The bus whose level triggers it.
- `settings` ([`DuckingOptions`](DuckingOptions.md)): Threshold, depth and timing; defaults duck 12 dB.

**Exceptions**

- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): A bus index is out of range.
- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): Both are the same bus, or the link would be circular.

### <a id="m-setfocused-bool"></a>SetFocused(bool)

```csharp
public void SetFocused(bool focused);
```

Tells the service whether the game window has focus. When [`AudioServiceOptions.PauseOnFocusLoss`](AudioServiceOptions.md#m-pauseonfocusloss) is set, losing focus pauses audio.

**Parameters**

- `focused` (`bool`): Whether the window is focused.

### <a id="m-setmasterlimiter-limiteroptions"></a>SetMasterLimiter(LimiterOptions?)

```csharp
public void SetMasterLimiter(LimiterOptions? options);
```

Retunes the master limiter, or switches it off with `null`. Needs [`AudioServiceOptions.MasterLimiter`](AudioServiceOptions.md#m-masterlimiter) at creation, which builds the lookahead.

**Parameters**

- `options` ([`LimiterOptions`](LimiterOptions.md)?): Ceiling and release, or null for off.

**Exceptions**

- [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception): The service was created without a master limiter.

### <a id="m-setpan-voiceid-float"></a>SetPan(VoiceId, float)

```csharp
public void SetPan(VoiceId voice, float pan);
```

Changes a playing voice's stereo position.

**Parameters**

- `voice` ([`VoiceId`](VoiceId.md)): The voice.
- `pan` (`float`): -1 to 1.

### <a id="m-setpitch-voiceid-float-double"></a>SetPitch(VoiceId, float, double)

```csharp
public void SetPitch(VoiceId voice, float pitch, double rampSeconds = 0);
```

Changes a playing voice's speed and pitch. Only voices started with a pitch other than 1 or with [`PlayOptions.VariablePitch`](PlayOptions.md#m-variablepitch) respond.

**Parameters**

- `voice` ([`VoiceId`](VoiceId.md)): The voice.
- `pitch` (`float`): Speed multiplier; 1 is the sound's own.
- `rampSeconds` (`double`): Time to glide there; 0 jumps.

### <a id="m-setsuspended-bool"></a>SetSuspended(bool)

```csharp
public void SetSuspended(bool suspended);
```

Tells the service the app left (`true`) or returned to the foreground. A suspended service renders nothing and stops the device, so a phone's audio hardware is released in the background; voices resume where they were. The host calls this on app lifecycle events.

**Parameters**

- `suspended` (`bool`): Whether the app is in the background.

### <a id="m-setvoicefilter-voiceid-audiofilterkind-float-float-double"></a>SetVoiceFilter(VoiceId, AudioFilterKind, float, float, double)

```csharp
public void SetVoiceFilter(VoiceId voice, AudioFilterKind kind, float cutoffHz, float q = 0f, double glideSeconds = 0.02);
```

Filters one voice before its volume and pan, for example a muffling low-pass on a sound behind a wall. A stale voice is ignored.

**Parameters**

- `voice` ([`VoiceId`](VoiceId.md)): The voice.
- `kind` ([`AudioFilterKind`](AudioFilterKind.md)): Which filter.
- `cutoffHz` (`float`): The corner frequency.
- `q` (`float`): Resonance; 0 (default) is flat.
- `glideSeconds` (`double`): Time to glide to the new setting. Default 0.02.

**Exceptions**

- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): The cutoff is not above 0 or Q is negative.

### <a id="m-setvolume-voiceid-float-double"></a>SetVolume(VoiceId, float, double)

```csharp
public void SetVolume(VoiceId voice, float volume, double rampSeconds = 0);
```

Changes a playing voice's volume.

**Parameters**

- `voice` ([`VoiceId`](VoiceId.md)): The voice.
- `volume` (`float`): Linear volume.
- `rampSeconds` (`double`): Time to reach it; 0 jumps.

### <a id="m-stop-voiceid-double"></a>Stop(VoiceId, double)

```csharp
public void Stop(VoiceId voice, double fadeOutSeconds = 0);
```

Stops a voice.

**Parameters**

- `voice` ([`VoiceId`](VoiceId.md)): The voice.
- `fadeOutSeconds` (`double`): Fade length; 0 stops at once.

### <a id="m-stopall-double"></a>StopAll(double)

```csharp
public void StopAll(double fadeOutSeconds = 0);
```

Stops every voice, music included.

**Parameters**

- `fadeOutSeconds` (`double`): Fade length; 0 stops at once.

### <a id="m-stopmusic-double"></a>StopMusic(double)

```csharp
public void StopMusic(double fadeOutSeconds = 0);
```

Fades out and stops the current music.

**Parameters**

- `fadeOutSeconds` (`double`): Fade length; 0 stops at once.

### <a id="m-tryenablebusmeter-audiobus"></a>TryEnableBusMeter(AudioBus)

```csharp
public BusMeterResult TryEnableBusMeter(AudioBus bus);
```

Like [`AudioService.EnableBusMeter()`](AudioService.md#m-enablebusmeter-audiobus), and says why a meter did not start.

**Parameters**

- `bus` ([`AudioBus`](AudioBus.md)): The bus.

**Returns:** [`BusMeterResult`](BusMeterResult.md) — The outcome; see [`BusMeterResult`](BusMeterResult.md).

**Exceptions**

- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): The bus is out of range.

### <a id="m-update"></a>Update()

```csharp
public void Update();
```

Call once per game frame: drains the mixer's events, forgets a music track that ended and raises [`AudioService.EventRaised`](AudioService.md#m-eventraised).
