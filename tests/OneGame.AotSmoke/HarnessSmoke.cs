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

    public override void Setup(World world) => world.Add(new Purse { Owner = "smoke" });

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

        // A scenario in YAML, run through the control layer.
        var scenario = ScenarioParser.Parse("""
            name: smoke
            steps:
              - click: add
              - step: 1
            expect:
              - ui: { id: total, contains: Total }
              - tick: 6
            """);
        var result = new ScenarioRunner(surface, new ScenarioRunOptions { ArtifactsDirectory = Path.GetTempPath() }).Run(scenario);

        // A capture through the CPU path, encoded and decoded again with OneImage.
        var png = surface.Invoke("capture_frame", """{"size":"160x100"}""").Png!;
        var back = Png.Decode(png);

        // Record, replay and compare.
        using var recorded = GameInstance.Create(new SmokeGame(), new GameOptions { Headless = true, Gpu = GpuMode.Off });
        var recorder = recorded.StartRecording();
        new ControlSurface(recorded).Call("command_send", """{"entity":"Purse","command":"Add","args":["1","2"]}""");
        recorded.Step(5);
        Recording.TryRead(recorder.Stop().ToBytes(), out var recording);
        using var fresh = GameInstance.Create(new SmokeGame(), new GameOptions { Headless = true, Gpu = GpuMode.Off });
        var replay = fresh.Replay(recording!);

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
            || state.Length == 0 || tree.Length == 0)
        {
            Console.Error.WriteLine($"harness smoke failed: scenario={result.Passed} image={back.Width}x{back.Height} replay={replay.Ok}");
            return 1;
        }

        Console.WriteLine($"ok: harness; scenario hash {result.Hash}; png {png.Length} bytes; replay of {recording!.EndTick - recording.StartTick} ticks identical");
        return 0;
    }
}
