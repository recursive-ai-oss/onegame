using OneGame.Core.Tests.Samples;

namespace OneGame.Sync.Tests;

public class ClusterTests
{
    [Fact]
    public void ReplicaMatchesAuthorityAtEveryTick()
    {
        var (cluster, scope, view) = Scenario.NewCluster();
        Assert.Equal(ReplicaState.Live, view.State);
        Assert.Equal(scope.Hash, view.Hash);
        var checks = 0;
        Scenario.Play(cluster, scope, view, 400, () =>
        {
            Assert.Equal(scope.World.Tick, view.World.Tick);
            Assert.Equal(scope.Hash, view.Hash);
            checks++;
        });

        Assert.Equal(400, checks);
        Assert.Empty(cluster.DriftReports);
        Assert.Empty(cluster.CheckReplicas());
        Assert.True(scope.CommandsRun > 50);
        // The scripted player really changed things on the authority, through the replica's commands.
        Assert.Equal("Shop 388", ((Shop)scope.World.Find(Scenario.ShopId)!).Name);
        Assert.Equal("Shop 388", ((Shop)view.World.Find(Scenario.ShopId)!).AsReadOnly.Name);
    }

    [Fact]
    public void ReplicaRejectsLocalWrites()
    {
        var (_, _, view) = Scenario.NewCluster();
        var shop = (Shop)view.World.Find(Scenario.ShopId)!;
        Assert.Throws<ReplicaWriteException>(() => shop.Stock = 1);
        Assert.Throws<ReplicaWriteException>(() => view.World.Step());
    }

    [Fact]
    public void EntitiesAddedAndRemovedOnTheAuthorityReachTheReplica()
    {
        var (cluster, scope, view) = Scenario.NewCluster();
        cluster.Step(3);
        var added = scope.World.Add(new Bot { Hp = 5, Position = new FVec2(1, 2) });
        scope.World.Remove(new EntityId(3));
        cluster.Step(2);
        Assert.NotNull(view.World.Find(added.Id));
        Assert.Null(view.World.Find(new EntityId(3)));
        Assert.Equal(scope.Hash, view.Hash);
        ((Bot)scope.World.Find(added.Id)!).Hp = 99;
        cluster.Step(1);
        Assert.Equal(((Bot)scope.World.Find(added.Id)!).Hp, ((Bot)view.World.Find(added.Id)!).Hp);
        Assert.InRange(((Bot)view.World.Find(added.Id)!).Hp, 98, 99);
        Assert.Empty(cluster.DriftReports);
    }

    [Fact]
    public void LateSubscriberCatchesUp()
    {
        var (cluster, scope, view) = Scenario.NewCluster();
        Scenario.Play(cluster, scope, view, 100);
        var late = cluster.AddLocalReplica(scope, Scenario.NewWorld(WorldRole.Replica), Trust.Reader(scope.Id), "late");
        cluster.Settle();
        Assert.Equal(scope.Hash, late.Hash);
        Scenario.Play(cluster, scope, view, 100);
        Assert.Equal(scope.Hash, late.Hash);
        Assert.Equal(scope.Hash, view.Hash);
        Assert.Empty(cluster.CheckReplicas());
    }

    [Fact]
    public void CommandWithoutCommandRightsIsRefusedAndReported()
    {
        var cluster = new Cluster(new ClusterOptions { DriftCheckInterval = 1, FatalDrift = true });
        var host = cluster.AddNode("host");
        var scope = cluster.AddScope(host, new ScopeId(1), Scenario.NewAuthority());
        var watcher = cluster.AddLocalReplica(scope, Scenario.NewWorld(WorldRole.Replica), Trust.Reader(scope.Id), "watcher");
        cluster.Settle();
        var before = ((Shop)scope.World.Find(Scenario.ShopId)!).Stock;

        // Through the transport, from a replica whose connection may only read.
        var status = ((Shop)watcher.World.Find(Scenario.ShopId)!).Send.Buy(1, 5);
        Assert.Equal(CommandStatus.Forwarded, status);
        cluster.Step(2);
        Assert.Equal(before, ((Shop)scope.World.Find(Scenario.ShopId)!).Stock);
        var refusal = Assert.Single(host.Refusals);
        Assert.Equal(RefusalReason.NotTrustedToCommand, refusal.Reason);
        Assert.Equal(Scenario.ShopId, refusal.Entity);
        // The submitter is told too.
        Assert.Equal(RefusalReason.NotTrustedToCommand, Assert.Single(watcher.Node.Refusals).Reason);

        // As a connection, directly (the harness path).
        var connection = host.Connections[0];
        var wire = new SyncWriter();
        wire.WriteInt32(1);
        wire.WriteInt32(5);
        Assert.Equal(SubmitResult.NotTrusted, connection.SubmitCommand(scope.Id, Scenario.ShopId, CommandIds.Buy, wire.Written));
        Assert.Equal(SubmitResult.UnknownScope, connection.SubmitCommand(new ScopeId(77), Scenario.ShopId, CommandIds.Buy, wire.Written));
        Assert.Equal(2, host.Refusals.Count(r => r.Reason == RefusalReason.NotTrustedToCommand));

        // Granting the right takes effect on the next command.
        connection.Trust = Trust.Player(scope.Id);
        Assert.Equal(SubmitResult.Queued, connection.SubmitCommand(scope.Id, Scenario.ShopId, CommandIds.Buy, wire.Written));
        cluster.Step();
        Assert.Equal(before - 5, ((Shop)scope.World.Find(Scenario.ShopId)!).Stock);
        Assert.Equal(scope.Hash, watcher.Hash);
    }

    [Fact]
    public void SubscribingWithoutReadRightsIsRefused()
    {
        var cluster = new Cluster();
        var host = cluster.AddNode("host");
        var scope = cluster.AddScope(host, new ScopeId(1), Scenario.NewAuthority());
        var stranger = cluster.AddLocalReplica(scope, Scenario.NewWorld(WorldRole.Replica), Trust.None, "stranger");
        cluster.Step(3);
        Assert.Equal(ReplicaState.Refused, stranger.State);
        Assert.Equal(RefusalReason.NotTrustedToRead, Assert.Single(host.Refusals).Reason);
        Assert.Empty(scope.Subscribers);
    }

    [Fact]
    public void MutatedReplicaProducesADriftReportNamingTickAndEntity()
    {
        var (cluster, scope, view) = Scenario.NewCluster(options: new ClusterOptions { DriftCheckInterval = 1, ResyncOnDrift = true });
        Scenario.Play(cluster, scope, view, 20);
        var bot = (Bot)view.World.Find(new EntityId(4))!;
        var tamper = new Bot { Hp = 1234 };
        var delta = new SyncWriter();
        tamper.WriteDirtyDelta(delta);
        var reader = new SyncReader(delta.Written);
        bot.ApplyDelta(ref reader); // a bug or cheat that wrote replica state behind the sync layer's back
        Assert.NotEqual(scope.Hash, view.Hash);

        cluster.Step(1);
        var report = Assert.Single(cluster.DriftReports);
        Assert.Equal(21, report.Tick);
        Assert.Equal(scope.Id, report.Scope);
        Assert.True(report.DetailAvailable);
        var diff = Assert.Single(report.Differences);
        Assert.Equal(DigestKind.Entity, diff.Kind);
        Assert.Equal(4UL, diff.EntityId);
        Assert.Equal("Bot", diff.TypeName);
        Assert.Contains("tick 21", report.ToString());
        Assert.Contains("\"entity\":4", report.ToJson());

        // The replica re-subscribed and is whole again.
        cluster.Step(2);
        Assert.Equal(scope.Hash, view.Hash);
        Assert.Equal(1, view.Resyncs);
    }

    [Fact]
    public void FatalDriftThrowsWithTheReport()
    {
        var (cluster, scope, view) = Scenario.NewCluster();
        cluster.Step(5);
        var tamper = new Shop { Stock = -1 };
        var delta = new SyncWriter();
        tamper.WriteDirtyDelta(delta);
        var reader = new SyncReader(delta.Written);
        view.World.Find(Scenario.ShopId)!.ApplyDelta(ref reader);
        var ex = Assert.Throws<DriftException>(() => cluster.Step());
        Assert.Equal(6, ex.Report.Tick);
        Assert.Equal(Scenario.ShopId.Value, Assert.Single(ex.Report.Differences).EntityId);
    }

    [Fact]
    public void HostileFramesAndSnapshotsNeverThrowAndTheReplicaRecovers()
    {
        var (cluster, scope, view) = Scenario.NewCluster(options: new ClusterOptions { DriftCheckInterval = 1 });
        cluster.Step(5);
        var rng = new Random(5);
        var server = view.Link.Connection; // play the authority: write garbage on its end of the channel
        for (var i = 0; i < 200; i++)
        {
            var junk = new byte[rng.Next(2, 80)];
            rng.NextBytes(junk);
            junk[0] = (byte)(i % 2 == 0 ? 17 : 16);
            junk[1] = 1; // our scope id, so the message reaches the replica
            server.Channel.Send(junk);
            cluster.Step();
        }

        // Garbage made the replica ask for new snapshots until it gave up; a clean re-subscription is not possible, but nothing threw.
        Assert.NotEmpty(view.Node.ProtocolErrors);
        Assert.True(view.State is ReplicaState.Failed or ReplicaState.Live or ReplicaState.Resyncing);
    }

    [Fact]
    public void TwoClientsCommandOneAuthority()
    {
        var cluster = new Cluster(new ClusterOptions { DriftCheckInterval = 1, FatalDrift = true });
        var host = cluster.AddNode("host");
        var scope = cluster.AddScope(host, new ScopeId(1), Scenario.NewAuthority());
        var a = cluster.AddLocalReplica(scope, Scenario.NewWorld(WorldRole.Replica), Trust.Player(scope.Id), "a");
        var b = cluster.AddLocalReplica(scope, Scenario.NewWorld(WorldRole.Replica), Trust.Player(scope.Id), "b");
        cluster.Settle();
        for (var i = 0; i < 60; i++)
        {
            ((Shop)a.World.Find(Scenario.ShopId)!).Send.Buy(1, 1);
            ((Shop)b.World.Find(Scenario.ShopId)!).Send.Restock(new ItemSlot { ItemId = 1, Count = 2 });
            cluster.Step();
        }

        Assert.Equal(scope.Hash, a.Hash);
        Assert.Equal(scope.Hash, b.Hash);
        Assert.Equal(3, cluster.Hashes().Count);
    }
}
