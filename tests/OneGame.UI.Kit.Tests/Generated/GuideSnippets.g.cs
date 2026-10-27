// Generated from docs/ui-guide.md by GuideSnippetTests (ONEGAME_UPDATE_SNIPPETS=1 regenerates it). Do not edit.
#nullable enable
using OneGame.Atlas;
using OneGame.Core;
using OneGame.Host;
using OneGame.Text;

namespace OneGame.UI.Kit.Tests.Guide;

// docs/ui-guide.md line 53
public static class GameLoop
{
    public static DisplayList OneFrame(KitHost host, float seconds, IEnumerable<UiEvent> input)
    {
        host.Advance(seconds);                 // tooltips wait for a delay; the UI never reads a clock itself
        foreach (var e in input)
        {
            host.Dispatch(e);                  // like ui.Dispatch, and it tells input glyphs which device is in use
        }

        return host.Tree.Frame();              // cheap and allocation-free when nothing changed
    }
}

// docs/ui-guide.md line 78
public sealed class MarketGame : GameDefinition
{
    private KitHost? _kit;
    private bool _clockRunning;

    public override string Name => "Market";

    // A game with entities calls the generated EntityTypes.Register(registry) here.
    public override void RegisterEntities(EntityRegistry registry)
    {
    }

    public override void Setup(World world)
    {
    }

    // 1. Art: the kit's SVGs (sort arrows, chevrons, frame, glyphs) go into the atlas the renderer draws from.
    public override void DefineArt(ArtAtlas art)
    {
        foreach (var a in KitArt.Assets)
        {
            art.AddSvg(a.Id, a.Svg, new NineSlice(a.Slice, a.Slice, a.Slice, a.Slice));
        }
    }

    // 2. Theme: the host calls this once, before the first screen is built.
    public override void Style(UiTree ui) => _kit = new KitHost(ui);

    public override void DefineScreens(ScreenSet screens) =>
        screens.Add("main", game =>
        {
            // 3. Clock: the kit's tooltip delay counts game ticks, so it is deterministic and also runs in headless tests.
            if (!_clockRunning)
            {
                _clockRunning = true;
                game.TickCompleted += _ => _kit!.Advance(1f / GameInstance.TickRate);
            }

            var screen = Ui.Column(
                UiKit.Button("Buy", () => { }, "buy"),
                UiKit.Slider(() => 1, _ => { }, 1, 99, 1, "qty")).Padding(16).Gap(8);

            // 4. The host replaces everything under the root when a screen opens, so the tooltip layer must live inside the screen.
            return _kit!.AttachTo(screen);
        });
}

// docs/ui-guide.md line 135
public sealed record Good(string Name, int Price, int Stock);

public sealed class GuideGame
{
    public int Gold = 500;
    public int Quantity = 1;
    public int Volume = 70;
    public bool Sound = true;
    public string Filter = "";
    public string Difficulty = "normal";
    public int Tab;
    public bool ConfirmOpen;
    public Good? Selected;
    public List<Good> Goods { get; } = [new("Grain", 12, 40), new("Iron", 30, 8), new("Silk", 95, 3), new("Salt", 7, 120)];
}

// docs/ui-guide.md line 657
public static class MyComponents
{
    /// <summary>A price tag: shows an amount, turns red when it exceeds the player's gold.</summary>
    public static Element PriceTag(Func<int> amount, Func<int> gold, string? id = null)
    {
        var tag = new Element("PriceTag") { Role = "price", Id = id };
        tag.Class("price-tag");
        var label = Ui.Label("");
        tag.Add(label);
        tag.OnUpdate(e =>
        {
            var price = amount();
            e.SetClass("unaffordable", price > gold());
            e.Value = label.Text;
        });
        label.BindText(amount, a => a + " gold");        // rebuilds the string only when the amount changes
        return tag;
    }
}

// docs/ui-guide.md line 744
public sealed class ScreenTest
{
    public UiTree Tree { get; } = new();

    public KitHost Host { get; }

    public ScreenTest()
    {
        Tree.SetViewport(1280, 720);
        Host = new KitHost(Tree);
    }

    // Components finish a few things on the frame after an input (layout feeds the scroll bars, focus requests apply after the update), so settle for a few frames.
    public void Settle()
    {
        for (var i = 0; i < 4; i++)
        {
            Host.Advance(0);
            Tree.Frame();
        }
    }

    public void Click(string id)
    {
        var r = Tree.Find(id)!.AbsoluteBounds;
        Host.Dispatch(UiEvent.PointerMove(r.X + (r.Width / 2), r.Y + (r.Height / 2)));
        Host.Dispatch(UiEvent.PointerDown(r.X + (r.Width / 2), r.Y + (r.Height / 2)));
        Host.Dispatch(UiEvent.PointerUp(r.X + (r.Width / 2), r.Y + (r.Height / 2)));
        Settle();
    }

    public void Press(string key)
    {
        Host.Dispatch(UiEvent.KeyDown(key));
        Host.Dispatch(UiEvent.KeyUp(key));
        Settle();
    }
}

internal static class Snippets
{
    // docs/ui-guide.md line 27
    public static void Setup1()
    {
        var fonts = new FontLibrary();
        fonts.Add(File.ReadAllBytes("fonts/Regular.ttf"));
        fonts.Add(File.ReadAllBytes("fonts/Bold.ttf"));
        var measurer = new FontTextMeasurer(fonts);

        var atlas = new ArtAtlas();
        foreach (var art in KitArt.Assets)
        {
            atlas.AddSvg(art.Id, art.Svg, new NineSlice(art.Slice, art.Slice, art.Slice, art.Slice));
        }

        var ui = new UiTree(measurer);
        ui.SetViewport(1920, 1080, scale: 1.5f);   // pixels, and pixels per UI unit
        var host = new KitHost(ui);                // adds the default theme and the tooltip layer
    }

    // docs/ui-guide.md line 161
    public static void Run5(UiTree ui, KitHost host, GuideGame game)
    {
        var panel = Ui.Column(
                Ui.Label("Port of Aldeen"),
                Ui.Row(Ui.Label("Gold"), Ui.Spacer(), Ui.Label("500")).Gap(8),
                Ui.Box().Height(1).Background(Color.Hex("#444")))   // a divider
            .Named("port")                                          // the id: unique, and what tests find it by
            .Class("panel dark")                                    // classes: what style rules select
            .Padding(12).Gap(6);
        ui.Root.Add(panel);
    }

    // docs/ui-guide.md line 183
    public static void Run6(UiTree ui, KitHost host, GuideGame game)
    {
        var gold = Ui.Label("")
            .Named("gold")
            .BindText(() => game.Gold, g => "Gold " + g);            // g => string runs only when the number changes
        var sound = Ui.Label("Sound on").BindVisible(() => game.Sound);
        var buy = UiKit.Button("Buy", () => game.Gold -= 10).BindEnabled(() => game.Gold >= 10);
        var low = Ui.Label("Low on gold").BindClass("warning", () => game.Gold < 50);
        ui.Root.Add(Ui.Column(gold, sound, buy, low));
    }

    // docs/ui-guide.md line 208
    public static void Run7(UiTree ui, KitHost host, GuideGame game)
    {
        var save = Ui.Box(Ui.Label("Save"))
            .Focusable()
            .OnClick(() => game.Gold++)                                  // pointer click, Enter, Space or the confirm button
            .OnPointerEnter(() => game.Sound = true)
            .OnFocus(() => game.Sound = false)
            .OnCancel(() => game.Gold = 0);                              // Escape or the back button, while focus is inside
        ui.Root.Add(save);
    }

    // docs/ui-guide.md line 228
    public static void Run8(UiTree ui, KitHost host, GuideGame game)
    {
        var found = ui.Find("port");                    // by id; null when absent
        var labels = ui.Query("Label");                 // by selector, in document order
        var warnings = ui.Query(".warning");
        var rows = ui.Query(".kit-row.selected");
        string json = ui.DumpJson();                    // the whole tree; DumpJson(element) for one subtree
        Console.WriteLine($"{found?.Id} {labels.Count} {warnings.Count} {rows.Count} {json.Length}");
    }

    // docs/ui-guide.md line 246
    public static void Run9(UiTree ui, KitHost host, GuideGame game)
    {
        var sheet = new StyleSheet()
            .Rule(":root", s => s.FontSize(Length.Ui(14)).TextColor(Color.Hex("#e8e8e8")))
            .Rule(".panel", s => s.Background(Color.Hex("#23262d")).Radius(Length.Ui(6)))
            .Rule(".panel.dark", s => s.Background(Color.Hex("#14161a")))
            .Rule("Button:hover, .clickable:hover", s => s.Opacity(0.9f))
            .Rule(".panel > Label", s => s.FontWeight(700));
        ui.AddStyleSheet(sheet);
    }

    // docs/ui-guide.md line 284
    public static void Run10(UiTree ui, KitHost host, GuideGame game)
    {
        // A scanline pattern from one small tile, instead of dozens of stacked bands: the tile is 2x4 UI units and repeats over the whole panel.
        ui.Root.Add(Ui.Box().Style(s => s
            .Size(Length.Percent(100), Length.Percent(100))
            .BackgroundImage("scanlines").BackgroundRepeat(BackgroundRepeat.Repeat).BackgroundSize(Length.Ui(2), Length.Ui(4))));
        // A poster that covers its box, centred; and a badge placed in the bottom-right corner, drawn once.
        ui.Root.Add(Ui.Box().Style(s => s.Size(Length.Ui(300), Length.Ui(160))
            .BackgroundImage("poster").BackgroundSize(BackgroundSizing.Cover).BackgroundPosition(Length.Percent(50), Length.Percent(50))));
        ui.Root.Add(Ui.Box().Style(s => s.Size(Length.Ui(300), Length.Ui(160))
            .BackgroundImage("badge").BackgroundSize(Length.Ui(32), Length.Ui(32)).BackgroundPosition(Length.Percent(100), Length.Percent(100))));
    }

    // docs/ui-guide.md line 318
    public static void Setup11()
    {
        // In DefineArt, or any time before the screen opens: the second argument is the UI scale the art will be drawn at.
        var art = new ArtAtlas();                       // or game.Art, the host's atlas
        var loading = art.Preload(["panel-frame", "backdrop", "coin"], 1.5f);
        // A loading screen can wait on it:
        if (loading.IsComplete) { /* show the screen */ }
        // Or finish everything right now on this thread:
        art.CompletePreload();
    }

    // docs/ui-guide.md line 335
    public static void Run12(UiTree ui, KitHost host, GuideGame game)
    {
        var theme = new StyleSheet()
            .Rule(":root", s => s.Token("--gold", Color.Hex("#d9a441")).Token("--gap", Length.Ui(8)))
            .Rule(".price", s => s.Var("color", "--gold"))
            .Rule(".stack", s => s.Var("row-gap", "--gap").Var("column-gap", "--gap"));
        ui.AddStyleSheet(theme);
        ui.Root.Add(Ui.Column(Ui.Label("12 gold").Class("price")).Class("stack"));
    }

    // docs/ui-guide.md line 352
    public static void Run13(UiTree ui, KitHost host, GuideGame game)
    {
        // Centered on the screen, whatever the size.
        var modal = Ui.Box(Ui.Label("Hello")).Padding(20).Anchor(AnchorPoint.Center, toScreen: true);

        // Header left, buttons right, a gap between: Row + Spacer.
        var bar = Ui.Row(Ui.Label("Title"), Ui.Spacer(), UiKit.Button("Close", () => { })).Align(Align.Center).Gap(8).Padding(8);

        // Two columns: a flexible list and a fixed detail panel.
        var split = Ui.Row(Ui.Column().Grow(1).MinWidth(0), Ui.Column().Width(Length.Ui(300))).Gap(12);

        // Wrapping tiles with equal gaps.
        var tiles = Ui.Box(Ui.Label("a"), Ui.Label("b"), Ui.Label("c")).Wrap().Gap(Length.Ui(6));

        // Pinned to a corner of the screen, outside the flow.
        var hud = Ui.Label("FPS 60").Anchor(AnchorPoint.TopRight, Length.Ui(8), Length.Ui(8), toScreen: true);

        // A scrolling region: give it a bounded height and an overflow.
        var scroller = Ui.Column(Ui.Label("one"), Ui.Label("two")).Height(Length.Ui(120)).Overflow(OverflowMode.Auto);

        ui.Root.With(Ui.Column(modal, bar, split, tiles, hud, scroller));
    }

    // docs/ui-guide.md line 392
    public static void Run14(UiTree ui, KitHost host, GuideGame game)
    {
        var save = UiKit.Button("Save game", () => game.Gold += 1, id: "save");
        var danger = UiKit.Button("Sell everything", () => game.Gold = 0, "sell", ButtonKind.Danger);
        var price = UiKit.Button(() => "Buy for " + game.Quantity, () => game.Gold -= game.Quantity);   // a label that follows a lambda
        ui.Root.Add(Ui.Row(save, danger, price).Gap(8));
    }

    // docs/ui-guide.md line 404
    public static void Run15(UiTree ui, KitHost host, GuideGame game)
    {
        var fastOn = false;
        var fast = UiKit.ToggleButton("FAST", () => fastOn, v => fastOn = v, id: "fast");    // clicking, Enter, Space and confirm flip it
        ui.Root.Add(Ui.Row(fast, UiKit.Button("Pause", () => { }, "pause")).Gap(8));
    }

    // docs/ui-guide.md line 416
    public static void Run16(UiTree ui, KitHost host, GuideGame game)
    {
        var sound = UiKit.Toggle("Sound", () => game.Sound, v => game.Sound = v, "sound");

        var level = UiKit.RadioGroup(
            [new Choice<string>("easy", "Easy"), new Choice<string>("normal", "Normal"), new Choice<string>("hard", "Hard")],
            () => game.Difficulty,
            v => game.Difficulty = v,
            "difficulty");

        var volume = UiKit.Slider(() => game.Volume, v => game.Volume = v, 0, 100, 5, "volume");   // int, or float with a float range
        var quantity = UiKit.NumberStepper(() => game.Quantity, v => game.Quantity = v, 1, 99, 1, "quantity");
        var loading = UiKit.Progress(() => game.Gold, max: 1000, id: "gold-bar");

        ui.Root.Add(Ui.Column(sound, level, volume, quantity, loading).Gap(8).Padding(12));
    }

    // docs/ui-guide.md line 442
    public static void Run17(UiTree ui, KitHost host, GuideGame game)
    {
        var detail = UiKit.Frame(
            KitArt.Frame,                  // an SVG registered in the art atlas, with nine-slice insets
            KitArt.FrameSlice,             // the unstretched border, in UI units
            UiKit.Card(
                "Selected",
                Ui.Label("").BindText(() => game.Selected?.Name ?? "Nothing selected"),
                Ui.Label("").BindText(() => game.Selected?.Price ?? 0, p => "Price " + p)));
        ui.Root.Add(detail.Width(Length.Ui(260)));
    }

    // docs/ui-guide.md line 459
    public static void Run18(UiTree ui, KitHost host, GuideGame game)
    {
        var tabs = UiKit.Tabs(
            [new TabPage("Market", Ui.Label("Prices go here")), new TabPage("Cargo", Ui.Label("Your hold")), new TabPage("Ships", Ui.Label("Your fleet"))],
            () => game.Tab,
            i => game.Tab = i,
            "tabs");
        ui.Root.Add(tabs);
    }

    // docs/ui-guide.md line 473
    public static void Run19(UiTree ui, KitHost host, GuideGame game)
    {
        var entries = Ui.Column(Enumerable.Range(0, 50).Select(i => Ui.Label("Entry " + i)).ToArray());
        var view = UiKit.ScrollView(entries, id: "log").Height(Length.Ui(200));
        ui.Root.Add(view);
    }

    // docs/ui-guide.md line 488
    public static void Run20(UiTree ui, KitHost host, GuideGame game)
    {
        var table = UiKit.Table<Good>(
            [
                new TableColumn<Good>("Name", g => g.Name, ColumnWidth.Flex(2)),
                new TableColumn<Good>("Price", g => g.Price.ToString(), ColumnWidth.Fixed(Length.Ui(90)), alignEnd: true),
                new TableColumn<Good>("Stock", g => g.Stock.ToString(), ColumnWidth.Fixed(Length.Ui(80)), alignEnd: true),
            ],
            () => game.Goods,
            rowHeight: Length.Ui(28),
            onSort: (column, descending) =>
            {
                // The table shows the sort arrow and asks you to order your list; it then rebinds the rows and keeps the same item selected.
                game.Goods.Sort((a, b) => (column == 1 ? a.Price.CompareTo(b.Price) : column == 2 ? a.Stock.CompareTo(b.Stock) : string.CompareOrdinal(a.Name, b.Name)) * (descending ? -1 : 1));
            },
            onSelect: (good, index) => game.Selected = good,
            onActivate: (good, index) => game.Gold -= good.Price,
            id: "goods");
        ui.Root.Add(Ui.Column(table).Height(Length.Ui(300)));
    }

    // docs/ui-guide.md line 524
    public static void Run21(UiTree ui, KitHost host, GuideGame game)
    {
        var sort = UiKit.Dropdown(
            [new Choice<string>("easy", "Easy"), new Choice<string>("normal", "Normal"), new Choice<string>("hard", "Hard")],
            () => game.Difficulty,
            v => game.Difficulty = v,
            "difficulty-select",
            placeholder: "Choose");
        ui.Root.Add(Ui.Column(sort, Ui.Label("below")).Padding(20));
    }

    // docs/ui-guide.md line 540
    public static void Run22(UiTree ui, KitHost host, GuideGame game)
    {
        var confirm = UiKit.ConfirmDialog(
            "Sell everything?",
            "This cannot be undone.",
            onConfirm: () => game.Gold += 100,
            onCancel: () => { },
            confirmText: "Sell",
            cancelText: "Keep",
            id: "confirm",
            danger: true);                      // focus starts on Cancel
        var open = UiKit.Button("Sell all", () => confirm.Open(), "sell-all");
        ui.Root.With(Ui.Column(open), confirm);  // add the dialog last: it paints over everything
    }

    // docs/ui-guide.md line 560
    public static void Run23(UiTree ui, KitHost host, GuideGame game)
    {
        var filter = UiKit.TextInput(
            get: () => game.Filter,
            set: v => game.Filter = v,
            placeholder: "Filter goods",
            maxLength: 40,
            onSubmit: text => game.Gold += text.Length,
            id: "filter");
        var password = UiKit.TextInput(placeholder: "Password", password: true, id: "pw");
        var digits = UiKit.TextInput(() => game.Quantity.ToString(), v => game.Quantity = int.Parse(v), validate: t => t.All(char.IsAsciiDigit) && t.Length is > 0 and < 4, id: "digits");
        ui.Root.Add(Ui.Column(filter, password, digits).Gap(8).Padding(12));
    }

    // docs/ui-guide.md line 588
    public static void Run24(UiTree ui, KitHost host, GuideGame game)
    {
        var buy = host.Tooltip(UiKit.Button("Buy", () => game.Gold -= 5, "buy"), "Buy one unit at the market price");
        var qty = host.Tooltip(UiKit.NumberStepper(() => game.Quantity, v => game.Quantity = v, 1, 99, 1, "qty"), () => "Quantity: " + game.Quantity);
        ui.Root.Add(Ui.Row(buy, qty).Gap(8).Padding(40));
    }

    // docs/ui-guide.md line 599
    public static void Run25(UiTree ui, KitHost host, GuideGame game)
    {
        host.Glyphs.Bind("confirm", keyboard: "Enter", mouse: "left", gamepad: "a")
                   .Bind("back", keyboard: "Escape", gamepad: "b")
                   .Bind("next-tab", keyboard: "PageDown", gamepad: "rb");
        var prompt = Ui.Row(UiKit.InputGlyph(host.Glyphs, "confirm", "confirm-glyph"), Ui.Label("Buy"), UiKit.InputGlyph(host.Glyphs, "back"), Ui.Label("Back")).Gap(6);
        ui.Root.Add(prompt);
    }

    // docs/ui-guide.md line 613
    public static void Run26(UiTree ui, KitHost host, GuideGame game)
    {
        var row = FocusHelpers.FocusGroup(Ui.Row(UiKit.Button("A", () => { }, "a"), UiKit.Button("B", () => { }, "b"), UiKit.Button("C", () => { }, "c")), horizontal: true, wrap: true);
        var start = UiKit.Button("Start", () => { }, "start").AsDefaultFocus();
        ui.Root.With(Ui.Column(start, row));
        FocusHelpers.FocusDefault(ui, ui.Root);       // call when a screen opens, so a controller player never starts with nothing focused
    }

    // docs/ui-guide.md line 687
    public static void Run28(UiTree ui, KitHost host, GuideGame game)
    {
        // 1. Start from a ready-made theme with a few changes.
        host.SetTheme(KitTheme.Create(KitTokens.Dark with { Accent = Color.Hex("#4fb3a5"), AccentHover = Color.Hex("#6fd0c2"), Radius = Length.Ui(8) }));

        // 2. Change a token for the whole UI later (a colorblind mode, a day/night switch). No element is rebuilt.
        ui.AddStyleSheet(new StyleSheet().Rule(":root", s => s.Token("--kit-accent", Color.Hex("#ffffff"))));

        // 3. Restyle one component or screen with your own rules; they win ties because they come later.
        ui.AddStyleSheet(new StyleSheet()
            .Rule(".kit-button.primary", s => s.Radius(Length.Ui(2)).FontWeight(700))
            .Rule(".market .kit-row.selected", s => s.Var("background-color", "--kit-accent")));
    }

    // docs/ui-guide.md line 726
    public static void Run29(UiTree ui, KitHost host, GuideGame game)
    {
        var report = ThemeLint.Check(ui);             // the tokens in effect on the root; ThemeLint.Check(KitTokens) checks a token set before you use it
        if (!report.Passed)
        {
            foreach (var f in report.Failures)         // worst first, e.g. "primary button text on accent: --kit-accent-text #d9a441 on --kit-accent, 1.0:1, needs 4.5"
            {
                Console.WriteLine($"{f.Name}: {f.Foreground} {f.ForegroundColor} on {f.Background} {f.BackgroundColor} = {f.Ratio}:1, needs {f.Required}");
            }
        }
    }

    // docs/ui-guide.md line 787
    public static void Run31(UiTree ui, KitHost host, GuideGame game)
    {
        var t = new ScreenTest();
        var state = new GuideGame();
        t.Tree.Root.Add(Ui.Column(
            UiKit.Slider(() => state.Quantity, v => state.Quantity = v, 1, 99, 1, "qty"),
            UiKit.Button("Buy", () => state.Gold -= 10, "buy")).Width(400));
        t.Settle();

        t.Click("buy");                                                     // 1. behavior: input in, game state out
        t.Tree.Focus(t.Tree.Find("qty"));
        t.Press("ArrowRight");
        Console.WriteLine($"{state.Gold} {state.Quantity}");                // 490 and 2

        Console.WriteLine(t.Tree.Find("qty")!.Value);                         // 2. the dump: roles, values, ids, states
        var box = t.Tree.Find("buy")!.AbsoluteBounds;                         // 3. boxes: expected sizes, hand-computed with the monospace measurer
        Console.WriteLine($"{box.Width} x {box.Height}");
        Console.WriteLine(t.Tree.DisplayList.ToText().Length);                // 4. the display list: compare with a golden file
        Console.WriteLine(t.Tree.DumpJson(t.Tree.Find("buy")).Length);          //    or the JSON of a subtree
    }

    public static readonly (string Name, Action<UiTree, KitHost, GuideGame> Run)[] Runnable =
    [
        ("Run5", Run5),
        ("Run6", Run6),
        ("Run7", Run7),
        ("Run8", Run8),
        ("Run9", Run9),
        ("Run10", Run10),
        ("Run12", Run12),
        ("Run13", Run13),
        ("Run14", Run14),
        ("Run15", Run15),
        ("Run16", Run16),
        ("Run17", Run17),
        ("Run18", Run18),
        ("Run19", Run19),
        ("Run20", Run20),
        ("Run21", Run21),
        ("Run22", Run22),
        ("Run23", Run23),
        ("Run24", Run24),
        ("Run25", Run25),
        ("Run26", Run26),
        ("Run28", Run28),
        ("Run29", Run29),
        ("Run31", Run31),
    ];
}
