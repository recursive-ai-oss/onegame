using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OneGame.Platform.Tests;

/// <summary>
/// A private Xvfb server for one test class. Tests skip themselves when Xvfb is not installed. The fixture returns only once a client has
/// connected to the display, so a loaded machine cannot hand tests a server that is still starting.
/// </summary>
public sealed unsafe class XvfbFixture : IDisposable
{
    private Process? _process;

    public XvfbFixture()
    {
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        // A start can fail for reasons outside the test (a display number taken between two processes picking it), so try a few times.
        for (var attempt = 0; attempt < 4 && Display is null; attempt++)
        {
            if (!TryStart(out var installed))
            {
                if (!installed)
                {
                    return;
                }

                Thread.Sleep(100 * (attempt + 1));
            }
        }

        if (Display is null)
        {
            throw new InvalidOperationException("Xvfb is installed but no server became ready after 4 attempts.");
        }
    }

    public string? Display { get; private set; }

    public bool Available => Display is not null;

    private static readonly Random Numbers = new(Environment.ProcessId ^ Environment.TickCount);

    // An X server resets itself, closing its sockets and opening them again, whenever its last client disconnects. A test that connects
    // in that moment is refused, so the server is started with -noreset. (This was the "Could not open the X11 display" flake.)
    //
    // The server is given a display number of our own choosing, drawn from a wide range. Letting Xvfb search for a free number
    // (-displayfd) makes concurrent test processes race for the same one: two servers can each think they own it, and clients
    // then reach the wrong one or none.
    private bool TryStart(out bool installed)
    {
        installed = true;
        int number;
        lock (Numbers)
        {
            number = Numbers.Next(1000, 60000);
        }

        var display = ":" + number;
        var start = new ProcessStartInfo("Xvfb", $"{display} -screen 0 1024x768x24 -nolisten tcp -noreset")
        {
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        Process process;
        try
        {
            process = Process.Start(start)!;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            installed = false;
            return false;
        }

        // Nobody reads a server's diagnostics, but a full pipe would stall it.
        var log = Environment.GetEnvironmentVariable("ONEGAME_TEST_XVFB_LOG");
        process.ErrorDataReceived += (_, e) =>
        {
            if (log is { Length: > 0 } && e.Data is not null)
            {
                lock (Numbers)
                {
                    File.AppendAllText(log, $"{DateTime.Now:HH:mm:ss.fff} xvfb {display} {process.Id}: {e.Data}\n");
                }
            }
        };
        process.BeginErrorReadLine();

        // A number somebody else holds makes our server exit at once; the probe may still connect to theirs, so check we are alive after it.
        if (!WaitUntilAccepting(display, process) || !StillRunningAfter(process, 200))
        {
            Stop(process);
            return false;
        }

        _process = process;
        Display = display;
        return true;
    }

    private static bool StillRunningAfter(Process process, int milliseconds)
    {
        Thread.Sleep(milliseconds);
        return !process.HasExited;
    }

    // The number is written once the sockets exist; the proof that the server serves clients is a successful connection.
    private static bool WaitUntilAccepting(string display, Process process)
    {
        var name = System.Text.Encoding.ASCII.GetBytes(display + "\0");
        var end = Environment.TickCount64 + 10_000;
        while (Environment.TickCount64 < end && !process.HasExited)
        {
            nint probe;
            fixed (byte* p = name)
            {
                probe = Xlib.OpenDisplay(p);
            }

            if (probe != 0)
            {
                Xlib.CloseDisplay(probe);
                return true;
            }

            Thread.Sleep(20);
        }

        return false;
    }

    private static void Stop(Process process)
    {
        if (!process.HasExited)
        {
            // SIGTERM lets the server remove its lock file and socket; a killed one leaves them behind.
            _ = Signals.Kill(process.Id, 15);
            if (!process.WaitForExit(3000))
            {
                process.Kill();
                process.WaitForExit(2000);
            }
        }

        process.Dispose();
    }

    public void Dispose()
    {
        if (_process is not null)
        {
            Stop(_process);
            _process = null;
        }
    }
}

internal static partial class Signals
{
    [LibraryImport("libc", EntryPoint = "kill")]
    public static partial int Kill(int pid, int signal);
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
