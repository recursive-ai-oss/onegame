using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace OneGame.Corpus;

/// <summary>The kinds of payload the corpus holds, as folder names.</summary>
internal static class Kinds
{
    public const string Frames = "frames";
    public const string Snapshots = "snapshots";
    public const string Saves = "saves";
    public const string Recordings = "recordings";
    public const string Transfer = "transfer";

    public static readonly string[] All = [Frames, Snapshots, Saves, Recordings, Transfer];
}

/// <summary>Writes payload files and the manifest. Every payload is one file; budgets and strides decide which are kept.</summary>
internal sealed class Sink
{
    private readonly string _root;
    private readonly Dictionary<string, long> _budget;
    private readonly Dictionary<string, int> _stride;
    private readonly Dictionary<(string Game, string Kind), Counter> _counters = [];
    private readonly List<string> _rows = [];
    private readonly HashSet<string> _made = [];

    public Sink(string root, Dictionary<string, long> budget, Dictionary<string, int> stride)
    {
        _root = root;
        _budget = budget;
        _stride = stride;
    }

    /// <summary>True once a kind of a game has used its byte budget.</summary>
    public bool Full(string game, string kind) => Count(game, kind).Full;

    /// <summary>Keeps a payload unless its kind is over budget or the stride skips it. Returns whether it was written.</summary>
    public bool Add(string game, string kind, ReadOnlySpan<byte> data, string schema, long tick)
    {
        var c = Count(game, kind);
        c.Seen++;
        if (c.Full || (c.Seen - 1) % Stride(kind) != 0)
        {
            return false;
        }

        if (c.Bytes + data.Length > Budget(game, kind))
        {
            c.Full = true;
            return false;
        }

        var dir = Path.Combine(_root, game, kind);
        if (_made.Add(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var name = c.Files.ToString("D6", CultureInfo.InvariantCulture) + ".bin";
        File.WriteAllBytes(Path.Combine(dir, name), data.ToArray());
        var hash = Convert.ToHexStringLower(SHA256.HashData(data));
        _rows.Add(string.Join('\t', game, kind, game + "/" + kind + "/" + name, data.Length.ToString(CultureInfo.InvariantCulture), hash, schema, tick.ToString(CultureInfo.InvariantCulture)));
        c.Files++;
        c.Bytes += data.Length;
        return true;
    }

    /// <summary>Files and bytes kept for a game and kind.</summary>
    public (int Files, long Bytes) Totals(string game, string kind)
    {
        var c = Count(game, kind);
        return (c.Files, c.Bytes);
    }

    /// <summary>Writes <c>manifest.tsv</c> and returns its SHA-256.</summary>
    public string WriteManifest()
    {
        Directory.CreateDirectory(_root);
        var text = new StringBuilder("game\tkind\tfile\tsize\tsha256\tschema\ttick\n");
        foreach (var row in _rows)
        {
            text.Append(row).Append('\n');
        }

        var bytes = new UTF8Encoding(false).GetBytes(text.ToString());
        File.WriteAllBytes(Path.Combine(_root, "manifest.tsv"), bytes);
        return Convert.ToHexStringLower(SHA256.HashData(bytes));
    }

    private long Budget(string game, string kind) => _budget.TryGetValue(game + ":" + kind, out var g) ? g : _budget.TryGetValue(kind, out var b) ? b : long.MaxValue;

    private int Stride(string kind) => _stride.TryGetValue(kind, out var s) ? s : 1;

    private Counter Count(string game, string kind)
    {
        if (!_counters.TryGetValue((game, kind), out var c))
        {
            _counters[(game, kind)] = c = new Counter();
        }

        return c;
    }

    private sealed class Counter
    {
        public int Files;
        public long Bytes;
        public long Seen;
        public bool Full;
    }
}
