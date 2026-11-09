using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace OneGame.Platform;

/// <summary>X11 through Xlib. Pointer and window events come from the X server; text comes from the server's keyboard layout via xkbcommon.</summary>
internal sealed unsafe partial class X11Backend : PlatformBackend
{
    private static bool s_errorHandlerInstalled;

    private readonly nint _display;
    private readonly int _screen;
    private readonly nuint _root;
    private readonly XkbKeyboard _keyboard;
    private readonly Dictionary<nuint, X11Window> _windows = [];
    private readonly nuint _wmProtocols, _wmDeleteWindow, _netWmName, _utf8String, _netWmState, _netWmStateFullscreen,
        _netWmStateMaxHorz, _netWmStateMaxVert, _netWorkArea, _netSupportingWmCheck;
    private float _scale;
    private readonly nuint _resourceManager;

    public X11Backend(PlatformApp app)
        : base(app)
    {
        try
        {
            _display = OpenDisplay(app.Options.DisplayName);
            if (_display == 0)
            {
                throw new PlatformException($"Could not open the X11 display '{app.Options.DisplayName ?? Environment.GetEnvironmentVariable("DISPLAY")}'.");
            }

            if (!s_errorHandlerInstalled)
            {
                // The default handler terminates the process on any protocol error, such as acting on a window that was just destroyed.
                Xlib.SetErrorHandler(&OnXError);
                s_errorHandlerInstalled = true;
            }

            _screen = Xlib.DefaultScreen(_display);
            _root = Xlib.RootWindow(_display, _screen);
            int supported;
            Xlib.XkbSetDetectableAutoRepeat(_display, 1, &supported);
            _keyboard = XkbKeyboard.FromX11(_display);
            _wmProtocols = Atom("WM_PROTOCOLS");
            _wmDeleteWindow = Atom("WM_DELETE_WINDOW");
            _netWmName = Atom("_NET_WM_NAME");
            _utf8String = Atom("UTF8_STRING");
            _netWmState = Atom("_NET_WM_STATE");
            _netWmStateFullscreen = Atom("_NET_WM_STATE_FULLSCREEN");
            _netWmStateMaxHorz = Atom("_NET_WM_STATE_MAXIMIZED_HORZ");
            _netWmStateMaxVert = Atom("_NET_WM_STATE_MAXIMIZED_VERT");
            _netWorkArea = Atom("_NET_WORKAREA");
            _netSupportingWmCheck = Atom("_NET_SUPPORTING_WM_CHECK");
            _resourceManager = Atom("RESOURCE_MANAGER");
            _scale = ReadScale();

            // The desktop's scale setting lives in the root window's RESOURCE_MANAGER property; watch it so a change reaches the windows.
            Xlib.SelectInput(_display, _root, (nint)Xlib.PropertyChangeMask);
            InitClipboard();
        }
        catch (DllNotFoundException e)
        {
            throw new PlatformException("The X11 backend needs libX11 and libxkbcommon: " + e.Message, e);
        }
    }

    public override PlatformBackendKind Kind => PlatformBackendKind.X11;

    [UnmanagedCallersOnly]
    private static int OnXError(nint display, void* errorEvent) => 0;

    public override Window CreateWindow(WindowOptions options, int id)
    {
        var window = new X11Window(this, App, id, options);
        _windows[window.Handle] = window;
        return window;
    }

    public override void Pump(int timeoutMilliseconds)
    {
        Xlib.Flush(_display);
        if (timeoutMilliseconds > 0 && Xlib.Pending(_display) == 0)
        {
            var fd = new Libc.PollFd { Fd = Xlib.ConnectionNumber(_display), Events = Libc.POLLIN };
            Libc.Poll(&fd, 1, timeoutMilliseconds);
        }

        Xlib.XEvent e;
        while (Xlib.Pending(_display) > 0)
        {
            Xlib.NextEvent(_display, &e);
            Handle(&e);
        }
    }

    public override void Dispose()
    {
        foreach (var w in new List<X11Window>(_windows.Values))
        {
            w.Close();
        }

        _keyboard.Dispose();
        if (_display != 0)
        {
            Xlib.CloseDisplay(_display);
        }

        base.Dispose();
    }

    private void Forget(X11Window w) => _windows.Remove(w.Handle);

    private static nint OpenDisplay(string? name)
    {
        if (name is null)
        {
            return Xlib.OpenDisplay(null);
        }

        var bytes = Encoding.UTF8.GetBytes(name + "\0");
        fixed (byte* p = bytes)
        {
            return Xlib.OpenDisplay(p);
        }
    }

    private nuint Atom(string name)
    {
        Span<byte> bytes = stackalloc byte[64];
        var n = Encoding.ASCII.GetBytes(name, bytes);
        bytes[n] = 0;
        fixed (byte* p = bytes)
        {
            return Xlib.InternAtom(_display, p, 0);
        }
    }

    // Xft.dpi in the X resource database is the desktop's scale setting (96 is 1.0); it may be fractional. The database is the root window's
    // RESOURCE_MANAGER property, read fresh each time (XResourceManagerString would return the copy taken when the display was opened).
    private float ReadScale()
    {
        nuint type, after, count;
        int format;
        byte* raw;
        if (Xlib.GetWindowProperty(_display, _root, _resourceManager, 0, 16384, 0, 0, &type, &format, &count, &after, &raw) != 0)
        {
            return 1f;
        }

        try
        {
            if (raw == null || format != 8)
            {
                return 1f;
            }

            var text = Encoding.UTF8.GetString(raw, (int)count);
            foreach (var line in text.Split('\n'))
            {
                if (line.StartsWith("Xft.dpi:", StringComparison.Ordinal)
                    && float.TryParse(line.AsSpan(8).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var dpi)
                    && dpi >= 48f)
                {
                    return dpi / 96f;
                }
            }

            return 1f;
        }
        finally
        {
            if (raw != null)
            {
                Xlib.Free(raw);
            }
        }
    }

    private static KeyModifiers ModifiersFromState(uint state)
    {
        var m = KeyModifiers.None;
        if ((state & 1) != 0)
        {
            m |= KeyModifiers.Shift;
        }

        if ((state & 2) != 0)
        {
            m |= KeyModifiers.CapsLock;
        }

        if ((state & 4) != 0)
        {
            m |= KeyModifiers.Control;
        }

        if ((state & 8) != 0)
        {
            m |= KeyModifiers.Alt;
        }

        if ((state & 16) != 0)
        {
            m |= KeyModifiers.NumLock;
        }

        if ((state & 64) != 0)
        {
            m |= KeyModifiers.Meta;
        }

        return m;
    }

    // Field offsets follow the Xlib event structs on 64-bit Linux: the common header is 40 bytes (type, serial, send_event, display, window).
    private void Handle(Xlib.XEvent* e)
    {
        if (HandleSelectionEvent(e))
        {
            return;
        }

        if (e->Type == Xlib.PropertyNotify && Xlib.At<nuint>(e, 32) == _root && Xlib.At<nuint>(e, 40) == _resourceManager)
        {
            var scale = ReadScale();
            if (scale != _scale)
            {
                _scale = scale;
                foreach (var each in _windows.Values)
                {
                    each.OnScale(scale);
                }
            }

            return;
        }

        var window = Xlib.At<nuint>(e, 32);
        if (!_windows.TryGetValue(window, out var w))
        {
            if (e->Type == Xlib.ConfigureNotify)
            {
                _windows.TryGetValue(Xlib.At<nuint>(e, 40), out w);
            }

            if (w is null)
            {
                return;
            }
        }

        switch (e->Type)
        {
            case Xlib.KeyPress:
            case Xlib.KeyRelease:
                HandleKey(e, w);
                break;
            case Xlib.ButtonPress:
            case Xlib.ButtonRelease:
                HandleButton(e, w);
                break;
            case Xlib.MotionNotify:
                w.OnMotion(Xlib.At<int>(e, 64), Xlib.At<int>(e, 68), ModifiersFromState(Xlib.At<uint>(e, 80)));
                break;
            case Xlib.EnterNotify:
            case Xlib.LeaveNotify:
                // Grab and ungrab crossings happen when the cursor is locked or released; they are not the user moving the pointer.
                if (Xlib.At<int>(e, 80) == 0)
                {
                    App.Post(new PlatformEvent { Kind = PlatformEventKind.PointerInside, WindowId = w.Id, Focused = e->Type == Xlib.EnterNotify, X = Xlib.At<int>(e, 64), Y = Xlib.At<int>(e, 68) });
                }

                break;
            case Xlib.FocusIn:
            case Xlib.FocusOut:
                var detail = Xlib.At<int>(e, 44);
                if (detail is not (5 or 6 or 7))
                {
                    w.OnFocus(e->Type == Xlib.FocusIn);
                }

                break;
            case Xlib.ConfigureNotify:
                w.OnConfigure(Xlib.At<int>(e, 56), Xlib.At<int>(e, 60));
                break;
            case Xlib.PropertyNotify:
                if (Xlib.At<nuint>(e, 40) == _netWmState)
                {
                    w.RefreshState();
                }

                break;
            case Xlib.MapNotify:
                w.OnMapped();
                break;
            case Xlib.UnmapNotify:
                w.OnUnmapped();
                break;
            case Xlib.ClientMessage:
                if (Xlib.At<nuint>(e, 40) == _wmProtocols && Xlib.At<nuint>(e, 56) == _wmDeleteWindow)
                {
                    App.Post(new PlatformEvent { Kind = PlatformEventKind.WindowCloseRequested, WindowId = w.Id });
                }

                break;
        }
    }

    private void HandleKey(Xlib.XEvent* e, X11Window w)
    {
        var code = Xlib.At<uint>(e, 84);
        var mods = ModifiersFromState(Xlib.At<uint>(e, 80));
        var key = XkbKeyboard.KeyFor(code);
        if (e->Type == Xlib.KeyRelease)
        {
            _keyboard.UpdateKey(code, false);
            App.Post(new PlatformEvent { Kind = PlatformEventKind.KeyUp, WindowId = w.Id, Key = key, Modifiers = mods });
            return;
        }

        // Ask for the text before the key changes the state, so Shift+A types "A" and Shift itself types nothing.
        var text = _keyboard.TextFor(code);
        _keyboard.UpdateKey(code, true);
        App.Post(new PlatformEvent
        {
            Kind = PlatformEventKind.KeyDown,
            WindowId = w.Id,
            Key = key,
            Modifiers = mods,
            IsRepeat = App.Input.IsKeyDown(key),
        });

        if (text is not null && (mods & (KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Meta)) == 0)
        {
            App.Post(new PlatformEvent { Kind = PlatformEventKind.TextInput, WindowId = w.Id, Text = text, Modifiers = mods });
        }
    }

    private void HandleButton(Xlib.XEvent* e, X11Window w)
    {
        var button = Xlib.At<uint>(e, 84);
        var down = e->Type == Xlib.ButtonPress;
        var mods = ModifiersFromState(Xlib.At<uint>(e, 80));
        var x = Xlib.At<int>(e, 64);
        var y = Xlib.At<int>(e, 68);

        // Buttons 4-7 are wheel notches, reported as presses.
        if (button is >= 4 and <= 7)
        {
            if (down)
            {
                var (dx, dy) = button switch { 4 => (0f, 1f), 5 => (0f, -1f), 6 => (-1f, 0f), _ => (1f, 0f) };
                App.Post(new PlatformEvent { Kind = PlatformEventKind.PointerWheel, WindowId = w.Id, DeltaX = dx, DeltaY = dy, X = x, Y = y, Modifiers = mods });
            }

            return;
        }

        MouseButton? mapped = button switch
        {
            1 => MouseButton.Left,
            2 => MouseButton.Middle,
            3 => MouseButton.Right,
            8 => MouseButton.Back,
            9 => MouseButton.Forward,
            _ => null,
        };
        if (mapped is { } b)
        {
            App.Post(new PlatformEvent
            {
                Kind = down ? PlatformEventKind.PointerButtonDown : PlatformEventKind.PointerButtonUp,
                WindowId = w.Id,
                Button = b,
                X = x,
                Y = y,
                Modifiers = mods,
            });
        }
    }

    // EWMH _NET_WORKAREA: the screen minus panels, one rectangle per desktop; the first desktop is the one that matters here.
    // There is no RandR use, so the screen counts as a single monitor.
    public override IReadOnlyList<ScreenRect> WorkAreas()
    {
        nuint type;
        int format;
        nuint count, after;
        byte* data;
        if (Xlib.GetWindowProperty(_display, _root, _netWorkArea, 0, 4, 0, 6 /* XA_CARDINAL */, &type, &format, &count, &after, &data) == 0 && data != null)
        {
            try
            {
                if (format == 32 && count >= 4)
                {
                    var v = (nint*)data;
                    if (v[2] > 0 && v[3] > 0 && v[2] < 1_000_000 && v[3] < 1_000_000)
                    {
                        return [new ScreenRect((int)v[0], (int)v[1], (int)v[2], (int)v[3])];
                    }
                }
            }
            finally
            {
                Xlib.Free(data);
            }
        }

        return [new ScreenRect(0, 0, Xlib.DisplayWidth(_display, _screen), Xlib.DisplayHeight(_display, _screen))];
    }

    // EWMH: a running window manager announces itself with _NET_SUPPORTING_WM_CHECK on the root window.
    private bool HasWindowManager()
    {
        nuint type, count, after;
        int format;
        byte* data;
        var found = Xlib.GetWindowProperty(_display, _root, _netSupportingWmCheck, 0, 1, 0, 33 /* XA_WINDOW */, &type, &format, &count, &after, &data) == 0 && data != null && count > 0;
        if (data != null)
        {
            Xlib.Free(data);
        }

        return found;
    }

    private sealed class X11Window : Window
    {
        private readonly X11Backend _b;
        private nuint _blankCursor;
        private bool _gotFocus;
        private int _lockCenterX, _lockCenterY;
        private bool _hiddenBeforeLock;

        public X11Window(X11Backend backend, PlatformApp app, int id, WindowOptions o)
            : base(app, id)
        {
            _b = backend;
            var d = backend._display;
            var at = o.Placement is { HasPosition: true } saved ? saved : (WindowPlacement?)null;
            Handle = Xlib.CreateSimpleWindow(d, backend._root, at?.X ?? 0, at?.Y ?? 0, (uint)o.Width, (uint)o.Height, 0, 0, 0);
            if (Handle == 0)
            {
                throw new PlatformException("The X server refused to create a window.");
            }

            Width = o.Width;
            Height = o.Height;
            Scale = backend._scale;
            Xlib.SelectInput(
                d,
                Handle,
                (nint)(Xlib.KeyPressMask | Xlib.KeyReleaseMask | Xlib.ButtonPressMask | Xlib.ButtonReleaseMask | Xlib.EnterWindowMask | Xlib.LeaveWindowMask
                    | Xlib.PointerMotionMask | Xlib.StructureNotifyMask | Xlib.FocusChangeMask | Xlib.PropertyChangeMask));
            var del = backend._wmDeleteWindow;
            Xlib.SetWMProtocols(d, Handle, &del, 1);
            SetTitle(o.Title);
            if (!o.Resizable || at is not null)
            {
                // USPosition asks the window manager to honour the position, and StaticGravity makes it the client area's corner rather
                // than the frame's (ICCCM 4.1.2.3), so a position read back from one window puts the next one in the same place.
                var hints = new Xlib.XSizeHints();
                if (at is { } place)
                {
                    hints.Flags |= (1 << 0) | (1 << 9);
                    hints.X = place.X;
                    hints.Y = place.Y;
                    hints.WinGravity = 10;
                }

                if (!o.Resizable)
                {
                    hints.Flags |= (1 << 4) | (1 << 5);
                    hints.MinWidth = o.Width;
                    hints.MinHeight = o.Height;
                    hints.MaxWidth = o.Width;
                    hints.MaxHeight = o.Height;
                }

                Xlib.SetWMNormalHints(d, Handle, &hints);
            }

            InitNormalRect(at?.X ?? 0, at?.Y ?? 0, o.Width, o.Height);
            if (o.Placement?.State == WindowState.Maximized && backend.HasWindowManager())
            {
                // A window that is not mapped yet takes its initial state from the property itself (EWMH, _NET_WM_STATE).
                var initial = stackalloc nuint[2] { backend._netWmStateMaxHorz, backend._netWmStateMaxVert };
                Xlib.ChangeProperty(d, Handle, backend._netWmState, 4 /* XA_ATOM */, 32, Xlib.PropModeReplace, (byte*)initial, 2);
                IsMaximized = true;
            }

            Xlib.MapWindow(d, Handle);
            if (o.Fullscreen)
            {
                SetFullscreen(true);
            }

            Xlib.Flush(d);
        }

        public nuint Handle { get; }

        public override NativeWindowHandle NativeHandle => new(NativeWindowKind.X11, _b._display, (nint)Handle);

        public override void SetTitle(string title)
        {
            Title = title;
            var bytes = Encoding.UTF8.GetBytes(title);
            fixed (byte* p = bytes)
            {
                Xlib.ChangeProperty(_b._display, Handle, _b._netWmName, _b._utf8String, 8, Xlib.PropModeReplace, p, bytes.Length);
                Xlib.ChangeProperty(_b._display, Handle, 39 /* XA_WM_NAME */, 31 /* XA_STRING */, 8, Xlib.PropModeReplace, p, bytes.Length);
            }

            Xlib.Flush(_b._display);
        }

        public override void RequestSize(int width, int height)
        {
            Xlib.ResizeWindow(_b._display, Handle, (uint)Math.Max(1, width), (uint)Math.Max(1, height));
            Xlib.Flush(_b._display);
        }

        public override bool CanPlace => true;

        // EWMH _NET_WM_STATE client message (action 0 remove, 1 add) to the root window; the window manager acts on it and updates the
        // property, which RefreshState reads back. Without a window manager nothing changes.
        private void SendState(int action, nuint first, nuint second)
        {
            Xlib.XEvent ev = default;
            ev.Type = Xlib.ClientMessage;
            *(int*)((byte*)&ev + 16) = 1; // send_event
            *(nint*)((byte*)&ev + 24) = _b._display;
            *(nuint*)((byte*)&ev + 32) = Handle;
            *(nuint*)((byte*)&ev + 40) = _b._netWmState;
            *(int*)((byte*)&ev + 48) = 32;
            *(nint*)((byte*)&ev + 56) = action;
            *(nuint*)((byte*)&ev + 64) = first;
            *(nuint*)((byte*)&ev + 72) = second;
            *(nint*)((byte*)&ev + 80) = 1; // source indication: a normal application
            Xlib.SendEvent(_b._display, _b._root, 0, (nint)(Xlib.SubstructureNotifyMask | Xlib.SubstructureRedirectMask), &ev);
            Xlib.Flush(_b._display);
        }

        public override void Maximize()
        {
            if (IsFullscreen)
            {
                SetFullscreen(false);
            }

            SendState(1, _b._netWmStateMaxHorz, _b._netWmStateMaxVert);
        }

        public override void Restore()
        {
            if (IsFullscreen)
            {
                SetFullscreen(false);
            }

            SendState(0, _b._netWmStateMaxHorz, _b._netWmStateMaxVert);
        }

        public override void MoveTo(int x, int y)
        {
            Xlib.MoveWindow(_b._display, Handle, x, y);
            Xlib.Flush(_b._display);
        }

        private protected override void PlaceNormal(WindowPlacement placement)
        {
            var w = (uint)Math.Max(1, placement.Width);
            var h = (uint)Math.Max(1, placement.Height);
            if (placement.HasPosition)
            {
                Xlib.MoveResizeWindow(_b._display, Handle, placement.X, placement.Y, w, h);
            }
            else
            {
                Xlib.ResizeWindow(_b._display, Handle, w, h);
            }

            Xlib.Flush(_b._display);
        }

        // Reads the window manager's view of this window: maximized when both directions are set.
        public void RefreshState()
        {
            nuint type, count, after;
            int format;
            byte* data;
            var horz = false;
            var vert = false;
            if (Xlib.GetWindowProperty(_b._display, Handle, _b._netWmState, 0, 32, 0, 4 /* XA_ATOM */, &type, &format, &count, &after, &data) == 0 && data != null)
            {
                if (format == 32)
                {
                    var atoms = (nuint*)data;
                    for (nuint i = 0; i < count && i < 32; i++)
                    {
                        horz |= atoms[i] == _b._netWmStateMaxHorz;
                        vert |= atoms[i] == _b._netWmStateMaxVert;
                    }
                }

                Xlib.Free(data);
            }

            IsMaximized = horz && vert;
        }

        public override void SetFullscreen(bool fullscreen)
        {
            // EWMH: ask the window manager through a client message to the root window. Without a window manager nothing changes.
            Xlib.XEvent ev = default;
            ev.Type = Xlib.ClientMessage;
            *(int*)((byte*)&ev + 16) = 1; // send_event
            *(nint*)((byte*)&ev + 24) = _b._display;
            *(nuint*)((byte*)&ev + 32) = Handle;
            *(nuint*)((byte*)&ev + 40) = _b._netWmState;
            *(int*)((byte*)&ev + 48) = 32;
            *(nint*)((byte*)&ev + 56) = fullscreen ? 1 : 0;
            *(nuint*)((byte*)&ev + 64) = _b._netWmStateFullscreen;
            *(nint*)((byte*)&ev + 88) = 1;
            Xlib.SendEvent(_b._display, _b._root, 0, (nint)(Xlib.SubstructureNotifyMask | Xlib.SubstructureRedirectMask), &ev);
            Xlib.Flush(_b._display);
            IsFullscreen = fullscreen;
            if (fullscreen)
            {
                IsMaximized = false;
            }
        }

        public override void Minimize()
        {
            Xlib.IconifyWindow(_b._display, Handle, _b._screen);
            Xlib.Flush(_b._display);
        }

        public override void SetCursorHidden(bool hidden)
        {
            if (hidden == CursorHidden)
            {
                return;
            }

            CursorHidden = hidden;
            ApplyCursor();
        }

        public override void SetCursorLocked(bool locked)
        {
            if (locked == CursorLocked)
            {
                return;
            }

            var d = _b._display;
            CursorLocked = locked;
            if (locked)
            {
                _hiddenBeforeLock = CursorHidden;
                CursorHidden = true;
                // X has no relative-pointer lock without extensions, so confine the pointer to the window, keep it at the centre and
                // report how far each motion moved it from there.
                _lockCenterX = Width / 2;
                _lockCenterY = Height / 2;
                EnsureBlankCursor();
                Xlib.GrabPointer(d, Handle, 1, (uint)(Xlib.ButtonPressMask | Xlib.ButtonReleaseMask | Xlib.PointerMotionMask), Xlib.GrabModeAsync, Xlib.GrabModeAsync, Handle, _blankCursor, 0);
                Xlib.WarpPointer(d, 0, Handle, 0, 0, 0, 0, _lockCenterX, _lockCenterY);
            }
            else
            {
                Xlib.UngrabPointer(d, 0);
                CursorHidden = _hiddenBeforeLock;
            }

            ApplyCursor();
        }

        public void OnMotion(int x, int y, KeyModifiers mods)
        {
            if (CursorLocked)
            {
                var dx = x - _lockCenterX;
                var dy = y - _lockCenterY;
                if (dx != 0 || dy != 0)
                {
                    App.Post(new PlatformEvent { Kind = PlatformEventKind.PointerRelativeMotion, WindowId = Id, DeltaX = dx, DeltaY = dy });
                    Xlib.WarpPointer(_b._display, 0, Handle, 0, 0, 0, 0, _lockCenterX, _lockCenterY);
                    Xlib.Flush(_b._display);
                }

                return;
            }

            App.Post(new PlatformEvent { Kind = PlatformEventKind.PointerMoved, WindowId = Id, X = x, Y = y, Modifiers = mods });
        }

        public void OnFocus(bool focused) => ApplyFocus(focused);

        public void OnScale(float scale) => ApplyScale(scale);

        public void OnConfigure(int width, int height)
        {
            // The state and the position are asked of the server now, which can be ahead of this event: a move and a maximize sent close
            // together give two events, and by the first one the window is already maximized. An event whose size is no longer the
            // window's size is stale, and the one that follows describes the window, so a stale one never updates the normal rectangle.
            RefreshState();
            nuint root, child;
            int gx, gy, x, y;
            uint gw, gh, border, depth;
            var stale = Xlib.GetGeometry(_b._display, Handle, &root, &gx, &gy, &gw, &gh, &border, &depth) != 0 && (gw != (uint)width || gh != (uint)height);
            if (Xlib.TranslateCoordinates(_b._display, Handle, _b._root, 0, 0, &x, &y, &child) != 0)
            {
                SetPosition(x, y, !stale);
            }

            if (width > 0 && height > 0)
            {
                ApplySize(width, height, !stale);
                if (CursorLocked)
                {
                    _lockCenterX = width / 2;
                    _lockCenterY = height / 2;
                }
            }
        }

        public void OnMapped()
        {
            IsMinimized = false;
            if (!_gotFocus)
            {
                // With no window manager nobody hands out keyboard focus, so take it once the window is visible.
                _gotFocus = true;
                Xlib.SetInputFocus(_b._display, Handle, 2 /* RevertToParent */, 0);
            }
        }

        public void OnUnmapped() => IsMinimized = true;

        private void EnsureBlankCursor()
        {
            if (_blankCursor != 0)
            {
                return;
            }

            var d = _b._display;
            var pixmap = Xlib.CreatePixmap(d, _b._root, 1, 1, 1);
            var color = default(Xlib.XColor);
            _blankCursor = Xlib.CreatePixmapCursor(d, pixmap, pixmap, &color, &color, 0, 0);
            Xlib.FreePixmap(d, pixmap);
        }

        private void ApplyCursor()
        {
            if (CursorHidden || CursorLocked)
            {
                EnsureBlankCursor();
                Xlib.DefineCursor(_b._display, Handle, _blankCursor);
            }
            else
            {
                Xlib.DefineCursor(_b._display, Handle, 0);
            }

            Xlib.Flush(_b._display);
        }

        private protected override void DestroyNative()
        {
            var d = _b._display;
            if (CursorLocked)
            {
                Xlib.UngrabPointer(d, 0);
            }

            if (_blankCursor != 0)
            {
                Xlib.FreeCursor(d, _blankCursor);
            }

            Xlib.DestroyWindow(d, Handle);
            Xlib.Flush(d);
            _b.Forget(this);
        }
    }
}
