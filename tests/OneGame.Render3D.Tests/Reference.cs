namespace OneGame.Render3D.Tests;

/// <summary>
/// Compares a capture with a committed reference image drawn by the software rasterizer (lavapipe, which is deterministic). The reference
/// is rewritten when ONEGAME_UPDATE_REFERENCES=1, so a change can be looked at before it is committed.
/// </summary>
internal static class Reference
{
    public static void Compare(string name, byte[] pixels, int width, int height, double meanTolerance, int maxTolerance, double outlierFraction = 0.005)
    {
        var path = Paths.Reference(Path.Combine("m4", name + ".png"));
        if (Environment.GetEnvironmentVariable("ONEGAME_UPDATE_REFERENCES") == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            Png.Write(path, width, height, pixels);
            return;
        }

        Assert.True(File.Exists(path), $"missing reference {path}; run with ONEGAME_UPDATE_REFERENCES=1 to create it");
        var reference = Png.Read(path);
        Assert.Equal((width, height), (reference.Width, reference.Height));
        double sum = 0;
        var outliers = 0;
        for (var i = 0; i < width * height; i++)
        {
            var worst = 0;
            for (var c = 0; c < 3; c++)
            {
                var d = Math.Abs(pixels[(i * 4) + c] - reference.Rgba[(i * 4) + c]);
                sum += d;
                worst = Math.Max(worst, d);
            }

            if (worst > maxTolerance)
            {
                outliers++;
            }
        }

        var mean = sum / (width * height * 3.0);
        Assert.True(mean <= meanTolerance, $"{name}: mean difference {mean:F3} exceeds {meanTolerance}");
        Assert.True(outliers <= width * height * outlierFraction, $"{name}: {outliers} pixels differ by more than {maxTolerance}");
    }
}
