using System.Runtime.CompilerServices;
using OneGame.Gpu;
using OneGame.Gpu.Vulkan;
using OneGame.UI;

// Each test makes its own GPU device; real drivers are not happy with several being created and destroyed at once.
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]

namespace OneGame.Render2D.Tests;

internal static class Paths
{
    private static string SourceDir([CallerFilePath] string path = "") => Path.GetDirectoryName(path)!;

    public static string RepoRoot => Path.GetFullPath(Path.Combine(SourceDir(), "..", ".."));

    public static string Golden(string name) => Path.Combine(RepoRoot, "testdata", "render2d", name + ".png");

    public static string TextData(string relative) => Path.Combine(RepoRoot, "testdata", "text", relative);
}

/// <summary>A device with the validation layer required, a renderer and an offscreen target; fails the test if the layer reported anything.</summary>
internal sealed class Rig : IDisposable
{
    private readonly List<IDisposable> _owned = [];

    public Rig(int width, int height)
    {
        Width = width;
        Height = height;
        Device = VulkanGpu.CreateDevice(new GpuDeviceOptions { Validation = true, RequireValidation = true });
        Renderer = Renderer2D.Create(Device, GpuFormat.Rgba8UnormSrgb, GeneratedShaders.Instance);
        Target = Device.CreateTexture(new TextureDesc((uint)width, (uint)height, GpuFormat.Rgba8UnormSrgb, TextureUsage.RenderTarget | TextureUsage.CopySource));
    }

    public int Width { get; }

    public int Height { get; }

    public GpuDevice Device { get; }

    public Renderer2D Renderer { get; }

    public GpuTexture Target { get; }

    public FrameStats Stats { get; private set; }

    public T Own<T>(T disposable)
        where T : IDisposable
    {
        _owned.Add(disposable);
        return disposable;
    }

    /// <summary>Records one frame, submits it and reads the target back as RGBA bytes.</summary>
    public byte[] Render(Action draw, Color? clear = null)
    {
        Renderer.BeginFrame(Target, new FrameOptions { Clear = clear });
        draw();
        var enc = Device.BeginCommands();
        Stats = Renderer.EndFrame(enc);
        Device.Submit(enc);
        var bytes = new byte[Width * Height * 4];
        Device.ReadTexture(Target, 0, 0, bytes);
        return bytes;
    }

    public void Dispose()
    {
        Device.WaitIdle();
        foreach (var o in _owned)
        {
            o.Dispose();
        }

        Renderer.Dispose();
        Target.Dispose();
        Device.WaitIdle();
        var messages = Device.TakeValidationMessages();
        Device.Dispose();
        Assert.True(messages.Length == 0, "validation layer reported:\n" + string.Join("\n", messages));
    }
}

/// <summary>Compares captured pixels with a committed reference image.</summary>
internal static class Golden
{
    /// <summary>Per-channel difference tolerated for a pixel (sRGB byte values).</summary>
    public const int Tolerance = 3;

    /// <summary>Share of pixels allowed to exceed the tolerance: rasterizers disagree a little on edge coverage.</summary>
    public const double EdgeBudget = 0.01;

    public static (int MaxDiff, double OutsideShare) Compare(string name, int w, int h, byte[] actual)
    {
        if (Environment.GetEnvironmentVariable("ONEGAME_CAPTURE_DIR") is { Length: > 0 } dir)
        {
            Directory.CreateDirectory(dir);
            Png.Write(Path.Combine(dir, name + ".png"), w, h, actual);
        }

        var path = Paths.Golden(name);
        if (Environment.GetEnvironmentVariable("ONEGAME_UPDATE_GOLDENS") == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            Png.Write(path, w, h, actual);
        }

        var (gw, gh, expected) = Png.Read(path);
        Assert.Equal((gw, gh), (w, h));
        var max = 0;
        var outside = 0;
        for (var p = 0; p < w * h; p++)
        {
            var worst = 0;
            for (var c = 0; c < 4; c++)
            {
                worst = Math.Max(worst, Math.Abs(actual[(p * 4) + c] - expected[(p * 4) + c]));
            }

            max = Math.Max(max, worst);
            if (worst > Tolerance)
            {
                outside++;
            }
        }

        var share = outside / (double)(w * h);
        if (Environment.GetEnvironmentVariable("ONEGAME_GOLDEN_REPORT") is { Length: > 0 } report)
        {
            File.AppendAllText(report, $"{name} maxDiff={max} outsideTolerance={share:P3}\n");
        }

        return (max, share);
    }

    /// <summary>Appends a measurement to the report file (when ONEGAME_GOLDEN_REPORT is set) and to the test's diagnostics.</summary>
    public static void Note(string text)
    {
        TestContext.Current.SendDiagnosticMessage(text);
        if (Environment.GetEnvironmentVariable("ONEGAME_GOLDEN_REPORT") is { Length: > 0 } report)
        {
            File.AppendAllText(report, text + "\n");
        }
    }

    public static void Check(string name, int w, int h, byte[] actual)
    {
        var (max, share) = Compare(name, w, h, actual);
        Assert.True(share <= EdgeBudget, $"{name}: {share:P2} of pixels differ by more than {Tolerance} (max diff {max})");
    }
}

/// <summary>Small procedural images, so no art files are needed.</summary>
internal static class Pixels
{
    public static byte[] Disc(int size, Color color, int ring = 0)
    {
        var data = new byte[size * size * 4];
        var c = (size - 1) / 2f;
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var d = MathF.Sqrt(((x - c) * (x - c)) + ((y - c) * (y - c)));
                var i = ((y * size) + x) * 4;
                var inside = d <= size / 2f - 1;
                var onRing = ring > 0 && d > size / 2f - 1 - ring && inside;
                var a = inside ? (byte)255 : (byte)0;
                var k = onRing ? 0.35f : 1f;
                data[i] = (byte)(color.R * k);
                data[i + 1] = (byte)(color.G * k);
                data[i + 2] = (byte)(color.B * k);
                data[i + 3] = a;
            }
        }

        return data;
    }

    public static byte[] Solid(int w, int h, Color color)
    {
        var data = new byte[w * h * 4];
        for (var i = 0; i < w * h; i++)
        {
            data[i * 4] = color.R;
            data[(i * 4) + 1] = color.G;
            data[(i * 4) + 2] = color.B;
            data[(i * 4) + 3] = color.A;
        }

        return data;
    }

    /// <summary>A two-colour checkerboard with cells of <paramref name="cell"/> pixels.</summary>
    public static byte[] Checker(int size, int cell, Color a, Color b)
    {
        var data = new byte[size * size * 4];
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var c = (((x / cell) + (y / cell)) & 1) == 0 ? a : b;
                var i = ((y * size) + x) * 4;
                data[i] = c.R;
                data[i + 1] = c.G;
                data[i + 2] = c.B;
                data[i + 3] = c.A;
            }
        }

        return data;
    }

    public static (byte R, byte G, byte B, byte A) At(byte[] rgba, int width, int x, int y)
    {
        var i = ((y * width) + x) * 4;
        return (rgba[i], rgba[i + 1], rgba[i + 2], rgba[i + 3]);
    }
}
