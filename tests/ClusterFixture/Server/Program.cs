using ClusterFixture;
using OneGame.Core;
using OneGame.Server;
using OneGame.Server.Live;

return ServerApp.Run(
    new ServerSetup
    {
        RegisterEntities = OneGame.Generated.ClusterServer.EntityTypes.Register,
        SeedWorld = (world, scope) =>
        {
            ArenaWorld.Seed(world);
            if (scope.Value == 2)
            {
                ArenaWorld.SeedWallets(world);
            }
        },
        // Every client is a player, numbered in the order it joins (5, 6, ...), so the scope's owner-only data reaches one of them.
        Started = host =>
        {
            var next = 5u;
            host.ClientJoined += c => c.Trust = c.Trust with { Who = new Audience(next++) };
        },
    },
    args);
