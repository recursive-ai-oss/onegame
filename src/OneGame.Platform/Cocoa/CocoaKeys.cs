namespace OneGame.Platform.Cocoa;

/// <summary>Maps macOS virtual key codes to physical keys. The codes name key positions on an ANSI keyboard (Carbon HIToolbox <c>Events.h</c>, the <c>kVK_*</c> constants), whatever the active layout types.</summary>
internal static class CocoaKeys
{
    private static readonly Key[] Table = Build();

    /// <summary>The key for a virtual key code, or <see cref="Key.Unknown"/>.</summary>
    public static Key FromVirtualKey(int code) => (uint)code < (uint)Table.Length ? Table[code] : Key.Unknown;

    /// <summary>Whether the code is one of the modifier keys, which arrive as flags-changed events rather than key down and up.</summary>
    public static bool IsModifier(Key key) =>
        key is Key.LeftShift or Key.RightShift or Key.LeftControl or Key.RightControl or Key.LeftAlt or Key.RightAlt or Key.LeftMeta or Key.RightMeta or Key.CapsLock;

    private static Key[] Build()
    {
        var t = new Key[0x80];
        void Set(int code, Key key) => t[code] = key;

        // Letters, in the order of the kVK_ANSI_* constants.
        Set(0x00, Key.A); Set(0x01, Key.S); Set(0x02, Key.D); Set(0x03, Key.F); Set(0x04, Key.H); Set(0x05, Key.G);
        Set(0x06, Key.Z); Set(0x07, Key.X); Set(0x08, Key.C); Set(0x09, Key.V); Set(0x0A, Key.IntlBackslash); Set(0x0B, Key.B);
        Set(0x0C, Key.Q); Set(0x0D, Key.W); Set(0x0E, Key.E); Set(0x0F, Key.R); Set(0x10, Key.Y); Set(0x11, Key.T);
        Set(0x1F, Key.O); Set(0x20, Key.U); Set(0x22, Key.I); Set(0x23, Key.P); Set(0x25, Key.L); Set(0x26, Key.J);
        Set(0x28, Key.K); Set(0x2D, Key.N); Set(0x2E, Key.M);

        // Digits and punctuation.
        Set(0x12, Key.Digit1); Set(0x13, Key.Digit2); Set(0x14, Key.Digit3); Set(0x15, Key.Digit4); Set(0x17, Key.Digit5);
        Set(0x16, Key.Digit6); Set(0x1A, Key.Digit7); Set(0x1C, Key.Digit8); Set(0x19, Key.Digit9); Set(0x1D, Key.Digit0);
        Set(0x18, Key.Equal); Set(0x1B, Key.Minus); Set(0x1E, Key.BracketRight); Set(0x21, Key.BracketLeft);
        Set(0x27, Key.Quote); Set(0x29, Key.Semicolon); Set(0x2A, Key.Backslash); Set(0x2B, Key.Comma);
        Set(0x2C, Key.Slash); Set(0x2F, Key.Period); Set(0x32, Key.Backquote);

        // Editing and whitespace.
        Set(0x24, Key.Enter); Set(0x30, Key.Tab); Set(0x31, Key.Space); Set(0x33, Key.Backspace); Set(0x35, Key.Escape);
        Set(0x75, Key.Delete); Set(0x72, Key.Insert); Set(0x73, Key.Home); Set(0x77, Key.End); Set(0x74, Key.PageUp); Set(0x79, Key.PageDown);
        Set(0x7B, Key.ArrowLeft); Set(0x7C, Key.ArrowRight); Set(0x7D, Key.ArrowDown); Set(0x7E, Key.ArrowUp); Set(0x6E, Key.Menu);

        // Modifiers.
        Set(0x37, Key.LeftMeta); Set(0x36, Key.RightMeta); Set(0x38, Key.LeftShift); Set(0x3C, Key.RightShift);
        Set(0x3A, Key.LeftAlt); Set(0x3D, Key.RightAlt); Set(0x3B, Key.LeftControl); Set(0x3E, Key.RightControl); Set(0x39, Key.CapsLock);

        // Function keys.
        Set(0x7A, Key.F1); Set(0x78, Key.F2); Set(0x63, Key.F3); Set(0x76, Key.F4); Set(0x60, Key.F5); Set(0x61, Key.F6);
        Set(0x62, Key.F7); Set(0x64, Key.F8); Set(0x65, Key.F9); Set(0x6D, Key.F10); Set(0x67, Key.F11); Set(0x6F, Key.F12);
        Set(0x69, Key.F13); Set(0x6B, Key.F14); Set(0x71, Key.F15); Set(0x6A, Key.F16); Set(0x40, Key.F17); Set(0x4F, Key.F18);
        Set(0x50, Key.F19); Set(0x5A, Key.F20);

        // Keypad.
        Set(0x52, Key.Keypad0); Set(0x53, Key.Keypad1); Set(0x54, Key.Keypad2); Set(0x55, Key.Keypad3); Set(0x56, Key.Keypad4);
        Set(0x57, Key.Keypad5); Set(0x58, Key.Keypad6); Set(0x59, Key.Keypad7); Set(0x5B, Key.Keypad8); Set(0x5C, Key.Keypad9);
        Set(0x41, Key.KeypadDecimal); Set(0x43, Key.KeypadMultiply); Set(0x45, Key.KeypadAdd); Set(0x4B, Key.KeypadDivide);
        Set(0x4C, Key.KeypadEnter); Set(0x4E, Key.KeypadSubtract); Set(0x47, Key.NumLock);
        return t;
    }

    /// <summary>NSEventModifierFlags bits (AppKit <c>NSEvent.h</c>) as <see cref="KeyModifiers"/>.</summary>
    public static KeyModifiers ModifiersFromFlags(ulong flags)
    {
        var m = KeyModifiers.None;
        if ((flags & (1UL << 17)) != 0)
        {
            m |= KeyModifiers.Shift;
        }

        if ((flags & (1UL << 18)) != 0)
        {
            m |= KeyModifiers.Control;
        }

        if ((flags & (1UL << 19)) != 0)
        {
            m |= KeyModifiers.Alt;
        }

        if ((flags & (1UL << 20)) != 0)
        {
            m |= KeyModifiers.Meta;
        }

        if ((flags & (1UL << 16)) != 0)
        {
            m |= KeyModifiers.CapsLock;
        }

        return m;
    }

    // Device-dependent modifier bits (IOKit IOLLEvent.h, NX_DEVICE*KEYMASK): they tell the left key from the right one.
    private const ulong AllDeviceBits = 0x207F;

    /// <summary>
    /// Whether a modifier key is down according to an event's modifier flags. The device bits say which side is held; a source that sets
    /// only the general bits (some remote-control tools) is read from those.
    /// </summary>
    public static bool IsDown(Key key, ulong flags)
    {
        var device = key switch
        {
            Key.LeftControl => 0x1UL,
            Key.LeftShift => 0x2UL,
            Key.RightShift => 0x4UL,
            Key.LeftMeta => 0x8UL,
            Key.RightMeta => 0x10UL,
            Key.LeftAlt => 0x20UL,
            Key.RightAlt => 0x40UL,
            Key.RightControl => 0x2000UL,
            _ => 0UL,
        };
        var general = key switch
        {
            Key.LeftShift or Key.RightShift => 1UL << 17,
            Key.LeftControl or Key.RightControl => 1UL << 18,
            Key.LeftAlt or Key.RightAlt => 1UL << 19,
            Key.LeftMeta or Key.RightMeta => 1UL << 20,
            Key.CapsLock => 1UL << 16,
            _ => 0UL,
        };
        if (device == 0 || (flags & AllDeviceBits) == 0)
        {
            return (flags & general) != 0;
        }

        return (flags & device) != 0;
    }

    /// <summary>
    /// Filters text from a key event: keeps it unless Control or Command is held (those are shortcuts), and drops control characters and
    /// AppKit's private-use codes for arrows and function keys (U+F700 to U+F8FF). Returns null when nothing is left.
    /// </summary>
    public static string? TypedText(string text, KeyModifiers modifiers)
    {
        if ((modifiers & (KeyModifiers.Control | KeyModifiers.Meta)) != 0 || text.Length == 0)
        {
            return null;
        }

        foreach (var c in text)
        {
            if (c < 0x20 || c == 0x7F || (c >= 0x80 && c < 0xA0) || (c >= 0xF700 && c <= 0xF8FF))
            {
                return null;
            }
        }

        return text;
    }
}
