using OneGame.Core;

namespace Bricks;

/// <summary>One line of the high-score table.</summary>
/// <param name="Name">The player's name, at most <see cref="Profile.NameLength"/> characters.</param>
/// <param name="Score">The final score.</param>
/// <param name="Level">The level reached.</param>
/// <param name="Victory">True when the player cleared every level.</param>
public readonly record struct ScoreEntry(string Name, int Score, int Level, bool Victory);

/// <summary>
/// What the game remembers between runs: the settings and the high-score table. It is written as one small binary file through the data
/// store and read back defensively, since the file may be damaged or come from somewhere else.
/// </summary>
public sealed class Profile
{
    /// <summary>The file name in the data store.</summary>
    public const string FileName = "bricks.bin";

    /// <summary>The longest name in the table.</summary>
    public const int NameLength = 10;

    /// <summary>The number of lines the table keeps.</summary>
    public const int TableSize = 8;

    /// <summary>The UI sizes the settings offer; 0 follows the screen.</summary>
    public static readonly float[] UiScales = [0f, 1f, 1.25f, 1.5f, 2f];

    private const uint Magic = 0x42524B53; // "BRKS"
    private const int Version = 1;
    private const int MaxBytes = 4 * 1024;

    /// <summary>Sound volume, 0 to 10.</summary>
    public int Volume { get; set; } = 8;

    /// <summary>An index into <see cref="UiScales"/>.</summary>
    public int UiScaleIndex { get; set; }

    /// <summary>The name last typed into the table, offered next time.</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>The table, best first.</summary>
    public List<ScoreEntry> Scores { get; } = [];

    /// <summary>The UI scale to use, or null to follow the screen.</summary>
    public float? UiScale => UiScaleIndex is > 0 && UiScaleIndex < UiScales.Length ? UiScales[UiScaleIndex] : null;

    /// <summary>The best score in the table, or 0.</summary>
    public int Best => Scores.Count == 0 ? 0 : Scores[0].Score;

    /// <summary>True when <paramref name="score"/> would earn a place in the table.</summary>
    public bool Qualifies(int score) => score > 0 && (Scores.Count < TableSize || score > Scores[Scores.Count - 1].Score);

    /// <summary>
    /// Puts a score in the table, after any equal ones (the older score ranks first), and cuts the table to its size. Returns the place, from
    /// 0, or -1 when the score did not earn one.
    /// </summary>
    public int Add(string name, int score, int level, bool victory)
    {
        if (!Qualifies(score))
        {
            return -1;
        }

        var clean = Clean(name);
        LastName = clean;
        var place = 0;
        while (place < Scores.Count && Scores[place].Score >= score)
        {
            place++;
        }

        Scores.Insert(place, new ScoreEntry(clean, score, level, victory));
        if (Scores.Count > TableSize)
        {
            Scores.RemoveAt(Scores.Count - 1);
        }

        return place;
    }

    /// <summary>Trims a typed name, drops anything that is not printable and cuts it to length; an empty name becomes "Player".</summary>
    public static string Clean(string? name)
    {
        var chars = new List<char>();
        foreach (var c in (name ?? string.Empty).Trim())
        {
            if (!char.IsControl(c) && chars.Count < NameLength)
            {
                chars.Add(c);
            }
        }

        var text = new string([.. chars]).Trim();
        return text.Length == 0 ? "Player" : text;
    }

    /// <summary>Empties the table and keeps the settings.</summary>
    public void ClearScores() => Scores.Clear();

    /// <summary>Writes the profile.</summary>
    public byte[] Write()
    {
        var w = new SyncWriter(256);
        w.WriteFixed32(Magic);
        w.WriteVarUInt(Version);
        w.WriteVarInt(Volume);
        w.WriteVarInt(UiScaleIndex);
        w.WriteString(LastName);
        w.WriteCount(Scores.Count, TableSize);
        foreach (var s in Scores)
        {
            w.WriteString(s.Name);
            w.WriteVarInt(s.Score);
            w.WriteVarInt(s.Level);
            w.WriteBool(s.Victory);
        }

        return w.ToArray();
    }

    /// <summary>
    /// Reads a profile. Every number is range-checked and the table length is capped before anything is allocated; a file that does not
    /// parse gives false and leaves nothing half-filled.
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
            Volume = Ranged(ref r, 0, 10),
            UiScaleIndex = Ranged(ref r, 0, UiScales.Length - 1),
            LastName = ReadName(ref r),
        };
        var count = r.ReadCount(TableSize);
        for (var i = 0; i < count && r.Ok; i++)
        {
            var name = ReadName(ref r);
            var score = Ranged(ref r, 0, 100_000_000);
            var level = Ranged(ref r, 1, 99);
            var victory = r.ReadBool();
            p.Scores.Add(new ScoreEntry(name, score, level, victory));
        }

        r.ExpectEnd();
        if (!r.Ok)
        {
            return false;
        }

        // The table must be in order; a file that is not was not written by this game.
        for (var i = 1; i < p.Scores.Count; i++)
        {
            if (p.Scores[i].Score > p.Scores[i - 1].Score)
            {
                return false;
            }
        }

        profile = p;
        return true;
    }

    private static string ReadName(ref SyncReader r)
    {
        var name = r.ReadString();
        if (name.Length > NameLength)
        {
            r.Fail(SyncError.InvalidValue);
            return "Player";
        }

        return name;
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
