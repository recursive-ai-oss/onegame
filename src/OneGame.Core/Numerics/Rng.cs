using System.Numerics;

namespace OneGame.Core;

/// <summary>
/// A named, seedable random stream (xoshiro256**). Sim code draws randomness only from streams obtained from
/// <see cref="RngStreams"/>, so the draws are part of the world's state: they are in snapshots and in the state hash,
/// and replaying the same commands redraws the same numbers.
/// </summary>
public sealed class Rng
{
    private ulong _s0;
    private ulong _s1;
    private ulong _s2;
    private ulong _s3;

    internal Rng(string name, ulong seed)
    {
        Name = name;
        Seed(seed);
    }

    /// <summary>The stream's name.</summary>
    public string Name { get; }

    internal void Seed(ulong seed)
    {
        // Splitmix64 expands one seed into four non-zero state words.
        _s0 = SplitMix(ref seed);
        _s1 = SplitMix(ref seed);
        _s2 = SplitMix(ref seed);
        _s3 = SplitMix(ref seed);
    }

    private static ulong SplitMix(ref ulong state)
    {
        state += 0x9E3779B97F4A7C15UL;
        return StateHash.Mix(state);
    }

    /// <summary>The next 64 random bits.</summary>
    [MutatesState]
    public ulong NextUInt64()
    {
        var result = BitOperations.RotateLeft(_s1 * 5, 7) * 9;
        var t = _s1 << 17;
        _s2 ^= _s0;
        _s3 ^= _s1;
        _s1 ^= _s2;
        _s0 ^= _s3;
        _s2 ^= t;
        _s3 = BitOperations.RotateLeft(_s3, 45);
        return result;
    }

    /// <summary>A random int in [0, <paramref name="maxExclusive"/>), without modulo bias.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxExclusive"/> is not positive.</exception>
    [MutatesState]
    public int NextInt(int maxExclusive)
    {
        if (maxExclusive <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxExclusive), "The bound must be positive.");
        }

        // Multiply-and-reject (Lemire): the high half of x * bound is uniform once the biased low range is rejected.
        var bound = (ulong)maxExclusive;
        var hi = Math.BigMul(NextUInt64(), bound, out var lo);
        if (lo < bound)
        {
            var threshold = (0UL - bound) % bound;
            while (lo < threshold)
            {
                hi = Math.BigMul(NextUInt64(), bound, out lo);
            }
        }

        return (int)hi;
    }

    /// <summary>A random int in [<paramref name="min"/>, <paramref name="maxExclusive"/>).</summary>
    /// <exception cref="ArgumentOutOfRangeException">The range is empty.</exception>
    [MutatesState]
    public int NextInt(int min, int maxExclusive)
    {
        if (maxExclusive <= min)
        {
            throw new ArgumentOutOfRangeException(nameof(maxExclusive), "The range is empty.");
        }

        return min + NextInt(maxExclusive - min);
    }

    /// <summary>A random <see cref="Fixed"/> in [0, 1).</summary>
    [MutatesState]
    public Fixed NextFixed() => Fixed.FromRaw((long)(NextUInt64() >> 32));

    /// <summary>A random <see cref="Fixed"/> in [<paramref name="min"/>, <paramref name="max"/>).</summary>
    [MutatesState]
    public Fixed NextFixed(Fixed min, Fixed max) => min + ((max - min) * NextFixed());

    /// <summary>True with the given probability; 0 is never and 1 is always.</summary>
    [MutatesState]
    public bool Chance(Fixed probability) => NextFixed() < probability;

    /// <summary>A random bool.</summary>
    [MutatesState]
    public bool NextBool() => (NextUInt64() >> 63) != 0;

    internal void CopyStateFrom(Rng other)
    {
        _s0 = other._s0;
        _s1 = other._s1;
        _s2 = other._s2;
        _s3 = other._s3;
    }

    internal void Write(SyncWriter w)
    {
        w.WriteFixed64(_s0);
        w.WriteFixed64(_s1);
        w.WriteFixed64(_s2);
        w.WriteFixed64(_s3);
    }

    internal void Read(ref SyncReader r)
    {
        var a = r.ReadFixed64();
        var b = r.ReadFixed64();
        var c = r.ReadFixed64();
        var d = r.ReadFixed64();
        if (r.Ok)
        {
            if ((a | b | c | d) == 0)
            {
                // An all-zero state never leaves zero; no honest writer produces it.
                r.Fail(SyncError.InvalidValue);
                return;
            }

            _s0 = a;
            _s1 = b;
            _s2 = c;
            _s3 = d;
        }
    }
}

/// <summary>
/// The world's random streams, found by name. Each stream is seeded from the world seed and its name, so adding a stream never
/// disturbs the others. Streams are kept in name order, so snapshots and hashes do not depend on creation order.
/// </summary>
public sealed class RngStreams
{
    private readonly List<Rng> _streams = [];
    private readonly ulong _seed;

    internal RngStreams(ulong seed) => _seed = seed;

    /// <summary>The number of streams created so far.</summary>
    public int Count => _streams.Count;

    /// <summary>Returns the stream with this name, creating it on first use.</summary>
    public Rng this[string name]
    {
        get
        {
            var index = IndexOf(name);
            if (index >= 0)
            {
                return _streams[index];
            }

            var stream = new Rng(name, _seed ^ StateHash.Of(name));
            _streams.Insert(~index, stream);
            return stream;
        }
    }

    private int IndexOf(string name)
    {
        int lo = 0, hi = _streams.Count - 1;
        while (lo <= hi)
        {
            var mid = (lo + hi) >>> 1;
            var c = string.CompareOrdinal(_streams[mid].Name, name);
            if (c == 0)
            {
                return mid;
            }

            if (c < 0)
            {
                lo = mid + 1;
            }
            else
            {
                hi = mid - 1;
            }
        }

        return ~lo;
    }

    internal void Write(SyncWriter w)
    {
        w.WriteVarUInt((ulong)_streams.Count);
        foreach (var s in _streams)
        {
            w.WriteString(s.Name);
            s.Write(w);
        }
    }

    internal void Read(ref SyncReader r)
    {
        var count = r.ReadCount(SyncLimits.MaxNamed);
        var fresh = new List<Rng>();
        string? previous = null;
        for (var i = 0; i < count && r.Ok; i++)
        {
            var name = r.ReadString();
            if (previous is not null && string.CompareOrdinal(previous, name) >= 0)
            {
                r.Fail(SyncError.InvalidValue);
                break;
            }

            previous = name;
            var s = new Rng(name, 0);
            s.Read(ref r);
            fresh.Add(s);
        }

        if (r.Ok)
        {
            // Keep existing stream objects, so code holding one sees the restored state.
            for (var i = 0; i < fresh.Count; i++)
            {
                var existing = IndexOf(fresh[i].Name);
                if (existing >= 0)
                {
                    _streams[existing].CopyStateFrom(fresh[i]);
                    fresh[i] = _streams[existing];
                }
            }

            _streams.Clear();
            _streams.AddRange(fresh);
        }
    }
}
