namespace OneGame.UI.Tests;

public class CascadeTests
{
    private static readonly Color Red = Color.Hex("#ff0000");
    private static readonly Color Green = Color.Hex("#00ff00");
    private static readonly Color Blue = Color.Hex("#0000ff");

    private static UiTree Tree(params StyleSheet[] sheets)
    {
        var ui = new UiTree();
        foreach (var s in sheets)
        {
            ui.AddStyleSheet(s);
        }

        return ui;
    }

    private static string Bg(Element e) => e.Computed.Get("background-color")!;

    [Fact]
    public void SpecificityBeatsOrder()
    {
        var sheet = new StyleSheet()
            .Rule("#x", s => s.Background(Green))
            .Rule(".a", s => s.Background(Blue))
            .Rule("Box", s => s.Background(Red));
        var ui = Tree(sheet);
        var both = new Element("Box").Class("a").Named("x");
        var cls = new Element("Box").Class("a");
        var type = new Element("Box");
        ui.Root.With(both, cls, type);
        ui.Frame();
        Assert.Equal("#00ff00", Bg(both));
        Assert.Equal("#0000ff", Bg(cls));
        Assert.Equal("#ff0000", Bg(type));
    }

    [Fact]
    public void EqualSpecificityLaterRuleWinsAcrossSheets()
    {
        var first = new StyleSheet().Rule(".a", s => s.Background(Red));
        var second = new StyleSheet().Rule(".a", s => s.Background(Blue));
        var ui = Tree(first, second);
        var e = new Element().Class("a");
        ui.Root.Add(e);
        ui.Frame();
        Assert.Equal("#0000ff", Bg(e));
        ui.RemoveStyleSheet(second);
        ui.Frame();
        Assert.Equal("#ff0000", Bg(e));
    }

    [Fact]
    public void InlineStyleBeatsEverySheetRule()
    {
        var ui = Tree(new StyleSheet().Rule("#x", s => s.Background(Green)));
        var e = new Element().Named("x").Background(Red);
        ui.Root.Add(e);
        ui.Frame();
        Assert.Equal("#ff0000", Bg(e));
    }

    [Fact]
    public void ClassChangeRestylesOnNextFrame()
    {
        var ui = Tree(new StyleSheet().Rule(".on", s => s.Background(Green)));
        var e = new Element();
        ui.Root.Add(e);
        ui.Frame();
        Assert.Equal("#00000000", Bg(e));
        e.SetClass("on", true);
        ui.Frame();
        Assert.Equal("#00ff00", Bg(e));
        e.SetClass("on", false);
        ui.Frame();
        Assert.Equal("#00000000", Bg(e));
    }

    [Fact]
    public void TextPropertiesInheritAndBoxPropertiesDoNot()
    {
        var ui = Tree();
        var leaf = new Element();
        var mid = new Element().With(leaf);
        var top = new Element().TextColor(Red).FontSize(30).Background(Blue).Padding(5).With(mid);
        ui.Root.Add(top);
        ui.Frame();
        Assert.Equal("#ff0000", leaf.Computed.Get("color"));
        Assert.Equal("30px", leaf.Computed.Get("font-size"));
        Assert.Equal("#00000000", leaf.Computed.Get("background-color"));
        Assert.Equal("0px", leaf.Computed.Get("padding-left"));
        mid.TextColor(Green);
        ui.Frame();
        Assert.Equal("#00ff00", leaf.Computed.Get("color"));
        top.FontSize(12);
        ui.Frame();
        Assert.Equal("12px", leaf.Computed.Get("font-size"));
    }

    [Fact]
    public void TokensCascadeDownAndCanBeOverriddenBelow()
    {
        var sheet = new StyleSheet()
            .Rule(":root", s => s.Token("--accent", Blue))
            .Rule(".dark", s => s.Token("--accent", Red))
            .Rule(".btn", s => s.Var("background-color", "--accent"));
        var ui = Tree(sheet);
        var inA = new Element().Class("btn");
        var inB = new Element().Class("btn");
        ui.Root.With(inA, new Element().Class("dark").With(new Element().With(inB)));
        ui.Frame();
        Assert.Equal("#0000ff", Bg(inA));
        Assert.Equal("#ff0000", Bg(inB));
    }

    [Fact]
    public void ChangingATokenRestylesDistantDescendants()
    {
        var theme = new StyleSheet().Rule(":root", s => s.Token("--accent", Blue));
        var lib = new StyleSheet().Rule(".btn", s => s.Var("background-color", "--accent"));
        var ui = Tree(theme, lib);
        var deep = new Element().Class("btn");
        ui.Root.With(new Element().With(new Element().With(new Element().With(deep))));
        ui.Frame();
        Assert.Equal("#0000ff", Bg(deep));

        // A game re-themes a shared library by swapping the sheet that defines the tokens.
        ui.RemoveStyleSheet(theme);
        ui.AddStyleSheet(new StyleSheet().Rule(":root", s => s.Token("--accent", Green)));
        ui.Frame();
        Assert.Equal("#00ff00", Bg(deep));

        // An inline token on an ancestor changes everything below it.
        var mid = deep.Parent!.Parent!;
        mid.Style(s => s.Token("--accent", Red));
        ui.Frame();
        Assert.Equal("#ff0000", Bg(deep));
    }

    [Fact]
    public void MissingTokenUsesFallbackOrTheInheritedValue()
    {
        var sheet = new StyleSheet()
            .Rule(".fb", s => s.Var("background-color", "--nope", Green))
            .Rule(".nofb", s => s.Var("color", "--nope"));
        var ui = Tree(sheet);
        var fb = new Element().Class("fb");
        var nofb = new Element().Class("nofb");
        ui.Root.With(new Element().TextColor(Red).With(fb, nofb));
        ui.Frame();
        Assert.Equal("#00ff00", Bg(fb));
        // Like CSS, a var() that cannot be resolved makes the declaration invalid, so an inherited property inherits.
        Assert.Equal("#ff0000", nofb.Computed.Get("color"));
    }

    [Fact]
    public void TokenOfTheWrongTypeIsIgnoredAndTokensCanRefer()
    {
        var sheet = new StyleSheet()
            .Rule(":root", s => s.Token("--accent", Blue).TokenFrom("--btn", "--accent").Token("--gap", Length.Px(7)))
            .Rule(".a", s => s.Var("width", "--accent").Var("padding-left", "--gap").Var("background-color", "--btn"));
        var ui = Tree(sheet);
        var a = new Element().Class("a");
        ui.Root.Add(a);
        ui.Frame();
        Assert.Equal("auto", a.Computed.Get("width"));
        Assert.Equal("7px", a.Computed.Get("padding-left"));
        Assert.Equal("#0000ff", Bg(a));
    }

    [Fact]
    public void DescendantAndChildCombinators()
    {
        var sheet = new StyleSheet()
            .Rule(".panel .label", s => s.TextColor(Red))
            .Rule(".list > .row", s => s.Background(Green));
        var ui = Tree(sheet);
        var deepLabel = new Element("Label").Class("label");
        var directRow = new Element().Class("row");
        var nestedRow = new Element().Class("row");
        var panel = new Element().Class("panel").With(new Element().With(deepLabel));
        var list = new Element().Class("list").With(directRow, new Element().With(nestedRow));
        ui.Root.With(panel, list);
        ui.Frame();
        Assert.Equal("#ff0000", deepLabel.Computed.Get("color"));
        Assert.Equal("#00ff00", Bg(directRow));
        Assert.Equal("#00000000", Bg(nestedRow));

        // Changing a class on an ancestor restyles the descendants that depend on it.
        panel.SetClass("panel", false);
        ui.Frame();
        Assert.Equal("#000000", deepLabel.Computed.Get("color"));
    }

    [Fact]
    public void StructuralPseudoClasses()
    {
        var sheet = new StyleSheet()
            .Rule(".row:nth-child(odd)", s => s.Background(Red))
            .Rule(".row:nth-child(even)", s => s.Background(Blue))
            .Rule(".row:first-child", s => s.Background(Green))
            .Rule(".row:last-child", s => s.Opacity(0.5f));
        var ui = Tree(sheet);
        var rows = Enumerable.Range(0, 4).Select(_ => new Element().Class("row")).ToArray();
        var list = new Element().With(rows);
        ui.Root.Add(list);
        ui.Frame();
        Assert.Equal(["#00ff00", "#0000ff", "#ff0000", "#0000ff"], rows.Select(Bg));
        Assert.Equal("0.5", rows[3].Computed.Get("opacity"));

        // Inserting a row at the front shifts every row's position, so the stripes follow.
        var added = new Element().Class("row");
        list.Insert(0, added);
        ui.Frame();
        Assert.Equal("#00ff00", Bg(added));
        Assert.Equal("#0000ff", Bg(rows[0]));
        Assert.Equal("#ff0000", Bg(rows[1]));
    }

    [Fact]
    public void HoverActiveFocusAndDisabledStates()
    {
        var sheet = new StyleSheet()
            .Rule(".b", s => s.Background(Red))
            .Rule(".b:hover", s => s.Background(Green))
            .Rule(".b:active", s => s.Background(Blue))
            .Rule(".b:focus", s => s.Opacity(0.25f))
            .Rule(".b:disabled", s => s.Opacity(0.1f));
        var ui = Tree(sheet);
        var b = new Element().Class("b").Focusable().Size(100, 40);
        ui.Root.Add(b);
        ui.Frame();
        Assert.Equal("#ff0000", Bg(b));

        ui.Dispatch(UiEvent.PointerMove(10, 10));
        ui.Frame();
        Assert.Equal("#00ff00", Bg(b));
        Assert.True(b.IsHovered);

        ui.Dispatch(UiEvent.PointerDown(10, 10));
        ui.Frame();
        Assert.Equal("#0000ff", Bg(b));
        Assert.True(b.IsFocused);
        Assert.Equal("0.25", b.Computed.Get("opacity"));

        ui.Dispatch(UiEvent.PointerUp(10, 10));
        ui.Frame();
        Assert.Equal("#00ff00", Bg(b));

        ui.Dispatch(UiEvent.PointerMove(300, 200));
        ui.Frame();
        Assert.Equal("#ff0000", Bg(b));

        b.IsEnabled = false;
        ui.Frame();
        Assert.Equal("0.1", b.Computed.Get("opacity"));
    }

    [Fact]
    public void UiUnitFollowsTheScale()
    {
        var ui = Tree();
        var e = new Element().Style(s => s.Width(Length.Ui(10)).Height(Length.Px(10)).Padding(Length.Ui(2)));
        ui.Root.Add(e);
        ui.SetViewport(400, 300, 1);
        ui.Frame();
        Assert.Equal(10, e.Bounds.Width);
        ui.SetViewport(400, 300, 2.5f);
        ui.Frame();
        Assert.Equal(25, e.Bounds.Width);
        Assert.Equal(10, e.Bounds.Height);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a..b")]
    [InlineData("a >")]
    [InlineData("> a")]
    [InlineData("a:visited")]
    [InlineData(".a:nth-child(foo)")]
    [InlineData("a,,b")]
    public void BadSelectorsThrowATypedException(string selector)
    {
        Assert.Throws<UiStyleException>(() => new StyleSheet().Rule(selector, s => s.Opacity(1)));
    }

    [Fact]
    public void BadStyleNamesAndValuesThrowATypedException()
    {
        var b = new StyleBuilder();
        Assert.Throws<UiStyleException>(() => b.Set("colour", Red));
        Assert.Throws<UiStyleException>(() => b.Set("width", Red));
        Assert.Throws<UiStyleException>(() => b.Token("accent", Red));
        Assert.Throws<UiStyleException>(() => b.Var("width", "gap"));
    }

    [Fact]
    public void EveryPropertyHasAnInitialValueAndAUniqueName()
    {
        Assert.Equal((int)StyleProp.Count, PropertyTable.All.Length);
        Assert.All(PropertyTable.All, p => Assert.NotNull(p));
        Assert.Equal(PropertyTable.All.Length, PropertyTable.All.Select(p => p.Name).Distinct().Count());
        var e = new Element();
        foreach (var name in ComputedStyle.PropertyNames)
        {
            Assert.NotNull(e.Computed.Get(name));
        }
    }
}
