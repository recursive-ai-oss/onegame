using System.Text;
using OneGame.Atlas;
using OneGame.Text;
using OneGame.UI;
using OneImage;

namespace OneGame.Render2D.Tests;

/// <summary>Tiled and positioned background images, and large images at full resolution, drawn through the real atlas and renderer.</summary>
public class BackgroundTests
{
    private const string DiamondSvg = """<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16"><rect width="16" height="16" fill="#1c2b3a"/><path d="M8 1 L15 8 L8 15 L1 8 Z" fill="#d9a441"/></svg>""";

    private const string BackdropSvg = """<svg xmlns="http://www.w3.org/2000/svg" width="1024" height="576" viewBox="0 0 1024 576"><rect width="1024" height="576" fill="#18202c"/><rect x="300.5" y="100" width="424" height="376" fill="#e8e0c8"/><path d="M0 0 L1024 576" stroke="#c04030" stroke-width="1"/></svg>""";

    private static FontLibrary Library()
    {
        var lib = new FontLibrary();
        lib.Add(File.ReadAllBytes(Paths.TextData("fonts/NotoSans-Subset-Regular.ttf")));
        return lib;
    }

    /// <summary>An 8×8 tile: dark fill, light top row and left column, one red dot. Every edge pixel matters when tiles meet.</summary>
    private static byte[] GridTile()
    {
        var px = new byte[8 * 8 * 4];
        for (var y = 0; y < 8; y++)
        {
            for (var x = 0; x < 8; x++)
            {
                var c = y == 0 || x == 0 ? (0x3f, 0x6a, 0xa0) : (0x20, 0x34, 0x4f);
                if (x == 4 && y == 4)
                {
                    c = (0xe0, 0x40, 0x30);
                }

                var o = ((y * 8) + x) * 4;
                (px[o], px[o + 1], px[o + 2], px[o + 3]) = ((byte)c.Item1, (byte)c.Item2, (byte)c.Item3, 255);
            }
        }

        return px;
    }

    /// <summary>A painted 2048×1152 backdrop: a diagonal gradient, a hard vertical edge at x = 1001, and bright one-pixel rules every 64 rows.</summary>
    private static byte[] Backdrop(int w, int h)
    {
        var px = new byte[w * h * 4];
        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                var o = ((y * w) + x) * 4;
                var v = 40 + (int)(60.0 * (x + y) / (w + h));
                if (x >= w * 1001 / 2048)
                {
                    v += 110;
                }

                if (y % (h / 18) == 0)
                {
                    v = 250;
                }

                (px[o], px[o + 1], px[o + 2], px[o + 3]) = ((byte)Math.Min(v, 255), (byte)Math.Min(v + 10, 255), (byte)Math.Min(v + 20, 255), 255);
            }
        }

        return px;
    }

    private sealed class Stage : IDisposable
    {
        public Stage(int width, int height, float scale, Action<ArtAtlas> art, Func<Element> build, ArtLimits? limits = null)
        {
            Width = width;
            Height = height;
            var lib = Library();
            var measurer = new FontTextMeasurer(lib);
            Glyphs = new GlyphAtlas(lib);
            Art = new ArtAtlas(limits: limits);
            art(Art);
            Text = new TextQuadBuilder(measurer, Glyphs);
            Tree = new UiTree(measurer) { ArtSizes = new ArtAtlasSizes(Art) };
            Tree.SetViewport(width, height, scale);
            Tree.Root.With(build());
            List = Tree.Frame();
        }

        public int Width { get; }

        public int Height { get; }

        public GlyphAtlas Glyphs { get; }

        public ArtAtlas Art { get; }

        public TextQuadBuilder Text { get; }

        public UiTree Tree { get; }

        public DisplayList List { get; private set; }

        public void Reframe() => List = Tree.Frame();

        public void Dispose()
        {
        }
    }

    private static Element Panels() =>
        Ui.Row(
            Ui.Column(
                Ui.Box().Style(s => s.Size(Length.Ui(96), Length.Ui(64)).BackgroundImage("grid").BackgroundRepeat(BackgroundRepeat.Repeat).BackgroundSize(BackgroundSizing.Auto)),
                Ui.Box().Style(s => s.Size(Length.Ui(96), Length.Ui(64)).BackgroundImage("diamond").BackgroundRepeat(BackgroundRepeat.Repeat).BackgroundSize(BackgroundSizing.Auto)),
                Ui.Box().Style(s => s.Size(Length.Ui(96), Length.Ui(36)).BackgroundImage("diamond").BackgroundRepeat(BackgroundRepeat.RepeatX).BackgroundSize(BackgroundSizing.Auto)
                    .BackgroundPosition(Length.Px(0), Length.Percent(50))))
                .Style(s => s.Gap(Length.Ui(6))),
            Ui.Column(
                Ui.Box().Style(s => s.Size(Length.Ui(96), Length.Ui(64)).BackgroundImage("diamond").BackgroundRepeat(BackgroundRepeat.Repeat).BackgroundSize(Length.Ui(10), Length.Auto)
                    .BackgroundPosition(Length.Ui(5), Length.Ui(5))),
                Ui.Box().Style(s => s.Size(Length.Ui(96), Length.Ui(64)).Background(Color.Hex("#101820")).BackgroundImage("diamond").BackgroundSize(BackgroundSizing.Auto)
                    .BackgroundSize(Length.Ui(32), Length.Ui(32)).BackgroundPosition(Length.Percent(100), Length.Percent(100))),
                Ui.Box().Style(s => s.Size(Length.Ui(96), Length.Ui(36)).Background(Color.Hex("#101820")).BackgroundImage("diamond").BackgroundSize(BackgroundSizing.Contain)
                    .BackgroundPosition(Length.Percent(50), Length.Percent(50))))
                .Style(s => s.Gap(Length.Ui(6))))
            .Style(s => s.Padding(Length.Ui(8)).Gap(Length.Ui(8)).Background(Color.Hex("#0b0e12")).Size(Length.Ui(224), Length.Ui(200)));

    private static void Assets(ArtAtlas art)
    {
        art.AddImage("grid", Png.Encode(8, 8, GridTile()));
        art.AddSvg("diamond", Encoding.UTF8.GetBytes(DiamondSvg));
    }

    // Full-size frames are too big to commit; they are checked against CPU renderings and written out for a look when a capture folder is set.
    private static void Capture(string name, int w, int h, byte[] px)
    {
        if (Environment.GetEnvironmentVariable("ONEGAME_CAPTURE_DIR") is { Length: > 0 } dir)
        {
            Directory.CreateDirectory(dir);
            Png.Write(Path.Combine(dir, name + ".png"), w, h, px);
        }
    }

    private static byte[] Draw(Rig rig, Stage s)
    {
        return rig.Render(() => rig.Renderer.Ui.Draw(s.List, s.Text, s.Glyphs, s.Art), new Color(0, 0, 0, 255));
    }

    [Theory]
    [InlineData(1f, "background-tiles-1x")]
    [InlineData(1.5f, "background-tiles-1.5x")]
    public void TiledAndPlacedBackgroundsMatchTheReference(float scale, string golden)
    {
        using var s = new Stage((int)(224 * scale), (int)(200 * scale), scale, Assets, Panels);
        using var rig = new Rig(s.Width, s.Height);
        var px = Draw(rig, s);
        // The references come from lavapipe. Another driver anti-aliases the one-pixel box corners a little differently (as in the other UI references), which is a
        // larger share of this small, dense frame than of a screen: allow 2% instead of 1%.
        var (maxDiff, outside) = Golden.Compare(golden, s.Width, s.Height, px);
        Assert.True(outside <= 0.02, $"{golden}: {outside:P2} of pixels differ by more than {Golden.Tolerance} (max diff {maxDiff})");
        Assert.Equal(0, rig.Renderer.Ui.MissingArt);

        // Six backgrounds with hundreds of tiles in all are six instances: the wrap happens in the shader.
        var tiled = s.List.Commands.ToArray().Count(c => c.Kind == DrawKind.Image && c.TileSize != default);
        Assert.Equal(6, tiled);
        Assert.Equal(6, rig.Stats.UiInstances - s.List.Commands.ToArray().Count(c => c.Kind != DrawKind.Image && c.Kind is DrawKind.Rect or DrawKind.Border or DrawKind.Shadow));
        Golden.Note($"{golden}: {tiled} tiled backgrounds, {rig.Stats.UiInstances} ui instances, {rig.Stats.DrawCalls} draw calls, {rig.Stats.Passes} passes");
    }

    [Fact]
    public void ARepeatedRasterTileIsPixelExactAtScaleOne()
    {
        // A lone repeated background: 40×20 pixels of 8×8 tiles, from an origin that is not on a tile boundary.
        using var s = new Stage(40, 20, 1f, Assets, () => Ui.Box().Style(st => st.Size(40, 20).BackgroundImage("grid").BackgroundRepeat(BackgroundRepeat.Repeat).BackgroundSize(BackgroundSizing.Auto)
            .BackgroundPosition(3, 2)));
        using var rig = new Rig(40, 20);
        var px = Draw(rig, s);
        var tile = GridTile();
        var worst = 0;
        // The one-pixel ring at the box's edge is anti-aliased by the shape, and drivers differ there; the tiles are checked inside it.
        for (var y = 1; y < 19; y++)
        {
            for (var x = 1; x < 39; x++)
            {
                var tx = ((x - 3) % 8 + 8) % 8;
                var ty = ((y - 2) % 8 + 8) % 8;
                for (var c = 0; c < 3; c++)
                {
                    worst = Math.Max(worst, Math.Abs(px[(((y * 40) + x) * 4) + c] - tile[(((ty * 8) + tx) * 4) + c]));
                }
            }
        }

        // sRGB texels sampled and blended in linear light come back within a rounding step.
        Assert.True(worst <= 2, $"the tiled pixels differ from a CPU tiling by up to {worst}");
        Golden.Note($"raster tiling vs CPU tiling: max channel difference {worst}");
    }

    [Theory]
    [InlineData("repeat")]
    [InlineData("sized")]
    [InlineData("positioned")]
    public void ElementOpacityAppliesToTiledBackgroundsAsToPlainOnes(string kind)
    {
        // A tile of one flat colour, tiled at half opacity, must match a plain background of that colour at half opacity.
        var flat = new byte[8 * 8 * 4];
        for (var i = 0; i < flat.Length; i += 4)
        {
            (flat[i], flat[i + 1], flat[i + 2], flat[i + 3]) = (0x3f, 0x6a, 0xa0, 255);
        }

        void FlatAssets(ArtAtlas art) => art.AddImage("flat", Png.Encode(8, 8, flat));
        Action<StyleBuilder> tile = kind switch
        {
            "repeat" => st => st.BackgroundImage("flat").BackgroundRepeat(BackgroundRepeat.Repeat).BackgroundSize(BackgroundSizing.Auto),
            "sized" => st => st.BackgroundImage("flat").BackgroundRepeat(BackgroundRepeat.Repeat).BackgroundSize(Length.Px(4), Length.Px(2)),
            _ => st => st.BackgroundImage("flat").BackgroundRepeat(BackgroundRepeat.Repeat).BackgroundSize(BackgroundSizing.Auto).BackgroundPosition(3, 2),
        };
        byte[] Render(Action<StyleBuilder> paint, float opacity)
        {
            using var s = new Stage(40, 20, 1f, FlatAssets, () => Ui.Box().Style(st => { st.Size(40, 20).Opacity(opacity); paint(st); }));
            using var rig = new Rig(40, 20);
            return Draw(rig, s);
        }

        var plain = Render(st => st.Background(new Color(0x3f, 0x6a, 0xa0, 255)), 0.5f);
        var tiled = Render(tile, 0.5f);
        var tiledFull = Render(tile, 1f);
        var worst = 0;
        for (var y = 2; y < 18; y++)
        {
            for (var x = 2; x < 38; x++)
            {
                for (var c = 0; c < 3; c++)
                {
                    worst = Math.Max(worst, Math.Abs(plain[(((y * 40) + x) * 4) + c] - tiled[(((y * 40) + x) * 4) + c]));
                }
            }
        }

        Assert.True(Pixels.At(tiledFull, 40, 20, 10).B > 0x90, "the full-strength tile is drawn");
        Assert.True(worst <= 2, $"{kind}: the tiled background at half opacity differs from a plain one by up to {worst}");
    }

    [Fact]
    public void LowOpacityVectorTileAtUiScaleBlendsLikeAPlainFill()
    {
        const string scan = """<svg xmlns="http://www.w3.org/2000/svg" width="4" height="2" viewBox="0 0 4 2"><rect y="0" width="4" height="1" fill="#fff"/></svg>""";
        void ScanAssets(ArtAtlas art) => art.AddSvg("scan", Encoding.UTF8.GetBytes(scan));
        byte[] Render(float opacity)
        {
            using var s = new Stage(120, 90, 1.5f, ScanAssets, () => Ui.Box(Ui.Box().Style(st => st.Position(PositionMode.Absolute).Left(0).Top(0).Width(Length.Percent(100)).Height(Length.Percent(100))
                .Opacity(opacity).BackgroundImage("scan").BackgroundRepeat(BackgroundRepeat.Repeat).BackgroundSize(Length.Px(4), Length.Px(2))))
                .Style(st => st.Size(Length.Ui(80), Length.Ui(60))));
            using var rig = new Rig(120, 90);
            return Draw(rig, s);
        }

        var full = Render(1f);
        var faint = Render(0.1f);
        var fullMax = 0;
        var faintMax = 0;
        for (var i = 0; i < full.Length; i += 4)
        {
            fullMax = Math.Max(fullMax, full[i]);
            faintMax = Math.Max(faintMax, faint[i]);
        }

        Assert.True(fullMax > 200, $"full strength lines are drawn ({fullMax})");
        // Blending is in linear light: 10% of white over black is 0.1 linear, which encodes to about 89 (sRGB transfer function).
        Assert.InRange(faintMax, 85, 93);
    }

    [Fact]
    public void NoRepeatDrawsOneCopyAndLeavesTheRestOfTheBoxAlone()
    {
        using var s = new Stage(40, 40, 1f, Assets, () => Ui.Box().Style(st => st.Size(40, 40).Background(Color.Hex("#000000")).BackgroundImage("grid").BackgroundSize(Length.Px(8), Length.Px(8))
            .BackgroundPosition(Length.Px(10), Length.Px(12))));
        using var rig = new Rig(40, 40);
        var px = Draw(rig, s);
        Assert.Equal((0, 0, 0, 255), Pixels.At(px, 40, 9, 12));
        Assert.Equal((0, 0, 0, 255), Pixels.At(px, 40, 18, 12));
        Assert.Equal((0, 0, 0, 255), Pixels.At(px, 40, 10, 20));
        Assert.NotEqual((0, 0, 0, 255), Pixels.At(px, 40, 14, 16));
        Assert.Equal(0x3f, Pixels.At(px, 40, 10, 12).R is var r && r > 0 ? 0x3f : 0);
    }

    private static (int W, int H, byte[] Rgba) CpuSvg(string svg, int w, int h)
    {
        var cfg = new ImageConfiguration().Add(OneImage.Svg.SvgFormat.Instance);
        var image = Image.Load(Encoding.UTF8.GetBytes(svg), new LoadOptions { Configuration = cfg }).Resize(w, h, Fit.Stretch);
        var px = image.ToArray<Rgba32>();
        var bytes = new byte[px.Length * 4];
        for (var i = 0; i < px.Length; i++)
        {
            (bytes[i * 4], bytes[(i * 4) + 1], bytes[(i * 4) + 2], bytes[(i * 4) + 3]) = (px[i].R, px[i].G, px[i].B, px[i].A);
        }

        return (w, h, bytes);
    }

    /// <summary>Bilinear resample with texel centres at +0.5, which is what the GPU's sampler does.</summary>
    private static byte[] Resample(byte[] src, int sw, int sh, int dw, int dh)
    {
        var dst = new byte[dw * dh * 4];
        for (var y = 0; y < dh; y++)
        {
            var fy = ((y + 0.5) * sh / dh) - 0.5;
            var y0 = (int)Math.Floor(fy);
            var ty = fy - y0;
            for (var x = 0; x < dw; x++)
            {
                var fx = ((x + 0.5) * sw / dw) - 0.5;
                var x0 = (int)Math.Floor(fx);
                var tx = fx - x0;
                for (var c = 0; c < 4; c++)
                {
                    double At(int xx, int yy) => src[((Math.Clamp(yy, 0, sh - 1) * sw) + Math.Clamp(xx, 0, sw - 1)) * 4 + c];
                    var v = (At(x0, y0) * (1 - tx) + At(x0 + 1, y0) * tx) * (1 - ty) + (At(x0, y0 + 1) * (1 - tx) + At(x0 + 1, y0 + 1) * tx) * ty;
                    dst[((y * dw) + x) * 4 + c] = (byte)Math.Round(v);
                }
            }
        }

        return dst;
    }

    /// <summary>Pixels across which a left-to-right step in the red channel goes from 10% to 90% of its height, on one row.</summary>
    private static double EdgeWidth(byte[] rgba, int width, int row, int from, int to)
    {
        double lo = rgba[((row * width) + from) * 4], hi = rgba[((row * width) + to) * 4];
        double Cross(double level)
        {
            for (var x = from; x < to; x++)
            {
                double a = rgba[((row * width) + x) * 4], b = rgba[((row * width) + x + 1) * 4];
                if (a <= level && b >= level)
                {
                    return x + ((level - a) / Math.Max(b - a, 1e-9));
                }
            }

            return double.NaN;
        }

        return Cross(lo + (0.9 * (hi - lo))) - Cross(lo + (0.1 * (hi - lo)));
    }

    private static (double Mean, int Max, double Over) Diff(byte[] a, byte[] b)
    {
        long sum = 0;
        var max = 0;
        var over = 0;
        for (var i = 0; i < a.Length; i++)
        {
            if (i % 4 == 3)
            {
                continue;
            }

            var d = Math.Abs(a[i] - b[i]);
            sum += d;
            max = Math.Max(max, d);
            if (d > 8)
            {
                over++;
            }
        }

        return (sum / (a.Length * 0.75), max, over / (a.Length * 0.75));
    }

    [Fact]
    public void APaintedBackdropLargerThanAPageKeepsItsFullResolution()
    {
        const int sw = 2048, sh = 1152, W = 1920, H = 1080;
        var source = Backdrop(sw, sh);
        using var s = new Stage(W, H, 1.5f, a => a.AddImage("backdrop", Png.Encode(sw, sh, source)),
            () => Ui.Box().Style(st => st.Size(Length.Percent(100), Length.Percent(100)).BackgroundImage("backdrop")));
        using var rig = new Rig(W, H);
        var px = Draw(rig, s);
        Assert.Equal(0, rig.Renderer.Ui.MissingArt);
        Assert.Equal(1, s.Art.StandaloneCount);

        // Reference: the same image resampled on the CPU straight from the full-size source.
        var reference = Resample(source, sw, sh, W, H);
        var (mean, max, over) = Diff(px, reference);
        // What a 1024-pixel page would have kept: the source shrunk to 1024×576 first, then stretched to the panel.
        var capped = Resample(Resample(source, sw, sh, 1024, 576), 1024, 576, W, H);
        var row = 540;
        var edgeX = (1001 * W / 2048);
        var gpuEdge = EdgeWidth(px, W, row, edgeX - 12, edgeX + 12);
        var refEdge = EdgeWidth(reference, W, row, edgeX - 12, edgeX + 12);
        var cappedEdge = EdgeWidth(capped, W, row, edgeX - 12, edgeX + 12);
        var (cappedMean, _, _) = Diff(capped, reference);
        Golden.Note($"backdrop 2048x1152 at 1920x1080, UI scale 1.5: mean diff {mean:F3}, max diff {max}, {over:P3} of channels off by more than 8, edge width {gpuEdge:F2} px (CPU reference {refEdge:F2} px, 1024 px page {cappedEdge:F2} px, page mean diff {cappedMean:F3})");
        Assert.True(mean < 0.5, $"mean difference from the CPU resample is {mean}");
        Assert.True(over < 0.001, $"{over:P3} of channels differ from the CPU resample by more than 8 (max {max})");
        // Not blurrier than the reference. A sampler with a different sub-texel rounding (Intel graphics) can land the step on a pixel
        // boundary and give a narrower edge, which is as sharp as the image gets, so only the soft side fails.
        Assert.True(gpuEdge < refEdge + 0.35, $"edge is {gpuEdge} px wide against {refEdge} in the reference");
        Assert.True(cappedEdge > gpuEdge + 0.5, "the sharpness check must be able to tell a 1024 px page from the full image");
        Capture("backdrop-raster-1080p-1.5x", W, H, px);
    }

    [Fact]
    public void AFullPanelSvgIsRasterizedAtThePanelSizeNotItsNaturalSize()
    {
        const int W = 1920, H = 1080;
        using var s = new Stage(W, H, 1.5f, a => a.AddSvg("backdrop", Encoding.UTF8.GetBytes(BackdropSvg)),
            () => Ui.Box().Style(st => st.Size(Length.Percent(100), Length.Percent(100)).BackgroundImage("backdrop")));
        using var rig = new Rig(W, H);
        var px = Draw(rig, s);
        Assert.Equal(0, rig.Renderer.Ui.MissingArt);
        Assert.Equal(1, s.Art.StandaloneCount);

        var (_, _, reference) = CpuSvg(BackdropSvg, W, H);
        var (mean, max, over) = Diff(px, reference);
        // The old route: rasterize at natural size times the scale (1536×864) and stretch.
        var (_, _, natural) = CpuSvg(BackdropSvg, 1536, 864);
        var stretched = Resample(natural, 1536, 864, W, H);
        var edgeX = (int)(300.5 * W / 1024);
        var gpuEdge = EdgeWidth(px, W, 540, edgeX - 10, edgeX + 10);
        var refEdge = EdgeWidth(reference, W, 540, edgeX - 10, edgeX + 10);
        var stretchedEdge = EdgeWidth(stretched, W, 540, edgeX - 10, edgeX + 10);
        Golden.Note($"svg backdrop at 1920x1080, UI scale 1.5: mean diff {mean:F3}, max diff {max}, {over:P3} of channels off by more than 8, edge width {gpuEdge:F2} px (CPU reference {refEdge:F2} px, natural-size raster stretched {stretchedEdge:F2} px)");
        Assert.True(mean < 0.5 && over < 0.001, $"differs from the CPU rendering: mean {mean}, {over:P3} over 8, max {max}");
        Assert.True(Math.Abs(gpuEdge - refEdge) < 0.35);
        Assert.True(stretchedEdge > gpuEdge + 0.3, "the sharpness check must be able to tell a stretched raster from an exact one");
        Capture("backdrop-svg-1080p-1.5x", W, H, px);
    }

    [Fact]
    public void StandaloneTexturesAreReleasedWithinTheMemoryBudgetAndTheGpuStaysClean()
    {
        // Room for two 512×300 rasters (about 614 KB each).
        var limits = new ArtLimits { MaxStandaloneBytes = 2 * 512 * 300 * 4 + 1000 };
        var png = Png.Encode(512, 300, Backdrop(512, 300));
        string[] ids = ["a", "b", "c", "d"];
        using var s = new Stage(256, 150, 1f, a =>
        {
            foreach (var id in ids)
            {
                a.AddImage(id, png);
            }
        }, () => Ui.Box().Style(st => st.Size(256, 150).BackgroundImage("a")), limits);
        using var rig = new Rig(256, 150);
        foreach (var id in ids.Concat(ids))
        {
            s.Tree.Root.ChildAt(0).Style(st => st.BackgroundImage(id));
            s.Reframe();
            Draw(rig, s);
            Assert.True(s.Art.StandaloneBytes <= limits.MaxStandaloneBytes);
            Assert.True(s.Art.StandaloneCount <= 2);
        }

        Assert.Equal(0, rig.Renderer.Ui.MissingArt);
    }
}
