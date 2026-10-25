using OneGame.Core;
using OneGame.Server;
using OneGame.Server.Live;
using OneGame.Sync;
using OneGame.Testing;

namespace OneGame.AotSmoke;

/// <summary>A plugin-less headless server under NativeAOT: configuration, scopes, a loopback client, commands, drift checks and the control surface.</summary>
internal static class ServerSmoke
{
    public static string? Run()
    {
        var setup = new ServerSetup
        {
            RegisterEntities = OneGame.Generated.OneGame_AotSmoke.EntityTypes.Register,
            BuildWorld = static (world, _) => world.AddTable(new SparkTable()),
        };
        var config = ServerConfig.Parse("name: smoke\ntickRate: 60\nscopes:\n  - { id: 1, seed: 11 }\n");
        using var server = new TestServer(config, setup);
        server.Host.Scopes[0].World.Add(new Purse { Owner = "server" });
        var client = server.AddClient("c", new ScopeId(1));
        server.Step(3);
        client.Send(new EntityId(1), "Add", "4", "1.5");
        server.Step(5);
        if (server.CheckSync().Count != 0 || server.Host.Scopes[0].World.Tick != 8)
        {
            return "server and client disagree";
        }

        var control = new ServerControl(server.Host);
        var info = control.Call("server_info");
        var state = control.Call("state_query", "{\"entity\":1}");
        if (!info.Contains("\"name\":\"smoke\"", StringComparison.Ordinal) || !state.Contains("server", StringComparison.Ordinal))
        {
            return "control surface answered " + info;
        }

        try
        {
            server.Host.LoadPlugin(new PluginConfig("none", "/nowhere", [], []));
            return "a host without a loader loaded a plugin";
        }
        catch (PluginException ex) when (ex.Code == "unsupported")
        {
            return Networked();
        }
    }

    // The server on a real socket with a remote client, the cluster spec reader and the network conditioner settings.
    private static string? Networked()
    {
        var setup = new ServerSetup
        {
            RegisterEntities = OneGame.Generated.OneGame_AotSmoke.EntityTypes.Register,
            BuildWorld = static (world, _) => world.AddTable(new SparkTable()),
            SeedWorld = static (world, _) =>
            {
                world.Add(new Purse { Owner = "server" });

                // Enough state that the first snapshot is over the transport's message limit and travels in pieces.
                for (var i = 0; i < 20_000; i++)
                {
                    world.Add(new Purse { Owner = "filler-purse-with-a-long-owner-name-to-take-space-" + i.ToString(System.Globalization.CultureInfo.InvariantCulture) });
                }
            },
        };
        var config = ServerConfig.Parse("name: net\nhashHistory: 64\nscopes: [{ id: 1, seed: 3 }]\nlisten:\n  - { transport: udp, port: 0, address: 127.0.0.1 }\n");
        var host = new ServerHost(config, setup);
        host.Start();
        var port = host.Endpoints[0].Port;
        var session = new OneGame.Host.RemoteSession(
            new OneGame.Host.ConnectOptions { Address = $"udp://127.0.0.1:{port}", Name = "smoke", Conditions = OneGame.Host.RemoteSession.ParseConditions("latency=0,loss=0") },
            host.CreateReplicaWorld(new ScopeId(1)));
        for (var i = 0; i < 300; i++)
        {
            host.PumpNetwork();
            host.StepOnce();
            session.Pump();
            host.PumpNetwork();
            Thread.Sleep(1);
        }

        var at = host.HashesAt(session.Replica.World.Tick)[0];
        var matches = session.Replica.State == ReplicaState.Live && at.Views.Contains(session.Replica.ViewHash.ToString("x16", System.Globalization.CultureInfo.InvariantCulture));
        var clients = new ServerControl(host).Call("clients");
        host.Stop();

        // The server goes away and comes back on the same port: the client notices, connects again and resubscribes.
        var again = new ServerHost(ServerConfig.Parse($"name: net\nhashHistory: 64\nscopes: [{{ id: 1, seed: 3 }}]\nlisten:\n  - {{ transport: udp, port: {port}, address: 127.0.0.1 }}\n"), setup);
        var clock = System.Diagnostics.Stopwatch.StartNew();
        while (!session.IsReconnecting && clock.ElapsedMilliseconds < 10_000)
        {
            session.Pump();
            Thread.Sleep(2);
        }

        if (!session.IsReconnecting)
        {
            return "the client did not notice the server stopping";
        }

        again.Start();
        clock.Restart();
        while ((session.Reconnects == 0 || session.Replica.State != ReplicaState.Live) && clock.ElapsedMilliseconds < 20_000)
        {
            again.PumpNetwork();
            again.StepOnce();
            session.Pump();
            again.PumpNetwork();
            Thread.Sleep(1);
        }

        var back = session.Reconnects == 1 && session.Replica.State == ReplicaState.Live && again.Node.Connections.Count == 1;
        again.Stop();
        if (!back)
        {
            return "the client did not reconnect";
        }

        var spec = ClusterSpec.Parse("nodes:\n  - { name: a, role: server, exe: /bin/true }\n  - { name: b, role: client, exe: /bin/true, connect: a }\n");
        return !matches ? "the remote client does not match the server" : spec.Nodes.Count != 2 || !clients.Contains("udp", StringComparison.Ordinal) ? "clients/spec: " + clients : null;
    }
}
