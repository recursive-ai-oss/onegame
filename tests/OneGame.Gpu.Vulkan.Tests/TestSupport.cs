using System.Runtime.CompilerServices;

namespace OneGame.Gpu.Vulkan.Tests;

/// <summary>Creates a device with the validation layer required, and fails the test if the layer reported anything.</summary>
internal sealed class GpuFixture : IDisposable
{
    public GpuDevice Device { get; }

    public GpuFixture()
    {
        Device = VulkanGpu.CreateDevice(new GpuDeviceOptions { Validation = true, RequireValidation = true });
    }

    public void Dispose()
    {
        Device.WaitIdle();
        var messages = Device.TakeValidationMessages();
        Device.Dispose();
        Assert.True(messages.Length == 0, "validation layer reported:\n" + string.Join("\n", messages));
    }
}

internal static class TestFiles
{
    static string SourceDir([CallerFilePath] string path = "") => Path.GetDirectoryName(path)!;

    public static string RepoRoot => Path.GetFullPath(Path.Combine(SourceDir(), "..", ".."));

    public static byte[] Spirv(string name) => File.ReadAllBytes(Path.Combine(SourceDir(), "Shaders", name + ".spv"));

    public static string GoldenPath(string name) => Path.Combine(RepoRoot, "testdata", "gpu", name + ".png");
}

/// <summary>Compares captured pixels with a committed reference image.</summary>
internal static class Golden
{
    /// <summary>Per-channel tolerance for pixels away from primitive edges.</summary>
    public const int Tolerance = 2;

    /// <summary>Share of pixels that may exceed the tolerance: rasterizers disagree on edge coverage.</summary>
    public const double EdgeBudget = 0.02;

    public static (int MaxDiff, double OutsideShare) Compare(string name, int w, int h, byte[] actual)
    {
        var captureDir = Environment.GetEnvironmentVariable("ONEGAME_CAPTURE_DIR");
        if (!string.IsNullOrEmpty(captureDir))
        {
            Directory.CreateDirectory(captureDir);
            Png.Write(Path.Combine(captureDir, name + ".png"), w, h, actual);
        }

        var path = TestFiles.GoldenPath(name);
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
                worst = Math.Max(worst, Math.Abs(actual[p * 4 + c] - expected[p * 4 + c]));
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
            File.AppendAllText(report, $"{name} maxDiff={max} outsideTolerance={share:P2}\n");
        }

        return (max, share);
    }

    public static void Assert_(string name, int w, int h, byte[] actual)
    {
        var (max, share) = Compare(name, w, h, actual);
        Assert.True(share <= EdgeBudget, $"{name}: {share:P2} of pixels differ by more than {Tolerance} (max diff {max})");
    }
}
