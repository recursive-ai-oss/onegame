using OneGame.Atlas;
using OneGame.Text;
using OneGame.UI;

namespace OneGame.Render2D.Tests;

/// <summary>Backdrop blur (backdrop-filter: blur) and nearest-neighbour image sampling (image-rendering: pixelated), drawn through the style system.</summary>
public class BackdropTests
{
    private static FontTextMeasurer Measurer()
    {
        var lib = new FontLibrary();
        lib.Add(File.ReadAllBytes(Paths.TextData("fonts/NotoSans-Subset-Regular.ttf")));
        return new FontTextMeasurer(lib);
    }

    private static double Linear(byte srgb)
    {
        var c = srgb / 255.0;
        return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
    }

    // The standard normal CDF, by the Abramowitz-Stegun 7.1.26 erf approximation (error under 1.5e-7).
    private static double Phi(double x)
    {
        var z = Math.Abs(x) / Math.Sqrt(2);
        var t = 1 / (1 + (0.3275911 * z));
        var poly = ((((((((1.061405429 * t) - 1.453152027) * t) + 1.421413741) * t) - 0.284496736) * t) + 0.254829592) * t;
        var erf = 1 - (poly * Math.Exp(-z * z));
        return 0.5 * (1 + (x < 0 ? -erf : erf));
    }

    private sealed class Frame : IDisposable
    {
        public Frame(int width, int height, Func<Element> build, Action<ArtAtlas>? art = null, float scale = 1f)
        {
            Width = width;
            Height = height;
            var measurer = Measurer();
            Glyphs = new GlyphAtlas(new FontLibrary());
            Art = new ArtAtlas();
            art?.Invoke(Art);
            Text = new TextQuadBuilder(measurer, Glyphs);
            Tree = new UiTree(measurer);
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

        public DisplayList List { get; }

        public void Dispose()
        {
        }
    }

    // White on the left half, black on the right, and a glass panel across the edge.
    private static Element Split(Length blur, bool glass = true) =>
        Ui.Box(
            Ui.Box().Style(s => s.Width(50).Height(40).Background(Color.White)),
            Ui.Box().Style(s => s.Width(50).Height(40).Background(Color.Black)),
            Ui.Box().Style(s => s.Position(PositionMode.Absolute).Left(25).Top(5).Width(50).Height(30).BackdropBlur(glass ? blur : 0)))
            .Style(s => s.Size(100, 40).Direction(FlexDirection.Row));

    [Theory]
    [InlineData(3f)]
    [InlineData(12f)]
    public void TheBlurFollowsTheGaussianEdgeProfileInsideTheBoxOnly(float sigma)
    {
        using var f = new Frame(100, 40, () => Split(sigma));
        Assert.Contains(f.List.Commands.ToArray(), c => c.Kind == DrawKind.BackdropBlur);
        using var rig = new Rig(100, 40);
        var px = rig.Render(() => rig.Renderer.Ui.Draw(f.List, f.Text, f.Glyphs, f.Art), new Color(255, 0, 255, 255));
        Assert.Equal(1, rig.Renderer.Ui.BackdropBlurs);

        // White (1) on the left falls to black (0): the blurred value at distance t from the edge is 1 - Phi(t / sigma), in linear light.
        var worst = 0.0;
        for (var x = 28; x < 72; x++)
        {
            var t = x + 0.5 - 50;
            var expected = 1 - Phi(t / sigma);
            var actual = Linear(Pixels.At(px, 100, x, 20).R);
            worst = Math.Max(worst, Math.Abs(actual - expected));
        }

        Assert.True(worst < 0.05, $"worst deviation from the Gaussian edge: {worst:F3}");

        // Outside the box the picture is untouched: crisp white, crisp black, and rows above and below the box.
        Assert.Equal((255, 255, 255, 255), Pixels.At(px, 100, 10, 20));
        Assert.Equal((0, 0, 0, 255), Pixels.At(px, 100, 90, 20));
        Assert.Equal((255, 255, 255, 255), Pixels.At(px, 100, 45, 2));
        Assert.Equal((0, 0, 0, 255), Pixels.At(px, 100, 55, 38));
    }

    [Fact]
    public void NoBlurMeansNoCommandNoExtraPassAndTheSamePixels()
    {
        using var plain = new Frame(100, 40, () => Split(0f, glass: false));
        Assert.DoesNotContain(plain.List.Commands.ToArray(), c => c.Kind == DrawKind.BackdropBlur);
        using var rig = new Rig(100, 40);
        var without = rig.Render(() => rig.Renderer.Ui.Draw(plain.List, plain.Text, plain.Glyphs, plain.Art));
        var passesWithout = rig.Stats.Passes;
        Assert.Equal(1, passesWithout);
        Assert.Equal(0, rig.Renderer.Ui.BackdropBlurs);

        using var blurred = new Frame(100, 40, () => Split(6f));
        rig.Render(() => rig.Renderer.Ui.Draw(blurred.List, blurred.Text, blurred.Glyphs, blurred.Art));
        Assert.True(rig.Stats.Passes > passesWithout);

        // And back: the next frame without a blur is as cheap as before.
        var again = rig.Render(() => rig.Renderer.Ui.Draw(plain.List, plain.Text, plain.Glyphs, plain.Art));
        Assert.Equal(passesWithout, rig.Stats.Passes);
        Assert.Equal(without, again);
    }

    [Fact]
    public void SeveralBoxesAndRoundedCornersBlurWhatIsBehindThemOnly()
    {
        using var f = new Frame(100, 40, () => Ui.Box(
            Ui.Box().Style(s => s.Width(50).Height(40).Background(Color.White)),
            Ui.Box().Style(s => s.Width(50).Height(40).Background(Color.Black)),
            Ui.Box().Style(s => s.Position(PositionMode.Absolute).Left(10).Top(4).Width(30).Height(32).Radius(15).BackdropBlur(4f)),
            Ui.Box().Style(s => s.Position(PositionMode.Absolute).Left(60).Top(4).Width(30).Height(32).BackdropBlur(30f)))
            .Style(s => s.Size(100, 40).Direction(FlexDirection.Row)));
        using var rig = new Rig(100, 40);
        var px = rig.Render(() => rig.Renderer.Ui.Draw(f.List, f.Text, f.Glyphs, f.Art));
        Assert.Equal(2, rig.Renderer.Ui.BackdropBlurs);

        // The corner of the first box is cut away by its radius, so the sharp white shows there; the second box is far from the edge but a wide
        // blur still pulls in some of the black next to it.
        Assert.Equal((255, 255, 255, 255), Pixels.At(px, 100, 10, 4));
        var inside = Pixels.At(px, 100, 62, 20).R;
        Assert.InRange(inside, 1, 254);
    }

    private static byte[] Checker()
    {
        // 2 x 2 texels: black and white on the diagonal.
        byte[] rgba = [0, 0, 0, 255, 255, 255, 255, 255, 255, 255, 255, 255, 0, 0, 0, 255];
        return Png.Encode(2, 2, rgba);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(2f)]
    public void PixelatedImagesKeepTheirTexelEdgesAndAutoSmoothsThem(float scale)
    {
        foreach (var pixelated in new[] { true, false })
        {
            var size = (int)(32 * scale);
            using var f = new Frame(size, size, () => (pixelated ? Ui.PixelImage("checker", 2, 2) : Ui.Image("checker", 2, 2)).Style(s => s.Size(Length.Ui(32), Length.Ui(32))),
                art => art.AddImage("checker", Checker()), scale);
            var list = f.List;
            using var rig = new Rig(size, size);
            var px = rig.Render(() => rig.Renderer.Ui.Draw(list, f.Text, f.Glyphs, f.Art), new Color(255, 0, 255, 255));
            var half = size / 2;
            var between = 0;
            for (var i = 0; i < size * size; i++)
            {
                var r = px[i * 4];
                Assert.Equal(255, px[(i * 4) + 3]);
                if (r is > 0 and < 255)
                {
                    between++;
                    Assert.True(!pixelated, $"pixel {i % size},{i / size} = {r}");
                }
            }

            if (pixelated)
            {
                // Every pixel is one texel's colour, and the texel edge falls exactly on the middle of the image.
                Assert.Equal(0, between);
                Assert.Equal((0, 255), (Pixels.At(px, size, half - 1, size / 4).R, Pixels.At(px, size, half, size / 4).R));
                Assert.Equal((255, 0), (Pixels.At(px, size, half - 1, size - 2).R, Pixels.At(px, size, half, size - 2).R));
            }
            else
            {
                Assert.True(between > size, "smooth sampling blends the texels");
            }
        }
    }
}
