using RecursiveAI.OnePath;

namespace OneGame.Navigation;

/// <summary>
/// Runs searches for <see cref="NavState"/>s, a few iterations per tick. It is a cache, not state: it holds the search engine for the
/// state it last produced, and builds it again from the state alone when handed a state it does not recognise (after a restore, a
/// snapshot load or a different grid). Nothing here needs saving.
/// <code>
/// protected override void OnTick()
/// {
///     var nav = Nav;
///     if (_searcher.Advance(ref nav, Map, budget: 200)) { Nav = nav; }   // assign only when something changed
/// }
/// </code>
/// One searcher serves one unit (it keeps that unit's search warm); keep it in a non-synced field. It searches a private copy of the map taken when the search starts (a byte per cell), so it holds nothing that needs releasing and edits made mid-search cannot give a path through a half-changed map.
/// </summary>
public sealed class NavSearcher
{
    private GridReplanner? _replanner;
    private NavGrid? _grid;
    private int _start;
    private int _goal;
    private int _spent;
    private NavOptions _options;

    /// <summary>Number of times the search engine was rebuilt from a state instead of continued (after a restore, for instance). Diagnostics.</summary>
    public int Rebuilds { get; private set; }

    /// <summary>Starts a search between two world positions.</summary>
    /// <param name="grid">The map.</param>
    /// <param name="from">Where the unit is.</param>
    /// <param name="to">Where it wants to go.</param>
    /// <param name="options">The unit's size and moves.</param>
    /// <returns>A state to store: <see cref="NavStatus.Searching"/>, or <see cref="NavStatus.StartInvalid"/> / <see cref="NavStatus.GoalInvalid"/> when a position is off the grid or blocked.</returns>
    /// <exception cref="NavRangeException">A position is beyond ±2^21 units.</exception>
    public NavState Begin(NavGrid grid, FVec2 from, FVec2 to, NavOptions options = default)
    {
        ArgumentNullException.ThrowIfNull(grid);
        var hasStart = grid.TryGetCell(from, out var a);
        var hasGoal = grid.TryGetCell(to, out var b);
        if (!hasStart)
        {
            return new NavState(NavStatus.StartInvalid, options, 0, hasGoal ? grid.IndexOf(b) : 0, 0, 0, grid.Stamp, null);
        }

        if (!hasGoal)
        {
            return new NavState(NavStatus.GoalInvalid, options, grid.IndexOf(a), 0, 0, 0, grid.Stamp, null);
        }

        return Begin(grid, a, b, options);
    }

    /// <summary>Starts a search between two cells.</summary>
    /// <param name="grid">The map.</param>
    /// <param name="start">The first cell.</param>
    /// <param name="goal">The last cell.</param>
    /// <param name="options">The unit's size and moves.</param>
    /// <returns>A state to store; see <see cref="Begin(NavGrid, FVec2, FVec2, NavOptions)"/>.</returns>
    public NavState Begin(NavGrid grid, NavCell start, NavCell goal, NavOptions options = default)
    {
        ArgumentNullException.ThrowIfNull(grid);
        Drop();
        if (!grid.Contains(start))
        {
            return new NavState(NavStatus.StartInvalid, options, 0, grid.Contains(goal) ? grid.IndexOf(goal) : 0, 0, 0, grid.Stamp, null);
        }

        if (!grid.Contains(goal))
        {
            return new NavState(NavStatus.GoalInvalid, options, grid.IndexOf(start), 0, 0, 0, grid.Stamp, null);
        }

        return new NavState(NavStatus.Searching, options, grid.IndexOf(start), grid.IndexOf(goal), 0, 0, grid.Stamp, null);
    }

    /// <summary>
    /// Spends up to <paramref name="budget"/> search iterations on the state. A finished search becomes <see cref="NavStatus.Found"/> or
    /// <see cref="NavStatus.NoPath"/>; an unfinished one stays <see cref="NavStatus.Searching"/> with its progress recorded. A map
    /// edited since the search or path was made restarts it from <see cref="NavState.Start"/>. Does nothing for the other statuses.
    /// </summary>
    /// <param name="state">The state to advance; replaced when it changed.</param>
    /// <param name="grid">The map.</param>
    /// <param name="budget">Iterations (queue pops) to spend this call, at least 1. A few hundred per tick keeps the cost flat.</param>
    /// <returns>True when <paramref name="state"/> changed, and so should be stored.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The budget is below 1.</exception>
    public bool Advance(ref NavState state, NavGrid grid, int budget)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentOutOfRangeException.ThrowIfLessThan(budget, 1);
        var s = state;
        switch (s.Status)
        {
            case NavStatus.Found or NavStatus.NoPath or NavStatus.Searching when s.Stamp != grid.Stamp:
                // The map changed (now, or before a restore that the state predates): the path or the half-done search may be wrong.
                Drop();
                s = s.With(NavStatus.Searching, 0, 0, null, grid.Stamp);
                break;
            case NavStatus.Searching:
                break;
            default:
                return false;
        }

        var count = grid.Width * grid.Height;
        if ((uint)s.Start >= (uint)count)
        {
            state = s.With(NavStatus.StartInvalid, 0, 0, null, grid.Stamp);
            return true;
        }

        if ((uint)s.Goal >= (uint)count)
        {
            state = s.With(NavStatus.GoalInvalid, 0, 0, null, grid.Stamp);
            return true;
        }

        var result = Run(s, grid, budget);
        var spent = (int)Math.Min(int.MaxValue, _replanner?.TotalIterations ?? s.Spent);
        switch (result.Status)
        {
            case PathStatus.BudgetExhausted:
                _spent = spent;
                state = s.With(NavStatus.Searching, spent, 0, null, s.Stamp);
                return true;
            case PathStatus.Found:
                var cells = Store(result, grid, out var tooLong);
                Drop();
                state = tooLong
                    ? s.With(NavStatus.NoPath, 0, 0, null, s.Stamp)
                    : s.With(NavStatus.Found, 0, 1, cells, s.Stamp);
                return true;
            case PathStatus.StartInvalid:
                Drop();
                state = s.With(NavStatus.StartInvalid, 0, 0, null, s.Stamp);
                return true;
            case PathStatus.GoalInvalid:
                Drop();
                state = s.With(NavStatus.GoalInvalid, 0, 0, null, s.Stamp);
                return true;
            default:
                Drop();
                state = s.With(NavStatus.NoPath, 0, 0, null, s.Stamp);
                return true;
        }
    }

    private GridPath Run(NavState s, NavGrid grid, int budget)
    {
        if (_replanner is null || !ReferenceEquals(_grid, grid) || _start != s.Start || _goal != s.Goal || _spent != s.Spent || _options != s.Options)
        {
            // Not the search this state came from (a restore, a load, another grid): rebuild it by replaying the iterations it had spent.
            // Any split of the same total gives the same search, so this is the search that would have been there.
            Drop();
            Rebuilds++;
            var start = grid.CellAt(s.Start);
            var goal = grid.CellAt(s.Goal);
            var copy = Grid.From(grid.Grid.Cells, grid.Width, grid.Height);
            copy.Areas.CopyFrom(grid.Grid.Areas);
            _replanner = copy.CreateReplanner(new Cell(start.X, start.Y), new Cell(goal.X, goal.Y), NavGrid.ToGridOptions(s.Options, GridAlgorithm.DStarLite));
            _grid = grid;
            _start = s.Start;
            _goal = s.Goal;
            _options = s.Options;
            _spent = 0;
            if (s.Spent > 0)
            {
                var replay = _replanner.Replan(s.Spent);
                if (replay.Status != PathStatus.BudgetExhausted)
                {
                    return replay;
                }
            }
        }

        return _replanner.Replan(budget);
    }

    private static int[] Store(GridPath result, NavGrid grid, out bool tooLong)
    {
        var src = result.Cells;
        tooLong = src.Length > NavLimits.MaxPathCells;
        if (tooLong)
        {
            return [];
        }

        var cells = new int[src.Length];
        for (var i = 0; i < cells.Length; i++)
        {
            cells[i] = (src[i].Y * grid.Width) + src[i].X;
        }

        return cells;
    }

    private void Drop()
    {
        _replanner = null;
        _grid = null;
    }
}
