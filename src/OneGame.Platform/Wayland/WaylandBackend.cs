using System.Runtime.InteropServices;
using System.Text;

namespace OneGame.Platform;

/// <summary>
/// Wayland through libwayland-client with our own protocol tables (generated from the protocol XML): core, xdg-shell, and where the
/// compositor offers them fractional scale, viewporter, server-side decorations, relative pointer and pointer constraints.
/// </summary>
internal sealed unsafe class WaylandBackend : PlatformBackend
{
    private const long RepeatPollMilliseconds = 5;

    private readonly nint _display;
    private readonly WlObject _displayObject;
    private readonly WlObject _registry;
    private readonly Dictionary<nint, WaylandWindow> _bySurface = [];
    private WlObject? _compositor, _shm, _wmBase, _seat, _fractionalScaleManager, _decorationManager, _viewporter, _relativePointerManager, _pointerConstraints, _cursorShapeManager;
    private WlObject? _pointer, _keyboard, _cursorShapeDevice;
    private XkbKeyboard? _xkb;
    private WaylandWindow? _pointerWindow, _keyboardWindow;
    private uint _pointerSerial;

    // Key repeat is the client's job on Wayland: the compositor only says how fast.
    private int _repeatRate = 25, _repeatDelay = 400;
    private uint _repeatCode;
    private long _repeatNext;

    public WaylandBackend(PlatformApp app)
        : base(app)
    {
        try
        {
            _display = Connect(app.Options.DisplayName);
        }
        catch (DllNotFoundException e)
        {
            throw new PlatformException("The Wayland backend needs libwayland-client and libxkbcommon: " + e.Message, e);
        }

        if (_display == 0)
        {
            throw new PlatformException("Could not connect to a Wayland compositor (check WAYLAND_DISPLAY and XDG_RUNTIME_DIR).");
        }

        _displayObject = WlObject.Wrap(_display, WlDisplay.Info, listen: false);
        _registry = _displayObject.Create(WlDisplay.Request.GetRegistry, WlRegistry.Info, 1, [0]);
        _registry.Handler = OnRegistry;
        Roundtrip();
        Roundtrip(); // The second round trip collects what the bound globals announce: seat capabilities, outputs.
        if (_compositor is null || _wmBase is null)
        {
            throw new PlatformException("The compositor does not offer wl_compositor and xdg_wm_base.");
        }
    }

    public override PlatformBackendKind Kind => PlatformBackendKind.Wayland;

    internal WlObject Compositor => _compositor!;

    internal WlObject? Shm => _shm;

    internal WlObject WmBase => _wmBase!;

    internal WlObject? FractionalScaleManager => _fractionalScaleManager;

    internal WlObject? DecorationManager => _decorationManager;

    internal WlObject? Viewporter => _viewporter;

    internal nint DisplayHandle => _display;

    internal bool HasPointer => _pointer is not null;

    internal bool HasKeyboard => _keyboard is not null;

    public override Window CreateWindow(WindowOptions options, int id)
    {
        var window = new WaylandWindow(this, App, id, options);
        _bySurface[window.Surface.Proxy] = window;
        window.CommitInitial();
        for (var i = 0; i < 100 && !window.Configured; i++)
        {
            Pump(20);
        }

        if (!window.Configured)
        {
            throw new PlatformException("The compositor never configured the new window.");
        }

        window.Present();
        return window;
    }

    public override void Pump(int timeoutMilliseconds)
    {
        while (WlClient.DisplayPrepareRead(_display) != 0)
        {
            Dispatch();
        }

        if (WlClient.DisplayFlush(_display) < 0 && Marshal.GetLastPInvokeError() != Libc.EAGAIN)
        {
            WlClient.DisplayCancelRead(_display);
            throw new PlatformException("Lost the connection to the Wayland compositor.");
        }

        var wait = RepeatActive() ? Math.Min(Math.Max(timeoutMilliseconds, 0), (int)RepeatPollMilliseconds) : timeoutMilliseconds;
        var fd = new Libc.PollFd { Fd = WlClient.DisplayGetFd(_display), Events = Libc.POLLIN };
        if (Libc.Poll(&fd, 1, wait) > 0)
        {
            if (WlClient.DisplayReadEvents(_display) < 0)
            {
                throw new PlatformException("Lost the connection to the Wayland compositor.");
            }
        }
        else
        {
            WlClient.DisplayCancelRead(_display);
        }

        Dispatch();
        TickRepeat();
    }

    public override void Dispose()
    {
        foreach (var w in new List<WaylandWindow>(_bySurface.Values))
        {
            w.Close();
        }

        _xkb?.Dispose();
        if (_display != 0)
        {
            _ = WlClient.DisplayFlush(_display);
            WlClient.DisplayDisconnect(_display);
        }

        base.Dispose();
    }

    internal void Forget(WaylandWindow w)
    {
        _bySurface.Remove(w.Surface.Proxy);
        if (_pointerWindow == w)
        {
            _pointerWindow = null;
        }

        if (_keyboardWindow == w)
        {
            _keyboardWindow = null;
            _repeatCode = 0;
        }
    }

    internal void ApplyPointerCursor(bool hidden)
    {
        if (_pointer is null || _pointerSerial == 0)
        {
            return;
        }

        if (hidden)
        {
            _pointer.Send(WlPointer.Request.SetCursor, [WlObject.Arg(_pointerSerial), 0, 0, 0]);
        }
        else if (_cursorShapeDevice is not null)
        {
            _cursorShapeDevice.Send(WpCursorShapeDeviceV1.Request.SetShape, [WlObject.Arg(_pointerSerial), WpCursorShapeDeviceV1.ShapeEnum.Default]);
        }
    }

    /// <summary>Locks the pointer to a surface; null when the compositor lacks the protocols. The callback gets unaccelerated motion.</summary>
    internal WaylandPointerLock? StartLock(WlObject surface, Action<double, double> onRelative)
    {
        if (_pointer is null || _pointerConstraints is null || _relativePointerManager is null)
        {
            return null;
        }

        var locked = _pointerConstraints.Create(
            ZwpPointerConstraintsV1.Request.LockPointer,
            ZwpLockedPointerV1.Info,
            _pointerConstraints.Version,
            [0, WlObject.Arg(surface), WlObject.Arg(_pointer), 0, ZwpPointerConstraintsV1.LifetimeEnum.Persistent]);
        var relative = _relativePointerManager.Create(
            ZwpRelativePointerManagerV1.Request.GetRelativePointer,
            ZwpRelativePointerV1.Info,
            _relativePointerManager.Version,
            [0, WlObject.Arg(_pointer)]);
        relative.Handler = (_, opcode, a) =>
        {
            if (opcode == ZwpRelativePointerV1.Event.RelativeMotion)
            {
                // Arguments 4 and 5 are the motion before pointer acceleration, which is what mouse look wants.
                onRelative(a.Fixed(4), a.Fixed(5));
            }
        };
        return new WaylandPointerLock(locked, relative);
    }

    private static nint Connect(string? name)
    {
        if (name is null)
        {
            return WlClient.DisplayConnect(null);
        }

        var bytes = Encoding.UTF8.GetBytes(name + "\0");
        fixed (byte* p = bytes)
        {
            return WlClient.DisplayConnect(p);
        }
    }

    private static void Rethrow()
    {
        if (WlObject.PendingException is { } e)
        {
            WlObject.PendingException = null;
            throw new PlatformException("A Wayland event handler failed: " + e.Message, e);
        }
    }

    private void Roundtrip()
    {
        if (WlClient.DisplayRoundtrip(_display) < 0)
        {
            throw new PlatformException("Wayland round trip failed: protocol error " + WlClient.DisplayGetError(_display));
        }

        Rethrow();
    }

    private bool RepeatActive() => _repeatCode != 0 && _repeatRate > 0;

    private void Dispatch()
    {
        if (WlClient.DisplayDispatchPending(_display) < 0)
        {
            throw new PlatformException("Wayland protocol error " + WlClient.DisplayGetError(_display));
        }

        Rethrow();
    }

    private WlObject Bind(uint name, WlInterfaceInfo info, uint serverVersion, uint cap)
    {
        var version = Math.Min(Math.Min(serverVersion, cap), (uint)info.Version);
        var interfaceName = *(nint*)WlInterfaces.Get(info.Name);
        return _registry.Create(WlRegistry.Request.Bind, info, version, [name, (ulong)interfaceName, version, 0]);
    }

    private void OnRegistry(WlObject sender, int opcode, WlArgs a)
    {
        if (opcode != WlRegistry.Event.Global)
        {
            return;
        }

        var name = a.Uint(0);
        var iface = a.String(1);
        var version = a.Uint(2);
        switch (iface)
        {
            case WlCompositor.Name:
                _compositor = Bind(name, WlCompositor.Info, version, 6);
                break;
            case WlShm.Name:
                _shm = Bind(name, WlShm.Info, version, 1);
                break;
            case XdgWmBase.Name:
                _wmBase = Bind(name, XdgWmBase.Info, version, 5);
                _wmBase.Handler = (o, op, args) =>
                {
                    if (op == XdgWmBase.Event.Ping)
                    {
                        o.Send(XdgWmBase.Request.Pong, [args.Uint(0)]);
                    }
                };
                break;
            case WlSeat.Name:
                _seat = Bind(name, WlSeat.Info, version, 8);
                _seat.Handler = OnSeat;
                break;
            case WpFractionalScaleManagerV1.Name:
                _fractionalScaleManager = Bind(name, WpFractionalScaleManagerV1.Info, version, 1);
                break;
            case ZxdgDecorationManagerV1.Name:
                _decorationManager = Bind(name, ZxdgDecorationManagerV1.Info, version, 1);
                break;
            case WpViewporter.Name:
                _viewporter = Bind(name, WpViewporter.Info, version, 1);
                break;
            case ZwpRelativePointerManagerV1.Name:
                _relativePointerManager = Bind(name, ZwpRelativePointerManagerV1.Info, version, 1);
                break;
            case ZwpPointerConstraintsV1.Name:
                _pointerConstraints = Bind(name, ZwpPointerConstraintsV1.Info, version, 1);
                break;
            case WpCursorShapeManagerV1.Name:
                _cursorShapeManager = Bind(name, WpCursorShapeManagerV1.Info, version, 1);
                break;
        }
    }

    private void OnSeat(WlObject seat, int opcode, WlArgs a)
    {
        if (opcode != WlSeat.Event.Capabilities)
        {
            return;
        }

        var caps = a.Uint(0);
        var hasPointer = (caps & WlSeat.CapabilityEnum.Pointer) != 0;
        if (hasPointer && _pointer is null)
        {
            _pointer = seat.Create(WlSeat.Request.GetPointer, WlPointer.Info, seat.Version, [0]);
            _pointer.Handler = OnPointer;
            if (_cursorShapeManager is not null)
            {
                _cursorShapeDevice = _cursorShapeManager.Create(
                    WpCursorShapeManagerV1.Request.GetPointer, WpCursorShapeDeviceV1.Info, _cursorShapeManager.Version, [0, WlObject.Arg(_pointer)]);
            }
        }
        else if (!hasPointer && _pointer is not null)
        {
            _pointer.Release();
            _pointer = null;
        }

        var hasKeyboard = (caps & WlSeat.CapabilityEnum.Keyboard) != 0;
        if (hasKeyboard && _keyboard is null)
        {
            _keyboard = seat.Create(WlSeat.Request.GetKeyboard, WlKeyboard.Info, seat.Version, [0]);
            _keyboard.Handler = OnKeyboard;
        }
        else if (!hasKeyboard && _keyboard is not null)
        {
            _keyboard.Release();
            _keyboard = null;
        }
    }

    private void OnPointer(WlObject pointer, int opcode, WlArgs a)
    {
        switch (opcode)
        {
            case WlPointer.Event.Enter:
                _pointerSerial = a.Uint(0);
                _pointerWindow = _bySurface.GetValueOrDefault(a.Object(1));
                if (_pointerWindow is { } w)
                {
                    ApplyPointerCursor(w.CursorHidden || w.CursorLocked);
                    App.Post(new PlatformEvent { Kind = PlatformEventKind.PointerInside, WindowId = w.Id, Focused = true });
                    w.OnPointerMove(a.Fixed(2), a.Fixed(3));
                }

                break;
            case WlPointer.Event.Leave:
                if (_pointerWindow is { } left)
                {
                    App.Post(new PlatformEvent { Kind = PlatformEventKind.PointerInside, WindowId = left.Id, Focused = false });
                }

                _pointerWindow = null;
                break;
            case WlPointer.Event.Motion:
                _pointerWindow?.OnPointerMove(a.Fixed(1), a.Fixed(2));
                break;
            case WlPointer.Event.Button:
                if (_pointerWindow is { } bw)
                {
                    // Button codes are the kernel's BTN_* values.
                    MouseButton? button = a.Uint(2) switch
                    {
                        0x110 => MouseButton.Left,
                        0x111 => MouseButton.Right,
                        0x112 => MouseButton.Middle,
                        0x113 => MouseButton.Back,
                        0x114 => MouseButton.Forward,
                        _ => null,
                    };
                    if (button is { } b)
                    {
                        App.Post(new PlatformEvent
                        {
                            Kind = a.Uint(3) == WlPointer.ButtonStateEnum.Pressed ? PlatformEventKind.PointerButtonDown : PlatformEventKind.PointerButtonUp,
                            WindowId = bw.Id,
                            Button = b,
                            X = App.Input.PointerX,
                            Y = App.Input.PointerY,
                            Modifiers = _xkb?.Modifiers ?? KeyModifiers.None,
                        });
                    }
                }

                break;
            case WlPointer.Event.AxisValue120 when _pointerWindow is not null:
                PostWheel(a.Uint(0), a.Int(1) / 120f);
                break;
            case WlPointer.Event.AxisDiscrete when _pointerWindow is not null && pointer.Version is >= 5 and < 8:
                PostWheel(a.Uint(0), a.Int(1));
                break;
            case WlPointer.Event.Axis when _pointerWindow is not null && pointer.Version < 5:
                PostWheel(a.Uint(1), (float)(a.Fixed(2) / 10.0));
                break;
        }
    }

    // Axis 0 is vertical (positive scrolls down), axis 1 horizontal (positive scrolls right). Ours: positive Y is away from the user.
    private void PostWheel(uint axis, float notches)
    {
        var w = _pointerWindow!;
        App.Post(new PlatformEvent
        {
            Kind = PlatformEventKind.PointerWheel,
            WindowId = w.Id,
            DeltaX = axis == WlPointer.AxisEnum.HorizontalScroll ? notches : 0f,
            DeltaY = axis == WlPointer.AxisEnum.VerticalScroll ? -notches : 0f,
            X = App.Input.PointerX,
            Y = App.Input.PointerY,
            Modifiers = _xkb?.Modifiers ?? KeyModifiers.None,
        });
    }

    private void OnKeyboard(WlObject keyboard, int opcode, WlArgs a)
    {
        switch (opcode)
        {
            case WlKeyboard.Event.Keymap:
                LoadKeymap(a.Fd(1), a.Uint(2));
                break;
            case WlKeyboard.Event.Enter:
                _keyboardWindow = _bySurface.GetValueOrDefault(a.Object(1));
                _keyboardWindow?.OnFocus(true);
                break;
            case WlKeyboard.Event.Leave:
                _keyboardWindow?.OnFocus(false);
                _keyboardWindow = null;
                _repeatCode = 0;
                break;
            case WlKeyboard.Event.Key:
                OnKey(a.Uint(2) + 8, a.Uint(3) == WlKeyboard.KeyStateEnum.Pressed);
                break;
            case WlKeyboard.Event.Modifiers:
                _xkb?.UpdateMask(a.Uint(1), a.Uint(2), a.Uint(3), a.Uint(4));
                break;
            case WlKeyboard.Event.RepeatInfo:
                _repeatRate = a.Int(0);
                _repeatDelay = a.Int(1);
                break;
        }
    }

    private void LoadKeymap(int fd, uint size)
    {
        try
        {
            var map = Libc.Mmap(null, size, Libc.PROT_READ, Libc.MAP_PRIVATE, fd, 0);
            if (map == (void*)-1)
            {
                return;
            }

            var text = Encoding.UTF8.GetString((byte*)map, (int)size).TrimEnd('\0');
            _ = Libc.Munmap(map, size);
            _xkb?.Dispose();
            _xkb = XkbKeyboard.FromText(text);
        }
        finally
        {
            _ = Libc.Close(fd);
        }
    }

    private void OnKey(uint code, bool down)
    {
        if (_keyboardWindow is not { } w || _xkb is null)
        {
            return;
        }

        var key = XkbKeyboard.KeyFor(code);
        var mods = _xkb.Modifiers;
        if (!down)
        {
            if (_repeatCode == code)
            {
                _repeatCode = 0;
            }

            App.Post(new PlatformEvent { Kind = PlatformEventKind.KeyUp, WindowId = w.Id, Key = key, Modifiers = mods });
            return;
        }

        PostKeyDown(w, code, key, mods, repeat: false);
        _repeatCode = code;
        _repeatNext = Environment.TickCount64 + _repeatDelay;
    }

    private void PostKeyDown(WaylandWindow w, uint code, Key key, KeyModifiers mods, bool repeat)
    {
        App.Post(new PlatformEvent { Kind = PlatformEventKind.KeyDown, WindowId = w.Id, Key = key, Modifiers = mods, IsRepeat = repeat });
        var text = _xkb!.TextFor(code);
        if (text is not null && (mods & (KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Meta)) == 0)
        {
            App.Post(new PlatformEvent { Kind = PlatformEventKind.TextInput, WindowId = w.Id, Text = text, Modifiers = mods });
        }
    }

    private void TickRepeat()
    {
        if (!RepeatActive() || _keyboardWindow is not { } w || _xkb is null)
        {
            return;
        }

        var now = Environment.TickCount64;
        var interval = Math.Max(1, 1000 / _repeatRate);
        while (now >= _repeatNext && _repeatCode != 0)
        {
            PostKeyDown(w, _repeatCode, XkbKeyboard.KeyFor(_repeatCode), _xkb.Modifiers, repeat: true);
            _repeatNext += interval;
        }
    }
}

/// <summary>The two Wayland objects that make up a pointer lock; destroy them to unlock.</summary>
internal sealed class WaylandPointerLock
{
    private readonly WlObject _locked;
    private readonly WlObject _relative;

    public WaylandPointerLock(WlObject locked, WlObject relative)
    {
        _locked = locked;
        _relative = relative;
    }

    public void Release()
    {
        _locked.Destroy(ZwpLockedPointerV1.Request.Destroy);
        _relative.Destroy(ZwpRelativePointerV1.Request.Destroy);
    }
}
