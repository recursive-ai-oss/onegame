using System.Text.Json;
using OneGame.Core;
using OneGame.Host;
using OneGame.Platform;
using OneGame.Steam.Input;
using OneGame.UI;

namespace OneGame.Testing.Tests;

[Collection("SteamInputService")]
public class SteamInputScenarioTests
{
    private sealed class PadGame : GameDefinition, IDisposable
    {
        private SteamInputBridge? _bridge;
        private SteamInputService? _steam;

        public override string Name => "PadGame";

        public override void RegisterEntities(EntityRegistry registry)
        {
        }

        public override void Setup(World world)
        {
        }

        public override void DefineScreens(ScreenSet screens)
        {
            screens.Add("map", _ => Ui.Column(Ui.Label("map")));
            screens.Add("menu", _ => Ui.Column(Ui.Label("menu")));
        }

        public override void OnEachFrame(GameInstance game)
        {
            if (_bridge is null)
            {
                var actions = new SteamInputActions()
                    .Set("Map", "Map", s => s.Button("confirm", "Confirm", GamepadButton.South).OnScreens("map"))
                    .Set("Menu", "Menu", s => s.Button("accept", "Accept", GamepadButton.East).OnScreens("menu"));
                _steam = SteamInputService.StartFake(actions);
                _bridge = new SteamInputBridge(_steam, game.Platform, game.Ui);
            }

            _bridge.Update(game.CurrentScreen);
        }

        public void Dispose()
        {
            _bridge?.Dispose();
            _steam?.Dispose();
        }
    }

    [Fact]
    public void AScenarioPlaysAPadAndChecksTheActionSetPerScreen()
    {
        const string yaml = """
            name: pad
            window: 320x200
            steps:
              - screen: map
              - step: 2
              - steam-input: { connect: 3 }
              - steam-input: { press: confirm }
              - expect: { steam-input: { set: Map, controllers: 1, down: confirm } }
              - screen: menu
              - step: 2
              - expect: { steam-input: { set: Menu, up: confirm } }
              - steam-input: { press: accept }
            expect:
              - steam-input: { set: Menu, down: accept, keyboard: false }
            """;
        using var game = new PadGame();
        using var cluster = TestCluster.Start(game, new TestOptions { Width = 320, Height = 200, Gpu = GpuMode.Off });
        var result = cluster.Run(ScenarioParser.Parse(yaml));
        Assert.True(result.Passed, JsonSerializer.Serialize(result, ScenarioJson.Default.ScenarioResult));
        Assert.Equal(3, result.Expectations.Length);
    }

    [Fact]
    public void AWrongExpectationFailsWithAReadableDiff()
    {
        const string yaml = """
            name: pad-fail
            steps:
              - screen: map
              - step: 2
            expect:
              - steam-input: { set: Menu, controllers: 2 }
            """;
        using var game = new PadGame();
        using var cluster = TestCluster.Start(game, new TestOptions { Width = 320, Height = 200, Gpu = GpuMode.Off });
        var result = cluster.Run(ScenarioParser.Parse(yaml));
        Assert.False(result.Passed);
        var json = JsonSerializer.Serialize(result, ScenarioJson.Default.ScenarioResult);
        Assert.Contains("active set is Map, expected Menu", json);
        Assert.Contains("0 controllers connected, expected 2", json);
    }
}
