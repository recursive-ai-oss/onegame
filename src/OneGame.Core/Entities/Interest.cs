namespace OneGame.Core;

/// <summary>
/// The interest keys one connection wants in one scope. An entity whose <see cref="InterestKeyAttribute"/> value is non-zero is sent to the
/// connection only while this set holds it; key 0 entities (and entities without a key) are always sent. The authority owns the set and
/// changes it between ticks; the connection never decides it.
/// </summary>
public sealed class InterestSet
{
    private readonly Dictionary<uint, uint> _keys = [];
    private uint _generation;

    /// <summary>The number of keys held.</summary>
    public int Count => _keys.Count;

    /// <summary>True when <paramref name="key"/> is held.</summary>
    /// <param name="key">An interest key.</param>
    public bool Contains(uint key) => _keys.ContainsKey(key);

    /// <summary>True when an entity with this key is sent: key 0 is global, otherwise the key must be held.</summary>
    /// <param name="key">The entity's interest key.</param>
    public bool Admits(uint key) => key == 0 || _keys.ContainsKey(key);

    /// <summary>The keys held, in no particular order. Do not change the set while enumerating.</summary>
    public IEnumerable<uint> Keys => _keys.Keys;

    /// <summary>Makes this set equal to <paramref name="other"/>.</summary>
    /// <param name="other">The set to copy.</param>
    public void CopyFrom(InterestSet other)
    {
        _keys.Clear();
        foreach (var k in other._keys.Keys)
        {
            _keys[k] = 0;
        }
    }

    /// <summary>Makes the set hold exactly <paramref name="keys"/> (duplicates and zeros are ignored). Allocation-free once the set has grown to its working size.</summary>
    /// <param name="keys">The new contents.</param>
    /// <returns>True when the set changed.</returns>
    public bool Replace(ReadOnlySpan<uint> keys)
    {
        _generation++;
        if (_generation == 0)
        {
            _keys.Clear();
            _generation = 1;
        }

        var changed = false;
        foreach (var k in keys)
        {
            if (k == 0)
            {
                continue;
            }

            if (_keys.TryGetValue(k, out var seen))
            {
                if (seen != _generation)
                {
                    _keys[k] = _generation;
                }
            }
            else
            {
                _keys[k] = _generation;
                changed = true;
            }
        }

        if (_keys.Count != 0)
        {
            var stale = 0;
            foreach (var kv in _keys)
            {
                if (kv.Value != _generation)
                {
                    stale++;
                }
            }

            if (stale != 0)
            {
                changed = true;
                foreach (var kv in _keys)
                {
                    if (kv.Value != _generation)
                    {
                        _keys.Remove(kv.Key); // removing while enumerating is allowed since .NET 5
                    }
                }
            }
        }

        return changed;
    }
}

/// <summary>
/// A flat square grid for interest keys, and the helpers that keep them from flapping at cell edges. Keys pack the cell column and row
/// into 16 bits each (offset so a key is never 0), which covers 65,534 cells a side: with 128-block cells that is far past ±100,000 blocks.
/// Positions are whole blocks (or <see cref="Fixed"/>); the grid does not wrap.
/// </summary>
public sealed class InterestGrid2D
{
    private const int Bias = 32768;
    private const int Limit = 32767;

    /// <summary>Creates a grid.</summary>
    /// <param name="cellSize">Cell edge in blocks (64 to 128 suits players moving at a few metres a second).</param>
    /// <param name="enterCells">A connection starts receiving a cell once its position is within this many cells (Chebyshev) of it.</param>
    /// <param name="leaveCells">...and stops only once it is farther than this many cells. Must be greater than <paramref name="enterCells"/> so that standing on a boundary cannot flap.</param>
    /// <exception cref="ArgumentOutOfRangeException">A size or radius is out of range, or leave is not greater than enter.</exception>
    public InterestGrid2D(int cellSize = 128, int enterCells = 2, int leaveCells = 3)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(cellSize, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(enterCells, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(leaveCells, enterCells);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(leaveCells, 64);
        CellSize = cellSize;
        EnterCells = enterCells;
        LeaveCells = leaveCells;
    }

    /// <summary>Cell edge in blocks.</summary>
    public int CellSize { get; }

    /// <summary>Radius in cells at which a cell is added.</summary>
    public int EnterCells { get; }

    /// <summary>Radius in cells past which a cell is dropped.</summary>
    public int LeaveCells { get; }

    /// <summary>The cell column or row holding a coordinate in blocks.</summary>
    /// <param name="blocks">A coordinate in blocks.</param>
    public int Cell(int blocks) => Math.Clamp(FloorDiv(blocks, CellSize), -Limit, Limit);

    /// <summary>The key of the cell holding the point (never 0).</summary>
    /// <param name="x">Position in blocks.</param>
    /// <param name="y">Position in blocks.</param>
    public uint Key(int x, int y) => Pack(Cell(x), Cell(y));

    /// <summary>The key of the cell holding the point (never 0).</summary>
    /// <param name="x">Position.</param>
    /// <param name="y">Position.</param>
    public uint Key(Fixed x, Fixed y) => Key(x.FloorToInt(), y.FloorToInt());

    /// <summary>The key of cell (<paramref name="cx"/>, <paramref name="cy"/>).</summary>
    /// <param name="cx">Cell column.</param>
    /// <param name="cy">Cell row.</param>
    public static uint Pack(int cx, int cy)
        => (uint)((Math.Clamp(cx, -Limit, Limit) + Bias) << 16) | (uint)(Math.Clamp(cy, -Limit, Limit) + Bias);

    /// <summary>Splits a key made by <see cref="Pack"/>.</summary>
    /// <param name="key">A grid key.</param>
    /// <param name="cx">Cell column.</param>
    /// <param name="cy">Cell row.</param>
    public static void Unpack(uint key, out int cx, out int cy)
    {
        cx = (int)(key >> 16) - Bias;
        cy = (int)(key & 0xFFFF) - Bias;
    }

    /// <summary>
    /// The key an entity should carry now: <paramref name="current"/> while the point is still inside that cell widened by
    /// <paramref name="marginBlocks"/>, otherwise the cell of the point. An entity walking along a cell edge keeps one key instead of
    /// switching on every step.
    /// </summary>
    /// <param name="current">The entity's present key (0 for none).</param>
    /// <param name="x">Position in blocks.</param>
    /// <param name="y">Position in blocks.</param>
    /// <param name="marginBlocks">How far past the cell edge the entity may stray before it changes cell.</param>
    public uint Sticky(uint current, int x, int y, int marginBlocks)
    {
        if (current != 0)
        {
            Unpack(current, out var cx, out var cy);
            var x0 = (long)cx * CellSize - marginBlocks;
            var y0 = (long)cy * CellSize - marginBlocks;
            var x1 = (long)(cx + 1) * CellSize + marginBlocks;
            var y1 = (long)(cy + 1) * CellSize + marginBlocks;
            if (x >= x0 && x < x1 && y >= y0 && y < y1)
            {
                return current;
            }
        }

        return Key(x, y);
    }

    /// <summary>A connection's cursor: the cells around a position with enter/leave hysteresis. One per connection and scope.</summary>
    public Follower Follow() => new(this);

    private static int FloorDiv(int a, int b)
    {
        var q = a / b;
        return a % b != 0 && (a < 0) != (b < 0) ? q - 1 : q;
    }

    /// <summary>Tracks the cells one connection should receive. Call <see cref="Update(int,int)"/> as the player moves and pass <see cref="Keys"/> to <c>SetInterest</c>.</summary>
    public sealed class Follower
    {
        private readonly InterestGrid2D _grid;
        private readonly List<uint> _keys = [];
        private uint _center;

        internal Follower(InterestGrid2D grid) => _grid = grid;

        /// <summary>The cells to receive now. Valid until the next update.</summary>
        public ReadOnlySpan<uint> Keys => System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_keys);

        /// <summary>Moves the cursor to a position in blocks: cells within the enter radius are added, cells beyond the leave radius dropped.</summary>
        /// <param name="x">Position in blocks.</param>
        /// <param name="y">Position in blocks.</param>
        /// <returns>True when the set of cells changed.</returns>
        public bool Update(int x, int y)
        {
            var cx = _grid.Cell(x);
            var cy = _grid.Cell(y);
            var key = Pack(cx, cy);
            if (key == _center)
            {
                return false;
            }

            _center = key;
            var changed = false;
            for (var i = _keys.Count - 1; i >= 0; i--)
            {
                Unpack(_keys[i], out var kx, out var ky);
                if (Math.Max(Math.Abs(kx - cx), Math.Abs(ky - cy)) > _grid.LeaveCells)
                {
                    _keys.RemoveAt(i);
                    changed = true;
                }
            }

            var r = _grid.EnterCells;
            for (var dy = -r; dy <= r; dy++)
            {
                for (var dx = -r; dx <= r; dx++)
                {
                    var k = Pack(cx + dx, cy + dy);
                    if (!_keys.Contains(k))
                    {
                        _keys.Add(k);
                        changed = true;
                    }
                }
            }

            return changed;
        }

        /// <summary>As <see cref="Update(int,int)"/> with fixed-point positions.</summary>
        /// <param name="x">Position.</param>
        /// <param name="y">Position.</param>
        public bool Update(Fixed x, Fixed y) => Update(x.FloorToInt(), y.FloorToInt());
    }
}
