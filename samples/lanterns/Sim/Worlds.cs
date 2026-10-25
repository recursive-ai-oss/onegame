using OneGame.Core;
using OneGame.Sync;

namespace Lanterns;

/// <summary>How the three kinds of scope are built. Every program that holds a copy of one of them calls the same code, so the tables line up.</summary>
public static class Worlds
{
    /// <summary>Registers every entity type of the game, including the swap desk.</summary>
    public static void Register(EntityRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        OneGame.Generated.Lanterns_Sim.EntityTypes.Register(registry);
        Escrow.Register(registry);
    }

    /// <summary>Adds the hidden tables an authority world needs: the hand-over log on islands, and the swap book on every scope. Replicas need none of it.</summary>
    public static void Build(World world, ScopeId scope)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (world.Role != WorldRole.Authority)
        {
            return;
        }

        if (scope.Value != Rules.Master)
        {
            TransferLog.AddTo(world);
        }

        Escrow.Install(world, new LanternPurse());
    }

    /// <summary>Creates the starting entities of a scope.</summary>
    public static void Seed(World world, ScopeId scope)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (scope.Value == Rules.Master)
        {
            world.Add(new Market { Keeper = Rules.ServerPlayer });
            return;
        }

        world.Add(new Island { Keeper = Rules.ServerPlayer, ScopeNo = scope.Value });
        var rng = world.Rng["spawn"];
        for (var i = 0; i < Rules.GroundTarget / 2; i++)
        {
            world.Add(new Lantern { X = rng.NextInt(16, Rules.Width - 16), Y = rng.NextInt(16, Rules.Height - 16), Color = rng.NextInt(Rules.Colors) });
        }
    }

    /// <summary>The single-player world: island one with the player and a walker to swap with.</summary>
    public static void SeedSinglePlayer(World world)
    {
        ArgumentNullException.ThrowIfNull(world);
        Seed(world, new ScopeId(Rules.IslandA));
        var island = Island.Of(world)!;
        island.Join(1, "You");
        island.Join(2, "Ada");
        foreach (var e in world.Entities)
        {
            if (e is Player p && p.Owner == 2)
            {
                p.Auto = true;
            }
        }
    }
}
