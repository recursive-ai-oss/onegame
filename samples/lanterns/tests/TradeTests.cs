using Lanterns.Views;
using OneGame.Host;
using OneGame.Sync;

namespace Lanterns.Tests;

public class TradeTests
{
    private sealed class Pair : IDisposable
    {
        private readonly uint _id1;
        private readonly uint _id2;

        public Pair(string? data = null)
        {
            Realms = new Realms(data);
            G1 = Realms.Client("p1", Rules.IslandA);
            G2 = Realms.Client("p2", Rules.IslandB);
            S1 = Session.For(G1);
            S2 = Session.For(G2);
            Realms.RunUntil(() => S1.MyAccount is not null && S2.MyAccount is not null && S1.Board().Count == 2, 20_000, "both accounts");
            _id1 = Realms.Players().First(p => p.Island == Rules.IslandA).Owner;
            _id2 = Realms.Players().First(p => p.Island == Rules.IslandB).Owner;
            P1.Change(0, 2);
            P2.Change(1, 2);
            Realms.RunUntil(() => S1.Board().All(b => b.Total == 2), 20_000, "totals reach the master");
            Total = Realms.Held();
        }

        public Realms Realms { get; }

        public GameInstance G1 { get; }

        public GameInstance G2 { get; }

        public Session S1 { get; }

        public Session S2 { get; }

        public Player P1 => Realms.Players().First(p => p.Owner == _id1);

        public Player P2 => Realms.Players().First(p => p.Owner == _id2);

        public long Total { get; }

        public void Offer()
        {
            S1.SwapTarget = P2.Owner;
            S1.SwapGive = 0;
            S1.SwapWant = 1;
            S1.SendSwap();
            Realms.RunUntil(() => S2.MyAccount?.Status == SwapStatus.Incoming, 20_000, "the offer to arrive");
            S2.Accept();
        }

        public void Dispose() => Realms.Dispose();
    }

    [Fact]
    public void ASwapAcrossIslandsMovesBothLanternsAndLosesNone()
    {
        using var t = new Pair();
        t.Offer();
        t.Realms.RunUntil(() => t.S1.MyAccount?.Status == SwapStatus.Done && t.S2.MyAccount?.Status == SwapStatus.Done, 30_000, "the swap to settle", _ => Assert.True(t.Realms.Held() == t.Total, "lanterns were created or lost"));
        Assert.Equal((1, 1, 0), (t.P1.Red, t.P1.Blue, t.P1.Gold));
        Assert.Equal((1, 1, 0), (t.P2.Red, t.P2.Blue, t.P2.Gold));
        Assert.Equal(0, t.Realms.InFlight());
        Assert.Equal(0, t.P1.Pending);
    }

    [Fact]
    public void ASwapForAColourTheOtherDoesNotHaveIsRefundedWithNothingLost()
    {
        using var t = new Pair();
        t.S1.SwapTarget = t.P2.Owner;
        t.S1.SwapGive = 0;
        t.S1.SwapWant = 2;
        t.S1.SendSwap();
        t.Realms.RunUntil(() => t.S2.MyAccount?.Status == SwapStatus.Incoming, 20_000, "the offer");
        t.S2.Accept();
        t.Realms.RunUntil(() => t.S1.MyAccount?.Status == SwapStatus.Failed, 30_000, "the failure", _ => Assert.True(t.Realms.Held() == t.Total));
        t.Realms.Run(60);
        Assert.Equal((2, 0, 0), (t.P1.Red, t.P1.Blue, t.P1.Gold));
        Assert.Equal((0, 2, 0), (t.P2.Red, t.P2.Blue, t.P2.Gold));
        Assert.Equal(0, t.Realms.InFlight());
    }

    [Theory]
    [InlineData(Rules.IslandB)]
    [InlineData(Rules.Master)]
    public void KillingAServerMidSwapAndStartingItFromItsSaveKeepsEveryLantern(uint victim)
    {
        var dir = Path.Combine(Path.GetTempPath(), "lanterns-test-" + Guid.NewGuid().ToString("N")[..8]);
        try
        {
            using var t = new Pair(dir);
            t.Offer();
            t.Realms.RunUntil(() => EscrowBook.Of(t.Realms.M.Scopes[0].World)!.Trades.Count > 0, 10_000, "the swap to open");
            t.Realms.Run(victim == Rules.Master ? 1 : 3, _ => Assert.True(t.Realms.Held() == t.Total));
            Assert.True(EscrowBook.Of(t.Realms.M.Scopes[0].World)!.Trades.Any(x => !x.IsFinal), "the swap should still be open when the server is killed");
            t.Realms.Kill(victim);
            t.Realms.Run(40);
            t.Realms.Start(victim);
            // The ledger on the master is the truth about the swap; a client behind a relay may need a moment after the master comes back.
            var accounts = () => t.Realms.M.Scopes[0].World.Entities.OfType<Account>().ToList();
            t.Realms.RunUntil(
                () => t.Realms.Held() == t.Total && t.Realms.InFlight() == 0 && accounts().Count == 2 && accounts().All(a => a.Status is SwapStatus.Done or SwapStatus.Failed),
                90_000,
                "the swap to finish after the restart",
                why: () => $"held {t.Realms.Held()}/{t.Total} inflight {t.Realms.InFlight()} accounts {string.Join(",", accounts().Select(a => a.Status))}");
            t.Realms.Run(120);
            Assert.Equal(t.Total, t.Realms.Held());
            Assert.Equal(0, t.Realms.InFlight());
            Assert.Equal(0, t.P1.Pending + t.P2.Pending);
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }
    }
}
