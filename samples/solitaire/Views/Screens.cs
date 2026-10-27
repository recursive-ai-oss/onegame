using OneGame.Core;
using OneGame.Host;
using OneGame.UI;
using OneGame.UI.Kit;

namespace Solitaire.Views;

/// <summary>The main menu, statistics and settings screens, built from the component kit.</summary>
[View]
internal static class Screens
{
    private static readonly string[] Suits = ["suit-spade", "suit-heart", "suit-club", "suit-diamond"];

    /// <summary>A full-screen column on the felt, with the kit's tooltip layer attached.</summary>
    private static Element Page(Session s, Element content) =>
        s.Kit.AttachTo(Ui.Box(content)
            .Class("screen")
            .Style(st => st.Position(PositionMode.Absolute).Inset(0).JustifyContent(Align.Center).AlignItems(Align.Center)));

    // ---- main menu ---------------------------------------------------------------------------------------------------

    public static Element Menu(GameInstance game)
    {
        SettingsPage.Hide(game);
        var s = Session.For(game);
        var cards = Ui.Row(Suits.Select(id => Ui.Image(id, 40, 40).Size(Length.Ui(40), Length.Ui(40))).ToArray()).Gap(Length.Ui(14));
        var quit = UiKit.Button("Quit", () => game.Window.Close(), "quit");
        var buttons = FocusHelpers.FocusGroup(
            Ui.Column(
                UiKit.Button("Continue", s.Continue, "continue", ButtonKind.Primary).AsDefaultFocus().BindEnabled(() => s.CanContinue),
                UiKit.Button("New game - draw 1", () => s.StartGame(1), "new-draw1"),
                UiKit.Button("New game - draw 3", () => s.StartGame(3), "new-draw3"),
                UiKit.Button("Statistics", () => s.Open("stats"), "stats"),
                UiKit.Button("Settings", () => s.Open("settings"), "settings"),
                quit)
                .Gap(Length.Ui(10))
                .Align(Align.Stretch)
                .Width(Length.Ui(300)),
            horizontal: false);
        var content = Ui.Column(
            cards,
            Ui.Label("Solitaire").Class("title").Named("menu-title"),
            Ui.Label("Klondike").Class("dim"),
            buttons,
            Ui.Label(string.Empty).Class("dim").Named("menu-notice").BindText(() => s.Notice ?? string.Empty))
            .Gap(Length.Ui(14))
            .Align(Align.Center);
        return Page(s, content);
    }

    // ---- statistics --------------------------------------------------------------------------------------------------

    public static Element Statistics(GameInstance game)
    {
        SettingsPage.Hide(game);
        var s = Session.For(game);
        var p = s.Profile;
        var rows = new List<GameRecord>();
        var shown = -1;
        IReadOnlyList<GameRecord> Rows()
        {
            if (shown != p.History.Count)
            {
                shown = p.History.Count;
                rows.Clear();
                for (var i = p.History.Count - 1; i >= 0; i--)
                {
                    rows.Add(p.History[i]);
                }
            }

            return rows;
        }

        var table = UiKit.Table<GameRecord>(
            [
                new TableColumn<GameRecord>("Game", r => r.Deal.ToString(CultureInfo.InvariantCulture), ColumnWidth.Fixed(Length.Ui(70)), sortable: false, alignEnd: true),
                new TableColumn<GameRecord>("Draw", r => r.DrawMode.ToString(CultureInfo.InvariantCulture), ColumnWidth.Fixed(Length.Ui(60)), sortable: false, alignEnd: true),
                new TableColumn<GameRecord>("Result", r => r.Won ? "Won" : "Unfinished", ColumnWidth.Flex(1), sortable: false),
                new TableColumn<GameRecord>("Score", r => r.Score.ToString(CultureInfo.InvariantCulture), ColumnWidth.Fixed(Length.Ui(80)), sortable: false, alignEnd: true),
                new TableColumn<GameRecord>("Moves", r => r.Moves.ToString(CultureInfo.InvariantCulture), ColumnWidth.Fixed(Length.Ui(80)), sortable: false, alignEnd: true),
                new TableColumn<GameRecord>("Time", r => TableView.FormatClock(r.Ticks / 60), ColumnWidth.Fixed(Length.Ui(80)), sortable: false, alignEnd: true),
            ],
            Rows,
            Length.Ui(28),
            id: "history");
        table.Height(Length.Ui(250));

        var confirm = UiKit.ConfirmDialog("Reset statistics?", "This clears your games, wins and records.", () =>
        {
            p.ResetStatistics();
            s.SaveProfile();
        }, confirmText: "Reset", cancelText: "Keep", id: "reset-confirm", danger: true);

        var summary = UiKit.Card(
            "Totals",
            Stat("Games played", "stat-games", () => p.Games, v => v.ToString(CultureInfo.InvariantCulture)),
            Stat("Games won", "stat-wins", () => p.Wins, v => v.ToString(CultureInfo.InvariantCulture)),
            Stat("Win rate", "stat-rate", () => p.Games == 0 ? -1 : p.Wins * 100 / p.Games, v => v < 0 ? "-" : v.ToString(CultureInfo.InvariantCulture) + "%"),
            Stat("Best time", "stat-time", () => p.BestTicks, v => v == 0 ? "-" : TableView.FormatClock(v / 60)),
            Stat("Best score", "stat-score", () => p.BestScore, v => v == 0 ? "-" : v.ToString(CultureInfo.InvariantCulture)))
            .Width(Length.Ui(280));

        var content = Ui.Column(
            Ui.Label("Statistics").Class("title"),
            Ui.Row(
                summary,
                Ui.Column(Ui.Label("Recent games").Class("heading"), table).Gap(Length.Ui(8)).Grow().MinWidth(Length.Px(0)))
                .Gap(Length.Ui(20))
                .Width(Length.Ui(860)),
            FocusHelpers.FocusGroup(
                Ui.Row(
                    UiKit.Button("Back", s.Back, "back", ButtonKind.Primary).AsDefaultFocus(),
                    UiKit.Button("Reset statistics", confirm.Open, "reset", ButtonKind.Danger))
                    .Gap(Length.Ui(10)),
                horizontal: true),
            confirm)
            .Gap(Length.Ui(16))
            .Align(Align.Center);
        content.On(UiEventKind.Cancel, (_, _) =>
        {
            s.Back();
            return true;
        });
        return Page(s, content);
    }

    private static Element Stat(string label, string id, Func<int> value, Func<int, string> format) =>
        Ui.Row(
            Ui.Label(label).Class("dim").Grow(),
            Ui.Label(string.Empty).Class("stat").Named(id).BindText(value, format))
            .Gap(Length.Ui(12));

    // ---- settings ----------------------------------------------------------------------------------------------------

    /// <summary>
    /// The settings screen is a OneRender page (<see cref="SettingsPage"/>) drawn over this felt. Escape with no control focused is the UI
    /// tree's cancel; with a control focused the page handles it.
    /// </summary>
    public static Element Settings(GameInstance game)
    {
        var s = Session.For(game);
        SettingsPage.Show(game);
        var content = Ui.Column();
        content.On(UiEventKind.Cancel, (_, _) =>
        {
            s.Back();
            return true;
        });
        return Page(s, content);
    }
}
