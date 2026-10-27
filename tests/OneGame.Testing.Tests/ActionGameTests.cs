using System.Text.Json;
using OneGame.Control;
using OneGame.Core;
using OneGame.Host;
using OneGame.Platform;
using OneGame.Steam.Input;
using OneGame.UI;
using OneGame.UI.Kit;

namespace OneGame.Testing.Tests;

/// <summary>A walker the player steers: synced position and a jump counter, changed only by commands.</summary>
[Entity]
public partial class Walker : Entity
{
    [Synced] public partial int X { get; private set; }

    [Synced] public partial int Y { get; private set; }

    [Synced] public partial int Jumps { get; private set; }

    [Command]
    public void Step(int dx, int dy)
    {
        X += dx;
        Y += dy;
    }

    [Command]
    public void Jump() => Jumps++;
}

/// <summary>
/// The whole of an action-driven game: two actions declared, and view code that turns them into commands. The same code answers to the keyboard, a pad, Steam Input and the harness.
/// </summary>
public class ActionGame : GameDefinition
{
    public override string Name => "ActionGame";

    public override void RegisterEntities(EntityRegistry registry) => OneGame.Generated.OneGame_Testing_Tests.EntityTypes.Register(registry);

    public override void Setup(World world) => world.Add(new Walker());

    public override void DefineActions(ActionMap actions) =>
        actions.Context("play", c => c
            .Button("jump", Bind.Key(Key.Space), Bind.Pad(GamepadButton.South))
            .Vector("move", Bind.Wasd, Bind.LeftStick, Bind.DPad));

    public override void DefineScreens(ScreenSet screens) => screens.Add("main", game => Ui.Column(
        Ui.Label(() => "x=" + Walker(game).X).Named("pos"),
        ActionUi.BindingList(game)).Padding(Length.Px(8)));

    public override void OnEachFrame(GameInstance game)
    {
        var actions = game.Actions;
        var walker = Walker(game);
        if (actions.Pressed("jump"))
        {
            walker.Send.Jump();
        }

        var move = actions.Vector("move");
        if (move.Length > 0.5f)
        {
            walker.Send.Step((int)MathF.Sign(MathF.Round(move.X)), (int)MathF.Sign(MathF.Round(move.Y)));
        }
    }

    protected static Walker Walker(GameInstance game) => (Walker)game.World.Entities[0];
}

public class ActionGameTests
{
    private static TestCluster Start(GameDefinition game) => TestCluster.Start(game, new TestOptions { Width = 320, Height = 200, Gpu = GpuMode.Off });

    private static ScenarioResult Run(string yaml)
    {
        var game = new ActionGame();
        using var cluster = Start(game);
        var result = cluster.Run(ScenarioParser.Parse(yaml));
        Assert.True(result.Passed, JsonSerializer.Serialize(result, ScenarioJson.Default.ScenarioResult));
        return result;
    }

    [Fact]
    public void KeyboardPadAndActionStepsReachTheSameHash()
    {
        var keyboard = Run("""
            name: keyboard
            steps:
              - press: { key: d, state: down }
              - step: 3
              - press: { key: d, state: up }
              - press: Space
              - step: 1
            expect:
              - state: { entity: Walker, property: X, equals: 3 }
              - state: { entity: Walker, property: Jumps, equals: 1 }
            """);
        var pad = Run("""
            name: pad
            steps:
              - pad: { axis: leftx, value: 1 }
              - step: 3
              - pad: { axis: leftx, value: 0 }
              - pad: { button: south }
              - step: 1
            expect:
              - state: { entity: Walker, property: X, equals: 3 }
              - state: { entity: Walker, property: Jumps, equals: 1 }
            """);
        var action = Run("""
            name: action
            steps:
              - action: { name: move, x: 1, y: 0 }
              - step: 3
              - action: { name: move, state: up }
              - action: jump
              - step: 1
            expect:
              - state: { entity: Walker, property: X, equals: 3 }
              - state: { entity: Walker, property: Jumps, equals: 1 }
              - action: { name: jump, held: true, pressed: true }
              - action: { name: move, x: 0 }
            """);
        Assert.Equal(keyboard.Hash, pad.Hash);
        Assert.Equal(keyboard.Hash, action.Hash);
    }

    [Fact]
    public void ADpadAndAWrongExpectationReadWell()
    {
        var game = new ActionGame();
        using var cluster = Start(game);
        var result = cluster.Run(ScenarioParser.Parse("""
            name: dpad
            steps:
              - pad: { button: dpad_down, state: down }
              - step: 2
            expect:
              - state: { entity: Walker, property: Y, equals: 2 }
              - action: { name: move, held: false }
              - action: { name: jump, pressed: true }
            """));
        Assert.False(result.Passed);
        var json = JsonSerializer.Serialize(result, ScenarioJson.Default.ScenarioResult);
        Assert.Contains("move held is true, expected false", json, StringComparison.Ordinal);
        Assert.Contains("jump pressed is false, expected true", json, StringComparison.Ordinal);
    }

    [Fact]
    public void TheActionsOperationReportsStateAndBindings()
    {
        var game = new ActionGame();
        using var cluster = Start(game);
        var c = cluster.Client;
        var instance = c.Game;
        using var before = JsonDocument.Parse(c.Call("actions", "{\"name\":\"move\",\"x\":0,\"y\":1}"));
        c.Call("step", "{\"count\":1}");
        using var doc = JsonDocument.Parse(c.Call("actions"));
        var move = doc.RootElement.GetProperty("actions").EnumerateArray().First(a => a.GetProperty("name").GetString() == "move");
        Assert.True(move.GetProperty("held").GetBoolean());
        Assert.Equal(1.0, move.GetProperty("y").GetDouble());
        Assert.Equal("quad(key:A,key:D,key:W,key:S)", move.GetProperty("bindings")[0].GetString());
        Assert.Throws<ControlException>(() => c.Call("actions", "{\"name\":\"jmup\"}"));
    }

    [Fact]
    public void RebindingThroughTheListSurvivesSaveAndLoad()
    {
        var game = new ActionGame();
        using var cluster = Start(game);
        var c = cluster.Client;
        var instance = c.Game;
        c.Call("ui_click", "{\"id\":\"rebind-jump\"}");
        Assert.Equal("jump", instance.Actions.Rebinding?.Action);
        c.Call("input_key", "{\"key\":\"j\"}");
        Assert.Null(instance.Actions.Rebinding);
        Assert.Equal(Bind.Key(Key.J), instance.Actions["jump"].Bindings[0].Part(0));
        c.Call("step", "{\"count\":1}");
        using (var ui = JsonDocument.Parse(c.Call("ui_query", "{\"selector\":\"#binding-jump\"}")))
        {
            Assert.Contains("J", ui.RootElement.ToString(), StringComparison.Ordinal);
        }

        var path = Path.Combine(Path.GetTempPath(), "onegame-actiongame-" + Guid.NewGuid().ToString("N"), "bindings.txt");
        try
        {
            instance.Actions.Save(path);
            c.Call("ui_click", "{\"id\":\"reset-jump\"}");
            Assert.True(instance.Actions["jump"].IsDefault);
            Assert.Empty(instance.Actions.Load(path).Problems);
            Assert.Equal(Bind.Key(Key.J), instance.Actions["jump"].Bindings[0].Part(0));

            // The rebound key now jumps; the old one no longer does.
            c.Call("input_key", "{\"key\":\"j\"}");
            c.Call("step", "{\"count\":1}");
            c.Call("input_key", "{\"key\":\" \"}");
            c.Call("step", "{\"count\":1}");
            Assert.Equal(1, ((Walker)instance.World.Entities[0]).Jumps);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, true);
        }
    }
}

// The service keeps a process-wide "active" instance, so these share a collection with the other Steam Input scenario tests.
[Collection("SteamInputService")]
public class ActionSteamTests
{
    private sealed class SteamActionGame : ActionGame, IDisposable
    {
        private SteamInputBridge? _bridge;
        private SteamInputService? _steam;
        private SteamActionSource? _source;

        public override void OnEachFrame(GameInstance game)
        {
            if (_bridge is null)
            {
                // One declaration: the Steam action sets and the actions file come from the action map.
                _steam = SteamInputService.StartFake(SteamActionSource.CreateActions(game.Actions));
                _source = new SteamActionSource(_steam, game.Actions);
                _bridge = new SteamInputBridge(_steam, game.Platform, game.Ui);
            }

            _bridge.Update(game.CurrentScreen);
            base.OnEachFrame(game);
        }

        public int Fed => _source?.FedActions ?? 0;

        public SteamInputActions? Declaration => _steam?.Actions;

        public void Dispose()
        {
            _source?.Dispose();
            _bridge?.Dispose();
            _steam?.Dispose();
        }
    }

    [Fact]
    public void SteamInputsFakeControllerDrivesTheSameActions()
    {
        using var game = new SteamActionGame();
        using var cluster = TestCluster.Start(game, new TestOptions { Width = 320, Height = 200, Gpu = GpuMode.Off });
        var result = cluster.Run(ScenarioParser.Parse("""
            name: steam-actions
            steps:
              - step: 2
              - steam-input: { connect: 3 }
              - steam-input: { press: jump }
              - steam-input: { release: jump }
              - steam-input: { analog: move, x: 1, y: 0 }
              - step: 3
              - steam-input: { analog: move, x: 0, y: -1 }
              - step: 2
            expect:
              - state: { entity: Walker, property: Jumps, equals: 1 }
              - action: { name: move, y: 1 }
              - state: { entity: Walker, property: Y, min: 1 }
            """));
        Assert.True(result.Passed, JsonSerializer.Serialize(result, ScenarioJson.Default.ScenarioResult));
        Assert.Equal(2, game.Fed);
        // Steam reports up as positive, actions run down-positive: the fake stick pushed to y: -1 (down) reads as the action's y = 1.
        Assert.Contains("move", game.Declaration!.ToVdf(), StringComparison.Ordinal);
    }
}
