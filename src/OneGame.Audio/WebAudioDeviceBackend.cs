using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Dev = OneAudio.Devices;

namespace OneGame.Audio;

/// <summary>The page's side of Web Audio, as the device backend needs it. The production implementation calls the functions of <c>onegame-audio.js</c>; tests supply a fake.</summary>
internal interface IWebAudioHost
{
    /// <summary>Creates the context and worklet; returns an empty string, or the reason it failed.</summary>
    string Create(int rate, int channels, int latencyMilliseconds);

    int Rate();

    int TargetFrames();

    int Queued();

    void Write(ReadOnlySpan<byte> bytes);

    void SetActive(bool active);

    string State();

    string Stats();

    double Consumed();

    int Underruns();

    void Close();

    void StartTimer(Action callback, int milliseconds);
}

/// <summary>
/// Web Audio behind the device package's backend seam. A page has no audio thread: a timer calls <c>AudioOutput.Pump</c>, which calls <see cref="PumpNow"/>,
/// and the stream renders what the worklet needs through the engine on the page's thread. (The package's manual-stream hook is internal, so the
/// stream pumps itself; in a browser the package's supervisor thread would still have to be switched off.)
/// </summary>
internal sealed class WebAudioDeviceBackend(IWebAudioHost host) : Dev.IAudioDeviceBackend, IPumpedBackend, IPumpedDeviceBackend
{
    private const int Period = 480;
    // The engine owns the stream and disposes it; the backend keeps only what the page timer needs.
    private Func<int, int>? _render;
    private int _bufferFrames;

    public string Name => "webaudio";

    public Dev.DeviceBackendKind Kind => Dev.DeviceBackendKind.Custom;

    public bool IsManual => false;

    public bool NotifiesDeviceChanges => false;

    public bool IsAvailable => true;

    public int PumpNow()
    {
        var render = _render;
        if (render is null)
        {
            return 0;
        }

        var wanted = _bufferFrames - host.Queued();
        return wanted < Period ? 0 : render(wanted / Period * Period);
    }

    public IReadOnlyList<Dev.AudioDeviceInfo> List(Dev.DeviceDirection direction) =>
        direction == Dev.DeviceDirection.Output ? [Browser] : [];

    public Dev.AudioDeviceInfo? GetDefault(Dev.DeviceDirection direction) => direction == Dev.DeviceDirection.Output ? Browser : null;

    public bool TryOpen(Dev.DeviceDirection direction, Dev.DeviceStreamOptions options, Dev.IDeviceEngine engine, out Dev.IOpenDevice? device, out string reason)
    {
        device = null;
        if (direction != Dev.DeviceDirection.Output)
        {
            reason = "The browser backend has no capture.";
            return false;
        }

        reason = host.Create(options.SampleRate, options.Channels, options.LatencyMilliseconds);
        if (reason.Length > 0)
        {
            return false;
        }

        if (host.Rate() != options.SampleRate)
        {
            // The device seam requires the asked-for rate. The page's context may run at another; resampling there is not built.
            reason = $"the browser's audio context runs at {host.Rate()} Hz, not {options.SampleRate} Hz";
            host.Close();
            return false;
        }

        var stream = new WebAudioStream(host, engine, options.SampleRate, options.Channels, Math.Max(host.TargetFrames(), Period * 2));
        _render = stream.Render;
        _bufferFrames = stream.BufferFrames;
        device = stream;
        return true;
    }

    public void SetPump(Action pump) => host.StartTimer(pump, 10);

    private static Dev.AudioDeviceInfo Browser { get; } =
        new(string.Empty, "Browser default output", Dev.DeviceDirection.Output, true, 0, 0, 0, Dev.DeviceBackendKind.Custom);
}

internal sealed class WebAudioStream : Dev.IOpenDevice
{
    private readonly IWebAudioHost _host;
    private readonly Dev.IDeviceEngine _engine;
    private readonly float[] _scratch;
    private readonly int _channels;
    private int _active;
    private int _lastUnderruns;

    public WebAudioStream(IWebAudioHost host, Dev.IDeviceEngine engine, int rate, int channels, int bufferFrames)
    {
        _host = host;
        _engine = engine;
        _channels = channels;
        _scratch = new float[480 * channels];
        Format = new Dev.AudioDeviceFormat(rate, channels);
        BufferFrames = bufferFrames;
    }

    public Dev.AudioDeviceFormat Format { get; }

    public Dev.AudioDeviceInfo? Device => null;

    public int PeriodFrames => 480;

    public int BufferFrames { get; }

    public long FramesPlayed => (long)_host.Consumed();

    public int QueuedFrames => _host.Queued();

    public string? Detail => _host.Stats();

    public bool Start(out string reason)
    {
        reason = string.Empty;
        Volatile.Write(ref _active, 1);
        _host.SetActive(true);
        return true;
    }

    public void Pause()
    {
        Volatile.Write(ref _active, 0);
        _host.SetActive(false);
    }

    public void Dispose()
    {
        Volatile.Write(ref _active, 0);
        _host.Close();
    }

    public int Render(int frames)
    {
        if (Volatile.Read(ref _active) == 0)
        {
            return 0;
        }

        var done = 0;
        while (done < frames)
        {
            var n = Math.Min(480, frames - done);
            var block = _scratch.AsSpan(0, n * _channels);
            _engine.Render(block);
            _host.Write(MemoryMarshal.AsBytes(block));
            done += n;
        }

        // The worklet counts its own dry spells; the engine wants to hear about them.
        var underruns = _host.Underruns();
        if (underruns > _lastUnderruns)
        {
            _engine.NotifyXrun(underruns - _lastUnderruns);
            _lastUnderruns = underruns;
        }

        return done;
    }
}

/// <summary>The production host: the functions of <c>onegame-audio.js</c>.</summary>
[SupportedOSPlatform("browser")]
internal sealed class JsWebAudioHost : IWebAudioHost
{
    public string Create(int rate, int channels, int latencyMilliseconds) => WebAudioJs.Create(rate, channels, latencyMilliseconds);

    public int Rate() => WebAudioJs.Rate();

    public int TargetFrames() => WebAudioJs.TargetFrames();

    public int Queued() => WebAudioJs.Queued();

    public void Write(ReadOnlySpan<byte> bytes)
    {
        WebAudioJs.Write(MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(bytes), bytes.Length));
    }

    public void SetActive(bool active) => WebAudioJs.SetActive(active);

    public string State() => WebAudioJs.State();

    public string Stats() => WebAudioJs.Stats();

    public double Consumed() => WebAudioJs.Consumed();

    public int Underruns() => WebAudioJs.Underruns();

    public void Close() => WebAudioJs.Close();

    public void StartTimer(Action callback, int milliseconds) => WebAudioJs.StartTimer(callback, milliseconds);
}
