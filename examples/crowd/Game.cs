using System.Numerics;
using OneGame.Core;
using OneGame.Host;
using OneGame.Navigation;
using OneGame.Platform;
using OneGame.UI;

namespace CrowdExample;

/// <summary>The room: a wall with a two-cell gap that the crowd has to squeeze through, on a 20 by 11 grid of 16-unit cells centred on the origin.</summary>
public static class Room
{
    /// <summary>Walls are #, open ground is a dot.</summary>
    public const string Layout = """
        ..........#.........
        ..........#.........
        ..........#.........
        ..........#.........
        ..........#.........
        ....................
        ....................
        ..........#.........
        ..........#.........
        ..........#.........
        ..........#.........
        """;

    /// <summary>Where the grid sits in the world.</summary>
    public static NavSpace Space => new(new FVec2(-160, -88), Fixed.FromInt(16));

    /// <summary>The map every node builds the same way: level data, not state.</summary>
    public static NavGrid Map() => NavGrid.FromText(Layout, Space);
}

/// <summary>
/// The people in the room. The crowd itself lives in the world as the <c>og.nav.crowd</c> table (added in <see cref="CrowdGame.RegisterSystems"/>),
/// which the world steps once per tick, saves, hashes and replicates; this entity only turns commands into crowd calls and draws the result.
/// </summary>
[Entity]
public partial class Plaza : Entity, IView2D
{
    /// <summary>Puts a person on a cell (or the nearest open ground). False when the room is full.</summary>
    [Command]
    public bool Spawn(int x, int y)
    {
        var crowd = NavCrowd.Of(World!)!;
        return crowd.TryAdd(crowd.Map.CellCenter(new NavCell(x, y)), out _);
    }

    /// <summary>Sends one person to a cell.</summary>
    [Command]
    public bool GoTo(int id, int x, int y)
    {
        var crowd = NavCrowd.Of(World!)!;
        return crowd.Map.Contains(new NavCell(x, y)) && crowd.SetDestination(id, crowd.Map.CellCenter(new NavCell(x, y)));
    }

    /// <summary>Takes one person out of the room.</summary>
    [Command]
    public bool Leave(int id) => NavCrowd.Of(World!)!.Remove(id);

    // Views read the crowd and never write it: the agents are a read-only span of fixed-point values.
    [View]
    public void Draw(Draw2D d)
    {
        var crowd = NavCrowd.Of(World!)!;
        var map = crowd.Map;
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

        foreach (var a in crowd.Agents)
        {
            var c = new Vector2(a.Position.X.ToSingle(), a.Position.Y.ToSingle());
            var done = a.Status is NavAgentStatus.Arrived or NavAgentStatus.ArrivedShort;
            d.FillCircle(c, a.Radius.ToSingle(), done ? Color.Rgb(0x6fcf97) : Color.Rgb(0xf2c14e), 5);
            if (d.DebugVisible)
            {
                d.Line(c, new Vector2(a.Destination.X.ToSingle(), a.Destination.Y.ToSingle()), Color.Rgba(0xffffff40), 1, 4);
            }
        }
    }
}

public sealed class CrowdGame : GameDefinition
{
    public override string Name => "Crowd";

    public override void RegisterEntities(EntityRegistry registry) => OneGame.Generated.CrowdExample.EntityTypes.Register(registry);

    // Tables and systems are added before a new game starts and before a save is loaded, in the same order on every node.
    public override void RegisterSystems(World world) => NavCrowd.AddTo(world, Room.Map(), new NavCrowdOptions { AgentRadius = Fixed.FromInt(3), MaxAgents = 64 });

    public override void Setup(World world) => world.Add(new Plaza());

    public override void DefineActions(ActionMap actions) => actions.Context("play", c => c.Button("go", Bind.Key(Key.Space)));

    // Space sends everyone to the far side (or back).
    public override void OnEachFrame(GameInstance game)
    {
        if (!game.Actions.Pressed("go"))
        {
            return;
        }

        var plaza = (Plaza)game.World.Entities[0];
        var crowd = NavCrowd.Of(game.World)!;
        var east = crowd.Agents.Length > 0 && crowd.Agents[0].Position.X.ToInt() < 0;
        foreach (var a in crowd.Agents)
        {
            plaza.Send.GoTo(a.Id, east ? 16 + (a.Id / 6) : 1 + (a.Id / 6), 1 + ((a.Id % 6) * 2));
        }
    }

    public override void DefineScreens(ScreenSet screens) => screens.Add("main", _ => Ui.Column());
}
