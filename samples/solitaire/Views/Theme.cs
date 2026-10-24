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

    /// <summary>The style sheet for every screen.</summary>
    public static StyleSheet Sheet() => new StyleSheet()
        .Rule(".screen", s => s.Background(Felt).TextColor(Text).FontSize(Length.Ui(16)).LineHeight(Length.Auto))
        .Rule(".bar", s => s.Background(FeltDark).Padding(Length.Ui(0), Length.Ui(14)).Gap(Length.Ui(18)).Align(Align.Center))
        .Rule(".stat", s => s.FontSize(Length.Ui(16)).FontWeight(700))
        .Rule(".dim", s => s.TextColor(Muted))
        .Rule(".btn", s => s.Padding(Length.Ui(9), Length.Ui(18)).Background(Panel).Radius(Length.Ui(8)).Border(Length.Px(1), PanelLine).MinWidth(Length.Ui(92)).FontWeight(700))
        .Rule(".btn:hover", s => s.Background(PanelRaised))
        .Rule(".btn:focus", s => s.Outline(Length.Ui(3), Accent, Length.Ui(2)))
        .Rule(".btn:disabled", s => s.Opacity(0.4f))
        .Rule(".btn.primary", s => s.Background(Accent).TextColor(AccentText).Border(Length.Px(1), Accent))
        .Rule(".btn.primary:hover", s => s.Background(Color.Rgb(0xF7D27C)))
        .Rule(".scrim", s => s.Background(Scrim))
        .Rule(".panel", s => s.Background(Panel).Radius(Length.Ui(14)).Border(Length.Px(1), PanelLine).Padding(Length.Ui(24)).Gap(Length.Ui(14)).Shadow(Length.Px(0), Length.Ui(10), Length.Ui(30), Length.Px(0), new Color(0, 0, 0, 140)))
        .Rule(".title", s => s.FontSize(Length.Ui(30)).FontWeight(700))
        .Rule(".heading", s => s.FontSize(Length.Ui(20)).FontWeight(700))
        .Rule(".row-head", s => s.TextColor(Muted).FontWeight(700))
        .Rule(".cell", s => s.Padding(Length.Ui(5), Length.Ui(8)))
        .Rule(".odd", s => s.Background(new Color(255, 255, 255, 12)))
        .Rule(".won", s => s.TextColor(Good))
        .Rule(".lost", s => s.TextColor(Muted))
        .Rule(".check", s => s.Size(Length.Ui(22), Length.Ui(22)).Radius(Length.Ui(5)).Border(Length.Px(2), PanelLine).Background(Panel))
        .Rule(".check.on", s => s.Background(Accent).Border(Length.Px(2), Accent))
        .Rule(".track", s => s.Height(Length.Ui(10)).Radius(Length.Ui(5)).Background(PanelLine))
        .Rule(".fill", s => s.Height(Length.Ui(10)).Radius(Length.Ui(5)).Background(Accent));
}
