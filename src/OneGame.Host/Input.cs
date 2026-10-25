using OneGame.Platform;
using OneGame.UI;

namespace OneGame.Host;

/// <summary>Key names: the platform's physical <see cref="Key"/> to the DOM-style key strings the UI uses, and back.</summary>
public static class KeyNames
{
    /// <summary>The UI's key string for a physical key. Letters are lower case unless <paramref name="shift"/> is set.</summary>
    public static string ToUiKey(Key key, bool shift = false)
    {
        if (key is >= Key.A and <= Key.Z)
        {
            var ch = (char)((shift ? 'A' : 'a') + (key - Key.A));
            return ch.ToString();
        }

        if (key is >= Key.Digit1 and <= Key.Digit9)
        {
            return ((char)('1' + (key - Key.Digit1))).ToString();
        }

        return key switch
        {
            Key.Digit0 => "0",
            Key.Space => " ",
            Key.Enter or Key.KeypadEnter => "Enter",
            Key.Escape => "Escape",
            Key.Tab => "Tab",
            Key.Backspace => "Backspace",
            Key.Delete => "Delete",
            Key.Insert => "Insert",
            Key.Home => "Home",
            Key.End => "End",
            Key.PageUp => "PageUp",
            Key.PageDown => "PageDown",
            Key.ArrowUp => "ArrowUp",
            Key.ArrowDown => "ArrowDown",
            Key.ArrowLeft => "ArrowLeft",
            Key.ArrowRight => "ArrowRight",
            Key.LeftShift or Key.RightShift => "Shift",
            Key.LeftControl or Key.RightControl => "Control",
            Key.LeftAlt or Key.RightAlt => "Alt",
            Key.LeftMeta or Key.RightMeta => "Meta",
            Key.Minus => "-",
            Key.Equal => "=",
            Key.Comma => ",",
            Key.Period => ".",
            Key.Slash => "/",
            Key.Semicolon => ";",
            Key.Quote => "'",
            Key.Backquote => "`",
            Key.BracketLeft => "[",
            Key.BracketRight => "]",
            Key.Backslash => "\\",
            >= Key.F1 and <= Key.F24 => "F" + (1 + (key - Key.F1)).ToString(CultureInfo.InvariantCulture),
            _ => key.ToString(),
        };
    }

    /// <summary>Reads a key from a DOM-style name (<c>Enter</c>, <c>a</c>, <c>ArrowLeft</c>, <c>F5</c>, a space) or a <see cref="Key"/> name (<c>Digit1</c>, <c>LeftShift</c>).</summary>
    public static bool TryParse(string? text, out Key key)
    {
        key = Key.Unknown;
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        if (text.Length == 1)
        {
            var c = text[0];
            if (c is >= 'a' and <= 'z')
            {
                key = (Key)((int)Key.A + (c - 'a'));
                return true;
            }

            if (c is >= 'A' and <= 'Z')
            {
                key = (Key)((int)Key.A + (c - 'A'));
                return true;
            }

            if (c is >= '1' and <= '9')
            {
                key = (Key)((int)Key.Digit1 + (c - '1'));
                return true;
            }

            switch (c)
            {
                case '0': key = Key.Digit0; return true;
                case ' ': key = Key.Space; return true;
                case '-': key = Key.Minus; return true;
                case '=': key = Key.Equal; return true;
                case ',': key = Key.Comma; return true;
                case '.': key = Key.Period; return true;
                case '/': key = Key.Slash; return true;
                case ';': key = Key.Semicolon; return true;
                case '\'': key = Key.Quote; return true;
                case '`': key = Key.Backquote; return true;
                case '[': key = Key.BracketLeft; return true;
                case ']': key = Key.BracketRight; return true;
                case '\\': key = Key.Backslash; return true;
                default: return false;
            }
        }

        switch (text)
        {
            case "Shift": key = Key.LeftShift; return true;
            case "Control" or "Ctrl": key = Key.LeftControl; return true;
            case "Alt": key = Key.LeftAlt; return true;
            case "Meta": key = Key.LeftMeta; return true;
            case "Esc": key = Key.Escape; return true;
            case "Return": key = Key.Enter; return true;
            default: return Enum.TryParse(text, true, out key) && key != Key.Unknown;
        }
    }
}

/// <summary>Turns platform events into UI events. Real and injected input arrive through the same queue, so both take this path.</summary>
internal static class InputRouter
{
    private const float WheelPixelsPerNotch = 48f;

    /// <summary>Translates one platform event. Returns false when the UI has no equivalent (window and gamepad-connect events).</summary>
    public static bool TryTranslate(in PlatformEvent e, out UiEvent ui)
    {
        var mods = (OneGame.UI.KeyModifiers)((int)e.Modifiers & 15);
        switch (e.Kind)
        {
            case PlatformEventKind.PointerMoved:
                ui = UiEvent.PointerMove(e.X, e.Y);
                return true;
            case PlatformEventKind.PointerButtonDown:
                ui = UiEvent.PointerDown(e.X, e.Y, Button(e.Button));
                return true;
            case PlatformEventKind.PointerButtonUp:
                ui = UiEvent.PointerUp(e.X, e.Y, Button(e.Button));
                return true;
            case PlatformEventKind.PointerWheel:
                ui = UiEvent.Wheel(e.X, e.Y, -e.DeltaX * WheelPixelsPerNotch, -e.DeltaY * WheelPixelsPerNotch);
                return true;
            case PlatformEventKind.PointerInside when !e.Focused:
                ui = UiEvent.PointerLeftWindow();
                return true;
            case PlatformEventKind.KeyDown:
                ui = UiEvent.KeyDown(KeyNames.ToUiKey(e.Key, (mods & OneGame.UI.KeyModifiers.Shift) != 0), mods, e.IsRepeat);
                return true;
            case PlatformEventKind.KeyUp:
                ui = UiEvent.KeyUp(KeyNames.ToUiKey(e.Key, (mods & OneGame.UI.KeyModifiers.Shift) != 0), mods);
                return true;
            case PlatformEventKind.TextInput when !string.IsNullOrEmpty(e.Text):
                ui = UiEvent.TextInput(e.Text);
                return true;
            case PlatformEventKind.TextComposition:
                ui = UiEvent.Composition(e.Text ?? "", e.CursorStart, e.CursorEnd);
                return true;
            case PlatformEventKind.GamepadButtonDown:
                return Gamepad(e.GamepadButton, out ui);
            default:
                ui = default;
                return false;
        }
    }

    private static int Button(MouseButton b) => b switch
    {
        MouseButton.Left => 0,
        MouseButton.Right => 1,
        MouseButton.Middle => 2,
        _ => 3,
    };

    private static bool Gamepad(GamepadButton button, out UiEvent ui)
    {
        switch (button)
        {
            case GamepadButton.South: ui = UiEvent.Activate(); return true;
            case GamepadButton.East: ui = UiEvent.Cancel(); return true;
            case GamepadButton.DPadUp: ui = UiEvent.Navigate(NavDirection.Up); return true;
            case GamepadButton.DPadDown: ui = UiEvent.Navigate(NavDirection.Down); return true;
            case GamepadButton.DPadLeft: ui = UiEvent.Navigate(NavDirection.Left); return true;
            case GamepadButton.DPadRight: ui = UiEvent.Navigate(NavDirection.Right); return true;
            case GamepadButton.LeftShoulder: ui = UiEvent.Navigate(NavDirection.Previous); return true;
            case GamepadButton.RightShoulder: ui = UiEvent.Navigate(NavDirection.Next); return true;
            default: ui = default; return false;
        }
    }
}

/// <summary>
/// Turns the left stick into focus navigation: a push past the threshold moves focus once, and holding it repeats after a delay, as a
/// d-pad does. Counting frames instead of reading a clock keeps scripted runs reproducible.
/// </summary>
internal sealed class StickNavigator
{
    private const float On = 0.6f, Off = 0.35f;
    private const int RepeatDelayFrames = 24, RepeatIntervalFrames = 6;

    private float _x, _y;
    private NavDirection? _held;
    private int _frames;

    /// <summary>Feeds a stick event. Returns the navigation to perform now, if the stick just crossed into a direction.</summary>
    public bool OnAxis(in PlatformEvent e, out UiEvent ui)
    {
        ui = default;
        switch (e.GamepadAxis)
        {
            case GamepadAxis.LeftX:
                _x = e.Value;
                break;
            case GamepadAxis.LeftY:
                _y = e.Value;
                break;
            default:
                return false;
        }

        var now = Direction();
        if (now == _held)
        {
            return false;
        }

        _held = now;
        _frames = 0;
        if (now is { } d)
        {
            ui = UiEvent.Navigate(d);
            return true;
        }

        return false;
    }

    /// <summary>Call once per frame; returns the repeated navigation while the stick is held.</summary>
    public bool Tick(out UiEvent ui)
    {
        ui = default;
        if (_held is not { } d)
        {
            return false;
        }

        _frames++;
        if (_frames >= RepeatDelayFrames && (_frames - RepeatDelayFrames) % RepeatIntervalFrames == 0)
        {
            ui = UiEvent.Navigate(d);
            return true;
        }

        return false;
    }

    // Stick values have down and right positive. The stick must come back below the lower threshold before another direction counts.
    private NavDirection? Direction()
    {
        var ax = MathF.Abs(_x);
        var ay = MathF.Abs(_y);
        var threshold = _held is null ? On : Off;
        if (MathF.Max(ax, ay) < threshold)
        {
            return null;
        }

        if (ax >= ay)
        {
            return _x > 0 ? NavDirection.Right : NavDirection.Left;
        }

        return _y > 0 ? NavDirection.Down : NavDirection.Up;
    }
}
