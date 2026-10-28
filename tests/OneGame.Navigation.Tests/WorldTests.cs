using OneGame.Core;
using OneGame.Navigation;
using OneGame.Sync;

namespace OneGame.Navigation.Tests;

/// <summary>Navigation state inside a world: hashed, saved, restored, replicated, and free of per-tick allocation when nothing changes.</summary>
[Collection("Level")]
public class WorldTests
{
    private static EntityRegistry Registry()
    {
        var registry = new EntityRegistry();
        OneGame.Generated.OneGame_Navigation_Tests.EntityTypes.Register(registry);
        return registry;
    }

    private static void UseLevel(int budget)
    {
        Level.Map = Maps.Serpentine(40, 21);
        Level.Budget = budget;
        Level.Speed = Fixed.FromRatio(3, 4);
    }

    private static World NewWorld(WorldRole role = WorldRole.Authority) => new(7, Registry(), role);

    private static World Populated()
    {
        var world = NewWorld();
        var w = world.Add(new Walker());
        w.Send.Place(0, 0);
        world.Step();
        w.Send.GoTo(39, 20);
        return world;
    }

    [Fact]
    public void TwoRunsGiveTheSameHashAtEveryTickAndTheUnitArrives()
    {
        UseLevel(60);
        ulong[] Run()
        {
            var world = Populated();
            var hashes = new List<ulong>();
            for (var t = 0; t < 700; t++)
            {
                world.Step();
                hashes.Add(world.ComputeHash());
            }

            var walker = (Walker)world.Entities[0];
            Assert.Equal(NavStatus.Arrived, walker.Nav.Status);
            Assert.Equal(Level.Map.CellCenter(new NavCell(39, 20)), walker.Position);
            return [.. hashes];
        }

        var a = Run();
        var b = Run();
        Assert.Equal(a, b);
        Assert.True(a.Distinct().Count() > 100, "the state really changed over the run");
    }

    [Theory]
    [InlineData(3)]
    [InlineData(11)]
    [InlineData(40)]
    public void ASnapshotTakenAnywhereRestoresToTheSameFutureHashes(int snapshotTick)
    {
        UseLevel(25);
        var live = Populated();
        for (var t = 0; t < snapshotTick; t++)
        {
            live.Step();
        }

        var walker = (Walker)live.Entities[0];
        if (snapshotTick == 3)
        {
            Assert.Equal(NavStatus.Searching, walker.Nav.Status); // the snapshot lands inside a running search
        }

        var w = new SyncWriter();
        live.WriteSnapshot(w);
        var restored = NewWorld();
        Assert.Equal(SyncError.None, restored.ReadSnapshot(w.Written));
        Assert.Equal(live.ComputeHash(), restored.ComputeHash());

        for (var t = 0; t < 900; t++)
        {
            live.Step();
            restored.Step();
            Assert.Equal(live.ComputeHash(), restored.ComputeHash());
        }

        Assert.Equal(NavStatus.Arrived, ((Walker)restored.Entities[0]).Nav.Status);
    }

    [Fact]
    public void AReplicaFollowsTheAuthorityWithZeroDriftAndSeesTheSamePath()
    {
        UseLevel(50);
        var cluster = new Cluster(new ClusterOptions { DriftCheckInterval = 1, FatalDrift = true });
        var host = cluster.AddNode("host");
        var scope = cluster.AddScope(host, new ScopeId(1), Populated());
        var view = cluster.AddLocalReplica(scope, NewWorld(WorldRole.Replica), Trust.Player(scope.Id));
        cluster.Settle();

        for (var t = 0; t < 900; t++)
        {
            cluster.Step();
            Assert.Equal(scope.Hash, view.Hash);
        }

        Assert.Empty(cluster.DriftReports);
        Assert.Empty(cluster.CheckReplicas());
        var a = (Walker)scope.World.Entities[0];
        var r = (Walker)view.World.Entities[0];
        Assert.Equal(NavStatus.Arrived, r.AsReadOnly.Nav.Status);
        Assert.Equal(a.Nav, r.AsReadOnly.Nav);
        Assert.Equal(a.Position, r.AsReadOnly.Position);
    }

    [Fact]
    public void ChangingTheMapMidWalkReroutesAndEveryNodeReroutesTheSame()
    {
        UseLevel(1000);
        Level.Map = NavGrid.FromText("""
            ..........
            ..........
            ..........
            ..........
            """);
        var a = NewWorld();
        var b = NewWorld();
        foreach (var world in new[] { a, b })
        {
            var w = world.Add(new Walker());
            w.Send.Place(0, 1);
            world.Step();
            w.Send.GoTo(9, 1);
        }

        for (var t = 0; t < 400; t++)
        {
            a.Step();
            b.Step();
            if (t == 6)
            {
                // The same edit at the same tick on both nodes: a wall drops across the corridor ahead of the unit.
                Level.Map.SetRect(7, 0, 1, 3, 255);
            }

            Assert.Equal(a.ComputeHash(), b.ComputeHash());
        }

        var walker = (Walker)a.Entities[0];
        Assert.Equal(NavStatus.Arrived, walker.Nav.Status);
        Assert.Equal(Level.Map.CellCenter(new NavCell(9, 1)), walker.Position);
    }

    [Fact]
    public void IdleWalkingAndSearchingTicksAllocateNothing()
    {
        UseLevel(20);
        var world = Populated();
        var walker = (Walker)world.Entities[0];

        // Warm the buffers (hash, command queue) and let the first search tick build its engine, then measure.
        for (var i = 0; i < 4; i++)
        {
            world.Step();
            world.ComputeHash();
        }

        var perTick = new List<long>();
        while (walker.Nav.Status == NavStatus.Searching)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            world.Step();
            world.ComputeHash();
            perTick.Add(GC.GetAllocatedBytesForCurrentThread() - before);
        }

        // A tick that stays in the search allocates nothing (the sliced search reuses its buffers). The tick that finishes copies the path out, once per search.
        TestContext.Current.SendDiagnosticMessage($"searching: {perTick.Count} ticks, in-search ticks {string.Join("/", perTick.Take(perTick.Count - 1).Distinct())} B, finishing tick {perTick[^1]} B");
        Assert.True(perTick.Count > 15);
        Assert.All(perTick.Take(perTick.Count - 1), b => Assert.Equal(0, b));

        // Walking: the unit moves, its state is re-assigned, nothing is allocated.
        world.Step();
        world.ComputeHash();
        var walkBefore = GC.GetAllocatedBytesForCurrentThread();
        var walked = 0;
        while (walker.Nav.Status == NavStatus.Found)
        {
            world.Step();
            world.ComputeHash();
            walked++;
        }

        var walkBytes = GC.GetAllocatedBytesForCurrentThread() - walkBefore;
        TestContext.Current.SendDiagnosticMessage($"walking: {walked} ticks, {walkBytes} B");
        Assert.True(walked > 100);
        Assert.Equal(0, walkBytes);

        // Idle: arrived, nothing to do.
        var idleBefore = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++)
        {
            world.Step();
            world.ComputeHash();
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - idleBefore);
    }
}
