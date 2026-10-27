using System.Numerics;
using OneGame.Atlas;
using OneGame.Gpu;
using OneGame.Gpu.Graph;
using OneGame.Gpu.Vulkan;
using OneGame.Host;
using OneGame.Text;
using OneRender.Image;
using OneRender.Painting;

// Each test makes its own GPU device; real drivers are not happy with several being created and destroyed at once.
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]

namespace OneGame.Pages.Tests;

/// <summary>A device with the validation layer required, a page renderer and an sRGB offscreen target; fails the test if the layer said anything.</summary>
internal sealed class Rig : IDisposable
{
    public const int Frame = 1;

    private int _frame = 100;

    public Rig(int width, int height, float scale = 1f)
    {
        Width = width;
        Height = height;
        Scale = scale;
        Device = GpuBackends.CreateDevice(new GpuDeviceOptions { Validation = true, RequireValidation = true });
        Library = new FontLibrary();
        Fonts = new PageFonts(Library);
        Fonts.Add(File.ReadAllBytes(Paths.TextData("fonts/NotoSans-Subset-Regular.ttf")), "Body");
        Glyphs = new GlyphAtlas(Library);
        Art = new ArtAtlas();
        Images = new PageImages(Art);
        Renderer = PageRenderer.Create(Device, GpuFormat.Rgba8UnormSrgb, Glyphs, Art);
        Target = Device.CreateTexture(new TextureDesc((uint)width, (uint)height, GpuFormat.Rgba8UnormSrgb, TextureUsage.RenderTarget | TextureUsage.CopySource));
        Graph = new RenderGraph(Device);
    }

    public int Width { get; }

    public int Height { get; }

    public float Scale { get; }

    public GpuDevice Device { get; }

    public FontLibrary Library { get; }

    public PageFonts Fonts { get; }

    public GlyphAtlas Glyphs { get; }

    public ArtAtlas Art { get; }

    public PageImages Images { get; }

    public PageRenderer Renderer { get; }

    public GpuTexture Target { get; }

    public RenderGraph Graph { get; }

    public PageScopes Scopes { get; } = new();

    public PageSet Pages { get; } = new();

    public PageOptions Options() => new(Fonts) { Images = Images };

    /// <summary>Draws the set's visible screens over white (or the given linear colour) and reads the target back as RGBA.</summary>
    public byte[] Render(Vector4? clear = null, int layer = 0, Action<RenderGraph, GraphTexture>? between = null)
    {
        Pages.Prepare(Width, Height, Scale);
        Graph.BeginFrame();
        var target = Graph.ImportTexture("target", Target);
        Renderer.AddPasses(Graph, target, Pages.Screens, Pages.Scopes, new PageFrame(Width, Height, Scale, _frame++, layer, clear ?? Vector4.One));
        between?.Invoke(Graph, target);
        Graph.Execute();
        var bytes = new byte[Width * Height * 4];
        Device.ReadTexture(Target, 0, 0, bytes);
        return bytes;
    }

    /// <summary>Records and runs one frame without reading the target back.</summary>
    public void Draw()
    {
        Pages.Prepare(Width, Height, Scale);
        Graph.BeginFrame();
        var target = Graph.ImportTexture("target", Target);
        Renderer.AddPasses(Graph, target, Pages.Screens, Pages.Scopes, new PageFrame(Width, Height, Scale, _frame++, 0, Vector4.One));
        Graph.Execute();
    }

    /// <summary>The list of the first screen drawn through OneRender's own rasterizer over white, at this rig's size.</summary>
    public byte[] Reference(PageScreen screen)
    {
        var list = screen.Draw!.Value.List;
        return DisplayListRasterizer.Render(list, Width, Height, new RasterOptions { DevicePixelRatio = Scale, Background = PaintColor.White }, TestContext.Current.CancellationToken).Pixels;
    }

    public void Dispose()
    {
        Device.WaitIdle();
        Graph.Dispose();
        Renderer.Dispose();
        Target.Dispose();
        Device.WaitIdle();
        var messages = Device.TakeValidationMessages();
        Device.Dispose();
        Assert.True(messages.Length == 0, "validation layer reported:\n" + string.Join("\n", messages));
    }
}

/// <summary>Compares captured pixels with a committed reference image or another capture.</summary>
internal static class Shots
{
    public static void Save(string name, int w, int h, byte[] rgba)
    {
        if (Environment.GetEnvironmentVariable("ONEGAME_CAPTURE_DIR") is { Length: > 0 } dir)
        {
            Directory.CreateDirectory(dir);
            Png.Save(new CapturedFrame(w, h, rgba, "test"), Path.Combine(dir, name + ".png"));
        }
    }

    /// <summary>Per-pixel worst channel difference: the maximum, the mean and the share of pixels above <paramref name="tolerance"/>.</summary>
    public static (int Max, double Mean, double Share) Diff(byte[] a, byte[] b, int tolerance)
    {
        Assert.Equal(a.Length, b.Length);
        var max = 0;
        var sum = 0L;
        var over = 0;
        var n = a.Length / 4;
        for (var p = 0; p < n; p++)
        {
            var worst = 0;
            for (var c = 0; c < 3; c++)
            {
                worst = Math.Max(worst, Math.Abs(a[(p * 4) + c] - b[(p * 4) + c]));
            }

            max = Math.Max(max, worst);
            sum += worst;
            if (worst > tolerance)
            {
                over++;
            }
        }

        return (max, sum / (double)n, over / (double)n);
    }

    public static void Note(string text)
    {
        TestContext.Current.SendDiagnosticMessage(text);
        if (Environment.GetEnvironmentVariable("ONEGAME_GOLDEN_REPORT") is { Length: > 0 } report)
        {
            File.AppendAllText(report, text + "\n");
        }
    }

    /// <summary>Checks against the committed reference (written when ONEGAME_UPDATE_GOLDENS=1): at most <paramref name="budget"/> of pixels may differ by more than <paramref name="tolerance"/>.</summary>
    public static void Golden(string name, int w, int h, byte[] actual, int tolerance = 3, double budget = 0.005)
    {
        Save(name, w, h, actual);
        var path = Paths.Golden(name);
        if (Environment.GetEnvironmentVariable("ONEGAME_UPDATE_GOLDENS") == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            Png.Save(new CapturedFrame(w, h, actual, "test"), path);
        }

        var golden = Png.Load(path);
        Assert.Equal((golden.Width, golden.Height), (w, h));
        var (max, mean, share) = Diff(actual, golden.Rgba, tolerance);
        Note($"{name} vs golden: max {max}, mean {mean:F3}, over-{tolerance} share {share:P3}");
        Assert.True(share <= budget, $"{name}: {share:P3} of pixels differ from the golden by more than {tolerance} (max {max})");
    }
}
