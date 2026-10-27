using System.Runtime.InteropServices;
using Dev = OneAudio.Devices;

namespace OneGame.Audio;

/// <summary>How an iOS app's audio relates to other apps' audio and the ring/silent switch (AVAudioSession categories).</summary>
public enum AudioSessionCategory
{
    /// <summary>Mixes with other apps' audio and follows the silent switch. The default for games.</summary>
    Ambient,

    /// <summary>Silences other apps' audio and follows the silent switch.</summary>
    SoloAmbient,

    /// <summary>Plays even with the silent switch on and silences other apps' audio.</summary>
    Playback,
}

/// <summary>
/// Apple output: RemoteIO on iOS (after the session is set up) and the default output unit on macOS, provided by the device package.
/// On iOS the backend reacts to the session's interruption notifications (calls, Siri, alarms): the device stops, the mixer's clock holds
/// still, and sound resumes where it was when the interruption ends. The app lifecycle stops and starts the device the same way.
/// </summary>
public sealed class AudioUnitBackend : IAudioBackend, IDevicesHolder
{
    private readonly DevicesBackend _backend;

    /// <summary>Creates the backend.</summary>
    /// <param name="category">The iOS session category; ignored on macOS.</param>
    public AudioUnitBackend(AudioSessionCategory category = AudioSessionCategory.Ambient)
    {
        Category = category;
        _backend = new DevicesBackend(new Dev.CoreAudioBackend())
        {
            AppleSession = category switch
            {
                AudioSessionCategory.Playback => Dev.AppleSessionCategory.Playback,
                AudioSessionCategory.SoloAmbient => Dev.AppleSessionCategory.SoloAmbient,
                _ => Dev.AppleSessionCategory.Ambient,
            },
        };
    }

    /// <summary>The iOS session category this backend sets.</summary>
    public AudioSessionCategory Category { get; }

    /// <inheritdoc/>
    public string Name => "audiounit";

    /// <inheritdoc/>
    public bool IsManual => false;

    /// <inheritdoc/>
    public IReadOnlyList<AudioDeviceInfo> ListDevices() => _backend.ListDevices();

    /// <inheritdoc/>
    public bool TryOpen(AudioDeviceOptions options, out IAudioSink? sink, out string reason) => _backend.TryOpen(options, out sink, out reason);

    DevicesBackend IDevicesHolder.Devices => _backend;

    /// <summary>Posts the same notification the system sends when another app or a call takes the audio session (<c>AVAudioSessionInterruptionNotification</c>), so the whole interruption path runs without a phone call. iOS only.</summary>
    /// <param name="began"><see langword="true"/> for the start of an interruption, <see langword="false"/> for its end.</param>
    /// <returns>Whether the notification was posted.</returns>
    public static bool SimulateInterruption(bool began) => AppleInterruption.Post(began);
}

// Posts AVAudioSession's interruption notification through the Objective-C runtime (Objective-C Runtime reference; AVAudioSession class reference).
internal static unsafe partial class AppleInterruption
{
    private const string Runtime = "/usr/lib/libobjc.A.dylib";
    private const string Framework = "/System/Library/Frameworks/AVFAudio.framework/AVFAudio";

    [LibraryImport(Runtime, EntryPoint = "objc_getClass", StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint GetClass(string name);

    [LibraryImport(Runtime, EntryPoint = "sel_registerName", StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint Sel(string name);

    [LibraryImport(Runtime, EntryPoint = "objc_msgSend")]
    private static partial nint Send(nint receiver, nint selector);

    [LibraryImport(Runtime, EntryPoint = "objc_msgSend")]
    private static partial nint SendUIntArg(nint receiver, nint selector, nuint a);

    [LibraryImport(Runtime, EntryPoint = "objc_msgSend")]
    private static partial nint SendDictionary(nint receiver, nint selector, nint value, nint key);

    [LibraryImport(Runtime, EntryPoint = "objc_msgSend")]
    private static partial void SendPost(nint receiver, nint selector, nint name, nint obj, nint userInfo);

    [LibraryImport(Runtime, EntryPoint = "objc_autoreleasePoolPush")]
    private static partial nint PoolPush();

    [LibraryImport(Runtime, EntryPoint = "objc_autoreleasePoolPop")]
    private static partial void PoolPop(nint pool);

    public static bool Post(bool began)
    {
        if (!OperatingSystem.IsIOS())
        {
            return false;
        }

        try
        {
            var lib = NativeLibrary.Load(Framework);
            var poolHandle = PoolPush();
            try
            {
                var session = Send(GetClass("AVAudioSession"), Sel("sharedInstance"));
                var center = Send(GetClass("NSNotificationCenter"), Sel("defaultCenter"));
                var number = SendUIntArg(GetClass("NSNumber"), Sel("numberWithUnsignedInteger:"), began ? 1u : 2u);
                var info = SendDictionary(GetClass("NSDictionary"), Sel("dictionaryWithObject:forKey:"), number, Constant(lib, "AVAudioSessionInterruptionTypeKey"));
                SendPost(center, Sel("postNotificationName:object:userInfo:"), Constant(lib, "AVAudioSessionInterruptionNotification"), session, info);
                return true;
            }
            finally
            {
                PoolPop(poolHandle);
            }
        }
        catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
        {
            return false;
        }
    }

    // An NSString* constant a framework exports as a global variable, such as a notification name.
    private static nint Constant(nint library, string symbol) => *(nint*)NativeLibrary.GetExport(library, symbol);
}

/// <summary>Windows output through WASAPI, provided by the device package. Kept as a type of this assembly so code that named it keeps working.</summary>
public sealed class WasapiBackend : IAudioBackend, IDevicesHolder
{
    private readonly DevicesBackend _backend = new(new Dev.WasapiBackend());

    /// <inheritdoc/>
    public string Name => _backend.Name;

    /// <inheritdoc/>
    public bool IsManual => false;

    /// <inheritdoc/>
    public IReadOnlyList<AudioDeviceInfo> ListDevices() => _backend.ListDevices();

    /// <inheritdoc/>
    public bool TryOpen(AudioDeviceOptions options, out IAudioSink? sink, out string reason) => _backend.TryOpen(options, out sink, out reason);

    DevicesBackend IDevicesHolder.Devices => _backend;
}
