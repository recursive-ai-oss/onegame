using System.Text.Json;
using OneGame.Host;
using OneGame.Live;
using OneGame.UI;
using OneGame.UI.Kit;

namespace OneGame.Testing.Tests;

/// <summary>A game that installs the kit's theme, like a real one.</summary>
public class KitShop : ShopGame
{
    public override void Style(UiTree ui) => _ = new KitHost(ui);
}

/// <summary>A game that installs the kit's theme and then sets its accent text to a color it cannot be read in.</summary>
public sealed class UnreadableShop : KitShop
{
    public override void Style(UiTree ui)
    {
        base.Style(ui);
        ui.AddStyleSheet(new StyleSheet().Rule(":root", s => s.Token("--kit-accent-text", Color.Hex("#d9a441"))));
    }
}

/// <summary>The theme lint as a game author runs it: <c>--lint-theme</c> on the game (what <c>onegame lint-theme</c> starts), and the <c>theme_lint</c> control operation.</summary>
public class ThemeLintTests
{
    private static (int Exit, JsonElement Result) Lint(GameDefinition game)
    {
        Dev.Enable();
        var output = new StringWriter();
        var exit = GameApp.Run(game, ["--headless", "--gpu", "off", "--json", "--lint-theme"], output, TextWriter.Null);
        return (exit, JsonDocument.Parse(output.ToString()).RootElement.GetProperty("result").Clone());
    }

    [Fact]
    public void AGameOnTheKitThemePasses()
    {
        var (exit, result) = Lint(new KitShop());
        Assert.Equal(0, exit);
        Assert.True(result.GetProperty("passed").GetBoolean());
        Assert.True(result.GetProperty("checked").GetInt32() > 30);
        Assert.Equal(0, result.GetProperty("failures").GetArrayLength());
    }

    [Fact]
    public void AnOverrideThatBreaksLegibilityIsNamedWithItsPair()
    {
        var (exit, result) = Lint(new UnreadableShop());
        Assert.Equal(1, exit);
        Assert.False(result.GetProperty("passed").GetBoolean());
        var worst = result.GetProperty("failures")[0];
        Assert.Equal("--kit-accent-text", worst.GetProperty("foreground").GetString());
        Assert.Equal("--kit-accent", worst.GetProperty("background").GetString());
        Assert.Equal("#d9a441", worst.GetProperty("foregroundColor").GetString());
        Assert.Equal(4.5, worst.GetProperty("required").GetDouble());
        Assert.True(worst.GetProperty("ratio").GetDouble() < 1.5);
    }

    [Fact]
    public void AGameWithoutTheKitThemeReportsMissingTokens()
    {
        var (exit, result) = Lint(new ShopGame());
        Assert.Equal(1, exit);
        Assert.Equal("missing", result.GetProperty("failures")[0].GetProperty("foregroundColor").GetString());
    }
}
