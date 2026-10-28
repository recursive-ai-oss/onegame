using OneGame.Core;

namespace OneGame.Gltf;

/// <summary>Turns a clip's root bone movement into numbers the sim can use.</summary>
public static class RootMotion
{
    /// <summary>
    /// Bakes the translation of a root node into a fixed-point step per tick. Run it once at load time (it uses floats; the result is plain
    /// <see cref="Fixed"/> data). The steps are the differences of positions rounded to fixed point, so they add up exactly to the clip's
    /// total movement with no drift over many loops. The clip is stretched to a whole number of ticks.
    /// </summary>
    /// <param name="clip">The clip.</param>
    /// <param name="rootNode">The node that carries the movement. Its parents should not rotate or scale; the node's own translation is used as written.</param>
    /// <param name="ticksPerSecond">The simulation rate.</param>
    /// <returns>One step per tick, in the model's axes (-Z is forward). A clip that does not animate the node gives all zeros.</returns>
    public static RootMotionTrack Bake(AnimationClip clip, int rootNode, int ticksPerSecond = 60)
    {
        ArgumentNullException.ThrowIfNull(clip);
        ArgumentOutOfRangeException.ThrowIfLessThan(ticksPerSecond, 1);
        var ticks = Math.Max(1, (int)MathF.Round(clip.Duration * ticksPerSecond));
        AnimationSampler? sampler = null;
        foreach (var ch in clip.Channels)
        {
            if (ch.Node == rootNode && ch.Path == AnimationPath.Translation)
            {
                sampler = clip.Samplers[ch.Sampler];
            }
        }

        var deltas = new FVec3[ticks];
        if (sampler is null)
        {
            return new RootMotionTrack(deltas);
        }

        Span<float> v = stackalloc float[4];
        var previous = default(FVec3);
        for (var i = 0; i <= ticks; i++)
        {
            sampler.Evaluate(clip.Duration * i / ticks, v);
            var p = new FVec3(Fixed.FromDouble(v[0]), Fixed.FromDouble(v[1]), Fixed.FromDouble(v[2]));
            if (i > 0)
            {
                deltas[i - 1] = p - previous;
            }

            previous = p;
        }

        return new RootMotionTrack(deltas);
    }
}
