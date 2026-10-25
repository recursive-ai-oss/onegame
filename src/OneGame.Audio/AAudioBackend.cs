using System.Runtime.InteropServices;
using System.Threading;

namespace OneGame.Audio;

/// <summary>
/// The AAudio calls the Android backend uses (NDK reference, "Audio" module, API level 26). Loaded lazily so an older system or a
/// build host without the library just has no device.
/// </summary>
internal static unsafe partial class AAudio
{
    private const string Lib = "libaaudio.so";

    public const int Ok = 0;
    public const int ErrorDisconnected = -899;
    public const int DirectionOutput = 0;
    public const int FormatPcmFloat = 2;
    public const int SharingShared = 1;
    public const int PerformanceLowLatency = 12;
    public const int UsageGame = 14;
    public const int ContentSonification = 4;

    public const int StateStarted = 4;
    public const int StatePaused = 6;
    public const int StateStopped = 10;
    public const int StateDisconnected = 13;

    [LibraryImport(Lib, EntryPoint = "AAudio_createStreamBuilder")]
    public static partial int CreateStreamBuilder(out nint builder);

    [LibraryImport(Lib, EntryPoint = "AAudioStreamBuilder_delete")]
    public static partial int BuilderDelete(nint builder);

    [LibraryImport(Lib, EntryPoint = "AAudioStreamBuilder_setDirection")]
    public static partial void SetDirection(nint builder, int direction);

    [LibraryImport(Lib, EntryPoint = "AAudioStreamBuilder_setFormat")]
    public static partial void SetFormat(nint builder, int format);

    [LibraryImport(Lib, EntryPoint = "AAudioStreamBuilder_setChannelCount")]
    public static partial void SetChannelCount(nint builder, int channels);

    [LibraryImport(Lib, EntryPoint = "AAudioStreamBuilder_setSampleRate")]
    public static partial void SetSampleRate(nint builder, int rate);

    [LibraryImport(Lib, EntryPoint = "AAudioStreamBuilder_setSharingMode")]
    public static partial void SetSharingMode(nint builder, int mode);

    [LibraryImport(Lib, EntryPoint = "AAudioStreamBuilder_setPerformanceMode")]
    public static partial void SetPerformanceMode(nint builder, int mode);

    [LibraryImport(Lib, EntryPoint = "AAudioStreamBuilder_setDeviceId")]
    public static partial void SetDeviceId(nint builder, int id);

    [LibraryImport(Lib, EntryPoint = "AAudioStreamBuilder_openStream")]
    public static partial int OpenStream(nint builder, out nint stream);

    [LibraryImport(Lib, EntryPoint = "AAudioStream_close")]
    public static partial int Close(nint stream);

    [LibraryImport(Lib, EntryPoint = "AAudioStream_requestStart")]
    public static partial int RequestStart(nint stream);

    [LibraryImport(Lib, EntryPoint = "AAudioStream_requestPause")]
    public static partial int RequestPause(nint stream);

    [LibraryImport(Lib, EntryPoint = "AAudioStream_requestStop")]
    public static partial int RequestStop(nint stream);

    [LibraryImport(Lib, EntryPoint = "AAudioStream_write")]
    public static partial int Write(nint stream, float* buffer, int frames, long timeoutNanoseconds);

    [LibraryImport(Lib, EntryPoint = "AAudioStream_getState")]
    public static partial int GetState(nint stream);

    [LibraryImport(Lib, EntryPoint = "AAudioStream_getSampleRate")]
    public static partial int GetSampleRate(nint stream);

    [LibraryImport(Lib, EntryPoint = "AAudioStream_getChannelCount")]
    public static partial int GetChannelCount(nint stream);

    [LibraryImport(Lib, EntryPoint = "AAudioStream_getFramesPerBurst")]
    public static partial int GetFramesPerBurst(nint stream);

    [LibraryImport(Lib, EntryPoint = "AAudioStream_getBufferSizeInFrames")]
    public static partial int GetBufferSize(nint stream);

    [LibraryImport(Lib, EntryPoint = "AAudioStream_getBufferCapacityInFrames")]
    public static partial int GetBufferCapacity(nint stream);

    [LibraryImport(Lib, EntryPoint = "AAudioStream_setBufferSizeInFrames")]
    public static partial int SetBufferSize(nint stream, int frames);

    [LibraryImport(Lib, EntryPoint = "AAudioStream_getFramesWritten")]
    public static partial long GetFramesWritten(nint stream);

    [LibraryImport(Lib, EntryPoint = "AAudioStream_getFramesRead")]
    public static partial long GetFramesRead(nint stream);

    [LibraryImport(Lib, EntryPoint = "AAudioStream_getXRunCount")]
    public static partial int GetXRunCount(nint stream);

    [LibraryImport(Lib, EntryPoint = "AAudioStream_isMMapUsed")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool IsMMapUsed(nint stream);

    // Added in API level 28; a system that lacks it just keeps the default usage, so it is looked up by name and optional.
    public static void TrySetUsage(nint builder, int usage, int contentType)
    {
        if (NativeLibrary.TryLoad(Lib, out var lib))
        {
            if (NativeLibrary.TryGetExport(lib, "AAudioStreamBuilder_setUsage", out var setUsage))
            {
                ((delegate* unmanaged<nint, int, void>)setUsage)(builder, usage);
            }

            if (NativeLibrary.TryGetExport(lib, "AAudioStreamBuilder_setContentType", out var setContent))
            {
                ((delegate* unmanaged<nint, int, void>)setContent)(builder, contentType);
            }
        }
    }
}

/// <summary>
/// Android output through AAudio: a low-latency shared stream of 32-bit float, written with blocking writes from the audio thread. The
/// stream is paused when the app goes to the background and started when it returns; a disconnected device (headphones pulled, a route
/// change the system answers by closing the stream) makes the next write report <see cref="SinkWriteResult.Lost"/>, and the output opens
/// a new stream on the new default device.
/// </summary>
public sealed class AAudioBackend : IAudioBackend
{
    /// <inheritdoc/>
    public string Name => "aaudio";

    /// <inheritdoc/>
    public bool IsManual => false;

    /// <inheritdoc/>
    public IReadOnlyList<AudioDeviceInfo> ListDevices() =>
        // Routing is the system's job; an app that wants a specific device passes its numeric id as the name.
        [new AudioDeviceInfo(string.Empty, "System default", true)];

    /// <inheritdoc/>
    public bool TryOpen(AudioDeviceOptions options, out IAudioSink? sink, out string reason)
    {
        sink = null;
        try
        {
            var stream = AAudioSink.TryOpen(options, out reason);
            sink = stream;
            return stream is not null;
        }
        catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
        {
            reason = "AAudio is not available on this system (Android 8.0 or later has it): " + e.Message;
            return false;
        }
    }
}

internal sealed unsafe class AAudioSink : IAudioSink, IAudioSinkDiagnostics, IAudioSinkControl
{
    private const int History = 128;

    private readonly nint _stream;
    private readonly bool _mmap;
    private readonly long[] _blockEnd = new long[History];
    private readonly float[] _blockPeak = new float[History];
    private int _head;
    private int _tail;
    private long _framesOffered;
    private int _peakBits;
    private long _seenXRuns;
    private int _active = 1;
    private int _closed;

    private AAudioSink(nint stream, AudioDeviceFormat format, int period, int buffer, bool mmap)
    {
        _stream = stream;
        Format = format;
        PeriodFrames = period;
        BufferFrames = buffer;
        _mmap = mmap;
    }

    public AudioDeviceFormat Format { get; }

    public int PeriodFrames { get; }

    public int BufferFrames { get; }

    public int QueuedFrames => (int)Math.Max(0, AAudio.GetFramesWritten(_stream) - AAudio.GetFramesRead(_stream));

    public Action<bool>? SystemSuspendChanged { get; set; }

    public static AAudioSink? TryOpen(AudioDeviceOptions options, out string reason)
    {
        var result = AAudio.CreateStreamBuilder(out var builder);
        if (result != AAudio.Ok)
        {
            reason = $"AAudio could not create a stream builder (code {result}).";
            return null;
        }

        try
        {
            AAudio.SetDirection(builder, AAudio.DirectionOutput);
            AAudio.SetFormat(builder, AAudio.FormatPcmFloat);
            AAudio.SetChannelCount(builder, options.Channels);
            AAudio.SetSampleRate(builder, options.SampleRate);
            AAudio.SetSharingMode(builder, AAudio.SharingShared);
            AAudio.SetPerformanceMode(builder, AAudio.PerformanceLowLatency);
            if (int.TryParse(options.DeviceName, out var deviceId))
            {
                AAudio.SetDeviceId(builder, deviceId);
            }

            // Game sound effects and music, so the system's focus and volume policy treats the app as a game.
            AAudio.TrySetUsage(builder, AAudio.UsageGame, AAudio.ContentSonification);

            result = AAudio.OpenStream(builder, out var stream);
            if (result != AAudio.Ok || stream == 0)
            {
                reason = $"AAudio could not open an output stream (code {result}).";
                return null;
            }

            var format = new AudioDeviceFormat(AAudio.GetSampleRate(stream), AAudio.GetChannelCount(stream));
            var burst = Math.Max(AAudio.GetFramesPerBurst(stream), 16);

            // The shared buffer is sized in whole bursts: the requested latency rounded up, never below two bursts so one late write doesn't underrun.
            var wanted = Math.Max(2 * burst, options.LatencyMilliseconds * format.SampleRate / 1000);
            wanted = (wanted + burst - 1) / burst * burst;
            var actual = AAudio.SetBufferSize(stream, wanted);
            var buffer = actual > 0 ? actual : AAudio.GetBufferSize(stream);

            var sink = new AAudioSink(stream, format, burst, buffer, AAudio.IsMMapUsed(stream));
            result = AAudio.RequestStart(stream);
            if (result != AAudio.Ok)
            {
                sink.Dispose();
                reason = $"AAudio could not start the stream (code {result}).";
                return null;
            }

            reason = string.Empty;
            return sink;
        }
        finally
        {
            _ = AAudio.BuilderDelete(builder);
        }
    }

    public SinkWriteResult Write(ReadOnlySpan<float> interleaved)
    {
        if (Volatile.Read(ref _closed) != 0)
        {
            return SinkWriteResult.Lost;
        }

        var channels = Format.Channels;
        var frames = interleaved.Length / channels;
        var peak = 0f;
        foreach (var s in interleaved)
        {
            var a = MathF.Abs(s);
            if (a > peak)
            {
                peak = a;
            }
        }

        var done = 0;
        var idle = 0;
        fixed (float* p = interleaved)
        {
            while (done < frames)
            {
                // The timeout bounds a stalled stream (the system stopped it); a normal write blocks for less than a buffer.
                var n = AAudio.Write(_stream, p + (done * channels), frames - done, 100_000_000);
                if (n < 0)
                {
                    return SinkWriteResult.Lost;
                }

                done += n;
                idle = n == 0 ? idle + 1 : 0;
                if (idle >= 5)
                {
                    // Half a second without the stream taking a frame: it is stuck, not slow. Ask for a fresh one.
                    return SinkWriteResult.Lost;
                }
            }
        }

        Record(frames, peak);
        var xruns = AAudio.GetXRunCount(_stream);
        if (xruns > _seenXRuns)
        {
            _seenXRuns = xruns;
            return SinkWriteResult.Underrun;
        }

        return SinkWriteResult.Ok;
    }

    // Each block's loudest sample is kept until the device has read past the block, so the report's peak is of audio the device took.
    private void Record(int frames, float peak)
    {
        _framesOffered += frames;
        if (_head - _tail < History)
        {
            _blockEnd[_head % History] = _framesOffered;
            _blockPeak[_head % History] = peak;
            _head++;
        }

        var read = AAudio.GetFramesRead(_stream);
        var played = BitConverter.Int32BitsToSingle(Volatile.Read(ref _peakBits));
        while (_tail < _head && _blockEnd[_tail % History] <= read)
        {
            played = Math.Max(played, _blockPeak[_tail % History]);
            _tail++;
        }

        Volatile.Write(ref _peakBits, BitConverter.SingleToInt32Bits(played));
    }

    public void SetActive(bool active)
    {
        if (Volatile.Read(ref _closed) != 0 || (Volatile.Read(ref _active) != 0) == active)
        {
            return;
        }

        Volatile.Write(ref _active, active ? 1 : 0);
        if (active)
        {
            _ = AAudio.RequestStart(_stream);
        }
        else
        {
            _ = AAudio.RequestPause(_stream);
        }
    }

    public AudioSinkReport Report()
    {
        if (Volatile.Read(ref _closed) != 0)
        {
            return new AudioSinkReport("closed", 0, 0, 0, 0, null);
        }

        var state = AAudio.GetState(_stream) switch
        {
            AAudio.StateStarted => "started",
            AAudio.StatePaused => "paused",
            AAudio.StateStopped => "stopped",
            AAudio.StateDisconnected => "disconnected",
            _ => "other",
        };
        return new AudioSinkReport(
            state, AAudio.GetFramesWritten(_stream), AAudio.GetFramesRead(_stream), BitConverter.Int32BitsToSingle(Volatile.Read(ref _peakBits)),
            AAudio.GetXRunCount(_stream), _mmap ? "mmap" : "legacy path");
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _closed, 1) != 0)
        {
            return;
        }

        _ = AAudio.RequestStop(_stream);
        _ = AAudio.Close(_stream);
    }
}
