using NetworkedCounterExample;
using OneGame.Server;
using OneGame.Server.Live;

// The whole server: listen, tick, serve live control. Options come from the cluster file or the command line.
return ServerApp.Run(
    new ServerSetup
    {
        RegisterEntities = OneGame.Generated.NetworkedCounterExampleServer.EntityTypes.Register,
        SeedWorld = (world, _) => world.Add(new Counter()),
    },
    args);
