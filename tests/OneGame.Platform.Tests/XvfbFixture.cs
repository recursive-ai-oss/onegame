using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OneGame.Platform.Tests;

/// <summary>A private Xvfb server for one test class. Tests skip themselves when Xvfb is not installed.</summary>
public sealed class XvfbFixture : IDisposable
{
    private readonly Process? _process;

    public XvfbFixture()
    {
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        var start = new ProcessStartInfo("Xvfb", "-displayfd 1 -screen 0 1024x768x24 -nolisten tcp")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        try
        {
            _process = Process.Start(start);
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return;
        }

        // Xvfb writes the display number it picked, then a newline, to the file descriptor given to -displayfd.
        var line = _process!.StandardOutput.ReadLine();
        if (!string.IsNullOrWhiteSpace(line))
        {
            Display = ":" + line.Trim();
        }
    }

    public string? Display { get; }

    public bool Available => Display is not null;

    public void Dispose()
    {
        if (_process is { HasExited: false })
        {
            _process.Kill();
            _process.WaitForExit(2000);
        }

        _process?.Dispose();
    }
}

internal static unsafe partial class XTest
{
    [LibraryImport("libXtst.so.6", EntryPoint = "XTestFakeKeyEvent")]
    public static partial void FakeKey(nint display, uint keycode, int press, nuint delay);

    [LibraryImport("libXtst.so.6", EntryPoint = "XTestFakeButtonEvent")]
    public static partial void FakeButton(nint display, uint button, int press, nuint delay);

    [LibraryImport("libXtst.so.6", EntryPoint = "XTestFakeMotionEvent")]
    public static partial void FakeMotion(nint display, int screen, int x, int y, nuint delay);
}

internal static class Pump
{
    /// <summary>Pumps until <paramref name="done"/> is true or the time runs out, collecting every event.</summary>
    public static List<PlatformEvent> Until(PlatformApp app, Func<List<PlatformEvent>, bool> done, int timeoutMilliseconds = 3000, List<PlatformEvent>? into = null)
    {
        var events = into ?? [];
        var end = Environment.TickCount64 + timeoutMilliseconds;
        while (true)
        {
            app.PumpEvents(20);
            while (app.TryGetEvent(out var e))
            {
                events.Add(e);
            }

            if (done(events) || Environment.TickCount64 > end)
            {
                return events;
            }
        }
    }

    /// <summary>Pumps for a short fixed time to let in-flight events arrive.</summary>
    public static List<PlatformEvent> For(PlatformApp app, int milliseconds, List<PlatformEvent>? into = null) =>
        Until(app, _ => false, milliseconds, into);
}
