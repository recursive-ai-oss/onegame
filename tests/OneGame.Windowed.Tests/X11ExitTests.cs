using System.Diagnostics;
using OneGame.Platform;
using OneGame.Platform.Tests;

namespace OneGame.Windowed.Tests;

/// <summary>
/// A game that opens an X11 window and closes it must leave the process with exit code 0. The graphics driver registers a hook on the
/// display connection, so closing the display after the driver is unloaded crashed on NVIDIA at exit. The check runs in a child process,
/// because the crash happens while the process shuts down; it covers whatever driver is active (set ONEGAME_WINDOWED_ADAPTER for a real GPU).
/// </summary>
public sealed class X11ExitTests : IClassFixture<XvfbFixture>
{
    private readonly XvfbFixture _xvfb;

    public X11ExitTests(XvfbFixture xvfb) => _xvfb = xvfb;

    /// <summary>The child half: open a window, then either dispose it ('quit') or have another client ask it to close first ('close').</summary>
    internal static unsafe int RunChild(string mode, string display)
    {
        using (var s = new Session(PlatformBackendKind.X11, display))
        {
            s.StepUntil(() => s.Game.PresentedFrames >= 5);
            if (mode == "close")
            {
                // What a window manager's close button does: a WM_DELETE_WINDOW client message from another connection.
                var name = System.Text.Encoding.ASCII.GetBytes(display + "\0");
                nint other;
                fixed (byte* p = name)
                {
                    other = Xlib.OpenDisplay(p);
                }

                nuint protocols, delete;
                fixed (byte* a = "WM_PROTOCOLS\0"u8)
                {
                    protocols = Xlib.InternAtom(other, a, 0);
                }

                fixed (byte* a = "WM_DELETE_WINDOW\0"u8)
                {
                    delete = Xlib.InternAtom(other, a, 0);
                }

                var window = (nuint)s.Game.Window.NativeHandle.Window;
                Xlib.XEvent ev = default;
                ev.Type = Xlib.ClientMessage;
                *(int*)((byte*)&ev + 16) = 1;
                *(nint*)((byte*)&ev + 24) = other;
                *(nuint*)((byte*)&ev + 32) = window;
                *(nuint*)((byte*)&ev + 40) = protocols;
                *(int*)((byte*)&ev + 48) = 32;
                *(nuint*)((byte*)&ev + 56) = delete;
                Xlib.SendEvent(other, window, 0, 0, &ev);
                Xlib.Flush(other);
                s.StepUntil(() => s.Game.QuitRequested);
                Xlib.CloseDisplay(other);
                if (!s.Game.QuitRequested)
                {
                    Console.Error.WriteLine("the window did not report the close request");
                    return 2;
                }
            }
        }

        Console.WriteLine("closed");
        return 0;
    }

    [Theory]
    [InlineData("quit")]
    [InlineData("close")]
    public async Task TheProcessExitsCleanlyAfterItsWindowCloses(string mode)
    {
        // The crash needed a real X server with the vendor driver; ONEGAME_EXIT_TEST_DISPLAY names one (Xvfb otherwise).
        var display = Environment.GetEnvironmentVariable("ONEGAME_EXIT_TEST_DISPLAY") is { Length: > 0 } named ? named : _xvfb.Display;
        Assert.SkipUnless(OperatingSystem.IsLinux() && display is not null, "Xvfb is not installed");
        var start = new ProcessStartInfo(Environment.ProcessPath!)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        start.ArgumentList.Add("--child-window");
        start.ArgumentList.Add(mode);
        start.ArgumentList.Add(display!);
        start.Environment.Remove("WAYLAND_DISPLAY");
        using var child = Process.Start(start)!;
        var cancel = TestContext.Current.CancellationToken;
        var output = child.StandardOutput.ReadToEndAsync(cancel);
        var error = child.StandardError.ReadToEndAsync(cancel);
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancel);
        limit.CancelAfter(TimeSpan.FromSeconds(120));
        try
        {
            await child.WaitForExitAsync(limit.Token);
        }
        catch (OperationCanceledException)
        {
            child.Kill(true);
            Assert.Fail("the child did not exit");
        }

        var text = await output;
        var errors = await error;

        Assert.True(child.ExitCode == 0, $"exit code {child.ExitCode} (139 = SIGSEGV)\n{text}\n{errors}");
        Assert.Contains("closed", text, StringComparison.Ordinal);
    }
}
