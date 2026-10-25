using System.Net;
using OneGame.Sync.Tests;

namespace OneGame.Net.Tests;

/// <summary>Nodes joined by the public channel API (Node.Accept and Node.ConnectVia) over a real UDP socket on the loopback interface.</summary>
public class RemoteNodeTests
{
    [Fact]
    public void ANodeFollowsARemoteAuthorityOverUdpWithNoDriftAndAnswersFlowBack()
    {
        var clock = new ManualNetClock();
        var options = new ClusterOptions { DriftCheckInterval = 1 };
        var authorityNode = new Node("host", options);
        var playerNode = new Node("player", options);
        var scope = authorityNode.AddScope(new ScopeId(1), Scenario.NewAuthority(8, 30));

        var server = UdpTransport.Listen(new SystemDatagramSocket(new IPEndPoint(IPAddress.Loopback, 0)), null, clock);
        var client = UdpTransport.Connect(new SystemDatagramSocket(new IPEndPoint(IPAddress.Loopback, 0)), server.LocalEndPointAddress(), null, clock);
        using var _ = server;
        INetConnection? accepted = null;
        for (var i = 0; i < 5000 && (accepted is null || client.State != ConnectionState.Connected); i++)
        {
            clock.Advance(5);
            server.Pump();
            client.Pump();
            accepted ??= server.TryAccept(out var a) ? a : null;
            Thread.Sleep(0);
        }

        Assert.NotNull(accepted);
        var connection = authorityNode.Accept(accepted!, Trust.Player(scope.Id), "udp-player");
        var link = playerNode.ConnectVia(client);
        Assert.True(link.IsRemote);
        var view = playerNode.Subscribe(link, scope.Id, Scenario.NewWorld(WorldRole.Replica));

        var hashAt = new List<ulong> { scope.World.ComputeHash() };
        var checks = 0;
        for (var t = 0; t < 600; t++)
        {
            clock.Advance(16);
            Scenario.Input(view.World, view.World.Tick);
            scope.StepAndPublish();
            hashAt.Add(scope.World.ComputeHash());
            for (var round = 0; round < 3; round++)
            {
                server.Pump();
                client.Pump();
                authorityNode.Pump();
                playerNode.Pump();
                Thread.Sleep(0);
            }

            var tick = (int)view.World.Tick;
            if (view.State == ReplicaState.Live && tick > 0 && tick < hashAt.Count)
            {
                Assert.True(hashAt[tick] == view.World.ComputeHash(), $"replica differs from the authority at tick {tick}");
                checks++;
            }
        }

        for (var i = 0; i < 400 && view.World.Tick < scope.World.Tick; i++)
        {
            clock.Advance(16);
            server.Pump();
            client.Pump();
            authorityNode.Pump();
            playerNode.Pump();
        }

        Assert.True(checks > 400, $"only {checks} per-tick comparisons");
        Assert.Equal(scope.World.Tick, view.World.Tick);
        Assert.Equal(scope.Hash, view.Hash);
        Assert.Equal(scope.ViewHashFor(connection), view.ViewHash);
        Assert.Empty(playerNode.DriftReports);
        Assert.True(scope.CommandsRun > 50);
        Assert.NotEmpty(view.Results);
        Assert.Contains(view.Results, r => r.Verdict == CommandVerdict.Executed);
    }
}
