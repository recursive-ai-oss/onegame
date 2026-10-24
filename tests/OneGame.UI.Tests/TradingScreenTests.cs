using System.Runtime.CompilerServices;
using System.Text.Json;

namespace OneGame.UI.Tests;

/// <summary>
/// A small trading screen written with the builder API: a header with a bound gold counter, tabs, a table whose rows come from a list,
/// and a tooltip overlay that follows the pointer. The boxes are checked by hand-computed values, and the display list and a JSON
/// dump are compared with files in golden/. Set ONEGAME_UPDATE_GOLDEN=1 to rewrite the files after an intended change.
/// </summary>
public class TradingScreenTests
{
    private sealed record Good(string Name, int Price, int Stock);

    private sealed class Game
    {
        public int Gold = 1200;
        public int Tab;
        public int Hover = -1;
        public float PointerX;
        public float PointerY;
        public List<Good> Market { get; } = [new("Grain", 12, 40), new("Iron", 30, 8), new("Silk", 95, 3)];
    }

    private static StyleSheet Theme() => new StyleSheet()
        .Rule(":root", s => s.FontSize(10).TextColor(Color.Hex("#e8e8e8"))
            .Token("--bg", Color.Hex("#14161a")).Token("--panel", Color.Hex("#23262d")).Token("--accent", Color.Hex("#d9a441")))
        .Rule(".screen", s => s.Var("background-color", "--bg"))
        .Rule(".header", s => s.Var("background-color", "--panel"))
        .Rule(".tab", s => s.Padding(4, 10).Radius(3))
        .Rule(".tab.active", s => s.Var("background-color", "--accent").TextColor(Color.Hex("#101010")))
        .Rule(".tab:hover", s => s.Opacity(0.8f))
        .Rule(".row:nth-child(odd)", s => s.Background(Color.Hex("#1b1d22")))
        .Rule(".row:hover", s => s.Background(Color.Hex("#2f3440")))
        .Rule(".tooltip", s => s.Padding(4).Background(Color.Hex("#000000cc")).Border(1, Color.Hex("#d9a441")).Radius(2));

    private static (UiTree Ui, Game Game) Build()
    {
        var g = new Game();
        var ui = new UiTree();
        ui.SetViewport(800, 480);
        ui.AddStyleSheet(Theme());

        Element Tab(string id, string text, int index) =>
            Ui.Button(text, () => g.Tab = index).Named("tab-" + id).Class("tab").BindClass("active", () => g.Tab == index);

        var header = Ui.Row(
            Ui.Label("Port of Aldeen").Named("title"),
            Ui.Spacer(),
            Ui.Label("").Named("gold").BindText(() => g.Gold, v => "Gold: " + v.ToString(System.Globalization.CultureInfo.InvariantCulture)))
            .Named("header").Class("header").Height(30).Padding(0, 8).Align(Align.Center);

        var tabs = Ui.Row(Tab("market", "Market", 0), Tab("cargo", "Cargo", 1), Tab("ships", "Ships", 2))
            .Named("tabs").Gap(4).Padding(4, 8);

        Element Cells(string a, string b, string c) => Ui.Row(
            Ui.Label(a).Grow().Basis(0f), Ui.Label(b).Grow().Basis(0f), Ui.Label(c).Grow().Basis(0f)).Height(18);

        var table = Ui.Column(
                Cells("Item", "Price", "Stock").Named("table-head"),
                Ui.Each(() => g.Market, good =>
                {
                    var index = g.Market.IndexOf(good);
                    return Cells(good.Name, good.Price.ToString(System.Globalization.CultureInfo.InvariantCulture), good.Stock.ToString(System.Globalization.CultureInfo.InvariantCulture))
                        .Named("row-" + good.Name.ToLowerInvariant()).Class("row")
                        .OnPointerEnter(() => g.Hover = index).OnPointerLeave(() => g.Hover = -1);
                }).Column().Gap(4).Named("rows"))
            .Named("market").Gap(4).BindVisible(() => g.Tab == 0);

        var cargo = Ui.Label("Nothing in the hold.").Named("cargo").BindVisible(() => g.Tab == 1);
        var ships = Ui.Label("No ships in port.").Named("ships").BindVisible(() => g.Tab == 2);
        var body = Ui.Column(table, cargo, ships).Named("body").Grow().Padding(8);

        var footer = Ui.Row(Ui.Spacer(), Ui.Button("Buy", () => g.Gold -= g.Market[Math.Max(g.Hover, 0)].Price).Named("buy").Class("tab"))
            .Named("footer").Height(32).Padding(0, 8).Align(Align.Center);

        var tooltip = new Element("Tooltip").Named("tooltip").Class("tooltip")
            .Style(s => s.Position(PositionMode.Fixed).PointerEvents(PointerEvents.None).ZIndex(10))
            .BindVisible(() => g.Hover >= 0 && g.Tab == 0)
            .BindStyle(s => s.Left(g.PointerX + 12).Top(g.PointerY + 12))
            .With(Ui.Label("").BindText(() => g.Hover, i => i < 0 ? "" : $"{g.Market[i].Name}: {g.Market[i].Price}g"));

        var screen = Ui.Column(header, tabs, body, footer).Named("screen").Class("screen").Grow();
        ui.Root.With(screen, tooltip);
        ui.Root.On(UiEventKind.PointerMove, (_, e) =>
        {
            g.PointerX = e.X;
            g.PointerY = e.Y;
            return false;
        });
        ui.Frame();
        return (ui, g);
    }

    private static string Golden(string name, string actual, [CallerFilePath] string here = "")
    {
        var path = Path.Combine(Path.GetDirectoryName(here)!, "golden", name);
        if (Environment.GetEnvironmentVariable("ONEGAME_UPDATE_GOLDEN") == "1")
        {
            File.WriteAllText(path, actual);
        }

        return File.ReadAllText(path);
    }

    private static void AssertBox(UiTree ui, string id, float x, float y, float w, float h)
    {
        var r = ui.Find(id)!.AbsoluteBounds;
        Assert.True(MathF.Abs(r.X - x) < 0.01f && MathF.Abs(r.Y - y) < 0.01f && MathF.Abs(r.Width - w) < 0.01f && MathF.Abs(r.Height - h) < 0.01f,
            $"{id}: expected ({x},{y} {w}x{h}) got {r}");
    }

    [Fact]
    public void BoxesMatchTheHandComputedLayout()
    {
        var (ui, _) = Build();
        // Font 10px: 6px per character, 12px per line. The screen fills the 800x480 root.
        AssertBox(ui, "screen", 0, 0, 800, 480);
        AssertBox(ui, "header", 0, 0, 800, 30);
        AssertBox(ui, "title", 8, 9, 84, 12);            // "Port of Aldeen" = 14 characters, centered in the 30px bar
        AssertBox(ui, "gold", 732, 9, 60, 12);           // "Gold: 1200" = 10 characters, pushed right by the spacer
        AssertBox(ui, "tabs", 0, 30, 800, 28);           // 4px padding around 20px tab buttons
        AssertBox(ui, "tab-market", 8, 34, 56, 20);      // 6 characters + 10px padding each side; 12 + 4px padding top and bottom
        AssertBox(ui, "tab-cargo", 68, 34, 50, 20);
        AssertBox(ui, "tab-ships", 122, 34, 50, 20);
        AssertBox(ui, "body", 0, 58, 800, 390);          // 480 - header 30 - tabs 28 - footer 32
        AssertBox(ui, "table-head", 8, 66, 784, 18);
        AssertBox(ui, "row-grain", 8, 88, 784, 18);
        AssertBox(ui, "row-iron", 8, 110, 784, 18);
        AssertBox(ui, "row-silk", 8, 132, 784, 18);
        AssertBox(ui, "footer", 0, 448, 800, 32);
        AssertBox(ui, "buy", 754, 454, 38, 20);          // 3 characters = 18 + 10px padding each side, centered in the 32px footer
        Assert.False(ui.Find("tooltip")!.IsVisible);
    }

    [Fact]
    public void MarketDisplayListMatchesTheGolden()
    {
        var (ui, _) = Build();
        Assert.Equal(Golden("trading-market.txt", ui.DisplayList.ToText()), ui.DisplayList.ToText());
    }

    [Fact]
    public void TabsSwitchTheBodyAndMoveTheActiveClass()
    {
        var (ui, g) = Build();
        Assert.True(ui.Find("tab-market")!.HasClass("active"));
        Assert.True(ui.Find("market")!.IsVisible);
        ui.Dispatch(UiEvent.PointerDown(90, 44));
        ui.Dispatch(UiEvent.PointerUp(90, 44));
        ui.Frame();
        Assert.Equal(1, g.Tab);
        Assert.False(ui.Find("tab-market")!.HasClass("active"));
        Assert.True(ui.Find("tab-cargo")!.HasClass("active"));
        Assert.False(ui.Find("market")!.IsVisible);
        Assert.True(ui.Find("cargo")!.IsVisible);
        Assert.Equal("#d9a441", ui.Find("tab-cargo")!.Computed.Get("background-color"));
        Assert.Equal("#00000000", ui.Find("tab-market")!.Computed.Get("background-color"));
    }

    [Fact]
    public void HoveringARowShowsTheTooltipAtThePointer()
    {
        var (ui, _) = Build();
        ui.Dispatch(UiEvent.PointerMove(100, 118));
        ui.Frame();
        var tip = ui.Find("tooltip")!;
        Assert.True(tip.IsVisible);
        Assert.True(ui.Find("row-iron")!.IsHovered);
        Assert.Equal("Iron: 30g", tip.ChildAt(0).Text);
        // 9 characters = 54, padding 4 and a 1px border (content-box width inside the border-box): 54 + 8 + 2 = 64 wide, 12 + 10 = 22 tall.
        AssertBox(ui, "tooltip", 112, 130, 64, 22);
        Assert.Equal(Golden("trading-tooltip.txt", ui.DisplayList.ToText()), ui.DisplayList.ToText());

        // The tooltip never takes the pointer from the row under it.
        ui.Dispatch(UiEvent.PointerMove(120, 140));
        Assert.Same(ui.Find("row-silk"), ui.Hovered!.Parent);
        ui.Dispatch(UiEvent.PointerMove(400, 300));
        ui.Frame();
        Assert.False(tip.IsVisible);
    }

    [Fact]
    public void ListChangesKeepRowsAndRestripeThem()
    {
        var (ui, g) = Build();
        var iron = ui.Find("row-iron")!;
        g.Market.RemoveAt(0);
        g.Market.Add(new Good("Wine", 20, 11));
        ui.Frame();
        Assert.Same(iron, ui.Find("row-iron"));
        Assert.Null(ui.Find("row-grain"));
        AssertBox(ui, "row-iron", 8, 88, 784, 18);
        AssertBox(ui, "row-wine", 8, 132, 784, 18);
        // Iron moved from position 2 to 1, so it now carries the odd-row stripe.
        Assert.Equal("#1b1d22", iron.Computed.Get("background-color"));
    }

    [Fact]
    public void InspectionJsonDescribesTheTabs()
    {
        var (ui, _) = Build();
        var json = ui.DumpJson(ui.Find("tabs"));
        Assert.Equal(Golden("trading-tabs.json", json), json);
        using var doc = JsonDocument.Parse(ui.DumpJson());
        var root = doc.RootElement;
        Assert.Equal(800, root.GetProperty("width").GetInt32());
        Assert.Equal("Root", root.GetProperty("root").GetProperty("kind").GetString());
    }

    [Fact]
    public void QueryFindsElementsBySelector()
    {
        var (ui, _) = Build();
        Assert.Equal(["tab-market", "tab-cargo", "tab-ships", "buy"], ui.Query(".tab").Select(e => e.Id));
        Assert.Equal(["tab-market"], ui.Query(".tab.active").Select(e => e.Id));
        Assert.Equal(3, ui.Query("#rows > .row").Count);
        Assert.Equal(3, ui.Query("#market .row").Count);
    }
}
