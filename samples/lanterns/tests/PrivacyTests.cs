using OneGame.Core;
using OneGame.Sync;

namespace Lanterns.Tests;

public class PrivacyTests
{
    private const int Secret = 123_457;

    /// <summary>A channel end that keeps a copy of everything that arrives through it.</summary>
    private sealed class Tap(IMessageChannel inner) : IMessageChannel
    {
        public List<byte> Seen { get; } = [];

        public void Send(ReadOnlySpan<byte> message) => inner.Send(message);

        public bool TryReceive(out ReadOnlySpan<byte> message)
        {
            if (inner.TryReceive(out message))
            {
                Seen.AddRange(message.ToArray());
                return true;
            }

            return false;
        }
    }

    private static byte[] Needle()
    {
        var w = new SyncWriter();
        w.WriteInt32(Secret);
        return w.ToArray();
    }

    private static bool Contains(List<byte> haystack, byte[] needle)
    {
        var h = haystack.ToArray();
        return h.AsSpan().IndexOf(needle) >= 0;
    }

    private static World NewWorld(WorldRole role)
    {
        var registry = new EntityRegistry();
        Worlds.Register(registry);
        var world = new World(5, registry, role);
        Worlds.Build(world, new ScopeId(Rules.IslandA));
        return world;
    }

    [Fact]
    public void AnotherPlayersInventoryNeverReachesAClientDirectlyOrThroughARelay()
    {
        var cluster = new Cluster(new ClusterOptions { DriftCheckInterval = 5 });
        var island = cluster.AddNode("island");
        var world = NewWorld(WorldRole.Authority);
        Worlds.Seed(world, new ScopeId(Rules.IslandA));
        var isle = Island.Of(world)!;
        isle.Join(1, "Ann");
        isle.Join(2, "Bob");
        var scope = cluster.AddScope(island, new ScopeId(Rules.IslandA), world);
        var ann = world.Entities.OfType<Player>().First(p => p.Owner == 1);
        var bob = world.Entities.OfType<Player>().First(p => p.Owner == 2);
        bob.Red = Secret;
        bob.Total = 5;
        ann.Change(1, 3);

        // Ann connects straight to the island; Bob too, as the control.
        var annTap = Connect(cluster, island, 1, out var annReplica);
        var bobTap = Connect(cluster, island, 2, out var bobReplica);

        // Carol reaches the island through a relay node.
        var relay = cluster.AddNode("relay");
        var (relayEnd, islandEnd) = Loopback.CreatePair();
        var upstream = relay.ConnectTo(island, Trust.Owner(new ScopeId(Rules.IslandA)).AsRelay(), relayEnd, islandEnd, "relay");
        var up = relay.Subscribe(upstream, new ScopeId(Rules.IslandA), NewWorld(WorldRole.Replica));
        relay.Relay(up);
        isle.Join(3, "Cat");
        var carolTap = Connect(cluster, relay, 3, out var carolReplica);

        cluster.Settle();
        cluster.Step(30);
        bob.Red++;
        cluster.Step(30);

        Assert.True(Contains(bobTap.Seen, Needle()), "control: the owner must receive its own inventory");
        Assert.False(Contains(annTap.Seen, Needle()), "another player's inventory reached a client");
        Assert.False(Contains(carolTap.Seen, Needle()), "another player's inventory reached a client through the relay");
        foreach (var replica in new[] { annReplica, carolReplica })
        {
            var seenBob = replica.World.Entities.OfType<Player>().First(p => p.Owner == 2);
            Assert.Equal((0, 0, 0, 0, false), (seenBob.Red, seenBob.Blue, seenBob.Gold, seenBob.Pending, seenBob.Mine));
            Assert.True(seenBob.Total > 0, "the count is public");
        }

        Assert.Equal(Secret + 1, bobReplica.World.Entities.OfType<Player>().First(p => p.Owner == 2).Red);
        Assert.Empty(cluster.DriftReports);
        _ = scope;
    }

    private static Tap Connect(Cluster cluster, Node server, uint player, out ReplicaScope replica)
    {
        var node = cluster.AddNode("client" + player);
        var (a, b) = Loopback.CreatePair();
        var tap = new Tap(a);
        var link = node.ConnectTo(server, Trust.Owner(new ScopeId(Rules.IslandA)).As(player), tap, b, "client" + player);
        replica = node.Subscribe(link, new ScopeId(Rules.IslandA), NewWorld(WorldRole.Replica));
        return tap;
    }
}
