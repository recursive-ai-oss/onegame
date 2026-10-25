using OneGame.Core;

namespace Solitaire;

/// <summary>Which input glyphs the hints show.</summary>
public enum GlyphStyle
{
    /// <summary>Keyboard names, or controller buttons once a controller has been used.</summary>
    Auto,

    /// <summary>Keyboard names.</summary>
    Keyboard,

    /// <summary>Controller button names.</summary>
    Gamepad,
}

/// <summary>One finished or abandoned game in the statistics.</summary>
/// <param name="Deal">The deal number.</param>
/// <param name="DrawMode">1 or 3.</param>
/// <param name="Won">True if the game was won.</param>
/// <param name="Score">Final score.</param>
/// <param name="Moves">Moves made.</param>
/// <param name="Ticks">Timer in simulation ticks (60 to the second).</param>
public readonly record struct GameRecord(int Deal, int DrawMode, bool Won, int Score, int Moves, int Ticks);

/// <summary>
/// The player's settings and statistics: everything the game remembers between runs apart from the saved game. It is written as a small
/// binary file and read back defensively, since the file may come from the cloud or be damaged.
/// </summary>
public sealed class Profile
{
    /// <summary>The most history entries kept.</summary>
    public const int HistoryLimit = 50;

    /// <summary>The file name in the data store.</summary>
    public const string FileName = "profile.bin";

    private const uint Magic = 0x504C4F53; // "SOLP"
    private const int Version = 1;
    private const int MaxBytes = 16 * 1024;

    /// <summary>Cards turned from the stock by a new game: 1 or 3.</summary>
    public int DrawMode { get; set; } = 1;

    /// <summary>Whether the game offers to finish itself once every card is face up.</summary>
    public bool AutoFinish { get; set; } = true;

    /// <summary>Music volume, 0 to 10.</summary>
    public int MusicVolume { get; set; } = 7;

    /// <summary>Effects volume, 0 to 10.</summary>
    public int EffectsVolume { get; set; } = 8;

    /// <summary>Which glyphs the hints show.</summary>
    public GlyphStyle Glyphs { get; set; }

    /// <summary>Games started (a game counts at its first move).</summary>
    public int Games { get; set; }

    /// <summary>Games won.</summary>
    public int Wins { get; set; }

    /// <summary>Fastest win in ticks, or 0 when there is none.</summary>
    public int BestTicks { get; set; }

    /// <summary>Highest score of a win, or 0 when there is none.</summary>
    public int BestScore { get; set; }

    /// <summary>The latest games, newest last.</summary>
    public List<GameRecord> History { get; } = [];

    /// <summary>Adds a finished or abandoned game to the history and, for a win, to the records.</summary>
    public void Add(GameRecord record)
    {
        History.Add(record);
        if (History.Count > HistoryLimit)
        {
            History.RemoveAt(0);
        }

        if (record.Won)
        {
            Wins++;
            Games = Math.Max(Games, Wins);
            BestScore = Math.Max(BestScore, record.Score);
            BestTicks = BestTicks == 0 ? record.Ticks : Math.Min(BestTicks, record.Ticks);
        }
    }

    /// <summary>Puts every setting and statistic back to its starting value.</summary>
    public void ResetAll()
    {
        var fresh = new Profile();
        DrawMode = fresh.DrawMode;
        AutoFinish = fresh.AutoFinish;
        MusicVolume = fresh.MusicVolume;
        EffectsVolume = fresh.EffectsVolume;
        Glyphs = fresh.Glyphs;
        ResetStatistics();
    }

    /// <summary>Clears the statistics and keeps the settings.</summary>
    public void ResetStatistics()
    {
        Games = 0;
        Wins = 0;
        BestTicks = 0;
        BestScore = 0;
        History.Clear();
    }

    /// <summary>Writes the profile.</summary>
    public byte[] Write()
    {
        var w = new SyncWriter(256);
        w.WriteFixed32(Magic);
        w.WriteVarUInt(Version);
        w.WriteVarInt(DrawMode);
        w.WriteBool(AutoFinish);
        w.WriteVarInt(MusicVolume);
        w.WriteVarInt(EffectsVolume);
        w.WriteVarInt((int)Glyphs);
        w.WriteVarInt(Games);
        w.WriteVarInt(Wins);
        w.WriteVarInt(BestTicks);
        w.WriteVarInt(BestScore);
        w.WriteCount(History.Count, HistoryLimit);
        foreach (var r in History)
        {
            w.WriteVarInt(r.Deal);
            w.WriteVarInt(r.DrawMode);
            w.WriteBool(r.Won);
            w.WriteVarInt(r.Score);
            w.WriteVarInt(r.Moves);
            w.WriteVarInt(r.Ticks);
        }

        return w.ToArray();
    }

    /// <summary>
    /// Reads a profile. Every number is range-checked and the history length is capped before anything is allocated; a file that does
    /// not parse gives false and leaves nothing half-filled.
    /// </summary>
    public static bool TryRead(ReadOnlySpan<byte> bytes, out Profile profile)
    {
        profile = new Profile();
        if (bytes.Length is 0 or > MaxBytes)
        {
            return false;
        }

        var r = new SyncReader(bytes);
        if (r.ReadFixed32() != Magic || r.ReadVarUInt() != Version)
        {
            return false;
        }

        var p = new Profile
        {
            DrawMode = Ranged(ref r, 1, 3),
            AutoFinish = r.ReadBool(),
            MusicVolume = Ranged(ref r, 0, 10),
            EffectsVolume = Ranged(ref r, 0, 10),
            Glyphs = (GlyphStyle)Ranged(ref r, 0, 2),
            Games = Ranged(ref r, 0, 100_000_000),
            Wins = Ranged(ref r, 0, 100_000_000),
            BestTicks = Ranged(ref r, 0, int.MaxValue / 2),
            BestScore = Ranged(ref r, 0, 10_000_000),
        };
        var count = r.ReadCount(HistoryLimit);
        for (var i = 0; i < count && r.Ok; i++)
        {
            p.History.Add(new GameRecord(Ranged(ref r, 0, 100_000_000), Ranged(ref r, 1, 3), r.ReadBool(), Ranged(ref r, 0, 10_000_000), Ranged(ref r, 0, 10_000_000), Ranged(ref r, 0, int.MaxValue / 2)));
        }

        r.ExpectEnd();
        if (!r.Ok || p.DrawMode is not (1 or 3))
        {
            return false;
        }

        profile = p;
        return true;
    }

    private static int Ranged(ref SyncReader r, int min, int max)
    {
        var v = r.ReadVarInt();
        if (v < min || v > max)
        {
            r.Fail(SyncError.InvalidValue);
            return min;
        }

        return (int)v;
    }
}
