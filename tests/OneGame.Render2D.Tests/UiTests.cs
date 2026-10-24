using System.Numerics;
using OneGame.Atlas;
using OneGame.Text;
using OneGame.UI;

namespace OneGame.Render2D.Tests;

/// <summary>The UI display list drawn through the real glyph and art atlases.</summary>
public class UiTests
{
    private static readonly Color Panel = Color.Hex("#23262d");

    private static FontLibrary Library()
    {
        var lib = new FontLibrary();
        lib.Add(File.ReadAllBytes(Paths.TextData("fonts/NotoSans-Subset-Regular.ttf")));
        lib.Add(File.ReadAllBytes(Paths.TextData("fonts/NotoSans-Subset-Bold-kern.ttf")));
        return lib;
    }

    private sealed class Scene : IDisposable
    {
        public Scene(float scale)
        {
            Scale = scale;
            var lib = Library();
            var measurer = new FontTextMeasurer(lib);
            Glyphs = new GlyphAtlas(lib);
            Art = new ArtAtlas();
            Art.AddSvg("coin", File.ReadAllBytes(Paths.TextData("svg/coin.svg")));
            Art.AddSvg("frame", File.ReadAllBytes(Paths.TextData("svg/frame.svg")), new NineSlice(8, 8, 8, 8));
            Text = new TextQuadBuilder(measurer, Glyphs);
            Tree = new UiTree(measurer);
            Tree.SetViewport(480 * scale, 300 * scale, scale);
            Tree.AddStyleSheet(new StyleSheet().Rule(":root", s => s.FontSize(Length.Ui(13)).TextColor(Color.Hex("#e8e8e8"))));
            Tree.Root.With(Build());
            List = Tree.Frame();
        }

        public float Scale { get; }

        public GlyphAtlas Glyphs { get; }

        public ArtAtlas Art { get; }

        public TextQuadBuilder Text { get; }

        public UiTree Tree { get; }

        public DisplayList List { get; }

        public int Width => (int)(480 * Scale);

        public int Height => (int)(300 * Scale);

        public void Dispose()
        {
        }

        private static Element Tab(string text, bool active, float opacity = 1) =>
            Ui.Label(text).Style(s => s.Padding(5, 12).Radius(4).Background(active ? Color.Hex("#d9a441") : Color.Hex("#3a3f4b")).TextColor(active ? Color.Hex("#101010") : Color.Hex("#e8e8e8")).Opacity(opacity));

        private static Element Build() =>
            Ui.Column(
                Ui.Row(
                    Ui.Label("Port of Aldeen").Style(s => s.FontWeight(700)),
                    Ui.Spacer(),
                    Ui.Image("coin", 24, 24),
                    Ui.Label("Gold: 1200").Style(s => s.MarginLeft(6)))
                    .Style(s => s.Background(Panel).Padding(6, 10).AlignItems(Align.Center)),
                Ui.Row(Tab("Market", true), Tab("Cargo", false), Tab("Ships", false, 0.5f)).Style(s => s.Padding(8, 10).Gap(6)),
                Ui.Row(
                    // A card with a shadow, border and rounded clip; the wide child spills out of it and must be cut at the rounded edge.
                    Ui.Box(Ui.Box().Style(s => s.Width(400).Height(60).Background(Color.Hex("#2f6fd0")).MarginLeft(-30).MarginTop(30)),
                           Ui.Label("Grain  12g  stock 40").Style(s => s.Padding(6)))
                        .Style(s => s.Width(150).Height(110).Radius(16).Border(2, Color.Hex("#d9a441")).Overflow(OverflowMode.Hidden)
                            .Background(Color.Hex("#1b1d22")).Shadow(0, 4, 12, 0, Color.Hex("#000000b0"))),
                    // The same idea, turned and half transparent: the clip is rotated and the whole group fades together.
                    Ui.Box(Ui.Box().Style(s => s.Width(300).Height(40).Background(Color.Hex("#c0392b")).MarginTop(40)),
                           Ui.Label("SALE").Style(s => s.Padding(6).FontWeight(700)))
                        .Style(s => s.Width(110).Height(90).Radius(10).Overflow(OverflowMode.Hidden).Background(Color.Hex("#3b6b4a")).Rotate(-8).Opacity(0.8f)),
                    // Nine-slice art: the corners keep their size while the middle stretches.
                    Ui.Box(Ui.Label("Frame").Style(s => s.Padding(12)))
                        .Style(s => s.Width(120).Height(80).BackgroundImage("frame").BackgroundSlice(8)))
                    .Style(s => s.Padding(8, 16).Gap(24)))
                .Style(s => s.Background(Color.Hex("#14161a")).Size(480, 300));
    }

    private static void Render(Rig rig, Scene s, bool extras)
    {
        rig.Renderer.Ui.Draw(s.List, s.Text, s.Glyphs, s.Art);
        if (extras)
        {
            // Gradients are not part of the display list yet; they go through the canvas directly.
            var k = s.Scale;
            var ui = rig.Renderer.Ui;
            var bar = new Rect(16 * k, 250 * k, 200 * k, 28 * k);
            ui.Fill(bar, Gradient.Linear(Color.Hex("#d9a441"), Color.Hex("#3b1f6b"), new Vector2(bar.X, bar.Y), new Vector2(bar.X + bar.Width, bar.Y)), new CornerRadii(14 * k, 14 * k, 14 * k, 14 * k));
            var disc = new Rect(240 * k, 240 * k, 48 * k, 48 * k);
            ui.Fill(disc, Gradient.Radial(Color.Hex("#fff3c4"), Color.Hex("#c0392b"), new Vector2(disc.X + (24 * k), disc.Y + (24 * k)), 24 * k), new CornerRadii(24 * k, 24 * k, 24 * k, 24 * k));
            ui.Border(new Rect(300 * k, 244 * k, 80 * k, 40 * k), Color.Hex("#e8e8e8"), new Edges(1 * k, 2 * k, 3 * k, 4 * k), new CornerRadii(0, 12 * k, 0, 12 * k));
        }
    }

    [Theory]
    [InlineData(1f, "ui-1x")]
    [InlineData(2f, "ui-2x")]
    public void TradingScreenMatchesTheReferenceAtTwoScales(float scale, string golden)
    {
        using var s = new Scene(scale);
        using var rig = new Rig(s.Width, s.Height);
        var px = rig.Render(() => Render(rig, s, extras: true), new Color(0, 0, 0, 255));
        Golden.Check(golden, s.Width, s.Height, px);
        Assert.Equal(0, rig.Renderer.Ui.MissingArt);
        // Text, images, shapes and the opacity group all went through; the group costs an extra pass.
        Assert.True(rig.Stats.Passes >= 2);
        Golden.Note($"{golden}: {rig.Stats}");
    }

    [Fact]
    public void RoundedClipCutsTheChildAndTheEdgeIsSmooth()
    {
        using var rig = new Rig(100, 100);
        var px = rig.Render(() =>
        {
            var ui = rig.Renderer.Ui;
            ui.PushClip(new Rect(10, 10, 80, 80), new CornerRadii(40, 40, 40, 40));
            ui.Fill(new Rect(0, 0, 100, 100), Color.White);
            ui.PopClip();
        }, new Color(0, 0, 0, 255));
        Assert.Equal((255, 255, 255, 255), Pixels.At(px, 100, 50, 50));
        // The corner of the square is far outside the circle.
        Assert.Equal((0, 0, 0, 255), Pixels.At(px, 100, 12, 12));
        // On the circle's edge (radius 40 from 50,50) the pixel is partly covered.
        var (r, _, _, _) = Pixels.At(px, 100, 50, 10);
        Assert.InRange(r, 1, 254);
    }

    [Fact]
    public void GroupOpacityBlendsOverlappingChildrenAsOneImage()
    {
        using var rig = new Rig(64, 32);
        var px = rig.Render(() =>
        {
            var ui = rig.Renderer.Ui;
            ui.PushOpacity(0.5f);
            ui.Fill(new Rect(0, 0, 40, 32), Color.White);
            ui.Fill(new Rect(20, 0, 40, 32), Color.White);
            ui.PopOpacity();
        }, new Color(0, 0, 0, 255));
        // Where the two squares overlap the group is still a single 50% layer, not 75%.
        var overlap = Pixels.At(px, 64, 30, 16).R;
        var single = Pixels.At(px, 64, 5, 16).R;
        Assert.Equal(single, overlap);
        Assert.InRange(single, 185, 190);
    }

    [Fact]
    public void NestedTransformsComposeInnerFirst()
    {
        using var rig = new Rig(100, 100);
        var px = rig.Render(() =>
        {
            var ui = rig.Renderer.Ui;
            ui.PushTransform(Matrix3x2.CreateTranslation(50, 0));
            ui.PushTransform(Matrix3x2.CreateScale(2f));
            // A 10x10 square at (5, 5): scaled to 20x20 at (10, 10), then moved to x 60.
            ui.Fill(new Rect(5, 5, 10, 10), Color.White);
            ui.PopTransform();
            ui.PopTransform();
        }, new Color(0, 0, 0, 255));
        Assert.Equal((255, 255, 255, 255), Pixels.At(px, 100, 70, 20));
        Assert.Equal((0, 0, 0, 255), Pixels.At(px, 100, 58, 20));
        Assert.Equal((0, 0, 0, 255), Pixels.At(px, 100, 82, 20));
    }

    [Fact]
    public void UnbalancedStacksAreReported()
    {
        using var rig = new Rig(16, 16);
        rig.Renderer.BeginFrame(rig.Target);
        rig.Renderer.Ui.PushClip(new Rect(0, 0, 8, 8));
        var enc = rig.Device.BeginCommands();
        Assert.Throws<InvalidOperationException>(() => rig.Renderer.EndFrame(enc));
        // Recover so the rig can shut down cleanly.
        rig.Renderer.Ui.PopClip();
        rig.Device.Submit(enc);
    }

    [Fact]
    public void MissingArtShowsAsMagentaAndIsCounted()
    {
        using var s = new Scene(1);
        using var rig = new Rig(s.Width, s.Height);
        s.Tree.Root.With(Ui.Image("nope", 10, 10));
        var list = s.Tree.Frame();
        rig.Render(() => rig.Renderer.Ui.Draw(list, s.Text, s.Glyphs, s.Art));
        Assert.True(rig.Renderer.Ui.MissingArt >= 1);
    }
}
