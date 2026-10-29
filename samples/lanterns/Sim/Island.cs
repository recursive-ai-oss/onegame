using OneGame.Core;

namespace Lanterns;

/// <summary>One per island scope: the island's counters, the place new players arrive, and the supply of lanterns on the ground.</summary>
[Entity]
public partial class Island : Entity
{
    /// <summary>Owned by server code (the identity servers use towards each other), so only a server can join or remove players.</summary>
    [Synced, OwnerKey] public partial uint Keeper { get; set; }

    /// <summary>The scope this island is.</summary>
    [Synced] public partial uint ScopeNo { get; set; }

    /// <summary>Lanterns ever picked up here. Held lanterns, lanterns in a swap and lanterns that left with their players (<see cref="Departed"/>) always equal the sum of these over the islands.</summary>
    [Synced] public partial long Collected { get; set; }

    /// <summary>Lanterns that left the game in the pockets of players who disconnected from this island.</summary>
    [Synced] public partial long Departed { get; set; }

    /// <summary>Players who arrived over the bridge from the other island.</summary>
    [Synced] public partial long Arrivals { get; set; }

    /// <summary>The number of the last player who joined here.</summary>
    [Synced] public partial uint NextPlayer { get; set; }

    /// <summary>False on an island with no neighbour (single player): the bridge ends at the water.</summary>
    [Synced] public partial bool Linked { get; set; }

    /// <summary>Puts a new player on the island.</summary>
    [Command(OwnerOnly = true)]
    public CommandReply Join(int player, string name)
    {
        var w = World!;
        foreach (var e in w.Entities)
        {
            if (e is Player p && p.Owner == (uint)player)
            {
                return CommandReply.Reject("already here");
            }
        }

        var serial = (uint)player % 100_000;
        NextPlayer = serial > NextPlayer ? serial : NextPlayer;
        w.Add(new Player
        {
            Owner = (uint)player,
            Name = name,
            Island = ScopeNo,
            X = Rules.Width / 2,
            Y = 32 + (int)(serial % 6 * 24),
            Mine = true,
        });
        return CommandReply.Ok;
    }

    /// <summary>
    /// Takes a player out of the game because their connection is gone. Safe to repeat: a player who is not here is fine. A player who is on the
    /// bridge or has a swap open is turned away for now, so the hand-over or swap that holds their lanterns can finish first; the caller asks again.
    /// </summary>
    [Command(OwnerOnly = true)]
    public CommandReply Leave(int player)
    {
        var w = World!;
        foreach (var e in w.Entities)
        {
            // Several Leave commands can arrive in one tick (the other island repeats its request until it sees the player gone). The
            // player removed by the first is no longer listed, so the others find nobody and add nothing to Departed.
            if (e is Player p && p.Owner == (uint)player)
            {
                if (p.Dest != 0 || p.Pending != 0)
                {
                    return CommandReply.Reject("busy");
                }

                Departed += p.Total;
                w.Remove(p.Id);
                break;
            }
        }

        return CommandReply.Ok;
    }

    /// <summary>Keeps the ground stocked.</summary>
    protected override void OnTick()
    {
        var w = World!;
        if (w.Role != WorldRole.Authority || w.Tick % Rules.SpawnEvery != 0)
        {
            return;
        }

        var onGround = 0;
        foreach (var e in w.Entities)
        {
            if (e is Lantern)
            {
                onGround++;
            }
        }

        if (onGround < Rules.GroundTarget)
        {
            var rng = w.Rng["spawn"];
            w.Add(new Lantern { X = rng.NextInt(16, Rules.Width - 16), Y = rng.NextInt(16, Rules.Height - 16), Color = rng.NextInt(Rules.Colors) });
        }
    }

    /// <summary>The island entity of a world, or null.</summary>
    public static Island? Of(World world)
    {
        foreach (var e in world.Entities)
        {
            if (e is Island i)
            {
                return i;
            }
        }

        return null;
    }
}

/// <summary>A lantern lying on the ground.</summary>
[Entity]
public partial class Lantern : Entity
{
    /// <summary>Position in units.</summary>
    [Synced] public partial int X { get; set; }

    /// <summary>Position in units.</summary>
    [Synced] public partial int Y { get; set; }

    /// <summary>0 red, 1 blue, 2 gold.</summary>
    [Synced] public partial int Color { get; set; }

    /// <summary>Set the moment a player takes it, so two players on one lantern cannot both get it.</summary>
    [Synced] public partial bool Taken { get; set; }
}
