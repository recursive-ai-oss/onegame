namespace OneGame.Sync.Tests;

/// <summary>An authority, a relay that follows it, and clients of different audiences that follow the relay.</summary>
internal sealed class RelayRig
{
    public RelayRig(ClusterOptions? options = null, Func<IMessageChannel, IMessageChannel>? tapUpstream = null)
    {
        Cluster = new Cluster(options ?? new ClusterOptions { DriftCheckInterval = 1, FatalDrift = true });
        Auth = Cluster.AddNode("auth");
        Relay = Cluster.AddNode("relay");
        Scope = Cluster.AddScope(Auth, new ScopeId(1), Arena.Match());
        Link link;
        if (tapUpstream is null)
        {
            link = Cluster.Connect(Relay, Auth, Trust.Owner(Scope.Id).AsRelay());
        }
        else
        {
            var (a, b) = Loopback.CreatePair();
            link = Relay.ConnectTo(Auth, Trust.Owner(Scope.Id).AsRelay(), tapUpstream(a), b, "relay");
        }

        Upstream = Relay.Subscribe(link, Scope.Id, Arena.Empty(WorldRole.Replica));
        RelayScope = Relay.Relay(Upstream);
        Cluster.Settle();
    }

    public Cluster Cluster { get; }

    public Node Auth { get; }

    public Node Relay { get; }

    public AuthorityScope Scope { get; }

    public ReplicaScope Upstream { get; }

    public AuthorityScope RelayScope { get; }

    /// <summary>A client of the relay, with a tap on what it receives.</summary>
    public Player JoinRelay(string name, Trust trust)
    {
        var node = Cluster.AddNode(name);
        var (a, b) = Loopback.CreatePair();
        var tap = new TapChannel(a);
        var link = node.ConnectTo(Relay, trust, tap, b, name);
        var replica = node.Subscribe(link, Scope.Id, Arena.Empty(WorldRole.Replica));
        return new Player(node, link, tap, replica);
    }

    /// <summary>A client of the authority itself, for comparison.</summary>
    public Player JoinDirect(string name, Trust trust)
    {
        var node = Cluster.AddNode(name);
        var (a, b) = Loopback.CreatePair();
        var tap = new TapChannel(a);
        var link = node.ConnectTo(Auth, trust, tap, b, name);
        var replica = node.Subscribe(link, Scope.Id, Arena.Empty(WorldRole.Replica));
        return new Player(node, link, tap, replica);
    }
}
