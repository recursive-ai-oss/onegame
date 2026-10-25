using OneGame.Core.Tests.Samples;

namespace OneGame.Core.Tests;

public class Transform3Tests
{
    private static EntityRegistry Registry()
    {
        var registry = new EntityRegistry();
        OneGame.Generated.OneGame_Core_Tests.EntityTypes.Register(registry);
        return registry;
    }

    private static World NewWorld(ulong seed = 11)
    {
        var world = new World(seed, Registry());
        world.Add(new Probe
        {
            Pose = new Transform3(new FVec3(1, 5, -2), FQuat.Identity, new FVec3(1, 2, 1)),
            Velocity = new FVec3(Fixed.FromRatio(1, 32), Fixed.Zero, Fixed.FromRatio(-1, 64)),
            Spin = FQuat.FromYawPitchRoll(Fixed.FromRatio(1, 50), Fixed.FromRatio(1, 80), Fixed.FromRatio(1, 120)),
        });
        return world;
    }

    private static void AssertNear(double expected, Fixed actual, double tolerance = 1e-6) => Assert.InRange(actual.ToDouble(), expected - tolerance, expected + tolerance);

    [Fact]
    public void DefaultIsTheIdentityTransform()
    {
        var t = default(Transform3);
        Assert.Equal(FVec3.Zero, t.Position);
        Assert.Equal(FVec3.One, t.Scale);
        Assert.Equal(FQuat.Identity, t.Rotation);
        Assert.Equal(FVec3.One, t.TransformPoint(FVec3.One));
        Assert.Equal(new FVec3(0, 0, -1), t.Forward);
        Assert.Equal(Transform3.Identity, new Transform3(FVec3.Zero, FQuat.Identity, FVec3.One).WithScale(FVec3.One));
    }

    [Fact]
    public void RotationsTurnAndComposeAsRightHanded()
    {
        var quarter = FQuat.FromAxisAngle(FVec3.UnitY, Fixed.HalfPi);
        var v = quarter.Rotate(new FVec3(0, 0, -1));
        AssertNear(-1, v.X);
        AssertNear(0, v.Y);
        AssertNear(0, v.Z);

        // a * b applies b first: turn about X then about Y.
        var both = FQuat.FromAxisAngle(FVec3.UnitY, Fixed.HalfPi) * FQuat.FromAxisAngle(FVec3.UnitX, Fixed.HalfPi);
        var w = both.Rotate(FVec3.UnitZ);
        var step = FQuat.FromAxisAngle(FVec3.UnitY, Fixed.HalfPi).Rotate(FQuat.FromAxisAngle(FVec3.UnitX, Fixed.HalfPi).Rotate(FVec3.UnitZ));
        AssertNear(step.X.ToDouble(), w.X);
        AssertNear(step.Y.ToDouble(), w.Y);
        AssertNear(step.Z.ToDouble(), w.Z);

        var back = both.Inverse().Rotate(w);
        AssertNear(0, back.X);
        AssertNear(0, back.Y);
        AssertNear(1, back.Z);
    }

    [Fact]
    public void LookAtFacesTheTarget()
    {
        var eye = new FVec3(3, 2, 8);
        var target = new FVec3(-1, 1, 0);
        var t = Transform3.LookAt(eye, target);
        var want = (target - eye).Normalized();
        AssertNear(want.X.ToDouble(), t.Forward.X, 1e-5);
        AssertNear(want.Y.ToDouble(), t.Forward.Y, 1e-5);
        AssertNear(want.Z.ToDouble(), t.Forward.Z, 1e-5);
        Assert.True(t.Up.Y > Fixed.Zero, "the camera is not upside down");
        AssertNear(0, FVec3.Dot(t.Right, t.Up), 1e-5);
    }

    [Fact]
    public void LerpBlendsPositionScaleAndRotation()
    {
        var a = Transform3.At(FVec3.Zero);
        var b = new Transform3(new FVec3(10, 0, 0), FQuat.FromAxisAngle(FVec3.UnitY, Fixed.HalfPi), new FVec3(3, 3, 3));
        Assert.Equal(a, Transform3.Lerp(a, b, Fixed.Zero));
        var mid = Transform3.Lerp(a, b, Fixed.Half);
        AssertNear(5, mid.Position.X);
        AssertNear(2, mid.Scale.Y);
        var f = mid.Forward;
        AssertNear(-Math.Sin(Math.PI / 4), f.X, 1e-4);
        AssertNear(-Math.Cos(Math.PI / 4), f.Z, 1e-4);
        Assert.Equal(b.Position, Transform3.Lerp(a, b, Fixed.One).Position);
    }

    [Fact]
    public void StateDeltaAndSnapshotRoundTrip()
    {
        var world = NewWorld();
        for (var i = 0; i < 120; i++)
        {
            world.Step();
        }

        var probe = (Probe)world.Entities[0];
        Assert.True(probe.Bounces > 0, "the scenario bounces");
        Assert.NotEqual(default, probe.Glide);

        var state = new SyncWriter();
        probe.WriteState(state);
        var copy = new Probe();
        var reader = new SyncReader(state.Written);
        copy.ReadState(ref reader);
        reader.ExpectEnd();
        Assert.True(reader.Ok, reader.Error.ToString());
        Assert.Equal(probe.Pose, copy.Pose);
        Assert.Equal(probe.Glide, copy.Glide);
        Assert.Equal(probe.Spin, copy.Spin);

        // A delta carries only what changed: here every property but the spin.
        var before = new Probe();
        var r2 = new SyncReader(state.Written);
        before.ReadState(ref r2);
        world.Step();
        var delta = new SyncWriter();
        Assert.True(((Probe)world.Entities[0]).WriteDeltaFrom(before, delta));
        Assert.True(delta.Length < state.Length);
        before.ClearDirty();
        var r3 = new SyncReader(delta.Written);
        before.ApplyDelta(ref r3);
        var now = new SyncWriter();
        ((Probe)world.Entities[0]).WriteState(now);
        var applied = new SyncWriter();
        before.WriteState(applied);
        Assert.Equal(now.Written.ToArray(), applied.Written.ToArray());

        var snapshot = new SyncWriter();
        world.WriteSnapshot(snapshot);
        var restored = new World(1, Registry());
        Assert.Equal(SyncError.None, restored.ReadSnapshot(snapshot.Written));
        Assert.Equal(world.ComputeHash(), restored.ComputeHash());
    }

    [Fact]
    public void JsonDumpShowsTheParts()
    {
        var json = NewWorld().DumpJson();
        Assert.Contains("\"position\"", json, StringComparison.Ordinal);
        Assert.Contains("\"rotation\"", json, StringComparison.Ordinal);
        Assert.Contains("\"scale\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void TimedTransformInterpolatesBetweenTicksDeterministically()
    {
        var from = Transform3.At(FVec3.Zero);
        var to = Transform3.At(new FVec3(8, 0, 0), FQuat.FromAxisAngle(FVec3.UnitY, Fixed.One));
        var glide = new Timed<Transform3>(from, to, 10, 20);
        Assert.Equal(from, glide.ValueAt(5));
        Assert.Equal(to, glide.ValueAt(25));
        AssertNear(4, glide.ValueAt(15).Position.X);
        AssertNear(4.08, glide.ValueAt(15, Fixed.FromRatio(1, 10)).Position.X, 1e-6);
        Assert.Equal(glide.ValueAt(13, Fixed.FromRatio(1, 4)), glide.ValueAt(13, Fixed.FromRatio(1, 4)));
    }

    private static ulong[] Trail(World world, int ticks)
    {
        var hashes = new ulong[ticks];
        for (var i = 0; i < ticks; i++)
        {
            world.Step();
            hashes[i] = world.ComputeHash();
        }

        return hashes;
    }

    [Fact]
    public void SimMathGivesTheSameHashesOnARerunAndAfterSnapshotRestore()
    {
        var first = Trail(NewWorld(), 600);
        var second = Trail(NewWorld(), 600);
        Assert.Equal(first, second);

        var halfway = NewWorld();
        _ = Trail(halfway, 300);
        var snapshot = new SyncWriter();
        halfway.WriteSnapshot(snapshot);
        var restored = new World(11, Registry());
        Assert.Equal(SyncError.None, restored.ReadSnapshot(snapshot.Written));
        Assert.Equal(first[300..], Trail(restored, 300));
    }

    [Fact]
    public void SimMathHasAKnownAnswerSoPlatformsCanBeCompared()
    {
        // The same on every machine: only integer arithmetic is involved. If this changes, fixed-point math changed.
        var hashes = Trail(NewWorld(), 600);
        Assert.Equal(10685258475833068967UL, hashes[599]);
    }
}
