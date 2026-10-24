namespace OneGame.UI.Kit.Tests;

public class ComponentTests
{
    [Fact]
    public void ButtonClickEnterSpaceAndConfirmAllCallBackAndDisabledDoesNot()
    {
        var s = new Screen();
        var count = 0;
        var b = UiKit.Button("Buy", () => count++, "buy");
        s.Root.Add(b);
        s.Settle();
        s.Click(b);
        Assert.Equal(1, count);
        Assert.Same(b, s.Ui.Focused);
        s.Key("Enter");
        s.Key(" ");
        s.Activate();
        Assert.Equal(4, count);
        b.IsEnabled = false;
        s.Click(b);
        Assert.Equal(4, count);
        var json = s.Ui.DumpJson(b);
        Assert.Contains("\"role\": \"button\"", json, StringComparison.Ordinal);
        Assert.Contains("\"id\": \"buy\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void ToggleFollowsStateAndReportsChanges()
    {
        var s = new Screen();
        var on = false;
        var t = UiKit.Toggle("Auto-sell", () => on, v => on = v, "auto");
        s.Root.Add(t);
        s.Settle();
        Assert.Equal("false", t.Value);
        s.Click(t);
        Assert.True(on);
        Assert.Equal("true", t.Value);
        Assert.True(t.HasClass("checked"));
        s.Key(" ");
        Assert.False(on);
        Assert.False(t.HasClass("checked"));
    }

    [Fact]
    public void RadioGroupRovesFocusAndSelectsWithArrows()
    {
        var s = new Screen();
        var pick = 1;
        var g = UiKit.RadioGroup([new Choice<int>(0, "Low"), new Choice<int>(1, "Mid"), new Choice<int>(2, "High")], () => pick, v => pick = v, "quality");
        s.Root.Add(Ui.Column(UiKit.Button("before", () => { }), g, UiKit.Button("after", () => { })));
        s.Settle();
        Assert.Equal("Mid", g.Value);
        // Tab goes into the group at the selected option only.
        s.Focus(s.Get("quality/1"));
        s.Key("ArrowDown");
        Assert.Equal(2, pick);
        Assert.Same(s.Get("quality/2"), s.Ui.Focused);
        s.Key("ArrowUp");
        s.Key("ArrowUp");
        Assert.Equal(0, pick);
        s.Key("ArrowUp"); // at the first option the key is not used, so focus leaves toward the button above
        Assert.Equal(0, pick);
        Assert.NotSame(s.Get("quality/0"), s.Ui.Focused);
        Assert.False(s.Get("quality/2").IsFocusable);
        s.Click(s.Get("quality/2"));
        Assert.Equal(2, pick);
        Assert.Same(s.Get("quality/2"), s.Ui.Focused);
    }

    [Fact]
    public void SliderSnapsStepsWithKeysDpadAndPointerDrag()
    {
        var s = new Screen();
        var v = 50;
        var sl = UiKit.Slider(() => v, x => v = x, 0, 100, 5, "qty");
        s.Root.Add(sl.Width(400));
        s.Settle();
        Assert.Equal("50", sl.Value);
        s.Focus(sl);
        s.Key("ArrowRight");
        Assert.Equal(55, v);
        s.Nav(NavDirection.Left);
        s.Nav(NavDirection.Left);
        Assert.Equal(45, v);
        s.Key("PageUp");
        Assert.Equal(95, v);
        s.Key("End");
        s.Key("ArrowRight");
        Assert.Equal(100, v);
        s.Key("Home");
        Assert.Equal(0, v);
        var track = s.Get("qty/track").AbsoluteBounds;
        s.Host.Dispatch(UiEvent.PointerMove(track.X + 5, track.Y + 3));
        s.Host.Dispatch(UiEvent.PointerDown(track.X + (track.Width / 2), track.Y + 3));
        Assert.Equal(50, v);
        // The drag continues while the pointer is far outside the slider.
        s.Host.Dispatch(UiEvent.PointerMove(track.X + (track.Width * 0.77f), track.Y + 300));
        Assert.Equal(75, v);
        s.Host.Dispatch(UiEvent.PointerUp(track.X + (track.Width * 0.77f), track.Y + 300));
        s.Host.Dispatch(UiEvent.PointerMove(track.X, 0));
        Assert.Equal(75, v);
        s.Settle();
        Assert.Equal("75", s.Get("qty/value").Text);
        Assert.Equal("75", sl.Value);
        // Up and Down are not used by a slider.
        s.Key("ArrowDown");
        Assert.Equal(75, v);
    }

    [Fact]
    public void SliderRejectsABadRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => UiKit.Slider(() => 0f, _ => { }, 5f, 5f));
        Assert.Throws<ArgumentOutOfRangeException>(() => UiKit.Slider(() => 0f, _ => { }, 0f, 5f, 0f));
    }

    [Fact]
    public void StepperStepsClampsAndDisablesItsButtonsAtTheEnds()
    {
        var s = new Screen();
        var n = 8;
        var st = UiKit.NumberStepper(() => n, x => n = x, 1, 10, 1, "n");
        s.Root.Add(st);
        s.Settle();
        s.Click(s.Get("n/plus"));
        s.Click(s.Get("n/plus"));
        Assert.Equal(10, n);
        Assert.False(s.Get("n/plus").IsEnabled);
        s.Click(s.Get("n/minus"));
        Assert.Equal(9, n);
        Assert.Equal("9", s.Get("n/value").Text);
        s.Focus(st);
        s.Key("ArrowLeft");
        Assert.Equal(8, n);
        s.Key("PageDown");
        Assert.Equal(1, n);
        s.Key("End");
        Assert.Equal(10, n);
    }

    [Fact]
    public void ProgressShowsTheFraction()
    {
        var s = new Screen();
        var done = 30f;
        var p = UiKit.Progress(() => done, 120, "load");
        s.Root.Add(p.Width(200));
        s.Settle();
        Assert.Equal("0.25", p.Value);
        Assert.Equal(50f, s.Get("load/fill").Bounds.Width, 0.01f);
        done = 500;
        s.Settle();
        Assert.Equal("1", p.Value);
        Assert.Equal(200f, s.Get("load/fill").Bounds.Width, 0.01f);
    }

    [Fact]
    public void TabsSelectByClickArrowAndCtrlPageAndHideOtherPanels()
    {
        var s = new Screen();
        var sel = 0;
        var tabs = UiKit.Tabs(
            [new TabPage("Market", Ui.Label("market content").Named("m")), new TabPage("Cargo", Ui.Label("cargo content").Named("c")), new TabPage("Ships", Ui.Label("ships content").Named("sh"))],
            () => sel,
            i => sel = i,
            "tabs");
        s.Root.Add(tabs);
        s.Settle();
        Assert.Equal("Market", tabs.Value);
        Assert.True(s.Get("m").Bounds.Width > 0);
        Assert.False(s.Get("tabs/panel/1").IsVisible);
        s.Click(s.Get("tabs/tab/1"));
        Assert.Equal(1, sel);
        Assert.True(s.Get("tabs/panel/1").IsVisible);
        Assert.False(s.Get("tabs/panel/0").IsVisible);
        Assert.Same(s.Get("tabs/tab/1"), s.Ui.Focused);
        s.Key("ArrowRight");
        Assert.Equal(2, sel);
        Assert.Same(s.Get("tabs/tab/2"), s.Ui.Focused);
        s.Key("PageUp", KeyModifiers.Ctrl);
        Assert.Equal(1, sel);
        s.Nav(NavDirection.Left);
        Assert.Equal(0, sel);
        Assert.Contains("\"role\": \"tab\"", s.Ui.DumpJson(s.Get("tabs/tab/0")), StringComparison.Ordinal);
    }

    [Fact]
    public void DropdownOpensTrapsFocusChoosesAndReturnsFocus()
    {
        var s = new Screen();
        var pick = "b";
        var dd = UiKit.Dropdown([new Choice<string>("a", "Alpha"), new Choice<string>("b", "Beta"), new Choice<string>("c", "Gamma")], () => pick, v => pick = v, "dd");
        s.Root.Add(Ui.Column(dd, UiKit.Button("other", () => { }, "other")).Padding(20));
        s.Settle();
        Assert.Equal("Beta", dd.Value);
        Assert.False(s.Get("dd/popup").IsVisible);
        s.Focus(s.Get("dd/head"));
        s.Key("Enter");
        Assert.True(s.Get("dd/popup").IsVisible);
        Assert.True(dd.HasClass("open"));
        Assert.Same(s.Get("dd/option/1"), s.Ui.Focused);
        s.Key("ArrowDown");
        Assert.Same(s.Get("dd/option/2"), s.Ui.Focused);
        s.Key("Tab");
        s.Key("Tab");
        Assert.True(s.Get("dd").Contains(s.Ui.Focused), "focus stays in the open list");
        s.Key("Enter");
        Assert.True(pick is "b" or "c");
        Assert.False(s.Get("dd/popup").IsVisible);
        Assert.Same(s.Get("dd/head"), s.Ui.Focused);
        // Escape closes without choosing.
        var before = pick;
        s.Key("Enter");
        s.Cancel();
        Assert.Equal(before, pick);
        Assert.False(s.Get("dd/popup").IsVisible);
        Assert.Same(s.Get("dd/head"), s.Ui.Focused);
        // Pointer: open, click an option.
        s.Click(s.Get("dd/head"));
        s.Click(s.Get("dd/option/0"));
        Assert.Equal("a", pick);
        Assert.Equal("Alpha", dd.Value);
    }

    [Fact]
    public void ScrollViewWheelThumbDragAndTrackClickScrollTheContent()
    {
        var s = new Screen();
        var content = Ui.Column(Enumerable.Range(0, 40).Select(i => Ui.Label("line " + i).Height(20)).ToArray());
        var sv = UiKit.ScrollView(content, id: "sv");
        s.Root.Add(sv.Height(200).Width(300));
        s.Settle();
        var scroller = s.Get("sv/scroller");
        Assert.True(scroller.ScrollSize.Y >= 800);
        Assert.True(s.Get("sv/vbar").IsVisible);
        s.Host.Dispatch(UiEvent.Wheel(50, 50, 0, 60));
        s.Settle();
        Assert.Equal(60, scroller.ScrollY, 0.01f);
        var thumb = s.Get("sv/vthumb");
        Assert.True(thumb.Bounds.Y > 0);
        var tb = thumb.AbsoluteBounds;
        s.Host.Dispatch(UiEvent.PointerMove(tb.X + 2, tb.Y + 2));
        s.Host.Dispatch(UiEvent.PointerDown(tb.X + 2, tb.Y + 2));
        s.Host.Dispatch(UiEvent.PointerMove(tb.X + 2, 5000));
        s.Host.Dispatch(UiEvent.PointerUp(tb.X + 2, 5000));
        s.Settle();
        Assert.Equal(scroller.MaxScroll.Y, scroller.ScrollY, 0.01f);
        var bar = s.Get("sv/vbar").AbsoluteBounds;
        s.Host.Dispatch(UiEvent.PointerMove(bar.X + 2, bar.Y + 2));
        s.Host.Dispatch(UiEvent.PointerDown(bar.X + 2, bar.Y + 2));
        s.Host.Dispatch(UiEvent.PointerUp(bar.X + 2, bar.Y + 2));
        Assert.True(scroller.ScrollY < scroller.MaxScroll.Y);
    }

    [Fact]
    public void FocusGroupKeepsArrowsInsideUntilTheEnds()
    {
        var s = new Screen();
        var row = FocusHelpers.FocusGroup(Ui.Row(UiKit.Button("a", () => { }, "a"), UiKit.Button("b", () => { }, "b"), UiKit.Button("c", () => { }, "c")), horizontal: true, wrap: true);
        s.Root.Add(row);
        s.Settle();
        s.Focus(s.Get("a"));
        s.Key("ArrowRight");
        Assert.Same(s.Get("b"), s.Ui.Focused);
        s.Key("ArrowRight");
        s.Key("ArrowRight");
        Assert.Same(s.Get("a"), s.Ui.Focused);
        s.Key("ArrowLeft");
        Assert.Same(s.Get("c"), s.Ui.Focused);
    }

    [Fact]
    public void FocusDefaultPrefersTheMarkedElementAndDialogRestoresFocus()
    {
        var s = new Screen();
        var confirmed = 0;
        var cancelled = 0;
        var open = UiKit.Button("Sell all", () => { }, "sell");
        var d = UiKit.ConfirmDialog("Sell everything?", "This cannot be undone.", () => confirmed++, () => cancelled++, "Sell", "Keep", "confirm", danger: true);
        s.Root.With(Ui.Column(open, UiKit.Button("Other", () => { }, "other")), d);
        s.Settle();
        s.Focus(open);
        d.Open();
        s.Settle();
        Assert.True(d.IsOpen);
        // Danger dialogs start on Cancel so a stray Enter does not confirm.
        Assert.Same(s.Get("confirm/buttons/0"), s.Ui.Focused);
        // Tab and the d-pad stay inside the dialog, and the pointer cannot reach what is behind it.
        for (var i = 0; i < 5; i++)
        {
            s.Key("Tab");
            Assert.True(d.Contains(s.Ui.Focused));
        }

        s.Nav(NavDirection.Up);
        s.Nav(NavDirection.Left);
        Assert.True(d.Contains(s.Ui.Focused));
        var other = s.Get("other").AbsoluteBounds;
        s.Host.Dispatch(UiEvent.PointerMove(other.X + 2, other.Y + 2));
        Assert.False(s.Get("other").IsHovered);
        s.Cancel();
        Assert.False(d.IsOpen);
        Assert.Equal(1, cancelled);
        Assert.Same(open, s.Ui.Focused);
        d.Open();
        s.Focus(s.Get("confirm/buttons/1"));
        s.Key("Enter");
        Assert.Equal(1, confirmed);
        Assert.False(d.IsOpen);
        Assert.Same(open, s.Ui.Focused);
    }

    [Fact]
    public void DialogCanFollowAFlagAndMovesFocusAfterTheUpdate()
    {
        var s = new Screen();
        var flag = false;
        var d = UiKit.Dialog("Info", Ui.Label("hello"), "info").BindOpen(() => flag);
        d.AddButton("Close", () => flag = false, isDefault: true);
        var b = UiKit.Button("Go", () => flag = true, "go");
        s.Root.With(b, d);
        s.Settle();
        s.Click(b);
        Assert.True(d.IsOpen);
        Assert.Same(s.Get("info/buttons/0"), s.Ui.Focused);
        s.Key("Enter");
        Assert.False(d.IsOpen);
        Assert.Same(b, s.Ui.Focused);
    }

    [Fact]
    public void TooltipWaitsForTheDelayFlipsAndStaysOnScreen()
    {
        var s = new Screen(400, 300);
        var b = s.Host.Tooltip(UiKit.Button("Buy", () => { }, "buy"), "Buy one unit at the market price");
        s.Root.Add(b.Anchor(AnchorPoint.BottomRight, 4, 4));
        s.Settle();
        var layer = s.Host.TooltipLayer;
        Assert.False(layer.IsVisible);
        var r = b.AbsoluteBounds;
        s.Host.Dispatch(UiEvent.PointerMove(r.X + 3, r.Y + 3));
        s.Host.Advance(0.2f);
        s.Ui.Frame();
        Assert.False(layer.IsVisible);
        s.Host.Advance(0.4f);
        s.Settle();
        Assert.True(layer.IsVisible);
        var box = layer.AbsoluteBounds;
        Assert.True(box.X >= 0 && box.Right <= 400.01f && box.Y >= 0 && box.Bottom <= 300.01f, box.ToString());
        Assert.True(box.Bottom <= r.Y, "flipped above the button because there is no room below");
        s.Host.Dispatch(UiEvent.PointerLeftWindow());
        s.Settle();
        Assert.False(layer.IsVisible);
        // Keyboard focus shows it too.
        s.Focus(b);
        s.Host.Advance(0.6f);
        s.Settle();
        Assert.True(layer.IsVisible);
        s.Ui.Focus(null);
        s.Settle();
        Assert.False(layer.IsVisible);
    }

    [Fact]
    public void InputGlyphFollowsTheLastUsedDevice()
    {
        var s = new Screen();
        s.Host.Glyphs.Bind("confirm", keyboard: "Enter", mouse: "left", gamepad: "a").Bind("back", keyboard: "Escape", gamepad: "b").Bind("up", keyboard: "ArrowUp");
        var g = UiKit.InputGlyph(s.Host.Glyphs, "confirm", "g");
        var back = UiKit.InputGlyph(s.Host.Glyphs, "back", "back");
        var up = UiKit.InputGlyph(s.Host.Glyphs, "up", "up");
        var none = UiKit.InputGlyph(s.Host.Glyphs, "missing", "none");
        s.Root.With(g, back, up, none);
        s.Settle();
        Assert.Equal("keyboard:Enter", g.Value);
        Assert.Equal("Enter", g.ChildAt(0).Text);
        s.Host.Dispatch(UiEvent.Navigate(NavDirection.Down));
        s.Settle();
        Assert.Equal("gamepad:a", g.Value);
        Assert.Equal("gamepad:b", back.Value);
        Assert.Equal("keyboard:ArrowUp", up.Value);
        Assert.Contains("kit/glyph/pad/a", s.Ui.DisplayList.ToText(), StringComparison.Ordinal);
        s.Host.Dispatch(UiEvent.PointerMove(1, 1));
        s.Settle();
        Assert.Equal("mouse:left", g.Value);
        Assert.Equal("keyboard:Escape", back.Value);
        Assert.True(none.HasClass("unbound"));
        Assert.Null(none.Value);
    }

    [Fact]
    public void KitArtIsValidSvgThatRasterizes()
    {
        var atlas = new OneGame.Atlas.ArtAtlas();
        foreach (var a in KitArt.Assets)
        {
            atlas.AddSvg(a.Id, a.Svg, new OneGame.Atlas.NineSlice(a.Slice, a.Slice, a.Slice, a.Slice));
            Assert.Equal(OneGame.Atlas.AtlasStatus.Ok, atlas.TryGetScaled(a.Id, 2, out var region));
            var page = atlas.Pages[region.Page];
            var covered = false;
            for (var y = region.Y; y < region.Y + region.Height && !covered; y++)
            {
                for (var x = region.X; x < region.X + region.Width; x++)
                {
                    if (page.Pixels[(y * page.Stride) + (x * 4) + 3] != 0)
                    {
                        covered = true;
                        break;
                    }
                }
            }

            Assert.True(covered, a.Id + " rasterized to nothing");
        }

        Assert.True(KitArt.Assets.Count >= 30);
        Assert.Equal("kit/glyph/pad/a", KitArt.GlyphId(InputDevice.Gamepad, "a"));
    }
}
