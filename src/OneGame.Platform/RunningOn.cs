namespace OneGame.Platform;

/// <summary>
/// Which kind of system the game is running on. Use this instead of <see cref="OperatingSystem"/> for Android: a NativeAOT game built
/// for Android (<c>linux-bionic</c>) runs on the Linux runtime, so <c>OperatingSystem.IsAndroid()</c> is false there while
/// <c>OperatingSystem.IsLinux()</c> is true. The answers are fixed for the life of the process and are right from the first call.
/// </summary>
public static class RunningOn
{
    // Every Android app process has ANDROID_ROOT, which the system exports to it; no desktop Linux sets it.
    private static readonly bool s_android =
        OperatingSystem.IsAndroid() || (OperatingSystem.IsLinux() && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ANDROID_ROOT")));

    /// <summary>True in an Android app, whether the game runs on the Android runtime or as a NativeAOT library.</summary>
    public static bool Android => s_android;

    /// <summary>True on iOS (not macOS).</summary>
    public static bool IOS => OperatingSystem.IsIOS();

    /// <summary>True in a web page.</summary>
    public static bool Browser => OperatingSystem.IsBrowser();

    /// <summary>True on Android or iOS: a touch device with no program to quit and no window to close.</summary>
    public static bool Phone => s_android || OperatingSystem.IsIOS();

    /// <summary>True on Android, iOS or in a browser: where a game has no process of its own to quit.</summary>
    public static bool Mobile => Phone || OperatingSystem.IsBrowser();

    /// <summary>True on Windows, macOS or Linux that is not Android.</summary>
    public static bool Desktop => !Mobile;
}
