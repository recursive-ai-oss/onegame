using OneAudio;

namespace OneGame.Audio;

/// <summary>A source file could not be decoded: it is malformed, uses an unsupported format, or asks for more than the limits allow.</summary>
public sealed class AudioLoadException : Exception
{
    /// <summary>Creates the exception.</summary>
    /// <param name="message">What was wrong.</param>
    /// <param name="inner">The decoder's own error.</param>
    public AudioLoadException(string message, Exception? inner = null)
        : base(message, inner)
    {
    }
}

/// <summary>
/// The engine's mixer: oneaudio's <see cref="AudioMixer"/> with WAV, Ogg Vorbis, Opus, FLAC and MP3 decoders behind the <see cref="IAudioCore"/> calls.
/// Any source rate or channel count is converted by the mixer. Files are decoded on the calling (game) thread, never on the audio thread.
/// </summary>
public sealed class OneAudioCore : IAudioCore
{
    private readonly AudioMixer _mixer;
    private readonly AudioCodecRegistry _codecs;
    private readonly StreamingPump _pump = new();
    private readonly Dictionary<long, StreamingSource> _streamsByVoice = [];
    private readonly object _streamLock = new();
    private readonly double _maxClipSeconds;

    /// <summary>Builds the mixer. Everything it needs is allocated here, so create it at startup.</summary>
    /// <param name="sampleRate">Output frames per second.</param>
    /// <param name="channels">Output channels.</param>
    /// <param name="maxVoices">The most simultaneous voices.</param>
    /// <param name="buses">The number of buses.</param>
    /// <param name="maxClipSeconds">The longest clip <see cref="LoadClip"/> decodes into memory; longer files must be streamed.</param>
    public OneAudioCore(int sampleRate, int channels, int maxVoices, int buses, double maxClipSeconds = 120)
        : this(new AudioCoreSettings(sampleRate, channels, maxVoices, buses), maxClipSeconds)
    {
    }

    /// <summary>Builds the mixer from full settings, including reverbs and the limiter lookahead.</summary>
    /// <param name="settings">The format, sizes and effect capacity.</param>
    /// <param name="maxClipSeconds">The longest clip <see cref="LoadClip"/> decodes into memory.</param>
    public OneAudioCore(AudioCoreSettings settings, double maxClipSeconds = 120)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _maxClipSeconds = maxClipSeconds;
        _codecs = new AudioCodecRegistry().AddWav().AddOgg().AddVorbis().AddOpus().AddFlac().AddMp3();
        try
        {
            _mixer = new AudioMixer(new MixerOptions
            {
                SampleRate = settings.SampleRate,
                Channels = settings.Channels,
                MaxVoices = settings.MaxVoices,
                MaxBuses = settings.Buses,
                MaxReverbs = settings.MaxReverbs,
                LimiterLookaheadMilliseconds = settings.LimiterLookaheadMilliseconds,
            });
        }
        catch (ArgumentException e)
        {
            throw new ArgumentOutOfRangeException(nameof(settings), e.Message);
        }
    }

    /// <summary>Builds <see cref="OneAudioCore"/>s; the default for <see cref="AudioServiceOptions.CoreFactory"/>.</summary>
    /// <param name="maxClipSeconds">The longest clip decoded into memory.</param>
    public sealed class Factory(double maxClipSeconds = 120) : IAudioCoreFactory
    {
        /// <inheritdoc/>
        public IAudioCore Create(int sampleRate, int channels, int maxVoices, int buses) => new OneAudioCore(sampleRate, channels, maxVoices, buses, maxClipSeconds);

        /// <inheritdoc/>
        public IAudioCore Create(AudioCoreSettings settings) => new OneAudioCore(settings, maxClipSeconds);
    }

    /// <inheritdoc/>
    public int SampleRate => _mixer.SampleRate;

    /// <inheritdoc/>
    public int Channels => _mixer.Channels;

    /// <inheritdoc/>
    public int ActiveVoices => _mixer.ActiveVoices;

    /// <inheritdoc/>
    public long DroppedCommands => _mixer.DroppedCommands;

    /// <summary>The underlying mixer, for counters and calls the service does not wrap.</summary>
    public AudioMixer Mixer => _mixer;

    /// <inheritdoc/>
    public void Render(Span<float> interleaved) => _mixer.Render(interleaved);

    /// <inheritdoc/>
    public object LoadClip(Stream data)
    {
        try
        {
            using var decoder = AudioDecoder.Open(data, _codecs, leaveOpen: true);
            var maxFrames = (long)(_maxClipSeconds * decoder.Format.SampleRate);
            return AudioClip.FromDecoder(decoder, maxFrames);
        }
        catch (Exception e) when (e is OneAudioException or EndOfStreamException or IOException or ArgumentException or InvalidOperationException)
        {
            throw new AudioLoadException("The audio file could not be decoded: " + e.Message, e);
        }
    }

    /// <inheritdoc/>
    public object OpenStream(Stream data, bool looping)
    {
        StreamingSource source;
        try
        {
            source = new StreamingSource(AudioDecoder.Open(data, _codecs), loop: looping);
            // Priming decodes the first block here, so a bad file fails now and the voice starts without an underrun.
            _pump.Add(source);
        }
        catch (Exception e) when (e is OneAudioException or EndOfStreamException or IOException or ArgumentException or InvalidOperationException)
        {
            data.Dispose();
            throw new AudioLoadException("The audio file could not be opened for streaming: " + e.Message, e);
        }

        return source;
    }

    /// <inheritdoc/>
    public double DurationSeconds(object source) => source switch
    {
        AudioClip c => c.Frames / (double)c.Format.SampleRate,
        StreamingSource { LengthFrames: >= 0 } s => s.LengthFrames / (double)s.Format.SampleRate,
        _ => -1,
    };

    /// <inheritdoc/>
    public VoiceId Play(in VoiceRequest request)
    {
        var source = (AudioSource)request.Source!;
        VoiceHandle handle;
        try
        {
            handle = _mixer.Play(ToParams(request));
        }
        catch
        {
            Release(source);
            throw;
        }

        if (!handle.IsValid)
        {
            Release(source);
            return default;
        }

        Track(handle, source);
        return new VoiceId(handle.Id);
    }

    /// <inheritdoc/>
    public bool StopVoice(VoiceId voice, int fadeOutFrames) => _mixer.Stop(new VoiceHandle(voice.Id), fadeOutFrames);

    /// <inheritdoc/>
    public bool StopAll(int fadeOutFrames) => _mixer.StopAll(fadeOutFrames);

    /// <inheritdoc/>
    public bool SetGain(VoiceId voice, float gain, int rampFrames) => _mixer.SetGain(new VoiceHandle(voice.Id), gain, rampFrames);

    /// <inheritdoc/>
    public bool SetPan(VoiceId voice, float pan) => _mixer.SetPan(new VoiceHandle(voice.Id), pan);

    /// <inheritdoc/>
    public bool SetLoop(VoiceId voice, bool looping) => _mixer.SetLoop(new VoiceHandle(voice.Id), looping);

    /// <inheritdoc/>
    public bool SetBusGain(int bus, float gain) => _mixer.SetBusGain(bus, gain);

    /// <inheritdoc/>
    public bool SetPitch(VoiceId voice, float pitch, int rampFrames) => _mixer.SetPitch(new VoiceHandle(voice.Id), pitch, rampFrames);

    /// <inheritdoc/>
    public bool SetBusMute(int bus, bool muted) => _mixer.SetBusMute(bus, muted);

    /// <inheritdoc/>
    public bool SetDucking(int targetBus, int sourceBus, DuckingOptions settings) => _mixer.SetDucking(targetBus, sourceBus, !settings.Enabled ? DuckingSettings.None : new DuckingSettings
    {
        ThresholdDb = settings.ThresholdDb,
        ReductionDb = settings.ReductionDb,
        AttackMs = settings.AttackMs,
        ReleaseMs = settings.ReleaseMs,
    });

    /// <inheritdoc/>
    public bool ClearDucking(int targetBus) => _mixer.ClearDucking(targetBus);

    /// <inheritdoc/>
    public bool SetBusFilter(int bus, AudioFilterKind kind, float cutoffHz, float q, int glideFrames) =>
        _mixer.SetBusFilter(bus, ToFilter(kind), cutoffHz, q > 0 ? q : AudioMixer.DefaultFilterQ, glideFrames);

    /// <inheritdoc/>
    public bool ClearBusFilter(int bus, AudioFilterKind kind, int glideFrames) => _mixer.ClearBusFilter(bus, ToFilter(kind), glideFrames);

    /// <inheritdoc/>
    public bool SetVoiceFilter(VoiceId voice, AudioFilterKind kind, float cutoffHz, float q, int glideFrames) =>
        _mixer.SetVoiceFilter(new VoiceHandle(voice.Id), ToFilter(kind), cutoffHz, q > 0 ? q : AudioMixer.DefaultFilterQ, glideFrames);

    /// <inheritdoc/>
    public bool ClearVoiceFilter(VoiceId voice, AudioFilterKind kind, int glideFrames) => _mixer.ClearVoiceFilter(new VoiceHandle(voice.Id), ToFilter(kind), glideFrames);

    /// <inheritdoc/>
    public bool SetBusReverb(int bus, ReverbOptions options, int glideFrames) => _mixer.SetBusReverb(bus, new ReverbSettings
    {
        RoomSize = options.RoomSize,
        DecaySeconds = options.DecaySeconds,
        Damping = options.Damping,
        PreDelayMilliseconds = options.PreDelayMilliseconds,
        Wet = options.Wet,
        Dry = options.Dry,
    }, glideFrames);

    /// <inheritdoc/>
    public bool ClearBusReverb(int bus) => _mixer.ClearBusReverb(bus);

    /// <inheritdoc/>
    public bool SetLimiter(LimiterOptions? options) => _mixer.SetLimiter(options is { } o ? new LimiterSettings { CeilingDb = o.CeilingDb, ReleaseMilliseconds = o.ReleaseMilliseconds } : null);

    /// <inheritdoc/>
    public bool HasLimiter => _mixer.HasLimiter;

    /// <inheritdoc/>
    public double LimiterGainReductionDb => _mixer.LimiterGainReductionDb;

    /// <inheritdoc/>
    public bool TryGetStreamPosition(VoiceId voice, out double positionSeconds, out double lengthSeconds)
    {
        StreamingSource? s;
        lock (_streamLock)
        {
            _streamsByVoice.TryGetValue(voice.Id, out s);
        }

        if (s is null)
        {
            positionSeconds = 0;
            lengthSeconds = -1;
            return false;
        }

        var rate = (double)s.Format.SampleRate;
        positionSeconds = s.PositionFrames / rate;
        lengthSeconds = s.LengthFrames < 0 ? -1 : s.LengthFrames / rate;
        return true;
    }

    /// <inheritdoc/>
    public VoiceId Crossfade(VoiceId from, in VoiceRequest incoming, int frames)
    {
        var source = (AudioSource)incoming.Source!;
        VoiceHandle handle;
        try
        {
            handle = _mixer.Crossfade(new VoiceHandle(from.Id), ToParams(incoming), Math.Max(1, frames));
        }
        catch
        {
            Release(source);
            throw;
        }

        if (!handle.IsValid)
        {
            Release(source);
            return default;
        }

        Track(handle, source);
        return new VoiceId(handle.Id);
    }

    /// <inheritdoc/>
    public bool TryGetEvent(out AudioEvent audioEvent)
    {
        if (!_mixer.TryGetEvent(out var e))
        {
            audioEvent = default;
            return false;
        }

        if (e.Kind is MixerEventKind.VoiceEnded or MixerEventKind.VoiceStolen)
        {
            ReleaseVoice(e.VoiceId);
        }

        var kind = e.Kind switch
        {
            MixerEventKind.VoiceEnded => AudioEventKind.VoiceEnded,
            MixerEventKind.VoiceStolen => AudioEventKind.VoiceStolen,
            MixerEventKind.PlayDropped => AudioEventKind.PlayDropped,
            _ => AudioEventKind.Underrun,
        };
        audioEvent = new AudioEvent(kind, new VoiceId(e.VoiceId), e.Value);
        return true;
    }

    /// <inheritdoc/>
    public void Pump()
    {
        _pump.PumpOnce();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        StreamingSource[] open;
        lock (_streamLock)
        {
            open = [.. _streamsByVoice.Values];
            _streamsByVoice.Clear();
        }

        foreach (var s in open)
        {
            _pump.Remove(s);
            s.Dispose();
        }

        _pump.Dispose();
    }

    private void Track(VoiceHandle handle, AudioSource source)
    {
        if (source is StreamingSource s)
        {
            lock (_streamLock)
            {
                _streamsByVoice[handle.Id] = s;
            }
        }
    }

    private void ReleaseVoice(long voice)
    {
        StreamingSource? s;
        lock (_streamLock)
        {
            if (!_streamsByVoice.Remove(voice, out s))
            {
                return;
            }
        }

        _pump.Remove(s);
        s.Dispose();
    }

    private void Release(AudioSource source)
    {
        if (source is StreamingSource s)
        {
            _pump.Remove(s);
            s.Dispose();
        }
    }

    private static FilterType ToFilter(AudioFilterKind kind) => kind == AudioFilterKind.HighPass ? FilterType.HighPass : FilterType.LowPass;

    private static PlayParams ToParams(in VoiceRequest r) => new((AudioSource)r.Source!)
    {
        Gain = r.Gain,
        Pan = r.Pan,
        Loop = r.Loop,
        Bus = r.Bus,
        Priority = r.Priority,
        StartAtFrame = r.StartAtFrame,
        FadeInFrames = r.FadeInFrames,
        Pitch = r.Pitch,
        VariablePitch = r.VariablePitch,
    };
}
