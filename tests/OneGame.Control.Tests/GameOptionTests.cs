using System.Text.Json;
using OneGame.Core;
using OneGame.Host;
using OneGame.UI;

namespace OneGame.Control.Tests;

/// <summary>A game with no entities that takes two options of its own.</summary>
public sealed class OptionGame : GameDefinition
{
    public override string Name => "options";

    public override IReadOnlyList<string> OptionNames => ["level", "quiet"];

    public string? SeenLevel { get; private set; }

    public override void RegisterEntities(EntityRegistry registry)
    {
    }

    public override void Setup(World world)
    {
    }

    public override void DefineScreens(ScreenSet screens) => screens.Add("main", game =>
    {
        SeenLevel = game.Args.Get("level");
        return Ui.Column(Ui.Label("x"));
    });
}

public class GameOptionTests
{
    private static (int Code, string Out, string Err) Run(GameDefinition game, params string[] args)
    {
        var o = new StringWriter();
        var e = new StringWriter();
        var code = GameApp.Run(game, ["--headless", "--gpu", "off", "--json", "--frames", "1", .. args], o, e);
        return (code, o.ToString(), e.ToString());
    }

    [Fact]
    public void AReleaseBuildPassesTheGamesOwnOptionsThrough()
    {
        var saved = GameApp.DevTools;
        GameApp.DevTools = null;
        try
        {
            var game = new OptionGame();
            var (code, text, _) = Run(game, "--level", "3", "--quiet");
            Assert.Equal(0, code);
            using var doc = JsonDocument.Parse(text);
            var seen = doc.RootElement.GetProperty("gameOptions");
            Assert.Equal("3", seen.GetProperty("level").GetString());
            Assert.Equal("", seen.GetProperty("quiet").GetString());
            Assert.Equal("3", game.SeenLevel);
        }
        finally
        {
            GameApp.DevTools = saved;
        }
    }

    [Fact]
    public void AMisspeltEngineOrDevOptionIsStillRefusedInAReleaseBuild()
    {
        var saved = GameApp.DevTools;
        GameApp.DevTools = null;
        try
        {
            var (code, text, _) = Run(new OptionGame(), "--level", "3", "--scenaro", "x.yaml");
            Assert.Equal(ExitCodes.NoDevTools, code);
            Assert.Contains("--scenaro", text, StringComparison.Ordinal);
            Assert.Contains("--level", text, StringComparison.Ordinal);
        }
        finally
        {
            GameApp.DevTools = saved;
        }
    }

    [Fact]
    public void AnOptionNameWithDashesIsRefused()
    {
        var (code, text, _) = Run(new BadGame());
        Assert.Equal(ExitCodes.Usage, code);
        Assert.Contains("bad_game_option", text, StringComparison.Ordinal);
    }

    private sealed class BadGame : OptionGameBase
    {
        public override IReadOnlyList<string> OptionNames => ["--level"];
    }
}

internal abstract class OptionGameBase : GameDefinition
{
    public override string Name => "bad";

    public override void RegisterEntities(EntityRegistry registry)
    {
    }

    public override void Setup(World world)
    {
    }

    public override void DefineScreens(ScreenSet screens)
    {
    }
}
