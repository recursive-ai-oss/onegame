using RecursiveAI.OnePath;

namespace OneGame.Navigation;

/// <summary>The result of a one-shot search: a status and, when there is one, the cells from start to goal.</summary>
public sealed class NavPath
{
    private static readonly NavCell[] None = [];
    private readonly NavCell[] _cells;

    private NavPath(NavStatus status, Fixed cost, NavCell[] cells)
    {
        Status = status;
        Cost = cost;
        _cells = cells;
    }

    /// <summary>Found, NoPath, StartInvalid or GoalInvalid.</summary>
    public NavStatus Status { get; }

    /// <summary>True when <see cref="Cells"/> holds a path.</summary>
    public bool Found => Status == NavStatus.Found;

    /// <summary>
    /// The cost of the path in open-ground steps (a straight step is 1, a diagonal one about 1.41, scaled by area costs), rounded to the
    /// nearest <see cref="Fixed"/> and capped at 2^21. Zero when there is no path.
    /// </summary>
    public Fixed Cost { get; }

    /// <summary>The cells from the start to the goal, both included. Empty when there is no path.</summary>
    public ReadOnlySpan<NavCell> Cells => _cells;

    /// <summary>Number of cells.</summary>
    public int Count => _cells.Length;

    internal static NavPath Failed(NavStatus status) => new(status, Fixed.Zero, None);

    internal static NavPath From(GridPath result)
    {
        if (result.Status != PathStatus.Found)
        {
            return Failed(result.Status switch
            {
                PathStatus.StartInvalid => NavStatus.StartInvalid,
                PathStatus.GoalInvalid => NavStatus.GoalInvalid,
                _ => NavStatus.NoPath,
            });
        }

        var src = result.Cells;
        if (src.Length > NavLimits.MaxPathCells)
        {
            return Failed(NavStatus.NoPath);
        }

        var cells = new NavCell[src.Length];
        for (var i = 0; i < cells.Length; i++)
        {
            cells[i] = new NavCell(src[i].X, src[i].Y);
        }

        return new NavPath(NavStatus.Found, CostToFixed(result.Cost), cells);
    }

    internal static Fixed CostToFixed(double cost)
        => cost >= NavLimits.MaxCoordinate ? Fixed.FromRaw(NavLimits.MaxCoordinateRaw) : NavConvert.FromDouble(cost);

    /// <summary>Writes the world positions of the path's cell centres into <paramref name="destination"/>.</summary>
    /// <param name="grid">The grid the path was found on.</param>
    /// <param name="destination">At least <see cref="Count"/> elements.</param>
    /// <returns>The number written.</returns>
    /// <exception cref="ArgumentException">The destination is too short.</exception>
    public int CopyPoints(NavGrid grid, Span<FVec2> destination)
    {
        ArgumentNullException.ThrowIfNull(grid);
        if (destination.Length < _cells.Length)
        {
            throw new ArgumentException($"The destination holds {destination.Length} points and the path has {_cells.Length}.", nameof(destination));
        }

        for (var i = 0; i < _cells.Length; i++)
        {
            destination[i] = grid.CellCenter(_cells[i]);
        }

        return _cells.Length;
    }
}
