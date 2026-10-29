using OneGame.Core;
using OneGame.Physics;

namespace OneGame.Physics.Tests;

public class ConversionTests
{
    [Fact]
    public void EveryFixedWithinRangeSurvivesADoubleAndBack()
    {
        var raws = new List<long> { 0, 1, -1, PhysicsLimits.MaxCoordinateRaw, -PhysicsLimits.MaxCoordinateRaw, PhysicsLimits.MaxCoordinateRaw - 1, 1L << 32, (1L << 32) + 1 };
        var x = 0x9E3779B97F4A7C15UL;
        for (var i = 0; i < 20000; i++)
        {
            x ^= x << 13;
            x ^= x >> 7;
            x ^= x << 17;
            raws.Add((long)(x % (2UL * PhysicsLimits.MaxCoordinateRaw + 1)) - PhysicsLimits.MaxCoordinateRaw);
        }

        foreach (var raw in raws)
        {
            var d = PhysicsLimits.ToDouble(Fixed.FromRaw(raw));
            Assert.Equal(raw / 4294967296.0, d);
            Assert.Equal(raw, PhysicsLimits.FromDouble(d).Raw);
        }
    }

    [Fact]
    public void OutOfRangeValuesThrowAClearError()
    {
        var past = Fixed.FromRaw(PhysicsLimits.MaxCoordinateRaw + 1);
        var e = Assert.Throws<PhysicsRangeException>(() => PhysicsLimits.ToDouble(past));
        Assert.Contains("2097152", e.Message);
        Assert.Throws<PhysicsRangeException>(() => PhysicsLimits.ToSingle(Fixed.FromRaw(-PhysicsLimits.MaxCoordinateRaw - 1)));
        Assert.Throws<PhysicsRangeException>(() => PhysicsLimits.FromDouble(2097152.0000001));
        Assert.Throws<PhysicsRangeException>(() => PhysicsLimits.FromDouble(double.NaN));
        Assert.Throws<PhysicsRangeException>(() => PhysicsLimits.FromDouble(double.NegativeInfinity));
        Assert.Equal(PhysicsLimits.MaxCoordinateRaw, PhysicsLimits.FromDouble(2097152.0).Raw);
    }

    [Fact]
    public void RoundingGoesToTheNearestTiePairingToEven()
    {
        // Half a raw unit is 2^-33: ties go to the even raw value.
        Assert.Equal(0, PhysicsLimits.FromDouble(Math.ScaleB(1.0, -33)).Raw);
        Assert.Equal(2, PhysicsLimits.FromDouble(Math.ScaleB(3.0, -33)).Raw);
    }

    [Fact]
    public void ASpaceRefusesAPositionOutsideTheRange()
    {
        var space = new PhysicsSpace2D();
        var far = Fixed.FromInt(PhysicsLimits.MaxCoordinate) + Fixed.One;
        Assert.Throws<PhysicsRangeException>(() => space.AddCircle(Fixed.One, new FVec2(far, Fixed.Zero)));
        Assert.Throws<PhysicsRangeException>(() => space.AddBox(new FVec2(far, Fixed.One), FVec2.Zero));
        Assert.Equal(0, space.BodyCount);
    }

    [Fact]
    public void ReferencesRoundTripThroughTheSyncCodecAndRejectNonsense()
    {
        var space = new PhysicsSpace2D();
        var body = space.AddCircle(Fixed.One, FVec2.Zero);
        var w = new SyncWriter();
        body.Write(w);
        var r = new SyncReader(w.Written);
        Assert.Equal(body, BodyRef.Read(ref r));
        Assert.True(r.Ok);

        var bad = new SyncWriter();
        bad.WriteInt32(-1);
        bad.WriteInt32(3);
        var rb = new SyncReader(bad.Written);
        BodyRef.Read(ref rb);
        Assert.False(rb.Ok);
        Assert.True(default(BodyRef).IsNull);
    }
}
