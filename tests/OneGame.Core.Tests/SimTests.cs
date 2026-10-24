using OneGame.Core.Tests.Samples;

namespace OneGame.Core.Tests;

public class SimTests
{
    private static EntityRegistry Registry()
    {
        var registry = new EntityRegistry();
        OneGame.Generated.OneGame_Core_Tests.EntityTypes.Register(registry);
        return registry;
    }

    private static (World World, BotRowTable Table) Build(ulong seed, int entityBots, int hotBots)
    {
        var world = new World(seed, Registry());
        var table = world.AddTable(new BotRowTable(hotBots));
        world.AddSystem(new BotSystem(table));
        var shop = world.Add(new Shop { Name = "S", Stock = 100 });
        shop.Items.Add(new ItemSlot { ItemId = 1, Count = 1000 });
        shop.Price = new Timed<Fixed>(1, 2, 0, 5);
        for (var i = 0; i < entityBots; i++)
        {
            world.Add(new Bot { Position = new FVec2(i, i * 2), Velocity = new FVec2(Fixed.FromRatio(1, 4), Fixed.FromRatio(-1, 8)), Hp = 20 });
        }

        for (var i = 0; i < hotBots; i++)
        {
            table.Add(new BotRow { X = i % 100, Y = (i * 7) % 100, Vx = Fixed.FromRatio((i % 5) - 2, 4), Vy = Fixed.FromRatio((i % 3) - 1, 2), Hp = 30 });
        }

        return (world, table);
    }

    /// <summary>The scripted scenario: a command every few ticks, from a fixed list.</summary>
    private static void Drive(World world, int tick)
    {
        var shop = (Shop)world.Entities[0];
        if (tick % 7 == 0)
        {
            shop.Send.Buy(1, 1 + (tick % 3));
        }

        if (tick % 11 == 0)
        {
            shop.Send.Restock(new ItemSlot { ItemId = 1, Count = 5 });
        }

        if (tick % 13 == 0 && world.Entities.Count > 1)
        {
            ((Bot)world.Entities[1]).Send.Heal(3);
        }
    }

    private static ulong[] Run(ulong seed, int ticks)
    {
        var (world, _) = Build(seed, 20, 200);
        var hashes = new ulong[ticks];
        for (var t = 0; t < ticks; t++)
        {
            Drive(world, t);
            world.Step();
            hashes[t] = world.ComputeHash();
        }

        return hashes;
    }

    [Fact]
    public void SameSeedAndCommandsGiveSameHashAtEveryTick()
    {
        var a = Run(42, 300);
        var b = Run(42, 300);
        Assert.Equal(a, b);
        Assert.Equal(300, a.Distinct().Count()); // the state really changes every tick
        Assert.NotEqual(a, Run(43, 300));
    }

    [Fact]
    public void SnapshotHalfwayThenRunningOnMatchesStraightThrough()
    {
        var expected = Run(5, 200);
        var (world, _) = Build(5, 20, 200);
        for (var t = 0; t < 100; t++)
        {
            Drive(world, t);
            world.Step();
        }

        var w = new SyncWriter();
        world.WriteSnapshot(w);
        var (restored, _) = Build(5, 0, 0);
        // The restore target has the same tables and systems, and no entities yet.
        Assert.Equal(SyncError.None, restored.ReadSnapshot(w.Written));
        Assert.Equal(expected[99], restored.ComputeHash());
        for (var t = 100; t < 200; t++)
        {
            Drive(restored, t);
            restored.Step();
            Assert.Equal(expected[t], restored.ComputeHash());
        }
    }

    [Fact]
    public void CommandLogReplayReproducesTheRun()
    {
        var (live, _) = Build(9, 5, 50);
        var log = new List<(long Tick, byte[] Wire)>();
        live.Observer = (tick, entity, id, payload, _) =>
        {
            var w = new SyncWriter();
            World.EncodeCommand(w, entity, id, payload);
            log.Add((tick, w.ToArray()));
        };
        for (var t = 0; t < 150; t++)
        {
            Drive(live, t);
            live.Step();
        }

        var (replay, _) = Build(9, 5, 50);
        var next = 0;
        for (var t = 0; t < 150; t++)
        {
            while (next < log.Count && log[next].Tick == replay.Tick)
            {
                Assert.Equal(CommandStatus.Queued, replay.SubmitEncoded(log[next].Wire));
                next++;
            }

            replay.Step();
        }

        Assert.NotEmpty(log);
        Assert.Equal(live.ComputeHash(), replay.ComputeHash());
    }

    [Fact]
    public void HotTableTicksAllocateNothing()
    {
        var (world, table) = Build(1, 0, 1000);
        Assert.Equal(1000, table.Count);
        for (var i = 0; i < 120; i++)
        {
            world.Step(); // warm up: JIT, stream creation
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 600; i++)
        {
            world.Step();
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(0, allocated);
    }

    [Fact]
    public void HashingAllocatesNothingOnceWarm()
    {
        var (world, _) = Build(1, 0, 1000);
        world.Step();
        _ = world.ComputeHash();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 20; i++)
        {
            _ = world.ComputeHash();
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    [Fact]
    public void TableAddRemoveAndRoundTrip()
    {
        var table = new BotRowTable(2);
        for (var i = 0; i < 5; i++)
        {
            table.Add(new BotRow { X = i, Hp = i });
        }

        table.RemoveAtSwap(1);
        Assert.Equal([0, 4, 2, 3], table.Hp.ToArray());
        var w = new SyncWriter();
        table.Write(w);
        var copy = new BotRowTable();
        var r = new SyncReader(w.Written);
        copy.Read(ref r);
        r.ExpectEnd();
        Assert.True(r.Ok);
        Assert.Equal(table.Hp.ToArray(), copy.Hp.ToArray());
        Assert.Equal(table.Get(2), copy.Get(2));
    }
}
