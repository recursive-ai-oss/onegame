namespace OneGame.Platform;

/// <summary>What the Win32 message translator needs from the window that owns the messages.</summary>
internal interface IWin32MessageSink
{
    int WindowId { get; }

    /// <summary>The modifier keys held right now (from GetKeyState).</summary>
    KeyModifiers Modifiers { get; }

    void Post(in PlatformEvent e);

    void Resized(int width, int height, bool minimized);

    void DpiChanged(int dpi);

    void FocusChanged(bool focused);

    /// <summary>Asks Windows to send WM_MOUSELEAVE when the pointer leaves, so enter and leave can be reported.</summary>
    void BeginMouseTracking();
}

/// <summary>
/// Turns Win32 window messages into <see cref="PlatformEvent"/>s. It reads only the message numbers and parameters, so it runs and is
/// tested on any operating system; the window procedure feeds it.
/// </summary>
internal sealed class Win32Translator
{
    public const uint WmClose = 0x0010, WmSize = 0x0005, WmSetFocus = 0x0007, WmKillFocus = 0x0008, WmKeyDown = 0x0100, WmKeyUp = 0x0101,
        WmChar = 0x0102, WmSysKeyDown = 0x0104, WmSysKeyUp = 0x0105, WmMouseMove = 0x0200, WmLButtonDown = 0x0201, WmLButtonUp = 0x0202,
        WmRButtonDown = 0x0204, WmRButtonUp = 0x0205, WmMButtonDown = 0x0207, WmMButtonUp = 0x0208, WmMouseWheel = 0x020A,
        WmXButtonDown = 0x020B, WmXButtonUp = 0x020C, WmMouseHWheel = 0x020E, WmMouseLeave = 0x02A3, WmDpiChanged = 0x02E0;

    private const int VkPause = 0x13, SizeMinimized = 1;
    private const float WheelNotch = 120f;

    private readonly IWin32MessageSink _sink;
    private char _highSurrogate;
    private bool _tracking;

    public Win32Translator(IWin32MessageSink sink) => _sink = sink;

    private static int LoWord(nint v) => (short)(v & 0xFFFF);

    private static int HiWord(nint v) => (short)((v >> 16) & 0xFFFF);

    /// <summary>Handles one message. Returns true when the message was fully dealt with and must not go to the default window procedure.</summary>
    public bool Translate(uint message, nuint wParam, nint lParam)
    {
        var id = _sink.WindowId;
        switch (message)
        {
            case WmClose:
                // The game decides whether to close; the window stays until it calls Close.
                _sink.Post(new PlatformEvent { Kind = PlatformEventKind.WindowCloseRequested, WindowId = id });
                return true;
            case WmSize:
                _sink.Resized((int)(lParam & 0xFFFF), (int)((lParam >> 16) & 0xFFFF), (int)wParam == SizeMinimized);
                return true;
            case WmDpiChanged:
                _sink.DpiChanged((int)(wParam & 0xFFFF));
                return true;
            case WmSetFocus:
                _sink.FocusChanged(true);
                return true;
            case WmKillFocus:
                _sink.FocusChanged(false);
                return true;
            case WmKeyDown:
            case WmSysKeyDown:
                PostKey(true, wParam, lParam);
                return message == WmKeyDown;
            case WmKeyUp:
            case WmSysKeyUp:
                PostKey(false, wParam, lParam);
                return message == WmKeyUp;
            case WmChar:
                PostChar((char)(wParam & 0xFFFF));
                return true;
            case WmMouseMove:
                if (!_tracking)
                {
                    _tracking = true;
                    _sink.BeginMouseTracking();
                    _sink.Post(new PlatformEvent { Kind = PlatformEventKind.PointerInside, WindowId = id, Focused = true, X = LoWord(lParam), Y = HiWord(lParam) });
                }

                _sink.Post(new PlatformEvent { Kind = PlatformEventKind.PointerMoved, WindowId = id, X = LoWord(lParam), Y = HiWord(lParam), Modifiers = _sink.Modifiers });
                return true;
            case WmMouseLeave:
                _tracking = false;
                _sink.Post(new PlatformEvent { Kind = PlatformEventKind.PointerInside, WindowId = id, Focused = false });
                return true;
            case WmLButtonDown:
                return Button(MouseButton.Left, true, lParam);
            case WmLButtonUp:
                return Button(MouseButton.Left, false, lParam);
            case WmRButtonDown:
                return Button(MouseButton.Right, true, lParam);
            case WmRButtonUp:
                return Button(MouseButton.Right, false, lParam);
            case WmMButtonDown:
                return Button(MouseButton.Middle, true, lParam);
            case WmMButtonUp:
                return Button(MouseButton.Middle, false, lParam);
            case WmXButtonDown:
            case WmXButtonUp:
                // The high word of wParam says which extra button: 1 is "back", 2 is "forward".
                var which = (int)((wParam >> 16) & 0xFFFF);
                Button(which == 1 ? MouseButton.Back : MouseButton.Forward, message == WmXButtonDown, lParam);
                return true;
            case WmMouseWheel:
            case WmMouseHWheel:
                var notches = (short)((wParam >> 16) & 0xFFFF) / WheelNotch;

                // Wheel messages carry screen coordinates, so report the last known pointer position instead of converting.
                _sink.Post(new PlatformEvent
                {
                    Kind = PlatformEventKind.PointerWheel,
                    WindowId = id,
                    DeltaX = message == WmMouseHWheel ? notches : 0f,
                    DeltaY = message == WmMouseWheel ? notches : 0f,
                    Modifiers = _sink.Modifiers,
                });
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Reads the relative motion out of a RAWINPUT block (WM_INPUT data). Returns false for non-mouse input and absolute devices.
    /// Layout on 64-bit Windows: RAWINPUTHEADER is 24 bytes; RAWMOUSE follows with usFlags at 0 and lLastX, lLastY at 12 and 16.
    /// </summary>
    public static bool TryReadRawMouseDelta(ReadOnlySpan<byte> rawInput, out int dx, out int dy)
    {
        dx = 0;
        dy = 0;
        const int headerSize = 24, mouseSize = 24;
        if (rawInput.Length < headerSize + mouseSize || BitConverter.ToUInt32(rawInput) != 0 /* RIM_TYPEMOUSE */)
        {
            return false;
        }

        var mouse = rawInput[headerSize..];
        if ((BitConverter.ToUInt16(mouse) & 1) != 0 /* MOUSE_MOVE_ABSOLUTE */)
        {
            return false;
        }

        dx = BitConverter.ToInt32(mouse[12..]);
        dy = BitConverter.ToInt32(mouse[16..]);
        return true;
    }

    private bool Button(MouseButton button, bool down, nint lParam)
    {
        _sink.Post(new PlatformEvent
        {
            Kind = down ? PlatformEventKind.PointerButtonDown : PlatformEventKind.PointerButtonUp,
            WindowId = _sink.WindowId,
            Button = button,
            X = LoWord(lParam),
            Y = HiWord(lParam),
            Modifiers = _sink.Modifiers,
        });
        return true;
    }

    private void PostKey(bool down, nuint wParam, nint lParam)
    {
        var scan = (int)((lParam >> 16) & 0xFF);
        var extended = ((lParam >> 24) & 1) != 0;
        var wasDown = ((lParam >> 30) & 1) != 0;
        var key = (int)wParam == VkPause ? Key.Pause : KeyTables.FromWindowsScan(scan, extended);
        _sink.Post(new PlatformEvent
        {
            Kind = down ? PlatformEventKind.KeyDown : PlatformEventKind.KeyUp,
            WindowId = _sink.WindowId,
            Key = key,
            Modifiers = _sink.Modifiers,
            IsRepeat = down && wasDown,
        });
    }

    // WM_CHAR delivers UTF-16 code units; characters outside the BMP arrive as a high then a low surrogate.
    private void PostChar(char c)
    {
        if (char.IsHighSurrogate(c))
        {
            _highSurrogate = c;
            return;
        }

        string text;
        if (char.IsLowSurrogate(c))
        {
            if (_highSurrogate == 0)
            {
                return;
            }

            text = new string([_highSurrogate, c]);
            _highSurrogate = '\0';
        }
        else
        {
            _highSurrogate = '\0';

            // Control characters (Enter, Tab, Backspace, Escape, Ctrl+letter) are keys, not text.
            if (char.IsControl(c))
            {
                return;
            }

            text = c.ToString();
        }

        _sink.Post(new PlatformEvent { Kind = PlatformEventKind.TextInput, WindowId = _sink.WindowId, Text = text, Modifiers = _sink.Modifiers });
    }
}
