using OneGame.Sync.Tests;
using System.Net;

namespace OneGame.Net.Tests;

/// <summary>
/// Joins two in-process nodes over a real transport without changing the sync API. The nodes are linked by a loopback pair as
/// usual; the bridge moves every message that either node writes into the transport and injects what the transport delivers on
/// the other side, with each node pumped only on what arrived through the network.
/// </summary>
internal sealed class NodeBridge
{
    private readonly Link _link;
    private readonly Node _authority;
    private readonly Node _replica;
    private readonly INetConnection _atReplica;
    private readonly INetConnection _atAuthority;
    private readonly Action _pumpNetwork;

    public NodeBridge(Node authority, Node replica, Link link, INetConnection atAuthority, INetConnection atReplica, Action pumpNetwork)
    {
        _authority = authority;
        _replica = replica;
        _link = link;
        _atAuthority = atAuthority;
        _atReplica = atReplica;
        _pumpNetwork = pumpNetwork;
    }

    /// <summary>Messages moved in each direction, for the report.</summary>
    public int ToAuthority { get; private set; }

    public int ToReplica { get; private set; }

    public void Step()
    {
        Capture();
        _pumpNetwork();
        while (_atAuthority.TryReceive(out var m))
        {
            _link.Channel.Send(m); // into the authority's receive queue
            ToAuthority++;
        }

        _ = _authority.Pump();
        Capture();
        _pumpNetwork();
        while (_atReplica.TryReceive(out var m))
        {
            _link.Connection.Channel.Send(m); // into the replica's receive queue
            ToReplica++;
        }

        _ = _replica.Pump();
        Capture();
    }

    private void Capture()
    {
        while (_link.Channel.TryReceive(out var m))
        {
            _atAuthority.Send(m); // the authority wrote it; it travels to the replica
        }

        while (_link.Connection.Channel.TryReceive(out var m))
        {
            _atReplica.Send(m); // the replica wrote it; it travels to the authority
        }
    }
}

public class TwoNodeSyncTests
{
    private static readonly NetworkConditions Hostile = new()
    {
        LatencyMs = 150,
        JitterMs = 30,
        LossRate = 0.05,
        DuplicateRate = 0.02,
        ReorderRate = 0.05,
        Seed = 11,
    };

    private const int Ticks = 1200;
    private const int TickMs = 16;

    private sealed record Result(int Ticks, int HashChecks, int Drift, int Commands, int Resyncs, NetStats ClientStats, NetStats ServerStats, int ToAuthority, int ToReplica, long MaxLagTicks);

    // Runs the shop-and-bots world for 1200 ticks with scripted commands from the replica, comparing the replica's world to the
    // authority's recorded hash at the same tick every time the replica advances.
    private static Result Play(Func<ManualNetClock, (INetConnection AtAuthority, INetConnection AtReplica, Action Pump, IDisposable Cleanup)> connect)
    {
        var clock = new ManualNetClock();
        var options = new ClusterOptions { DriftCheckInterval = 1 };
        var authority = new Node("host", options);
        var replica = new Node("player", options);
        var scope = authority.AddScope(new ScopeId(1), Scenario.NewAuthority(8, 30));
        var link = replica.ConnectTo(authority, Trust.Player(scope.Id));
        var view = replica.Subscribe(link, scope.Id, Scenario.NewWorld(WorldRole.Replica));
        var (atAuthority, atReplica, pump, cleanup) = connect(clock);
        using var _ = cleanup;
        var bridge = new NodeBridge(authority, replica, link, atAuthority, atReplica, pump);

        var hashAt = new List<ulong> { scope.World.ComputeHash() };
        var checks = 0;
        long maxLag = 0;
        for (var t = 0; t < Ticks; t++)
        {
            clock.Advance(TickMs);
            Scenario.Input(view.World, view.World.Tick);
            scope.StepAndPublish();
            hashAt.Add(scope.World.ComputeHash());
            bridge.Step();
            var tick = (int)view.World.Tick;
            maxLag = Math.Max(maxLag, scope.World.Tick - tick);
            if (view.State == ReplicaState.Live && tick > 0 && tick < hashAt.Count)
            {
                Assert.True(hashAt[tick] == view.World.ComputeHash(), $"replica differs from the authority at tick {tick} (loop {t})");
                checks++;
            }
        }

        // Stop the authority and let the network drain: the replica must catch up and end on the very same state.
        for (var i = 0; i < 2000 && view.World.Tick < scope.World.Tick; i++)
        {
            clock.Advance(TickMs);
            bridge.Step();
        }

        Console.WriteLine($"DEBUG auth {scope.World.Tick} replica {view.World.Tick} state {view.State} server {Fmt(atAuthority.Stats)} client {Fmt(atReplica.Stats)}");
        Assert.Equal(scope.World.Tick, view.World.Tick);
        Assert.Equal(scope.World.ComputeHash(), view.World.ComputeHash());
        Assert.Equal(0, replica.DriftCount);
        Assert.Empty(replica.DriftReports);
        return new Result(Ticks, checks, replica.DriftCount, (int)scope.CommandsRun, view.Resyncs, atReplica.Stats, atAuthority.Stats, bridge.ToAuthority, bridge.ToReplica, maxLag);
    }

    [Fact]
    public void OverRealUdpSocketsWithAHostileNetworkThereIsNoDrift()
    {
        var r = Play(clock =>
        {
            var server = UdpTransport.Listen(new ConditionedDatagramSocket(new SystemDatagramSocket(new IPEndPoint(IPAddress.Loopback, 0)), Hostile, clock), null, clock);
            var client = UdpTransport.Connect(
                new ConditionedDatagramSocket(new SystemDatagramSocket(new IPEndPoint(IPAddress.Loopback, 0)), new NetworkConditions { LatencyMs = 150, JitterMs = 30, LossRate = 0.05, DuplicateRate = 0.02, ReorderRate = 0.05, Seed = 12 }, clock),
                server.LocalEndPointAddress(),
                null,
                clock);
            INetConnection? accepted = null;
            void Pump()
            {
                server.Pump();
                client.Pump();
                accepted ??= server.TryAccept(out var a) ? a : null;
                Thread.Sleep(0);
            }

            // Handshake first, in virtual time.
            for (var i = 0; i < 5000 && (accepted is null || client.State != ConnectionState.Connected); i++)
            {
                clock.Advance(5);
                Pump();
            }

            Assert.NotNull(accepted);
            return (accepted!, client, Pump, new Disposer(server.Dispose));
        });
        Assert.True(r.HashChecks > 900, $"only {r.HashChecks} per-tick comparisons");
        Assert.True(r.ClientStats.Retransmits + r.ServerStats.Retransmits > 0, "the conditioner should have forced repeats");
        Assert.True(r.Commands > 100);
        Assert.True(r.ToAuthority > 100 && r.ToReplica > 1000);
        Console.WriteLine($"udp: {r.HashChecks} per-tick comparisons, {r.Commands} commands, {r.ToReplica} messages to replica / {r.ToAuthority} to authority, retransmits {r.ClientStats.Retransmits}+{r.ServerStats.Retransmits}, rtt {r.ClientStats.RttMs:F0} ms, inbound loss {r.ClientStats.InboundLoss:P1}/{r.ServerStats.InboundLoss:P1}, max lag {r.MaxLagTicks} ticks, cwnd {r.ServerStats.CongestionWindowBytes}");
    }

    [Fact]
    public void OverWebSocketWithAHostileNetworkThereIsNoDrift()
    {
        var r = Play(clock =>
        {
            var inner = WebSocketTransport.Listen(new IPEndPoint(IPAddress.Loopback, 0), null, clock);
            var server = new ConditionedListener(inner, Hostile, clock);
            var client = new ConditionedConnection(
                WebSocketTransport.Connect((IPEndPoint)inner.LocalEndPoint, null, clock),
                new NetworkConditions { LatencyMs = 150, JitterMs = 30, LossRate = 0.05, DuplicateRate = 0.02, ReorderRate = 0.05, Seed = 12 },
                clock);
            INetConnection? accepted = null;
            void Pump()
            {
                server.Pump();
                client.Pump();
                accepted ??= server.TryAccept(out var a) ? a : null;
                Thread.Sleep(0);
            }

            for (var i = 0; i < 5000 && (accepted is null || client.State != ConnectionState.Connected); i++)
            {
                clock.Advance(5);
                Pump();
                Thread.Sleep(1);
            }

            Assert.NotNull(accepted);
            return (accepted!, client, Pump, new Disposer(server.Dispose));
        });
        Assert.True(r.HashChecks > 900, $"only {r.HashChecks} per-tick comparisons");
        Assert.True(r.Commands > 100);
        Console.WriteLine($"websocket: {r.HashChecks} per-tick comparisons, {r.Commands} commands, {r.ToReplica} messages to replica / {r.ToAuthority} to authority");
    }

    [Fact]
    public void TheSameRunOverTheMemoryNetworkIsIdenticalToTheAuthority()
    {
        var r = Play(clock =>
        {
            var net = new MemoryDatagramNetwork();
            var serverSocket = net.CreateSocket();
            var server = UdpTransport.Listen(new ConditionedDatagramSocket(serverSocket, Hostile, clock, ownsInner: false), null, clock);
            var client = UdpTransport.Connect(new ConditionedDatagramSocket(net.CreateSocket(), new NetworkConditions { LatencyMs = 150, JitterMs = 30, LossRate = 0.05, DuplicateRate = 0.02, ReorderRate = 0.05, Seed = 12 }, clock), serverSocket.LocalAddress, null, clock);
            INetConnection? accepted = null;
            void Pump()
            {
                server.Pump();
                client.Pump();
                accepted ??= server.TryAccept(out var a) ? a : null;
            }

            for (var i = 0; i < 5000 && (accepted is null || client.State != ConnectionState.Connected); i++)
            {
                clock.Advance(5);
                Pump();
            }

            return (accepted!, client, Pump, new Disposer(() => { }));
        });
        Assert.True(r.HashChecks > 900);
    }

    private static string Fmt(NetStats s) => $"q={s.SendQueueBytes} inflight={s.InFlightBytes} cwnd={s.CongestionWindowBytes} rtx={s.Retransmits} rtt={s.RttMs:F0} sent={s.PacketsSent} recv={s.PacketsReceived} dropped={s.PacketsDropped} msgs={s.MessagesSent}/{s.MessagesReceived}";

    private sealed class Disposer(Action dispose) : IDisposable
    {
        public void Dispose() => dispose();
    }
}

internal static class ListenerExtensions
{
    public static SocketAddress LocalEndPointAddress(this UdpListener l) => ((IPEndPoint)l.LocalEndPoint).Serialize();
}
