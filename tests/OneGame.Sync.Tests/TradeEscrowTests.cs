namespace OneGame.Sync.Tests;

public class TradeEscrowTests
{
    private const ulong Tx = 7001;

    private static void AssertSettled(TradeRig rig, long sellerGold = 10, int sellerSwords = 3, long buyerGold = 100, int buyerSwords = 0, long price = 50)
    {
        Assert.Equal(EscrowState.Settled, rig.Trade(Tx)!.State);
        Assert.Equal(sellerGold + price, rig.PurseOf(TradeRig.ScopeA, TradeRig.Seller).Gold);
        Assert.Equal(sellerSwords - 1, rig.PurseOf(TradeRig.ScopeA, TradeRig.Seller).Swords);
        Assert.Equal(buyerGold - price, rig.PurseOf(TradeRig.ScopeB, TradeRig.Buyer).Gold);
        Assert.Equal(buyerSwords + 1, rig.PurseOf(TradeRig.ScopeB, TradeRig.Buyer).Swords);
    }

    private static void AssertRefunded(TradeRig rig, long sellerGold = 10, int sellerSwords = 3, long buyerGold = 100, int buyerSwords = 0)
    {
        Assert.Equal(EscrowState.Refunded, rig.Trade(Tx)!.State);
        Assert.Equal(sellerGold, rig.PurseOf(TradeRig.ScopeA, TradeRig.Seller).Gold);
        Assert.Equal(sellerSwords, rig.PurseOf(TradeRig.ScopeA, TradeRig.Seller).Swords);
        Assert.Equal(buyerGold, rig.PurseOf(TradeRig.ScopeB, TradeRig.Buyer).Gold);
        Assert.Equal(buyerSwords, rig.PurseOf(TradeRig.ScopeB, TradeRig.Buyer).Swords);
    }

    [Fact]
    public void ASwordForGoldSettlesThroughTheMasterAndTotalsAreConserved()
    {
        var rig = new TradeRig();
        rig.Open(Tx);
        Assert.True(rig.RunUntil(() => rig.Trade(Tx)?.IsFinal == true, 100), rig.Describe(Tx));
        rig.Cluster.Step(5);
        AssertSettled(rig);
        Assert.Equal(rig.InitialGold, rig.TotalGold());
        Assert.Equal(rig.InitialSwords, rig.TotalSwords());
        Assert.Empty(EscrowBook.InFlight(new[] { TradeRig.ScopeA, TradeRig.ScopeB }.Select(s => EscrowBook.Of(rig.Seat(s).World)!)));
    }

    [Fact]
    public void ADuplicateSettleOrAnyRepeatedStepIsANoOp()
    {
        var rig = new TradeRig();
        rig.Open(Tx);
        Assert.True(rig.RunUntil(() => rig.Trade(Tx)?.IsFinal == true, 100));
        var goldA = rig.PurseOf(TradeRig.ScopeA, TradeRig.Seller).Gold;
        var swordsB = rig.PurseOf(TradeRig.ScopeB, TradeRig.Buyer).Swords;

        // The coordinator's node replays the credits and the withdraws, and aborts after the fact; the books answer as before and change nothing.
        var master = rig.NodeOf(TradeRig.MasterScope);
        var toA = master.ReplicaScopes.Single(r => r.Id == TradeRig.ScopeA);
        var toB = master.ReplicaScopes.Single(r => r.Id == TradeRig.ScopeB);
        for (var i = 0; i < 3; i++)
        {
            Escrow.DeskOf(toA.World)!.Send.Credit(Tx, 1, TradeRig.Seller, PurseHandler.GoldKind, 50);
            Escrow.DeskOf(toB.World)!.Send.Credit(Tx, 0, TradeRig.Buyer, PurseHandler.SwordKind, 1);
            Escrow.DeskOf(toA.World)!.Send.Withdraw(Tx, 0, TradeRig.Seller, PurseHandler.SwordKind, 1);
            Escrow.DeskOf(toA.World)!.Send.Abort(Tx, 0);
            rig.Tick();
        }

        rig.Cluster.Step(4);
        Assert.Equal(goldA, rig.PurseOf(TradeRig.ScopeA, TradeRig.Seller).Gold);
        Assert.Equal(swordsB, rig.PurseOf(TradeRig.ScopeB, TradeRig.Buyer).Swords);
        AssertSettled(rig);
        Assert.Contains(toA.Results, r => r.Verdict == CommandVerdict.Rejected && r.Reason == "held"); // the late abort was declined
        Assert.Contains(toA.Results, r => r.Verdict == CommandVerdict.Executed);
    }

    [Fact]
    public void APlayerCannotCallTheDesk()
    {
        var rig = new TradeRig();
        var player = rig.Cluster.AddNode("player");
        var link = rig.Cluster.Connect(player, rig.NodeOf(TradeRig.ScopeA), Trust.Owner(TradeRig.ScopeA).As(TradeRig.Seller));
        var view = player.Subscribe(link, TradeRig.ScopeA, TradeRig.NewWorld(WorldRole.Replica, 5));
        rig.Cluster.Settle();
        Escrow.DeskOf(view.World)!.Send.Credit(1, 0, TradeRig.Seller, PurseHandler.GoldKind, 1_000_000);
        rig.Tick();
        rig.Tick();
        Assert.Equal(CommandVerdict.Refused, view.Results.Single().Verdict);
        Assert.Equal(RefusalReason.NotOwner, view.Results.Single().Refusal);
        Assert.Equal(10, rig.PurseOf(TradeRig.ScopeA, TradeRig.Seller).Gold);
    }

    [Fact]
    public void AGiverWithTooLittleCancelsTheTradeAndTheOtherSideIsRefunded()
    {
        var rig = new TradeRig(buyerGold: 20);
        rig.Open(Tx);
        Assert.True(rig.RunUntil(() => rig.Trade(Tx)?.IsFinal == true, 100));
        AssertRefunded(rig, buyerGold: 20);
    }

    [Fact]
    public void ADeadServerTimesTheTradeOutAndWhatWasTakenGoesBack()
    {
        var rig = new TradeRig(collectTicks: 40);
        var node = rig.NodeOf(TradeRig.ScopeB);
        rig.Cluster.RemoveNode(node); // B is down
        rig.Open(Tx);
        Assert.True(rig.RunUntil(() => rig.Trade(Tx)?.State == EscrowState.Cancelled, 100));
        Assert.True(rig.RunUntil(() => rig.Trade(Tx)!.Legs[0].Delivery == EscrowDelivery.Delivered, 100));

        // The seller already has the sword back; the buyer's leg is still unknown because B is down.
        Assert.Equal(3, rig.PurseOf(TradeRig.ScopeA, TradeRig.Seller).Swords);
        Assert.Equal(EscrowState.Cancelled, rig.Trade(Tx)!.State);
        Assert.Equal(EscrowHold.Pending, rig.Trade(Tx)!.Legs[1].Hold);

        // B comes back from its last save; the coordinator tells it to abort the withdraw it never did. The trade ends refunded.
        rig.Restart(TradeRig.ScopeB);
        Assert.True(rig.RunUntil(() => rig.Trade(Tx)?.IsFinal == true, 100));
        AssertRefunded(rig);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void TheTradeSettlesExactlyOnceUnderLossDuplicationAndReordering(int seed)
    {
        var rig = new TradeRig(seed, drop: 0.25, duplicate: 0.3, maxDelay: 6);
        rig.Open(Tx);
        Assert.True(rig.RunUntil(() => rig.Trade(Tx)?.IsFinal == true, 600), $"seed {seed}: the trade did not finish");
        rig.Cluster.Step(5);
        Assert.True(rig.Dropped + rig.Duplicated + rig.Delayed > 0, "the network did not misbehave");
        AssertSettled(rig);
        Assert.Equal(rig.InitialGold, rig.TotalGold());
        Assert.Equal(rig.InitialSwords, rig.TotalSwords());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void ManyTradesAtOnceUnderBadNetworkConserveEverything(int seed)
    {
        var rig = new TradeRig(seed, drop: 0.2, duplicate: 0.2, maxDelay: 4, sellerGold: 1000, sellerSwords: 30, buyerGold: 100_000);
        for (ulong tx = 100; tx < 120; tx++)
        {
            rig.Open(tx, 10 + (long)tx);
            rig.Tick();
        }

        Assert.True(rig.RunUntil(() => EscrowBook.Of(rig.Master.World)!.Trades.Count(t => t.IsFinal) == 20, 1500), "not every trade finished");
        Assert.All(EscrowBook.Of(rig.Master.World)!.Trades, t => Assert.Equal(EscrowState.Settled, t.State));
        Assert.Equal(10, rig.PurseOf(TradeRig.ScopeA, TradeRig.Seller).Swords);
        Assert.Equal(20, rig.PurseOf(TradeRig.ScopeB, TradeRig.Buyer).Swords);
        Assert.Equal(1000 + Enumerable.Range(100, 20).Sum(x => 10 + (long)x), rig.PurseOf(TradeRig.ScopeA, TradeRig.Seller).Gold);
        Assert.True(rig.Dropped > 0 && rig.Duplicated > 0 && rig.Delayed > 0, "the network did not misbehave");
    }

    public static IEnumerable<object[]> KillPoints()
    {
        foreach (var who in new[] { 1u, 2u, 100u })
        {
            for (var tick = 1; tick <= 24; tick++)
            {
                yield return [who, tick, 1];
                yield return [who, tick, 4];
            }
        }
    }

    [Theory]
    [MemberData(nameof(KillPoints))]
    public void AKilledNodeRestartedFromItsLastSaveAtAnyStepLosesAndDuplicatesNothing(uint who, int killAtTick, int saveEvery)
    {
        var rig = new TradeRig(seed: killAtTick, drop: 0.1, duplicate: 0.1, maxDelay: 2, saveEvery: saveEvery);
        rig.Open(Tx);
        for (var i = 0; i < killAtTick; i++)
        {
            rig.Tick();
        }

        rig.Restart(new ScopeId(who));
        Assert.True(rig.RunUntil(() => rig.Trade(Tx)?.IsFinal == true, 800, reopen: Tx), $"node {who} killed at tick {killAtTick}: the trade did not finish");
        rig.Cluster.Step(5);

        // The trade ended one way or the other, every total is intact (checked at every tick), and the books agree with the purses.
        var state = rig.Trade(Tx)!.State;
        if (state == EscrowState.Settled)
        {
            AssertSettled(rig);
        }
        else
        {
            AssertRefunded(rig);
        }

        Assert.Equal(rig.InitialGold, rig.TotalGold());
        Assert.Equal(rig.InitialSwords, rig.TotalSwords());
        Assert.Empty(EscrowBook.InFlight(new[] { TradeRig.ScopeA, TradeRig.ScopeB }.Select(s => EscrowBook.Of(rig.Seat(s).World)!)));
    }

    [Fact]
    public void WithoutDurabilityARestartFromAnOlderSaveCanBreakTheTotals()
    {
        // The control for the test above: if answers leave before the state they describe is saved, a restart from the last save contradicts
        // what a peer was told, and value is duplicated or lost at some kill point. This is why AfterSave exists.
        var broken = 0;
        foreach (var who in new[] { 1u, 2u, 100u })
        {
            for (var kill = 1; kill <= 24; kill++)
            {
                var rig = new TradeRig(seed: kill, saveEvery: 4, durability: Durability.Immediate);
                rig.Open(Tx);
                try
                {
                    for (var i = 0; i < kill; i++)
                    {
                        rig.Tick();
                    }

                    rig.Restart(new ScopeId(who));
                    rig.RunUntil(() => rig.Trade(Tx)?.IsFinal == true, 400, reopen: Tx);
                }
                catch (InvalidOperationException)
                {
                    broken++;
                }
            }
        }

        Assert.True(broken > 0, "no kill point broke the totals without durability, so the restart test proves nothing");
    }

    [Fact]
    public void TheCoordinatorsRecordingReplaysToItsHashes()
    {
        var rig = new TradeRig(seed: 3, drop: 0.1, duplicate: 0.1, maxDelay: 2);
        var recorder = rig.Master.StartRecording(new RecordingOptions { CheckpointInterval = 1 });
        rig.Open(Tx);
        Assert.True(rig.RunUntil(() => rig.Trade(Tx)?.IsFinal == true, 300));
        rig.Cluster.Step(3);
        var recording = recorder.Stop();
        var replay = Replayer.Replay(recording, TradeRig.NewWorld(WorldRole.Authority, 23));
        Assert.True(replay.Ok, replay.Divergence?.ToString());
        Assert.Equal(rig.Master.Hash, replay.FinalHash);
        Assert.Equal(EscrowState.Settled, rig.Trade(Tx)!.State);
    }

    [Fact]
    public void AGameServerOpensATradeOverItsOwnLinkAndAPlayerCannot()
    {
        var rig = new TradeRig();
        var server = rig.NodeOf(TradeRig.ScopeA);
        var link = rig.Cluster.Connect(server, rig.NodeOf(TradeRig.MasterScope), Escrow.CoordinatorTrust(TradeRig.MasterScope));
        var view = server.Subscribe(link, TradeRig.MasterScope, TradeRig.NewWorld(WorldRole.Replica, 6));
        var player = rig.Cluster.AddNode("player");
        var playerLink = rig.Cluster.Connect(player, rig.NodeOf(TradeRig.MasterScope), Trust.Owner(TradeRig.MasterScope).As(TradeRig.Seller));
        var playerView = player.Subscribe(playerLink, TradeRig.MasterScope, TradeRig.NewWorld(WorldRole.Replica, 7));
        rig.Cluster.Settle();

        Escrow.DeskOf(playerView.World)!.Send.Open(Tx, 1, 1, 2, 2, 1, 2, 2, 1, 1, 1);
        rig.Tick();
        Assert.Null(rig.Trade(Tx));
        Assert.Equal(CommandVerdict.Refused, playerView.Results.Single().Verdict);

        Escrow.DeskOf(view.World)!.Send.Open(Tx, TradeRig.ScopeA.Value, TradeRig.Seller, TradeRig.Buyer, PurseHandler.SwordKind, 1, TradeRig.ScopeB.Value, TradeRig.Buyer, TradeRig.Seller, PurseHandler.GoldKind, 25);
        Assert.True(rig.RunUntil(() => rig.Trade(Tx)?.IsFinal == true, 100));
        AssertSettled(rig, price: 25);
    }
}
