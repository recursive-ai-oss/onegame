namespace OneGame.UI.Kit.Tests;

public class ThemeTests
{
    private static Screen Make(out TradingGame game)
    {
        var s = new Screen();
        game = new TradingGame(200);
        TradingScreen.Build(s.Host, game, out _, out _);
        s.Settle();
        return s;
    }

    private static string Boxes(Screen s) => string.Join('|', s.Ui.Query("*").Select(e => e.AbsoluteBounds.ToString()));

    [Fact]
    public void SwappingTheThemeRecolorsTheSameTree()
    {
        var s = Make(out _);
        var buy = s.Get("buy");
        var count = s.Ui.Query("*").Count;
        var boxes = Boxes(s);
        var before = s.Ui.DisplayList.ToText();
        var version = s.Ui.DisplayList.Version;
        Assert.Contains("#d9a441", before, StringComparison.Ordinal);
        Assert.Contains("#14161a", before, StringComparison.Ordinal);
        Assert.DoesNotContain("#1f6fd1", before, StringComparison.Ordinal);

        s.Host.SetTheme(KitTheme.Create(KitTokens.Light));
        s.Settle();

        var after = s.Ui.DisplayList.ToText();
        Assert.True(s.Ui.DisplayList.Version > version);
        Assert.Contains("#1f6fd1", after, StringComparison.Ordinal);
        Assert.Contains("#eef0f4", after, StringComparison.Ordinal);
        Assert.DoesNotContain("#d9a441", after, StringComparison.Ordinal);
        Assert.DoesNotContain("#14161a", after, StringComparison.Ordinal);
        // Same elements, same boxes: only colors changed.
        Assert.Same(buy, s.Get("buy"));
        Assert.Equal(count, s.Ui.Query("*").Count);
        Assert.Equal(boxes, Boxes(s));
        // The command stream has the same shape: the same kinds in the same order.
        Assert.Equal(Kinds(before), Kinds(after));
    }

    [Fact]
    public void ATokenOverrideSheetChangesOnlyWhatItNames()
    {
        var s = Make(out _);
        var boxes = Boxes(s);
        s.Ui.AddStyleSheet(new StyleSheet().Rule(":root", st => st.Token("--kit-accent", Color.Hex("#ff0044")).Token("--kit-radius", Length.Px(12))));
        s.Settle();
        var list = s.Ui.DisplayList.ToText();
        Assert.Contains("#ff0044", list, StringComparison.Ordinal);
        Assert.DoesNotContain("#d9a441", list, StringComparison.Ordinal);
        Assert.Contains("radii=12,12,12,12", list, StringComparison.Ordinal);
        Assert.Contains("#2b303a", list, StringComparison.Ordinal); // untouched tokens keep their values
        Assert.Equal(boxes, Boxes(s));
    }

    [Fact]
    public void GameRulesAfterTheThemeWinAndSurviveAThemeSwap()
    {
        var s = Make(out _);
        s.Ui.AddStyleSheet(new StyleSheet().Rule(".kit-button.primary", st => st.Background(Color.Hex("#00aa00"))));
        s.Settle();
        Assert.Contains("#00aa00", s.Ui.DisplayList.ToText(), StringComparison.Ordinal);
        s.Host.SetTheme(KitTheme.Create(KitTokens.Light));
        s.Settle();
        Assert.Contains("#00aa00", s.Ui.DisplayList.ToText(), StringComparison.Ordinal);
    }

    [Fact]
    public void UiScaleScalesTheKitLayout()
    {
        var s = Make(out _);
        var tab1 = s.Get("tabs/tab/0").AbsoluteBounds;
        var row1 = s.Get("goods/row/0").Bounds;
        s.Ui.SetViewport(2560, 1440, 2);
        s.Settle();
        var tab2 = s.Get("tabs/tab/0").AbsoluteBounds;
        var row2 = s.Get("goods/row/0").Bounds;
        // Padding is in pixels (12 px) while the kit's own sizes are in UI units, so tab heights and row heights double.
        Assert.Equal(tab1.Height * 2, tab2.Height, 0.1f);
        Assert.Equal(row1.Height * 2, row2.Height, 0.01f);
        Assert.Equal(56, row2.Height, 0.01f);
        Assert.Equal(2, s.Ui.DisplayList.Scale);
    }

    private static string Kinds(string list) => string.Join(',', list.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.TrimStart().Split(' ')[0]));
}
