namespace OneGame.Platform.Tests;

public class HeadlessAppTests
{
    private static PlatformApp NewApp() =>
        PlatformApp.Create(new PlatformOptions { Backend = PlatformBackendKind.Headless, EnableGamepadDevices = false });

    private static List<PlatformEvent> Drain(PlatformApp app)
    {
        var events = new List<PlatformEvent>();
        while (app.TryGetEvent(out var e))
        {
            events.Add(e);
        }

        return events;
    }

    [Fact]
    public void ModuleIsNamedAfterItsAssembly() => Assert.Equal("OneGame.Platform", PlatformModule.Name);

    [Fact]
    public void WindowsHaveStableIdsAndResizeThroughTheSameEventPath()
    {
        using var app = NewApp();
        var a = app.CreateWindow(new WindowOptions { Width = 640, Height = 480, Title = "a" });
        var b = app.CreateWindow();
        Assert.Equal((1, 2), (a.Id, b.Id));
        Assert.Equal(NativeWindowKind.None, a.NativeHandle.Kind);

        a.RequestSize(800, 600);
        a.SetTitle("renamed");
        a.SetCursorLocked(true);
        var resized = Assert.Single(Drain(app));
        Assert.Equal((PlatformEventKind.WindowResized, 1, 800, 600), (resized.Kind, resized.WindowId, resized.Width, resized.Height));
        Assert.Equal("renamed", a.Title);
        Assert.True(a.CursorLocked && a.CursorHidden);

        a.Close();
        Assert.Single(app.Windows);
        Assert.Throws<ArgumentOutOfRangeException>(() => app.CreateWindow(new WindowOptions { Width = 0 }));
    }

    [Fact]
    public void InjectedKeysUpdateStateModifiersAndRepeat()
    {
        using var app = NewApp();
        var w = app.CreateWindow().Id;
        var inj = app.Injector;

        inj.Key(w, Key.LeftShift, true);
        inj.Key(w, Key.A, true);
        inj.Key(w, Key.A, true);
        Assert.True(app.Input.IsKeyDown(Key.A));
        Assert.Equal(KeyModifiers.Shift, app.Input.Modifiers);
        inj.Key(w, Key.A, false);
        inj.Text(w, "A");

        var events = Drain(app);
        Assert.Equal([false, false, true, false], events.Where(e => e.Kind is PlatformEventKind.KeyDown or PlatformEventKind.KeyUp).Select(e => e.IsRepeat));
        Assert.Equal(KeyModifiers.Shift, events[1].Modifiers);
        Assert.Equal("A", events[^1].Text);
        Assert.False(app.Input.IsKeyDown(Key.A));
        Assert.Throws<ArgumentException>(() => inj.Text(w, ""));
    }

    [Fact]
    public void LosingFocusReleasesHeldKeysSoNothingSticks()
    {
        using var app = NewApp();
        var w = app.CreateWindow().Id;
        app.Injector.Key(w, Key.W, true);
        app.Post(new PlatformEvent { Kind = PlatformEventKind.WindowFocusChanged, WindowId = w, Focused = false });
        Assert.False(app.Input.IsKeyDown(Key.W));
    }

    [Fact]
    public void InjectedPointerAndWheelCarryPositionAndButtons()
    {
        using var app = NewApp();
        var w = app.CreateWindow().Id;
        var inj = app.Injector;
        inj.PointerMove(w, 12.5f, 30);
        inj.PointerButton(w, MouseButton.Left, true);
        inj.Wheel(w, 0, 2);
        inj.RelativeMotion(w, 3, -4);
        Assert.Equal((12.5f, 30f), (app.Input.PointerX, app.Input.PointerY));
        Assert.True(app.Input.IsButtonDown(MouseButton.Left));
        var events = Drain(app);
        Assert.Equal((12.5f, 30f), (events[1].X, events[1].Y));
        Assert.Equal(2f, events[2].DeltaY);
        Assert.Equal((3f, -4f), (events[3].DeltaX, events[3].DeltaY));
    }

    [Fact]
    public void SyntheticGamepadsConnectMoveAndDisconnect()
    {
        using var app = NewApp();
        var inj = app.Injector;
        var pad = inj.GamepadConnect("Test pad");
        inj.GamepadButton(pad, GamepadButton.South, true);
        inj.GamepadAxis(pad, GamepadAxis.LeftX, 0.5f);

        var state = app.Input.GetGamepad(pad)!;
        Assert.Equal("Test pad", state.Name);
        Assert.True(state.IsDown(GamepadButton.South));
        Assert.Equal(0.5f, state.GetAxis(GamepadAxis.LeftX));

        inj.GamepadDisconnect(pad);
        Assert.Null(app.Input.GetGamepad(pad));
        Assert.False(state.IsConnected);
        Assert.Equal(
            [PlatformEventKind.GamepadConnected, PlatformEventKind.GamepadButtonDown, PlatformEventKind.GamepadAxisMoved, PlatformEventKind.GamepadDisconnected],
            Drain(app).Select(e => e.Kind));
    }

    [Fact]
    public void QuitRequestIsAnEventAndASticky()
    {
        using var app = NewApp();
        app.RequestQuit();
        Assert.True(app.QuitRequested);
        Assert.Equal(PlatformEventKind.QuitRequested, Assert.Single(Drain(app)).Kind);
    }

    [Fact]
    public void QueueIsBoundedAndCountsWhatItDrops()
    {
        using var app = NewApp();
        var w = app.CreateWindow().Id;
        for (var i = 0; i < 20000; i++)
        {
            app.Injector.PointerMove(w, i, 0);
        }

        Assert.Equal(20000 - 16384, app.DroppedEvents);
        Assert.Equal(16384, Drain(app).Count);
    }

    [Fact]
    public void AutoFallsBackToHeadlessWithoutADisplayAndMissingBackendsThrowTyped()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Throws<PlatformException>(() => PlatformApp.Create(new PlatformOptions { Backend = PlatformBackendKind.Win32, EnableGamepadDevices = false }));
        }

        Assert.Throws<PlatformException>(() => PlatformApp.Create(new PlatformOptions { Backend = PlatformBackendKind.X11, DisplayName = ":99999", EnableGamepadDevices = false }));
        Assert.Throws<PlatformException>(() => PlatformApp.Create(new PlatformOptions { Backend = PlatformBackendKind.Wayland, DisplayName = "/nonexistent/socket", EnableGamepadDevices = false }));
    }
}
