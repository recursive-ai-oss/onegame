using OneAudio.Loudness;

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

    /// <summary>The mixer. <see langword="null"/> uses <see cref="OneAudioCore"/>: oneaudio's mixer with WAV, Ogg Vorbis, Opus, FLAC and MP3 decoding.</summary>
    public IAudioCoreFactory? CoreFactory { get; init; }

    /// <summary>The longest file <see cref="AudioService.LoadSound"/> decodes into memory, in seconds; longer files must be streamed as music. Default 120.</summary>
    public double MaxClipSeconds { get; init; } = 120;

    /// <summary>Most simultaneous voices. Default 64.</summary>
    public int MaxVoices { get; init; } = 64;

    /// <summary>Number of buses; at least 3. Default 8.</summary>
    public int BusCount { get; init; } = 8;

    /// <summary>How the output keeps samples in range. Default soft knee.</summary>
    public OutputLimiterMode Limiter { get; init; } = OutputLimiterMode.SoftKnee;

    /// <summary>Reverbs that can be active at once across all buses, 0 to 16. Each is built at startup and costs about 0.3 MB. Default 2.</summary>
    public int MaxReverbs { get; init; } = 2;

    /// <summary>
    /// A lookahead limiter on the master output: no sample passes the ceiling, and it reduces gain gently before a peak instead of clipping it. <see langword="null"/> (the default) builds none.
    /// A limiter delays all audio by <see cref="LimiterLookaheadMilliseconds"/> for the life of the service, and can be retuned or switched off with <see cref="AudioService.SetMasterLimiter"/> but not added later.
    /// </summary>
    public LimiterOptions? MasterLimiter { get; init; }

    /// <summary>The master limiter's lookahead in milliseconds, 1 to 100; only used when <see cref="MasterLimiter"/> is set. Default 5.</summary>
    public int LimiterLookaheadMilliseconds { get; init; } = 5;

    /// <summary>
    /// Buses whose loudness can be measured at once, 0 to 64 (see <see cref="AudioService.EnableBusMeter"/>). Each costs about 0.1 MB, built at startup.
    /// When <see cref="Metering"/> is on, the music, sfx and ui buses (as many as fit) are metered from the start. Default 3.
    /// </summary>
    public int MaxBusMeters { get; init; } = 3;

    /// <summary>Whether the output measures loudness (EBU R 128), readable as <see cref="AudioService.Loudness"/>. The cost is small. Default true.</summary>
    public bool Metering { get; init; } = true;

    /// <summary>Whether <see cref="AudioService.SetFocused"/> with false silences the game. Default true.</summary>
    public bool PauseOnFocusLoss { get; init; } = true;
}

/// <summary>Where a streamed track is.</summary>
/// <param name="PositionSeconds">Seconds into the track; wraps with a loop.</param>
/// <param name="LengthSeconds">The track's length, or a negative number when the format can't say.</param>
public readonly record struct StreamPosition(double PositionSeconds, double LengthSeconds);

/// <summary>A bus filter's setting.</summary>
/// <param name="CutoffHz">The corner frequency.</param>
/// <param name="Q">The resonance; 0 means flat.</param>
public sealed record FilterStatus(float CutoffHz, float Q);

/// <summary>A bus's ducking link.</summary>
/// <param name="WatchedBus">The bus whose level triggers it.</param>
/// <param name="ThresholdDb">Trigger level in dBFS.</param>
/// <param name="ReductionDb">Depth in dB.</param>
/// <param name="AttackMs">Attack time constant.</param>
/// <param name="ReleaseMs">Release time constant.</param>
public sealed record DuckingStatus(int WatchedBus, float ThresholdDb, float ReductionDb, float AttackMs, float ReleaseMs);

/// <summary>One bus as the service set it up.</summary>
/// <param name="Index">The bus index.</param>
/// <param name="Name"><c>music</c>, <c>sfx</c> or <c>ui</c> for the named buses, otherwise null.</param>
/// <param name="Volume">The bus volume (before the master volume).</param>
/// <param name="Muted">Whether it is muted.</param>
/// <param name="LowPass">Its low-pass, or null.</param>
/// <param name="HighPass">Its high-pass, or null.</param>
/// <param name="Reverb">Its reverb, or null.</param>
/// <param name="Ducking">What ducks it, or null.</param>
/// <param name="Loudness">Its loudness (see <see cref="AudioService.GetBusLoudness"/>), or null when the bus is not metered.</param>
/// <param name="Metered">Whether a loudness meter is assigned to the bus (a reading follows at the next block).</param>
public sealed record BusStatus(int Index, string? Name, float Volume, bool Muted, FilterStatus? LowPass, FilterStatus? HighPass, ReverbOptions? Reverb, DuckingStatus? Ducking, LoudnessStatus? Loudness = null, bool Metered = false);

/// <summary>What asking for a bus meter came to. Nothing throws for any of these.</summary>
public enum BusMeterResult
{
    /// <summary>The bus is metered, or its meter starts at the next block.</summary>
    Metered,

    /// <summary>Every meter slot is in use by other buses (<see cref="AudioService.FreeBusMeters"/> is 0). Disable one first.</summary>
    NoFreeSlot,

    /// <summary>A slot is free but the mixer's command queue was full. Nothing changed; try again.</summary>
    QueueFull,

    /// <summary>This service measures no buses: metering is off, the sample rate is too low, or <see cref="AudioServiceOptions.MaxBusMeters"/> is 0.</summary>
    Unavailable,
}

/// <summary>The master limiter.</summary>
/// <param name="Enabled">Whether it is switched on.</param>
/// <param name="CeilingDb">Its ceiling, when on.</param>
/// <param name="ReleaseMs">Its release, when on.</param>
/// <param name="GainReductionDb">The reduction in the last block (0 or negative).</param>
public sealed record LimiterStatus(bool Enabled, float? CeilingDb, float? ReleaseMs, double GainReductionDb);

/// <summary>Loudness at the output. A value is null while it is silent or unmeasured.</summary>
/// <param name="MomentaryLufs">Over the last 400 ms.</param>
/// <param name="ShortTermLufs">Over the last 3 s.</param>
/// <param name="IntegratedLufs">Gated, since the last reset.</param>
/// <param name="TruePeakDbtp">Highest true peak since the last reset.</param>
/// <param name="MeasuredSeconds">Audio measured since the last reset.</param>
public sealed record LoudnessStatus(double? MomentaryLufs, double? ShortTermLufs, double? IntegratedLufs, double? TruePeakDbtp, double MeasuredSeconds);

/// <summary>The playing music track.</summary>
/// <param name="PositionSeconds">Seconds into the track.</param>
/// <param name="LengthSeconds">Its length, or null when unknown.</param>
public sealed record MusicStatus(double PositionSeconds, double? LengthSeconds);

/// <summary>The mixing state, from <see cref="AudioService.Describe"/>.</summary>
/// <param name="SampleRate">Output frames per second.</param>
/// <param name="Channels">Output channels.</param>
/// <param name="Output">The output's status (<c>Running</c>, <c>NoDevice</c>, ...).</param>
/// <param name="Capture">Whether the service records instead of playing.</param>
/// <param name="MasterVolume">The master volume.</param>
/// <param name="Muted">Whether everything is muted.</param>
/// <param name="ActiveVoices">Voices playing now.</param>
/// <param name="Buses">Every bus.</param>
/// <param name="Limiter">The master limiter, or null when the service has none.</param>
/// <param name="Loudness">The output loudness.</param>
/// <param name="Music">The music track, or null when none plays.</param>
/// <param name="FreeBusMeters">Loudness meter slots not in use.</param>
/// <param name="Device">The device side: backend, its state, underruns, and the frames and peak the device has taken.</param>
/// <param name="MeterRequest">What a meter request in the same call came to (the <c>audio</c> operation's <c>enableMeter</c>/<c>disableMeter</c>): metered, no_free_slot, queue_full, unavailable or disabled; null otherwise.</param>
public sealed record AudioStatus(
    int SampleRate, int Channels, string Output, bool Capture, float MasterVolume, bool Muted, int ActiveVoices, BusStatus[] Buses, LimiterStatus? Limiter, LoudnessStatus Loudness, MusicStatus? Music, int FreeBusMeters = 0, string? MeterRequest = null, AudioDeviceStatus? Device = null);

/// <summary>
/// The game-facing audio service: load clips, play sounds and music on buses, set volumes, crossfade, and pump events. Everything here is called from
/// the game thread. It owns the device output and the mixer, and keeps working (silently) when there is no device.
/// </summary>
public sealed class AudioService : IDisposable
{
    private static AudioService? s_active;
    private readonly AudioServiceOptions _options;
    private readonly float[] _busVolume;
    private readonly Thread? _pump;
    private float _master = 1f;
    private bool _muted;
    private bool _focused = true;
    private bool _userPaused;
    private int _stopPump;
    private VoiceId _music;
    private readonly BusSettings[] _busState;
    private LimiterOptions? _limiterState;

    private AudioService(AudioServiceOptions options, AudioOutput output, IAudioCore core, CaptureAudioBackend? capture)
    {
        _options = options;
        Output = output;
        Core = core;
        _busVolume = new float[options.BusCount];
        Array.Fill(_busVolume, 1f);
        _busState = new BusSettings[options.BusCount];
        _limiterState = options.MasterLimiter;
        Capture = capture?.Sink;
        if (!output.Format.Equals(new AudioDeviceFormat(core.SampleRate, core.Channels)))
        {
            throw new InvalidOperationException("The core was built for a different format than the output.");
        }

        output.Start(core);
        if (capture is null && output.IsPumped)
        {
            // A web page has no threads: the page's timer calls the output, and the stream feeding rides along.
            output.BeforePump = () => Core.Pump();
        }
        else if (capture is null)
        {
            _pump = new Thread(PumpLoop) { IsBackground = true, Name = "onegame-audio-pump" };
            _pump.Start();
        }
    }

    /// <summary>The most recently created service that is still alive, so the harness can reach a game's audio without the game handing it over. <see langword="null"/> when none.</summary>
    public static AudioService? Active => Volatile.Read(ref s_active);

    /// <summary>Raised from <see cref="Update"/> for each event the audio thread produced.</summary>
    public event Action<AudioEvent>? EventRaised;

    /// <summary>The device output, for status, latency and counters.</summary>
    public AudioOutput Output { get; }

    /// <summary>The mixer.</summary>
    public IAudioCore Core { get; }

    /// <summary>The recording, when created with <see cref="AudioServiceOptions.Capture"/>; otherwise <see langword="null"/>.</summary>
    public CaptureSink? Capture { get; }

    /// <summary>How many buses this service can meter at once: <see cref="AudioServiceOptions.MaxBusMeters"/>, or 0 when metering is off or the rate is too low.</summary>
    private int MaxMeters { get; init; }

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
        ArgumentOutOfRangeException.ThrowIfLessThan(options.MaxReverbs, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(options.MaxBusMeters, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(options.MaxBusMeters, 64);
        if (options.MasterLimiter is not null)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(options.LimiterLookaheadMilliseconds, 1);
        }

        var output = AudioOutput.Create(backend, options.Device, options.Limiter);
        output.Metering = options.Metering;
        var settings = new AudioCoreSettings(output.Format.SampleRate, output.Format.Channels, options.MaxVoices, options.BusCount)
        {
            MaxReverbs = options.MaxReverbs,
            // The mixer's meters need a sample rate the loudness filters support; below it there are none.
            MaxBusMeters = options.Metering && output.Format.SampleRate >= LoudnessMeter.MinSampleRate ? options.MaxBusMeters : 0,
            LimiterLookaheadMilliseconds = options.MasterLimiter is null ? 0 : options.LimiterLookaheadMilliseconds,
        };
        var core = (options.CoreFactory ?? new OneAudioCore.Factory(options.MaxClipSeconds)).Create(settings);
        try
        {
            var service = new AudioService(options, output, core, capture) { MaxMeters = settings.MaxBusMeters };
            if (options.MasterLimiter is { } limiter)
            {
                core.SetLimiter(limiter);
            }

            for (var i = 0; i < Math.Min(settings.MaxBusMeters, 3); i++)
            {
                service.EnableBusMeter(new AudioBus(i));
            }

            Volatile.Write(ref s_active, service);
            return service;
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
        // Android is Linux underneath, so it is asked first.
        IsAndroidProcess() ? new AAudioBackend()
        : OperatingSystem.IsLinux() ? new AlsaBackend()
        : OperatingSystem.IsWindows() ? new WasapiBackend()
        : OperatingSystem.IsIOS() || OperatingSystem.IsMacOS() ? new AudioUnitBackend()
        : OperatingSystem.IsBrowser() ? new WebAudioBackend()
        : new NoDeviceBackend();

    // A NativeAOT game built for linux-bionic runs on the Linux runtime, where OperatingSystem.IsAndroid() is false; every Android app process
    // does have the ANDROID_ROOT variable the system exports to it.
    private static bool IsAndroidProcess() =>
        OperatingSystem.IsAndroid() || (OperatingSystem.IsLinux() && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ANDROID_ROOT")));

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

    /// <summary>Tells the service the app left (<see langword="true"/>) or returned to the foreground. A suspended service renders nothing and stops the device, so a phone's audio hardware is released in the background; voices resume where they were. The host calls this on app lifecycle events.</summary>
    /// <param name="suspended">Whether the app is in the background.</param>
    public void SetSuspended(bool suspended) => Output.Suspended = suspended;

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
        _busState[bus.Index].Muted = muted;
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

        if (!settings.Enabled)
        {
            ClearDucking(target);
            return;
        }

        Core.SetDucking(target.Index, watched.Index, settings);
        _busState[target.Index].Ducking = new DuckingStatus(watched.Index, settings.ThresholdDb, settings.ReductionDb, settings.AttackMs, settings.ReleaseMs);
    }

    /// <summary>Removes the ducking of a bus.</summary>
    /// <param name="target">The bus.</param>
    public void ClearDucking(AudioBus target)
    {
        CheckBus(target);
        Core.ClearDucking(target.Index);
        _busState[target.Index].Ducking = null;
    }

    /// <summary>Turns on or retunes a low-pass or high-pass filter (12 dB per octave) on a bus. A low-pass and a high-pass together make a band-pass. Changes glide, so sweeps make no clicks.</summary>
    /// <param name="bus">The bus.</param>
    /// <param name="kind">Which filter.</param>
    /// <param name="cutoffHz">The corner frequency, where the level is 3 dB down with the default <paramref name="q"/>.</param>
    /// <param name="q">Resonance; 0 (default) is flat. Values above 0.71 add a peak at the corner.</param>
    /// <param name="glideSeconds">Time to glide to the new setting. Default 0.02.</param>
    /// <exception cref="ArgumentOutOfRangeException">The bus is out of range, the cutoff is not above 0, or Q is negative.</exception>
    public void SetBusFilter(AudioBus bus, AudioFilterKind kind, float cutoffHz, float q = 0f, double glideSeconds = 0.02)
    {
        CheckBus(bus);
        CheckFilter(cutoffHz, q);
        Core.SetBusFilter(bus.Index, kind, cutoffHz, q, Frames(glideSeconds));
        var f = new FilterStatus(cutoffHz, q);
        if (kind == AudioFilterKind.LowPass)
        {
            _busState[bus.Index].LowPass = f;
        }
        else
        {
            _busState[bus.Index].HighPass = f;
        }
    }

    /// <summary>Turns a bus filter off.</summary>
    /// <param name="bus">The bus.</param>
    /// <param name="kind">Which filter.</param>
    /// <param name="glideSeconds">Time to glide to transparent first. Default 0.02.</param>
    public void ClearBusFilter(AudioBus bus, AudioFilterKind kind, double glideSeconds = 0.02)
    {
        CheckBus(bus);
        Core.ClearBusFilter(bus.Index, kind, Frames(glideSeconds));
        if (kind == AudioFilterKind.LowPass)
        {
            _busState[bus.Index].LowPass = null;
        }
        else
        {
            _busState[bus.Index].HighPass = null;
        }
    }

    /// <summary>Filters one voice before its volume and pan, for example a muffling low-pass on a sound behind a wall. A stale voice is ignored.</summary>
    /// <param name="voice">The voice.</param>
    /// <param name="kind">Which filter.</param>
    /// <param name="cutoffHz">The corner frequency.</param>
    /// <param name="q">Resonance; 0 (default) is flat.</param>
    /// <param name="glideSeconds">Time to glide to the new setting. Default 0.02.</param>
    /// <exception cref="ArgumentOutOfRangeException">The cutoff is not above 0 or Q is negative.</exception>
    public void SetVoiceFilter(VoiceId voice, AudioFilterKind kind, float cutoffHz, float q = 0f, double glideSeconds = 0.02)
    {
        CheckFilter(cutoffHz, q);
        Core.SetVoiceFilter(voice, kind, cutoffHz, q, Frames(glideSeconds));
    }

    /// <summary>Turns a voice filter off.</summary>
    /// <param name="voice">The voice.</param>
    /// <param name="kind">Which filter.</param>
    /// <param name="glideSeconds">Time to glide to transparent first. Default 0.02.</param>
    public void ClearVoiceFilter(VoiceId voice, AudioFilterKind kind, double glideSeconds = 0.02) => Core.ClearVoiceFilter(voice, kind, Frames(glideSeconds));

    /// <summary>Puts a reverb on a bus, or retunes the one there. The tail keeps ringing after the bus's sounds stop. At most <see cref="AudioServiceOptions.MaxReverbs"/> buses can have one at once.</summary>
    /// <param name="bus">The bus.</param>
    /// <param name="options">Room size, decay, damping, pre-delay and the wet/dry mix.</param>
    /// <param name="glideSeconds">Time changes take. Default 0.05.</param>
    /// <returns><see langword="false"/> when every reverb is already in use by other buses (check <see cref="FreeReverbSlots"/>; see <see cref="AudioServiceOptions.MaxReverbs"/>) or the command queue was full; nothing changed then. It never throws for a lack of slots.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The bus is out of range.</exception>
    /// <exception cref="ArgumentException">A reverb setting is out of range.</exception>
    public bool SetBusReverb(AudioBus bus, ReverbOptions options, double glideSeconds = 0.05)
    {
        CheckBus(bus);
        if (options.Equals(default(ReverbOptions)))
        {
            options = new ReverbOptions();
        }

        if (!Core.SetBusReverb(bus.Index, options, Frames(glideSeconds)))
        {
            return false;
        }

        _busState[bus.Index].Reverb = options;
        return true;
    }

    /// <summary>Reverbs that can still be put on buses that have none.</summary>
    public int FreeReverbSlots => Core.FreeReverbSlots;

    /// <summary>Removes a bus's reverb. The tail stops at once; to fade it, lower <see cref="ReverbOptions.Wet"/> first.</summary>
    /// <param name="bus">The bus.</param>
    public void ClearBusReverb(AudioBus bus)
    {
        CheckBus(bus);
        Core.ClearBusReverb(bus.Index);
        _busState[bus.Index].Reverb = null;
    }

    /// <summary>Retunes the master limiter, or switches it off with <see langword="null"/>. Needs <see cref="AudioServiceOptions.MasterLimiter"/> at creation, which builds the lookahead.</summary>
    /// <param name="options">Ceiling and release, or null for off.</param>
    /// <exception cref="InvalidOperationException">The service was created without a master limiter.</exception>
    public void SetMasterLimiter(LimiterOptions? options)
    {
        if (!Core.HasLimiter)
        {
            throw new InvalidOperationException("The service was created without a master limiter. Set AudioServiceOptions.MasterLimiter.");
        }

        Core.SetLimiter(options);
        _limiterState = options;
    }

    /// <summary>The loudness at the output after the limiter, as the speakers get it: momentary, short-term, gated integrated (EBU R 128) and true peak. All -infinity until sound has rendered, and always so when <see cref="AudioServiceOptions.Metering"/> is off.</summary>
    public LoudnessReading Loudness => Output.Loudness;

    /// <summary>Starts the integrated loudness and true peak over, for measuring a scene from its beginning. Metered buses start over too.</summary>
    public void ResetLoudness()
    {
        Output.ResetLoudness();
        for (var i = 0; i < _busState.Length; i++)
        {
            if (Core.IsBusMetered(i))
            {
                Core.ResetBusMeter(i);
            }
        }
    }

    /// <summary>Meter slots not yet used by a bus (see <see cref="AudioServiceOptions.MaxBusMeters"/>).</summary>
    public int FreeBusMeters => Core.FreeBusMeters;

    /// <summary>Whether a meter is assigned to a bus. A reading follows at the next block.</summary>
    /// <param name="bus">The bus.</param>
    /// <returns>True when the bus is metered or its meter is about to start.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The bus is out of range.</exception>
    public bool IsBusMetered(AudioBus bus)
    {
        CheckBus(bus);
        return Core.IsBusMetered(bus.Index);
    }

    /// <summary>
    /// Starts measuring a bus's loudness (EBU R 128) so <see cref="GetBusLoudness"/> can read it. The music, sfx and ui buses are metered at startup; use this for others or after <see cref="DisableBusMeter"/>.
    /// A bus is measured as it leaves the bus: after its sounds, filters, reverb, volume and mute, but before ducking and before the master volume and limiter,
    /// so the reading is what the bus itself puts out.
    /// </summary>
    /// <param name="bus">The bus.</param>
    /// <returns>True when the bus is metered or is about to be. False when no meter slot is free (see <see cref="FreeBusMeters"/>), metering is off or the sample rate is too low, or the command queue was full; nothing changed then. It never throws for a lack of slots. <see cref="TryEnableBusMeter"/> says which.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The bus is out of range.</exception>
    public bool EnableBusMeter(AudioBus bus) => TryEnableBusMeter(bus) == BusMeterResult.Metered;

    /// <summary>Like <see cref="EnableBusMeter"/>, and says why a meter did not start.</summary>
    /// <param name="bus">The bus.</param>
    /// <returns>The outcome; see <see cref="BusMeterResult"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The bus is out of range.</exception>
    public BusMeterResult TryEnableBusMeter(AudioBus bus)
    {
        CheckBus(bus);
        if (Core.IsBusMetered(bus.Index) || Core.EnableBusMeter(bus.Index))
        {
            return BusMeterResult.Metered;
        }

        // The mixer's own rule: no free slot means 0 free; any other failure left a slot free and was a full queue.
        return Core.FreeBusMeters > 0 ? BusMeterResult.QueueFull : MaxMeters == 0 ? BusMeterResult.Unavailable : BusMeterResult.NoFreeSlot;
    }

    /// <summary>Stops measuring a bus and frees its meter slot.</summary>
    /// <param name="bus">The bus.</param>
    /// <returns>False when the command queue was full (the meter keeps running).</returns>
    /// <exception cref="ArgumentOutOfRangeException">The bus is out of range.</exception>
    public bool DisableBusMeter(AudioBus bus)
    {
        CheckBus(bus);
        return !Core.IsBusMetered(bus.Index) || Core.DisableBusMeter(bus.Index);
    }

    /// <summary>
    /// Reads a bus's loudness: momentary, short-term, gated integrated and true peak, with silence as -infinity (as <see cref="Loudness"/>).
    /// Ducking, the master volume and the limiter do not change it; use <see cref="Loudness"/> for what the speakers get.
    /// </summary>
    /// <param name="bus">The bus.</param>
    /// <returns>The reading, or <see langword="null"/> when the bus is not metered (yet: a meter starts at the next block).</returns>
    /// <exception cref="ArgumentOutOfRangeException">The bus is out of range.</exception>
    public LoudnessReading? GetBusLoudness(AudioBus bus)
    {
        CheckBus(bus);
        return Core.TryGetBusLoudness(bus.Index, out var r) ? r : null;
    }

    /// <summary>Where a streamed track is. Positions are source time, ahead of the speakers by the mixer's and device's buffering.</summary>
    /// <param name="voice">A voice from <see cref="PlayMusic"/>.</param>
    /// <returns>The position and length, or <see langword="null"/> when the voice is not a live stream.</returns>
    public StreamPosition? GetStreamPosition(VoiceId voice) =>
        Core.TryGetStreamPosition(voice, out var position, out var length) ? new StreamPosition(position, length) : null;

    /// <summary>Describes the mixing state: every bus (volume, mute, filters, reverb, ducking), the master limiter, the loudness and the music track. This is what the harness reports.</summary>
    /// <returns>A snapshot.</returns>
    public AudioStatus Describe()
    {
        var buses = new BusStatus[_busVolume.Length];
        for (var i = 0; i < buses.Length; i++)
        {
            var b = _busState[i];
            buses[i] = new BusStatus(i, i switch { 0 => "music", 1 => "sfx", 2 => "ui", _ => null }, _busVolume[i], b.Muted, b.LowPass, b.HighPass, b.Reverb, b.Ducking, GetBusLoudness(new AudioBus(i)) is { } bl ? Status(bl) : null, Core.IsBusMetered(i));
        }

        var l = Loudness;
        var limiter = Core.HasLimiter ? new LimiterStatus(_limiterState is not null, _limiterState?.CeilingDb, _limiterState?.ReleaseMilliseconds, Core.LimiterGainReductionDb) : null;
        var music = GetStreamPosition(_music);
        return new AudioStatus(
            Format.SampleRate, Format.Channels, Output.Status.ToString(), Capture is not null, _master, _muted, Core.ActiveVoices, buses, limiter,
            Status(l),
            music is { } m ? new MusicStatus(m.PositionSeconds, m.LengthSeconds < 0 ? null : m.LengthSeconds) : null,
            Core.FreeBusMeters, Device: Output.DeviceStatus());

        LoudnessStatus Status(LoudnessReading r) => new(Finite(r.MomentaryLufs), Finite(r.ShortTermLufs), Finite(r.IntegratedLufs), Finite(r.TruePeakDbtp), r.Frames / (double)Format.SampleRate);

        // JSON has no infinity: silence is reported as null.
        static double? Finite(double v) => double.IsFinite(v) ? v : null;
    }

    private void CheckBus(AudioBus bus)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bus.Index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(bus.Index, _busVolume.Length);
    }

    private static void CheckFilter(float cutoffHz, float q)
    {
        if (!(cutoffHz > 0f) || !float.IsFinite(cutoffHz))
        {
            throw new ArgumentOutOfRangeException(nameof(cutoffHz), cutoffHz, "The cutoff must be above 0 Hz.");
        }

        if (!float.IsFinite(q) || q < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(q), q, "Q must be 0 (flat) or above.");
        }
    }

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
        Interlocked.CompareExchange(ref s_active, null, this);
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

    private struct BusSettings
    {
        public bool Muted;
        public FilterStatus? LowPass;
        public FilterStatus? HighPass;
        public ReverbOptions? Reverb;
        public DuckingStatus? Ducking;
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
