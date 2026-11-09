using OneGame.Audio;
using System.Text.Json;
using OneGame.Sync;
using OneGame.Control;
using OneGame.Core;
using OneGame.Host;
using OneGame.Live;
using OneGame.Testing;
using OneGame.UI;

namespace OneGame.AotSmoke;

/// <summary>A one-screen game for the smoke test: a label bound to the purse and a button that adds to it.</summary>
internal sealed class SmokeGame : GameDefinition
{
    public override string Name => "AotSmokeGame";

    public override void RegisterEntities(EntityRegistry registry) => OneGame.Generated.OneGame_AotSmoke.EntityTypes.Register(registry);

    public override void RegisterSystems(World world) => world.AddTable(new SparkTable());

    public override void Setup(World world)
    {
        world.Add(new Purse { Owner = "smoke" });
        ((SparkTable)world.Tables[0]).Add(new Spark { X = Fixed.FromInt(1), Life = 3 });
    }

    public override void DefineScreens(ScreenSet screens) =>
        screens.Add("main", game => Ui.Column(
            Ui.Label(() => "Total " + ((Purse)game.World.Entities[0]).Total).Named("total"),
            Ui.Button("Add", () => ((Purse)game.World.Entities[0]).Send.Add(1, Fixed.FromInt(2))).Named("add").Padding(6))
            .Padding(8).Background(Color.Rgb(0x203040)).TextColor(Color.White));
}

/// <summary>Runs the harness (host, control layer, scenarios, capture, recording, MCP messages) so the native publish compiles all of it.</summary>
internal static class HarnessSmoke
{
    public static int Run()
    {
        using var game = GameInstance.Create(new SmokeGame(), new GameOptions { Headless = true, Width = 320, Height = 200, Gpu = GpuMode.Off });
        var surface = new ControlSurface(game, Path.GetTempPath());
        surface.Call("command_send", """{"entity":"Purse","command":"Add","args":["3","1.5"]}""");
        surface.Call("step", """{"frames":3}""");
        surface.Call("ui_click", """{"id":"add"}""");
        surface.Call("step", """{"frames":2}""");
        var state = surface.Call("state_query", """{"entity":"Purse","property":"Coins"}""");
        var tree = surface.Call("ui_tree");

        // A hot-table cell written and read back by address, and a path into a list.
        surface.Call("state_patch", """{"table":"Spark","row":0,"property":"Life","value":"9"}""");
        if (surface.Call("state_query", """{"table":"Spark","property":"[0].Life"}""") != "9")
        {
            throw new InvalidOperationException("The hot table cell was not written.");
        }

        AssetsSmoke.Run();

        // Shader hot reload: compile a file, register it, edit it, and let the control layer recompile and swap it.
        var hlsl = Path.Combine(Path.GetTempPath(), "aot-smoke-" + Environment.ProcessId + ".hlsl");
        const string Body = "[shader(\"pixel\")] float4 PSMain() : SV_Target { return float4(@V@, 0, 0, 1); }";
        File.WriteAllText(hlsl, Body.Replace("@V@", "1", StringComparison.Ordinal));
        var baseline = OneShader.ShaderCompiler.CompileFile(hlsl).Reflection!;
        var applied = 0;
        var registration = game.HotShaders.Register(new HotShader("smoke", hlsl, "PSMain", baseline, _ => applied++));
        File.WriteAllText(hlsl, Body.Replace("@V@", "0.5", StringComparison.Ordinal));
        File.SetLastWriteTimeUtc(hlsl, DateTime.UtcNow.AddSeconds(5));
        var shaders = surface.Call("shaders");
        var themeLint = surface.Call("theme_lint");
        var overlay = surface.Call("overlay", """{"visible":true,"key":"F4","chord":"Back+Start"}""");
        game.HotShaders.Unregister(registration);
        File.Delete(hlsl);

        // A scenario in YAML, run through the control layer.
        var scenario = ScenarioParser.Parse("""
            name: smoke
            steps:
              - click: add
              - overlay: on
              - step: 1
            expect:
              - ui: { id: total, contains: Total }
              - tick: 6
            """);
        var result = new ScenarioRunner(surface, new ScenarioRunOptions { ArtifactsDirectory = Path.GetTempPath() }).Run(scenario);

        // A capture through the CPU path, encoded and decoded again with OneImage.
        var png = surface.Invoke("capture_frame", """{"size":"160x100"}""").Png!;
        var back = Png.Decode(png);

        // Retain the sequence beside other smoke captures so the native binary's audio/video path can be inspected.
        using (var audio = AudioService.Create(new AudioServiceOptions { Capture = true }))
        {
            audio.Play(audio.LoadSound(new MemoryStream(WavTone())));
            surface.Audio = audio;
            using var sequence = JsonDocument.Parse(surface.Call("capture_sequence", """{"dir":"aot-sequence","count":4,"fps":60,"size":"160x100"}"""));
            var wav = File.ReadAllBytes(sequence.RootElement.GetProperty("wav").GetString()!);
            if (wav.Length != 44 + (3200 * 2 * 4) || !wav.AsSpan(44).ContainsAnyExcept((byte)0))
            {
                throw new InvalidOperationException("The native sequence did not record its mixed audio.");
            }

            Console.WriteLine("ok: sequence " + sequence.RootElement.GetRawText());
            surface.Audio = null;
        }

        // Record, replay and compare.
        using var recorded = GameInstance.Create(new SmokeGame(), new GameOptions { Headless = true, Gpu = GpuMode.Off });
        var recorder = recorded.StartRecording();
        new ControlSurface(recorded).Call("command_send", """{"entity":"Purse","command":"Add","args":["1","2"]}""");
        recorded.Step(5);
        Recording.TryRead(recorder.Stop().ToBytes(), out var recording);
        using var fresh = GameInstance.Create(new SmokeGame(), new GameOptions { Headless = true, Gpu = GpuMode.Off });
        var replay = fresh.Replay(recording!);

        // A session across a reset and a state patch, saved and replayed; the UI scale override; every controller button by name.
        using var session = GameInstance.Create(new SmokeGame(), new GameOptions { Headless = true, Gpu = GpuMode.Off });
        var sessionControl = new ControlSurface(session);
        var sessionRecorder = session.StartSessionRecording();
        session.Step(3);
        sessionControl.Call("reset", """{"seed":"9"}""");
        session.Step(3);
        sessionControl.Call("ui_scale", """{"scale":1.5}""");
        sessionControl.Call("input_nav", """{"action":"start"}""");
        var sessionPath = Path.Combine(Path.GetTempPath(), "aot-smoke-" + Environment.ProcessId + ".ogrp");
        RecordingFile.Save(sessionRecorder.Stop(), sessionPath);
        using var sessionFresh = GameInstance.Create(new SmokeGame(), new GameOptions { Headless = true, Gpu = GpuMode.Off });
        var sessionReplay = sessionFresh.Replay(RecordingFile.LoadSession(sessionPath));
        File.Delete(sessionPath);
        if (!sessionReplay.Ok || session.UiScale != 1.5f)
        {
            throw new InvalidOperationException("The session did not replay.");
        }

        // The MCP messages.
        var mcp = new McpProtocol(surface);
        var tools = mcp.Handle("""{"jsonrpc":"2.0","id":1,"method":"tools/list"}""");

        // The development tools: the listener, and a game run with a development option.
        var socket = Path.Combine(Path.GetTempPath(), "aot-smoke-" + Environment.ProcessId + ".sock");
        using (var server = new LiveServer(surface))
        {
            server.Start(socket);
            server.Pump();
        }

        Dev.Enable();
        var devOutput = new StringWriter();
        var devExit = GameApp.Run(new SmokeGame(), ["--headless", "--frames", "2", "--capture", "aot-smoke.png", "--artifacts", Path.GetTempPath(), "--json"], devOutput, devOutput);

        if (devExit != 0 || !devOutput.ToString().Contains("\"sha256\"", StringComparison.Ordinal))
        {
            Console.Error.WriteLine($"harness smoke failed: dev run exit {devExit}: {devOutput}");
            return 1;
        }

        if (!result.Passed || back.Width != 160 || back.Height != 100 || !replay.Ok || tools is null || !tools.Contains("capture_frame", StringComparison.Ordinal)
            || state.Length == 0 || tree.Length == 0 || !themeLint.Contains("\"checks\"", StringComparison.Ordinal) || !overlay.Contains("entities", StringComparison.Ordinal) || applied != 1 || !shaders.Contains("\"changed\":[\"smoke\"]", StringComparison.Ordinal))
        {
            Console.Error.WriteLine($"harness smoke failed: scenario={result.Passed} image={back.Width}x{back.Height} replay={replay.Ok}");
            return 1;
        }

        Console.WriteLine($"ok: harness; scenario hash {result.Hash}; png {png.Length} bytes; replay of {recording!.EndTick - recording.StartTick} ticks identical");
        return 0;
    }

    private static byte[] WavTone()
    {
        const int Rate = 48_000;
        var bytes = new byte[44 + (Rate * 2)];
        "RIFF"u8.CopyTo(bytes);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4), bytes.Length - 8);
        "WAVEfmt "u8.CopyTo(bytes.AsSpan(8));
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(16), 16);
        System.Buffers.Binary.BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(20), 1);
        System.Buffers.Binary.BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(22), 1);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(24), Rate);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(28), Rate * 2);
        System.Buffers.Binary.BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(32), 2);
        System.Buffers.Binary.BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(34), 16);
        "data"u8.CopyTo(bytes.AsSpan(36));
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(40), Rate * 2);
        for (var i = 0; i < Rate; i++)
        {
            System.Buffers.Binary.BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(44 + i * 2), (short)(Math.Sin(i * Math.PI / 24) * 3000));
        }

        return bytes;
    }
}
