using System.Runtime.InteropServices;

namespace OneGame.Platform;

/// <summary>Win32: one window class, windows driven by a message pump on the thread that created them.</summary>
internal sealed unsafe class Win32Backend : PlatformBackend
{
    private const string ClassName = "OneGameWindow";
    private const nint IdcArrow = 32512;

    private readonly nint _instance;

    public Win32Backend(PlatformApp app)
        : base(app)
    {
        // Per-monitor v2 awareness makes Windows report real pixels and send WM_DPICHANGED instead of scaling the window bitmap.
        Win32Native.SetProcessDpiAwarenessContext(Win32Native.DpiAwarenessPerMonitorV2);
        _instance = Win32Native.GetModuleHandle(0);
        Arrow = Win32Native.LoadCursor(0, IdcArrow);

        var wc = new Win32Native.WndClassEx
        {
            Size = (uint)sizeof(Win32Native.WndClassEx),
            WndProc = &WindowProc,
            Instance = _instance,
            Cursor = Arrow,
            ClassName = Marshal.StringToHGlobalUni(ClassName),
        };
        if (Win32Native.RegisterClassEx(&wc) == 0 && Marshal.GetLastPInvokeError() != 1410 /* ERROR_CLASS_ALREADY_EXISTS */)
        {
            throw new PlatformException("Could not register the window class (error " + Marshal.GetLastPInvokeError() + ").");
        }
    }

    public override PlatformBackendKind Kind => PlatformBackendKind.Win32;

    internal static string WindowClassName => ClassName;

    internal nint Instance => _instance;

    internal nint Arrow { get; }

    internal static Exception? PendingException { get; set; }

    public override Window CreateWindow(WindowOptions options, int id) => new Win32Window(this, App, id, options);

    // CF_UNICODETEXT (Microsoft Learn, "Standard Clipboard Formats"): UTF-16 with CR LF line ends and a terminating NUL, in a movable global block.
    public override bool ClipboardAvailable => App.Windows.Count > 0;

    private nint ClipboardOwner => App.Windows.Count > 0 ? App.Windows[0].NativeHandle.Window : 0;

    // Another program may hold the clipboard open for a moment, so opening is retried briefly.
    private bool OpenClipboardWithRetry()
    {
        for (var i = 0; i < 10; i++)
        {
            if (Win32Native.OpenClipboard(ClipboardOwner) != 0)
            {
                return true;
            }

            System.Threading.Thread.Sleep(5);
        }

        return false;
    }

    public override string? ClipboardGet(ClipboardSelection selection)
    {
        if (!OpenClipboardWithRetry())
        {
            return null;
        }

        try
        {
            if (Win32Native.IsClipboardFormatAvailable(Win32Native.CfUnicodeText) == 0)
            {
                return null;
            }

            var handle = Win32Native.GetClipboardData(Win32Native.CfUnicodeText);
            if (handle == 0)
            {
                return null;
            }

            var size = Win32Native.GlobalSize(handle);
            if (size > (nuint)PlatformClipboard.MaxTextBytes * 2)
            {
                return null;
            }

            var p = (char*)Win32Native.GlobalLock(handle);
            if (p == null)
            {
                return null;
            }

            try
            {
                var span = new ReadOnlySpan<char>(p, (int)(size / 2));
                var nul = span.IndexOf('\0');
                return ClipboardText.FromCrLf(new string(nul >= 0 ? span[..nul] : span));
            }
            finally
            {
                _ = Win32Native.GlobalUnlock(handle);
            }
        }
        finally
        {
            _ = Win32Native.CloseClipboard();
        }
    }

    public override bool ClipboardSet(ClipboardSelection selection, string text)
    {
        var wide = ClipboardText.ToCrLf(text);
        var bytes = (nuint)((wide.Length + 1) * 2);
        var memory = Win32Native.GlobalAlloc(Win32Native.GmemMoveable, bytes);
        if (memory == 0)
        {
            return false;
        }

        var p = (char*)Win32Native.GlobalLock(memory);
        if (p == null)
        {
            _ = Win32Native.GlobalFree(memory);
            return false;
        }

        wide.AsSpan().CopyTo(new Span<char>(p, wide.Length));
        p[wide.Length] = '\0';
        _ = Win32Native.GlobalUnlock(memory);
        if (!OpenClipboardWithRetry())
        {
            _ = Win32Native.GlobalFree(memory);
            return false;
        }

        try
        {
            _ = Win32Native.EmptyClipboard();
            // On success the system owns the block; on failure it is still ours to free.
            if (Win32Native.SetClipboardData(Win32Native.CfUnicodeText, memory) == 0)
            {
                _ = Win32Native.GlobalFree(memory);
                return false;
            }

            return true;
        }
        finally
        {
            _ = Win32Native.CloseClipboard();
        }
    }

    public override void Pump(int timeoutMilliseconds)
    {
        Win32Native.Msg msg;
        if (timeoutMilliseconds > 0 && Win32Native.PeekMessage(&msg, 0, 0, 0, 0 /* PM_NOREMOVE */) == 0)
        {
            Win32Native.MsgWaitForMultipleObjects(0, null, 0, (uint)timeoutMilliseconds, Win32Native.QsAllInput);
        }

        while (Win32Native.PeekMessage(&msg, 0, 0, 0, Win32Native.PmRemove) != 0)
        {
            Win32Native.TranslateMessage(&msg);
            Win32Native.DispatchMessage(&msg);
        }

        if (PendingException is { } e)
        {
            PendingException = null;
            throw new PlatformException("A window message handler failed: " + e.Message, e);
        }
    }

    [UnmanagedCallersOnly]
    private static nint WindowProc(nint hwnd, uint message, nuint wParam, nint lParam)
    {
        var data = Win32Native.GetWindowLongPtr(hwnd, Win32Native.GwlpUserData);
        if (data != 0)
        {
            try
            {
                if (GCHandle.FromIntPtr(data).Target is Win32Window window && window.OnMessage(message, wParam, lParam, out var result))
                {
                    return result;
                }
            }
            catch (Exception e)
            {
                PendingException = e;
            }
        }

        return Win32Native.DefWindowProc(hwnd, message, wParam, lParam);
    }
}

/// <summary>A Win32 top-level window.</summary>
internal sealed unsafe class Win32Window : Window, IWin32MessageSink
{
    private const int VkShift = 0x10, VkControl = 0x11, VkMenu = 0x12, VkCapital = 0x14, VkNumLock = 0x90, VkLWin = 0x5B, VkRWin = 0x5C;

    private readonly Win32Backend _b;
    private readonly Win32Translator _translator;
    private readonly nint _hwnd;
    private GCHandle _handle;
    private uint _style;
    private Win32Native.Rect _windowedRect;
    private uint _windowedStyle;
    private bool _hiddenBeforeLock;

    public Win32Window(Win32Backend backend, PlatformApp app, int id, WindowOptions o)
        : base(app, id)
    {
        _b = backend;
        _translator = new Win32Translator(this);
        _style = Win32Native.WsOverlappedWindow;
        if (!o.Resizable)
        {
            _style &= ~(Win32Native.WsThickFrame | Win32Native.WsMaximizeBox);
        }

        var outer = new Win32Native.Rect { Right = o.Width, Bottom = o.Height };
        Win32Native.AdjustWindowRectEx(&outer, _style, 0, 0);
        _hwnd = Win32Native.CreateWindowEx(
            0, Win32Backend.WindowClassName, o.Title, _style, unchecked((int)0x80000000) /* CW_USEDEFAULT */, unchecked((int)0x80000000), outer.Right - outer.Left, outer.Bottom - outer.Top, 0, 0, backend.Instance, 0);
        if (_hwnd == 0)
        {
            throw new PlatformException("CreateWindowEx failed (error " + Marshal.GetLastPInvokeError() + ").");
        }

        _handle = GCHandle.Alloc(this);
        Win32Native.SetWindowLongPtr(_hwnd, Win32Native.GwlpUserData, GCHandle.ToIntPtr(_handle));
        Title = o.Title;
        Width = o.Width;
        Height = o.Height;
        var dpi = Win32Native.GetDpiForWindow(_hwnd);
        Scale = dpi == 0 ? 1f : dpi / 96f;
        Win32Native.ShowWindow(_hwnd, Win32Native.SwShow);
        Win32Native.SetFocus(_hwnd);
        if (o.Fullscreen)
        {
            SetFullscreen(true);
        }
    }

    public int WindowId => Id;

    public KeyModifiers Modifiers
    {
        get
        {
            var m = KeyModifiers.None;
            if (Down(VkShift))
            {
                m |= KeyModifiers.Shift;
            }

            if (Down(VkControl))
            {
                m |= KeyModifiers.Control;
            }

            if (Down(VkMenu))
            {
                m |= KeyModifiers.Alt;
            }

            if (Down(VkLWin) || Down(VkRWin))
            {
                m |= KeyModifiers.Meta;
            }

            if ((Win32Native.GetKeyState(VkCapital) & 1) != 0)
            {
                m |= KeyModifiers.CapsLock;
            }

            if ((Win32Native.GetKeyState(VkNumLock) & 1) != 0)
            {
                m |= KeyModifiers.NumLock;
            }

            return m;
        }
    }

    public override NativeWindowHandle NativeHandle => new(NativeWindowKind.Win32, _b.Instance, _hwnd);

    void IWin32MessageSink.Post(in PlatformEvent e) => App.Post(e);

    public void Resized(int width, int height, bool minimized)
    {
        IsMinimized = minimized;
        if (!minimized && width > 0 && height > 0)
        {
            ApplySize(width, height);
        }
    }

    public void DpiChanged(int dpi) => ApplyScale(dpi / 96f);

    public void FocusChanged(bool focused) => ApplyFocus(focused);

    public void BeginMouseTracking()
    {
        var track = new Win32Native.TrackMouseEvent { Size = (uint)sizeof(Win32Native.TrackMouseEvent), Flags = Win32Native.TmeLeave, Hwnd = _hwnd };
        Win32Native.TrackMouse(&track);
    }

    /// <summary>Handles a window message; returns true with the result when the default procedure must be skipped.</summary>
    public bool OnMessage(uint message, nuint wParam, nint lParam, out nint result)
    {
        result = 0;
        switch (message)
        {
            case Win32Native.WmSetCursor when (lParam & 0xFFFF) == Win32Native.HtClient:
                Win32Native.SetCursor(CursorHidden ? 0 : _b.Arrow);
                result = 1;
                return true;
            case Win32Native.WmInput:
                if (CursorLocked)
                {
                    ReadRawMouse(lParam);
                }

                return false; // WM_INPUT must still reach DefWindowProc so the system can release the data.
            default:
                return _translator.Translate(message, wParam, lParam);
        }
    }

    public override void SetTitle(string title)
    {
        Title = title;
        Win32Native.SetWindowText(_hwnd, title);
    }

    public override void RequestSize(int width, int height)
    {
        var outer = new Win32Native.Rect { Right = width, Bottom = height };
        Win32Native.AdjustWindowRectEx(&outer, _style, 0, 0);
        Win32Native.SetWindowPos(_hwnd, 0, 0, 0, outer.Right - outer.Left, outer.Bottom - outer.Top, Win32Native.SwpNoMove | Win32Native.SwpNoZOrder | Win32Native.SwpNoActivate);
    }

    public override void SetFullscreen(bool fullscreen)
    {
        if (fullscreen == IsFullscreen)
        {
            return;
        }

        if (fullscreen)
        {
            fixed (Win32Native.Rect* r = &_windowedRect)
            {
                Win32Native.GetWindowRect(_hwnd, r);
            }

            _windowedStyle = _style;
            var info = new Win32Native.MonitorInfo { Size = (uint)sizeof(Win32Native.MonitorInfo) };
            Win32Native.GetMonitorInfo(Win32Native.MonitorFromWindow(_hwnd, 2 /* MONITOR_DEFAULTTONEAREST */), &info);

            // A borderless popup covering the monitor: the window manager has no separate fullscreen mode on Windows.
            _style = Win32Native.WsPopup | Win32Native.WsVisible;
            Win32Native.SetWindowLongPtr(_hwnd, Win32Native.GwlStyle, (nint)_style);
            Win32Native.SetWindowPos(_hwnd, 0, info.Monitor.Left, info.Monitor.Top, info.Monitor.Right - info.Monitor.Left, info.Monitor.Bottom - info.Monitor.Top, Win32Native.SwpNoZOrder | Win32Native.SwpFrameChanged);
        }
        else
        {
            _style = _windowedStyle;
            Win32Native.SetWindowLongPtr(_hwnd, Win32Native.GwlStyle, (nint)(_style | Win32Native.WsVisible));
            Win32Native.SetWindowPos(_hwnd, 0, _windowedRect.Left, _windowedRect.Top, _windowedRect.Right - _windowedRect.Left, _windowedRect.Bottom - _windowedRect.Top, Win32Native.SwpNoZOrder | Win32Native.SwpFrameChanged);
        }

        IsFullscreen = fullscreen;
    }

    public override void Minimize() => Win32Native.ShowWindow(_hwnd, Win32Native.SwMinimize);

    public override void SetCursorHidden(bool hidden)
    {
        CursorHidden = hidden;
        Win32Native.SetCursor(hidden ? 0 : _b.Arrow);
    }

    public override void SetCursorLocked(bool locked)
    {
        if (locked == CursorLocked)
        {
            return;
        }

        CursorLocked = locked;
        if (locked)
        {
            _hiddenBeforeLock = CursorHidden;
            CursorHidden = true;
            Win32Native.SetCursor(0);

            // Pin the cursor to one pixel at the centre; raw input still reports every movement of the mouse.
            var centre = new Win32Native.Point { X = Width / 2, Y = Height / 2 };
            Win32Native.ClientToScreen(_hwnd, &centre);
            var clip = new Win32Native.Rect { Left = centre.X, Top = centre.Y, Right = centre.X + 1, Bottom = centre.Y + 1 };
            Win32Native.ClipCursor(&clip);
            var device = new Win32Native.RawInputDevice { UsagePage = 1, Usage = 2, Target = _hwnd };
            Win32Native.RegisterRawInputDevices(&device, 1, (uint)sizeof(Win32Native.RawInputDevice));
        }
        else
        {
            Win32Native.ClipCursor(null);
            var device = new Win32Native.RawInputDevice { UsagePage = 1, Usage = 2, Flags = Win32Native.RidevRemove };
            Win32Native.RegisterRawInputDevices(&device, 1, (uint)sizeof(Win32Native.RawInputDevice));
            CursorHidden = _hiddenBeforeLock;
            Win32Native.SetCursor(CursorHidden ? 0 : _b.Arrow);
        }
    }

    private protected override void DestroyNative()
    {
        if (CursorLocked)
        {
            SetCursorLocked(false);
        }

        Win32Native.SetWindowLongPtr(_hwnd, Win32Native.GwlpUserData, 0);
        Win32Native.DestroyWindow(_hwnd);
        if (_handle.IsAllocated)
        {
            _handle.Free();
        }
    }

    private static bool Down(int vk) => (Win32Native.GetKeyState(vk) & 0x8000) != 0;

    private void ReadRawMouse(nint lParam)
    {
        uint size = 0;
        const uint headerSize = 24;
        if (Win32Native.GetRawInputData(lParam, Win32Native.RidInput, null, &size, headerSize) != 0 || size == 0 || size > 1024)
        {
            return;
        }

        Span<byte> buffer = stackalloc byte[(int)size];
        fixed (byte* p = buffer)
        {
            if (Win32Native.GetRawInputData(lParam, Win32Native.RidInput, p, &size, headerSize) != size)
            {
                return;
            }
        }

        if (Win32Translator.TryReadRawMouseDelta(buffer, out var dx, out var dy) && (dx != 0 || dy != 0))
        {
            App.Post(new PlatformEvent { Kind = PlatformEventKind.PointerRelativeMotion, WindowId = Id, DeltaX = dx, DeltaY = dy });
        }
    }
}
