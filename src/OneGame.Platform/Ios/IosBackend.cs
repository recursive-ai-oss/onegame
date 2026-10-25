using System.Globalization;

namespace OneGame.Platform;

/// <summary>
/// The iOS backend: one window that is the app's scene, drawn through a <c>CAMetalLayer</c>. It runs on the game thread; the UIKit
/// callbacks on the main thread queue events (see <see cref="IosAppHost"/>) and this class turns them into platform events.
/// Gamepads come from the GameController framework and are read here, on the game thread, once per pump.
/// </summary>
internal sealed class IosBackend : PlatformBackend
{
    private readonly IosAppHost _host;
    private readonly List<IosEvent> _scratch = [];
    private readonly Dictionary<nint, IosPad> _pads = [];
    private readonly List<nint> _seen = [];
    private int _nextPadId = 1;
    private int _pausedCount;
    private int _resumedCount;
    private IosWindow? _window;

    public IosBackend(PlatformApp app, IosAppHost host)
        : base(app)
    {
        _host = host;
        if (app.Options.EnableGamepadDevices)
        {
            IosObjc.LoadFrameworks();
        }
    }

    public override PlatformBackendKind Kind => PlatformBackendKind.Ios;

    public override bool ClipboardAvailable => true;

    public override Window CreateWindow(WindowOptions options, int id)
    {
        if (_window is not null)
        {
            throw new PlatformException("An iOS app has one window: the scene's.");
        }

        if (!_host.WaitForScene(10000))
        {
            throw new PlatformException("The UIKit scene did not connect within 10 seconds, so there is no window to draw into.");
        }

        _window = new IosWindow(App, id, options, _host);
        if (_host.TryGetLayout(out var layout))
        {
            _window.ApplyLayout(layout);
        }

        Pump(0);
        return _window;
    }

    public override void Pump(int timeoutMilliseconds)
    {
        var pool = IosObjc.objc_autoreleasePoolPush();
        try
        {
            _scratch.Clear();
            _host.Drain(_scratch, Math.Max(0, timeoutMilliseconds));
            foreach (var e in _scratch)
            {
                Apply(e);
            }

            if (App.Options.EnableGamepadDevices)
            {
                PollPads();
            }
        }
        finally
        {
            IosObjc.objc_autoreleasePoolPop(pool);
        }
    }

    private void Apply(in IosEvent e)
    {
        var window = _window;
        if (window is null)
        {
            return;
        }

        switch (e.Kind)
        {
            case IosEventKind.TouchDown or IosEventKind.TouchMove or IosEventKind.TouchUp or IosEventKind.TouchCancel:
                App.Post(new PlatformEvent
                {
                    Kind = e.Kind switch
                    {
                        IosEventKind.TouchDown => PlatformEventKind.TouchDown,
                        IosEventKind.TouchMove => PlatformEventKind.TouchMove,
                        IosEventKind.TouchUp => PlatformEventKind.TouchUp,
                        _ => PlatformEventKind.TouchCancel,
                    },
                    WindowId = window.Id,
                    TouchId = e.Id,
                    X = e.X,
                    Y = e.Y,
                    Pressure = e.Pressure,
                });
                break;
            case IosEventKind.KeyDown or IosEventKind.KeyUp:
                App.Post(new PlatformEvent
                {
                    Kind = e.Kind == IosEventKind.KeyDown ? PlatformEventKind.KeyDown : PlatformEventKind.KeyUp,
                    WindowId = window.Id,
                    Key = e.Key,
                    Modifiers = e.Modifiers,
                    IsRepeat = e.Repeat,
                });
                break;
            case IosEventKind.Text:
                App.Post(new PlatformEvent { Kind = PlatformEventKind.TextInput, WindowId = window.Id, Text = e.Text, Modifiers = e.Modifiers });
                break;
            case IosEventKind.Composition:
                App.Post(new PlatformEvent { Kind = PlatformEventKind.TextComposition, WindowId = window.Id, Text = e.Text, CursorStart = e.CursorStart, CursorEnd = e.CursorEnd });
                break;
            case IosEventKind.Layout:
                window.ApplyLayout(e);
                break;
            case IosEventKind.Paused:
                // Nothing may be drawn once the app is in the background.
                window.SetMinimized(true);
                _pausedCount++;
                App.Post(new PlatformEvent { Kind = PlatformEventKind.AppLifecycle, Lifecycle = AppLifecycleState.Paused });
                break;
            case IosEventKind.Resumed:
                window.SetMinimized(false);
                _resumedCount++;
                App.Post(new PlatformEvent { Kind = PlatformEventKind.AppLifecycle, Lifecycle = AppLifecycleState.Resumed });
                break;
            case IosEventKind.LowMemory:
                App.Post(new PlatformEvent { Kind = PlatformEventKind.AppLifecycle, Lifecycle = AppLifecycleState.LowMemory });
                break;
            case IosEventKind.Focus:
                window.SetFocus(e.Flag);
                break;
        }
    }

    // ---- clipboard ----

    public override string? ClipboardGet(ClipboardSelection selection)
    {
        string? text = null;
        IosObjc.OnMainWait(() =>
        {
            var board = IosObjc.Obj(IosObjc.Cls("UIPasteboard"), IosObjc.Sel("generalPasteboard"));
            var s = IosObjc.Obj(board, IosObjc.Sel("string"));
            text = s == 0 ? null : IosObjc.ToManaged(s);
        });
        return text;
    }

    public override bool ClipboardSet(ClipboardSelection selection, string text)
    {
        IosObjc.OnMainWait(() =>
        {
            var board = IosObjc.Obj(IosObjc.Cls("UIPasteboard"), IosObjc.Sel("generalPasteboard"));
            IosObjc.Void(board, IosObjc.Sel("setString:"), IosObjc.NSString(text));
        });
        return true;
    }

    // ---- control surface ----

    public override string NativeCommand(string action, string? argument)
    {
        switch (action)
        {
            case "describe":
                var w = _window;
                if (w is null)
                {
                    return "{\"backend\":\"ios\",\"window\":false}";
                }

                var s = w.SafeArea;
                return string.Create(CultureInfo.InvariantCulture,
                    $"{{\"backend\":\"ios\",\"width\":{w.Width},\"height\":{w.Height},\"scale\":{w.Scale},\"safeArea\":{{\"left\":{s.Left},\"top\":{s.Top},\"right\":{s.Right},\"bottom\":{s.Bottom}}},\"orientation\":\"{OrientationName(IosUi.Orientation())}\",\"applicationState\":\"{(IosUi.ApplicationState() switch { 0 => "active", 1 => "inactive", _ => "background" })}\",\"textInputActive\":{(IosText.IsActive() ? "true" : "false")},\"pads\":{_pads.Count}}}");
            case "orientation":
                return IosUi.RequestOrientation(argument switch
                {
                    "portrait" => 2,
                    "portrait-upside-down" => 4,
                    "landscape-right" => 8,
                    "landscape-left" => 16,
                    "landscape" => 24,
                    "all" => 30,
                    _ => throw new PlatformException("orientation is portrait, portrait-upside-down, landscape-left, landscape-right, landscape or all."),
                });
            case "text_insert" or "text_mark":
                return IosText.Drive(action, argument ?? throw new PlatformException(action + " needs a text argument."));
            case "text_unmark" or "text_delete" or "text_probe":
                return IosText.Drive(action, "");
            default:
                return base.NativeCommand(action, argument);
        }
    }

    // UIInterfaceOrientation: portrait 1, upside down 2, landscape right 3, landscape left 4.
    private static string OrientationName(int value) => value switch
    {
        1 => "portrait",
        2 => "portrait-upside-down",
        3 => "landscape-right",
        4 => "landscape-left",
        _ => "unknown",
    };

    // ---- gamepads (GameController) ----

    private sealed class IosPad
    {
        public int Id;
        public readonly bool[] Buttons = new bool[19];
        public readonly float[] Axes = new float[6];
    }

    private static readonly (string Selector, GamepadButton Button)[] ButtonMap =
    [
        ("buttonA", GamepadButton.South),
        ("buttonB", GamepadButton.East),
        ("buttonX", GamepadButton.West),
        ("buttonY", GamepadButton.North),
        ("leftShoulder", GamepadButton.LeftShoulder),
        ("rightShoulder", GamepadButton.RightShoulder),
        ("buttonOptions", GamepadButton.Back),
        ("buttonMenu", GamepadButton.Start),
        ("buttonHome", GamepadButton.Guide),
        ("leftThumbstickButton", GamepadButton.LeftStick),
        ("rightThumbstickButton", GamepadButton.RightStick),
    ];

    private void PollPads()
    {
        var controllerClass = IosObjc.Cls("GCController");
        if (controllerClass == 0)
        {
            return;
        }

        var list = IosObjc.Items(IosObjc.Obj(controllerClass, IosObjc.Sel("controllers")));
        _seen.Clear();
        foreach (var controller in list)
        {
            var pad = IosObjc.Obj(controller, IosObjc.Sel("extendedGamepad"));
            if (pad == 0)
            {
                continue;
            }

            _seen.Add(controller);
            if (!_pads.TryGetValue(controller, out var state))
            {
                state = new IosPad { Id = _nextPadId++ };
                _pads[controller] = state;
                var name = IosObjc.ToManaged(IosObjc.Obj(controller, IosObjc.Sel("vendorName")));
                App.Gamepads.RegisterSynthetic(state.Id, name.Length > 0 ? name : "iOS gamepad " + state.Id.ToString(CultureInfo.InvariantCulture));
            }

            foreach (var (selector, button) in ButtonMap)
            {
                var b = IosObjc.Obj(pad, IosObjc.Sel(selector));
                SetButton(state, button, b != 0 && IosObjc.Flag(b, IosObjc.Sel("isPressed")));
            }

            var dpad = IosObjc.Obj(pad, IosObjc.Sel("dpad"));
            SetButton(state, GamepadButton.DPadUp, Pressed(dpad, "up"));
            SetButton(state, GamepadButton.DPadDown, Pressed(dpad, "down"));
            SetButton(state, GamepadButton.DPadLeft, Pressed(dpad, "left"));
            SetButton(state, GamepadButton.DPadRight, Pressed(dpad, "right"));

            var dead = App.Options.StickDeadZone;
            var left = IosObjc.Obj(pad, IosObjc.Sel("leftThumbstick"));
            var right = IosObjc.Obj(pad, IosObjc.Sel("rightThumbstick"));
            // GameController reports up as positive; the platform layer's standard has down positive.
            SetAxis(state, GamepadAxis.LeftX, Stick(AxisValue(left, "xAxis"), dead));
            SetAxis(state, GamepadAxis.LeftY, Stick(-AxisValue(left, "yAxis"), dead));
            SetAxis(state, GamepadAxis.RightX, Stick(AxisValue(right, "xAxis"), dead));
            SetAxis(state, GamepadAxis.RightY, Stick(-AxisValue(right, "yAxis"), dead));
            var tdead = App.Options.TriggerDeadZone;
            SetAxis(state, GamepadAxis.LeftTrigger, Trigger(IosObjc.Float(IosObjc.Obj(pad, IosObjc.Sel("leftTrigger")), IosObjc.Sel("value")), tdead));
            SetAxis(state, GamepadAxis.RightTrigger, Trigger(IosObjc.Float(IosObjc.Obj(pad, IosObjc.Sel("rightTrigger")), IosObjc.Sel("value")), tdead));
        }

        if (_pads.Count != _seen.Count)
        {
            var gone = new List<nint>();
            foreach (var key in _pads.Keys)
            {
                if (!_seen.Contains(key))
                {
                    gone.Add(key);
                }
            }

            foreach (var key in gone)
            {
                App.Post(new PlatformEvent { Kind = PlatformEventKind.GamepadDisconnected, GamepadId = _pads[key].Id });
                _pads.Remove(key);
            }
        }
    }

    private static bool Pressed(nint dpad, string direction)
    {
        if (dpad == 0)
        {
            return false;
        }

        var b = IosObjc.Obj(dpad, IosObjc.Sel(direction));
        return b != 0 && IosObjc.Flag(b, IosObjc.Sel("isPressed"));
    }

    private static float AxisValue(nint stick, string axis) =>
        stick == 0 ? 0f : IosObjc.Float(IosObjc.Obj(stick, IosObjc.Sel(axis)), IosObjc.Sel("value"));

    private static float Stick(float v, float dead) => Math.Abs(v) < dead ? 0f : v;

    private static float Trigger(float v, float dead) => v < dead ? 0f : v;

    private void SetButton(IosPad pad, GamepadButton button, bool down)
    {
        if (pad.Buttons[(int)button] == down)
        {
            return;
        }

        pad.Buttons[(int)button] = down;
        App.Post(new PlatformEvent { Kind = down ? PlatformEventKind.GamepadButtonDown : PlatformEventKind.GamepadButtonUp, GamepadId = pad.Id, GamepadButton = button });
    }

    private void SetAxis(IosPad pad, GamepadAxis axis, float value)
    {
        if (pad.Axes[(int)axis] == value)
        {
            return;
        }

        pad.Axes[(int)axis] = value;
        App.Post(new PlatformEvent { Kind = PlatformEventKind.GamepadAxisMoved, GamepadId = pad.Id, GamepadAxis = axis, Value = value });
    }

    public override void Dispose()
    {
        _window = null;
        base.Dispose();
    }
}

/// <summary>The app's scene as a platform window.</summary>
internal sealed class IosWindow : Window
{
    private readonly IosAppHost _host;

    public IosWindow(PlatformApp app, int id, WindowOptions options, IosAppHost host)
        : base(app, id)
    {
        _host = host;
        Width = options.Width;
        Height = options.Height;
        Title = options.Title;
        IsFocused = true;
        IsFullscreen = true;
    }

    /// <summary>The <c>CAMetalLayer</c> the GPU layer presents to; it lives as long as the app.</summary>
    public override NativeWindowHandle NativeHandle => _host.Layer == 0 ? default : new NativeWindowHandle(NativeWindowKind.MetalLayer, 0, _host.Layer);

    // One logical unit is one point (1/163 inch on iPhones, 1/132 on iPads), the unit UIKit lays out in: scale is the points-to-pixels factor.
    public void ApplyLayout(in IosEvent e)
    {
        if (e.Width <= 0 || e.Height <= 0)
        {
            return;
        }

        ApplyScale(e.Scale);
        ApplySize(e.Width, e.Height);
        ApplySafeArea(e.Insets);
    }

    public void SetFocus(bool focused) => ApplyFocus(focused);

    public void SetMinimized(bool minimized) => IsMinimized = minimized;

    public override void SetTitle(string title) => Title = title;

    public override void RequestSize(int width, int height)
    {
        // The system decides the size of an app window.
    }

    public override void SetFullscreen(bool fullscreen)
    {
        // An iOS app window always covers the screen; SafeArea says what to keep clear.
    }

    public override void Minimize()
    {
        // The user sends an app to the background, not the app.
    }

    public override void SetCursorHidden(bool hidden) => CursorHidden = hidden;

    public override void SetCursorLocked(bool locked)
    {
        // A touch screen has no cursor to lock.
    }

    private protected override void ApplyTextInput()
    {
        var (x, y, w, h) = TextInputArea;
        IosUi.SetTextInput(TextInputActive, x, y, w, h);
    }

    private protected override void DestroyNative()
    {
        // The scene outlives the game; the process ends when the game does.
    }
}
