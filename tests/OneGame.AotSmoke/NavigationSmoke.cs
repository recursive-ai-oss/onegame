using OneGame.Core;
using OneGame.Navigation;

namespace OneGame.AotSmoke;

/// <summary>A unit that searches and walks, with its navigation as synced state.</summary>
[Entity]
internal sealed partial class NavSmokeUnit : Entity
{
    internal static readonly NavGrid Map = NavGrid.FromText("""
        ........
        .######.
        .#....#.
        .#.##.#.
        ...#....
        """);

    [Synced] public partial FVec2 Position { get; private set; }

    [Synced] public partial NavState Nav { get; private set; }

    private readonly NavSearcher _searcher = new();

    [Command]
    public void Go() => Nav = _searcher.Begin(Map, new NavCell(0, 0), new NavCell(5, 3));

    protected override void OnTick()
    {
        var nav = Nav;
        if (_searcher.Advance(ref nav, Map, 2))
        {
            Nav = nav;
        }

        if (nav.Status == NavStatus.Found)
        {
            var p = Position;
            Nav = nav.Follow(Map, ref p, Fixed.FromRatio(1, 2));
            Position = p;
        }
    }
}

/// <summary>Navigation under the native publish: a budgeted search inside a world, snapshot and restore of the state, and a one-shot search.</summary>
internal static class NavigationSmoke
{
    public static string? Run()
    {
        var registry = new EntityRegistry();
        OneGame.Generated.OneGame_AotSmoke.EntityTypes.Register(registry);
        var world = new World(1, registry);
        var unit = world.Add(new NavSmokeUnit());
        unit.Send.Go();
        world.Step();
        world.Step();
        if (unit.Nav.Status != NavStatus.Searching)
        {
            return "the search did not span ticks";
        }

        var writer = new SyncWriter();
        world.WriteSnapshot(writer);
        var copy = new World(1, registry);
        if (copy.ReadSnapshot(writer.Written) != SyncError.None)
        {
            return "the snapshot did not load";
        }

        for (var i = 0; i < 200; i++)
        {
            world.Step();
            copy.Step();
            if (world.ComputeHash() != copy.ComputeHash())
            {
                return $"restored world diverged at tick {world.Tick}";
            }
        }

        if (unit.Nav.Status != NavStatus.Arrived)
        {
            return "the unit did not arrive: " + unit.Nav;
        }

        var path = NavSmokeUnit.Map.FindPath(new NavCell(0, 0), new NavCell(5, 3));
        return path.Found && path.Cells[^1] == new NavCell(5, 3) ? null : "the one-shot search failed";
    }
}
