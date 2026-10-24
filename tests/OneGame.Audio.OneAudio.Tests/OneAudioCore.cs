using OneAudio;

namespace OneGame.Audio.OneAudio.Tests;

/// <summary>Adapts oneaudio's <see cref="AudioMixer"/> to the service's core seam. This is the class that moves into the runtime project when the oneaudio package replaces the project reference.</summary>
internal sealed class OneAudioCore : IAudioCore
{
    private readonly AudioMixer _mixer;
    private readonly AudioCodecRegistry _codecs;
    private readonly List<StreamingSource> _streams = [];
    private readonly object _streamLock = new();

    public OneAudioCore(int sampleRate, int channels, int maxVoices, int buses, AudioCodecRegistry codecs)
    {
        _codecs = codecs;
        _mixer = new AudioMixer(new MixerOptions { SampleRate = sampleRate, Channels = channels, MaxVoices = maxVoices, MaxBuses = buses });
    }

    public sealed class Factory(AudioCodecRegistry codecs) : IAudioCoreFactory
    {
        public IAudioCore Create(int sampleRate, int channels, int maxVoices, int buses) => new OneAudioCore(sampleRate, channels, maxVoices, buses, codecs);
    }

    public int SampleRate => _mixer.SampleRate;

    public int Channels => _mixer.Channels;

    public int ActiveVoices => _mixer.ActiveVoices;

    public long DroppedCommands => _mixer.DroppedCommands;

    public AudioMixer Mixer => _mixer;

    public void Render(Span<float> interleaved) => _mixer.Render(interleaved);

    public object LoadClip(Stream data)
    {
        using var decoder = AudioDecoder.Open(data, _codecs, leaveOpen: true);
        return RequireRate(AudioClip.FromDecoder(decoder));
    }

    public object OpenStream(Stream data, bool looping)
    {
        var source = new StreamingSource(AudioDecoder.Open(data, _codecs), loop: looping);
        RequireRate(source);
        lock (_streamLock)
        {
            _streams.Add(source);
        }

        return source;
    }

    public double DurationSeconds(object source) => source is AudioClip c ? c.Frames / (double)c.Format.SampleRate : -1;

    public VoiceId Play(in VoiceRequest request) => new(_mixer.Play(ToParams(request)).Id);

    public bool StopVoice(VoiceId voice, int fadeOutFrames) => _mixer.Stop(new VoiceHandle(voice.Id), fadeOutFrames);

    public bool StopAll(int fadeOutFrames) => _mixer.StopAll(fadeOutFrames);

    public bool SetGain(VoiceId voice, float gain, int rampFrames) => _mixer.SetGain(new VoiceHandle(voice.Id), gain, rampFrames);

    public bool SetPan(VoiceId voice, float pan) => _mixer.SetPan(new VoiceHandle(voice.Id), pan);

    public bool SetLoop(VoiceId voice, bool looping) => _mixer.SetLoop(new VoiceHandle(voice.Id), looping);

    public bool SetBusGain(int bus, float gain) => _mixer.SetBusGain(bus, gain);

    public VoiceId Crossfade(VoiceId from, in VoiceRequest incoming, int frames) =>
        new(_mixer.Crossfade(new VoiceHandle(from.Id), ToParams(incoming), frames).Id);

    public bool TryGetEvent(out AudioEvent audioEvent)
    {
        if (!_mixer.TryGetEvent(out var e))
        {
            audioEvent = default;
            return false;
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

    public void Pump()
    {
        StreamingSource[] snapshot;
        lock (_streamLock)
        {
            snapshot = [.. _streams];
        }

        foreach (var s in snapshot)
        {
            s.Pump();
        }
    }

    public void Dispose()
    {
        lock (_streamLock)
        {
            foreach (var s in _streams)
            {
                s.Dispose();
            }

            _streams.Clear();
        }
    }

    private AudioSource RequireRate(AudioSource source)
    {
        if (source.Format.SampleRate != _mixer.SampleRate)
        {
            throw new NotSupportedException($"The clip runs at {source.Format.SampleRate} Hz but the mixer at {_mixer.SampleRate} Hz; resampling on load is not wired in yet.");
        }

        return source;
    }

    private static PlayParams ToParams(in VoiceRequest r) => new((AudioSource)r.Source!)
    {
        Gain = r.Gain,
        Pan = r.Pan,
        Loop = r.Loop,
        Bus = r.Bus,
        Priority = r.Priority,
        StartAtFrame = r.StartAtFrame,
        FadeInFrames = r.FadeInFrames,
    };
}
