using System.Security.Cryptography;
using OneGame.Core.Tests.Samples;

namespace OneGame.Core.Tests.Fuzzing;

/// <summary>
/// The decoders that read untrusted bytes, wrapped as functions of a byte array. The fuzzer and the regression test call the same
/// functions. A target returns normally for any input; a decoder that throws, hangs or allocates far beyond its input is a bug.
/// </summary>
public static class FuzzTargets
{
    public sealed class Target(string name, Func<byte[], bool> run, Func<IEnumerable<byte[]>> seeds)
    {
        public string Name { get; } = name;

        /// <summary>Runs the target; returns true if the input decoded successfully.</summary>
        public bool Run(byte[] input) => run(input);

        public IEnumerable<byte[]> Seeds() => seeds();
    }

    public static EntityRegistry Registry()
    {
        var registry = new EntityRegistry();
        registry.Add(new Shop().TypeId, "Shop", static () => new Shop());
        registry.Add(new Bot().TypeId, "Bot", static () => new Bot());
        return registry;
    }

    public static World NewWorld(out BotRowTable table)
    {
        var world = new World(1, Registry());
        table = world.AddTable(new BotRowTable());
        return world;
    }

    private static World Populated()
    {
        var world = NewWorld(out var table);
        var shop = world.Add(new Shop { Name = "Fuzz Shop", Stock = 4, Open = true, Mood = Mood.Angry });
        shop.Items.Add(new ItemSlot { ItemId = 3, Count = 9, Tag = Mood.Eager });
        shop.Items.Add(new ItemSlot { ItemId = -4, Count = 1 });
        shop.Price = new Timed<Fixed>(1, 9, 3, 30);
        world.Add(new Bot { Position = new FVec2(3, 4), Velocity = new FVec2(Fixed.FromRatio(1, 3), -1), Hp = 9 });
        for (var i = 0; i < 6; i++)
        {
            table.Add(new BotRow { X = i, Y = -i, Vx = Fixed.FromRatio(i, 7), Vy = 1, Hp = 5 - i });
        }

        for (var i = 0; i < 5; i++)
        {
            world.Step();
        }

        return world;
    }

    private static byte[] Snapshot(World world)
    {
        var w = new SyncWriter();
        world.WriteSnapshot(w);
        return w.ToArray();
    }

    public static IReadOnlyList<Target> All { get; } =
    [
        new Target("snapshot", SnapshotRestore, () =>
        {
            var empty = Snapshot(NewWorld(out _));
            return [Snapshot(Populated()), empty];
        }),
        new Target("entity-state", EntityState, () =>
        {
            var w = new SyncWriter();
            var shop = (Shop)Populated().Entities[0];
            w.WriteByte(2); // schema selector: current
            shop.WriteState(w);
            var old = new SyncWriter();
            old.WriteByte(1); // schema selector: version 1
            old.WriteString("Old");
            old.WriteInt32(12);
            old.WriteVarUInt(1);
            old.WriteInt32(1); old.WriteInt32(2); old.WriteByte(1);
            return [w.ToArray(), old.ToArray()];
        }),
        new Target("entity-delta", EntityDelta, () =>
        {
            var world = Populated();
            var shop = (Shop)world.Entities[0];
            var other = (Shop)Populated().Entities[0];
            other.Name = "Changed";
            other.Items.Add(new ItemSlot { ItemId = 8, Count = 8 });
            other.Stock = 400;
            var w = new SyncWriter();
            other.WriteDeltaFrom(shop, w);
            var dirty = new SyncWriter();
            other.WriteDirtyDelta(dirty);
            return [w.ToArray(), dirty.ToArray()];
        }),
        new Target("command", CommandWire, () =>
        {
            var world = Populated();
            var shop = (Shop)world.Entities[0];
            var wire = new List<byte[]>();
            world.Observer = (_, entity, id, payload, _) =>
            {
                var w = new SyncWriter();
                World.EncodeCommand(w, entity, id, payload);
                wire.Add(w.ToArray());
            };
            shop.Send.Buy(3, 2);
            shop.Send.Restock(new ItemSlot { ItemId = 5, Count = 5 });
            shop.Send.Rename("Fuzzed");
            ((Bot)world.Entities[1]).Send.Heal(2);
            world.Step();
            return wire;
        }),
        new Target("table", TableRead, () =>
        {
            var w = new SyncWriter();
            NewWorldWithRows().Write(w);
            return [w.ToArray()];
        }),
    ];

    private static BotRowTable NewWorldWithRows()
    {
        var table = new BotRowTable();
        for (var i = 0; i < 8; i++)
        {
            table.Add(new BotRow { X = i, Y = 2 * i, Vx = -i, Vy = 3, Hp = i });
        }

        return table;
    }

    private static bool SnapshotRestore(byte[] input)
    {
        var world = NewWorld(out _);
        if (world.ReadSnapshot(input) != SyncError.None)
        {
            return false;
        }

        // Whatever decodes must also re-encode and decode to the same state.
        var hash = world.ComputeHash();
        _ = world.DumpJson();
        var again = NewWorld(out _);
        if (again.ReadSnapshot(Snapshot(world)) != SyncError.None || again.ComputeHash() != hash)
        {
            throw new InvalidOperationException("a decoded snapshot did not survive re-encoding");
        }

        return true;
    }

    private static bool EntityState(byte[] input)
    {
        if (input.Length == 0)
        {
            return false;
        }

        var shop = new Shop();
        var reader = new SyncReader(input.AsSpan(1));
        shop.ReadState(input[0] % 3u, ref reader);
        reader.ExpectEnd();
        if (!reader.Ok)
        {
            return false;
        }

        var w = new SyncWriter();
        shop.WriteState(w);
        var copy = new Shop();
        var r = new SyncReader(w.Written);
        copy.ReadState(ref r);
        if (!r.Ok || copy.ComputeHash() != shop.ComputeHash())
        {
            throw new InvalidOperationException("decoded entity state did not survive re-encoding");
        }

        return true;
    }

    private static bool EntityDelta(byte[] input)
    {
        var shop = (Shop)Populated().Entities[0];
        var before = shop.ComputeHash();
        var reader = new SyncReader(input);
        shop.ApplyDelta(ref reader);
        if (!reader.Ok)
        {
            // A delta that fails to decode must leave the entity untouched.
            if (shop.ComputeHash() != before)
            {
                throw new InvalidOperationException("a refused delta changed the entity");
            }

            return false;
        }

        reader.ExpectEnd();
        var w = new SyncWriter();
        shop.WriteState(w);
        var r = new SyncReader(w.Written);
        new Shop().ReadState(ref r);
        if (!r.Ok)
        {
            throw new InvalidOperationException("state after a delta did not re-encode");
        }

        return true;
    }

    private static bool CommandWire(byte[] input)
    {
        var world = Populated();
        var status = world.SubmitEncoded(input);
        world.Step();
        return status == CommandStatus.Queued;
    }

    private static bool TableRead(byte[] input)
    {
        var table = new BotRowTable();
        var reader = new SyncReader(input);
        table.Read(ref reader);
        reader.ExpectEnd();
        return reader.Ok;
    }

    public static string Digest(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes))[..12].ToLowerInvariant();
}
