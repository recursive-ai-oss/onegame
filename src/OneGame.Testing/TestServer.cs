using OneGame.Core;
using OneGame.Server;
using OneGame.Sync;

namespace OneGame.Testing;

/// <summary>
/// A server under test, with in-process clients on the loopback transport. One call to <see cref="Step"/> runs a server tick and then
/// delivers the frames to every client and records every scope's hash, so a test can compare hash sequences between runs, check that
/// replicas never drift, and reload plugins in the middle of a run.
/// </summary>
public sealed class TestServer : IDisposable
{
    private readonly List<ServerTestClient> _clients = [];
    private readonly List<(long Tick, ulong Hash)>[] _history;
    private readonly List<ScopeId> _scopeIds = [];

    /// <summary>Creates and starts a server.</summary>
    /// <param name="config">The configuration.</param>
    /// <param name="setup">The game's own types and world setup, or null.</param>
    /// <param name="loader">The plugin loader (<c>new PluginLoader()</c> from <c>OneGame.Server.Plugins</c> on CoreCLR), or null for none.</param>
    public TestServer(ServerConfig config, ServerSetup? setup = null, IPluginLoader? loader = null)
    {
        Host = new ServerHost(config, setup, loader, new ServerLog());
        Host.Start();
        Host.DeliverLocalMessages = Settle;
        foreach (var s in Host.Scopes)
        {
            _scopeIds.Add(s.Id);
        }

        _history = new List<(long, ulong)>[_scopeIds.Count];
        for (var i = 0; i < _history.Length; i++)
        {
            _history[i] = [];
        }
    }

    /// <summary>The server under test.</summary>
    public ServerHost Host { get; }

    /// <summary>The connected clients.</summary>
    public IReadOnlyList<ServerTestClient> Clients => _clients;

    /// <summary>Drift reports filed by any client's replicas. Must stay empty.</summary>
    public int DriftCount => _clients.Sum(c => c.Node.DriftCount);

    /// <summary>Connects a client that subscribes to a scope, and delivers its first snapshot.</summary>
    /// <param name="name">The client's name.</param>
    /// <param name="scope">The scope to subscribe to.</param>
    /// <param name="trust">What the client may do; <see cref="Trust.Player"/> on the scope when null.</param>
    public ServerTestClient AddClient(string name, ScopeId scope, Trust? trust = null)
    {
        var node = new Node(name);
        var link = node.ConnectTo(Host.Node, trust ?? Trust.Player(scope), name);
        var replica = node.Subscribe(link, scope, Host.CreateReplicaWorld(scope));
        var client = new ServerTestClient(node, replica);
        _clients.Add(client);
        Settle();
        return client;
    }

    /// <summary>Delivers pending messages both ways without ticking.</summary>
    public void Settle()
    {
        foreach (var c in _clients)
        {
            c.Node.Pump();
        }

        Host.Node.Pump();
        foreach (var c in _clients)
        {
            c.Node.Pump();
        }
    }

    /// <summary>Runs ticks: the server steps, then every client applies the frames. The hash of each scope after each tick is recorded.</summary>
    /// <param name="ticks">How many.</param>
    public void Step(int ticks = 1)
    {
        for (var i = 0; i < ticks; i++)
        {
            Host.StepOnce();
            foreach (var c in _clients)
            {
                c.Node.Pump();
            }

            for (var s = 0; s < _scopeIds.Count; s++)
            {
                var world = Host.FindScope(_scopeIds[s])!.World;
                _history[s].Add((world.Tick, world.ComputeHash()));
            }
        }
    }

    /// <summary>The (tick, hash) pairs recorded for a scope, one per <see cref="Step"/>.</summary>
    /// <param name="scope">The scope.</param>
    public IReadOnlyList<(long Tick, ulong Hash)> HashHistory(ScopeId scope) => _history[_scopeIds.IndexOf(scope)];

    /// <summary>Compares every client's replica to the authority right now. Returns a description of each difference; empty means in sync.</summary>
    public IReadOnlyList<string> CheckSync()
    {
        var problems = new List<string>();
        foreach (var c in _clients)
        {
            var authority = Host.FindScope(c.Replica.Id)!.World;
            if (c.Replica.State != ReplicaState.Live)
            {
                problems.Add($"{c.Node.Name}: replica is {c.Replica.State}");
            }
            else if (c.Replica.World.Tick != authority.Tick || c.Replica.World.ComputeHash() != authority.ComputeHash())
            {
                problems.Add($"{c.Node.Name}: replica tick {c.Replica.World.Tick} hash {c.Replica.World.ComputeHash():x16}, authority tick {authority.Tick} hash {authority.ComputeHash():x16}");
            }
        }

        if (DriftCount > 0)
        {
            problems.Add($"{DriftCount} drift report(s) filed");
        }

        return problems;
    }

    /// <summary>Stops the server.</summary>
    public void Dispose() => Host.Stop();
}

/// <summary>A client of a <see cref="TestServer"/>: a node holding a replica of one scope.</summary>
public sealed class ServerTestClient
{
    internal ServerTestClient(Node node, ReplicaScope replica)
    {
        Node = node;
        Replica = replica;
    }

    /// <summary>The client's node.</summary>
    public Node Node { get; }

    /// <summary>The replica of the scope.</summary>
    public ReplicaScope Replica { get; }

    /// <summary>The replica world; views read it.</summary>
    public World World => Replica.World;

    /// <summary>Sends a command as a player would: forwarded to the server, run on its next tick.</summary>
    /// <param name="entity">The entity.</param>
    /// <param name="command">The command name.</param>
    /// <param name="args">Arguments as text.</param>
    public CommandStatus Send(EntityId entity, string command, params string[] args)
    {
        var e = World.Find(entity) ?? throw new InvalidOperationException($"The replica has no entity {entity}.");
        return e.SendFromText(command, args);
    }
}
