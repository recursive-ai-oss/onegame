using System.Buffers.Binary;

namespace OneGame.Steam;

/// <summary>How the Steam library packs the callback structs it hands out. The C++ headers pack them at 4 bytes on Linux and macOS
/// and at 8 bytes on Windows, which moves any 64-bit field that follows an odd number of 32-bit ones.</summary>
public enum SteamPacking
{
    /// <summary>4-byte packing (Linux, macOS).</summary>
    Small = 4,

    /// <summary>8-byte packing (Windows).</summary>
    Large = 8,
}

/// <summary>Primitive kinds a callback struct is made of.</summary>
public enum SteamField : byte
{
    /// <summary>One byte (uint8 or bool).</summary>
    U8,

    /// <summary>Four bytes, signed.</summary>
    I32,

    /// <summary>Four bytes, unsigned.</summary>
    U32,

    /// <summary>Eight bytes, unsigned.</summary>
    U64,

    /// <summary>A 128-byte character array (stat and achievement names).</summary>
    Name128,
}

/// <summary>A callback or call-result struct Steam can deliver, decoded from its raw bytes.</summary>
/// <typeparam name="TSelf">The struct itself.</typeparam>
public interface ISteamCallbackData<TSelf>
    where TSelf : struct, ISteamCallbackData<TSelf>
{
    /// <summary>The callback's id, which tags it in the dispatch loop.</summary>
    static abstract int CallbackId { get; }

    /// <summary>The fields in declaration order, used to compute the native layout.</summary>
    static abstract ReadOnlySpan<SteamField> Fields { get; }

    /// <summary>Decodes the value from <paramref name="data"/>, laid out for <paramref name="packing"/>. Returns false when the data
    /// is too short, so a short or garbled message is ignored rather than read past its end.</summary>
    static abstract bool TryRead(ReadOnlySpan<byte> data, SteamPacking packing, out TSelf value);
}

/// <summary>The packing in effect on this operating system.</summary>
public static class SteamLayout
{
    /// <summary>The packing the Steam library for the running operating system uses.</summary>
    public static SteamPacking Current => OperatingSystem.IsWindows() ? SteamPacking.Large : SteamPacking.Small;

    /// <summary>Computes field offsets and the total struct size under C layout rules: each field aligns to the smaller of its own
    /// size and the packing, and the struct is padded to its largest field alignment.</summary>
    /// <param name="fields">Fields in declaration order.</param>
    /// <param name="packing">The packing to use.</param>
    /// <param name="offsets">Receives one offset per field; must be at least as long as <paramref name="fields"/>.</param>
    /// <returns>The struct size in bytes.</returns>
    public static int Compute(ReadOnlySpan<SteamField> fields, SteamPacking packing, Span<int> offsets)
    {
        var pack = (int)packing;
        var offset = 0;
        var structAlign = 1;
        for (var i = 0; i < fields.Length; i++)
        {
            var (size, natural) = fields[i] switch
            {
                SteamField.U8 => (1, 1),
                SteamField.I32 or SteamField.U32 => (4, 4),
                SteamField.U64 => (8, 8),
                SteamField.Name128 => (128, 1),
                _ => throw new ArgumentOutOfRangeException(nameof(fields)),
            };
            var align = Math.Min(natural, pack);
            offset = (offset + align - 1) / align * align;
            offsets[i] = offset;
            offset += size;
            structAlign = Math.Max(structAlign, align);
        }

        return (offset + structAlign - 1) / structAlign * structAlign;
    }

    /// <summary>The size of a callback struct under <paramref name="packing"/>.</summary>
    public static int SizeOf<T>(SteamPacking packing)
        where T : struct, ISteamCallbackData<T>
    {
        Span<int> offsets = stackalloc int[T.Fields.Length];
        return Compute(T.Fields, packing, offsets);
    }
}

/// <summary>Reads fields from a callback buffer at the offsets <see cref="SteamLayout.Compute"/> gave.</summary>
internal readonly ref struct CallbackReader
{
    private readonly ReadOnlySpan<byte> _data;

    internal CallbackReader(ReadOnlySpan<byte> data) => _data = data;

    internal byte U8(int offset) => _data[offset];

    internal int I32(int offset) => BinaryPrimitives.ReadInt32LittleEndian(_data[offset..]);

    internal uint U32(int offset) => BinaryPrimitives.ReadUInt32LittleEndian(_data[offset..]);

    internal ulong U64(int offset) => BinaryPrimitives.ReadUInt64LittleEndian(_data[offset..]);

    /// <summary>A zero-terminated UTF-8 string inside a fixed array. An array without a terminator is cut at its end.</summary>
    internal string Name(int offset, int capacity)
    {
        var bytes = _data.Slice(offset, capacity);
        var end = bytes.IndexOf((byte)0);
        return System.Text.Encoding.UTF8.GetString(end < 0 ? bytes : bytes[..end]);
    }
}

/// <summary>A previously started async call finished. The dispatch loop uses it to fetch the call's result; games do not see it.</summary>
/// <param name="Call">Handle of the finished call.</param>
/// <param name="CallbackId">Id of the result type.</param>
/// <param name="ResultSize">Size in bytes of the result.</param>
public readonly record struct SteamApiCallCompleted(ulong Call, int CallbackId, uint ResultSize) : ISteamCallbackData<SteamApiCallCompleted>
{
    static int ISteamCallbackData<SteamApiCallCompleted>.CallbackId => 703;

    private static ReadOnlySpan<SteamField> FieldList => [SteamField.U64, SteamField.I32, SteamField.U32];

    static ReadOnlySpan<SteamField> ISteamCallbackData<SteamApiCallCompleted>.Fields => FieldList;

    static bool ISteamCallbackData<SteamApiCallCompleted>.TryRead(ReadOnlySpan<byte> data, SteamPacking packing, out SteamApiCallCompleted value)
    {
        Span<int> o = stackalloc int[3];
        var size = SteamLayout.Compute(FieldList, packing, o);
        if (data.Length < size)
        {
            value = default;
            return false;
        }

        var r = new CallbackReader(data);
        value = new SteamApiCallCompleted(r.U64(o[0]), r.I32(o[1]), r.U32(o[2]));
        return true;
    }
}

/// <summary>The latest stats and achievements of a user arrived.</summary>
/// <param name="GameId">The game the stats belong to.</param>
/// <param name="Result">Steam result code; 1 is success.</param>
/// <param name="UserId">The user whose stats arrived.</param>
public readonly record struct UserStatsReceived(ulong GameId, int Result, ulong UserId) : ISteamCallbackData<UserStatsReceived>
{
    /// <summary>True when <see cref="Result"/> is the success code.</summary>
    public bool Succeeded => Result == 1;

    static int ISteamCallbackData<UserStatsReceived>.CallbackId => 1101;

    private static ReadOnlySpan<SteamField> FieldList => [SteamField.U64, SteamField.I32, SteamField.U64];

    static ReadOnlySpan<SteamField> ISteamCallbackData<UserStatsReceived>.Fields => FieldList;

    static bool ISteamCallbackData<UserStatsReceived>.TryRead(ReadOnlySpan<byte> data, SteamPacking packing, out UserStatsReceived value)
    {
        Span<int> o = stackalloc int[3];
        var size = SteamLayout.Compute(FieldList, packing, o);
        if (data.Length < size)
        {
            value = default;
            return false;
        }

        var r = new CallbackReader(data);
        value = new UserStatsReceived(r.U64(o[0]), r.I32(o[1]), r.U64(o[2]));
        return true;
    }
}

/// <summary>Stats were (or were not) stored on the server.</summary>
/// <param name="GameId">The game the stats belong to.</param>
/// <param name="Result">Steam result code; 1 is success, 8 means the server rejected a value and sent corrected ones.</param>
public readonly record struct UserStatsStored(ulong GameId, int Result) : ISteamCallbackData<UserStatsStored>
{
    /// <summary>True when <see cref="Result"/> is the success code.</summary>
    public bool Succeeded => Result == 1;

    static int ISteamCallbackData<UserStatsStored>.CallbackId => 1102;

    private static ReadOnlySpan<SteamField> FieldList => [SteamField.U64, SteamField.I32];

    static ReadOnlySpan<SteamField> ISteamCallbackData<UserStatsStored>.Fields => FieldList;

    static bool ISteamCallbackData<UserStatsStored>.TryRead(ReadOnlySpan<byte> data, SteamPacking packing, out UserStatsStored value)
    {
        Span<int> o = stackalloc int[2];
        var size = SteamLayout.Compute(FieldList, packing, o);
        if (data.Length < size)
        {
            value = default;
            return false;
        }

        var r = new CallbackReader(data);
        value = new UserStatsStored(r.U64(o[0]), r.I32(o[1]));
        return true;
    }
}

/// <summary>An achievement was stored, or its progress was shown. When both progress numbers are zero it is fully unlocked.</summary>
/// <param name="GameId">The game the achievement belongs to.</param>
/// <param name="GroupAchievement">Unused by Steam.</param>
/// <param name="Name">The achievement's API name.</param>
/// <param name="CurrentProgress">Progress so far.</param>
/// <param name="MaxProgress">Progress needed to unlock.</param>
public readonly record struct UserAchievementStored(ulong GameId, bool GroupAchievement, string Name, uint CurrentProgress, uint MaxProgress)
    : ISteamCallbackData<UserAchievementStored>
{
    static int ISteamCallbackData<UserAchievementStored>.CallbackId => 1103;

    private static ReadOnlySpan<SteamField> FieldList => [SteamField.U64, SteamField.U8, SteamField.Name128, SteamField.U32, SteamField.U32];

    static ReadOnlySpan<SteamField> ISteamCallbackData<UserAchievementStored>.Fields => FieldList;

    static bool ISteamCallbackData<UserAchievementStored>.TryRead(ReadOnlySpan<byte> data, SteamPacking packing, out UserAchievementStored value)
    {
        Span<int> o = stackalloc int[5];
        var size = SteamLayout.Compute(FieldList, packing, o);
        if (data.Length < size)
        {
            value = default;
            return false;
        }

        var r = new CallbackReader(data);
        value = new UserAchievementStored(r.U64(o[0]), r.U8(o[1]) != 0, r.Name(o[2], 128), r.U32(o[3]), r.U32(o[4]));
        return true;
    }
}

/// <summary>Result of <c>GetNumberOfCurrentPlayers</c>.</summary>
/// <param name="Success">Whether the call worked.</param>
/// <param name="Players">Players currently in the game.</param>
public readonly record struct NumberOfCurrentPlayers(bool Success, int Players) : ISteamCallbackData<NumberOfCurrentPlayers>
{
    static int ISteamCallbackData<NumberOfCurrentPlayers>.CallbackId => 1107;

    private static ReadOnlySpan<SteamField> FieldList => [SteamField.U8, SteamField.I32];

    static ReadOnlySpan<SteamField> ISteamCallbackData<NumberOfCurrentPlayers>.Fields => FieldList;

    static bool ISteamCallbackData<NumberOfCurrentPlayers>.TryRead(ReadOnlySpan<byte> data, SteamPacking packing, out NumberOfCurrentPlayers value)
    {
        Span<int> o = stackalloc int[2];
        var size = SteamLayout.Compute(FieldList, packing, o);
        if (data.Length < size)
        {
            value = default;
            return false;
        }

        var r = new CallbackReader(data);
        value = new NumberOfCurrentPlayers(r.U8(o[0]) != 0, r.I32(o[1]));
        return true;
    }
}

/// <summary>The overlay opened or closed. Only the documented first field is read; the struct may be longer.</summary>
/// <param name="Active">True when the overlay just opened.</param>
public readonly record struct GameOverlayActivated(bool Active) : ISteamCallbackData<GameOverlayActivated>
{
    static int ISteamCallbackData<GameOverlayActivated>.CallbackId => 331;

    private static ReadOnlySpan<SteamField> FieldList => [SteamField.U8];

    static ReadOnlySpan<SteamField> ISteamCallbackData<GameOverlayActivated>.Fields => FieldList;

    static bool ISteamCallbackData<GameOverlayActivated>.TryRead(ReadOnlySpan<byte> data, SteamPacking packing, out GameOverlayActivated value)
    {
        if (data.Length < 1)
        {
            value = default;
            return false;
        }

        value = new GameOverlayActivated(data[0] != 0);
        return true;
    }
}

/// <summary>Steam wants the game to shut down (the client is exiting). The struct has no fields.</summary>
public readonly record struct SteamShutdown : ISteamCallbackData<SteamShutdown>
{
    static int ISteamCallbackData<SteamShutdown>.CallbackId => 704;

    private static ReadOnlySpan<SteamField> FieldList => [];

    static ReadOnlySpan<SteamField> ISteamCallbackData<SteamShutdown>.Fields => FieldList;

    static bool ISteamCallbackData<SteamShutdown>.TryRead(ReadOnlySpan<byte> data, SteamPacking packing, out SteamShutdown value)
    {
        value = default;
        return true;
    }
}
