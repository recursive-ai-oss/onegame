namespace OneGame.UI.Tests;

/// <summary>A hover, press or focus change restyles what its rules can reach, and no more than that.</summary>
public class StateInvalidationTests
{
    private static readonly Color Red = Color.Hex("#ff0000");
    private static readonly Color Green = Color.Hex("#00ff00");
    private static readonly Color Blue = Color.Hex("#0000ff");

    private static string Bg(Element e) => e.Computed.Get("background-color")!;

    private static (float X, float Y) Mid(Element e)
    {
        var r = e.AbsoluteBounds;
        return (r.X + (r.Width / 2), r.Y + (r.Height / 2));
    }

    private static void MoveTo(UiTree ui, Element e)
    {
        var (x, y) = Mid(e);
        ui.Dispatch(UiEvent.PointerMove(x, y));
        ui.Frame();
    }

    // root > panel(.p) > [ button(.btn, hit target), sibling(.c) ], plus a bystander outside the panel.
    private static (UiTree Ui, Element Panel, Element Btn, Element Sib, Element Outside) Build(StyleSheet sheet)
    {
        var ui = new UiTree();
        ui.SetViewport(400, 300, 1);
        ui.AddStyleSheet(sheet);
        var btn = new Element().Class("btn").Focusable().Size(100, 40);
        var sib = new Element().Class("c").Size(100, 40);
        var panel = new Element().Class("p").Column().Size(200, 120).With(btn, sib);
        var outside = new Element().Class("c").Size(100, 40);
        ui.Root.Column().With(panel, outside);
        ui.Frame();
        return (ui, panel, btn, sib, outside);
    }

    [Fact]
    public void HoverOnANonHitAncestorRestylesItsDescendantsAndOnlyThose()
    {
        var (ui, _, btn, sib, outside) = Build(new StyleSheet()
            .Rule(".c", s => s.Background(Red))
            .Rule(".p:hover .c", s => s.Background(Green)));
        Assert.Equal("#ff0000", Bg(sib));
        MoveTo(ui, btn);
        Assert.Equal("#00ff00", Bg(sib));
        Assert.Equal("#ff0000", Bg(outside));
        ui.Dispatch(UiEvent.PointerMove(399, 299));
        ui.Frame();
        Assert.Equal("#ff0000", Bg(sib));
    }

    [Fact]
    public void ChildCombinatorOnHoverFollowsTheParent()
    {
        var (ui, _, btn, sib, outside) = Build(new StyleSheet()
            .Rule(".c", s => s.Background(Red))
            .Rule(".p:hover > .c", s => s.Background(Blue)));
        MoveTo(ui, btn);
        Assert.Equal("#0000ff", Bg(sib));
        Assert.Equal("#ff0000", Bg(outside));
    }

    [Fact]
    public void ActiveOnAnAncestorRestylesSiblingsOfThePressedElement()
    {
        var (ui, _, btn, sib, _) = Build(new StyleSheet()
            .Rule(".c", s => s.Background(Red))
            .Rule(".p:active .c", s => s.Background(Green)));
        var (x, y) = Mid(btn);
        ui.Dispatch(UiEvent.PointerMove(x, y));
        ui.Dispatch(UiEvent.PointerDown(x, y));
        ui.Frame();
        Assert.Equal("#00ff00", Bg(sib));
        ui.Dispatch(UiEvent.PointerUp(x, y));
        ui.Frame();
        Assert.Equal("#ff0000", Bg(sib));
    }

    [Fact]
    public void FocusOnTheLeftSideRestylesDescendantsOfTheFocusedElement()
    {
        var ui = new UiTree();
        ui.SetViewport(400, 300, 1);
        ui.AddStyleSheet(new StyleSheet()
            .Rule(".part", s => s.Background(Red))
            .Rule(".f:focus .part", s => s.Background(Green)));
        var part = new Element().Class("part").Size(20, 20);
        var f = new Element().Class("f").Focusable().Size(100, 40).With(new Element().Size(50, 20).With(part));
        var other = new Element().Class("part").Size(20, 20);
        ui.Root.Column().With(f, other);
        ui.Frame();
        Assert.True(ui.Focus(f));
        ui.Frame();
        Assert.Equal("#00ff00", Bg(part));
        Assert.Equal("#ff0000", Bg(other));
        ui.Focus(null);
        ui.Frame();
        Assert.Equal("#ff0000", Bg(part));
    }

    [Fact]
    public void APressInATreeWithManyHiddenElementsCostsTheHitPath()
    {
        // Combinator rules on state are present, as in the kit theme, but none names the pressed button or its ancestors.
        var sheet = new StyleSheet()
            .Rule(".btn", s => s.Background(Red))
            .Rule(".btn:hover", s => s.Background(Green))
            .Rule(".btn:active", s => s.Background(Blue))
            .Rule(".tg:hover .tg-box", s => s.Background(Green))
            .Rule(".tg:active .tg-box", s => s.Background(Blue))
            .Rule(".tg:focus .tg-box", s => s.Background(Blue));
        var ui = new UiTree();
        ui.SetViewport(400, 300, 1);
        ui.AddStyleSheet(sheet);
        var btn = new Element().Class("btn").Focusable().Size(100, 40);
        var hidden = new Element().Column();
        for (var i = 0; i < 400; i++)
        {
            hidden.Add(new Element().Class("tg").With(new Element().Class("tg-box"), new Element(), new Element(), new Element()));
        }

        hidden.IsVisible = false;
        ui.Root.Column().With(btn, hidden);
        ui.Frame();
        var total = 1 + ui.Query("*").Count;
        Assert.True(total > 2000, $"{total} elements");

        var (x, y) = Mid(btn);
        ui.Dispatch(UiEvent.PointerMove(x, y));
        ui.Frame();

        var before = ui.StyleRecomputes;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        ui.Dispatch(UiEvent.PointerDown(x, y));
        ui.Frame();
        var pressCost = ui.StyleRecomputes - before;
        before = ui.StyleRecomputes;
        ui.Dispatch(UiEvent.PointerUp(x, y));
        ui.Frame();
        var releaseCost = ui.StyleRecomputes - before;
        sw.Stop();

        // Diagnostic: before the change a press and a release each recomputed every element in the tree.
        Console.WriteLine($"elements={total} press restyled={pressCost} release restyled={releaseCost} both={sw.Elapsed.TotalMilliseconds:F3} ms");
        Assert.Equal("#00ff00", Bg(btn));
        Assert.InRange(pressCost, 1, 8);
        Assert.InRange(releaseCost, 1, 8);
    }
}
