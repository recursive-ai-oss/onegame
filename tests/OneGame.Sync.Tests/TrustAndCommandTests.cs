using OneGame.Core.Tests.Samples;

namespace OneGame.Sync.Tests;

public class TrustAndCommandTests
{
    private static readonly uint Spend = StateHash.Fnv32("Spend");
    private static readonly uint Rename = StateHash.Fnv32("Rename");
    private static readonly uint Walk = StateHash.Fnv32("Walk");

    private static Trust Ada(Rig rig) => Trust.Player(rig.Scope.Id).As(1, 10);

    [Fact]
    public void AHandlerRejectionComesBackWithItsReasonToTheSender()
    {
        var rig = new Rig();
        var ada = rig.Join("ada", Ada(rig));
        rig.StepChecked(5, ada);
        var hero = Rig.HeroOf(ada.Replica, 1);

        hero.Send.Spend(100_000);
        var tooMuch = ada.Replica.LastSequence;
        hero.Send.Spend(1);
        var fine = ada.Replica.LastSequence;
        hero.Send.Spend(-4);
        var negative = ada.Replica.LastSequence;
        Assert.Equal(3, ada.Replica.PendingCommands);
        Assert.NotEqual(tooMuch, fine);

        var completed = new List<CommandResult>();
        ada.Replica.CommandCompleted += completed.Add;
        rig.StepChecked(2, ada);

        Assert.Equal(0, ada.Replica.PendingCommands);
        var byId = ada.Replica.Results.ToDictionary(r => r.Sequence);
        Assert.Equal(CommandVerdict.Rejected, byId[tooMuch].Verdict);
        Assert.Equal("not enough gold", byId[tooMuch].Reason);
        Assert.Equal(CommandVerdict.Executed, byId[fine].Verdict);
        Assert.Null(byId[fine].Reason);
        Assert.Equal(CommandVerdict.Rejected, byId[negative].Verdict);
        Assert.Equal("amount must be positive", byId[negative].Reason);
        Assert.Equal(3, completed.Count);
        Assert.Equal(hero.Id, byId[tooMuch].Entity);
        Assert.Equal(Spend, byId[tooMuch].CommandId);

        // The authority saw the same three outcomes.
        Assert.Equal(2, rig.Scope.Outcomes.Count(o => o.Status == CommandStatus.Rejected));
    }

    [Fact]
    public void ABoolHandlerRejectsWithoutAReasonAndUnknownTargetsAreReported()
    {
        var (cluster, scope, view) = Scenario.NewCluster();
        var shop = (Shop)view.World.Find(Scenario.ShopId)!;
        shop.Send.Buy(99, 1); // no such item: the handler returns false
        var rejected = view.LastSequence;
        view.World.Submit(new EntityId(999), StateHash.Fnv32("Buy"), []);
        var noEntity = view.LastSequence;
        view.World.Submit(Scenario.ShopId, StateHash.Fnv32("Nope"), []);
        var noCommand = view.LastSequence;
        view.World.Submit(Scenario.ShopId, StateHash.Fnv32("Buy"), [1]);
        var malformed = view.LastSequence;
        cluster.Step(2);

        var byId = view.Results.ToDictionary(r => r.Sequence);
        Assert.Equal(CommandVerdict.Rejected, byId[rejected].Verdict);
        Assert.Null(byId[rejected].Reason);
        Assert.Equal(CommandVerdict.UnknownEntity, byId[noEntity].Verdict);
        Assert.Equal(CommandVerdict.UnknownCommand, byId[noCommand].Verdict);
        Assert.Equal(CommandVerdict.Malformed, byId[malformed].Verdict);
    }

    [Fact]
    public void RefusalsReachBothEndsWithTheSenderSequence()
    {
        var rig = new Rig();
        var reader = rig.Join("reader", Trust.Reader(rig.Scope.Id));
        rig.StepChecked(3, reader);
        var hero = Rig.HeroOf(reader.Replica, 1);

        hero.Send.Walk(1, 1);
        var sequence = reader.Replica.LastSequence;
        rig.StepChecked(2, reader);

        var result = Assert.Single(reader.Replica.Results);
        Assert.Equal(sequence, result.Sequence);
        Assert.Equal(CommandVerdict.Refused, result.Verdict);
        Assert.Equal(RefusalReason.NotTrustedToCommand, result.Refusal);
        Assert.Equal(RefusalReason.NotTrustedToCommand, Assert.Single(rig.Host.Refusals).Reason);
        Assert.Equal(RefusalReason.NotTrustedToCommand, Assert.Single(reader.Node.Refusals).Reason);
        Assert.Equal(hero.Id, Assert.Single(reader.Node.Refusals).Entity);
        Assert.Equal(Rig.HeroOf(rig.Scope, 1).Position, new FVec2(1, 1)); // untouched
    }

    [Fact]
    public void OwnerOnlyCommandsNeedOwnTrustAndTheOwnersIdentity()
    {
        var rig = new Rig();
        var watcher = rig.Join("watcher", Trust.Player(rig.Scope.Id).As(1, 10)); // may command, may not own
        var owner = rig.Join("owner", Trust.Owner(rig.Scope.Id).As(1, 10));
        var rival = rig.Join("rival", Trust.Owner(rig.Scope.Id).As(2, 20));
        rig.StepChecked(3, watcher, owner, rival);

        Rig.HeroOf(watcher.Replica, 1).Send.Rename("Watcher");
        var watcherSeq = watcher.Replica.LastSequence;
        Rig.HeroOf(rival.Replica, 1).Send.Rename("Rival");
        var rivalSeq = rival.Replica.LastSequence;
        Rig.HeroOf(owner.Replica, 1).Send.Rename("Lady Ada");
        var ownerSeq = owner.Replica.LastSequence;
        rig.StepChecked(3, watcher, owner, rival);

        var w = watcher.Replica.Results.Single(r => r.Sequence == watcherSeq);
        Assert.Equal(CommandVerdict.Refused, w.Verdict);
        Assert.Equal(RefusalReason.NotTrustedToOwn, w.Refusal);
        var r2 = rival.Replica.Results.Single(r => r.Sequence == rivalSeq);
        Assert.Equal(CommandVerdict.Refused, r2.Verdict);
        Assert.Equal(RefusalReason.NotOwner, r2.Refusal);
        Assert.Equal(CommandVerdict.Executed, owner.Replica.Results.Single(r => r.Sequence == ownerSeq).Verdict);
        Assert.Equal("Lady Ada", Rig.HeroOf(rig.Scope, 1).Name);
        Assert.Equal("Lady Ada", Rig.HeroOf(rival.Replica, 1).Name);

        // Both ends keep a record of the refusals.
        Assert.Contains(rig.Host.Refusals, r => r.Reason == RefusalReason.NotTrustedToOwn && r.Connection == watcher.Connection.Id);
        Assert.Contains(rig.Host.Refusals, r => r.Reason == RefusalReason.NotOwner && r.Connection == rival.Connection.Id);
        Assert.Contains(watcher.Node.Refusals, r => r.Reason == RefusalReason.NotTrustedToOwn);
        Assert.Contains(rival.Node.Refusals, r => r.Reason == RefusalReason.NotOwner);

        // The harness path is checked the same way.
        var wire = new SyncWriter();
        wire.WriteString("Hijacked");
        Assert.Equal(SubmitResult.NotTrusted, watcher.Connection.SubmitCommand(rig.Scope.Id, Rig.HeroOf(rig.Scope, 1).Id, Rename, wire.Written));
        Assert.Equal(SubmitResult.Queued, owner.Connection.SubmitCommand(rig.Scope.Id, Rig.HeroOf(rig.Scope, 1).Id, Rename, wire.Written));

        // Granting the right takes effect on the next command.
        watcher.Connection.Trust = Trust.Owner(rig.Scope.Id).As(1, 10);
        Assert.Equal(SubmitResult.Queued, watcher.Connection.SubmitCommand(rig.Scope.Id, Rig.HeroOf(rig.Scope, 1).Id, Rename, wire.Written));
        rig.StepChecked(2, watcher, owner, rival);
        Assert.Equal("Hijacked", Rig.HeroOf(rig.Scope, 1).Name);
    }

    [Fact]
    public void ReadAndCommandAreRefusedPerScope()
    {
        // One host, two scopes; the player may read both but command only the match.
        var cluster = new Cluster(new ClusterOptions { DriftCheckInterval = 1, FatalDrift = true });
        var host = cluster.AddNode("host");
        var match = cluster.AddScope(host, new ScopeId(1), Arena.Match());
        var bank = cluster.AddScope(host, new ScopeId(2), Arena.Master(1, 2));
        var node = cluster.AddNode("ada");
        var link = cluster.Connect(node, host, new Trust(ScopeSet.Of(match.Id, bank.Id), ScopeSet.Of(match.Id), ScopeSet.None, new Audience(1, 10)));
        var inMatch = node.Subscribe(link, match.Id, Arena.Empty(WorldRole.Replica));
        var inBank = node.Subscribe(link, bank.Id, Arena.Empty(WorldRole.Replica));
        var third = node.Subscribe(link, new ScopeId(3), Arena.Empty(WorldRole.Replica));
        cluster.Step(3);

        Assert.Equal(ReplicaState.Live, inMatch.State);
        Assert.Equal(ReplicaState.Live, inBank.State);
        Assert.Equal(ReplicaState.Refused, third.State);
        Assert.Contains(node.Refusals, r => r.Scope == new ScopeId(3) && r.Reason == RefusalReason.UnknownScope);

        Rig.AccountOf(inBank, 1).Send.Deposit(5);
        var seq = inBank.LastSequence;
        Rig.HeroOf(inMatch, 1).Send.Walk(2, 0);
        var walk = inMatch.LastSequence;
        cluster.Step(3);
        Assert.Equal(CommandVerdict.Refused, inBank.Results.Single(r => r.Sequence == seq).Verdict);
        Assert.Equal(RefusalReason.NotTrustedToCommand, inBank.Results.Single(r => r.Sequence == seq).Refusal);
        Assert.Equal(CommandVerdict.Executed, inMatch.Results.Single(r => r.Sequence == walk).Verdict);
        Assert.Equal(new FVec2(3, 1), Rig.HeroOf(match, 1).Position);
    }

    [Fact]
    public void ReadTrustTakenAwayStopsTheFlowAndEmptiesTheReplica()
    {
        var rig = new Rig();
        var ada = rig.Join("ada", Ada(rig));
        var bo = rig.Join("bo", Trust.Player(rig.Scope.Id).As(2, 20));
        rig.StepChecked(10, ada, bo);
        Assert.NotEmpty(ada.Replica.World.Entities);

        ada.Connection.Trust = Trust.None;
        rig.StepChecked(1, bo);
        Assert.Equal(ReplicaState.Revoked, ada.Replica.State);
        Assert.Empty(ada.Replica.World.Entities);
        Assert.Equal(0, ada.Replica.World.Tables[0].Count);
        Assert.Same(bo.Connection, Assert.Single(rig.Scope.Subscribers));
        Assert.Contains(ada.Node.Refusals, r => r.Reason == RefusalReason.ReadRevoked);
        var frames = ada.Replica.FramesApplied;
        rig.StepChecked(10, bo);
        Assert.Equal(frames, ada.Replica.FramesApplied);

        // Granting it again and asking again brings the data back.
        ada.Connection.Trust = Ada(rig);
        ada.Node.Resubscribe(ada.Replica);
        rig.StepChecked(10, ada, bo);
        Assert.Equal(ReplicaState.Live, ada.Replica.State);
        Assert.NotEmpty(ada.Replica.World.Entities);
        Assert.Equal("ruby-hoard-7731", Rig.HeroOf(ada.Replica, 1).Stash);
    }

    [Fact]
    public void NodesSubscribeAndUnsubscribeAtRuntimeAndManyFollowOneScope()
    {
        var rig = new Rig();
        var a = rig.Join("a", Trust.Reader(rig.Scope.Id));
        var b = rig.Join("b", Trust.Reader(rig.Scope.Id));
        var c = rig.Join("c", Trust.Reader(rig.Scope.Id));
        rig.StepChecked(10, a, b, c);
        Assert.Equal(3, rig.Scope.Subscribers.Count);

        Assert.True(b.Node.Unsubscribe(b.Replica));
        Assert.False(b.Node.Unsubscribe(b.Replica));
        Assert.Equal(ReplicaState.Unsubscribed, b.Replica.State);
        rig.StepChecked(10, a, c);
        Assert.Equal(2, rig.Scope.Subscribers.Count);
        Assert.Empty(b.Node.ReplicaScopes);
        var frozen = b.Replica.World.Tick;
        rig.StepChecked(5, a, c);
        Assert.Equal(frozen, b.Replica.World.Tick);

        b.Node.Resubscribe(b.Replica);
        rig.StepChecked(10, a, b, c);
        Assert.Equal(ReplicaState.Live, b.Replica.State);
        Assert.Equal(rig.Scope.World.Tick, b.Replica.World.Tick);
        Assert.Equal(3, rig.Scope.Subscribers.Count);

        // Following the same scope twice over one link is a mistake with a clear message.
        var ex = Assert.Throws<ArgumentException>(() => a.Node.Subscribe(a.Link, rig.Scope.Id, Arena.Empty(WorldRole.Replica)));
        Assert.Contains("already follows", ex.Message);
    }

    [Fact]
    public void CommandsQueuedWhenASaveIsLoadedAreReportedDropped()
    {
        var rig = new Rig();
        var ada = rig.Join("ada", Trust.Player(rig.Scope.Id).As(1, 10));
        rig.StepChecked(5, ada);
        var save = rig.Scope.Save();
        Rig.HeroOf(ada.Replica, 1).Send.Walk(1, 0);
        var seq = ada.Replica.LastSequence;
        rig.Cluster.Settle(); // the command is queued on the authority, not yet run
        Assert.Equal(SaveStatus.Ok, rig.Scope.Load(save).Status);
        rig.Cluster.Step(2);
        Assert.Equal(CommandVerdict.Dropped, ada.Replica.Results.Single(r => r.Sequence == seq).Verdict);
        Assert.Equal(0, ada.Replica.PendingCommands);
    }
}

public class DigestTimeoutTests
{
    [Fact]
    public void ADigestRequestThatNeverGetsAnAnswerTimesOutAndIsReported()
    {
        var options = new ClusterOptions { DriftCheckInterval = 1, DigestTimeoutTicks = 5, ResyncOnDrift = false };
        var host = new Node("host", options);
        var client = new Node("client", options);
        var scope = host.AddScope(new ScopeId(1), Arena.Match());
        var link = client.ConnectTo(host, Trust.Player(scope.Id).As(1, 10));
        var replica = client.Subscribe(link, scope.Id, Arena.Empty(WorldRole.Replica));
        host.Pump();
        client.Pump();
        Assert.Equal(ReplicaState.Live, replica.State);

        // Corrupt the replica, then keep the authority from ever answering: only the client pumps.
        var hero = Rig.HeroOf(replica, 2);
        var tamper = new Hero { Position = new FVec2(77, 77) };
        var delta = new SyncWriter();
        tamper.WriteMaskedDelta(delta, 1UL << 3);
        var reader = new SyncReader(delta.Written);
        hero.ApplyDelta(ref reader);

        scope.StepAndPublish();
        client.Pump();
        Assert.Empty(client.DriftReports); // the request is out; no verdict yet
        for (var i = 0; i < 4; i++)
        {
            scope.StepAndPublish();
            client.Pump();
        }

        Assert.Empty(client.DriftReports);
        scope.StepAndPublish();
        client.Pump();
        var report = Assert.Single(client.DriftReports);
        Assert.True(report.TimedOut);
        Assert.False(report.DetailAvailable);
        Assert.Empty(report.Differences);
        Assert.Contains("did not answer", report.ToString());
        Assert.Contains("\"timedOut\":true", report.ToJson());
    }

    [Fact]
    public void AnAnsweredDigestRequestIsNotReportedAsATimeout()
    {
        var rig = new Rig(new ClusterOptions { DriftCheckInterval = 1, DigestTimeoutTicks = 5, ResyncOnDrift = true });
        var ada = rig.Join("ada", Trust.Player(rig.Scope.Id).As(1, 10));
        rig.StepChecked(5, ada);
        var tamper = new Hero { Position = new FVec2(77, 77) };
        var delta = new SyncWriter();
        tamper.WriteMaskedDelta(delta, 1UL << 3);
        var reader = new SyncReader(delta.Written);
        Rig.HeroOf(ada.Replica, 2).ApplyDelta(ref reader);
        rig.Cluster.Step(20);
        var report = Assert.Single(rig.Cluster.DriftReports);
        Assert.False(report.TimedOut);
        Assert.True(report.DetailAvailable);
    }
}
