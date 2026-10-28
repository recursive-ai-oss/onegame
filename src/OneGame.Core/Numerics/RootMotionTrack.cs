namespace OneGame.Core;

/// <summary>
/// The movement of an animation's root bone, one fixed-point step per simulation tick. Sim code that lets an animation move a character
/// keeps an integer tick counter in synced state and applies <see cref="DeltaAt"/> each tick, so every node computes the same position.
/// Make one at load time with <c>RootMotion.Bake</c> (in <c>OneGame.Gltf</c>) or from your own numbers; the track itself holds only
/// <see cref="Fixed"/> values and is safe to use in sim code.
/// </summary>
public sealed class RootMotionTrack
{
    private readonly FVec3[] _deltas;
    private readonly FVec3[] _sums;

    /// <summary>Creates a track from per-tick steps.</summary>
    /// <param name="deltas">Movement during tick 0, tick 1, and so on, in the model's own axes (-Z is forward).</param>
    /// <exception cref="ArgumentException">There are no steps.</exception>
    public RootMotionTrack(FVec3[] deltas)
    {
        ArgumentNullException.ThrowIfNull(deltas);
        if (deltas.Length == 0)
        {
            throw new ArgumentException("A root motion track needs at least one tick.", nameof(deltas));
        }

        _deltas = (FVec3[])deltas.Clone();
        _sums = new FVec3[deltas.Length + 1];
        for (var i = 0; i < deltas.Length; i++)
        {
            _sums[i + 1] = _sums[i] + deltas[i];
        }
    }

    /// <summary>The number of ticks in one pass of the animation.</summary>
    public int Length => _deltas.Length;

    /// <summary>The movement over one whole pass.</summary>
    public FVec3 Total => _sums[_deltas.Length];

    /// <summary>The movement during one tick.</summary>
    /// <param name="tick">Ticks since the animation started. Negative is zero movement; past the end it wraps when <paramref name="loop"/> is set and is zero otherwise.</param>
    /// <param name="loop">Whether the animation repeats.</param>
    public FVec3 DeltaAt(long tick, bool loop = true)
    {
        if (tick < 0 || (!loop && tick >= _deltas.Length))
        {
            return FVec3.Zero;
        }

        return _deltas[(int)(tick % _deltas.Length)];
    }

    /// <summary>The movement from the start of tick <paramref name="from"/> up to the start of tick <paramref name="to"/>.</summary>
    /// <param name="from">The first tick, at least 0.</param>
    /// <param name="to">The tick to stop before, not less than <paramref name="from"/>.</param>
    /// <param name="loop">Whether the animation repeats.</param>
    public FVec3 Displacement(long from, long to, bool loop = true)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(from);
        ArgumentOutOfRangeException.ThrowIfLessThan(to, from);
        return Offset(to, loop) - Offset(from, loop);
    }

    private FVec3 Offset(long tick, bool loop)
    {
        var n = _deltas.Length;
        if (!loop)
        {
            return _sums[(int)Math.Min(tick, n)];
        }

        var passes = tick / n;
        var rest = (int)(tick % n);
        return (Total * Fixed.FromInt(passes)) + _sums[rest];
    }

    /// <summary>Moves a transform by the step of one tick, turned by the transform's own rotation.</summary>
    /// <param name="at">Where the character is.</param>
    /// <param name="tick">Ticks since the animation started.</param>
    /// <param name="loop">Whether the animation repeats.</param>
    public Transform3 Step(Transform3 at, long tick, bool loop = true)
        => at.WithPosition(at.Position + at.Rotation.Rotate(DeltaAt(tick, loop) * at.Scale));
}
