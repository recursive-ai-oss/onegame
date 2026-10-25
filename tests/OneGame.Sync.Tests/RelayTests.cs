namespace OneGame.Sync.Tests;

public class RelayTests
{
    private static Trust Of(RelayRig rig, uint player, uint team) => Trust.Owner(rig.Scope.Id).As(player, team);

    [Fact]
    public void ARelayServesTwoAudiencesFor1200TicksWithZeroDriftAndNeverWidensAView()
    {
        var rig = new RelayRig();
        var ada = rig.JoinRelay("ada", Of(rig, 1, 10));
        var bo = rig.JoinRelay("bo", Of(rig, 2, 20));
        var guest = rig.JoinRelay("guest", Trust.Reader(rig.Scope.Id));
        var direct = rig.JoinDirect("ada-direct", Of(rig, 1, 10));
        rig.Cluster.Settle();

        for (var tick = 0; tick < 1200; tick++)
        {
            var hero = Rig.HeroOf(ada.Replica, 1);
            if (tick % 3 == 0)
            {
                hero.Send.Walk(1, tick % 5);
            }

            if (tick % 7 == 0)
            {
                hero.Send.Hoard("ruby-hoard-7731");
            }

            if (tick % 50 == 0)
            {
                hero.Send.Rename("Ada " + tick); // owner-only: the relay checks the sender, the authority trusts the relay's check
            }

            if (tick % 11 == 0)
            {
                Rig.HeroOf(bo.Replica, 2).Send.Spend(1);
            }

            if (tick % 40 == 0)
            {
                Rig.HeroOf(bo.Replica, 1).Send.Rename("Bo hijacks Ada"); // not the owner: refused at the relay
            }

            rig.Cluster.Step();
            if (tick % 100 == 99)
            {
                Assert.Empty(rig.Cluster.CheckReplicas());
            }
        }

        rig.Cluster.Step(3);
        Assert.Empty(rig.Cluster.DriftReports);
        Assert.Empty(rig.Cluster.CheckReplicas());
        Assert.Equal(rig.Scope.World.Tick, ada.Replica.World.Tick);
        Assert.Equal(rig.Scope.World.Tick, bo.Replica.World.Tick);
        Assert.True(ada.Replica.FramesApplied >= 1200 && bo.Replica.FramesApplied >= 1200 && rig.Upstream.FramesApplied >= 1200, "every hop applied every frame");
        Assert.Equal(0, ada.Replica.Resyncs + bo.Replica.Resyncs + rig.Upstream.Resyncs);

        // Through the relay, Ada sees exactly what she sees directly.
        Assert.Equal(direct.Replica.ViewHash, ada.Replica.ViewHash);
        Assert.Equal(rig.Scope.ViewHashFor(direct.Connection), ada.Replica.ViewHash);

        // Owner-only members reach their owner and nobody else, byte for byte on the wire.
        Assert.True(ada.Tap.Contains("ruby-hoard-7731"));
        Assert.True(ada.Tap.Contains("ada-diary-secret"));
        Assert.False(bo.Tap.Contains("ruby-hoard-7731"));
        Assert.False(bo.Tap.Contains("ada-diary-secret"));
        Assert.True(bo.Tap.Contains("bo-diary-secret"));
        Assert.False(ada.Tap.Contains("bo-diary-secret"));
        Assert.False(guest.Tap.Contains("ruby-hoard-7731"));
        Assert.False(guest.Tap.Contains("ada-diary-secret"));
        Assert.False(guest.Tap.Contains("emerald-cache-2208"));
        Assert.False(guest.Tap.Contains("bo-diary-secret"));

        // Hidden members never left the authority, not even to the relay.
        Assert.DoesNotContain(rig.Upstream.World.Entities, e => e is Director);
        Assert.Equal(0, Rig.HeroOf(rig.Upstream, 1).Plot);
        Assert.NotEqual(0, Rig.HeroOf(rig.Scope, 1).Plot);

        // Team data follows the team: Ada never sees Bo's team intel.
        Assert.Single(ada.Replica.World.Entities.OfType<Intel>());
        Assert.Equal(10u, ada.Replica.World.Entities.OfType<Intel>().Single().Team);
        Assert.Empty(guest.Replica.World.Entities.OfType<Intel>());

        // Answers came back through the relay with the senders' own sequence numbers.
        Assert.Contains(ada.Replica.Results, r => r.Verdict == CommandVerdict.Executed);
        Assert.Contains(bo.Replica.Results, r => r.Verdict == CommandVerdict.Refused && r.Refusal == RefusalReason.NotOwner);
        Assert.Equal(0, ada.Replica.PendingCommands);
        Assert.StartsWith("Ada ", Rig.HeroOf(rig.Scope, 1).Name);
    }

    [Fact]
    public void ARelayResumesWhenItsAuthorityRestartsFromAnOlderSave()
    {
        var rig = new RelayRig();
        var ada = rig.JoinRelay("ada", Of(rig, 1, 10));
        var guest = rig.JoinRelay("guest", Trust.Reader(rig.Scope.Id));
        rig.Cluster.Step(10);
        var save = rig.Scope.Save();
        var savedTick = rig.Scope.World.Tick;
        rig.Cluster.Step(15);
        Assert.Equal(rig.Scope.World.Tick, ada.Replica.World.Tick);

        // The authority comes back from its save: its tick is lower than what the relay already served.
        Assert.Equal(SaveStatus.Ok, rig.Scope.Load(save).Status);
        Assert.Equal(savedTick, rig.Scope.World.Tick);
        rig.Cluster.Step(20);

        Assert.Equal(rig.Scope.World.Tick, rig.Upstream.World.Tick);
        Assert.Equal(rig.Scope.World.Tick, ada.Replica.World.Tick);
        Assert.Equal(rig.Scope.World.Tick, guest.Replica.World.Tick);
        Assert.Empty(rig.Cluster.CheckReplicas());
        Assert.Empty(rig.Cluster.DriftReports);
    }

    /// <summary>A channel whose far end can be replaced, like a link that opens again to a restarted peer.</summary>
    private sealed class Swappable(IMessageChannel first) : IMessageChannel
    {
        public IMessageChannel Inner { get; set; } = first;

        public void Send(ReadOnlySpan<byte> message) => Inner.Send(message);

        public bool TryReceive(out ReadOnlySpan<byte> message) => Inner.TryReceive(out message);
    }

    [Theory]
    [InlineData(10, 25)]
    [InlineData(15, 15)]
    [InlineData(0, 12)]
    public void ARelayResumesWhenItsAuthorityIsReplacedByOneRestartedFromAnOlderSave(int ticksBeforeSave, int ticksBeforeCrash)
    {
        Swappable? link = null;
        var rig = new RelayRig(null, end => link = new Swappable(end));
        var ada = rig.JoinRelay("ada", Of(rig, 1, 10));
        var guest = rig.JoinRelay("guest", Trust.Reader(rig.Scope.Id));
        rig.Cluster.Step(ticksBeforeSave);
        var save = rig.Scope.Save();
        var savedTick = rig.Scope.World.Tick;
        rig.Cluster.Step(ticksBeforeCrash);
        Assert.Equal(rig.Scope.World.Tick, ada.Replica.World.Tick);

        // The authority process dies and a new one starts from the save: a lower tick than the relay already served, and a new connection.
        Assert.True(rig.Cluster.RemoveNode(rig.Auth));
        var restarted = rig.Cluster.AddNode("auth-again");
        var scope = rig.Cluster.AddScope(restarted, rig.Scope.Id, Arena.Empty(WorldRole.Authority));
        Assert.Equal(SaveStatus.Ok, scope.Load(save).Status);
        Assert.Equal(savedTick, scope.World.Tick);
        var (relayEnd, authorityEnd) = Loopback.CreatePair();
        var connection = restarted.Accept(authorityEnd, Trust.Owner(scope.Id).AsRelay(), "relay");
        link!.Inner = relayEnd;
        rig.Relay.Resubscribe(rig.Upstream);
        rig.Cluster.Step(30);

        Assert.Equal(scope.World.Tick, rig.Upstream.World.Tick);
        Assert.Equal(scope.World.Tick, ada.Replica.World.Tick);
        Assert.Equal(scope.World.Tick, guest.Replica.World.Tick);

        // The relay's link still names the node that died, so its hop is compared by hand; the other hops by the cluster.
        Assert.Equal(scope.ViewHashFor(connection), rig.Upstream.ViewHash);
        Assert.DoesNotContain(rig.Cluster.CheckReplicas(), r => r.ReplicaNode != "relay");
        Assert.Empty(rig.Cluster.DriftReports);
    }

    [Fact]
    public void AMismatchInjectedAtTheRelayIsReportedNamingTheHop()
    {
        var rig = new RelayRig(new ClusterOptions { DriftCheckInterval = 1, FatalDrift = false, ResyncOnDrift = true });
        var ada = rig.JoinRelay("ada", Of(rig, 1, 10));
        rig.Cluster.Step(10);
        Assert.Empty(rig.Cluster.DriftReports);

        // The relay's copy of Ada's name is changed behind the protocol's back.
        Tamper(Rig.HeroOf(rig.Upstream, 1), "Name", "tampered");
        rig.Cluster.Step(3);

        Assert.NotEmpty(rig.Relay.DriftReports);
        var report = rig.Relay.DriftReports[0];
        Assert.Equal("relay", report.ReplicaNode);
        Assert.Equal("auth", report.Upstream);
        Assert.Contains("auth -> relay", report.ToString());
        Assert.Contains("auth", report.ToJson());
        Assert.Contains(report.Differences, d => d.EntityId == Rig.HeroOf(rig.Scope, 1).Id.Value);

        // Ada's own hop is consistent with the relay's tampered state, so only the relay's hop was named; the resync heals both.
        Assert.Empty(ada.Node.DriftReports);
        rig.Cluster.Step(10);
        Assert.Equal("Ada", Rig.HeroOf(rig.Upstream, 1).Name);
        Assert.Equal("Ada", Rig.HeroOf(ada.Replica, 1).Name);
        Assert.Empty(rig.Cluster.CheckReplicas());

        // And a mismatch at the last hop names that one.
        Tamper(Rig.HeroOf(ada.Replica, 1), "Name", "tampered-too");
        rig.Cluster.Step(3);
        Assert.NotEmpty(ada.Node.DriftReports);
        var last = ada.Node.DriftReports[0];
        Assert.Equal("relay", last.Upstream);
        Assert.Equal("ada", last.ReplicaNode);
        Assert.Contains("relay -> ada", last.ToString());
    }

    [Fact]
    public void ARelayThatIsNotTrustedAsOneOnlySeesItsOwnAudience()
    {
        // Without Trust.AsRelay the relay is an ordinary subscriber: it can pass on only what its own audience sees.
        var cluster = new Cluster(new ClusterOptions { DriftCheckInterval = 1, FatalDrift = true });
        var auth = cluster.AddNode("auth");
        var relay = cluster.AddNode("relay");
        var scope = cluster.AddScope(auth, new ScopeId(1), Arena.Match());
        var link = cluster.Connect(relay, auth, Trust.Owner(scope.Id).As(2, 20));
        var up = relay.Subscribe(link, scope.Id, Arena.Empty(WorldRole.Replica));
        relay.Relay(up);
        var node = cluster.AddNode("ada");
        var (a, b) = Loopback.CreatePair();
        var tap = new TapChannel(a);
        var clientLink = node.ConnectTo(relay, Trust.Owner(scope.Id).As(1, 10), tap, b, "ada");
        var view = node.Subscribe(clientLink, scope.Id, Arena.Empty(WorldRole.Replica));
        cluster.Step(30);

        // Ada claims to be player 1, but the relay only holds what player 2 may see, so her private stash cannot reach her.
        Assert.False(tap.Contains("ruby-hoard-7731"));
        Assert.False(tap.Contains("ada-diary-secret"));
        Assert.Equal(string.Empty, Rig.HeroOf(view, 1).Stash ?? string.Empty);
        Assert.Empty(cluster.DriftReports);
    }

    private static void Tamper(Entity entity, string property, string value)
    {
        var index = -1;
        for (var i = 0; i < entity.PropertyCount; i++)
        {
            if (entity.PropertyName(i) == property)
            {
                index = i;
            }
        }

        Assert.True(index >= 0);
        var scratch = entity.TypeId == new Hero().TypeId ? new Hero() : throw new InvalidOperationException("only heroes");
        Assert.True(scratch.TrySetFromText(property, value));
        var w = new SyncWriter(64);
        scratch.WriteMaskedDelta(w, 1UL << index);
        var r = new SyncReader(w.Written);
        entity.ApplyDelta(ref r);
    }
}
