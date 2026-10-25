using System.Globalization;
using OneGame.Control;
using OneGame.Host;
using OneGame.Testing;

namespace OneGame.Cli.Verbs;

/// <summary>Clusters of real processes: start servers and clients from a spec, look at them, compare their hashes, stop them.</summary>
internal sealed class ClusterVerb : IVerb
{
    private const string Usage = "Usage: onegame cluster up --spec <file> [--dir <folder>] | status | hashes | down | kill <node>... | call <node> <operation> [json] | test <scenario.yaml>... --spec <file>   (--cluster <file|folder> picks a running cluster)";

    public string Name => "cluster";

    public string Summary => "Processes on a real network: cluster up --spec f.yaml | status | hashes | down | kill <node>... | call <node> <op> [json] | test <scenario> --spec f.yaml.";

    public int Run(string[] args, bool json, TextWriter output, TextWriter error)
    {
        if (args.Length == 0)
        {
            throw new CliException(ExitCodes.Usage, "bad_args", Usage);
        }

        var sub = args[0];
        string? spec = null, dir = null, cluster = null;
        var positional = new List<string>();
        for (var i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--spec" when i + 1 < args.Length:
                    spec = args[++i];
                    break;
                case "--dir" when i + 1 < args.Length:
                    dir = args[++i];
                    break;
                case "--cluster" when i + 1 < args.Length:
                    cluster = args[++i];
                    break;
                case var a when a.StartsWith("--", StringComparison.Ordinal):
                    throw new CliException(ExitCodes.Usage, "bad_args", $"Unknown option {a}. {Usage}");
                default:
                    positional.Add(args[i]);
                    break;
            }
        }

        try
        {
            return sub switch
            {
                "up" => Up(spec, dir, json, output),
                "status" => Status(Find(cluster, dir), json, output),
                "hashes" => Hashes(Find(cluster, dir), json, output),
                "down" => Down(Find(cluster, dir), json, output),
                "kill" => Kill(Find(cluster, dir), positional, json, output),
                "call" => Call(Find(cluster, dir), positional, output),
                "test" => TestScenarios(spec, dir, positional, json, output),
                _ => throw new CliException(ExitCodes.Usage, "bad_args", Usage),
            };
        }
        catch (ClusterException ex)
        {
            throw new CliException(ex.Code is "no_cluster" or "no_node" ? ExitCodes.Usage : ExitCodes.RuntimeError, ex.Code, ex.Message);
        }
        catch (ScenarioException ex)
        {
            throw new CliException(ExitCodes.Usage, "bad_spec", ex.Message);
        }
        catch (ControlException ex)
        {
            throw new CliException(ExitCodes.Failed, ex.Code, ex.Message);
        }
    }

    private static string Find(string? cluster, string? dir) =>
        cluster ?? dir ?? Environment.GetEnvironmentVariable("ONEGAME_CLUSTER") ?? Path.Combine(Directory.GetCurrentDirectory(), ".onegame-cluster");

    private static ClusterSpec LoadSpec(string? spec, string? dir)
    {
        if (spec is null)
        {
            throw new CliException(ExitCodes.Usage, "bad_args", "Give the cluster with --spec <file>. " + Usage);
        }

        var loaded = ClusterSpec.Load(spec);
        return dir is null ? loaded : loaded with { Dir = Path.GetFullPath(dir) };
    }

    private static void Print<T>(TextWriter output, bool json, bool ok, T result, System.Text.Json.Serialization.Metadata.JsonTypeInfo<ClusterResult<T>> info, Action<TextWriter> text)
    {
        if (json)
        {
            output.WriteLine(JsonSerializer.Serialize(new ClusterResult<T>(ok, result), info));
        }
        else
        {
            text(output);
        }
    }

    private static int Up(string? specPath, string? dir, bool json, TextWriter output)
    {
        // The nodes outlive this command: they are stopped by 'cluster down'.
        var spec = LoadSpec(specPath, dir);
        spec = spec with { Dir = spec.Dir ?? Path.Combine(Directory.GetCurrentDirectory(), ".onegame-cluster") };
        var cluster = ProcessCluster.Up(spec, 0);
        Print(output, json, true, cluster.State, CliJson.Default.ClusterResultClusterState, o =>
        {
            o.WriteLine($"cluster '{cluster.State.Name}' is up ({cluster.State.Nodes.Length} nodes); state in {cluster.StateFile}");
            foreach (var n in cluster.State.Nodes)
            {
                o.WriteLine($"  {n.Name,-12} {n.Role,-6} pid {n.Pid}  {string.Join(" ", n.Listen.Select(l => $"{l.Transport}://{l.Address}:{l.Port}"))}{(n.Connect is null ? "" : $"-> {n.Connect} ({n.Transport})")}");
            }
        });
        return ExitCodes.Ok;
    }

    private static int Status(string where, bool json, TextWriter output)
    {
        var status = ProcessCluster.Attach(where).Status();
        var ok = status.Nodes.All(n => n.Alive);
        Print(output, json, ok, status, CliJson.Default.ClusterResultClusterStatus, o =>
        {
            foreach (var n in status.Nodes)
            {
                o.WriteLine($"{n.Name,-12} {n.Role,-6} pid {n.Pid,-7} {(n.Alive ? "up" : "DOWN")}  tick {n.Tick}  hash {n.Hash}");
            }
        });
        return ok ? ExitCodes.Ok : ExitCodes.Failed;
    }

    private static int Hashes(string where, bool json, TextWriter output)
    {
        var report = ProcessCluster.Attach(where).Hashes();
        Print(output, json, report.Ok, report, CliJson.Default.ClusterResultClusterHashReport, o =>
        {
            foreach (var n in report.Nodes)
            {
                o.WriteLine($"{n.Node,-12} {n.Role,-6} scope {n.Scope}  tick {n.Tick}  hash {n.Hash}");
            }

            foreach (var c in report.Checks)
            {
                o.WriteLine($"{(c.Match ? "ok  " : "FAIL")} {c.Client} vs {c.Server} at tick {c.Tick}: {c.ClientHash} / {c.ServerHash ?? "(none)"}");
            }

            foreach (var p in report.Problems)
            {
                o.WriteLine("problem: " + p);
            }
        });
        return report.Ok ? ExitCodes.Ok : ExitCodes.Failed;
    }

    private static int Down(string where, bool json, TextWriter output)
    {
        var report = ProcessCluster.Attach(where).Down();
        Print(output, json, report.Alive.Length == 0, report, CliJson.Default.ClusterResultDownReport, o =>
            o.WriteLine($"stopped {report.Stopped.Length}, killed {report.Killed.Length}, still running {report.Alive.Length}"));
        return report.Alive.Length == 0 ? ExitCodes.Ok : ExitCodes.Failed;
    }

    private static int Kill(string where, List<string> positional, bool json, TextWriter output)
    {
        if (positional.Count == 0)
        {
            throw new CliException(ExitCodes.Usage, "bad_args", "Usage: onegame cluster kill <node>... (SIGKILL: nothing is saved or announced; the other nodes carry on).");
        }

        var cluster = ProcessCluster.Attach(where);
        var killed = new List<KilledNode>();
        foreach (var name in positional)
        {
            var node = cluster.State.Nodes.FirstOrDefault(n => n.Name == name) ?? throw new ClusterException("no_node", $"The cluster has no node '{name}'.");
            cluster.Kill(name);
            killed.Add(new KilledNode(name, node.Pid, ProcessCluster.IsAlive(node.Pid)));
        }

        var report = new KillReport([.. killed]);
        var ok = killed.All(k => !k.Alive);
        Print(output, json, ok, report, CliJson.Default.ClusterResultKillReport, o =>
        {
            foreach (var k in killed)
            {
                o.WriteLine($"{k.Name} (pid {k.Pid}) {(k.Alive ? "is still running" : "killed")}");
            }
        });
        return ok ? ExitCodes.Ok : ExitCodes.Failed;
    }

    private static int Call(string where, List<string> positional, TextWriter output)
    {
        if (positional.Count < 2)
        {
            throw new CliException(ExitCodes.Usage, "bad_args", "Usage: onegame cluster call <node> <operation> [json arguments].");
        }

        output.WriteLine(ProcessCluster.Attach(where).Call(positional[0], positional[1], positional.Count > 2 ? positional[2] : null));
        return ExitCodes.Ok;
    }

    private static int TestScenarios(string? specPath, string? dir, List<string> files, bool json, TextWriter output)
    {
        if (files.Count == 0)
        {
            throw new CliException(ExitCodes.Usage, "bad_args", "Give at least one scenario file: onegame cluster test <scenario.yaml> --spec <file>.");
        }

        var scenarios = files.Select(ScenarioParser.Load).ToArray();
        var spec = LoadSpec(specPath, dir);
        var results = new List<ScenarioResult>();
        DownReport down;
        using (var cluster = ProcessCluster.Up(spec))
        {
            foreach (var s in scenarios)
            {
                results.Add(cluster.Run(s));
            }

            down = cluster.Down();
        }

        var failed = results.Count(r => !r.Passed);
        var result = new ClusterTestResult(results.Count - failed, failed, [.. results], down);
        Print(output, json, failed == 0, result, CliJson.Default.ClusterResultClusterTestResult, o =>
        {
            foreach (var r in results)
            {
                o.WriteLine($"{(r.Passed ? "PASS" : "FAIL")} {r.Name} ({r.ElapsedMs} ms)");
                foreach (var s in r.Steps.Where(s => !s.Ok))
                {
                    o.WriteLine($"  step {s.Index} ({s.Kind}{(s.Node is null ? "" : " on " + s.Node)}, line {s.Line}): {s.Error}");
                }

                foreach (var e in r.Expectations.Where(e => !e.Passed))
                {
                    o.WriteLine($"  expect {e.Kind}{(e.Node is null ? "" : " on " + e.Node)} (line {e.Line}): {e.Message}");
                }
            }

            o.WriteLine($"{results.Count - failed} passed, {failed} failed");
        });
        return failed == 0 ? ExitCodes.Ok : ExitCodes.Failed;
    }
}
