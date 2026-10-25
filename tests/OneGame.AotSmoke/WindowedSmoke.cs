using OneGame.Control;
using OneGame.Gpu;
using OneGame.Host;
using OneGame.Platform;

namespace OneGame.AotSmoke;

/// <summary>
/// The window-system paths, so the native publish compiles and links them: the clipboard and input-method events always (headless), and
/// with ONEGAME_SMOKE_DISPLAY set (an X display name) a real window that presents frames and a clipboard shared with a second connection.
/// </summary>
internal static class WindowedSmoke
{
    public static int Run()
    {
        using (var app = PlatformApp.Create(new PlatformOptions { Backend = PlatformBackendKind.Headless, EnableGamepadDevices = false }))
        {
            var window = app.CreateWindow();
            app.Clipboard.SetText("smoke clip");
            app.Injector.Composition(window.Id, "ni", 0, 2);
            window.SetTextInput(true, 1, 2, 3, 4);
            if (app.Clipboard.GetText() != "smoke clip" || !app.TryGetEvent(out var e) || e.Kind != PlatformEventKind.TextComposition || e.Text != "ni" || !window.TextInputActive)
            {
                Console.Error.WriteLine("clipboard/composition smoke failed");
                return 1;
            }
        }

        if (Environment.GetEnvironmentVariable("ONEGAME_SMOKE_DISPLAY") is not { Length: > 0 } display)
        {
            Console.WriteLine("ok: clipboard and composition (headless); set ONEGAME_SMOKE_DISPLAY to run the window paths");
            return 0;
        }

        var options = new PlatformOptions { Backend = PlatformBackendKind.X11, DisplayName = display, EnableGamepadDevices = false };
        using var done = new ManualResetEventSlim();
        using var ready = new ManualResetEventSlim();
        var owner = new Thread(() =>
        {
            using var other = PlatformApp.Create(options);
            other.Clipboard.SetText("from the other connection");
            ready.Set();
            while (!done.IsSet)
            {
                other.PumpEvents(5);
            }
        });
        owner.Start();
        ready.Wait();
        string? pasted;
        using (var app = PlatformApp.Create(options))
        {
            pasted = app.Clipboard.GetText();
        }

        done.Set();
        owner.Join();
        if (pasted != "from the other connection")
        {
            Console.Error.WriteLine($"x11 clipboard smoke failed: '{pasted}'");
            return 1;
        }

        using var game = GameInstance.Create(new SmokeGame(), new GameOptions { Width = 320, Height = 200, Gpu = GpuMode.Required, Backend = PlatformBackendKind.X11, DisplayName = display });
        for (var i = 0; i < 20 && game.PresentedFrames < 5; i++)
        {
            game.Step();
            Thread.Sleep(5);
        }

        var frame = game.CapturePresented();
        var surface = new ControlSurface(game, Path.GetTempPath());
        surface.Call("input_composition", """{"text":"ka"}""");
        if (game.PresentedFrames < 5 || frame is null || frame.Width != game.Window.Width)
        {
            Console.Error.WriteLine($"windowed smoke failed: presented {game.PresentedFrames}");
            return 1;
        }

        Console.WriteLine($"ok: window on {display}; {game.PresentedFrames} frames presented by {frame.Renderer}; clipboard shared with a second connection");
        return 0;
    }
}
