using System.Text.Json;
using OneGame.Core;
using OneGame.Host;
using OneGame.Platform;
using OneGame.Steam.Input;
using OneGame.UI;
using OneGame.UI.Kit;

namespace OneGame.Control.Tests;

public class SteamInputOpTests
{
    private sealed class PadGame : GameDefinition, IDisposable
    {
        public void Dispose() => _bridge?.Dispose();

        private SteamInputBridge? _bridge;
        private readonly List<string> _log = [];

        public SteamInputService? Steam { get; private set; }

        public IReadOnlyList<string> Log => _log;

        public InputGlyphs Glyphs { get; } = new InputGlyphs().Bind("confirm", keyboard: "Enter");

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
            screens.Add("menu", _ => Ui.Column(UiKit.TextInput(() => "", _ => { }, "Name", id: "name")));
        }

        public override void OnEachFrame(GameInstance game)
        {
            if (_bridge is null)
            {
                var actions = new SteamInputActions()
                    .Set("Map", "Map", s => s.Button("confirm", "Confirm", GamepadButton.South).Stick("move", "Move", SteamStickMode.JoystickMove, SteamStick.Left).OnScreens("map"))
                    .Set("Menu", "Menu", s => s.Button("accept", "Accept", GamepadButton.East).OnScreens("menu"));
                Steam = SteamInputService.StartFake(actions);
                _bridge = new SteamInputBridge(Steam, game.Platform, game.Ui, () => game.UiScale, Glyphs);
            }

            _bridge.Update(game.CurrentScreen);
        }

        public override void OnInput(GameInstance game, in PlatformEvent input)
        {
            if (input.Kind is PlatformEventKind.GamepadButtonDown or PlatformEventKind.GamepadAxisMoved)
            {
                _log.Add($"{input.Kind}:{input.GamepadButton}:{input.GamepadAxis}:{input.Value:0.##}");
            }
        }
    }

    [Fact]
    public void TheHarnessDrivesAStandInPadThroughTheSamePathARealOneUses()
    {
        var dir = Path.Combine(Path.GetTempPath(), "onegame-steam-input-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var def = new PadGame();
        try
        {
            using var game = GameInstance.Create(def, new GameOptions { Headless = true, Gpu = GpuMode.Off });
            var surface = new ControlSurface(game, dir);
            Assert.Equal("no_steam_input", Assert.Throws<ControlException>(() => surface.Call("steam_input")).Code);

            surface.Call("screen_open", """{"name":"map"}""");
            surface.Call("step", """{"count":2}""");
            using (var doc = JsonDocument.Parse(surface.Call("steam_input")))
            {
                var r = doc.RootElement;
                Assert.True(r.GetProperty("enabled").GetBoolean());
                Assert.True(r.GetProperty("fake").GetBoolean());
                Assert.Equal("Map", r.GetProperty("activeSet").GetString());
                Assert.Equal(2, r.GetProperty("sets").GetArrayLength());
                Assert.Equal(0, r.GetProperty("controllers").GetArrayLength());
            }

            surface.Call("steam_input", """{"connect":14}""");
            surface.Call("steam_input", """{"press":"confirm"}""");
            surface.Call("steam_input", """{"analog":"move","x":0,"y":1}""");
            surface.Call("step", """{"count":2}""");
            using (var doc = JsonDocument.Parse(surface.Call("steam_input")))
            {
                var r = doc.RootElement;
                Assert.Equal("steam-deck", r.GetProperty("controllers")[0].GetProperty("type").GetString());
                Assert.Equal("Map", r.GetProperty("controllers")[0].GetProperty("activeSet").GetString());
                var confirm = r.GetProperty("values").EnumerateArray().Single(v => v.GetProperty("action").GetString() == "confirm");
                Assert.True(confirm.GetProperty("down").GetBoolean());
                var move = r.GetProperty("values").EnumerateArray().Single(v => v.GetProperty("action").GetString() == "move");
                Assert.Equal(1.0, move.GetProperty("y").GetDouble());
            }

            // The game saw it as gamepad input.
            var gamepad = game.Platform.Input.GetGamepad(1)!;
            Assert.True(gamepad.IsDown(GamepadButton.South));
            Assert.Equal(-1f, gamepad.GetAxis(GamepadAxis.LeftY)); // Steam's up is the engine's negative Y

            // A screen change switches the set and releases what the new set does not have.
            surface.Call("screen_open", """{"name":"menu"}""");
            surface.Call("step", """{"count":2}""");
            using (var doc = JsonDocument.Parse(surface.Call("steam_input")))
            {
                Assert.Equal("Menu", doc.RootElement.GetProperty("activeSet").GetString());
                Assert.False(doc.RootElement.GetProperty("values").EnumerateArray().Single(v => v.GetProperty("action").GetString() == "confirm").GetProperty("down").GetBoolean());
            }

            // Focusing the text field under a pad player opens the on-screen keyboard.
            def.Glyphs.SetDevice(InputDevice.Gamepad);
            Assert.True(game.Ui.Focus(game.Ui.Find("name")));
            surface.Call("step", """{"count":2}""");
            using (var doc = JsonDocument.Parse(surface.Call("steam_input")))
            {
                var kb = doc.RootElement.GetProperty("keyboard");
                Assert.True(kb.GetProperty("visible").GetBoolean());
                Assert.Equal(1, kb.GetProperty("shown").GetInt32());
            }

            Assert.Equal("bad_args", Assert.Throws<ControlException>(() => surface.Call("steam_input", """{"press":"nope"}""")).Code);
        }
        finally
        {
            def.Dispose();
            def.Steam?.Dispose();
            Directory.Delete(dir, true);
        }
    }
}
