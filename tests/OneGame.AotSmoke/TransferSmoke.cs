using OneGame.Core;
using OneGame.Sync;

namespace OneGame.AotSmoke;

/// <summary>Counts the hits it takes; the hand-off smoke moves one between scopes.</summary>
[Entity]
public partial class Wanderer : Entity
{
    [Synced, OwnerKey] public partial uint Player { get; set; }

    [Synced] public partial int Hits { get; set; }

    [Command]
    public void Hit() => Hits++;
}

/// <summary>A player's gold on a server, for the escrow smoke.</summary>
[Entity]
public partial class Pouch : Entity
{
    [Synced, OwnerKey] public partial uint Player { get; set; }

    [Synced] public partial long Gold { get; set; }
}

internal sealed class PouchHandler : IEscrowHandler
{
    public bool TryDebit(World world, uint owner, uint kind, long amount)
    {
        foreach (var e in world.Entities)
        {
            if (e is Pouch p && p.Player == owner && p.Gold >= amount)
            {
                p.Gold -= amount;
                return true;
            }
        }

        return false;
    }

    public bool Credit(World world, uint owner, uint kind, long amount)
    {
        foreach (var e in world.Entities)
        {
            if (e is Pouch p && p.Player == owner)
            {
                p.Gold += amount;
                return true;
            }
        }

        return false;
    }
}

/// <summary>Authority transfer, relaying and escrow under NativeAOT: a hand-off between two scopes with a recording replay, a relay with a filtered client, and a settled trade.</summary>
internal static class TransferSmoke
{
    private static World NewWorld(WorldRole role, ulong seed, bool escrow = false)
    {
        var registry = new EntityRegistry();
        OneGame.Generated.OneGame_AotSmoke.EntityTypes.Register(registry);
        Escrow.Register(registry);
        var world = new World(seed, registry, role);
        if (role == WorldRole.Authority)
        {
            TransferLog.AddTo(world);
            if (escrow)
            {
                Escrow.Install(world, new PouchHandler());
            }
        }

        return world;
    }

    public static string? Run() => Handoff() ?? Relay() ?? Trade();

    private static string? Handoff()
    {
        var cluster = new Cluster(new ClusterOptions { DriftCheckInterval = 1, FatalDrift = true });
        var a = cluster.AddNode("A");
        var b = cluster.AddNode("B");
        var client = cluster.AddNode("client");
        var scopeA = cluster.AddScope(a, new ScopeId(1), NewWorld(WorldRole.Authority, 1));
        var scopeB = cluster.AddScope(b, new ScopeId(2), NewWorld(WorldRole.Authority, 2));
        var wanderer = scopeA.World.Add(new Wanderer { Player = 5 });
        var peer = cluster.Connect(a, b, Trust.Owner(scopeB.Id));
        var toA = cluster.Connect(client, a, Trust.Owner(scopeA.Id).As(5));
        var toB = cluster.Connect(client, b, Trust.Owner(scopeB.Id).As(5));
        client.FollowResolver = _ => new FollowTarget(toB, NewWorld(WorldRole.Replica, 2));
        client.Subscribe(toA, scopeA.Id, NewWorld(WorldRole.Replica, 1));
        cluster.Settle();
        var recorder = scopeA.StartRecording(new RecordingOptions { CheckpointInterval = 1 });
        var sent = 0;
        Transfer? transfer = null;
        for (var tick = 0; tick < 60; tick++)
        {
            if (tick == 10)
            {
                transfer = scopeA.BeginTransfer(peer, scopeB.Id, [wanderer.Id]);
            }

            foreach (var r in client.ReplicaScopes)
            {
                foreach (var e in r.World.Entities)
                {
                    if (e is Wanderer w && r.State == ReplicaState.Live)
                    {
                        w.Send.Hit();
                        sent++;
                    }
                }
            }

            cluster.Step();
        }

        if (transfer?.State != TransferState.Complete)
        {
            return "hand-off did not complete: " + transfer?.State;
        }

        var landed = 0;
        foreach (var e in scopeB.World.Entities)
        {
            if (e is Wanderer w)
            {
                landed = w.Hits;
            }
        }

        if (landed != sent || scopeA.World.Entities.Count != 0)
        {
            return $"hand-off counted {landed} of {sent} commands";
        }

        var replay = Replayer.Replay(recorder.Stop(), NewWorld(WorldRole.Authority, 1));
        return replay.Ok ? null : "hand-off recording diverged: " + replay.Divergence;
    }

    private static string? Relay()
    {
        var cluster = new Cluster(new ClusterOptions { DriftCheckInterval = 1, FatalDrift = true });
        var auth = cluster.AddNode("auth");
        var relay = cluster.AddNode("relay");
        var authority = NewWorld(WorldRole.Authority, 3);
        authority.Add(new Locker { Player = 1, Combination = "secret-one" });
        authority.Add(new Locker { Player = 2, Combination = "secret-two" });
        var scope = cluster.AddScope(auth, new ScopeId(1), authority);
        var upstream = relay.Subscribe(cluster.Connect(relay, auth, Trust.Owner(scope.Id).AsRelay()), scope.Id, NewWorld(WorldRole.Replica, 3));
        relay.Relay(upstream);
        var one = cluster.AddNode("one");
        var viewOne = one.Subscribe(cluster.Connect(one, relay, Trust.Owner(scope.Id).As(1)), scope.Id, NewWorld(WorldRole.Replica, 3));
        cluster.Step(40);
        foreach (var e in viewOne.World.Entities)
        {
            if (e is Locker l && l.Player == 2 && l.Combination.Length != 0)
            {
                return "relay leaked another player's private member";
            }
        }

        return viewOne.ViewHash == scope.ViewHashFor(scope.Subscribers[^1]) || cluster.CheckReplicas().Count == 0 ? null : "relayed view drifted";
    }

    private static string? Trade()
    {
        var cluster = new Cluster(new ClusterOptions { DriftCheckInterval = 0 });
        var nodeA = cluster.AddNode("A");
        var nodeB = cluster.AddNode("B");
        var nodeM = cluster.AddNode("M");
        var worldA = NewWorld(WorldRole.Authority, 4, escrow: true);
        worldA.Add(new Pouch { Player = 1, Gold = 100 });
        var worldB = NewWorld(WorldRole.Authority, 5, escrow: true);
        worldB.Add(new Pouch { Player = 2, Gold = 100 });
        var scopeA = cluster.AddScope(nodeA, new ScopeId(1), worldA);
        var scopeB = cluster.AddScope(nodeB, new ScopeId(2), worldB);
        var master = cluster.AddScope(nodeM, new ScopeId(9), NewWorld(WorldRole.Authority, 6, escrow: true));
        var replicas = new Dictionary<ScopeId, ReplicaScope>();
        foreach (var s in new[] { scopeA, scopeB })
        {
            replicas[s.Id] = nodeM.Subscribe(cluster.Connect(nodeM, s.Node, Escrow.CoordinatorTrust(s.Id)), s.Id, NewWorld(WorldRole.Replica, 7));
        }

        cluster.Settle();
        var driver = new EscrowDriver(master, id => replicas.GetValueOrDefault(id));
        Escrow.DeskOf(master.World)!.Send.Open(1, 1, 1, 2, 1, 30, 2, 2, 1, 1, 10);
        for (var i = 0; i < 40; i++)
        {
            cluster.Step();
            driver.Pump();
        }

        long gold = 0;
        foreach (var w in new[] { worldA, worldB })
        {
            foreach (var e in w.Entities)
            {
                if (e is Pouch p)
                {
                    gold += p.Gold;
                }
            }
        }

        var trade = EscrowBook.Of(master.World)!.FindTrade(1);
        return trade?.State == EscrowState.Settled && gold == 200 ? null : $"trade ended {trade?.State} with {gold} gold";
    }
}
