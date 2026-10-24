using OneGame.UI;

namespace OneGame.Text.Tests;

/// <summary>A trading-screen-shaped UI laid out with the real measurer: header, tabs, a table, a tooltip that appears on hover.</summary>
public class UiIntegrationTests
{
    internal sealed record Good(string Name, int Price, int Stock);

    internal sealed class Game
    {
        public int Gold = 1200;
        public List<Good> Market { get; } = [new("Grain", 12, 40), new("Iron", 30, 8), new("Silk", 95, 3)];
    }

    internal static (UiTree Ui, Game Game, FontTextMeasurer Measurer, GlyphAtlas Atlas) Build(float scale = 1)
    {
        var g = new Game();
        var lib = Fonts.Library();
        var measurer = new FontTextMeasurer(lib);
        var ui = new UiTree(measurer);
        ui.SetViewport(640 * scale, 360 * scale, scale);
        ui.AddStyleSheet(new StyleSheet()
            .Rule(":root", s => s.FontSize(Length.Ui(14)).TextColor(Color.Hex("#e8e8e8")))
            .Rule(".screen", s => s.Background(Color.Hex("#14161a")))
            .Rule(".header", s => s.Background(Color.Hex("#23262d")).Padding(Length.Ui(6), Length.Ui(10)))
            .Rule(".cell", s => s.Width(Length.Ui(120))));

        static Element Cells(string a, string b, string c) =>
            Ui.Row(Ui.Label(a).Class("cell"), Ui.Label(b).Class("cell"), Ui.Label(c).Class("cell"));

        var header = Ui.Row(Ui.Label("Port of Aldeen").Named("title"), Ui.Spacer(), Ui.Label("").BindText(() => g.Gold, gold => $"Gold: {gold}").Named("gold")).Class("header");
        var table = Ui.Column(
            Cells("Good", "Price", "Stock"),
            Ui.Each(() => g.Market, good => Cells(good.Name, good.Price.ToString(System.Globalization.CultureInfo.InvariantCulture), good.Stock.ToString(System.Globalization.CultureInfo.InvariantCulture)))
                .Column()).Padding(Length.Ui(10));
        var note = Ui.Label("Prices change with the tide: buy grain where it is cheap and sell it where the harbour is hungry.").Named("note").Width(Length.Ui(300));
        ui.Root.With(Ui.Column(header, table, note).Class("screen").Grow());
        ui.Frame();
        return (ui, g, measurer, new GlyphAtlas(lib));
    }

    [Fact]
    public void RealMeasurerProducesTextRunsWithGlyphsForEveryLine()
    {
        var (ui, _, measurer, atlas) = Build();
        var list = ui.Frame();
        Assert.True(list.TextRuns.Length >= 12, $"{list.TextRuns.Length} text runs");
        var note = ui.Find("note")!;
        var noteRun = list.TextRuns[^1];
        Assert.StartsWith("Prices change", noteRun.Text, StringComparison.Ordinal);
        Assert.True(noteRun.LineCount >= 2, "the note wraps at 300 px");
        Assert.True(note.AbsoluteBounds.Width <= 300.01f);

        atlas.BeginFrame(1);
        var quads = new List<GlyphQuad>();
        Assert.Equal(0, new TextQuadBuilder(measurer, atlas).BuildAll(list, quads));

        // Every non-space character of every run became a quad.
        var expected = 0;
        for (var r = 0; r < list.TextRuns.Length; r++)
        {
            var run = list.TextRuns[r];
            for (var i = 0; i < run.LineCount; i++)
            {
                var line = list.Lines[run.FirstLine + i];
                Assert.True(line.Width <= list.Width);
                foreach (var c in run.Text.AsSpan(line.Start, line.Length))
                {
                    expected += char.IsWhiteSpace(c) ? 0 : 1;
                }
            }
        }

        Assert.Equal(expected, quads.Count);
        Assert.All(quads, q => Assert.True(q.X0 >= 0 && q.X1 <= list.Width && q.Y0 >= 0 && q.Y1 <= list.Height));
    }

    [Fact]
    public void UiScaleChangesGlyphSizeAndLayoutTogether()
    {
        var (ui1, _, m1, _) = Build(1);
        var (ui2, _, m2, _) = Build(2);
        var t1 = ui1.Find("title")!.AbsoluteBounds;
        var t2 = ui2.Find("title")!.AbsoluteBounds;
        Assert.Equal(t1.Width * 2, t2.Width, 0.2);
        Assert.Equal(t1.Height * 2, t2.Height, 0.2);
        Assert.NotNull(m1);
        Assert.NotNull(m2);
    }

    [Fact]
    public void NoChangeFrameAllocatesNothingAndChangedTextReshapesOnlyThatText()
    {
        var (ui, g, measurer, atlas) = Build();
        var quads = new List<GlyphQuad>(1024);
        var builder = new TextQuadBuilder(measurer, atlas);
        var list = ui.Frame();
        atlas.BeginFrame(1);
        builder.BuildAll(list, quads);

        // Warm everything once more, then count.
        ui.Frame();
        var version = list.Version;
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 100; i++)
        {
            ui.Frame();
        }

        var idle = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(0, idle);
        Assert.Equal(version, ui.DisplayList.Version);

        // Rebuilding quads for an unchanged list hits the shaping and glyph caches: no allocation either.
        quads.Clear();
        builder.BuildAll(list, quads);
        quads.Clear();
        before = GC.GetAllocatedBytesForCurrentThread();
        builder.BuildAll(list, quads);
        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);

        // A bound value changes: the display list changes and the new text is shaped.
        g.Gold = 1357;
        ui.Frame();
        Assert.True(ui.DisplayList.Version > version);
        Assert.Contains(ui.DisplayList.TextRuns.ToArray(), r => r.Text == "Gold: 1357");
    }
}
