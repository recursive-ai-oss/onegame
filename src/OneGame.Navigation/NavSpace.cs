using RecursiveAI.OnePath;

namespace OneGame.Navigation;

/// <summary>
/// Where a grid sits in the world: the top-left corner and the edge length of a cell, both in <see cref="Fixed"/> units. Cell
/// <c>(x, y)</c> covers <c>origin + (x, y) * CellSize</c> to <c>origin + (x + 1, y + 1) * CellSize</c>.
/// </summary>
public readonly struct NavSpace : IEquatable<NavSpace>
{
    private readonly Fixed _cellSize;

    /// <summary>Creates a mapping.</summary>
    /// <param name="origin">World position of the grid's top-left corner.</param>
    /// <param name="cellSize">Edge length of a cell; positive.</param>
    /// <exception cref="NavRangeException">A value is beyond ±2^21 units.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The cell size is not positive.</exception>
    public NavSpace(FVec2 origin, Fixed cellSize)
    {
        NavLimits.Check(origin.X, nameof(origin));
        NavLimits.Check(origin.Y, nameof(origin));
        if (cellSize.Raw <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cellSize), cellSize.ToString(), "The cell size must be positive.");
        }

        NavLimits.Check(cellSize, nameof(cellSize));
        Origin = origin;
        _cellSize = cellSize;
    }

    /// <summary>Unit cells with the corner at (0, 0).</summary>
    public static NavSpace Unit => new(FVec2.Zero, Fixed.One);

    /// <summary>World position of the top-left corner.</summary>
    public FVec2 Origin { get; }

    /// <summary>Edge length of a cell. Zero only for <c>default</c>, which behaves as <see cref="Unit"/>.</summary>
    public Fixed CellSize => _cellSize.Raw == 0 ? Fixed.One : _cellSize;

    /// <summary>Equality of origin and cell size.</summary>
    public bool Equals(NavSpace other) => Origin == other.Origin && CellSize == other.CellSize;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is NavSpace s && Equals(s);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Origin, CellSize);

    /// <summary>Equality.</summary>
    public static bool operator ==(NavSpace a, NavSpace b) => a.Equals(b);

    /// <summary>Inequality.</summary>
    public static bool operator !=(NavSpace a, NavSpace b) => !a.Equals(b);
}
