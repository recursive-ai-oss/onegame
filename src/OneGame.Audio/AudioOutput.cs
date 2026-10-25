using System.Diagnostics;
using System.Threading;
using OneAudio.Loudness;

namespace OneGame.Audio;

/// <summary>Where an <see cref="AudioOutput"/> is in its life.</summary>
public enum AudioOutputStatus
{
    /// <summary>Created, no source attached yet.</summary>
    Created,

    /// <summary>Samples are going to a device (or a capture sink).</summary>
    Running,

    /// <summary>No device is open. The output keeps rendering at real-time pace and discards the samples, so the game behaves the same without sound, and retries opening in the background.</summary>
    NoDevice,

    /// <summary>Disposed.</summary>
    Stopped,
}

/// <summary>What the loudness meter reads at the output (EBU R 128 / ITU-R BS.1770). Silence reads -infinity, shown as <see cref="double.NegativeInfinity"/>.</summary>
/// <param name="MomentaryLufs">Loudness over the last 400 ms.</param>
/// <param name="ShortTermLufs">Loudness over the last 3 s.</param>
/// <param name="IntegratedLufs">Gated loudness since the last reset.</param>
/// <param name="TruePeakDbtp">The highest true peak since the last reset, in dBTP.</param>
/// <param name="Frames">Frames measured since the last reset.</param>
public readonly record struct LoudnessReading(double MomentaryLufs, double ShortTermLufs, double IntegratedLufs, double TruePeakDbtp, long Frames);

/// <summary>
/// The real-time side of audio: a thread pulls frames from an <see cref="IAudioRenderSource"/>, limits them and writes them to an
/// <see cref="IAudioSink"/>. After construction the thread allocates nothing, takes no lock shared with the game thread and lets no exception out.
/// A lost device drops to a silent, real-time-paced mode and is reopened at the same format when it comes back.
/// </summary>
public sealed class AudioOutput : IDisposable
{
    private const int MaxChunkFrames = 2048;
    private const int MinChunkFrames = 64;
    private const int WarmupTicks = 8;
    private static readonly long RetryTicks = Stopwatch.Frequency / 2;

    private readonly IAudioBackend _backend;
    private readonly AudioDeviceOptions _options;
    private readonly OutputLimiterMode _limiter;
    private readonly float[] _buffer;
    private readonly Lock _lifecycle = new();
    private Thread? _thread;
    private IAudioRenderSource? _source;
    private IAudioSink? _sink;
    private string? _deviceName;
    private string? _lastError;
    private int _status;
    private int _stop;
    private int _paused;
    private int _reopenRequested;
    private int _chunkFrames;
    private int _bufferFrames;
    private int _periodFrames;
    private long _queuedFrames;
    private long _underruns;
    private long _losses;
    private long _reopens;
    private long _faults;
    private long _framesRendered;
    private long _silentFrames;
    private long _allocBaseline;
    private long _allocExcluded;
    private long _allocNow;
    private long _nextRetry;
    private long _silentOrigin;
    private long _silentFramesPaced;
    private LoudnessMeter? _meter;
    private int _resetMeter;
    private double _momentary = double.NegativeInfinity;
    private double _shortTerm = double.NegativeInfinity;
    private double _integrated = double.NegativeInfinity;
    private double _truePeak = double.NegativeInfinity;
    private long _meteredFrames;

    private AudioOutput(IAudioBackend backend, AudioDeviceOptions options, OutputLimiterMode limiter)
    {
        _backend = backend;
        _options = options;
        _limiter = limiter;
        Format = new AudioDeviceFormat(options.SampleRate, options.Channels);
        _buffer = new float[MaxChunkFrames * options.Channels];
        _deviceName = options.DeviceName;
        _chunkFrames = Math.Clamp(options.SampleRate / 100, MinChunkFrames, MaxChunkFrames);
    }

    /// <summary>Whether the output measures loudness. Set before <see cref="Start"/>; off by default for an output built directly.</summary>
    public bool Metering { get; set; }

    /// <summary>The loudness at the output, after the limiter. Safe from any thread; updated once per block while sound is rendering (not while paused).</summary>
    public LoudnessReading Loudness => new(
        Volatile.Read(ref _momentary), Volatile.Read(ref _shortTerm), Volatile.Read(ref _integrated), Volatile.Read(ref _truePeak), Interlocked.Read(ref _meteredFrames));

    /// <summary>Clears the meter's history (integrated loudness, true peak) at the next block, so a new measurement starts clean.</summary>
    public void ResetLoudness()
    {
        Volatile.Write(ref _resetMeter, 1);
        if (_backend.IsManual)
        {
            // No audio thread competes for the meter, and a reader expects the reset to show at once.
            ApplyMeterReset();
            Volatile.Write(ref _momentary, double.NegativeInfinity);
            Volatile.Write(ref _shortTerm, double.NegativeInfinity);
            Volatile.Write(ref _integrated, double.NegativeInfinity);
            Volatile.Write(ref _truePeak, double.NegativeInfinity);
        }
    }

    private void ApplyMeterReset()
    {
        if (Interlocked.Exchange(ref _resetMeter, 0) != 0)
        {
            _meter?.Reset();
            Volatile.Write(ref _meteredFrames, 0);
        }
    }

    /// <summary>Opens the device and returns the output. When no device opens, the output still exists and is silent (see <see cref="Status"/>); it never throws for a missing device.</summary>
    /// <param name="backend">The audio system to use.</param>
    /// <param name="options">What to ask of the device.</param>
    /// <param name="limiter">How to keep samples in range.</param>
    /// <returns>The output, not yet started.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The rate is outside 8000..384000 or the channel count outside 1..16.</exception>
    public static AudioOutput Create(IAudioBackend backend, AudioDeviceOptions options, OutputLimiterMode limiter = OutputLimiterMode.SoftKnee)
    {
        ArgumentNullException.ThrowIfNull(backend);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentOutOfRangeException.ThrowIfLessThan(options.SampleRate, 8000);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(options.SampleRate, 384_000);
        ArgumentOutOfRangeException.ThrowIfLessThan(options.Channels, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(options.Channels, 16);

        var output = new AudioOutput(backend, options, limiter);
        if (!output.TryOpenSink(first: true))
        {
            output._nextRetry = Stopwatch.GetTimestamp() + RetryTicks;
        }

        return output;
    }

    /// <summary>The format the mixer must render at. Fixed once <see cref="Start"/> is called, including across device loss.</summary>
    public AudioDeviceFormat Format { get; private set; }

    /// <summary>The current state.</summary>
    public AudioOutputStatus Status => (AudioOutputStatus)Volatile.Read(ref _status);

    /// <summary>The device name in use or being retried; <see langword="null"/> for the system default.</summary>
    public string? DeviceName => Volatile.Read(ref _deviceName);

    /// <summary>The most recent reason a device failed to open, or <see langword="null"/>.</summary>
    public string? LastError => Volatile.Read(ref _lastError);

    /// <summary>Stops sound without stopping the device: it is fed silence and the source is not rendered, so the mixer's timeline holds still.</summary>
    public bool Paused
    {
        get => Volatile.Read(ref _paused) != 0;
        set => Volatile.Write(ref _paused, value ? 1 : 0);
    }

    /// <summary>The device's buffer in frames, or 0 with no device.</summary>
    public int BufferFrames => Volatile.Read(ref _bufferFrames);

    /// <summary>The device's write size in frames, or 0 with no device.</summary>
    public int PeriodFrames => Volatile.Read(ref _periodFrames);

    /// <summary>Output latency in milliseconds: audio queued in the device right after a write. 0 with no device.</summary>
    public double LatencyMilliseconds => Volatile.Read(ref _queuedFrames) * 1000.0 / Format.SampleRate;

    /// <summary>Times the device ran dry (the game could not keep up). Expected 0.</summary>
    public long Underruns => Interlocked.Read(ref _underruns);

    /// <summary>Times an open device was lost.</summary>
    public long DeviceLosses => Interlocked.Read(ref _losses);

    /// <summary>Times a device was opened again after a loss, a failed open or a device change.</summary>
    public long Reopens => Interlocked.Read(ref _reopens);

    /// <summary>Exceptions the render source threw. The block is replaced by silence. Expected 0.</summary>
    public long RenderFaults => Interlocked.Read(ref _faults);

    /// <summary>Frames produced so far, to a device or not.</summary>
    public long FramesRendered => Interlocked.Read(ref _framesRendered);

    /// <summary>Frames produced while no device was open.</summary>
    public long SilentFrames => Interlocked.Read(ref _silentFrames);

    /// <summary>Bytes the audio thread allocated after its first few blocks, not counting device opens. Expected 0.</summary>
    public long AudioThreadAllocatedBytes => Interlocked.Read(ref _allocNow);

    /// <summary>Attaches the source and, for a real device, starts the audio thread.</summary>
    /// <param name="source">The mixer. Its rate and channel count must equal <see cref="Format"/>.</param>
    /// <exception cref="ArgumentException">The source's format differs from <see cref="Format"/>.</exception>
    /// <exception cref="InvalidOperationException">Already started or disposed.</exception>
    public void Start(IAudioRenderSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source.SampleRate != Format.SampleRate || source.Channels != Format.Channels)
        {
            throw new ArgumentException($"The source renders {source.SampleRate} Hz x {source.Channels} but the output runs at {Format.SampleRate} Hz x {Format.Channels}. Create the mixer from AudioOutput.Format.", nameof(source));
        }

        lock (_lifecycle)
        {
            if (_source is not null || Status == AudioOutputStatus.Stopped)
            {
                throw new InvalidOperationException("The output is already started or was disposed.");
            }

            _source = source;
            if (Metering && Format.SampleRate >= LoudnessMeter.MinSampleRate)
            {
                // BS.1770 K-weighting is designed per sample rate; the meter and its memory are built here, never on the audio thread.
                _meter = new LoudnessMeter(Format.SampleRate, Format.Channels, default, true, false);
            }

            Volatile.Write(ref _status, (int)(_sink is not null ? AudioOutputStatus.Running : AudioOutputStatus.NoDevice));
            if (!_backend.IsManual)
            {
                _thread = new Thread(Run) { IsBackground = true, Name = "onegame-audio", Priority = ThreadPriority.Highest };
                _thread.Start();
            }
        }
    }

    /// <summary>Asks the audio thread to switch to another device at the same format. Takes effect within a block or two; if the new device fails to open the output goes silent and keeps retrying it.</summary>
    /// <param name="deviceName">A name from <see cref="IAudioBackend.ListDevices"/>, or <see langword="null"/> for the system default.</param>
    public void SelectDevice(string? deviceName)
    {
        Volatile.Write(ref _deviceName, deviceName);
        Volatile.Write(ref _reopenRequested, 1);
        if (_backend.IsManual && _source is not null)
        {
            // No thread to notice the request: apply it now.
            ApplyReopenRequest();
        }
    }

    /// <summary>Renders exactly <paramref name="frames"/> frames through the limiter into the sink on the calling thread. For manual backends such as the capture sink, where tests and the harness control time.</summary>
    /// <param name="frames">Frames to produce.</param>
    /// <exception cref="InvalidOperationException">The backend is not manual, or no source is attached.</exception>
    public void Advance(int frames)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(frames);
        if (!_backend.IsManual || _source is null)
        {
            throw new InvalidOperationException("Advance needs a manual backend and a started output.");
        }

        while (frames > 0)
        {
            var n = Math.Min(frames, MaxChunkFrames);
            if (_sink is null)
            {
                RetryOpen();
            }

            Tick(n);
            frames -= n;
        }
    }

    /// <summary>Stops the thread and closes the device.</summary>
    public void Dispose()
    {
        Thread? thread;
        lock (_lifecycle)
        {
            if (Status == AudioOutputStatus.Stopped)
            {
                return;
            }

            Volatile.Write(ref _stop, 1);
            thread = _thread;
        }

        // A blocked device write returns within one buffer; a hung driver must not hang the game's shutdown.
        if (thread is not null && !thread.Join(TimeSpan.FromSeconds(3)))
        {
            Volatile.Write(ref _status, (int)AudioOutputStatus.Stopped);
            return;
        }

        CloseSink();
        Volatile.Write(ref _status, (int)AudioOutputStatus.Stopped);
    }

    private bool TryOpenSink(bool first)
    {
        var request = _options with { DeviceName = DeviceName, SampleRate = Format.SampleRate, Channels = Format.Channels };
        IAudioSink? sink;
        string error;
        try
        {
            if (!_backend.TryOpen(request, out sink, out error) || sink is null)
            {
                Volatile.Write(ref _lastError, error);
                return false;
            }
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            Volatile.Write(ref _lastError, e.Message);
            return false;
        }

        if (first && sink.Format.SampleRate >= 8000 && sink.Format.Channels >= 1)
        {
            // The first open decides the mixer's format: whatever the device settled on.
            Format = sink.Format;
        }

        if (sink.Format != Format)
        {
            sink.Dispose();
            Volatile.Write(ref _lastError, $"device delivered {sink.Format.SampleRate} Hz x {sink.Format.Channels}, expected {Format.SampleRate} Hz x {Format.Channels}");
            return false;
        }

        _sink = sink;
        _chunkFrames = Math.Clamp(sink.PeriodFrames, MinChunkFrames, MaxChunkFrames);
        Volatile.Write(ref _periodFrames, sink.PeriodFrames);
        Volatile.Write(ref _bufferFrames, sink.BufferFrames);
        Volatile.Write(ref _lastError, null);
        if (_source is not null)
        {
            Volatile.Write(ref _status, (int)AudioOutputStatus.Running);
        }

        return true;
    }

    private void CloseSink()
    {
        var sink = _sink;
        _sink = null;
        Volatile.Write(ref _bufferFrames, 0);
        Volatile.Write(ref _periodFrames, 0);
        Volatile.Write(ref _queuedFrames, 0);
        try
        {
            sink?.Dispose();
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            // Closing a device that already vanished may fail; there is nothing left to release.
        }
    }

    private void ApplyReopenRequest()
    {
        if (Interlocked.Exchange(ref _reopenRequested, 0) != 0)
        {
            CloseSink();
            _nextRetry = 0;
            RetryOpen();
        }
    }

    private void RetryOpen()
    {
        if (Stopwatch.GetTimestamp() < _nextRetry)
        {
            return;
        }

        // Opening allocates (strings, handles); that is not steady-state audio work, so it is excluded from the audio-thread count.
        var before = GC.GetAllocatedBytesForCurrentThread();
        var opened = TryOpenSink(first: false);
        _allocExcluded += GC.GetAllocatedBytesForCurrentThread() - before;
        if (opened)
        {
            Interlocked.Increment(ref _reopens);
            _silentOrigin = 0;
        }
        else
        {
            _nextRetry = Stopwatch.GetTimestamp() + RetryTicks;
        }
    }

    private void Run()
    {
        try
        {
            var ticks = 0;
            while (Volatile.Read(ref _stop) == 0)
            {
                ApplyReopenRequest();
                if (_sink is null)
                {
                    RetryOpen();
                }

                Tick(_chunkFrames);

                var allocated = GC.GetAllocatedBytesForCurrentThread();
                if (ticks < WarmupTicks)
                {
                    ticks++;
                    _allocBaseline = allocated;
                    _allocExcluded = 0;
                }
                else
                {
                    Interlocked.Exchange(ref _allocNow, allocated - _allocBaseline - _allocExcluded);
                }
            }
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            // Nothing may escape the audio thread; the game carries on without sound.
            Volatile.Write(ref _lastError, "audio thread stopped: " + e.Message);
            Volatile.Write(ref _status, (int)AudioOutputStatus.NoDevice);
        }
    }

    // One block: render (or silence), limit, write. With no device it paces itself against the clock instead of the device.
    private void Tick(int frames)
    {
        var span = _buffer.AsSpan(0, frames * Format.Channels);
        var source = _source;
        if (source is null || Paused)
        {
            span.Clear();
        }
        else
        {
            try
            {
                source.Render(span);
            }
            catch (Exception e) when (e is not OutOfMemoryException)
            {
                Interlocked.Increment(ref _faults);
                span.Clear();
            }
        }

        OutputLimiter.Apply(span, _limiter);
        if (source is not null && !Paused)
        {
            Interlocked.Add(ref _framesRendered, frames);
            Measure(span);
        }

        var sink = _sink;
        if (sink is null)
        {
            Volatile.Write(ref _status, (int)AudioOutputStatus.NoDevice);
            Interlocked.Add(ref _silentFrames, frames);
            PaceSilently(frames);
            return;
        }

        var result = sink.Write(span);
        Volatile.Write(ref _queuedFrames, sink.QueuedFrames);
        if (result == SinkWriteResult.Underrun)
        {
            Interlocked.Increment(ref _underruns);
        }
        else if (result == SinkWriteResult.Lost)
        {
            Interlocked.Increment(ref _losses);
            CloseSink();
            _nextRetry = Stopwatch.GetTimestamp() + RetryTicks;
            _silentOrigin = 0;
        }
    }

    private void Measure(Span<float> block)
    {
        var meter = _meter;
        if (meter is null)
        {
            return;
        }

        ApplyMeterReset();
        meter.Process(block);
        Interlocked.Add(ref _meteredFrames, block.Length / Format.Channels);
        Volatile.Write(ref _momentary, meter.MomentaryLufs);
        Volatile.Write(ref _shortTerm, meter.ShortTermLufs);
        Volatile.Write(ref _integrated, meter.IntegratedLufs);
        Volatile.Write(ref _truePeak, meter.TruePeakDbtp);
    }

    private void PaceSilently(int frames)
    {
        if (_backend.IsManual)
        {
            return;
        }

        if (_silentOrigin == 0)
        {
            _silentOrigin = Stopwatch.GetTimestamp();
            _silentFramesPaced = 0;
        }

        _silentFramesPaced += frames;
        var due = _silentOrigin + (_silentFramesPaced * Stopwatch.Frequency / Format.SampleRate);
        var waitMs = (due - Stopwatch.GetTimestamp()) * 1000 / Stopwatch.Frequency;
        if (waitMs > 0)
        {
            Thread.Sleep((int)waitMs);
        }
    }
}
