using OneGame.Host;
using OneGame.Net;
using OneGame.Server.Plugins;

namespace OneGame.Server.Tests;

/// <summary>The server host listening on a real socket: clients connect, are trusted as configured, and stay in sync across plugin reloads.</summary>
public class NetworkTests : IDisposable
{
    private readonly List<string> _folders = [];

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        foreach (var f in _folders)
        {
            try
            {
                Directory.Delete(f, true);
            }
            catch (IOException)
            {
            }
        }
    }

    private static ServerConfig Config(string extra = "") =>
        ServerConfig.Parse("name: net\nhashHistory: 512\nscopes: [{ id: 1, seed: 7 }]\nlisten:\n  - { transport: udp, port: 0, address: 127.0.0.1 }\n  - { transport: ws, port: 0, address: 127.0.0.1 }\n" + extra);

    private static (RemoteSession Session, World World) Connect(ServerHost host, string transport, string name = "c")
    {
        var port = host.Endpoints.First(e => e.Transport == transport).Port;
        var world = host.CreateReplicaWorld(new ScopeId(1));
        return (new RemoteSession(new ConnectOptions { Address = $"{transport}://127.0.0.1:{port}", Name = name }, world), world);
    }

    // One round of the loop a deployed server and client run: both sides pump the network, the server ticks.
    private static void Round(ServerHost host, params RemoteSession[] clients)
    {
        host.PumpNetwork();
        host.StepOnce();
        for (var i = 0; i < 3; i++)
        {
            foreach (var c in clients)
            {
                c.Pump();
            }

            host.PumpNetwork();
            Thread.Sleep(1);
        }
    }

    private static bool Matches(ServerHost host, RemoteSession c)
    {
        var at = host.HashesAt(c.Replica.World.Tick)[0];
        return at.Hash is not null && at.Views.Contains(c.Replica.ViewHash.ToString("x16"));
    }

    [Fact]
    public void AConfiguredListenerAcceptsUdpAndWebSocketClientsWithTheConfiguredTrust()
    {
        var host = new ServerHost(Config("trust: { read: [1], command: [] }\n"));
        host.Start();
        Assert.Equal(2, host.Endpoints.Count);
        Assert.All(host.Endpoints, e => Assert.True(e.Port > 0));
        var (udp, _) = Connect(host, "udp", "u");
        var (ws, _) = Connect(host, "ws", "w");
        for (var i = 0; i < 100; i++)
        {
            Round(host, udp, ws);
        }

        Assert.Equal(ReplicaState.Live, udp.Replica.State);
        Assert.Equal(ReplicaState.Live, ws.Replica.State);
        Assert.Equal(2, host.Clients().Length);
        Assert.Equal("udp,ws", string.Join(",", host.Clients().Select(c => c.Transport).Order()));
        Assert.True(Matches(host, udp));
        Assert.True(Matches(host, ws));
        Assert.All(host.Node.Connections, c => Assert.False(c.Trust.Command.Contains(new ScopeId(1))));
        host.Stop();
    }

    [Fact]
    public void TheAuthenticationHookCanRefuseAClient()
    {
        var host = new ServerHost(Config()) { Authenticate = request => request.Transport == "ws" ? null : Trust.Player(new ScopeId(1)) };
        host.Start();
        var (ws, _) = Connect(host, "ws", "refused");
        var (udp, _) = Connect(host, "udp", "allowed");
        for (var i = 0; i < 100; i++)
        {
            Round(host, ws, udp);
        }

        Assert.Equal(ReplicaState.Live, udp.Replica.State);
        Assert.NotEqual(ReplicaState.Live, ws.Replica.State);
        Assert.Single(host.Clients());
        host.Stop();
    }

    [Fact]
    public void ADisconnectedClientIsUnsubscribedAndDropped()
    {
        var host = new ServerHost(Config());
        host.Start();
        var (udp, _) = Connect(host, "udp");
        for (var i = 0; i < 60; i++)
        {
            Round(host, udp);
        }

        Assert.Single(host.Scopes[0].Authority.Subscribers);
        udp.Connection.Disconnect(DisconnectReason.Normal);
        for (var i = 0; i < 40; i++)
        {
            Round(host);
        }

        Assert.Empty(host.Clients());
        Assert.Empty(host.Scopes[0].Authority.Subscribers);
        host.Stop();
    }

    [Fact]
    public void APluginHotReloadedWhileARemoteClientIsConnectedKeepsItDriftFree()
    {
        var dir = PluginBuilds.NewFolder();
        _folders.Add(dir);
        PluginBuilds.Build(dir, 1);
        var host = new ServerHost(Config(), loader: new PluginLoader());
        host.Start();
        host.LoadPlugin(new PluginConfig("counter", dir, [], []));
        var (udp, world) = Connect(host, "udp");
        var (ws, _) = Connect(host, "ws", "w");
        host.DeliverLocalMessages = () => host.PumpNetwork();
        for (var i = 0; i < 40; i++)
        {
            Round(host, udp, ws);
        }

        Assert.Equal(ReplicaState.Live, udp.Replica.State);
        Assert.Single(world.Entities);

        // Edit, rebuild, reload: the new build's logic runs on the same entities and the client never notices a gap.
        PluginBuilds.Build(dir, 2);
        var result = host.ReloadPlugin("counter");
        Assert.NotEqual(result.OldBuild, result.NewBuild);
        for (var i = 0; i < 60; i++)
        {
            Round(host, udp, ws);
            if (udp.Replica.World.Tick > 0 && i % 10 == 0)
            {
                Assert.True(Matches(host, udp), $"client differs from the server at tick {udp.Replica.World.Tick} after the reload");
            }
        }

        Assert.Equal(0, udp.Node.DriftCount);
        Assert.Equal(0, ws.Node.DriftCount);
        Assert.True(Matches(host, udp));
        Assert.True(Matches(host, ws));
        Assert.Equal(host.Scopes[0].World.Tick, udp.Replica.World.Tick);
        host.Stop();
    }

    [Theory]
    [InlineData("udp")]
    [InlineData("ws")]
    public void AClientReconnectsToARestartedServerResubscribesAndAgreesAgain(string transport)
    {
        var port = FreePort(transport);
        ServerHost Start() =>
            new(ServerConfig.Parse($"name: net\nhashHistory: 512\nscopes: [{{ id: 1, seed: 7 }}]\nlisten:\n  - {{ transport: {transport}, port: {port}, address: 127.0.0.1 }}\n"));

        var first = Start();
        first.Start();
        var (client, _) = Connect(first, transport, "back");
        for (var i = 0; i < 60; i++)
        {
            Round(first, client);
        }

        Assert.Equal(ReplicaState.Live, client.Replica.State);
        var tickBefore = client.Replica.World.Tick;
        first.Stop();

        // The client sees the connection go and keeps trying; the session is not closed for good.
        var clock = System.Diagnostics.Stopwatch.StartNew();
        while (!client.IsReconnecting && clock.ElapsedMilliseconds < 15_000)
        {
            client.Pump();
            Thread.Sleep(2);
        }

        Assert.True(client.IsReconnecting, "the client never noticed the server was gone");
        Assert.False(client.IsClosed);

        var second = Start();
        second.Start();
        clock.Restart();
        while ((client.Reconnects == 0 || client.Replica.State != ReplicaState.Live || !Matches(second, client)) && clock.ElapsedMilliseconds < 20_000)
        {
            Round(second, client);
        }

        Assert.Equal(1, client.Reconnects);
        Assert.False(client.IsReconnecting);
        Assert.Equal(ReplicaState.Live, client.Replica.State);
        Assert.True(Matches(second, client), "the client differs from the restarted server");
        Assert.Equal(0, client.Node.DriftCount);
        Assert.Single(second.Clients());
        Assert.Single(second.Node.Connections);
        Assert.True(client.Replica.World.Tick <= second.Scopes[0].World.Tick, $"before {tickBefore}");
        Assert.Equal(1, client.Status().Reconnects);
        second.Stop();
    }

    [Fact]
    public void AClientTheServerRefusedDoesNotKeepTryingAndAClosedSessionStaysClosed()
    {
        var host = new ServerHost(Config()) { Authenticate = _ => null };
        host.Start();
        var (udp, _) = Connect(host, "udp", "refused");
        var clock = System.Diagnostics.Stopwatch.StartNew();
        while (!udp.IsClosed && clock.ElapsedMilliseconds < 5000)
        {
            Round(host, udp);
        }

        Assert.True(udp.IsClosed);
        Assert.Equal(0, udp.Reconnects);
        host.Stop();
    }

    [Fact]
    public void DisconnectedClientsLeaveNoStaleConnectionsOnTheServerNode()
    {
        var host = new ServerHost(Config());
        host.Start();
        for (var n = 0; n < 5; n++)
        {
            var (udp, _) = Connect(host, "udp", "c" + n);
            for (var i = 0; i < 20; i++)
            {
                Round(host, udp);
            }

            Assert.Single(host.Node.Connections);
            udp.Connection.Disconnect(DisconnectReason.Normal, flush: true);
            for (var i = 0; i < 40 && host.Node.Connections.Count > 0; i++)
            {
                Round(host);
            }

            Assert.Empty(host.Node.Connections);
        }

        Assert.Empty(host.Scopes[0].Authority.Subscribers);
        host.Stop();
    }

    private static int FreePort(string transport)
    {
        if (transport == "udp")
        {
            using var udp = new System.Net.Sockets.UdpClient(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 0));
            return ((System.Net.IPEndPoint)udp.Client.LocalEndPoint!).Port;
        }

        var tcp = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        tcp.Start();
        var port = ((System.Net.IPEndPoint)tcp.LocalEndpoint).Port;
        tcp.Stop();
        return port;
    }

    [Fact]
    public void ListenAndTrustAreCheckedWhenTheConfigurationIsRead()
    {
        var ex = Assert.Throws<ServerConfigException>(() => ServerConfig.Parse("listen:\n  - { transport: tcp, port: 1 }\n"));
        Assert.Contains("udp or ws", ex.Message);
        Assert.Throws<ServerConfigException>(() => ServerConfig.Parse("trust: { read: [9] }\n"));
        Assert.Throws<ServerConfigException>(() => ServerConfig.Parse("listen:\n  - { transport: udp, port: 70000 }\n"));
    }
}
