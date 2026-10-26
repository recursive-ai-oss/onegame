using System.Runtime.InteropServices;

namespace OneGame.Audio;

/// <summary>
/// Linux output through ALSA's PCM interface. On a PipeWire or PulseAudio desktop the <c>default</c> device is the sound server's own ALSA plugin,
/// so this reaches the system mixer, follows the user's default output and converts rate and channels; on a bare system it is the hardware.
/// One small, stable C API covers every Linux audio setup, which a sound server's native client API does not.
/// </summary>
public sealed unsafe class AlsaBackend : IAudioBackend
{
    private static int s_quiet;

    /// <inheritdoc/>
    public string Name => "alsa";

    /// <inheritdoc/>
    public bool IsManual => false;

    /// <inheritdoc/>
    public IReadOnlyList<AudioDeviceInfo> ListDevices()
    {
        var result = new List<AudioDeviceInfo>();
        try
        {
            Quiet();
            if (Alsa.NameHint(-1, "pcm", out var hints) < 0 || hints == 0)
            {
                return result;
            }

            try
            {
                for (var p = (nint*)hints; *p != 0; p++)
                {
                    var name = TakeString(*p, "NAME");
                    var io = TakeString(*p, "IOID");
                    if (name is null || io == "Input" || name == "null")
                    {
                        continue;
                    }

                    var desc = TakeString(*p, "DESC")?.Replace('\n', ' ') ?? name;
                    result.Add(new AudioDeviceInfo(name, desc, name == "default"));
                }
            }
            finally
            {
                _ = Alsa.NameFreeHint(hints);
            }
        }
        catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
        {
            result.Clear();
        }

        return result;
    }

    /// <inheritdoc/>
    public bool TryOpen(AudioDeviceOptions options, out IAudioSink? sink, out string reason)
    {
        ArgumentNullException.ThrowIfNull(options);
        sink = null;
        try
        {
            Quiet();
            var name = options.DeviceName ?? "default";
            var latency = (uint)Math.Clamp(options.LatencyMilliseconds, 5, 500) * 1000u;
            foreach (var format in (ReadOnlySpan<int>)[Alsa.FormatFloatLe, Alsa.FormatS16Le])
            {
                var rc = Alsa.Open(out var pcm, name, Alsa.StreamPlayback, 0);
                if (rc < 0)
                {
                    reason = $"snd_pcm_open('{name}') failed: errno {-rc}";
                    return false;
                }

                rc = Alsa.SetParams(pcm, format, Alsa.AccessRwInterleaved, (uint)options.Channels, (uint)options.SampleRate, 1, latency);
                if (rc < 0 || Alsa.GetParams(pcm, out var buffer, out var period) < 0)
                {
                    _ = Alsa.Close(pcm);
                    continue;
                }

                sink = new AlsaSink(pcm, format == Alsa.FormatFloatLe, options.SampleRate, options.Channels, (int)period, (int)buffer);
                reason = string.Empty;
                return true;
            }

            reason = $"'{options.DeviceName ?? "default"}' accepts neither float nor 16-bit {options.SampleRate} Hz x {options.Channels}";
            return false;
        }
        catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
        {
            reason = "libasound is not installed";
            return false;
        }
    }

    // alsa-lib's handler takes (file, line, function, err, format, ...); a callee that ignores its arguments is safe for the variadic tail.
    [UnmanagedCallersOnly(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static void Silence(byte* file, int line, byte* function, int err, byte* format)
    {
    }

    private static void Quiet()
    {
        if (Interlocked.Exchange(ref s_quiet, 1) == 0)
        {
            _ = Alsa.SetErrorHandler((nint)(delegate* unmanaged[Cdecl]<byte*, int, byte*, int, byte*, void>)&Silence);
        }
    }

    private static string? TakeString(nint hint, string id)
    {
        var p = Alsa.NameGetHint(hint, id);
        if (p == 0)
        {
            return null;
        }

        var s = Marshal.PtrToStringUTF8(p);
        Alsa.Free(p);
        return s;
    }
}

internal sealed unsafe class AlsaSink : IAudioSink, IAudioSinkDiagnostics
{
    private const int MaxRecoveries = 8;
    private nint _pcm;
    private readonly bool _float;
    private readonly short[]? _s16;
    private int _queued;

    // What the device side has seen. Only the audio thread (Write) touches the PCM handle; Report reads these cached figures,
    // because alsa-lib handles are not safe to query while another thread is inside a write.
    private const int BlockRing = 512;
    private readonly long[] _blockStart = new long[BlockRing];
    private readonly float[] _blockPeak = new float[BlockRing];
    private int _blockHead;
    private int _blockCount;
    private long _written;
    private int _peakBits;
    private long _xruns;
    private int _state;

    public AlsaSink(nint pcm, bool isFloat, int rate, int channels, int period, int buffer)
    {
        _pcm = pcm;
        _float = isFloat;
        Format = new AudioDeviceFormat(rate, channels);
        PeriodFrames = period;
        BufferFrames = buffer;
        // Sized for the largest block the output stage writes, so Write never allocates.
        _s16 = isFloat ? null : new short[2048 * channels];
    }

    public AudioDeviceFormat Format { get; }

    public int PeriodFrames { get; }

    public int BufferFrames { get; }

    public int QueuedFrames => Volatile.Read(ref _queued);

    public SinkWriteResult Write(ReadOnlySpan<float> interleaved)
    {
        if (_pcm == 0)
        {
            return SinkWriteResult.Lost;
        }

        var channels = Format.Channels;
        var frames = interleaved.Length / channels;
        var result = SinkWriteResult.Ok;
        var failures = 0;
        var done = 0;

        if (!_float)
        {
            if (_s16 is null || interleaved.Length > _s16.Length)
            {
                return SinkWriteResult.Lost;
            }

            for (var i = 0; i < interleaved.Length; i++)
            {
                _s16[i] = (short)MathF.Round(interleaved[i] * 32767f);
            }
        }

        fixed (float* f = interleaved)
        fixed (short* s = _s16)
        {
            while (done < frames)
            {
                var offset = done * channels;
                void* p = _float ? f + offset : s + offset;
                var n = Alsa.WriteI(_pcm, p, (nuint)(frames - done));
                if (n >= 0)
                {
                    done += (int)n;
                    continue;
                }

                var err = (int)-n;
                if (err == Alsa.ErrAgain)
                {
                    continue;
                }

                if (err == Alsa.ErrPipe)
                {
                    result = SinkWriteResult.Underrun;
                }

                // Underrun and suspend are recoverable; anything else (a vanished device) is not.
                if (++failures > MaxRecoveries || Alsa.Recover(_pcm, (int)n, 1) < 0)
                {
                    return SinkWriteResult.Lost;
                }
            }
        }

        if (result == SinkWriteResult.Underrun)
        {
            Interlocked.Increment(ref _xruns);
        }

        // Frames the device has taken are those we handed over minus what is still queued (snd_pcm_delay: frames between the application
        // position and the speaker). The call fails in the xrun state, where everything handed over has been consumed.
        var written = _written + frames;
        var queued = Alsa.Delay(_pcm, out var delay) == 0 ? (int)Math.Clamp(delay, 0, written) : 0;
        Volatile.Write(ref _queued, queued);
        Volatile.Write(ref _state, Alsa.State(_pcm));
        TrackPlayed(interleaved, written, written - queued);
        return result;
    }

    // Peak is measured on the frames the device has consumed, not on what we wrote: a block counts once its first frame has been played.
    private void TrackPlayed(ReadOnlySpan<float> block, long written, long played)
    {
        var peak = 0f;
        foreach (var v in block)
        {
            peak = MathF.Max(peak, MathF.Abs(v));
        }

        if (_blockCount == BlockRing)
        {
            Fold(_blockPeak[_blockHead]);
            _blockHead = (_blockHead + 1) % BlockRing;
            _blockCount--;
        }

        var slot = (_blockHead + _blockCount) % BlockRing;
        _blockStart[slot] = _written;
        _blockPeak[slot] = peak;
        _blockCount++;
        _written = written;
        while (_blockCount > 0 && _blockStart[_blockHead] < played)
        {
            Fold(_blockPeak[_blockHead]);
            _blockHead = (_blockHead + 1) % BlockRing;
            _blockCount--;
        }

        Interlocked.Exchange(ref _playedShared, played);
        Interlocked.Exchange(ref _writtenShared, written);
    }

    private long _playedShared;
    private long _writtenShared;

    private void Fold(float peak)
    {
        if (peak > BitConverter.Int32BitsToSingle(Volatile.Read(ref _peakBits)))
        {
            Volatile.Write(ref _peakBits, BitConverter.SingleToInt32Bits(peak));
        }
    }

    public AudioSinkReport Report()
    {
        var state = _pcm == 0 ? "closed" : Alsa.StateName(Volatile.Read(ref _state));
        return new AudioSinkReport(
            state, Interlocked.Read(ref _writtenShared), Interlocked.Read(ref _playedShared), BitConverter.Int32BitsToSingle(Volatile.Read(ref _peakBits)),
            Interlocked.Read(ref _xruns), null);
    }

    public void Dispose()
    {
        var pcm = _pcm;
        _pcm = 0;
        if (pcm != 0)
        {
            // Drop rather than drain: closing must not wait for queued sound.
            _ = Alsa.Drop(pcm);
            _ = Alsa.Close(pcm);
        }
    }
}
