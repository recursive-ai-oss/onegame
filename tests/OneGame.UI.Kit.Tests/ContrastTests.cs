namespace OneGame.UI.Kit.Tests;

public class ContrastTests
{
    /// <summary>An amber-on-near-black theme in the spirit of a CRT terminal. Its accent text, dim text and borders are deliberately weak, the way a hand-picked theme often is.</summary>
    internal static KitTokens Amber { get; } = KitTokens.Dark with
    {
        Background = Color.Hex("#0b0905"),
        Panel = Color.Hex("#16110a"),
        Raised = Color.Hex("#261c0f"),
        Field = Color.Hex("#120e08"),
        Text = Color.Hex("#f3dfae"),
        TextDim = Color.Hex("#7a6840"),
        Accent = Color.Hex("#ffb000"),
        AccentHover = Color.Hex("#ffc640"),
        AccentText = Color.Hex("#8a6a1c"),
        Border = Color.Hex("#4a3a1c"),
        Focus = Color.Hex("#ffe9a8"),
        Hover = Color.Hex("#3a2a14"),
        Active = Color.Hex("#100b05"),
        Selection = Color.Hex("#5a4210"),
        SelectionText = Color.Hex("#ffd98a"),
        Scrollbar = Color.Hex("#4a3a1c"),
    };

    public static TheoryData<string> Themes => ["dark", "light", "amber"];

    internal static KitTokens Tokens(string name) => name switch
    {
        "dark" => KitTokens.Dark,
        "light" => KitTokens.Light,
        _ => Amber,
    };

    [Theory]
    [MemberData(nameof(Themes))]
    public void EveryReadableTextAndIndicatorPairPassesAa(string theme)
    {
        var report = ThemeLint.Check(Tokens(theme));
        Assert.True(report.Passed, theme + " fails: " + string.Join("; ", report.Failures.Select(f => $"{f.Name} {f.ForegroundColor} on {f.BackgroundColor} = {f.Ratio} (needs {f.Required})")));
        Assert.True(report.Checked > 30);
    }

    [Fact]
    public void ARunningUiIsLintedWithItsOwnTokenOverrides()
    {
        var s = new Screen();
        s.Settle();
        Assert.True(ThemeLint.Check(s.Ui).Passed);

        // A game that sets a token by hand skips the kit's repair, and the lint of the live UI names the pair.
        s.Ui.AddStyleSheet(new StyleSheet().Rule(":root", st => st.Token("--kit-accent-text", Color.Hex("#d9a441"))));
        s.Settle();
        var report = ThemeLint.Check(s.Ui);
        Assert.False(report.Passed);
        Assert.All(report.Failures, f => Assert.Equal("--kit-accent-text", f.Foreground));
        Assert.Contains(report.Failures, f => f.Name == "primary button text on accent" && f.Ratio < 1.1);
    }

    [Fact]
    public void ALintOfATreeWithoutTheKitThemeReportsMissingTokens()
    {
        var report = ThemeLint.Check(new UiTree());
        Assert.False(report.Passed);
        Assert.All(report.Failures, f => Assert.Equal(0, f.Ratio));
    }

    [Fact]
    public void WeakTokensAreRepairedJustEnough()
    {
        var weak = KitTokens.Dark with { AccentText = Color.Hex("#d9a441"), TextDim = Color.Hex("#40444c"), Focus = Color.Hex("#1e2128") };
        var palette = KitTheme.Palette(weak);
        Color Get(string n) => palette.First(p => p.Name == n).Color;
        Assert.True(KitContrast.Ratio(Get("--kit-accent-text"), Get("--kit-accent")) >= 4.5);
        Assert.True(KitContrast.Ratio(Get("--kit-text-dim"), Get("--kit-panel")) >= 4.5);
        Assert.True(KitContrast.Ratio(Get("--kit-focus"), Get("--kit-panel")) >= 3);
        // A token that already passes is never touched.
        Assert.Equal(KitTokens.Dark.Text, Get("--kit-text"));
        Assert.Equal(KitTokens.Dark.Accent, Get("--kit-accent"));
    }

    [Fact]
    public void ContrastMatchesTheWcagDefinition()
    {
        Assert.Equal(21, KitContrast.Ratio(Color.White, Color.Black), 3);
        Assert.Equal(1, KitContrast.Ratio(Color.Hex("#777777"), Color.Hex("#777777")), 3);
        Assert.Equal(4.48, KitContrast.Ratio(Color.Hex("#777777"), Color.White), 2);   // the well-known #777 on white
    }

    /// <summary>The state colors that make disabled and focused controls unambiguous, as numbers: how far each stands from the panel it sits on.</summary>
    [Theory]
    [MemberData(nameof(Themes))]
    public void DisabledIsFlatterThanEnabledAndFocusStandsOut(string theme)
    {
        var t = Tokens(theme);
        var p = KitTheme.Palette(t);
        Color Get(string n) => p.First(x => x.Name == n).Color;
        var panel = Get("--kit-panel");
        // A disabled fill is never farther from the panel than an enabled secondary or primary fill.
        var disabled = Math.Abs(KitContrast.Luminance(Get("--kit-disabled-fill")) - KitContrast.Luminance(panel));
        Assert.True(disabled <= Math.Abs(KitContrast.Luminance(Get("--kit-raised")) - KitContrast.Luminance(panel)) + 1e-9);
        Assert.True(KitContrast.Ratio(Get("--kit-disabled-fill"), panel) < KitContrast.Ratio(Get("--kit-accent"), panel));
        // Disabled text is dimmer than dim text but readable.
        Assert.True(KitContrast.Ratio(Get("--kit-disabled-text"), Get("--kit-disabled-fill")) < KitContrast.Ratio(Get("--kit-text-dim"), Get("--kit-disabled-fill")) + 1e-9);
        Assert.True(KitContrast.Ratio(Get("--kit-disabled-text"), Get("--kit-disabled-fill")) >= 3);
        // The ring against the panel is a strong edge.
        Assert.True(KitContrast.Ratio(Get("--kit-focus"), panel) >= 3);
    }
}
