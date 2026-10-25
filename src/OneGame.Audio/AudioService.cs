namespace OneGame.Audio;

/// <summary>A mixing bus. The service creates three named ones; further indices up to <see cref="AudioServiceOptions.BusCount"/> are free for games.</summary>
/// <param name="Index">The bus index.</param>
public readonly record struct AudioBus(int Index)
{
    /// <summary>Background music.</summary>
    public static AudioBus Music => new(0);

    /// <summary>Sound effects.</summary>
    public static AudioBus Sfx => new(1);

    /// <summary>User interface sounds.</summary>
    public static AudioBus Ui => new(2);
}

/// <summary>A decoded clip held in memory, ready to play any number of times at once.</summary>
public sealed class Sound
{
    internal Sound(object native, string name, double durationSeconds)
    {
        Native = native;
        Name = name;
        DurationSeconds = durationSeconds;
    }

    internal object Native { get; }

    /// <summary>The name given at load time, for diagnostics.</summary>
    public string Name { get; }

    /// <summary>The clip length in seconds, or a negative number when the core can't tell.</summary>
    public double DurationSeconds { get; }
}

/// <summary>How to play a <see cref="Sound"/>.</summary>
public readonly record struct PlayOptions
{
    /// <summary>Creates options for a plain one-shot on the sfx bus.</summary>
    public PlayOptions()
    {
    }

    /// <summary>Linear volume, 0 and up. Default 1.</summary>
    public float Volume { get; init; } = 1f;

    /// <summary>Stereo position, -1 (left) to 1 (right). Default 0.</summary>
    public float Pan { get; init; }

    /// <summary>Whether to restart at the end. Default false.</summary>
    public bool Loop { get; init; }

    /// <summary>The bus to play on. Default <see cref="AudioBus.Sfx"/>.</summary>
    public AudioBus Bus { get; init; } = AudioBus.Sfx;

    /// <summary>Higher priorities win when all voices are busy. Default 0.</summary>
    public int Priority { get; init; }

    /// <summary>Seconds to fade in from silence. Default 0.</summary>
    public double FadeInSeconds { get; init; }

    /// <summary>Speed and pitch multiplier, 0.0625 to 16 (a clip's rate times this must stay within the mixer's conversion range). Default 1.</summary>
    public float Pitch { get; init; } = 1f;

    /// <summary>Set to change the pitch later with <see cref="AudioService.SetPitch"/>; a sound that starts at pitch 1 ignores later changes otherwise. Default false.</summary>
    public bool VariablePitch { get; init; }

    /// <summary>The audio-timeline frame to start at, for sounds that must land on a given frame; 0 plays as soon as possible. Default 0.</summary>
    public long StartAtFrame { get; init; }
}

/// <summary>How to start a music track.</summary>
public readonly record struct MusicOptions
{
    /// <summary>Creates options for a looping track at full volume that cuts in.</summary>
    public MusicOptions()
    {
    }

    /// <summary>Linear volume of the track. Default 1.</summary>
    public float Volume { get; init; } = 1f;

    /// <summary>Whether the track repeats. Default true.</summary>
    public bool Loop { get; init; } = true;

    /// <summary>Seconds over which the previous track fades out and this one fades in. 0 cuts. Default 0.</summary>
    public double CrossfadeSeconds { get; init; }
}

/// <summary>Settings for <see cref="AudioService.Create"/>.</summary>
public sealed record AudioServiceOptions
{
    /// <summary>The device to ask for.</summary>
    public AudioDeviceOptions Device { get; init; } = new();

    /// <summary>The audio system. <see langword="null"/> picks the operating system's own (ALSA on Linux, WASAPI on Windows) and falls back to silence elsewhere.</summary>
    public IAudioBackend? Backend { get; init; }

    /// <summary>Records instead of playing: no device, no thread, and time moves only with <see cref="AudioService.Advance"/>. See <see cref="AudioService.Capture"/>. Overrides <see cref="Backend"/>.</summary>
    public bool Capture { get; init; }

    /// <summary>The mixer. <see langword="null"/> uses <see cref="OneAudioCore"/>: oneaudio's mixer with WAV, Ogg Vorbis and Opus decoding.</summary>
    public IAudioCoreFactory? CoreFactory { get; init; }

    /// <summary>The longest file <see cref="AudioService.LoadSound"/> decodes into memory, in seconds; longer files must be streamed as music. Default 120.</summary>
    public double MaxClipSeconds { get; init; } = 120;

    /// <summary>Most simultaneous voices. Default 64.</summary>
    public int MaxVoices { get; init; } = 64;

    /// <summary>Number of buses; at least 3. Default 8.</summary>
    public int BusCount { get; init; } = 8;

    /// <summary>How the output keeps samples in range. Default soft knee.</summary>
    public OutputLimiterMode Limiter { get; init; } = OutputLimiterMode.SoftKnee;

    /// <summary>Whether <see cref="AudioService.SetFocused"/> with false silences the game. Default true.</summary>
    public bool PauseOnFocusLoss { get; init; } = true;
}

/// <summary>
/// The game-facing audio service: load clips, play sounds and music on buses, set volumes, crossfade, and pump events. Everything here is called from
/// the game thread. It owns the device output and the mixer, and keeps working (silently) when there is no device.
/// </summary>
public sealed class AudioService : IDisposable
{
    private readonly AudioServiceOptions _options;
    private readonly float[] _busVolume;
    private readonly Thread? _pump;
    private float _master = 1f;
    private bool _muted;
    private bool _focused = true;
    private bool _userPaused;
    private int _stopPump;
    private VoiceId _music;

    private AudioService(AudioServiceOptions options, AudioOutput output, IAudioCore core, CaptureAudioBackend? capture)
    {
        _options = options;
        Output = output;
        Core = core;
        _busVolume = new float[options.BusCount];
        Array.Fill(_busVolume, 1f);
        Capture = capture?.Sink;
        if (!output.Format.Equals(new AudioDeviceFormat(core.SampleRate, core.Channels)))
        {
            throw new InvalidOperationException("The core was built for a different format than the output.");
        }

        output.Start(core);
        if (capture is null)
        {
            _pump = new Thread(PumpLoop) { IsBackground = true, Name = "onegame-audio-pump" };
            _pump.Start();
        }
    }

    /// <summary>Raised from <see cref="Update"/> for each event the audio thread produced.</summary>
    public event Action<AudioEvent>? EventRaised;

    /// <summary>The device output, for status, latency and counters.</summary>
    public AudioOutput Output { get; }

    /// <summary>The mixer.</summary>
    public IAudioCore Core { get; }

    /// <summary>The recording, when created with <see cref="AudioServiceOptions.Capture"/>; otherwise <see langword="null"/>.</summary>
    public CaptureSink? Capture { get; }

    /// <summary>The format everything renders at: the device's own, or the requested one with no device.</summary>
    public AudioDeviceFormat Format => Output.Format;

    /// <summary>The playing music voice, or an invalid id.</summary>
    public VoiceId CurrentMusic => _music;

    /// <summary>Opens the device, builds the mixer at the device's format and starts audio. A missing or failing device is not an error: <see cref="AudioOutput.Status"/> reports <see cref="AudioOutputStatus.NoDevice"/> and the game runs silently.</summary>
    /// <param name="options">Settings; defaults are fine for most games.</param>
    /// <returns>The service.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A setting is out of range.</exception>
    public static AudioService Create(AudioServiceOptions? options = null)
    {
        options ??= new AudioServiceOptions();
        ArgumentOutOfRangeException.ThrowIfLessThan(options.BusCount, 3);
        ArgumentOutOfRangeException.ThrowIfLessThan(options.MaxVoices, 1);

        CaptureAudioBackend? capture = options.Capture ? new CaptureAudioBackend() : null;
        var backend = capture ?? options.Backend ?? DefaultBackend();
        var output = AudioOutput.Create(backend, options.Device, options.Limiter);
        var core = (options.CoreFactory ?? new OneAudioCore.Factory(options.MaxClipSeconds)).Create(output.Format.SampleRate, output.Format.Channels, options.MaxVoices, options.BusCount);
        try
        {
            return new AudioService(options, output, core, capture);
        }
        catch
        {
            core.Dispose();
            output.Dispose();
            throw;
        }
    }

    /// <summary>The backend for this operating system, or one that never opens a device.</summary>
    /// <returns>A backend.</returns>
    public static IAudioBackend DefaultBackend() =>
        OperatingSystem.IsLinux() ? new AlsaBackend()
        : OperatingSystem.IsWindows() ? new WasapiBackend()
        : new NoDeviceBackend();

    /// <summary>The master volume, 0 and up; scales every bus.</summary>
    public float MasterVolume
    {
        get => _master;
        set
        {
            _master = Math.Max(0f, value);
            ApplyBusGains();
        }
    }

    /// <summary>Silences everything without changing any volume.</summary>
    public bool Muted
    {
        get => _muted;
        set
        {
            _muted = value;
            ApplyBusGains();
        }
    }

    /// <summary>Stops sound and holds the mixer's clock, for a pause menu or a lost window. Voices resume where they were.</summary>
    public bool Paused
    {
        get => _userPaused;
        set
        {
            _userPaused = value;
            ApplyPause();
        }
    }

    /// <summary>Tells the service whether the game window has focus. When <see cref="AudioServiceOptions.PauseOnFocusLoss"/> is set, losing focus pauses audio.</summary>
    /// <param name="focused">Whether the window is focused.</param>
    public void SetFocused(bool focused)
    {
        _focused = focused;
        ApplyPause();
    }

    /// <summary>Sets a bus's volume (linear, 0 and up). The effective gain is this times the master volume.</summary>
    /// <param name="bus">The bus.</param>
    /// <param name="volume">The volume.</param>
    /// <exception cref="ArgumentOutOfRangeException">The bus index is not below <see cref="AudioServiceOptions.BusCount"/>.</exception>
    public void SetBusVolume(AudioBus bus, float volume)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bus.Index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(bus.Index, _busVolume.Length);
        _busVolume[bus.Index] = Math.Max(0f, volume);
        ApplyBusGains();
    }

    /// <summary>Reads back a bus's volume.</summary>
    /// <param name="bus">The bus.</param>
    /// <returns>The volume set by <see cref="SetBusVolume"/>.</returns>
    public float GetBusVolume(AudioBus bus)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bus.Index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(bus.Index, _busVolume.Length);
        return _busVolume[bus.Index];
    }

    /// <summary>Decodes a whole audio file into memory.</summary>
    /// <param name="data">The encoded file; read to the end and not kept.</param>
    /// <param name="name">A label for diagnostics.</param>
    /// <returns>The clip.</returns>
    public Sound LoadSound(Stream data, string name = "sound")
    {
        ArgumentNullException.ThrowIfNull(data);
        var native = Core.LoadClip(data);
        return new Sound(native, name, Core.DurationSeconds(native));
    }

    /// <summary>Starts a sound.</summary>
    /// <param name="sound">A clip from <see cref="LoadSound"/>.</param>
    /// <param name="options">How to play it; defaults to a one-shot on the sfx bus.</param>
    /// <returns>The voice, or an invalid id when the command queue was full (a dropped sound).</returns>
    public VoiceId Play(Sound sound, PlayOptions options = default)
    {
        ArgumentNullException.ThrowIfNull(sound);
        if (options.Equals(default(PlayOptions)))
        {
            options = new PlayOptions();
        }

        return Core.Play(Request(sound.Native, options.Volume, options.Pan, options.Loop, options.Bus.Index, options.Priority, options.FadeInSeconds, options.StartAtFrame, options.Pitch, options.VariablePitch));
    }

    /// <summary>Stops a voice.</summary>
    /// <param name="voice">The voice.</param>
    /// <param name="fadeOutSeconds">Fade length; 0 stops at once.</param>
    public void Stop(VoiceId voice, double fadeOutSeconds = 0)
    {
        Core.StopVoice(voice, Frames(fadeOutSeconds));
        if (voice == _music)
        {
            _music = default;
        }
    }

    /// <summary>Stops every voice, music included.</summary>
    /// <param name="fadeOutSeconds">Fade length; 0 stops at once.</param>
    public void StopAll(double fadeOutSeconds = 0)
    {
        Core.StopAll(Frames(fadeOutSeconds));
        _music = default;
    }

    /// <summary>Changes a playing voice's volume.</summary>
    /// <param name="voice">The voice.</param>
    /// <param name="volume">Linear volume.</param>
    /// <param name="rampSeconds">Time to reach it; 0 jumps.</param>
    public void SetVolume(VoiceId voice, float volume, double rampSeconds = 0) => Core.SetGain(voice, Math.Max(0f, volume), Frames(rampSeconds));

    /// <summary>Changes a playing voice's stereo position.</summary>
    /// <param name="voice">The voice.</param>
    /// <param name="pan">-1 to 1.</param>
    public void SetPan(VoiceId voice, float pan) => Core.SetPan(voice, Math.Clamp(pan, -1f, 1f));

    /// <summary>Changes a playing voice's speed and pitch. Only voices started with a pitch other than 1 or with <see cref="PlayOptions.VariablePitch"/> respond.</summary>
    /// <param name="voice">The voice.</param>
    /// <param name="pitch">Speed multiplier; 1 is the sound's own.</param>
    /// <param name="rampSeconds">Time to glide there; 0 jumps.</param>
    public void SetPitch(VoiceId voice, float pitch, double rampSeconds = 0) => Core.SetPitch(voice, pitch, Frames(rampSeconds));

    /// <summary>Mutes or unmutes a bus without changing its volume.</summary>
    /// <param name="bus">The bus.</param>
    /// <param name="muted">Whether to silence it.</param>
    /// <exception cref="ArgumentOutOfRangeException">The bus index is not below <see cref="AudioServiceOptions.BusCount"/>.</exception>
    public void SetBusMuted(AudioBus bus, bool muted)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bus.Index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(bus.Index, _busVolume.Length);
        Core.SetBusMute(bus.Index, muted);
    }

    /// <summary>Turns <paramref name="target"/> down while <paramref name="watched"/> is loud, for example music under speech.</summary>
    /// <param name="target">The bus that is turned down.</param>
    /// <param name="watched">The bus whose level triggers it.</param>
    /// <param name="settings">Threshold, depth and timing; defaults duck 12 dB.</param>
    /// <exception cref="ArgumentOutOfRangeException">A bus index is out of range.</exception>
    /// <exception cref="ArgumentException">Both are the same bus, or the link would be circular.</exception>
    public void SetDucking(AudioBus target, AudioBus watched, DuckingOptions settings = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(target.Index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(target.Index, _busVolume.Length);
        ArgumentOutOfRangeException.ThrowIfNegative(watched.Index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(watched.Index, _busVolume.Length);
        if (settings.Equals(default(DuckingOptions)))
        {
            settings = new DuckingOptions();
        }

        Core.SetDucking(target.Index, watched.Index, settings);
    }

    /// <summary>Removes the ducking of a bus.</summary>
    /// <param name="target">The bus.</param>
    public void ClearDucking(AudioBus target) => Core.ClearDucking(target.Index);

    /// <summary>Starts a streamed music track on the music bus. If music is already playing it is replaced, with a crossfade when asked for.</summary>
    /// <param name="data">The encoded file; the stream is owned by the track and closed when it ends.</param>
    /// <param name="options">Volume, looping and crossfade.</param>
    /// <returns>The voice, or an invalid id when the command queue was full.</returns>
    public VoiceId PlayMusic(Stream data, MusicOptions options = default)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (options.Equals(default(MusicOptions)))
        {
            options = new MusicOptions();
        }

        var source = Core.OpenStream(data, options.Loop);
        var fade = Frames(options.CrossfadeSeconds);
        var request = new VoiceRequest { Source = source, Gain = options.Volume, Bus = AudioBus.Music.Index, Loop = options.Loop, Priority = int.MaxValue / 2, FadeInFrames = fade };
        var previous = _music;
        VoiceId voice;
        if (previous.IsValid && fade > 0)
        {
            voice = Core.Crossfade(previous, request, fade);
        }
        else
        {
            if (previous.IsValid)
            {
                Core.StopVoice(previous, 0);
            }

            voice = Core.Play(request);
        }

        _music = voice;
        return voice;
    }

    /// <summary>Fades out and stops the current music.</summary>
    /// <param name="fadeOutSeconds">Fade length; 0 stops at once.</param>
    public void StopMusic(double fadeOutSeconds = 0)
    {
        if (_music.IsValid)
        {
            Core.StopVoice(_music, Frames(fadeOutSeconds));
            _music = default;
        }
    }

    /// <summary>Call once per game frame: drains the mixer's events, forgets a music track that ended and raises <see cref="EventRaised"/>.</summary>
    public void Update()
    {
        while (Core.TryGetEvent(out var e))
        {
            if (e.Kind == AudioEventKind.VoiceEnded && e.Voice == _music)
            {
                _music = default;
            }

            EventRaised?.Invoke(e);
        }
    }

    /// <summary>Capture mode only: feeds streams, then renders exactly <paramref name="frames"/> frames into <see cref="Capture"/>.</summary>
    /// <param name="frames">Frames to render.</param>
    /// <exception cref="InvalidOperationException">The service is not in capture mode.</exception>
    public void Advance(int frames)
    {
        if (Capture is null)
        {
            throw new InvalidOperationException("Advance is only available when the service was created with Capture = true.");
        }

        // Streams are filled before every slice so the capture never depends on a pump thread's timing, however long the advance is.
        const int Slice = 2048;
        for (var left = frames; left > 0; left -= Slice)
        {
            Core.Pump();
            Output.Advance(Math.Min(left, Slice));
        }
    }

    /// <summary>Stops audio, the pump thread and the device.</summary>
    public void Dispose()
    {
        Volatile.Write(ref _stopPump, 1);
        _pump?.Join(TimeSpan.FromSeconds(2));
        Output.Dispose();
        Core.Dispose();
    }

    private VoiceRequest Request(object source, float volume, float pan, bool loop, int bus, int priority, double fadeInSeconds, long startAt, float pitch = 1f, bool variablePitch = false) => new()
    {
        Source = source,
        Gain = volume,
        Pan = pan,
        Loop = loop,
        Bus = bus,
        Priority = priority,
        FadeInFrames = Frames(fadeInSeconds),
        StartAtFrame = startAt,
        Pitch = pitch,
        VariablePitch = variablePitch,
    };

    private int Frames(double seconds) => (int)Math.Clamp(Math.Round(seconds * Format.SampleRate), 0, int.MaxValue / 2);

    private void ApplyBusGains()
    {
        for (var i = 0; i < _busVolume.Length; i++)
        {
            Core.SetBusGain(i, _muted ? 0f : _master * _busVolume[i]);
        }
    }

    private void ApplyPause() => Output.Paused = _userPaused || (!_focused && _options.PauseOnFocusLoss);

    private void PumpLoop()
    {
        while (Volatile.Read(ref _stopPump) == 0)
        {
            try
            {
                Core.Pump();
            }
            catch (Exception e) when (e is not OutOfMemoryException)
            {
                // A broken stream must not take the pump down; its voice plays silence.
            }

            Thread.Sleep(5);
        }
    }

    private sealed class NoDeviceBackend : IAudioBackend
    {
        public string Name => "none";

        public bool IsManual => false;

        public IReadOnlyList<AudioDeviceInfo> ListDevices() => [];

        public bool TryOpen(AudioDeviceOptions options, out IAudioSink? sink, out string reason)
        {
            sink = null;
            reason = "no audio backend for this platform";
            return false;
        }
    }
}
