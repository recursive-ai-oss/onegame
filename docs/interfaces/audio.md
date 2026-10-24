# Audio

Device output and the game-facing audio service (`OneGame.Audio`). Decoding and mixing come from oneaudio; this assembly owns the real-time thread, the operating-system backends and the service on top.

## Layers

```
AudioService   game thread: load, play, buses, music, events        (AudioService.cs)
  IAudioCore   the mixer seam: Render + thread-safe control calls   (AudioCore.cs)
AudioOutput    real-time thread: render -> limiter -> sink          (AudioOutput.cs)
  IAudioSink   one open device (blocking Write of float frames)
IAudioBackend  ALSA, WASAPI, capture, fakes in tests
```

The device decides the format. `AudioService.Create` opens the device first, then builds the core at the device's rate and channel count (`service.Format`). Rate and channels never change afterwards; if a device comes back at another rate, the backend must convert or the open is refused.

## Using it

```csharp
using var audio = AudioService.Create(new AudioServiceOptions { CoreFactory = ... });
var click = audio.LoadSound(File.OpenRead("click.wav"));
audio.Play(click, new PlayOptions { Bus = AudioBus.Ui, Volume = 0.8f, Pan = -0.2f });
audio.PlayMusic(File.OpenRead("theme.ogg"), new MusicOptions { CrossfadeSeconds = 2 });
audio.SetBusVolume(AudioBus.Music, 0.5f);
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

## The core seam and the switch to oneaudio

`IAudioCore` mirrors oneaudio's `AudioMixer` contract: `Render(Span<float>)` overwrites, never allocates, locks or throws, and output is not clipped; control calls are thread-safe and report a full queue with `false` / an invalid id; `TryGetEvent` polls `VoiceEnded`, `VoiceStolen`, `PlayDropped`, `Underrun`. `AudioServiceOptions.CoreFactory` supplies it; without one the service uses `NullAudioCore` (silence).

The oneaudio package is not published yet, so runtime code does not reference it. An adapter, `OneAudioCore` (about 130 lines, in `tests/OneGame.Audio.OneAudio.Tests`), already drives the real mixer against the service. **The switch is one step:** when the package exists, add the `RecursiveAI.OneAudio` reference to `OneGame.Audio.csproj`, move `OneAudioCore.cs` into `src/OneGame.Audio`, make it the default `CoreFactory` (with `AddWav()` plus whichever codecs are shipped), and give the adapter on-load resampling for files at another rate (`AudioMixer.Play` rejects a source whose rate differs from the mixer's; the adapter currently throws `NotSupportedException` instead). The integration project then becomes an ordinary test of the service. Until then it is outside `OneGame.slnx` and needs the sibling checkout next to the repository (`ONEAUDIO_PROJECT` overrides the path).

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

`AudioServiceOptions { Capture = true }` swaps the device for `CaptureAudioBackend`: no thread, no device, time moves only with `service.Advance(frames)`. `service.Capture` (`CaptureSink`) holds the post-limiter interleaved float samples (`Samples`, `FramesCaptured`, `Peak`, `Clear()`, `WriteWav(Stream)`), capped at ten minutes. Same scenario, same samples, every run. A future harness "audio capture" operation wraps `Advance` + `Capture`.

## Tests

`dotnet test tests/OneGame.Audio.Tests`: capture-sink scenarios (sums, gains, pans, bus/master, frame-exact start, clamp and knee, pause, mute, crossfade), the output thread against a fake device (pacing, loss, reconnect, rate refusal, no device, device switch, render fault, zero allocation), WASAPI logic, ALSA failure paths. `ONEGAME_AUDIO_DEVICE_TESTS=1` adds a quiet tone plus soak on the real device (`ONEGAME_AUDIO_SOAK_SECONDS`, `ONEGAME_AUDIO_REPORT` for the numbers).
