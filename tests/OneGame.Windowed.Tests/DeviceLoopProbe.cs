using System.Diagnostics;
using OneGame.Host;
using WaterExample;

namespace OneGame.Windowed.Tests;

/// <summary>
/// A probe, not a test: makes a graphics device, renders a 3D scene offscreen and closes the device, several times in one process, and
/// prints how long each step took. There is no window and no swapchain, so it shows what device creation and destruction cost on a driver
/// without involving presentation. Run it as <c>--child-device-loop N</c> with the adapter named in ONEGPU_ADAPTER.
/// </summary>
internal static class DeviceLoopProbe
{
    public static int Run(int runs)
    {
        var clock = Stopwatch.StartNew();
        for (var i = 1; i <= runs; i++)
        {
            var t0 = clock.ElapsedMilliseconds;
            var game = GameInstance.Create(new WaterGame(), new GameOptions { Headless = true, Width = 640, Height = 360, Gpu = GpuMode.Required });
            game.Step(30);
            var frame = game.Capture(640, 360);
            var t1 = clock.ElapsedMilliseconds;
            game.Dispose();
            var t2 = clock.ElapsedMilliseconds;
            Console.WriteLine($"{{\"run\":{i},\"adapter\":\"{frame.Renderer}\",\"createAndCaptureMs\":{t1 - t0},\"disposeMs\":{t2 - t1}}}");
        }

        Console.WriteLine("done");
        return 0;
    }
}
