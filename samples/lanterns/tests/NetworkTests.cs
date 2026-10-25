using Lanterns.Views;
using OneGame.Sync;

namespace Lanterns.Tests;

public class NetworkTests
{
    private static void Walk(Session s, int x, int y)
    {
        if (s.Me is { } me)
        {
            s.Steer(Math.Sign(x - me.X) * (Math.Abs(x - me.X) > Rules.Speed ? 1 : 0), Math.Sign(y - me.Y) * (Math.Abs(y - me.Y) > Rules.Speed ? 1 : 0), every: true);
        }
    }

    private static void NoDrift(Realms r, params OneGame.Host.GameInstance[] clients)
    {
        foreach (var scope in new[] { Rules.IslandA, Rules.IslandB, Rules.Master })
        {
            Assert.Empty(r.Host(scope).Node.DriftReports);
            Assert.Empty(r.Host(scope).Node.ProtocolErrors);
        }

        foreach (var c in clients)
        {
            Assert.Empty(c.DriftReports);
        }
    }

    [Fact]
    public void AClientJoinsAndSeesItselfOnItsIsland()
    {
        using var r = new Realms();
        var game = r.Client("p1", Rules.IslandA);
        var s = Session.For(game);
        r.Run(120);
        Assert.NotNull(s.Me);
        Assert.Equal(Rules.IslandA, s.Scope);
        Assert.Single(r.Players());
        NoDrift(r, game);
    }

    [Fact]
    public void CrossingTheBridgeHandsThePlayerOverAndEveryMoveRunsOnce()
    {
        using var r = new Realms();
        var game = r.Client("p1", Rules.IslandA);
        var s = Session.For(game);
        r.Run(60);
        uint? island = null;
        var crossings = 0;
        r.Run(1500, _ =>
        {
            var me = s.Me;
            if (me is null)
            {
                return;
            }

            if (island is { } was && was != s.Scope)
            {
                crossings++;
            }

            island = s.Scope;
            var east = crossings % 2 == 0 ? s.Scope == Rules.IslandA : s.Scope == Rules.IslandB;
            Walk(s, east == (s.Scope == Rules.IslandA) ? Rules.Width + 8 : -8, 96);
        });
        Assert.True(crossings >= 2, $"crossed {crossings} times");
        s.Steer(0, 0);
        r.Run(120);

        // The player's own count of applied steering commands travelled with it: none lost, none run twice.
        var truth = r.Players().Single();
        Assert.Equal((uint)s.SteersSent, truth.Moves);
        NoDrift(r, game);
    }
}
