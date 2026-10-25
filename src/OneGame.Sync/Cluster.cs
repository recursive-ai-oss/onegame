namespace OneGame.Sync;

/// <summary>One scope's state on one node, as listed by <see cref="Cluster.Hashes"/>.</summary>
/// <param name="Node">The node name.</param>
/// <param name="Scope">The scope.</param>
/// <param name="Role">Authority or replica.</param>
/// <param name="Tick">The world's completed tick count.</param>
/// <param name="Hash">The world hash.</param>
public readonly record struct ScopeHash(string Node, ScopeId Scope, WorldRole Role, long Tick, ulong Hash);

/// <summary>
/// A set of nodes in one process, joined by loopback links and stepped together. A single-player game is a cluster with one
/// authority node and one local replica; tests build bigger ones. The cluster only decides the order things happen in: each tick it
/// delivers input, steps every authority scope, publishes the frames and delivers them.
/// </summary>
public sealed class Cluster
{
    private const int MaxPumpRounds = 1000;

    private readonly List<Node> _nodes = [];
    private readonly List<int> _seenDrift = [];

    /// <summary>Creates a cluster.</summary>
    /// <param name="options">Options shared by all nodes; defaults if null.</param>
    public Cluster(ClusterOptions? options = null)
    {
        Options = options ?? new ClusterOptions();
    }

    /// <summary>The options every node of this cluster follows.</summary>
    public ClusterOptions Options { get; }

    /// <summary>The nodes, in the order they step.</summary>
    public IReadOnlyList<Node> Nodes => _nodes;

    /// <summary>The number of cluster ticks stepped.</summary>
    public long Tick { get; private set; }

    /// <summary>The fixed-step driver <see cref="Frame"/> uses. Replace its settings (pause, time scale) here.</summary>
    public FixedStepDriver Driver { get; set; } = new();

    /// <summary>Adds a node.</summary>
    public Node AddNode(string name)
    {
        var node = new Node(name, Options);
        _nodes.Add(node);
        _seenDrift.Add(0);
        return node;
    }

    /// <summary>Makes <paramref name="authority"/> the authority of a new scope.</summary>
    public AuthorityScope AddScope(Node authority, ScopeId id, World world)
    {
        Require(authority);
        return authority.AddScope(id, world);
    }

    /// <summary>Connects two nodes.</summary>
    /// <param name="client">The node that will subscribe and send commands.</param>
    /// <param name="server">The node holding the authority.</param>
    /// <param name="trust">What the client is trusted to do.</param>
    public Link Connect(Node client, Node server, Trust trust)
    {
        Require(client);
        Require(server);
        return client.ConnectTo(server, trust);
    }

    private void Require(Node node)
    {
        if (!_nodes.Contains(node))
        {
            throw new ArgumentException($"Node '{node.Name}' is not part of this cluster. Create it with Cluster.AddNode.", nameof(node));
        }
    }

    /// <summary>
    /// Adds a local player: a new node connected to the scope's node, subscribed to the scope, with a replica world to read. The
    /// replica fills in on the next <see cref="Step"/> (or call <see cref="Settle"/>).
    /// </summary>
    /// <param name="scope">The authority scope.</param>
    /// <param name="replicaWorld">An empty replica world with the same registry and tables as the authority's.</param>
    /// <param name="trust">What the local player may do; <see cref="Trust.Full"/> if null.</param>
    /// <param name="name">The new node's name.</param>
    public ReplicaScope AddLocalReplica(AuthorityScope scope, World replicaWorld, Trust? trust = null, string name = "client")
    {
        var node = AddNode(name);
        var link = Connect(node, scope.Node, trust ?? Trust.Full);
        return node.Subscribe(link, scope.Id, replicaWorld);
    }

    /// <summary>Delivers pending messages without stepping, so new subscriptions get their snapshot.</summary>
    public void Settle()
    {
        PumpAll();
        CheckFatalDrift();
    }

    /// <summary>Runs <paramref name="ticks"/> ticks.</summary>
    /// <exception cref="DriftException">A replica drifted and <see cref="ClusterOptions.FatalDrift"/> is on.</exception>
    public void Step(int ticks = 1)
    {
        for (var n = 0; n < ticks; n++)
        {
            PumpAll();
            for (var i = 0; i < _nodes.Count; i++)
            {
                var scopes = _nodes[i].AuthorityScopes;
                for (var s = 0; s < scopes.Count; s++)
                {
                    scopes[s].StepAndPublish();
                }
            }

            PumpAll();
            Tick++;
            CheckFatalDrift();
        }
    }

    /// <summary>Feeds one rendered frame's elapsed time to the <see cref="Driver"/> and runs the ticks it says are due. Returns how many ran.</summary>
    public int Frame(TimeSpan frameTime)
    {
        var steps = Driver.Advance(frameTime);
        Step(steps);
        return steps;
    }

    /// <summary>Compares every replica to its authority right now, and describes any difference. Empty means in sync.</summary>
    public IReadOnlyList<DriftReport> CheckReplicas()
    {
        var reports = new List<DriftReport>();
        var scratch = new SyncWriter(1024);
        foreach (var node in _nodes)
        {
            foreach (var replica in node.ReplicaScopes)
            {
                var authority = replica.Link.Server.FindAuthority(replica.Id);
                if (authority is null || replica.State != ReplicaState.Live)
                {
                    continue;
                }

                // Compare what this replica is allowed to hold: the authority's view for its audience against the replica's own.
                var expected = authority.ViewHashFor(replica.Link.Connection);
                var actual = replica.ViewHash;
                if (expected == actual)
                {
                    continue;
                }

                var a = new StateDigest();
                var b = new StateDigest();
                var includeRandom = authority.RandomStreams == Visibility.Everyone;
                StateDigest.CaptureView(authority.World, a, scratch, int.MaxValue, replica.Link.Connection.Trust.Who, includeRandom);
                StateDigest.CaptureView(replica.World, b, scratch, int.MaxValue, null, replica.World.RandomIncluded);
                var differences = new List<StateDifference>();
                StateDigest.Diff(a, b, authority.World, replica.World, differences);
                reports.Add(new DriftReport(replica.Id, node.Name, replica.World.Tick, expected, actual, differences, true));
            }
        }

        return reports;
    }

    /// <summary>The tick and hash of every scope on every node.</summary>
    public IReadOnlyList<ScopeHash> Hashes()
    {
        var list = new List<ScopeHash>();
        foreach (var node in _nodes)
        {
            foreach (var s in node.AuthorityScopes)
            {
                list.Add(new ScopeHash(node.Name, s.Id, WorldRole.Authority, s.World.Tick, s.World.ComputeHash()));
            }

            foreach (var s in node.ReplicaScopes)
            {
                list.Add(new ScopeHash(node.Name, s.Id, WorldRole.Replica, s.World.Tick, s.World.ComputeHash()));
            }
        }

        return list;
    }

    /// <summary>Every drift report filed by the nodes' replicas.</summary>
    public IReadOnlyList<DriftReport> DriftReports
    {
        get
        {
            var list = new List<DriftReport>();
            foreach (var node in _nodes)
            {
                list.AddRange(node.DriftReports);
            }

            return list;
        }
    }

    /// <summary>Every refused command and subscription on every node.</summary>
    public IReadOnlyList<CommandRefusal> Refusals
    {
        get
        {
            var list = new List<CommandRefusal>();
            foreach (var node in _nodes)
            {
                list.AddRange(node.Refusals);
            }

            return list;
        }
    }

    private void PumpAll()
    {
        for (var round = 0; round < MaxPumpRounds; round++)
        {
            var any = false;
            for (var i = 0; i < _nodes.Count; i++)
            {
                any |= _nodes[i].Pump();
            }

            if (!any)
            {
                return;
            }
        }

        throw new InvalidOperationException($"Messages were still being exchanged after {MaxPumpRounds} rounds. Two nodes are answering each other without end.");
    }

    private void CheckFatalDrift()
    {
        for (var i = 0; i < _nodes.Count; i++)
        {
            var count = _nodes[i].DriftCount;
            if (count > _seenDrift[i])
            {
                _seenDrift[i] = count;
                if (Options.FatalDrift)
                {
                    throw new DriftException(_nodes[i].DriftReports[^1]);
                }
            }
        }
    }
}
