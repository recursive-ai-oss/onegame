namespace OneGame.UI.Tests;

public class HitFocusTests
{
    private static UiTree Screen(float w = 400, float h = 300)
    {
        var ui = new UiTree();
        ui.SetViewport(w, h);
        return ui;
    }

    private static Element Box(string id, float x, float y, float w, float h) =>
        new Element("Box").Named(id).Style(s => s.Anchor(AnchorPoint.TopLeft, x, y).Size(w, h));

    // ---- hit testing ---------------------------------------------------------------------------------------------

    [Fact]
    public void TopmostSiblingWinsAndZIndexReorders()
    {
        var ui = Screen();
        var a = Box("a", 0, 0, 100, 100);
        var b = Box("b", 50, 50, 100, 100);
        ui.Root.With(a, b);
        Assert.Same(a, ui.HitTest(10, 10));
        Assert.Same(b, ui.HitTest(60, 60));
        a.Style(s => s.ZIndex(1));
        Assert.Same(a, ui.HitTest(60, 60));
        Assert.Null(ui.HitTest(300, 250) is { } e && e != ui.Root ? e : null);
        Assert.Same(ui.Root, ui.HitTest(300, 250));
    }

    [Fact]
    public void PointerEventsVisibilityAndDisplayLetInputThrough()
    {
        var ui = Screen();
        var under = Box("under", 0, 0, 100, 100);
        var none = Box("none", 0, 0, 100, 100).Style(s => s.PointerEvents(PointerEvents.None));
        var hidden = Box("hidden", 0, 0, 100, 100).Style(s => s.Visibility(VisibilityMode.Hidden));
        var gone = Box("gone", 0, 0, 100, 100).Style(s => s.Display(DisplayMode.None));
        ui.Root.With(under, none, hidden, gone);
        Assert.Same(under, ui.HitTest(50, 50));
    }

    [Fact]
    public void ClippedContentCannotBeHitOutsideItsContainer()
    {
        var ui = Screen();
        var child = Box("child", 0, 0, 200, 200);
        var clip = Box("clip", 10, 10, 50, 50).Overflow(OverflowMode.Hidden).With(child);
        ui.Root.Add(clip);
        Assert.Same(child, ui.HitTest(30, 30));
        Assert.Same(ui.Root, ui.HitTest(100, 100));
    }

    [Fact]
    public void ScrollOffsetMovesWhatIsUnderThePointer()
    {
        var ui = Screen();
        var rows = Enumerable.Range(0, 10).Select(i => new Element().Named("r" + i).Height(40).Shrink(0)).ToArray();
        var list = Ui.Column(rows).Named("list").Size(100, 100).Overflow(OverflowMode.Scroll).Style(s => s.Anchor(AnchorPoint.TopLeft));
        ui.Root.Add(list);
        ui.Frame();
        Assert.Equal(400, list.ScrollSize.Y);
        Assert.Equal(300, list.MaxScroll.Y);
        Assert.Same(rows[0], ui.HitTest(10, 10));
        Assert.Same(rows[2], ui.HitTest(10, 90));
        list.ScrollTo(0, 120);
        Assert.Same(rows[3], ui.HitTest(10, 10));
        Assert.Equal(new Rect(0, 0, 100, 40), rows[3].AbsoluteBounds);
        list.ScrollTo(0, 9999);
        Assert.Equal(300, list.ScrollY);
        // Wheel input scrolls the container under the pointer.
        Assert.True(ui.Dispatch(UiEvent.Wheel(10, 10, 0, -100)));
        Assert.Equal(200, list.ScrollY);
    }

    [Fact]
    public void TransformsApplyToHitTesting()
    {
        var ui = Screen();
        var scaled = Box("scaled", 0, 0, 100, 100).Style(s => s.Scale(0.5f));
        var rotated = Box("rotated", 0, 150, 100, 20).Style(s => s.Rotate(90));
        var moved = Box("moved", 200, 0, 100, 50).Style(s => s.Translate(Length.Percent(50), 20));
        ui.Root.With(scaled, rotated, moved);
        // Scaled about its center: only the middle 50x50 remains.
        Assert.Same(scaled, ui.HitTest(50, 50));
        Assert.Same(ui.Root, ui.HitTest(5, 5));
        // Rotated a quarter turn about (50, 160): now 20 wide and 100 tall.
        Assert.Same(rotated, ui.HitTest(50, 120));
        Assert.Same(ui.Root, ui.HitTest(10, 160));
        // Translated by half its width and 20px down.
        Assert.Same(ui.Root, ui.HitTest(210, 10));
        Assert.Same(moved, ui.HitTest(270, 60));
    }

    [Fact]
    public void FixedElementsIgnoreTheirParentsScroll()
    {
        var ui = Screen();
        var hud = new Element().Named("hud").Style(s => s.Anchor(AnchorPoint.BottomRight, 10, 10, toScreen: true).Size(50, 20));
        var page = Ui.Column(new Element().Height(1000), hud).Size(400, 300).Overflow(OverflowMode.Scroll);
        ui.Root.Add(page);
        ui.Frame();
        Assert.Equal(new Rect(340, 270, 50, 20), hud.AbsoluteBounds);
        page.ScrollTo(0, 500);
        Assert.Same(hud, ui.HitTest(360, 280));
        Assert.Equal(new Rect(340, 270, 50, 20), hud.AbsoluteBounds);
    }

    // ---- clicks --------------------------------------------------------------------------------------------------

    [Fact]
    public void ClickNeedsPressAndReleaseOnTheSameElement()
    {
        var ui = Screen();
        var clicks = new List<string>();
        var a = Box("a", 0, 0, 100, 50).OnClick(() => clicks.Add("a"));
        var b = Box("b", 150, 0, 100, 50).OnClick(() => clicks.Add("b"));
        ui.Root.With(a, b);
        ui.Dispatch(UiEvent.PointerDown(10, 10));
        ui.Dispatch(UiEvent.PointerUp(10, 10));
        ui.Dispatch(UiEvent.PointerDown(10, 10));
        ui.Dispatch(UiEvent.PointerUp(160, 10));
        ui.Dispatch(UiEvent.PointerDown(160, 10));
        ui.Dispatch(UiEvent.PointerUp(160, 10));
        Assert.Equal(["a", "b"], clicks);
    }

    [Fact]
    public void DisabledElementsIgnoreClicksAndHandlersBubble()
    {
        var ui = Screen();
        var hits = new List<string>();
        var inner = Box("inner", 0, 0, 100, 50).OnClick(() => hits.Add("inner"));
        var outer = Box("outer", 0, 0, 200, 100).With(inner).OnClick(() => hits.Add("outer"));
        ui.Root.Add(outer);
        // The inner handler takes the click, so it does not bubble.
        ui.Dispatch(UiEvent.PointerDown(10, 10));
        ui.Dispatch(UiEvent.PointerUp(10, 10));
        Assert.Equal(["inner"], hits);
        inner.IsEnabled = false;
        ui.Dispatch(UiEvent.PointerDown(10, 10));
        ui.Dispatch(UiEvent.PointerUp(10, 10));
        Assert.Equal(["inner"], hits);
        Assert.False(ui.Invoke(inner));
        Assert.True(ui.Invoke(outer));
    }

    [Fact]
    public void HoverEnterAndLeaveFireOncePerElement()
    {
        var ui = Screen();
        var log = new List<string>();
        var a = Box("a", 0, 0, 100, 50).OnPointerEnter(() => log.Add("+a")).OnPointerLeave(() => log.Add("-a"));
        ui.Root.Add(a);
        ui.Dispatch(UiEvent.PointerMove(10, 10));
        ui.Dispatch(UiEvent.PointerMove(20, 20));
        ui.Dispatch(UiEvent.PointerMove(300, 200));
        ui.Dispatch(UiEvent.PointerMove(10, 10));
        ui.Dispatch(UiEvent.PointerLeftWindow());
        Assert.Equal(["+a", "-a", "+a", "-a"], log);
    }

    [Fact]
    public void CursorFollowsTheHoveredElement()
    {
        var ui = Screen();
        ui.Root.Add(Box("a", 0, 0, 100, 50).Style(s => s.Cursor(CursorShape.Hand)));
        Assert.Equal(CursorShape.Default, ui.Cursor);
        ui.Dispatch(UiEvent.PointerMove(10, 10));
        Assert.Equal(CursorShape.Hand, ui.Cursor);
    }

    // ---- focus ---------------------------------------------------------------------------------------------------

    private static Element Btn(string id, Action? onClick = null) =>
        Ui.Button(id, onClick ?? (() => { })).Named(id).Size(60, 30);

    [Fact]
    public void TabFollowsTabIndexThenDocumentOrderAndWraps()
    {
        var ui = Screen();
        var a = Btn("a");
        var b = Btn("b").Tab(2);
        var c = Btn("c");
        var d = Btn("d").Tab(1);
        ui.Root.With(a, b, c, d);
        var order = new List<string>();
        for (var i = 0; i < 5; i++)
        {
            ui.Dispatch(UiEvent.KeyDown("Tab"));
            order.Add(ui.Focused!.Id!);
        }

        Assert.Equal(["d", "b", "a", "c", "d"], order);
        ui.Dispatch(UiEvent.KeyDown("Tab", KeyModifiers.Shift));
        Assert.Equal("c", ui.Focused!.Id);
    }

    [Fact]
    public void DisabledAndHiddenElementsAreSkipped()
    {
        var ui = Screen();
        var a = Btn("a");
        var b = Btn("b").Enabled(false);
        var c = Btn("c").Style(s => s.Display(DisplayMode.None));
        var d = Btn("d");
        ui.Root.With(a, b, c, d);
        ui.MoveFocus(NavDirection.Next);
        Assert.Equal("a", ui.Focused!.Id);
        ui.MoveFocus(NavDirection.Next);
        Assert.Equal("d", ui.Focused!.Id);
        Assert.False(ui.Focus(b));
        Assert.False(ui.Focus(c));
    }

    private static (UiTree Ui, Element[,] Grid) GridScreen()
    {
        var ui = Screen();
        var grid = new Element[3, 3];
        var rows = new List<Element>();
        for (var r = 0; r < 3; r++)
        {
            var row = Ui.Row().Gap(10);
            for (var c = 0; c < 3; c++)
            {
                grid[r, c] = Btn($"b{r}{c}");
                row.Add(grid[r, c]);
            }

            rows.Add(row);
        }

        ui.Root.Add(Ui.Column(rows.ToArray()).Gap(10).Padding(20).AlignSelf(Align.Start));
        return (ui, grid);
    }

    [Fact]
    public void DirectionalNavigationMovesOneCellAtATime()
    {
        var (ui, g) = GridScreen();
        ui.Focus(g[1, 1]);
        ui.Dispatch(UiEvent.Navigate(NavDirection.Right));
        Assert.Same(g[1, 2], ui.Focused);
        ui.Dispatch(UiEvent.Navigate(NavDirection.Up));
        Assert.Same(g[0, 2], ui.Focused);
        ui.Dispatch(UiEvent.Navigate(NavDirection.Left));
        ui.Dispatch(UiEvent.Navigate(NavDirection.Left));
        Assert.Same(g[0, 0], ui.Focused);
        // At the edge nothing happens and nothing is consumed.
        Assert.False(ui.Dispatch(UiEvent.Navigate(NavDirection.Left)));
        Assert.False(ui.Dispatch(UiEvent.Navigate(NavDirection.Up)));
        ui.Dispatch(UiEvent.Navigate(NavDirection.Down));
        ui.Dispatch(UiEvent.Navigate(NavDirection.Down));
        Assert.Same(g[2, 0], ui.Focused);
        ui.Dispatch(UiEvent.KeyDown("ArrowRight"));
        Assert.Same(g[2, 1], ui.Focused);
    }

    [Fact]
    public void DirectionalNavigationPrefersTheAlignedNeighbourOverANearerDiagonal()
    {
        var ui = Screen();
        var from = Btn("from").Style(s => s.Anchor(AnchorPoint.TopLeft, 100, 100));
        var aligned = Btn("aligned").Style(s => s.Anchor(AnchorPoint.TopLeft, 220, 100));
        var diagonal = Btn("diagonal").Style(s => s.Anchor(AnchorPoint.TopLeft, 170, 160));
        ui.Root.With(from, aligned, diagonal);
        ui.Focus(from);
        ui.Dispatch(UiEvent.Navigate(NavDirection.Right));
        Assert.Equal("aligned", ui.Focused!.Id);
    }

    [Fact]
    public void FocusStaysInsideATrap()
    {
        var ui = Screen();
        var a = Btn("a");
        var b = Btn("b");
        var x = Btn("x");
        var y = Btn("y");
        var dialog = Ui.Column(x, y).TrapFocus().Style(s => s.Anchor(AnchorPoint.Center));
        ui.Root.With(a, b, dialog);
        ui.Focus(a);
        ui.Dispatch(UiEvent.Navigate(NavDirection.Next));
        Assert.Equal("x", ui.Focused!.Id);
        ui.Dispatch(UiEvent.Navigate(NavDirection.Next));
        ui.Dispatch(UiEvent.Navigate(NavDirection.Next));
        Assert.Equal("x", ui.Focused!.Id);
        dialog.IsVisible = false;
        ui.Dispatch(UiEvent.Navigate(NavDirection.Next));
        Assert.True(ui.Focused!.Id is "a" or "b");
    }

    [Fact]
    public void FocusingARowInAScrolledListScrollsItIntoView()
    {
        var ui = Screen();
        var rows = Enumerable.Range(0, 20).Select(i => Btn("r" + i).Height(30).Shrink(0).Width(Length.Percent(100))).ToArray();
        var list = Ui.Column(rows).Size(100, 100).Overflow(OverflowMode.Scroll).AlignSelf(Align.Start);
        ui.Root.Add(list);
        ui.Focus(rows[0]);
        for (var i = 0; i < 7; i++)
        {
            ui.Dispatch(UiEvent.Navigate(NavDirection.Down));
        }

        Assert.Same(rows[7], ui.Focused);
        var r = rows[7].AbsoluteBounds;
        Assert.True(r.Y >= 0 && r.Bottom <= 100, $"focused row at {r}");
        Assert.True(list.ScrollY > 0);
        ui.Focus(rows[0]);
        Assert.Equal(0, list.ScrollY);
    }

    [Fact]
    public void ConfirmAndBackButtonsDriveFocusedElements()
    {
        var ui = Screen();
        var log = new List<string>();
        var ok = Btn("ok", () => log.Add("ok"));
        var panel = Ui.Column(ok).OnCancel(() => log.Add("cancel"));
        ui.Root.Add(panel);
        Assert.False(ui.Dispatch(UiEvent.Activate()));
        ui.Focus(ok);
        Assert.True(ui.Dispatch(UiEvent.Activate()));
        ui.Dispatch(UiEvent.KeyDown("Enter"));
        ui.Dispatch(UiEvent.KeyDown(" "));
        ui.Dispatch(UiEvent.Cancel());
        ui.Dispatch(UiEvent.KeyDown("Escape"));
        Assert.Equal(["ok", "ok", "ok", "cancel", "cancel"], log);
    }

    [Fact]
    public void KeyAndTextEventsReachTheFocusedElementFirst()
    {
        var ui = Screen();
        var typed = new List<string>();
        var field = Btn("field").On(UiEventKind.TextInput, (_, e) =>
        {
            typed.Add(e.Text!);
            return true;
        }).On(UiEventKind.KeyDown, (_, e) => e.Key == "Tab");
        var other = Btn("other");
        ui.Root.With(field, other);
        ui.Focus(field);
        ui.Dispatch(UiEvent.TextInput("hi"));
        // The field handles Tab itself, so focus does not move.
        ui.Dispatch(UiEvent.KeyDown("Tab"));
        Assert.Equal(["hi"], typed);
        Assert.Same(field, ui.Focused);
    }

    [Fact]
    public void PointerPressFocusesAndClickingEmptySpaceBlurs()
    {
        var ui = Screen();
        var btn = Btn("btn").Style(s => s.Anchor(AnchorPoint.TopLeft, 10, 10));
        var focusEvents = new List<string>();
        btn.OnFocus(() => focusEvents.Add("focus")).OnBlur(() => focusEvents.Add("blur"));
        ui.Root.Add(btn);
        ui.Dispatch(UiEvent.PointerDown(20, 20));
        ui.Dispatch(UiEvent.PointerUp(20, 20));
        Assert.Same(btn, ui.Focused);
        ui.Dispatch(UiEvent.PointerDown(300, 250));
        ui.Dispatch(UiEvent.PointerUp(300, 250));
        Assert.Null(ui.Focused);
        Assert.Equal(["focus", "blur"], focusEvents);
    }
}
