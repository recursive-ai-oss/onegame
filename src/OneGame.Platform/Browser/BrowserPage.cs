using System.Runtime.Versioning;

namespace OneGame.Platform;

/// <summary>The page-level services a browser host needs around the platform backend: the animation-frame loop.</summary>
[SupportedOSPlatform("browser")]
public static class BrowserPage
{
    /// <summary>
    /// Calls <paramref name="frame"/> once per animation frame, with the time in milliseconds, until <see cref="StopFrames"/>.
    /// The browser owns the loop: a managed thread cannot wait for the next frame.
    /// </summary>
    public static void StartFrames(Action<double> frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        PlatformJs.StartFrames(frame);
    }

    /// <summary>Stops the loop started by <see cref="StartFrames"/>.</summary>
    public static void StopFrames() => PlatformJs.StopFrames();
}
