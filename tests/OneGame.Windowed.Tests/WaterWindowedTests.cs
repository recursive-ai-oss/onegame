using OneGame.Host;
using OneGame.Platform;
using OneGame.Platform.Tests;
using WaterExample;

namespace OneGame.Windowed.Tests;

/// <summary>The water example in a real X11 window: the planar reflection pass runs on the window's device, the presented frame equals the capture, and validation stays silent.</summary>
public sealed class WaterWindowedTests : IClassFixture<XvfbFixture>
{
    private readonly XvfbFixture _xvfb;

    public WaterWindowedTests(XvfbFixture xvfb) => _xvfb = xvfb;

    [Fact]
    public void TheBuoyIsMirroredInThePresentedFrame() => MainThread.Run(() =>
    {
        Assert.SkipUnless(_xvfb.Available, "Xvfb is not installed");
        using var s = new Session(PlatformBackendKind.X11, _xvfb.Display!, game: new WaterGame());
        s.StepUntil(() => s.Game.PresentedFrames >= 40);
        var shot = s.Game.Capture(s.Game.Window.Width, s.Game.Window.Height, s.Game.UiScale);
        var shown = s.Game.CapturePresented()!;
        Session.Save("water-capture", shot);
        Session.Save("water-presented", shown);

        // Below the horizon only the water shows (the banks are green, the tower white), so a pixel redder than green there is the buoy's image.
        foreach (var f in new[] { shot, shown })
        {
            var red = 0;
            for (var y = f.Height * 6 / 10; y < f.Height; y++)
            {
                for (var x = 0; x < f.Width; x++)
                {
                    var i = ((y * f.Width) + x) * 4;
                    red += f.Rgba[i] > f.Rgba[i + 1] + 8 ? 1 : 0;
                }
            }

            Assert.True(red > 300, $"only {red} reddish pixels below the horizon");
        }

        var worst = 0;
        for (var y = 0; y < shot.Height; y++)
        {
            for (var x = 0; x < shot.Width; x++)
            {
                if (x >= 280 && y < 150)
                {
                    continue;   // the debug panel's corner changes from frame to frame
                }

                for (var c = 0; c < 4; c++)
                {
                    var i = (((y * shot.Width) + x) * 4) + c;
                    worst = Math.Max(worst, Math.Abs(shot.Rgba[i] - shown.Rgba[i]));
                }
            }
        }

        Assert.True(worst <= 2, $"capture differs from the presented frame by up to {worst}");
        Assert.Empty(s.Validation());
    });
}
