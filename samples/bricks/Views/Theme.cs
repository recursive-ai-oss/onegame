using OneGame.UI;

namespace Bricks.Views;

/// <summary>
/// The look of the game: a handful of colours and the style sheet that applies them. Screens name classes (<c>screen</c>, <c>bar</c>,
/// <c>title</c>) and never colours, so a different look is a different sheet. The court's pieces are drawn from the same colours.
/// </summary>
internal static class Theme
{
    public static readonly Color Night = Color.Rgb(0x141A2E);
    public static readonly Color NightDark = Color.Rgb(0x0E1222);
    public static readonly Color Court = Color.Rgb(0x1B2340);
    public static readonly Color CourtLine = Color.Rgb(0x3A4670);
    public static readonly Color Text = Color.Rgb(0xF2F4F7);
    public static readonly Color Muted = Color.Rgb(0xA3ADC4);
    public static readonly Color Accent = Color.Rgb(0xF2C14E);
    public static readonly Color Good = Color.Rgb(0x6FD08C);
    public static readonly Color Scrim = new(6, 9, 20, 190);

    /// <summary>The colours of the six plain bricks, index 1 to 6 (index 0 is unused).</summary>
    public static readonly Color[] Tints =
    [
        Color.White,
        Color.Rgb(0xE5626C),
        Color.Rgb(0xF2A04E),
        Color.Rgb(0xF2D14E),
        Color.Rgb(0x6FD08C),
        Color.Rgb(0x4EC3F2),
        Color.Rgb(0xA78BFA),
    ];

    /// <summary>Tough bricks darken from light to dark steel with the hits they still take: index is the hits left (1 to 3).</summary>
    public static readonly Color[] Steel =
    [
        Color.White,
        Color.Rgb(0xD3DCEB),
        Color.Rgb(0x8E9DB5),
        Color.Rgb(0x5D6D86),
    ];

    public static readonly Color Solid = Color.Rgb(0x7A8294);

    /// <summary>The game's own rules, added after the component kit's theme. Buttons, dialogs, tables and the rest take their look from the kit's tokens.</summary>
    public static StyleSheet Sheet() => new StyleSheet()
        .Rule(".screen", s => s.Background(Night).TextColor(Text))
        .Rule(".bar", s => s.Background(NightDark).Padding(Length.Ui(0), Length.Ui(14)).Gap(Length.Ui(16)).AlignItems(Align.Center))
        .Rule(".stat", s => s.FontWeight(700))
        .Rule(".dim", s => s.TextColor(Muted))
        .Rule(".best", s => s.TextColor(Good))
        .Rule(".title", s => s.FontSize(Length.Ui(34)).FontWeight(700))
        .Rule(".logo", s => s.FontSize(Length.Ui(64)).FontWeight(700).TextColor(Accent))
        .Rule(".heading", s => s.FontSize(Length.Ui(20)).FontWeight(700))
        .Rule(".banner", s => s.FontSize(Length.Ui(30)).FontWeight(700).TextColor(Accent))
        .Rule(".court", s => s.Background(Court).Border(Length.Px(3), CourtLine));
}
