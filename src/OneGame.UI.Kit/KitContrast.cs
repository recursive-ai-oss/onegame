using System.Globalization;

namespace OneGame.UI.Kit;

/// <summary>
/// Color contrast as WCAG 2.x defines it (Understanding SC 1.4.3 and 1.4.11): relative luminance of sRGB, and the ratio <c>(L1 + 0.05) / (L2 + 0.05)</c>.
/// The theme uses it to pick legible state colors, and <see cref="ThemeLint"/> uses it to report the pairs that fail.
/// </summary>
public static class KitContrast
{
    /// <summary>The ratio normal text must reach (WCAG 1.4.3 AA).</summary>
    public const double TextAa = 4.5;

    /// <summary>The ratio large text and non-text indicators such as a focus ring must reach (WCAG 1.4.3 and 1.4.11 AA).</summary>
    public const double IndicatorAa = 3.0;

    /// <summary>The relative luminance of a color, 0 (black) to 1 (white). Alpha is ignored; composite first with <see cref="Over"/>.</summary>
    public static double Luminance(Color c) =>
        (0.2126 * Linear(c.R)) + (0.7152 * Linear(c.G)) + (0.0722 * Linear(c.B));

    /// <summary>The contrast ratio between two colors, 1 to 21. A translucent foreground is first composited over the background.</summary>
    public static double Ratio(Color foreground, Color background)
    {
        var a = Luminance(Over(foreground, background));
        var b = Luminance(background);
        var (hi, lo) = a >= b ? (a, b) : (b, a);
        return (hi + 0.05) / (lo + 0.05);
    }

    /// <summary>Composites <paramref name="top"/> over an opaque <paramref name="bottom"/>.</summary>
    public static Color Over(Color top, Color bottom)
    {
        if (top.A == 255)
        {
            return top;
        }

        var t = top.A / 255.0;
        return new Color(Channel((top.R * t) + (bottom.R * (1 - t))), Channel((top.G * t) + (bottom.G * (1 - t))), Channel((top.B * t) + (bottom.B * (1 - t))));
    }

    /// <summary>Blends two colors in sRGB: <paramref name="t"/> 0 gives <paramref name="a"/>, 1 gives <paramref name="b"/>. The result is opaque.</summary>
    public static Color Mix(Color a, Color b, double t)
    {
        t = Math.Clamp(t, 0, 1);
        return new Color(Channel((a.R * (1 - t)) + (b.R * t)), Channel((a.G * (1 - t)) + (b.G * t)), Channel((a.B * (1 - t)) + (b.B * t)));
    }

    /// <summary>
    /// Returns <paramref name="color"/> unchanged when it reaches <paramref name="minimum"/> against every background; otherwise the nearest color
    /// on the way to white or to black (whichever gets there with the smaller change) that does. When no color can, the better of black and white.
    /// </summary>
    /// <param name="color">The wanted color.</param>
    /// <param name="backgrounds">The colors it is drawn on.</param>
    /// <param name="minimum">The ratio a color must reach to be left alone.</param>
    /// <param name="repairTo">The ratio to reach when it has to be changed (at least <paramref name="minimum"/>; 0 for the same). A repaired color gets some margin, so it does not sit exactly on the line.</param>
    public static Color Ensure(Color color, ReadOnlySpan<Color> backgrounds, double minimum, double repairTo = 0)
    {
        if (Worst(color, backgrounds) >= minimum)
        {
            return color;
        }

        minimum = Math.Max(minimum, repairTo);
        if (Worst(Color.White, backgrounds) < minimum && Worst(Color.Black, backgrounds) < minimum)
        {
            minimum = Math.Min(minimum, Math.Max(Worst(Color.White, backgrounds), Worst(Color.Black, backgrounds)));
        }

        var toWhite = Reach(color, Color.White, backgrounds, minimum);
        var toBlack = Reach(color, Color.Black, backgrounds, minimum);
        return (toWhite, toBlack) switch
        {
            ({ } w, { } b) => Distance(color, w) <= Distance(color, b) ? w : b,
            ({ } w, null) => w,
            (null, { } b) => b,
            _ => Worst(Color.White, backgrounds) >= Worst(Color.Black, backgrounds) ? Color.White : Color.Black,
        };
    }

    /// <summary>The lowest ratio of <paramref name="color"/> against any of the backgrounds.</summary>
    public static double Worst(Color color, ReadOnlySpan<Color> backgrounds)
    {
        var worst = double.MaxValue;
        foreach (var bg in backgrounds)
        {
            worst = Math.Min(worst, Ratio(color, bg));
        }

        return worst;
    }

    /// <summary>A color as <c>#rrggbb</c> (with alpha as <c>#rrggbbaa</c> when it is not opaque).</summary>
    public static string ToHex(Color c) => c.A == 255
        ? string.Create(CultureInfo.InvariantCulture, $"#{c.R:x2}{c.G:x2}{c.B:x2}")
        : string.Create(CultureInfo.InvariantCulture, $"#{c.R:x2}{c.G:x2}{c.B:x2}{c.A:x2}");

    private static Color? Reach(Color from, Color to, ReadOnlySpan<Color> backgrounds, double minimum)
    {
        if (Worst(to, backgrounds) < minimum)
        {
            return null;
        }

        double lo = 0, hi = 1;
        for (var i = 0; i < 24; i++)
        {
            var mid = (lo + hi) / 2;
            if (Worst(Mix(from, to, mid), backgrounds) >= minimum)
            {
                hi = mid;
            }
            else
            {
                lo = mid;
            }
        }

        return Mix(from, to, hi);
    }

    private static double Distance(Color a, Color b)
    {
        double dr = a.R - b.R, dg = a.G - b.G, db = a.B - b.B;
        return (dr * dr) + (dg * dg) + (db * db);
    }

    private static double Linear(byte v)
    {
        var c = v / 255.0;
        return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
    }

    private static byte Channel(double v) => (byte)Math.Clamp(Math.Round(v), 0, 255);
}
