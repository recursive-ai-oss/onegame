# Pages: screens written in HTML and CSS

A **page** is a screen drawn by [OneRender](https://github.com/recursive-ai-oss/onerender-csharp): an HTML and CSS document (or a tree built in C#) that OneRender lays out, styles, animates and hit-tests, and that onegame draws through the render graph over your game. `OneGame.UI` stays: a game can use either, per screen, or both at once. Solitaire's settings screen is a page (`samples/solitaire/Views/SettingsPage.cs`); its menu, statistics and table are still `OneGame.UI`.

Pages are **view code**. A handler reads state and sends commands, never writes state, and showing a page changes no hash.

## Page or OneGame.UI?

| Reach for a page when | Reach for `OneGame.UI` when |
|---|---|
| you want real CSS: grid, gradients, shadows, filters, transitions, animations, blend modes | you want the component kit (tables, tabs, tooltips, dialogs, lists, text input with its focus rules) |
| the screen is mostly layout and text, or you already have the HTML | the screen needs focus navigation by controller across custom widgets |
| you want native form controls (radio buttons, checkboxes, sliders, text fields) with no code | the game must run in a browser (see the limits below) |

## An HTML page

Build the page when its screen opens. `Show` replaces a page of the same name, so a screen that is built again simply remakes it.

```csharp
using OneGame.Host;
using OneGame.Pages;
using OneGame.UI;
using OneRender.Interaction;
using PagesGame;   // Meter: the game's own entity, with an Add command

public static class MenuScreen
{
    private const string Html = """
        <body style="margin:0;font:16px Body;color:#e8ecf4">
          <h1 style="margin:10px">Menu</h1>
          <button id="start" style="width:120px;height:36px">Start</button>
        </body>
        """;

    public static Element Build(GameInstance game)
    {
        var fonts = new PageFonts(game.Fonts);                       // the same faces OneGame.Text draws with
        fonts.Add(File.ReadAllBytes("fonts/Body.ttf"), "Body");      // the page says: font: 16px Body

        game.Pages.Show("menu", PageScreen.FromHtml(Html, new PageOptions(fonts)
        {
            // Runs every time the page is made, which includes after a window resize or a UI scale change.
            Setup = s => s.Interactive!.Events.AddEventListener(
                EventTarget.Of(s.Interactive.FindElement("start")), "click", _ => Meter.Of(game.World).Send.Add(1)),
        }));
        return Ui.Column();   // the screen's OneGame.UI side: empty, or the felt behind the page
    }
}
```

Things worth knowing:

- Sizes are CSS pixels at the UI scale: a 620 px wide card is 620 `OneGame.UI` pixels wide, and a 1.5 scale draws it at 930 device pixels.
- The page is transparent where you give it no background, and clicks on a transparent part pass through to what is under it.
- An HTML page cannot resize in place, so it is made again when the window size or UI scale changes. Focus, scroll position and form values are lost then. Put what the page shows in your own state (the profile, the settings object) and have `Setup` apply it: set control values with `SetChecked` and `SetRangeValue`, and focus the first control with `Focus`. `Setup`'s changes are painted before the first frame.
- Hide a page by calling `game.Pages.Remove("menu")` when another screen opens (the screen builders of the other screens are the place), or `SetVisible` to keep it.
- The document is read-only after it is made: you cannot change a paragraph's text from a handler. Show a changing value in a form control (a range or a field), build the page again, or use a builder page.

## Handlers send commands

A DOM listener registered with `page.Events.AddEventListener` is view code. It may read state, send commands, and change the player's own settings and profile. It may not write synced state or call sim-only methods; the analyzer says so at build time:

```
error OG0008: View code cannot change simulation state: 'Table.NewGame' changes state.
Views read state and send commands; use entity.Send.<Command>(...) instead.
```

That holds for lambdas, method groups and local functions passed to `AddEventListener`. A command sent from a handler runs on the next tick, like any command from a view.

Solitaire's handlers save the profile as the player changes a control:

```csharp
using OneGame.Pages;
using OneRender.Interaction;

public static class Handlers
{
    public static void Wire(InteractivePage page, Action<int> setMusic, Action back)
    {
        page.Events.AddEventListener(EventTarget.Of(page.FindElement("music")), "input", _ => setMusic((int)page.GetRangeValue(page.FindElement("music"))));
        page.Events.AddEventListener(EventTarget.Of(page.FindElement("back")), "click", _ => back());
    }
}
```

## A builder page

A page can also be a `StyledTree` built in C#, with no HTML. It suits a HUD that changes every frame: edit the tree and the page redraws on the next frame, and the usual edits (text, state, style) allocate nothing.

```csharp
using OneGame.Host;
using OneGame.Pages;
using OneRender.Style;
using OneRender.Tree;

public static class HudScreen
{
    public static void Show(GameInstance game, PageFonts fonts)
    {
        var tree = new StyledTree();
        var root = tree.AddRoot("div", new StyleDeclaration().Set("width", "320px").Set("height", "40px").Set("font", "18px Body").Set("color", "#ffd040"));
        var gold = tree.AddElement(root, "span");
        tree.AddText(gold, "Gold 0");
        tree.SetName(gold, "gold");
        game.Pages.Show("hud", PageScreen.FromTree(tree, new PageOptions(fonts)));
    }

    public static void Update(PageSet pages, int gold)
    {
        var tree = pages.Get("hud")!.Live!.Styled;
        tree.SetText(tree.FindByName("gold"), $"Gold {gold}");
    }
}
```

A builder page draws and can be found by name, but it takes no pointer, key or text input: the game sends input to its nodes itself.

## Images and SVG

`PageImages` puts raster and SVG files in the game's art atlas and gives pages addresses to name. SVG is drawn at the size the page shows it, with `currentColor` following the CSS `color`.

```csharp
using OneGame.Host;
using OneGame.Pages;

public static class Art
{
    public static PageOptions Options(GameInstance game, PageFonts fonts)
    {
        var images = new PageImages(game.Art);
        images.Add("coin.png", File.ReadAllBytes("art/coin.png"));
        images.Add("heart.svg", File.ReadAllBytes("art/heart.svg"));
        return new PageOptions(fonts) { Images = images };   // <img src="coin.png"> and background-image: url(heart.svg)
    }
}
```

## Finding pages in scenarios and the harness

`ui_query`, `ui_click` and `ui_invoke`, and the scenario steps `click:`, `invoke:` and `ui:` that use them, look in the `OneGame.UI` tree first and then in the shown pages, **by name**: an element's `id`, a form control's `name`, or `#name`. A click sends real pointer events through the platform queue at the element's centre, so hover, focus, transitions and handlers run as for a player. A controller button with a page focused acts like the keyboard: d-pad up and down move between controls (as Shift+Tab and Tab), left and right move along sliders and radio groups, and the south button presses.

```yaml
name: settings-pad
steps:
  - screen: settings
  - step: 3
  - nav: down            # focus moves from Draw 1 to Draw 3
  - nav: south           # the south button presses it
  - nav: down
  - nav: south           # the checkbox
  - nav: down
  - nav: dpad_right      # Music volume goes up one step
  - nav: dpad_right
  - step: 3
  - expect:
      capture: { reference: refs/settings-pad-720p.png, size: 1280x720, tolerance: 2, max-diff: 0.001 }
```

`click: draw-3` and `expect: { ui: { id: start, text: Start } }` work the same way, as in `tests/PagesGame/scenarios/menu.yaml`.

A capture draws the page. Tolerances for text edges follow the rest of the game (lavapipe is the reference); look at the frame before keeping it as the reference.

## Limits and what is not there yet

- **Browsers.** OneRender stops inside the browser's .NET runtime when a page is made, so pages do not run on the web target yet. Solitaire keeps its kit settings screen for browsers (`OperatingSystem.IsBrowser()`). Pages have run on Linux (lavapipe); Metal, WebGPU, Windows and phones have not run them.
- **Native control looks.** Radio buttons, checkboxes and sliders keep OneRender's built-in look; only their accent colour is yours (`accent-color`). Their unfilled parts are light, which sits oddly on a dark card. Style your own with `appearance: none`.
- **No live text.** An HTML page's text cannot change after it is made (see above).
- **No text-input method yet.** Typing works; composed text from an input method and clipboard paste do not reach page fields.
- **Tab order.** A radio group's buttons are each a tab stop when set from code, so the controller visits all of them.
- **Cost.** OneRender's cost is in its own notes; a steady page allocates nothing in the adapter. Bounds on SVG keys, images, instances and clips are in `docs/interfaces/onerender.md`.
