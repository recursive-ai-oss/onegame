namespace OneGame.Platform.Tests;

public class TouchTests
{
    private static PlatformApp NewApp(bool emulate = true) =>
        PlatformApp.Create(new PlatformOptions { Backend = PlatformBackendKind.Headless, EnableGamepadDevices = false, TouchEmulatesPointer = emulate });

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
    public void FirstFingerDrivesThePointerLikeAMouse()
    {
        using var app = NewApp();
        var w = app.CreateWindow().Id;
        app.Injector.TouchDown(w, 3, 120, 80);
        app.Injector.TouchMove(w, 3, 130, 90);
        app.Injector.TouchUp(w, 3, 130, 90);

        Assert.Equal(
            [
                PlatformEventKind.TouchDown, PlatformEventKind.PointerMoved, PlatformEventKind.PointerButtonDown,
                PlatformEventKind.TouchMove, PlatformEventKind.PointerMoved,
                PlatformEventKind.TouchUp, PlatformEventKind.PointerMoved, PlatformEventKind.PointerButtonUp,
            ],
            Drain(app).Select(e => e.Kind));
        Assert.False(app.Input.IsButtonDown(MouseButton.Left));
        Assert.Equal((130f, 90f), (app.Input.PointerX, app.Input.PointerY));
    }

    [Fact]
    public void LaterFingersAreTouchEventsOnly()
    {
        using var app = NewApp();
        var w = app.CreateWindow().Id;
        app.Injector.TouchDown(w, 0, 10, 10);
        app.Injector.TouchDown(w, 1, 500, 400);
        Assert.Equal(2, app.Input.TouchCount);
        Assert.True(app.Input.TryGetTouch(1, out var x, out var y));
        Assert.Equal((500f, 400f), (x, y));

        Drain(app);
        app.Injector.TouchMove(w, 1, 510, 410);
        app.Injector.TouchUp(w, 1, 510, 410);
        Assert.Equal([PlatformEventKind.TouchMove, PlatformEventKind.TouchUp], Drain(app).Select(e => e.Kind));
        Assert.True(app.Input.IsButtonDown(MouseButton.Left), "the first finger is still down");

        // When the first finger lifts, the next one to touch becomes the pointer.
        app.Injector.TouchUp(w, 0, 10, 10);
        Assert.False(app.Input.IsButtonDown(MouseButton.Left));
        Drain(app);
        app.Injector.TouchDown(w, 1, 200, 200);
        Assert.Contains(Drain(app), e => e.Kind == PlatformEventKind.PointerButtonDown);
    }

    [Fact]
    public void CancelReleasesTheButtonAndForgetsTheFinger()
    {
        using var app = NewApp();
        var w = app.CreateWindow().Id;
        app.Injector.TouchDown(w, 0, 40, 50);
        app.Injector.TouchCancel(w, 0);
        var events = Drain(app);
        Assert.Contains(events, e => e.Kind == PlatformEventKind.TouchCancel && e.TouchId == 0 && e.X == 40 && e.Y == 50);
        Assert.Equal(PlatformEventKind.PointerButtonUp, events[^1].Kind);
        Assert.Equal(0, app.Input.TouchCount);
    }

    [Fact]
    public void EmulationCanBeTurnedOff()
    {
        using var app = NewApp(emulate: false);
        var w = app.CreateWindow().Id;
        app.Injector.Tap(w, 5, 5);
        Assert.Equal([PlatformEventKind.TouchDown, PlatformEventKind.TouchUp], Drain(app).Select(e => e.Kind));
        Assert.False(app.Input.IsButtonDown(MouseButton.Left));
    }

    [Fact]
    public void LosingTheSurfaceDropsFingersThatNeverLift()
    {
        using var app = NewApp();
        var w = app.CreateWindow().Id;
        app.Injector.TouchDown(w, 0, 1, 1);
        app.Injector.TouchDown(w, 1, 2, 2);
        app.Injector.Lifecycle(AppLifecycleState.Paused);
        Assert.Equal(2, app.Input.TouchCount);
        Assert.Contains(Drain(app), e => e is { Kind: PlatformEventKind.AppLifecycle, Lifecycle: AppLifecycleState.Paused });
    }
}

public class AndroidLogicTests
{
    [Theory]
    [InlineData(29, Key.A)]
    [InlineData(54, Key.Z)]
    [InlineData(7, Key.Digit0)]
    [InlineData(8, Key.Digit1)]
    [InlineData(16, Key.Digit9)]
    [InlineData(131, Key.F1)]
    [InlineData(142, Key.F12)]
    [InlineData(144, Key.Keypad0)]
    [InlineData(66, Key.Enter)]
    [InlineData(67, Key.Backspace)]
    [InlineData(62, Key.Space)]
    [InlineData(111, Key.Escape)]
    [InlineData(21, Key.ArrowLeft)]
    [InlineData(24, Key.Unknown)]
    [InlineData(4, Key.Unknown)]
    public void KeyCodesMapToPhysicalKeys(int code, Key expected) => Assert.Equal(expected, AndroidKeys.ToKey(code));

    [Theory]
    [InlineData(96, GamepadButton.South)]
    [InlineData(97, GamepadButton.East)]
    [InlineData(99, GamepadButton.West)]
    [InlineData(100, GamepadButton.North)]
    [InlineData(108, GamepadButton.Start)]
    [InlineData(22, GamepadButton.DPadRight)]
    public void PadKeyCodesMapToTheStandardLayout(int code, GamepadButton expected) => Assert.Equal(expected, AndroidKeys.ToGamepadButton(code));

    [Fact]
    public void TriggerKeyCodesAreLeftToTheAnalogAxes()
    {
        Assert.Null(AndroidKeys.ToGamepadButton(104));
        Assert.True(AndroidKeys.IsDigitalTrigger(105));
    }

    [Fact]
    public void MetaStateBecomesModifiers() =>
        Assert.Equal(KeyModifiers.Shift | KeyModifiers.Control | KeyModifiers.CapsLock, AndroidKeys.ToModifiers(0x1 | 0x1000 | 0x100000));

    [Theory]
    [InlineData("", 0)]
    [InlineData("--headless --frames 3", 3)]
    [InlineData("--scenario '/data/x y/s' --json", 3)]
    [InlineData("--name \"two words\" --x", 3)]
    [InlineData("   a    b  ", 2)]
    public void LaunchArgumentsSplitOnSpacesAndQuotes(string line, int count) => Assert.Equal(count, AndroidActivityHost.SplitArguments(line).Length);

    [Fact]
    public void QuotedArgumentKeepsItsSpaces() =>
        Assert.Equal(["--scenario", "/data/x y/s", "--json"], AndroidActivityHost.SplitArguments("--scenario '/data/x y/s' --json"));

    [Fact]
    public void AnEmptyQuotedArgumentIsKept() => Assert.Equal(["a", "", "b"], AndroidActivityHost.SplitArguments("a '' b"));
}
