using System.Net;
using OneGame.Sync.Tests;

namespace OneGame.Net.Tests;

/// <summary>Client-side prediction over a real UDP socket pair on the loopback interface, with the network conditioner between them.</summary>
public class PredictionOverUdpTests
{
    private const int Ticks = 1200;
    private const int TickMs = 16;

    private static NetworkConditions Hostile(int seed) => new()
    {
        LatencyMs = 120,
        JitterMs = 20,
        LossRate = 0.03,
        Seed = seed,
    };

    [Fact]
    public void PredictedMovementOverAHostileLinkShowsAtOnceAndConfirmsWithNoDrift()
    {
        var clock = new ManualNetClock();
        var options = new ClusterOptions { DriftCheckInterval = 1 };
        var authorityNode = new Node("host", options);
        var playerNode = new Node("player", options);
        var scope = authorityNode.AddScope(new ScopeId(1), Track.NewAuthority());

        var server = UdpTransport.Listen(new ConditionedDatagramSocket(new SystemDatagramSocket(new IPEndPoint(IPAddress.Loopback, 0)), Hostile(21), clock), null, clock);
        var client = UdpTransport.Connect(
            new ConditionedDatagramSocket(new SystemDatagramSocket(new IPEndPoint(IPAddress.Loopback, 0)), Hostile(22), clock), server.LocalEndPointAddress(), null, clock);
        using var listener = server;
        INetConnection? accepted = null;
        for (var i = 0; i < 20000 && (accepted is null || client.State != ConnectionState.Connected); i++)
        {
            clock.Advance(5);
            server.Pump();
            client.Pump();
            accepted ??= server.TryAccept(out var a) ? a : null;
            Thread.Sleep(0);
        }

        Assert.NotNull(accepted);
        var connection = authorityNode.Accept(accepted!, Trust.Owner(scope.Id).As(1), "udp-player");
        var link = playerNode.ConnectVia(client);
        var view = playerNode.Subscribe(link, scope.Id, Track.NewWorld(WorldRole.Replica));
        var prediction = view.EnablePrediction(Track.NewWorld(WorldRole.Predicted), new PredictionOptions { LeadTicks = 8 });

        void PumpAll()
        {
            for (var round = 0; round < 3; round++)
            {
                server.Pump();
                client.Pump();
                authorityNode.Pump();
                playerNode.Pump();
                Thread.Sleep(0);
            }
        }

        var hashAt = new List<ulong> { scope.ViewHashFor(connection) };
        var checks = 0;
        var shown = 0;
        var maxUnconfirmed = 0;
        var lagSum = 0L;
        var lagTicks = 0;
        for (var t = 0; t < Ticks; t++)
        {
            clock.Advance(TickMs);
            PumpAll();
            var tick = (int)view.World.Tick;
            if (view.State == ReplicaState.Live && tick > 0 && tick < hashAt.Count)
            {
                Assert.True(hashAt[tick] == view.World.ComputeViewHash(), $"the confirmed replica differs from the authority at tick {tick} (loop {t})");
                checks++;
                lagSum += scope.World.Tick - tick;
                lagTicks++;
            }

            if (view.State == ReplicaState.Live && view.World.Find(new EntityId(1)) is not null)
            {
                var runner = (Runner)prediction.World.Find(new EntityId(1))!;
                var dx = (t / 25 % 2 == 0) ? 1 : -1;
                var dy = (t % 7) - 3;
                runner.Send.Steer(dx, dy);
                Assert.True(((Runner)prediction.World.Find(new EntityId(1))!).Velocity == new FVec2(dx, dy), $"the predicted state does not show the steer sent in loop {t}");
                shown++;
                if (t % 50 == 0)
                {
                    runner.Send.Dash(2);
                }

                maxUnconfirmed = Math.Max(maxUnconfirmed, prediction.Unconfirmed);
            }

            prediction.Advance();
            scope.StepAndPublish();
            hashAt.Add(scope.ViewHashFor(connection));
        }

        // Stop sending. The link drains, every command is answered, and the prediction is the confirmed state carried forward.
        for (var i = 0; i < 600 && (view.World.Tick < scope.World.Tick || prediction.Unconfirmed > 0); i++)
        {
            clock.Advance(TickMs);
            PumpAll();
            prediction.Advance();
            scope.StepAndPublish();
            hashAt.Add(scope.ViewHashFor(connection));
        }

        for (var i = 0; i < 400 && view.World.Tick < scope.World.Tick; i++)
        {
            clock.Advance(TickMs);
            PumpAll();
            hashAt.Add(scope.ViewHashFor(connection));
        }

        Assert.True(checks > 900, $"only {checks} per-tick comparisons");
        Assert.True(shown > 1000);
        Assert.Empty(playerNode.DriftReports);
        Assert.Empty(authorityNode.DriftReports);
        Assert.Equal(0, view.Resyncs);
        Assert.Equal(ReplicaState.Live, view.State);
        Assert.Equal(scope.ViewHashFor(connection), view.World.ComputeViewHash());
        Assert.Equal(0, prediction.Unconfirmed);
        Assert.True(prediction.CommandsPredicted >= Ticks);

        var p = (Runner)prediction.World.Find(new EntityId(1))!;
        var c = (Runner)view.World.Find(new EntityId(1))!;
        var ahead = prediction.Tick - view.World.Tick;
        Assert.True(ahead >= 0);
        Assert.True(p.Position == c.Position + (c.Velocity * Fixed.FromInt(ahead)), "with nothing in flight, the prediction is the confirmed state moving on");

        Console.WriteLine(
            $"UDP 120 ms +-20 ms, 3% loss each way: {Ticks} ticks, {prediction.CommandsPredicted} predicted commands (all shown on the tick sent), "
            + $"{checks} confirmed ticks matched the authority's view hash, 0 drift reports, {view.Resyncs} resyncs; "
            + $"{prediction.Reconciliations} reconciliations, {prediction.Rollbacks} rollbacks, {prediction.ResimulatedTicks} resimulated ticks, "
            + $"final lead {prediction.LeadTicks} ticks, up to {maxUnconfirmed} commands in flight, confirmed state {(double)lagSum / Math.Max(1, lagTicks):F1} ticks behind the authority on average; "
            + $"{view.Results.Count(r => r.Verdict == CommandVerdict.Rejected)} rejected of the last {view.Results.Count} answers");
    }
}
