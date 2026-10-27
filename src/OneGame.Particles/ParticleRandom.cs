namespace OneGame.Particles;

// A small seedable generator (SplitMix-style mixing of a counter). Particles are view-only, so this is independent of the simulation's streams.
internal struct ParticleRandom
{
    private ulong _state;

    public ParticleRandom(uint seed) => _state = ((ulong)seed << 32) | 0x9E3779B9u;

    public void Reseed(uint seed) => _state = ((ulong)seed << 32) | 0x9E3779B9u;

    public uint NextUInt()
    {
        _state += 0x9E3779B97F4A7C15UL;
        var z = _state;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        z ^= z >> 31;
        return (uint)(z >> 32);
    }

    /// <summary>A number in [0, 1).</summary>
    public float NextSingle() => (NextUInt() >> 8) * (1f / 16777216f);

    public float Next(FloatRange r) => r.Min + ((r.Max - r.Min) * NextSingle());
}
