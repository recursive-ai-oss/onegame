namespace OneGame.UI.Tests;

// The stand-in measurer makes a 10px font 6px per character and 12px per line, so every number below can be checked by hand.
public class TextLayoutTests
{
    private static Element Text(string text, Action<StyleBuilder>? style = null)
    {
        var l = Ui.Label(text).FontSize(10);
        return style is null ? l : l.Style(style);
    }

    private static UiTree Frame(Element content, float w = 400, float h = 300)
    {
        var ui = new UiTree();
        ui.SetViewport(w, h);
        ui.Root.Add(content);
        ui.Frame();
        return ui;
    }

    [Fact]
    public void LabelWrapsAtItsContainerWidth()
    {
        var label = Text("hello world");
        Frame(Ui.Column(label).Width(60));
        Assert.Equal(60, label.Bounds.Width);
        Assert.Equal(24, label.Bounds.Height);
        Assert.Equal(2, label.TextLines!.Count);
        Assert.Equal(new TextLine(0, 5, 30, 0, 12), label.TextLines[0]);
        Assert.Equal(new TextLine(6, 5, 30, 12, 12), label.TextLines[1]);
    }

    [Fact]
    public void AutoWidthRowUsesTheMaxContentWidth()
    {
        var a = Text("hello world");
        var b = Text("ab");
        var row = Ui.Row(a, b).Gap(4).Padding(3);
        Frame(Ui.Column(row).Align(Align.Start));
        Assert.Equal(66, a.Bounds.Width);
        Assert.Equal(12, a.Bounds.Height);
        Assert.Equal(66 + 4 + 12 + 6, row.Bounds.Width);
        Assert.Equal(12 + 6, row.Bounds.Height);
    }

    [Fact]
    public void FlexItemsNeverShrinkBelowTheirLongestWord()
    {
        var a = Text("hello");
        var b = Text("world");
        Frame(Ui.Row(a, b).Width(40).Align(Align.Start));
        Assert.Equal(30, a.Bounds.Width);
        Assert.Equal(30, b.Bounds.Width);
        Assert.Equal(30, b.Bounds.X);
    }

    [Fact]
    public void WrappedTextMakesAColumnTaller()
    {
        var a = Text("one two three four");
        var after = Ui.Box().Height(10);
        var col = Ui.Column(a, after).Width(80).AlignSelf(Align.Start);
        Frame(col);
        // 18 characters is 108px, so it wraps to two lines at 80px: "one two" and "three four" (60px, fits).
        Assert.Equal(2, a.TextLines!.Count);
        Assert.Equal(24, a.Bounds.Height);
        Assert.Equal(24, after.Bounds.Y);
        Assert.Equal(34, col.Bounds.Height);
    }

    [Fact]
    public void NoWrapAndPreAreHonored()
    {
        var nw = Text("hello world", s => s.WhiteSpace(WhiteSpace.NoWrap));
        var pre = Text("a\nbbb\ncc", s => s.WhiteSpace(WhiteSpace.Pre));
        Frame(Ui.Column(nw, pre).Width(20).Align(Align.Start));
        Assert.Single(nw.TextLines!);
        Assert.Equal(66, nw.Bounds.Width);
        Assert.Equal(3, pre.TextLines!.Count);
        Assert.Equal(18, pre.Bounds.Width);
        Assert.Equal(36, pre.Bounds.Height);
    }

    [Fact]
    public void LineHeightLetterSpacingAndPaddingCount()
    {
        var a = Text("abc", s => s.LineHeight(20).LetterSpacing(1).Padding(2, 5));
        Frame(Ui.Row(a).Align(Align.Start));
        Assert.Equal((3 * 7) + 10, a.Bounds.Width);
        Assert.Equal(20 + 4, a.Bounds.Height);
    }

    [Fact]
    public void TextAlignmentPositionsLinesInTheDisplayList()
    {
        var a = Text("ab", s => s.TextAlign(TextAlignment.Center));
        var b = Text("ab", s => s.TextAlign(TextAlignment.End));
        var c = Text("ab");
        var ui = Frame(Ui.Column(a, b, c).Width(100).Padding(0, 10));
        var lines = ui.DisplayList.Lines;
        Assert.Equal(3, lines.Length);
        Assert.Equal(10 + 34, lines[0].X);
        Assert.Equal(10 + 68, lines[1].X);
        Assert.Equal(10, lines[2].X);
        Assert.Equal(12, lines[1].Y);
    }

    [Fact]
    public void ChangingTextRelayoutsOnlyWhatMoves()
    {
        var a = Text("one");
        var b = Text("two");
        var ui = Frame(Ui.Column(a, b).Width(30).Align(Align.Start));
        Assert.Equal(12, b.Bounds.Y);
        a.Text = "one two";
        ui.Frame();
        Assert.Equal(24, a.Bounds.Height);
        Assert.Equal(24, b.Bounds.Y);
    }

    [Fact]
    public void ImagesUseTheirNaturalSizeAndAspect()
    {
        var img = Ui.Image("icon", 40, 20);
        var wide = Ui.Image("icon", 40, 20).Width(80);
        Frame(Ui.Column(img, wide).Align(Align.Start).Width(200));
        Assert.Equal(new Rect(0, 0, 40, 20), img.Bounds);
        Assert.Equal(new Rect(0, 20, 80, 40), wide.Bounds);
    }
}
