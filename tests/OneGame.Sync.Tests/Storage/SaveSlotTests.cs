using OneGame.Core.Tests.Samples;
using OneGame.Storage;

namespace OneGame.Sync.Tests.Storage;

public class SaveSlotTests : IDisposable
{
    private static T[] A<T>(params T[] items) => items;

    private readonly string _dir = Path.Combine(Path.GetTempPath(), "onegame-slots-" + Guid.NewGuid().ToString("N"));
    private readonly ManualClock _clock = new();

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, true);
        }
    }

    private SaveSlots Slots(IByteStore store, string version = "1.2") =>
        new(store, new SaveSlotOptions { GameVersion = version, Time = _clock });

    private static World Played(int ticks)
    {
        var world = Scenario.NewAuthority();
        for (var i = 0; i < ticks; i++)
        {
            Scenario.Input(world, world.Tick);
            world.Step();
        }

        return world;
    }

    [Fact]
    public void ASlotSavedByOneProcessLoadsInAFreshOneWithTheSameHashAndShowsItsDescription()
    {
        var first = Played(120);
        var hash = first.ComputeHash();
        var firstSlots = Slots(new FolderByteStore(_dir));
        var saved = firstSlots.Save("day-3", first, "Day 3: the shop opens");
        Assert.True(saved.Ok, saved.Detail);

        // A fresh process: new world, new store object over the same folder, new slot manager.
        var second = Scenario.NewWorld(WorldRole.Authority);
        var secondSlots = Slots(new FolderByteStore(_dir));
        var listed = Assert.Single(secondSlots.List());
        Assert.Equal(("day-3", "Day 3: the shop opens", SlotKind.Manual, "1.2", 120L, hash), (listed.Slot, listed.Label, listed.Kind, listed.GameVersion, listed.Tick, listed.WorldHash));
        Assert.Equal(new DateTimeOffset(2026, 10, 27, 12, 0, 0, TimeSpan.Zero), listed.SavedAt);
        Assert.Equal(new FileInfo(Path.Combine(_dir, "slot-day-3.sav")).Length, listed.Bytes);

        var loaded = secondSlots.Load("day-3", second);
        Assert.True(loaded.Ok, loaded.Detail);
        Assert.True(loaded.Load!.HashVerified);
        Assert.False(loaded.Load.Migrated);
        Assert.Equal(hash, second.ComputeHash());
        Assert.Equal(120, second.Tick);

        // And it keeps running the same as the original would have.
        first.Step();
        second.Step();
        Assert.Equal(first.ComputeHash(), second.ComputeHash());
    }

    [Fact]
    public void AnAuthorityScopeSavesAndLoadsThroughItsOwnPath()
    {
        var (cluster, scope, view) = Scenario.NewCluster();
        Scenario.Play(cluster, scope, view, 40);
        var slots = Slots(new MemoryByteStore());
        var hashAt40 = scope.Hash;
        Assert.True(slots.Save("a", scope, "forty").Ok);
        Assert.Equal(40, scope.SavedTick);
        Scenario.Play(cluster, scope, view, 25);
        Assert.NotEqual(hashAt40, scope.Hash);

        var loaded = slots.Load("a", scope);
        Assert.True(loaded.Ok, loaded.Detail);
        cluster.Settle();
        Assert.Equal(hashAt40, scope.Hash);
        Assert.Equal(hashAt40, view.Hash);
    }

    [Fact]
    public void ASaveWrittenByAnOlderSchemaLoadsThroughItsMigrationFromASlot()
    {
        var slots = Slots(new MemoryByteStore(), "0.9");
        Assert.True(slots.Import("old", Fuzzing.Scenario1SaveFixture.Build(), "from the old build").Ok);
        var world = Scenario.NewWorld(WorldRole.Authority);
        var loaded = slots.Load("old", world);
        Assert.True(loaded.Ok, loaded.Detail);
        Assert.True(loaded.Load!.Migrated);
        Assert.Equal("Old Shop", ((Shop)world.Find(Scenario.ShopId)!).Name);
        Assert.Equal(Fixed.FromInt(7), ((Shop)world.Find(Scenario.ShopId)!).Gold);
        Assert.Equal("0.9", slots.List().Single().GameVersion);
    }

    [Fact]
    public void ListIsNewestFirstAndKeepsEveryKindApart()
    {
        var slots = Slots(new MemoryByteStore());
        var world = Played(3);
        slots.Save("b", world, "first");
        _clock.Advance(TimeSpan.FromMinutes(1));
        slots.QuickSave(world, "quick");
        _clock.Advance(TimeSpan.FromMinutes(1));
        slots.Save("a", world, "last");
        _clock.Advance(TimeSpan.FromMinutes(1));
        slots.Autosave(world, "auto");

        var list = slots.List();
        Assert.Equal(A("auto-0", "a", "quick", "b"), list.Select(i => i.Slot));
        Assert.Equal(A(SlotKind.Auto, SlotKind.Manual, SlotKind.Quick, SlotKind.Manual), list.Select(i => i.Kind));
        Assert.Equal("auto-0", slots.Latest()!.Slot);
        Assert.True(slots.Delete("a"));
        Assert.False(slots.Exists("a"));
        Assert.Equal(3, slots.List().Count);
    }

    [Fact]
    public void AutosavesRotateOverTheOldestSlot()
    {
        var slots = Slots(new MemoryByteStore());
        var world = Played(1);
        for (var i = 0; i < 5; i++)
        {
            _clock.Advance(TimeSpan.FromSeconds(10));
            var r = slots.Autosave(world, "n" + i);
            Assert.True(r.Ok);
            world.Step();
        }

        var list = slots.List();
        Assert.Equal(3, list.Count);
        Assert.Equal(A("n4", "n3", "n2"), list.Select(i => i.Label));
    }

    [Fact]
    public void ALongLabelIsCutOnACharacterBoundary()
    {
        var slots = Slots(new MemoryByteStore());
        var label = string.Concat(Enumerable.Repeat("é🙂", 100));
        Assert.True(slots.Save("x", Played(1), label).Ok);
        var read = slots.List().Single().Label;
        Assert.True(System.Text.Encoding.UTF8.GetByteCount(read) <= SaveSlots.MaxLabelBytes);
        Assert.StartsWith(read, label, StringComparison.Ordinal);
    }

    [Fact]
    public void AWriteThatFailsOrIsInterruptedLeavesThePreviousSaveWhole()
    {
        var folder = new FolderByteStore(_dir);
        var slots = Slots(folder);
        var early = Played(10);
        Assert.True(slots.Save("a", early, "early").Ok);
        var earlyHash = early.ComputeHash();

        var later = Played(60);
        folder.BeforeCommit = _ => throw new IOException("killed while writing");
        var interrupted = slots.Save("a", later, "later");
        folder.BeforeCommit = null;
        Assert.Equal(SlotStatus.WriteFailed, interrupted.Status);

        var flaky = new FlakyStore(folder) { WritesLeft = 0 };
        Assert.Equal(SlotStatus.WriteFailed, Slots(flaky).Save("a", later, "later").Status);

        var fresh = Scenario.NewWorld(WorldRole.Authority);
        var loaded = Slots(new FolderByteStore(_dir)).Load("a", fresh);
        Assert.True(loaded.Ok, loaded.Detail);
        Assert.Equal(earlyHash, fresh.ComputeHash());
        Assert.Equal("early", slots.List().Single().Label);
        Assert.Empty(Directory.GetFiles(_dir, "*.tmp"));
    }

    [Fact]
    public void BadNamesMissingAndDamagedSlotsGiveTypedAnswers()
    {
        var store = new MemoryByteStore();
        var slots = Slots(store);
        var world = Played(5);
        Assert.Equal(SlotStatus.InvalidName, slots.Save("../evil", world).Status);
        Assert.Equal(SlotStatus.InvalidName, slots.Save("", world).Status);
        Assert.Equal(SlotStatus.InvalidName, slots.Load("a b", world).Status);
        Assert.Equal(SlotStatus.NoSuchSlot, slots.Load("nope", world).Status);

        Assert.True(slots.Save("ok", world).Ok);
        store.TryRead("slot-ok.sav", int.MaxValue, out var file);

        store.Write("slot-cut.sav", file.AsSpan(0, file.Length / 2));
        Assert.Equal(SlotStatus.BadSlotFile, slots.Load("cut", Scenario.NewWorld(WorldRole.Authority)).Status);

        var flipped = (byte[])file.Clone();
        flipped[^7] ^= 0x40;
        store.Write("slot-flip.sav", flipped);
        var flip = slots.Load("flip", Scenario.NewWorld(WorldRole.Authority));
        Assert.Equal(SlotStatus.SaveRejected, flip.Status);
        Assert.Equal(SaveStatus.Corrupt, flip.Load!.Status);

        store.Write("slot-junk.sav", [1, 2, 3, 4, 5, 6, 7, 8]);
        Assert.Equal(SlotStatus.BadSlotFile, slots.Load("junk", world).Status);

        var newer = (byte[])file.Clone();
        newer[4] = 2; // container format 2: written by a later build
        store.Write("slot-newer.sav", newer);
        Assert.Equal(SlotStatus.UnsupportedFormat, slots.Load("newer", world).Status);

        // Damaged slots are left out of the list; the good one stays.
        Assert.Equal(A("flip", "ok").Order(), slots.List().Select(i => i.Slot).Order());
        Assert.Equal(SlotStatus.BadSlotFile, slots.TryGetInfo("junk", out _).Status);
    }

    [Fact]
    public void ASlotOverTheSizeLimitIsRefusedOnWriteAndOnRead()
    {
        var store = new MemoryByteStore();
        var tiny = new SaveSlots(store, new SaveSlotOptions { MaxSaveBytes = 1024, Time = _clock });
        Assert.Equal(SlotStatus.TooLarge, tiny.Save("big", Scenario.NewAuthority(8, 300)).Status);
        Assert.Empty(store.List());

        var roomy = Slots(store);
        Assert.True(roomy.Save("big", Scenario.NewAuthority(8, 300)).Ok);
        var before = GC.GetAllocatedBytesForCurrentThread();
        Assert.Equal(SlotStatus.TooLarge, tiny.Load("big", Scenario.NewWorld(WorldRole.Authority)).Status);
        Assert.True(GC.GetAllocatedBytesForCurrentThread() - before < 20_000, "a refused slot must not be copied");
    }

    [Fact]
    public void AHostileHeaderClaimingHugeLengthsIsRefusedWithoutAllocatingThem()
    {
        var w = new SyncWriter();
        w.WriteFixed32(0x4C534F47);
        w.WriteVarUInt(1);
        w.WriteByte(0);
        w.WriteVarInt(0);
        w.WriteVarUInt(int.MaxValue); // a version string of 2 GiB, with nothing behind it
        var bytes = w.ToArray();
        var before = GC.GetAllocatedBytesForCurrentThread();
        Assert.Equal(SlotStatus.BadSlotFile, SaveSlots.TryParse(bytes, "x", out var info, out _));
        Assert.Null(info);
        Assert.True(GC.GetAllocatedBytesForCurrentThread() - before < 2_000);
    }

    [Fact]
    public void SavingAndLoadingAreRepeatableByteForByte()
    {
        var a = new MemoryByteStore();
        var b = new MemoryByteStore();
        Assert.True(Slots(a).Save("s", Played(30), "same").Ok);
        Assert.True(Slots(b).Save("s", Played(30), "same").Ok);
        a.TryRead("slot-s.sav", int.MaxValue, out var one);
        b.TryRead("slot-s.sav", int.MaxValue, out var two);
        Assert.Equal(one, two);
    }
}
