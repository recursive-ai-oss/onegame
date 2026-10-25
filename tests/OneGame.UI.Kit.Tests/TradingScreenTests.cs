using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace OneGame.UI.Kit.Tests;

/// <summary>
/// The acceptance screen: tabs, a 5,000-row sortable table with selection, a detail card in a nine-slice frame, a quantity slider and stepper,
/// a filter field, a confirm dialog and tooltips, all from kit components. Set ONEGAME_UPDATE_GOLDEN=1 to rewrite golden files after an intended change.
/// </summary>
public class TradingScreenTests(Xunit.ITestOutputHelper output)
{
    private static (Screen Screen, TradingGame Game, TableView<Good> Table, DialogView Dialog) Make(int rows = 5000)
    {
        var s = new Screen();
        var game = new TradingGame(rows);
        TradingScreen.Build(s.Host, game, out var table, out var dialog);
        s.Settle();
        return (s, game, table, dialog);
    }

    [Fact]
    public void LayoutBoxesMatchTheExpectedValues()
    {
        var (s, _, table, _) = Make();
        // 12 px screen padding, 8 px header padding, 14 px font at 1.2 line height => 16.8 px label, header 32.8 px.
        AssertBox(s, "screen", 0, 0, 1280, 720);
        var tabs = s.Get("tabs").AbsoluteBounds;
        Assert.Equal(12, tabs.X, 0.01f);
        Assert.Equal(12 + 32.8f + 8, tabs.Y, 0.05f);
        var detail = s.Ui.Query(".kit-frame")[0].AbsoluteBounds;
        Assert.Equal(300, detail.Width, 0.01f);
        Assert.Equal(1280 - 12 - 300, detail.X, 0.01f);
        var filter = s.Get("filter").AbsoluteBounds;
        Assert.Equal(12, filter.X, 0.01f);
        Assert.Equal(32, filter.Height, 0.01f);
        // The table fills the left column; its header is 30 px at least and rows are 28 px.
        var body = table.Body.AbsoluteBounds;
        var tb = table.AbsoluteBounds;
        Assert.Equal(filter.Width, tb.Width, 0.01f);
        Assert.True(body.Y > tb.Y + 30);
        Assert.Equal(28, s.Get("goods/row/0").Bounds.Height, 0.01f);
        Assert.Equal(28, s.Get("goods/row/1").Bounds.Y, 0.01f);
        // Flex columns split what the fixed ones leave: 90 + 80 fixed, 2:1 for name and category.
        var row = s.Get("goods/row/0");
        // Flex items start from their 16 px of padding, and the rest is shared 2:1.
        var flex = row.Bounds.Width - 170 - 32;
        Assert.Equal(tb.Width - 2, row.Bounds.Width, 0.01f);
        Assert.Equal((flex * 2 / 3) + 16, row.ChildAt(0).Bounds.Width, 0.05f);
        Assert.Equal((flex / 3) + 16, row.ChildAt(1).Bounds.Width, 0.05f);
        Assert.Equal(90, row.ChildAt(2).Bounds.Width, 0.01f);
        var header = s.Get("goods/header/0").Bounds;
        Assert.Equal(row.ChildAt(0).Bounds.Width, header.Width, 0.05f);
    }

    [Fact]
    public void TheTableIsVirtualizedAndSelectionAndScrollingWork()
    {
        var (s, game, table, _) = Make();
        Assert.Equal(5000, table.Body.Count);
        Assert.InRange(table.Body.RealizedRowCount, 10, 40);
        Assert.Equal(table.Body.RealizedRowCount, s.Ui.Query(".kit-table-row").Count(r => r.IsVisible));
        Assert.Equal(5000 * 28, table.Body.View.Scroller.ScrollSize.Y, 1f);
        s.Click(s.Get("goods/row/3"));
        Assert.Equal(3, table.Body.SelectedIndex);
        Assert.Equal(game.Selected, table.Body.SelectedItem);
        Assert.Equal("Salted Grain 4", s.Get("detail-name").Text);
        Assert.True(s.Get("goods/row/3").HasClass("selected"));
        // The wheel scrolls; rows are recycled and rebound, so row ids move with the data.
        s.Host.Dispatch(UiEvent.Wheel(300, 300, 0, 28 * 1000));
        s.Settle();
        Assert.Equal(1000, table.Body.FirstRealizedIndex);
        Assert.NotNull(s.Ui.Find("goods/row/1000"));
        Assert.Null(s.Ui.Find("goods/row/3"));
        Assert.Equal(table.Body.RealizedRowCount, s.Ui.Query(".kit-table-row").Count(r => r.IsVisible));
        // Keyboard: End jumps to the last row and shows it; Home goes back.
        s.Focus(table.Body);
        s.Key("End");
        Assert.Equal(4999, table.Body.SelectedIndex);
        Assert.Equal(4999 - table.Body.RealizedRowCount + 1, table.Body.FirstRealizedIndex);
        var last = s.Get("goods/row/4999").AbsoluteBounds;
        var view = table.Body.View.Scroller.AbsoluteBounds;
        Assert.True(last.Bottom <= view.Bottom + 0.01f && last.Y >= view.Y - 0.01f);
        s.Key("Home");
        Assert.Equal(0, table.Body.SelectedIndex);
        Assert.Equal(0, table.Body.FirstRealizedIndex);
        s.Key("PageDown");
        Assert.True(table.Body.SelectedIndex > 5);
        s.Key("ArrowUp");
        s.Key("ArrowDown");
        Assert.Equal("Selected", s.Ui.Query(".kit-card-title")[0].Text);
    }

    [Fact]
    public void SortingKeepsTheSelectedItemAndFlipsDirection()
    {
        var (s, game, table, _) = Make();
        s.Click(s.Get("goods/row/10"));
        var item = game.Selected!;
        s.Click(s.Get("goods/header/2"));
        Assert.Equal(2, table.SortColumn);
        Assert.False(table.SortDescending);
        Assert.True(s.Get("goods/header/2").HasClass("asc"));
        Assert.True(s.Get("goods/header/2").HasClass("sorted"));
        var prices = Enumerable.Range(0, 20).Select(i => game.View[i].Price).ToList();
        Assert.Equal(prices.OrderBy(p => p), prices);
        Assert.Equal(game.View.ToList().IndexOf(item), table.Body.SelectedIndex);
        Assert.Equal(item, table.Body.SelectedItem);
        Assert.Equal(game.View[0].Price.ToString(CultureInfo.InvariantCulture), s.Get("goods/row/0").ChildAt(2).Text);
        s.Click(s.Get("goods/header/2"));
        Assert.True(table.SortDescending);
        Assert.True(s.Get("goods/header/2").HasClass("desc"));
        Assert.False(s.Get("goods/header/2").HasClass("asc"));
        Assert.True(game.View[0].Price >= game.View[1].Price);
        Assert.Equal(item, table.Body.SelectedItem);
        // The header takes keyboard activation too, and another column resets to ascending.
        s.Focus(s.Get("goods/header/0"));
        s.Key("Enter");
        Assert.Equal(0, table.SortColumn);
        Assert.False(table.SortDescending);
        Assert.False(s.Get("goods/header/2").HasClass("sorted"));
        var names = Enumerable.Range(0, 20).Select(i => game.View[i].Name).ToList();
        Assert.Equal(names.Order(StringComparer.Ordinal), names);
    }

    [Fact]
    public void FilteringNarrowsTheRowsAndDropsAMissingSelection()
    {
        var (s, game, table, _) = Make();
        s.Click(s.Get("goods/row/0"));
        Assert.Equal("Fine Grain 1", game.Selected!.Name);
        s.Click(s.Get("filter"));
        s.Type("jade");
        Assert.Equal("jade", game.Filter);
        Assert.Equal(200, table.Body.Count);
        Assert.All(Enumerable.Range(0, table.Body.RealizedRowCount), i => Assert.Contains("Jade", s.Get("goods/row/" + i).ChildAt(0).Text!, StringComparison.Ordinal));
        Assert.Equal(-1, table.Body.SelectedIndex);
        s.Type(" 1");
        Assert.True(table.Body.Count is > 0 and < 200);
        s.Key("a", KeyModifiers.Ctrl);
        s.Key("Backspace");
        Assert.Equal(5000, table.Body.Count);
        // Filter then sort, then pick a row: the detail card shows the same item as the row.
        s.Type("wine");
        s.Click(s.Get("goods/header/3"));
        s.Click(s.Get("goods/row/2"));
        Assert.Equal(s.Get("goods/row/2").ChildAt(0).Text, s.Get("detail-name").Text);
    }

    [Fact]
    public void TheBuyFlowGoesThroughTheConfirmDialogWithFocusTrapped()
    {
        var (s, game, table, dialog) = Make();
        var buy = s.Get("buy");
        Assert.False(buy.IsEnabled);
        s.Click(s.Get("goods/row/5"));
        Assert.True(buy.IsEnabled);
        s.Click(s.Get("qty-stepper/plus"));
        s.Click(s.Get("qty-stepper/plus"));
        Assert.Equal(3, game.Qty);
        Assert.Equal("Total " + game.Total.ToString(CultureInfo.InvariantCulture), s.Get("total").Text);
        var gold = game.Gold;
        var price = game.Total;
        s.Click(buy);
        Assert.True(dialog.IsOpen);
        Assert.Contains("Buy 3 x", s.Ui.Query(".kit-dialog-panel Label")[1].Text!, StringComparison.Ordinal);
        // The dialog's default button is focused, focus cannot leave, and the table behind does not react to the pointer.
        Assert.Same(s.Get("confirm/buttons/0"), s.Ui.Focused);
        for (var i = 0; i < 6; i++)
        {
            s.Key("Tab");
            Assert.True(dialog.Contains(s.Ui.Focused));
        }

        s.Nav(NavDirection.Down);
        s.Nav(NavDirection.Left);
        Assert.True(dialog.Contains(s.Ui.Focused));
        var row = s.Get("goods/row/7").AbsoluteBounds;
        s.Host.Dispatch(UiEvent.PointerDown(row.X + 5, row.Y + 5));
        s.Host.Dispatch(UiEvent.PointerUp(row.X + 5, row.Y + 5));
        Assert.Equal(5, table.Body.SelectedIndex);
        s.Cancel();
        Assert.False(dialog.IsOpen);
        Assert.Equal(gold, game.Gold);
        Assert.Same(buy, s.Ui.Focused);
        s.Key("Enter");
        Assert.True(dialog.IsOpen);
        s.Focus(s.Get("confirm/buttons/1"));
        s.Activate();
        Assert.False(dialog.IsOpen);
        Assert.Equal(gold - price, game.Gold);
        Assert.Equal("Gold " + game.Gold.ToString(CultureInfo.InvariantCulture), s.Get("gold").Text);
        Assert.Same(buy, s.Ui.Focused);
    }

    [Fact]
    public void KeyboardAndDpadReachEveryControlOnTheScreen()
    {
        var (s, game, table, _) = Make();
        var first = FocusHelpers.FocusDefault(s.Ui, s.Get("screen"));
        Assert.NotNull(first);
        var order = new List<string>();
        for (var i = 0; i < 40; i++)
        {
            order.Add(Describe(s.Ui.Focused));
            s.Key("Tab");
        }

        var seen = order.Distinct().ToList();
        output.WriteLine("Tab order: " + string.Join(" > ", order.Take(14)));
        foreach (var expected in new[] { "tabs/tab/0", "filter", "goods/header/0", "goods/header/1", "goods/header/2", "goods/header/3", "goods/body", "qty-slider", "qty-stepper" })
        {
            Assert.Contains(expected, seen);
        }

        // Tab wraps; Shift+Tab goes back.
        Assert.Equal(order[0], order[seen.Count]);
        // Directional moves with the d-pad: filter -> header -> table -> slider.
        s.Focus(s.Get("filter"));
        s.Nav(NavDirection.Down);
        Assert.StartsWith("goods/header/", Describe(s.Ui.Focused), StringComparison.Ordinal);
        var under = Describe(s.Ui.Focused);
        s.Nav(NavDirection.Right);
        Assert.NotEqual(under, Describe(s.Ui.Focused));
        Assert.StartsWith("goods/header/", Describe(s.Ui.Focused), StringComparison.Ordinal);
        s.Nav(NavDirection.Left);
        Assert.Equal(under, Describe(s.Ui.Focused));
        s.Nav(NavDirection.Down);
        Assert.Equal("goods/body", Describe(s.Ui.Focused));
        Assert.Equal(0, table.Body.SelectedIndex);
        s.Nav(NavDirection.Down);
        s.Nav(NavDirection.Down);
        Assert.Equal(2, table.Body.SelectedIndex);
        s.Nav(NavDirection.Up);
        s.Nav(NavDirection.Up);
        Assert.Equal(0, table.Body.SelectedIndex);
        s.Nav(NavDirection.Up); // off the first row: focus leaves toward the headers
        Assert.StartsWith("goods/header/", Describe(s.Ui.Focused), StringComparison.Ordinal);
        s.Nav(NavDirection.Down);
        Assert.Equal("goods/body", Describe(s.Ui.Focused));
        s.Nav(NavDirection.Right);
        Assert.True(s.Ui.Query(".kit-frame")[0].Contains(s.Ui.Focused), "Right from the table lands in the detail panel, not on the headers above it");
        s.Focus(s.Get("qty-slider"));
        s.Nav(NavDirection.Right);
        Assert.Equal(2, game.Qty);
        s.Nav(NavDirection.Down);
        Assert.Equal("qty-stepper", Describe(s.Ui.Focused));
        s.Nav(NavDirection.Right);
        Assert.Equal(3, game.Qty);
        // The tab strip answers Left and Right and moves the content.
        s.Focus(s.Get("tabs/tab/0"));
        s.Nav(NavDirection.Right);
        Assert.Equal(1, game.Tab);
        Assert.False(s.Get("tabs/panel/0").IsVisible);
    }

    [Fact]
    public void TooltipsAppearOnHoverAfterTheDelay()
    {
        var (s, _, _, _) = Make();
        var b = s.Get("qty-slider").AbsoluteBounds;
        s.Host.Dispatch(UiEvent.PointerMove(b.X + 20, b.Y + 4));
        s.Host.Advance(0.6f);
        s.Settle();
        Assert.True(s.Host.TooltipLayer.IsVisible);
        Assert.Equal("Drag, or use Left and Right", s.Host.TooltipLayer.ChildAt(0).Text);
        var t = s.Host.TooltipLayer.AbsoluteBounds;
        Assert.True(t.X >= 0 && t.Right <= 1280.01f && t.Bottom <= 720.01f);
    }

    [Fact]
    public void NothingIsAllocatedOnAFrameWhereNothingChanged()
    {
        var (s, _, _, _) = Make();
        s.Click(s.Get("goods/row/2"));
        s.Focus(s.Get("filter"));
        s.Type("a");
        s.Key("ArrowLeft");
        s.Settle(8);
        var bytes = s.AllocatedOverFrames(200);
        output.WriteLine("Bytes allocated over 200 unchanged frames: " + bytes);
        Assert.Equal(0, bytes);
    }

    [Fact]
    public void FrameTimesForTheFiveThousandRowTable()
    {
        var sw = Stopwatch.StartNew();
        var (s, _, table, _) = Make();
        var build = sw.Elapsed.TotalMilliseconds;
        // Warm up, then time frames in which the table scrolls one row (all rows rebound) and frames where nothing changed.
        for (var i = 0; i < 20; i++)
        {
            s.Host.Dispatch(UiEvent.Wheel(300, 300, 0, 28));
            s.Ui.Frame();
        }

        sw.Restart();
        const int Scrolls = 400;
        for (var i = 0; i < Scrolls; i++)
        {
            s.Host.Dispatch(UiEvent.Wheel(300, 300, 0, 28));
            s.Ui.Frame();
        }

        var scrollMs = sw.Elapsed.TotalMilliseconds / Scrolls;
        sw.Restart();
        const int Idle = 2000;
        for (var i = 0; i < Idle; i++)
        {
            s.Host.Advance(0.016f);
            s.Ui.Frame();
        }

        var idleMs = sw.Elapsed.TotalMilliseconds / Idle;
        output.WriteLine($"build+first frames {build:0.0} ms; scroll one row per frame (rebind {table.Body.RealizedRowCount} rows, relayout, repaint) {scrollMs:0.000} ms; unchanged frame {idleMs:0.0000} ms");
        Assert.True(scrollMs < 8, $"scrolling frame took {scrollMs} ms");
        Assert.True(idleMs < 1, $"unchanged frame took {idleMs} ms");
    }

    [Fact]
    public void DisplayListAndOutlineMatchTheGoldenFiles()
    {
        var (s, _, _, _) = Make();
        s.Click(s.Get("goods/row/2"));
        s.Host.Dispatch(UiEvent.PointerMove(1, 1));
        s.Settle();
        var outline = Outline(s.Root);
        Assert.Equal(Golden("trading-outline.txt", outline), outline);
        var list = s.Ui.DisplayList.ToText();
        Assert.Equal(Golden("trading-displaylist.txt", list), list);
    }

    // ---- helpers -----------------------------------------------------------------------------------------------

    private static string Describe(Element? e) => e is null ? "(none)" : e.Id ?? e.Kind;

    private static void AssertBox(Screen s, string id, float x, float y, float w, float h)
    {
        var r = s.Get(id).AbsoluteBounds;
        Assert.True(MathF.Abs(r.X - x) < 0.01f && MathF.Abs(r.Y - y) < 0.01f && MathF.Abs(r.Width - w) < 0.01f && MathF.Abs(r.Height - h) < 0.01f, $"{id}: expected ({x},{y} {w}x{h}) got {r}");
    }

    // One line per element that has a role: depth, role, id, value, state classes and the screen box. This is what a scenario would query.
    private static string Outline(Element root)
    {
        var sb = new StringBuilder();
        void Walk(Element e, int depth)
        {
            if (!e.IsVisible)
            {
                return;
            }

            if (e.Role is not null)
            {
                var b = e.AbsoluteBounds;
                sb.Append(' ', depth * 2).Append(e.Role);
                if (e.Id is not null)
                {
                    sb.Append(" #").Append(e.Id);
                }

                if (e.Value is not null)
                {
                    sb.Append(" =").Append(e.Value);
                }

                var states = string.Join(',', e.Classes.Where(c => c is "selected" or "checked" or "sorted" or "asc" or "desc" or "open" or "odd"));
                if (states.Length > 0)
                {
                    sb.Append(" [").Append(states).Append(']');
                }

                if (e.IsFocused)
                {
                    sb.Append(" focus");
                }

                sb.Append(CultureInfo.InvariantCulture, $" ({b.X:0.##},{b.Y:0.##} {b.Width:0.##}x{b.Height:0.##})\n");
                depth++;
            }

            for (var i = 0; i < e.ChildCount; i++)
            {
                Walk(e.ChildAt(i), depth);
            }
        }

        Walk(root, 0);
        return sb.ToString();
    }

    private static string Golden(string name, string actual, [CallerFilePath] string here = "")
    {
        var dir = Path.Combine(Path.GetDirectoryName(here)!, "golden");
        var path = Path.Combine(dir, name);
        if (Environment.GetEnvironmentVariable("ONEGAME_UPDATE_GOLDEN") == "1")
        {
            Directory.CreateDirectory(dir);
            File.WriteAllText(path, actual);
        }

        return File.ReadAllText(path);
    }
}
