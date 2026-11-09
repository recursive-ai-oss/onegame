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

    [Fact]
    public void FrameTimingRunsOnlyWhileTheOverlayIsShownAndTheOverlayOpReportsIt()
    {
        using var game = Start();
        var surface = new ControlSurface(game);
        game.Step(5);
        Assert.Equal(0, game.Timings.Count);
        Assert.False(game.Timings.Enabled);
        Assert.Equal(JsonValueKind.Undefined, JsonDocument.Parse(surface.Call("info")).RootElement.TryGetProperty("timing", out var none) ? none.ValueKind : JsonValueKind.Undefined);

        surface.Call("overlay", """{"visible":true}""");
        game.Step(10);
        var overlay = JsonDocument.Parse(surface.Call("overlay")).RootElement;
        var timing = overlay.GetProperty("timing");
        Assert.Equal(10, timing.GetProperty("samples").GetInt32());
        Assert.True(timing.GetProperty("cpu").GetDouble() > 0);
        Assert.True(timing.GetProperty("cpuMax").GetDouble() >= timing.GetProperty("cpuAvg").GetDouble());
        Assert.True(timing.GetProperty("sim").GetDouble() <= timing.GetProperty("cpu").GetDouble());
        Assert.Equal(10, timing.GetProperty("cpuHistory").GetArrayLength());
        Assert.Equal("none", timing.GetProperty("gpu").GetString());   // no graphics device in this run
        Assert.True(JsonDocument.Parse(surface.Call("info")).RootElement.TryGetProperty("timing", out _));

        // Headless runs mask machine-dependent text, so the panel's lines carry no timing; the numbers above are never masked.
        Assert.DoesNotContain(overlay.GetProperty("lines").EnumerateArray(), l => l.GetString()!.StartsWith("cpu ", StringComparison.Ordinal));

        game.Step(100);
        Assert.Equal(FrameTimings.Capacity, game.Timings.Count);
        surface.Call("overlay", """{"visible":false}""");
        game.Step(3);
        Assert.False(game.Timings.Enabled);
    }

    [Fact]
    public void UnmaskedPanelShowsCpuTimingLines()
    {
        using var game = Start();
        var surface = new ControlSurface(game);
        surface.Call("overlay", """{"visible":true,"masked":false}""");
        game.Step(20);
        var lines = JsonDocument.Parse(surface.Call("overlay")).RootElement.GetProperty("lines").EnumerateArray().Select(l => l.GetString()!).ToArray();
        Assert.Contains(lines, l => l.StartsWith("cpu ", StringComparison.Ordinal) && l.Contains("sim ", StringComparison.Ordinal) && l.Contains("draw ", StringComparison.Ordinal));
    }

    [Fact]
    public void TimingAddsNothingToAFrameAllocationWiseAndAHiddenPanelDrawsNothing()
    {
        using var game = Start();
        var overlay = DebugOverlay.For(game);
        game.Step(50);

        long Measure()
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 200; i++)
            {
                game.Step(1);
            }

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        Measure();
        var hidden = Measure();
        overlay.Visible = true;
        game.Step(5);
        var shown = Measure();
        Assert.Equal(200, game.Timings.Count > 0 ? 200 : 0);
        Assert.True(shown - hidden <= 256, $"timing allocated {shown - hidden} B over 200 frames (hidden {hidden}, shown {shown})");

        overlay.Visible = false;
        var drawn = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++)
        {
            overlay.Draw(null!);   // hidden: returns before touching the canvas
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - drawn);
    }
}
