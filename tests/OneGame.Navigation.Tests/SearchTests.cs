using OneGame.Core;
using OneGame.Navigation;

namespace OneGame.Navigation.Tests;

public static class Maps
{
    /// <summary>Walls across every other row, each with one gap alternating between the two ends: the shortest path snakes through all rows.</summary>
    public static NavGrid Serpentine(int width, int height)
    {
        var grid = new NavGrid(width, height);
        var left = false;
        for (var y = 1; y < height; y += 2)
        {
            grid.SetRect(0, y, width, 1, 255);
            grid.SetBlocked(left ? 0 : width - 1, y, false);
            left = !left;
        }

        return grid;
    }

    /// <summary>Walls scattered by a fixed generator, with the corners kept open.</summary>
    public static NavGrid Scattered(int width, int height, ulong seed, int percent)
    {
        var grid = new NavGrid(width, height);
        var x = seed;
        for (var cy = 0; cy < height; cy++)
        {
            for (var cx = 0; cx < width; cx++)
            {
                x ^= x << 13;
                x ^= x >> 7;
                x ^= x << 17;
                if ((int)(x % 100) < percent)
                {
                    grid.SetBlocked(cx, cy, true);
                }
            }
        }

        grid.SetBlocked(0, 0, false);
        grid.SetBlocked(width - 1, height - 1, false);
        return grid;
    }

    public static ulong Hash(ReadOnlySpan<NavCell> cells)
    {
        var h = 14695981039346656037UL;
        foreach (var c in cells)
        {
            h = (h ^ (uint)c.X) * 1099511628211UL;
            h = (h ^ (uint)c.Y) * 1099511628211UL;
        }

        return h;
    }
}

public class SearchTests
{
    [Fact]
    public void AOneShotSearchFindsTheSamePathEveryTimeAndAroundWalls()
    {
        var grid = NavGrid.FromText("""
            ........
            .#####..
            .#...#..
            .#.#.#..
            ...#....
            """);
        var a = grid.FindPath(new NavCell(0, 0), new NavCell(4, 3));
        var b = grid.FindPath(new NavCell(0, 0), new NavCell(4, 3));
        Assert.True(a.Found);
        Assert.Equal(a.Cells.ToArray(), b.Cells.ToArray());
        Assert.Equal(a.Cost, b.Cost);
        Assert.Equal(new NavCell(0, 0), a.Cells[0]);
        Assert.Equal(new NavCell(4, 3), a.Cells[^1]);
        foreach (var c in a.Cells)
        {
            Assert.False(grid.IsBlocked(c.X, c.Y));
        }

        // Pinned: the same cells, the same cost on every platform and runtime.
        Assert.Equal(5310985265043681927UL, Maps.Hash(a.Cells));
    }

    [Fact]
    public void AStarJpsAndThetaStarAgreeOnReachabilityAndFourMovesStayOrthogonal()
    {
        var grid = Maps.Scattered(40, 40, 7, 25);
        var astar = grid.FindPath(new NavCell(0, 0), new NavCell(39, 39));
        var jps = grid.FindPath(new NavCell(0, 0), new NavCell(39, 39), new NavOptions(algorithm: NavAlgorithm.Jps));
        var theta = grid.FindPath(new NavCell(0, 0), new NavCell(39, 39), new NavOptions(algorithm: NavAlgorithm.ThetaStar));
        Assert.Equal(astar.Found, jps.Found);
        Assert.Equal(astar.Found, theta.Found);
        Assert.True(astar.Found);
        Assert.True(Math.Abs(astar.Cost.ToDouble() - jps.Cost.ToDouble()) < 1e-6);
        Assert.True(theta.Cost <= astar.Cost + Fixed.Epsilon);

        var four = grid.FindPath(new NavCell(0, 0), new NavCell(39, 39), new NavOptions(NavMoves.Four));
        if (four.Found)
        {
            for (var i = 1; i < four.Count; i++)
            {
                var d = Math.Abs(four.Cells[i].X - four.Cells[i - 1].X) + Math.Abs(four.Cells[i].Y - four.Cells[i - 1].Y);
                Assert.Equal(1, d);
            }
        }
    }

    [Fact]
    public void BadEndpointsReportWhichOneAndNoPathIsReported()
    {
        var grid = NavGrid.FromText("""
            ..#..
            ..#..
            ..#..
            """);
        Assert.Equal(NavStatus.NoPath, grid.FindPath(new NavCell(0, 0), new NavCell(4, 0)).Status);
        Assert.Equal(NavStatus.StartInvalid, grid.FindPath(new NavCell(2, 0), new NavCell(4, 0)).Status);
        Assert.Equal(NavStatus.GoalInvalid, grid.FindPath(new NavCell(0, 0), new NavCell(2, 1)).Status);
        Assert.Equal(NavStatus.StartInvalid, grid.FindPath(new NavCell(-1, 0), new NavCell(4, 0)).Status);
        Assert.Equal(NavStatus.GoalInvalid, grid.FindPath(new NavCell(0, 0), new NavCell(9, 0)).Status);
        Assert.Equal(NavStatus.GoalInvalid, grid.FindPath(FVec2.Zero, new FVec2(50, 0)).Status);
        Assert.Throws<ArgumentOutOfRangeException>(() => grid.FindPath(new NavCell(0, 0), new NavCell(1, 0), new NavOptions(agentSize: 0)));
    }

    [Fact]
    public void WorldPositionsGoThroughTheSameCellsAndCentresComeBackExact()
    {
        var grid = new NavGrid(16, 16, new NavSpace(new FVec2(100, -50), Fixed.FromRatio(5, 4)));
        var path = grid.FindPath(new FVec2(101, -49), new FVec2(110, -40));
        Assert.True(path.Found);
        var points = new FVec2[path.Count];
        Assert.Equal(path.Count, path.CopyPoints(grid, points));
        Assert.Equal(grid.CellCenter(path.Cells[0]), points[0]);
        Assert.True(grid.TryGetCell(points[^1], out var last));
        Assert.Equal(path.Cells[^1], last);
        Assert.Throws<ArgumentException>(() => path.CopyPoints(grid, new FVec2[1]));
    }

    private static NavState Finish(NavSearcher searcher, ref NavState state, NavGrid grid, int budget, out int ticks)
    {
        ticks = 0;
        while (state.Status == NavStatus.Searching)
        {
            Assert.True(searcher.Advance(ref state, grid, budget));
            ticks++;
            Assert.InRange(ticks, 0, 1_000_000);
        }

        return state;
    }

    [Fact]
    public void ALargeSearchSpansTicksAndEndsWithTheResultOfOneUnsplitCall()
    {
        var grid = Maps.Serpentine(96, 63);
        var searcher = new NavSearcher();
        var whole = searcher.Begin(grid, new NavCell(0, 0), new NavCell(95, 62));
        Finish(searcher, ref whole, grid, int.MaxValue, out var wholeTicks);
        Assert.Equal(1, wholeTicks);
        Assert.Equal(NavStatus.Found, whole.Status);

        foreach (var budget in new[] { 1, 7, 50, 300 })
        {
            var s = new NavSearcher();
            var state = s.Begin(grid, new NavCell(0, 0), new NavCell(95, 62));
            Finish(s, ref state, grid, budget, out var ticks);
            Assert.True(ticks > 1 || budget == 300, $"budget {budget} took {ticks} ticks");
            Assert.Equal(NavStatus.Found, state.Status);
            Assert.Equal(whole.Cells.ToArray(), state.Cells.ToArray());
            Assert.Equal(1, state.Next);
        }

        // The path is a real route: neighbours only, no walls, start to goal.
        var cells = whole.Cells.ToArray();
        Assert.Equal(0, cells[0]);
        Assert.Equal((62 * 96) + 95, cells[^1]);
        for (var i = 0; i < cells.Length; i++)
        {
            var c = grid.CellAt(cells[i]);
            Assert.False(grid.IsBlocked(c.X, c.Y));
            if (i > 0)
            {
                var p = grid.CellAt(cells[i - 1]);
                Assert.True(Math.Abs(c.X - p.X) <= 1 && Math.Abs(c.Y - p.Y) <= 1);
            }
        }
    }

    [Fact]
    public void ARestoredSearchContinuesExactlyWhereTheOriginalWould()
    {
        var grid = Maps.Serpentine(64, 47);
        var original = new NavSearcher();
        var state = original.Begin(grid, new NavCell(0, 0), new NavCell(63, 46));
        for (var i = 0; i < 5; i++)
        {
            Assert.True(original.Advance(ref state, grid, 100));
        }

        Assert.Equal(NavStatus.Searching, state.Status);
        Assert.Equal(500, state.Spent);

        // Through the sync codec, as a save would carry it, into a searcher that has never seen it.
        var w = new SyncWriter();
        state.Write(w);
        var bytes = w.ToArray();
        var reader = new SyncReader(bytes);
        var loaded = NavState.Read(ref reader);
        Assert.True(reader.Ok);
        Assert.Equal(state, loaded);

        var fresh = new NavSearcher();
        var a = state;
        var b = loaded;
        var trace = new List<(NavState, NavState)>();
        while (a.Status == NavStatus.Searching)
        {
            Assert.True(original.Advance(ref a, grid, 70));
            Assert.True(fresh.Advance(ref b, grid, 70));
            Assert.Equal(a, b);
            trace.Add((a, b));
        }

        Assert.Equal(NavStatus.Found, b.Status);
        Assert.Equal(1, fresh.Rebuilds);
        Assert.Equal(0, original.Rebuilds - 1); // the original rebuilt only to start
    }

    [Fact]
    public void ChangingTheMapRestartsAPathOrSearchEvenWhenTheChangeIsSeenOnlyAfterARestore()
    {
        var grid = NavGrid.FromText("""
            .....
            .....
            .....
            """);
        var searcher = new NavSearcher();
        var state = searcher.Begin(grid, new NavCell(0, 1), new NavCell(4, 1));
        Finish(searcher, ref state, grid, 1000, out _);
        Assert.Equal(5, state.Cells.Length);
        Assert.False(searcher.Advance(ref state, grid, 10)); // nothing changed: nothing to do

        var saved = state;
        grid.SetBlocked(2, 1, true);
        grid.SetBlocked(2, 0, true);
        var other = new NavSearcher(); // a searcher that never saw the old map, handed the old state
        var next = saved;
        Assert.True(other.Advance(ref next, grid, 1));
        Assert.Equal(NavStatus.Searching, next.Status);
        Finish(other, ref next, grid, 1000, out _);
        Assert.Equal(NavStatus.Found, next.Status);
        Assert.Equal(grid.IndexOf(new NavCell(0, 1)), next.Start);
        Assert.Contains(grid.IndexOf(new NavCell(2, 2)), next.Cells.ToArray());

        // Closing the last gap makes the goal unreachable; opening it again finds a way again.
        grid.SetBlocked(2, 2, true);
        Assert.True(other.Advance(ref next, grid, 1000));
        Assert.Equal(NavStatus.NoPath, next.Status);
        grid.SetBlocked(2, 2, false);
        Assert.True(other.Advance(ref next, grid, 1000));
        Assert.Equal(NavStatus.Found, next.Status);
    }

    [Fact]
    public void StatesFromAHostileSaveNeverThrowAndNeverWalkOutsideTheGrid()
    {
        var grid = new NavGrid(4, 4);
        var searcher = new NavSearcher();
        var hostile = new NavState(NavStatus.Searching, default, 99999, 3, 0, 0, grid.Stamp, null);
        Assert.True(searcher.Advance(ref hostile, grid, 10));
        Assert.Equal(NavStatus.StartInvalid, hostile.Status);

        var badGoal = new NavState(NavStatus.Searching, default, 0, 1 << 30, 0, 0, grid.Stamp, null);
        Assert.True(searcher.Advance(ref badGoal, grid, 10));
        Assert.Equal(NavStatus.GoalInvalid, badGoal.Status);

        var farSpent = new NavState(NavStatus.Searching, default, 0, 15, int.MaxValue, 0, grid.Stamp, null);
        Assert.True(searcher.Advance(ref farSpent, grid, 10));
        Assert.Equal(NavStatus.Found, farSpent.Status); // replay ends when the search ends: no runaway

        var wild = new NavState(NavStatus.Found, default, 0, 3, 0, 1, grid.Stamp, [0, 5, 1000000]);
        var pos = grid.CellCenter(new NavCell(0, 0));
        var after = wild.Follow(grid, ref pos, Fixed.FromInt(100));
        Assert.Equal(NavStatus.NoPath, after.Status);
        Assert.True(grid.TryGetCell(pos, out _));

        // The codec rejects what cannot be a state, without throwing.
        var random = new Random(1);
        var buffer = new byte[64];
        for (var i = 0; i < 20000; i++)
        {
            random.NextBytes(buffer);
            var r = new SyncReader(buffer.AsSpan(0, random.Next(0, buffer.Length)));
            _ = NavState.Read(ref r);
        }

        var w = new SyncWriter();
        new NavState(NavStatus.Found, default, 0, 3, 0, 5, 0, [0, 1]).Write(w);
        var rr = new SyncReader(w.Written);
        _ = NavState.Read(ref rr);
        Assert.False(rr.Ok); // Next beyond the path
    }

    [Fact]
    public void FollowWalksExactDistancesTurnsCornersAndArrives()
    {
        var grid = NavGrid.FromText("""
            ...
            ##.
            ...
            """);
        var searcher = new NavSearcher();
        var state = searcher.Begin(grid, new NavCell(0, 0), new NavCell(0, 2));
        Finish(searcher, ref state, grid, 1000, out _);
        Assert.Equal(NavStatus.Found, state.Status);

        var pos = grid.CellCenter(new NavCell(0, 0));
        var total = Fixed.Zero;
        var steps = 0;
        var quarter = Fixed.FromRatio(1, 4);
        while (state.Status == NavStatus.Found)
        {
            var before = pos;
            state = state.Follow(grid, ref pos, quarter);
            total += (pos - before).Length;
            steps++;
            Assert.InRange(steps, 0, 200);
        }

        Assert.Equal(NavStatus.Arrived, state.Status);
        Assert.Equal(grid.CellCenter(new NavCell(0, 2)), pos);
        Assert.Equal(grid.IndexOf(new NavCell(0, 2)), state.Start);
        Assert.Equal(24, steps); // six straight cells (the wall forbids cutting its corner), a quarter of a cell per step

        // Follow with no distance, or without a path, does nothing; a negative distance is an error.
        var same = state.Follow(grid, ref pos, Fixed.FromInt(5));
        Assert.Equal(state, same);
        Assert.Throws<ArgumentOutOfRangeException>(() => state.Follow(grid, ref pos, Fixed.FromInt(-1)));
    }

    [Fact]
    public void AVeryLongStepDoesNotOverflowTheLengthMath()
    {
        var space = new NavSpace(new FVec2(-1_000_000, -1_000_000), Fixed.FromInt(1_000_000));
        var grid = new NavGrid(2, 2, space);
        var state = new NavState(NavStatus.Found, default, 0, 3, 0, 1, grid.Stamp, [0, 3]);
        var pos = grid.CellCenter(new NavCell(0, 0));
        var after = state.Follow(grid, ref pos, Fixed.FromInt(500_000));
        Assert.Equal(NavStatus.Found, after.Status);
        var moved = pos - grid.CellCenter(new NavCell(0, 0));
        Assert.True(moved.X > 0 && moved.Y > 0);
        Assert.InRange(Math.Abs(moved.X.ToDouble() - 353553.39), 0, 2);
        Assert.Equal(moved.X, moved.Y);
    }
}
