using System.Collections.Concurrent;
using System.Buffers.Binary;

namespace OneGame.Audio.Tests;

/// <summary>
/// A small stand-in mixer for tests: sources are mono float arrays, voices have gain, balance pan, bus gain, looping, frame-exact start and linear fades.
/// It follows the same contract as a real core (commands queued from the game thread, applied on the render thread).
/// </summary>
internal sealed class TestCore : IAudioCore
{
    private enum Op { Play, Stop, StopAll, Gain, Pan, Loop, Bus }

    private struct Cmd
    {
        public Op Op;
        public long Id;
        public float F;
        public int I;
        public bool B;
        public VoiceRequest Request;
    }

    private sealed class Voice
    {
        public long Id;
        public float[] Data = [];
        public float Gain, Pan, GainTarget, GainStep;
        public int RampLeft;
        public bool Loop, Stopping;
        public int Bus;
        public long StartAt;
        public long Position;
        public int FadeIn, FadeInDone;
    }

    private readonly ConcurrentQueue<Cmd> _commands = new();
    private readonly ConcurrentQueue<AudioEvent> _events = new();
    private readonly Voice[] _voices;
    private readonly float[] _busGain;
    private long _next;
    private long _timeline;
    private int _active;

    public TestCore(int sampleRate, int channels, int maxVoices, int buses)
    {
        SampleRate = sampleRate;
        Channels = channels;
        _voices = new Voice[maxVoices];
        for (var i = 0; i < maxVoices; i++)
        {
            _voices[i] = new Voice();
        }

        _busGain = new float[buses];
        Array.Fill(_busGain, 1f);
    }

    public static IAudioCoreFactory Factory { get; } = new F();

    /// <summary>What <see cref="FreeBusMeters"/> reports. This core never starts a meter, so a service sees "no slot" at 0 and "queue full" above it.</summary>
    public int MeterSlotsFree { get; set; }

    public int FreeBusMeters => MeterSlotsFree;

    public bool EnableBusMeter(int bus) => false;

    public int SampleRate { get; }

    public int Channels { get; }

    public long Timeline => Volatile.Read(ref _timeline);

    public int ActiveVoices => Volatile.Read(ref _active);

    public long DroppedCommands => 0;

    public static byte[] ClipBytes(params float[] samples)
    {
        var bytes = new byte[samples.Length * 4];
        for (var i = 0; i < samples.Length; i++)
        {
            BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(i * 4), samples[i]);
        }

        return bytes;
    }

    public static byte[] Constant(float value, int frames) => ClipBytes(Enumerable.Repeat(value, frames).ToArray());

    public static byte[] Sine(int sampleRate, double hz, float amplitude, int frames) =>
        ClipBytes(Enumerable.Range(0, frames).Select(i => (float)(amplitude * Math.Sin(2 * Math.PI * hz * i / sampleRate))).ToArray());

    public object LoadClip(Stream data)
    {
        using var ms = new MemoryStream();
        data.CopyTo(ms);
        var bytes = ms.ToArray();
        var f = new float[bytes.Length / 4];
        for (var i = 0; i < f.Length; i++)
        {
            f[i] = BinaryPrimitives.ReadSingleLittleEndian(bytes.AsSpan(i * 4));
        }

        return f;
    }

    public object OpenStream(Stream data, bool loop) => LoadClip(data);

    public double DurationSeconds(object source) => ((float[])source).Length / (double)SampleRate;

    public VoiceId Play(in VoiceRequest request)
    {
        var id = Interlocked.Increment(ref _next);
        _commands.Enqueue(new Cmd { Op = Op.Play, Id = id, Request = request });
        return new VoiceId(id);
    }

    public bool StopVoice(VoiceId voice, int fadeOutFrames) => Q(new Cmd { Op = Op.Stop, Id = voice.Id, I = fadeOutFrames });

    public bool StopAll(int fadeOutFrames) => Q(new Cmd { Op = Op.StopAll, I = fadeOutFrames });

    public bool SetGain(VoiceId voice, float gain, int rampFrames) => Q(new Cmd { Op = Op.Gain, Id = voice.Id, F = gain, I = rampFrames });

    public bool SetPan(VoiceId voice, float pan) => Q(new Cmd { Op = Op.Pan, Id = voice.Id, F = pan });

    public bool SetLoop(VoiceId voice, bool loop) => Q(new Cmd { Op = Op.Loop, Id = voice.Id, B = loop });

    public bool SetBusGain(int bus, float gain) => Q(new Cmd { Op = Op.Bus, I = bus, F = gain });

    public VoiceId Crossfade(VoiceId from, in VoiceRequest incoming, int frames)
    {
        StopVoice(from, frames);
        return Play(incoming);
    }

    public bool TryGetEvent(out AudioEvent audioEvent) => _events.TryDequeue(out audioEvent);

    public void Pump()
    {
    }

    public void Dispose()
    {
    }

    private bool Q(Cmd c)
    {
        _commands.Enqueue(c);
        return true;
    }

    public void Render(Span<float> interleaved)
    {
        while (_commands.TryDequeue(out var c))
        {
            Apply(c);
        }

        interleaved.Clear();
        var frames = interleaved.Length / Channels;
        var active = 0;
        foreach (var v in _voices)
        {
            if (v.Id == 0)
            {
                continue;
            }

            for (var i = 0; i < frames && v.Id != 0; i++)
            {
                if (_timeline + i < v.StartAt)
                {
                    continue;
                }

                if (v.Position >= v.Data.Length)
                {
                    if (v.Loop && v.Data.Length > 0)
                    {
                        v.Position = 0;
                    }
                    else
                    {
                        _events.Enqueue(new AudioEvent(AudioEventKind.VoiceEnded, new VoiceId(v.Id), 0));
                        v.Id = 0;
                        break;
                    }
                }

                var g = v.Gain;
                if (v.RampLeft > 0)
                {
                    v.Gain += v.GainStep;
                    v.RampLeft--;
                    if (v.RampLeft == 0)
                    {
                        v.Gain = v.GainTarget;
                        if (v.Stopping)
                        {
                            _events.Enqueue(new AudioEvent(AudioEventKind.VoiceEnded, new VoiceId(v.Id), 0));
                            v.Id = 0;
                        }
                    }
                }

                if (v.FadeInDone < v.FadeIn)
                {
                    g *= (float)v.FadeInDone / v.FadeIn;
                    v.FadeInDone++;
                }

                var s = v.Data[v.Position++] * g * _busGain[v.Bus];
                var l = v.Pan > 0 ? 1f - v.Pan : 1f;
                var r = v.Pan < 0 ? 1f + v.Pan : 1f;
                if (Channels == 1)
                {
                    interleaved[i] += s;
                }
                else
                {
                    interleaved[i * Channels] += s * l;
                    interleaved[(i * Channels) + 1] += s * r;
                }
            }

            if (v.Id != 0)
            {
                active++;
            }
        }

        Volatile.Write(ref _active, active);
        Interlocked.Add(ref _timeline, frames);
    }

    private void Apply(Cmd c)
    {
        switch (c.Op)
        {
            case Op.Play:
                foreach (var v in _voices)
                {
                    if (v.Id != 0)
                    {
                        continue;
                    }

                    v.Id = c.Id;
                    v.Data = (float[])c.Request.Source!;
                    v.Gain = v.GainTarget = c.Request.Gain;
                    v.Pan = c.Request.Pan;
                    v.Loop = c.Request.Loop;
                    v.Bus = c.Request.Bus;
                    v.StartAt = c.Request.StartAtFrame;
                    v.Position = 0;
                    v.RampLeft = 0;
                    v.Stopping = false;
                    v.FadeIn = c.Request.FadeInFrames;
                    v.FadeInDone = 0;
                    return;
                }

                _events.Enqueue(new AudioEvent(AudioEventKind.PlayDropped, new VoiceId(c.Id), 0));
                return;
            case Op.Bus:
                _busGain[c.I] = c.F;
                return;
            case Op.StopAll:
                foreach (var v in _voices)
                {
                    Stop(v, c.I);
                }

                return;
        }

        foreach (var v in _voices)
        {
            if (v.Id != c.Id)
            {
                continue;
            }

            switch (c.Op)
            {
                case Op.Stop:
                    Stop(v, c.I);
                    break;
                case Op.Gain:
                    Ramp(v, c.F, c.I, stopping: false);
                    break;
                case Op.Pan:
                    v.Pan = c.F;
                    break;
                case Op.Loop:
                    v.Loop = c.B;
                    break;
            }
        }
    }

    private static void Stop(Voice v, int fade)
    {
        if (v.Id == 0)
        {
            return;
        }

        if (fade <= 0)
        {
            v.Id = 0;
        }
        else
        {
            Ramp(v, 0f, fade, stopping: true);
        }
    }

    private static void Ramp(Voice v, float target, int frames, bool stopping)
    {
        v.Stopping = stopping;
        if (frames <= 0)
        {
            v.Gain = v.GainTarget = target;
            v.RampLeft = 0;
            return;
        }

        v.GainTarget = target;
        v.RampLeft = frames;
        v.GainStep = (target - v.Gain) / frames;
    }

    private sealed class F : IAudioCoreFactory
    {
        public IAudioCore Create(int sampleRate, int channels, int maxVoices, int buses) => new TestCore(sampleRate, channels, maxVoices, buses);
    }
}

/// <summary>A backend that behaves like a device: Write blocks for the duration of the audio, and the device can vanish and return.</summary>
internal sealed class FakeBackend : IAudioBackend
{
    private readonly List<FakeSink> _sinks = [];
    private int _available = 1;

    public FakeBackend(int deviceRate = 48_000)
    {
        DeviceRate = deviceRate;
    }

    public int DeviceRate { get; set; }

    public string Name => "fake";

    public bool IsManual => false;

    public bool Available
    {
        get => Volatile.Read(ref _available) == 1;
        set => Volatile.Write(ref _available, value ? 1 : 0);
    }

    public int Opens { get; private set; }

    public string? LastRequestedDevice { get; private set; }

    public long TotalFrames
    {
        get
        {
            lock (_sinks)
            {
                return _sinks.Sum(s => Interlocked.Read(ref s.Frames));
            }
        }
    }

    public FakeSink? Current
    {
        get
        {
            lock (_sinks)
            {
                return _sinks.Count == 0 ? null : _sinks[^1];
            }
        }
    }

    public IReadOnlyList<AudioDeviceInfo> ListDevices() => [new AudioDeviceInfo("fake0", "Fake", true)];

    public bool TryOpen(AudioDeviceOptions options, out IAudioSink? sink, out string reason)
    {
        LastRequestedDevice = options.DeviceName;
        if (!Available)
        {
            sink = null;
            reason = "unplugged";
            return false;
        }

        Opens++;
        var s = new FakeSink(new AudioDeviceFormat(DeviceRate, options.Channels));
        lock (_sinks)
        {
            _sinks.Add(s);
        }

        sink = s;
        reason = string.Empty;
        return true;
    }

    public void Unplug()
    {
        Available = false;
        Current?.Kill();
    }
}

internal sealed class FakeSink : IAudioSink
{
    private int _dead;
    public long Frames;
    public float Peak;

    public FakeSink(AudioDeviceFormat format) => Format = format;

    public AudioDeviceFormat Format { get; }

    public int PeriodFrames => 480;

    public int BufferFrames => 1920;

    public int QueuedFrames => 960;

    public void Kill() => Volatile.Write(ref _dead, 1);

    public SinkWriteResult Write(ReadOnlySpan<float> interleaved)
    {
        if (Volatile.Read(ref _dead) == 1)
        {
            return SinkWriteResult.Lost;
        }

        var frames = interleaved.Length / Format.Channels;
        foreach (var s in interleaved)
        {
            if (Math.Abs(s) > Peak)
            {
                Peak = Math.Abs(s);
            }
        }

        Interlocked.Add(ref Frames, frames);
        Thread.Sleep(Math.Max(1, frames * 1000 / Format.SampleRate));
        return SinkWriteResult.Ok;
    }

    public void Dispose()
    {
    }
}
