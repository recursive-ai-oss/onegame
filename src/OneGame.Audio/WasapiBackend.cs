using System.Runtime.InteropServices;

namespace OneGame.Audio;

/// <summary>The parts of the WASAPI backend that are plain arithmetic and byte layout, kept apart so they can be checked on any OS.</summary>
internal static class WasapiLogic
{
    public const int ExtensibleFormatBytes = 40;
    public const int AudclntEDeviceInvalidated = unchecked((int)0x88890004);
    public const int AudclntEResourcesInvalidated = unchecked((int)0x88890026);
    public const int AudclntEServiceNotRunning = unchecked((int)0x88890010);
    public const int AudclntEBufferError = unchecked((int)0x88890018);
    public const int AudclntEEndpointCreateFailed = unchecked((int)0x88890008);

    /// <summary>100-nanosecond units, which is how the shared-mode API states buffer durations.</summary>
    public static long ToReferenceTime(int milliseconds) => milliseconds * 10_000L;

    /// <summary>Whether an HRESULT from a running stream means the endpoint is gone (unplugged, disabled, default changed under an exclusive client, service restarted).</summary>
    public static bool IsDeviceLost(int hresult) =>
        hresult is AudclntEDeviceInvalidated or AudclntEResourcesInvalidated or AudclntEServiceNotRunning or AudclntEEndpointCreateFailed;

    /// <summary>The speaker-position bits for a channel count (ksmedia.h SPEAKER_*), 0 when unspecified.</summary>
    public static uint ChannelMask(int channels) => channels switch
    {
        1 => 0x4,        // front center
        2 => 0x3,        // front left, front right
        4 => 0x33,       // quad
        6 => 0x3F,       // 5.1
        8 => 0x63F,      // 7.1
        _ => 0,
    };

    /// <summary>Writes a WAVEFORMATEXTENSIBLE for 32-bit IEEE float PCM into <paramref name="destination"/> (40 bytes).</summary>
    public static void WriteFloatFormat(Span<byte> destination, int sampleRate, int channels)
    {
        destination[..ExtensibleFormatBytes].Clear();
        var blockAlign = channels * 4;
        System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(destination, 0xFFFE);                       // WAVE_FORMAT_EXTENSIBLE
        System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(destination[2..], (ushort)channels);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(destination[4..], (uint)sampleRate);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(destination[8..], (uint)(sampleRate * blockAlign));
        System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(destination[12..], (ushort)blockAlign);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(destination[14..], 32);                     // bits per sample
        System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(destination[16..], 22);                     // cbSize
        System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(destination[18..], 32);                     // valid bits
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(destination[20..], ChannelMask(channels));
        // KSDATAFORMAT_SUBTYPE_IEEE_FLOAT {00000003-0000-0010-8000-00AA00389B71}
        ReadOnlySpan<byte> subFormat = [0x03, 0, 0, 0, 0, 0, 0x10, 0, 0x80, 0, 0, 0xAA, 0, 0x38, 0x9B, 0x71];
        subFormat.CopyTo(destination[24..]);
    }

    /// <summary>How many frames may be written now: the free part of the buffer, never more than wanted.</summary>
    public static int WritableFrames(int bufferFrames, int paddingFrames, int wanted) =>
        Math.Clamp(bufferFrames - paddingFrames, 0, wanted);
}

/// <summary>Windows output through WASAPI in shared mode: the system mixer converts to the device's format, so any rate and channel count works.</summary>
public sealed class WasapiBackend : IAudioBackend
{
    /// <inheritdoc/>
    public string Name => "wasapi";

    /// <inheritdoc/>
    public bool IsManual => false;

    /// <inheritdoc/>
    public IReadOnlyList<AudioDeviceInfo> ListDevices() => OperatingSystem.IsWindows() ? [new AudioDeviceInfo(string.Empty, "Default output", true)] : [];

    /// <inheritdoc/>
    public bool TryOpen(AudioDeviceOptions options, out IAudioSink? sink, out string reason)
    {
        ArgumentNullException.ThrowIfNull(options);
        sink = null;
        if (!OperatingSystem.IsWindows())
        {
            reason = "WASAPI is only available on Windows";
            return false;
        }

        try
        {
            var opened = WasapiSink.TryCreate(options, out var created, out reason);
            sink = created;
            return opened;
        }
        catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
        {
            reason = "the Windows audio libraries are missing";
            return false;
        }
    }
}

internal static unsafe partial class Com
{
    [LibraryImport("ole32.dll")]
    public static partial int CoInitializeEx(nint reserved, uint flags);

    [LibraryImport("ole32.dll")]
    public static partial int CoCreateInstance(Guid* clsid, nint outer, uint context, Guid* iid, out nint instance);

    [LibraryImport("ole32.dll")]
    public static partial void CoTaskMemFree(nint pointer);

    [LibraryImport("kernel32.dll")]
    public static partial void Sleep(uint milliseconds);

}

// COM calls through the interface's vtable: slots 0..2 are IUnknown, the interface's own methods follow in declaration order.
internal sealed unsafe class WasapiSink : IAudioSink
{
    private const uint CoinitMultithreaded = 0;
    private const uint ClsctxAll = 23;
    private const uint StreamFlags = 0x80000000u | 0x08000000u; // AUTOCONVERTPCM | SRC_DEFAULT_QUALITY

    [ThreadStatic]
    private static bool s_comReady;

    private nint _client;
    private nint _render;
    private readonly int _bufferFrames;
    private int _queued;
    private bool _started;
    private bool _primed;

    private WasapiSink(nint client, nint render, int rate, int channels, int bufferFrames)
    {
        _client = client;
        _render = render;
        Format = new AudioDeviceFormat(rate, channels);
        _bufferFrames = bufferFrames;
    }

    public AudioDeviceFormat Format { get; }

    public int PeriodFrames => Math.Max(64, _bufferFrames / 4);

    public int BufferFrames => _bufferFrames;

    public int QueuedFrames => Volatile.Read(ref _queued);

    private static void EnsureCom()
    {
        if (!s_comReady)
        {
            // S_FALSE (already initialised) and RPC_E_CHANGED_MODE (the thread is already in another apartment) both leave COM usable.
            _ = Com.CoInitializeEx(0, CoinitMultithreaded);
            s_comReady = true;
        }
    }

    public static bool TryCreate(AudioDeviceOptions options, out WasapiSink? sink, out string reason)
    {
        sink = null;
        EnsureCom();
        var clsidEnumerator = new Guid("BCDE0395-E52F-467C-8E3D-C4579291692E");
        var iidEnumerator = new Guid("A95664D2-9614-4F35-A746-DE8DB63617E6");
        var iidClient = new Guid("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2");
        var iidRender = new Guid("F294ACFC-3146-4483-A7BF-ADDCA7C260E2");
        nint enumerator = 0, device = 0, client = 0, render = 0;
        try
        {
            var hr = Com.CoCreateInstance(&clsidEnumerator, 0, ClsctxAll, &iidEnumerator, out enumerator);
            if (hr < 0)
            {
                reason = $"no audio device enumerator (0x{hr:X8})";
                return false;
            }

            if (string.IsNullOrEmpty(options.DeviceName))
            {
                // IMMDeviceEnumerator::GetDefaultAudioEndpoint(eRender = 0, eConsole = 0)
                hr = ((delegate* unmanaged[Stdcall]<nint, int, int, nint*, int>)(*(nint**)enumerator)[4])(enumerator, 0, 0, &device);
            }
            else
            {
                fixed (char* id = options.DeviceName)
                {
                    hr = ((delegate* unmanaged[Stdcall]<nint, char*, nint*, int>)(*(nint**)enumerator)[5])(enumerator, id, &device);
                }
            }

            if (hr < 0)
            {
                reason = $"no output device (0x{hr:X8})";
                return false;
            }

            // IMMDevice::Activate(iid, context, params, out interface)
            hr = ((delegate* unmanaged[Stdcall]<nint, Guid*, uint, nint, nint*, int>)(*(nint**)device)[3])(device, &iidClient, ClsctxAll, 0, &client);
            if (hr < 0)
            {
                reason = $"cannot activate the audio client (0x{hr:X8})";
                return false;
            }

            Span<byte> format = stackalloc byte[WasapiLogic.ExtensibleFormatBytes];
            WasapiLogic.WriteFloatFormat(format, options.SampleRate, options.Channels);
            fixed (byte* f = format)
            {
                // IAudioClient::Initialize(AUDCLNT_SHAREMODE_SHARED = 0, flags, duration, periodicity, format, session)
                hr = ((delegate* unmanaged[Stdcall]<nint, int, uint, long, long, byte*, nint, int>)(*(nint**)client)[3])(
                    client, 0, StreamFlags, WasapiLogic.ToReferenceTime(options.LatencyMilliseconds), 0, f, 0);
            }

            if (hr < 0)
            {
                reason = $"the device rejected {options.SampleRate} Hz x {options.Channels} (0x{hr:X8})";
                return false;
            }

            uint frames = 0;
            hr = ((delegate* unmanaged[Stdcall]<nint, uint*, int>)(*(nint**)client)[4])(client, &frames);
            if (hr < 0)
            {
                reason = $"cannot read the buffer size (0x{hr:X8})";
                return false;
            }

            hr = ((delegate* unmanaged[Stdcall]<nint, Guid*, nint*, int>)(*(nint**)client)[14])(client, &iidRender, &render);
            if (hr < 0)
            {
                reason = $"cannot get the render client (0x{hr:X8})";
                return false;
            }

            sink = new WasapiSink(client, render, options.SampleRate, options.Channels, (int)frames);
            client = 0;
            render = 0;
            reason = string.Empty;
            return true;
        }
        finally
        {
            ReleaseObject(render);
            ReleaseObject(client);
            ReleaseObject(device);
            ReleaseObject(enumerator);
        }
    }

    private static void ReleaseObject(nint obj)
    {
        if (obj != 0)
        {
            ((delegate* unmanaged[Stdcall]<nint, uint>)(*(nint**)obj)[2])(obj);
        }
    }

    public SinkWriteResult Write(ReadOnlySpan<float> interleaved)
    {
        if (_client == 0)
        {
            return SinkWriteResult.Lost;
        }

        EnsureCom();
        var channels = Format.Channels;
        var total = interleaved.Length / channels;
        var done = 0;
        var result = SinkWriteResult.Ok;
        var idle = 0;
        while (done < total)
        {
            uint padding = 0;
            // IAudioClient::GetCurrentPadding
            var hr = ((delegate* unmanaged[Stdcall]<nint, uint*, int>)(*(nint**)_client)[6])(_client, &padding);
            if (hr < 0)
            {
                return SinkWriteResult.Lost;
            }

            if (_primed && padding == 0 && done == 0)
            {
                result = SinkWriteResult.Underrun;
            }

            var n = WasapiLogic.WritableFrames(_bufferFrames, (int)padding, total - done);
            if (n == 0)
            {
                // The buffer is full: wait roughly a quarter of it for the device to drain, and give up if it never does.
                if (++idle > 500)
                {
                    return SinkWriteResult.Lost;
                }

                Com.Sleep((uint)Math.Max(1, (_bufferFrames / 4) * 1000 / Format.SampleRate));
                continue;
            }

            idle = 0;
            byte* data = null;
            // IAudioRenderClient::GetBuffer / ReleaseBuffer
            hr = ((delegate* unmanaged[Stdcall]<nint, uint, byte**, int>)(*(nint**)_render)[3])(_render, (uint)n, &data);
            if (hr < 0)
            {
                return SinkWriteResult.Lost;
            }

            interleaved.Slice(done * channels, n * channels).CopyTo(new Span<float>(data, n * channels));
            hr = ((delegate* unmanaged[Stdcall]<nint, uint, uint, int>)(*(nint**)_render)[4])(_render, (uint)n, 0);
            if (hr < 0)
            {
                return SinkWriteResult.Lost;
            }

            done += n;
            Volatile.Write(ref _queued, (int)padding + n);
        }

        _primed = true;
        if (!_started)
        {
            _started = true;
            // IAudioClient::Start, after the first block is queued so the stream does not begin empty.
            if (((delegate* unmanaged[Stdcall]<nint, int>)(*(nint**)_client)[10])(_client) < 0)
            {
                return SinkWriteResult.Lost;
            }
        }

        return result;
    }

    public void Dispose()
    {
        var client = _client;
        var render = _render;
        _client = 0;
        _render = 0;
        if (client != 0 && _started)
        {
            ((delegate* unmanaged[Stdcall]<nint, int>)(*(nint**)client)[11])(client); // Stop
        }

        ReleaseObject(render);
        ReleaseObject(client);
    }
}
