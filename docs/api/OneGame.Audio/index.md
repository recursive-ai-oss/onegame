[API reference](../index.md) › OneGame.Audio

# OneGame.Audio

Audio device output and the audio service that drives a mixer.

**Package:** `RecursiveAI.OneGame.Audio`

```sh
dotnet add package RecursiveAI.OneGame.Audio
```

## OneGame.Audio

| Type | Kind | Description |
| --- | --- | --- |
| [`AAudioBackend`](OneGame.Audio/AAudioBackend.md) | class | Android output through AAudio (Android 8.0 and later), provided by the device package: shared low-latency float streams driven by the system's data callback, with the device clock, xrun counts and reopening on a route change reported by the package. |
| [`AlsaBackend`](OneGame.Audio/AlsaBackend.md) | class | Linux output through ALSA, provided by the device package. |
| [`AudioBus`](OneGame.Audio/AudioBus.md) | struct | A mixing bus. |
| [`AudioCoreSettings`](OneGame.Audio/AudioCoreSettings.md) | record | What a core is built with. |
| [`AudioDeviceFormat`](OneGame.Audio/AudioDeviceFormat.md) | struct | The format an open device runs at. |
| [`AudioDeviceInfo`](OneGame.Audio/AudioDeviceInfo.md) | struct | One output device a backend can open. |
| [`AudioDeviceOptions`](OneGame.Audio/AudioDeviceOptions.md) | record | What the game asks of an output device. |
| [`AudioDeviceStatus`](OneGame.Audio/AudioDeviceStatus.md) | record | The device section of the `audio` report. |
| [`AudioEvent`](OneGame.Audio/AudioEvent.md) | struct | One thing that happened on the audio thread. |
| [`AudioEventKind`](OneGame.Audio/AudioEventKind.md) | enum | What a core reports back to the game thread. |
| [`AudioFilterKind`](OneGame.Audio/AudioFilterKind.md) | enum | Which side of the spectrum a filter lets through. |
| [`AudioLoadException`](OneGame.Audio/AudioLoadException.md) | class | A source file could not be decoded: it is malformed, uses an unsupported format, or asks for more than the limits allow. |
| [`AudioModule`](OneGame.Audio/AudioModule.md) | static class | Identifies the Audio assembly. |
| [`AudioOutput`](OneGame.Audio/AudioOutput.md) | class | The real-time side of audio: a thread pulls frames from an [`IAudioRenderSource`](OneGame.Audio/IAudioRenderSource.md), limits them and writes them to an [`IAudioSink`](OneGame.Audio/IAudioSink.md). |
| [`AudioOutputStatus`](OneGame.Audio/AudioOutputStatus.md) | enum | Where an [`AudioOutput`](OneGame.Audio/AudioOutput.md) is in its life. |
| [`AudioService`](OneGame.Audio/AudioService.md) | class | The game-facing audio service: load clips, play sounds and music on buses, set volumes, crossfade, and pump events. |
| [`AudioServiceOptions`](OneGame.Audio/AudioServiceOptions.md) | record | Settings for [`AudioService.Create()`](OneGame.Audio/AudioService.md#m-create-audioserviceoptions). |
| [`AudioSessionCategory`](OneGame.Audio/AudioSessionCategory.md) | enum | How an iOS app's audio relates to other apps' audio and the ring/silent switch (AVAudioSession categories). |
| [`AudioSinkReport`](OneGame.Audio/AudioSinkReport.md) | struct | What a sink can say about the device behind it, measured on the device's side of the hand-over. |
| [`AudioStatus`](OneGame.Audio/AudioStatus.md) | record | The mixing state, from [`AudioService.Describe()`](OneGame.Audio/AudioService.md#m-describe). |
| [`AudioUnitBackend`](OneGame.Audio/AudioUnitBackend.md) | class | Apple output: RemoteIO on iOS (after the session is set up) and the default output unit on macOS, provided by the device package. |
| [`BusMeterResult`](OneGame.Audio/BusMeterResult.md) | enum | What asking for a bus meter came to. |
| [`BusStatus`](OneGame.Audio/BusStatus.md) | record | One bus as the service set it up. |
| [`CaptureAudioBackend`](OneGame.Audio/CaptureAudioBackend.md) | class | A backend with no device: it records the samples the output stage would have sent to the speakers. |
| [`CaptureSink`](OneGame.Audio/CaptureSink.md) | class | The recording kept by [`CaptureAudioBackend`](OneGame.Audio/CaptureAudioBackend.md): interleaved float samples after the limiter. |
| [`DuckingOptions`](OneGame.Audio/DuckingOptions.md) | struct | How one bus is turned down by the level of another, for example music under spoken lines. |
| [`DuckingStatus`](OneGame.Audio/DuckingStatus.md) | record | A bus's ducking link. |
| [`FilterStatus`](OneGame.Audio/FilterStatus.md) | record | A bus filter's setting. |
| [`IAudioBackend`](OneGame.Audio/IAudioBackend.md) | interface | An operating-system audio API, or a stand-in for one. |
| [`IAudioCore`](OneGame.Audio/IAudioCore.md) | interface | The mixer behind the audio service: renders interleaved float frames and is controlled from the game thread. |
| [`IAudioCoreFactory`](OneGame.Audio/IAudioCoreFactory.md) | interface | Creates the mixer once the device has fixed the sample rate and channel count. |
| [`IAudioRenderSource`](OneGame.Audio/IAudioRenderSource.md) | interface | Anything that produces interleaved float frames on demand; the shape of a mixer's render call. |
| [`IAudioSink`](OneGame.Audio/IAudioSink.md) | interface | Where the output stage's final float samples go. |
| [`IAudioSinkControl`](OneGame.Audio/IAudioSinkControl.md) | interface | Implemented by sinks whose device can be stopped and started again, as a phone does when the app leaves the foreground. |
| [`IAudioSinkDiagnostics`](OneGame.Audio/IAudioSinkDiagnostics.md) | interface | Implemented by sinks that can describe their device. |
| [`IPumpedBackend`](OneGame.Audio/IPumpedBackend.md) | interface | Implemented by backends of pumped devices: the output hands over the function the page's timer must call. |
| [`IPumpedSink`](OneGame.Audio/IPumpedSink.md) | interface | Implemented by sinks of pumped backends, where the host calls [`AudioOutput.Pump()`](OneGame.Audio/AudioOutput.md#m-pump) on its own schedule instead of a thread blocking in [`IAudioSink.Write()`](OneGame.Audio/IAudioSink.md#m-write-readonlyspan-float). |
| [`LimiterOptions`](OneGame.Audio/LimiterOptions.md) | struct | The master limiter's settings. |
| [`LimiterStatus`](OneGame.Audio/LimiterStatus.md) | record | The master limiter. |
| [`LoudnessReading`](OneGame.Audio/LoudnessReading.md) | struct | What the loudness meter reads at the output (EBU R 128 / ITU-R BS.1770). |
| [`LoudnessStatus`](OneGame.Audio/LoudnessStatus.md) | record | Loudness at the output. |
| [`MusicOptions`](OneGame.Audio/MusicOptions.md) | struct | How to start a music track. |
| [`MusicStatus`](OneGame.Audio/MusicStatus.md) | record | The playing music track. |
| [`OneAudioCore`](OneGame.Audio/OneAudioCore.md) | class | The engine's mixer: oneaudio's `AudioMixer` with WAV, Ogg Vorbis, Opus, FLAC and MP3 decoders behind the [`IAudioCore`](OneGame.Audio/IAudioCore.md) calls. |
| [`OneAudioCore.Factory`](OneGame.Audio/OneAudioCore.Factory.md) | class | Builds [`OneAudioCore`](OneGame.Audio/OneAudioCore.md)s; the default for [`AudioServiceOptions.CoreFactory`](OneGame.Audio/AudioServiceOptions.md#m-corefactory). |
| [`OutputLimiter`](OneGame.Audio/OutputLimiter.md) | static class | Keeps float samples inside the range a device accepts. |
| [`OutputLimiterMode`](OneGame.Audio/OutputLimiterMode.md) | enum | How the output stage keeps samples in range before they reach the device. |
| [`PlayOptions`](OneGame.Audio/PlayOptions.md) | struct | How to play a [`Sound`](OneGame.Audio/Sound.md). |
| [`PlayedSound`](OneGame.Audio/PlayedSound.md) | record | One sound the game asked the service to start, as the event log keeps it. |
| [`ReverbOptions`](OneGame.Audio/ReverbOptions.md) | struct | A reverb insert on a bus: the bus signal becomes `Dry * signal + Wet * reverb`. |
| [`SinkWriteResult`](OneGame.Audio/SinkWriteResult.md) | enum | The outcome of one [`IAudioSink.Write()`](OneGame.Audio/IAudioSink.md#m-write-readonlyspan-float). |
| [`Sound`](OneGame.Audio/Sound.md) | class | A decoded clip held in memory, ready to play any number of times at once. |
| [`StreamPosition`](OneGame.Audio/StreamPosition.md) | struct | Where a streamed track is. |
| [`VoiceId`](OneGame.Audio/VoiceId.md) | struct | Identifies one playing sound inside a core. |
| [`VoiceRequest`](OneGame.Audio/VoiceRequest.md) | struct | A request to start one voice. |
| [`WasapiBackend`](OneGame.Audio/WasapiBackend.md) | class | Windows output through WASAPI, provided by the device package. |
| [`WebAudioBackend`](OneGame.Audio/WebAudioBackend.md) | class | Output in a web page through Web Audio, behind the device package's backend seam. |
