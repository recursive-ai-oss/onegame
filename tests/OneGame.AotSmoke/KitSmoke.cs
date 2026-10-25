using OneGame.Text;
using OneGame.UI;
using OneGame.UI.Kit;

namespace OneGame.AotSmoke;

/// <summary>A small component-kit screen run under the native publish: table, text input, dialog and a theme swap.</summary>
internal static class KitSmoke
{
    private sealed record Item(string Name, int Price);

    public static string? Run(FontTextMeasurer measurer)
    {
        var items = new List<Item> { new("Grain", 12), new("Iron", 30), new("Silk", 95), new("Salt", 7) };
        var filter = "";
        var gold = 100;
        var shown = items;

        var ui = new UiTree(measurer);
        ui.SetViewport(640, 400);
        var host = new KitHost(ui);

        var table = UiKit.Table<Item>(
            [
                new TableColumn<Item>("Name", g => g.Name, ColumnWidth.Flex(2)),
                new TableColumn<Item>("Price", g => g.Price.ToString(System.Globalization.CultureInfo.InvariantCulture), ColumnWidth.Fixed(Length.Ui(80)), alignEnd: true),
            ],
            () => shown,
            rowHeight: Length.Ui(24),
            onSort: (column, descending) => items.Sort((a, b) => (column == 1 ? a.Price.CompareTo(b.Price) : string.CompareOrdinal(a.Name, b.Name)) * (descending ? -1 : 1)),
            onActivate: (item, _) => gold -= item.Price,
            id: "goods");
        var box = UiKit.TextInput(
            () => filter,
            v =>
            {
                filter = v;
                shown = items.Where(i => i.Name.Contains(v, StringComparison.OrdinalIgnoreCase)).ToList();
            },
            placeholder: "Filter",
            id: "filter");
        var confirm = UiKit.ConfirmDialog("Sell?", "Sure?", onConfirm: () => gold += 1, onCancel: () => { }, id: "confirm");
        ui.Root.With(Ui.Column(box, Ui.Column(table).Height(Length.Ui(200)), UiKit.Button("Sell", () => confirm.Open(), "sell")).Gap(4).Padding(8), confirm);
        Settle(host);

        var field = ui.Find("filter")!.AbsoluteBounds;
        host.Dispatch(UiEvent.PointerDown(field.X + 4, field.Y + 4));
        host.Dispatch(UiEvent.PointerUp(field.X + 4, field.Y + 4));
        host.Dispatch(UiEvent.TextInput("s"));
        Settle(host);
        if (filter != "s" || shown.Count != 2)
        {
            return $"filter '{filter}' shows {shown.Count}";
        }

        var header = ui.Find("goods/header/1")!.AbsoluteBounds;
        host.Dispatch(UiEvent.PointerDown(header.X + 4, header.Y + 4));
        host.Dispatch(UiEvent.PointerUp(header.X + 4, header.Y + 4));
        Settle(host);
        if (items[0].Name != "Salt" || ui.Find("goods/row/0") is null)
        {
            return "sort by price did not reorder";
        }

        var before = ui.Frame().Commands.Length;
        host.SetTheme(KitTheme.Create(KitTokens.Light));
        Settle(host);

        var sell = ui.Find("sell")!.AbsoluteBounds;
        host.Dispatch(UiEvent.PointerDown(sell.X + 4, sell.Y + 4));
        host.Dispatch(UiEvent.PointerUp(sell.X + 4, sell.Y + 4));
        Settle(host);
        if (!ui.DumpJson().Contains("\"confirm\"", StringComparison.Ordinal) || ui.Frame().Commands.Length == 0 || before == 0)
        {
            return "dialog or display list missing";
        }

        return null;
    }

    private static void Settle(KitHost host)
    {
        for (var i = 0; i < 4; i++)
        {
            host.Advance(0);
            host.Tree.Frame();
        }
    }
}
