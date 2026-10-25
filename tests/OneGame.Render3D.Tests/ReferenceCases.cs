using System.Text.Json;
using OneGame.Gltf;

namespace OneGame.Render3D.Tests;

/// <summary>One entry of testdata/render3d/cases.json.</summary>
internal sealed class ReferenceCase
{
    public required string Name { get; init; }

    public required string Model { get; init; }

    public required Camera3D Camera { get; init; }

    public string? Env { get; init; }

    public bool Ibl { get; init; }

    public List<Light3D> Lights { get; } = [];

    public string? PoseAnimation { get; init; }

    public float PoseTime { get; init; }

    public static (int Width, int Height, Vector3 Background, List<ReferenceCase> Cases) LoadAll()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(Paths.RepoRoot, "testdata", "render3d", "cases.json")));
        var root = doc.RootElement;
        var size = root.GetProperty("size");
        var hex = root.GetProperty("background").GetProperty("color").GetString()!;
        var bg = new Vector3(Linear(Convert.ToInt32(hex.Substring(1, 2), 16)), Linear(Convert.ToInt32(hex.Substring(3, 2), 16)), Linear(Convert.ToInt32(hex.Substring(5, 2), 16)));
        var cases = new List<ReferenceCase>();
        foreach (var c in root.GetProperty("cases").EnumerateArray())
        {
            var cam = c.GetProperty("camera");
            var camera = Camera3D.LookAt(V3(cam.GetProperty("eye")), V3(cam.GetProperty("target")), V3(cam.GetProperty("up")));
            camera.YFov = cam.GetProperty("yfov").GetSingle();
            camera.Near = cam.GetProperty("near").GetSingle();
            camera.Far = cam.GetProperty("far").GetSingle();
            var pose = c.TryGetProperty("pose", out var p) ? p : default;
            var rc = new ReferenceCase
            {
                Name = c.GetProperty("name").GetString()!,
                Model = c.GetProperty("model").GetString()!,
                Camera = camera,
                Env = c.GetProperty("env").ValueKind == JsonValueKind.String ? c.GetProperty("env").GetString() : null,
                Ibl = c.GetProperty("ibl").GetBoolean(),
                PoseAnimation = pose.ValueKind == JsonValueKind.Object ? pose.GetProperty("animation").GetString() : null,
                PoseTime = pose.ValueKind == JsonValueKind.Object ? pose.GetProperty("time").GetSingle() : 0,
            };
            if (c.TryGetProperty("lights", out var lights))
            {
                foreach (var l in lights.EnumerateArray())
                {
                    var color = V3(l.GetProperty("color"));
                    var intensity = l.GetProperty("intensity").GetSingle();
                    var range = l.TryGetProperty("range", out var r) ? r.GetSingle() : 0f;
                    rc.Lights.Add(l.GetProperty("type").GetString() switch
                    {
                        "directional" => Light3D.Directional(V3(l.GetProperty("direction")), color, intensity),
                        "point" => Light3D.Point(V3(l.GetProperty("position")), color, intensity, range),
                        _ => Light3D.Spot(V3(l.GetProperty("position")), V3(l.GetProperty("direction")), color, intensity, l.GetProperty("inner").GetSingle(), l.GetProperty("outer").GetSingle(), range),
                    });
                }
            }

            cases.Add(rc);
        }

        return (size[0].GetInt32(), size[1].GetInt32(), bg, cases);
    }

    private static Vector3 V3(JsonElement e) => new(e[0].GetSingle(), e[1].GetSingle(), e[2].GetSingle());

    private static float Linear(int v)
    {
        var c = v / 255f;
        return c <= 0.04045f ? c / 12.92f : MathF.Pow((c + 0.055f) / 1.055f, 2.4f);
    }
}

/// <summary>How far a capture is from a reference image, over the pixels the model covers.</summary>
internal readonly record struct Difference(int Pixels, double MeanAbs, int P95, double Over32)
{
    public override string ToString() => $"pixels={Pixels} meanAbs={MeanAbs:F2} p95={P95} over32={Over32:P2}";
}

internal static class Compare
{
    /// <summary>
    /// Compares two 8-bit sRGB images over the model's pixels: those where the reference differs from its flat background colour,
    /// eroded by <paramref name="erode"/> pixels so that edge coverage (which depends on the rasterizer and its anti-aliasing) does not count.
    /// Reports the mean absolute per-channel difference, the 95th percentile of the worst channel per pixel, and the share of pixels
    /// whose worst channel differs by more than 32.
    /// </summary>
    public static Difference Measure(int w, int h, byte[] reference, byte[] actual, int erode = 2, byte[]? diffOut = null)
    {
        var bg = (reference[0], reference[1], reference[2]);
        var mask = new bool[w * h];
        for (var i = 0; i < w * h; i++)
        {
            mask[i] = Math.Abs(reference[i * 4] - bg.Item1) + Math.Abs(reference[(i * 4) + 1] - bg.Item2) + Math.Abs(reference[(i * 4) + 2] - bg.Item3) > 6;
        }

        var eroded = new bool[w * h];
        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                var keep = mask[(y * w) + x];
                for (var dy = -erode; dy <= erode && keep; dy++)
                {
                    for (var dx = -erode; dx <= erode && keep; dx++)
                    {
                        var xx = x + dx;
                        var yy = y + dy;
                        keep = xx >= 0 && yy >= 0 && xx < w && yy < h && mask[(yy * w) + xx];
                    }
                }

                eroded[(y * w) + x] = keep;
            }
        }

        var worst = new List<int>();
        long sum = 0;
        var over = 0;
        for (var i = 0; i < w * h; i++)
        {
            var m = 0;
            for (var c = 0; c < 3; c++)
            {
                var d = Math.Abs(reference[(i * 4) + c] - actual[(i * 4) + c]);
                m = Math.Max(m, d);
                if (eroded[i])
                {
                    sum += d;
                }

                if (diffOut is not null)
                {
                    diffOut[(i * 4) + c] = (byte)Math.Min(255, d * 4);
                }
            }

            if (diffOut is not null)
            {
                diffOut[(i * 4) + 3] = 255;
            }

            if (eroded[i])
            {
                worst.Add(m);
                if (m > 32)
                {
                    over++;
                }
            }
        }

        if (worst.Count == 0)
        {
            return new Difference(0, 0, 0, 0);
        }

        worst.Sort();
        return new Difference(worst.Count, sum / (3.0 * worst.Count), worst[(int)(worst.Count * 0.95)], over / (double)worst.Count);
    }

    /// <summary>Reference, capture and the difference (times four) side by side.</summary>
    public static byte[] Triptych(int w, int h, byte[] reference, byte[] actual, byte[] diff)
    {
        var o = new byte[w * 3 * h * 4];
        for (var y = 0; y < h; y++)
        {
            Array.Copy(reference, y * w * 4, o, y * w * 12, w * 4);
            Array.Copy(actual, y * w * 4, o, (y * w * 12) + (w * 4), w * 4);
            Array.Copy(diff, y * w * 4, o, (y * w * 12) + (w * 8), w * 4);
        }

        return o;
    }
}
