using System.Globalization;
using System.Text;

namespace OneGame.Corpus;

/// <summary>What to generate. Defaults give roughly 20 to 50 MB per game; <see cref="Sample"/> gives a few hundred KB for the three games.</summary>
internal sealed class CorpusOptions
{
    public ulong Seed { get; set; } = 1;

    public string Out { get; set; } = "corpus";

    public List<string> Games { get; } = ["lanterns", "bricks", "solitaire"];

    public bool Sample { get; set; }

    /// <summary>Ticks to run per game. Null: run until the frame budget is full.</summary>
    public int? Ticks { get; set; }

    /// <summary>Byte budget per kind and game, overriding the defaults.</summary>
    public Dictionary<string, long> Budget { get; } = [];

    /// <summary>Sync pieces: messages longer than this are sent in pieces. Lower it to see pieces in a small corpus.</summary>
    public int PieceBytes { get; set; } = 256 * 1024;

    /// <summary>Bots in Lanterns.</summary>
    public int Bots { get; set; } = 6;

    /// <summary>Parses command-line arguments; throws <see cref="ArgumentException"/> with a message that says what to type.</summary>
    public static CorpusOptions Parse(string[] args)
    {
        var o = new CorpusOptions();
        for (var i = 0; i < args.Length; i++)
        {
            string Value() => i + 1 < args.Length ? args[++i] : throw new ArgumentException($"{args[i]} needs a value.");
            switch (args[i])
            {
                case "--seed":
                    o.Seed = ulong.Parse(Value(), CultureInfo.InvariantCulture);
                    break;
                case "--out":
                    o.Out = Value();
                    break;
                case "--games":
                    o.Games.Clear();
                    o.Games.AddRange(Value().Split(',', StringSplitOptions.RemoveEmptyEntries));
                    break;
                case "--sample":
                    o.Sample = true;
                    break;
                case "--ticks":
                    o.Ticks = int.Parse(Value(), CultureInfo.InvariantCulture);
                    break;
                case "--budget":
                    var pair = Value().Split('=', 2);
                    if (pair.Length != 2 || Array.IndexOf(Kinds.All, pair[0]) < 0)
                    {
                        throw new ArgumentException("--budget takes kind=size, with kind one of " + string.Join(", ", Kinds.All) + " and size like 500K or 12M.");
                    }

                    o.Budget[pair[0]] = Size(pair[1]);
                    break;
                case "--piece-bytes":
                    o.PieceBytes = int.Parse(Value(), CultureInfo.InvariantCulture);
                    break;
                case "--bots":
                    o.Bots = int.Parse(Value(), CultureInfo.InvariantCulture);
                    break;
                default:
                    throw new ArgumentException("Unknown option " + args[i] + ". Options: --seed N --out DIR [--games lanterns,bricks,solitaire] [--sample] [--ticks N] [--budget kind=SIZE] [--piece-bytes N] [--bots N].");
            }
        }

        foreach (var g in o.Games)
        {
            if (g is not ("lanterns" or "bricks" or "solitaire"))
            {
                throw new ArgumentException("Unknown game " + g + ". Games: lanterns, bricks, solitaire.");
            }
        }

        return o;
    }

    private static long Size(string text)
    {
        var t = text.Trim().ToUpperInvariant();
        var unit = t[^1] switch { 'K' => 1L << 10, 'M' => 1L << 20, 'G' => 1L << 30, _ => 1L };
        return long.Parse(unit == 1 ? t : t[..^1], CultureInfo.InvariantCulture) * unit;
    }

    /// <summary>The budgets in effect: defaults, then <c>--budget</c> overrides.</summary>
    public Dictionary<string, long> Budgets()
    {
        var b = Sample
            ? new Dictionary<string, long> { [Kinds.Frames] = 30 << 10, [Kinds.Snapshots] = 20 << 10, [Kinds.Saves] = 20 << 10, [Kinds.Recordings] = 40 << 10, [Kinds.Transfer] = 8 << 10 }
            : new Dictionary<string, long> { [Kinds.Frames] = 12L << 20, [Kinds.Snapshots] = 6L << 20, [Kinds.Saves] = 6L << 20, [Kinds.Recordings] = 6L << 20, [Kinds.Transfer] = 2L << 20, ["solitaire:" + Kinds.Frames] = 18L << 20 };
        foreach (var (k, v) in Budget)
        {
            b[k] = v;
            foreach (var game in Games)
            {
                b.Remove(game + ":" + k);
            }
        }

        return b;
    }

    /// <summary>The sample keeps every Nth frame, so a few hundred KB still span the whole run.</summary>
    public Dictionary<string, int> Strides() => Sample ? new Dictionary<string, int> { [Kinds.Frames] = 25, [Kinds.Snapshots] = 2, [Kinds.Saves] = 1 } : [];

    /// <summary>The report printed when the run ends.</summary>
    public static string Report(Sink sink, IEnumerable<string> games, string manifestHash)
    {
        var s = new StringBuilder("{\"manifestSha256\":\"").Append(manifestHash).Append("\",\"games\":{");
        var firstGame = true;
        long all = 0;
        foreach (var g in games)
        {
            s.Append(firstGame ? "" : ",").Append('"').Append(g).Append("\":{");
            firstGame = false;
            var firstKind = true;
            long total = 0;
            foreach (var k in Kinds.All)
            {
                var (files, bytes) = sink.Totals(g, k);
                s.Append(firstKind ? "" : ",").Append('"').Append(k).Append("\":{\"files\":").Append(files).Append(",\"bytes\":").Append(bytes).Append('}');
                firstKind = false;
                total += bytes;
            }

            s.Append(",\"bytes\":").Append(total).Append('}');
            all += total;
        }

        return s.Append("},\"bytes\":").Append(all).Append('}').ToString();
    }
}
