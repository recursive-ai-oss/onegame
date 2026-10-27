using OneGame.Tweens;

namespace OneGame.UI;

// Value interpolation shared by tweens and style transitions.
internal static class Interp
{
    /// <summary>Blends two colors the way CSS does for legacy sRGB colors: each channel premultiplied by alpha, mixed, then divided by the mixed alpha (CSS Color 4 §12.3).</summary>
    public static Color Mix(Color a, Color b, float e)
    {
        var aa = a.A / 255f;
        var ba = b.A / 255f;
        var alpha = TweenSet.Mix(aa, ba, e);
        if (alpha <= 0)
        {
            return Color.Transparent;
        }

        return new Color(
            Channel(TweenSet.Mix(a.R * aa, b.R * ba, e) / alpha),
            Channel(TweenSet.Mix(a.G * aa, b.G * ba, e) / alpha),
            Channel(TweenSet.Mix(a.B * aa, b.B * ba, e) / alpha),
            Channel(alpha * 255f));
    }

    private static byte Channel(float v) => (byte)Math.Clamp(MathF.Round(v), 0, 255);
}
