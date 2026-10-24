using System.Text;

namespace OneGame.Platform.Tests;

public sealed unsafe class X11Tests : IClassFixture<XvfbFixture>, IDisposable
{
    private const uint KeycodeA = 30 + 8, KeycodeLeftShift = 42 + 8, KeycodeLeft = 105 + 8;

    private readonly PlatformApp? _app;
    private readonly nint _xtest;
    private readonly Window? _window;

    public X11Tests(XvfbFixture xvfb)
    {
        if (!xvfb.Available)
        {
            return;
        }

        _app = PlatformApp.Create(new PlatformOptions { Backend = PlatformBackendKind.X11, DisplayName = xvfb.Display, EnableGamepadDevices = false });
        _window = _app.CreateWindow(new WindowOptions { Title = "x11 test", Width = 320, Height = 200 });
        var name = Encoding.ASCII.GetBytes(xvfb.Display + "\0");
        fixed (byte* p = name)
        {
            _xtest = Xlib.OpenDisplay(p);
        }


        // Wait until the window is mapped and has focus so injected input has somewhere to go.
        Pump.Until(_app, _ => _window.IsFocused);
    }

    public void Dispose()
    {
        _app?.Dispose();
        if (_xtest != 0)
        {
            Xlib.CloseDisplay(_xtest);
        }
    }

    private PlatformApp App
    {
        get
        {
            Assert.SkipUnless(_app is not null, "Xvfb is not installed");
            return _app!;
        }
    }

    private void Fake(Action action)
    {
        action();
        Xlib.Sync(_xtest, 0);
    }

    private static string Describe(PlatformEvent e) =>
        $"{e.Kind} key={e.Key} text={e.Text} button={e.Button} pos={e.X},{e.Y} d={e.DeltaX},{e.DeltaY} mods={e.Modifiers} rep={e.IsRepeat}";

    private static List<string> Input(IEnumerable<PlatformEvent> events) => events
        .Where(e => e.Kind is PlatformEventKind.KeyDown or PlatformEventKind.KeyUp or PlatformEventKind.TextInput or PlatformEventKind.PointerMoved
            or PlatformEventKind.PointerButtonDown or PlatformEventKind.PointerButtonUp or PlatformEventKind.PointerWheel)
        .Select(Describe)
        .ToList();

    [Fact]
    public void WindowIsCreatedMappedAndFocused()
    {
        var app = App;
        Assert.Equal(PlatformBackendKind.X11, app.Backend);
        Assert.Equal(NativeWindowKind.X11, _window!.NativeHandle.Kind);
        Assert.NotEqual(0, _window.NativeHandle.Display);
        Assert.NotEqual(0, _window.NativeHandle.Window);
        Assert.Equal(320, _window.Width);
        Assert.Equal(200, _window.Height);
        Assert.True(_window.IsFocused);
        Assert.Equal("x11 test", _window.Title);
    }

    [Fact]
    public void ResizeIsReportedWithTheNewPixelSize()
    {
        var app = App;
        _window!.RequestSize(400, 300);
        var events = Pump.Until(app, ev => ev.Any(e => e.Kind == PlatformEventKind.WindowResized && e.Width == 400));

        var resized = events.Last(e => e.Kind == PlatformEventKind.WindowResized);
        Assert.Equal((400, 300), (resized.Width, resized.Height));
        Assert.Equal(_window.Id, resized.WindowId);
        Assert.Equal((400, 300), (_window.Width, _window.Height));
    }

    [Fact]
    public void XTestInputArrivesAsKeyTextPointerAndWheelEvents()
    {
        var app = App;
        Pump.For(app, 50);
        while (app.TryGetEvent(out _))
        {
        }

        Fake(() => XTest.FakeMotion(_xtest, -1, 50, 60, 0));
        Fake(() => XTest.FakeButton(_xtest, 1, 1, 0));
        Fake(() => XTest.FakeButton(_xtest, 1, 0, 0));
        Fake(() => XTest.FakeButton(_xtest, 4, 1, 0));
        Fake(() => XTest.FakeButton(_xtest, 4, 0, 0));
        Fake(() => XTest.FakeKey(_xtest, KeycodeA, 1, 0));
        Fake(() => XTest.FakeKey(_xtest, KeycodeA, 0, 0));
        Fake(() => XTest.FakeKey(_xtest, KeycodeLeftShift, 1, 0));
        Fake(() => XTest.FakeKey(_xtest, KeycodeA, 1, 0));
        Fake(() => XTest.FakeKey(_xtest, KeycodeA, 0, 0));
        Fake(() => XTest.FakeKey(_xtest, KeycodeLeftShift, 0, 0));
        Fake(() => XTest.FakeKey(_xtest, KeycodeLeft, 1, 0));
        Fake(() => XTest.FakeKey(_xtest, KeycodeLeft, 0, 0));

        var events = Pump.Until(app, ev => ev.Count(e => e.Kind == PlatformEventKind.KeyUp) >= 5);
        var lines = Input(events);
        Assert.True(lines.Count >= 12, "ALL: " + string.Join(" / ", events.Select(Describe)) + " fakes ok");

        Assert.Contains(events, e => e is { Kind: PlatformEventKind.PointerMoved, X: 50, Y: 60 });
        Assert.Contains(events, e => e is { Kind: PlatformEventKind.PointerButtonDown, Button: MouseButton.Left, X: 50, Y: 60 });
        Assert.Contains(events, e => e is { Kind: PlatformEventKind.PointerButtonUp, Button: MouseButton.Left });
        Assert.Contains(events, e => e is { Kind: PlatformEventKind.PointerWheel, DeltaY: 1f });
        Assert.Contains(events, e => e is { Kind: PlatformEventKind.KeyDown, Key: Key.A, Modifiers: KeyModifiers.None });
        Assert.Contains(events, e => e is { Kind: PlatformEventKind.TextInput, Text: "a" });
        Assert.Contains(events, e => e is { Kind: PlatformEventKind.KeyDown, Key: Key.LeftShift });
        Assert.Contains(events, e => e is { Kind: PlatformEventKind.KeyDown, Key: Key.A, Modifiers: KeyModifiers.Shift });
        Assert.Contains(events, e => e is { Kind: PlatformEventKind.TextInput, Text: "A" });
        Assert.Contains(events, e => e is { Kind: PlatformEventKind.KeyDown, Key: Key.ArrowLeft });
        Assert.DoesNotContain(events, e => e.Kind == PlatformEventKind.TextInput && e.Text is not ("a" or "A"));
        Assert.False(app.Input.IsKeyDown(Key.A));
    }

    [Fact]
    public void InjectedInputProducesTheSameEventsAsRealInput()
    {
        var app = App;
        Pump.For(app, 50);
        while (app.TryGetEvent(out _))
        {
        }

        var id = _window!.Id;

        // Real input through the X server.
        Fake(() => XTest.FakeMotion(_xtest, -1, 70, 80, 0));
        Fake(() => XTest.FakeButton(_xtest, 3, 1, 0));
        Fake(() => XTest.FakeButton(_xtest, 3, 0, 0));
        Fake(() => XTest.FakeKey(_xtest, KeycodeA, 1, 0));
        Fake(() => XTest.FakeKey(_xtest, KeycodeA, 0, 0));
        var real = Input(Pump.Until(app, ev => ev.Any(e => e.Kind == PlatformEventKind.KeyUp)));

        // The same sequence through the injector.
        var inj = app.Injector;
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

        var synthetic = Input(events);
        Assert.Equal(real.Count, synthetic.Count);
        Assert.Equal(real.OrderBy(x => x, StringComparer.Ordinal), synthetic.OrderBy(x => x, StringComparer.Ordinal));
    }

    [Fact]
    public void CloseRequestFromTheWindowManagerProtocolIsReported()
    {
        var app = App;
        var display = _window!.NativeHandle.Display;
        var xwin = (nuint)_window.NativeHandle.Window;

        Xlib.XEvent ev = default;
        ev.Type = Xlib.ClientMessage;
        *(int*)((byte*)&ev + 16) = 1;
        *(nint*)((byte*)&ev + 24) = _xtest;
        *(nuint*)((byte*)&ev + 32) = xwin;
        *(nuint*)((byte*)&ev + 40) = Atom("WM_PROTOCOLS");
        *(int*)((byte*)&ev + 48) = 32;
        *(nuint*)((byte*)&ev + 56) = Atom("WM_DELETE_WINDOW");
        Xlib.SendEvent(_xtest, xwin, 0, 0, &ev);
        Xlib.Sync(_xtest, 0);
        Assert.NotEqual(0, display);

        var events = Pump.Until(app, e => e.Any(x => x.Kind == PlatformEventKind.WindowCloseRequested));
        Assert.Contains(events, e => e.Kind == PlatformEventKind.WindowCloseRequested && e.WindowId == _window.Id);
        Assert.False(_window.IsClosed);
    }

    [Fact]
    public void LockedCursorReportsRelativeMotionAndHidesTheCursor()
    {
        var app = App;
        _window!.SetCursorLocked(true);
        Assert.True(_window.CursorLocked);
        Assert.True(_window.CursorHidden);
        Pump.For(app, 100);
        while (app.TryGetEvent(out _))
        {
        }

        // The locked pointer sits at the window centre; moving by (+15, -7) from there reports that delta.
        Fake(() => XTest.FakeMotion(_xtest, -1, 160 + 15, 100 - 7, 0));
        var events = Pump.Until(app, ev => ev.Any(e => e.Kind == PlatformEventKind.PointerRelativeMotion));

        var motion = events.First(e => e.Kind == PlatformEventKind.PointerRelativeMotion);
        Assert.Equal((15f, -7f), (motion.DeltaX, motion.DeltaY));
        Assert.DoesNotContain(events, e => e.Kind == PlatformEventKind.PointerMoved);

        _window.SetCursorLocked(false);
        Assert.False(_window.CursorLocked);
    }

    [Fact]
    public void ClosingAWindowRemovesItFromTheApp()
    {
        var app = App;
        var second = app.CreateWindow(new WindowOptions { Width = 100, Height = 100 });
        Assert.Contains(second, app.Windows);
        second.Close();
        Assert.DoesNotContain(second, app.Windows);
        Assert.True(second.IsClosed);
        second.Close();
    }

    private nuint Atom(string name)
    {
        var bytes = Encoding.ASCII.GetBytes(name + "\0");
        fixed (byte* p = bytes)
        {
            return Xlib.InternAtom(_xtest, p, 0);
        }
    }
}
