using System.Text.Json;
using OneGame.Control;
using OneGame.Testing;

namespace Lanterns.Tests;

/// <summary>The whole game as separate programs: a master, two islands and two bots over real sockets.</summary>
public class ClusterTests
{
    private static string Program(string project, string exe)
    {
        // <artifacts>/bin/<this test app>/<configuration>/ sits next to <artifacts>/bin/<project>/<configuration>/
        var config = new DirectoryInfo(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar)).Name;
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", project, config, exe));
        return File.Exists(path) ? path : throw new FileNotFoundException(path + " is not built; build the test project first.");
    }

    private static NodeSpec Server(string name, string listen, uint scope, params string[] peers) => new()
    {
        Name = name,
        Role = NodeRole.Server,
        Exe = Program("Lanterns.Server", "LanternsServer"),
        Listen = listen,
        Scopes = [new ScopeSpec(scope, scope)],
        Args = ["--option", $"peers={{port:{name}.peer}}", .. peers.SelectMany(p => new[] { "--option", p }), "--option", "data={dir}/data"],
    };

    private static ClusterSpec Spec(string name) => new()
    {
        Name = name,
        StartupTimeoutMs = 90_000,
        Nodes =
        [
            Server("master", "udp", 3, "peer.1=udp://127.0.0.1:{port:islandA.peer}", "peer.2=udp://127.0.0.1:{port:islandB.peer}"),
            Server("islandA", "udp+ws", 1, "peer.2=udp://127.0.0.1:{port:islandB.peer}", "peer.3=udp://127.0.0.1:{port:master.peer}"),
            Server("islandB", "udp+ws", 2, "peer.1=udp://127.0.0.1:{port:islandA.peer}", "peer.3=udp://127.0.0.1:{port:master.peer}"),
            new NodeSpec { Name = "bot1", Role = NodeRole.Client, Exe = Program("Lanterns", "Lanterns"), Connect = "islandA", Scope = 1, Args = ["--also-scopes", "2,3"], Net = "latency=100,loss=0.02,seed=5" },
            new NodeSpec { Name = "bot2", Role = NodeRole.Client, Exe = Program("Lanterns", "Lanterns"), Connect = "islandB", Transport = "ws", Scope = 2, Args = ["--also-scopes", "1,3"] },
        ],
    };

    [Fact]
    public void TwoBotsPlayTwoThousandTicksOverRealSocketsWithZeroDrift()
    {
        int[] pids;
        using (var cluster = TestCluster.Launch(Spec("lanterns-play")))
        {
            pids = [.. cluster.Pids];
            var result = cluster.Run(Path.Combine(AppContext.BaseDirectory, "cluster", "play.yaml"));
            Assert.True(result.Passed, JsonSerializer.Serialize(result, ScenarioJson.Default.ScenarioResult));
            var report = cluster.Hashes();
            Assert.True(report.Ok, string.Join("; ", report.Problems));
            Assert.Equal(2, report.Checks.Length);
        }

        Assert.All(pids, pid => Assert.False(ProcessCluster.IsAlive(pid), $"process {pid} is still running"));
    }
}
