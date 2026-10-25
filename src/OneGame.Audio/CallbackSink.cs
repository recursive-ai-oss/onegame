using System.Diagnostics;
using System.Threading;

namespace OneGame.Audio;

/// <summary>
/// The sink for operating systems that pull audio through a render callback (Audio Unit and the like): the audio thread's
/// <see cref="Write"/> fills a ring and blocks while it is full; the callback drains it. The device's start and stop go through the
/// three abstract members.
/// </summary>
internal abstract class CallbackSink : IAudioSink, IAudioSinkDiagnostics, IAudioSinkControl
{
    private readonly CallbackRing _ring;
    private readonly int _bufferFrames;
    private readonly Lock _gate = new();
    private long _seenUnderruns;
    private int _active;
    private int _wanted = 1;
    private int _systemSuspended;
    private int _closed;
    private int _lost;

    protected CallbackSink(AudioDeviceFormat format, int periodFrames, int bufferFrames)
    {
        Format = format;
        PeriodFrames = periodFrames;
        _bufferFrames = bufferFrames;
        // The ring holds the device buffer plus a little slack, so the audio thread can run one period ahead of the callback.
        _ring = new CallbackRing(bufferFrames + (periodFrames * 2), format.Channels);
    }

    public AudioDeviceFormat Format { get; }

    public int PeriodFrames { get; }

    public int BufferFrames => _bufferFrames;

    public int QueuedFrames => _ring.QueuedFrames;

    /// <summary>The callback's source. Call it from the device callback only.</summary>
    protected CallbackRing Ring => _ring;

    /// <summary>Whether the device is running and drawing from the ring.</summary>
    protected bool IsActive => Volatile.Read(ref _active) != 0;

    /// <summary>Whether the system has taken the device away (an interruption).</summary>
    public bool IsSystemSuspended => Volatile.Read(ref _systemSuspended) != 0;

    public Action<bool>? SystemSuspendChanged { get; set; }

    /// <summary>Starts the device's callbacks; returns whether it started.</summary>
    protected abstract bool StartDevice();

    /// <summary>Stops the device's callbacks.</summary>
    protected abstract void StopDevice();

    /// <summary>Releases the device.</summary>
    protected abstract void CloseDevice();

    /// <summary>The device state word for the report.</summary>
    protected abstract string DeviceState { get; }

    /// <summary>Platform extras for the report.</summary>
    protected virtual string? DeviceDetail => null;

    /// <summary>Starts the stream the first time; call once after construction.</summary>
    public bool Begin()
    {
        Apply();
        return IsActive;
    }

    /// <summary>Marks the device unusable (the system reset its audio services): the next write reports <see cref="SinkWriteResult.Lost"/> and the output opens a new one.</summary>
    protected void MarkLost() => Volatile.Write(ref _lost, 1);

    /// <summary>Called by the platform when the system stopped (<see langword="true"/>) or handed back the device on its own.</summary>
    protected void NotifySystemSuspended(bool suspended)
    {
        Volatile.Write(ref _systemSuspended, suspended ? 1 : 0);
        Apply();
        SystemSuspendChanged?.Invoke(suspended);
    }

    /// <summary>The app asks for the device to run or rest (the lifecycle). A system interruption overrides a request to run.</summary>
    public void SetActive(bool active)
    {
        Volatile.Write(ref _wanted, active ? 1 : 0);
        Apply();
    }

    // The device runs when the app wants it and the system hasn't taken it.
    private void Apply()
    {
        lock (_gate)
        {
            var should = Volatile.Read(ref _wanted) != 0 && Volatile.Read(ref _systemSuspended) == 0 && Volatile.Read(ref _closed) == 0;
            if (should == IsActive)
            {
                return;
            }

            if (should)
            {
                _ring.Flush();
                if (StartDevice())
                {
                    Volatile.Write(ref _active, 1);
                }
            }
            else
            {
                Volatile.Write(ref _active, 0);
                StopDevice();
            }
        }
    }

    public SinkWriteResult Write(ReadOnlySpan<float> interleaved)
    {
        var channels = Format.Channels;
        var done = 0;
        var started = Stopwatch.GetTimestamp();
        while (done < interleaved.Length)
        {
            if (Volatile.Read(ref _closed) != 0 || Volatile.Read(ref _lost) != 0)
            {
                return SinkWriteResult.Lost;
            }

            done += _ring.Write(interleaved.Slice(done)) * channels;
            if (done >= interleaved.Length)
            {
                break;
            }

            if (!IsActive || Stopwatch.GetElapsedTime(started) > TimeSpan.FromSeconds(1))
            {
                // The device isn't drawing (stopped by the system, or stalled): drop the rest so the mixer keeps its pace.
                _ring.Flush();
                break;
            }

            Thread.Sleep(1);
        }

        var seen = _ring.UnderrunEvents;
        if (seen != _seenUnderruns)
        {
            _seenUnderruns = seen;
            return SinkWriteResult.Underrun;
        }

        return SinkWriteResult.Ok;
    }

    public AudioSinkReport Report() =>
        new(DeviceState, _ring.FramesWritten, _ring.FramesRead, _ring.Peak, _ring.UnderrunEvents, DeviceDetail);

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _closed, 1) != 0)
        {
            return;
        }

        lock (_gate)
        {
            Volatile.Write(ref _active, 0);
        }

        try
        {
            StopDevice();
        }
        finally
        {
            CloseDevice();
        }
    }
}
