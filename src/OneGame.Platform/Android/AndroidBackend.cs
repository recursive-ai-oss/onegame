namespace OneGame.Platform;

/// <summary>
/// The Android backend: one window that is the activity's drawing surface. It runs on the game thread; the activity host hands it the
/// callbacks the system made as commands, and the input queue to read.
/// </summary>
internal sealed unsafe class AndroidBackend : PlatformBackend
{
    private readonly AndroidActivityHost _host;
    private readonly Dictionary<int, int> _pads = [];
    private readonly Dictionary<int, float[]> _padAxes = [];
    private readonly float[] _lastX = new float[32];
    private readonly float[] _lastY = new float[32];
    private int _nextPadId = 1;
    private AndroidWindow? _window;
    private bool _lossPending;
    private int _mouseButtons;

    public AndroidBackend(PlatformApp app, AndroidActivityHost host)
        : base(app)
    {
        _host = host;
    }

    public override PlatformBackendKind Kind => PlatformBackendKind.Android;

    public override Window CreateWindow(WindowOptions options, int id)
    {
        if (_window is not null)
        {
            throw new PlatformException("An Android app has one window: the activity's surface.");
        }

        // The surface arrives a moment after the activity starts; the first frame needs its size.
        var have = _host.WaitForFirstSurface(5000);
        _window = new AndroidWindow(App, id, options, _host);
        Pump(0);
        if (!have)
        {
            // No surface yet (the app started in the background): the window keeps the requested size until one arrives.
            _window.UpdateScale(_host.Density);
        }

        return _window;
    }

    public override void Pump(int timeoutMilliseconds)
    {
        if (_lossPending)
        {
            // The game has handled the SurfaceLost event by now (it drops its swapchain when it sees it); let the system go on.
            _lossPending = false;
            _host.SurfaceLossHandled();
        }

        var timeout = Math.Max(0, timeoutMilliseconds);
        while (true)
        {
            var result = AndroidNative.ALooper_pollOnce(timeout, null, null, null);
            timeout = 0;
            while (_host.TryDequeue(out var command))
            {
                Apply(command);
            }

            _host.DrainInput(HandleInput);
            if (result is AndroidNative.LooperPollTimeout or AndroidNative.LooperPollError)
            {
                break;
            }
        }
    }

    public override void Dispose()
    {
        _window = null;
        base.Dispose();
    }

    private void Apply(AndroidActivityHost.Command c)
    {
        switch (c.Kind)
        {
            case AndroidActivityHost.CommandKind.Resume:
                App.Post(new PlatformEvent { Kind = PlatformEventKind.AppLifecycle, Lifecycle = AppLifecycleState.Resumed });
                break;
            case AndroidActivityHost.CommandKind.Pause:
                App.Post(new PlatformEvent { Kind = PlatformEventKind.AppLifecycle, Lifecycle = AppLifecycleState.Paused });
                break;
            case AndroidActivityHost.CommandKind.LowMemory:
                App.Post(new PlatformEvent { Kind = PlatformEventKind.AppLifecycle, Lifecycle = AppLifecycleState.LowMemory });
                break;
            case AndroidActivityHost.CommandKind.Focus:
                _window?.SetFocus(c.A != 0);
                break;
            case AndroidActivityHost.CommandKind.WindowCreated:
                _window?.SetSurface(c.Pointer, _host.Density);
                break;
            case AndroidActivityHost.CommandKind.WindowResized:
                _window?.Resize(c.Pointer);
                break;
            case AndroidActivityHost.CommandKind.WindowDestroyed:
                if (_window is not null && _window.ClearSurface(c.Pointer))
                {
                    _lossPending = true;
                }
                else
                {
                    // A surface that was replaced before this command ran: nothing of ours uses it.
                    _host.SurfaceLossHandled();
                }

                break;
            case AndroidActivityHost.CommandKind.ConfigurationChanged:
                _window?.UpdateScale(_host.Density);
                break;
            case AndroidActivityHost.CommandKind.Insets:
                _window?.SetSafeArea(c.Insets);
                break;
            case AndroidActivityHost.CommandKind.Destroy:
                App.Post(new PlatformEvent { Kind = PlatformEventKind.QuitRequested });
                break;
        }
    }

    // ---- input events ----

    // Returns whether the app used the event (false lets the system act on it, as it does for the volume keys).
    private bool HandleInput(nint ev)
    {
        var window = _window;
        if (window is null)
        {
            return false;
        }

        var source = AndroidNative.AInputEvent_getSource(ev);
        return AndroidNative.AInputEvent_getType(ev) switch
        {
            AndroidNative.EventTypeKey => HandleKey(ev, window, source),
            AndroidNative.EventTypeMotion => HandleMotion(ev, window, source),
            _ => false,
        };
    }

    private bool HandleKey(nint ev, AndroidWindow window, int source)
    {
        var action = AndroidNative.AKeyEvent_getAction(ev);
        if (action is not (AndroidNative.KeyActionDown or AndroidNative.KeyActionUp))
        {
            return false;
        }

        var down = action == AndroidNative.KeyActionDown;
        var code = AndroidNative.AKeyEvent_getKeyCode(ev);
        var meta = AndroidNative.AKeyEvent_getMetaState(ev);
        var device = AndroidNative.AInputEvent_getDeviceId(ev);
        var repeat = AndroidNative.AKeyEvent_getRepeatCount(ev) > 0;

        if (code == AndroidKeys.KeyBack)
        {
            if (!down)
            {
                App.Post(new PlatformEvent { Kind = PlatformEventKind.BackRequested, WindowId = window.Id });
            }

            return true;
        }

        if ((source & AndroidNative.SourceGamepad) == AndroidNative.SourceGamepad || AndroidKeys.IsDigitalTrigger(code))
        {
            if (AndroidKeys.IsDigitalTrigger(code))
            {
                return true;
            }

            if (AndroidKeys.ToGamepadButton(code) is { } button)
            {
                var pad = PadFor(device);
                App.Post(new PlatformEvent { Kind = down ? PlatformEventKind.GamepadButtonDown : PlatformEventKind.GamepadButtonUp, GamepadId = pad, GamepadButton = button });
                return true;
            }
        }

        var key = AndroidKeys.ToKey(code);
        if (key == Key.Unknown)
        {
            // Volume, camera and the like stay with the system.
            return false;
        }

        var mods = AndroidKeys.ToModifiers(meta);
        App.Post(new PlatformEvent { Kind = down ? PlatformEventKind.KeyDown : PlatformEventKind.KeyUp, WindowId = window.Id, Key = key, Modifiers = mods, IsRepeat = down && repeat });

        // Text comes from the device's key layout. This is how a soft keyboard's letters arrive when the activity has no text editor of
        // its own, and how `adb shell input text` types (see docs/interfaces/platform.md, "Android").
        if (down && (mods & (KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Meta)) == 0 && _host.GameJni is { } jni)
        {
            var ch = jni.GetUnicodeChar(device, code, meta);
            if (ch >= 0x20 && ch != 0x7f)
            {
                App.Post(new PlatformEvent { Kind = PlatformEventKind.TextInput, WindowId = window.Id, Text = char.ConvertFromUtf32(ch), Modifiers = mods });
            }
        }

        return true;
    }

    private bool HandleMotion(nint ev, AndroidWindow window, int source)
    {
        if ((source & AndroidNative.SourceJoystick) == AndroidNative.SourceJoystick)
        {
            HandleJoystick(ev);
            return true;
        }

        var action = AndroidNative.AMotionEvent_getAction(ev);
        var kind = action & AndroidNative.MotionActionMask;
        var index = (nuint)((action & AndroidNative.MotionActionPointerIndexMask) >> AndroidNative.MotionActionPointerIndexShift);

        if ((source & AndroidNative.SourceMouse) == AndroidNative.SourceMouse && AndroidNative.AMotionEvent_getToolType(ev, 0) == AndroidNative.ToolTypeMouse)
        {
            HandleMouse(ev, window, kind);
            return true;
        }

        if ((source & AndroidNative.SourceTouchscreen) != AndroidNative.SourceTouchscreen)
        {
            return false;
        }

        switch (kind)
        {
            case AndroidNative.MotionDown:
            case AndroidNative.MotionPointerDown:
                Touch(ev, window, PlatformEventKind.TouchDown, index);
                break;
            case AndroidNative.MotionUp:
            case AndroidNative.MotionPointerUp:
                Touch(ev, window, PlatformEventKind.TouchUp, index);
                break;
            case AndroidNative.MotionMove:
                var count = AndroidNative.AMotionEvent_getPointerCount(ev);
                for (nuint i = 0; i < count; i++)
                {
                    var id = AndroidNative.AMotionEvent_getPointerId(ev, i);
                    var x = AndroidNative.AMotionEvent_getX(ev, i);
                    var y = AndroidNative.AMotionEvent_getY(ev, i);
                    if ((uint)id < (uint)_lastX.Length && _lastX[id] == x && _lastY[id] == y)
                    {
                        continue;
                    }

                    Touch(ev, window, PlatformEventKind.TouchMove, i);
                }

                break;
            case AndroidNative.MotionCancel:
                var live = AndroidNative.AMotionEvent_getPointerCount(ev);
                for (nuint i = 0; i < live; i++)
                {
                    Touch(ev, window, PlatformEventKind.TouchCancel, i);
                }

                break;
        }

        return true;
    }

    private void Touch(nint ev, AndroidWindow window, PlatformEventKind kind, nuint index)
    {
        var id = AndroidNative.AMotionEvent_getPointerId(ev, index);
        var x = AndroidNative.AMotionEvent_getX(ev, index);
        var y = AndroidNative.AMotionEvent_getY(ev, index);
        if ((uint)id < (uint)_lastX.Length)
        {
            _lastX[id] = x;
            _lastY[id] = y;
        }

        App.Post(new PlatformEvent
        {
            Kind = kind,
            WindowId = window.Id,
            TouchId = id,
            X = x,
            Y = y,
            Pressure = kind is PlatformEventKind.TouchUp or PlatformEventKind.TouchCancel ? 0f : Math.Clamp(AndroidNative.AMotionEvent_getPressure(ev, index), 0f, 1f),
        });
    }

    private void HandleMouse(nint ev, AndroidWindow window, int kind)
    {
        var x = AndroidNative.AMotionEvent_getX(ev, 0);
        var y = AndroidNative.AMotionEvent_getY(ev, 0);
        var buttons = AndroidNative.AMotionEvent_getButtonState(ev);
        if (kind == AndroidNative.MotionScroll)
        {
            App.Post(new PlatformEvent
            {
                Kind = PlatformEventKind.PointerWheel,
                WindowId = window.Id,
                DeltaX = AndroidNative.AMotionEvent_getAxisValue(ev, AndroidNative.AxisHScroll, 0),
                DeltaY = AndroidNative.AMotionEvent_getAxisValue(ev, AndroidNative.AxisVScroll, 0),
            });
            return;
        }

        App.Post(new PlatformEvent { Kind = PlatformEventKind.PointerMoved, WindowId = window.Id, X = x, Y = y });
        Transition(window, buttons, AndroidNative.ButtonPrimary, MouseButton.Left, x, y);
        Transition(window, buttons, AndroidNative.ButtonSecondary, MouseButton.Right, x, y);
        Transition(window, buttons, AndroidNative.ButtonTertiary, MouseButton.Middle, x, y);
        _mouseButtons = buttons;
    }

    private void Transition(AndroidWindow window, int now, int flag, MouseButton button, float x, float y)
    {
        var was = (_mouseButtons & flag) != 0;
        var isNow = (now & flag) != 0;
        if (was != isNow)
        {
            App.Post(new PlatformEvent { Kind = isNow ? PlatformEventKind.PointerButtonDown : PlatformEventKind.PointerButtonUp, WindowId = window.Id, Button = button, X = x, Y = y });
        }
    }

    // Android already presents pads in the standard layout (A south, B east, sticks X/Y and Z/RZ, triggers, hat).
    private void HandleJoystick(nint ev)
    {
        var pad = PadFor(AndroidNative.AInputEvent_getDeviceId(ev));
        var v = _padAxes[pad];
        var dead = App.Options.StickDeadZone;

        float Axis(int axis) => AndroidNative.AMotionEvent_getAxisValue(ev, axis, 0);
        float Stick(float value) => Math.Abs(value) < dead ? 0f : value;

        var rx = Axis(AndroidNative.AxisZ);
        var ry = Axis(AndroidNative.AxisRz);
        if (rx == 0 && ry == 0)
        {
            rx = Axis(AndroidNative.AxisRx);
            ry = Axis(AndroidNative.AxisRy);
        }

        Set(pad, v, GamepadAxis.LeftX, Stick(Axis(AndroidNative.AxisX)));
        Set(pad, v, GamepadAxis.LeftY, Stick(Axis(AndroidNative.AxisY)));
        Set(pad, v, GamepadAxis.RightX, Stick(rx));
        Set(pad, v, GamepadAxis.RightY, Stick(ry));
        var lt = Math.Max(Axis(AndroidNative.AxisLTrigger), Axis(AndroidNative.AxisBrake));
        var rt = Math.Max(Axis(AndroidNative.AxisRTrigger), Axis(AndroidNative.AxisGas));
        var tdead = App.Options.TriggerDeadZone;
        Set(pad, v, GamepadAxis.LeftTrigger, lt < tdead ? 0f : lt);
        Set(pad, v, GamepadAxis.RightTrigger, rt < tdead ? 0f : rt);
    }

    private void Set(int pad, float[] cache, GamepadAxis axis, float value)
    {
        if (cache[(int)axis] == value)
        {
            return;
        }

        cache[(int)axis] = value;
        App.Post(new PlatformEvent { Kind = PlatformEventKind.GamepadAxisMoved, GamepadId = pad, GamepadAxis = axis, Value = value });
    }

    private int PadFor(int deviceId)
    {
        if (_pads.TryGetValue(deviceId, out var id))
        {
            return id;
        }

        id = _nextPadId++;
        _pads[deviceId] = id;
        _padAxes[id] = new float[6];
        App.Gamepads.RegisterSynthetic(id, "Android gamepad " + deviceId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return id;
    }
}

/// <summary>The activity's drawing surface as a platform window.</summary>
internal sealed class AndroidWindow : Window
{
    private readonly AndroidActivityHost _host;
    private nint _surface;

    public AndroidWindow(PlatformApp app, int id, WindowOptions options, AndroidActivityHost host)
        : base(app, id)
    {
        _host = host;
        Width = options.Width;
        Height = options.Height;
        Title = options.Title;
        IsFocused = true;
        IsFullscreen = true;
    }

    public override NativeWindowHandle NativeHandle => _surface == 0 ? default : new NativeWindowHandle(NativeWindowKind.Android, 0, _surface);

    // Android's logical unit is the dp, 160 per inch; one logical unit of ours is one dp so touch targets keep their physical size.
    public void UpdateScale(int density) => ApplyScale(density / 160f);

    public void SetFocus(bool focused) => ApplyFocus(focused);

    public void SetSafeArea(EdgeInsets insets) => ApplySafeArea(insets);

    public void SetSurface(nint surface, int density)
    {
        _surface = surface;
        UpdateScale(density);
        Resize(surface);
        IsMinimized = false;
        App.Post(new PlatformEvent { Kind = PlatformEventKind.SurfaceCreated, WindowId = Id });
    }

    public void Resize(nint surface)
    {
        if (surface == 0 || surface != _surface)
        {
            return;
        }

        var w = AndroidNative.ANativeWindow_getWidth(surface);
        var h = AndroidNative.ANativeWindow_getHeight(surface);
        if (w > 0 && h > 0)
        {
            ApplySize(w, h);
        }
    }

    // Returns false when the destroyed surface is not the current one.
    public bool ClearSurface(nint surface)
    {
        if (_surface != surface)
        {
            return false;
        }

        _surface = 0;
        IsMinimized = true;
        App.Post(new PlatformEvent { Kind = PlatformEventKind.SurfaceLost, WindowId = Id });
        return true;
    }

    public override void SetTitle(string title) => Title = title;

    public override void RequestSize(int width, int height)
    {
        // The system decides the size of an app window.
    }

    public override void SetFullscreen(bool fullscreen)
    {
        // An Android app window always covers the screen; SafeArea says what to keep clear.
    }

    public override void Minimize()
    {
        // The user sends an app to the background, not the app.
    }

    public override void SetCursorHidden(bool hidden) => CursorHidden = hidden;

    public override void SetCursorLocked(bool locked)
    {
        // Pointer capture needs a Java view; a touch screen has no cursor to lock.
    }

    private protected override void ApplyTextInput() => _host.ShowKeyboard(TextInputActive);

    private protected override void DestroyNative() => AndroidNative.ANativeActivity_finish(_host.Activity);
}
