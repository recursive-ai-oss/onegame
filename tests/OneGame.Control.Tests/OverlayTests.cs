using System.Text.Json;
using OneGame.Audio;
using OneGame.Core;
using OneGame.Host;
using OneGame.Platform;
using OneGame.UI;

namespace OneGame.Control.Tests;

// The newest audio service is process-wide, so tests that create one run one at a time.
[Collection("AudioService.Active")]
public class OverlayTests
{
    private sealed class QuietGame : GameDefinition
    {
        public override string Name => "QuietGame";

        public override void RegisterEntities(EntityRegistry registry)
        {
        }

        public override void Setup(World world)
        {
        }

        public override void DefineScreens(ScreenSet screens) => screens.Add("main", _ => Ui.Column(Ui.Label("x")));
    }

    private static GameInstance Start() => GameInstance.Create(new QuietGame(), new GameOptions { Headless = true, Gpu = GpuMode.Off });

    private static bool Visible(ControlSurface surface) => JsonDocument.Parse(surface.Call("overlay")).RootElement.GetProperty("visible").GetBoolean();

    [Fact]
    public void TheKeyAndThePadChordToggleItAndTheGameNeverSeesThem()
    {
        using var game = Start();
        var surface = new ControlSurface(game);
        var inj = game.Platform.Injector;
        Assert.False(Visible(surface));

        inj.KeyPress(game.Window.Id, Key.F3);
        game.DrainInput();
        Assert.True(Visible(surface));
        inj.KeyPress(game.Window.Id, Key.F3);
        game.DrainInput();
        Assert.False(Visible(surface));

        // One button of the chord does nothing; both together toggle; releasing and pressing again toggles back.
        var pad = inj.GamepadConnect();
        inj.GamepadButton(pad, GamepadButton.Back, true);
        game.DrainInput();
        Assert.False(Visible(surface));
        inj.GamepadButton(pad, GamepadButton.Start, true);
        game.DrainInput();
        Assert.True(Visible(surface));
        inj.GamepadButton(pad, GamepadButton.Start, false);
        inj.GamepadButton(pad, GamepadButton.Back, false);
        game.DrainInput();
        inj.GamepadButton(pad, GamepadButton.Start, true);
        game.DrainInput();
        Assert.True(Visible(surface));
        inj.GamepadButton(pad, GamepadButton.Back, true);
        game.DrainInput();
        Assert.False(Visible(surface));
    }

    [Fact]
    public void TheKeyAndTheChordCanBeChangedOrTurnedOff()
    {
        using var game = Start();
        var surface = new ControlSurface(game);
        var inj = game.Platform.Injector;
        var reply = JsonDocument.Parse(surface.Call("overlay", """{"key":"F4","chord":"LeftShoulder+RightShoulder"}""")).RootElement;
        Assert.Equal("F4", reply.GetProperty("key").GetString());
        Assert.Equal(["LeftShoulder", "RightShoulder"], reply.GetProperty("chord").EnumerateArray().Select(e => e.GetString()).ToArray());

        inj.KeyPress(game.Window.Id, Key.F3);
        game.DrainInput();
        Assert.False(Visible(surface));
        inj.KeyPress(game.Window.Id, Key.F4);
        game.DrainInput();
        Assert.True(Visible(surface));

        var pad = inj.GamepadConnect();
        inj.GamepadButton(pad, GamepadButton.LeftShoulder, true);
        inj.GamepadButton(pad, GamepadButton.RightShoulder, true);
        game.DrainInput();
        Assert.False(Visible(surface));

        surface.Call("overlay", """{"key":"none","chord":"none"}""");
        inj.KeyPress(game.Window.Id, Key.F4);
        game.DrainInput();
        Assert.False(Visible(surface));

        Assert.Equal("bad_args", Assert.Throws<ControlException>(() => surface.Call("overlay", """{"key":"Nope"}""")).Code);
        Assert.Equal("bad_args", Assert.Throws<ControlException>(() => surface.Call("overlay", """{"chord":"Back+Nope"}""")).Code);
    }

    [Fact]
    public void ThePanelReportsTheFiguresTheControlLayerHasAndMasksTheOnesThatVary()
    {
        using var game = Start();
        var surface = new ControlSurface(game);
        game.Step(3);
        var lines = JsonDocument.Parse(surface.Call("overlay", """{"visible":true}""")).RootElement.GetProperty("lines").EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Contains("frame -- ms", lines);
        Assert.Contains($"tick {game.World.Tick}  entities {game.World.Entities.Count}", lines);
        Assert.DoesNotContain(lines, l => l!.StartsWith("net", StringComparison.Ordinal) || l.StartsWith("audio", StringComparison.Ordinal));

        using var audio = AudioService.Create(new AudioServiceOptions { Capture = true, Device = new AudioDeviceOptions { SampleRate = 48_000, Channels = 2 } });
        surface.Audio = audio;
        var masked = surface.Overlay.Refresh().ToArray();
        Assert.Contains(masked, l => l.StartsWith("audio ", StringComparison.Ordinal) && l.Contains("underruns --", StringComparison.Ordinal));
        Assert.Contains("written -- played --", masked);

        surface.Call("overlay", """{"masked":false}""");
        var open = surface.Overlay.Refresh().ToArray();
        Assert.Contains(open, l => l.StartsWith("audio ", StringComparison.Ordinal) && l.Contains("underruns 0", StringComparison.Ordinal));
        Assert.Contains(open, l => l.StartsWith("written ", StringComparison.Ordinal) && !l.Contains("--", StringComparison.Ordinal));
    }

    [Fact]
    public void ToggleStepsAndTheHashStayApartInAHeadlessRun()
    {
        using var game = Start();
        var surface = new ControlSurface(game);
        game.Step(10);
        var hash = surface.Call("hash");
        surface.Call("overlay", """{"toggle":true}""");
        game.Step(10);
        var on = surface.Call("hash");
        surface.Call("overlay", """{"toggle":true}""");
        using var other = Start();
        var otherSurface = new ControlSurface(other);
        other.Step(20);
        Assert.NotEqual(hash, on);
        Assert.Equal(JsonDocument.Parse(otherSurface.Call("hash")).RootElement.GetProperty("hash").GetString(), JsonDocument.Parse(on).RootElement.GetProperty("hash").GetString());
    }
}
