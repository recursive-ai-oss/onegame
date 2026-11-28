namespace OneGame.UI.Kit.Tests;

public sealed class StateInvalidationTests
{
    private static string Bg(Element e) => e.Computed.Get("background-color")!;

    [Fact]
    public void KitControlPartsFollowHoverAndPressWhileAPressStaysCheapInABigTree()
    {
        var s = new Screen();
        var on = false;
        var pick = 0;
        var v = 10f;
        s.Root.Column().With(
            UiKit.Button("Go", () => { }, "go"),
            UiKit.Toggle("Sound", () => on, x => on = x, "snd"),
            UiKit.RadioGroup([new Choice<int>(0, "A"), new Choice<int>(1, "B")], () => pick, x => pick = x, "rg"),
            UiKit.Slider(() => v, x => v = x, 0, 100, 1, "sl"));
        var hidden = new Element().Column();
        for (var i = 0; i < 300; i++)
        {
            hidden.Add(UiKit.Toggle("t" + i, () => false, _ => { }));
        }

        hidden.IsVisible = false;
        s.Root.Add(hidden);
        s.Settle();
        var total = 1 + s.Ui.Query("*").Count;

        // Parts react to the state of their control.
        var box = s.Ui.Query(".kit-toggle-box").First(e => e.IsEffectivelyVisible);
        var rest = Bg(box);
        var toggle = s.Get("snd");
        var r = toggle.AbsoluteBounds;
        s.Host.Dispatch(UiEvent.PointerMove(r.X + 2, r.Y + (r.Height / 2)));
        s.Settle();
        var hover = Bg(box);
        Assert.NotEqual(rest, hover);
        s.Host.Dispatch(UiEvent.PointerDown(r.X + 2, r.Y + (r.Height / 2)));
        s.Settle();
        var pressed = Bg(box);
        Assert.NotEqual(hover, pressed);
        s.Host.Dispatch(UiEvent.PointerUp(r.X + 2, r.Y + (r.Height / 2)));
        s.Host.Dispatch(UiEvent.PointerMove(1270, 710));
        s.Settle();

        var thumb = s.Ui.Query(".kit-slider-thumb").First(e => e.IsEffectivelyVisible);
        var thumbRest = Bg(thumb) + thumb.Computed.Get("border-color");
        var sr = s.Get("sl").AbsoluteBounds;
        s.Host.Dispatch(UiEvent.PointerMove(sr.X + (sr.Width / 2), sr.Y + (sr.Height / 2)));
        s.Settle();
        Assert.NotEqual(thumbRest, Bg(thumb) + thumb.Computed.Get("border-color"));

        var ring = s.Ui.Query(".kit-radio-ring").First(e => e.IsEffectivelyVisible);
        var ringRest = Bg(ring);
        var rr = ring.Parent!.AbsoluteBounds;
        s.Host.Dispatch(UiEvent.PointerMove(rr.X + (rr.Width / 2), rr.Y + (rr.Height / 2)));
        s.Settle();
        Assert.NotEqual(ringRest, Bg(ring));

        // A press on the button restyles a handful of elements, not the tree.
        var go = s.Get("go").AbsoluteBounds;
        var gx = go.X + (go.Width / 2);
        var gy = go.Y + (go.Height / 2);
        s.Host.Dispatch(UiEvent.PointerMove(gx, gy));
        s.Settle();
        var before = s.Ui.StyleRecomputes;
        s.Host.Dispatch(UiEvent.PointerDown(gx, gy));
        s.Ui.Frame();
        var press = s.Ui.StyleRecomputes - before;
        before = s.Ui.StyleRecomputes;
        s.Host.Dispatch(UiEvent.PointerUp(gx, gy));
        s.Ui.Frame();
        var release = s.Ui.StyleRecomputes - before;
        Console.WriteLine($"kit elements={total} press restyled={press} release restyled={release}");
        Assert.InRange(press, 1, 40);
        Assert.InRange(release, 1, 40);
    }
}
