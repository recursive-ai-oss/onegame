using OneGame.Host;

namespace OneGame.Testing;

/// <summary>How two images differ.</summary>
/// <param name="SameSize">False when the sizes differ; the other fields are then zero.</param>
/// <param name="Width">Compared width.</param>
/// <param name="Height">Compared height.</param>
/// <param name="DifferingPixels">Pixels where some channel differs by more than the tolerance.</param>
/// <param name="Fraction">Differing pixels as a fraction of all pixels.</param>
/// <param name="MaxChannelDelta">The largest difference of any channel of any pixel (regardless of tolerance).</param>
public readonly record struct ImageDiff(bool SameSize, int Width, int Height, long DifferingPixels, double Fraction, int MaxChannelDelta)
{
    /// <summary>Whether the images match when up to <paramref name="maxFraction"/> of the pixels may differ.</summary>
    public bool Within(double maxFraction) => SameSize && Fraction <= maxFraction;
}

/// <summary>Per-pixel image comparison for capture checks.</summary>
public static class ImageCompare
{
    /// <summary>Compares two frames. A pixel differs when any of its four channels differs by more than <paramref name="channelTolerance"/>.</summary>
    public static ImageDiff Compare(CapturedFrame actual, CapturedFrame expected, int channelTolerance = 0)
    {
        ArgumentNullException.ThrowIfNull(actual);
        ArgumentNullException.ThrowIfNull(expected);
        if (actual.Width != expected.Width || actual.Height != expected.Height)
        {
            return new ImageDiff(false, 0, 0, 0, 1, 0);
        }

        long differing = 0;
        var max = 0;
        var a = actual.Rgba;
        var e = expected.Rgba;
        for (var i = 0; i + 3 < a.Length; i += 4)
        {
            var worst = 0;
            for (var c = 0; c < 4; c++)
            {
                worst = Math.Max(worst, Math.Abs(a[i + c] - e[i + c]));
            }

            max = Math.Max(max, worst);
            if (worst > channelTolerance)
            {
                differing++;
            }
        }

        var total = (long)actual.Width * actual.Height;
        return new ImageDiff(true, actual.Width, actual.Height, differing, total == 0 ? 0 : (double)differing / total, max);
    }

    /// <summary>An image that shows where two same-sized frames differ: the expected frame dimmed, with differing pixels in red.</summary>
    public static CapturedFrame DiffImage(CapturedFrame actual, CapturedFrame expected, int channelTolerance = 0)
    {
        ArgumentNullException.ThrowIfNull(actual);
        ArgumentNullException.ThrowIfNull(expected);
        var w = Math.Min(actual.Width, expected.Width);
        var h = Math.Min(actual.Height, expected.Height);
        var pixels = new byte[w * h * 4];
        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                var o = ((y * w) + x) * 4;
                var ia = ((y * actual.Width) + x) * 4;
                var ie = ((y * expected.Width) + x) * 4;
                var worst = 0;
                for (var c = 0; c < 4; c++)
                {
                    worst = Math.Max(worst, Math.Abs(actual.Rgba[ia + c] - expected.Rgba[ie + c]));
                }

                if (worst > channelTolerance)
                {
                    pixels[o] = 255;
                    pixels[o + 3] = 255;
                }
                else
                {
                    pixels[o] = (byte)(expected.Rgba[ie] / 3);
                    pixels[o + 1] = (byte)(expected.Rgba[ie + 1] / 3);
                    pixels[o + 2] = (byte)(expected.Rgba[ie + 2] / 3);
                    pixels[o + 3] = 255;
                }
            }
        }

        return new CapturedFrame(w, h, pixels, "diff");
    }
}
