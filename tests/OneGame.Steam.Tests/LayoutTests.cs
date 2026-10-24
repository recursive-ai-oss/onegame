using OneGame.Steam.Native;

namespace OneGame.Steam.Tests;

public class LayoutTests
{
    // Sizes worked out by hand from the field lists in the ISteamUserStats / ISteamUtils reference pages under C layout rules.
    // 4-byte packing is what the library uses on Linux and macOS, 8-byte packing on Windows.
    [Theory]
    [InlineData(SteamPacking.Small, 16, 20, 12, 148, 8)]
    [InlineData(SteamPacking.Large, 16, 24, 16, 152, 8)]
    public void StructSizesMatchTheNativeLayouts(SteamPacking pack, int callCompleted, int statsReceived, int statsStored, int achStored, int players)
    {
        Assert.Equal(callCompleted, SteamLayout.SizeOf<SteamApiCallCompleted>(pack));
        Assert.Equal(statsReceived, SteamLayout.SizeOf<UserStatsReceived>(pack));
        Assert.Equal(statsStored, SteamLayout.SizeOf<UserStatsStored>(pack));
        Assert.Equal(achStored, SteamLayout.SizeOf<UserAchievementStored>(pack));
        Assert.Equal(players, SteamLayout.SizeOf<NumberOfCurrentPlayers>(pack));
        Assert.Equal(1, SteamLayout.SizeOf<GameOverlayActivated>(pack));
        Assert.Equal(0, SteamLayout.SizeOf<SteamShutdown>(pack));
    }

    [Fact]
    public void PackingMovesTheSteamIdOnlyInStructsWhereItFollowsA32BitField()
    {
        Span<int> o = stackalloc int[3];
        SteamLayout.Compute(UserStatsReceivedFields, SteamPacking.Small, o);
        Assert.Equal([0, 8, 12], o.ToArray());
        SteamLayout.Compute(UserStatsReceivedFields, SteamPacking.Large, o);
        Assert.Equal([0, 8, 16], o.ToArray());
    }

    private static ReadOnlySpan<SteamField> UserStatsReceivedFields => [SteamField.U64, SteamField.I32, SteamField.U64];

    [Fact]
    public void ManualDispatchRecordIsTwentyFourBytes() =>
        Assert.Equal(24, System.Runtime.CompilerServices.Unsafe.SizeOf<CallbackMsg>());

    [Fact]
    public void CurrentPackingFollowsTheOperatingSystem() =>
        Assert.Equal(OperatingSystem.IsWindows() ? SteamPacking.Large : SteamPacking.Small, SteamLayout.Current);

    [Theory]
    [InlineData(SteamPacking.Small)]
    [InlineData(SteamPacking.Large)]
    public void CallbacksDecodeFromBytesLaidOutByTheNativePacking(SteamPacking pack)
    {
        var p = (int)pack;

        var received = Wire.Pack(p, 480UL, 1, 76561198000000001UL);
        Assert.Equal(SteamLayout.SizeOf<UserStatsReceived>(pack), received.Length);
        Assert.True(Read(received, pack, out UserStatsReceived r));
        Assert.Equal(new UserStatsReceived(480, 1, 76561198000000001UL), r);
        Assert.True(r.Succeeded);

        var stored = Wire.Pack(p, 480UL, 8);
        Assert.Equal(SteamLayout.SizeOf<UserStatsStored>(pack), stored.Length);
        Assert.True(Read(stored, pack, out UserStatsStored s));
        Assert.Equal(new UserStatsStored(480, 8), s);

        var ach = Wire.Pack(p, 480UL, (byte)0, "ACH_WIN", 0u, 0u);
        Assert.Equal(SteamLayout.SizeOf<UserAchievementStored>(pack), ach.Length);
        Assert.True(Read(ach, pack, out UserAchievementStored a));
        Assert.Equal(new UserAchievementStored(480, false, "ACH_WIN", 0, 0), a);

        var prog = Wire.Pack(p, 480UL, (byte)0, "ACH_TEN", 3u, 10u);
        Assert.True(Read(prog, pack, out UserAchievementStored a2));
        Assert.Equal((3u, 10u), (a2.CurrentProgress, a2.MaxProgress));

        var players = Wire.Pack(p, (byte)1, 1234);
        Assert.True(Read(players, pack, out NumberOfCurrentPlayers n));
        Assert.Equal(new NumberOfCurrentPlayers(true, 1234), n);

        var done = Wire.Pack(p, 99UL, 1107, 8u);
        Assert.True(Read(done, pack, out SteamApiCallCompleted c));
        Assert.Equal(new SteamApiCallCompleted(99, 1107, 8), c);

        Assert.True(Read([1, 0, 0, 0, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F], pack, out GameOverlayActivated g));
        Assert.True(g.Active);
    }

    [Theory]
    [InlineData(SteamPacking.Small)]
    [InlineData(SteamPacking.Large)]
    public void ShortOrEmptyDataIsRejectedNotReadPastItsEnd(SteamPacking pack)
    {
        var full = Wire.Pack((int)pack, 480UL, 1, 76561198000000001UL);
        for (var len = 0; len < full.Length; len++)
        {
            Assert.False(Read(full.AsSpan(0, len), pack, out UserStatsReceived _), $"length {len}");
        }

        Assert.False(Read([], pack, out GameOverlayActivated _));
        Assert.False(Read(new byte[100], pack, out UserAchievementStored _));
    }

    [Fact]
    public void ANameWithoutATerminatorIsCutAtTheArrayEnd()
    {
        var bytes = Wire.Pack(4, 480UL, (byte)0, new string('x', 128), 0u, 0u);
        Assert.True(Read(bytes, SteamPacking.Small, out UserAchievementStored a));
        Assert.Equal(128, a.Name.Length);
    }

    private static bool Read<T>(ReadOnlySpan<byte> data, SteamPacking pack, out T value)
        where T : struct, ISteamCallbackData<T> => T.TryRead(data, pack, out value);

    [Fact]
    public void CallbackIdsAreDistinct()
    {
        int[] ids = [CallbackId<SteamApiCallCompleted>(), CallbackId<UserStatsReceived>(), CallbackId<UserStatsStored>(),
            CallbackId<UserAchievementStored>(), CallbackId<NumberOfCurrentPlayers>(), CallbackId<GameOverlayActivated>(), CallbackId<SteamShutdown>()];
        Assert.Equal(ids.Length, ids.Distinct().Count());
        Assert.Equal([703, 1101, 1102, 1103, 1107, 331, 704], ids);
    }

    private static int CallbackId<T>()
        where T : struct, ISteamCallbackData<T> => T.CallbackId;
}
