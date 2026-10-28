using System;
using System.Collections.Generic;
using System.Numerics;
using OneGame.Core;
using OneGame.Host;
using OneGame.Navigation;
using OneGame.Platform;
using OneGame.UI;

namespace PathingExample;

/// <summary>The course: a 20 by 11 grid of 16-unit cells, centred on the origin so the 320 by 176 area fills a 320 by 180 window.</summary>
public static class Course
{
    /// <summary>Walls are #, open ground is a dot.</summary>
    public const string Layout = """
        ....................
        ....#.........#.....
        ....#.........#.....
        ....#...####..#.....
        ....#...#..#..#.....
        ....#...#..#........
        ..........#..#####..
        ..#####...#.........
        ..#...#...#####.....
        ....................
        ....................
        """;

    /// <summary>Where the grid sits in the world.</summary>
    public static NavSpace Space => new(new FVec2(-160, -88), Fixed.FromInt(16));

    /// <summary>Search iterations a unit spends per tick, so a long search takes several ticks.</summary>
    public const int Budget = 12;

    /// <summary>World units a unit walks per tick.</summary>
    public static Fixed Speed => Fixed.FromInt(2);
}

/// <summary>
/// The ground. It owns the navigation map of its world, built from the base layout plus the walls players have added, and it is the only
/// entity that does: units find it by id, so every world (the authority, a replica, a restored copy) navigates its own map.
/// </summary>
[Entity]
public partial class Terrain : Entity, IView2D
{
    public static readonly EntityId TerrainId = new(1);

    /// <summary>Cell indices of the walls added at run time, in the order they were added.</summary>
    [Synced] public SyncList<int> Walls { get; } = new();

    private NavGrid? _map;
    private readonly List<int> _applied = [];
    private readonly NavDebugShapes _shapes = new();

    /// <summary>Puts a wall on a cell, or takes a wall added earlier away again.</summary>
    [Command]
    public bool Toggle(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Map.Width || y >= Map.Height)
        {
            return false;
        }

        var index = Map.IndexOf(new NavCell(x, y));
        if (!Walls.Remove(index))
        {
            Walls.Add(index);
        }

        return true;
    }

    /// <summary>The map, brought up to date with the synced walls first (after a restore or a command it is rebuilt).</summary>
    public NavGrid Map
    {
        get
        {
            if (_map is null || !SameWalls())
            {
                _map = NavGrid.FromText(Course.Layout, Course.Space);
                _applied.Clear();
                for (var i = 0; i < Walls.Count; i++)
                {
                    _map.SetBlocked(Walls[i] % _map.Width, Walls[i] / _map.Width, true);
                    _applied.Add(Walls[i]);
                }
            }

            return _map;
        }
    }

    private bool SameWalls()
    {
        if (_applied.Count != Walls.Count)
        {
            return false;
        }

        for (var i = 0; i < _applied.Count; i++)
        {
            if (_applied[i] != Walls[i])
            {
                return false;
            }
        }

        return true;
    }

    [View]
    public void Draw(Draw2D d)
    {
        var map = Map;
        var size = map.Space.CellSize.ToSingle();
        var ox = map.Space.Origin.X.ToSingle();
        var oy = map.Space.Origin.Y.ToSingle();
        d.FillRect(new Rect(ox, oy, map.Width * size, map.Height * size), Color.Rgb(0x1d2733), 0, -2);
        for (var y = 0; y < map.Height; y++)
        {
            for (var x = 0; x < map.Width; x++)
            {
                if (map.IsBlocked(x, y))
                {
                    d.FillRect(new Rect(ox + (x * size), oy + (y * size), size, size), Color.Rgb(0x5b6b7c), 0, -1);
                }
            }
        }

        // The development overlay (F3) also shows what the search sees: blocked cells, costed areas, the grid's outline.
        if (d.DebugVisible)
        {
            _shapes.Clear();
            _shapes.AddGrid(map);
            Units.Draw(d, _shapes, 8);
        }
    }
}

/// <summary>Draws collected navigation shapes with the 2D calls.</summary>
public static class Units
{
    public static void Draw(Draw2D d, NavDebugShapes shapes, float z)
    {
        foreach (var r in shapes.Rects)
        {
            d.FillRect(new Rect(r.Position.X, r.Position.Y, r.Size.X, r.Size.Y), Color.Rgba(r.Rgba), 0, z);
        }

        foreach (var l in shapes.Lines)
        {
            d.Line(l.From, l.To, Color.Rgba(l.Rgba), l.Width, z + 1);
        }
    }
}

/// <summary>
/// A unit that walks to where it is told. Its simulation spends a few search iterations per tick until the path is ready, then walks it, and
/// searches again from where it stands when the map changes. All of its navigation state is the one synced <see cref="Nav"/> value, so
/// saves, replays and replicas carry it.
/// </summary>
[Entity]
public partial class Unit : Entity, IView2D
{
    /// <summary>Width in cells; a wagon of 2 only fits where the ground is two cells wide.</summary>
    [Synced] public partial int Size { get; internal set; }

    [Synced] public partial FVec2 Position { get; internal set; }

    [Synced] public partial NavState Nav { get; private set; }

    private readonly NavSearcher _searcher = new();
    private readonly NavDebugShapes _shapes = new();

    private Terrain Ground => (Terrain)World!.Find(Terrain.TerrainId)!;

    /// <summary>Puts the unit on a cell (the top-left cell of its block) with a width.</summary>
    [Command]
    public void Spawn(int x, int y, int size)
    {
        Size = Math.Clamp(size, 1, 4);
        Position = Ground.Map.CellCenter(new NavCell(x, y));
        Nav = default;
    }

    /// <summary>Starts a search to a cell. The unit walks when the search finishes.</summary>
    [Command]
    public bool GoTo(int x, int y)
    {
        var map = Ground.Map;
        if (!map.Contains(new NavCell(x, y)))
        {
            return false;
        }

        Nav = _searcher.Begin(map, Position, map.CellCenter(new NavCell(x, y)), new NavOptions(agentSize: Size));
        return true;
    }

    protected override void OnTick()
    {
        var map = Ground.Map;
        var nav = Nav;
        if (_searcher.Advance(ref nav, map, Course.Budget))
        {
            Nav = nav;
        }

        if (nav.Status == NavStatus.Found)
        {
            var p = Position;
            Nav = nav.Follow(map, ref p, Course.Speed);
            Position = p;
        }
    }

    [View]
    public void Draw(Draw2D d)
    {
        var half = (Size - 1) * 8f;
        var c = new Vector2(Position.X.ToSingle() + half, Position.Y.ToSingle() + half);
        var side = (Size * 16f) - 4f;
        d.FillRect(new Rect(c.X - (side / 2), c.Y - (side / 2), side, side), Size == 1 ? Color.Rgb(0xf2c14e) : Color.Rgb(0x4ec9f2), 0, 5);
        if (d.DebugVisible)
        {
            _shapes.Clear();
            _shapes.AddPath(Ground.Map, Nav);
            Units.Draw(d, _shapes, 9);
        }
    }
}

public sealed class PathingGame : GameDefinition
{
    public override string Name => "Pathing";

    public override void RegisterEntities(EntityRegistry registry) => OneGame.Generated.PathingExample.EntityTypes.Register(registry);

    public override void Setup(World world)
    {
        var ground = world.Add(new Terrain());
        var map = ground.Map;
        world.Add(new Unit { Size = 1, Position = map.CellCenter(new NavCell(0, 5)) });
        world.Add(new Unit { Size = 2, Position = map.CellCenter(new NavCell(0, 9)) });
    }

    public override void DefineActions(ActionMap actions) => actions.Context("play", c => c
        .Button("go", Bind.Key(Key.Space))
        .Button("wall", Bind.Key(Key.W)));

    // Views read state and send commands. Space sends both units to the far side (or back); W toggles a wall across the middle.
    public override void OnEachFrame(GameInstance game)
    {
        if (game.Actions.Pressed("go"))
        {
            var scout = (Unit)game.World.Entities[1];
            var east = scout.Position.X.ToInt() < 0;
            scout.Send.GoTo(east ? 19 : 0, 5);
            ((Unit)game.World.Entities[2]).Send.GoTo(east ? 18 : 0, 9);
        }

        if (game.Actions.Pressed("wall"))
        {
            ((Terrain)game.World.Entities[0]).Send.Toggle(9, 5);
        }
    }

    public override void DefineScreens(ScreenSet screens) => screens.Add("main", _ => Ui.Column());
}
