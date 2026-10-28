namespace OneGame.Gltf;

/// <summary>
/// Something that produces a pose from a phase: one clip, or a blend of clips chosen by inputs. Phase is a share of the motion's length
/// (0 to 1), so clips of different lengths in one blend stay in step. Use a motion by hand, or give it to an <see cref="Animator"/> state.
/// </summary>
public abstract class Motion
{
    /// <summary>The input this motion reads for its first axis (set by <see cref="Animator"/> from the parameter of this name), or null.</summary>
    public string? XParameter { get; init; }

    /// <summary>The input for the second axis of a 2D blend, or null.</summary>
    public string? YParameter { get; init; }

    /// <summary>The first input value.</summary>
    public float X { get; set; }

    /// <summary>The second input value.</summary>
    public float Y { get; set; }

    /// <summary>How long one pass takes in seconds at the current inputs.</summary>
    public abstract float Duration { get; }

    /// <summary>Writes the pose at a phase.</summary>
    /// <param name="rig">The rig the clips belong to.</param>
    /// <param name="phase">0 to 1 through one pass.</param>
    /// <param name="dst">Receives the pose.</param>
    /// <param name="scratch">A second pose buffer the motion may use.</param>
    public abstract void Sample(Rig rig, float phase, NodePose dst, NodePose scratch);
}

/// <summary>A single clip.</summary>
public sealed class ClipMotion : Motion
{
    private readonly AnimationClip _clip;

    /// <summary>Creates a motion from a clip.</summary>
    /// <param name="clip">The clip.</param>
    public ClipMotion(AnimationClip clip)
    {
        ArgumentNullException.ThrowIfNull(clip);
        _clip = clip;
    }

    /// <inheritdoc />
    public override float Duration => Math.Max(_clip.Duration, 1e-4f);

    /// <inheritdoc />
    public override void Sample(Rig rig, float phase, NodePose dst, NodePose scratch)
        => rig.Sample(_clip, Math.Clamp(phase, 0f, 1f) * _clip.Duration, dst);
}

/// <summary>Clips blended along one input, for example speed.</summary>
public sealed class BlendSpace1DMotion : Motion
{
    private readonly BlendSpace1D _space;
    private readonly AnimationClip[] _clips;

    /// <summary>Creates the motion.</summary>
    /// <param name="space">The positions of the clips.</param>
    /// <param name="clips">One clip per position, in the same order.</param>
    /// <exception cref="ArgumentException">The counts differ.</exception>
    public BlendSpace1DMotion(BlendSpace1D space, params AnimationClip[] clips)
    {
        ArgumentNullException.ThrowIfNull(space);
        ArgumentNullException.ThrowIfNull(clips);
        if (clips.Length != space.Count)
        {
            throw new ArgumentException($"The blend space has {space.Count} samples but {clips.Length} clips were given.", nameof(clips));
        }

        _space = space;
        _clips = (AnimationClip[])clips.Clone();
    }

    /// <inheritdoc />
    public override float Duration
    {
        get
        {
            _space.Locate(X, out var a, out var b, out var t);
            return Math.Max(1e-4f, _clips[a].Duration + ((_clips[b].Duration - _clips[a].Duration) * t));
        }
    }

    /// <inheritdoc />
    public override void Sample(Rig rig, float phase, NodePose dst, NodePose scratch)
    {
        phase = Math.Clamp(phase, 0f, 1f);
        _space.Locate(X, out var a, out var b, out var t);
        rig.Sample(_clips[a], phase * _clips[a].Duration, dst);
        if (b != a && t > 0f)
        {
            rig.Sample(_clips[b], phase * _clips[b].Duration, scratch);
            PoseBlend.Lerp(dst, dst, scratch, t);
        }
    }
}

/// <summary>Clips blended over a plane, for example direction by speed.</summary>
public sealed class BlendSpace2DMotion : Motion
{
    private readonly BlendSpace2D _space;
    private readonly AnimationClip[] _clips;
    private readonly float[] _weights;

    /// <summary>Creates the motion.</summary>
    /// <param name="space">The positions of the clips.</param>
    /// <param name="clips">One clip per position, in the same order.</param>
    /// <exception cref="ArgumentException">The counts differ.</exception>
    public BlendSpace2DMotion(BlendSpace2D space, params AnimationClip[] clips)
    {
        ArgumentNullException.ThrowIfNull(space);
        ArgumentNullException.ThrowIfNull(clips);
        if (clips.Length != space.Count)
        {
            throw new ArgumentException($"The blend space has {space.Count} samples but {clips.Length} clips were given.", nameof(clips));
        }

        _space = space;
        _clips = (AnimationClip[])clips.Clone();
        _weights = new float[clips.Length];
    }

    /// <inheritdoc />
    public override float Duration
    {
        get
        {
            _space.Weights(new Vector2(X, Y), _weights);
            var d = 0f;
            for (var i = 0; i < _weights.Length; i++)
            {
                d += _weights[i] * _clips[i].Duration;
            }

            return Math.Max(1e-4f, d);
        }
    }

    /// <inheritdoc />
    public override void Sample(Rig rig, float phase, NodePose dst, NodePose scratch)
    {
        phase = Math.Clamp(phase, 0f, 1f);
        _space.Weights(new Vector2(X, Y), _weights);
        var total = 0f;
        var first = true;
        for (var i = 0; i < _weights.Length; i++)
        {
            var w = _weights[i];
            if (w <= 1e-6f)
            {
                continue;
            }

            if (first)
            {
                rig.Sample(_clips[i], phase * _clips[i].Duration, dst);
                first = false;
            }
            else
            {
                rig.Sample(_clips[i], phase * _clips[i].Duration, scratch);

                // Running average: after adding sample i the result holds every sample so far in proportion to its weight.
                PoseBlend.Lerp(dst, dst, scratch, w / (total + w));
            }

            total += w;
        }
    }
}
