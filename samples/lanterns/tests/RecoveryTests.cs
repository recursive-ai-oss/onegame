using OneGame.Core;
using Lanterns.Views;
using OneGame.Host;
using OneGame.Sync;

namespace Lanterns.Tests;

/// <summary>Servers that die and come back, and players who leave.</summary>
public class RecoveryTests
{
    private static bool Agrees(Realms r, uint island, GameInstance game)
    {
        var replica = game.Replica;
        var at = r.Host(island).HashesAt(replica.World.Tick)[0];
        return at.Hash is not null && at.Views.Contains(replica.ViewHash.ToString("x16", System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void AClientWhoseIslandIsKilledReconnectsAsTheSamePlayerWhenItIsBackAndAgreesWithItAgain()
    {
        var data = Path.Combine(Path.GetTempPath(), "lanterns-recovery-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var r = new Realms(data);
            var game = r.Client("p1", Rules.IslandA);
            var s = Session.For(game);
            r.RunUntil(() => s.Me is not null, 20_000, "the player to appear");
            r.Run(60);
            var before = s.Me!.Owner;

            r.Kill(Rules.IslandA);
            r.RunUntil(() => game.Remote!.IsReconnecting, 20_000, "the client to notice");
            Assert.False(game.Remote!.IsClosed);

            r.Start(Rules.IslandA); // from its last save
            r.RunUntil(() => game.Remote!.Reconnects == 1 && s.Me is not null && game.Replica.State == ReplicaState.Live, 30_000, "the client to be back", why: () => game.Remote!.Status().Replica);
            r.Run(40);
            Assert.Equal(ReplicaState.Live, game.Replica.State);
            Assert.True(Agrees(r, Rules.IslandA, game), "the client differs from its restarted island");
            Assert.Empty(game.DriftReports);

            // The key is the player: the client is the same player as before the restart, and is not removed as a leaver afterwards
            // (the island marked everyone it restored as a leaver until they show up again; running past that window proves it was cleared).
            Assert.Equal(before, s.Me!.Owner);
            r.Run(400);
            Assert.Equal(before, Assert.Single(r.Players()).Owner);
        }
        finally
        {
            if (Directory.Exists(data))
            {
                Directory.Delete(data, true);
            }
        }
    }

    [Fact]
    public void APlayerWhoLeavesIsRemovedFromTheIslandAndTheirLanternsAreCounted()
    {
        using var r = new Realms();
        var game = r.Client("p1", Rules.IslandA);
        var s = Session.For(game);
        r.RunUntil(() => s.Me is not null, 20_000, "the player to appear");
        r.Players().Single().Change(0, 2);
        r.Run(10);

        r.Leave(game);
        r.RunUntil(() => !r.Players().Any(), 20_000, "the player to be removed");
        Assert.Equal(2, r.Departed());
        Assert.DoesNotContain(r.Host(Rules.IslandA).Node.Connections, c => c.Name.StartsWith("udp", StringComparison.Ordinal));
    }

    [Fact]
    public void LeaveCommandsArrivingInOneTickCountThePlayersLanternsOnce()
    {
        // The other island asks again every few ticks until it sees the player gone, so on a slow machine several requests can queue up
        // and run in the same tick. The removal only takes effect at the end of the tick.
        var registry = new EntityRegistry();
        Worlds.Register(registry);
        var world = new World(7, registry, WorldRole.Authority);
        Worlds.Build(world, new ScopeId(Rules.IslandA));
        Worlds.Seed(world, new ScopeId(Rules.IslandA));
        var island = Island.Of(world)!;
        island.Join(1, "Ann");
        world.Entities.OfType<Player>().Single().Change(0, 4);

        Assert.Equal(CommandStatus.Queued, island.SendFromText("Leave", ["1"]));
        Assert.Equal(CommandStatus.Queued, island.SendFromText("Leave", ["1"]));
        Assert.Equal(CommandStatus.Queued, island.SendFromText("Leave", ["1"]));
        world.Step();

        Assert.Empty(world.Entities.OfType<Player>());
        Assert.Equal(4, island.Departed);
    }

    [Fact]
    public void APlayerWhoLeavesWhileOnTheOtherIslandIsRemovedThereToo()
    {
        using var r = new Realms();
        var game = r.Client("p1", Rules.IslandA);
        var s = Session.For(game);
        r.Run(60);
        r.RunUntil(() => s.Me is not null && s.Scope == Rules.IslandB, 40_000, "the player to cross to island B", _ =>
        {
            if (s.Me is { } me)
            {
                s.Steer(me.X < Rules.Width ? 1 : 0, Math.Sign(96 - me.Y) * (Math.Abs(96 - me.Y) > Rules.Speed ? 1 : 0), every: true);
            }
        });
        s.Steer(0, 0);
        r.Run(60);
        Assert.Single(r.Host(Rules.IslandB).Scopes[0].World.Entities.OfType<Player>());

        r.Leave(game);
        r.RunUntil(() => !r.Players().Any(), 20_000, "the player to be removed from island B", why: () => string.Join(",", r.Players().Select(p => p.Owner + "@" + p.Island)));
    }
}
