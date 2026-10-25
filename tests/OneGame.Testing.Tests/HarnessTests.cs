using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Net.Sockets;
using OneGame.Control;
using OneGame.Core;
using OneGame.Host;
using OneGame.Sync;
using OneGame.Live;

namespace OneGame.Testing.Tests;

public class HarnessTests
{
    private static string ScenarioPath(string name) => Path.Combine(AppContext.BaseDirectory, "scenarios", name);

    private static TestOptions Options() => new() { Width = 320, Height = 200, Gpu = GpuMode.Off };

    [Fact]
    public void ControlOperationsQueryPatchSendAndStep()
    {
        using var cluster = TestCluster.Start(new ShopGame(), Options());
        var c = cluster.Client;

        using (var info = JsonDocument.Parse(c.Call("info")))
        {
            Assert.Equal("ShopGame", info.RootElement.GetProperty("game").GetString());
            Assert.Equal(1, info.RootElement.GetProperty("entities").GetInt32());
        }

        Assert.Equal("3", c.Get("Shop", "Stock"));
        c.Call("state_patch", """{"entity":"Shop","property":"Stock","value":"9"}""");
        Assert.Equal("9", c.Get("Shop#1", "Stock"));

        c.Send("Shop", "Buy", "2").Step();
        Assert.Equal("7", c.Get("1", "Stock"));
        Assert.Equal("sold 2", c.Get("Shop", "Status"));
        Assert.Equal(1, c.Tick);

        // Text arguments are parsed per parameter type.
        c.Send("Shop", "Rename", "hello", "true").Step();
        Assert.Equal("HELLO", c.Get("Shop", "Status"));

        var wrongCount = Assert.Throws<ControlException>(() => c.Send("Shop", "Buy"));
        Assert.Equal("bad_args", wrongCount.Code);
        Assert.Contains("Buy(int count)", wrongCount.Message, StringComparison.Ordinal);
        Assert.Equal("not_found", Assert.Throws<ControlException>(() => c.Send("Shop", "Steal", "1")).Code);
        Assert.Equal("not_found", Assert.Throws<ControlException>(() => c.Get("Bank", "Gold")).Code);
        Assert.Equal("rejected", Assert.Throws<ControlException>(() => c.Call("state_patch", """{"entity":"Shop","property":"Stock","value":"many"}""")).Code);
        Assert.Equal("unknown_op", Assert.Throws<ControlException>(() => c.Call("explode")).Code);
        Assert.Equal("bad_args", Assert.Throws<ControlException>(() => c.Call("step", "{\"frames\":0}")).Code);
    }

    [Fact]
    public void InputGoesThroughThePlatformQueueToTheUi()
    {
        using var cluster = TestCluster.Start(new ShopGame(), Options());
        var c = cluster.Client;
        c.Click("buy").Step();
        Assert.Equal("2", c.Get("Shop", "Stock"));

        // Keyboard: the click left focus on Buy, so Tab moves to Restock; Enter presses it.
        c.Press("Tab").Press("Enter").Step();
        Assert.Equal("4", c.Get("Shop", "Stock"));

        // Controller: navigation actions come from a virtual gamepad.
        c.Call("input_nav", """{"action":"previous"}""");
        c.Call("input_nav", """{"action":"activate"}""");
        c.Step();
        Assert.Equal("3", c.Get("Shop", "Stock"));

        var hit = JsonDocument.Parse(c.Call("ui_hit", """{"x":10,"y":10}"""));
        Assert.NotNull(hit.RootElement.GetProperty("element").GetString());
    }

    [Fact]
    public void ClickOnAnObscuredElementSaysSo()
    {
        using var cluster = TestCluster.Start(new ShopGame(), Options());
        cluster.Client.Call("screen_open", """{"name":"help"}""");
        var ex = Assert.Throws<ControlException>(() => cluster.Client.Click("buy"));
        Assert.Equal("not_found", ex.Code);
    }

    [Fact]
    public void ScenarioPassesFromAFile()
    {
        using var cluster = TestCluster.Start(new ShopGame(), Options());
        var result = cluster.Run(ScenarioPath("buy-flow.yaml"));
        Assert.True(result.Passed, JsonSerializer.Serialize(result, ScenarioJson.Default.ScenarioResult));
        Assert.Equal(3, result.Tick);
        Assert.Equal(7, result.Expectations.Length);
    }

    [Fact]
    public void FailingScenarioReportsAJsonDiff()
    {
        using var cluster = TestCluster.Start(new ShopGame(), Options());
        var result = cluster.Run(ScenarioPath("fails.yaml"));
        Assert.False(result.Passed);
        var json = JsonSerializer.Serialize(result, ScenarioJson.Default.ScenarioResult);
        using var doc = JsonDocument.Parse(json);
        var failed = doc.RootElement.GetProperty("expectations")[0];
        Assert.False(failed.GetProperty("passed").GetBoolean());
        Assert.Equal("Shop.Gold", failed.GetProperty("target").GetString());
        Assert.Equal("99", failed.GetProperty("expected").GetString());
        Assert.Equal("5", failed.GetProperty("actual").GetString());
        Assert.Equal(2, result.Expectations.Count(e => !e.Passed));
    }

    [Fact]
    public void ScenarioFilesAreStrict()
    {
        var ex = Assert.Throws<ScenarioException>(() => ScenarioParser.Load(ScenarioPath("unknown-key.yaml")));
        Assert.Equal(3, ex.Line);
        Assert.Contains("clik", ex.Message, StringComparison.Ordinal);
        Assert.Throws<ScenarioException>(() => ScenarioParser.Parse("name: x\nbogus: 1\n"));
        Assert.Throws<ScenarioException>(() => ScenarioParser.Parse("steps:\n  - click: {id: a, x: 1}\n"));
        Assert.Throws<ScenarioException>(() => ScenarioParser.Parse("expect:\n  - state: {entity: Shop, property: Gold, colour: red}\n"));
        Assert.Throws<ScenarioException>(() => ScenarioParser.Parse("a: [1\n"));
    }

    [Fact]
    public void StepErrorStopsTheScenarioAndNamesTheLine()
    {
        using var cluster = TestCluster.Start(new ShopGame(), Options());
        var scenario = ScenarioParser.Parse("steps:\n  - click: nowhere\n  - step: 5\nexpect:\n  - tick: 5\n", "inline.yaml", "inline");
        var result = cluster.Run(scenario);
        Assert.False(result.Passed);
        Assert.Single(result.Steps);
        Assert.Equal(2, result.Steps[0].Line);
        Assert.Contains("not_found", result.Steps[0].Error, StringComparison.Ordinal);
        Assert.Empty(result.Expectations);
    }

    [Fact]
    public void CaptureWritesAPngAtANamedSizeAndComparesToAReference()
    {
        using var cluster = TestCluster.Start(new ShopGame(), Options());
        var c = cluster.Client;
        var result = JsonDocument.Parse(c.Call("capture_frame", """{"size":"360p","path":"a.png"}"""));
        Assert.Equal(640, result.RootElement.GetProperty("width").GetInt32());
        Assert.Equal(360, result.RootElement.GetProperty("height").GetInt32());
        var file = Path.Combine(cluster.OutputDirectory, "a.png");
        var png = Png.Load(file);
        Assert.Equal((640, 360), (png.Width, png.Height));
        Assert.Equal(0x20, png.Rgba[0]); // the screen's root background
        Assert.Equal(StubStage.Clear.R, png.Rgba[(((png.Height - 1) * png.Width) + (png.Width - 1)) * 4]); // outside the UI box there is the clear color

        // The same state again is the same frame; a changed state is not.
        var again = c.Capture("640x360");
        Assert.Equal(0, ImageCompare.Compare(again, png).DifferingPixels);
        c.Call("state_patch", """{"entity":"Shop","property":"Gold","value":"12345"}""");
        var changed = c.Capture("640x360");
        var diff = ImageCompare.Compare(changed, png);
        Assert.True(diff.DifferingPixels > 0, "buying changes the label text bar, so the frame must differ");

        // The same state at another UI scale is a different layout.
        var big = c.Capture("640x360", 2f);
        Assert.True(ImageCompare.Compare(big, changed).DifferingPixels > 0);

        // A path outside the output folder is refused.
        Assert.Equal("path_outside", Assert.Throws<ControlException>(() => c.Call("capture_frame", """{"path":"../x.png"}""")).Code);
    }

    [Fact]
    public void CaptureCompareFailsOnAChangedFrameWithinAndOutsideTolerance()
    {
        using var cluster = TestCluster.Start(new ShopGame(), Options());
        var refDir = Path.Combine(cluster.OutputDirectory, "refs");
        var artifacts = Path.Combine(cluster.OutputDirectory, "art");
        var scenario = ScenarioParser.Parse(
            "window: 320x200\nsteps:\n  - step: 1\nexpect:\n  - capture: { reference: refs/x.png }\n", Path.Combine(cluster.OutputDirectory, "s.yaml"), "cap");
        var missing = cluster.Run(scenario, new ScenarioRunOptions { ArtifactsDirectory = artifacts });
        Assert.False(missing.Passed);
        Assert.Contains("ONEGAME_UPDATE_REFERENCES", missing.Expectations[0].Message, StringComparison.Ordinal);

        var created = cluster.Run(scenario, new ScenarioRunOptions { ArtifactsDirectory = artifacts, UpdateReferences = true });
        Assert.True(created.Passed);
        Assert.Equal("created", created.Expectations[0].ReferenceAction);
        Assert.True(File.Exists(Path.Combine(refDir, "x.png")));
        Assert.True(cluster.Run(scenario, new ScenarioRunOptions { ArtifactsDirectory = artifacts }).Passed);

        cluster.Client.Call("state_patch", """{"entity":"Shop","property":"Gold","value":"12345"}""");
        var changed = cluster.Run(scenario, new ScenarioRunOptions { ArtifactsDirectory = artifacts });
        Assert.False(changed.Passed);
        Assert.Contains("px", changed.Expectations[0].Message, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(artifacts, "cap.0.actual.png")));
        Assert.True(File.Exists(Path.Combine(artifacts, "cap.0.diff.png")));

        // A generous allowance accepts the same change.
        var lenient = ScenarioParser.Parse(
            "window: 320x200\nexpect:\n  - capture: { reference: refs/x.png, max-diff: 0.5 }\n", Path.Combine(cluster.OutputDirectory, "s.yaml"), "cap");
        Assert.True(cluster.Run(lenient, new ScenarioRunOptions { ArtifactsDirectory = artifacts }).Passed);
    }

    [Fact]
    public void CaptureSequenceWritesFramesAndAManifest()
    {
        using var cluster = TestCluster.Start(new ShopGame(), Options());
        var c = cluster.Client;
        c.Send("Shop", "Buy", "1");
        var result = JsonDocument.Parse(c.Call("capture_sequence", """{"dir":"seq","count":4,"ticksPerFrame":2,"size":"160x100"}"""));
        Assert.Equal(4, result.RootElement.GetProperty("frames").GetInt32());
        var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(cluster.OutputDirectory, "seq", "manifest.json")));
        Assert.Equal(30, manifest.RootElement.GetProperty("fps").GetInt32());
        var frames = manifest.RootElement.GetProperty("frames");
        Assert.Equal(4, frames.GetArrayLength());
        Assert.Equal(6, frames[3].GetProperty("tick").GetInt64());
        Assert.True(File.Exists(Path.Combine(cluster.OutputDirectory, "seq", "frame-0003.png")));
    }

    private static void Drive(TestClient c, int frame)
    {
        if (frame % 7 == 0)
        {
            c.Send("Shop", "Restock", "2");
        }

        if (frame % 3 == 0)
        {
            c.Send("Shop", "Buy", "1");
        }
    }

    [Fact]
    public void DeterminismHelpersAgree()
    {
        Determinism.RunTwice(() => new ShopGame(), Drive, 60, Options()).ThrowIfDifferent();
        Determinism.SnapshotRestore(() => new ShopGame(), Drive, 60, 25, Options()).ThrowIfDifferent();
        var replay = Determinism.Replay(() => new ShopGame(), Drive, 60, Options());
        replay.ThrowIfDifferent();
        Assert.Equal(60, replay.Frames);
    }

    [Fact]
    public void DeterminismHelpersCatchDivergence()
    {
        var calls = 0;
        var report = Determinism.RunTwice(() => new ShopGame(), (c, f) => { if (f == 10 && calls++ == 1) { c.Send("Shop", "Restock", "1"); } }, 20, Options());
        Assert.False(report.Identical);
        Assert.Equal(10, report.FirstMismatchFrame);
        Assert.Throws<DeterminismException>(report.ThrowIfDifferent);
    }

    [Fact]
    public void RecordingRoundTripsAndRejectsDamagedBytes()
    {
        using var cluster = TestCluster.Start(new ShopGame(), Options());
        var recorder = cluster.Client.Game.StartRecording();
        for (var f = 0; f < 20; f++)
        {
            Drive(cluster.Client, f);
            cluster.Client.Step();
        }

        var recording = recorder.Stop();
        var bytes = recording.ToBytes();
        Assert.Equal(SyncError.None, Recording.TryRead(bytes, out var back));
        Assert.Equal(recording.Commands.Count, back!.Commands.Count);
        Assert.True(back.Commands.Count > 0);

        using var fresh = TestCluster.Start(new ShopGame(), Options());
        var ok = fresh.Client.Game.Replay(back);
        Assert.True(ok.Ok, ok.Divergence?.ToString());
        Assert.Equal(cluster.Client.Hash, ok.FinalHash);

        // Drop a recorded command: the replay diverges.
        Assert.Equal(SyncError.None, Recording.TryRead(bytes, out var tampered));
        tampered!.Commands.RemoveAt(0);
        using var fresh2 = TestCluster.Start(new ShopGame(), Options());
        var bad = fresh2.Client.Game.Replay(tampered);
        Assert.False(bad.Ok);
        Assert.NotNull(bad.Divergence);

        // Damaged files are refused, never thrown on.
        for (var cut = 0; cut < bytes.Length; cut += Math.Max(1, bytes.Length / 40))
        {
            Assert.NotEqual(SyncError.None, Recording.TryRead(bytes.AsSpan(0, cut), out _));
        }

        var flipped = (byte[])bytes.Clone();
        flipped[0] ^= 0xFF;
        Assert.NotEqual(SyncError.None, Recording.TryRead(flipped, out _));
    }

    [Fact]
    public void LocalReplicaStaysInSyncWithTheAuthority()
    {
        using var cluster = TestCluster.Start(new ShopGame(), Options());
        for (var f = 0; f < 40; f++)
        {
            Drive(cluster.Client, f);
            cluster.Client.Step();
        }

        var game = cluster.Client.Game;
        Assert.Empty(game.DriftReports);
        Assert.Empty(game.Cluster.CheckReplicas());
        Assert.Equal(game.World.ComputeHash(), game.Replica.World.ComputeHash());
    }

    [Fact]
    public void SnapshotAndFixtureOperations()
    {
        using var cluster = TestCluster.Start(new ShopGame(), Options());
        var c = cluster.Client;
        c.Send("Shop", "Buy", "2").Step(3);
        var saved = JsonDocument.Parse(c.Call("snapshot_save", """{"path":"s.bin"}"""));
        var hash = saved.RootElement.GetProperty("hash").GetString();
        c.Call("fixture_load", """{"name":"rich"}""");
        Assert.Equal("50", c.Get("Shop", "Stock"));
        c.Call("snapshot_load", """{"path":"s.bin"}""");
        using var hashDoc = JsonDocument.Parse(c.Call("hash"));
        Assert.Equal(hash, hashDoc.RootElement.GetProperty("hash").GetString());
        Assert.Equal("1", c.Get("Shop", "Stock"));
        Assert.Equal("bad_data", Assert.Throws<ControlException>(() => c.Call("snapshot_load", "{\"base64\":\"AAAA\"}")).Code);
        Assert.Equal("not_found", Assert.Throws<ControlException>(() => c.Call("fixture_load", """{"name":"poor"}""")).Code);
        c.Call("reset", "{\"seed\":7}");
        Assert.Equal("3", c.Get("Shop", "Stock"));
    }

    // ---- MCP ----

    private static JsonElement Rpc(McpProtocol mcp, string request)
    {
        var response = mcp.Handle(request);
        Assert.NotNull(response);
        return JsonDocument.Parse(response!).RootElement.Clone();
    }

    [Fact]
    public void McpProtocolFollowsTheSpecShapes()
    {
        using var cluster = TestCluster.Start(new ShopGame(), Options());
        var mcp = new McpProtocol(cluster.Client.Control);

        var init = Rpc(mcp, """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-03-26","capabilities":{},"clientInfo":{"name":"t","version":"1"}}}""");
        Assert.Equal("2025-03-26", init.GetProperty("result").GetProperty("protocolVersion").GetString());
        Assert.True(init.GetProperty("result").GetProperty("capabilities").TryGetProperty("tools", out _));
        Assert.Null(mcp.Handle("""{"jsonrpc":"2.0","method":"notifications/initialized"}"""));
        Assert.Equal("2025-06-18", Rpc(mcp, """{"jsonrpc":"2.0","id":"a","method":"initialize","params":{"protocolVersion":"1999-01-01"}}""").GetProperty("result").GetProperty("protocolVersion").GetString());

        var tools = Rpc(mcp, """{"jsonrpc":"2.0","id":2,"method":"tools/list"}""").GetProperty("result").GetProperty("tools");
        Assert.Equal(ControlOps.All.Count, tools.GetArrayLength());
        foreach (var t in tools.EnumerateArray())
        {
            Assert.Matches("^[a-z_]{1,64}$", t.GetProperty("name").GetString()!);
            Assert.Equal("object", t.GetProperty("inputSchema").GetProperty("type").GetString());
        }

        var call = Rpc(mcp, """{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"state_query","arguments":{"entity":"Shop","property":"Stock"}}}""");
        Assert.False(call.GetProperty("result").GetProperty("isError").GetBoolean());
        Assert.Equal("3", call.GetProperty("result").GetProperty("content")[0].GetProperty("text").GetString());

        var failed = Rpc(mcp, """{"jsonrpc":"2.0","id":4,"method":"tools/call","params":{"name":"state_query","arguments":{"entity":"Bank"}}}""");
        Assert.True(failed.GetProperty("result").GetProperty("isError").GetBoolean());
        Assert.Contains("not_found", failed.GetProperty("result").GetProperty("content")[0].GetProperty("text").GetString(), StringComparison.Ordinal);

        Assert.Equal(-32602, Rpc(mcp, """{"jsonrpc":"2.0","id":5,"method":"tools/call","params":{"name":"nope"}}""").GetProperty("error").GetProperty("code").GetInt32());
        Assert.Equal(-32601, Rpc(mcp, """{"jsonrpc":"2.0","id":6,"method":"resources/list"}""").GetProperty("error").GetProperty("code").GetInt32());
        Assert.Equal(-32700, Rpc(mcp, "{not json").GetProperty("error").GetProperty("code").GetInt32());
        Assert.Equal(-32600, Rpc(mcp, "[]").GetProperty("error").GetProperty("code").GetInt32());
        Assert.Equal(JsonValueKind.Object, Rpc(mcp, """{"jsonrpc":"2.0","id":7,"method":"ping"}""").GetProperty("result").ValueKind);
    }

    [Fact]
    public void LiveListenerOnEveryInterfaceAcceptsIpv4AndIpv6Connections()
    {
        // "tcp:*:port" is for a device the computer reaches over a cable's network tunnel; plain "tcp:port" stays on the loopback.
        using var cluster = TestCluster.Start(new ShopGame(), Options());
        using var server = new LiveServer(cluster.Client.Control);
        int port;
        using (var probe = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0))
        {
            probe.Start();
            port = ((System.Net.IPEndPoint)probe.LocalEndpoint).Port;
        }

        server.Start($"tcp:*:{port}");
        Assert.Equal($"tcp:*:{port}", server.Path);
        foreach (var address in new[] { System.Net.IPAddress.Loopback, System.Net.IPAddress.IPv6Loopback })
        {
            using var client = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            client.Connect(address, port);
            Assert.True(client.Connected);
        }

        using var other = new LiveServer(cluster.Client.Control);
        Assert.Throws<IOException>(() => other.Start("tcp:*:0"));
    }

    [Fact]
    public void AgentDrivesARunningGameOverTheLiveSocket()
    {
        using var cluster = TestCluster.Start(new ShopGame(), Options());
        var game = cluster.Client.Game;
        using var server = new LiveServer(cluster.Client.Control);
        var socketPath = Path.Combine(cluster.OutputDirectory, "live.sock");
        server.Start(socketPath);

        // The agent is a client on its own thread; the game thread pumps requests, as a running game's loop does.
        var done = new ManualResetEventSlim();
        var transcript = new List<string>();
        Exception? failure = null;
        var agent = new Thread(() =>
        {
            try
            {
                using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                socket.Connect(new UnixDomainSocketEndPoint(socketPath));
                using var stream = new NetworkStream(socket);
                using var reader = new StreamReader(stream, Encoding.UTF8);
                using var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true, NewLine = "\n" };
                var id = 0;
                string Send(string method, string parameters)
                {
                    writer.WriteLine($"{{\"jsonrpc\":\"2.0\",\"id\":{++id},\"method\":\"{method}\",\"params\":{parameters}}}");
                    var line = reader.ReadLine()!;
                    transcript.Add(line);
                    return line;
                }

                Send("initialize", """{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"agent","version":"1"}}""");
                writer.WriteLine("""{"jsonrpc":"2.0","method":"notifications/initialized"}""");
                Send("tools/list", "{}");
                Send("tools/call", """{"name":"state_query","arguments":{"entity":"Shop"}}""");
                Send("tools/call", """{"name":"state_patch","arguments":{"entity":"Shop","property":"Stock","value":"12"}}""");
                Send("tools/call", """{"name":"input_pointer","arguments":{"action":"click","x":40,"y":120}}""");
                Send("tools/call", """{"name":"input_key","arguments":{"key":"Tab"}}""");
                Send("tools/call", """{"name":"step","arguments":{"frames":10}}""");
                Send("tools/call", """{"name":"ui_tree","arguments":{"allStyles":false}}""");
                Send("tools/call", """{"name":"capture_frame","arguments":{"size":"160x100"}}""");
            }
            catch (Exception ex)
            {
                failure = ex;
            }
            finally
            {
                done.Set();
            }
        });
        agent.Start();
        var deadline = Environment.TickCount64 + 30000;
        while (!done.IsSet && Environment.TickCount64 < deadline)
        {
            server.Pump();
            server.WaitForWork(5);
        }

        agent.Join();
        Assert.Null(failure);
        Assert.Equal(9, transcript.Count);
        Assert.Contains("\"tools\"", transcript[1], StringComparison.Ordinal);
        Assert.Contains("Shop", transcript[2], StringComparison.Ordinal);
        Assert.Contains("\"type\":\"image\"", transcript[^1], StringComparison.Ordinal);
        using (var stepResult = JsonDocument.Parse(JsonDocument.Parse(transcript[6]).RootElement.GetProperty("result").GetProperty("content")[0].GetProperty("text").GetString()!))
        {
            Assert.Equal(10, stepResult.RootElement.GetProperty("tick").GetInt64());
        }

        Assert.Equal(10, game.World.Tick);
        Assert.Equal("14", cluster.Client.Get("Shop", "Stock")); // 12 patched, +2 from the Restock button the click landed on
    }
}
