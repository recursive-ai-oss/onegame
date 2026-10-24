using System.Diagnostics;

namespace OneGame.UI.Tests;

public class TreeTests
{
    private sealed class Row(int id, string name)
    {
        public int Id { get; } = id;

        public string Name { get; set; } = name;
    }

    private static void Note(string line)
    {
        var dir = Environment.GetEnvironmentVariable("ONEGAME_UI_REPORT");
        if (dir is not null)
        {
            File.AppendAllText(dir, line + "\n");
        }
    }

    // A table of 100 rows of 10 cells: 1,100 elements.
    private static (UiTree Ui, Element Table, int[] Values) BuildTable(int rows = 100, int cols = 10)
    {
        var values = new int[rows * cols];
        var ui = new UiTree();
        ui.SetViewport(1280, 720);
        ui.AddStyleSheet(new StyleSheet()
            .Rule(".row", s => s.Direction(FlexDirection.Row).Height(20).Gap(2))
            .Rule(".row:nth-child(even)", s => s.Background(Color.Hex("#202020")))
            .Rule(".cell", s => s.Grow(1).Basis(0f).Padding(2, 4).FontSize(12)));
        var table = Ui.Column().Named("table").Overflow(OverflowMode.Scroll).Grow().Style(s => s.Height(720));
        for (var r = 0; r < rows; r++)
        {
            var row = new Element("Row").Class("row");
            for (var c = 0; c < cols; c++)
            {
                var i = (r * cols) + c;
                row.Add(new Element("Label").Class("cell").BindText(() => values[i], v => "v" + v.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            }

            table.Add(row);
        }

        ui.Root.Add(table);
        return (ui, table, values);
    }

    [Fact]
    public void UnchangedFrameAllocatesNothing()
    {
        var (ui, table, _) = BuildTable();
        Assert.True(CountElements(ui.Root) >= 1100);
        for (var i = 0; i < 5; i++)
        {
            ui.Frame();
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++)
        {
            ui.Frame();
        }

        var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Note($"no-change frame: {bytes} bytes over 1000 frames, {CountElements(ui.Root)} elements");
        Assert.Equal(0, bytes);
        Assert.Equal(1280, table.Bounds.Width);
    }

    [Fact]
    public void UnchangedPointerMoveAllocatesNothing()
    {
        var (ui, _, _) = BuildTable();
        ui.Dispatch(UiEvent.PointerMove(100, 100));
        ui.Frame();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++)
        {
            ui.Dispatch(UiEvent.PointerMove(100, 100));
            ui.Frame();
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    [Fact]
    public void ReportsTimesForAThousandElementScreen()
    {
        var (ui, table, values) = BuildTable();
        var sw = Stopwatch.StartNew();
        ui.Frame();
        var first = sw.Elapsed.TotalMilliseconds;

        // Full layout again: a scale change invalidates every box.
        sw.Restart();
        ui.SetViewport(1280, 720, 1.5f);
        ui.Update();
        var relayout = sw.Elapsed.TotalMilliseconds;
        sw.Restart();
        ui.Frame();
        var paint = sw.Elapsed.TotalMilliseconds;

        // One cell changes its text: only that row is relaid out.
        values[505] = 42;
        sw.Restart();
        ui.Frame();
        var oneCell = sw.Elapsed.TotalMilliseconds;

        // Many repeats for a stable full-layout figure.
        var best = double.MaxValue;
        for (var i = 0; i < 20; i++)
        {
            ui.SetViewport(1280, 720, 1 + (i % 2));
            sw.Restart();
            ui.Update();
            best = Math.Min(best, sw.Elapsed.TotalMilliseconds);
        }

        Note($"{CountElements(ui.Root)} elements: first frame (style+layout+paint, cold) {first:0.00} ms; full relayout {relayout:0.00} ms (best of 20: {best:0.00} ms); paint {paint:0.00} ms; one changed cell {oneCell:0.000} ms; display list {ui.DisplayList.Commands.Length} commands");
        Assert.True(best < 100, $"full layout took {best} ms");
        Assert.Equal("v42", ((Label505(table))).Text);
    }

    private static Element Label505(Element table) => table.ChildAt(50).ChildAt(5);

    private static int CountElements(Element e)
    {
        var n = 1;
        for (var i = 0; i < e.ChildCount; i++)
        {
            n += CountElements(e.ChildAt(i));
        }

        return n;
    }

    [Fact]
    public void ChangingOneLeafDoesNotRelayoutUnrelatedSubtrees()
    {
        var (ui, table, values) = BuildTable(20, 4);
        ui.Frame();
        var untouched = table.ChildAt(10);
        Assert.False(untouched.LayoutDirty);
        values[0] = 7;
        ui.Frame();
        Assert.False(untouched.LayoutDirty);
        Assert.Equal("v7", table.ChildAt(0).ChildAt(0).Text);
    }

    [Fact]
    public void NoWorkAndNoNewDisplayListWhenNothingChanged()
    {
        var (ui, _, _) = BuildTable(5, 3);
        var list = ui.Frame();
        var v = list.Version;
        ui.Frame();
        ui.Frame();
        Assert.Equal(v, list.Version);
    }

    [Fact]
    public void BindingsReEvaluateEveryFrameAndOnlyTouchOnChange()
    {
        var ui = new UiTree();
        var count = 0;
        var calls = 0;
        var shown = true;
        var on = false;
        var label = new Element("Label")
            .BindText(() => count, v => "n=" + v.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .BindClass("hot", () => on)
            .BindVisible(() => shown)
            .OnUpdate(_ => calls++);
        ui.Root.Add(label);
        ui.Frame();
        Assert.Equal("n=0", label.Text);
        Assert.False(label.HasClass("hot"));
        count = 5;
        on = true;
        ui.Frame();
        Assert.Equal("n=5", label.Text);
        Assert.True(label.HasClass("hot"));
        shown = false;
        ui.Frame();
        Assert.False(label.IsVisible);
        Assert.Equal(3, calls);
    }

    [Fact]
    public void StyleBindingFollowsStateWithoutChurn()
    {
        var ui = new UiTree();
        var x = 10;
        var e = new Element().Size(20, 20).Anchor(AnchorPoint.TopLeft).BindStyle(s => s.Left(x).Top(5));
        ui.Root.Add(e);
        ui.Frame();
        Assert.Equal(10, e.Bounds.X);
        x = 30;
        ui.Frame();
        Assert.Equal(30, e.Bounds.X);
        Assert.Equal(5, e.Bounds.Y);
    }

    [Fact]
    public void BindChildrenKeepsElementsForItemsThatStay()
    {
        var items = new List<Row> { new(1, "a"), new(2, "b"), new(3, "c") };
        var built = 0;
        var ui = new UiTree();
        var list = Ui.Each<Row>(() => items, r =>
        {
            built++;
            return Ui.Label(r.Name).Named("row" + r.Id);
        });
        ui.Root.Add(list);
        ui.Frame();
        Assert.Equal(3, list.ChildCount);
        var b = ui.Find("row2")!;

        items.Reverse();
        ui.Frame();
        Assert.Equal(["row3", "row2", "row1"], list.Children.Select(c => c.Id));
        Assert.Same(b, ui.Find("row2"));
        Assert.Equal(3, built);

        items.RemoveAt(1);
        items.Add(new Row(4, "d"));
        ui.Frame();
        Assert.Equal(["row3", "row1", "row4"], list.Children.Select(c => c.Id));
        Assert.Equal(4, built);
        Assert.Null(ui.Find("row2"));
    }

    [Fact]
    public void StructuralMistakesThrowATypedException()
    {
        var a = new Element();
        var b = new Element();
        a.Add(b);
        Assert.Throws<UiTreeException>(() => new Element().Add(b));
        Assert.Throws<UiTreeException>(() => b.Add(a));
        Assert.Throws<UiTreeException>(() => a.Add(a));
        var ui = new UiTree();
        Assert.Throws<UiTreeException>(() => a.Add(ui.Root));
        Assert.True(a.Remove(b));
        Assert.False(a.Remove(b));
        Assert.Null(b.Parent);
    }

    [Fact]
    public void RemovedElementsDropHoverAndFocus()
    {
        var ui = new UiTree();
        var btn = Ui.Button("x", () => { }).Size(50, 50);
        ui.Root.Add(btn);
        ui.Dispatch(UiEvent.PointerDown(5, 5));
        Assert.Same(btn, ui.Focused);
        Assert.Same(btn, ui.Hovered);
        btn.RemoveFromParent();
        ui.Frame();
        Assert.Null(ui.Focused);
        Assert.Null(ui.Hovered);
    }

    [Fact]
    public void ResizingTheScreenRelaysOutPercentagesAndAnchors()
    {
        var ui = new UiTree();
        ui.SetViewport(400, 300);
        var half = new Element().Style(s => s.Width(Length.Percent(50)).Height(Length.Percent(100)));
        var hud = new Element().Size(40, 20).Style(s => s.Anchor(AnchorPoint.BottomRight, 5, 5, toScreen: true));
        ui.Root.With(half, hud);
        ui.Frame();
        Assert.Equal(new Rect(0, 0, 200, 300), half.Bounds);
        Assert.Equal(new Rect(355, 275, 40, 20), hud.AbsoluteBounds);
        ui.SetViewport(800, 600);
        ui.Frame();
        Assert.Equal(new Rect(0, 0, 400, 600), half.Bounds);
        Assert.Equal(new Rect(755, 575, 40, 20), hud.AbsoluteBounds);
    }

    [Fact]
    public void InlineStyleChangesRelayoutAndRepaint()
    {
        var ui = new UiTree();
        var a = new Element().Size(50, 20);
        var b = new Element().Size(50, 20);
        ui.Root.With(Ui.Column(a, b));
        var v = ui.Frame().Version;
        Assert.Equal(20, b.Bounds.Y);
        a.Height(35);
        ui.Frame();
        Assert.Equal(35, b.Bounds.Y);
        Assert.True(ui.DisplayList.Version > v);
        v = ui.DisplayList.Version;
        a.Background(Color.Hex("#123456"));
        ui.Frame();
        Assert.True(ui.DisplayList.Version > v);
        Assert.Equal(35, b.Bounds.Y);
    }
}
