using OneGame.Core;
using OneGame.Navigation;

namespace OneGame.Navigation.Tests;

public class ConversionTests
{
    [Fact]
    public void EveryFixedWithinRangeSurvivesADoubleAndBack()
    {
        // Raw values across the range, including both edges and values with every low bit set.
        var raws = new List<long> { 0, 1, -1, NavLimits.MaxCoordinateRaw, -NavLimits.MaxCoordinateRaw, NavLimits.MaxCoordinateRaw - 1, 1L << 32, (1L << 32) + 1, 0x1F_FFFF_FFFF_FFFFL, -0x1F_FFFF_FFFF_FFFFL };
        var x = 0x9E3779B97F4A7C15UL;
        for (var i = 0; i < 20000; i++)
        {
            x ^= x << 13;
            x ^= x >> 7;
            x ^= x << 17;
            raws.Add((long)(x % (2UL * NavLimits.MaxCoordinateRaw + 1)) - NavLimits.MaxCoordinateRaw);
        }

        foreach (var raw in raws)
        {
            var f = Fixed.FromRaw(raw);
            var d = NavConvert.ToDouble(f);
            Assert.Equal(raw, NavConvert.FromDouble(d).Raw);
            Assert.Equal(raw / 4294967296.0, d); // the double is the exact quotient, no rounding happened
        }
    }

    [Fact]
    public void OutOfRangeValuesThrowAClearErrorInsteadOfConvertingWrongly()
    {
        var past = Fixed.FromRaw(NavLimits.MaxCoordinateRaw + 1);
        var e = Assert.Throws<NavRangeException>(() => NavConvert.ToDouble(past));
        Assert.Contains("2097152", e.Message);
        Assert.Throws<NavRangeException>(() => NavConvert.ToDouble(Fixed.FromRaw(-NavLimits.MaxCoordinateRaw - 1)));
        Assert.Throws<NavRangeException>(() => NavConvert.FromDouble(2097152.0000001));
        Assert.Throws<NavRangeException>(() => NavConvert.FromDouble(double.NaN));
        Assert.Throws<NavRangeException>(() => NavConvert.FromDouble(double.PositiveInfinity));
        Assert.Equal(NavLimits.MaxCoordinateRaw, NavConvert.FromDouble(2097152.0).Raw);
    }

    [Fact]
    public void FromDoubleRoundsToTheNearestFixedWithTiesToEven()
    {
        Assert.Equal(2, NavConvert.FromDouble(1.5 / 4294967296.0).Raw);
        Assert.Equal(2, NavConvert.FromDouble(2.5 / 4294967296.0).Raw);
        Assert.Equal(-2, NavConvert.FromDouble(-2.5 / 4294967296.0).Raw);
    }

    [Fact]
    public void AGridOrPositionBeyondTheRangeIsRejectedAtTheEdge()
    {
        var edge = Fixed.FromInt(NavLimits.MaxCoordinate);
        Assert.Throws<NavRangeException>(() => new NavSpace(new FVec2(edge + Fixed.Epsilon, 0), Fixed.One));
        Assert.Throws<ArgumentOutOfRangeException>(() => new NavSpace(FVec2.Zero, Fixed.Zero));

        // A grid that just fits, and one a single cell too wide.
        var fits = new NavSpace(new FVec2(Fixed.FromInt(-NavLimits.MaxCoordinate), 0), Fixed.FromInt(1 << 16));
        _ = new NavGrid(64, 1, fits);
        Assert.Throws<NavRangeException>(() => new NavGrid(65, 1, fits));

        var grid = new NavGrid(4, 4);
        Assert.Throws<NavRangeException>(() => grid.TryGetCell(new FVec2(edge + Fixed.Epsilon, 0), out _));
        Assert.Throws<NavRangeException>(() => grid.FindPath(new FVec2(0, -edge - Fixed.Epsilon), FVec2.Zero));
    }

    [Fact]
    public void PositionsMapToCellsWithFloorDivisionAndCentresAreExact()
    {
        var grid = new NavGrid(10, 10, new NavSpace(new FVec2(-5, -5), Fixed.FromRatio(3, 2)));
        Assert.True(grid.TryGetCell(new FVec2(-5, -5), out var c));
        Assert.Equal(new NavCell(0, 0), c);
        Assert.True(grid.TryGetCell(new FVec2(Fixed.FromRatio(-7, 2), -5), out c)); // exactly on the edge between cells 0 and 1 -> cell 1
        Assert.Equal(new NavCell(1, 0), c);
        Assert.True(grid.TryGetCell(new FVec2(Fixed.FromRaw(Fixed.FromRatio(-7, 2).Raw - 1), -5), out c));
        Assert.Equal(new NavCell(0, 0), c);
        Assert.False(grid.TryGetCell(new FVec2(-5 - Fixed.Epsilon, 0), out _)); // left of the grid: floor division, not truncation toward zero
        Assert.False(grid.TryGetCell(new FVec2(-5 + Fixed.FromInt(15), 0), out _)); // the far edge is outside
        Assert.True(grid.TryGetCell(new FVec2(-5 + Fixed.FromInt(15) - Fixed.Epsilon, -5), out c));
        Assert.Equal(new NavCell(9, 0), c);

        var centre = grid.CellCenter(new NavCell(2, 3));
        Assert.Equal(Fixed.FromRatio(-5 * 4 + 4 * 3 + 3, 4), centre.X); // -5 + 2*1.5 + 0.75 = -1.25
        Assert.Equal(Fixed.FromRatio(-5 * 4 + 6 * 3 + 3, 4), centre.Y); // -5 + 3*1.5 + 0.75 = 0.25
        Assert.True(grid.TryGetCell(centre, out c));
        Assert.Equal(new NavCell(2, 3), c);
    }
}
