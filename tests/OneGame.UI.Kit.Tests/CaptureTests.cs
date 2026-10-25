using System.Runtime.CompilerServices;
using OneGame.Atlas;
using OneGame.Render2D.Tests;
using OneGame.Text;

namespace OneGame.UI.Kit.Tests;

/// <summary>
/// The trading screen drawn by the 2D renderer into an offscreen target and compared with committed reference images.
/// They need a Vulkan driver and the validation layer (VK_ADD_LAYER_PATH); without them the tests skip. The references come from lavapipe.
/// Set ONEGAME_UPDATE_GOLDENS=1 to rewrite them, ONEGAME_CAPTURE_DIR to also write each capture as a PNG, ONEGAME_GOLDEN_REPORT to append the measured difference to a file.
/// </summary>
public class CaptureTests
{
    private const int Tolerance = 3;
    private const double EdgeBudget = 0.01;

    private static string Dir([CallerFilePath] string here = "") => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "testdata"));

    private static void RequireGpu() =>
        Assert.SkipUnless(OneGame.Gpu.GpuBackends.IsAvailable && Environment.GetEnvironmentVariable("VK_ADD_LAYER_PATH") is { Length: > 0 }, "needs a Vulkan driver and the validation layer");

    private sealed class Rendered : IDisposable
    {
        public Rendered(int width, int height, float scale, Action<Screen, TradingGame, TableView<Good>, DialogView> arrange, KitTokens? tokens = null)
        {
            var lib = new FontLibrary();
            lib.Add(File.ReadAllBytes(Path.Combine(Dir(), "text", "fonts", "NotoSans-Subset-Regular.ttf")));
            lib.Add(File.ReadAllBytes(Path.Combine(Dir(), "text", "fonts", "NotoSans-Subset-Bold-kern.ttf")));
            var measurer = new FontTextMeasurer(lib);
            var glyphs = new GlyphAtlas(lib);
            var art = new ArtAtlas();
            foreach (var a in KitArt.Assets)
            {
                art.AddSvg(a.Id, a.Svg, new NineSlice(a.Slice, a.Slice, a.Slice, a.Slice));
            }

            var screen = new Screen(width, height, scale, measurer);
            if (tokens is not null)
            {
                screen.Host.SetTheme(KitTheme.Create(tokens));
            }

            var game = new TradingGame(5000);
            TradingScreen.Build(screen.Host, game, out var table, out var dialog);
            screen.Settle();
            arrange(screen, game, table, dialog);
            screen.Settle();
            using var rig = new Rig(width, height);
            var text = new TextQuadBuilder(measurer, glyphs);
            Pixels = rig.Render(() => rig.Renderer.Ui.Draw(screen.Ui.DisplayList, text, glyphs, art), new Color(0, 0, 0, 255));
            MissingArt = rig.Renderer.Ui.MissingArt;
            Width = width;
            Height = height;
        }

        public byte[] Pixels { get; }

        public int MissingArt { get; }

        public int Width { get; }

        public int Height { get; }

        public void Dispose()
        {
        }
    }

    private static void Check(string name, Rendered r)
    {
        Assert.Equal(0, r.MissingArt);
        if (Environment.GetEnvironmentVariable("ONEGAME_CAPTURE_DIR") is { Length: > 0 } capture)
        {
            Directory.CreateDirectory(capture);
            Png.Write(Path.Combine(capture, name + ".png"), r.Width, r.Height, r.Pixels);
        }

        var path = Path.Combine(Dir(), "ui-kit", name + ".png");
        if (Environment.GetEnvironmentVariable("ONEGAME_UPDATE_GOLDENS") == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            Png.Write(path, r.Width, r.Height, r.Pixels);
        }

        var (w, h, expected) = Png.Read(path);
        Assert.Equal((w, h), (r.Width, r.Height));
        int max = 0, outside = 0;
        for (var p = 0; p < w * h; p++)
        {
            var worst = 0;
            for (var c = 0; c < 4; c++)
            {
                worst = Math.Max(worst, Math.Abs(r.Pixels[(p * 4) + c] - expected[(p * 4) + c]));
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

        Assert.True(share <= EdgeBudget, $"{name}: {share:P2} of pixels differ by more than {Tolerance} (max diff {max})");
    }

    [Fact]
    public void MarketScreenAtOneX()
    {
        RequireGpu();
        using var r = new Rendered(960, 540, 1, (s, g, t, d) =>
        {
            s.Click(s.Get("goods/row/2"));
            s.Click(s.Get("goods/header/2"));
            s.Host.Dispatch(UiEvent.PointerMove(s.Get("qty-slider").AbsoluteBounds.X + 30, s.Get("qty-slider").AbsoluteBounds.Y + 5));
            s.Host.Advance(1);
            s.Settle();
        });
        Check("market-1x", r);
    }

    [Fact]
    public void MarketScreenAtOnePointFiveX()
    {
        RequireGpu();
        using var r = new Rendered(960, 540, 1.5f, (s, g, t, d) =>
        {
            s.Click(s.Get("filter"));
            s.Type("wine");
            s.Click(s.Get("goods/row/1"));
            s.Click(s.Get("qty-stepper/plus"));
        });
        Check("market-1.5x", r);
    }

    [Fact]
    public void ConfirmDialogOverTheLightTheme()
    {
        RequireGpu();
        using var r = new Rendered(960, 540, 1, (s, g, t, d) =>
        {
            s.Click(s.Get("goods/row/4"));
            s.Click(s.Get("buy"));
        }, KitTokens.Light);
        Check("dialog-light-1x", r);
    }
}
