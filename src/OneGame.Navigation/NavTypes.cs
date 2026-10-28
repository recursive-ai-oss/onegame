// Everything here gives the same result on every machine: positions are converted with integer arithmetic only, and the searches are
// OnePath's, which use only exact IEEE operations and count iterations rather than time. Sim code may call all of it.
[assembly: OneGame.Core.SimSafe]

namespace OneGame.Navigation;

/// <summary>The limits of the fixed-point to grid conversion.</summary>
public static class NavLimits
{
    /// <summary>Largest absolute world coordinate, in world units: 2^21. Inside it a <see cref="Fixed"/> (32 fraction bits) is exactly a double (53 bits), which is what the search library needs to give identical results everywhere.</summary>
    public const int MaxCoordinate = 1 << 21;

    /// <summary>The same limit as a raw <see cref="Fixed"/> value.</summary>
    public const long MaxCoordinateRaw = (long)MaxCoordinate << Fixed.FractionBits;

    /// <summary>Largest number of cells in one grid.</summary>
    public const int MaxCells = 1 << 24;

    /// <summary>Longest stored path, in cells. A longer path is reported as <see cref="NavStatus.NoPath"/>.</summary>
    public const int MaxPathCells = SyncLimits.MaxListCount;

    /// <summary>Throws <see cref="NavRangeException"/> unless <paramref name="value"/> is within ±<see cref="MaxCoordinate"/>.</summary>
    /// <param name="value">The coordinate.</param>
    /// <param name="paramName">Name reported in the exception.</param>
    /// <exception cref="NavRangeException">The value is out of range.</exception>
    public static void Check(Fixed value, string paramName)
    {
        var raw = value.Raw;
        if (raw < -MaxCoordinateRaw || raw > MaxCoordinateRaw)
        {
            throw new NavRangeException(paramName, value);
        }
    }
}

/// <summary>A world position was outside what navigation can convert exactly (±2^21 world units).</summary>
public sealed class NavRangeException : ArgumentOutOfRangeException
{
    /// <summary>Creates the exception.</summary>
    /// <param name="paramName">The offending argument.</param>
    /// <param name="value">Its value.</param>
    public NavRangeException(string paramName, Fixed value)
        : base(paramName, value.ToString(), $"World coordinate {value} is outside the navigation range of ±{NavLimits.MaxCoordinate} units. Keep levels inside that range, or move the grid origin closer to the action.")
    {
    }

    /// <summary>Creates the exception with a custom message.</summary>
    /// <param name="paramName">The offending argument.</param>
    /// <param name="message">What is wrong.</param>
    public NavRangeException(string paramName, string message)
        : base(paramName, message)
    {
    }
}

/// <summary>Converts between <see cref="Fixed"/> numbers and doubles, for the cases where a double is unavoidable.</summary>
public static class NavConvert
{
    /// <summary>A fixed-point number as a double: exact, because the value is within ±2^21 and so needs at most 53 bits.</summary>
    /// <param name="value">The number.</param>
    /// <returns>The same value.</returns>
    /// <exception cref="NavRangeException">The value is beyond ±2^21.</exception>
    public static double ToDouble(Fixed value)
    {
        NavLimits.Check(value, nameof(value));
        return value.ToDouble();
    }

    /// <summary>A double as a fixed-point number, rounded to the nearest value (ties to even). Exact for any double that came from a <see cref="Fixed"/>.</summary>
    /// <param name="value">The number.</param>
    /// <returns>The nearest fixed-point value.</returns>
    /// <exception cref="NavRangeException">The value is NaN or beyond ±2^21.</exception>
    public static Fixed FromDouble(double value)
    {
        if (!(value >= -NavLimits.MaxCoordinate && value <= NavLimits.MaxCoordinate))
        {
            throw new NavRangeException(nameof(value), $"The number {value:R} is NaN or outside the navigation range of ±{NavLimits.MaxCoordinate} units.");
        }

        return Fixed.FromRaw((long)Math.Round(value * 4294967296.0, MidpointRounding.ToEven));
    }
}

/// <summary>A grid cell.</summary>
/// <param name="X">Column, from the left.</param>
/// <param name="Y">Row, from the top.</param>
public readonly record struct NavCell(int X, int Y);

/// <summary>How a unit may step between cells.</summary>
public enum NavMoves : byte
{
    /// <summary>Eight directions; a diagonal step needs both side cells free, so units never cut a wall corner.</summary>
    Eight = 0,

    /// <summary>Up, down, left and right only.</summary>
    Four = 1,

    /// <summary>Eight directions; a diagonal step may not squeeze between two blocked cells, but may pass one.</summary>
    EightNoSqueeze = 2,
}

/// <summary>Which search a one-shot <see cref="NavGrid.FindPath(NavCell, NavCell, NavOptions)"/> runs. Budgeted searches always use the repairing one.</summary>
public enum NavAlgorithm : byte
{
    /// <summary>A*: the cheapest path on the grid.</summary>
    AStar = 0,

    /// <summary>Jump point search: the same cost as A* on uniform grids, much faster on open ground.</summary>
    Jps = 1,

    /// <summary>Theta*: any-angle paths, shorter than grid paths where there is line of sight.</summary>
    ThetaStar = 2,
}

/// <summary>What kind of unit is searching. <c>default</c> is one cell wide, eight directions without corner cutting, A*.</summary>
public readonly struct NavOptions : IEquatable<NavOptions>
{
    private readonly byte _sizeMinusOne;

    /// <summary>Creates options.</summary>
    /// <param name="moves">The allowed steps.</param>
    /// <param name="agentSize">Width of the unit in cells, 1 to 255; its position is the top-left cell of the block it fills.</param>
    /// <param name="algorithm">The search used by one-shot queries.</param>
    /// <exception cref="ArgumentOutOfRangeException">An argument is out of range.</exception>
    public NavOptions(NavMoves moves = NavMoves.Eight, int agentSize = 1, NavAlgorithm algorithm = NavAlgorithm.AStar)
    {
        if (!Enum.IsDefined(moves))
        {
            throw new ArgumentOutOfRangeException(nameof(moves), moves, "Not a NavMoves value.");
        }

        if (!Enum.IsDefined(algorithm))
        {
            throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, "Not a NavAlgorithm value.");
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(agentSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(agentSize, 255);
        Moves = moves;
        _sizeMinusOne = (byte)(agentSize - 1);
        Algorithm = algorithm;
    }

    /// <summary>One cell wide, eight directions, no corner cutting, A*.</summary>
    public static NavOptions Default => default;

    /// <summary>The allowed steps.</summary>
    public NavMoves Moves { get; }

    /// <summary>Width of the unit in cells.</summary>
    public int AgentSize => _sizeMinusOne + 1;

    /// <summary>The search used by one-shot queries.</summary>
    public NavAlgorithm Algorithm { get; }

    /// <summary>Equality of every part.</summary>
    public bool Equals(NavOptions other) => Moves == other.Moves && _sizeMinusOne == other._sizeMinusOne && Algorithm == other.Algorithm;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is NavOptions o && Equals(o);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Moves, _sizeMinusOne, Algorithm);

    /// <summary>Equality.</summary>
    public static bool operator ==(NavOptions a, NavOptions b) => a.Equals(b);

    /// <summary>Inequality.</summary>
    public static bool operator !=(NavOptions a, NavOptions b) => !a.Equals(b);
}

/// <summary>The outcome of a search.</summary>
public enum NavStatus : byte
{
    /// <summary>No search has been asked for.</summary>
    Idle = 0,

    /// <summary>A search is running over several ticks.</summary>
    Searching = 1,

    /// <summary>A path is ready.</summary>
    Found = 2,

    /// <summary>The goal cannot be reached from the start.</summary>
    NoPath = 3,

    /// <summary>The start is blocked, outside the grid, or too narrow for the unit.</summary>
    StartInvalid = 4,

    /// <summary>The goal is blocked, outside the grid, or too narrow for the unit.</summary>
    GoalInvalid = 5,

    /// <summary>The unit walked the whole path.</summary>
    Arrived = 6,
}
