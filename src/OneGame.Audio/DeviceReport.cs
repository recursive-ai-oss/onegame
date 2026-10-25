using System.Threading;

namespace OneGame.Audio;

/// <summary>What a sink can say about the device behind it, measured on the device's side of the hand-over.</summary>
/// <param name="State">The device's own state, in a few lowercase words (for example <c>started</c>, <c>paused</c>, <c>awaiting_gesture</c>).</param>
/// <param name="FramesWritten">Frames handed to the device since it was opened.</param>
/// <param name="FramesPlayed">Frames the device has taken from us (its read position). Advancing means the speaker path is running.</param>
/// <param name="PeakPlayed">The highest absolute sample value among frames the device has taken; 0 means everything it played was silence.</param>
/// <param name="DeviceUnderruns">Underruns the device itself counted (for devices that count them).</param>
/// <param name="Detail">Platform extras such as the audio route, session category or autoplay state; <see langword="null"/> if none.</param>
public readonly record struct AudioSinkReport(string State, long FramesWritten, long FramesPlayed, float PeakPlayed, long DeviceUnderruns, string? Detail);

/// <summary>Implemented by sinks that can describe their device. The output reports it through the harness.</summary>
public interface IAudioSinkDiagnostics
{
    /// <summary>Reads the current report. Safe from any thread.</summary>
    AudioSinkReport Report();
}

/// <summary>Implemented by sinks whose device can be stopped and started again, as a phone does when the app leaves the foreground.</summary>
public interface IAudioSinkControl
{
    /// <summary>Starts (<see langword="true"/>) or stops (<see langword="false"/>) the device. Stopping must make <see cref="IAudioSink.Write"/> return promptly. Never throws.</summary>
    void SetActive(bool active);

    /// <summary>Set by the output: called with <see langword="true"/> when the system stopped the device on its own (a call, an alarm) and <see langword="false"/> when it handed it back.</summary>
    Action<bool>? SystemSuspendChanged { get; set; }
}

/// <summary>Implemented by sinks of pumped backends, where the host calls <see cref="AudioOutput.Pump"/> on its own schedule instead of a thread blocking in <see cref="IAudioSink.Write"/>.</summary>
public interface IPumpedSink
{
    /// <summary>How many frames to render now to keep the device fed, or 0.</summary>
    int WantedFrames { get; }
}

/// <summary>Implemented by backends of pumped devices: the output hands over the function the page's timer must call.</summary>
public interface IPumpedBackend
{
    /// <summary>Registers <paramref name="pump"/> (the output's <see cref="AudioOutput.Pump"/>) to be called every few milliseconds on the page's thread.</summary>
    void SetPump(Action pump);
}

/// <summary>The device section of the <c>audio</c> report.</summary>
/// <param name="Backend">The operating-system API, such as <c>aaudio</c>, <c>audiounit</c>, <c>webaudio</c>.</param>
/// <param name="State">The output's state (<c>Running</c>, <c>NoDevice</c>, ...).</param>
/// <param name="DeviceState">The device's own state from the sink, or null for sinks that don't say.</param>
/// <param name="Device">The device name asked for; null for the system default.</param>
/// <param name="Error">The last reason a device failed to open, or null.</param>
/// <param name="Suspended">Whether the app lifecycle has the device stopped.</param>
/// <param name="Paused">Whether the output is feeding silence (focus loss or pause).</param>
/// <param name="BufferFrames">The device buffer in frames.</param>
/// <param name="PeriodFrames">The write size in frames.</param>
/// <param name="LatencyMs">Audio queued in the device after the last write, in milliseconds.</param>
/// <param name="Underruns">Times the device ran dry, as seen by the output.</param>
/// <param name="DeviceUnderruns">Underruns the device counted itself.</param>
/// <param name="Losses">Times the device was lost.</param>
/// <param name="Reopens">Times a device was opened again.</param>
/// <param name="Suspensions">Times the lifecycle stopped the device.</param>
/// <param name="FramesRendered">Frames the mixer produced.</param>
/// <param name="FramesWritten">Frames handed to the device.</param>
/// <param name="FramesPlayed">Frames the device took.</param>
/// <param name="PeakPlayed">Highest absolute sample among frames the device took.</param>
/// <param name="AudioThreadAllocatedBytes">Bytes the audio thread allocated since warm-up. Expected 0.</param>
/// <param name="Detail">Platform extras (route, session, autoplay), or null.</param>
/// <param name="Request">What a device request in the same <c>audio</c> call came to (unlock, interrupt, suspend), or null.</param>
public sealed record AudioDeviceStatus(
    string Backend, string State, string? DeviceState, string? Device, string? Error, bool Suspended, bool Paused, int BufferFrames, int PeriodFrames, double LatencyMs,
    long Underruns, long DeviceUnderruns, long Losses, long Reopens, long Suspensions, long FramesRendered, long FramesWritten, long FramesPlayed, float PeakPlayed,
    long AudioThreadAllocatedBytes, string? Detail, string? Request = null);

/// <summary>
/// A single-producer, single-consumer ring of float samples between the audio thread (which calls <see cref="IAudioSink.Write"/>) and an
/// operating-system render callback that must never block. The callback side allocates nothing, takes no lock and counts the frames it had to
/// fill with silence as underruns.
/// </summary>
internal sealed class CallbackRing
{
    private readonly float[] _data;
    private readonly int _channels;
    private readonly int _capacity;
    private long _written;
    private long _read;
    private long _underrunFrames;
    private long _underrunEvents;
    private int _peakBits;
    private int _lastWasStarved;
    private int _primed;

    public CallbackRing(int capacityFrames, int channels)
    {
        _capacity = capacityFrames;
        _channels = channels;
        _data = new float[capacityFrames * channels];
    }

    public int CapacityFrames => _capacity;

    public long FramesWritten => Interlocked.Read(ref _written);

    public long FramesRead => Interlocked.Read(ref _read);

    public int QueuedFrames => (int)(Interlocked.Read(ref _written) - Interlocked.Read(ref _read));

    public int FreeFrames => _capacity - QueuedFrames;

    /// <summary>Frames the callback had to fill with silence because the ring was empty.</summary>
    public long UnderrunFrames => Interlocked.Read(ref _underrunFrames);

    /// <summary>Separate dry spells (a run of starved callbacks counts once).</summary>
    public long UnderrunEvents => Interlocked.Read(ref _underrunEvents);

    /// <summary>The highest absolute value among frames the callback consumed.</summary>
    public float Peak => BitConverter.Int32BitsToSingle(Volatile.Read(ref _peakBits));

    /// <summary>Producer: appends as many whole frames as fit and returns how many were taken.</summary>
    public int Write(ReadOnlySpan<float> interleaved)
    {
        var frames = Math.Min(interleaved.Length / _channels, FreeFrames);
        var pos = (int)(Interlocked.Read(ref _written) % _capacity);
        var first = Math.Min(frames, _capacity - pos);
        interleaved.Slice(0, first * _channels).CopyTo(_data.AsSpan(pos * _channels));
        if (frames > first)
        {
            interleaved.Slice(first * _channels, (frames - first) * _channels).CopyTo(_data);
        }

        // The release makes the samples visible before the consumer sees the new count.
        Interlocked.Add(ref _written, frames);
        Volatile.Write(ref _primed, 1);
        return frames;
    }

    /// <summary>Consumer (device callback): fills <paramref name="output"/> and pads with silence when short. Returns the frames that were real.</summary>
    public int Read(Span<float> output)
    {
        var want = output.Length / _channels;
        var have = (int)(Interlocked.Read(ref _written) - Interlocked.Read(ref _read));
        var frames = Math.Min(want, have);
        var pos = (int)(Interlocked.Read(ref _read) % _capacity);
        var first = Math.Min(frames, _capacity - pos);
        _data.AsSpan(pos * _channels, first * _channels).CopyTo(output);
        if (frames > first)
        {
            _data.AsSpan(0, (frames - first) * _channels).CopyTo(output.Slice(first * _channels));
        }

        var peak = BitConverter.Int32BitsToSingle(_peakBits);
        var real = output.Slice(0, frames * _channels);
        foreach (var s in real)
        {
            var a = MathF.Abs(s);
            if (a > peak)
            {
                peak = a;
            }
        }

        Volatile.Write(ref _peakBits, BitConverter.SingleToInt32Bits(peak));
        if (frames < want)
        {
            output.Slice(frames * _channels).Clear();
            if (Volatile.Read(ref _primed) == 0)
            {
                // The device started pulling before the first write (or after a restart): that is start-up, not a game falling behind.
                return frames;
            }

            Interlocked.Add(ref _underrunFrames, want - frames);
            if (Interlocked.Exchange(ref _lastWasStarved, 1) == 0)
            {
                Interlocked.Increment(ref _underrunEvents);
            }
        }
        else
        {
            Volatile.Write(ref _lastWasStarved, 0);
        }

        Interlocked.Add(ref _read, frames);
        return frames;
    }

    /// <summary>Drops what is queued (producer and consumer must be quiet), so a restart doesn't play stale audio.</summary>
    public void Flush()
    {
        Interlocked.Exchange(ref _read, Interlocked.Read(ref _written));
        Volatile.Write(ref _primed, 0);
        Volatile.Write(ref _lastWasStarved, 0);
    }
}
