using System.Globalization;
using System.Numerics;

namespace OneGame.UI;

/// <summary>The unit of a <see cref="Length"/>.</summary>
public enum LengthUnit : byte
{
    /// <summary>No value: CSS <c>auto</c> (or <c>none</c> for maximum sizes).</summary>
    Auto,
    /// <summary>Device pixels; unaffected by the UI scale.</summary>
    Px,
    /// <summary>A percentage of the reference box for the property, as in CSS.</summary>
    Percent,
    /// <summary>UI units: one unit is <see cref="UiTree.Scale"/> pixels, so a layout written in UI units follows the player's UI scale setting.</summary>
    Ui,
}

/// <summary>A CSS-style length: a number and a unit. A bare number converts implicitly to pixels.</summary>
/// <param name="Value">The number, in <paramref name="Unit"/>.</param>
/// <param name="Unit">The unit.</param>
public readonly record struct Length(float Value, LengthUnit Unit)
{
    /// <summary>The <c>auto</c> length.</summary>
    public static Length Auto => default;

    /// <summary>Pixels.</summary>
    public static Length Px(float value) => new(value, LengthUnit.Px);

    /// <summary>A percentage (50 means half).</summary>
    public static Length Percent(float value) => new(value, LengthUnit.Percent);

    /// <summary>UI units, scaled by <see cref="UiTree.Scale"/>.</summary>
    public static Length Ui(float value) => new(value, LengthUnit.Ui);

    /// <summary>True for <c>auto</c>.</summary>
    public bool IsAuto => Unit == LengthUnit.Auto;

    /// <summary>Converts a pixel count to a length.</summary>
    public static implicit operator Length(float px) => Px(px);

    /// <summary>The CSS spelling, such as <c>12px</c>, <c>50%</c>, <c>2ui</c> or <c>auto</c>.</summary>
    public override string ToString() => Unit switch
    {
        LengthUnit.Auto => "auto",
        LengthUnit.Px => Num.Format(Value) + "px",
        LengthUnit.Percent => Num.Format(Value) + "%",
        _ => Num.Format(Value) + "ui",
    };
}

internal static class Num
{
    public static string Format(float v) => v.ToString("0.####", CultureInfo.InvariantCulture);
}

/// <summary>An 8-bit-per-channel RGBA color (not premultiplied).</summary>
/// <param name="R">Red.</param>
/// <param name="G">Green.</param>
/// <param name="B">Blue.</param>
/// <param name="A">Alpha; 255 is opaque.</param>
public readonly record struct Color(byte R, byte G, byte B, byte A = 255)
{
    /// <summary>Fully transparent black.</summary>
    public static Color Transparent => new(0, 0, 0, 0);

    /// <summary>Opaque black.</summary>
    public static Color Black => new(0, 0, 0);

    /// <summary>Opaque white.</summary>
    public static Color White => new(255, 255, 255);

    /// <summary>Builds an opaque color from <c>0xRRGGBB</c>.</summary>
    public static Color Rgb(uint rgb) => new((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);

    /// <summary>Builds a color from <c>0xRRGGBBAA</c>.</summary>
    public static Color Rgba(uint rgba) => new((byte)(rgba >> 24), (byte)(rgba >> 16), (byte)(rgba >> 8), (byte)rgba);

    /// <summary>Parses <c>#rgb</c>, <c>#rrggbb</c> or <c>#rrggbbaa</c>.</summary>
    /// <exception cref="FormatException">The text is not one of those forms.</exception>
    public static Color Hex(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var s = text.AsSpan();
        if (s.Length > 0 && s[0] == '#')
        {
            s = s[1..];
        }

        if (s.Length is 3 && uint.TryParse(s, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var v3))
        {
            var r = (byte)(((v3 >> 8) & 0xF) * 17);
            var g = (byte)(((v3 >> 4) & 0xF) * 17);
            var b = (byte)((v3 & 0xF) * 17);
            return new Color(r, g, b);
        }

        if (s.Length is 6 && uint.TryParse(s, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var v6))
        {
            return Rgb(v6);
        }

        if (s.Length is 8 && uint.TryParse(s, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var v8))
        {
            return Rgba(v8);
        }

        throw new FormatException($"'{text}' is not a color; use #rgb, #rrggbb or #rrggbbaa.");
    }

    /// <summary>This color with its alpha multiplied by <paramref name="factor"/> (0..1).</summary>
    public Color WithOpacity(float factor) => this with { A = (byte)Math.Clamp(MathF.Round(A * factor), 0, 255) };

    internal uint Pack() => ((uint)R << 24) | ((uint)G << 16) | ((uint)B << 8) | A;

    internal static Color Unpack(uint v) => Rgba(v);

    /// <summary>The CSS hex spelling; the alpha digits appear only when not opaque.</summary>
    public override string ToString() => A == 255 ? $"#{R:x2}{G:x2}{B:x2}" : $"#{R:x2}{G:x2}{B:x2}{A:x2}";
}

/// <summary>An axis-aligned rectangle in pixels.</summary>
/// <param name="X">Left edge.</param>
/// <param name="Y">Top edge.</param>
/// <param name="Width">Width.</param>
/// <param name="Height">Height.</param>
public readonly record struct Rect(float X, float Y, float Width, float Height)
{
    /// <summary>The right edge.</summary>
    public float Right => X + Width;

    /// <summary>The bottom edge.</summary>
    public float Bottom => Y + Height;

    /// <summary>True when the point lies inside (left/top inclusive, right/bottom exclusive).</summary>
    public bool Contains(float x, float y) => x >= X && y >= Y && x < X + Width && y < Y + Height;

    /// <summary>The overlap with <paramref name="other"/>, or an empty rectangle.</summary>
    public Rect Intersect(Rect other)
    {
        var x = MathF.Max(X, other.X);
        var y = MathF.Max(Y, other.Y);
        var r = MathF.Min(Right, other.Right);
        var b = MathF.Min(Bottom, other.Bottom);
        return r <= x || b <= y ? new Rect(x, y, 0, 0) : new Rect(x, y, r - x, b - y);
    }

    internal Vector2 Center => new(X + (Width / 2), Y + (Height / 2));
}

/// <summary>Four per-side values (border widths, nine-slice insets).</summary>
/// <param name="Left">Left side.</param>
/// <param name="Top">Top side.</param>
/// <param name="Right">Right side.</param>
/// <param name="Bottom">Bottom side.</param>
public readonly record struct Edges(float Left, float Top, float Right, float Bottom)
{
    /// <summary>True when every side is zero.</summary>
    public bool IsZero => Left == 0 && Top == 0 && Right == 0 && Bottom == 0;
}

/// <summary>Four corner radii in pixels.</summary>
/// <param name="TopLeft">Top-left corner.</param>
/// <param name="TopRight">Top-right corner.</param>
/// <param name="BottomRight">Bottom-right corner.</param>
/// <param name="BottomLeft">Bottom-left corner.</param>
public readonly record struct CornerRadii(float TopLeft, float TopRight, float BottomRight, float BottomLeft)
{
    /// <summary>True when every corner is square.</summary>
    public bool IsZero => TopLeft == 0 && TopRight == 0 && BottomRight == 0 && BottomLeft == 0;
}
