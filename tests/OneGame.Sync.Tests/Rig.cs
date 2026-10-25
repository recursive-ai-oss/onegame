namespace OneGame.Sync.Tests;

/// <summary>One match scope on a host, with players joining over tapped channels so tests can read the exact bytes each one received.</summary>
internal sealed class Rig
{
    public Rig(ClusterOptions? options = null, World? world = null)
    {
        Cluster = new Cluster(options ?? new ClusterOptions { DriftCheckInterval = 1, FatalDrift = true });
        Host = Cluster.AddNode("host");
        Scope = Cluster.AddScope(Host, new ScopeId(1), world ?? Arena.Match());
    }

    public Cluster Cluster { get; }

    public Node Host { get; }

    public AuthorityScope Scope { get; }

    public Player Join(string name, Trust trust)
    {
        var node = Cluster.AddNode(name);
        var (a, b) = Loopback.CreatePair();
        var tap = new TapChannel(a);
        var link = node.ConnectTo(Host, trust, tap, b, name);
        var replica = node.Subscribe(link, Scope.Id, Arena.Empty(WorldRole.Replica));
        return new Player(node, link, tap, replica);
    }

    public static Hero HeroOf(ReplicaScope replica, uint player)
    {
        foreach (var e in replica.World.Entities)
        {
            if (e is Hero h && h.Player == player)
            {
                return h;
            }
        }

        return null!;
    }

    public static Account AccountOf(ReplicaScope replica, uint player)
    {
        foreach (var e in replica.World.Entities)
        {
            if (e is Account a && a.Player == player)
            {
                return a;
            }
        }

        return null!;
    }

    public static Hero HeroOf(AuthorityScope scope, uint player)
    {
        foreach (var e in scope.World.Entities)
        {
            if (e is Hero h && h.Player == player)
            {
                return h;
            }
        }

        return null!;
    }

    public static int Count<T>(ReplicaScope replica)
        where T : Entity
        => replica.World.Entities.Count(e => e is T);
}

internal sealed record Player(Node Node, Link Link, TapChannel Tap, ReplicaScope Replica)
{
    public Connection Connection => Link.Connection;
}
