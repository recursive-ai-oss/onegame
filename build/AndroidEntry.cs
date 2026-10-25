using System.Runtime.InteropServices;

// Compiled into a game built for Android (build/Android.targets). The native activity loads the game's shared library and calls this
// exported function on the system's main thread; the game's Main then runs on a thread of its own.
internal static class OneGameAndroidEntry
{
    [UnmanagedCallersOnly(EntryPoint = "ANativeActivity_onCreate")]
    private static void OnCreate(nint activity, nint savedState, nuint savedStateSize) =>
        OneGame.Platform.AndroidActivityHost.Start(activity, args =>
        {
#if ONEGAME_DEV
            OneGame.Live.Dev.Enable();
#endif
            return global::Program.Main(args);
        });
}
