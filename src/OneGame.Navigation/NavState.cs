using OneGame.Core;

namespace OneGame.Navigation;

/// <summary>
/// One unit's navigation, as a value you keep in a synced property: what it asked for, how far the search got, and the path it found.
/// Because it is synced state it is saved with the world, restored from a snapshot, sent to replicas and covered by the drift hash.
/// It is immutable; <see cref="NavSearcher"/> and <see cref="Follow"/> return the next state, and you assign it back.
/// <code>[Synced] public partial NavState Nav { get; private set; }</code>
/// <para>
/// What a restore needs is only here: a search in progress is described by its start, goal and the number of iterations spent, and
/// replaying that many iterations gives the identical search, so no search internals are stored. A stored path that does not fit the
/// grid it is used with (an edited save) is dropped, never followed.
/// </para>
/// </summary>
public readonly struct NavState : ISyncValue<NavState>
{
    private readonly int[]? _cells;
    private readonly byte _sizeMinusOne;

    internal NavState(NavStatus status, NavOptions options, int start, int goal, int spent, int next, ulong stamp, int[]? cells)
    {
        Status = status;
        Moves = options.Moves;
        _sizeMinusOne = (byte)(options.AgentSize - 1);
        Start = start;
        Goal = goal;
        Spent = spent;
        Next = next;
        Stamp = stamp;
        _cells = cells is { Length: > 0 } ? cells : null;
    }

    /// <summary>Where the search or the walk stands.</summary>
    public NavStatus Status { get; }

    /// <summary>The unit's moves in the search.</summary>
    public NavMoves Moves { get; }

    /// <summary>The unit's width in cells (at least 1).</summary>
    public int AgentSize => _sizeMinusOne + 1;

    /// <summary>Index (<c>y * width + x</c>) of the cell the search starts from; it follows the unit as it walks, so a repeat search starts where it is.</summary>
    public int Start { get; }

    /// <summary>Index of the goal cell.</summary>
    public int Goal { get; }

    /// <summary>Search iterations spent so far on the current search.</summary>
    public int Spent { get; }

    /// <summary>Index into <see cref="Cells"/> of the waypoint the unit is walking to.</summary>
    public int Next { get; }

    /// <summary>The <see cref="NavGrid.Stamp"/> the search or path was made for.</summary>
    public ulong Stamp { get; }

    /// <summary>The path as cell indices from start to goal; empty unless a path was found.</summary>
    public ReadOnlySpan<int> Cells => _cells;

    /// <summary>True while a path is ready to follow.</summary>
    public bool HasPath => Status == NavStatus.Found && _cells is not null;

    /// <summary>The options this state searches with.</summary>
    public NavOptions Options => new(Moves, AgentSize);

    internal int[]? CellArray => _cells;

    internal NavState With(NavStatus status, int spent, int next, int[]? cells, ulong stamp)
        => new(status, Options, Start, Goal, spent, next, stamp, cells);

    internal NavState WithProgress(NavStatus status, int start, int next)
        => new(status, Options, start, Goal, Spent, next, Stamp, _cells);

    /// <summary>
    /// Walks a unit along the path: moves <paramref name="position"/> up to <paramref name="distance"/> toward the next waypoint (cell
    /// centre), then on to the following ones with what is left. Reaching the last one makes the status <see cref="NavStatus.Arrived"/>.
    /// Integer arithmetic only, so every node walks the same way.
    /// </summary>
    /// <param name="grid">The grid the path was found on.</param>
    /// <param name="position">The unit's position, moved in place.</param>
    /// <param name="distance">How far the unit may travel this tick (speed times the tick length); not negative.</param>
    /// <returns>The next state: unchanged unless a waypoint was reached.</returns>
    /// <exception cref="NavRangeException">The position is beyond ±2^21 units.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The distance is negative.</exception>
    public NavState Follow(NavGrid grid, ref FVec2 position, Fixed distance)
    {
        ArgumentNullException.ThrowIfNull(grid);
        if (distance.Raw < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(distance), distance.ToString(), "A unit cannot travel a negative distance.");
        }

        NavLimits.Check(position.X, nameof(position));
        NavLimits.Check(position.Y, nameof(position));
        if (Status != NavStatus.Found || _cells is null)
        {
            return this;
        }

        var count = grid.Width * grid.Height;
        var next = Next;
        var start = Start;
        var left = distance;
        var pos = position;
        while (next < _cells.Length)
        {
            var index = _cells[next];
            if ((uint)index >= (uint)count)
            {
                return With(NavStatus.NoPath, 0, 0, null, Stamp);
            }

            var target = grid.CellCenter(grid.CellAt(index));
            var delta = target - pos;
            var length = Distance(delta);
            if (length <= left)
            {
                pos = target;
                left -= length;
                start = index;
                next++;
                continue;
            }

            if (length.Raw > 0)
            {
                pos += delta * (left / length);
            }

            break;
        }

        position = pos;
        if (next >= _cells.Length)
        {
            return WithProgress(NavStatus.Arrived, start, next);
        }

        return next == Next && start == Start ? this : WithProgress(Status, start, next);
    }

    // The length of a vector without overflowing Fixed: squares of components beyond 2^15 units would leave its 31 integer bits, so very
    // long vectors are scaled down, measured and scaled back (losing low bits, identically everywhere).
    private static Fixed Distance(FVec2 v)
    {
        var m = Math.Max(Math.Abs(v.X.Raw), Math.Abs(v.Y.Raw));
        var shift = 0;
        while ((m >> shift) >= (1L << 47))
        {
            shift++;
        }

        if (shift == 0)
        {
            return v.Length;
        }

        var scaled = new FVec2(Fixed.FromRaw(v.X.Raw >> shift), Fixed.FromRaw(v.Y.Raw >> shift));
        return Fixed.FromRaw(scaled.Length.Raw << shift);
    }

    /// <inheritdoc />
    public void Write(SyncWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteByte((byte)Status);
        writer.WriteByte((byte)Moves);
        writer.WriteByte((byte)AgentSize);
        writer.WriteInt32(Start);
        writer.WriteInt32(Goal);
        writer.WriteInt32(Spent);
        writer.WriteInt32(Next);
        writer.WriteUInt64(Stamp);
        var n = _cells?.Length ?? 0;
        writer.WriteCount(n, SyncLimits.MaxListCount);
        for (var i = 0; i < n; i++)
        {
            writer.WriteInt32(_cells![i]);
        }
    }

    /// <inheritdoc />
    public static NavState Read(ref SyncReader reader)
    {
        var status = reader.ReadByte();
        var moves = reader.ReadByte();
        var size = reader.ReadByte();
        var start = reader.ReadInt32();
        var goal = reader.ReadInt32();
        var spent = reader.ReadInt32();
        var next = reader.ReadInt32();
        var stamp = reader.ReadUInt64();
        var n = reader.ReadCount(SyncLimits.MaxListCount);
        if (!reader.Ok)
        {
            return default;
        }

        if (status > (byte)NavStatus.Arrived || moves > (byte)NavMoves.EightNoSqueeze || size == 0 || start < 0 || goal < 0 || spent < 0 || next < 0 || next > n)
        {
            reader.Fail(SyncError.InvalidValue);
            return default;
        }

        var cells = n == 0 ? null : new int[n];
        for (var i = 0; i < n; i++)
        {
            var c = reader.ReadInt32();
            if (c < 0)
            {
                reader.Fail(SyncError.InvalidValue);
                return default;
            }

            cells![i] = c;
        }

        return reader.Ok ? new NavState((NavStatus)status, new NavOptions((NavMoves)moves, size), start, goal, spent, next, stamp, cells) : default;
    }

    /// <inheritdoc />
    public void WriteJson(JsonOut json)
    {
        ArgumentNullException.ThrowIfNull(json);
        json.BeginObject();
        json.Name("status");
        json.Value(Status.ToString());
        json.Name("start");
        json.Value(Start);
        json.Name("goal");
        json.Value(Goal);
        json.Name("spent");
        json.Value(Spent);
        json.Name("next");
        json.Value(Next);
        json.Name("path");
        json.BeginArray();
        if (_cells is not null)
        {
            foreach (var c in _cells)
            {
                json.Value(c);
            }
        }

        json.EndArray();
        json.EndObject();
    }

    /// <inheritdoc />
    public bool Equals(NavState other)
        => Status == other.Status && Moves == other.Moves && AgentSize == other.AgentSize && Start == other.Start && Goal == other.Goal
            && Spent == other.Spent && Next == other.Next && Stamp == other.Stamp
            && (ReferenceEquals(_cells, other._cells) || Cells.SequenceEqual(other.Cells));

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is NavState s && Equals(s);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Status, Start, Goal, Spent, Next, Stamp, _cells?.Length ?? 0);

    /// <summary>Equality of every part.</summary>
    public static bool operator ==(NavState a, NavState b) => a.Equals(b);

    /// <summary>Inequality.</summary>
    public static bool operator !=(NavState a, NavState b) => !a.Equals(b);

    /// <inheritdoc />
    public override string ToString() => $"{Status} {Start}->{Goal} spent {Spent}, {_cells?.Length ?? 0} cells, next {Next}";
}
