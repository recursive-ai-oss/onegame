using System.Diagnostics;
using System.Text.Json.Serialization;
using OneGame.Control;

namespace OneGame.Testing;

/// <summary>A process cluster could not be started, reached or stopped. The message says which node and why.</summary>
public sealed class ClusterException : Exception
{
    /// <summary>Creates the exception.</summary>
    public ClusterException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    /// <summary>A short stable identifier such as <c>start_failed</c>, <c>no_node</c> or <c>orphans</c>.</summary>
    public string Code { get; }
}

/// <summary>A listening address of a server node.</summary>
/// <param name="Transport"><c>udp</c> or <c>ws</c>.</param>
/// <param name="Address">The bound address.</param>
/// <param name="Port">The bound port.</param>
public sealed record EndpointState(string Transport, string Address, int Port);

/// <summary>One node as recorded in <c>cluster.json</c>.</summary>
/// <param name="Name">The node's name.</param>
/// <param name="Role"><c>server</c> or <c>client</c>.</param>
/// <param name="Pid">The process id.</param>
/// <param name="Socket">The live-control socket.</param>
/// <param name="Log">The file the process writes its output to.</param>
/// <param name="Listen">Servers: where it accepts clients.</param>
/// <param name="Connect">Clients: the server node they are connected to.</param>
/// <param name="Transport">Clients: the transport they use.</param>
/// <param name="Release">Clients: an exported release build with no live control; its result is read from its output when it exits.</param>
public sealed record NodeState(string Name, string Role, int Pid, string Socket, string Log, EndpointState[] Listen, string? Connect, string? Transport, bool Release = false);

/// <summary>A running cluster as <c>cluster.json</c> records it, enough for another process to find every node.</summary>
/// <param name="Name">The cluster's name.</param>
/// <param name="Dir">The folder with logs, sockets and this file.</param>
/// <param name="Owner">The process that started it.</param>
/// <param name="Nodes">The nodes in start order.</param>
public sealed record ClusterState(string Name, string Dir, int Owner, NodeState[] Nodes);

/// <summary>What <c>cluster status</c> reports for one node.</summary>
/// <param name="Name">The node's name.</param>
/// <param name="Role"><c>server</c> or <c>client</c>.</param>
/// <param name="Pid">The process id.</param>
/// <param name="Alive">Whether the process exists.</param>
/// <param name="Tick">The node's tick, or -1 when it could not be asked.</param>
/// <param name="Hash">The node's state hash.</param>
/// <param name="Detail">The node's own status JSON (<c>server_info</c> or <c>net_status</c>), or the reason it could not be read.</param>
public sealed record NodeStatusLine(string Name, string Role, int Pid, bool Alive, long Tick, string? Hash, string Detail);

/// <summary>The state of a cluster, node by node.</summary>
/// <param name="Name">The cluster's name.</param>
/// <param name="Nodes">One line per node.</param>
public sealed record ClusterStatus(string Name, NodeStatusLine[] Nodes);

/// <summary>One scope's tick and hash on one node.</summary>
/// <param name="Node">The node.</param>
/// <param name="Role"><c>server</c> or <c>client</c>.</param>
/// <param name="Scope">The scope id.</param>
/// <param name="Tick">The scope's tick.</param>
/// <param name="Hash">The state hash (16 hex digits).</param>
public sealed record NodeHash(string Node, string Role, uint Scope, long Tick, string Hash);

/// <summary>A client replica compared with its server's recorded hash at the replica's tick.</summary>
/// <param name="Client">The client node.</param>
/// <param name="Server">The server node.</param>
/// <param name="Scope">The scope.</param>
/// <param name="Tick">The replica's tick, at which both hashes are compared.</param>
/// <param name="ClientHash">The replica's hash.</param>
/// <param name="ServerHash">The server's hash at that tick, or null when it was not recorded.</param>
/// <param name="Match">True when the hashes are equal.</param>
/// <param name="Note">Why the check could not be made or what differs.</param>
public sealed record HashCheck(string Client, string Server, uint Scope, long Tick, string ClientHash, string? ServerHash, bool Match, string? Note);

/// <summary>A drift report filed by a client's replica.</summary>
/// <param name="Node">The client node.</param>
/// <param name="Scope">The scope.</param>
/// <param name="Tick">The tick at which the replica differed from the server.</param>
/// <param name="Expected">The server's hash.</param>
/// <param name="Actual">The replica's hash.</param>
/// <param name="Differences">The first entities that differ.</param>
public sealed record DriftEntry(string Node, uint Scope, long Tick, string Expected, string Actual, string[] Differences);

/// <summary>What <c>cluster hashes</c> reports: every node's hash, every replica checked against its server at the same tick, and all drift.</summary>
/// <param name="Ok">True when every check matched and no node reported drift.</param>
/// <param name="Nodes">Every node's current scopes.</param>
/// <param name="Checks">One per client.</param>
/// <param name="Drift">Drift reports from the clients.</param>
/// <param name="Problems">One sentence per failure, naming node, scope and tick.</param>
public sealed record ClusterHashReport(bool Ok, NodeHash[] Nodes, HashCheck[] Checks, DriftEntry[] Drift, string[] Problems);

/// <summary>What <c>cluster down</c> did.</summary>
/// <param name="Stopped">Nodes that stopped when asked.</param>
/// <param name="Killed">Nodes that had to be killed.</param>
/// <param name="Alive">Nodes still alive afterwards: empty unless something is badly wrong.</param>
public sealed record DownReport(string[] Stopped, string[] Killed, string[] Alive);

/// <summary>Source-generated JSON for the cluster's files and reports.</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, WriteIndented = false)]
[JsonSerializable(typeof(ClusterState))]
[JsonSerializable(typeof(ClusterStatus))]
[JsonSerializable(typeof(ClusterHashReport))]
[JsonSerializable(typeof(DownReport))]
public sealed partial class ClusterJson : JsonSerializerContext
{
}

/// <summary>
/// A cluster of real processes on this machine: each node of a <see cref="ClusterSpec"/> is its own OS process (a dedicated server or a
/// headless game connected to one) talking over the real transport, each with its live-control socket. The cluster starts them, calls
/// operations on any node, compares every client's hash with its server's at the same tick, and stops them all.
/// </summary>
/// <remarks>
/// Every node is told to watch the process that started it (<c>--parent</c>), so a crashed test leaves nothing running. <see cref="Down"/>
/// stops the nodes by pid and checks that none is left.
/// </remarks>
public sealed class ProcessCluster : IDisposable
{
    private ClusterState _state;
    private readonly ClusterSpec? _spec;
    private readonly Dictionary<string, int>? _ports;
    private readonly int _parent;
    private readonly string _stateFile;
    private readonly Dictionary<string, LiveClient> _clients = new(StringComparer.Ordinal);
    private readonly bool _owner;
    private readonly bool _ownsDir;
    // How far a followed replica may be from its authority tick when the cluster is compared (latency, loss and the time between reads).
    private const long FollowedLagTicks = 240;

    private bool _down;

    private ProcessCluster(ClusterState state, string stateFile, bool owner, bool ownsDir, ClusterSpec? spec = null, Dictionary<string, int>? ports = null, int parent = 0)
    {
        _spec = spec;
        _ports = ports;
        _parent = parent;
        _state = state;
        _stateFile = stateFile;
        _owner = owner;
        _ownsDir = ownsDir;
    }

    /// <summary>The nodes as started.</summary>
    public ClusterState State => _state;

    /// <summary>The folder with logs, sockets and <c>cluster.json</c>. A folder the cluster made itself (no <c>Dir</c> in the spec) is deleted by <see cref="Down"/>.</summary>
    public string Dir => _state.Dir;

    /// <summary>The path of <c>cluster.json</c>.</summary>
    public string StateFile => _stateFile;

    /// <summary>The process ids of every node.</summary>
    public IReadOnlyList<int> Pids => [.. _state.Nodes.Select(n => n.Pid)];

    /// <summary>Starts every node of the spec and waits until each is up (servers listening, clients connected and subscribed).</summary>
    /// <param name="spec">The cluster.</param>
    /// <param name="parentPid">The process the nodes watch; this process when null; 0 for none (nodes that outlive the caller, stopped with <see cref="Down"/>). A node exits within about a second of the watched process ending.</param>
    /// <exception cref="ClusterException">A node did not start; everything started so far is stopped again.</exception>
    public static ProcessCluster Up(ClusterSpec spec, int? parentPid = null)
    {
        ArgumentNullException.ThrowIfNull(spec);
        spec.Validate();
        var dir = Path.GetFullPath(spec.Dir ?? Path.Combine(Path.GetTempPath(), "onegame-cluster-" + Guid.NewGuid().ToString("N")[..8]));
        Directory.CreateDirectory(dir);
        var parent = parentPid ?? Environment.ProcessId;
        var started = new List<NodeState>();
        var processes = new List<Process>();
        var stateFile = Path.Combine(dir, "cluster.json");
        var ports = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var n in spec.Nodes.Where(n => n.Role == NodeRole.Server))
        {
            ports[n.Name] = FreePort();
            ports[n.Name + ".peer"] = FreePort(); // a second port per server, for servers that talk to each other privately
        }
        ProcessCluster? cluster = null;
        try
        {
            foreach (var node in spec.Nodes.Where(n => n.Role == NodeRole.Server).Concat(spec.Nodes.Where(n => n.Role == NodeRole.Client)))
            {
                var (state, process) = StartProcess(spec, node, dir, parent, started, ports);
                started.Add(state);
                processes.Add(process);
                WriteState(new ClusterState(spec.Name, dir, Environment.ProcessId, [.. started]), stateFile);
            }

            cluster = new ProcessCluster(new ClusterState(spec.Name, dir, Environment.ProcessId, [.. started]), stateFile, true, spec.Dir is null, spec, ports, parent);
            return cluster;
        }
        catch
        {
            foreach (var p in processes)
            {
                Kill(p.Id);
            }

            throw;
        }
    }

    /// <summary>Finds the nodes of a cluster that another process started, from its <c>cluster.json</c>.</summary>
    /// <param name="stateFile">The file, or the folder that holds it.</param>
    /// <exception cref="ClusterException">There is no such cluster.</exception>
    public static ProcessCluster Attach(string stateFile)
    {
        var path = Directory.Exists(stateFile) ? Path.Combine(stateFile, "cluster.json") : stateFile;
        if (!File.Exists(path))
        {
            throw new ClusterException("no_cluster", $"There is no cluster state at {path}. Start one with 'onegame cluster up --spec <file>'.");
        }

        var info = new FileInfo(path);
        if (info.Length > 4 * 1024 * 1024)
        {
            throw new ClusterException("bad_state", $"{path} is too large to be a cluster file.");
        }

        ClusterState? state;
        try
        {
            state = JsonSerializer.Deserialize(File.ReadAllText(path), ClusterJson.Default.ClusterState);
        }
        catch (JsonException ex)
        {
            throw new ClusterException("bad_state", $"{path} is not a cluster file: {ex.Message}");
        }

        return new ProcessCluster(state ?? throw new ClusterException("bad_state", $"{path} is empty."), path, false, false);
    }

    /// <summary>
    /// Kills a node's process outright (SIGKILL: nothing is saved or announced on the way out), like a crash or a power cut. The node stays in
    /// the cluster, not running, until <see cref="Start"/>.
    /// </summary>
    /// <param name="node">The node's name.</param>
    /// <exception cref="ClusterException">No such node, or this object did not start the cluster.</exception>
    public void Kill(string node)
    {
        var n = Node(node);
        RequireOwner();
        if (_clients.Remove(n.Name, out var live))
        {
            live.Dispose();
        }

        Kill(n.Pid);
        if (!WaitExit(n.Pid, 5000))
        {
            throw new ClusterException("orphans", $"Node '{node}' (pid {n.Pid}) is still running after being killed.");
        }

        try
        {
            File.Delete(n.Socket);
        }
        catch (IOException)
        {
            // A leftover socket file is replaced when the node starts again.
        }
    }

    /// <summary>
    /// Starts a node that was killed (or never ran) again with the same arguments, data folder and ports, and waits until it is up: a server
    /// listening, a client connected and subscribed. Servers load what they saved; a client is a new connection and, to the game, a new player.
    /// </summary>
    /// <param name="node">The node's name.</param>
    /// <exception cref="ClusterException">The node is still running, did not start, or this object did not start the cluster.</exception>
    public void Start(string node)
    {
        var n = Node(node);
        RequireOwner();
        if (IsAlive(n.Pid))
        {
            throw new ClusterException("running", $"Node '{node}' is running (pid {n.Pid}); kill it first, or use Restart.");
        }

        var spec = _spec!;
        var (state, _) = StartProcess(spec, spec.Nodes.First(x => x.Name == node), _state.Dir, _parent, [.. _state.Nodes], _ports!);
        _state = _state with { Nodes = [.. _state.Nodes.Select(x => x.Name == node ? state : x)] };
        WriteState(_state, _stateFile);
    }

    /// <summary>Kills a node's process and starts it again (see <see cref="Kill(string)"/> and <see cref="Start(string)"/>).</summary>
    /// <param name="node">The node's name.</param>
    public void Restart(string node)
    {
        Kill(node);
        Start(node);
    }

    private void RequireOwner()
    {
        if (_spec is null || !_owner)
        {
            throw new ClusterException("not_owner", "Only the process that started the cluster can kill and start its nodes.");
        }
    }

    /// <summary>Runs a control operation on a node and returns its JSON.</summary>
    /// <param name="node">The node's name.</param>
    /// <param name="op">The operation (see the control layer's table, or the server's).</param>
    /// <param name="argsJson">A JSON object, or null.</param>
    /// <exception cref="ClusterException">No such node.</exception>
    /// <exception cref="ControlException">The operation failed on the node.</exception>
    public string Call(string node, string op, string? argsJson = null) => Client(node).Call(op, argsJson);

    /// <summary>The names of the operations a node offers.</summary>
    public IReadOnlyList<string> Operations(string node) => Client(node).Tools();

    /// <summary>The node's state as recorded.</summary>
    /// <exception cref="ClusterException">No such node.</exception>
    public NodeState Node(string name) =>
        _state.Nodes.FirstOrDefault(n => n.Name == name) ?? throw new ClusterException("no_node", $"There is no node '{name}'. Nodes: {string.Join(", ", _state.Nodes.Select(n => n.Name))}.");

    /// <summary>The last lines of a node's output.</summary>
    public string LogTail(string node, int lines = 40)
    {
        var file = Node(node).Log;
        return File.Exists(file) ? string.Join('\n', File.ReadAllLines(file).TakeLast(lines)) : "";
    }

    /// <summary>The tick of the node's scope (a server's first scope, or a client's replica).</summary>
    public long TickOf(string node)
    {
        var n = Node(node);
        if (n.Release)
        {
            using var result = ReleaseResult(n) ?? throw new ClusterException("running", $"Node '{node}' is a release build that has not finished: it reports its tick only when it exits.");
            return result.RootElement.GetProperty("tick").GetInt64();
        }

        using var doc = JsonDocument.Parse(Call(node, n.Role == "server" ? "hash" : "net_status"));
        return doc.RootElement.GetProperty("tick").GetInt64();
    }

    /// <summary>Waits until a node's tick reaches <paramref name="tick"/>. A client pumps its connection while it waits; a server must be ticking on its own clock.</summary>
    /// <returns>True when it got there before the timeout.</returns>
    public bool WaitForTick(string node, long tick, int timeoutMs = 30_000)
    {
        var n = Node(node);
        var clock = Stopwatch.StartNew();
        if (n.Release)
        {
            // It has no live control to ask: it is done when its process has exited, at the tick it was told to stop at.
            return WaitExit(n.Pid, timeoutMs) && TickOf(node) >= tick;
        }

        if (n.Role == "client")
        {
            using var doc = JsonDocument.Parse(Call(node, "net_wait", $"{{\"tick\":{tick.ToString(CultureInfo.InvariantCulture)},\"timeoutMs\":{Math.Min(timeoutMs, 120_000).ToString(CultureInfo.InvariantCulture)}}}"));
            return doc.RootElement.GetProperty("reached").GetBoolean();
        }

        while (clock.ElapsedMilliseconds < timeoutMs)
        {
            if (TickOf(node) >= tick)
            {
                return true;
            }

            Thread.Sleep(10);
        }

        return false;
    }

    /// <summary>Every node's current tick and hash, every client's replica compared with its server's recorded hash at the replica's tick, and all drift reports.</summary>
    public ClusterHashReport Hashes()
    {
        var nodes = new List<NodeHash>();
        var checks = new List<HashCheck>();
        var drift = new List<DriftEntry>();
        var problems = new List<string>();

        // Clients first: their tick is behind their server's, so the server still has it in its history.
        var clientStates = new Dictionary<string, (long Tick, string Hash, uint Scope)>(StringComparer.Ordinal);
        var followed = new List<(string Client, uint Scope, string Replica, long Tick)>();
        foreach (var n in _state.Nodes.Where(n => n.Role == "client"))
        {
            if (n.Release)
            {
                ReadReleaseClient(n, nodes, clientStates, followed, drift, problems);
                continue;
            }

            try
            {
                using var doc = JsonDocument.Parse(Call(n.Name, "net_status"));
                var r = doc.RootElement;
                var tick = r.GetProperty("tick").GetInt64();
                var hash = r.GetProperty("hash").GetString() ?? "";
                var scope = r.GetProperty("scope").GetUInt32();
                var replica = r.GetProperty("replica").GetString();
                var connection = r.GetProperty("connection").GetString();
                nodes.Add(new NodeHash(n.Name, "client", scope, tick, hash));
                clientStates[n.Name] = (tick, hash, scope);
                if (replica != "live" || connection != "connected")
                {
                    problems.Add($"{n.Name}: connection {connection}, replica {replica} at tick {tick}");
                }

                if (r.TryGetProperty("followed", out var others))
                {
                    foreach (var f in others.EnumerateArray())
                    {
                        followed.Add((n.Name, f.GetProperty("scope").GetUInt32(), f.GetProperty("replica").GetString() ?? "", f.GetProperty("tick").GetInt64()));
                    }
                }

                foreach (var d in r.GetProperty("reports").EnumerateArray())
                {
                    var diffs = d.GetProperty("differences").EnumerateArray().Select(x => x.GetString() ?? "").ToArray();
                    var entry = new DriftEntry(d.GetProperty("node").GetString() ?? n.Name, d.GetProperty("scope").GetUInt32(), d.GetProperty("tick").GetInt64(), d.GetProperty("expected").GetString() ?? "", d.GetProperty("actual").GetString() ?? "", diffs);
                    drift.Add(entry);
                    problems.Add($"{n.Name}: drift on scope {entry.Scope} at tick {entry.Tick} (server {entry.Expected}, replica {entry.Actual}){(diffs.Length > 0 ? ": " + string.Join("; ", diffs) : "")}");
                }
            }
            catch (ControlException ex)
            {
                problems.Add($"{n.Name}: {ex.Code}: {ex.Message}");
            }
        }

        foreach (var n in _state.Nodes.Where(n => n.Role == "server"))
        {
            try
            {
                using var doc = JsonDocument.Parse(Call(n.Name, "hash"));
                foreach (var s in doc.RootElement.GetProperty("scopes").EnumerateArray())
                {
                    nodes.Add(new NodeHash(n.Name, "server", s.GetProperty("id").GetUInt32(), s.GetProperty("tick").GetInt64(), s.GetProperty("hash").GetString() ?? ""));
                }
            }
            catch (ControlException ex)
            {
                problems.Add($"{n.Name}: {ex.Code}: {ex.Message}");
            }
        }

        // A scope a client follows through its server relay has no recorded hash on that server, so it is checked for being alive and
        // current: a relay that stopped passing frames on leaves the replica live but far behind its authority.
        foreach (var (client, scope, replica, tick) in followed)
        {
            var authority = nodes.FirstOrDefault(x => x.Role == "server" && x.Scope == scope);
            if (authority is null)
            {
                problems.Add($"{client}: follows scope {scope}, which no server of the cluster runs");
            }
            else if (replica != "live")
            {
                problems.Add($"{client}: scope {scope} (followed) is {replica} at tick {tick}");
            }
            else if (Math.Abs(authority.Tick - tick) > FollowedLagTicks)
            {
                problems.Add($"{client}: scope {scope} (followed) is at tick {tick} but '{authority.Node}' is at {authority.Tick}: the relay stopped passing frames on");
            }
        }

        foreach (var n in _state.Nodes.Where(n => n.Role == "client"))
        {
            if (!clientStates.TryGetValue(n.Name, out var c) || n.Connect is null)
            {
                continue;
            }

            string? serverHash = null;
            string? note = null;
            try
            {
                using var doc = JsonDocument.Parse(Call(n.Connect, "hash_at", $"{{\"tick\":{c.Tick.ToString(CultureInfo.InvariantCulture)}}}"));
                foreach (var s in doc.RootElement.EnumerateArray())
                {
                    if (s.GetProperty("scope").GetUInt32() == c.Scope)
                    {
                        serverHash = s.TryGetProperty("hash", out var h) && h.ValueKind == JsonValueKind.String ? h.GetString() : null;

                        // The replica holds its audience's view of the scope: it matches when it equals the view of one of the server's clients
                        // (or the whole scope, when everything is visible).
                        foreach (var v in s.GetProperty("views").EnumerateArray())
                        {
                            if (v.GetString() == c.Hash)
                            {
                                serverHash = c.Hash;
                            }
                        }
                    }
                }

                if (serverHash is null)
                {
                    note = $"server '{n.Connect}' has no hash recorded for scope {c.Scope} at tick {c.Tick} (history off, or the tick is older than its history)";
                }
            }
            catch (ControlException ex)
            {
                note = $"{ex.Code}: {ex.Message}";
            }

            var match = serverHash is not null && serverHash == c.Hash;
            checks.Add(new HashCheck(n.Name, n.Connect, c.Scope, c.Tick, c.Hash, serverHash, match, note ?? (match ? null : "the hashes differ")));
            if (!match)
            {
                problems.Add($"{n.Name}: scope {c.Scope} at tick {c.Tick}: replica hash {c.Hash}, server '{n.Connect}' hash {serverHash ?? "(none)"}{(note is null ? "" : " — " + note)}");
            }
        }

        return new ClusterHashReport(problems.Count == 0, [.. nodes], [.. checks], [.. drift], [.. problems]);
    }

    // The last JSON line a release client printed, or null while it runs or when it printed none.
    private static JsonDocument? ReleaseResult(NodeState n)
    {
        if (IsAlive(n.Pid) || !File.Exists(n.Log))
        {
            return null;
        }

        var lines = File.ReadAllLines(n.Log);
        for (var i = lines.Length - 1; i >= 0; i--)
        {
            if (lines[i].StartsWith('{'))
            {
                try
                {
                    return JsonDocument.Parse(lines[i]);
                }
                catch (JsonException)
                {
                    return null;
                }
            }
        }

        return null;
    }

    // A release client has no live control, so what it printed when it stopped stands for net_status.
    private static void ReadReleaseClient(NodeState n, List<NodeHash> nodes, Dictionary<string, (long Tick, string Hash, uint Scope)> clientStates, List<(string Client, uint Scope, string Replica, long Tick)> followed, List<DriftEntry> drift, List<string> problems)
    {
        if (IsAlive(n.Pid))
        {
            problems.Add($"{n.Name}: still running; a release client reports only when it stops (wait for it with a wait step)");
            return;
        }

        using var doc = ReleaseResult(n);
        if (doc is null || !doc.RootElement.TryGetProperty("viewHash", out _))
        {
            problems.Add($"{n.Name}: stopped without a result; its output:\n{Tail(n.Log)}");
            return;
        }

        var r = doc.RootElement;
        var tick = r.GetProperty("tick").GetInt64();
        var hash = r.GetProperty("viewHash").GetString() ?? "";
        var scope = r.GetProperty("scope").GetUInt32();
        var replica = r.GetProperty("replica").GetString();
        var connection = r.GetProperty("connection").GetString();
        nodes.Add(new NodeHash(n.Name, "client", scope, tick, hash));
        clientStates[n.Name] = (tick, hash, scope);
        if (replica != "live" || connection != "connected")
        {
            problems.Add($"{n.Name}: connection {connection}, replica {replica} at tick {tick}");
        }

        foreach (var f in r.GetProperty("followed").EnumerateArray())
        {
            followed.Add((n.Name, f.GetProperty("scope").GetUInt32(), f.GetProperty("replica").GetString() ?? "", f.GetProperty("tick").GetInt64()));
        }

        var count = r.GetProperty("drift").GetInt32();
        if (count > 0)
        {
            drift.Add(new DriftEntry(n.Name, scope, tick, "", "", [$"{count} drift report(s); the node's output has the details"]));
            problems.Add($"{n.Name}: {count} drift report(s) on scope {scope}");
        }
    }

    /// <summary>Each node's pid, whether it is alive, and its own status.</summary>
    public ClusterStatus Status()
    {
        var lines = new List<NodeStatusLine>();
        foreach (var n in _state.Nodes)
        {
            var alive = IsAlive(n.Pid);
            long tick = -1;
            string? hash = null;
            string detail;
            if (n.Release)
            {
                // No live control: while it runs there is nothing to ask, and when it has stopped its printed result says where it ended.
                using var result = ReleaseResult(n);
                detail = result is null ? (alive ? "release client running (no live control)" : "not running") : result.RootElement.GetRawText();
                if (result is not null && result.RootElement.TryGetProperty("tick", out var rt))
                {
                    tick = rt.GetInt64();
                }
            }
            else if (!alive)
            {
                detail = "not running";
            }
            else
            {
                try
                {
                    detail = Call(n.Name, n.Role == "server" ? "server_info" : "net_status");
                    using var doc = JsonDocument.Parse(detail);
                    if (doc.RootElement.TryGetProperty("tick", out var t))
                    {
                        tick = t.GetInt64();
                    }

                    if (doc.RootElement.TryGetProperty("hash", out var h) && h.ValueKind == JsonValueKind.String)
                    {
                        hash = h.GetString();
                    }
                }
                catch (ControlException ex)
                {
                    detail = $"{ex.Code}: {ex.Message}";
                }
            }

            lines.Add(new NodeStatusLine(n.Name, n.Role, n.Pid, alive, tick, hash, detail));
        }

        return new ClusterStatus(_state.Name, [.. lines]);
    }

    /// <summary>Runs a scenario against the cluster: steps and expectations aimed at nodes, in order.</summary>
    public ScenarioResult Run(Scenario scenario) => new ClusterScenarioRunner(this).Run(scenario);

    /// <summary>Reads and runs a scenario file against the cluster.</summary>
    public ScenarioResult Run(string scenarioPath) => Run(ScenarioParser.Load(scenarioPath));

    /// <summary>Stops every node (asking first, then killing what does not stop), checks that none is left, and removes the sockets.</summary>
    /// <exception cref="ClusterException">A node is still alive after being killed.</exception>
    public DownReport Down()
    {
        _down = true;
        var stopped = new List<string>();
        var killed = new List<string>();
        var alive = new List<string>();
        foreach (var client in _clients.Values)
        {
            client.Dispose();
        }

        _clients.Clear();
        foreach (var n in _state.Nodes.Reverse())
        {
            if (!IsAlive(n.Pid))
            {
                continue;
            }

            try
            {
                if (!n.Release)
                {
                    using var ask = new LiveClient(n.Socket, 3000);
                    _ = ask.Call(n.Role == "server" ? "shutdown" : "quit");
                }
            }
            catch (ControlException)
            {
                // Not answering: it is killed below.
            }

            if (WaitExit(n.Pid, 4000))
            {
                stopped.Add(n.Name);
            }
            else
            {
                Kill(n.Pid);
                killed.Add(n.Name);
            }
        }

        foreach (var n in _state.Nodes)
        {
            if (IsAlive(n.Pid) && !WaitExit(n.Pid, 3000))
            {
                alive.Add($"{n.Name} (pid {n.Pid})");
            }

            try
            {
                File.Delete(n.Socket);
            }
            catch (IOException)
            {
                // A leftover socket file is harmless.
            }
        }

        var report = new DownReport([.. stopped], [.. killed], [.. alive]);
        if (alive.Count > 0)
        {
            throw new ClusterException("orphans", "These nodes are still running after being killed: " + string.Join(", ", alive));
        }

        try
        {
            File.Delete(_stateFile);
            if (_owner && _ownsDir)
            {
                Directory.Delete(_state.Dir, true);
            }
        }
        catch (IOException)
        {
            // The state file is only a pointer, and a leftover temporary folder is harmless.
        }

        return report;
    }

    /// <summary>Stops the cluster when this object started it (see <see cref="Down"/>); one found with <see cref="Attach"/> is left running.</summary>
    public void Dispose()
    {
        foreach (var client in _clients.Values)
        {
            client.Dispose();
        }

        if (_owner && !_down)
        {
            try
            {
                Down();
            }
            catch (ClusterException)
            {
                // Dispose must not throw; Down reports orphans to a caller that asks.
            }
        }
    }

    private LiveClient Client(string node)
    {
        var n = Node(node);
        if (n.Release)
        {
            throw new ClusterException("no_live", $"Node '{node}' is a release build: it has no live control. Wait for it to finish and read its result with cluster hashes.");
        }

        if (!_clients.TryGetValue(n.Name, out var c))
        {
            _clients[n.Name] = c = new LiveClient(n.Socket);
        }

        return c;
    }

    // ---- processes ----

    private static (NodeState State, Process Process) StartProcess(ClusterSpec spec, NodeSpec node, string dir, int parent, List<NodeState> started, Dictionary<string, int> ports)
    {
        var socket = Path.Combine(dir, node.Name + ".sock");
        var log = Path.Combine(dir, node.Name + ".log");
        var args = new List<string>();
        string? connect = null;
        if (node.Role == NodeRole.Server)
        {
            args.AddRange(node.Args.Select(a => Expand(a, ports, dir)));
            args.AddRange(["--name", node.Name]);
            foreach (var l in node.Listen.Split('+'))
            {
                args.AddRange(["--listen", l + ":" + ports[node.Name].ToString(CultureInfo.InvariantCulture) + "@127.0.0.1"]);
            }

            foreach (var s in node.Scopes)
            {
                args.AddRange(["--scope", $"{s.Id.ToString(CultureInfo.InvariantCulture)}:{s.Seed.ToString(CultureInfo.InvariantCulture)}"]);
            }

            if (node.TickRate > 0)
            {
                args.AddRange(["--tick-rate", node.TickRate.ToString(CultureInfo.InvariantCulture)]);
            }

            args.AddRange(["--hash-history", "512"]);
            if (node.Manual)
            {
                args.Add("--manual");
            }

            args.AddRange(["--live", socket]);
        }
        else
        {
            var target = started.First(s => s.Name == node.Connect);
            var endpoint = target.Listen.First(e => e.Transport == node.Transport);
            connect = $"{node.Transport}://127.0.0.1:{endpoint.Port.ToString(CultureInfo.InvariantCulture)}";
            args.AddRange(node.Args.Select(a => Expand(a, ports, dir)));
            args.AddRange(["--headless", "--gpu", "off", "--name", node.Name, "--connect", connect, "--scope", node.Scope.ToString(CultureInfo.InvariantCulture)]);
            if (node.Net is not null)
            {
                args.AddRange(["--net", node.Net]);
            }

            // A release build takes no development options (no --live, no --parent): it stops at its tick or when its server is gone.
            args.AddRange(node.Release ? ["--realtime", "--json"] : ["--live", socket, "--realtime"]);
        }

        if (parent > 0 && !node.Release)
        {
            args.AddRange(["--parent", parent.ToString(CultureInfo.InvariantCulture)]);
        }

        var info = new ProcessStartInfo("/bin/sh") { UseShellExecute = false, RedirectStandardInput = true, WorkingDirectory = dir };
        info.ArgumentList.Add("-c");
        info.ArgumentList.Add("exec \"$@\" >\"$0\" 2>&1");
        info.ArgumentList.Add(log);
        if (node.Exe.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            info.ArgumentList.Add(DotnetHost());
        }

        info.ArgumentList.Add(node.Exe);
        foreach (var a in args)
        {
            info.ArgumentList.Add(a);
        }

        if (!File.Exists(node.Exe))
        {
            throw new ClusterException("start_failed", $"Node '{node.Name}': the program {node.Exe} does not exist. Build or export it first.");
        }

        var process = Process.Start(info) ?? throw new ClusterException("start_failed", $"Node '{node.Name}' did not start.");
        process.StandardInput.Close();
        try
        {
            if (node.Release)
            {
                // No control socket to ask: it is up when it is still running after a moment (a bad option ends it at once).
                Thread.Sleep(500);
                return process.HasExited
                    ? throw new ClusterException("start_failed", $"Node '{node.Name}' exited with code {process.ExitCode} while starting. Its output:\n{Tail(log)}")
                    : (new NodeState(node.Name, "client", process.Id, socket, log, [], node.Connect, node.Transport, true), process);
            }

            return (WaitUp(spec, node, process, socket, log, connect), process);
        }
        catch
        {
            Kill(process.Id);
            throw;
        }
    }

    private static NodeState WaitUp(ClusterSpec spec, NodeSpec node, Process process, string socket, string log, string? connect)
    {
        var clock = Stopwatch.StartNew();
        var role = node.Role == NodeRole.Server ? "server" : "client";
        var op = node.Role == NodeRole.Server ? "server_info" : "net_status";
        using var probe = new LiveClient(socket, 5000);
        string? last = null;
        while (true)
        {
            if (process.HasExited)
            {
                throw new ClusterException("start_failed", $"Node '{node.Name}' exited with code {process.ExitCode} while starting. Its output:\n{Tail(log)}");
            }

            if (clock.ElapsedMilliseconds > spec.StartupTimeoutMs)
            {
                throw new ClusterException("start_timeout", $"Node '{node.Name}' was not ready after {spec.StartupTimeoutMs} ms{(last is null ? "" : " (" + last + ")")}. Its output:\n{Tail(log)}");
            }

            try
            {
                var text = probe.Call(op);
                using var doc = JsonDocument.Parse(text);
                var root = doc.RootElement;
                if (node.Role == NodeRole.Server)
                {
                    var endpoints = root.GetProperty("listen").EnumerateArray()
                        .Select(e => new EndpointState(e.GetProperty("transport").GetString() ?? "", e.GetProperty("address").GetString() ?? "", e.GetProperty("port").GetInt32()))
                        .ToArray();
                    return new NodeState(node.Name, role, process.Id, socket, log, endpoints, null, null);
                }

                var replica = root.GetProperty("replica").GetString();
                if (replica == "live")
                {
                    return new NodeState(node.Name, role, process.Id, socket, log, [], node.Connect, node.Transport);
                }

                last = $"replica {replica}, connection {root.GetProperty("connection").GetString()}";
                if (root.GetProperty("connection").GetString() == "closed")
                {
                    throw new ClusterException("start_failed", $"Node '{node.Name}' could not connect to {connect} ({root.GetProperty("closeReason").GetString()}). Its output:\n{Tail(log)}");
                }
            }
            catch (ControlException ex)
            {
                last = ex.Code;
            }

            Thread.Sleep(30);
        }
    }

    // Every server gets its port before any node starts, so servers can be told where their peers will listen (a server that starts first
    // is given the address of one that starts later and connects when it is up). The same number serves udp and ws: they are different protocols.
    private static int FreePort()
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            using var tcp = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            tcp.Start();
            var port = ((System.Net.IPEndPoint)tcp.LocalEndpoint).Port;
            try
            {
                using var udp = new System.Net.Sockets.UdpClient(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, port));
                return port;
            }
            catch (System.Net.Sockets.SocketException)
            {
                // That number is taken for udp; try another.
            }
        }

        throw new ClusterException("no_port", "Could not find a free port for a server.");
    }

    // {port:NAME} is the port server NAME listens on, {port:NAME.peer} a second free port reserved for it; {dir} is the cluster's folder (saves and logs).
    private static string Expand(string arg, Dictionary<string, int> ports, string dir)
    {
        var text = arg.Replace("{dir}", dir, StringComparison.Ordinal);
        foreach (var (name, port) in ports)
        {
            text = text.Replace("{port:" + name + "}", port.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        }

        return text;
    }

    private static string Tail(string log) => File.Exists(log) ? string.Join('\n', File.ReadAllLines(log).TakeLast(15)) : "(no output)";

    private static string DotnetHost()
    {
        var root = Environment.GetEnvironmentVariable("DOTNET_ROOT");
        if (root is not null && File.Exists(Path.Combine(root, "dotnet")))
        {
            return Path.Combine(root, "dotnet");
        }

        return Environment.ProcessPath is { } p && Path.GetFileNameWithoutExtension(p) == "dotnet" ? p : "dotnet";
    }

    private static void WriteState(ClusterState state, string path)
    {
        var temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(state, ClusterJson.Default.ClusterState));
        File.Move(temp, path, true);
    }

    /// <summary>Whether a process with this id exists and has not exited.</summary>
    public static bool IsAlive(int pid)
    {
        try
        {
            using var p = Process.GetProcessById(pid);
            return !p.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static bool WaitExit(int pid, int timeoutMs)
    {
        var clock = Stopwatch.StartNew();
        while (clock.ElapsedMilliseconds < timeoutMs)
        {
            if (!IsAlive(pid))
            {
                return true;
            }

            Thread.Sleep(20);
        }

        return !IsAlive(pid);
    }

    private static void Kill(int pid)
    {
        try
        {
            using var p = Process.GetProcessById(pid);
            p.Kill(entireProcessTree: true);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            // Already gone.
        }
    }
}
