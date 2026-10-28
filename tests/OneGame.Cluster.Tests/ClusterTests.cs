using System.Diagnostics;
using System.Text.Json;
using OneGame.Control;

namespace OneGame.Cluster.Tests;

public class ClusterTests
{
    private static ClusterSpec Spec(string name, params NodeSpec[] nodes) => new() { Name = name, Nodes = nodes, StartupTimeoutMs = 90_000 };

    private static void AssertGone(IEnumerable<int> pids) => Assert.All(pids, pid => Assert.False(ProcessCluster.IsAlive(pid), $"process {pid} is still running"));

    [Fact]
    public void ThreeProcessesRunAScenarioFileWithNodeTargetedSteps()
    {
        var spec = Spec("three", Fixture.ServerNode("master", tickRate: 120), Fixture.ServerNode("srv", tickRate: 120), Fixture.ClientNode("p1", "srv"));
        int[] pids;
        using (var cluster = TestCluster.Launch(spec))
        {
            pids = [.. cluster.Pids];
            Assert.Equal(3, pids.Length);
            Assert.All(pids, pid => Assert.True(ProcessCluster.IsAlive(pid)));
            var result = cluster.Run(Fixture.Scenario("three-process.yaml"));
            Assert.True(result.Passed, JsonSerializer.Serialize(result, ScenarioJson.Default.ScenarioResult));
            Assert.Contains(result.Steps, s => s.Node == "p1" && s.Kind == "command");
            Assert.Contains(result.Expectations, e => e.Kind == "agree" && e.Passed);
            var report = cluster.Hashes();
            Assert.True(report.Ok, string.Join("; ", report.Problems));
            Assert.Single(report.Checks);
            Assert.Equal("srv", report.Checks[0].Server);

            // The master is its own process with its own world: p1 did not touch it.
            Assert.Equal("0", Json(cluster.Call("master", "state_query", "{\"entity\":\"Arena\",\"property\":\"Hits\"}")));
        }

        AssertGone(pids);
    }

    [Fact]
    public void AServerWhosePortWasTakenBeforeItBoundIsStartedAgainOnOtherPorts()
    {
        // Hold a port on both protocols, as another process would, and hand it out as the server's port once.
        using var udp = new System.Net.Sockets.UdpClient(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 0));
        var taken = ((System.Net.IPEndPoint)udp.Client.LocalEndPoint!).Port;
        using var tcp = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, taken);
        tcp.Start();
        var handed = 0;
        ProcessCluster.PortSource = () =>
        {
            if (handed++ == 0)
            {
                return taken;
            }

            using var probe = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            probe.Start();
            return ((System.Net.IPEndPoint)probe.LocalEndpoint).Port;
        };
        try
        {
            using var cluster = TestCluster.Launch(Spec("race", Fixture.ServerNode("srv", tickRate: 120), Fixture.ClientNode("p1", "srv")));
            Assert.True(handed > 2, "the start should have asked for new ports after the first attempt failed");
            Assert.True(cluster.Hashes().Ok);
        }
        finally
        {
            ProcessCluster.PortSource = null;
        }
    }

    [Fact]
    public void TwoClientsOverUdpAndWebSocketStayInSyncThroughALossyNetwork()
    {
        var spec = Spec(
            "two",
            Fixture.ServerNode("arena", "udp+ws", tickRate: 240),
            Fixture.ClientNode("p1", "arena", "udp", "latency=100,loss=0.02,seed=3"),
            Fixture.ClientNode("p2", "arena", "ws"));
        using var cluster = TestCluster.Launch(spec);
        var target = 1200;
        for (var i = 0; i < 40; i++)
        {
            cluster.Call("p1", "command_send", "{\"entity\":\"Arena\",\"command\":\"Hit\",\"args\":[\"1\",\"p1\"]}");
            cluster.Call("p2", "command_send", "{\"entity\":\"Fighter#1\",\"command\":\"AddScore\",\"args\":[\"2\"]}");
            Thread.Sleep(30);
        }

        Assert.True(cluster.WaitForTick("p1", target, 60_000));
        Assert.True(cluster.WaitForTick("p2", target, 60_000));
        var report = cluster.Hashes();
        Assert.True(report.Ok, string.Join("; ", report.Problems));
        Assert.Equal(2, report.Checks.Length);
        Assert.All(report.Checks, c => Assert.True(c.Tick >= target && c.Match));
        Assert.Empty(report.Drift);
        Assert.Equal("40", Json(cluster.Call("arena", "state_query", "{\"entity\":\"Arena\",\"property\":\"Hits\"}")));
        Assert.Equal("80", Json(cluster.Call("arena", "state_query", "{\"entity\":\"Fighter#1\",\"property\":\"Score\"}")));
        using var status = JsonDocument.Parse(cluster.Call("p1", "net_status"));
        Assert.Equal("udp", status.RootElement.GetProperty("transport").GetString());
        Assert.True(status.RootElement.GetProperty("rttMs").GetDouble() >= 90, "the conditioner's latency should show in the round-trip time");
    }

    [Fact]
    public void AnInjectedDivergenceFailsWithADriftReportNamingNodeScopeAndTick()
    {
        var spec = Spec("drift", Fixture.ServerNode("srv", tickRate: 240), Fixture.ClientNode("p1", "srv"), Fixture.ClientNode("p2", "srv"));
        using var cluster = TestCluster.Launch(spec);
        Assert.True(cluster.Hashes().Ok);
        cluster.Call("p1", "net_diverge", "{\"entity\":\"Fighter#2\"}");

        // The server's frames carry its hash every 30 ticks; the replica compares and files a report.
        DriftEntry? drift = null;
        var clock = Stopwatch.StartNew();
        while (drift is null && clock.Elapsed < TimeSpan.FromSeconds(20))
        {
            Thread.Sleep(50);
            drift = cluster.Hashes().Drift.FirstOrDefault();
        }

        Assert.NotNull(drift);
        Assert.Equal("p1", drift!.Node);
        Assert.Equal(1u, drift.Scope);
        Assert.True(drift.Tick > 0);
        Assert.NotEqual(drift.Expected, drift.Actual);
        Assert.Contains("Fighter", string.Join(" ", drift.Differences));

        var report = cluster.Hashes();
        Assert.False(report.Ok);
        Assert.Contains(report.Problems, p => p.StartsWith("p1:", StringComparison.Ordinal) && p.Contains("scope 1", StringComparison.Ordinal) && p.Contains($"tick {drift.Tick}", StringComparison.Ordinal));
        Assert.DoesNotContain(report.Drift, d => d.Node == "p2");

        var scenario = ScenarioParser.Parse("name: no-drift\nexpect:\n  - drift: { nodes: [p1, p2] }\n");
        var result = cluster.Run(scenario);
        Assert.False(result.Passed);
        var failed = Assert.Single(result.Expectations, e => !e.Passed);
        Assert.Equal("p1", failed.Node);
        Assert.Contains($"scope 1 at tick {drift.Tick}", failed.Message);
        Assert.Contains(result.Expectations, e => e.Passed && e.Node == "p2");
    }

    [Fact]
    public void ACrashedTestLeavesNoProcessesBehind()
    {
        // The nodes watch the process that started them. Stand in for a test host that dies: a process we kill.
        using var sleeper = Process.Start(OperatingSystem.IsWindows() ? new ProcessStartInfo("ping", "-n 120 127.0.0.1") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true } : new ProcessStartInfo("/bin/sleep", "120") { UseShellExecute = false })!;
        var spec = Spec("orphans", Fixture.ServerNode("srv"), Fixture.ClientNode("p1", "srv"));
        var cluster = ProcessCluster.Up(spec, sleeper.Id);
        int[] pids = [.. cluster.Pids];
        try
        {
            Assert.All(pids, pid => Assert.True(ProcessCluster.IsAlive(pid)));
            sleeper.Kill();
            sleeper.WaitForExit();
            var clock = Stopwatch.StartNew();
            while (pids.Any(ProcessCluster.IsAlive) && clock.Elapsed < TimeSpan.FromSeconds(15))
            {
                Thread.Sleep(100);
            }

            AssertGone(pids);
        }
        finally
        {
            cluster.Dispose();
        }
    }

    [Fact]
    public void DownFromAnotherProcessWorksThroughTheStateFile()
    {
        var spec = Spec("attach", Fixture.ServerNode("srv", tickRate: 120), Fixture.ClientNode("p1", "srv"));
        var owner = ProcessCluster.Up(spec);
        var pids = owner.Pids.ToArray();
        var attached = ProcessCluster.Attach(owner.Dir);
        var status = attached.Status();
        Assert.All(status.Nodes, n => Assert.True(n.Alive));
        Assert.True(attached.Hashes().Ok);
        var down = attached.Down();
        Assert.Empty(down.Alive);
        AssertGone(pids);
        owner.Dispose();
    }

    [Fact]
    public void KillingAndStartingNodesReplacesTheirProcessesAndKeepsThePorts()
    {
        var spec = Spec("restart", Fixture.ServerNode("master", tickRate: 120), Fixture.ServerNode("srv", tickRate: 120), Fixture.ClientNode("p1", "srv"));
        int[] pids;
        using (var cluster = TestCluster.Launch(spec))
        {
            var first = cluster.Pids.ToArray();
            var port = cluster.Node("srv").Listen[0].Port;
            Assert.Throws<ClusterException>(() => cluster.Start("srv")); // still running
            var result = cluster.Run(Fixture.Scenario("restart.yaml"));
            Assert.True(result.Passed, JsonSerializer.Serialize(result, ScenarioJson.Default.ScenarioResult));
            Assert.Contains(result.Steps, s => s.Kind == "kill" && s.Node == "master");
            Assert.Contains(result.Steps, s => s.Kind == "restart" && s.Node == "srv");
            var second = cluster.Pids.ToArray();
            Assert.All(second, pid => Assert.True(ProcessCluster.IsAlive(pid)));
            Assert.All(first, pid => Assert.DoesNotContain(pid, second));
            Assert.All(first, pid => Assert.False(ProcessCluster.IsAlive(pid), $"the old process {pid} is still running"));
            Assert.Equal(port, cluster.Node("srv").Listen[0].Port);
            Assert.True(cluster.Hashes().Ok);

            // Someone else's cluster can have a node killed (that needs only its pid) but cannot start one again (that needs the spec).
            var attached = ProcessCluster.Attach(cluster.Dir);
            var victim = attached.Node("p1").Pid;
            attached.Kill("p1");
            Assert.False(ProcessCluster.IsAlive(victim));
            Assert.Equal("not_owner", Assert.Throws<ClusterException>(() => attached.Start("p1")).Code);
            pids = [.. first, .. second];
        }

        AssertGone(pids);
    }

    [Fact]
    public void AReleaseClientRunsToATickOnItsOwnAndItsPrintedResultIsComparedWithItsServer()
    {
        var release = Fixture.ClientNode("p1", "srv") with { Release = true, Args = ["--until-tick", "300"] };
        var spec = Spec("release", Fixture.ServerNode("srv", tickRate: 120), release);
        int[] pids;
        using (var cluster = TestCluster.Launch(spec))
        {
            pids = [.. cluster.Pids];
            Assert.True(cluster.Node("p1").Release);
            Assert.Equal("no_live", Assert.Throws<ClusterException>(() => cluster.Call("p1", "net_status")).Code);
            Assert.Contains("still running", string.Join("; ", cluster.Hashes().Problems));
            Assert.True(cluster.WaitForTick("p1", 300, 30_000), "the release client should stop at its tick");
            var report = cluster.Hashes();
            Assert.True(report.Ok, string.Join("; ", report.Problems));
            var check = Assert.Single(report.Checks);
            Assert.True(check.Match);
            Assert.True(check.Tick >= 300);
            Assert.Empty(report.Drift);
        }

        AssertGone(pids);
        Assert.Throws<ScenarioException>(() => ClusterSpec.Parse("nodes:\n  - { name: a, role: server, exe: x }\n  - { name: b, role: client, exe: x, connect: a, release: true }\n"));
    }

    [Fact]
    public void ASingleGameRefusesStepsAimedAtNodes()
    {
        var scenario = ScenarioParser.Parse("steps:\n  - step: 1\n    node: srv\n");
        Assert.Equal("srv", scenario.Steps[0].Node);
        using var game = TestCluster.Start(new EmptyGame());
        var result = game.Run(scenario);
        Assert.False(result.Passed);
        Assert.Contains("cluster", result.Steps[0].Error);
    }

    [Fact]
    public void SpecsAreStrict()
    {
        var ok = ClusterSpec.Parse("name: x\nnodes:\n  - { name: a, role: server, exe: /bin/true, listen: udp+ws }\n  - { name: b, role: client, exe: /bin/true, connect: a, transport: ws, net: { latencyMs: 50, lossRate: 0.01 } }\n");
        Assert.Equal("latency=50,loss=0.01", ok.Nodes[1].Net);
        Assert.Throws<ScenarioException>(() => ClusterSpec.Parse("nodes:\n  - { name: a, role: server, exe: x, bogus: 1 }\n"));
        Assert.Throws<ScenarioException>(() => ClusterSpec.Parse("nodes:\n  - { name: b, role: client, exe: x, connect: nowhere }\n"));
        Assert.Throws<ScenarioException>(() => ClusterSpec.Parse("nodes:\n  - { name: a, role: server, exe: x, listen: udp }\n  - { name: b, role: client, exe: x, connect: a, transport: ws }\n"));
    }

    private sealed class EmptyGame : OneGame.Host.GameDefinition
    {
        public override string Name => "Empty";

        public override void RegisterEntities(OneGame.Core.EntityRegistry registry)
        {
        }

        public override void Setup(OneGame.Core.World world)
        {
        }

        public override void DefineScreens(OneGame.Host.ScreenSet screens) => screens.Add("main", _ => OneGame.UI.Ui.Label("empty"));
    }

    private static string Json(string text)
    {
        using var doc = JsonDocument.Parse(text);
        return doc.RootElement.ValueKind == JsonValueKind.String ? doc.RootElement.GetString()! : doc.RootElement.GetRawText();
    }
}
