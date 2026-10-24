using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace OneGame.Platform;

/// <summary>X11 through Xlib. Pointer and window events come from the X server; text comes from the server's keyboard layout via xkbcommon.</summary>
internal sealed unsafe class X11Backend : PlatformBackend
{
    private static bool s_errorHandlerInstalled;

    private readonly nint _display;
    private readonly int _screen;
    private readonly nuint _root;
    private readonly XkbKeyboard _keyboard;
    private readonly Dictionary<nuint, X11Window> _windows = [];
    private readonly nuint _wmProtocols, _wmDeleteWindow, _netWmName, _utf8String, _netWmState, _netWmStateFullscreen;
    private readonly float _scale;

    public X11Backend(PlatformApp app)
        : base(app)
    {
        try
        {
            _display = OpenDisplay(app.Options.DisplayName);
            if (_display == 0)
            {
                throw new PlatformException("Could not open the X11 display named by DISPLAY.");
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
            _scale = ReadScale();
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

    // Xft.dpi in the X resource database is the desktop's scale setting (96 is 1.0); it may be fractional.
    private float ReadScale()
    {
        var raw = Xlib.ResourceManagerString(_display);
        if (raw == null)
        {
            return 1f;
        }

        var text = Marshal.PtrToStringUTF8((nint)raw) ?? "";
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
            Handle = Xlib.CreateSimpleWindow(d, backend._root, 0, 0, (uint)o.Width, (uint)o.Height, 0, 0, 0);
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
                    | Xlib.PointerMotionMask | Xlib.StructureNotifyMask | Xlib.FocusChangeMask));
            var del = backend._wmDeleteWindow;
            Xlib.SetWMProtocols(d, Handle, &del, 1);
            SetTitle(o.Title);
            if (!o.Resizable)
            {
                var hints = new Xlib.XSizeHints { Flags = (1 << 4) | (1 << 5), MinWidth = o.Width, MinHeight = o.Height, MaxWidth = o.Width, MaxHeight = o.Height };
                Xlib.SetWMNormalHints(d, Handle, &hints);
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

        public void OnConfigure(int width, int height)
        {
            if (width > 0 && height > 0)
            {
                ApplySize(width, height);
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
