using ClusterFixture;
using OneGame.Server;
using OneGame.Server.Live;

return ServerApp.Run(
    new ServerSetup
    {
        RegisterEntities = OneGame.Generated.ClusterServer.EntityTypes.Register,
        SeedWorld = (world, _) => ArenaWorld.Seed(world),
    },
    args);
