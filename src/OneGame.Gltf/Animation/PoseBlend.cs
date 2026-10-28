namespace OneGame.Gltf;

/// <summary>Per-node weights that limit how much of a blend applies: for example only the upper body of a character.</summary>
public sealed class PoseMask
{
    private readonly float[] _weights;

    private PoseMask(float[] weights)
    {
        _weights = weights;
    }

    /// <summary>The weight of a node, 0 to 1.</summary>
    public float this[int node] => _weights[node];

    /// <summary>A mask that covers a node and everything below it.</summary>
    /// <param name="model">The model.</param>
    /// <param name="node">The top node of the part to cover.</param>
    /// <param name="weight">The weight of the covered nodes; the rest are 0.</param>
    public static PoseMask Subtree(GltfModel model, int node, float weight = 1f)
    {
        ArgumentNullException.ThrowIfNull(model);
        var w = new float[model.Nodes.Length];
        for (var i = 0; i < w.Length; i++)
        {
            for (var n = i; n >= 0; n = model.Nodes[n].Parent)
            {
                if (n == node)
                {
                    w[i] = weight;
                    break;
                }
            }
        }

        return new PoseMask(w);
    }

    /// <summary>A mask from explicit weights, one per node.</summary>
    /// <param name="weights">Values from 0 to 1.</param>
    public static PoseMask FromWeights(float[] weights)
    {
        ArgumentNullException.ThrowIfNull(weights);
        return new PoseMask((float[])weights.Clone());
    }
}

/// <summary>Combines poses. All operations work in place on buffers you own and allocate nothing.</summary>
public static class PoseBlend
{
    /// <summary>Blends two poses: <c>dst = a + (b - a) * t</c> per node. Translations, scales and morph weights are interpolated linearly; rotations take the shorter way and are normalized (a good match for poses that are near each other).</summary>
    /// <param name="dst">Receives the result; it may be <paramref name="a"/> or <paramref name="b"/>.</param>
    /// <param name="a">The pose at weight 0.</param>
    /// <param name="b">The pose at weight 1.</param>
    /// <param name="t">The weight of <paramref name="b"/>, clamped to 0..1.</param>
    /// <param name="mask">Optional per-node limit on <paramref name="t"/>.</param>
    public static void Lerp(NodePose dst, NodePose a, NodePose b, float t, PoseMask? mask = null)
    {
        ArgumentNullException.ThrowIfNull(dst);
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        t = Math.Clamp(t, 0f, 1f);
        var n = dst.Translation.Length;
        for (var i = 0; i < n; i++)
        {
            var w = mask is null ? t : t * mask[i];
            if (w <= 0f)
            {
                if (!ReferenceEquals(dst, a))
                {
                    Copy(dst, a, i);
                }

                continue;
            }

            dst.Translation[i] = Vector3.Lerp(a.Translation[i], b.Translation[i], w);
            dst.Scale[i] = Vector3.Lerp(a.Scale[i], b.Scale[i], w);
            var qa = a.Rotation[i];
            var qb = b.Rotation[i];
            if (Quaternion.Dot(qa, qb) < 0f)
            {
                qb = Quaternion.Negate(qb);
            }

            dst.Rotation[i] = Quaternion.Normalize(new Quaternion(
                qa.X + ((qb.X - qa.X) * w), qa.Y + ((qb.Y - qa.Y) * w), qa.Z + ((qb.Z - qa.Z) * w), qa.W + ((qb.W - qa.W) * w)));
            var wa = a.Weights[i];
            var wb = b.Weights[i];
            var wd = dst.Weights[i];
            if (wd.Length == wa.Length && wa.Length == wb.Length)
            {
                for (var k = 0; k < wd.Length; k++)
                {
                    wd[k] = wa[k] + ((wb[k] - wa[k]) * w);
                }
            }
        }
    }

    private static void Copy(NodePose dst, NodePose src, int i)
    {
        dst.Translation[i] = src.Translation[i];
        dst.Rotation[i] = src.Rotation[i];
        dst.Scale[i] = src.Scale[i];
        if (dst.Weights[i].Length == src.Weights[i].Length)
        {
            Array.Copy(src.Weights[i], dst.Weights[i], src.Weights[i].Length);
        }
    }
}

/// <summary>A crossfade clock: how much of the new pose shows, over a duration.</summary>
public struct Crossfade
{
    private float _duration;
    private float _elapsed;

    /// <summary>Starts a fade.</summary>
    /// <param name="seconds">How long it lasts; zero or less is finished at once.</param>
    public Crossfade(float seconds)
    {
        _duration = Math.Max(0f, seconds);
        _elapsed = 0f;
    }

    /// <summary>The share of the new pose, 0 at the start and 1 at the end. Linear in time.</summary>
    public readonly float Weight => _duration <= 0f ? 1f : Math.Min(1f, _elapsed / _duration);

    /// <summary>True once the fade reached its end.</summary>
    public readonly bool Done => _elapsed >= _duration;

    /// <summary>Moves the clock on.</summary>
    /// <param name="dt">Seconds, not negative.</param>
    public void Advance(float dt) => _elapsed += Math.Max(0f, dt);
}
