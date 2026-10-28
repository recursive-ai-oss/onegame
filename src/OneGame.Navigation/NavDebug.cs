using System.Numerics;

namespace OneGame.Navigation;

/// <summary>A line of debug geometry in world units.</summary>
/// <param name="From">First end.</param>
/// <param name="To">Second end.</param>
/// <param name="Rgba">Colour as <c>0xRRGGBBAA</c>.</param>
/// <param name="Width">Thickness in world units.</param>
public readonly record struct NavDebugLine(Vector2 From, Vector2 To, uint Rgba, float Width);

/// <summary>A filled rectangle of debug geometry in world units.</summary>
/// <param name="Position">Top-left corner.</param>
/// <param name="Size">Width and height.</param>
/// <param name="Rgba">Colour as <c>0xRRGGBBAA</c>.</param>
public readonly record struct NavDebugRect(Vector2 Position, Vector2 Size, uint Rgba);

/// <summary>
/// Debug geometry for navigation: blocked cells, costed areas, paths and searches, as plain lines and rectangles in world units. It is
/// view-side data (floats, never state): collect it in a view while the debug overlay is on and draw it with the 2D drawing calls.
/// <code>
/// if (d.DebugVisible)
/// {
///     _shapes.Clear();
///     _shapes.AddGrid(Map);
///     _shapes.AddPath(Map, Nav);
///     foreach (var r in _shapes.Rects) { d.FillRect(new Rect(r.Position.X, r.Position.Y, r.Size.X, r.Size.Y), Color.FromRgba(r.Rgba), z: 9); }
///     foreach (var l in _shapes.Lines) { d.Line(l.From, l.To, Color.FromRgba(l.Rgba), l.Width, z: 10); }
/// }
/// </code>
/// </summary>
public sealed class NavDebugShapes
{
    /// <summary>Colour of blocked cells.</summary>
    public const uint BlockedColor = 0xC03030A0;

    /// <summary>Colour of costed areas.</summary>
    public const uint AreaColor = 0xD0A03070;

    /// <summary>Colour of paths.</summary>
    public const uint PathColor = 0x30E0A0FF;

    /// <summary>Colour of the part of a path already walked.</summary>
    public const uint WalkedColor = 0x808080A0;

    /// <summary>Colour of the start and goal markers.</summary>
    public const uint MarkerColor = 0xFFFFFFFF;

    private readonly List<NavDebugLine> _lines = [];
    private readonly List<NavDebugRect> _rects = [];

    /// <summary>The lines collected so far.</summary>
    public IReadOnlyList<NavDebugLine> Lines => _lines;

    /// <summary>The rectangles collected so far.</summary>
    public IReadOnlyList<NavDebugRect> Rects => _rects;

    /// <summary>Forgets everything collected.</summary>
    public void Clear()
    {
        _lines.Clear();
        _rects.Clear();
    }

    /// <summary>Adds the blocked and costed cells (horizontal runs of equal cells become one rectangle) and the grid's outline.</summary>
    /// <param name="grid">The map.</param>
    public void AddGrid(NavGrid grid)
    {
        ArgumentNullException.ThrowIfNull(grid);
        var size = grid.Space.CellSize.ToSingle();
        var origin = new Vector2(grid.Space.Origin.X.ToSingle(), grid.Space.Origin.Y.ToSingle());
        for (var y = 0; y < grid.Height; y++)
        {
            var x = 0;
            while (x < grid.Width)
            {
                var area = grid.GetArea(x, y);
                var run = 1;
                while (x + run < grid.Width && grid.GetArea(x + run, y) == area)
                {
                    run++;
                }

                if (area != 0)
                {
                    _rects.Add(new NavDebugRect(origin + new Vector2(x * size, y * size), new Vector2(run * size, size), area == 255 ? BlockedColor : AreaColor));
                }

                x += run;
            }
        }

        var w = grid.Width * size;
        var h = grid.Height * size;
        var thin = size / 16f;
        var a = origin;
        var b = origin + new Vector2(w, 0);
        var c = origin + new Vector2(w, h);
        var d = origin + new Vector2(0, h);
        _lines.Add(new NavDebugLine(a, b, MarkerColor, thin));
        _lines.Add(new NavDebugLine(b, c, MarkerColor, thin));
        _lines.Add(new NavDebugLine(c, d, MarkerColor, thin));
        _lines.Add(new NavDebugLine(d, a, MarkerColor, thin));
    }

    /// <summary>Adds a unit's path (walked part grey, the rest green) with squares on the start and goal; a running search shows only the markers.</summary>
    /// <param name="grid">The map the state belongs to.</param>
    /// <param name="state">The unit's navigation.</param>
    public void AddPath(NavGrid grid, in NavState state)
    {
        ArgumentNullException.ThrowIfNull(grid);
        var count = grid.Width * grid.Height;
        var size = grid.Space.CellSize.ToSingle();
        if (state.Status == NavStatus.Idle)
        {
            return;
        }

        if ((uint)state.Goal < (uint)count)
        {
            Marker(grid, state.Goal, size);
        }

        if ((uint)state.Start < (uint)count)
        {
            Marker(grid, state.Start, size);
        }

        var cells = state.Cells;
        for (var i = 1; i < cells.Length; i++)
        {
            if ((uint)cells[i - 1] >= (uint)count || (uint)cells[i] >= (uint)count)
            {
                return;
            }

            _lines.Add(new NavDebugLine(Center(grid, cells[i - 1]), Center(grid, cells[i]), i < state.Next ? WalkedColor : PathColor, size / 8f));
        }
    }

    private void Marker(NavGrid grid, int index, float size)
    {
        var c = Center(grid, index);
        var half = size / 4f;
        _rects.Add(new NavDebugRect(c - new Vector2(half, half), new Vector2(half * 2, half * 2), MarkerColor));
    }

    private static Vector2 Center(NavGrid grid, int index)
    {
        var p = grid.CellCenter(grid.CellAt(index));
        return new Vector2(p.X.ToSingle(), p.Y.ToSingle());
    }
}
