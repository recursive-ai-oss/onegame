namespace OneGame.UI.Kit.Tests;

/// <summary>
/// State galleries: every kit control in enabled, hover, pressed, focused and disabled (and focused-and-disabled where it makes sense). A real pointer can hover only one
/// element at a time, so the gallery sets the same flags the input code sets (hover, pressed, focus) on each cell. The style rules see no difference.
/// </summary>
internal static class Gallery
{
    internal enum State { Enabled, Hover, Pressed, Focused, Disabled, FocusedDisabled }

    internal static readonly State[] Columns = [State.Enabled, State.Hover, State.Pressed, State.Focused, State.Disabled];

    internal static string Id(string row, State state) => row + "/" + state.ToString().ToLowerInvariant();

    /// <summary>Puts a cell in a state by the flags the input code would set. A disabled element cannot take focus, so FocusedDisabled is forced, to see how the two rules combine.</summary>
    internal static void Force(Screen s, string row, State state)
    {
        var e = s.Get(Id(row, state));
        var target = e.Kind switch
        {
            "RadioGroup" => e.Children[0],
            "Dropdown" => s.Get(e.Id + "/head"),
            "Tabs" => s.Get(e.Id + (state is State.Hover or State.Pressed ? "/tab/1" : "/tab/0")),
            "List" when state is State.Hover or State.Pressed => s.Get(e.Id + "/row/2"),
            "Table" when state is State.Hover or State.Pressed => s.Get(e.Id + "/header/1"),
            "Table" when state is State.Focused => s.Get(e.Id + "/header/1"),
            _ => e,
        };
        if (state is State.Disabled or State.FocusedDisabled)
        {
            e.IsEnabled = false;
        }

        if (state is State.Hover or State.Pressed)
        {
            target.HoverChain = true;
        }

        if (state == State.Pressed)
        {
            target.ActiveChain = true;
        }

        if (state is State.Focused or State.FocusedDisabled)
        {
            target.FocusFlag = true;
        }

        target.MarkStyle(true);
    }

    private static Element Cell(Element content) => Ui.Box(content).Style(st => st.Width(Length.Ui(190)).Shrink(0).AlignItems(Align.Center).JustifyContent(Align.Start).Padding(Length.Ui(8), Length.Ui(10)));

    private static Element Heading(string text) => Ui.Label(text).Style(st => st.Var("color", "--kit-text-dim").FontSize(Length.Ui(12)));

    /// <summary>One row per control, one column per state. <paramref name="make"/> receives the id the cell must carry.</summary>
    internal static Element Matrix(string title, IReadOnlyList<(string Row, Func<string, Element> Make)> rows, IReadOnlyList<State> columns)
    {
        var header = Ui.Row(Cell(Heading(title).Style(st => st.FontWeight(700))).Style(st => st.Width(Length.Ui(110))));
        foreach (var c in columns)
        {
            header.Add(Cell(Heading(c == State.FocusedDisabled ? "focused + disabled" : c.ToString().ToLowerInvariant())));
        }

        var grid = Ui.Column(header);
        foreach (var (row, make) in rows)
        {
            var line = Ui.Row(Cell(Ui.Label(row.Replace('-', ' '))).Style(st => st.Width(Length.Ui(110))));
            foreach (var c in columns)
            {
                line.Add(Cell(make(Id(row, c))));
            }

            grid.Add(line);
        }

        return grid;
    }

    private static Element Page(params Element[] sections) =>
        Ui.Column(sections).Gap(14).Padding(Length.Ui(14)).Named("gallery").Style(st => st.Anchor(AnchorPoint.Fill).Var("background-color", "--kit-panel"));

    /// <summary>Every button variant in every state.</summary>
    internal static (Element Page, string[] Rows) Buttons(KitHost host)
    {
        var rows = new (string, Func<string, Element>)[]
        {
            ("normal", id => UiKit.Button("Settings", () => { }, id)),
            ("primary", id => UiKit.Button("Continue", () => { }, id, ButtonKind.Primary)),
            ("danger", id => UiKit.Button("Delete", () => { }, id, ButtonKind.Danger)),
            ("flat", id => UiKit.Button("Skip", () => { }, id, ButtonKind.Flat)),
        };
        var page = Page(Matrix("Buttons", rows, [.. Columns, State.FocusedDisabled]));
        host.Tree.Root.Add(page);
        return (page, [.. rows.Select(r => r.Item1)]);
    }

    /// <summary>Toggles, radios, sliders, steppers, inputs and drop-downs in every state.</summary>
    internal static (Element Page, string[] Rows) Controls(KitHost host)
    {
        var on = true;
        var radio = 1;
        var value = 40f;
        var qty = 3;
        var rows = new (string, Func<string, Element>)[]
        {
            ("checkbox", id => UiKit.Toggle("Music", () => on, v => on = v, id)),
            ("checkbox-off", id => UiKit.Toggle("Sound", () => false, _ => { }, id)),
            ("radio", id => UiKit.RadioGroup<int>([new Choice<int>(1, "Low"), new Choice<int>(2, "High")], () => radio, v => radio = v, id)),
            ("slider", id => UiKit.Slider(() => value, v => value = v, 0, 100, 1, id)),
            ("stepper", id => UiKit.NumberStepper(() => qty, v => qty = v, 1, 9, 1, id)),
            ("input", id => UiKit.TextInput(() => "Salted fish", _ => { }, "Name", id: id)),
            ("input-empty", id => UiKit.TextInput(() => "", _ => { }, "Search goods", id: id)),
            ("dropdown", id => UiKit.Dropdown<int>([new Choice<int>(1, "Small"), new Choice<int>(2, "Large")], () => 2, _ => { }, id)),
        };
        var page = Page(Matrix("Controls", rows, Columns));
        host.Tree.Root.Add(page);
        return (page, [.. rows.Select(r => r.Item1)]);
    }

    /// <summary>Tabs, a list, a table with its header states and an open drop-down.</summary>
    internal static (Element Page, string[] Rows) Collections(KitHost host)
    {
        var tab = 0;
        var items = new List<string> { "Grain", "Iron", "Silk", "Salt", "Amber" };
        var goods = new List<(string, int)> { ("Grain", 12), ("Iron", 30), ("Silk", 90) };
        var rows = new (string, Func<string, Element>)[]
        {
            ("tabs", id => UiKit.Tabs([new TabPage("Market", Ui.Label("")), new TabPage("Cargo", Ui.Label(""))], () => tab, v => tab = v, id).Style(st => st.Grow(0).Height(Length.Ui(44)))),
            ("list", id =>
            {
                var l = UiKit.TextList<string>(() => items, s => s, Length.Ui(24), id: id);
                l.Style(st => st.Grow(0).Height(Length.Ui(100)).Width(Length.Ui(150)));
                l.Select(1);
                return l;
            }),
            ("table", id =>
            {
                var t = UiKit.Table<(string, int)>([new TableColumn<(string, int)>("Name", g => g.Item1, ColumnWidth.Flex(1)), new TableColumn<(string, int)>("Price", g => g.Item2.ToString(System.Globalization.CultureInfo.InvariantCulture), ColumnWidth.Fixed(Length.Ui(64)), alignEnd: true)],
                    () => goods, Length.Ui(22), id: id);
                t.Style(st => st.Grow(0).Height(Length.Ui(100)).Width(Length.Ui(170)));
                t.Body.Select(1);
                return t;
            }),
        };
        var page = Page(Matrix("Collections", rows, Columns));
        host.Tree.Root.Add(page);
        return (page, [.. rows.Select(r => r.Item1)]);
    }

    /// <summary>An open drop-down (selected option, then the focus moved to another one) over a card, and a dialog-style button row with a disabled primary.</summary>
    internal static void Overlay(KitHost host)
    {
        var choice = 2;
        var dropdown = UiKit.Dropdown<int>([new Choice<int>(1, "Small"), new Choice<int>(2, "Medium"), new Choice<int>(3, "Large"), new Choice<int>(4, "Huge")], () => choice, v => choice = v, "size", "Size");
        var card = UiKit.Card("Ship options", Ui.Label("Pick a hull size"), UiKit.Toggle("Insure cargo", () => true, _ => { }, "insure"),
            Ui.Row(UiKit.Button("Cancel", () => { }, "cancel"), UiKit.Button("Confirm", () => { }, "confirm", ButtonKind.Primary).Style(st => st.Grow(0))).Gap(8), dropdown);
        card.Style(st => st.Width(Length.Ui(320)).AlignSelf(Align.Start));
        host.Tree.Root.Add(Page(card));
    }
}
