# Audio

Device output and the game-facing audio service (`OneGame.Audio`). Decoding and mixing come from the oneaudio package; this assembly owns the real-time thread, the operating-system backends and the service on top.

## Layers

```
AudioService   game thread: load, play, buses, music, events        (AudioService.cs)
  IAudioCore   the mixer: Render + thread-safe control calls        (AudioCore.cs)
    OneAudioCore  the default: oneaudio mixer + WAV/Ogg/Vorbis/Opus/FLAC/MP3  (OneAudioCore.cs)
AudioOutput    real-time thread: render -> limiter -> sink          (AudioOutput.cs)
  IAudioSink   one open device (blocking Write of float frames)
IAudioBackend  ALSA, WASAPI, capture, fakes in tests
```

The device decides the format. `AudioService.Create` opens the device first, then builds the core at the device's rate and channel count (`service.Format`). Rate and channels never change afterwards; if a device comes back at another rate, the backend must convert or the open is refused.

## Using it

```csharp
using var audio = AudioService.Create();          // the default core is oneaudio's mixer
var click = audio.LoadSound(File.OpenRead("click.wav"));
audio.Play(click, new PlayOptions { Bus = AudioBus.Ui, Volume = 0.8f, Pan = -0.2f });
audio.PlayMusic(File.OpenRead("theme.ogg"), new MusicOptions { CrossfadeSeconds = 2 });
audio.SetBusVolume(AudioBus.Music, 0.5f);
audio.SetBusMuted(AudioBus.Sfx, true);                       // mute keeps the volume
audio.SetDucking(AudioBus.Music, AudioBus.Ui);               // music dips 12 dB while ui sounds play
audio.SetBusFilter(AudioBus.Sfx, AudioFilterKind.LowPass, 800f);   // muffled; ClearBusFilter undoes it
audio.SetBusReverb(AudioBus.Sfx, new ReverbOptions { DecaySeconds = 2f, Wet = 0.3f });
var lufs = audio.Loudness.IntegratedLufs;                   // EBU R 128 at the output
var v = audio.Play(click, new PlayOptions { Pitch = 1.2f, VariablePitch = true });
audio.SetPitch(v, 0.9f, rampSeconds: 0.2);
// once per frame:
audio.Update();                      // drains mixer events, raises EventRaised
// from the window layer:
audio.SetFocused(hasFocus);          // pauses when PauseOnFocusLoss (default true)
```

- Buses: `Music` 0, `Sfx` 1, `Ui` 2; `BusCount` (default 8) allows more by index. Effective bus gain is `MasterVolume x bus volume`; `Muted` zeroes all without touching volumes.
- `Play` returns a `VoiceId`; an invalid id means the command queue was full (the sound was dropped). Stale ids are ignored by the core.
- `StartAtFrame` places a sound frame-exactly on the mixer timeline (`TimelineFrames` of the core).
- `PlayMusic` keeps one current music voice; a new track replaces it, with a crossfade when `CrossfadeSeconds > 0`. A track that ends on its own is forgotten on the next `Update`.
- `Paused` / `SetFocused(false)` feed the device silence and stop rendering the core, so the mixer clock holds still and voices resume where they were.
- All service calls are for the game thread. The service runs its own small pump thread that calls `IAudioCore.Pump()` (streaming decode); cores must tolerate `Pump` concurrent with the control calls.

## The core and oneaudio

`IAudioCore` is the small surface the service needs from a mixer: `Render(Span<float>)` overwrites, never allocates, locks or throws, and output is not clipped; control calls are thread-safe and report a full queue with `false` / an invalid id; `TryGetEvent` polls `VoiceEnded`, `VoiceStolen`, `PlayDropped`, `Underrun`. `OneAudioCore` implements it over `RecursiveAI.OneAudio` and is what `AudioService.Create` uses unless `AudioServiceOptions.CoreFactory` says otherwise (tests use a tiny stand-in). `OneAudioCore.Mixer` exposes the `AudioMixer` for counters.

- **Formats:** WAV, Ogg Vorbis, Opus, FLAC and MP3 (MPEG-1/2/2.5 Layer III), detected from the bytes. Any source rate and channel count plays; the mixer converts to the device format (ratio 0.5 to 16 after pitch, so a 8 kHz clip into a 48 kHz device is the limit).
- **Clips vs streams:** `LoadSound` decodes a whole file into memory (refused above `MaxClipSeconds`, default 120). `PlayMusic` streams: the first block is decoded before the call returns, then the service's pump thread refills; a stream is released when its voice ends or is stolen.
- **Failure:** a malformed, truncated, unsupported or over-limit file throws `AudioLoadException` from `LoadSound` / `PlayMusic` on the game thread. A NaN or out-of-range volume, pan or pitch throws `ArgumentException` (or a subclass) from `Play`; nothing from content reaches the audio thread.
- **Pitch:** `PlayOptions.Pitch` (0.0625 to 16) works at play time. To change it later set `VariablePitch` (the mixer reserves a converter) and call `SetPitch`; without that, `SetPitch` is ignored. A full converter pool gives an invalid `VoiceId`, as a full queue does.
- **Gain changes smooth** over about 10 ms (bus volume, mute, `SetVolume` with a ramp of 0); crossfades, fades and ducking (`SetDucking(target, watched, DuckingOptions)`, which rejects cycles) run inside the mixer.
- **Ducking** (`DuckingOptions`): the attack and release times are time constants of an exponential: one release time recovers 63%, four recover 98%. The watched level is taken after the watched bus's own gain and mute. `DuckingOptions.None` (or `ClearDucking`) removes the link.
- **Master volume / `Muted`** still scale the bus gains the service sets, so they behave the same on every core.
- A mono source is centred with an equal-power law (-3 dB per channel).
- Build the service at startup: it allocates the mixer and about 2.6 MB of converters up front. Setting `GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency` while a game runs keeps collections off the pump thread.

## Mixing effects and metering

All of these are on `AudioService`; none exposes a oneaudio type. A core without them (the test stand-in) ignores the calls.

- **Filters:** `SetBusFilter(bus, AudioFilterKind.LowPass|HighPass, hz, q = 0, glideSeconds = 0.02)`, `ClearBusFilter`, and the same per voice (`SetVoiceFilter`, applied before the voice's gain and pan). Second order, 12 dB per octave, Butterworth when `q` is 0 (3 dB down at the cutoff; 1 kHz low-pass takes 24.1 dB off a 4 kHz tone). One filter of each kind per bus; a band-pass is both. Cutoff and Q glide, so sweeps do not click. A cutoff of 0 or less, NaN or a negative Q throws `ArgumentOutOfRangeException`.
- **Reverb:** `SetBusReverb(bus, ReverbOptions, glideSeconds = 0.05)` is an insert, `Dry * signal + Wet * reverb`, after the bus filters and before the bus gain; `ClearBusReverb` cuts the tail at once (lower `Wet` first to fade). At most `AudioServiceOptions.MaxReverbs` (default 2, about 0.3 MB each, built at startup) buses can have one; asking for more returns `false` from `SetBusReverb` and changes nothing (as does a full command queue); a setting out of range throws `ArgumentException`. The tail rings on after the bus's voices end.
- **Master limiter:** `AudioServiceOptions.MasterLimiter = new LimiterOptions { CeilingDb = -1, ReleaseMilliseconds = 100 }` builds a lookahead brickwall (`LimiterLookaheadMilliseconds`, default 5). No sample passes the ceiling. It costs a fixed latency of the lookahead on all output for the service's life, so it is chosen at creation; `SetMasterLimiter` retunes or switches it off (the delay stays). It limits sample peaks, not inter-sample peaks. It runs inside the mixer, before the output safety limiter of `AudioServiceOptions.Limiter`.
- **Loudness:** the output meters what the speakers get (after both limiters) per ITU-R BS.1770-5 and EBU R 128: `service.Loudness` gives momentary (400 ms), short-term (3 s), gated integrated and true peak (dBTP), and the frames measured; silence is `-Infinity`. `ResetLoudness()` starts a measurement over (at once in capture mode, at the next block with a device). Anything paused is not measured. `AudioServiceOptions.Metering = false` turns it off. A mono sound centred by the mixer loses 3 dB per channel, so a mono 1 kHz sine at -20 dBFS peak reads -23.0 LUFS.
- **Streams:** `GetStreamPosition(voice)` gives `PositionSeconds` and `LengthSeconds` (negative when the format can't say) of a `PlayMusic` voice; the position is source time, ahead of the speakers by the buffering, and wraps with a loop. `Describe().Music` has the same for the current track.
- **Per-bus loudness:** `GetBusLoudness(bus)` returns a `LoudnessReading` (as `Loudness`, silence is `-Infinity`) or `null` when the bus is not metered. A bus is measured as it leaves the bus: after its sounds, filters, reverb, volume and mute, before ducking, the master volume and both limiters. So ducking the music bus under sfx lowers `Loudness` (the output) but not the music bus reading; a bus muted or at volume 0 reads silence. The music, sfx and ui buses are metered from startup; `AudioServiceOptions.MaxBusMeters` (default 3, 0 to 64, about 0.1 MB each, built at startup) caps how many buses can be metered at once, and `EnableBusMeter(bus)` / `DisableBusMeter(bus)` move slots between buses (`FreeBusMeters` counts what is left). `ResetLoudness()` restarts the bus meters too. With `Metering = false`, or an output rate below the meter's minimum, there are none.
- **Out of slots:** `SetBusReverb` and `EnableBusMeter` return `false` and change nothing when a bus without one finds every slot taken (`FreeReverbSlots`, `FreeBusMeters`), or when the command queue is full. They do not throw for that, whatever the mixer underneath does. Retuning a bus that already has a reverb, or enabling an already metered bus, always works.

## Harness

`AudioService.Describe()` returns an `AudioStatus` (buses with volume, mute, filters, reverb, ducking, loudness; limiter and its gain reduction; loudness; music position). Two control operations (`docs/interfaces/control.md`) expose it as JSON to the CLI, tests and MCP:

- `audio` `{reset?}`: the status. Finds the newest live `AudioService` (`AudioService.Active`); `ControlSurface.Audio` overrides it. Fails with `no_audio`.
- `audio_capture` `{seconds?, path?, clear?, reset?}`: for a service created with `Capture = true`, renders that many seconds, then reports frames, peak, the output loudness and `busLoudness` (each metered bus: index, name, loudness); with `path` writes the recording as a WAV under the output folder with its SHA-256. Fails with `rejected` when audio plays on a device.

A mix check is then: `audio_capture {"seconds":30,"reset":true}` and read `loudness.integratedLufs`, or `busLoudness[0].loudness.integratedLufs` for the music bus alone (JSON leaves a silent value out). `audio` reports the same per bus under `buses[i].loudness`.

## Device selection, latency, no device

- `AudioDeviceOptions`: `DeviceName` (null = system default; names come from `backend.ListDevices()`), `SampleRate` (48000), `Channels` (2), `LatencyMilliseconds` (total device buffer, default 40).
- `AudioOutput.SelectDevice(name)` switches device at runtime at the same format; a failing new device leaves the output silent and retrying.
- **Linux: ALSA** (`libasound.so.2`, `AlsaBackend`). One stable C API reaches everything: on PipeWire (host, Steam Deck) and PulseAudio the `default` device is the sound server's ALSA plugin, so output joins the system mixer, follows the user's default output and converts rate/channels; on a bare system it is the hardware. A sound server's native client API would be a second code path per server. Float samples are tried first, then 16-bit. The sound server moves the stream when the user changes the default device, so no change event is needed.
- **Windows: WASAPI shared mode** (`WasapiBackend`), 32-bit float with automatic rate/channel conversion, polling writes. Compiles everywhere; the byte layout and arithmetic are unit-tested; runtime on Windows is unverified. Named device selection takes an endpoint id; `ListDevices` lists only the default.
- **Measured latency** is `AudioOutput.LatencyMilliseconds` (audio queued in the device right after a write). `BufferFrames` / `PeriodFrames` give the negotiated buffer.
- **Device loss** (write error that is not a recoverable underrun): the sink is closed, `DeviceLosses` counts it, status becomes `NoDevice`, and the output keeps rendering at real-time pace into nothing so game time and sound events behave the same. It retries opening every 500 ms at the original format; success counts a `Reopen` and sound resumes.
- **No device at start**: same silent mode from the first block; `Status == NoDevice`, `LastError` says why. `Create` never throws for a missing device or missing library.
- **Counters** on `AudioOutput`: `Underruns`, `DeviceLosses`, `Reopens`, `RenderFaults` (core threw; block silenced), `FramesRendered`, `SilentFrames`, `AudioThreadAllocatedBytes` (after warm-up, excluding device opens).
- Output limiter: `OutputLimiterMode.SoftKnee` (default; below 0.9 untouched, tanh knee above, never reaches 1) or `Clamp`. NaN becomes 0.

## Capture sink (harness hook)

`AudioServiceOptions { Capture = true }` swaps the device for `CaptureAudioBackend`: no thread, no device, time moves only with `service.Advance(frames)`. `service.Capture` (`CaptureSink`) holds the post-limiter interleaved float samples (`Samples`, `FramesCaptured`, `Peak`, `Clear()`, `WriteWav(Stream)`), capped at ten minutes. Same scenario, same samples, every run. The `audio_capture` operation wraps `Advance` + `Capture`.

## Tests

`dotnet test tests/OneGame.Audio.Tests`: capture-sink scenarios on the stand-in core (sums, gains, pans, bus/master, frame-exact start, clamp and knee, pause, mute, crossfade) and on the real mixer (`RealMixerTests`: 44.1 kHz Vorbis into 48 kHz, looping Opus with pitch, crossfade, bus volume and mute, ducking, limiter, bad content), and `MixingTests` (filters against the Butterworth curve, reverb tail, master limiter ceiling and latency, FLAC and MP3 fixtures, stream length and position, a calibrated -23 LUFS tone, bus description). The Control tests cover the `audio` operations, with fixtures from `testdata/audio`, the output thread against a fake device (pacing, loss, reconnect, rate refusal, no device, device switch, render fault, zero allocation), WASAPI logic, ALSA failure paths. `ONEGAME_AUDIO_DEVICE_TESTS=1` adds a quiet Vorbis tone plus a Vorbis/Opus soak on the real device (`ONEGAME_AUDIO_SOAK_SECONDS`, `ONEGAME_AUDIO_REPORT` for the numbers).
