using OneGame.Platform;

namespace OneGame.Platform.Tests;

public class ActionMapTests : IDisposable
{
    private readonly PlatformApp _app = PlatformApp.Create(new PlatformOptions { Backend = PlatformBackendKind.Headless, EnableGamepadDevices = false });
    private readonly int _window;
    private readonly int _pad;
    private readonly ActionMap _map = new();

    public ActionMapTests()
    {
        _window = _app.CreateWindow().Id;
        _pad = _app.Injector.GamepadConnect("test pad");
        _map.Context("play", c => c
            .Button("jump", Bind.Key(Key.Space), Bind.Pad(GamepadButton.South), Bind.Mouse(MouseButton.Left))
            .Button("fire", Bind.PadAxis(GamepadAxis.RightTrigger))
            .Axis("zoom", Bind.Wheel(), Bind.Pair(Key.Q, Key.E))
            .Vector("move", Bind.Wasd, Bind.Arrows, Bind.LeftStick, Bind.DPad)
            .Button("tap", Bind.Touch(TouchRegion.Left)));
        _map.Context("menu", c => c
            .Button("accept", Bind.Key(Key.Enter), Bind.Key(Key.Space))
            .OnScreens("menu"));
        _map.Context("hud", c => c.OnScreens("play"));
    }

    public void Dispose()
    {
        _app.Dispose();
        GC.SuppressFinalize(this);
    }

    // One frame: take the queued events in, then compute the state, as the host does.
    private void Frame()
    {
        _app.PumpEvents();
        while (_app.TryGetEvent(out var e))
        {
            _map.Observe(e);
        }

        _map.Update(_app.Input, 200, 100);
    }

    [Fact]
    public void KeyboardPadAndMouseDriveTheSameButton()
    {
        _app.Injector.Key(_window, Key.Space, true);
        Frame();
        Assert.True(_map.Pressed("jump"));
        Assert.True(_map.Held("jump"));
        Frame();
        Assert.False(_map.Pressed("jump"));
        Assert.True(_map.Held("jump"));
        _app.Injector.Key(_window, Key.Space, false);
        Frame();
        Assert.True(_map.Released("jump"));
        Assert.False(_map.Held("jump"));

        _app.Injector.GamepadButton(_pad, GamepadButton.South, true);
        Frame();
        Assert.True(_map.Pressed("jump"));
        _app.Injector.GamepadButton(_pad, GamepadButton.South, false);
        Frame();

        _app.Injector.PointerButton(_window, MouseButton.Left, true);
        Frame();
        Assert.True(_map.Pressed("jump"));
    }

    [Fact]
    public void APressShorterThanAFrameIsNotLost()
    {
        _app.Injector.KeyPress(_window, Key.Space);
        Frame();
        Assert.True(_map.Pressed("jump"));
        Assert.True(_map.Released("jump"));
        Assert.False(_map.Held("jump"));
        Frame();
        Assert.False(_map.Pressed("jump"));
        Assert.False(_map.Released("jump"));
    }

    [Fact]
    public void WasdAndSticksMakeOneVector()
    {
        _app.Injector.Key(_window, Key.D, true);
        _app.Injector.Key(_window, Key.S, true);
        Frame();
        var v = _map.Vector("move");
        Assert.Equal(MathF.Sqrt(0.5f), v.X, 3);
        Assert.Equal(MathF.Sqrt(0.5f), v.Y, 3);
        Assert.Equal(1f, v.Length, 3);

        _app.Injector.Key(_window, Key.D, false);
        _app.Injector.Key(_window, Key.S, false);
        _app.Injector.GamepadAxis(_pad, GamepadAxis.LeftX, -1f);
        Frame();
        Assert.Equal(-1f, _map.Vector("move").X, 3);
        Assert.Equal(0f, _map.Vector("move").Y, 3);
    }

    [Fact]
    public void AStickDeadZoneIsRescaledAndRadial()
    {
        _app.Injector.GamepadAxis(_pad, GamepadAxis.LeftX, 0.1f);
        _app.Injector.GamepadAxis(_pad, GamepadAxis.LeftY, 0.1f);
        Frame();
        Assert.Equal(0f, _map.Vector("move").Length);
        Assert.False(_map.Held("move"));

        _app.Injector.GamepadAxis(_pad, GamepadAxis.LeftX, 0.575f);
        _app.Injector.GamepadAxis(_pad, GamepadAxis.LeftY, 0f);
        Frame();
        // (0.575 - 0.15) / 0.85 = 0.5
        Assert.Equal(0.5f, _map.Vector("move").X, 3);
        Assert.False(_map.Held("move"));
        _app.Injector.GamepadAxis(_pad, GamepadAxis.LeftX, 1f);
        Frame();
        Assert.True(_map.Held("move"));
        Assert.True(_map.Pressed("move"));
    }

    [Fact]
    public void ATriggerIsAButtonPastItsThresholdAndKeepsItsStrength()
    {
        _app.Injector.GamepadAxis(_pad, GamepadAxis.RightTrigger, 0.3f);
        Frame();
        Assert.False(_map.Held("fire"));
        Assert.Equal(0.3f, _map.Value("fire"), 3);
        _app.Injector.GamepadAxis(_pad, GamepadAxis.RightTrigger, 0.8f);
        Frame();
        Assert.True(_map.Pressed("fire"));
    }

    [Fact]
    public void AnAxisTakesWheelNotchesForOneFrameAndKeyPairs()
    {
        _app.Injector.Wheel(_window, 0, 3);
        Frame();
        Assert.Equal(3f, _map.Value("zoom"));
        Frame();
        Assert.Equal(0f, _map.Value("zoom"));
        _app.Injector.Key(_window, Key.Q, true);
        Frame();
        Assert.Equal(-1f, _map.Value("zoom"));
        _app.Injector.Key(_window, Key.E, true);
        Frame();
        Assert.Equal(0f, _map.Value("zoom"));
    }

    [Fact]
    public void TouchRegionsReadTheFingersPosition()
    {
        _app.Injector.TouchDown(_window, 0, 150, 50);
        Frame();
        Assert.False(_map.Held("tap"));
        _app.Injector.TouchUp(_window, 0, 150, 50);
        _app.Injector.TouchDown(_window, 0, 20, 50);
        Frame();
        Assert.True(_map.Held("tap"));
    }

    [Fact]
    public void ContextsFollowScreensAndInactiveActionsReadZero()
    {
        _map.ActivateForScreen("menu");
        Assert.True(_map.FollowsScreens);
        Assert.True(_map.IsContextActive("menu"));
        Assert.False(_map.IsContextActive("hud"));
        Assert.True(_map.IsContextActive("play"), "a context that names no screens stays on");
        _app.Injector.Key(_window, Key.Enter, true);
        Frame();
        Assert.True(_map.Pressed("accept"));

        _map.ActivateForScreen("play");
        Frame();
        Assert.False(_map.Held("accept"), "the menu context is off");
        Assert.True(_map.Released("accept"), "turning it off releases what was held");
        _map.SetActive("menu");
        Frame();
        Assert.False(_map["jump"].IsActive);
    }

    [Fact]
    public void AnUnknownActionThrowsAndListsTheDeclaredOnes()
    {
        var e = Assert.Throws<ArgumentException>(() => _map.Pressed("jmup"));
        Assert.Contains("jump", e.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DeclarationErrorsAreCaughtEarly()
    {
        Assert.Throws<ArgumentException>(() => new ActionMap().Context("a", c => c.Button("x", Bind.Wasd)));
        Assert.Throws<ArgumentException>(() => new ActionMap().Context("a", c => c.Vector("x", Bind.Key(Key.A))));
        Assert.Throws<ArgumentException>(() => new ActionMap().Context("a", c => c.Button("1bad", Bind.Key(Key.A))));
        Assert.Throws<ArgumentException>(() => _map.Context("other", c => c.Button("jump", Bind.Key(Key.J))));
    }

    [Fact]
    public void ARebindCapturesTheNextInputAndReportsConflicts()
    {
        RebindResult? result = null;
        _map.RebindFinished += r => result = r;
        _map.BeginRebind("jump", 0);
        Assert.Equal("jump", _map.Rebinding?.Action);

        // Input meanwhile is swallowed; Escape would cancel.
        var swallowed = _map.Observe(new PlatformEvent { Kind = PlatformEventKind.KeyUp, Key = Key.A });
        Assert.True(swallowed);
        Assert.False(_map.Observe(new PlatformEvent { Kind = PlatformEventKind.WindowFocusChanged, Focused = true }));
        Assert.True(_map.Observe(new PlatformEvent { Kind = PlatformEventKind.KeyDown, Key = Key.E }));

        Assert.Null(_map.Rebinding);
        Assert.NotNull(result);
        Assert.False(result.Cancelled);
        Assert.Equal(Bind.Key(Key.E), result.Source);
        Assert.Equal("zoom", Assert.Single(result.Conflicts));
        Assert.Equal(Bind.Key(Key.E), _map["jump"].Bindings[0].Part(0));
        Assert.Equal(3, _map["jump"].Bindings.Count);

        _app.Injector.Key(_window, Key.E, true);
        Frame();
        Assert.True(_map.Pressed("jump"));
        Assert.False(_map["jump"].IsDefault);
    }

    [Fact]
    public void ReplaceTakesTheSourceAwayFromTheOtherAction()
    {
        _map.BeginRebind("jump", 1, 0, ConflictPolicy.Replace);
        _map.Observe(new PlatformEvent { Kind = PlatformEventKind.KeyDown, Key = Key.Q });
        Assert.Equal(Bind.Key(Key.Q), _map["jump"].Bindings[1].Part(0));
        var pair = _map["zoom"].Bindings.First(b => b.Shape == BindingShape.Pair);
        Assert.True(pair.Part(0).IsNone);
        Assert.Equal(Bind.Key(Key.E), pair.Part(1));
    }

    [Fact]
    public void EscapeCancelsAndAPadButtonRebindsAComposite()
    {
        _map.BeginRebind("jump", 0);
        _map.Observe(new PlatformEvent { Kind = PlatformEventKind.KeyDown, Key = Key.Escape });
        Assert.Null(_map.Rebinding);
        Assert.True(_map["jump"].IsDefault);

        // The up part (index 2) of WASD becomes a pad button.
        _map.BeginRebind("move", 0, 2);
        _map.Observe(new PlatformEvent { Kind = PlatformEventKind.GamepadButtonDown, GamepadButton = GamepadButton.North });
        Assert.Equal(Bind.Pad(GamepadButton.North), _map["move"].Bindings[0].Part(2));
        Assert.Equal(Bind.Key(Key.A), _map["move"].Bindings[0].Part(0));

        // A stick slot only takes a stick axis.
        _map.BeginRebind("move", 2);
        _map.Observe(new PlatformEvent { Kind = PlatformEventKind.KeyDown, Key = Key.J });
        Assert.NotNull(_map.Rebinding);
        _map.Observe(new PlatformEvent { Kind = PlatformEventKind.GamepadAxisMoved, GamepadAxis = GamepadAxis.RightY, Value = 0.9f });
        Assert.Equal(Bind.RightStick, _map["move"].Bindings[2]);
    }

    [Fact]
    public void BindingsRoundTripThroughTextAndAFile()
    {
        _map.SetBinding("jump", 0, Bind.Key(Key.J));
        _map.SetBinding("jump", 3, Bind.PadAxis(GamepadAxis.LeftTrigger));
        _map.SetBinding("zoom", 1, Bind.Pair(Key.Z, Key.X));
        _map.RemoveBinding("move", 3);
        var text = _map.ToText();
        Assert.StartsWith(ActionMap.FileHeader + "\n", text, StringComparison.Ordinal);
        Assert.DoesNotContain("accept", text, StringComparison.Ordinal);

        var path = Path.Combine(Path.GetTempPath(), "onegame-bindings-" + Guid.NewGuid().ToString("N"), "bindings.txt");
        try
        {
            _map.Save(path);
            _map.ResetBindings();
            Assert.True(_map["jump"].IsDefault);
            var result = _map.Load(path);
            Assert.Empty(result.Problems);
            Assert.Equal(3, result.Applied);
            Assert.Equal(text, _map.ToText());
            Assert.Equal(Bind.Key(Key.J), _map["jump"].Bindings[0].Part(0));
            Assert.Equal(3, _map["move"].Bindings.Count);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, true);
        }

        Assert.Equal(0, _map.Load(path).Applied);
    }

    [Theory]
    [InlineData("jump = key:Nope")]
    [InlineData("nothing = key:A")]
    [InlineData("jump = quad(key:A,key:B,key:C,key:D)")]
    [InlineData("move = key:Space")]
    [InlineData("jump key:A")]
    [InlineData("jump = pad:South | wheel:z+ ")]
    [InlineData("jump = axis:LeftX")]
    [InlineData("jump = key:A,key:B")]
    [InlineData("= key:A")]
    public void BadLinesAreSkippedAndReportedWithoutHurtingTheRest(string bad)
    {
        var result = _map.LoadText(ActionMap.FileHeader + "\nzoom = pair(key:Z,key:X)\n" + bad + "\n");
        Assert.Equal(1, result.Applied);
        Assert.Single(result.Problems);
        Assert.Equal(Bind.Pair(Key.Z, Key.X), _map["zoom"].Bindings[0]);
        Assert.True(_map["jump"].IsDefault);
    }

    [Fact]
    public void HugeOrForeignTextIsRejectedWhole()
    {
        _map.SetBinding("jump", 0, Bind.Key(Key.J));
        var huge = _map.LoadText(new string('x', 70_000));
        Assert.Equal(0, huge.Applied);
        Assert.Single(huge.Problems);
        Assert.Equal(Bind.Key(Key.J), _map["jump"].Bindings[0].Part(0));
        Assert.Single(_map.LoadText("onegame-bindings 9\njump = key:A").Problems);
        var many = _map.LoadText("jump = " + string.Join(" | ", Enumerable.Repeat("key:A", 40)));
        Assert.Equal(0, many.Applied);
        Assert.Single(many.Problems);
    }

    [Fact]
    public void AnExternalSourceAndPulseDriveActions()
    {
        var steam = _map.CreateSource("test");
        steam.Set("move", 0f, -1f);
        steam.Pulse("jump");
        Frame();
        Assert.True(_map.Pressed("jump"));
        Assert.True(_map.Held("jump"));
        Assert.Equal(-1f, _map.Vector("move").Y);
        Frame();
        Assert.False(_map.Held("jump"));
        Assert.True(_map.Released("jump"));
        Assert.True(_map.Held("move"));
        steam.Release("move");
        Frame();
        Assert.False(_map.Held("move"));

        _map.Harness.Set("jump", true);
        Frame();
        Assert.True(_map.Held("jump"));
        _map.Harness.Clear();
        Frame();
        Assert.False(_map.Held("jump"));
    }

    [Fact]
    public void AKeyboardCapturedByATextFieldIsIgnored()
    {
        _app.Injector.Key(_window, Key.D, true);
        _map.KeyboardCaptured = true;
        Frame();
        Assert.Equal(0f, _map.Vector("move").X);
        _app.Injector.KeyPress(_window, Key.Space);
        Frame();
        Assert.False(_map.Pressed("jump"));
        _map.KeyboardCaptured = false;
        Frame();
        Assert.Equal(1f, _map.Vector("move").X);
    }

    [Fact]
    public void AFrameOfUpdatesAllocatesNothing()
    {
        _app.Injector.Key(_window, Key.D, true);
        _app.Injector.GamepadAxis(_pad, GamepadAxis.LeftY, 0.7f);
        _app.Injector.TouchDown(_window, 0, 10, 10);
        Frame();
        _map.Harness.Set("jump", true);
        var input = _app.Input;
        for (var i = 0; i < 50; i++)
        {
            _map.Update(input, 200, 100);
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++)
        {
            _map.Update(input, 200, 100);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    [Fact]
    public void SourcesRoundTripThroughTheirSpelling()
    {
        InputSource[] all =
        [
            Bind.Key(Key.F5), Bind.Mouse(MouseButton.Forward), Bind.Pad(GamepadButton.Paddle2), Bind.PadAxis(GamepadAxis.RightY, -1),
            Bind.Wheel(-1, true), Bind.Touch(TouchRegion.Bottom), default,
        ];
        foreach (var s in all)
        {
            Assert.True(InputSource.TryParse(s.ToString(), out var back), s.ToString());
            Assert.Equal(s, back);
        }

        foreach (var b in new[] { Bind.Wasd, Bind.LeftStick, Bind.RightStick, Bind.Pair(Key.A, Key.B), (ActionBinding)Key.Space })
        {
            Assert.True(ActionBinding.TryParse(b.ToString(), out var back), b.ToString());
            Assert.Equal(b, back);
        }
    }
}
