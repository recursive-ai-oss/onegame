namespace OneGame.Platform;

/// <summary>Android key codes (android.view.KeyEvent, public constants) mapped onto the platform's physical keys and gamepad buttons.</summary>
internal static class AndroidKeys
{
    public const int KeyBack = 4;

    /// <summary>The physical key for an Android key code, or <see cref="Key.Unknown"/>.</summary>
    public static Key ToKey(int code)
    {
        switch (code)
        {
            case >= 29 and <= 54: return (Key)((int)Key.A + (code - 29));
            case 7: return Key.Digit0;
            case >= 8 and <= 16: return (Key)((int)Key.Digit1 + (code - 8));
            case >= 131 and <= 142: return (Key)((int)Key.F1 + (code - 131));
            case >= 144 and <= 153: return (Key)((int)Key.Keypad0 + (code - 144));
        }

        return code switch
        {
            19 => Key.ArrowUp,
            20 => Key.ArrowDown,
            21 => Key.ArrowLeft,
            22 => Key.ArrowRight,
            23 or 66 => Key.Enter,
            160 => Key.KeypadEnter,
            55 => Key.Comma,
            56 => Key.Period,
            57 => Key.LeftAlt,
            58 => Key.RightAlt,
            59 => Key.LeftShift,
            60 => Key.RightShift,
            61 => Key.Tab,
            62 => Key.Space,
            67 => Key.Backspace,
            112 => Key.Delete,
            68 => Key.Backquote,
            69 => Key.Minus,
            70 => Key.Equal,
            71 => Key.BracketLeft,
            72 => Key.BracketRight,
            73 => Key.Backslash,
            74 => Key.Semicolon,
            75 => Key.Quote,
            76 => Key.Slash,
            82 => Key.Menu,
            92 => Key.PageUp,
            93 => Key.PageDown,
            111 => Key.Escape,
            113 => Key.LeftControl,
            114 => Key.RightControl,
            115 => Key.CapsLock,
            116 => Key.ScrollLock,
            117 => Key.LeftMeta,
            118 => Key.RightMeta,
            120 => Key.PrintScreen,
            121 => Key.Pause,
            122 => Key.Home,
            123 => Key.End,
            124 => Key.Insert,
            143 => Key.NumLock,
            154 => Key.KeypadDivide,
            155 => Key.KeypadMultiply,
            156 => Key.KeypadSubtract,
            157 => Key.KeypadAdd,
            158 => Key.KeypadDecimal,
            _ => Key.Unknown,
        };
    }

    /// <summary>The standard-layout gamepad button for a key code from a gamepad or d-pad source, or null when the code is not a pad button.</summary>
    public static GamepadButton? ToGamepadButton(int code) => code switch
    {
        96 or 23 => GamepadButton.South,
        97 => GamepadButton.East,
        99 => GamepadButton.West,
        100 => GamepadButton.North,
        102 => GamepadButton.LeftShoulder,
        103 => GamepadButton.RightShoulder,
        109 => GamepadButton.Back,
        108 => GamepadButton.Start,
        110 => GamepadButton.Guide,
        106 => GamepadButton.LeftStick,
        107 => GamepadButton.RightStick,
        19 => GamepadButton.DPadUp,
        20 => GamepadButton.DPadDown,
        21 => GamepadButton.DPadLeft,
        22 => GamepadButton.DPadRight,
        _ => null,
    };

    /// <summary>Whether the code is a pad's digital trigger; the analog axis reports the same press.</summary>
    public static bool IsDigitalTrigger(int code) => code is 104 or 105;

    /// <summary>The modifiers in an Android meta state (AMETA_* flags).</summary>
    public static KeyModifiers ToModifiers(int meta)
    {
        var m = KeyModifiers.None;
        if ((meta & 0x1) != 0)
        {
            m |= KeyModifiers.Shift;
        }

        if ((meta & 0x2) != 0)
        {
            m |= KeyModifiers.Alt;
        }

        if ((meta & 0x1000) != 0)
        {
            m |= KeyModifiers.Control;
        }

        if ((meta & 0x10000) != 0)
        {
            m |= KeyModifiers.Meta;
        }

        if ((meta & 0x100000) != 0)
        {
            m |= KeyModifiers.CapsLock;
        }

        if ((meta & 0x200000) != 0)
        {
            m |= KeyModifiers.NumLock;
        }

        return m;
    }
}
