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
            return null;
        }
    }
}
