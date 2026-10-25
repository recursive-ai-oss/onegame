using OneGame.UI;

namespace Lanterns.Views;

/// <summary>A handful of colours and the rules that apply them. Screens name classes, never colours.</summary>
internal static class Theme
{
    public static readonly Color Sea = Color.Rgb(0x16324A);
    public static readonly Color Sand = Color.Rgb(0x3E5A47);
    public static readonly Color Bridge = Color.Rgb(0x8A6B45);
    public static readonly Color Panel = Color.Rgb(0x1F2833);
    public static readonly Color Text = Color.Rgb(0xF2F4F7);
    public static readonly Color Muted = Color.Rgb(0xA9B3C1);
    public static readonly Color Accent = Color.Rgb(0xF2C14E);
    public static readonly Color Good = Color.Rgb(0x6FD08C);
    public static readonly Color Bad = Color.Rgb(0xE5626C);
    public static readonly Color Me = Color.Rgb(0xFFFFFF);
    public static readonly Color Other = Color.Rgb(0xC9D3E0);

    /// <summary>Lantern colours: red, blue, gold.</summary>
    public static readonly Color[] Lantern = [Color.Rgb(0xE5626C), Color.Rgb(0x5AA9E6), Color.Rgb(0xF2C14E)];

    public static StyleSheet Sheet() => new StyleSheet()
        .Rule(".screen", s => s.Background(Sea).TextColor(Text))
        .Rule(".bar", s => s.Background(Panel).Padding(Length.Ui(0), Length.Ui(14)).Gap(Length.Ui(18)).AlignItems(Align.Center))
        .Rule(".side", s => s.Background(Panel).Padding(Length.Ui(10)).Gap(Length.Ui(6)))
        .Rule(".stat", s => s.FontWeight(700))
        .Rule(".dim", s => s.TextColor(Muted))
        .Rule(".good", s => s.TextColor(Good))
        .Rule(".bad", s => s.TextColor(Bad))
        .Rule(".heading", s => s.FontSize(Length.Ui(18)).FontWeight(700));
}
