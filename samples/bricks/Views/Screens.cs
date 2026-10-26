using OneGame.Core;
using OneGame.Host;
using OneGame.Platform;
using OneGame.UI;
using OneGame.UI.Kit;

namespace Bricks.Views;

/// <summary>The title, high-score and settings screens, built from the component kit.</summary>
[View]
internal static class Screens
{
    /// <summary>A full-screen column on the night background, with the kit's tooltip layer attached.</summary>
    private static Element Page(Session s, Element content, Length? top = null) =>
        s.Kit.AttachTo(Ui.Box(content)
            .Class("screen")
            .Style(st => st.Position(PositionMode.Absolute).Inset(0).Direction(FlexDirection.Column).JustifyContent(top is null ? Align.Center : Align.Start).AlignItems(Align.Center).Padding(top ?? Length.Ui(0), Length.Ui(0))));

    // ---- title -------------------------------------------------------------------------------------------------------

    public static Element Title(GameInstance game)
    {
        var s = Session.For(game);
        var buttons = new List<Element>
        {
            UiKit.Button("Play", () => { s.Click(); s.StartGame(); }, "play", ButtonKind.Primary).AsDefaultFocus(),
            UiKit.Button("High scores", () => { s.Click(); s.Open("scores"); }, "scores"),
            UiKit.Button("Settings", () => { s.Click(); s.Open("settings"); }, "settings"),
        };

        // A phone or a page has no program to quit (RunningOn knows Android even in a NativeAOT build).
        if (RunningOn.Desktop)
        {
            buttons.Add(UiKit.Button("Quit", () => game.Window.Close(), "quit"));
        }

        var menu = FocusHelpers.FocusGroup(
            Ui.Column([.. buttons]).Gap(Length.Ui(10)).Align(Align.Stretch).Width(Length.Ui(260)),
            horizontal: false);
        var content = Ui.Column(
            Ui.Label("BRICKS").Class("logo").Named("menu-title"),
            Ui.Label("Break the wall. Keep the ball in play.").Class("dim"),
            Ui.Label(Levels.Count.ToString(CultureInfo.InvariantCulture) + " levels").Class("dim").Named("level-count"),
            menu,
            Ui.Label(string.Empty).Class("dim").Named("best").BindText(() => s.Profile.Best, v => v == 0 ? string.Empty : "Best score " + v.ToString(CultureInfo.InvariantCulture)))
            .Gap(Length.Ui(16))
            .Align(Align.Center);
        // Anchored to the top so that the title looks the same with and without the Quit button (a page or a phone has none).
        return Page(s, content, Length.Ui(130));
    }

    // ---- high scores -------------------------------------------------------------------------------------------------

    public static Element HighScores(GameInstance game)
    {
        var s = Session.For(game);
        var p = s.Profile;
        var rows = new List<(int Place, ScoreEntry Entry)>();
        var shown = -1L;
        IReadOnlyList<(int Place, ScoreEntry Entry)> Rows()
        {
            // Rebuilt when the table changes (its length and the sum of its scores are enough to notice that here).
            var stamp = p.Scores.Count * 1_000_000_007L;
            foreach (var entry in p.Scores)
            {
                stamp += entry.Score;
            }

            if (shown != stamp)
            {
                shown = stamp;
                rows.Clear();
                for (var i = 0; i < p.Scores.Count; i++)
                {
                    rows.Add((i + 1, p.Scores[i]));
                }
            }

            return rows;
        }

        var table = UiKit.Table<(int Place, ScoreEntry Entry)>(
            [
                new TableColumn<(int Place, ScoreEntry Entry)>("#", r => r.Place.ToString(CultureInfo.InvariantCulture), ColumnWidth.Fixed(Length.Ui(50)), sortable: false, alignEnd: true),
                new TableColumn<(int Place, ScoreEntry Entry)>("Name", r => r.Entry.Name, ColumnWidth.Flex(1), sortable: false),
                new TableColumn<(int Place, ScoreEntry Entry)>("Score", r => r.Entry.Score.ToString(CultureInfo.InvariantCulture), ColumnWidth.Fixed(Length.Ui(90)), sortable: false, alignEnd: true),
                new TableColumn<(int Place, ScoreEntry Entry)>("Level", r => r.Entry.Victory ? "All" : r.Entry.Level.ToString(CultureInfo.InvariantCulture), ColumnWidth.Fixed(Length.Ui(70)), sortable: false, alignEnd: true),
            ],
            Rows,
            Length.Ui(30),
            id: "table");
        table.Height(Length.Ui(Profile.TableSize * 30 + 36));

        var confirm = UiKit.ConfirmDialog("Clear the table?", "This removes every score.", () =>
        {
            p.ClearScores();
            s.SaveProfile();
        }, confirmText: "Clear", cancelText: "Keep", id: "clear-confirm", danger: true);

        var content = Ui.Column(
            Ui.Label("High scores").Class("title"),
            Ui.Box(table).Width(Length.Ui(420)),
            Ui.Label(string.Empty).Class("dim").Named("empty").BindText(() => p.Scores.Count, v => v == 0 ? "No scores yet. Play a game." : string.Empty),
            FocusHelpers.FocusGroup(
                Ui.Row(
                    UiKit.Button("Back", () => { s.Click(); s.Back(); }, "back", ButtonKind.Primary).AsDefaultFocus(),
                    UiKit.Button("Clear", confirm.Open, "clear", ButtonKind.Danger))
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

    // ---- settings ----------------------------------------------------------------------------------------------------

    public static Element Settings(GameInstance game)
    {
        var s = Session.For(game);
        var p = s.Profile;

        var volume = UiKit.Slider(() => p.Volume, v =>
        {
            p.Volume = v;
            s.SaveProfile();
        }, 0, 10, 1, "volume");
        var size = UiKit.RadioGroup(
            [new Choice<int>(0, "Auto"), new Choice<int>(1, "100%"), new Choice<int>(2, "125%"), new Choice<int>(3, "150%"), new Choice<int>(4, "200%")],
            () => p.UiScaleIndex,
            v =>
            {
                p.UiScaleIndex = v;
                s.ApplyUiScale();
                s.SaveProfile();
            },
            "ui-size",
            horizontal: true);

        var rows = Ui.Column(
            Row("Sound volume", volume),
            Row("UI size", size),
            Ui.Label("Saved in: " + s.Data.Description).Class("dim").Named("save-location"))
            .Gap(Length.Ui(14));

        var content = Ui.Column(
            Ui.Label("Settings").Class("title"),
            UiKit.Card(null, rows).Width(Length.Ui(560)),
            UiKit.Button("Back", () => { s.Click(); s.Back(); }, "back", ButtonKind.Primary))
            .Gap(Length.Ui(16))
            .Align(Align.Center);
        content.On(UiEventKind.Cancel, (_, _) =>
        {
            s.Back();
            return true;
        });
        return Page(s, content);
    }

    private static Element Row(string label, Element control) =>
        Ui.Row(Ui.Label(label).Width(Length.Ui(130)), control.Grow()).Gap(Length.Ui(14)).Align(Align.Center);
}
