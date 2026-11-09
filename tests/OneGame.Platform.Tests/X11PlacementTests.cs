namespace OneGame.Platform.Tests;

/// <summary>Window placement on X11: with a (test) window manager that maximizes, and on a bare server that does not.</summary>
public sealed class X11PlacementTests : IClassFixture<XvfbFixture>, IDisposable
{
    private readonly XvfbFixture _xvfb;
    private PlatformApp? _app;
    private FakeWindowManager? _wm;

    public X11PlacementTests(XvfbFixture xvfb) => _xvfb = xvfb;

    public void Dispose()
    {
        _app?.Dispose();
        _wm?.Dispose();
    }

    private PlatformApp Start(bool windowManager)
    {
        Assert.SkipUnless(_xvfb.Available, "Xvfb is not installed");
        if (windowManager)
        {
            _wm = new FakeWindowManager(_xvfb.Display!);
        }

        _app = PlatformApp.Create(new PlatformOptions { Backend = PlatformBackendKind.X11, DisplayName = _xvfb.Display, EnableGamepadDevices = false });
        return _app;
    }

    [Fact]
    public void MaximizeReadCloseCreateFromThePlacementAndItIsMaximizedAtTheSamePosition()
    {
        var app = Start(windowManager: true);
        var first = app.CreateWindow(new WindowOptions { Title = "placed", Placement = new WindowPlacement(120, 90, 400, 300) });
        Pump.Until(app, _ => first.IsFocused || first.X == 120);
        Assert.Equal(new WindowPlacement(120, 90, 400, 300), first.Placement);

        first.Maximize();
        Pump.Until(app, _ => first.IsMaximized && first.Width == 1024);
        Assert.True(first.IsMaximized);
        Assert.Equal((1024, 768), (first.Width, first.Height));

        // The saved value still describes the window to come back to, not the maximized size.
        var saved = first.Placement;
        Assert.Equal(new WindowPlacement(120, 90, 400, 300, WindowState.Maximized), saved);
        Assert.True(WindowPlacement.TryParse(saved.Format(), out var reloaded));
        first.Close();

        var second = app.CreateWindow(new WindowOptions { Title = "placed again", Placement = reloaded });
        Pump.Until(app, _ => second.IsMaximized && second.Width == 1024);
        Assert.True(second.IsMaximized);
        Assert.Equal(new WindowPlacement(120, 90, 400, 300, WindowState.Maximized), second.Placement);

        second.Restore();
        Pump.Until(app, _ => !second.IsMaximized && second.Width == 400);
        Assert.Equal(WindowState.Normal, second.State);
        Assert.Equal(new WindowPlacement(120, 90, 400, 300), second.Placement);
        Assert.Equal((120, 90), (second.X, second.Y));
    }

    [Fact]
    public void ApplyingAPlacementToALiveWindowMovesResizesAndMaximizesIt()
    {
        var app = Start(windowManager: true);
        var w = app.CreateWindow(new WindowOptions { Width = 320, Height = 200 });
        Pump.Until(app, _ => w.IsFocused);

        w.ApplyPlacement(new WindowPlacement(200, 150, 500, 350));
        Pump.Until(app, _ => w.X == 200 && w.Width == 500);
        Assert.Equal(new WindowPlacement(200, 150, 500, 350), w.Placement);

        w.ApplyPlacement(new WindowPlacement(300, 100, 600, 400, WindowState.Maximized));
        Pump.Until(app, _ => w.IsMaximized && w.Width == 1024);
        Assert.Equal(new WindowPlacement(300, 100, 600, 400, WindowState.Maximized), w.Placement);

        w.ApplyPlacement(new WindowPlacement(300, 100, 600, 400));
        Pump.Until(app, _ => !w.IsMaximized && w.Width == 600);
        Assert.Equal(new WindowPlacement(300, 100, 600, 400), w.Placement);
    }

    [Fact]
    public void WithoutAWindowManagerMovingWorksAndMaximizingChangesNothing()
    {
        var app = Start(windowManager: false);
        var w = app.CreateWindow(new WindowOptions { Placement = new WindowPlacement(50, 60, 300, 200) });
        Pump.Until(app, _ => w.X == 50 && w.Y == 60);
        Assert.Equal((50, 60), (w.X, w.Y));

        w.MoveTo(200, 150);
        Pump.Until(app, _ => w.X == 200);
        Assert.Equal(new WindowPlacement(200, 150, 300, 200), w.Placement);

        w.Maximize();
        Pump.For(app, 200);
        Assert.False(w.IsMaximized);
        Assert.Equal((300, 200), (w.Width, w.Height));
    }

    [Fact]
    public void APlacementOffTheScreenIsMovedOntoIt()
    {
        var app = Start(windowManager: false);
        Assert.Equal([new ScreenRect(0, 0, 1024, 768)], app.GetWorkAreas());

        var w = app.CreateWindow(new WindowOptions { Placement = new WindowPlacement(5000, 4000, 400, 300) });
        Pump.Until(app, _ => w.X != 0 || w.Y != 0);
        Assert.Equal(new WindowPlacement(624, 468, 400, 300), w.Placement);
        Assert.Equal((624, 468), (w.X, w.Y));
    }
}
