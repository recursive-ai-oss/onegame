using OneGame.Storage;

namespace OneGame.Sync.Tests.Storage;

/// <summary>A clock tests move by hand, so slot files are the same bytes every run.</summary>
internal sealed class ManualClock : TimeProvider
{
    private DateTimeOffset _now = new(2026, 10, 27, 12, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now += by;
}

/// <summary>A store that refuses writes after a number of good ones (a full disk, a revoked permission).</summary>
internal sealed class FlakyStore(IByteStore inner) : IByteStore
{
    public int WritesLeft { get; set; } = int.MaxValue;

    public string Description => "flaky " + inner.Description;

    public bool TryRead(string name, int maxBytes, out byte[] data) => inner.TryRead(name, maxBytes, out data);

    public bool Write(string name, ReadOnlySpan<byte> data) => WritesLeft-- > 0 && inner.Write(name, data);

    public bool Delete(string name) => inner.Delete(name);

    public IReadOnlyList<string> List(string prefix = "") => inner.List(prefix);
}

public class ByteStoreTests : IDisposable
{
    private static T[] A<T>(params T[] items) => items;

    private readonly string _dir = Path.Combine(Path.GetTempPath(), "onegame-store-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, true);
        }
    }

    public static TheoryData<string> Kinds() => new() { "memory", "folder" };

    private IByteStore Make(string kind) => kind == "memory" ? new MemoryByteStore() : new FolderByteStore(_dir);

    [Theory]
    [MemberData(nameof(Kinds))]
    public void WritesReadsReplacesListsAndDeletes(string kind)
    {
        var store = Make(kind);
        Assert.False(store.TryRead("a.bin", 100, out _));
        Assert.True(store.Write("a.bin", [1, 2, 3]));
        Assert.True(store.Write("b.bin", [9]));
        Assert.True(store.Write("slot-x.sav", [7]));
        Assert.True(store.TryRead("a.bin", 100, out var a));
        Assert.Equal(A<byte>(1, 2, 3), a);

        Assert.True(store.Write("a.bin", [4, 5]));
        Assert.True(store.TryRead("a.bin", 100, out a));
        Assert.Equal(A<byte>(4, 5), a);

        Assert.Equal(A("a.bin", "b.bin", "slot-x.sav"), store.List());
        Assert.Equal(A("slot-x.sav"), store.List("slot-"));
        Assert.True(store.Delete("a.bin"));
        Assert.True(store.Delete("a.bin"));
        Assert.False(store.TryRead("a.bin", 100, out _));
        Assert.DoesNotContain("a.bin", store.List());
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void AFileOverTheLimitIsRefusedBeforeItIsCopied(string kind)
    {
        var store = Make(kind);
        store.Write("big.bin", new byte[1_000_000]);
        // Only the store call sits between the two readings: the assertion library allocates on first use, which is not the store's doing.
        // The first refused read runs before the window so one-time JIT and type-loading work is not counted either.
        store.TryRead("big.bin", 100, out _);
        var before = GC.GetAllocatedBytesForCurrentThread();
        var refused = !store.TryRead("big.bin", 100, out var data);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.True(refused);
        Assert.Empty(data);
        Assert.True(allocated < 100_000, $"a refused read must not allocate the file (allocated {allocated} bytes)");
        Assert.True(store.TryRead("big.bin", 1_000_000, out data));
        Assert.Equal(1_000_000, data.Length);
    }

    [Theory]
    [InlineData("")]
    [InlineData(".hidden")]
    [InlineData("a/b")]
    [InlineData("..")]
    [InlineData("x.tmp")]
    [InlineData("white space")]
    public void BadNamesAreRejected(string name)
    {
        Assert.False(StoreNames.IsValid(name));
        Assert.Throws<ArgumentException>(() => new MemoryByteStore().Write(name, [1]));
        Assert.Throws<ArgumentException>(() => new FolderByteStore(_dir).TryRead(name, 10, out _));
    }

    [Fact]
    public void ACrashBetweenTheTempFileAndTheRenameKeepsTheOldFile()
    {
        var store = new FolderByteStore(_dir);
        Assert.True(store.Write("save.bin", [1, 1, 1, 1]));

        // The "process" dies right after the new bytes reached the temp file: the exception stands for the crash.
        store.BeforeCommit = _ => throw new IOException("power cut");
        Assert.False(store.Write("save.bin", [2, 2, 2, 2, 2, 2]));
        store.BeforeCommit = null;

        Assert.True(store.TryRead("save.bin", 100, out var data));
        Assert.Equal(A<byte>(1, 1, 1, 1), data);
        Assert.Equal(A("save.bin"), store.List());
        Assert.Empty(Directory.GetFiles(_dir, "*.tmp"));
    }

    [Fact]
    public void ALeftoverHalfWrittenTempFileIsNeverReadOrListedAndTheNextWriteReplacesIt()
    {
        var store = new FolderByteStore(_dir);
        Assert.True(store.Write("save.bin", [1, 2, 3]));
        File.WriteAllBytes(Path.Combine(_dir, "save.bin.tmp"), [9, 9]); // what a hard kill leaves behind
        File.WriteAllBytes(Path.Combine(_dir, "new.bin.tmp"), [9]);

        Assert.True(store.TryRead("save.bin", 100, out var data));
        Assert.Equal(A<byte>(1, 2, 3), data);
        Assert.False(store.TryRead("new.bin", 100, out _));
        Assert.Equal(A("save.bin"), store.List());

        Assert.True(store.Write("save.bin", [4]));
        Assert.True(store.TryRead("save.bin", 100, out data));
        Assert.Equal(A<byte>(4), data);
    }

    [Fact]
    public void AnUnwritableFolderAnswersFalseInsteadOfThrowing()
    {
        File.WriteAllText(_dir, "a file where the folder should be");
        var store = new FolderByteStore(_dir);
        Assert.False(store.Write("a.bin", [1]));
        Assert.False(store.TryRead("a.bin", 10, out _));
        Assert.Empty(store.List());
        File.Delete(_dir);
    }
}
