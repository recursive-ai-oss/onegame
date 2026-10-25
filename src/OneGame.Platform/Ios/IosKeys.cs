namespace OneGame.Platform;

/// <summary>
/// Hardware keyboard keys on iOS. UIKit reports each key as a USB HID keyboard usage (UIKeyboardHIDUsage; the numbers are the
/// "Keyboard/Keypad Page" of the USB HID Usage Tables 1.4, section 10) and its modifiers as UIKeyModifierFlags.
/// </summary>
internal static class IosKeys
{
    // UIKeyModifierFlags (UIKit documentation).
    private const int ModShift = 0x20000;
    private const int ModControl = 0x40000;
    private const int ModAlternate = 0x80000;
    private const int ModCommand = 0x100000;
    private const int ModAlphaShift = 0x10000;

    /// <summary>The physical key for a HID usage, or <see cref="Key.Unknown"/>.</summary>
    public static Key ToKey(int usage)
    {
        switch (usage)
        {
            case >= 4 and <= 29: return (Key)((int)Key.A + (usage - 4));
            case >= 30 and <= 38: return (Key)((int)Key.Digit1 + (usage - 30));
            case 39: return Key.Digit0;
            case >= 58 and <= 69: return (Key)((int)Key.F1 + (usage - 58));
            case >= 104 and <= 115: return (Key)((int)Key.F13 + (usage - 104));
            case >= 89 and <= 97: return (Key)((int)Key.Keypad1 + (usage - 89));
        }

        return usage switch
        {
            40 => Key.Enter,
            41 => Key.Escape,
            42 => Key.Backspace,
            43 => Key.Tab,
            44 => Key.Space,
            45 => Key.Minus,
            46 => Key.Equal,
            47 => Key.BracketLeft,
            48 => Key.BracketRight,
            49 => Key.Backslash,
            51 => Key.Semicolon,
            52 => Key.Quote,
            53 => Key.Backquote,
            54 => Key.Comma,
            55 => Key.Period,
            56 => Key.Slash,
            57 => Key.CapsLock,
            70 => Key.PrintScreen,
            71 => Key.ScrollLock,
            72 => Key.Pause,
            73 => Key.Insert,
            74 => Key.Home,
            75 => Key.PageUp,
            76 => Key.Delete,
            77 => Key.End,
            78 => Key.PageDown,
            79 => Key.ArrowRight,
            80 => Key.ArrowLeft,
            81 => Key.ArrowDown,
            82 => Key.ArrowUp,
            83 => Key.NumLock,
            84 => Key.KeypadDivide,
            85 => Key.KeypadMultiply,
            86 => Key.KeypadSubtract,
            87 => Key.KeypadAdd,
            88 => Key.KeypadEnter,
            98 => Key.Keypad0,
            99 => Key.KeypadDecimal,
            100 => Key.IntlBackslash,
            101 => Key.Menu,
            224 => Key.LeftControl,
            225 => Key.LeftShift,
            226 => Key.LeftAlt,
            227 => Key.LeftMeta,
            228 => Key.RightControl,
            229 => Key.RightShift,
            230 => Key.RightAlt,
            231 => Key.RightMeta,
            _ => Key.Unknown,
        };
    }

    /// <summary>Whether the key types a character (letters, digits, punctuation, space and the keypad's digits and operators). Enter, Backspace, Tab and arrows do not.</summary>
    public static bool IsPrintable(int usage) =>
        usage is (>= 4 and <= 39) or (>= 44 and <= 49) or (>= 51 and <= 56) or (>= 84 and <= 87) or (>= 89 and <= 100);

    public static KeyModifiers ToModifiers(int flags)
    {
        var m = KeyModifiers.None;
        if ((flags & ModShift) != 0)
        {
            m |= KeyModifiers.Shift;
        }

        if ((flags & ModControl) != 0)
        {
            m |= KeyModifiers.Control;
        }

        if ((flags & ModAlternate) != 0)
        {
            m |= KeyModifiers.Alt;
        }

        if ((flags & ModCommand) != 0)
        {
            m |= KeyModifiers.Meta;
        }

        if ((flags & ModAlphaShift) != 0)
        {
            m |= KeyModifiers.CapsLock;
        }

        return m;
    }
}
