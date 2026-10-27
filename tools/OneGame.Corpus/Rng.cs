namespace OneGame.Corpus;

/// <summary>SplitMix64: the bots' own deterministic randomness. It never touches the games' named streams.</summary>
internal sealed class Rng
{
    private ulong _state;

    public Rng(ulong seed) => _state = seed;

    /// <summary>Mixes a seed with a label so each use gets its own stream.</summary>
    public static ulong Mix(ulong seed, ulong label)
    {
        var r = new Rng(seed ^ (label * 0x9E3779B97F4A7C15UL));
        r.Next();
        return r.Next();
    }

    public ulong Next()
    {
        _state += 0x9E3779B97F4A7C15UL;
        var z = _state;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    /// <summary>A number in [low, high).</summary>
    public int Range(int low, int high) => low + (int)(Next() % (ulong)(high - low));

    /// <summary>True with probability one in <paramref name="n"/>.</summary>
    public bool OneIn(int n) => Next() % (ulong)n == 0;
}
