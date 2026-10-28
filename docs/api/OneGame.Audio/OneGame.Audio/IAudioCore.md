[API reference](../../index.md) › [OneGame.Audio](../index.md) › OneGame.Audio

# IAudioCore interface

**Namespace:** `OneGame.Audio`  
**Package:** [`RecursiveAI.OneGame.Audio`](../index.md)  
**Implements:** [`IAudioRenderSource`](IAudioRenderSource.md), [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public interface IAudioCore : IAudioRenderSource, IDisposable
```

The mixer behind the audio service: renders interleaved float frames and is controlled from the game thread. The control calls are thread-safe and never block. A full command queue makes a call return false (or an invalid id).

## Properties

| Member | Description |
| --- | --- |
| [`ActiveVoices`](#m-activevoices) | How many voices are playing now. |
| [`DroppedCommands`](#m-droppedcommands) | Commands dropped because the queue was full. |
| [`FreeBusMeters`](#m-freebusmeters) | Loudness meter slots not yet used. |
| [`FreeReverbSlots`](#m-freereverbslots) | Reverb slots not yet used by a bus. |
| [`HasLimiter`](#m-haslimiter) | Whether the core was built with a master limiter (a lookahead above zero). |
| [`LimiterGainReductionDb`](#m-limitergainreductiondb) | Gain reduction of the master limiter in the last render pass, in dB (0 or negative). |

### <a id="m-activevoices"></a>ActiveVoices

```csharp
int ActiveVoices { get; }
```

How many voices are playing now.

**Type:** `int`

### <a id="m-droppedcommands"></a>DroppedCommands

```csharp
long DroppedCommands { get; }
```

Commands dropped because the queue was full.

**Type:** `long`

### <a id="m-freebusmeters"></a>FreeBusMeters

```csharp
int FreeBusMeters { get; }
```

Loudness meter slots not yet used. Cores without bus meters report 0.

**Type:** `int`

### <a id="m-freereverbslots"></a>FreeReverbSlots

```csharp
int FreeReverbSlots { get; }
```

Reverb slots not yet used by a bus. Cores without reverb report 0.

**Type:** `int`

### <a id="m-haslimiter"></a>HasLimiter

```csharp
bool HasLimiter { get; }
```

Whether the core was built with a master limiter (a lookahead above zero).

**Type:** `bool`

### <a id="m-limitergainreductiondb"></a>LimiterGainReductionDb

```csharp
double LimiterGainReductionDb { get; }
```

Gain reduction of the master limiter in the last render pass, in dB (0 or negative).

**Type:** `double`

## Methods

| Member | Description |
| --- | --- |
| [`ClearBusFilter(int, AudioFilterKind, int)`](#m-clearbusfilter-int-audiofilterkind-int) | Turns a bus filter off. |
| [`ClearBusReverb(int)`](#m-clearbusreverb-int) | Removes a bus's reverb; its tail stops at once. |
| [`ClearDucking(int)`](#m-clearducking-int) | Removes the ducking of a bus. |
| [`ClearVoiceFilter(VoiceId, AudioFilterKind, int)`](#m-clearvoicefilter-voiceid-audiofilterkind-int) | Turns a voice filter off. |
| [`Crossfade(VoiceId, in VoiceRequest, int)`](#m-crossfade-voiceid-voicerequest-int) | Fades one voice out while another fades in over the same frames. |
| [`DisableBusMeter(int)`](#m-disablebusmeter-int) | Stops measuring a bus and frees its slot. |
| [`DurationSeconds(object)`](#m-durationseconds-object) | The length of a source in seconds, or a negative number when unknown. |
| [`EnableBusMeter(int)`](#m-enablebusmeter-int) | Starts measuring a bus's loudness, after its effects and gain and before ducking. |
| [`IsBusMetered(int)`](#m-isbusmetered-int) | Whether a meter is assigned to the bus (it may take until the next block to produce readings). |
| [`LoadClip(Stream)`](#m-loadclip-stream) | Decodes a whole file into memory. |
| [`OpenStream(Stream, bool)`](#m-openstream-stream-bool) | Opens a file for streaming playback. |
| [`Play(in VoiceRequest)`](#m-play-voicerequest) | Starts a voice. |
| [`Pump()`](#m-pump) | Feeds streaming sources from their decoders. |
| [`ResetBusMeter(int)`](#m-resetbusmeter-int) | Starts a metered bus's measurement over. |
| [`SetBusFilter(int, AudioFilterKind, float, float, int)`](#m-setbusfilter-int-audiofilterkind-float-float-int) | Turns on or retunes a low-pass or high-pass filter on a bus (12 dB per octave). |
| [`SetBusGain(int, float)`](#m-setbusgain-int-float) | Sets a bus's gain. |
| [`SetBusMute(int, bool)`](#m-setbusmute-int-bool) | Mutes or unmutes a bus without touching its gain. |
| [`SetBusReverb(int, ReverbOptions, int)`](#m-setbusreverb-int-reverboptions-int) | Puts a reverb insert on a bus, or retunes the one there. |
| [`SetDucking(int, int, DuckingOptions)`](#m-setducking-int-int-duckingoptions) | Turns `targetBus` down while `sourceBus` is loud. |
| [`SetGain(VoiceId, float, int)`](#m-setgain-voiceid-float-int) | Changes a voice's gain. |
| [`SetLimiter(LimiterOptions?)`](#m-setlimiter-limiteroptions) | Switches the master limiter on, retunes it, or (with `null`) off. |
| [`SetLoop(VoiceId, bool)`](#m-setloop-voiceid-bool) | Changes a voice's looping. |
| [`SetPan(VoiceId, float)`](#m-setpan-voiceid-float) | Changes a voice's stereo position. |
| [`SetPitch(VoiceId, float, int)`](#m-setpitch-voiceid-float-int) | Changes a voice's playback speed and pitch. |
| [`SetVoiceFilter(VoiceId, AudioFilterKind, float, float, int)`](#m-setvoicefilter-voiceid-audiofilterkind-float-float-int) | Like [`IAudioCore.SetBusFilter()`](IAudioCore.md#m-setbusfilter-int-audiofilterkind-float-float-int) for one voice. |
| [`StopAll(int)`](#m-stopall-int) | Stops every voice. |
| [`StopVoice(VoiceId, int)`](#m-stopvoice-voiceid-int) | Fades out and removes a voice. |
| [`TryGetBusLoudness(int, out LoudnessReading)`](#m-trygetbusloudness-int-loudnessreading) | Reads a metered bus. |
| [`TryGetEvent(out AudioEvent)`](#m-trygetevent-audioevent) | Takes the next event the audio thread produced. |
| [`TryGetStreamPosition(VoiceId, out double, out double)`](#m-trygetstreamposition-voiceid-double-double) | Reads where a streaming voice is. |

### <a id="m-clearbusfilter-int-audiofilterkind-int"></a>ClearBusFilter(int, AudioFilterKind, int)

```csharp
bool ClearBusFilter(int bus, AudioFilterKind kind, int glideFrames);
```

Turns a bus filter off.

**Parameters**

- `bus` (`int`): The bus index.
- `kind` ([`AudioFilterKind`](AudioFilterKind.md)): Which filter.
- `glideFrames` (`int`): Frames to glide to transparent first.

**Returns:** `bool` — Whether the command was queued. Cores without filters return false.

### <a id="m-clearbusreverb-int"></a>ClearBusReverb(int)

```csharp
bool ClearBusReverb(int bus);
```

Removes a bus's reverb; its tail stops at once.

**Parameters**

- `bus` (`int`): The bus index.

**Returns:** `bool` — Whether the command was queued. Cores without reverb return false.

### <a id="m-clearducking-int"></a>ClearDucking(int)

```csharp
bool ClearDucking(int targetBus);
```

Removes the ducking of a bus.

**Parameters**

- `targetBus` (`int`): The bus.

**Returns:** `bool` — Whether the command was queued. Cores without ducking return false.

### <a id="m-clearvoicefilter-voiceid-audiofilterkind-int"></a>ClearVoiceFilter(VoiceId, AudioFilterKind, int)

```csharp
bool ClearVoiceFilter(VoiceId voice, AudioFilterKind kind, int glideFrames);
```

Turns a voice filter off.

**Parameters**

- `voice` ([`VoiceId`](VoiceId.md)): The voice.
- `kind` ([`AudioFilterKind`](AudioFilterKind.md)): Which filter.
- `glideFrames` (`int`): Frames to glide to transparent first.

**Returns:** `bool` — Whether the command was queued. Cores without filters return false.

### <a id="m-crossfade-voiceid-voicerequest-int"></a>Crossfade(VoiceId, in VoiceRequest, int)

```csharp
VoiceId Crossfade(VoiceId from, in VoiceRequest incoming, int frames);
```

Fades one voice out while another fades in over the same frames.

**Parameters**

- `from` ([`VoiceId`](VoiceId.md)): The voice to fade out; may be invalid.
- `incoming` ([`VoiceRequest`](VoiceRequest.md)): The voice to start.
- `frames` (`int`): The fade length.

**Returns:** [`VoiceId`](VoiceId.md) — The new voice, or an invalid id when the queue is full.

### <a id="m-disablebusmeter-int"></a>DisableBusMeter(int)

```csharp
bool DisableBusMeter(int bus);
```

Stops measuring a bus and frees its slot.

**Parameters**

- `bus` (`int`): The bus index.

**Returns:** `bool` — Whether the command was queued.

### <a id="m-durationseconds-object"></a>DurationSeconds(object)

```csharp
double DurationSeconds(object source);
```

The length of a source in seconds, or a negative number when unknown.

**Parameters**

- `source` (`object`): A source from this core.

**Returns:** `double` — Seconds, or a negative number.

### <a id="m-enablebusmeter-int"></a>EnableBusMeter(int)

```csharp
bool EnableBusMeter(int bus);
```

Starts measuring a bus's loudness, after its effects and gain and before ducking.

**Parameters**

- `bus` (`int`): The bus index.

**Returns:** `bool` — True when the bus is metered or the start was queued. False when no meter slot is free ([`IAudioCore.FreeBusMeters`](IAudioCore.md#m-freebusmeters) is 0), the command queue was full (it is more than 0), or the core has no bus meters (it is 0); nothing changed then.

### <a id="m-isbusmetered-int"></a>IsBusMetered(int)

```csharp
bool IsBusMetered(int bus);
```

Whether a meter is assigned to the bus (it may take until the next block to produce readings).

**Parameters**

- `bus` (`int`): The bus index.

**Returns:** `bool` — True when the bus is metered or its meter start is queued.

### <a id="m-loadclip-stream"></a>LoadClip(Stream)

```csharp
object LoadClip(Stream data);
```

Decodes a whole file into memory. Throws for malformed or unsupported data.

**Parameters**

- `data` ([`Stream`](https://learn.microsoft.com/dotnet/api/system.io.stream)): The encoded file.

**Returns:** `object` — A source that can be played any number of times at once.

### <a id="m-openstream-stream-bool"></a>OpenStream(Stream, bool)

```csharp
object OpenStream(Stream data, bool looping);
```

Opens a file for streaming playback. The source plays once at a time; it takes ownership of the stream.

**Parameters**

- `data` ([`Stream`](https://learn.microsoft.com/dotnet/api/system.io.stream)): The encoded file.
- `looping` (`bool`): Whether the decoder wraps at the end.

**Returns:** `object` — A streaming source.

### <a id="m-play-voicerequest"></a>Play(in VoiceRequest)

```csharp
VoiceId Play(in VoiceRequest request);
```

Starts a voice.

**Parameters**

- `request` ([`VoiceRequest`](VoiceRequest.md)): The request.

**Returns:** [`VoiceId`](VoiceId.md) — The voice, or an invalid id when the command queue is full.

### <a id="m-pump"></a>Pump()

```csharp
void Pump();
```

Feeds streaming sources from their decoders. Called off the audio thread, repeatedly, by the service.

### <a id="m-resetbusmeter-int"></a>ResetBusMeter(int)

```csharp
bool ResetBusMeter(int bus);
```

Starts a metered bus's measurement over.

**Parameters**

- `bus` (`int`): The bus index.

**Returns:** `bool` — Whether the command was queued.

### <a id="m-setbusfilter-int-audiofilterkind-float-float-int"></a>SetBusFilter(int, AudioFilterKind, float, float, int)

```csharp
bool SetBusFilter(int bus, AudioFilterKind kind, float cutoffHz, float q, int glideFrames);
```

Turns on or retunes a low-pass or high-pass filter on a bus (12 dB per octave).

**Parameters**

- `bus` (`int`): The bus index.
- `kind` ([`AudioFilterKind`](AudioFilterKind.md)): Which filter.
- `cutoffHz` (`float`): The corner frequency.
- `q` (`float`): Resonance; 0 or less picks a flat (Butterworth) response.
- `glideFrames` (`int`): Frames over which the change glides.

**Returns:** `bool` — Whether the command was queued. Cores without filters return false.

### <a id="m-setbusgain-int-float"></a>SetBusGain(int, float)

```csharp
bool SetBusGain(int bus, float gain);
```

Sets a bus's gain.

**Parameters**

- `bus` (`int`): The bus index.
- `gain` (`float`): Linear gain.

**Returns:** `bool` — Whether the command was queued.

### <a id="m-setbusmute-int-bool"></a>SetBusMute(int, bool)

```csharp
bool SetBusMute(int bus, bool muted);
```

Mutes or unmutes a bus without touching its gain.

**Parameters**

- `bus` (`int`): The bus index.
- `muted` (`bool`): Whether to silence the bus.

**Returns:** `bool` — Whether the command was queued. Cores without bus mute return false.

### <a id="m-setbusreverb-int-reverboptions-int"></a>SetBusReverb(int, ReverbOptions, int)

```csharp
bool SetBusReverb(int bus, ReverbOptions options, int glideFrames);
```

Puts a reverb insert on a bus, or retunes the one there.

**Parameters**

- `bus` (`int`): The bus index.
- `options` ([`ReverbOptions`](ReverbOptions.md)): The reverb.
- `glideFrames` (`int`): Frames over which changes glide.

**Returns:** `bool` — Whether the command was queued. Cores without reverb return false, and so does a core with no free reverb slot for a bus that has none.

### <a id="m-setducking-int-int-duckingoptions"></a>SetDucking(int, int, DuckingOptions)

```csharp
bool SetDucking(int targetBus, int sourceBus, DuckingOptions settings);
```

Turns `targetBus` down while `sourceBus` is loud.

**Parameters**

- `targetBus` (`int`): The bus that is turned down.
- `sourceBus` (`int`): The bus whose level is watched.
- `settings` ([`DuckingOptions`](DuckingOptions.md)): Threshold, depth and timing.

**Returns:** `bool` — Whether the command was queued. Cores without ducking return false.

### <a id="m-setgain-voiceid-float-int"></a>SetGain(VoiceId, float, int)

```csharp
bool SetGain(VoiceId voice, float gain, int rampFrames);
```

Changes a voice's gain.

**Parameters**

- `voice` ([`VoiceId`](VoiceId.md)): The voice.
- `gain` (`float`): Linear target gain.
- `rampFrames` (`int`): Frames to reach it; 0 jumps.

**Returns:** `bool` — Whether the command was queued.

### <a id="m-setlimiter-limiteroptions"></a>SetLimiter(LimiterOptions?)

```csharp
bool SetLimiter(LimiterOptions? options);
```

Switches the master limiter on, retunes it, or (with `null`) off.

**Parameters**

- `options` ([`LimiterOptions`](LimiterOptions.md)?): Ceiling and release, or null for off.

**Returns:** `bool` — Whether the command was queued. False when the core was built without a limiter.

### <a id="m-setloop-voiceid-bool"></a>SetLoop(VoiceId, bool)

```csharp
bool SetLoop(VoiceId voice, bool looping);
```

Changes a voice's looping.

**Parameters**

- `voice` ([`VoiceId`](VoiceId.md)): The voice.
- `looping` (`bool`): Whether to loop.

**Returns:** `bool` — Whether the command was queued.

### <a id="m-setpan-voiceid-float"></a>SetPan(VoiceId, float)

```csharp
bool SetPan(VoiceId voice, float pan);
```

Changes a voice's stereo position.

**Parameters**

- `voice` ([`VoiceId`](VoiceId.md)): The voice.
- `pan` (`float`): -1 to 1.

**Returns:** `bool` — Whether the command was queued.

### <a id="m-setpitch-voiceid-float-int"></a>SetPitch(VoiceId, float, int)

```csharp
bool SetPitch(VoiceId voice, float pitch, int rampFrames);
```

Changes a voice's playback speed and pitch. Only voices started with a pitch other than 1 or with [`VoiceRequest.VariablePitch`](VoiceRequest.md#m-variablepitch) respond.

**Parameters**

- `voice` ([`VoiceId`](VoiceId.md)): The voice.
- `pitch` (`float`): Speed multiplier; 1 is the source's own.
- `rampFrames` (`int`): Frames to glide; 0 jumps.

**Returns:** `bool` — Whether the command was queued. Cores without pitch support return false.

### <a id="m-setvoicefilter-voiceid-audiofilterkind-float-float-int"></a>SetVoiceFilter(VoiceId, AudioFilterKind, float, float, int)

```csharp
bool SetVoiceFilter(VoiceId voice, AudioFilterKind kind, float cutoffHz, float q, int glideFrames);
```

Like [`IAudioCore.SetBusFilter()`](IAudioCore.md#m-setbusfilter-int-audiofilterkind-float-float-int) for one voice.

**Parameters**

- `voice` ([`VoiceId`](VoiceId.md)): The voice.
- `kind` ([`AudioFilterKind`](AudioFilterKind.md)): Which filter.
- `cutoffHz` (`float`): The corner frequency.
- `q` (`float`): Resonance; 0 or less picks a flat response.
- `glideFrames` (`int`): Frames over which the change glides.

**Returns:** `bool` — Whether the command was queued. Cores without filters return false.

### <a id="m-stopall-int"></a>StopAll(int)

```csharp
bool StopAll(int fadeOutFrames);
```

Stops every voice.

**Parameters**

- `fadeOutFrames` (`int`): Fade length; 0 stops at once.

**Returns:** `bool` — Whether the command was queued.

### <a id="m-stopvoice-voiceid-int"></a>StopVoice(VoiceId, int)

```csharp
bool StopVoice(VoiceId voice, int fadeOutFrames);
```

Fades out and removes a voice.

**Parameters**

- `voice` ([`VoiceId`](VoiceId.md)): The voice.
- `fadeOutFrames` (`int`): Fade length; 0 stops at once.

**Returns:** `bool` — Whether the command was queued.

### <a id="m-trygetbusloudness-int-loudnessreading"></a>TryGetBusLoudness(int, out LoudnessReading)

```csharp
bool TryGetBusLoudness(int bus, out LoudnessReading reading);
```

Reads a metered bus.

**Parameters**

- `bus` (`int`): The bus index.
- `reading` ([`LoudnessReading`](LoudnessReading.md)): The reading; silence and no data are -infinity.

**Returns:** `bool` — False when the bus is not metered.

### <a id="m-trygetevent-audioevent"></a>TryGetEvent(out AudioEvent)

```csharp
bool TryGetEvent(out AudioEvent audioEvent);
```

Takes the next event the audio thread produced.

**Parameters**

- `audioEvent` ([`AudioEvent`](AudioEvent.md)): The event.

**Returns:** `bool` — Whether there was one.

### <a id="m-trygetstreamposition-voiceid-double-double"></a>TryGetStreamPosition(VoiceId, out double, out double)

```csharp
bool TryGetStreamPosition(VoiceId voice, out double positionSeconds, out double lengthSeconds);
```

Reads where a streaming voice is. Positions are in source time, ahead of the speakers by the mixer's and device's buffering.

**Parameters**

- `voice` ([`VoiceId`](VoiceId.md)): A voice started from a stream.
- `positionSeconds` (`double`): Seconds into the stream; wraps with a loop.
- `lengthSeconds` (`double`): The stream's length, or a negative number when unknown.

**Returns:** `bool` — Whether the voice is a live stream.
