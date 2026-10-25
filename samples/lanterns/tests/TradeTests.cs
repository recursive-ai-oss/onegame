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

    // A player who reconnects after a restart gets a fresh idle account (status None) next to the swap's two; only the accounts that
    // took part in a swap say whether it is over.
    private static bool SwapAccountsFinal(List<Account> accounts)
    {
        var inSwap = accounts.Where(a => a.Status != SwapStatus.None).ToList();
        return accounts.Count >= 2 && inSwap.Count > 0 && inSwap.All(a => a.Status is SwapStatus.Done or SwapStatus.Failed);
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
            // It came back from its file, not from a fresh start: the world already holds what the save held.
            Assert.True(t.Realms.Host(victim).Scopes[0].World.Tick > 5, "the restored scope should carry its tick");
            Assert.True(victim != Rules.Master || t.Realms.M.Scopes[0].World.Entities.OfType<Account>().Count() == 2, "the accounts should be back");

            // The ledger on the master is the truth about the swap; a client behind a relay may need a moment after the master comes back.
            var accounts = () => t.Realms.M.Scopes[0].World.Entities.OfType<Account>().ToList();
            t.Realms.RunUntil(
                () => t.Realms.Held() == t.Total && t.Realms.InFlight() == 0 && SwapAccountsFinal(accounts()),
                90_000,
                "the swap to finish after the restart",
                why: () => $"held {t.Realms.Held()}/{t.Total} inflight {t.Realms.InFlight()} accounts {string.Join(",", accounts().Select(a => a.Status))}");
            t.Realms.Run(120);
            Assert.Equal(t.Total, t.Realms.Held());
            Assert.Equal(0, t.Realms.InFlight());
            // A player whose island was killed is gone after the restart (their connection died with it); their lanterns are counted as departed.
            Assert.Equal(0, t.P1.Pending + (victim == Rules.IslandB ? 0 : t.P2.Pending));

            // Players follow the master through their island's relay: once it is back they must see the settled swap too, with no drift.
            // A player whose own island was killed lost the connection itself, so only the other player is checked then.
            var settled = (Account? a) => a?.Status is SwapStatus.Done or SwapStatus.Failed;
            t.Realms.RunUntil(
                () => settled(t.S1.MyAccount) && (victim == Rules.IslandB || settled(t.S2.MyAccount)),
                30_000,
                "the players to see the settled swap",
                why: () => $"p1 {t.S1.MyAccount?.Status} p2 {t.S2.MyAccount?.Status}; master tick {t.Realms.M.Scopes[0].World.Tick}");
            t.Realms.Run(120);
            var followed = t.S1.ReplicaOf(Rules.Master)!;
            Assert.True(
                Math.Abs(t.Realms.M.Scopes[0].World.Tick - followed.LastTick) <= 10,
                $"the client follows the master at tick {followed.LastTick}, the master is at {t.Realms.M.Scopes[0].World.Tick} ({followed.State})");
            Assert.Empty(t.G1.DriftReports);
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
