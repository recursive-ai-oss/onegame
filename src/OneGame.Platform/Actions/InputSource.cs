namespace OneGame.Platform;

/// <summary>What kind of control an <see cref="InputSource"/> reads.</summary>
public enum InputSourceKind : byte
{
    /// <summary>Nothing: an empty slot.</summary>
    None,

    /// <summary>A keyboard key (physical position, like <see cref="Key"/>).</summary>
    Key,

    /// <summary>A mouse button.</summary>
    Mouse,

    /// <summary>A gamepad button on the standard layout; any connected pad counts.</summary>
    PadButton,

    /// <summary>A gamepad stick or trigger axis, in one direction (or inverted when used on its own).</summary>
    PadAxis,

    /// <summary>The mouse wheel, in notches per frame.</summary>
    Wheel,

    /// <summary>A finger down in part of the window.</summary>
    Touch,
}

/// <summary>The part of the window a touch binding listens to.</summary>
public enum TouchRegion : byte
{
    /// <summary>Anywhere in the window.</summary>
    Any,

    /// <summary>The left half.</summary>
    Left,

    /// <summary>The right half.</summary>
    Right,

    /// <summary>The top half.</summary>
    Top,

    /// <summary>The bottom half.</summary>
    Bottom,
}

/// <summary>
/// One physical control an action can listen to: a key, a mouse button, a pad button or axis, the wheel, or a touch region. Small and comparable, so bindings can be
/// stored, compared for conflicts and written to a file. Make them with <see cref="Bind"/>.
/// </summary>
/// <param name="Kind">The kind of control.</param>
/// <param name="Code">The <see cref="Key"/>, <see cref="MouseButton"/>, <see cref="GamepadButton"/>, <see cref="GamepadAxis"/> or <see cref="TouchRegion"/> value; for the wheel 0 = vertical, 1 = horizontal.</param>
/// <param name="Sign">For axes and the wheel: +1 reads the axis as it is (right, down, trigger pulled; wheel away from the user), -1 reads it inverted.</param>
public readonly record struct InputSource(InputSourceKind Kind, int Code, int Sign = 1)
{
    /// <summary>True for an empty slot.</summary>
    public bool IsNone => Kind == InputSourceKind.None;

    /// <summary>The device this source belongs to, to pick a glyph set.</summary>
    public bool IsPad => Kind is InputSourceKind.PadButton or InputSourceKind.PadAxis;

    /// <summary>A key as a source.</summary>
    public static implicit operator InputSource(Key key) => new(InputSourceKind.Key, (int)key);

    /// <summary>The file and harness spelling: <c>key:Space</c>, <c>mouse:Left</c>, <c>pad:South</c>, <c>axis:LeftX+</c>, <c>wheel:y+</c>, <c>touch:Left</c>, <c>none</c>.</summary>
    public override string ToString() => Kind switch
    {
        InputSourceKind.Key => "key:" + ((Key)Code),
        InputSourceKind.Mouse => "mouse:" + ((MouseButton)Code),
        InputSourceKind.PadButton => "pad:" + ((GamepadButton)Code),
        InputSourceKind.PadAxis => "axis:" + ((GamepadAxis)Code) + (Sign < 0 ? "-" : "+"),
        InputSourceKind.Wheel => "wheel:" + (Code == 1 ? "x" : "y") + (Sign < 0 ? "-" : "+"),
        InputSourceKind.Touch => "touch:" + ((TouchRegion)Code),
        _ => "none",
    };

    /// <summary>A short label for a binding list: <c>Space</c>, <c>Left mouse</c>, <c>Pad South</c>, <c>Left stick left</c>.</summary>
    public string Describe() => Kind switch
    {
        InputSourceKind.Key => ((Key)Code).ToString(),
        InputSourceKind.Mouse => (MouseButton)Code + " mouse",
        InputSourceKind.PadButton => "Pad " + (GamepadButton)Code,
        InputSourceKind.PadAxis => "Pad " + (GamepadAxis)Code + (Sign < 0 ? " -" : " +"),
        InputSourceKind.Wheel => "Wheel " + (Code == 1 ? "right" : "up") + (Sign < 0 ? " (reversed)" : ""),
        InputSourceKind.Touch => "Touch " + ((TouchRegion)Code),
        _ => "(none)",
    };

    /// <summary>Reads the spelling <see cref="ToString"/> writes.</summary>
    /// <param name="text">The text.</param>
    /// <param name="source">The source, when it parsed.</param>
    /// <returns>True when <paramref name="text"/> names a source.</returns>
    public static bool TryParse(ReadOnlySpan<char> text, out InputSource source)
    {
        source = default;
        text = text.Trim();
        if (text.Equals("none", StringComparison.Ordinal))
        {
            return true;
        }

        var colon = text.IndexOf(':');
        if (colon <= 0)
        {
            return false;
        }

        var kind = text[..colon];
        var arg = text[(colon + 1)..];
        if (arg.Length is 0 or > 32)
        {
            return false;
        }

        // Enum parsing would accept "A,B" as flags and numbers; a binding names exactly one control.
        var nameEnd = arg.Length - (kind.Equals("axis", StringComparison.Ordinal) || kind.Equals("wheel", StringComparison.Ordinal) ? 1 : 0);
        if (nameEnd <= 0 || !char.IsAsciiLetter(arg[0]))
        {
            return false;
        }

        for (var i = 1; i < nameEnd; i++)
        {
            if (!char.IsAsciiLetterOrDigit(arg[i]))
            {
                return false;
            }
        }

        if (kind.Equals("key", StringComparison.Ordinal))
        {
            if (Enum.TryParse<Key>(arg, false, out var k) && k != Key.Unknown && Enum.IsDefined(k))
            {
                source = new InputSource(InputSourceKind.Key, (int)k);
                return true;
            }
        }
        else if (kind.Equals("mouse", StringComparison.Ordinal))
        {
            if (Enum.TryParse<MouseButton>(arg, false, out var m) && Enum.IsDefined(m))
            {
                source = new InputSource(InputSourceKind.Mouse, (int)m);
                return true;
            }
        }
        else if (kind.Equals("pad", StringComparison.Ordinal))
        {
            if (Enum.TryParse<GamepadButton>(arg, false, out var b) && Enum.IsDefined(b))
            {
                source = new InputSource(InputSourceKind.PadButton, (int)b);
                return true;
            }
        }
        else if (kind.Equals("axis", StringComparison.Ordinal) || kind.Equals("wheel", StringComparison.Ordinal))
        {
            var sign = arg[^1] switch { '+' => 1, '-' => -1, _ => 0 };
            if (sign == 0)
            {
                return false;
            }

            var name = arg[..^1];
            if (kind.Equals("wheel", StringComparison.Ordinal))
            {
                if (name.Length == 1 && name[0] is 'x' or 'y')
                {
                    source = new InputSource(InputSourceKind.Wheel, name[0] == 'x' ? 1 : 0, sign);
                    return true;
                }
            }
            else if (Enum.TryParse<GamepadAxis>(name, false, out var a) && Enum.IsDefined(a))
            {
                source = new InputSource(InputSourceKind.PadAxis, (int)a, sign);
                return true;
            }
        }
        else if (kind.Equals("touch", StringComparison.Ordinal))
        {
            if (Enum.TryParse<TouchRegion>(arg, false, out var t) && Enum.IsDefined(t))
            {
                source = new InputSource(InputSourceKind.Touch, (int)t);
                return true;
            }
        }

        return false;
    }
}

/// <summary>The arrangement of an <see cref="ActionBinding"/>.</summary>
public enum BindingShape : byte
{
    /// <summary>One source. Buttons take it as pressed; a 1D axis reads its value.</summary>
    One,

    /// <summary>Two sources for a 1D axis: the first pulls it to -1, the second to +1.</summary>
    Pair,

    /// <summary>Four sources for a 2D vector: left, right, up, down (y grows downward, like the screen).</summary>
    Quad,

    /// <summary>A pad stick as a 2D vector. Source 0 is the left stick or the right stick (<see cref="GamepadAxis.LeftX"/> or <see cref="GamepadAxis.RightX"/>).</summary>
    Stick,
}

/// <summary>
/// What one slot of an action listens to: a single source or a composite (two keys making an axis, WASD making a vector, a stick). An action holds several, and any of them can drive it.
/// Make them with <see cref="Bind"/>; a bare <see cref="InputSource"/> converts.
/// </summary>
public readonly struct ActionBinding : IEquatable<ActionBinding>
{
    private readonly InputSource _a, _b, _c, _d;

    internal ActionBinding(BindingShape shape, InputSource a, InputSource b = default, InputSource c = default, InputSource d = default)
    {
        Shape = shape;
        _a = a;
        _b = b;
        _c = c;
        _d = d;
    }

    /// <summary>The arrangement.</summary>
    public BindingShape Shape { get; }

    /// <summary>How many sources the shape has (1, 2, 4 or 1).</summary>
    public int PartCount => Shape switch { BindingShape.Pair => 2, BindingShape.Quad => 4, _ => 1 };

    /// <summary>One source of the binding.</summary>
    /// <param name="part">0 for a single source; 0..1 for a pair; 0..3 (left, right, up, down) for a quad.</param>
    /// <returns>The source.</returns>
    public InputSource Part(int part) => part switch { 0 => _a, 1 => _b, 2 => _c, 3 => _d, _ => throw new ArgumentOutOfRangeException(nameof(part)) };

    /// <summary>Returns a copy with one part replaced.</summary>
    /// <param name="part">The part index.</param>
    /// <param name="source">The new source.</param>
    /// <returns>The changed binding.</returns>
    public ActionBinding With(int part, InputSource source)
    {
        if ((uint)part >= (uint)PartCount)
        {
            throw new ArgumentOutOfRangeException(nameof(part));
        }

        return part switch
        {
            0 => new(Shape, source, _b, _c, _d),
            1 => new(Shape, _a, source, _c, _d),
            2 => new(Shape, _a, _b, source, _d),
            _ => new(Shape, _a, _b, _c, source),
        };
    }

    /// <summary>True when every part is empty.</summary>
    public bool IsEmpty => Shape != BindingShape.Stick && _a.IsNone && (PartCount < 2 || _b.IsNone) && (PartCount < 4 || (_c.IsNone && _d.IsNone));

    /// <summary>True when any part reads exactly <paramref name="source"/>.</summary>
    public bool Uses(InputSource source)
    {
        if (source.IsNone)
        {
            return false;
        }

        for (var i = 0; i < PartCount; i++)
        {
            if (Part(i) == source)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>A single source as a binding.</summary>
    public static implicit operator ActionBinding(InputSource source) => new(BindingShape.One, source);

    /// <summary>A key as a binding.</summary>
    public static implicit operator ActionBinding(Key key) => new(BindingShape.One, key);

    /// <inheritdoc />
    public bool Equals(ActionBinding other) => Shape == other.Shape && _a == other._a && _b == other._b && _c == other._c && _d == other._d;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is ActionBinding o && Equals(o);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Shape, _a, _b, _c, _d);

    /// <summary>Equality.</summary>
    public static bool operator ==(ActionBinding left, ActionBinding right) => left.Equals(right);

    /// <summary>Inequality.</summary>
    public static bool operator !=(ActionBinding left, ActionBinding right) => !left.Equals(right);

    /// <summary>The file spelling: a source, <c>pair(a,b)</c>, <c>quad(l,r,u,d)</c> or <c>stick:left</c>/<c>stick:right</c>.</summary>
    public override string ToString() => Shape switch
    {
        BindingShape.Pair => $"pair({_a},{_b})",
        BindingShape.Quad => $"quad({_a},{_b},{_c},{_d})",
        BindingShape.Stick => _a.Code == (int)GamepadAxis.RightX ? "stick:right" : "stick:left",
        _ => _a.ToString(),
    };

    /// <summary>A short label for a binding list, such as <c>W A S D</c> or <c>Space</c>.</summary>
    public string Describe() => Shape switch
    {
        BindingShape.Pair => _a.Describe() + " / " + _b.Describe(),
        BindingShape.Quad => _c.Describe() + " " + _a.Describe() + " " + _d.Describe() + " " + _b.Describe(),
        BindingShape.Stick => _a.Code == (int)GamepadAxis.RightX ? "Right stick" : "Left stick",
        _ => _a.Describe(),
    };

    /// <summary>Reads the spelling <see cref="ToString"/> writes.</summary>
    /// <param name="text">The text.</param>
    /// <param name="binding">The binding, when it parsed.</param>
    /// <returns>True when <paramref name="text"/> is a binding.</returns>
    public static bool TryParse(ReadOnlySpan<char> text, out ActionBinding binding)
    {
        binding = default;
        text = text.Trim();
        if (text.Equals("stick:left", StringComparison.Ordinal) || text.Equals("stick:right", StringComparison.Ordinal))
        {
            binding = Bind.Stick(text.EndsWith("right", StringComparison.Ordinal));
            return true;
        }

        var parts = 1;
        var shape = BindingShape.One;
        if (text.StartsWith("pair(", StringComparison.Ordinal) && text.EndsWith(")", StringComparison.Ordinal))
        {
            (parts, shape) = (2, BindingShape.Pair);
            text = text[5..^1];
        }
        else if (text.StartsWith("quad(", StringComparison.Ordinal) && text.EndsWith(")", StringComparison.Ordinal))
        {
            (parts, shape) = (4, BindingShape.Quad);
            text = text[5..^1];
        }

        Span<InputSource> s = stackalloc InputSource[4];
        for (var i = 0; i < parts; i++)
        {
            var comma = text.IndexOf(',');
            ReadOnlySpan<char> one;
            if (i == parts - 1)
            {
                if (comma >= 0)
                {
                    return false;
                }

                one = text;
            }
            else
            {
                if (comma < 0)
                {
                    return false;
                }

                one = text[..comma];
                text = text[(comma + 1)..];
            }

            if (!InputSource.TryParse(one, out s[i]))
            {
                return false;
            }
        }

        binding = new ActionBinding(shape, s[0], s[1], s[2], s[3]);
        return true;
    }
}

/// <summary>Short ways to write bindings: <c>Bind.Key(Key.Space)</c>, <c>Bind.Wasd</c>, <c>Bind.LeftStick</c>.</summary>
public static class Bind
{
    /// <summary>W, A, S, D as a vector (by key position, so it works on other layouts too).</summary>
    public static ActionBinding Wasd { get; } = Keys(Platform.Key.A, Platform.Key.D, Platform.Key.W, Platform.Key.S);

    /// <summary>The arrow keys as a vector.</summary>
    public static ActionBinding Arrows { get; } = Keys(Platform.Key.ArrowLeft, Platform.Key.ArrowRight, Platform.Key.ArrowUp, Platform.Key.ArrowDown);

    /// <summary>The pad's d-pad as a vector.</summary>
    public static ActionBinding DPad { get; } = Pads(GamepadButton.DPadLeft, GamepadButton.DPadRight, GamepadButton.DPadUp, GamepadButton.DPadDown);

    /// <summary>The left stick as a vector.</summary>
    public static ActionBinding LeftStick { get; } = Stick(false);

    /// <summary>The right stick as a vector.</summary>
    public static ActionBinding RightStick { get; } = Stick(true);

    /// <summary>A keyboard key.</summary>
    public static InputSource Key(Platform.Key key) => new(InputSourceKind.Key, (int)key);

    /// <summary>A mouse button.</summary>
    public static InputSource Mouse(MouseButton button) => new(InputSourceKind.Mouse, (int)button);

    /// <summary>A pad button (South is A on Xbox and Cross on PlayStation).</summary>
    public static InputSource Pad(GamepadButton button) => new(InputSourceKind.PadButton, (int)button);

    /// <summary>A pad axis. For buttons and composites it counts as pressed in the direction of <paramref name="sign"/>; on a 1D axis action it is the value, inverted when <paramref name="sign"/> is -1.</summary>
    public static InputSource PadAxis(GamepadAxis axis, int sign = 1) => new(InputSourceKind.PadAxis, (int)axis, sign < 0 ? -1 : 1);

    /// <summary>The mouse wheel in notches per frame (positive away from the user).</summary>
    public static InputSource Wheel(int sign = 1, bool horizontal = false) => new(InputSourceKind.Wheel, horizontal ? 1 : 0, sign < 0 ? -1 : 1);

    /// <summary>A finger down in part of the window.</summary>
    public static InputSource Touch(TouchRegion region = TouchRegion.Any) => new(InputSourceKind.Touch, (int)region);

    /// <summary>Two sources making a 1D axis: <paramref name="negative"/> gives -1, <paramref name="positive"/> gives +1.</summary>
    public static ActionBinding Pair(InputSource negative, InputSource positive) => new(BindingShape.Pair, negative, positive);

    /// <summary>Four sources making a 2D vector. Up is -y, down is +y.</summary>
    public static ActionBinding Quad(InputSource left, InputSource right, InputSource up, InputSource down) => new(BindingShape.Quad, left, right, up, down);

    /// <summary>Four keys making a vector.</summary>
    public static ActionBinding Keys(Platform.Key left, Platform.Key right, Platform.Key up, Platform.Key down) => Quad(Key(left), Key(right), Key(up), Key(down));

    /// <summary>Four pad buttons making a vector.</summary>
    public static ActionBinding Pads(GamepadButton left, GamepadButton right, GamepadButton up, GamepadButton down) => Quad(Pad(left), Pad(right), Pad(up), Pad(down));

    /// <summary>A pad stick as a vector.</summary>
    public static ActionBinding Stick(bool right) => new(BindingShape.Stick, new InputSource(InputSourceKind.PadAxis, (int)(right ? GamepadAxis.RightX : GamepadAxis.LeftX)));
}
