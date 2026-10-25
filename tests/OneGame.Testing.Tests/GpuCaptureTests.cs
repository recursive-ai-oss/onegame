using System.Runtime.CompilerServices;
using OneGame.Host;
using OneGame.Text;

namespace OneGame.Testing.Tests;

/// <summary>The shop with real fonts, so the UI is drawn by the 2D renderer when a device and shaders exist.</summary>
public sealed class FontedShopGame : ShopGame
{
    public override string Name => "FontedShopGame";

    public override void DefineFonts(FontLibrary fonts) =>
        fonts.Add(File.ReadAllBytes(Path.Combine(GpuPaths.RepoRoot, "testdata", "text", "fonts", "NotoSans-Subset-Regular.ttf")));
}

internal static class GpuPaths
{
    private static string SourceDir([CallerFilePath] string path = "") => Path.GetDirectoryName(path)!;

    public static string RepoRoot => Path.GetFullPath(Path.Combine(SourceDir(), "..", ".."));
}

/// <summary>Captures drawn on the graphics device by the 2D renderer, with the validation layer on when it is installed.</summary>
public class GpuCaptureTests
{
    private static TestOptions Options() => new() { Width = 320, Height = 200, Gpu = GpuMode.Required };

    [Fact]
    public void ARendererCaptureWithAUiScaleOverrideEqualsOneAtThatWindowScale()
    {
        Environment.SetEnvironmentVariable("ONEGAME_GPU_VALIDATION", "1");
        using var native = TestCluster.Start(new FontedShopGame(), Options() with { Scale = 1.5f });
        using var overridden = TestCluster.Start(new FontedShopGame(), Options());
        overridden.Client.Game.UiScaleOverride = 1.5f;

        // Draw at the window scale first, so the glyph and art atlases hold entries for scale 1 and must not leak into 1.5.
        overridden.Client.Capture("320x200", 1f);
        var a = native.Client.Capture("320x200", native.Client.Game.UiScale);
        var b = overridden.Client.Capture("320x200", overridden.Client.Game.UiScale);
        Assert.Equal("render2d", overridden.Client.Game.Stage.Name);
        Assert.Equal(0, ImageCompare.Compare(a, b).DifferingPixels);
        Assert.NotEqual(0, ImageCompare.Compare(a, overridden.Client.Capture("320x200", 1f)).DifferingPixels);
        Assert.Empty(overridden.Client.Game.TakeGpuValidationMessages());
    }

    [Fact]
    public void TheUiIsDrawnByTheRenderer()
    {
        Environment.SetEnvironmentVariable("ONEGAME_GPU_VALIDATION", "1");
        using var cluster = TestCluster.Start(new FontedShopGame(), Options());
        var c = cluster.Client;
        c.Click("buy").Step(2);
        var frame = c.Capture("320x200");
        Assert.Equal("render2d", c.Game.Stage.Name);
        Assert.NotEqual("cpu", c.Game.AdapterName ?? "cpu");

        var pixels = frame;
        var colours = new HashSet<uint>();
        for (var i = 0; i + 3 < pixels.Rgba.Length; i += 4)
        {
            colours.Add(BitConverter.ToUInt32(pixels.Rgba, i));
        }

        // Anti-aliased text, panels and borders make many colours; a flat or black frame would not.
        Assert.True(colours.Count > 12, $"only {colours.Count} distinct colours");
        Assert.Empty(c.Game.TakeGpuValidationMessages());
        if (Environment.GetEnvironmentVariable("ONEGAME_TEST_SAVE") is { Length: > 0 } dir)
        {
            Directory.CreateDirectory(dir);
            File.WriteAllBytes(Path.Combine(dir, "gpu-" + (c.Game.AdapterName ?? "cpu").Split(' ')[0] + ".png"), Png.Encode(frame));
        }
    }

    [Fact]
    public void GpuCaptureCompareThroughAScenario()
    {
        Environment.SetEnvironmentVariable("ONEGAME_GPU_VALIDATION", "1");
        using var cluster = TestCluster.Start(new FontedShopGame(), Options());
        var art = Path.Combine(cluster.OutputDirectory, "art");
        var scenario = ScenarioParser.Parse(
            "window: 320x200\nsteps:\n  - step: 1\nexpect:\n  - capture: { reference: refs/gpu.png }\n", Path.Combine(cluster.OutputDirectory, "s.yaml"), "gpu");
        Assert.True(cluster.Run(scenario, new ScenarioRunOptions { ArtifactsDirectory = art, UpdateReferences = true }).Passed);
        Assert.True(cluster.Run(scenario, new ScenarioRunOptions { ArtifactsDirectory = art }).Passed);

        // A changed frame fails even though it is drawn on the device.
        cluster.Client.Call("state_patch", """{"entity":"Shop","property":"Gold","value":"98765"}""");
        var changed = cluster.Run(scenario, new ScenarioRunOptions { ArtifactsDirectory = art });
        Assert.False(changed.Passed);
        Assert.Contains("px", changed.Expectations[0].Message, StringComparison.Ordinal);
        Assert.Empty(cluster.Client.Game.TakeGpuValidationMessages());
    }
}
