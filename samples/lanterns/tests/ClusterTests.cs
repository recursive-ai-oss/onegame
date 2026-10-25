using System.Text.Json;
using OneGame.Control;
using OneGame.Testing;

namespace Lanterns.Tests;

/// <summary>The whole game as separate programs: a master, two islands and two bots over real sockets.</summary>
public class ClusterTests
{
    internal static string Program(string project, string exe)
    {
        // <artifacts>/bin/<this test app>/<configuration>/ sits next to <artifacts>/bin/<project>/<configuration>/
        var config = new DirectoryInfo(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar)).Name;
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", project, config, exe + (OperatingSystem.IsWindows() ? ".exe" : "")));
        return File.Exists(path) ? path : throw new FileNotFoundException(path + " is not built; build the test project first.");
    }

    internal static NodeSpec Server(string name, string listen, uint scope, params string[] peers) => new()
    {
        Name = name,
        Role = NodeRole.Server,
        Exe = Program("Lanterns.Server", "LanternsServer"),
        Listen = listen,
        Scopes = [new ScopeSpec(scope, scope)],
        Args = ["--option", $"peers={{port:{name}.peer}}", .. peers.SelectMany(p => new[] { "--option", p }), "--option", "data={dir}/data"],
    };

    internal static ClusterSpec Spec(string name) => new()
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

    [Fact]
    public void KillingTheMasterMidSwapAndThenAnIslandLosesNoLanternAndEveryClientAgreesAfterwards()
    {
        int[] pids;
        using (var cluster = TestCluster.Launch(Spec("lanterns-crash")))
        {
            pids = [.. cluster.Pids];
            var result = cluster.Run(Path.Combine(AppContext.BaseDirectory, "cluster", "crash.yaml"));
            Assert.True(result.Passed, JsonSerializer.Serialize(result, ScenarioJson.Default.ScenarioResult));
            Assert.All(cluster.State.Nodes, n => Assert.True(ProcessCluster.IsAlive(n.Pid), n.Name + " should be running"));

            // Every lantern ever picked up is held by a player, left with one who disconnected, or is in a swap that has not settled. The islands are read one after the other while
            // the bots keep playing, so a read that catches a hand-over or a swap half way is skipped; a loss or a copy would show in every read.
            string last = "";
            var balanced = false;
            for (var attempt = 0; attempt < 60 && !balanced; attempt++)
            {
                Thread.Sleep(100);
                long held = 0, collected = 0;
                var quiet = true;
                foreach (var (node, scope) in new[] { ("islandA", 1), ("islandB", 2) })
                {
                    using var doc = JsonDocument.Parse(cluster.Call(node, "state_query", $"{{\"scope\":{scope}}}"));
                    foreach (var e in doc.RootElement.GetProperty("entities").EnumerateArray())
                    {
                        switch (e.GetProperty("type").GetString())
                        {
                            case "Player":
                                held += e.GetProperty("Total").GetInt64();
                                quiet &= e.GetProperty("Pending").GetInt64() == 0 && e.GetProperty("Dest").GetInt64() == 0;
                                break;
                            case "Island":
                                collected += e.GetProperty("Collected").GetInt64();
                                held += e.GetProperty("Departed").GetInt64(); // lanterns that left in the pockets of players who disconnected
                                break;
                        }
                    }

                    quiet &= doc.RootElement.GetProperty("tables").GetProperty("og.transfers").GetProperty("open").GetArrayLength() == 0;
                }

                using (var master = JsonDocument.Parse(cluster.Call("master", "state_query", "{\"scope\":3}")))
                {
                    quiet &= master.RootElement.GetProperty("tables").GetProperty("og.escrow").GetProperty("trades").EnumerateArray().All(t => t.GetProperty("state").GetString() is "Settled" or "Refunded");
                }

                last = $"held {held}, collected {collected}";
                balanced = quiet && held == collected;
            }

            Assert.True(balanced, "lanterns held by players never matched lanterns collected: " + last);
            var report = cluster.Hashes();
            Assert.True(report.Ok, string.Join("; ", report.Problems));
            Assert.Equal(2, report.Checks.Length);
            Assert.Empty(report.Drift);
            pids = [.. pids, .. cluster.Pids]; // the first processes (killed) and the ones that replaced them
        }

        Assert.All(pids, pid => Assert.False(ProcessCluster.IsAlive(pid), $"process {pid} is still running"));
    }

    [Fact]
    public void ABotReconnectsOnItsOwnWhenItsIslandIsKilledAndStartedAgain()
    {
        int[] pids;
        using (var cluster = TestCluster.Launch(Spec("lanterns-reconnect")))
        {
            pids = [.. cluster.Pids];
            var result = cluster.Run(Path.Combine(AppContext.BaseDirectory, "cluster", "reconnect.yaml"));
            Assert.True(result.Passed, JsonSerializer.Serialize(result, ScenarioJson.Default.ScenarioResult));

            // The bot process is the one that was running before: it reconnected, it was not started again.
            using var status = JsonDocument.Parse(cluster.Call("bot1", "net_status"));
            Assert.True(status.RootElement.GetProperty("reconnects").GetInt32() >= 1, status.RootElement.ToString());
            Assert.Equal("live", status.RootElement.GetProperty("replica").GetString());
            Assert.All(status.RootElement.GetProperty("followed").EnumerateArray(), f => Assert.Equal("live", f.GetProperty("replica").GetString()));
            var report = cluster.Hashes();
            Assert.True(report.Ok, string.Join("; ", report.Problems));
            pids = [.. pids, .. cluster.Pids];
        }

        Assert.All(pids, pid => Assert.False(ProcessCluster.IsAlive(pid), $"process {pid} is still running"));
    }

    [Fact]
    public void APlayerWhoseClientIsKilledIsRemovedFromTheIslands()
    {
        using var cluster = TestCluster.Launch(Spec("lanterns-leave"));
        var result = cluster.Run(Path.Combine(AppContext.BaseDirectory, "cluster", "leave.yaml"));
        Assert.True(result.Passed, JsonSerializer.Serialize(result, ScenarioJson.Default.ScenarioResult));

        var clock = System.Diagnostics.Stopwatch.StartNew();
        var players = -1;
        while (clock.ElapsedMilliseconds < 20_000)
        {
            players = 0;
            foreach (var (node, scope) in new[] { ("islandA", 1), ("islandB", 2) })
            {
                using var doc = JsonDocument.Parse(cluster.Call(node, "state_query", $"{{\"scope\":{scope}}}"));
                players += doc.RootElement.GetProperty("entities").EnumerateArray().Count(e => e.GetProperty("type").GetString() == "Player");
            }

            if (players == 1)
            {
                break;
            }

            Thread.Sleep(250);
        }

        Assert.Equal(1, players);
    }

    [Fact]
    public void AClientFollowingTheMasterThroughARelayCatchesUpAfterTheMasterIsKilledAndStarted()
    {
        using var cluster = TestCluster.Launch(Spec("lanterns-relay"));
        Assert.True(cluster.WaitForTick("bot1", 120));
        cluster.Kill("master");
        cluster.Start("master");
        // A killed process says nothing: the islands' links must notice the new process on their own, and the relays must pass its frames on.
        Assert.True(cluster.WaitForTick("master", cluster.TickOf("master") + 240), "the master did not tick");
        var clock = System.Diagnostics.Stopwatch.StartNew();
        string last;
        do
        {
            Thread.Sleep(250);
            using var status = JsonDocument.Parse(cluster.Call("bot1", "net_status"));
            var followed = status.RootElement.GetProperty("followed").EnumerateArray().First(f => f.GetProperty("scope").GetUInt32() == 3);
            using var master = JsonDocument.Parse(cluster.Call("master", "hash"));
            var masterTick = master.RootElement.GetProperty("scopes")[0].GetProperty("tick").GetInt64();
            last = $"client {followed.GetProperty("tick").GetInt64()} ({followed.GetProperty("replica").GetString()}), master {masterTick}";
            if (Math.Abs(masterTick - followed.GetProperty("tick").GetInt64()) < 60)
            {
                break;
            }
        }
        while (clock.ElapsedMilliseconds < 5000);
        Assert.True(clock.ElapsedMilliseconds < 5000, "the client did not catch up within 5 s of the master ticking: " + last);
        Assert.True(cluster.Hashes().Ok, string.Join("; ", cluster.Hashes().Problems));
        Console.WriteLine($"caught up {clock.ElapsedMilliseconds} ms after the master had ticked 240 times: {last}");
    }
}
