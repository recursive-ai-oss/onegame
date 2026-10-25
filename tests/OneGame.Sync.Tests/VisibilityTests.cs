namespace OneGame.Sync.Tests;

public class VisibilityTests
{
    private static Trust Ada(Rig rig) => Trust.Player(rig.Scope.Id).As(1, 10);

    private static Trust Bo(Rig rig) => Trust.Player(rig.Scope.Id).As(2, 20);

    [Fact]
    public void OwnerOnlyMembersReachTheirOwnerAndNobodyElse()
    {
        var rig = new Rig();
        var ada = rig.Join("ada", Ada(rig));
        var bo = rig.Join("bo", Bo(rig));
        var stranger = rig.Join("stranger", Trust.Reader(rig.Scope.Id));
        rig.StepChecked(40, ada, bo, stranger);

        // The bytes that crossed each channel: a unique string in an owner-only member shows up only on its owner's.
        Assert.True(ada.Tap.Contains("ruby-hoard-7731"));
        Assert.False(bo.Tap.Contains("ruby-hoard-7731"));
        Assert.False(stranger.Tap.Contains("ruby-hoard-7731"));
        Assert.True(bo.Tap.Contains("emerald-cache-2208"));
        Assert.False(ada.Tap.Contains("emerald-cache-2208"));
        Assert.False(stranger.Tap.Contains("emerald-cache-2208"));

        // And the replicas agree: the owner's gold and hand arrived, everyone else holds the defaults.
        Assert.True(Rig.HeroOf(ada.Replica, 1).Gold > 40);
        Assert.Equal("ruby-hoard-7731", Rig.HeroOf(ada.Replica, 1).Stash);
        Assert.NotEmpty(Rig.HeroOf(ada.Replica, 1).Hand);
        Assert.Equal(0, Rig.HeroOf(ada.Replica, 2).Gold);
        Assert.Equal(string.Empty, Rig.HeroOf(ada.Replica, 2).Stash);
        Assert.Empty(Rig.HeroOf(ada.Replica, 2).Hand);
        Assert.Equal(0, Rig.HeroOf(stranger.Replica, 1).Gold);
        Assert.Equal(string.Empty, Rig.HeroOf(stranger.Replica, 1).Stash);

        // Public members reach everyone, whoever they are.
        Assert.Equal("Ada", Rig.HeroOf(stranger.Replica, 1).Name);
        Assert.Equal(Rig.HeroOf(rig.Scope, 2).Position, Rig.HeroOf(ada.Replica, 2).Position);
        Assert.Empty(rig.Cluster.DriftReports);
        Assert.Empty(rig.Cluster.CheckReplicas());
    }

    [Fact]
    public void TeamMembersSeeTeamDataAndHiddenNeverLeavesTheAuthority()
    {
        var rig = new Rig();
        var ada = rig.Join("ada", Ada(rig));
        var cara = rig.Join("cara", Trust.Player(rig.Scope.Id).As(3, 20));
        rig.StepChecked(30, ada, cara);

        // Team-visible member: own team and owner only.
        Assert.NotEqual(0, Rig.HeroOf(ada.Replica, 1).Stamina);
        Assert.Equal(0, Rig.HeroOf(ada.Replica, 2).Stamina);
        Assert.NotEqual(0, Rig.HeroOf(cara.Replica, 2).Stamina); // cara is on bo's team without owning anything
        Assert.Equal(0, Rig.HeroOf(cara.Replica, 1).Stamina);
        Assert.Equal(string.Empty, Rig.HeroOf(cara.Replica, 2).Stash); // owner-only stays with bo

        // Team-visible entity: only its team gets the entity at all.
        Assert.Equal(1, Rig.Count<Intel>(ada.Replica));
        Assert.Equal(1, Rig.Count<Intel>(cara.Replica));
        Assert.Equal(10u, ada.Replica.World.Entities.OfType<Intel>().Single().Team);
        Assert.Equal(20u, cara.Replica.World.Entities.OfType<Intel>().Single().Team);

        // Owner-visible entity: the diary exists only for its owner.
        Assert.Equal(1, Rig.Count<Diary>(ada.Replica));
        Assert.Equal(0, Rig.Count<Diary>(cara.Replica));
        Assert.True(ada.Tap.Contains("ada-diary-secret"));
        Assert.False(cara.Tap.Contains("ada-diary-secret"));
        Assert.False(cara.Tap.Contains("bo-diary-secret"));

        // Hidden entities, hidden members and hidden tables are on no replica and in no byte.
        Assert.Equal(0, Rig.Count<Director>(ada.Replica));
        Assert.Equal(0, Rig.Count<Director>(cara.Replica));
        Assert.Equal(0, Rig.HeroOf(ada.Replica, 1).Plot);
        Assert.NotEqual(0, Rig.HeroOf(rig.Scope, 1).Plot);
        Assert.Equal(0, ada.Replica.World.Tables[0].Count);
        Assert.Equal(1, rig.Scope.World.Tables[0].Count);
        Assert.Empty(rig.Cluster.DriftReports);
    }

    [Fact]
    public void ReplicasThatSeeEverythingStillMatchTheAuthorityHash()
    {
        // With no restricted data the view hash is the plain hash: filtering costs nothing in the common case.
        var (cluster, scope, view) = Scenario.NewCluster();
        cluster.Step(50);
        Assert.Equal(scope.Hash, view.Hash);
        Assert.Equal(scope.Hash, view.ViewHash);
        Assert.Equal(scope.Hash, scope.ViewHashFor(view.Link.Connection));
    }

    [Fact]
    public void ChangingTheTrustMidRunAddsAndRemovesDataAndStaysInSync()
    {
        var rig = new Rig();
        var ada = rig.Join("ada", Ada(rig));
        rig.StepChecked(20, ada);
        Assert.Equal(0, Rig.HeroOf(ada.Replica, 2).Stamina);
        Assert.Equal(10u, ada.Replica.World.Entities.OfType<Intel>().Single().Team);

        // Ada changes sides: she gains team 20's data and loses team 10's entity.
        ada.Connection.Trust = Trust.Player(rig.Scope.Id).As(1, 20);
        rig.StepChecked(1, ada);
        Assert.NotEqual(0, Rig.HeroOf(ada.Replica, 2).Stamina);
        Assert.Equal(20u, ada.Replica.World.Entities.OfType<Intel>().Single().Team);
        Assert.NotEqual(0, Rig.HeroOf(ada.Replica, 1).Stamina); // she still owns her own hero
        rig.StepChecked(60, ada);

        // She gives up her player id: owner-only data of her own hero goes away.
        Assert.True(Rig.HeroOf(ada.Replica, 1).Gold > 0);
        ada.Connection.Trust = Trust.Player(rig.Scope.Id).As(0, 20);
        rig.StepChecked(30, ada);
        Assert.Equal(0, Rig.HeroOf(ada.Replica, 1).Gold);
        Assert.Equal(string.Empty, Rig.HeroOf(ada.Replica, 1).Stash);
        Assert.Equal(0, Rig.Count<Diary>(ada.Replica));

        // And takes it back.
        ada.Connection.Trust = Trust.Player(rig.Scope.Id).As(1, 20);
        rig.StepChecked(30, ada);
        Assert.True(Rig.HeroOf(ada.Replica, 1).Gold > 0);
        Assert.Equal(1, Rig.Count<Diary>(ada.Replica));
        Assert.Empty(rig.Cluster.DriftReports);
        Assert.Empty(rig.Cluster.CheckReplicas());
    }

    [Fact]
    public void ChangingWhoOwnsAnEntityMovesItsPrivateDataBetweenSubscribers()
    {
        var rig = new Rig();
        var ada = rig.Join("ada", Ada(rig));
        var bo = rig.Join("bo", Bo(rig));
        rig.StepChecked(10, ada, bo);
        Assert.Equal("ruby-hoard-7731", Rig.HeroOf(ada.Replica, 1).Stash);

        // The authority hands the hero to player 2 (a game rule, not a trust change).
        Rig.HeroOf(rig.Scope, 1).Player = 2;
        rig.StepChecked(5, ada, bo);
        var moved = bo.Replica.World.Entities.OfType<Hero>().Single(h => h.Name == "Ada");
        Assert.Equal("ruby-hoard-7731", moved.Stash);
        var lost = ada.Replica.World.Entities.OfType<Hero>().Single(h => h.Name == "Ada");
        Assert.Equal(string.Empty, lost.Stash);
        Assert.Equal(0, lost.Gold);
        Assert.Empty(rig.Cluster.DriftReports);
    }

    [Fact]
    public void RandomStreamsCanBeKeptOnTheAuthority()
    {
        var open = new Rig();
        var seen = open.Join("seen", Trust.Reader(open.Scope.Id));
        open.StepChecked(30, seen);
        Assert.True(seen.Replica.World.Rng.Count > 0);

        var secret = new Rig();
        secret.Scope.RandomStreams = Visibility.Hidden;
        var blind = secret.Join("blind", Trust.Reader(secret.Scope.Id));
        secret.StepChecked(60, blind);
        Assert.Equal(0, blind.Replica.World.Rng.Count);
        Assert.NotEqual(0, secret.Scope.World.Rng.Count);
        Assert.Empty(secret.Cluster.DriftReports);

        // Flipping it live resends snapshots and the replicas stay in step.
        secret.Scope.RandomStreams = Visibility.Everyone;
        secret.StepChecked(10, blind);
        Assert.True(blind.Replica.World.Rng.Count > 0);
        Assert.Throws<ArgumentException>(() => secret.Scope.RandomStreams = Visibility.Owner);
    }

    [Fact]
    public void TamperingWithAVisiblePropertyOnAFilteredReplicaIsCaughtAndNamed()
    {
        var rig = new Rig(new ClusterOptions { DriftCheckInterval = 1, ResyncOnDrift = true });
        var ada = rig.Join("ada", Ada(rig));
        rig.StepChecked(10, ada);
        var hero = Rig.HeroOf(ada.Replica, 2);
        var tamper = new Hero { Position = new FVec2(99, 99) };
        var delta = new SyncWriter();
        tamper.WriteMaskedDelta(delta, 1UL << 3); // Position
        var reader = new SyncReader(delta.Written);
        hero.ApplyDelta(ref reader);

        rig.Cluster.Step();
        var report = Assert.Single(rig.Cluster.DriftReports);
        Assert.True(report.DetailAvailable);
        var difference = Assert.Single(report.Differences);
        Assert.Equal("Hero", difference.TypeName);
        Assert.Equal(hero.Id.Value, difference.EntityId);
        rig.StepChecked(3, ada);
        Assert.Equal(Rig.HeroOf(rig.Scope, 2).Position, Rig.HeroOf(ada.Replica, 2).Position);
    }

    [Fact]
    public void ADeltaOutsideTheViewIsRefusedByTheReplica()
    {
        // A misbehaving authority tries to push a property the replica was never allowed to hold.
        var rig = new Rig(new ClusterOptions { DriftCheckInterval = 0 });
        var ada = rig.Join("ada", Ada(rig));
        rig.StepChecked(3, ada);
        var bosHero = Rig.HeroOf(ada.Replica, 2);

        var body = new SyncWriter();
        var foreign = new Hero { Gold = 5 };
        foreign.WriteMaskedDelta(body, 1UL << 5); // Gold: owner-only, and Ada does not own this hero
        var frame = new SyncWriter();
        frame.WriteByte(17);
        frame.WriteVarUInt(rig.Scope.Id.Value);
        frame.WriteVarInt(ada.Replica.LastTick + 1);
        frame.WriteByte(0);
        frame.WriteVarUInt(0); // removed
        frame.WriteVarUInt(0); // added
        frame.WriteVarUInt(1); // deltas
        frame.WriteVarUInt(bosHero.Id.Value);
        frame.WriteBytes(body.Written);
        frame.WriteVarUInt(0); // tables
        ada.Connection.Channel.Send(frame.Written);
        rig.Cluster.Settle();

        Assert.Equal(0, bosHero.Gold);
        Assert.NotEmpty(ada.Node.ProtocolErrors);
    }
}
