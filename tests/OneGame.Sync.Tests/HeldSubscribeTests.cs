namespace OneGame.Sync.Tests;

/// <summary>A subscribe that arrives before the game has set the connection's trust is held, not refused.</summary>
public class HeldSubscribeTests
{
    private sealed class Rig
    {
        public long Now;
        public readonly Node Host;
        public readonly Node Client;
        public readonly AuthorityScope Scope;
        public readonly Connection Conn;
        public readonly ReplicaScope Replica;
        public readonly List<byte[]> Sent = [];

        public Rig(Trust initial)
        {
            var registry = new EntityRegistry();
            registry.Add(new Marker().TypeId, "Marker", static () => new Marker());
            Host = new Node("host", new ClusterOptions { Clock = () => Now });
            var world = new World(2, registry);
            world.Add(new Marker { Cell = 1, Tag = "held-payload" });
            Scope = Host.AddScope(new ScopeId(1), world);
            var (a, b) = Loopback.CreatePair();
            Conn = Host.Accept(b, initial, "late");
            Client = new Node("client", new ClusterOptions { Clock = () => Now });
            Replica = Client.Subscribe(Client.ConnectVia(a, "host"), Scope.Id, new World(2, registry, WorldRole.Replica));
        }

        public void Run(int ticks = 3)
        {
            for (var i = 0; i < ticks; i++)
            {
                _ = Host.Pump();
                Scope.StepAndPublish();
                _ = Client.Pump();
            }
        }
    }

    [Fact]
    public void ASubscribeHeldWhileTrustIsEmptyIsAnsweredOnceTrustIsSet()
    {
        var rig = new Rig(Trust.None);
        rig.Run();
        Assert.Equal(ReplicaState.Subscribing, rig.Replica.State);
        Assert.Empty(rig.Host.Refusals);

        rig.Conn.Trust = Trust.Reader(rig.Scope.Id);
        rig.Run();
        Assert.Equal(ReplicaState.Live, rig.Replica.State);
        Assert.Single(rig.Replica.World.Entities);
        Assert.Empty(rig.Host.Refusals);
    }

    [Fact]
    public void ASubscribeHeldWhileTrustIsEmptyIsRefusedWhenTheTrustLeavesTheScopeOut()
    {
        var rig = new Rig(Trust.None);
        rig.Run();
        rig.Conn.Trust = Trust.Reader(new ScopeId(99));
        rig.Run();
        Assert.Equal(ReplicaState.Refused, rig.Replica.State);
        Assert.Equal(RefusalReason.NotTrustedToRead, Assert.Single(rig.Host.Refusals).Reason);
    }

    [Fact]
    public void ASubscribeThatIsHeldTooLongIsRefused()
    {
        var rig = new Rig(Trust.None);
        rig.Run();
        rig.Now += 4_999;
        rig.Run();
        Assert.Equal(ReplicaState.Subscribing, rig.Replica.State);
        rig.Now += 2;
        rig.Run();
        Assert.Equal(ReplicaState.Refused, rig.Replica.State);

        // Setting the trust afterwards does not resurrect the dropped request.
        rig.Conn.Trust = Trust.Reader(rig.Scope.Id);
        rig.Run();
        Assert.Equal(ReplicaState.Refused, rig.Replica.State);
    }

    [Fact]
    public void ASubscribeHeldWhenTheConnectionDropsIsForgotten()
    {
        var rig = new Rig(Trust.None);
        rig.Run();
        Assert.True(rig.Host.Detach(rig.Conn));
        rig.Conn.Trust = Trust.Reader(rig.Scope.Id);
        rig.Run();
        Assert.Equal(ReplicaState.Subscribing, rig.Replica.State);
        Assert.Empty(rig.Scope.Subscribers);
    }

    [Fact]
    public void ATrustThatNamesOtherScopesStillRefusesAtOnce()
    {
        var rig = new Rig(Trust.Reader(new ScopeId(99)));
        rig.Run(1);
        Assert.Equal(ReplicaState.Refused, rig.Replica.State);
    }

    [Fact]
    public void AReconnectDoesNotAskAgainForAScopeThatWasRefused()
    {
        var rig = new Rig(Trust.Reader(new ScopeId(99)));
        rig.Run(2);
        Assert.Equal(ReplicaState.Refused, rig.Replica.State);
        var refusals = rig.Host.Refusals.Count;

        var (a, b) = Loopback.CreatePair();
        var again = rig.Host.Accept(b, Trust.Reader(new ScopeId(99)), "again");
        rig.Client.Reconnect(rig.Replica.Link, a);
        rig.Run(3);
        Assert.Equal(ReplicaState.Refused, rig.Replica.State);
        Assert.Equal(refusals, rig.Host.Refusals.Count);
        Assert.Empty(again.HeldSubscribes ?? []);
    }
}
