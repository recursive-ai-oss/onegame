using OneGame.Tweens;

namespace OneGame.UI.Tests;

/// <summary>The style <c>transition</c> property: a changed value moves over time on the tree's tween clock (CSS Transitions Level 1).</summary>
public class TransitionTests
{
    private static UiTree Tree(string transition, out Element box, Action<StyleBuilder>? on = null)
    {
        var ui = new UiTree();
        ui.AddStyleSheet(new StyleSheet()
            .Rule(".b", s => s.Size(100, 40).Background(Color.Rgb(0x000000)).Opacity(1).Transition(transition))
            .Rule(".b.on", s =>
            {
                s.Background(Color.Rgb(0xffffff)).Opacity(0.5f).Width(300);
                on?.Invoke(s);
            }));
        box = new Element("Box").Named("b").Class("b");
        ui.Root.Add(box);
        ui.Frame();
        return ui;
    }

    private static void Step(UiTree ui, double seconds)
    {
        ui.Tweens.Advance(seconds);
        ui.Frame();
    }

    private static float Opacity(Element e) => float.Parse(e.Computed.Get("opacity")!, System.Globalization.CultureInfo.InvariantCulture);

    [Fact]
    public void TheFirstStyleOfAnElementNeverAnimates()
    {
        var ui = new UiTree();
        ui.AddStyleSheet(new StyleSheet().Rule(".b", s => s.Opacity(0.25f).Transition("opacity 1s")));
        var e = new Element("Box").Class("b");
        ui.Root.Add(e);
        ui.Frame();
        Assert.Equal(0.25f, Opacity(e));
        Assert.Equal(0, ui.Tweens.ActiveCount);
    }

    [Fact]
    public void AChangedValueMovesOverTheDurationAndLandsExactly()
    {
        var ui = Tree("background-color 200ms linear, opacity 200ms linear", out var box);
        box.SetClass("on", true);
        ui.Frame();
        Assert.Equal(1f, Opacity(box));
        Assert.Equal(2, ui.Tweens.ActiveCount);
        Step(ui, 0.1);
        Assert.Equal(0.75f, Opacity(box), 1e-5f);
        // Premultiplied blend of two opaque greys: half way is half grey.
        Assert.Equal("#808080", box.Computed.Get("background-color"));
        Step(ui, 0.1);
        Assert.Equal(0.5f, Opacity(box));
        Assert.Equal("#ffffff", box.Computed.Get("background-color"));
        Assert.Equal(0, ui.Tweens.ActiveCount);
        // A property without a transition changed at once.
        Assert.Equal(300f, box.Bounds.Width);
    }

    [Fact]
    public void LayoutPropertiesReflowEveryFrameOfTheTransition()
    {
        var ui = Tree("width 400ms linear", out var box);
        Assert.Equal(100f, box.Bounds.Width);
        box.SetClass("on", true);
        ui.Frame();
        Assert.Equal(100f, box.Bounds.Width);
        Step(ui, 0.1);
        Assert.Equal(150f, box.Bounds.Width, 1e-3f);
        Step(ui, 0.1);
        Assert.Equal(200f, box.Bounds.Width, 1e-3f);
        Step(ui, 0.2);
        Assert.Equal(300f, box.Bounds.Width);
    }

    [Fact]
    public void ReversingMidwayStartsFromTheValueReached()
    {
        var ui = Tree("opacity 200ms linear", out var box);
        box.SetClass("on", true);
        ui.Frame();
        Step(ui, 0.1);
        Assert.Equal(0.75f, Opacity(box), 1e-5f);
        box.SetClass("on", false);
        ui.Frame();
        Assert.Equal(0.75f, Opacity(box), 1e-5f);
        Step(ui, 0.1);
        Assert.Equal(0.875f, Opacity(box), 1e-5f);
        Step(ui, 0.1);
        Assert.Equal(1f, Opacity(box));
        Assert.Equal(0, ui.Tweens.ActiveCount);
    }

    [Fact]
    public void DelayAndTheEasingOfTheListItemApply()
    {
        var ui = Tree("opacity 200ms ease-in 100ms", out var box);
        box.SetClass("on", true);
        ui.Frame();
        Step(ui, 0.1);
        Assert.Equal(1f, Opacity(box));
        Step(ui, 0.1); // half way through the duration, with ease-in
        Assert.Equal(1 - (0.5f * Easing.EaseIn.Evaluate(0.5f)), Opacity(box), 1e-4f);
    }

    [Fact]
    public void AllCoversEveryBlendableProperty()
    {
        var ui = Tree("all 100ms linear", out var box);
        box.SetClass("on", true);
        ui.Frame();
        Step(ui, 0.05);
        Assert.Equal(0.75f, Opacity(box), 1e-5f);
        Assert.Equal(200f, box.Bounds.Width, 1e-3f);
        Assert.Equal("#808080", box.Computed.Get("background-color"));
    }

    [Fact]
    public void TheLastMatchingListItemWins()
    {
        var ui = Tree("all 1s linear, opacity 100ms linear", out var box);
        box.SetClass("on", true);
        ui.Frame();
        Step(ui, 0.1);
        Assert.Equal(0.5f, Opacity(box));
        Assert.Equal(120f, box.Bounds.Width, 1e-2f);
    }

    [Fact]
    public void ValuesThatCannotBlendSwitchAtOnce()
    {
        var ui = Tree("all 1s", out var box, s => s.Display(DisplayMode.None).Width(Length.Percent(50)));
        box.SetClass("on", true);
        ui.Frame();
        Assert.Equal("none", box.Computed.Get("display"));
        // px to percent has no value in between, so width switches at once while opacity animates.
        Assert.Equal("50%", box.Computed.Get("width"));
        Assert.Equal(1f, Opacity(box));
    }

    [Fact]
    public void NoneZeroDurationAndGarbageDoNotAnimate()
    {
        foreach (var text in new[] { "none", "opacity 0s", "opacity", "", "banana 1s", "opacity 1s bogus" })
        {
            var ui = Tree(text, out var box);
            box.SetClass("on", true);
            ui.Frame();
            Assert.Equal(0.5f, Opacity(box));
            Assert.Equal(0, ui.Tweens.ActiveCount);
        }
    }

    [Fact]
    public void RemovingTheTransitionStopsARunningOneAtTheTarget()
    {
        var ui = Tree("opacity 1s linear", out var box);
        box.SetClass("on", true);
        ui.Frame();
        Step(ui, 0.1);
        Assert.InRange(Opacity(box), 0.9f, 0.96f);
        box.Style(s => s.Transition("none"));
        ui.Frame();
        Assert.Equal(0.5f, Opacity(box));
        Assert.Equal(0, ui.Tweens.ActiveCount);
    }

    [Fact]
    public void ShorthandsAndTimingFunctionsInTheListParse()
    {
        var ui = new UiTree();
        ui.AddStyleSheet(new StyleSheet()
            .Rule(".b", s => s.Padding(0).Margin(0).Transition("padding 1s steps(2, jump-end), margin-left 1s cubic-bezier(0.2, 0, 0, 1)"))
            .Rule(".on", s => s.Padding(10).MarginLeft(20)));
        var e = new Element("Box").Class("b");
        ui.Root.Add(e);
        ui.Frame();
        e.SetClass("on", true);
        ui.Frame();
        Assert.Equal(5, ui.Tweens.ActiveCount);
        Step(ui, 0.6);
        Assert.Equal("5px", e.Computed.Get("padding-top"));
        Assert.Equal(Easing.CubicBezier(0.2f, 0, 0, 1).Evaluate(0.6f) * 20, float.Parse(e.Computed.Get("margin-left")![..^2], System.Globalization.CultureInfo.InvariantCulture), 1e-2f);
    }

    [Fact]
    public void TheListingNamesTheElementAndPropertyOfEachTransition()
    {
        var ui = Tree("opacity 200ms", out var box);
        box.SetClass("on", true);
        ui.Frame();
        var list = new List<TweenInfo>();
        ui.Tweens.Snapshot(list);
        Assert.Equal("Box#b opacity", Assert.Single(list).Label);
    }

    [Fact]
    public void TheSameStepsGiveTheSameStylesAndDisplayList()
    {
        string Run()
        {
            var ui = Tree("all 300ms ease-in-out", out var box);
            var log = new System.Text.StringBuilder();
            box.SetClass("on", true);
            foreach (var dt in new[] { 0.016, 0.05, 0.1, 0.034, 0.2 })
            {
                Step(ui, dt);
                log.Append(box.Computed.Get("opacity")).Append(' ').Append(box.Computed.Get("background-color")).Append(' ').Append(box.Bounds.Width).Append(' ').Append(ui.DisplayList.Version).Append('\n');
            }

            return log.ToString();
        }

        Assert.Equal(Run(), Run());
    }

    [Fact]
    public void SteadyFramesOfRunningTransitionsAllocateNothingInTheTweenClock()
    {
        var ui = new UiTree();
        ui.AddStyleSheet(new StyleSheet()
            .Rule(".b", s => s.Size(40, 20).Opacity(1).Background(Color.Black).Transition("opacity 100s linear, background-color 100s linear"))
            .Rule(".on", s => s.Opacity(0.2f).Background(Color.White)));
        var boxes = new List<Element>();
        for (var i = 0; i < 60; i++)
        {
            var e = new Element("Box").Class("b");
            boxes.Add(e);
            ui.Root.Add(e);
        }

        ui.Frame();
        foreach (var e in boxes)
        {
            e.SetClass("on", true);
        }

        ui.Frame();
        Assert.Equal(120, ui.Tweens.ActiveCount);
        for (var i = 0; i < 30; i++)
        {
            ui.Tweens.Advance(1 / 60.0);
            ui.Frame();
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 300; i++)
        {
            ui.Tweens.Advance(1 / 60.0);
        }

        var advanceOnly = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.True(advanceOnly == 0, $"Advance allocated {advanceOnly} B in 300 frames");
        Assert.Equal(120, ui.Tweens.ActiveCount);
    }
}
