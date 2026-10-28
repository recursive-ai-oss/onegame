using OneGame.Core;
using OneGame.Navigation;

namespace OneGame.Navigation.Tests;

/// <summary>The level the test walkers read: shared, deterministic data, built the same way for every world.</summary>
public static class Level
{
    /// <summary>The map.</summary>
    public static NavGrid Map { get; set; } = new(8, 8);

    /// <summary>Search iterations per tick.</summary>
    public static int Budget { get; set; } = 40;

    /// <summary>Travel per tick.</summary>
    public static Fixed Speed { get; set; } = Fixed.FromRatio(1, 2);
}

/// <summary>A unit that searches a few iterations per tick and then walks the path, the way a game would write it.</summary>
[Entity]
public partial class Walker : Entity
{
    [Synced] public partial FVec2 Position { get; private set; }

    [Synced] public partial NavState Nav { get; private set; }

    private readonly NavSearcher _searcher = new();

    public int Rebuilds => _searcher.Rebuilds;

    [Command]
    public void GoTo(int x, int y)
    {
        var map = Level.Map;
        Nav = _searcher.Begin(map, Position, map.CellCenter(new NavCell(x, y)));
    }

    [Command]
    public void Place(int x, int y) => Position = Level.Map.CellCenter(new NavCell(x, y));

    protected override void OnTick()
    {
        var nav = Nav;
        if (_searcher.Advance(ref nav, Level.Map, Level.Budget))
        {
            Nav = nav;
        }

        if (nav.Status == NavStatus.Found)
        {
            var p = Position;
            Nav = nav.Follow(Level.Map, ref p, Level.Speed);
            Position = p;
        }
    }
}
