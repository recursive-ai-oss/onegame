using RecursiveAI.OnePath;

namespace OneGame.Navigation;

/// <summary>
/// The walkable map a unit searches: a grid of cells, each open, blocked or an area with its own movement cost, placed in the world by a
/// <see cref="NavSpace"/>. It is level data: build it the same way on every node (from the level, or from synced cells), and edit it
/// from sim code only. Searches read it through <see cref="NavSearcher"/> or <see cref="FindPath(NavCell, NavCell, NavOptions)"/>.
/// </summary>
public sealed class NavGrid
{
    private const int AreaCount = 255;
    private readonly byte[] _areaBytes = new byte[AreaCount * 8];
    private ulong _stamp;
    private int _stampGridVersion = -1;
    private int _stampAreaVersion = -1;

    /// <summary>Creates an all-open grid.</summary>
    /// <param name="width">Cells across.</param>
    /// <param name="height">Cells down.</param>
    /// <param name="space">Where the grid sits in the world; unit cells at the origin by default.</param>
    /// <exception cref="ArgumentOutOfRangeException">A size is below 1, or the grid has more than <see cref="NavLimits.MaxCells"/> cells.</exception>
    /// <exception cref="NavRangeException">The grid reaches beyond ±2^21 units.</exception>
    public NavGrid(int width, int height, NavSpace space = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
        CheckSize(width, height);
        Space = space;
        CheckExtent(width, height, space);
        Grid = new Grid(width, height);
    }

    private NavGrid(Grid grid, NavSpace space)
    {
        CheckSize(grid.Width, grid.Height);
        CheckExtent(grid.Width, grid.Height, space);
        Space = space;
        Grid = grid;
    }

    /// <summary>The search library's grid, for what this class does not cover. Edits made on it are seen by searches, but not through <see cref="NavSpace"/> checks.</summary>
    public Grid Grid { get; }

    /// <summary>Cells across.</summary>
    public int Width => Grid.Width;

    /// <summary>Cells down.</summary>
    public int Height => Grid.Height;

    /// <summary>Where the grid sits in the world.</summary>
    public NavSpace Space { get; }

    /// <summary>
    /// A hash of everything a search depends on (size, every cell, every area cost). A <see cref="NavState"/> remembers it, so a path
    /// or a running search noticed to be out of date after the map changed, including a change that happened before a restore.
    /// Recomputed only when the map was edited.
    /// </summary>
    public ulong Stamp
    {
        get
        {
            var gv = Grid.Version;
            var av = Grid.Areas.Version;
            if (gv != _stampGridVersion || av != _stampAreaVersion)
            {
                for (var i = 0; i < AreaCount; i++)
                {
                    System.Buffers.Binary.BinaryPrimitives.WriteInt64LittleEndian(_areaBytes.AsSpan(i * 8, 8), BitConverter.DoubleToInt64Bits(Grid.Areas[(byte)i]));
                }

                var h = StateHash.Of(Grid.Cells);
                h = StateHash.Combine(h, StateHash.Of(_areaBytes));
                h = StateHash.Combine(h, ((ulong)(uint)Width << 32) | (uint)Height);
                _stamp = h;
                _stampGridVersion = gv;
                _stampAreaVersion = av;
            }

            return _stamp;
        }
    }

    /// <summary>Creates a grid from text: <c>.</c> is open, <c>#</c> a wall and <c>1</c> to <c>9</c> are areas; rows must be equally long.</summary>
    /// <param name="text">The rows, one per line.</param>
    /// <param name="space">Where the grid sits in the world.</param>
    /// <returns>The grid.</returns>
    /// <exception cref="ArgumentException">The text is not a rectangle of known characters.</exception>
    public static NavGrid FromText(string text, NavSpace space = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        try
        {
            return new NavGrid(Grid.FromText(text), space);
        }
        catch (GridFormatException e)
        {
            throw new ArgumentException(e.Message, nameof(text), e);
        }
    }

    /// <summary>Whether a cell is inside the grid.</summary>
    /// <param name="cell">The cell.</param>
    /// <returns>True if it is.</returns>
    public bool Contains(NavCell cell) => Grid.Contains(cell.X, cell.Y);

    /// <summary>The cell's index, <c>y * Width + x</c>, which is how paths are stored.</summary>
    /// <param name="cell">A cell inside the grid.</param>
    /// <returns>The index.</returns>
    public int IndexOf(NavCell cell) => (cell.Y * Width) + cell.X;

    /// <summary>The cell at an index.</summary>
    /// <param name="index">0 to <c>Width * Height - 1</c>.</param>
    /// <returns>The cell.</returns>
    public NavCell CellAt(int index) => new(index % Width, index / Width);

    /// <summary>The area of a cell: 0 is open, 1 to 254 are costed areas, 255 is a wall.</summary>
    /// <param name="x">Column.</param>
    /// <param name="y">Row.</param>
    /// <returns>The area id.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The cell is outside the grid.</exception>
    public byte GetArea(int x, int y) => Grid.GetArea(x, y);

    /// <summary>True if the cell is a wall.</summary>
    /// <param name="x">Column.</param>
    /// <param name="y">Row.</param>
    /// <returns>True for a wall.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The cell is outside the grid.</exception>
    public bool IsBlocked(int x, int y) => !Grid.IsWalkable(x, y);

    /// <summary>Sets the area of one cell.</summary>
    /// <param name="x">Column.</param>
    /// <param name="y">Row.</param>
    /// <param name="area">0 open, 1 to 254 an area, 255 a wall.</param>
    /// <exception cref="ArgumentOutOfRangeException">The cell is outside the grid.</exception>
    public void SetArea(int x, int y, byte area) => Grid.SetArea(x, y, area);

    /// <summary>Opens or blocks one cell.</summary>
    /// <param name="x">Column.</param>
    /// <param name="y">Row.</param>
    /// <param name="blocked">True for a wall.</param>
    /// <exception cref="ArgumentOutOfRangeException">The cell is outside the grid.</exception>
    public void SetBlocked(int x, int y, bool blocked) => Grid.SetWalkable(x, y, !blocked);

    /// <summary>Sets the area of a rectangle of cells.</summary>
    /// <param name="x">Left column.</param>
    /// <param name="y">Top row.</param>
    /// <param name="width">Columns.</param>
    /// <param name="height">Rows.</param>
    /// <param name="area">0 open, 1 to 254 an area, 255 a wall.</param>
    /// <exception cref="ArgumentOutOfRangeException">The rectangle is not inside the grid.</exception>
    public void SetRect(int x, int y, int width, int height, byte area) => Grid.SetRect(x, y, width, height, area);

    /// <summary>Sets what moving through an area costs, relative to an open cell (1).</summary>
    /// <param name="area">1 to 254.</param>
    /// <param name="multiplier">Greater than zero; 3 means three times the effort of open ground.</param>
    /// <exception cref="ArgumentOutOfRangeException">The area is 0 or 255, or the multiplier is not positive or is out of range.</exception>
    public void SetAreaCost(byte area, Fixed multiplier)
    {
        if (area is 0 or 255)
        {
            throw new ArgumentOutOfRangeException(nameof(area), area, "Areas 1 to 254 have a cost; 0 is open ground and 255 a wall.");
        }

        if (multiplier.Raw <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(multiplier), multiplier.ToString(), "An area cost must be positive.");
        }

        Grid.Areas[area] = NavConvert.ToDouble(multiplier);
    }

    /// <summary>The cell holding a world position.</summary>
    /// <param name="position">A world position.</param>
    /// <param name="cell">The cell, when the position is inside the grid.</param>
    /// <returns>False when the position is outside the grid.</returns>
    /// <exception cref="NavRangeException">The position is beyond ±2^21 units.</exception>
    public bool TryGetCell(FVec2 position, out NavCell cell)
    {
        NavLimits.Check(position.X, nameof(position));
        NavLimits.Check(position.Y, nameof(position));
        var size = Space.CellSize.Raw;
        var cx = FloorDiv(position.X.Raw - Space.Origin.X.Raw, size);
        var cy = FloorDiv(position.Y.Raw - Space.Origin.Y.Raw, size);
        if (cx < 0 || cy < 0 || cx >= Width || cy >= Height)
        {
            cell = default;
            return false;
        }

        cell = new NavCell((int)cx, (int)cy);
        return true;
    }

    /// <summary>The world position of a cell's centre. Exact: half a cell is a shift of the raw value, rounded down.</summary>
    /// <param name="cell">A cell inside the grid.</param>
    /// <returns>The centre.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The cell is outside the grid.</exception>
    public FVec2 CellCenter(NavCell cell)
    {
        if (!Contains(cell))
        {
            throw new ArgumentOutOfRangeException(nameof(cell), cell, $"Cell ({cell.X}, {cell.Y}) is outside the {Width}x{Height} grid.");
        }

        var size = Space.CellSize.Raw;
        return new FVec2(
            Fixed.FromRaw(Space.Origin.X.Raw + (cell.X * size) + (size >> 1)),
            Fixed.FromRaw(Space.Origin.Y.Raw + (cell.Y * size) + (size >> 1)));
    }

    /// <summary>Finds a path between two world positions, running the search to the end in this call.</summary>
    /// <param name="from">Where the unit is.</param>
    /// <param name="to">Where it wants to go.</param>
    /// <param name="options">The unit's size and moves; <see cref="NavOptions.Default"/> when omitted.</param>
    /// <returns>The path; its status says <see cref="NavStatus.StartInvalid"/> or <see cref="NavStatus.GoalInvalid"/> when a position is outside the grid.</returns>
    /// <exception cref="NavRangeException">A position is beyond ±2^21 units.</exception>
    public NavPath FindPath(FVec2 from, FVec2 to, NavOptions options = default)
    {
        var start = TryGetCell(from, out var a);
        var goal = TryGetCell(to, out var b);
        if (!start)
        {
            return NavPath.Failed(NavStatus.StartInvalid);
        }

        return goal ? FindPath(a, b, options) : NavPath.Failed(NavStatus.GoalInvalid);
    }

    /// <summary>Finds a path between two cells, running the search to the end in this call. Use <see cref="NavSearcher"/> to spread a search over ticks.</summary>
    /// <param name="start">The first cell.</param>
    /// <param name="goal">The last cell.</param>
    /// <param name="options">The unit's size and moves; <see cref="NavOptions.Default"/> when omitted.</param>
    /// <returns>The path, or why there is none.</returns>
    public NavPath FindPath(NavCell start, NavCell goal, NavOptions options = default)
    {
        if (!Contains(start))
        {
            return NavPath.Failed(NavStatus.StartInvalid);
        }

        if (!Contains(goal))
        {
            return NavPath.Failed(NavStatus.GoalInvalid);
        }

        var result = Grid.FindPath(new Cell(start.X, start.Y), new Cell(goal.X, goal.Y), ToGridOptions(options, options.Algorithm switch
        {
            NavAlgorithm.Jps => GridAlgorithm.Jps,
            NavAlgorithm.ThetaStar => GridAlgorithm.ThetaStar,
            _ => GridAlgorithm.AStar,
        }));
        return NavPath.From(result);
    }

    internal static GridOptions ToGridOptions(NavOptions options, GridAlgorithm algorithm) => new()
    {
        Connectivity = options.Moves == NavMoves.Four ? Connectivity.Four : Connectivity.Eight,
        Diagonals = options.Moves == NavMoves.EightNoSqueeze ? DiagonalRule.NoSqueezing : DiagonalRule.NoCornerCutting,
        AgentSize = options.AgentSize,
        Algorithm = algorithm,
    };

    private static long FloorDiv(long a, long b)
    {
        var q = a / b;
        return (a % b != 0 && a < 0) ? q - 1 : q;
    }

    private static void CheckSize(int width, int height)
    {
        if ((long)width * height > NavLimits.MaxCells)
        {
            throw new ArgumentOutOfRangeException(nameof(width), $"A navigation grid holds at most {NavLimits.MaxCells} cells; {width} x {height} is {(long)width * height}.");
        }
    }

    private static void CheckExtent(int width, int height, NavSpace space)
    {
        var size = (Int128)space.CellSize.Raw;
        var farX = space.Origin.X.Raw + (width * size);
        var farY = space.Origin.Y.Raw + (height * size);
        if (farX > NavLimits.MaxCoordinateRaw || farY > NavLimits.MaxCoordinateRaw)
        {
            throw new NavRangeException(nameof(space), $"A {width} x {height} grid of {space.CellSize}-unit cells at {space.Origin} reaches beyond the navigation range of ±{NavLimits.MaxCoordinate} units.");
        }
    }
}
