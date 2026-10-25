namespace OneGame.Sync.Tests;

public class HandoffTests
{
    [Fact]
    public void ATravellerMovesBetweenServersWhileTheClientKeepsHittingIt()
    {
        var rig = new HandoffRig();
        var recordA = rig.ScopeOnA.StartRecording(new RecordingOptions { CheckpointInterval = 1 });
        var recordB = rig.ScopeOnB.StartRecording(new RecordingOptions { CheckpointInterval = 1 });
        rig.Run(20);
        var transfer = rig.ScopeOnA.BeginTransfer(rig.LinkAB, HandoffRig.ScopeB, [rig.Original]);
        rig.Run(120);

        Assert.Equal(TransferState.Complete, transfer.State);
        Assert.Empty(rig.Violations);
        Assert.Null(Realm.FindTraveler(rig.ScopeOnA.World, HandoffRig.Player));
        var landed = Realm.FindTraveler(rig.ScopeOnB.World, HandoffRig.Player);
        Assert.NotNull(landed);
        Assert.Equal(transfer.NewIds[0], landed.Id);

        // Every command the client sent applied exactly once: the counter equals the number of answers that said "executed", and no command is missing.
        var (executed, other, waiting) = rig.Outcomes();
        Assert.True(rig.Sent > 60, $"only {rig.Sent} commands were sent");
        Assert.Equal(0, other);
        Assert.Equal(0, waiting);
        Assert.Equal(rig.Sent, executed);
        Assert.Equal(rig.Sent, landed.Hits);

        // The client's replica found the new scope without being told to, and shows the traveller there.
        Assert.Contains(rig.Client.ReplicaScopes, r => r.Id == HandoffRig.ScopeB && r.State == ReplicaState.Live);
        var seen = rig.Visible();
        Assert.NotNull(seen);
        Assert.Equal(HandoffRig.ScopeB, seen.Value.Replica.Id);
        Assert.Equal(landed.Hits, seen.Value.Traveler.Hits);
        Assert.Equal("owner-only-diary-4417", seen.Value.Traveler.Diary);
        Assert.Empty(rig.Cluster.DriftReports);
        Assert.Empty(rig.Cluster.CheckReplicas());

        // Each side's recording replays to its own hashes, across the boundary.
        var ra = recordA.Stop();
        var rb = recordB.Stop();
        Assert.Contains(ra.Commands, c => TransferOps.IsOp(c.Entity, c.CommandId));
        Assert.Contains(rb.Commands, c => TransferOps.IsOp(c.Entity, c.CommandId));
        var replayA = Replayer.Replay(Recording.TryRead(ra.ToBytes(), out var da) == SyncError.None ? da! : ra, Realm.NewWorld(WorldRole.Authority));
        var replayB = Replayer.Replay(Recording.TryRead(rb.ToBytes(), out var db) == SyncError.None ? db! : rb, Realm.NewWorld(WorldRole.Authority, 12));
        Assert.True(replayA.Ok, replayA.Divergence?.ToString());
        Assert.True(replayB.Ok, replayB.Divergence?.ToString());
        Assert.True(replayA.CheckpointsVerified > 100);
        Assert.Equal(rig.ScopeOnA.Hash, replayA.FinalHash);
        Assert.Equal(rig.ScopeOnB.Hash, replayB.FinalHash);
    }

    [Fact]
    public void OwnerOnlyCommandsAreCheckedAgainstTheDepartingEntityAndForwarded()
    {
        var wire = new Wire();
        var rig = new HandoffRig(wire);
        rig.Run(10);
        wire.CutBackward = true; // B's answers are lost for a while, so the traveller stays in transit
        var spy = rig.Cluster.AddNode("spy");
        var spyLink = rig.Cluster.Connect(spy, rig.A, Trust.Owner(HandoffRig.ScopeA).As(77));
        var transfer = rig.ScopeOnA.BeginTransfer(rig.LinkAB, HandoffRig.ScopeB, [rig.Original]);
        rig.Run(2, send: false);
        Assert.Equal(TransferState.Offered, transfer.State);
        var fake = spy.Subscribe(spyLink, HandoffRig.ScopeA, Realm.NewWorld(WorldRole.Replica));
        rig.Cluster.Settle();

        // While the traveller is in transit, a stranger's owner-only command is still refused at A, and the owner's is held and applied later.
        var result = spyLink.Connection.SubmitCommand(HandoffRig.ScopeA, rig.Original, Cmd.Write, WriteArgs("hijack"));
        Assert.Equal(SubmitResult.NotTrusted, result);
        var ownerLink = rig.ViewOfA.Link;
        Assert.Equal(SubmitResult.Queued, ownerLink.Connection.SubmitCommand(HandoffRig.ScopeA, rig.Original, Cmd.Write, WriteArgs("during-transit")));
        rig.Run(5, send: false);
        Assert.Equal(TransferState.Offered, transfer.State);
        wire.CutBackward = false;
        rig.Run(60, send: false);
        Assert.Equal(TransferState.Complete, transfer.State);
        Assert.Equal("during-transit", rig.Authoritative()!.Diary);
        Assert.NotNull(fake);

        // After the move the old id still works for the owner (A forwards it) and still refuses the stranger.
        Assert.Equal(SubmitResult.NotTrusted, spyLink.Connection.SubmitCommand(HandoffRig.ScopeA, rig.Original, Cmd.Write, WriteArgs("late-hijack")));
        Assert.Equal(SubmitResult.Queued, ownerLink.Connection.SubmitCommand(HandoffRig.ScopeA, rig.Original, Cmd.Write, WriteArgs("new-entry")));
        rig.Run(10, send: false);
        Assert.Equal("new-entry", rig.Authoritative()!.Diary);
    }

    [Fact]
    public void AnOfferTheGateRefusesLeavesTheTravellerWhereItWas()
    {
        var rig = new HandoffRig();
        rig.ScopeOnB.TransferGate = _ => false;
        rig.Run(5);
        var transfer = rig.ScopeOnA.BeginTransfer(rig.LinkAB, HandoffRig.ScopeB, [rig.Original]);
        rig.Run(30);
        Assert.Equal(TransferState.Aborted, transfer.State);
        Assert.Contains("refused", transfer.Reason);
        Assert.NotNull(Realm.FindTraveler(rig.ScopeOnA.World, HandoffRig.Player));
        Assert.Null(Realm.FindTraveler(rig.ScopeOnB.World, HandoffRig.Player));
        Assert.Equal(rig.Original, rig.Authoritative()!.Id);
        var (executed, other, waiting) = rig.Outcomes();
        Assert.Equal(0, other + waiting);
        Assert.Equal(rig.Sent, executed);
        Assert.Equal(rig.Sent, rig.Authoritative()!.Hits);
        Assert.Empty(rig.Violations);
    }

    [Fact]
    public void ATransferWithoutTrustToOwnIsRefused()
    {
        var rig = new HandoffRig();
        var weak = rig.Cluster.AddNode("weak");
        var link = rig.Cluster.Connect(weak, rig.B, Trust.Player(HandoffRig.ScopeB));
        var other = rig.Cluster.AddScope(weak, new ScopeId(9), Realm.NewWorld(WorldRole.Authority));
        var item = other.World.Add(new Traveler { Player = 9, Name = "Nope" });
        rig.Cluster.Step(2);
        var transfer = other.BeginTransfer(link, HandoffRig.ScopeB, [item.Id]);
        rig.Cluster.Step(10);
        Assert.Equal(TransferState.Aborted, transfer.State);
        Assert.NotNull(other.World.Find(item.Id));
        Assert.Null(Realm.FindTraveler(rig.ScopeOnB.World, 9));
        Assert.Contains(rig.B.Refusals, f => f.Reason == RefusalReason.NotTrustedToOwn);
    }

    private static byte[] WriteArgs(string text)
    {
        var w = new SyncWriter(32);
        w.WriteString(text);
        return w.ToArray();
    }
}

internal static class Cmd
{
    public static uint Write => StateHash.Fnv32("Write");
}
