using OneGame.Core.Tests.Samples;

namespace OneGame.Core.Tests;

public class RngAndTimedTests
{
    [Fact]
    public void StreamsAreSeededByNameAndIndependent()
    {
        var a = new World(1, new EntityRegistry());
        var b = new World(1, new EntityRegistry());
        Assert.Equal(a.Rng["loot"].NextUInt64(), b.Rng["loot"].NextUInt64());
        Assert.NotEqual(a.Rng["loot"].NextUInt64(), a.Rng["weather"].NextUInt64());

        // Creating another stream first changes nothing about "loot".
        var c = new World(1, new EntityRegistry());
        _ = c.Rng["aaa"].NextUInt64();
        _ = c.Rng["zzz"].NextUInt64();
        var d = new World(1, new EntityRegistry());
        Assert.Equal(d.Rng["loot"].NextUInt64(), c.Rng["loot"].NextUInt64());
        Assert.NotEqual(new World(2, new EntityRegistry()).Rng["loot"].NextUInt64(), new World(1, new EntityRegistry()).Rng["loot"].NextUInt64());
    }

    [Fact]
    public void GoldenSequence()
    {
        var rng = new World(123456789, new EntityRegistry()).Rng["golden"];
        var drawn = string.Join(",", Enumerable.Range(0, 4).Select(_ => rng.NextUInt64().ToString()));
        Assert.Equal("15058304859219535098,13537695482163401893,1650061634855746973,2927160555017114854", drawn);
    }

    [Fact]
    public void BoundedDrawsStayInRangeAndCoverIt()
    {
        var rng = new World(3, new EntityRegistry()).Rng["r"];
        var counts = new int[7];
        for (var i = 0; i < 70000; i++)
        {
            counts[rng.NextInt(7)]++;
        }

        Assert.All(counts, c => Assert.InRange(c, 9000, 11000));
        for (var i = 0; i < 1000; i++)
        {
            Assert.InRange(rng.NextInt(-5, 5), -5, 4);
            Assert.InRange(rng.NextFixed().Raw, 0, 1L << 32);
        }

        Assert.Throws<ArgumentOutOfRangeException>(() => rng.NextInt(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => rng.NextInt(5, 5));
    }

    [Fact]
    public void TimedInterpolatesAndClamps()
    {
        var t = new Timed<Fixed>(10, 20, 100, 200);
        Assert.Equal((Fixed)10, t.ValueAt(0));
        Assert.Equal((Fixed)10, t.ValueAt(100));
        Assert.Equal((Fixed)15, t.ValueAt(150));
        Assert.Equal((Fixed)20, t.ValueAt(200));
        Assert.Equal((Fixed)20, t.ValueAt(9999));

        var v = new Timed<FVec2>(new FVec2(0, 0), new FVec2(10, -10), 0, 10);
        Assert.Equal(new FVec2(5, -5), v.ValueAt(5));

        var instant = new Timed<Fixed>(1, 2, 5, 5);
        Assert.Equal((Fixed)1, instant.ValueAt(4));
        Assert.Equal((Fixed)2, instant.ValueAt(5));

        var w = new SyncWriter();
        t.Write(w);
        var r = new SyncReader(w.Written);
        Assert.Equal(t, SyncRead.Value<Timed<Fixed>>(ref r));
        Assert.True(r.Ok && r.AtEnd);
    }
}

public class DecodeHardeningTests
{
    private static byte[] ShopBytes()
    {
        var world = new World(1, new EntityRegistry());
        var shop = world.Add(new Shop { Name = "Hardening", Stock = 3 });
        shop.Items.Add(new ItemSlot { ItemId = 1, Count = 2 });
        world.Add(new Bot { Hp = 4 });
        var w = new SyncWriter();
        world.WriteSnapshot(w);
        return w.ToArray();
    }

    private static World Fresh()
    {
        var registry = new EntityRegistry();
        OneGame.Generated.OneGame_Core_Tests.EntityTypes.Register(registry);
        return new World(1, registry);
    }

    [Fact]
    public void EveryTruncationOfASnapshotIsAnErrorNotAnException()
    {
        var bytes = ShopBytes();
        for (var n = 0; n < bytes.Length; n++)
        {
            Assert.NotEqual(SyncError.None, Fresh().ReadSnapshot(bytes.AsSpan(0, n)));
        }

        Assert.Equal(SyncError.None, Fresh().ReadSnapshot(bytes));
    }

    [Fact]
    public void TrailingBytesAreRefused()
        => Assert.Equal(SyncError.TrailingBytes, Fresh().ReadSnapshot([.. ShopBytes(), 0]));

    [Fact]
    public void EverySingleByteCorruptionIsHandled()
    {
        var bytes = ShopBytes();
        for (var i = 0; i < bytes.Length; i++)
        {
            for (var v = 0; v < 256; v += 51)
            {
                var copy = (byte[])bytes.Clone();
                copy[i] = (byte)v;
                _ = Fresh().ReadSnapshot(copy); // must neither throw nor hang
            }
        }
    }

    [Fact]
    public void HugeCountsFailBeforeAllocating()
    {
        var w = new SyncWriter();
        w.WriteFixed32(0x4E53474F);
        w.WriteVarUInt(1);
        w.WriteVarInt(0);
        w.WriteVarUInt(2);
        w.WriteVarUInt(0);
        w.WriteVarUInt(ulong.MaxValue >> 1); // an entity count far past the data
        Assert.Equal(SyncError.LimitExceeded, Fresh().ReadSnapshot(w.Written));

        var r = new SyncReader(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F });
        Assert.Equal(0, r.ReadCount(1 << 20));
        Assert.Equal(SyncError.LimitExceeded, r.Error);
    }

    [Fact]
    public void MalformedPrimitivesAreRecordedOnTheReader()
    {
        var r = new SyncReader(new byte[] { 2 });
        Assert.False(r.ReadBool());
        Assert.Equal(SyncError.InvalidValue, r.Error);

        r = new SyncReader(new byte[] { 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x7F });
        _ = r.ReadVarUInt();
        Assert.Equal(SyncError.VarIntOverflow, r.Error);

        r = new SyncReader(new byte[] { 2, 0xC3, 0x28 });
        Assert.Equal(string.Empty, r.ReadString());
        Assert.Equal(SyncError.InvalidValue, r.Error);

        var w = new SyncWriter();
        w.WriteVarInt(int.MaxValue + 1L);
        r = new SyncReader(w.Written);
        _ = r.ReadInt32();
        Assert.Equal(SyncError.InvalidValue, r.Error);
    }

    [Fact]
    public void DeltaWithUnknownBitsOrBadValuesChangesNothing()
    {
        var shop = new Shop { Name = "Keep" };
        var w = new SyncWriter();
        w.WriteVarUInt(1UL << 40);
        var r = new SyncReader(w.Written);
        shop.ApplyDelta(ref r);
        Assert.Equal(SyncError.InvalidValue, r.Error);

        w.Reset();
        w.WriteVarUInt(0b101); // Name and Stock; Stock is cut short
        w.WriteString("Changed");
        r = new SyncReader(w.Written);
        shop.ApplyDelta(ref r);
        Assert.False(r.Ok);
        Assert.Equal("Keep", shop.Name);
    }

    [Fact]
    public void OverlongStringsAreRefusedOnWrite()
    {
        var w = new SyncWriter();
        Assert.Throws<SyncLimitException>(() => w.WriteString(new string('x', SyncLimits.MaxStringBytes + 1)));
    }
}
