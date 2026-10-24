using OneGame.Core.Tests.Samples;

namespace OneGame.Sync.Tests;

/// <summary>The shop-and-bots game the sync tests run: a shop, wandering bot entities and a hot table of bots, driven by a scripted player.</summary>
internal static class Scenario
{
    public const ulong Seed = 42;
    public static readonly EntityId ShopId = new(1);

    public static EntityRegistry Registry()
    {
        var registry = new EntityRegistry();
        registry.Add(new Shop().TypeId, "Shop", static () => new Shop());
        registry.Add(new Bot().TypeId, "Bot", static () => new Bot());
        return registry;
    }

    /// <summary>An empty world with the game's tables and systems. Authority worlds are filled by <see cref="Populate"/>.</summary>
    public static World NewWorld(WorldRole role)
    {
        var world = new World(Seed, Registry(), role);
        var table = world.AddTable(new BotRowTable());
        world.AddSystem(new BotSystem(table));
        return world;
    }

    public static World NewAuthority(int bots = 8, int hotBots = 100)
    {
        var world = NewWorld(WorldRole.Authority);
        Populate(world, bots, hotBots);
        return world;
    }

    public static void Populate(World world, int bots, int hotBots)
    {
        var shop = world.Add(new Shop { Name = "Corner Shop", Stock = 500, Open = true });
        shop.Items.Add(new ItemSlot { ItemId = 1, Count = 400 });
        shop.Items.Add(new ItemSlot { ItemId = 2, Count = 100, Tag = Mood.Eager });
        shop.Price = new Timed<Fixed>(1, 2, 0, 5);
        for (var i = 0; i < bots; i++)
        {
            world.Add(new Bot { Position = new FVec2(10 + i, 20 + (2 * i)), Velocity = new FVec2(Fixed.FromRatio(1, 4), Fixed.FromRatio(-1, 8)), Hp = 20 });
        }

        var table = (BotRowTable)world.Tables[0];
        for (var i = 0; i < hotBots; i++)
        {
            table.Add(new BotRow { X = i % 50, Y = (i * 7) % 50, Vx = Fixed.FromRatio((i % 5) - 2, 8), Vy = Fixed.FromRatio(1, 8), Hp = 10 });
        }
    }

    /// <summary>The scripted player's input for the given authority tick, sent through whichever world is passed (the replica in a cluster).</summary>
    public static void Input(World view, long tick)
    {
        if (view.Find(ShopId) is not Shop shop)
        {
            return;
        }

        if (tick % 7 == 0)
        {
            shop.Send.Buy(1 + (int)(tick % 2), 1 + (int)(tick % 3));
        }

        if (tick % 31 == 0)
        {
            shop.Send.Restock(new ItemSlot { ItemId = 1, Count = 5 });
        }

        if (tick % 53 == 0 && view.Find(new EntityId(2)) is Bot bot)
        {
            bot.Send.Heal(3);
        }

        if (tick % 97 == 0)
        {
            shop.Send.Rename("Shop " + tick);
        }
    }

    /// <summary>A one-node cluster: the authority and one loopback replica, settled.</summary>
    public static (Cluster Cluster, AuthorityScope Scope, ReplicaScope View) NewCluster(World? authority = null, ClusterOptions? options = null)
    {
        var cluster = new Cluster(options ?? new ClusterOptions { DriftCheckInterval = 1, FatalDrift = true });
        var host = cluster.AddNode("host");
        var scope = cluster.AddScope(host, new ScopeId(1), authority ?? NewAuthority());
        var view = cluster.AddLocalReplica(scope, NewWorld(WorldRole.Replica), Trust.Player(scope.Id));
        cluster.Settle();
        return (cluster, scope, view);
    }

    /// <summary>Runs the scripted player for <paramref name="ticks"/> ticks, one cluster step at a time.</summary>
    public static void Play(Cluster cluster, AuthorityScope scope, ReplicaScope view, int ticks, Action? afterTick = null)
    {
        for (var i = 0; i < ticks; i++)
        {
            Input(view.World, scope.World.Tick);
            cluster.Step();
            afterTick?.Invoke();
        }
    }
}

internal static class CommandIds
{
    public static uint Buy => OneGame.Core.StateHash.Fnv32("Buy");
}
