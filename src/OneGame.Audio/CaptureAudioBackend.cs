using System.Buffers.Binary;

namespace OneGame.Audio;

/// <summary>
/// A backend with no device: it records the samples the output stage would have sent to the speakers. It is manual, so time moves only when
/// <see cref="AudioOutput.Advance"/> is called, which makes captures exact and repeatable. Tests and the harness use it.
/// </summary>
public sealed class CaptureAudioBackend : IAudioBackend, IDisposable
{
    private CaptureSink? _sink;

    /// <summary>The most recent sink opened through this backend, or <see langword="null"/> before the first open.</summary>
    public CaptureSink? Sink => _sink;

    /// <inheritdoc/>
    public string Name => "capture";

    /// <summary>Releases the sink object; the samples stay readable.</summary>
    public void Dispose() => _sink?.Dispose();


    /// <inheritdoc/>
    public bool IsManual => true;

    /// <inheritdoc/>
    public IReadOnlyList<AudioDeviceInfo> ListDevices() => [new AudioDeviceInfo("capture", "Capture sink", true)];

    /// <inheritdoc/>
    public bool TryOpen(AudioDeviceOptions options, out IAudioSink? sink, out string reason)
    {
        // A reopen continues the same recording instead of starting a new one.
        var format = new AudioDeviceFormat(options.SampleRate, options.Channels);
        if (_sink is null || _sink.Format != format)
        {
            _sink = new CaptureSink(format);
        }

        sink = _sink;
        reason = string.Empty;
        return true;
    }
}

/// <summary>The recording kept by <see cref="CaptureAudioBackend"/>: interleaved float samples after the limiter.</summary>
public sealed class CaptureSink : IAudioSink
{
    /// <summary>The most frames a capture keeps (ten minutes at 48 kHz); further frames are counted in <see cref="DroppedFrames"/> but not stored.</summary>
    public const long MaxFrames = 48_000L * 600;

    private float[] _samples = new float[4096];
    private int _count;

    internal CaptureSink(AudioDeviceFormat format) => Format = format;

    /// <inheritdoc/>
    public AudioDeviceFormat Format { get; }

    /// <inheritdoc/>
    public int PeriodFrames => 480;

    /// <inheritdoc/>
    public int BufferFrames => 1920;

    /// <inheritdoc/>
    public int QueuedFrames => 0;

    /// <summary>The captured interleaved samples.</summary>
    public ReadOnlySpan<float> Samples => _samples.AsSpan(0, _count);

    /// <summary>Frames captured so far.</summary>
    public int FramesCaptured => _count / Format.Channels;

    /// <summary>Frames discarded because the capture reached <see cref="MaxFrames"/>.</summary>
    public long DroppedFrames { get; private set; }

    /// <summary>The largest absolute sample captured.</summary>
    public float Peak { get; private set; }

    /// <summary>Discards everything captured so far.</summary>
    public void Clear()
    {
        _count = 0;
        Peak = 0f;
        DroppedFrames = 0;
    }

    /// <inheritdoc/>
    public SinkWriteResult Write(ReadOnlySpan<float> interleaved)
    {
        if (((long)_count + interleaved.Length) / Format.Channels > MaxFrames)
        {
            DroppedFrames += interleaved.Length / Format.Channels;
            return SinkWriteResult.Ok;
        }

        if (_count + interleaved.Length > _samples.Length)
        {
            Array.Resize(ref _samples, Math.Max(_samples.Length * 2, _count + interleaved.Length));
        }

        interleaved.CopyTo(_samples.AsSpan(_count));
        _count += interleaved.Length;
        foreach (var s in interleaved)
        {
            var a = MathF.Abs(s);
            if (a > Peak)
            {
                Peak = a;
            }
        }

        return SinkWriteResult.Ok;
    }

    /// <summary>Writes the capture as a 32-bit float WAV file, so it can be opened or diffed with ordinary audio tools.</summary>
    /// <param name="destination">The stream to write to.</param>
    public void WriteWav(Stream destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        var dataBytes = _count * 4;
        Span<byte> h = stackalloc byte[44];
        "RIFF"u8.CopyTo(h);
        BinaryPrimitives.WriteInt32LittleEndian(h[4..], 36 + dataBytes);
        "WAVEfmt "u8.CopyTo(h[8..]);
        BinaryPrimitives.WriteInt32LittleEndian(h[16..], 16);
        BinaryPrimitives.WriteInt16LittleEndian(h[20..], 3); // IEEE float
        BinaryPrimitives.WriteInt16LittleEndian(h[22..], (short)Format.Channels);
        BinaryPrimitives.WriteInt32LittleEndian(h[24..], Format.SampleRate);
        BinaryPrimitives.WriteInt32LittleEndian(h[28..], Format.SampleRate * Format.Channels * 4);
        BinaryPrimitives.WriteInt16LittleEndian(h[32..], (short)(Format.Channels * 4));
        BinaryPrimitives.WriteInt16LittleEndian(h[34..], 32);
        "data"u8.CopyTo(h[36..]);
        BinaryPrimitives.WriteInt32LittleEndian(h[40..], dataBytes);
        destination.Write(h);
        var bytes = new byte[Math.Min(dataBytes, 1 << 16)];
        var done = 0;
        while (done < _count)
        {
            var n = Math.Min(_count - done, bytes.Length / 4);
            for (var i = 0; i < n; i++)
            {
                BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(i * 4), _samples[done + i]);
            }

            destination.Write(bytes, 0, n * 4);
            done += n;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // The recording outlives the device so it can be read after shutdown.
    }
}
