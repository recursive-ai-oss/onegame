# Building a screen: the UI guide

This guide is for someone who has never seen onegame and has to build game screens from it. Read it top to bottom once; after that the API
documentation (XML docs on every public type) answers the details. Every code block marked `csharp` compiles, and most of them also run, in
`tests/OneGame.UI.Kit.Tests/GuideSnippetTests.cs`.

## 1. The picture in one page

- A screen is a **tree of `Element`s**. Each element has a *kind* (its type selector, such as `Button`), an optional *id*, *classes*, children,
  inline style and event handlers. The tree lives in a `UiTree`.
- You **build** the tree with factory calls: `Ui.Column(...)`, `Ui.Label(...)` and the **component kit** `UiKit.Button(...)`,
  `UiKit.Table(...)`... (project `OneGame.UI.Kit`). Components are *controlled*: they read the game's value through a lambda and report changes
  through another lambda, so your own fields stay the single source of truth.
- **Looks** come from **style sheets** written in the CSS model: rules with selectors, a cascade, and theme **tokens** (`--kit-accent`).
  Layout is **flexbox**. Every element is a flex container.
- Each frame, `ui.Frame()` runs the **bindings** (lambdas that copy game state into the tree), resolves styles, lays out, and returns a
  `DisplayList`: draw commands in pixels that the 2D renderer replays. A frame in which nothing changed does no work and allocates nothing.
- **Input** goes in as `UiEvent`s through `ui.Dispatch(...)`: pointer, keys, text, d-pad, confirm and back. Focus moves with the keyboard and
  the gamepad by itself.
- The harness and your tests see the same tree: `ui.Find("id")`, `ui.Query(".class")`, `ui.DumpJson()`.

## 2. Setting up

You need a text measurer (fonts), an art atlas for SVG art, a `UiTree`, and a `KitHost` that installs the kit's theme.

```csharp setup
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
```

Tests and headless runs use `new UiTree()`, whose `MonospaceTextMeasurer` needs no font: every character is 0.6 em wide and a line is 1.2 em
tall, so expected boxes are easy to compute by hand.

**Units.** `Length` is pixels (`12` converts implicitly), `Length.Ui(12)` (UI units: pixels times the viewport's scale, so the layout follows
the player's UI-scale setting), `Length.Percent(50)` or `Length.Auto`. Write sizes in UI units, hairlines in pixels.

**The per-frame loop** belongs to the game host. Order matters: advance the kit's clock, send input, build the frame, hand the list to the
renderer.

```csharp type
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
```

The renderer draws the list with `renderer.Ui.Draw(list, textQuadBuilder, glyphAtlas, artAtlas)` (see `docs/interfaces/render2d.md`).
Nothing is drawn unless `list.Version` changed; you can skip re-recording when it did not.

### Setting up inside a game (the normal way)

A game made with `onegame new` does not write that loop. Its `GameDefinition` declares screens, and the host owns the `UiTree`, translates platform input
(mouse, keys, text, and the gamepad: South = confirm, East = back, d-pad and shoulder buttons = navigate) into `UiEvent`s, and runs the frames. You plug the kit in
in four places:

```csharp type
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
```

Fonts are the `fonts/` folder next to the game (see `docs/driving-a-game.md`). Inside a screen, `game.Ui` is the tree (`game.Ui.Find("buy")`), and game state is the
`World`: bind labels to `game.World` entities and send commands from button lambdas; never write synced state from a view. Styles for the whole game: add more sheets in `Style`
after the `KitHost` line, so yours win ties. After a screen opens, call `FocusHelpers.FocusDefault(game.Ui, screen)` if a controller player needs a starting focus.

## 3. The state your screens read

The guide's examples read and write one small game object. Your game has its own; the point is that **components never own game state**.

```csharp type
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
```

The snippets below run with `ui` (a `UiTree`), `host` (a `KitHost` for it) and `game` (a `GuideGame`) already defined.

## 4. The builder

### Elements

`Ui` has the basic factories. They return `Element`, and `Element`'s methods return the element, so calls chain.

```csharp
var panel = Ui.Column(
        Ui.Label("Port of Aldeen"),
        Ui.Row(Ui.Label("Gold"), Ui.Spacer(), Ui.Label("500")).Gap(8),
        Ui.Box().Height(1).Background(Color.Hex("#444")))   // a divider
    .Named("port")                                          // the id: unique, and what tests find it by
    .Class("panel dark")                                    // classes: what style rules select
    .Padding(12).Gap(6);
ui.Root.Add(panel);
```

- `Ui.Box`, `Ui.Row`, `Ui.Column` hold children; `Ui.Label` shows text; `Ui.Image("asset-id", w, h)` shows an image or SVG by atlas id;
  `Ui.Spacer()` grows to fill; `Ui.Each(items, build)` builds children from a list.
- An element has **one parent**. Adding it twice throws `UiTreeException`; `Remove` it first.
- The first child of `ui.Root` is your screen; a screen fills the viewport with `.Style(s => s.Anchor(AnchorPoint.Fill))`.
- Quick style helpers (`Width`, `Padding`, `Gap`, `Grow`, `Background`, `Radius`, `Border`, ...) are shorthand for `Style(s => ...)`. Anything
  else goes through `Style` (section 6).

### Bindings: copying game state into the tree

A binding is a lambda the UI runs **every frame** before styles are resolved. It must be cheap and must not allocate when nothing changed.

```csharp
var gold = Ui.Label("")
    .Named("gold")
    .BindText(() => game.Gold, g => "Gold " + g);            // g => string runs only when the number changes
var sound = Ui.Label("Sound on").BindVisible(() => game.Sound);
var buy = UiKit.Button("Buy", () => game.Gold -= 10).BindEnabled(() => game.Gold >= 10);
var low = Ui.Label("Low on gold").BindClass("warning", () => game.Gold < 50);
ui.Root.Add(Ui.Column(gold, sound, buy, low));
```

| Binding | Does |
|---|---|
| `BindText(Func<string>)` | sets the text; return a cached string, or the frame allocates |
| `BindText(Func<T>, Func<T,string>)` | rebuilds the text only when the value changes (`T` implements `IEquatable<T>`: `int`, `bool`, `string`...) |
| `BindClass(name, Func<bool>)`, `BindVisible`, `BindEnabled` | toggle a class, show or hide, enable or disable |
| `BindStyle(Action<StyleBuilder>)` | re-apply inline style from game values (unchanged values cost nothing) |
| `BindChildren(Func<IReadOnlyList<T>>, Func<T,Element>)` / `Ui.Each` | children follow a list; an item keeps its element while it stays in the list |
| `OnUpdate(Action<Element>)` | anything else: runs against the element every frame |

**Allocation rule.** A frame where nothing changed must allocate zero bytes (the acceptance test measures it). In your own bindings: do not
build strings, closures or lists per frame. One C# trap: a lambda that *captures a local variable* makes the compiler allocate its closure at
the start of the enclosing block, even if an early `return` comes first. Keep capturing lambdas in a separate method that only runs on change.

### Events

```csharp
var save = Ui.Box(Ui.Label("Save"))
    .Focusable()
    .OnClick(() => game.Gold++)                                  // pointer click, Enter, Space or the confirm button
    .OnPointerEnter(() => game.Sound = true)
    .OnFocus(() => game.Sound = false)
    .OnCancel(() => game.Gold = 0);                              // Escape or the back button, while focus is inside
ui.Root.Add(save);
```

`On(UiEventKind, handler)` is the general form: the handler gets the element and the event and returns `true` when it handled it, which stops the
event bubbling to parents. Events go to the element under the pointer, or to the focused element, and **bubble upward**.

### Replacing and updating the tree

Prefer bindings over rebuilding. When a whole screen changes, `Remove` the old one and `Add` the new one; elements are cheap. Never keep an
element in two places.

## 5. Looking things up: ids, roles, queries

```csharp
var found = ui.Find("port");                    // by id; null when absent
var labels = ui.Query("Label");                 // by selector, in document order
var warnings = ui.Query(".warning");
var rows = ui.Query(".kit-row.selected");
string json = ui.DumpJson();                    // the whole tree; DumpJson(element) for one subtree
Console.WriteLine($"{found?.Id} {labels.Count} {warnings.Count} {rows.Count} {json.Length}");
```

Every kit component sets `Element.Role` (`button`, `slider`, `table`, `row`, `tab`, `dialog`...) and `Element.Value` (the number a slider
shows, the title of the selected tab, `true` on a checked box), and gives its parts ids derived from the id you pass (`qty/track`, `goods/row/3`).
The JSON dump prints `kind`, `id`, `role`, `value`, `classes`, `states` (`hover`, `focus`, `disabled`...), `text`, local and screen boxes,
scroll extents and non-default computed style, so a scenario can locate and check anything. **Give every component you will test an id.**

## 6. Styles: the CSS model

### Style sheets and selectors

```csharp
var sheet = new StyleSheet()
    .Rule(":root", s => s.FontSize(Length.Ui(14)).TextColor(Color.Hex("#e8e8e8")))
    .Rule(".panel", s => s.Background(Color.Hex("#23262d")).Radius(Length.Ui(6)))
    .Rule(".panel.dark", s => s.Background(Color.Hex("#14161a")))
    .Rule("Button:hover, .clickable:hover", s => s.Opacity(0.9f))
    .Rule(".panel > Label", s => s.FontWeight(700));
ui.AddStyleSheet(sheet);
```

Selectors: a type (the element's kind, `Button`), `#id`, `.class`, `*`, and the pseudo-classes `:hover :focus :active :disabled :root :first-child
:last-child :nth-child(odd|even|N)`, with the descendant (space) and child (`>`) combinators and comma lists. Specificity follows CSS (ids, then classes
and pseudo-classes, then types); ties go to the later rule, and later sheets win over earlier ones. **Inline style** (`Style(...)`, `.Width(...)`)
beats every sheet rule.

Selectors with combinators make state changes restyle subtrees, so prefer single classes on hot paths such as rows.

### Properties

`StyleBuilder` has a method per property. The ones you will use most:

| Group | Methods |
|---|---|
| Box | `Width Height MinWidth MaxWidth MinHeight MaxHeight Margin Padding BorderWidth Border BoxSizing` (border-box by default) |
| Flex | `Direction Wrap JustifyContent AlignItems AlignSelf AlignContent Grow Shrink Basis Flex Gap RowGap ColumnGap Order` |
| Position | `Position(Relative/Absolute/Fixed) Left Top Right Bottom Inset Anchor ZIndex Overflow Translate Scale Rotate` |
| Paint | `Background BackgroundImage BackgroundSlice BackgroundTint BorderColor Radius Opacity Shadow Outline Visibility` |
| Text | `TextColor FontFamily FontSize FontWeight FontStyle LineHeight LetterSpacing TextAlign WhiteSpace` |
| Input | `PointerEvents Cursor` |

`Set("css-name", value)` sets any property by name. `Display(DisplayMode.None)` or `IsVisible = false` removes an element from layout and input.
Text properties are inherited; everything else is not.

### Tokens: values the theme can change

A token is a named value defined on an element (usually `:root`) and read by that element's descendants:

```csharp
var theme = new StyleSheet()
    .Rule(":root", s => s.Token("--gold", Color.Hex("#d9a441")).Token("--gap", Length.Ui(8)))
    .Rule(".price", s => s.Var("color", "--gold"))
    .Rule(".stack", s => s.Var("row-gap", "--gap").Var("column-gap", "--gap"));
ui.AddStyleSheet(theme);
ui.Root.Add(Ui.Column(Ui.Label("12 gold").Class("price")).Class("stack"));
```

`Var(property, token, fallback)` uses the nearest definition up the tree and the fallback when none exists. A token of the wrong type for the
property is ignored. Redefining a token (a later rule, or on a subtree) restyles everything that reads it **without rebuilding any element**; this is
how the kit's themes work.

## 7. Layout recipes

Every element is a flex container (row by default, `Column()` for a column). There is no block or inline flow, no floats and no grid.

```csharp
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
```

Rules of thumb:

- **Fill the rest:** `Grow(1)` on the one child that should take the free space; on a flex child that must be allowed to shrink below its content
  (anything that holds a table, a list or text that can clip), add `MinWidth(0)` or `MinHeight(0)`.
- **Equal columns:** `Style(s => s.Flex(1, 1, Length.Px(0)))` on each child.
- **Alignment:** `Justify` is along the main axis, `Align` across it (`Align.Center`, `Start`, `End`, `Stretch`, `SpaceBetween`...).
- **Overlays** (tooltips, dialogs, menus): `Position(Fixed)` or `Anchor(..., toScreen: true)` plus a `ZIndex`. Absolute children sit against the
  parent's padding box.
- **Order of painting and hit testing** is document order within a parent, then `z-index`.
- Text wraps at the width the parent gives it; `WhiteSpace(WhiteSpace.NoWrap)` stops that, and `Overflow(Hidden)` clips it.

## 8. The component kit

All components are in namespace `OneGame.UI.Kit` and created by `UiKit.*`. Each takes an optional `id`, which also prefixes the ids of its parts.
Every component is themeable (section 11), reachable by keyboard and gamepad (section 9), and allocates nothing on a frame without change.

### Button

```csharp
var save = UiKit.Button("Save game", () => game.Gold += 1, id: "save");
var danger = UiKit.Button("Sell everything", () => game.Gold = 0, "sell", ButtonKind.Danger);
var price = UiKit.Button(() => "Buy for " + game.Quantity, () => game.Gold -= game.Quantity);   // a label that follows a lambda
ui.Root.Add(Ui.Row(save, danger, price).Gap(8));
```

Kinds: `Normal`, `Primary` (accent), `Danger`, `Flat`. Disable with `button.IsEnabled = false` or `.BindEnabled(...)`; a disabled button never calls back.
Click, Enter, Space and the confirm button all call the lambda.

The states are told apart by more than color (section 11, "States and legibility"): hover and pressed change the fill, focus adds a ring outside the button, and a disabled button of any kind is flat and dim, with no accent or danger fill, so a disabled primary never looks like the main action.

### Checkbox, radio group, slider, stepper, progress bar

```csharp
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
```

- **Toggle**: class `checked`, `Value` `true`/`false`.
- **RadioGroup<T>**: options are `Choice<T>(value, label)`. Only the selected option takes Tab focus; arrow keys and the d-pad select the
  neighbor; `horizontal: true` lays it out in a row. Parts: `id/0`, `id/1`...
- **Slider**: drag, click the track, or with focus: Left/Right (arrow keys and d-pad) one step, PageUp/PageDown ten, Home/End the ends. Up and
  Down move focus away. Parts `id/track`, `id/fill`, `id/thumb`, `id/value`. `Value` is the formatted number; pass `format:` to change it.
- **NumberStepper**: `-` and `+` buttons (parts `id/minus`, `id/plus`) and keys like the slider. It clamps and disables a button at its limit.
- **Progress**: `Value` is the fraction (`0.25`); part `id/fill`.

### Card and nine-slice frame

```csharp
var detail = UiKit.Frame(
    KitArt.Frame,                  // an SVG registered in the art atlas, with nine-slice insets
    KitArt.FrameSlice,             // the unstretched border, in UI units
    UiKit.Card(
        "Selected",
        Ui.Label("").BindText(() => game.Selected?.Name ?? "Nothing selected"),
        Ui.Label("").BindText(() => game.Selected?.Price ?? 0, p => "Price " + p)));
ui.Root.Add(detail.Width(Length.Ui(260)));
```

`Card(title, content...)` is a raised panel with an optional heading. `Frame(artId, slice, content...)` stretches the art so its corners keep their size and
pads the content by the slice. Register your own art with `atlas.AddSvg(id, svgBytes, new NineSlice(l, t, r, b))` (insets in the SVG's own pixels at scale 1).
`KitArt.Assets` also holds the sort arrows, the drop-down chevron, the key cap and the input glyphs; register all of it once (section 2).

### Tabs

```csharp
var tabs = UiKit.Tabs(
    [new TabPage("Market", Ui.Label("Prices go here")), new TabPage("Cargo", Ui.Label("Your hold")), new TabPage("Ships", Ui.Label("Your fleet"))],
    () => game.Tab,
    i => game.Tab = i,
    "tabs");
ui.Root.Add(tabs);
```

Pages stay in the tree while hidden, so their state survives. Parts: `tabs/tab/0`..., `tabs/panel/0`...; `Value` of `tabs` is the selected title. Only the selected tab takes
Tab focus; Left/Right switch tabs (and move focus); **Ctrl+PageUp/PageDown** switch from anywhere inside.

### Scroll view

```csharp
var entries = Ui.Column(Enumerable.Range(0, 50).Select(i => Ui.Label("Entry " + i)).ToArray());
var view = UiKit.ScrollView(entries, id: "log").Height(Length.Ui(200));
ui.Root.Add(view);
```

The wheel, the thumb (drag it), a click on the bar's track and focus moving to a hidden child all scroll. Bars overlay the content, so they take no layout space and only
appear when needed. `horizontal: true` adds a horizontal bar. Give the view a bounded height (a fixed one, or `Grow(1)` inside a column with a bounded height).

### Lists and tables: thousands of rows

`UiKit.VirtualList` and `UiKit.Table` create row elements only for the rows in view and recycle them while scrolling, so 5,000 or 500,000 items cost the same per frame.
**All rows have the same height.** The lists read the items through a lambda every frame: return the **same list instance** while nothing changed, and a **new
instance** (or bump the optional `version:` counter) when the content changed.

```csharp
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
```

- **Sorting.** Header cells of sortable columns are focusable; click or Enter calls `onSort(column, descending)`: first ascending, then flipping. The header gets
  `sorted` and `asc`/`desc`. Sort *in place*, or build a new list; both work because the table refreshes after the callback.
- **Selection.** Click a row, or focus the table body and use Up/Down/PageUp/PageDown/Home/End (arrow keys or d-pad). `table.Body.SelectedIndex`, `SelectedItem` and
  `Select(index)` read and set it. Enter, Space or the confirm button calls `onActivate`. At the first or last row Up or Down is not used, so focus moves on to the
  neighbor: that is how a gamepad player gets out of a table.
- **Changing items.** After replacing the list (a filter), the selection follows its item to the new index, or clears if the item is gone. If you change an item's fields in
  place, call `table.Body.Invalidate()` or bump `version:`.
- **Custom cells** (icons, bars, buttons): `new TableColumn<Good>(title, createCell: () => ..., bindCell: (cell, good) => ..., width)`. `createCell` runs once per recycled row,
  `bindCell` runs when a row gets a different item and never on a quiet frame.
- **Ids:** `goods` (the table), `goods/header/0`..., `goods/body` (focusable, role `listbox`), `goods/row/N` (the item index, role `row`, class `selected`, `odd`).
  `table.Body.RealizedRowCount` is how many row elements exist.

`UiKit.TextList(items, text, rowHeight, onSelect, onActivate, id)` is the one-column version; `UiKit.VirtualList(items, createRow, bindRow, rowHeight, ...)` takes any row element.

### Drop-down

```csharp
var sort = UiKit.Dropdown(
    [new Choice<string>("easy", "Easy"), new Choice<string>("normal", "Normal"), new Choice<string>("hard", "Hard")],
    () => game.Difficulty,
    v => game.Difficulty = v,
    "difficulty-select",
    placeholder: "Choose");
ui.Root.Add(Ui.Column(sort, Ui.Label("below")).Padding(20));
```

Enter, Space, confirm or a click opens the list and focuses the chosen option; Up/Down move; the open list **traps focus**; Enter or a click chooses; Escape or the
back button closes without choosing; focus returns to the button. The list opens upward when there is no room below. Parts: `id/head`, `id/popup`, `id/option/N`; the root
has the class `open` while open.

### Dialog

```csharp
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
```

While a dialog is open the pointer cannot reach what is behind it, **focus, Tab and the d-pad stay inside it**, Escape and the back button close it (and call `onCancel`),
and focus returns to what had it before. `dialog.Open()` and `Close()` from code; `dialog.BindOpen(() => game.ConfirmOpen)` to follow a flag. For your own content
use `UiKit.Dialog(title, body, id, onCancel)` and `dialog.AddButton(text, onClick, kind, isDefault)`. Mark the element to focus first with `.AsDefaultFocus()`.

### Text input

```csharp
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
```

- The field reads the text through `get` every frame and reports each edit through `set`. If your `set` refuses the edit by not storing it, the field shows the old text on the
  next frame. Without `get` the field keeps its own text (`field.FieldText`, `field.SetText(...)`).
- Editing works on **grapheme clusters**: a letter with its accents or an emoji sequence is one step for the arrow keys, Backspace and Delete. Arrow keys move through the
  text **as it looks on screen**, also where left-to-right and right-to-left text mix. Shift extends the selection; Home and End jump; Ctrl or Cmd with A, C, X and V select all,
  copy, cut and paste. Enter calls `onSubmit`. Up, Down, Tab and the d-pad leave the field.
- `password: true` shows bullets, never copies, and keeps the text out of `Value` and the dump.
- Text comes in as `UiEvent.TextInput` (committed text, input-method results included). An input method's uncommitted text arrives as `UiEvent.Composition`;
  the field shows it at the caret (`field.Preedit`) until the commit or a clear. A field sets `AcceptsText`, so the window system's input method is on while it has focus, and keeps
  `CaretArea` (`field.CaretScreenRect`) current so the candidate window follows the caret.
- **Clipboard:** in a windowed game the game instance sets `UiKit.DefaultClipboard` to the system clipboard, so copy, cut and paste reach other programs. Headless runs, and windowed runs on a
  compositor without a seat, keep it in memory. Pass `clipboard:` to give one field its own `IClipboard`.
- `validate` receives the text an edit would produce; return false to reject it. `maxLength` counts grapheme clusters.

### Tooltips

```csharp
var buy = host.Tooltip(UiKit.Button("Buy", () => game.Gold -= 5, "buy"), "Buy one unit at the market price");
var qty = host.Tooltip(UiKit.NumberStepper(() => game.Quantity, v => game.Quantity = v, 1, 99, 1, "qty"), () => "Quantity: " + game.Quantity);
ui.Root.Add(Ui.Row(buy, qty).Gap(8).Padding(40));
```

After `host.TooltipDelay` (half a second) of pointer hover or keyboard focus, a box appears below the element (above it when there is no room), clamped to the screen, and follows
it if it moves. Time comes from `host.Advance(seconds)` in your loop; tests advance it by hand, which keeps them deterministic. Any element can have one; the text may be a lambda.

### Input glyphs: "press (A)"

```csharp
host.Glyphs.Bind("confirm", keyboard: "Enter", mouse: "left", gamepad: "a")
           .Bind("back", keyboard: "Escape", gamepad: "b")
           .Bind("next-tab", keyboard: "PageDown", gamepad: "rb");
var prompt = Ui.Row(UiKit.InputGlyph(host.Glyphs, "confirm", "confirm-glyph"), Ui.Label("Buy"), UiKit.InputGlyph(host.Glyphs, "back"), Ui.Label("Back")).Gap(6);
ui.Root.Add(prompt);
```

The glyph shows the control of the device used last: a key cap with the key's name, a mouse with the button lit, or a gamepad button picture. Send input through `host.Dispatch`
(or call `host.Glyphs.Observe(e)`) and the glyphs change by themselves; `host.Glyphs.SetDevice(...)` sets it directly. Names: keyboard = DOM key names; mouse = `left right middle move`;
gamepad = `a b x y lb rb lt rt ls rs dpad dpad-up dpad-down dpad-left dpad-right start select`. An unbound action hides the glyph (class `unbound`). The pictures are in `KitArt.Assets`.

### Focus helpers

```csharp
var row = FocusHelpers.FocusGroup(Ui.Row(UiKit.Button("A", () => { }, "a"), UiKit.Button("B", () => { }, "b"), UiKit.Button("C", () => { }, "c")), horizontal: true, wrap: true);
var start = UiKit.Button("Start", () => { }, "start").AsDefaultFocus();
ui.Root.With(Ui.Column(start, row));
FocusHelpers.FocusDefault(ui, ui.Root);       // call when a screen opens, so a controller player never starts with nothing focused
```

- `FocusGroup` keeps the arrow keys and d-pad inside a row or column of controls until its ends (or wraps).
- `.AsDefaultFocus()` marks what `FocusDefault` focuses first; otherwise it is the first focusable element.
- Every focusable component has the class `kit-focusable`, and the theme draws the focus ring (an outline, so it takes no space) on `.kit-focusable:focus`.

## 9. Focus, keyboard and controller navigation

The UI owns **one focused element**. Input arrives as `UiEvent`s; the platform layer or your test creates them with factories:

| You send | Means |
|---|---|
| `UiEvent.PointerMove/PointerDown/PointerUp(x, y)` | mouse or touch, in screen pixels; a press focuses the nearest focusable element |
| `UiEvent.Wheel(x, y, dx, dy)` | scroll |
| `UiEvent.KeyDown("Enter")`, `KeyUp`, with `KeyModifiers` | keyboard, DOM key names |
| `UiEvent.TextInput("é")` | committed text |
| `UiEvent.Navigate(NavDirection.Up)` | d-pad or stick step; bumpers send `Next`/`Previous` |
| `UiEvent.Activate()` | the confirm button (clicks the focused element) |
| `UiEvent.Cancel()` | the back button (delivered up from the focused element) |

What happens by default: **Tab / Shift+Tab** move through focusable elements in document order (`TabIndex` > 0 first). **Arrow keys and the d-pad** move focus to the nearest
focusable element in that direction, preferring one wholly beyond the current element and lined up with it. Enter and Space click the focused element. Escape is a Cancel.
A component may use a key itself first (a slider takes Left/Right, a text field takes the arrows) and returns `true`; unused keys fall back to moving focus. So a
gamepad player (the host sends South as confirm, East as back, the d-pad and the shoulder buttons as navigate) moves with the d-pad across the screen, adjusts sliders with Left/Right, and leaves a table with Up at the first row.

- `ui.Focus(element)` focuses directly (false when the element cannot take focus); `ui.Focus(null)` clears it. `ui.RequestFocus(element)` does it at the end of the next update,
  which is what a binding must use. `ui.Focused` is the current element.
- Focus **traps**: `element.TrapFocus()` keeps Tab and the d-pad inside it while it is shown (dialogs and drop-down lists do this).
- A disabled or hidden element is skipped. Focusing an element scrolls scroll views so it is visible.
- Dragging (sliders, scroll thumbs, text selection) uses **pointer capture**: `ui.CapturePointer(element)` from a `PointerDown` handler sends moves and the release to that
  element until the button is released.
- A `Click` carries `Button` 0 for the pointer and -1 when it came from Enter, Space or the confirm button.

Design for the controller first: every action reachable by d-pad and confirm, back always leaves, and a default focus on every screen.

## 10. Writing your own component

A component is a function that returns an `Element`. Follow the kit's pattern so it behaves like the rest:

```csharp type
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
```

Give it a `Kind` (the type selector), a `Role` and `Value`, classes for its states, ids for its parts, take game values as lambdas, and put its look in a style sheet that reads tokens. Ship
the sheet with the component (`public static StyleSheet Styles(...)`) so a game can add it, then add its own rules after it. That is all a **shared UI library** is: a package of such
functions plus a sheet, themed by tokens and never forked.

## 11. Theming a game

The kit's look is made of tokens; `KitTokens` lists them with their names (`--kit-bg`, `--kit-panel`, `--kit-accent`, `--kit-text`, `--kit-radius`, `--kit-font-size`...).

```csharp
// 1. Start from a ready-made theme with a few changes.
host.SetTheme(KitTheme.Create(KitTokens.Dark with { Accent = Color.Hex("#4fb3a5"), AccentHover = Color.Hex("#6fd0c2"), Radius = Length.Ui(8) }));

// 2. Change a token for the whole UI later (a colorblind mode, a day/night switch). No element is rebuilt.
ui.AddStyleSheet(new StyleSheet().Rule(":root", s => s.Token("--kit-accent", Color.Hex("#ffffff"))));

// 3. Restyle one component or screen with your own rules; they win ties because they come later.
ui.AddStyleSheet(new StyleSheet()
    .Rule(".kit-button.primary", s => s.Radius(Length.Ui(2)).FontWeight(700))
    .Rule(".market .kit-row.selected", s => s.Var("background-color", "--kit-accent")));
```

- `host.SetTheme(sheet)` swaps the kit's sheet in place, so the sheets you added after it still win.
- Scope a different look to a screen by putting tokens on a class and giving that class to the screen's root: `new StyleSheet().Rule(".night", s => s.Token("--kit-panel", Color.Hex("#0b0c10")))`.
- The structure of every component is stable: kind, role, classes and part ids are API. Style them through those, never through child positions.
- SVG art (`KitArt`): the small icons (sort arrows, chevron, check) are white and take their color from the theme with `background-tint`, so they follow a light or dark theme. The frame, key caps and gamepad glyphs have their own colors (dark) baked in; replace them by registering your own SVG under the same ids when your theme is light.

### States and legibility

Every kit control looks different in each state, and the differences do not rely on color alone:

| State | Look |
|---|---|
| hover | the fill goes lighter (accent and hover tokens), the pointer is a hand |
| pressed | the fill goes darker (`--kit-active`, `--kit-accent-active`) |
| focus | a ring of `--kit-focus-width` outside the control, 2 px away from it, so it reads against the surface behind and not against a primary fill; inside clipped parts (list rows, table headers, drop-down options, tabs) the ring is drawn inside |
| selected | row and option: fill, text color and an accent frame inside the row; tab: accent underline; checkbox and radio: accent fill |
| disabled | the fill is flat (`--kit-disabled-fill`), the outline dim (`--kit-disabled-border`), the text `--kit-disabled-text`; no hover or pressed change, no opacity fade, the same for every variant |

`:disabled` matches a disabled element and everything inside it, so `card.IsEnabled = false` dims the whole card.

The theme derives state colors from your tokens and makes the ones that carry text or an indicator legible by the WCAG 2.x AA ratios (text 4.5:1, focus ring, control outlines, accent fills and scroll thumbs 3:1,
disabled text 3:1). `KitTheme.Create` moves a token that would fall short just far enough toward white or black: `Text` and `TextDim` on every surface, `AccentText` on the accent in all its states,
`SelectionText`, `Focus`, `Border` for control outlines (`--kit-control-border`), `Scrollbar`. It never touches one that already passes. Colors that are not text, such as your `Accent` against the panel, are only checked.

Check a theme, including any token your game overrides by hand after the kit's sheet (which skips the repair), from a test or from the command line:

```csharp
var report = ThemeLint.Check(ui);             // the tokens in effect on the root; ThemeLint.Check(KitTokens) checks a token set before you use it
if (!report.Passed)
{
    foreach (var f in report.Failures)         // worst first, e.g. "primary button text on accent: --kit-accent-text #d9a441 on --kit-accent, 1.0:1, needs 4.5"
    {
        Console.WriteLine($"{f.Name}: {f.Foreground} {f.ForegroundColor} on {f.Background} {f.BackgroundColor} = {f.Ratio}:1, needs {f.Required}");
    }
}
```

`onegame lint-theme --json` runs the game headless and prints the same report (exit code 1 when a pair fails); the control operation `theme_lint` returns it from a running game over MCP.
Put a `ThemeLint.Check` in your game's tests so a theme edit cannot silently break legibility.

## 12. Testing a screen

Test through the same events and frames as the harness; do not call handlers. A test screen is a `UiTree` with the monospace measurer, a `KitHost`, and a helper that runs frames.

```csharp type
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
```

What to assert, from cheap to strong:

```csharp
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
```

- **Find and invoke:** `ui.Find(id)`, `ui.Query(selector)`, `ui.Invoke(element)` (a click without hit testing), `ui.Dispatch(event)` for everything else.
- **Allocation:** `GC.GetAllocatedBytesForCurrentThread()` around 100 `Frame()` calls on a quiet screen must be 0.
- **Golden files:** compare `DisplayList.ToText()` and an outline of roles and boxes with committed text files; rewrite them with an env var when a change is intended
  (`TradingScreenTests` shows the pattern). The display list is plain text, one command per line, with colors and boxes.
- **Pictures:** the 2D renderer draws a display list offscreen on any device (`CaptureTests` does it on lavapipe and compares with PNG references at a stated tolerance).
  Look at the capture at two UI scales.

### Test your screen with the harness

The unit-style test above builds a tree by hand. A game also gets tests as text: a scenario file drives the real game (the host, the screens, the same input path as a player) and
asserts on the UI, the state and a captured frame. `docs/driving-a-game.md` has the whole story; for a screen it looks like this, using the ids you gave your components:

```yaml
name: buy-with-the-pad
window: 1280x720
steps:
  - nav: down              # the d-pad (up down left right next previous); focus starts on the default element
  - nav: activate          # the confirm button; or: press: Enter, or: click: buy
  - click: filter
  - type: "sil"
  - step: 2                # frames, so the table rebinds
expect:
  - ui: { id: buy, enabled: true }
  - ui: { selector: .kit-row, count: 1 }      # the filter left one row; rows are recycled, so this counts the rows that exist
  - ui: { id: confirm, visible: false }
  - state: { entity: Shop, property: Gold, equals: 10 }
  - capture: { reference: refs/market.png, size: 1280x720, tolerance: 2 }
```

Run it with `onegame test --json`. The expectation keys for the UI are `id` or `selector` plus `text`, `contains`, `count`, `visible`, `enabled`, `focused`; to check a role
or a value, run `ui_tree` over the live connection or `DumpJson` in a unit test. A scenario is the right tool for flows (open the dialog, press Escape, check focus is back); the unit test is
the right tool for boxes and display-list goldens.

## 13. Pitfalls

- **One parent per element.** Build each element once; use `Ui.Each`/`BindChildren` to repeat.
- **Ids must be unique** within what you query; give components ids so their parts get stable ids too.
- **Rows have one fixed height** in lists and tables. Variable-height rows are not supported.
- **Frames after input.** Layout feeds back into scroll bars, focus requests and tooltips for a frame or two: tests should `Settle()`; a real game never notices.
- **Do not write game state in a binding** and do not call `ui.Focus` inside one (use `RequestFocus`). Bindings read.
- **Do not allocate per frame** in bindings (section 4). A capturing lambda inside a hot method is an allocation even on the path that returns early.
- **Percent lengths** are of the containing box; `Length.Ui` follows the UI scale and `Length.Px` does not. Mixing them is how a layout breaks at 4K.
- **Selectors with combinators** restyle subtrees when state changes: fine for screens, avoid on thousands of rows.
- **A hidden element is gone:** `IsVisible = false` removes it from layout, input and focus; `Visibility(Hidden)` keeps its space.
