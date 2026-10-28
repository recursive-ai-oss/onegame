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
        if (!path.Found || path.Cells[^1] != new NavCell(5, 3))
        {
            return "the one-shot search failed";
        }

        return Crowd();
    }

    // A crowd as a table of the world: four agents cross a gap, a snapshot taken mid-walk restores to the same hashes, and everybody arrives.
    private static string? Crowd()
    {
        const string room = """
            ....#....
            ....#....
            .........
            ....#....
            ....#....
            """;
        var space = new NavSpace(new FVec2(-72, -40), Fixed.FromInt(16));
        World Build()
        {
            var w = new World(1, new EntityRegistry());
            NavCrowd.AddTo(w, NavGrid.FromText(room, space), new NavCrowdOptions { AgentRadius = Fixed.FromInt(3), MaxAgents = 8 });
            return w;
        }

        var world = Build();
        var crowd = NavCrowd.Of(world)!;
        for (var i = 0; i < 4; i++)
        {
            if (!crowd.TryAdd(crowd.Map.CellCenter(new NavCell(0, i)), out var id) || !crowd.SetDestination(id, crowd.Map.CellCenter(new NavCell(8, i))))
            {
                return "the crowd refused an agent";
            }
        }

        for (var i = 0; i < 60; i++)
        {
            world.Step();
        }

        var writer = new SyncWriter();
        world.WriteSnapshot(writer);
        var copy = Build();
        if (copy.ReadSnapshot(writer.Written) != SyncError.None || copy.ComputeHash() != world.ComputeHash())
        {
            return "the crowd snapshot did not restore";
        }

        for (var i = 0; i < 400; i++)
        {
            world.Step();
            copy.Step();
            if (world.ComputeHash() != copy.ComputeHash())
            {
                return $"the restored crowd diverged at tick {world.Tick}";
            }
        }

        foreach (var a in crowd.Agents)
        {
            if (a.Status is not (NavAgentStatus.Arrived or NavAgentStatus.ArrivedShort))
            {
                return $"crowd agent {a.Id} did not arrive: {a.Status}";
            }
        }

        return null;
    }
}
