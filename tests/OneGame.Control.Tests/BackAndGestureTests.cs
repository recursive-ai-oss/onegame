using System.Text.Json;
using OneGame.Core;
using OneGame.Host;
using OneGame.Platform;
using OneGame.UI;

namespace OneGame.Control.Tests;

[Collection("AudioService.Active")]
public class BackAndGestureTests
{
    private sealed class BackGame : GameDefinition
    {
        public int Backs;
        public bool Handle;

        public override string Name => "BackGame";

        public override void RegisterEntities(EntityRegistry registry)
        {
        }

        public override void Setup(World world)
        {
        }

        public override bool OnBack(GameInstance game)
        {
            Backs++;
            return Handle;
        }

        public override void DefineScreens(ScreenSet screens) => screens.Add("main", _ => Ui.Column(Ui.Label("x")));
    }

    private static GameInstance Create(BackGame game) => GameInstance.Create(game, new GameOptions { Headless = true, Gpu = GpuMode.Off });

    [Fact]
    public void BackReachesTheGameFirstAndAHandledBackKeepsTheAppOpen()
    {
        var def = new BackGame { Handle = true };
        using var game = Create(def);
        var surface = new ControlSurface(game);

        surface.Call("input_back", "{}");
        Assert.Equal(1, def.Backs);
        Assert.False(game.QuitRequested);
    }

    [Fact]
    public void AnUnhandledBackLeavesTheAppAsBefore()
    {
        var def = new BackGame();
        using var game = Create(def);
        var surface = new ControlSurface(game);

        surface.Call("input_back", "{}");
        Assert.Equal(1, def.Backs);
        Assert.True(game.QuitRequested);
    }

    [Fact]
    public void TheGestureOpClaimsReadsAndReleasesRegions()
    {
        using var game = Create(new BackGame());
        var surface = new ControlSurface(game);
        var w = game.Window.Width;

        var empty = JsonDocument.Parse(surface.Call("system_gestures", "{}")).RootElement;
        Assert.Equal(0, empty.GetProperty("regions").GetArrayLength());

        var set = JsonDocument.Parse(surface.Call("system_gestures", $$"""{"regions":["{{w - 40}},100,40,300"]}""")).RootElement;
        Assert.Equal($"{w - 40},100,40,300", set.GetProperty("regions")[0].GetString());
        Assert.Equal("right", set.GetProperty("edges")[0].GetString());
        Assert.Equal(ScreenEdges.Right, game.Window.ClaimedEdges);

        var cleared = JsonDocument.Parse(surface.Call("system_gestures", """{"clear":true}""")).RootElement;
        Assert.Equal(0, cleared.GetProperty("regions").GetArrayLength());
        Assert.Equal(ScreenEdges.None, game.Window.ClaimedEdges);

        Assert.Equal("bad_args", Assert.Throws<ControlException>(() => surface.Call("system_gestures", """{"regions":["1,2,3"]}""")).Code);
    }

    [Fact]
    public void EmptyRegionsAreDroppedAndTheSameClaimTwiceChangesNothing()
    {
        using var game = Create(new BackGame());
        var window = game.Window;
        window.ClaimSystemGestures([new GestureRegion(0, 10, 30, 200), new GestureRegion(5, 5, 0, 9)]);
        Assert.Single(window.ClaimedGestureRegions);
        Assert.Equal(ScreenEdges.Left, window.ClaimedEdges);
        window.ClaimSystemGestures([new GestureRegion(0, 10, 30, 200)]);
        Assert.Single(window.ClaimedGestureRegions);
    }
}
