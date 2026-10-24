using System.Runtime.CompilerServices;
using OneGame.Atlas;
using OneGame.Text;
using OneGame.UI;
using OneImage;

namespace OneGame.Text.Tests;

/// <summary>A white RGBA canvas that glyph quads are blended onto on the CPU, the way a renderer's text shader does it.</summary>
internal sealed class CpuCanvas
{
    public CpuCanvas(int width, int height)
    {
        Width = width;
        Height = height;
        Pixels = new Rgba32[width * height];
        Array.Fill(Pixels, new Rgba32(255, 255, 255, 255));
    }

    public int Width { get; }

    public int Height { get; }

    public Rgba32[] Pixels { get; }

    public void Draw(IEnumerable<GlyphQuad> quads, GlyphAtlas atlas)
    {
        foreach (var q in quads)
        {
            var page = atlas.Pages[q.Page];
            var px = (int)MathF.Round(q.U0 * page.Width);
            var py = (int)MathF.Round(q.V0 * page.Height);
            var w = (int)MathF.Round(q.X1 - q.X0);
            var h = (int)MathF.Round(q.Y1 - q.Y0);
            var ox = (int)q.X0;
            var oy = (int)q.Y0;
            Assert.Equal(q.X0, ox);
            Assert.Equal(q.Y0, oy);
            for (var y = 0; y < h; y++)
            {
                for (var x = 0; x < w; x++)
                {
                    var dx = ox + x;
                    var dy = oy + y;
                    if (dx < 0 || dy < 0 || dx >= Width || dy >= Height)
                    {
                        continue;
                    }

                    var a = page.Pixels[((py + y) * page.Stride) + px + x] / 255f * (q.Color.A / 255f);
                    ref var d = ref Pixels[(dy * Width) + dx];
                    d = new Rgba32(Mix(d.R, q.Color.R, a), Mix(d.G, q.Color.G, a), Mix(d.B, q.Color.B, a), 255);
                }
            }
        }
    }

    private static byte Mix(byte dst, byte src, float a) => (byte)MathF.Round((dst * (1 - a)) + (src * a));

    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        Image.FromPixels<Rgba32>(Pixels, Width, Height).Save(path);
    }
}

internal static class Golden
{
    public static string Dir([CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "testdata", "text", "ref"));

    /// <summary>
    /// Compares a canvas with a committed PNG: no pixel may differ by more than <paramref name="maxChannelDiff"/> levels and the mean
    /// difference stays under 0.05 levels. ONEGAME_UPDATE_GOLDEN=1 rewrites the file after an intended change.
    /// </summary>
    public static void Check(string name, int width, int height, Rgba32[] actual, int maxChannelDiff = 3)
    {
        var path = Path.Combine(Dir(), name);
        if (Environment.GetEnvironmentVariable("ONEGAME_UPDATE_GOLDEN") == "1")
        {
            Directory.CreateDirectory(Dir());
            Image.FromPixels<Rgba32>(actual, width, height).Save(path);
        }

        var expected = Image.Load(path);
        Assert.Equal((width, height), (expected.Width, expected.Height));
        var exp = expected.ConvertTo(PixelFormat.Rgba8).ToArray<Rgba32>();
        var worst = 0;
        long total = 0;
        for (var i = 0; i < exp.Length; i++)
        {
            var d = Math.Max(Math.Max(Math.Abs(exp[i].R - actual[i].R), Math.Abs(exp[i].G - actual[i].G)), Math.Max(Math.Abs(exp[i].B - actual[i].B), Math.Abs(exp[i].A - actual[i].A)));
            worst = Math.Max(worst, d);
            total += d;
        }

        var mean = (double)total / exp.Length;
        Assert.True(worst <= maxChannelDiff && mean < 0.05, $"{name}: max channel difference {worst} (limit {maxChannelDiff}), mean {mean:F4}");
    }
}
