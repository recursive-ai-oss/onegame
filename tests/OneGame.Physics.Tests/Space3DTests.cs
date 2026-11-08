using OneGame.Core;
using OneGame.Sync;

namespace OneGame.Physics.Tests;

public class Space3DTests
{
    private static World Scene(out PhysicsSpace3D space, out BodyRef box)
    {
        var world = new World(5, new EntityRegistry());
        space = PhysicsSpace3D.AddTo(world);
        space.AddBox(new FVec3(40, 1, 40), new FVec3(0, -1, 0), new BodyOptions3D { Kind = BodyKind.Static });
        box = space.AddBox(new FVec3(1, 1, 1), new FVec3(0, 4, 0));
        for (var i = 0; i < 3; i++) space.AddSphere(Fixed.FromRatio(1, 2), new FVec3(3, 2 + i * 2, 0));
        return world;
    }

    [Fact]
    public void EveryTickIsIdenticalAcrossTwoRunsAndMidFallRestore()
    {
        var a = Scene(out var sa, out var box);
        var b = Scene(out var sb, out _);
        for (var i = 0; i < 180; i++)
        {
            if (i == 40)
            {
                var saved = new SyncWriter(); a.WriteSnapshot(saved);
                Assert.Equal(SyncError.None, b.ReadSnapshot(saved.Written));
            }
            a.Step(); b.Step();
            Assert.Equal(a.ComputeHash(), b.ComputeHash());
            Assert.Equal(sa.Hash, sb.Hash);
        }
        Assert.InRange(sa.Position(box).Y.Raw, Fixed.FromInt(-1).Raw, Fixed.One.Raw);
    }

    [Fact]
    public void FullDeltaCompressedAndRegionRoundTrip()
    {
        _ = Scene(out var space, out var box);
        for (var i = 0; i < 30; i++) space.Step();
        var baseline = space.Snapshot();
        for (var i = 0; i < 10; i++) space.Step();
        var copy = space.Fork();
        copy.Restore(baseline);
        copy.RestoreDelta(baseline, space.SnapshotDelta(baseline));
        Assert.Equal(space.Hash, copy.Hash);
        copy.Restore(baseline); copy.RestoreCompressed(space.SnapshotCompressed());
        Assert.Equal(space.Hash, copy.Hash);
        var region = space.SnapshotRegion(new FVec3(-10, -10, -10), new FVec3(10, 10, 10));
        copy.SetVelocity(box, new FVec3(2, 0, 0));
        Assert.NotEqual(space.Hash, copy.Hash);
        copy.RestoreRegion(region);
        Assert.Equal(space.Position(box), copy.Position(box));
        Assert.Equal(space.Velocity(box), copy.Velocity(box));
        // Region restore refreshes broadphase proxies rather than importing their history.
        space.RestoreRegion(region);
        Assert.Equal(space.Hash, copy.Hash);
        Assert.Equal(space.Snapshot(), copy.Snapshot());
    }

    [Fact]
    public void ForkPredictionRewindsAndResimulatesWithNoDrift()
    {
        _ = Scene(out var authority, out var box);
        var predicted = authority.Fork();
        var confirmed = authority.Snapshot();
        predicted.ApplyImpulse(box, new FVec3(9, 0, 0));
        for (var i = 0; i < 20; i++) predicted.Step(resimulating: true);
        predicted.Restore(confirmed);
        for (var i = 0; i < 120; i++)
        {
            if (i == 10) { authority.ApplyImpulse(box, new FVec3(1, 0, 0)); predicted.ApplyImpulse(box, new FVec3(1, 0, 0)); }
            authority.Step(); predicted.Step(resimulating: true);
            Assert.Equal(authority.Hash, predicted.Hash);
        }
    }

    [Fact]
    public void EventsHaveIdenticalOrderAndSurviveRestore()
    {
        string Run(bool restore)
        {
            _ = Scene(out var space, out _);
            space.AddBox(new FVec3(8, 1, 8), new FVec3(0, 2, 0), new BodyOptions3D { Kind = BodyKind.Static, IsTrigger = true });
            var log = new List<string>();
            for (var i = 0; i < 240; i++)
            {
                if (restore && i == 40) space.Restore(space.Snapshot());
                space.Step();
                foreach (var e in space.Events) log.Add($"{i}:{e.Kind}:{e.A}:{e.B}");
            }
            return string.Join("|", log);
        }
        var log = Run(false);
        Assert.Equal(log, Run(true));
        Assert.Contains("TriggerEnter", log); Assert.Contains("TriggerExit", log); Assert.Contains("ContactBegin", log);
    }

    [Fact]
    public void RebaseBeforeCrossingLocalLimitPreservesTwoNodeHashes()
    {
        var options = new PhysicsOptions3D { Gravity = FVec3.Zero };
        var a = new PhysicsSpace3D(options); var b = new PhysicsSpace3D(options);
        var at = new FVec3(PhysicsLimits.MaxCoordinate - 2, 0, 0);
        var body = a.AddSphere(Fixed.One, at); b.AddSphere(Fixed.One, at);
        for (var i = 0; i < 3; i++)
        {
            var offset = new FVec3(PhysicsLimits.MaxCoordinate - 4, 0, 0);
            a.ShiftOrigin(offset); b.ShiftOrigin(offset);
            a.Teleport(body, at); b.Teleport(body, at);
            a.Step(); b.Step(); Assert.Equal(a.Hash, b.Hash);
            var restored = new PhysicsSpace3D(options); restored.Restore(a.Snapshot());
            Assert.Equal(a.Hash, restored.Hash);
            Assert.Equal(a.Origin, restored.Origin);
        }
    }

    [Fact]
    public void DifferenceReportNamesBodyAndField()
    {
        _ = Scene(out var a, out var body); var b = a.Fork();
        b.SetVelocity(body, new FVec3(1, 0, 0));
        var report = a.FindDifferences(b);
        Assert.Contains($"body {body.Index}", report); Assert.Contains("differing fields", report);
    }

    [Fact]
    public void CharactersVehiclesAndQueriesWork()
    {
        _ = Scene(out var space, out _);
        var character = space.AddCharacter(new FVec3(-4, 2, 0));
        var chassis = space.AddBox(new FVec3(2, Fixed.FromRatio(1, 2), 4), new FVec3(8, 1, 0), new BodyOptions3D { Density = 100 });
        var car = space.AddVehicle(chassis);
        for (var i = 0; i < 180; i++)
        {
            space.Move(character, new FVec3(Fixed.FromRatio(1, 100), Fixed.FromRatio(-1, 10), 0));
            space.Drive(car, Fixed.One); space.Step();
        }
        Assert.True(space.Grounded(character)); Assert.True(space.Position(character).X > Fixed.FromInt(-3));
        Assert.Equal(4, space.WheelCount(car)); Assert.True(space.Wheel(car, 0).Radius > Fixed.Zero);
        Assert.True(space.Position(chassis).Z > Fixed.One);
        Assert.True(space.Raycast(new FVec3(-8, 4, 0), -FVec3.UnitY, 10, out var ray));
        Assert.True(ray.Distance > Fixed.FromInt(4));
        Assert.True(space.SphereCast(new FVec3(-8, 4, 0), Fixed.One, -FVec3.UnitY, 10, out _));
        Assert.True(space.OverlapSphere(new FVec3(-8, -1, 0), Fixed.One, out _));
    }

    [Theory]
    [InlineData(20)]
    [InlineData(240)]
    public void WarmedStepsAllocateNothing(int warmTicks)
    {
        var world = Scene(out var space, out _);
        for (var i = 0; i < warmTicks; i++) world.Step();
        space.Warmup();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 120; i++) { world.Step(); _ = world.ComputeHash(); }
        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    [Fact]
    public void MalformedTableBytesAreRejectedAtomically()
    {
        _ = Scene(out var space, out _); space.Step(); var hash = space.Hash;
        var writer = new SyncWriter(); writer.WriteByte(1); writer.WriteInt32(60); writer.WriteBytes(new byte[] { 1, 2, 3 });
        var reader = new SyncReader(writer.Written); space.Read(ref reader);
        Assert.False(reader.Ok); Assert.Equal(hash, space.Hash);
        Assert.Throws<ArgumentOutOfRangeException>(() => space.AddSphere(Fixed.One, new FVec3(PhysicsLimits.MaxCoordinate, 0, 0)));
    }
}
