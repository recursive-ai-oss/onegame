using System.Runtime.InteropServices;

namespace OneGame.Audio;

/// <summary>The few ALSA PCM calls the Linux backend uses (alsa-lib reference manual, PCM interface). Loaded lazily so a machine without libasound just has no device.</summary>
internal static unsafe partial class Alsa
{
    private const string Lib = "libasound.so.2";

    public const int StreamPlayback = 0;
    public const int FormatS16Le = 2;
    public const int FormatFloatLe = 14;
    public const int AccessRwInterleaved = 3;
    public const int ErrPipe = 32;
    public const int ErrAgain = 11;

    // Installing a handler that does nothing silences alsa-lib's own stderr chatter; failures are reported through return codes and our own messages.
    [LibraryImport(Lib, EntryPoint = "snd_lib_error_set_handler")]
    public static partial int SetErrorHandler(nint handler);

    [LibraryImport(Lib, EntryPoint = "snd_pcm_open", StringMarshalling = StringMarshalling.Utf8)]
    public static partial int Open(out nint pcm, string name, int stream, int mode);

    [LibraryImport(Lib, EntryPoint = "snd_pcm_close")]
    public static partial int Close(nint pcm);

    // snd_pcm_set_params picks hardware parameters in one call; soft_resample lets the plugin layer convert rate when the hardware can't.
    [LibraryImport(Lib, EntryPoint = "snd_pcm_set_params")]
    public static partial int SetParams(nint pcm, int format, int access, uint channels, uint rate, int softResample, uint latencyMicroseconds);

    [LibraryImport(Lib, EntryPoint = "snd_pcm_get_params")]
    public static partial int GetParams(nint pcm, out nuint bufferSize, out nuint periodSize);

    [LibraryImport(Lib, EntryPoint = "snd_pcm_writei")]
    public static partial nint WriteI(nint pcm, void* buffer, nuint frames);

    [LibraryImport(Lib, EntryPoint = "snd_pcm_recover")]
    public static partial int Recover(nint pcm, int error, int silent);

    [LibraryImport(Lib, EntryPoint = "snd_pcm_delay")]
    public static partial int Delay(nint pcm, out nint frames);

    [LibraryImport(Lib, EntryPoint = "snd_pcm_state")]
    public static partial int State(nint pcm);

    /// <summary>Names of snd_pcm_state_t values (alsa-lib PCM interface).</summary>
    public static string StateName(int state) => state switch
    {
        0 => "open",
        1 => "setup",
        2 => "prepared",
        3 => "running",
        4 => "xrun",
        5 => "draining",
        6 => "paused",
        7 => "suspended",
        8 => "disconnected",
        _ => "unknown",
    };

    [LibraryImport(Lib, EntryPoint = "snd_pcm_drop")]
    public static partial int Drop(nint pcm);

    [LibraryImport(Lib, EntryPoint = "snd_device_name_hint", StringMarshalling = StringMarshalling.Utf8)]
    public static partial int NameHint(int card, string iface, out nint hints);

    [LibraryImport(Lib, EntryPoint = "snd_device_name_get_hint", StringMarshalling = StringMarshalling.Utf8)]
    public static partial nint NameGetHint(nint hint, string id);

    [LibraryImport(Lib, EntryPoint = "snd_device_name_free_hint")]
    public static partial int NameFreeHint(nint hints);

    [LibraryImport("libc", EntryPoint = "free")]
    public static partial void Free(nint pointer);
}
