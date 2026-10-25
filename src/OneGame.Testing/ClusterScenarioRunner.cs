using System.Diagnostics;
using OneGame.Control;

namespace OneGame.Testing;

/// <summary>
/// Runs a scenario against a <see cref="ProcessCluster"/>: each step goes to the node it names through that node's live control, the same
/// operations an agent calls, and the cluster-wide expectations compare hashes across processes.
/// </summary>
internal sealed class ClusterScenarioRunner(ProcessCluster cluster)
{
    public ScenarioResult Run(Scenario scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        var clock = Stopwatch.StartNew();
        var steps = new List<StepResult>();
        var results = new List<ExpectationResult>();
        var failed = false;
        if (scenario.Fixture is not null || scenario.Snapshot is not null || scenario.Seed is not null || scenario.Window is not null)
        {
            steps.Add(new StepResult(-1, "setup", 0, false, "bad_args: fixture, snapshot, seed and window do not apply to a cluster scenario; servers own the world."));
            failed = true;
        }

        for (var i = 0; i < scenario.Steps.Count && !failed; i++)
        {
            var step = scenario.Steps[i];
            string? error = null;
            try
            {
                Execute(step, i, scenario, results);
            }
            catch (ControlException ex)
            {
                error = $"{ex.Code}: {ex.Message}";
            }
            catch (ClusterException ex)
            {
                error = $"{ex.Code}: {ex.Message}";
            }

            steps.Add(new StepResult(i, step.Kind.ToString().ToLowerInvariant(), step.Line, error is null, error, step.Node));
            failed = error is not null;
        }

        if (!failed)
        {
            for (var i = 0; i < scenario.Expect.Count; i++)
            {
                results.AddRange(Check(scenario.Expect[i], i));
            }
        }

        var passed = !failed && results.TrueForAll(r => r.Passed);
        return new ScenarioResult(scenario.Name, scenario.SourcePath, passed, 0, "", clock.ElapsedMilliseconds, [.. steps], [.. results]);
    }

    private static string RequireNode(ScenarioStep s) =>
        s.Node ?? throw new ControlException("bad_args", $"Line {s.Line}: a {s.Kind.ToString().ToLowerInvariant()} step in a cluster scenario needs a node: (add node: <name> beside it).");

    private void Execute(ScenarioStep s, int index, Scenario scenario, List<ExpectationResult> inline)
    {
        var obj = ScenarioRunner.Obj;
        switch (s.Kind)
        {
            case StepKind.Command:
                cluster.Call(RequireNode(s), "command_send", obj(("entity", s.Entity), ("command", s.Name), ("args", s.Args)));
                break;
            case StepKind.Set:
                cluster.Call(RequireNode(s), "state_patch", obj(("entity", s.Entity), ("property", s.Name), ("value", s.Args[0])));
                break;
            case StepKind.Step:
                if (s.Node is null)
                {
                    foreach (var n in cluster.State.Nodes)
                    {
                        StepNode(n, s.Count);
                    }
                }
                else
                {
                    StepNode(cluster.Node(s.Node), s.Count);
                }

                break;
            case StepKind.Wait:
                if (!cluster.WaitForTick(s.Node!, s.Tick, s.TimeoutMs))
                {
                    throw new ControlException("timeout", $"Line {s.Line}: node '{s.Node}' did not reach tick {s.Tick} within {s.TimeoutMs} ms (it is at {cluster.TickOf(s.Node!)}).");
                }

                break;
            case StepKind.Click:
                cluster.Call(RequireNode(s), "ui_click", s.Id is not null ? obj(("id", s.Id)) : obj(("selector", s.Selector)));
                break;
            case StepKind.ClickAt:
                cluster.Call(RequireNode(s), "input_pointer", obj(("action", "click"), ("x", s.X), ("y", s.Y)));
                break;
            case StepKind.Invoke:
                cluster.Call(RequireNode(s), "ui_invoke", s.Id is not null ? obj(("id", s.Id)) : obj(("selector", s.Selector)));
                break;
            case StepKind.Press:
                cluster.Call(RequireNode(s), "input_key", obj(("key", s.Text), ("modifiers", s.Modifiers)));
                break;
            case StepKind.Type:
                cluster.Call(RequireNode(s), "input_text", obj(("text", s.Text)));
                break;
            case StepKind.Nav:
                cluster.Call(RequireNode(s), "input_nav", obj(("action", s.Text)));
                break;
            case StepKind.Screen:
                cluster.Call(RequireNode(s), "screen_open", obj(("name", s.Text)));
                break;
            case StepKind.Expect:
                var rs = Check(s.Expect!, index);
                inline.AddRange(rs);
                if (rs.Any(r => !r.Passed))
                {
                    throw new ControlException("expectation_failed", rs.First(r => !r.Passed).Message ?? "an expectation failed");
                }

                break;
            default:
                throw new ControlException("bad_args", $"Line {s.Line}: a {s.Kind.ToString().ToLowerInvariant()} step cannot run in a cluster scenario. Servers own the world, so fixtures, snapshots and captures belong to single-game scenarios.");
        }
    }

    private void StepNode(NodeState n, int count)
    {
        // A server steps ticks (a manual server only moves this way); a client pumps frames.
        _ = n.Role == "server"
            ? cluster.Call(n.Name, "step", ScenarioRunner.Obj(("ticks", count)))
            : cluster.Call(n.Name, "step", ScenarioRunner.Obj(("frames", count)));
    }

    private List<ExpectationResult> Check(ScenarioExpectation e, int index)
    {
        var kind = e.Kind.ToString().ToLowerInvariant();
        try
        {
            switch (e.Kind)
            {
                case ExpectKind.Agree:
                    return Agree(e, index);
                case ExpectKind.Drift:
                    return Drift(e, index);
                case ExpectKind.State:
                {
                    var node = Require(e);
                    var wait = Stopwatch.StartNew();
                    while (true)
                    {
                        var r = CheckState(e, index, node);
                        if (r.Passed || wait.ElapsedMilliseconds >= e.WithinMs)
                        {
                            return [r];
                        }

                        Thread.Sleep(10);
                    }
                }

                case ExpectKind.Tick:
                {
                    var node = Require(e);
                    return [ScenarioRunner.Compare(e, index, "tick", node + ".tick", cluster.TickOf(node).ToString(CultureInfo.InvariantCulture), e.EqualTo!) with { Node = node }];
                }

                case ExpectKind.Hash:
                {
                    var node = Require(e);
                    using var doc = JsonDocument.Parse(cluster.Call(node, cluster.Node(node).Role == "server" ? "hash" : "net_status"));
                    var root = doc.RootElement;
                    var actual = cluster.Node(node).Role == "server" ? root.GetProperty("scopes")[0].GetProperty("hash").GetString()! : root.GetProperty("hash").GetString()!;
                    return [ScenarioRunner.Compare(e, index, "hash", node + ".hash", actual, e.EqualTo!.ToLowerInvariant()) with { Node = node }];
                }

                default:
                    throw new ControlException("bad_args", $"Line {e.Line}: a {kind} expectation cannot run in a cluster scenario.");
            }
        }
        catch (Exception ex) when (ex is ControlException or ClusterException)
        {
            var code = ex is ControlException c ? c.Code : ((ClusterException)ex).Code;
            return [new ExpectationResult(index, kind, e.Line, false, null, null, null, $"{code}: {ex.Message}", e.Node)];
        }
    }

    private static string Require(ScenarioExpectation e) =>
        e.Node ?? throw new ControlException("bad_args", $"Line {e.Line}: a {e.Kind.ToString().ToLowerInvariant()} expectation in a cluster scenario needs a node: (add node: <name> beside it).");

    private ExpectationResult CheckState(ScenarioExpectation e, int index, string node)
    {
        var json = cluster.Call(node, "state_query", ScenarioRunner.Obj(("entity", e.Entity), ("property", e.Property), ("scope", e.Scope ?? 1)));
        using var doc = JsonDocument.Parse(json);
        var el = doc.RootElement;
        var actual = el.ValueKind == JsonValueKind.String ? el.GetString()! : el.GetRawText();
        var target = $"{node}:{e.Entity}.{e.Property}";
        if (e.EqualTo is not null)
        {
            return ScenarioRunner.Compare(e, index, "state", target, actual, e.EqualTo) with { Node = node };
        }

        if (!double.TryParse(actual, NumberStyles.Float, CultureInfo.InvariantCulture, out var n))
        {
            return new ExpectationResult(index, "state", e.Line, false, target, null, actual, $"{target} is '{actual}', which is not a number, so min and max cannot be checked", node);
        }

        if (e.Min is { } min && n < min)
        {
            return new ExpectationResult(index, "state", e.Line, false, target, $">= {min.ToString(CultureInfo.InvariantCulture)}", actual, $"{target} is {actual}, expected at least {min.ToString(CultureInfo.InvariantCulture)}", node);
        }

        if (e.Max is { } max && n > max)
        {
            return new ExpectationResult(index, "state", e.Line, false, target, $"<= {max.ToString(CultureInfo.InvariantCulture)}", actual, $"{target} is {actual}, expected at most {max.ToString(CultureInfo.InvariantCulture)}", node);
        }

        return new ExpectationResult(index, "state", e.Line, true, target, null, actual, null, node);
    }

    private List<ExpectationResult> Agree(ScenarioExpectation e, int index)
    {
        var report = cluster.Hashes();
        var server = cluster.Node(e.Server!);
        var wanted = e.Nodes.Length > 0 ? e.Nodes : [.. cluster.State.Nodes.Where(n => n.Role == "client" && n.Connect == server.Name).Select(n => n.Name)];
        var list = new List<ExpectationResult>();
        foreach (var client in wanted)
        {
            var check = report.Checks.FirstOrDefault(c => c.Client == client);
            if (check is null)
            {
                list.Add(new ExpectationResult(index, "agree", e.Line, false, client, "a hash check", "none", $"{client}: could not be compared with {server.Name}: {string.Join("; ", report.Problems.Where(p => p.StartsWith(client + ":", StringComparison.Ordinal)))}", client));
            }
            else if (check.Server != server.Name)
            {
                list.Add(new ExpectationResult(index, "agree", e.Line, false, client, server.Name, check.Server, $"{client} is connected to '{check.Server}', not '{server.Name}'", client));
            }
            else
            {
                list.Add(new ExpectationResult(
                    index, "agree", e.Line, check.Match, $"{client} vs {server.Name}", check.ServerHash, check.ClientHash,
                    check.Match ? $"{client} and {server.Name} agree at tick {check.Tick}" : $"{client}: scope {check.Scope} at tick {check.Tick}: replica hash {check.ClientHash}, server '{server.Name}' hash {check.ServerHash ?? "(none)"}{(check.Note is null ? "" : " (" + check.Note + ")")}", client));
            }
        }

        return list;
    }

    private List<ExpectationResult> Drift(ScenarioExpectation e, int index)
    {
        var report = cluster.Hashes();
        var wanted = e.Nodes.Length > 0 ? e.Nodes : [.. cluster.State.Nodes.Where(n => n.Role == "client").Select(n => n.Name)];
        var list = new List<ExpectationResult>();
        foreach (var node in wanted)
        {
            var mine = report.Drift.Where(d => d.Node == node).ToArray();
            if (mine.Length == 0)
            {
                list.Add(new ExpectationResult(index, "drift", e.Line, true, node, "0 drift reports", "0 drift reports", null, node));
                continue;
            }

            var d = mine[0];
            list.Add(new ExpectationResult(
                index, "drift", e.Line, false, node, "0 drift reports", $"{mine.Length} drift report(s)",
                $"{node}: drift on scope {d.Scope} at tick {d.Tick} (server {d.Expected}, replica {d.Actual}){(d.Differences.Length > 0 ? ": " + string.Join("; ", d.Differences) : "")}", node));
        }

        return list;
    }
}
