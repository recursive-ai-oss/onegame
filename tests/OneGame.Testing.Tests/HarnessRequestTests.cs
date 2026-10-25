using System.Text.Json;
using OneGame.Control;
using OneGame.Host;
using OneGame.Live;
using OneGame.Platform;
using OneGame.Sync;

namespace OneGame.Testing.Tests;

/// <summary>A shop whose screen records the controller buttons it sees, so a test can tell which ones arrived.</summary>
public sealed class PadGame : ShopGame
{
    public List<GamepadButton> Seen { get; } = [];

    public override void OnInput(GameInstance game, in PlatformEvent input)
    {
        if (input.Kind == PlatformEventKind.GamepadButtonDown)
        {
            Seen.Add(input.GamepadButton);
        }
    }
}

public class HarnessRequestTests
{
    private static TestOptions Options(float scale = 1f) => new() { Width = 320, Height = 200, Gpu = GpuMode.Off, Scale = scale };

    // ---- UI scale override ----

    [Fact]
    public void UiScaleOverrideLaysOutHitTestsAndCapturesLikeAWindowAtThatScale()
    {
        using var native = TestCluster.Start(new ShopGame(), Options(1.5f));
        using var overridden = TestCluster.Start(new ShopGame(), Options(1f));
        overridden.Client.Game.UiScaleOverride = 1.5f;

        Assert.Equal(1.5f, overridden.Client.Game.UiScale);
        Assert.Equal(1f, overridden.Client.Game.WindowScale);

        // The control layer's own default capture scale is the effective one.
        var a = native.Client.Call("capture_frame", """{"size":"320x200"}""");
        var b = overridden.Client.Call("capture_frame", """{"size":"320x200"}""");
        using (var da = JsonDocument.Parse(a))
        using (var db = JsonDocument.Parse(b))
        {
            Assert.Equal(1.5, db.RootElement.GetProperty("scale").GetDouble());
            Assert.Equal(da.RootElement.GetProperty("sha256").GetString(), db.RootElement.GetProperty("sha256").GetString());
        }

        Assert.Equal(0, ImageCompare.Compare(native.Client.Capture("320x200", 1.5f), overridden.Client.Capture("320x200", 1.5f)).DifferingPixels);

        // Layout and hit-testing follow too: the 60x20 unit Buy button is 90x30 px.
        Assert.Equal(native.Client.Call("ui_query", """{"selector":"#buy"}"""), overridden.Client.Call("ui_query", """{"selector":"#buy"}"""));
        var buy = overridden.Client.Find("buy")!;
        Assert.Equal(90, buy.AbsoluteBounds.Width, 0.01);
        var hit = overridden.Client.Game.Ui.HitTest(buy.AbsoluteBounds.X + 80, buy.AbsoluteBounds.Y + 25);
        Assert.Same(buy, hit);

        // A different scale gives a different picture, so the equality above is not vacuous.
        Assert.NotEqual(0, ImageCompare.Compare(native.Client.Capture("320x200", 1.5f), native.Client.Capture("320x200", 1f)).DifferingPixels);
    }

    [Fact]
    public void UiScaleOverrideFollowsTheWindowAgainWhenCleared()
    {
        using var cluster = TestCluster.Start(new ShopGame(), Options(1f));
        var c = cluster.Client;
        using (var d = JsonDocument.Parse(c.Call("ui_scale", """{"scale":2}""")))
        {
            Assert.Equal(2, d.RootElement.GetProperty("scale").GetDouble());
            Assert.Equal(2, d.RootElement.GetProperty("override").GetDouble());
            Assert.Equal(1, d.RootElement.GetProperty("windowScale").GetDouble());
        }

        using (var info = JsonDocument.Parse(c.Call("info")))
        {
            Assert.Equal(2, info.RootElement.GetProperty("scale").GetDouble());
            Assert.Equal(2, info.RootElement.GetProperty("scaleOverride").GetDouble());
        }

        Assert.Equal(120, c.Find("buy")!.AbsoluteBounds.Width, 0.01);
        using (var d = JsonDocument.Parse(c.Call("ui_scale", """{"scale":null}""")))
        {
            Assert.Equal(1, d.RootElement.GetProperty("scale").GetDouble());
            Assert.False(d.RootElement.TryGetProperty("override", out _));
        }

        Assert.Equal(60, c.Find("buy")!.AbsoluteBounds.Width, 0.01);
        c.Call("ui_scale", """{"scale":1.25}""");
        c.Call("ui_scale", """{"reset":true}""");
        Assert.Null(c.Game.UiScaleOverride);
        Assert.Equal("bad_args", Assert.Throws<ControlException>(() => c.Call("ui_scale", """{"scale":0}""")).Code);
        Assert.Equal("bad_args", Assert.Throws<ControlException>(() => c.Call("ui_scale", """{"scale":99}""")).Code);
        Assert.Throws<ArgumentOutOfRangeException>(() => c.Game.UiScaleOverride = float.NaN);
    }

    [Fact]
    public void ScenarioCanSetTheUiScale()
    {
        using var cluster = TestCluster.Start(new ShopGame(), Options(1f));
        var scenario = ScenarioParser.Parse("steps:\n  - ui-scale: 2\n  - step: 1\nexpect:\n  - ui: {id: buy, count: 1}\n", "inline.yaml", "scale");
        Assert.True(cluster.Run(scenario).Passed);
        Assert.Equal(2f, cluster.Client.Game.UiScale);
        Assert.True(cluster.Run(ScenarioParser.Parse("steps:\n  - ui-scale: auto\n", "inline.yaml", "auto")).Passed);
        Assert.Equal(1f, cluster.Client.Game.UiScale);
    }

    // ---- resets in recordings ----

    private static string Temp()
    {
        var dir = Path.Combine(Path.GetTempPath(), "og-req-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public void ARecordedSessionThatResetsWithANewSeedReplaysIdentically()
    {
        var dir = Temp();
        try
        {
            using var cluster = TestCluster.Start(new ShopGame(), Options());
            var c = cluster.Client;
            var recorder = c.Game.StartSessionRecording();
            c.Send("Shop", "Buy", "2").Step(7);
            c.Call("reset", """{"seed":"4242"}""");
            c.Send("Shop", "Buy", "1").Step(9);
            c.Call("state_patch", """{"entity":"Shop","property":"Gold","value":"500"}""");
            c.Step(6);
            var finalHash = c.Hash;
            var session = recorder.Stop();

            Assert.Equal(3, session.Segments.Count);
            Assert.Equal(22, session.Ticks);
            Assert.Equal(4242UL, session.Segments[1].Seed);

            var path = Path.Combine(dir, "session.ogrp");
            RecordingFile.Save(session, path);
            var loaded = RecordingFile.LoadSession(path);
            Assert.Equal(3, loaded.Segments.Count);
            Assert.Equal(22, loaded.Ticks);

            using var fresh = TestCluster.Start(new ShopGame(), Options());
            var result = fresh.Client.Game.Replay(loaded);
            Assert.True(result.Ok, result.Divergence?.ToString());
            Assert.Equal(22, result.TicksRun);
            Assert.Equal(finalHash, result.FinalHash);

            // A single-recording replay of just the last world cannot see the first one: the session is what carries both.
            Assert.True(session.Segments[2].StartTick > 0);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void ScenarioWithAMidRunResetAndPatchRecordedThroughTheCliReplaysIdentical()
    {
        var dir = Temp();
        try
        {
            var file = Path.Combine(dir, "s.yaml");
            File.WriteAllText(file, """
                name: reset-mid-run
                steps:
                  - command: {entity: Shop, name: Buy, args: [2]}
                  - step: 5
                  - reset: 99
                  - command: {entity: Shop, name: Buy, args: [1]}
                  - step: 8
                  - set: {entity: Shop, property: Stock, value: "40"}
                  - step: 4
                  - reset: {seed: 5}
                  - step: 3
                expect:
                  - state: {entity: Shop, property: Stock, equals: 3}
                """);
            Dev.Enable();
            var rec = Path.Combine(dir, "r.ogrp");
            var outText = new StringWriter();
            var code = GameApp.Run(new ShopGame(), ["--headless", "--gpu", "off", "--json", "--scenario", file, "--record", rec, "--artifacts", dir], outText, TextWriter.Null);
            Assert.True(code == 0, outText.ToString());

            var replayOut = new StringWriter();
            Assert.Equal(0, GameApp.Run(new ShopGame(), ["--headless", "--gpu", "off", "--json", "--replay", rec], replayOut, TextWriter.Null));
            using var doc = JsonDocument.Parse(replayOut.ToString());
            Assert.True(doc.RootElement.GetProperty("identical").GetBoolean());
            Assert.Equal(20, doc.RootElement.GetProperty("ticks").GetInt32());
            Assert.Equal(2, doc.RootElement.GetProperty("commands").GetInt32());
            Assert.Equal(4, doc.RootElement.GetProperty("segments").GetInt32());

            // Damage in a later segment is found and named.
            var bytes = File.ReadAllBytes(rec);
            bytes[^20] ^= 0xFF;
            var bad = Path.Combine(dir, "bad.ogrp");
            File.WriteAllBytes(bad, bytes);
            var badOut = new StringWriter();
            Assert.NotEqual(0, GameApp.Run(new ShopGame(), ["--headless", "--gpu", "off", "--json", "--replay", bad], badOut, TextWriter.Null));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void EditingStateWithCommandsQueuedWhileRecordingIsRefusedNotSilentlyLost()
    {
        using var cluster = TestCluster.Start(new ShopGame(), Options());
        var c = cluster.Client;
        var recorder = c.Game.StartSessionRecording();
        c.Send("Shop", "Buy", "1");
        var ex = Assert.Throws<ControlException>(() => c.Call("state_patch", """{"entity":"Shop","property":"Gold","value":"5"}"""));
        Assert.Equal("rejected", ex.Code);
        Assert.Equal("0", c.Get("Shop", "Gold"));
        c.Step();
        c.Call("state_patch", """{"entity":"Shop","property":"Gold","value":"5"}""");
        c.Step(2);
        using var fresh = TestCluster.Start(new ShopGame(), Options());
        Assert.True(fresh.Client.Game.Replay(recorder.Stop()).Ok);
    }

    [Fact]
    public void SessionFilesAreReadSafely()
    {
        var dir = Temp();
        try
        {
            using var cluster = TestCluster.Start(new ShopGame(), Options());
            var c = cluster.Client;
            var recorder = c.Game.StartSessionRecording();
            c.Step(3);
            c.Call("reset");
            c.Step(3);
            var path = Path.Combine(dir, "s.ogrp");
            RecordingFile.Save(recorder.Stop(), path);
            var bytes = File.ReadAllBytes(path);
            for (var cut = 0; cut < bytes.Length; cut += Math.Max(1, bytes.Length / 40))
            {
                var p = Path.Combine(dir, "cut.ogrp");
                File.WriteAllBytes(p, bytes.AsSpan(0, cut).ToArray());
                Assert.Throws<InvalidDataException>(() => RecordingFile.LoadSession(p));
            }

            // An absurd segment count or length is refused before anything is allocated.
            var huge = (byte[])bytes.Clone();
            huge[8] = huge[9] = huge[10] = huge[11] = 0xFF;
            File.WriteAllBytes(Path.Combine(dir, "huge.ogrp"), huge);
            Assert.Throws<InvalidDataException>(() => RecordingFile.LoadSession(Path.Combine(dir, "huge.ogrp")));
            huge = (byte[])bytes.Clone();
            huge[12] = huge[13] = huge[14] = huge[15] = 0xFF;
            File.WriteAllBytes(Path.Combine(dir, "huge2.ogrp"), huge);
            Assert.Throws<InvalidDataException>(() => RecordingFile.LoadSession(Path.Combine(dir, "huge2.ogrp")));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    // ---- gamepad buttons ----

    [Fact]
    public void EveryGamepadButtonCanBeInjectedByName()
    {
        var game = new PadGame();
        using var cluster = TestCluster.Start(game, Options());
        var all = Enum.GetValues<GamepadButton>();
        foreach (var button in all)
        {
            // The platform's own names, in the spelling the schema lists.
            var found = false;
            foreach (var name in NavActions.Names)
            {
                if (NavActions.TryParse(name, out var b) && b == button)
                {
                    found = true;
                    cluster.Client.Call("input_nav", "{\"action\":\"" + name + "\"}");
                    break;
                }
            }

            Assert.True(found, $"{button} has no nav name");
        }

        // The menu buttons (d-pad, shoulders, South, East) drive the UI; every other button reaches the game's own input handler.
        GamepadButton[] menu = [GamepadButton.South, GamepadButton.East, GamepadButton.LeftShoulder, GamepadButton.RightShoulder, GamepadButton.DPadUp, GamepadButton.DPadDown, GamepadButton.DPadLeft, GamepadButton.DPadRight];
        foreach (var button in all.Where(b => !menu.Contains(b)))
        {
            Assert.Contains(button, game.Seen);
        }

        Assert.True(NavActions.TryParse("Left-Shoulder", out var ls) && ls == GamepadButton.LeftShoulder);
        Assert.True(NavActions.TryParse("Y", out var y) && y == GamepadButton.North);
        Assert.Equal("bad_args", Assert.Throws<ControlException>(() => cluster.Client.Call("input_nav", """{"action":"turbo"}""")).Code);
    }

    [Fact]
    public void ScenarioNavStepsInjectStartAndFaceButtons()
    {
        var game = new PadGame();
        using var cluster = TestCluster.Start(game, Options());
        var result = cluster.Run(ScenarioParser.Parse("steps:\n  - nav: start\n  - nav: x\n  - nav: north\n  - nav: back\n", "inline.yaml", "pad"));
        Assert.True(result.Passed, JsonSerializer.Serialize(result, ScenarioJson.Default.ScenarioResult));
        Assert.Equal([GamepadButton.Start, GamepadButton.West, GamepadButton.North, GamepadButton.Back], game.Seen);
    }
}
