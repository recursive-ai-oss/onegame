using System.Reflection;
using OneGame.Core;
using OneYaml;

namespace Bricks;

/// <summary>
/// A level file, as authored. The loader rejects unknown keys, so a misspelt field is an error with a position in the file rather than a
/// silently ignored setting.
/// </summary>
/// <param name="Name">The level's name, shown on the HUD.</param>
/// <param name="Rows">Up to fourteen rows of twelve glyphs: <c>.</c> empty, <c>1</c> to <c>6</c> a one-hit brick in one of six colours,
/// <c>T</c> a brick that takes two hits, <c>H</c> one that takes three, <c>#</c> a brick nothing breaks.</param>
/// <param name="Speed">Extra ball speed in fiftieths of a unit per tick (0 to 100).</param>
/// <param name="Capsules">The chance, in percent, that a broken brick drops a power-up (0 to 100).</param>
public sealed record LevelFile(string Name, List<string> Rows, int Speed = 0, int Capsules = 12);

/// <summary>Reads the YAML level files through generated metadata, so nothing is found by reflection at run time.</summary>
[YamlSerializable(typeof(LevelFile))]
internal sealed partial class LevelYaml : YamlSerializerContext;

/// <summary>A level file that does not describe a playable level.</summary>
public sealed class LevelException : Exception
{
    /// <summary>Creates the exception.</summary>
    public LevelException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with the error that caused it.</summary>
    public LevelException(string message, Exception inner)
        : base(message, inner)
    {
    }

    /// <summary>Creates an empty exception.</summary>
    public LevelException()
    {
    }
}

/// <summary>A checked level, ready to put on the court.</summary>
public sealed class LevelData
{
    /// <summary>Creates a level from its parts; use <see cref="Levels.Parse"/> for text.</summary>
    internal LevelData(string name, ushort[] cells, Fixed speed, int capsulePercent)
    {
        Name = name;
        Cells = cells;
        Speed = speed;
        CapsulePercent = capsulePercent;
        foreach (var c in cells)
        {
            if (Brick.IsBreakable(c))
            {
                Breakable++;
            }
        }
    }

    /// <summary>The level's name.</summary>
    public string Name { get; }

    /// <summary>The packed bricks, row by row: <see cref="Rules.Columns"/> by <see cref="Rules.Rows"/>.</summary>
    public ushort[] Cells { get; }

    /// <summary>Extra ball speed on top of the speed for the level's number.</summary>
    public Fixed Speed { get; }

    /// <summary>Chance in percent that a broken brick drops a power-up.</summary>
    public int CapsulePercent { get; }

    /// <summary>How many bricks the player has to break.</summary>
    public int Breakable { get; }
}

/// <summary>
/// The game's levels, loaded once from the YAML files in the program and never changed afterwards. The simulation reads them when a level
/// starts and copies the bricks into synced state, so a saved game or a replica never needs the files. The class is vouched for as
/// <see cref="SimSafeAttribute"/> because the loading (file parsing) happens once at start-up, before any tick, and the result is read-only.
/// </summary>
[SimSafe]
public static class Levels
{
    private static readonly LevelData[] All = LoadEmbedded();

    /// <summary>How many levels there are.</summary>
    public static int Count => All.Length;

    /// <summary>The level with the given number (1-based).</summary>
    public static LevelData Get(int number) => All[number - 1];

    /// <summary>Checks and converts the text of one level file.</summary>
    /// <exception cref="LevelException">The text is not valid YAML for a level, or the level breaks a rule (row length, glyphs, ranges).</exception>
    public static LevelData Parse(string yaml, string source)
    {
        ArgumentNullException.ThrowIfNull(yaml);
        LevelFile file;
        try
        {
            var options = new YamlSerializerOptions
            {
                UnmappedMemberHandling = YamlUnmappedMemberHandling.Disallow,
                Limits = YamlLimits.Untrusted,
                SourceName = source,
            };
            file = YamlSerializer.Deserialize(yaml, new LevelYaml(options).LevelFile) ?? throw new LevelException(source + ": the file is empty.");
        }
        catch (YamlException ex)
        {
            throw new LevelException(source + ": " + ex.Message, ex);
        }

        if (file.Rows is null || file.Rows.Count is 0 or > Rules.Rows)
        {
            throw new LevelException($"{source}: 'rows' needs 1 to {Rules.Rows} rows.");
        }

        if (file.Speed is < 0 or > 100 || file.Capsules is < 0 or > 100)
        {
            throw new LevelException($"{source}: 'speed' and 'capsules' are each 0 to 100.");
        }

        var cells = new ushort[Rules.Columns * Rules.Rows];
        for (var row = 0; row < file.Rows.Count; row++)
        {
            var text = file.Rows[row] ?? string.Empty;
            if (text.Length != Rules.Columns)
            {
                throw new LevelException($"{source}: row {row + 1} has {text.Length} glyphs and each row needs {Rules.Columns}.");
            }

            for (var col = 0; col < Rules.Columns; col++)
            {
                cells[(row * Rules.Columns) + col] = text[col] switch
                {
                    '.' => 0,
                    >= '1' and <= '6' => Brick.Pack(1, 1, text[col] - '0'),
                    'T' => Brick.Pack(2, 2, 7),
                    'H' => Brick.Pack(3, 3, 7),
                    '#' => Brick.Pack(Brick.Unbreakable, Brick.Unbreakable, 8),
                    _ => throw new LevelException($"{source}: row {row + 1} has '{text[col]}', which is not one of . 1-6 T H #."),
                };
            }
        }

        var level = new LevelData(file.Name ?? string.Empty, cells, Fixed.FromRatio(file.Speed, 50), file.Capsules);
        return level.Breakable > 0 ? level : throw new LevelException(source + ": the level has nothing to break.");
    }

    private static LevelData[] LoadEmbedded()
    {
        var assembly = typeof(Levels).Assembly;
        var names = assembly.GetManifestResourceNames().Where(n => n.StartsWith("levels/", StringComparison.Ordinal)).ToList();
        names.Sort(StringComparer.Ordinal);
        var list = new List<LevelData>();
        foreach (var name in names)
        {
            using var stream = assembly.GetManifestResourceStream(name) ?? throw new LevelException(name + " is missing.");
            using var reader = new StreamReader(stream);
            list.Add(Parse(reader.ReadToEnd(), name));
        }

        return [.. list];
    }
}
