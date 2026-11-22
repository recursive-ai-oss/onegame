using OneGame.Core;
using OneGame.Host;
using OneGame.UI;

namespace EventsExample;

/// <summary>Something that happened on one tick. The sim emits it; views, sound and tests react to it once.</summary>
[SimEvent]
public partial struct WallDone
{
    public int Cell { get; set; }
}

/// <summary>An event only the owner of the emitting entity receives (everyone else's connection never carries it).</summary>
[SimEvent(Visibility = Visibility.Owner)]
public partial struct Bonus
{
    public int Points { get; set; }
}

[Entity]
public partial class Builder : Entity
{
    [Synced, OwnerKey] public partial uint Player { get; set; }

    [Synced] public partial int Walls { get; private set; }

    [Command]
    public void Build(int cell)
    {
        Walls++;
        Emit(new WallDone { Cell = cell });
        if (Walls % 3 == 0)
        {
            Emit(new Bonus { Points = Walls * 10 });
        }
    }
}

public sealed class EventsGame : GameDefinition
{
    public override string Name => "Events";

    public override void RegisterEntities(EntityRegistry registry) => OneGame.Generated.EventsExample.EntityTypes.Register(registry);

    public override void Setup(World world) => world.Add(new Builder { Player = 1 });

    public override void DefineScreens(ScreenSet screens) => screens.Add("main", game =>
    {
        // One reader per consumer. It starts at the end of the log, so it only sees what happens from now on, and it is made again when
        // the game's world is replaced (a loaded save, a reset), because a new world has a new log.
        var world = game.World;
        var reader = world.Events.NewReader();
        int done = 0, lastCell = 0, points = 0;
        string Status()
        {
            if (!ReferenceEquals(world, game.World))
            {
                world = game.World;
                reader = world.Events.NewReader();
                done = lastCell = points = 0;
            }

            while (reader.Next(out var e))
            {
                if (e.TryRead(out WallDone wall))
                {
                    done++;
                    lastCell = wall.Cell;
                }
                else if (e.TryRead(out Bonus bonus))
                {
                    points += bonus.Points;
                }
            }

            return $"Walls done: {done}, last cell {lastCell}, bonus {points}";
        }

        return Ui.Column(
            Ui.Label(Status).Named("status"),
            Ui.Button("Build at 7", () => ((Builder)game.World.Entities[0]).Send.Build(7)).Named("seven"),
            Ui.Button("Build at 12", () => ((Builder)game.World.Entities[0]).Send.Build(12)).Named("twelve"),
            Ui.Button("Save", () => game.Storage.Save("slot1", "Walls")).Named("save"),
            Ui.Button("Load", () => game.Storage.Load("slot1")).Named("load"))
            .Padding(Length.Px(16)).Gap(Length.Px(8)).TextColor(Color.White);
    });
}
