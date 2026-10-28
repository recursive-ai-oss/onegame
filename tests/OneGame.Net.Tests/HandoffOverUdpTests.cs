using System.Net;
using OneGame.Sync.Tests;

namespace OneGame.Net.Tests;

/// <summary>
/// The hand-off between two game servers over real UDP sockets on the loopback interface, with 80 ms of latency each way and 2% loss in
/// both directions on every connection, while a client keeps sending commands to the traveller.
/// </summary>
public class HandoffOverUdpTests
{
    private const uint Player = 5;
    private static readonly ScopeId ScopeA = new(1);
    private static readonly ScopeId ScopeB = new(2);

    private sealed class Pairing(UdpListener server, UdpConnection client, INetConnection accepted)
    {
        public UdpListener Server { get; } = server;

        public UdpConnection Client { get; } = client;

        public INetConnection Accepted { get; } = accepted;
    }

    private sealed class Network(ManualNetClock clock) : IDisposable
    {
        private readonly List<UdpListener> _listeners = [];
        private readonly List<UdpConnection> _clients = [];
        private readonly List<INetConnection> _accepted = [];
        private int _seed = 100;

        public void Pump()
        {
            foreach (var l in _listeners)
            {
                l.Pump();
            }

            foreach (var c in _clients)
            {
                c.Pump();
            }
        }

        /// <summary>A client connection and the connection its server accepted, both through the lossy conditioner.</summary>
        public Pairing Pair()
        {
            NetworkConditions Conditions() => new() { LatencyMs = 80, JitterMs = 8, LossRate = 0.02, Seed = _seed++ };
            var server = UdpTransport.Listen(new ConditionedDatagramSocket(new SystemDatagramSocket(new IPEndPoint(IPAddress.Loopback, 0)), Conditions(), clock), Plain.Udp, clock);
            var client = UdpTransport.Connect(new ConditionedDatagramSocket(new SystemDatagramSocket(new IPEndPoint(IPAddress.Loopback, 0)), Conditions(), clock), server.LocalEndPointAddress(), Plain.Udp, clock);
            _listeners.Add(server);
            _clients.Add(client);
            INetConnection? accepted = null;
            for (var i = 0; i < 20_000 && (accepted is null || client.State != ConnectionState.Connected); i++)
            {
                clock.Advance(5);
                Pump();
                accepted ??= server.TryAccept(out var a) ? a : null;
                Thread.Sleep(0);
            }

            Assert.NotNull(accepted);
            _accepted.Add(accepted!);
            return new Pairing(server, client, accepted!);
        }

        public long Retransmits => _clients.Sum(c => c.Stats.Retransmits) + _accepted.Sum(a => a.Stats.Retransmits);

        public void Dispose()
        {
            foreach (var l in _listeners)
            {
                l.Dispose();
            }
        }
    }

    [Fact]
    public void ATravellerMovesBetweenGameServersOverLossyUdpWhileTheClientKeepsHittingIt()
    {
        var clock = new ManualNetClock();
        using var net = new Network(clock);
        var options = new ClusterOptions { DriftCheckInterval = 1, DigestTimeoutTicks = 200 };
        var nodeA = new Node("A", options);
        var nodeB = new Node("B", options);
        var client = new Node("client", options);
        var scopeA = nodeA.AddScope(ScopeA, Realm.NewWorld(WorldRole.Authority));
        var scopeB = nodeB.AddScope(ScopeB, Realm.NewWorld(WorldRole.Authority, 12));
        scopeA.World.Add(new Beacon());
        scopeB.World.Add(new Beacon());
        var traveler = scopeA.World.Add(new Traveler { Player = Player, Name = "Wanda", Diary = "owner-only-diary-4417", Secret = 99 });
        var original = traveler.Id;
        scopeA.TransferOfferTicks = 400;
        scopeA.TransferResendTicks = 20;

        // Three real connections: A to B (the hand-off link), the client to A, and the client to B.
        var ab = net.Pair();
        var linkAB = nodeA.ConnectVia(ab.Client, "B");
        nodeB.Accept(ab.Accepted, Trust.Owner(ScopeB), "A");
        var ca = net.Pair();
        var linkCA = client.ConnectVia(ca.Client, "A");
        nodeA.Accept(ca.Accepted, Trust.Owner(ScopeA).As(Player), "client");
        var cb = net.Pair();
        var linkCB = client.ConnectVia(cb.Client, "B");
        var connectionAtB = nodeB.Accept(cb.Accepted, Trust.Owner(ScopeB).As(Player), "client");
        client.FollowResolver = scope => scope == ScopeB ? new FollowTarget(linkCB, Realm.NewWorld(WorldRole.Replica, 12)) : null;
        var viewA = client.Subscribe(linkCA, ScopeA, Realm.NewWorld(WorldRole.Replica));

        // A replica keeps only its latest 256 answers, so collect every one as it arrives.
        var verdicts = new Dictionary<(ReplicaScope, uint), CommandVerdict>();
        void Collect(ReplicaScope replica) => replica.CommandCompleted += r => verdicts[(replica, r.Sequence)] = r.Verdict;
        Collect(viewA);
        client.ReplicaAdded += Collect;

        var recordA = scopeA.StartRecording(new RecordingOptions { CheckpointInterval = 1 });
        var recordB = scopeB.StartRecording(new RecordingOptions { CheckpointInterval = 1 });
        var nodes = new[] { nodeA, nodeB, client };
        var sent = new List<(ReplicaScope Replica, uint Sequence)>();
        var violations = 0;
        var maxCopiesSeen = 0;

        void Tick(bool send, bool step = true)
        {
            if (send)
            {
                foreach (var r in client.ReplicaScopes)
                {
                    if (r.State == ReplicaState.Live && Realm.FindTraveler(r.World, Player) is { } t)
                    {
                        t.Send.Hit();
                        sent.Add((r, r.LastSequence));
                        break;
                    }
                }
            }

            clock.Advance(16);
            net.Pump();
            foreach (var n in nodes)
            {
                n.Pump();
            }

            if (step)
            {
                scopeA.StepAndPublish();
                scopeB.StepAndPublish();
            }

            net.Pump();
            foreach (var n in nodes)
            {
                n.Pump();
            }

            Thread.Sleep(0);
            var copies = Realm.CountTravelers(scopeA.World, Player) + Realm.CountTravelers(scopeB.World, Player);
            maxCopiesSeen = Math.Max(maxCopiesSeen, copies);
            if (copies > 1)
            {
                violations++;
            }
        }

        for (var i = 0; i < 80; i++)
        {
            Tick(send: true);
        }

        var transfer = scopeA.BeginTransfer(linkAB, ScopeB, [original]);
        for (var i = 0; i < 500; i++)
        {
            Tick(send: true);
        }

        // Stop sending, let the traveller settle, then stop the servers' clocks and let everything in flight land.
        for (var i = 0; i < 100; i++)
        {
            Tick(send: false);
        }

        for (var i = 0; i < 300; i++)
        {
            Tick(send: false, step: false);
        }

        Assert.Equal(TransferState.Complete, transfer.State);
        Assert.Equal(0, violations);
        Assert.True(maxCopiesSeen <= 1);
        Assert.Null(Realm.FindTraveler(scopeA.World, Player));
        var landed = Realm.FindTraveler(scopeB.World, Player)!;
        Assert.NotNull(landed);

        // Every command applied exactly once.
        int executed = 0, other = 0, waiting = 0;
        foreach (var key in sent)
        {
            if (!verdicts.TryGetValue(key, out var verdict))
            {
                waiting++;
            }
            else if (verdict == CommandVerdict.Executed)
            {
                executed++;
            }
            else
            {
                other++;
            }
        }

        Assert.True(sent.Count > 300, $"only {sent.Count} commands sent");
        Assert.Equal(0, other);
        Assert.Equal(0, waiting);
        Assert.Equal(sent.Count, executed);
        Assert.Equal(sent.Count, landed.Hits);

        // The client's replica followed to the second server by itself and agrees with it; nothing drifted anywhere.
        var viewB = client.ReplicaScopes.Single(r => r.Id == ScopeB);
        Assert.Equal(ReplicaState.Live, viewB.State);
        Assert.Equal(scopeB.World.Tick, viewB.World.Tick);
        Assert.Equal(scopeB.ViewHashFor(connectionAtB), viewB.ViewHash);
        Assert.Equal(landed.Hits, Realm.FindTraveler(viewB.World, Player)!.Hits);
        Assert.Equal("owner-only-diary-4417", Realm.FindTraveler(viewB.World, Player)!.Diary);
        Assert.Equal(0, nodeA.DriftCount + nodeB.DriftCount + client.DriftCount);
        Assert.Equal(0, viewA.Resyncs + viewB.Resyncs);
        Assert.True(net.Retransmits > 0, "the conditioner should have forced repeats");
        Assert.True(scopeA.CommandsHeld + scopeA.CommandsForwarded > 0, "no command met the hand-off in flight");

        // Each side's recording replays to its own hashes.
        var ra = recordA.Stop();
        var rb = recordB.Stop();
        var replayA = Replayer.Replay(ra, Realm.NewWorld(WorldRole.Authority));
        var replayB = Replayer.Replay(rb, Realm.NewWorld(WorldRole.Authority, 12));
        Assert.True(replayA.Ok, replayA.Divergence?.ToString());
        Assert.True(replayB.Ok, replayB.Divergence?.ToString());
        Assert.Equal(scopeA.Hash, replayA.FinalHash);
        Assert.Equal(scopeB.Hash, replayB.FinalHash);
        Console.WriteLine($"udp hand-off: {sent.Count} commands, all applied once; {replayA.CheckpointsVerified}+{replayB.CheckpointsVerified} checkpoints replayed; {net.Retransmits} retransmits; held {scopeA.CommandsHeld} and forwarded {scopeA.CommandsForwarded} in flight; transfer id {transfer.Id:x}");
    }
}
