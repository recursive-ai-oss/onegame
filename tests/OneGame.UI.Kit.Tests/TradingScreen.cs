namespace OneGame.UI.Kit.Tests;

public sealed record Good(int Id, string Name, string Category, int Price, int Stock);

/// <summary>The state of the trading screen: the game owns every number, the kit components only read and write them through lambdas.</summary>
public sealed class TradingGame
{
    private static readonly string[] Adjectives = ["Fine", "Rough", "Dried", "Salted", "Spiced", "Pressed", "Raw", "Cured"];
    private static readonly string[] Nouns = ["Grain", "Iron", "Silk", "Salt", "Amber", "Tin", "Wool", "Wine", "Fur", "Lamp oil", "Copper", "Dye", "Tea", "Rope", "Glass", "Cedar", "Hemp", "Olives", "Pearls", "Spice", "Tar", "Flax", "Lead", "Jade", "Cocoa"];
    private static readonly string[] Categories = ["Food", "Metal", "Cloth", "Luxury", "Craft", "Raw"];

    private string _filter = "";
    private int _sortColumn = -1;
    private bool _descending;
    private readonly List<Good> _all;

    public TradingGame(int count = 5000)
    {
        _all = new List<Good>(count);
        for (var i = 0; i < count; i++)
        {
            _all.Add(new Good(i, Adjectives[i % 8] + " " + Nouns[(i / 8) % Nouns.Length] + " " + (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture), Categories[i % 6], 5 + (i * 37 % 997), (i * 13) % 200));
        }

        View = _all;
    }

    public int Gold { get; set; } = 2500;

    public int Tab { get; set; }

    public int Qty { get; set; } = 1;

    public Good? Selected { get; set; }

    public IReadOnlyList<Good> View { get; private set; }

    public string ConfirmText { get; set; } = "";

    public int Purchases { get; set; }

    public int Total => Selected is null ? 0 : Qty * Selected.Price;

    public bool CanBuy => Selected is not null && Qty <= Selected.Stock && Total <= Gold;

    public string Filter
    {
        get => _filter;
        set
        {
            _filter = value;
            Rebuild();
        }
    }

    public void SortBy(int column, bool descending)
    {
        _sortColumn = column;
        _descending = descending;
        Rebuild();
    }

    public void Buy()
    {
        Gold -= Total;
        Purchases++;
    }

    private void Rebuild()
    {
        IEnumerable<Good> q = _all;
        if (_filter.Length > 0)
        {
            q = q.Where(g => g.Name.Contains(_filter, StringComparison.OrdinalIgnoreCase));
        }

        var list = q.ToList();
        Comparison<Good>? cmp = _sortColumn switch
        {
            0 => (a, b) => string.CompareOrdinal(a.Name, b.Name),
            1 => (a, b) => string.CompareOrdinal(a.Category, b.Category),
            2 => (a, b) => a.Price.CompareTo(b.Price),
            3 => (a, b) => a.Stock.CompareTo(b.Stock),
            _ => null,
        };
        if (cmp is not null)
        {
            // A stable order: ties fall back to the id so equal prices do not shuffle between sorts.
            list.Sort((a, b) =>
            {
                var c = cmp(a, b);
                if (_descending)
                {
                    c = -c;
                }

                return c != 0 ? c : a.Id.CompareTo(b.Id);
            });
        }

        View = list;
    }
}

/// <summary>Builds the trading screen from kit components only.</summary>
public static class TradingScreen
{
    public static Element Build(KitHost host, TradingGame game, out TableView<Good> table, out DialogView dialog)
    {
        var t = UiKit.Table<Good>(
            [
                new TableColumn<Good>("Name", g => g.Name, ColumnWidth.Flex(2)),
                new TableColumn<Good>("Category", g => g.Category, ColumnWidth.Flex(1)),
                new TableColumn<Good>("Price", g => g.Price.ToString(System.Globalization.CultureInfo.InvariantCulture), ColumnWidth.Fixed(Length.Ui(90)), alignEnd: true),
                new TableColumn<Good>("Stock", g => g.Stock.ToString(System.Globalization.CultureInfo.InvariantCulture), ColumnWidth.Fixed(Length.Ui(80)), alignEnd: true),
            ],
            () => game.View,
            Length.Ui(28),
            onSort: game.SortBy,
            onSelect: (g, _) => game.Selected = g,
            id: "goods");
        table = t;

        var filter = UiKit.TextInput(() => game.Filter, v => game.Filter = v, "Filter goods", id: "filter");
        var left = Ui.Column(filter, t).Gap(8).Grow(1).Style(s => s.MinWidth(0f));

        var confirm = UiKit.Dialog("Confirm purchase", Ui.Label("").BindText(() => game.ConfirmText), "confirm");
        confirm.AddButton("Cancel", null, isDefault: true);
        confirm.AddButton("Buy", () => game.Buy(), ButtonKind.Primary);
        dialog = confirm;

        var qtySlider = host.Tooltip(UiKit.Slider(() => game.Qty, v => game.Qty = v, 1, 99, 1, "qty-slider"), "Drag, or use Left and Right");
        var qtyStepper = UiKit.NumberStepper(() => game.Qty, v => game.Qty = v, 1, 99, 1, "qty-stepper");
        var buy = host.Tooltip(UiKit.Button("Buy", () =>
        {
            game.ConfirmText = $"Buy {game.Qty} x {game.Selected!.Name} for {game.Total} gold?";
            confirm.Open();
        }, "buy", ButtonKind.Primary).BindEnabled(() => game.CanBuy), "Buy the selected goods");

        var detail = UiKit.Frame(
            KitArt.Frame,
            KitArt.FrameSlice,
            UiKit.Card(
                "Selected",
                Ui.Label("").Named("detail-name").BindText(() => game.Selected?.Name ?? "Nothing selected"),
                Ui.Label("").Named("detail-price").BindText(() => game.Selected?.Price ?? 0, p => "Price " + p.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                Ui.Label("").Named("detail-stock").BindText(() => game.Selected?.Stock ?? 0, p => "In stock " + p.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                Ui.Label("Quantity"),
                qtySlider,
                qtyStepper,
                Ui.Label("").Named("total").BindText(() => game.Total, v => "Total " + v.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                buy)).Width(Length.Ui(300));

        var market = Ui.Row(left, detail).Gap(12).Grow(1).Style(s => s.MinHeight(0f));
        var tabs = UiKit.Tabs(
            [new TabPage("Market", market), new TabPage("Cargo", Ui.Label("Your hold is empty.")), new TabPage("Ships", Ui.Label("No ships."))],
            () => game.Tab,
            i => game.Tab = i,
            "tabs");

        var header = Ui.Row(
            Ui.Label("Trade").Class("title"),
            Ui.Spacer(),
            Ui.Label("").Named("gold").BindText(() => game.Gold, g => "Gold " + g.ToString(System.Globalization.CultureInfo.InvariantCulture))).Padding(8).Style(s => s.AlignItems(Align.Center));

        var screen = Ui.Column(header, tabs).Padding(12).Gap(8).Named("screen").Style(s => s.Anchor(AnchorPoint.Fill).Var("background-color", "--kit-bg"));
        host.Tree.Root.With(screen, confirm);
        return screen;
    }
}
