using System.Diagnostics;
using RecursiveAI.OneGpu;

namespace OneGame.Render3D.Tests;

/// <summary>The off-thread billboard upload gives the same texture as the synchronous one and leaves the calling thread free.</summary>
public class BillboardLoadTests
{
    private static byte[] Pixels(int w, int h)
    {
        var px = new byte[w * h * 4];
        var rng = new Random(7);
        rng.NextBytes(px);
        return px;
    }

    // The original loop, kept here as the reference the new code must match.
    private static byte[] Reference(byte[] rgba)
    {
        static float Dec(byte b)
        {
            var c = b / 255f;
            return c <= 0.04045f ? c / 12.92f : MathF.Pow((c + 0.055f) / 1.055f, 2.4f);
        }

        static byte Enc(float l)
        {
            l = Math.Clamp(l, 0f, 1f);
            var c = l <= 0.0031308f ? l * 12.92f : (1.055f * MathF.Pow(l, 1f / 2.4f)) - 0.055f;
            return (byte)MathF.Round(c * 255f);
        }

        var data = new byte[rgba.Length];
        for (var i = 0; i < data.Length; i += 4)
        {
            var a = rgba[i + 3] / 255f;
            for (var c = 0; c < 3; c++)
            {
                data[i + c] = Enc(Dec(rgba[i + c]) * a);
            }

            data[i + 3] = rgba[i + 3];
        }

        return data;
    }

    [Fact]
    public void PremultiplyMatchesTheOriginalLoopByteForByte()
    {
        var src = Pixels(333, 77);
        var expect = Reference(src);
        var got = new byte[src.Length];
        Renderer3D.PremultiplyRows(src, got, 0, src.Length);
        Assert.Equal(expect, got);
    }

    [Fact]
    public void AsyncLoadMakesTheSameTextureAndTheCallerIsNotHeldUp()
    {
        using var rig = new Rig(64, 64, GpuFormat.Rgba8UnormSrgb);
        const int w = 2048;
        const int h = 2048;
        var src = Pixels(w, h);

        // Warm the code paths once so timings exclude JIT.
        rig.Own(rig.Renderer.CreateBillboardTexture(16, 16, Pixels(16, 16)));
        rig.Own(rig.Renderer.BeginCreateBillboardTexture(16, 16, Pixels(16, 16)).Complete());

        var sw = Stopwatch.StartNew();
        Reference(src);
        var oldMs = sw.Elapsed.TotalMilliseconds;
        sw.Restart();
        var sync = rig.Own(rig.Renderer.CreateBillboardTexture(w, h, src, "sync"));
        var syncMs = sw.Elapsed.TotalMilliseconds;

        sw.Restart();
        var load = rig.Renderer.BeginCreateBillboardTexture(w, h, src, "async");
        var beginMs = sw.Elapsed.TotalMilliseconds;
        while (!load.IsReady)
        {
            Thread.Sleep(1);
        }

        sw.Restart();
        var made = rig.Own(load.Complete());
        var completeMs = sw.Elapsed.TotalMilliseconds;
        Assert.Same(made, load.Complete());
        Assert.Equal((w, h), (made.Width, made.Height));

        Console.WriteLine($"billboard {w}x{h}: original loop {oldMs:F1} ms, CreateBillboardTexture {syncMs:F1} ms on the caller; Begin {beginMs:F2} ms + Complete {completeMs:F1} ms on the caller");
        Assert.True(beginMs < syncMs / 2, $"Begin took {beginMs:F1} ms against {syncMs:F1} ms");
    }
}
