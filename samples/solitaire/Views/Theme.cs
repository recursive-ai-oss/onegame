using OneGame.UI;

namespace Solitaire.Views;

/// <summary>
/// The look of the game: a handful of colours and the style sheet that applies them. Screens name classes (<c>btn</c>, <c>panel</c>,
/// <c>title</c>) and never colours, so a different look is a different sheet.
/// </summary>
internal static class Theme
{
    public static readonly Color Felt = Color.Rgb(0x1F5E3B);
    public static readonly Color FeltDark = Color.Rgb(0x174A2E);
    public static readonly Color Panel = Color.Rgb(0x1F2833);
    public static readonly Color PanelRaised = Color.Rgb(0x2B3745);
    public static readonly Color PanelLine = Color.Rgb(0x3D4B5C);
    public static readonly Color Text = Color.Rgb(0xF2F4F7);
    public static readonly Color Muted = Color.Rgb(0xA9B3C1);
    public static readonly Color Accent = Color.Rgb(0xF2C14E);
    public static readonly Color AccentText = Color.Rgb(0x1B1A12);
    public static readonly Color Good = Color.Rgb(0x6FD08C);
    public static readonly Color Bad = Color.Rgb(0xE5626C);
    public static readonly Color Scrim = new(8, 14, 12, 190);

    /// <summary>
    /// The game's own rules, added after the component kit's theme: the felt, the two bars, and text sizes. Buttons, dialogs, tables and the
    /// rest take their look from the kit's tokens.
    /// </summary>
    public static StyleSheet Sheet() => new StyleSheet()
        .Rule(".screen", s => s.Background(Felt).TextColor(Text))
        .Rule(".bar", s => s.Background(FeltDark).Padding(Length.Ui(0), Length.Ui(14)).Gap(Length.Ui(18)).AlignItems(Align.Center))
        .Rule(".stat", s => s.FontWeight(700))
        .Rule(".dim", s => s.TextColor(Muted))
        .Rule(".best", s => s.TextColor(Good))
        .Rule(".title", s => s.FontSize(Length.Ui(34)).FontWeight(700))
        .Rule(".heading", s => s.FontSize(Length.Ui(20)).FontWeight(700));
}
