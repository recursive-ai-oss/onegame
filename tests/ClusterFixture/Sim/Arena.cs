using OneGame.Core;

namespace ClusterFixture;

/// <summary>A shared scoreboard that players hit and that heats up over time: integers, fixed-point, text and a random roll, so frames carry every kind of delta.</summary>
[Entity]
public partial class Arena : Entity
{
    [Synced] public partial int Hits { get; private set; }

    [Synced] public partial int Ticks { get; private set; }

    [Synced] public partial Fixed Heat { get; private set; }

    [Synced] public partial string LastBy { get; private set; }

    [Command]
    public void Hit(int amount, string who)
    {
        Hits += amount;
        LastBy = who;
    }

    protected override void OnTick()
    {
        Ticks++;
        if (World!.Tick % 3 == 0)
        {
            Heat += Fixed.FromRatio(World.Rng["heat"].NextInt(1, 8), 16);
        }
    }
}

/// <summary>One of the players' marks on the arena: a score per fighter.</summary>
[Entity]
public partial class Fighter : Entity
{
    [Synced] public partial int Score { get; private set; }

    [Synced] public partial int Streak { get; private set; }

    [Command]
    public void AddScore(int points)
    {
        Score += points;
        Streak = points > 0 ? Streak + 1 : 0;
    }
}

/// <summary>Where the arena starts.</summary>
public static class ArenaWorld
{
    /// <summary>Adds the arena and two fighters.</summary>
    public static void Seed(World world)
    {
        world.Add(new Arena());
        world.Add(new Fighter());
        world.Add(new Fighter());
    }
}
