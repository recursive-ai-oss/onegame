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
/// Output in a web page through Web Audio, behind the device package's backend seam. A page has one thread, so the device is manual: a page timer
/// calls the output's pump, which advances the device by the frames the AudioWorklet wants. Browsers keep audio suspended until the user has
/// interacted with the page; until then the frames are discarded at real-time pace and the device state reads <c>awaiting_gesture</c>. The first
/// click, tap or key press starts sound.
/// </summary>
[SupportedOSPlatform("browser")]
public sealed class WebAudioBackend : IAudioBackend, IDevicesHolder
{
    private readonly DevicesBackend _backend = new(new WebAudioDeviceBackend(new JsWebAudioHost()));

    /// <inheritdoc/>
    public string Name => _backend.Name;

    /// <inheritdoc/>
    public bool IsManual => true;

    /// <inheritdoc/>
    public IReadOnlyList<AudioDeviceInfo> ListDevices() => _backend.ListDevices();

    /// <inheritdoc/>
    public bool TryOpen(AudioDeviceOptions options, out IAudioSink? sink, out string reason) => _backend.TryOpen(options, out sink, out reason);

    DevicesBackend IDevicesHolder.Devices => _backend;

    /// <summary>Asks the browser to start audio now (<c>AudioContext.resume</c>). It only succeeds once the page has had a user gesture.</summary>
    /// <returns>The context's state afterwards, such as <c>running</c> or <c>suspended</c>.</returns>
    public static string Unlock() => WebAudioJs.Unlock();
}
