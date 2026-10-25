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
    public void AClientWhoseIslandIsKilledReconnectsWhenItIsBackAndAgreesWithItAgain()
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
            r.RunUntil(() => game.Remote!.Reconnects == 1 && s.Me is not null && s.Me.Owner != before, 30_000, "the client to be back as a new player", why: () => game.Remote!.Status().Replica);
            r.Run(40);
            Assert.Equal(ReplicaState.Live, game.Replica.State);
            Assert.True(Agrees(r, Rules.IslandA, game), "the client differs from its restarted island");
            Assert.Empty(game.DriftReports);

            // The player the old connection had is gone: one player on the islands, the one this connection holds.
            r.RunUntil(() => r.Players().Count() == 1, 20_000, "the old player to be removed", why: () => string.Join(",", r.Players().Select(p => p.Owner)));
            Assert.Equal(s.Me!.Owner, r.Players().Single().Owner);
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
