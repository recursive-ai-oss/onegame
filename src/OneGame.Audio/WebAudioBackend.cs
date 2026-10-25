using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace OneGame.Audio;

/// <summary>The functions of <c>onegame-audio.js</c>. The page registers the script as the module "onegame-audio" before the game starts.</summary>
[SupportedOSPlatform("browser")]
internal static partial class WebAudioJs
{
    private const string Module = "onegame-audio";

    [JSImport("create", Module)]
    public static partial string Create(int rate, int channels, int latencyMs);

    [JSImport("rate", Module)]
    public static partial int Rate();

    [JSImport("targetFrames", Module)]
    public static partial int TargetFrames();

    [JSImport("queued", Module)]
    public static partial int Queued();

    [JSImport("write", Module)]
    public static partial bool Write([JSMarshalAs<JSType.MemoryView>] Span<byte> bytes);

    [JSImport("setActive", Module)]
    public static partial void SetActive(bool active);

    [JSImport("unlock", Module)]
    public static partial string Unlock();

    [JSImport("state", Module)]
    public static partial string State();

    [JSImport("stats", Module)]
    public static partial string Stats();

    [JSImport("consumed", Module)]
    public static partial double Consumed();

    [JSImport("underruns", Module)]
    public static partial int Underruns();

    [JSImport("peak", Module)]
    public static partial double Peak();

    [JSImport("startTimer", Module)]
    public static partial void StartTimer([JSMarshalAs<JSType.Function>] Action callback, int milliseconds);

    [JSImport("close", Module)]
    public static partial void Close();
}

/// <summary>
/// Output in a web page through Web Audio. A page has one thread, so there is no audio thread: a page timer calls the output's pump, which
/// renders the mixer into the sink, and the sink posts the frames to an AudioWorklet that plays them. Browsers keep audio suspended until
/// the user has interacted with the page; until then the frames are discarded at real-time pace, and <see cref="AudioDeviceStatus.DeviceState"/>
/// reads <c>awaiting_gesture</c>. The first click, tap or key press starts sound.
/// </summary>
[SupportedOSPlatform("browser")]
public sealed class WebAudioBackend : IAudioBackend, IPumpedBackend
{
    /// <inheritdoc/>
    public string Name => "webaudio";

    /// <inheritdoc/>
    public bool IsManual => true;

    /// <inheritdoc/>
    public IReadOnlyList<AudioDeviceInfo> ListDevices() => [new AudioDeviceInfo(string.Empty, "Browser default output", true)];

    /// <inheritdoc/>
    public bool TryOpen(AudioDeviceOptions options, out IAudioSink? sink, out string reason)
    {
        sink = null;
        try
        {
            reason = WebAudioJs.Create(options.SampleRate, options.Channels, options.LatencyMilliseconds);
            if (reason.Length > 0)
            {
                return false;
            }

            sink = new WebAudioSink(new AudioDeviceFormat(WebAudioJs.Rate(), options.Channels), WebAudioJs.TargetFrames());
            return true;
        }
        catch (Exception e) when (e is JSException or PlatformNotSupportedException or DllNotFoundException)
        {
            reason = "the page did not register the audio module (onegame-audio): " + e.Message;
            return false;
        }
    }

    /// <inheritdoc/>
    public void SetPump(Action pump) => WebAudioJs.StartTimer(pump, 10);

    /// <summary>Asks the browser to start audio now (<c>AudioContext.resume</c>). It only succeeds once the page has had a user gesture.</summary>
    /// <returns>The context's state afterwards, such as <c>running</c> or <c>suspended</c>.</returns>
    public static string Unlock() => WebAudioJs.Unlock();
}

[SupportedOSPlatform("browser")]
internal sealed class WebAudioSink : IAudioSink, IPumpedSink, IAudioSinkDiagnostics, IAudioSinkControl
{
    private const int Period = 480;

    private readonly int _target;
    private long _written;

    public WebAudioSink(AudioDeviceFormat format, int targetFrames)
    {
        Format = format;
        _target = Math.Max(targetFrames, Period * 2);
    }

    public AudioDeviceFormat Format { get; }

    public int PeriodFrames => Period;

    public int BufferFrames => _target;

    public int QueuedFrames => WebAudioJs.Queued();

    public Action<bool>? SystemSuspendChanged { get; set; }

    public int WantedFrames
    {
        get
        {
            var wanted = _target - WebAudioJs.Queued();
            return wanted < Period ? 0 : wanted / Period * Period;
        }
    }

    public SinkWriteResult Write(ReadOnlySpan<float> interleaved)
    {
        _written += interleaved.Length / Format.Channels;
        var bytes = MemoryMarshal.AsBytes(interleaved);
        WebAudioJs.Write(MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(bytes), bytes.Length));
        return SinkWriteResult.Ok;
    }

    public void SetActive(bool active) => WebAudioJs.SetActive(active);

    public AudioSinkReport Report() =>
        new(WebAudioJs.State(), _written, (long)WebAudioJs.Consumed(), (float)WebAudioJs.Peak(), WebAudioJs.Underruns(), WebAudioJs.Stats());

    public void Dispose() => WebAudioJs.Close();
}
