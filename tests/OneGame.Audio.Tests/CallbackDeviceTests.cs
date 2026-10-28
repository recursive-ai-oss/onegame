using System.Diagnostics;

namespace OneGame.Audio.Tests;

/// <summary>
/// The device-side behaviour the phone and web backends share: a callback-driven device behind a ring, stopping and starting with the app
/// lifecycle, system interruptions, underrun reporting, and a render path that allocates nothing. The "device" here is a thread pulling from
/// the ring the way an operating-system render callback does.
/// </summary>
public class CallbackDeviceTests
{
    private static bool WaitFor(Func<bool> condition, int milliseconds = 6000)
    {
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < milliseconds)
        {
            if (condition())
            {
                return true;
            }

            Thread.Sleep(5);
        }

        return condition();
    }

    private sealed class FakeCallbackSink : CallbackSink
    {
        private readonly Thread _device;
        private volatile bool _running;
        private volatile bool _stop;
        private volatile int _burstFrames;
        private float[] _scratch;

        public FakeCallbackSink(AudioDeviceFormat format)
            : base(format, 480, 1920)
        {
            _scratch = new float[48_000 * format.Channels];
            _device = new Thread(Pull) { IsBackground = true };
            _device.Start();
        }

        public int Starts;
        public int Stops;
        public long Callbacks;
        public long CallbackAllocated;

        protected override string DeviceState => _running ? "started" : "stopped";

        protected override bool StartDevice()
        {
            Starts++;
            _running = true;
            return true;
        }

        protected override void StopDevice()
        {
            Stops++;
            _running = false;
        }

        protected override void CloseDevice() => _stop = true;

        public void System(bool suspended) => NotifySystemSuspended(suspended);

        // Next pull takes this many frames at once, which empties the ring.
        public void Starve(int frames) => _burstFrames = frames;

        // Like an OS render callback: every 10 ms ask for 480 frames, never block, never allocate.
        private void Pull()
        {
            var start = Stopwatch.GetTimestamp();
            var next = 0L;
            long baseline = 0;
            while (!_stop)
            {
                var due = start + (next * Stopwatch.Frequency / 100);
                if (Stopwatch.GetTimestamp() < due)
                {
                    Thread.Sleep(1);
                    continue;
                }

                next++;
                if (!_running)
                {
                    continue;
                }

                var burst = _burstFrames;
                _burstFrames = 0;
                var frames = burst > 0 ? burst : 480;
                Ring.Read(_scratch.AsSpan(0, frames * Format.Channels));
                Callbacks++;
                if (Callbacks == 20)
                {
                    baseline = GC.GetAllocatedBytesForCurrentThread();
                }
                else if (Callbacks > 20)
                {
                    CallbackAllocated = GC.GetAllocatedBytesForCurrentThread() - baseline;
                }
            }
        }
    }

    private sealed class FakeCallbackBackend : IAudioBackend
    {
        public FakeCallbackSink? Sink;

        public string Name => "fake-callback";

        public bool IsManual => false;

        public IReadOnlyList<AudioDeviceInfo> ListDevices() => [];

        public bool TryOpen(AudioDeviceOptions options, out IAudioSink? sink, out string reason)
        {
            var created = new FakeCallbackSink(new AudioDeviceFormat(options.SampleRate, options.Channels));
            created.Begin();
            Sink = created;
            sink = created;
            reason = string.Empty;
            return true;
        }
    }

    private static AudioOutput StartOutput(FakeCallbackBackend backend, out TestCore core)
    {
        var output = AudioOutput.Create(backend, new AudioDeviceOptions { SampleRate = 48_000, Channels = 2 }, OutputLimiterMode.Clamp);
        core = new TestCore(output.Format.SampleRate, 2, 8, 4);
        output.Start(core);
        return output;
    }

    [Fact]
    public void TheDeviceTakesTheFramesAndTheReportShowsThePeak()
    {
        var backend = new FakeCallbackBackend();
        using var output = StartOutput(backend, out var core);
        core.Play(new VoiceRequest { Source = core.LoadClip(new MemoryStream(TestCore.Constant(0.5f, 4800))) });

        Assert.True(WaitFor(() => output.DeviceStatus().FramesPlayed > 9600));
        var s = output.DeviceStatus();
        Assert.Equal("fake-callback", s.Backend);
        Assert.Equal("started", s.DeviceState);
        Assert.Equal("Running", s.State);
        Assert.InRange(s.PeakPlayed, 0.2f, 0.51f);
        Assert.Equal(0, s.Underruns);
        Assert.True(s.FramesWritten >= s.FramesPlayed);
        Assert.Equal(0, s.AudioThreadAllocatedBytes);
    }

    [Fact]
    public void SilenceReachingTheDeviceReadsAsZeroPeak()
    {
        var backend = new FakeCallbackBackend();
        using var output = StartOutput(backend, out _);
        Assert.True(WaitFor(() => output.DeviceStatus().FramesPlayed > 4800));
        Assert.Equal(0f, output.DeviceStatus().PeakPlayed);
    }

    [Fact]
    public void SuspendingStopsTheDeviceAndHoldsTheMixerClock()
    {
        var backend = new FakeCallbackBackend();
        using var output = StartOutput(backend, out var core);
        Assert.True(WaitFor(() => core.Timeline > 4800));

        output.Suspended = true;
        Assert.True(WaitFor(() => backend.Sink!.Stops == 1));
        Assert.Equal("stopped", output.DeviceStatus().DeviceState);
        Thread.Sleep(100);
        var held = core.Timeline;
        var played = output.DeviceStatus().FramesPlayed;
        Thread.Sleep(300);
        Assert.Equal(held, core.Timeline);
        Assert.Equal(played, output.DeviceStatus().FramesPlayed);
        Assert.True(output.DeviceStatus().Suspended);

        output.Suspended = false;
        Assert.True(WaitFor(() => backend.Sink!.Starts == 2));
        Assert.True(WaitFor(() => core.Timeline > held + 4800));
        Assert.True(WaitFor(() => output.DeviceStatus().FramesPlayed > played + 4800));
        Assert.Equal(1, output.DeviceStatus().Suspensions);
        Assert.Equal(0, output.DeviceLosses);
    }

    [Fact]
    public void ASystemInterruptionStopsAndResumesWithoutLosingTheDevice()
    {
        var backend = new FakeCallbackBackend();
        using var output = StartOutput(backend, out var core);
        Assert.True(WaitFor(() => core.Timeline > 4800));

        backend.Sink!.System(true);
        Assert.True(WaitFor(() => output.IsDeviceStopped));
        Thread.Sleep(100);
        var held = core.Timeline;
        Thread.Sleep(300);
        Assert.Equal(held, core.Timeline);
        Assert.Equal("stopped", output.DeviceStatus().DeviceState);

        backend.Sink.System(false);
        Assert.True(WaitFor(() => core.Timeline > held + 4800));
        Assert.Equal("started", output.DeviceStatus().DeviceState);
        Assert.Equal(0, output.DeviceLosses);
        Assert.Equal(0, output.Reopens);
    }

    [Fact]
    public void AnInterruptionEndingWhileInTheBackgroundDoesNotRestartTheDevice()
    {
        var backend = new FakeCallbackBackend();
        using var output = StartOutput(backend, out _);
        Assert.True(WaitFor(() => output.DeviceStatus().FramesPlayed > 2400));
        backend.Sink!.System(true);
        output.Suspended = true;
        Assert.True(WaitFor(() => output.Suspensions == 1));
        Thread.Sleep(100);
        Assert.Equal("stopped", output.DeviceStatus().DeviceState);
        var starts = backend.Sink.Starts;
        backend.Sink.System(false);
        Thread.Sleep(150);
        Assert.Equal(starts, backend.Sink.Starts);
        Assert.Equal("stopped", output.DeviceStatus().DeviceState);
    }

    [Fact]
    public void ADeviceThatRunsDryIsCountedAsAnUnderrun()
    {
        var backend = new FakeCallbackBackend();
        using var output = StartOutput(backend, out _);
        Assert.True(WaitFor(() => output.DeviceStatus().FramesPlayed > 9600));
        Assert.Equal(0, output.Underruns);

        backend.Sink!.Starve(30_000);
        Assert.True(WaitFor(() => output.Underruns >= 1));
        var s = output.DeviceStatus();
        Assert.True(s.DeviceUnderruns >= 1);
        Assert.Equal(AudioOutputStatus.Running, output.Status);
    }

    [Fact]
    public void TheRenderCallbackAndTheAudioThreadAllocateNothingInSteadyState()
    {
        var backend = new FakeCallbackBackend();
        using var output = StartOutput(backend, out var core);
        core.Play(new VoiceRequest { Source = core.LoadClip(new MemoryStream(TestCore.Sine(48_000, 440, 0.3f, 96_000))), Loop = true });
        Assert.True(WaitFor(() => backend.Sink!.Callbacks > 200));
        var before = backend.Sink!.CallbackAllocated;
        Assert.True(WaitFor(() => backend.Sink.Callbacks > 400));
        Assert.Equal(0, backend.Sink.CallbackAllocated);
        Assert.Equal(0, before);
        Assert.Equal(0, output.AudioThreadAllocatedBytes);
    }

    [Fact]
    public void TheRingKeepsOrderAcrossTheWrapAndAllocatesNothing()
    {
        var ring = new CallbackRing(1000, 2);
        var input = new float[700 * 2];
        var output = new float[700 * 2];
        var next = 0f;
        var wrote = 0;
        var read = 0;
        var wrong = 0;

        void Rounds(int count)
        {
            for (var round = 0; round < count; round++)
            {
                for (var i = 0; i < input.Length; i++)
                {
                    input[i] = next + i;
                }

                wrote += ring.Write(input);
                read += ring.Read(output);
                for (var i = 0; i < output.Length; i += 97)
                {
                    if (output[i] != next + i)
                    {
                        wrong++;
                    }
                }

                next += 1000;
            }
        }

        // Warm up with the same loop, wrap included, so code paths and runtime types are first used outside the measured window.
        Rounds(200);
        wrote = read = 0;

        // A one-off cost the runtime charges to this thread (a lazily created type, a tiering step) can land in the first measured window; a
        // per-callback allocation lands in every window. So the ring passes when its best window allocates nothing, which a real
        // allocation cannot do: each window is 5000 write/read pairs.
        var best = long.MaxValue;
        for (var attempt = 0; attempt < 3 && best != 0; attempt++)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            Rounds(5000);
            best = Math.Min(best, GC.GetAllocatedBytesForCurrentThread() - before);
        }

        // The assertions come after the measurement: they allocate.
        Assert.Equal(0, best);
        Assert.Equal(0, wrong);
        Assert.True(wrote >= 5000 * 700 && wrote % (5000 * 700) == 0, $"wrote {wrote}");
        Assert.Equal(wrote, read);
        Assert.Equal(0, ring.UnderrunFrames);
        Assert.Equal(0, ring.QueuedFrames);
    }

    [Fact]
    public void ReadingAnEmptyRingGivesSilenceAndOneUnderrunPerDrySpell()
    {
        var ring = new CallbackRing(1000, 1);
        var span = new float[100];
        Array.Fill(span, 9f);
        Assert.Equal(0, ring.Read(span));
        Assert.All(span, v => Assert.Equal(0f, v));

        // Before anything was written the device is only starting up: not an underrun.
        Assert.Equal(0, ring.UnderrunFrames);

        ring.Write(new float[100]);
        ring.Read(span);
        ring.Read(span);
        ring.Read(span);
        Assert.Equal(200, ring.UnderrunFrames);
        Assert.Equal(1, ring.UnderrunEvents);
        ring.Write(new float[100]);
        ring.Read(span);
        ring.Read(span);
        Assert.Equal(2, ring.UnderrunEvents);

        // A restart flushes and starts over.
        ring.Flush();
        ring.Read(span);
        Assert.Equal(2, ring.UnderrunEvents);
    }

    private sealed class PumpedBackend : IAudioBackend, IPumpedBackend, IDisposable
    {
        public Action? Pump;
        public PumpedSink? Sink;

        public string Name => "fake-pumped";

        public bool IsManual => true;

        public void SetPump(Action pump) => Pump = pump;

        public void Dispose() => Sink?.Dispose();

        public IReadOnlyList<AudioDeviceInfo> ListDevices() => [];

        public bool TryOpen(AudioDeviceOptions options, out IAudioSink? sink, out string reason)
        {
            Sink = new PumpedSink(new AudioDeviceFormat(options.SampleRate, options.Channels));
            sink = Sink;
            reason = string.Empty;
            return true;
        }
    }

    private sealed class PumpedSink(AudioDeviceFormat format) : IAudioSink, IPumpedSink
    {
        public long Queued;
        public long Total;
        public int Target = 4800;

        public AudioDeviceFormat Format { get; } = format;

        public int PeriodFrames => 480;

        public int BufferFrames => Target;

        public int QueuedFrames => (int)Queued;

        public int WantedFrames => Math.Max(0, Target - (int)Queued) / 480 * 480;

        public SinkWriteResult Write(ReadOnlySpan<float> interleaved)
        {
            var frames = interleaved.Length / Format.Channels;
            Queued += frames;
            Total += frames;
            return SinkWriteResult.Ok;
        }

        public void Dispose()
        {
        }
    }

    [Fact]
    public void APumpedOutputHasNoThreadAndRendersWhatTheDeviceWants()
    {
        using var backend = new PumpedBackend();
        using var output = AudioOutput.Create(backend, new AudioDeviceOptions { SampleRate = 48_000, Channels = 2 }, OutputLimiterMode.Clamp);
        var core = new TestCore(48_000, 2, 4, 4);
        var fed = 0;
        output.BeforePump = () => fed++;
        output.Start(core);
        Assert.True(output.IsPumped);
        Assert.NotNull(backend.Pump);

        backend.Pump!();
        Assert.Equal(1, fed);
        Assert.Equal(4800, backend.Sink!.Total);
        Assert.Equal(4800, core.Timeline);

        // A full device wants nothing more; once it has played some, the next pump tops it up.
        backend.Pump();
        Assert.Equal(4800, backend.Sink.Total);
        backend.Sink.Queued -= 1000;
        backend.Pump();
        Assert.Equal(5760, backend.Sink.Total);

        // Suspended: nothing renders, the clock holds.
        output.Suspended = true;
        backend.Sink.Queued = 0;
        backend.Pump();
        Assert.Equal(5760, backend.Sink.Total);
        output.Suspended = false;
        backend.Pump();
        Assert.Equal(5760 + 4800, backend.Sink.Total);
    }
}
