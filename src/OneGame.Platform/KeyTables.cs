namespace OneGame.Platform;

/// <summary>Maps operating-system scan codes to <see cref="Key"/>.</summary>
internal static class KeyTables
{
    // Linux evdev key codes (linux/input-event-codes.h numbering).
    private static readonly Key[] Evdev = BuildEvdev();

    private static Key[] BuildEvdev()
    {
        var t = new Key[256];
        t[1] = Key.Escape;
        t[2] = Key.Digit1;
        t[3] = Key.Digit2;
        t[4] = Key.Digit3;
        t[5] = Key.Digit4;
        t[6] = Key.Digit5;
        t[7] = Key.Digit6;
        t[8] = Key.Digit7;
        t[9] = Key.Digit8;
        t[10] = Key.Digit9;
        t[11] = Key.Digit0;
        t[12] = Key.Minus;
        t[13] = Key.Equal;
        t[14] = Key.Backspace;
        t[15] = Key.Tab;
        t[16] = Key.Q;
        t[17] = Key.W;
        t[18] = Key.E;
        t[19] = Key.R;
        t[20] = Key.T;
        t[21] = Key.Y;
        t[22] = Key.U;
        t[23] = Key.I;
        t[24] = Key.O;
        t[25] = Key.P;
        t[26] = Key.BracketLeft;
        t[27] = Key.BracketRight;
        t[28] = Key.Enter;
        t[29] = Key.LeftControl;
        t[30] = Key.A;
        t[31] = Key.S;
        t[32] = Key.D;
        t[33] = Key.F;
        t[34] = Key.G;
        t[35] = Key.H;
        t[36] = Key.J;
        t[37] = Key.K;
        t[38] = Key.L;
        t[39] = Key.Semicolon;
        t[40] = Key.Quote;
        t[41] = Key.Backquote;
        t[42] = Key.LeftShift;
        t[43] = Key.Backslash;
        t[44] = Key.Z;
        t[45] = Key.X;
        t[46] = Key.C;
        t[47] = Key.V;
        t[48] = Key.B;
        t[49] = Key.N;
        t[50] = Key.M;
        t[51] = Key.Comma;
        t[52] = Key.Period;
        t[53] = Key.Slash;
        t[54] = Key.RightShift;
        t[55] = Key.KeypadMultiply;
        t[56] = Key.LeftAlt;
        t[57] = Key.Space;
        t[58] = Key.CapsLock;
        t[59] = Key.F1;
        t[60] = Key.F2;
        t[61] = Key.F3;
        t[62] = Key.F4;
        t[63] = Key.F5;
        t[64] = Key.F6;
        t[65] = Key.F7;
        t[66] = Key.F8;
        t[67] = Key.F9;
        t[68] = Key.F10;
        t[69] = Key.NumLock;
        t[70] = Key.ScrollLock;
        t[71] = Key.Keypad7;
        t[72] = Key.Keypad8;
        t[73] = Key.Keypad9;
        t[74] = Key.KeypadSubtract;
        t[75] = Key.Keypad4;
        t[76] = Key.Keypad5;
        t[77] = Key.Keypad6;
        t[78] = Key.KeypadAdd;
        t[79] = Key.Keypad1;
        t[80] = Key.Keypad2;
        t[81] = Key.Keypad3;
        t[82] = Key.Keypad0;
        t[83] = Key.KeypadDecimal;
        t[86] = Key.IntlBackslash;
        t[87] = Key.F11;
        t[88] = Key.F12;
        t[96] = Key.KeypadEnter;
        t[97] = Key.RightControl;
        t[98] = Key.KeypadDivide;
        t[99] = Key.PrintScreen;
        t[100] = Key.RightAlt;
        t[102] = Key.Home;
        t[103] = Key.ArrowUp;
        t[104] = Key.PageUp;
        t[105] = Key.ArrowLeft;
        t[106] = Key.ArrowRight;
        t[107] = Key.End;
        t[108] = Key.ArrowDown;
        t[109] = Key.PageDown;
        t[110] = Key.Insert;
        t[111] = Key.Delete;
        t[119] = Key.Pause;
        t[125] = Key.LeftMeta;
        t[126] = Key.RightMeta;
        t[127] = Key.Menu;
        t[183] = Key.F13;
        t[184] = Key.F14;
        t[185] = Key.F15;
        t[186] = Key.F16;
        t[187] = Key.F17;
        t[188] = Key.F18;
        t[189] = Key.F19;
        t[190] = Key.F20;
        t[191] = Key.F21;
        t[192] = Key.F22;
        t[193] = Key.F23;
        t[194] = Key.F24;
        return t;
    }

    /// <summary>Translates a Linux evdev key code (X11 keycode minus 8) to a key.</summary>
    public static Key FromEvdev(int code) => (uint)code < (uint)Evdev.Length ? Evdev[code] : Key.Unknown;

    /// <summary>Translates a Windows set-1 scan code (as in WM_KEYDOWN lParam bits 16-23) and its extended flag to a key.</summary>
    public static Key FromWindowsScan(int scan, bool extended)
    {
        if (extended)
        {
            switch (scan)
            {
                case 0x1C: return Key.KeypadEnter;
                case 0x1D: return Key.RightControl;
                case 0x35: return Key.KeypadDivide;
                case 0x37: return Key.PrintScreen;
                case 0x38: return Key.RightAlt;
                case 0x47: return Key.Home;
                case 0x48: return Key.ArrowUp;
                case 0x49: return Key.PageUp;
                case 0x4B: return Key.ArrowLeft;
                case 0x4D: return Key.ArrowRight;
                case 0x4F: return Key.End;
                case 0x50: return Key.ArrowDown;
                case 0x51: return Key.PageDown;
                case 0x52: return Key.Insert;
                case 0x53: return Key.Delete;
                case 0x5B: return Key.LeftMeta;
                case 0x5C: return Key.RightMeta;
                case 0x5D: return Key.Menu;
                default: return Key.Unknown;
            }
        }

        switch (scan)
        {
            case 1: return Key.Escape;
            case 2: return Key.Digit1;
            case 3: return Key.Digit2;
            case 4: return Key.Digit3;
            case 5: return Key.Digit4;
            case 6: return Key.Digit5;
            case 7: return Key.Digit6;
            case 8: return Key.Digit7;
            case 9: return Key.Digit8;
            case 10: return Key.Digit9;
            case 11: return Key.Digit0;
            case 12: return Key.Minus;
            case 13: return Key.Equal;
            case 14: return Key.Backspace;
            case 15: return Key.Tab;
            case 16: return Key.Q;
            case 17: return Key.W;
            case 18: return Key.E;
            case 19: return Key.R;
            case 20: return Key.T;
            case 21: return Key.Y;
            case 22: return Key.U;
            case 23: return Key.I;
            case 24: return Key.O;
            case 25: return Key.P;
            case 26: return Key.BracketLeft;
            case 27: return Key.BracketRight;
            case 28: return Key.Enter;
            case 29: return Key.LeftControl;
            case 30: return Key.A;
            case 31: return Key.S;
            case 32: return Key.D;
            case 33: return Key.F;
            case 34: return Key.G;
            case 35: return Key.H;
            case 36: return Key.J;
            case 37: return Key.K;
            case 38: return Key.L;
            case 39: return Key.Semicolon;
            case 40: return Key.Quote;
            case 41: return Key.Backquote;
            case 42: return Key.LeftShift;
            case 43: return Key.Backslash;
            case 44: return Key.Z;
            case 45: return Key.X;
            case 46: return Key.C;
            case 47: return Key.V;
            case 48: return Key.B;
            case 49: return Key.N;
            case 50: return Key.M;
            case 51: return Key.Comma;
            case 52: return Key.Period;
            case 53: return Key.Slash;
            case 54: return Key.RightShift;
            case 55: return Key.KeypadMultiply;
            case 56: return Key.LeftAlt;
            case 57: return Key.Space;
            case 58: return Key.CapsLock;
            case 59: return Key.F1;
            case 60: return Key.F2;
            case 61: return Key.F3;
            case 62: return Key.F4;
            case 63: return Key.F5;
            case 64: return Key.F6;
            case 65: return Key.F7;
            case 66: return Key.F8;
            case 67: return Key.F9;
            case 68: return Key.F10;
            case 70: return Key.ScrollLock;
            case 71: return Key.Keypad7;
            case 72: return Key.Keypad8;
            case 73: return Key.Keypad9;
            case 74: return Key.KeypadSubtract;
            case 75: return Key.Keypad4;
            case 76: return Key.Keypad5;
            case 77: return Key.Keypad6;
            case 78: return Key.KeypadAdd;
            case 79: return Key.Keypad1;
            case 80: return Key.Keypad2;
            case 81: return Key.Keypad3;
            case 82: return Key.Keypad0;
            case 83: return Key.KeypadDecimal;
            case 86: return Key.IntlBackslash;
            case 87: return Key.F11;
            case 88: return Key.F12;
            case 100: return Key.F13;
            case 101: return Key.F14;
            case 102: return Key.F15;
            case 0x45: return Key.NumLock;
            default: return Key.Unknown;
        }
    }
}
