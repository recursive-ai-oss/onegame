namespace OneGame.Platform.Tests;

internal static class EventText
{
    public static string Describe(PlatformEvent e) =>
        $"{e.Kind} key={e.Key} text={e.Text} button={e.Button} pos={e.X},{e.Y} d={e.DeltaX},{e.DeltaY} mods={e.Modifiers} rep={e.IsRepeat}";

    public static List<string> Input(IEnumerable<PlatformEvent> events) => events
        .Where(e => e.Kind is PlatformEventKind.KeyDown or PlatformEventKind.KeyUp or PlatformEventKind.TextInput or PlatformEventKind.PointerMoved
            or PlatformEventKind.PointerButtonDown or PlatformEventKind.PointerButtonUp or PlatformEventKind.PointerWheel)
        .Select(Describe)
        .ToList();
}

/// <summary>The Wayland backend against a headless Weston: windows, configure, resize, and a CPU-drawn surface the compositor accepts.</summary>
public sealed class WaylandWestonTests : IClassFixture<WestonFixture>, IDisposable
{
    private readonly PlatformApp? _app;
    private readonly Window? _window;

    public WaylandWestonTests(WestonFixture weston)
    {
        if (!weston.Available)
        {
            return;
        }

        _app = PlatformApp.Create(new PlatformOptions { Backend = PlatformBackendKind.Wayland, DisplayName = weston.SocketPath, EnableGamepadDevices = false });
        _window = _app.CreateWindow(new WindowOptions { Title = "wayland test", Width = 320, Height = 200, SoftwareSurface = true });
        Pump.For(_app, 100);
    }

    public void Dispose() => _app?.Dispose();

    private PlatformApp App
    {
        get
        {
            Assert.SkipUnless(_app is not null, "no headless Wayland compositor (set ONEGAME_TEST_WESTON)");
            return _app!;
        }
    }

    [Fact]
    public void WindowIsCreatedConfiguredAndExposesItsSurface()
    {
        _ = App;
        Assert.Equal(PlatformBackendKind.Wayland, _app!.Backend);
        Assert.Equal(NativeWindowKind.Wayland, _window!.NativeHandle.Kind);
        Assert.NotEqual(0, _window.NativeHandle.Display);
        Assert.NotEqual(0, _window.NativeHandle.Window);
        Assert.Equal("wayland test", _window.Title);

        // Weston's kiosk shell sizes every window to the output (1024x768); the window follows what the compositor configured.
        Assert.Equal((1024, 768), (_window.Width, _window.Height));
        Assert.Equal(1f, _window.Scale);
    }

    [Fact]
    public void ResizeIsReportedAndTheNewSurfaceIsAcceptedByTheCompositor()
    {
        var app = App;
        _window!.RequestSize(400, 300);
        _window.FillSolid(0xFF336699);
        var events = Pump.For(app, 300);

        var resized = events.Last(e => e.Kind == PlatformEventKind.WindowResized);
        Assert.Equal((400, 300), (resized.Width, resized.Height));
        Assert.Equal((400, 300), (_window.Width, _window.Height));

        // A protocol error (bad buffer size, bad pool) would have closed the connection and thrown from the pump.
        Pump.For(app, 100);
        Assert.False(_window.IsClosed);
    }

    [Fact]
    public void TitleFullscreenAndMinimizeRequestsAreAccepted()
    {
        var app = App;
        _window!.SetTitle("renamed");
        _window.SetFullscreen(true);
        _window.SetFullscreen(false);
        _window.Minimize();
        Pump.For(app, 100);
        Assert.Equal("renamed", _window.Title);
        Assert.True(_window.IsMinimized);
    }
}

/// <summary>The Wayland backend against a scripted compositor that sends exact input events on the wire.</summary>
public sealed class WaylandInputTests : IDisposable
{
    private readonly FakeCompositor? _server;
    private readonly PlatformApp? _app;
    private readonly Window? _window;

    public WaylandInputTests()
    {
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        try
        {
            _server = new FakeCompositor(Path.Combine(Path.GetTempPath(), $"onegame-fake-{Environment.ProcessId}-{Guid.NewGuid():N}"));
        }
        catch (DllNotFoundException)
        {
            return;
        }

        _app = PlatformApp.Create(new PlatformOptions { Backend = PlatformBackendKind.Wayland, DisplayName = _server.SocketPath, EnableGamepadDevices = false });
        _window = _app.CreateWindow(new WindowOptions { Title = "wayland input", Width = 320, Height = 200 });
        Pump.For(_app, 100);
        Assert.True(_server.WaitForWindow(2000));
    }

    public void Dispose()
    {
        _app?.Dispose();
        _server?.Dispose();
    }

    private (PlatformApp App, FakeCompositor Server) Env()
    {
        Assert.SkipUnless(_app is not null, "libwayland-server is not installed");
        return (_app!, _server!);
    }

    private static void Script(FakeCompositor s)
    {
        s.Run(() =>
        {
            s.KeyboardEnter();
            s.PointerEnter(10, 20);
            s.PointerMotion(50, 60);
            s.PointerButton(0x110, true);
            s.PointerButton(0x110, false);
            s.PointerAxisDiscrete(vertical: true, -1);
            s.Modifiers(0);
            s.Key(30, true);
            s.Key(30, false);
            s.Modifiers(1);
            s.Key(30, true);
            s.Key(30, false);
            s.Modifiers(0);
            s.Key(105, true);
            s.Key(105, false);
        });
    }

    [Fact]
    public void WindowGetsFocusAndExposesItsWaylandHandles()
    {
        var (app, server) = Env();
        server.Run(server.KeyboardEnter);
        Pump.Until(app, _ => _window!.IsFocused);
        Assert.True(_window!.IsFocused);
        Assert.Equal(NativeWindowKind.Wayland, _window.NativeHandle.Kind);
    }

    [Fact]
    public void CompositorInputArrivesAsKeyTextPointerAndWheelEvents()
    {
        var (app, server) = Env();
        Script(server);
        var events = Pump.Until(app, ev => ev.Count(e => e.Kind == PlatformEventKind.KeyUp) >= 3);

        Assert.Contains(events, e => e is { Kind: PlatformEventKind.PointerMoved, X: 50, Y: 60 });
        Assert.Contains(events, e => e is { Kind: PlatformEventKind.PointerButtonDown, Button: MouseButton.Left, X: 50, Y: 60 });
        Assert.Contains(events, e => e is { Kind: PlatformEventKind.PointerButtonUp, Button: MouseButton.Left });
        Assert.Contains(events, e => e is { Kind: PlatformEventKind.PointerWheel, DeltaY: 1f });
        Assert.Contains(events, e => e is { Kind: PlatformEventKind.KeyDown, Key: Key.A, Modifiers: KeyModifiers.None });
        Assert.Contains(events, e => e is { Kind: PlatformEventKind.TextInput, Text: "a" });
        Assert.Contains(events, e => e is { Kind: PlatformEventKind.KeyDown, Key: Key.A, Modifiers: KeyModifiers.Shift });
        Assert.Contains(events, e => e is { Kind: PlatformEventKind.TextInput, Text: "A" });
        Assert.Contains(events, e => e is { Kind: PlatformEventKind.KeyDown, Key: Key.ArrowLeft });
        Assert.DoesNotContain(events, e => e.Kind == PlatformEventKind.TextInput && e.Text is not ("a" or "A"));
        Assert.True(_window!.IsFocused);
        Assert.False(app.Input.IsKeyDown(Key.A));
    }

    [Fact]
    public void InjectedInputProducesTheSameEventsAsCompositorInput()
    {
        var (app, server) = Env();
        var id = _window!.Id;

        server.Run(() =>
        {
            server.KeyboardEnter();
            server.PointerEnter(5, 5);
            server.PointerMotion(70, 80);
            server.PointerButton(0x111, true);
            server.PointerButton(0x111, false);
            server.Key(30, true);
            server.Key(30, false);
        });
        var real = EventText.Input(Pump.Until(app, ev => ev.Any(e => e.Kind == PlatformEventKind.KeyUp)));

        var inj = app.Injector;
        inj.PointerMove(id, 5, 5);
        inj.PointerMove(id, 70, 80);
        inj.PointerButton(id, MouseButton.Right, true);
        inj.PointerButton(id, MouseButton.Right, false);
        inj.Key(id, Key.A, true);
        inj.Text(id, "a");
        inj.Key(id, Key.A, false);
        var events = new List<PlatformEvent>();
        while (app.TryGetEvent(out var e))
        {
            events.Add(e);
        }

        var synthetic = EventText.Input(events);

        // The compositor path also reports the pointer entering at (5, 5); drop that first move from the real side to compare.
        var realWithoutEnter = real.Where(l => !l.Contains("pos=5,5", StringComparison.Ordinal)).ToList();
        var syntheticWithoutEnter = synthetic.Where(l => !l.Contains("pos=5,5", StringComparison.Ordinal)).ToList();
        Assert.Equal(realWithoutEnter.OrderBy(x => x, StringComparer.Ordinal), syntheticWithoutEnter.OrderBy(x => x, StringComparer.Ordinal));
    }

    [Fact]
    public void CloseRequestFromTheCompositorIsReported()
    {
        var (app, server) = Env();
        server.Run(server.CloseToplevel);
        var events = Pump.Until(app, ev => ev.Any(e => e.Kind == PlatformEventKind.WindowCloseRequested));
        Assert.Contains(events, e => e.Kind == PlatformEventKind.WindowCloseRequested && e.WindowId == _window!.Id);
    }

    [Fact]
    public void CursorLockIsRefusedWhenTheCompositorLacksPointerConstraints()
    {
        var (app, server) = Env();
        server.Run(() => server.PointerEnter(1, 1));
        Pump.For(app, 100);
        _window!.SetCursorHidden(true);
        Assert.True(_window.CursorHidden);
        _window.SetCursorLocked(true);
        Assert.False(_window.CursorLocked);
        Pump.For(app, 50);
        Assert.False(_window.IsClosed);
    }
}
