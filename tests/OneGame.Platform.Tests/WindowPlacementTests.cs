namespace OneGame.Platform.Tests;

public class WindowPlacementTests
{
    private static readonly ScreenRect Primary = new(0, 0, 1920, 1080);
    private static readonly ScreenRect Second = new(1920, 0, 1280, 1024);

    [Fact]
    public void APlacementOnAConnectedScreenIsKeptAsItIs()
    {
        var p = new WindowPlacement(300, 200, 1280, 720, WindowState.Maximized);
        Assert.Equal(p, p.ClampTo([Primary, Second]));
        Assert.Equal(new WindowPlacement(2000, 100, 800, 600), new WindowPlacement(2000, 100, 800, 600).ClampTo([Primary, Second]));
    }

    [Fact]
    public void APartlyOffScreenWindowStaysWhenEnoughOfItCanBeGrabbed()
    {
        var p = new WindowPlacement(-700, 50, 800, 600);
        Assert.Equal(p, p.ClampTo([Primary]));
    }

    [Fact]
    public void AWindowOnAnUnpluggedMonitorMovesOntoTheRemainingOne()
    {
        var p = new WindowPlacement(2300, 200, 800, 600);
        var clamped = p.ClampTo([Primary]);
        Assert.Equal(new WindowPlacement(1120, 200, 800, 600), clamped);
    }

    [Fact]
    public void AWindowFarOffInAnyDirectionComesBackFullyVisible()
    {
        foreach (var (x, y) in new[] { (-9000, -9000), (9000, 9000), (-9000, 500), (500, 9000) })
        {
            var c = new WindowPlacement(x, y, 800, 600).ClampTo([Primary]);
            Assert.True(c.X >= 0 && c.Y >= 0 && c.X + c.Width <= 1920 && c.Y + c.Height <= 1080, c.ToString());
        }
    }

    [Fact]
    public void AWindowLargerThanTheScreenIsShrunkToTheWorkArea()
    {
        var c = new WindowPlacement(0, 0, 4000, 3000, WindowState.Normal).ClampTo([new ScreenRect(0, 40, 1366, 728)]);
        Assert.Equal(new WindowPlacement(0, 40, 1366, 728), c);
    }

    [Fact]
    public void AWindowHangingOffTheSecondScreenIsKeptAndOneAboveEveryScreenGoesToThePrimary()
    {
        var onSecond = new WindowPlacement(3000, 100, 800, 600);
        Assert.Equal(onSecond, onSecond.ClampTo([Primary, Second]));
        Assert.Equal(new WindowPlacement(1120, 0, 800, 600), new WindowPlacement(1900, -3000, 800, 600).ClampTo([Primary, Second]));
    }

    [Fact]
    public void NoScreensLeavesPositionAlone()
    {
        var p = new WindowPlacement(-5000, 7000, 800, 600);
        Assert.Equal(p, p.ClampTo([]));
    }

    [Fact]
    public void WithoutAPositionOnlyTheSizeIsFitted()
    {
        var c = WindowPlacement.SizeOnly(3000, 500, WindowState.Maximized).ClampTo([Primary]);
        Assert.Equal(WindowPlacement.SizeOnly(1920, 500, WindowState.Maximized), c);
    }

    [Theory]
    [InlineData("100,80,1280x720,maximized", 100, 80, 1280, 720, WindowState.Maximized, true)]
    [InlineData("-1920,-40,640x480,normal", -1920, -40, 640, 480, WindowState.Normal, true)]
    [InlineData("-,-,800x600,fullscreen", 0, 0, 800, 600, WindowState.Fullscreen, false)]
    public void TextRoundTrips(string text, int x, int y, int w, int h, WindowState state, bool hasPosition)
    {
        Assert.True(WindowPlacement.TryParse(text, out var p));
        Assert.Equal(new WindowPlacement(x, y, w, h, state, hasPosition), p);
        Assert.Equal(text, p.Format());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1,2,3x4")]
    [InlineData("1,2,0x4,normal")]
    [InlineData("1,2,70000x4,normal")]
    [InlineData("a,2,10x4,normal")]
    [InlineData("1,2,10x4,sideways")]
    [InlineData("1,2,10x4,7")]
    [InlineData("99999999999,2,10x4,normal")]
    [InlineData("1,2,-10x4,normal")]
    public void BadTextIsRefused(string? text) => Assert.False(WindowPlacement.TryParse(text, out _));

    private static PlatformApp NewApp() => PlatformApp.Create(new PlatformOptions { Backend = PlatformBackendKind.Headless, EnableGamepadDevices = false });

    [Fact]
    public void AHeadlessWindowKeepsItsNormalRectangleWhileMaximizedAndRestoresIt()
    {
        using var app = NewApp();
        var first = app.CreateWindow(new WindowOptions { Placement = new WindowPlacement(120, 90, 800, 600) });
        Assert.Equal(new WindowPlacement(120, 90, 800, 600), first.Placement);

        first.Maximize();
        first.MoveTo(0, 0); // a maximized window moves with the screen, not as a new normal position
        var saved = first.Placement;
        Assert.Equal(WindowState.Maximized, saved.State);
        Assert.Equal((800, 600), (saved.Width, saved.Height));

        first.Close();
        var second = app.CreateWindow(new WindowOptions { Placement = saved });
        Assert.True(second.IsMaximized);
        Assert.Equal(saved.Width, second.Placement.Width);
        second.Restore();
        Assert.Equal(WindowState.Normal, second.State);
    }

    [Fact]
    public void ApplyingAPlacementMovesResizesAndMaximizes()
    {
        using var app = NewApp();
        var w = app.CreateWindow(new WindowOptions { Width = 640, Height = 480 });
        w.ApplyPlacement(new WindowPlacement(40, 30, 900, 500, WindowState.Maximized));
        Assert.Equal(new WindowPlacement(40, 30, 900, 500, WindowState.Maximized), w.Placement);
        w.ApplyPlacement(new WindowPlacement(60, 70, 700, 400));
        Assert.Equal(new WindowPlacement(60, 70, 700, 400), w.Placement);
        Assert.Equal((700, 400), (w.Width, w.Height));
        w.ApplyPlacement(new WindowPlacement(0, 0, 700, 400, WindowState.Fullscreen));
        Assert.True(w.IsFullscreen);
        Assert.False(w.IsMaximized);
    }
}
