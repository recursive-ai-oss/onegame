using OneGame.Core.Tests.Samples;

namespace OneGame.Core.Tests;

public class EntityTests
{
    private static EntityRegistry Registry()
    {
        var registry = new EntityRegistry();
        OneGame.Generated.OneGame_Core_Tests.EntityTypes.Register(registry);
        return registry;
    }

    private static World NewWorld(ulong seed = 7) => new(seed, Registry());

    private static Shop MakeShop(World world)
    {
        var shop = world.Add(new Shop { Name = "Corner Store", Stock = 12, Open = true, Mood = Mood.Eager });
        shop.Items.Add(new ItemSlot { ItemId = 1, Count = 5, Tag = Mood.Calm });
        shop.Items.Add(new ItemSlot { ItemId = 2, Count = 7, Tag = Mood.Angry });
        shop.Price = new Timed<Fixed>(Fixed.FromInt(3), Fixed.FromInt(5), 10, 20);
        return shop;
    }

    [Fact]
    public void CommandsCanBeSentByNameWithTextArguments()
    {
        var world = NewWorld();
        var shop = MakeShop(world);

        // A struct parameter has no text form, so Restock is not offered; the others are.
        Assert.Equal(["Buy(int itemId, int count)", "Rename(string name)"], shop.CommandSignatures);
        Assert.Equal(CommandStatus.UnknownCommand, shop.SendFromText("Restock", ["1"]));
        Assert.Equal(CommandStatus.UnknownCommand, shop.SendFromText("Nope", []));

        Assert.Equal(CommandStatus.Queued, shop.SendFromText("Buy", ["1", "2"]));
        Assert.Equal(CommandStatus.Queued, shop.SendFromText("Rename", ["Big Store"]));
        Assert.Equal(CommandStatus.Malformed, shop.SendFromText("Buy", ["1"]));
        Assert.Equal(CommandStatus.Malformed, shop.SendFromText("Buy", ["one", "2"]));
        Assert.Equal(CommandStatus.Malformed, shop.SendFromText("Buy", ["1", "99999999999"]));

        world.Step();
        Assert.Equal("Big Store", shop.Name);
        Assert.Equal(10, shop.Stock);
    }

    [Fact]
    public void CodecRoundTripsAndHashesMatch()
    {
        var world = NewWorld();
        var shop = MakeShop(world);
        var writer = new SyncWriter();
        shop.WriteState(writer);

        var copy = new Shop();
        var reader = new SyncReader(writer.Written);
        copy.ReadState(ref reader);
        reader.ExpectEnd();

        Assert.True(reader.Ok, reader.Error.ToString());
        Assert.Equal("Corner Store", copy.Name);
        Assert.Equal(12, copy.Stock);
        Assert.Equal(Mood.Eager, copy.Mood);
        Assert.Equal(2, copy.Items.Count);
        Assert.Equal(shop.Price, copy.Price);
        Assert.Equal(0UL, copy.DirtyMask);

        var other = NewWorld();
        other.AddWithId(copy, shop.Id);
        Assert.Equal(shop.ComputeHash(), copy.ComputeHash());
    }

    [Fact]
    public void SnapshotRestoreKeepsTheWorldHash()
    {
        var world = NewWorld();
        MakeShop(world);
        world.Add(new Bot { Position = new FVec2(5, 6), Velocity = new FVec2(Fixed.FromRatio(1, 3), 0), Hp = 10 });
        for (var i = 0; i < 40; i++)
        {
            world.Step();
        }

        var w = new SyncWriter();
        world.WriteSnapshot(w);

        var restored = NewWorld(999);
        Assert.Equal(SyncError.None, restored.ReadSnapshot(w.Written));
        Assert.Equal(world.ComputeHash(), restored.ComputeHash());
        Assert.Equal(world.Tick, restored.Tick);
        Assert.Equal(world.DumpJson().Replace("\"seed\":7", string.Empty), restored.DumpJson().Replace("\"seed\":999", string.Empty));

        // The restored world carries on exactly like the original.
        for (var i = 0; i < 40; i++)
        {
            world.Step();
            restored.Step();
            Assert.Equal(world.ComputeHash(), restored.ComputeHash());
        }
    }

    [Fact]
    public void DeltaFromBaselineAppliesToGiveTheNewState()
    {
        var a = NewWorld();
        var b = NewWorld();
        var shopA = MakeShop(a);
        var shopB = MakeShop(b);
        Assert.Equal(shopA.ComputeHash(), shopB.ComputeHash());

        shopB.Name = "Renamed";
        shopB.Stock = 99;
        shopB.Items.RemoveAt(0);
        shopB.Items.Add(new ItemSlot { ItemId = 9, Count = 1 });
        Assert.NotEqual(shopA.ComputeHash(), shopB.ComputeHash());

        var w = new SyncWriter();
        Assert.True(shopB.WriteDeltaFrom(shopA, w));
        var full = new SyncWriter();
        shopB.WriteState(full);
        Assert.True(w.Length < full.Length, "a delta of three changed properties is smaller than the full state");

        shopA.ClearDirty();
        var reader = new SyncReader(w.Written);
        shopA.ApplyDelta(ref reader);
        reader.ExpectEnd();
        Assert.True(reader.Ok);
        Assert.Equal(shopB.ComputeHash(), shopA.ComputeHash());
        Assert.Equal(1UL | (1UL << 2) | (1UL << 6), shopA.DirtyMask);
    }

    [Fact]
    public void DirtyDeltaCarriesOnlyWhatChangedSinceClear()
    {
        var world = NewWorld();
        var shop = MakeShop(world);
        shop.ClearDirty();
        Assert.Equal(0UL, shop.DirtyMask);
        shop.Stock = 3;
        shop.Stock = 3;
        shop.Mood = Mood.Angry;
        Assert.Equal((1UL << 2) | (1UL << 4), shop.DirtyMask);

        var replicaWorld = NewWorld();
        var replica = MakeShop(replicaWorld);
        var w = new SyncWriter();
        shop.WriteDirtyDelta(w);
        var r = new SyncReader(w.Written);
        replica.ApplyDelta(ref r);
        Assert.True(r.Ok);
        Assert.Equal(shop.ComputeHash(), replica.ComputeHash());

        shop.ClearDirty();
        w.Reset();
        shop.WriteDirtyDelta(w);
        Assert.Equal(1, w.Length); // just the empty mask
    }

    [Fact]
    public void SchemaOneSnapshotLoadsThroughTheDeclaredMigration()
    {
        // A snapshot written by the schema-1 build: name, whole gold, items.
        var body = new SyncWriter();
        body.WriteString("Old Shop");
        body.WriteInt32(250);
        body.WriteVarUInt(2);
        body.WriteInt32(4); body.WriteInt32(10); body.WriteByte((byte)Mood.Eager);
        body.WriteInt32(5); body.WriteInt32(1); body.WriteByte((byte)Mood.Calm);

        var shopType = new Shop().TypeId;
        var w = new SyncWriter();
        w.WriteFixed32(0x4E53474F);
        w.WriteVarUInt(1);
        w.WriteVarInt(12); // tick
        w.WriteVarUInt(2); // next id
        w.WriteVarUInt(0); // no rng streams
        w.WriteVarUInt(1); // one entity
        w.WriteFixed32(shopType);
        w.WriteVarUInt(1);
        w.WriteVarUInt(1); // schema 1
        w.WriteBytes(body.Written);
        w.WriteVarUInt(0); // no tables

        var world = NewWorld();
        Assert.Equal(SyncError.None, world.ReadSnapshot(w.Written));
        var shop = Assert.IsType<Shop>(world.Find(new EntityId(1)));
        Assert.Equal("Old Shop", shop.Name);
        Assert.Equal(Fixed.FromInt(250), shop.Gold);
        Assert.Equal(0, shop.Stock);
        Assert.Equal(2, shop.Items.Count);
        Assert.Equal(10, shop.Items[0].Count);
        Assert.Equal(0UL, shop.DirtyMask);

        // Written again it is current-schema, and round-trips.
        var again = new SyncWriter();
        world.WriteSnapshot(again);
        var world2 = NewWorld();
        Assert.Equal(SyncError.None, world2.ReadSnapshot(again.Written));
        Assert.Equal(world.ComputeHash(), world2.ComputeHash());
    }

    [Fact]
    public void UnknownSchemaVersionsAreRefusedNotGuessed()
    {
        var shop = new Shop();
        var r = new SyncReader(new byte[] { 0, 0, 0 });
        shop.ReadState(7, ref r);
        Assert.Equal(SyncError.UnsupportedVersion, r.Error);
    }

    [Fact]
    public void CommandsRunOnTheAuthorityNextTick()
    {
        var world = NewWorld();
        var shop = MakeShop(world);
        var seen = new List<(long, uint, CommandStatus)>();
        world.Observer = (tick, _, id, _, status) => seen.Add((tick, id, status));

        Assert.Equal(CommandStatus.Queued, shop.Send.Buy(1, 2));
        Assert.Equal(CommandStatus.Queued, shop.Send.Buy(1, 99));
        shop.Send.Rename("Bigger Store");
        Assert.Equal(5, shop.Items[0].Count); // nothing has run yet
        world.Step();

        Assert.Equal(3, shop.Items[0].Count);
        Assert.Equal(10, shop.Stock);
        Assert.Equal(Fixed.FromInt(10), shop.Gold);
        Assert.Equal("Bigger Store", shop.Name);
        Assert.Equal([CommandStatus.Executed, CommandStatus.Rejected, CommandStatus.Executed], seen.Select(s => s.Item3));
    }

    [Fact]
    public void BadCommandsAreStatusesNotExceptions()
    {
        var world = NewWorld();
        var shop = MakeShop(world);
        var statuses = new List<CommandStatus>();
        world.Observer = (_, _, _, _, status) => statuses.Add(status);

        world.Submit(new EntityId(999), 1, default);
        world.Submit(shop.Id, 0xDEADBEEF, default);
        world.Submit(shop.Id, EncodedId("Buy"), new byte[] { 2 }); // one argument short
        world.Submit(shop.Id, EncodedId("Rename"), new byte[] { 1, (byte)'x', 0 }); // trailing byte
        Assert.Equal(CommandStatus.Malformed, world.Submit(shop.Id, 1, new byte[SyncLimits.MaxCommandBytes + 1]));
        Assert.Equal(CommandStatus.Malformed, world.SubmitEncoded(new byte[] { 0x80 }));
        world.Step();
        Assert.Equal([CommandStatus.UnknownEntity, CommandStatus.UnknownCommand, CommandStatus.Malformed, CommandStatus.Malformed], statuses);
    }

    private static uint EncodedId(string name) => StateHash.Fnv32(name);

    [Fact]
    public void ReplicasRefuseToChangeStateOrTick()
    {
        var authority = NewWorld();
        MakeShop(authority);
        var w = new SyncWriter();
        authority.WriteSnapshot(w);

        var replica = new World(7, Registry(), WorldRole.Replica);
        Assert.Equal(SyncError.None, replica.ReadSnapshot(w.Written));
        var shop = Assert.IsType<Shop>(replica.Entities[0]);
        Assert.Throws<ReplicaWriteException>(() => shop.Stock = 5);
        Assert.Throws<ReplicaWriteException>(() => replica.Step());
        Assert.Equal(authority.ComputeHash(), replica.ComputeHash());

        var forwarded = new List<CommandStatus>();
        replica.Observer = (_, _, _, _, s) => forwarded.Add(s);
        Assert.Equal(CommandStatus.Forwarded, shop.Send.Buy(1, 1));
        Assert.Equal([CommandStatus.Forwarded], forwarded);
        Assert.Equal(12, shop.Stock);
    }

    [Fact]
    public void EntityOutsideAWorldCannotSendCommands()
        => Assert.Throws<NotInWorldException>(() => new Shop().Send.Rename("x"));

    [Fact]
    public void JsonDumpListsStateInDeclarationOrder()
    {
        var world = NewWorld();
        MakeShop(world);
        var json = world.DumpJson();
        Assert.Contains("{\"id\":1,\"type\":\"Shop\",\"Name\":\"Corner Store\",\"Gold\":0,\"Stock\":12,\"Open\":true,\"Mood\":1,"
            + "\"Price\":{\"from\":3,\"to\":5,\"t0\":10,\"t1\":20},\"Items\":[{\"ItemId\":1,\"Count\":5,\"Tag\":0},{\"ItemId\":2,\"Count\":7,\"Tag\":2}]}", json);
    }

    [Fact]
    public void StatePatchSetsScalarsFromText()
    {
        var world = NewWorld();
        var shop = MakeShop(world);
        Assert.True(shop.TrySetFromText("Name", "Patched"));
        Assert.True(shop.TrySetFromText("Gold", "12.5"));
        Assert.True(shop.TrySetFromText("Stock", "-4"));
        Assert.True(shop.TrySetFromText("Open", "false"));
        Assert.True(shop.TrySetFromText("Mood", "2"));
        Assert.Equal(("Patched", Fixed.FromDouble(12.5), -4, false, Mood.Angry), (shop.Name, shop.Gold, shop.Stock, shop.Open, shop.Mood));
        Assert.False(shop.TrySetFromText("Stock", "99999999999"));
        Assert.False(shop.TrySetFromText("Mood", "999"));
        Assert.False(shop.TrySetFromText("Nope", "1"));
        Assert.False(shop.TrySetFromText("Items", "1"));
    }

    [Fact]
    public void ReadOnlyViewExposesStateWithoutSetters()
    {
        var world = NewWorld();
        var shop = MakeShop(world);
        var view = shop.AsReadOnly;
        Assert.Equal(shop.Id, view.Id);
        Assert.Equal("Corner Store", view.Name);
        Assert.Equal(2, view.Items.Count);
        Assert.Equal("Corner Store: 2 stacks", shop.Label);
    }

    [Fact]
    public void RemovedEntitiesKeepTheirIdsRetired()
    {
        var world = NewWorld();
        var a = MakeShop(world);
        var b = MakeShop(world);
        Assert.True(world.Remove(a.Id));
        Assert.False(world.Remove(a.Id));
        Assert.Null(world.Find(a.Id));
        var c = MakeShop(world);
        Assert.True(c.Id.Value > b.Id.Value);
        Assert.Equal([b.Id, c.Id], world.Entities.Select(e => e.Id));
    }

    [Fact]
    public void RegisteringTwoTypesUnderOneIdIsRefused()
    {
        var registry = new EntityRegistry();
        registry.Add(5, "A", static () => new Bot());
        registry.Add(5, "A", static () => new Bot());
        Assert.Throws<DuplicateTypeException>(() => registry.Add(5, "B", static () => new Bot()));
    }
}
