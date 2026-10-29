using OneGame.Core;
using OneGame.Navigation;
using OneGame.Sync;

namespace OneGame.Navigation.Tests;

/// <summary>Drives a world's crowd through commands, the way a game does.</summary>
[Entity]
public partial class CrowdDirector : Entity
{
    [Synced] public partial int Spawned { get; private set; }

    [Command]
    public bool Spawn(int x, int y)
    {
        var crowd = NavCrowd.Of(World!)!;
        if (!crowd.TryAdd(crowd.Map.CellCenter(new NavCell(x, y)), out _))
        {
            return false;
        }

        Spawned++;
        return true;
    }

    [Command]
    public bool GoTo(int id, int x, int y)
    {
        var crowd = NavCrowd.Of(World!)!;
        return crowd.SetDestination(id, crowd.Map.CellCenter(new NavCell(x, y)));
    }

    [Command]
    public bool Leave(int id) => NavCrowd.Of(World!)!.Remove(id);
}

/// <summary>A crowd in the world: stepped by the tick, saved, hashed, replicated and replayed with it.</summary>
public class CrowdTests
{
    // A 20 x 12 room split by a wall with a two-cell gap: the crowd has to squeeze through.
    private const string Room = """
        ..........#.........
        ..........#.........
        ..........#.........
        ..........#.........
        ..........#.........
        ....................
        ....................
        ..........#.........
        ..........#.........
        ..........#.........
        ..........#.........
        ..........#.........
        """;

    private static readonly NavSpace Space = new(new FVec2(-160, -96), Fixed.FromInt(16));

    private static EntityRegistry Registry()
    {
        var registry = new EntityRegistry();
        OneGame.Generated.OneGame_Navigation_Tests.EntityTypes.Register(registry);
        return registry;
    }

    private static World NewWorld(WorldRole role = WorldRole.Authority, Visibility visibility = Visibility.Everyone)
    {
        var world = new World(11, Registry(), role);
        NavCrowd.AddTo(world, NavGrid.FromText(Room, Space), new NavCrowdOptions { MaxAgents = 64, Visibility = visibility });
        return world;
    }

    // Twelve agents in two columns on the left, all sent to the right side one tick apart.
    private static World Crossing(int agents = 12)
    {
        var world = NewWorld();
        var director = world.Add(new CrowdDirector());
        for (var i = 0; i < agents; i++)
        {
            director.Send.Spawn(1 + (i / 6), 3 + (i % 6));
        }

        world.Step();
        for (var i = 0; i < agents; i++)
        {
            director.Send.GoTo(i, 16 + (i / 6), 1 + ((i % 6) * 2));
        }

        return world;
    }

    private static NavCrowd CrowdOf(World world) => NavCrowd.Of(world)!;

    [Fact]
    public void TheCrowdCrossesTheGapAndEveryNodeComputesTheSameHashEveryTick()
    {
        ulong[] Run(out NavAgent[] final)
        {
            var world = Crossing();
            var hashes = new List<ulong>();
            for (var t = 0; t < 1200; t++)
            {
                world.Step();
                hashes.Add(world.ComputeHash());
            }

            final = CrowdOf(world).Agents.ToArray();
            return [.. hashes];
        }

        var a = Run(out var agentsA);
        var b = Run(out var agentsB);
        Assert.Equal(a, b);
        Assert.True(a.Distinct().Count() > 200, "the crowd really moved");
        Assert.Equal(agentsA, agentsB);
        Assert.Equal(12, agentsA.Length);
        // Everybody got through: right of the wall (cell 10 starts at x = 0), inside the map, and stopped near the goal.
        Assert.All(agentsA, ag =>
        {
            Assert.True(ag.Position.X > Fixed.FromInt(16), $"agent {ag.Id} at {ag.Position}");
            Assert.True(ag.Status is NavAgentStatus.Arrived or NavAgentStatus.ArrivedShort, $"agent {ag.Id}: {ag.Status}");
        });
    }

    [Theory]
    [InlineData(2)]
    [InlineData(90)]
    [InlineData(400)]
    public void ASnapshotTakenMidCrowdRestoresToTheSameFutureHashes(int snapshotTick)
    {
        var live = Crossing();
        for (var t = 0; t < snapshotTick; t++)
        {
            live.Step();
        }

        var w = new SyncWriter();
        live.WriteSnapshot(w);
        var restored = NewWorld();
        Assert.Equal(SyncError.None, restored.ReadSnapshot(w.Written));
        Assert.Equal(live.ComputeHash(), restored.ComputeHash());
        Assert.Equal(CrowdOf(live).StateHash(), CrowdOf(restored).StateHash());
        Assert.Equal(CrowdOf(live).Agents.ToArray(), CrowdOf(restored).Agents.ToArray());

        for (var t = 0; t < 700; t++)
        {
            live.Step();
            restored.Step();
            Assert.Equal(live.ComputeHash(), restored.ComputeHash());
        }
    }

    [Fact]
    public void TheCrowdIsPartOfTheWorldHash()
    {
        var a = Crossing();
        var b = Crossing();
        for (var t = 0; t < 30; t++)
        {
            a.Step();
            b.Step();
        }

        Assert.Equal(a.ComputeHash(), b.ComputeHash());
        Assert.Equal(CrowdOf(a).StateHash(), CrowdOf(b).StateHash());

        // A change that touches nothing but the crowd moves both hashes.
        var before = a.ComputeHash();
        Assert.True(CrowdOf(a).Stop(0));
        Assert.NotEqual(before, a.ComputeHash());
        Assert.NotEqual(CrowdOf(a).StateHash(), CrowdOf(b).StateHash());
    }

    [Fact]
    public void AReplicaFollowsTheAuthorityWithZeroDriftAndSeesTheSameAgents()
    {
        var cluster = new Cluster(new ClusterOptions { DriftCheckInterval = 1, FatalDrift = true });
        var host = cluster.AddNode("host");
        var scope = cluster.AddScope(host, new ScopeId(1), Crossing());
        var view = cluster.AddLocalReplica(scope, NewWorld(WorldRole.Replica), Trust.Player(scope.Id));
        cluster.Settle();

        for (var t = 0; t < 600; t++)
        {
            cluster.Step();
            Assert.Equal(scope.Hash, view.Hash);
        }

        Assert.Empty(cluster.DriftReports);
        Assert.Empty(cluster.CheckReplicas());
        Assert.Equal(CrowdOf(scope.World).Agents.ToArray(), CrowdOf(view.World).Agents.ToArray());
        Assert.Equal(12, CrowdOf(view.World).Count);
    }

    [Fact]
    public void ReplayingTheCommandLogRebuildsTheSameCrowd()
    {
        var cluster = new Cluster(new ClusterOptions { DriftCheckInterval = 1, FatalDrift = true });
        var host = cluster.AddNode("host");
        var scope = cluster.AddScope(host, new ScopeId(1), NewWorld());
        var director = scope.World.Add(new CrowdDirector());
        var recorder = scope.StartRecording(new RecordingOptions { CheckpointInterval = 10 });
        for (var i = 0; i < 6; i++)
        {
            director.Send.Spawn(1, 3 + i);
        }

        for (var t = 0; t < 500; t++)
        {
            cluster.Step();
            if (t == 1)
            {
                for (var i = 0; i < 6; i++)
                {
                    director.Send.GoTo(i, 17, 4 + i);
                }
            }

            if (t == 150)
            {
                director.Send.Leave(2);
                director.Send.Spawn(2, 5);
            }

            if (t == 160)
            {
                director.Send.GoTo(6, 3, 5);
            }
        }

        var fresh = NewWorld();
        var replay = Replayer.Replay(recorder.Stop(), fresh);
        Assert.True(replay.Ok, replay.Divergence?.ToJson());
        Assert.Equal(scope.Hash, fresh.ComputeHash());
        Assert.Equal(CrowdOf(scope.World).StateHash(), CrowdOf(fresh).StateHash());
        Assert.Equal(6, CrowdOf(fresh).Count);
    }

    [Fact]
    public void ACrossingHasTheSameStateHashesOnEveryPlatform()
    {
        // Known answers from the first run on x64; the same values came out of linux-arm64 under qemu. The crowd's maths is OnePath's (IEEE + - * / sqrt and its own
        // transcendentals, bit-identical everywhere) and the doubles become Fixed by one exact scaling and a rounding, so any platform that differs here has a real bug.
        var world = new World(1, new EntityRegistry());
        var map = NavGrid.FromText(Room, Space);
        var crowd = NavCrowd.AddTo(world, map, new NavCrowdOptions { AgentRadius = Fixed.FromInt(3), MaxAgents = 64 });
        for (var i = 0; i < 12; i++)
        {
            Assert.True(crowd.TryAdd(map.CellCenter(new NavCell(1 + (i / 6), 3 + (i % 6))), out _));
        }

        for (var i = 0; i < 12; i++)
        {
            crowd.SetDestination(i, map.CellCenter(new NavCell(16 + (i / 6), 1 + ((i % 6) * 2))));
        }

        for (var t = 1; t <= 600; t++)
        {
            world.Step();
            if (t == 100)
            {
                Assert.Equal(0xb4cffaa19531b3c2UL, crowd.StateHash());
                Assert.Equal(0x67a3fd9c01e68465UL, world.ComputeHash());
            }
        }

        Assert.Equal(0x27088758b6115415UL, crowd.StateHash());
        Assert.Equal(0x0b2ebe17ef29e770UL, world.ComputeHash());
    }

    [Fact]
    public void AgentsJoinLeaveAndAreNumberedForGood()
    {
        var world = NewWorld();
        var d = world.Add(new CrowdDirector());
        d.Send.Spawn(1, 1);
        d.Send.Spawn(2, 2);
        world.Step();
        var crowd = CrowdOf(world);
        Assert.Equal([0, 1], crowd.Agents.ToArray().Select(a => a.Id));
        d.Send.Leave(0);
        d.Send.Spawn(3, 3);
        world.Step();
        Assert.Equal([1, 2], crowd.Agents.ToArray().Select(a => a.Id).Order()); // the slot is reused, the number is not
        Assert.False(crowd.Remove(0));
        Assert.False(crowd.SetDestination(99, FVec2.Zero));
        Assert.True(crowd.TryGet(2, out var a2));
        Assert.Equal(crowd.Map.CellCenter(new NavCell(3, 3)), a2.Position);
    }

    [Fact]
    public void TheCrowdIsFullOrTheGroundIsMissingAndAddSaysSoInsteadOfThrowing()
    {
        var world = NewWorld();
        var crowd = CrowdOf(world);
        for (var i = 0; i < 64; i++)
        {
            Assert.True(crowd.TryAdd(crowd.Map.CellCenter(new NavCell(i % 8, i / 8)), out _));
        }

        Assert.False(crowd.TryAdd(crowd.Map.CellCenter(new NavCell(1, 1)), out var id));
        Assert.Equal(-1, id);
        Assert.Throws<NavRangeException>(() => crowd.SetDestination(0, new FVec2(Fixed.FromInt(4_000_000), Fixed.Zero)));
    }

    [Fact]
    public void AnAgentAddedInsideAWallIsPlacedOnTheNearestGroundNextToIt()
    {
        var crowd = CrowdOf(NewWorld());
        var wall = crowd.Map.CellCenter(new NavCell(10, 2));
        Assert.True(crowd.TryAdd(wall, out var id));
        Assert.True(crowd.TryGet(id, out var a));
        Assert.True(Math.Abs((a.Position.X - wall.X).ToDouble()) <= 16 && a.Position.X != wall.X || Math.Abs((a.Position.Y - wall.Y).ToDouble()) <= 16);
        Assert.True(crowd.Map.TryGetCell(a.Position, out var cell));
        Assert.False(crowd.Map.IsBlocked(cell.X, cell.Y));
    }

    [Fact]
    public void AReplicaRefusesToChangeItsCrowd()
    {
        var replica = NewWorld(WorldRole.Replica);
        Assert.Throws<ReplicaWriteException>(() => CrowdOf(replica).TryAdd(FVec2.Zero, out _));
    }

    [Fact]
    public void StepsAllocateNothingFromTheFirstTickAndHashesOnceTheBuffersHaveGrown()
    {
        var world = Crossing();
        world.Step();
        long worst = 0;
        var hashGrowths = 0;
        for (var i = 0; i < 900; i++)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            world.Step();
            worst = Math.Max(worst, GC.GetAllocatedBytesForCurrentThread() - before);
            var mid = GC.GetAllocatedBytesForCurrentThread();
            world.ComputeHash();
            if (GC.GetAllocatedBytesForCurrentThread() != mid && i >= 300)
            {
                hashGrowths++;
            }
        }

        // The world's hash buffer grows with the table while the crowd's state grows (corridors, wall caches), then stays; the crowd itself never allocates.
        TestContext.Current.SendDiagnosticMessage($"crowd of {CrowdOf(world).Count}: Step max {worst} B/tick over 900 ticks; hash allocations after tick 300: {hashGrowths}");
        Assert.Equal(0, worst);
        Assert.Equal(0, hashGrowths);
    }

    [Fact]
    public void HostileCrowdBytesAreRefusedWithoutThrowingAndLeaveTheCrowdAlone()
    {
        var world = Crossing(6);
        for (var t = 0; t < 40; t++)
        {
            world.Step();
        }

        var crowd = CrowdOf(world);
        var good = new SyncWriter();
        crowd.Write(good);
        var bytes = good.ToArray();
        var target = NewWorld();
        var other = CrowdOf(target);
        var rng = new Random(5);
        var accepted = 0;
        var refused = 0;
        for (var i = 0; i < 3000; i++)
        {
            var mutated = (byte[])bytes.Clone();
            for (var k = 0; k < 1 + (i % 3); k++)
            {
                mutated[rng.Next(mutated.Length)] = (byte)rng.Next(256);
            }

            if (i % 7 == 0)
            {
                Array.Resize(ref mutated, rng.Next(mutated.Length));
            }

            var held = other.StateHash();
            var reader = new SyncReader(mutated);
            other.Read(ref reader);
            if (reader.Ok)
            {
                accepted++;
                // What was accepted must also survive being stepped and written.
                other.Step();
                other.Write(new SyncWriter());
                var restored = new SyncReader(bytes);
                other.Read(ref restored);
            }
            else
            {
                refused++;
                Assert.Equal(held, other.StateHash());
            }
        }

        TestContext.Current.SendDiagnosticMessage($"hostile crowd bytes: {refused} refused, {accepted} accepted and stepped");
        Assert.True(refused > 1000);
    }

    [Fact]
    public void ACrowdStateTooBigForItsLimitIsRefusedBeforeItIsRead()
    {
        var w = new SyncWriter();
        w.WriteByte(1);
        w.WriteVarUInt(100_000_000);
        var reader = new SyncReader(w.Written);
        CrowdOf(NewWorld()).Read(ref reader);
        Assert.False(reader.Ok);
    }

    [Fact]
    public void AHiddenCrowdStaysOnTheAuthority()
    {
        var cluster = new Cluster(new ClusterOptions { DriftCheckInterval = 1, FatalDrift = true });
        var host = cluster.AddNode("host");
        var scope = cluster.AddScope(host, new ScopeId(1), NewWorld(visibility: Visibility.Hidden));
        var director = scope.World.Add(new CrowdDirector());
        director.Send.Spawn(1, 1);
        var view = cluster.AddLocalReplica(scope, NewWorld(WorldRole.Replica, Visibility.Hidden), Trust.Player(scope.Id));
        cluster.Settle();
        for (var t = 0; t < 20; t++)
        {
            cluster.Step();
        }

        Assert.Equal(1, CrowdOf(scope.World).Count);
        Assert.Equal(0, CrowdOf(view.World).Count);
        Assert.Empty(cluster.DriftReports); // the drift contract is the view hash, which leaves the hidden table out
        Assert.Empty(cluster.CheckReplicas());
    }

    [Fact]
    public void AStateSavedByAnOlderMapStampLoadsAndSearchesAgain()
    {
        // 1.12 stored its own hash of the map in NavState.Stamp; a different stamp means "the map changed", so a path from a save restarts from where the unit is.
        var map = NavGrid.FromText("""
            ........
            ..####..
            ........
            """);
        var searcher = new NavSearcher();
        var state = searcher.Begin(map, new NavCell(0, 1), new NavCell(7, 1));
        searcher.Advance(ref state, map, 1000);
        Assert.Equal(NavStatus.Found, state.Status);
        var old = new NavState(NavStatus.Found, default, state.Start, state.Goal, 0, 1, 0x1234UL, state.Cells.ToArray());
        Assert.NotEqual(map.Stamp, old.Stamp);

        var later = new NavSearcher();
        Assert.True(later.Advance(ref old, map, 1));
        Assert.Equal(NavStatus.Searching, old.Status);
        later.Advance(ref old, map, 1000);
        Assert.Equal(NavStatus.Found, old.Status);
        Assert.Equal(state.Cells.ToArray(), old.Cells.ToArray());
    }

    [Fact]
    public void TheMapStampIsOnePathsContentHash()
    {
        var map = NavGrid.FromText("..#.");
        Assert.Equal(map.Grid.ContentHash(), map.Stamp);
        map.SetBlocked(0, 0, true);
        Assert.Equal(map.Grid.ContentHash(), map.Stamp);
    }
}
