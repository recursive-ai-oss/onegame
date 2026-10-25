namespace OneGame.Sync.Tests;

/// <summary>
/// The other game server dies, or goes silent, at each point of a hand-off. After every one the traveller is in exactly one world (never two,
/// and never lost): back with its first server, or complete on the second.
/// </summary>
public class HandoffFailureTests
{
    private static (HandoffRig Rig, Wire Wire, Transfer Transfer) Start(Action<HandoffRig, Wire>? arm = null)
    {
        var wire = new Wire();
        var rig = new HandoffRig(wire);
        rig.ScopeOnA.TransferOfferTicks = 40;
        rig.ScopeOnA.TransferResendTicks = 5;
        rig.Run(15);
        arm?.Invoke(rig, wire);
        var transfer = rig.ScopeOnA.BeginTransfer(rig.LinkAB, HandoffRig.ScopeB, [rig.Original]);
        return (rig, wire, transfer);
    }

    private static void AssertCountsHold(HandoffRig rig, bool exact = true)
    {
        Assert.Empty(rig.Violations);
        var (executed, other, waiting) = rig.Outcomes();
        Assert.Equal(0, other);
        Assert.Equal(0, waiting);
        Assert.Equal(rig.Sent, executed);
        var t = rig.Authoritative();
        Assert.NotNull(t);
        if (exact)
        {
            Assert.Equal(rig.Sent, t.Hits);
        }

        Assert.Equal(1, Realm.CountTravelers(rig.ScopeOnA.World, HandoffRig.Player) + Realm.CountTravelers(rig.ScopeOnB.World, HandoffRig.Player));
        Assert.Empty(rig.Cluster.DriftReports);
    }

    [Fact]
    public void PhaseOfferNeverArrivesTheTransferAbortsAndTheTravellerIsUnchanged()
    {
        var (rig, wire, transfer) = Start();
        wire.Dead = true;
        rig.RunUntil(() => transfer.State == TransferState.Aborted, 100);
        Assert.Equal(TransferState.Aborted, transfer.State);
        Assert.Equal(rig.Original, rig.Authoritative()!.Id);
        Assert.Equal("owner-only-diary-4417", rig.Authoritative()!.Diary);
        Assert.Equal(0, TransferLog.Of(rig.ScopeOnA.World)!.OpenCount);
        wire.Dead = false;
        rig.Run(40);
        AssertCountsHold(rig);
        Assert.Equal(rig.Original, rig.Authoritative()!.Id);
        Assert.Equal(0, TransferLog.Of(rig.ScopeOnB.World)!.Count);
    }

    [Fact]
    public void PhaseStagedAnswerIsLostTheOfferTimesOutAndBNeverInstalls()
    {
        var (rig, wire, transfer) = Start();
        wire.CutBackward = true;
        rig.RunUntil(() => transfer.State == TransferState.Aborted, 100);
        Assert.Equal(TransferState.Aborted, transfer.State);
        wire.CutBackward = false;
        rig.Run(40);

        // A late answer from B after the abort changes nothing: A tells B to forget it.
        AssertCountsHold(rig);
        Assert.Equal(rig.Original, rig.Authoritative()!.Id);
        Assert.Null(Realm.FindTraveler(rig.ScopeOnB.World, HandoffRig.Player));
    }

    [Fact]
    public void PhaseCommitIsLostTheBundleStaysDurableAtAAndTheTransferFinishesWhenBReturns()
    {
        var (rig, wire, transfer) = Start();
        rig.ScopeOnA.TransferCommitted += _ => wire.CutForward = true; // the decision is made, the commit message is lost
        rig.Run(30);
        Assert.Equal(TransferState.Committed, transfer.State);

        // The traveller is in no world, but A's log holds it, committed.
        var record = TransferLog.Of(rig.ScopeOnA.World)!.Open.Single();
        Assert.True(record.Committed);
        Assert.Null(Realm.FindTraveler(rig.ScopeOnA.World, HandoffRig.Player));
        Assert.Null(Realm.FindTraveler(rig.ScopeOnB.World, HandoffRig.Player));

        wire.CutForward = false;
        rig.Run(40);
        Assert.Equal(TransferState.Complete, transfer.State);
        Assert.Equal(transfer.NewIds[0], rig.Authoritative()!.Id);
        AssertCountsHold(rig);
        Assert.Equal(0, TransferLog.Of(rig.ScopeOnA.World)!.OpenCount);
    }

    [Fact]
    public void PhaseDoneIsLostTheRepeatedCommitInstallsNothingNew()
    {
        var (rig, wire, transfer) = Start();
        rig.ScopeOnB.TransferInstalled += (_, _) => wire.CutBackward = true;
        rig.Run(30);
        Assert.Equal(TransferState.Committed, transfer.State);
        Assert.NotNull(Realm.FindTraveler(rig.ScopeOnB.World, HandoffRig.Player));
        var installedAs = rig.Authoritative()!.Id;
        var installs = 0;
        rig.ScopeOnB.TransferInstalled += (_, _) => installs++;

        wire.CutBackward = false;
        rig.Run(40);
        Assert.Equal(TransferState.Complete, transfer.State);
        Assert.Equal(0, installs);
        Assert.Equal(installedAs, rig.Authoritative()!.Id);
        Assert.Equal(installedAs, transfer.NewIds[0]);
        AssertCountsHold(rig);
    }

    [Fact]
    public void BCrashingAfterInstallingButBeforeSavingIsBroughtUpToDate()
    {
        var wire = new Wire();
        var rig = new HandoffRig(wire);
        rig.ScopeOnA.TransferOfferTicks = 40;
        rig.ScopeOnA.TransferResendTicks = 5;
        rig.ScopeOnA.Durability = Durability.AfterSave;
        rig.ScopeOnB.Durability = Durability.AfterSave;
        var saveB = rig.ScopeOnB.Save();
        var bSaves = true;
        void Tick(bool send = true)
        {
            rig.Tick(send);
            rig.ScopeOnA.Save();
            if (bSaves)
            {
                saveB = rig.ScopeOnB.Save();
            }
        }

        for (var i = 0; i < 15; i++)
        {
            Tick();
        }

        var transfer = rig.ScopeOnA.BeginTransfer(rig.LinkAB, HandoffRig.ScopeB, [rig.Original]);

        // B installs the traveller and crashes before its next save: the confirmation never left, and nothing it executed since was acknowledged.
        var crashes = true;
        rig.ScopeOnB.TransferInstalled += (_, _) => bSaves &= !crashes;
        for (var i = 0; i < 20; i++)
        {
            Tick();
        }

        Assert.Equal(TransferState.Committed, transfer.State);
        Assert.NotNull(Realm.FindTraveler(rig.ScopeOnB.World, HandoffRig.Player));
        Assert.Equal(SaveStatus.Ok, rig.ScopeOnB.Load(saveB).Status);
        crashes = false;
        bSaves = true;
        Assert.Null(Realm.FindTraveler(rig.ScopeOnB.World, HandoffRig.Player));
        Assert.Equal(1, TransferLog.Of(rig.ScopeOnA.World)!.OpenCount);

        // A's next resend brings B up to date; the traveller exists once.
        for (var i = 0; i < 40; i++)
        {
            Tick();
        }

        for (var i = 0; i < 4; i++)
        {
            Tick(send: false);
        }

        Assert.Equal(TransferState.Complete, transfer.State);
        Assert.Equal(1, Realm.CountTravelers(rig.ScopeOnB.World, HandoffRig.Player));
        Assert.Equal(0, Realm.CountTravelers(rig.ScopeOnA.World, HandoffRig.Player));

        // Every command B acknowledged is in the counter, none twice; the rolled-back ones were never acknowledged.
        var (executed, other, _) = rig.Outcomes();
        Assert.Equal(0, other);
        Assert.Equal(executed, rig.Authoritative()!.Hits);
        Assert.Empty(rig.Violations);
    }

    [Fact]
    public void ANodeRestoredFromItsSaveWhileOfferedPutsTheTravellerBack()
    {
        var wire = new Wire();
        var rig = new HandoffRig(wire);
        rig.Run(10);
        var save = rig.ScopeOnA.Save();
        var transfer = rig.ScopeOnA.BeginTransfer(rig.LinkAB, HandoffRig.ScopeB, [rig.Original]);
        wire.CutBackward = true;
        rig.Run(3);
        Assert.Equal(TransferState.Offered, transfer.State);
        Assert.Null(Realm.FindTraveler(rig.ScopeOnA.World, HandoffRig.Player));

        // A saved while the traveller was in transit, then crashed. The save holds the bundle; on restart the unanswered offer is undone.
        var inTransit = rig.ScopeOnA.Save();
        Assert.Equal(SaveStatus.Ok, rig.ScopeOnA.Load(inTransit).Status);
        rig.Run(3, send: false);
        Assert.Equal(1, Realm.CountTravelers(rig.ScopeOnA.World, HandoffRig.Player));
        Assert.Equal(0, TransferLog.Of(rig.ScopeOnA.World)!.OpenCount);
        Assert.NotNull(save);
    }
}
