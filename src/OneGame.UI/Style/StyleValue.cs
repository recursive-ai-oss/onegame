namespace OneGame.UI;

internal enum ValueKind : byte
{
    None,
    Length,
    Number,
    Color,
    Enum,
    Text,
}

/// <summary>A value for a style property or a theme token: a length, a number, a color, a keyword or a string.</summary>
public readonly struct StyleValue : IEquatable<StyleValue>
{
    internal readonly ValueKind Kind;
    internal readonly float F;
    internal readonly uint Bits;
    internal readonly LengthUnit Unit;
    internal readonly string? Text;

    private StyleValue(ValueKind kind, float f, uint bits, LengthUnit unit, string? text)
    {
        Kind = kind;
        F = f;
        Bits = bits;
        Unit = unit;
        Text = text;
    }

    /// <summary>A plain number (for <c>opacity</c>, <c>flex-grow</c>, <c>z-index</c> and similar).</summary>
    public static StyleValue Number(float value) => new(ValueKind.Number, value, 0, LengthUnit.Auto, null);

    /// <summary>A length.</summary>
    public static implicit operator StyleValue(Length value) => new(ValueKind.Length, value.Value, 0, value.Unit, null);

    /// <summary>A color.</summary>
    public static implicit operator StyleValue(Color value) => new(ValueKind.Color, 0, value.Pack(), LengthUnit.Auto, null);

    /// <summary>A string (font family, image id, transition list).</summary>
    public static implicit operator StyleValue(string value) => new(ValueKind.Text, 0, 0, LengthUnit.Auto, value);

    internal static StyleValue OfEnum(int value) => new(ValueKind.Enum, 0, (uint)value, LengthUnit.Auto, null);

    internal Length AsLength => new(F, Unit);

    internal Color AsColor => Color.Unpack(Bits);

    internal int AsEnum => (int)Bits;

    /// <inheritdoc/>
    public bool Equals(StyleValue other) =>
        Kind == other.Kind && F.Equals(other.F) && Bits == other.Bits && Unit == other.Unit && string.Equals(Text, other.Text, StringComparison.Ordinal);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is StyleValue v && Equals(v);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Kind, F, Bits, Unit, Text);

    /// <summary>Equality.</summary>
    public static bool operator ==(StyleValue a, StyleValue b) => a.Equals(b);

    /// <summary>Inequality.</summary>
    public static bool operator !=(StyleValue a, StyleValue b) => !a.Equals(b);

    /// <summary>The CSS spelling of the value (keywords need the property, so enums print as numbers here).</summary>
    public override string ToString() => Kind switch
    {
        ValueKind.Length => AsLength.ToString(),
        ValueKind.Number => Num.Format(F),
        ValueKind.Color => AsColor.ToString(),
        ValueKind.Enum => Bits.ToString(System.Globalization.CultureInfo.InvariantCulture),
        ValueKind.Text => Text ?? "",
        _ => "",
    };
}
