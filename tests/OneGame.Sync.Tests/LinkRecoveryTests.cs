using OneGame.Core.Tests.Samples;

namespace OneGame.Sync.Tests;

/// <summary>Connections that go away or go quiet, messages too long to send whole, and the way back after an authority restarts.</summary>
public class LinkRecoveryTests
{
    private const int TransportCap = 1 << 20;

    private sealed class Wire
    {
        public Wire(ClusterOptions options, Func<IMessageChannel, IMessageChannel>? wrapServerEnd = null)
        {
            Cluster = new Cluster(options);
            Host = Cluster.AddNode("host");
            Client = Cluster.AddNode("client");
            Scope = Cluster.AddScope(Host, new ScopeId(1), Arena.Match());
            (ClientEnd, var serverEnd) = Loopback.CreatePair();
            ServerEnd = wrapServerEnd?.Invoke(serverEnd) ?? serverEnd;
            Connection = Host.Accept(ServerEnd, Trust.Owner(Scope.Id).As(1, 10), "remote-ada");
            Link = Client.ConnectVia(ClientEnd);
            Replica = Client.Subscribe(Link, Scope.Id, Arena.Empty(WorldRole.Replica));
        }

        public Cluster Cluster { get; }

        public Node Host { get; }

        public Node Client { get; }

        public AuthorityScope Scope { get; }

        public IMessageChannel ClientEnd { get; }

        public IMessageChannel ServerEnd { get; }

        public Connection Connection { get; }

        public Link Link { get; }

        public ReplicaScope Replica { get; }
    }

    // What a transport does with a message over its limit: it refuses it. Sync must never ask for that.
    private sealed class CappedChannel(IMessageChannel inner) : IMessageChannel
    {
        public int Sent { get; private set; }

        public int Parts { get; private set; }

        public void Send(ReadOnlySpan<byte> message)
        {
            Assert.True(message.Length <= TransportCap, $"a {message.Length} byte message went to a transport that takes {TransportCap}");
            Sent++;
            if (message.Length > 0 && message[0] == 23)
            {
                Parts++;
            }

            inner.Send(message);
        }

        public bool TryReceive(out ReadOnlySpan<byte> message) => inner.TryReceive(out message);
    }

    [Fact]
    public void ADetachedConnectionLeavesNoTraceAndTheScopeKeepsPublishing()
    {
        var wire = new Wire(new ClusterOptions { DriftCheckInterval = 1, FatalDrift = true });
        wire.Cluster.Step(10);
        Assert.Single(wire.Host.Connections);
        Assert.Single(wire.Scope.Subscribers);

        Assert.True(wire.Host.Detach(wire.Connection));
        Assert.Empty(wire.Host.Connections);
        Assert.Empty(wire.Scope.Subscribers);
        Assert.False(wire.Host.Detach(wire.Connection)); // twice is fine

        var tick = wire.Scope.World.Tick;
        wire.Cluster.Step(5);
        Assert.Equal(tick + 5, wire.Scope.World.Tick);
        Assert.Empty(wire.Host.ProtocolErrors);
    }

    [Fact]
    public void ManyConnectionsComingAndGoingNeverLeaveStaleEntries()
    {
        var cluster = new Cluster(new ClusterOptions());
        var host = cluster.AddNode("host");
        var scope = cluster.AddScope(host, new ScopeId(1), Arena.Match());
        for (var round = 0; round < 20; round++)
        {
            var (a, b) = Loopback.CreatePair();
            var connection = host.Accept(b, Trust.Player(scope.Id), "c" + round);
            var client = cluster.AddNode("n" + round);
            var link = client.ConnectVia(a);
            client.Subscribe(link, scope.Id, Arena.Empty(WorldRole.Replica));
            cluster.Step(2);
            Assert.Single(host.Connections);
            Assert.True(host.Detach(connection));
            cluster.RemoveNode(client);
        }

        Assert.Empty(host.Connections);
        Assert.Empty(scope.Subscribers);
    }

    [Fact]
    public void ASnapshotOverTheTransportLimitArrivesInPiecesAndTheReplicaAgrees()
    {
        var capped = (CappedChannel?)null;
        var wire = new Wire(new ClusterOptions { DriftCheckInterval = 1, FatalDrift = true }, end => capped = new CappedChannel(end));
        for (var i = 0; i < 30_000; i++)
        {
            wire.Scope.World.Add(new Hero { Player = (uint)(100 + i), Team = 1, Name = "hero-with-a-rather-long-name-number-" + i });
        }

        wire.Cluster.Step(10);
        Assert.Equal(ReplicaState.Live, wire.Replica.State);
        Assert.True(capped!.Parts >= 5, "the snapshot should have been split, pieces: " + capped.Parts);
        Assert.Equal(wire.Scope.ViewHashFor(wire.Connection), wire.Replica.ViewHash);
        Assert.Empty(wire.Client.ProtocolErrors);
        Assert.True(Rig.Count<Hero>(wire.Replica) >= 30_000);

        // A frame that adds as much is split the same way.
        for (var i = 0; i < 30_000; i++)
        {
            wire.Scope.World.Add(new Hero { Player = (uint)(50_000 + i), Team = 1, Name = "second-wave-of-heroes-with-long-names-" + i });
        }

        var before = capped.Parts;
        wire.Cluster.Step(3);
        Assert.True(capped.Parts > before + 4);
        Assert.Equal(wire.Scope.ViewHashFor(wire.Connection), wire.Replica.ViewHash);
        Assert.Equal(ReplicaState.Live, wire.Replica.State);
        Assert.Empty(wire.Client.ProtocolErrors);
    }

    [Fact]
    public void HostilePiecesAreRefusedBeforeAnythingIsAllocatedAndTheReplicaRecovers()
    {
        var wire = new Wire(new ClusterOptions { DriftCheckInterval = 1 });
        wire.Cluster.Step(5);
        var fault = (Action<byte[]>)(bytes => wire.ServerEnd.Send(bytes));
        var errors = wire.Client.ProtocolErrors.Count;

        // 4 GiB claimed in the first piece's header.
        var before = GC.GetAllocatedBytesForCurrentThread();
        fault([23, 0x80, 0x80, 0x80, 0x80, 0x10, 0, 1, 2, 3]);
        wire.Client.Pump();
        Assert.True(GC.GetAllocatedBytesForCurrentThread() - before < 256 * 1024, "the claimed size must not be allocated");
        Assert.True(wire.Client.ProtocolErrors.Count > errors);

        // A continuation with no start, an offset that skips ahead, and more bytes than announced.
        errors = wire.Client.ProtocolErrors.Count;
        fault([23, 100, 50, 1, 2, 3]);
        fault([23, 10, 0, 1, 2, 3]);
        fault([23, 10, 5, 1, 2, 3]);
        fault([23, 4, 0, 1, 2, 3, 4, 5, 6]);
        wire.Client.Pump();
        Assert.True(wire.Client.ProtocolErrors.Count >= errors + 3);

        // Truncated headers and a piece inside a piece (assembled bytes that start with another piece marker).
        fault([23]);
        fault([23, 3, 0, 23, 0, 0]);
        wire.Client.Pump();

        // The authority carries on; the replica asks again where it must and ends up agreeing, or gives up in the open (Failed), never throws.
        wire.Cluster.Step(30);
        Assert.True(wire.Replica.State is ReplicaState.Live or ReplicaState.Failed);
        if (wire.Replica.State == ReplicaState.Live)
        {
            Assert.Equal(wire.Scope.ViewHashFor(wire.Connection), wire.Replica.ViewHash);
        }
    }

    [Fact]
    public void ACommandNobodyAnswersTimesOutAndALateAnswerIsIgnored()
    {
        long now = 0;
        var wire = new Wire(new ClusterOptions { DriftCheckInterval = 0, Clock = () => now, CommandTimeoutMs = 2000, SilenceTimeoutMs = 0 });
        wire.Cluster.Step(5);
        var hero = Rig.HeroOf(wire.Replica, 1);
        var completed = new List<CommandResult>();
        wire.Replica.CommandCompleted += completed.Add;

        hero.Send.Spend(1);
        var sequence = wire.Replica.LastSequence;
        Assert.Equal(1, wire.Replica.PendingCommands);

        now += 1999;
        wire.Client.Pump();
        Assert.Equal(1, wire.Replica.PendingCommands);
        now += 1;
        wire.Client.Pump();
        Assert.Equal(0, wire.Replica.PendingCommands);
        var result = Assert.Single(completed);
        Assert.Equal(sequence, result.Sequence);
        Assert.Equal(CommandVerdict.TimedOut, result.Verdict);

        // The authority was only slow: its answer arrives and changes nothing.
        wire.Cluster.Step(2);
        Assert.Single(completed);
        Assert.Single(wire.Replica.Results);
    }

    [Fact]
    public void AnAuthorityThatGoesQuietIsNoticedWithinTheSilenceBoundAndTheReplicaRecoversWhenItSpeaks()
    {
        long now = 0;
        var wire = new Wire(new ClusterOptions { DriftCheckInterval = 1, Clock = () => now, SilenceTimeoutMs = 3000, CommandTimeoutMs = 60_000 });
        wire.Cluster.Step(5);
        Assert.False(wire.Replica.IsSilent);
        var silent = 0;
        var resumed = 0;
        wire.Replica.AuthoritySilent += _ => silent++;
        wire.Replica.AuthorityResumed += _ => resumed++;
        var hero = Rig.HeroOf(wire.Replica, 1);
        hero.Send.Spend(1);

        // The authority stops ticking and answering. Nothing is flagged before the bound, everything is after it.
        now += 2999;
        wire.Client.Pump();
        Assert.False(wire.Replica.IsSilent);
        Assert.Equal(1, wire.Replica.PendingCommands);
        now += 1;
        wire.Client.Pump();
        Assert.True(wire.Replica.IsSilent);
        Assert.Equal(1, silent);
        Assert.Equal(0, wire.Replica.PendingCommands);
        Assert.Equal(CommandVerdict.TimedOut, wire.Replica.Results[^1].Verdict);
        wire.Client.Pump();
        Assert.Equal(1, silent); // raised once

        wire.Cluster.Step(3);
        Assert.False(wire.Replica.IsSilent);
        Assert.Equal(1, resumed);
        Assert.Equal(wire.Scope.ViewHashFor(wire.Connection), wire.Replica.ViewHash);
    }

    [Fact]
    public void ReconnectingOverANewChannelRebuildsTheReplicaFromAFreshSnapshot()
    {
        var wire = new Wire(new ClusterOptions { DriftCheckInterval = 1, FatalDrift = true });
        wire.Cluster.Step(20);
        var hero = Rig.HeroOf(wire.Replica, 1);
        hero.Send.Spend(1);
        Assert.Equal(1, wire.Replica.PendingCommands);

        // The old connection dies; the authority forgets it. The restarted authority (here the same one) accepts a new channel.
        wire.Host.Detach(wire.Connection);
        var (clientEnd, serverEnd) = Loopback.CreatePair();
        var connection = wire.Host.Accept(serverEnd, Trust.Owner(wire.Scope.Id).As(1, 10), "remote-ada-again");
        wire.Client.Reconnect(wire.Link, clientEnd);
        Assert.Equal(0, wire.Replica.PendingCommands);
        Assert.Equal(CommandVerdict.Dropped, wire.Replica.Results[^1].Verdict);
        Assert.Equal(ReplicaState.Subscribing, wire.Replica.State);

        wire.Cluster.Step(10);
        Assert.Equal(ReplicaState.Live, wire.Replica.State);
        Assert.Equal(wire.Scope.ViewHashFor(connection), wire.Replica.ViewHash);
        Assert.Single(wire.Host.Connections);

        // Commands work again over the new channel.
        Rig.HeroOf(wire.Replica, 1).Send.Spend(1);
        wire.Cluster.Step(3);
        Assert.Equal(CommandVerdict.Executed, wire.Replica.Results[^1].Verdict);
    }

    [Fact]
    public void ReconnectingARefusedReplicaOrAForeignLinkIsRefused()
    {
        var wire = new Wire(new ClusterOptions());
        var other = new Node("other");
        var (a, _) = Loopback.CreatePair();
        Assert.Throws<ArgumentException>(() => other.Reconnect(wire.Link, a));
        Assert.True(wire.Client.Detach(wire.Link));
        Assert.Equal(ReplicaState.Unsubscribed, wire.Replica.State);
        Assert.Throws<ArgumentException>(() => wire.Client.Reconnect(wire.Link, a));
    }
}
