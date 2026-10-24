namespace OneGame.Audio;

/// <summary>What the game asks of an output device. The device may deliver a different buffer size; rate and channel count are always honoured (the backend converts when the hardware can't).</summary>
public sealed record AudioDeviceOptions
{
    /// <summary>Backend-specific device name from <see cref="IAudioBackend.ListDevices"/>; <see langword="null"/> selects the system default.</summary>
    public string? DeviceName { get; init; }

    /// <summary>Preferred sample rate in Hz. Default 48000.</summary>
    public int SampleRate { get; init; } = 48_000;

    /// <summary>Output channel count. Default 2.</summary>
    public int Channels { get; init; } = 2;

    /// <summary>Requested total device buffer in milliseconds. Smaller is lower latency and more underrun risk. Default 40.</summary>
    public int LatencyMilliseconds { get; init; } = 40;
}

/// <summary>The format an open device runs at.</summary>
/// <param name="SampleRate">Frames per second.</param>
/// <param name="Channels">Samples per frame.</param>
public readonly record struct AudioDeviceFormat(int SampleRate, int Channels);

/// <summary>One output device a backend can open.</summary>
/// <param name="Name">The name to put in <see cref="AudioDeviceOptions.DeviceName"/>.</param>
/// <param name="Description">A human-readable label.</param>
/// <param name="IsDefault">Whether this is the system default.</param>
public readonly record struct AudioDeviceInfo(string Name, string Description, bool IsDefault);

/// <summary>The outcome of one <see cref="IAudioSink.Write"/>.</summary>
public enum SinkWriteResult
{
    /// <summary>All frames were accepted.</summary>
    Ok,

    /// <summary>The device had run dry before this write and recovered; all frames were accepted.</summary>
    Underrun,

    /// <summary>The device is gone. The sink must be discarded.</summary>
    Lost,
}

/// <summary>Where the output stage's final float samples go. Implemented per operating system, by the capture sink, and by fakes in tests.</summary>
public interface IAudioSink : IDisposable
{
    /// <summary>The negotiated format.</summary>
    AudioDeviceFormat Format { get; }

    /// <summary>The device's natural write size in frames.</summary>
    int PeriodFrames { get; }

    /// <summary>The device's total buffer in frames.</summary>
    int BufferFrames { get; }

    /// <summary>The newest measure of audio queued ahead of the speaker, in frames.</summary>
    int QueuedFrames { get; }

    /// <summary>Hands interleaved samples (a whole number of frames, already limited to -1..1) to the device and returns when they are queued. Blocks while the device buffer is full. Must not allocate and must not throw.</summary>
    SinkWriteResult Write(ReadOnlySpan<float> interleaved);
}

/// <summary>An operating-system audio API, or a stand-in for one.</summary>
public interface IAudioBackend
{
    /// <summary>A short name for diagnostics, such as <c>alsa</c>.</summary>
    string Name { get; }

    /// <summary>When true, nothing paces <see cref="IAudioSink.Write"/> and <see cref="AudioOutput"/> runs without a thread; the caller advances it with <see cref="AudioOutput.Advance"/>.</summary>
    bool IsManual { get; }

    /// <summary>Lists output devices. Returns an empty list when the audio system is unavailable.</summary>
    IReadOnlyList<AudioDeviceInfo> ListDevices();

    /// <summary>Opens a device. Never throws: failure returns false with a reason.</summary>
    bool TryOpen(AudioDeviceOptions options, out IAudioSink? sink, out string reason);
}

/// <summary>Anything that produces interleaved float frames on demand; the shape of a mixer's render call.</summary>
public interface IAudioRenderSource
{
    /// <summary>Frames per second the source renders at.</summary>
    int SampleRate { get; }

    /// <summary>Samples per frame.</summary>
    int Channels { get; }

    /// <summary>Fills <paramref name="interleaved"/> (a whole number of frames) on the audio thread. Overwrites the span; may exceed -1..1.</summary>
    void Render(Span<float> interleaved);
}

/// <summary>How the output stage keeps samples in range before they reach the device.</summary>
public enum OutputLimiterMode
{
    /// <summary>Hard clamp to -1..1.</summary>
    Clamp,

    /// <summary>Samples below 0.9 pass untouched; above that a smooth knee approaches 1 without reaching it.</summary>
    SoftKnee,
}

/// <summary>Keeps float samples inside the range a device accepts.</summary>
public static class OutputLimiter
{
    private const float Knee = 0.9f;

    /// <summary>Limits one sample. Not-a-number becomes 0.</summary>
    /// <param name="x">The sample.</param>
    /// <param name="mode">The limiter.</param>
    public static float Apply(float x, OutputLimiterMode mode)
    {
        if (float.IsNaN(x))
        {
            return 0f;
        }

        if (mode == OutputLimiterMode.Clamp)
        {
            return x > 1f ? 1f : x < -1f ? -1f : x;
        }

        var a = MathF.Abs(x);
        if (a <= Knee)
        {
            return x;
        }

        // tanh maps the overshoot onto the last 0.1 of headroom.
        var limited = Knee + ((1f - Knee) * MathF.Tanh((a - Knee) / (1f - Knee)));
        return x < 0f ? -limited : limited;
    }

    /// <summary>Limits a buffer in place.</summary>
    /// <param name="samples">The samples.</param>
    /// <param name="mode">The limiter.</param>
    public static void Apply(Span<float> samples, OutputLimiterMode mode)
    {
        for (var i = 0; i < samples.Length; i++)
        {
            samples[i] = Apply(samples[i], mode);
        }
    }
}
