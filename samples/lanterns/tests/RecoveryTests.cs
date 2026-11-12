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
    public void AClientAdmittedWhileItsPlayersRemovalIsQueuedStillEndsUpWithAPlayer()
    {
        // The restarted island lists the players it restored as leaving and asks for their removal on a pass every few ticks. A removal queued on
        // one pass lands on the next tick, and a client who connects in between is told it is back (the entity is still there) and then loses the
        // entity to that removal. This forces the order: pass, then admission, then the tick that runs the removal.
        var data = Path.Combine(Path.GetTempPath(), "lanterns-queued-" + Guid.NewGuid().ToString("N"));
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
            r.Start(Rules.IslandA);

            // Tick the island alone until a pass has queued the removal; the client is not run, so it cannot connect yet.
            var island = r.A;
            do
            {
                island.PumpNetwork();
                island.StepOnce();
            }
            while (island.Tick % 10 != 0);

            Assert.Equal(before, Assert.Single(island.Scopes[0].World.Entities.OfType<Player>()).Owner); // still there, removal queued

            // Let the client connect without a tick in between.
            var clock = System.Diagnostics.Stopwatch.StartNew();
            while (island.Clients().Length == 0)
            {
                Assert.True(clock.ElapsedMilliseconds < 30_000, "the client never connected");
                game.Step();
                island.PumpNetwork();
                Thread.Sleep(1);
            }

            r.RunUntil(() => s.Me is not null && game.Replica.State == ReplicaState.Live, 30_000, "the client to be back");
            r.Run(120);
            Assert.Equal(before, Assert.Single(r.Players()).Owner);
            Assert.Equal(before, s.Me!.Owner);
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
    public void ARejoinedPlayerWhoseOldEntityArrivesLaterIsKeptOnce()
    {
        // An entity in transit from the other island is in neither world, so a rejoin can make a second one before it lands. Forced here by
        // installing the late arrival by hand next to the player the island made, carrying lanterns.
        using var r = new Realms();
        var game = r.Client("p1", Rules.IslandA);
        var s = Session.For(game);
        r.RunUntil(() => s.Me is not null, 20_000, "the player to appear");
        var owner = s.Me!.Owner;
        r.Run(30);

        var world = r.A.Scopes[0].World;
        var arrived = new Player { Owner = owner, Name = "Player", Island = Rules.IslandA, X = 10, Y = 10 };
        world.Add(arrived);
        arrived.Change(0, 3);
        var held = r.Held();

        r.Run(60);
        var mine = r.Players().Where(p => p.Owner == owner).ToList();
        Assert.Single(mine);
        Assert.Equal(held, r.Held()); // lanterns conserved: the removed one counted as departed
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
