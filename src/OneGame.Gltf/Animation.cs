namespace OneGame.Gltf;

/// <summary>Which node property a channel animates.</summary>
public enum AnimationPath
{
    /// <summary>Local translation (3 floats per key).</summary>
    Translation,

    /// <summary>Local rotation as a quaternion (4 floats per key).</summary>
    Rotation,

    /// <summary>Local scale (3 floats per key).</summary>
    Scale,

    /// <summary>Morph target weights (one float per target per key).</summary>
    Weights,
}

/// <summary>How values between two keys are computed.</summary>
public enum AnimationInterpolation
{
    /// <summary>Linear; rotations use spherical interpolation.</summary>
    Linear,

    /// <summary>The earlier key's value holds until the next key.</summary>
    Step,

    /// <summary>Cubic Hermite spline with in and out tangents per key.</summary>
    CubicSpline,
}

/// <summary>Keys and values of one animated property.</summary>
public sealed class AnimationSampler
{
    /// <summary>Key times in seconds, increasing.</summary>
    public float[] Input { get; internal set; } = [];

    /// <summary>Key values, flattened. Linear and step: <see cref="Components"/> floats per key. Cubic spline: in-tangent, value and out-tangent per key.</summary>
    public float[] Output { get; internal set; } = [];

    /// <summary>Floats in one value: 3 for translation and scale, 4 for rotation, the target count for weights.</summary>
    public int Components { get; internal set; }

    /// <summary>How values between keys are computed.</summary>
    public AnimationInterpolation Interpolation { get; internal set; }

    /// <summary>True when the values are rotations (renormalized after interpolation).</summary>
    public bool IsRotation { get; internal set; }

    /// <summary>Evaluates the sampler at a time; times outside the keys use the first or last value.</summary>
    /// <param name="time">Seconds.</param>
    /// <param name="result">Receives <see cref="Components"/> floats.</param>
    public void Evaluate(float time, Span<float> result)
    {
        var n = Input.Length;
        var c = Components;
        if (result.Length < c)
        {
            throw new ArgumentException("The result is shorter than the sampler's components.", nameof(result));
        }

        var cubic = Interpolation == AnimationInterpolation.CubicSpline;
        var stride = cubic ? c * 3 : c;
        var valueOffset = cubic ? c : 0;
        if (n == 0 || float.IsNaN(time) || time <= Input[0])
        {
            if (n > 0)
            {
                Output.AsSpan(valueOffset, c).CopyTo(result);
            }

            return;
        }

        if (time >= Input[n - 1])
        {
            Output.AsSpan(((n - 1) * stride) + valueOffset, c).CopyTo(result);
            return;
        }

        // The last key at or before the time: Input[k] <= time < Input[k + 1].
        int lo = 0, hi = n - 1;
        while (hi - lo > 1)
        {
            var mid = lo + ((hi - lo) >> 1);
            if (Input[mid] <= time)
            {
                lo = mid;
            }
            else
            {
                hi = mid;
            }
        }

        var k = lo;
        var td = Input[k + 1] - Input[k];
        var t = (time - Input[k]) / td;
        var a = Output.AsSpan((k * stride) + valueOffset, c);
        var b = Output.AsSpan(((k + 1) * stride) + valueOffset, c);
        switch (Interpolation)
        {
            case AnimationInterpolation.Step:
                a.CopyTo(result);
                return;
            case AnimationInterpolation.Linear:
                if (IsRotation)
                {
                    var q = Quaternion.Normalize(Quaternion.Slerp(new Quaternion(a[0], a[1], a[2], a[3]), new Quaternion(b[0], b[1], b[2], b[3]), t));
                    result[0] = q.X;
                    result[1] = q.Y;
                    result[2] = q.Z;
                    result[3] = q.W;
                }
                else
                {
                    for (var i = 0; i < c; i++)
                    {
                        result[i] = a[i] + ((b[i] - a[i]) * t);
                    }
                }

                return;
            default:
                // glTF Appendix C: p(t) = (2t^3 - 3t^2 + 1) v_k + (t^3 - 2t^2 + t) td b_k + (-2t^3 + 3t^2) v_k+1 + (t^3 - t^2) td a_k+1,
                // with b_k the out-tangent of key k and a_k+1 the in-tangent of key k+1.
                var t2 = t * t;
                var t3 = t2 * t;
                var h00 = (2 * t3) - (3 * t2) + 1;
                var h10 = t3 - (2 * t2) + t;
                var h01 = (-2 * t3) + (3 * t2);
                var h11 = t3 - t2;
                var outTangent = Output.AsSpan((k * stride) + (2 * c), c);
                var inTangent = Output.AsSpan(((k + 1) * stride), c);
                for (var i = 0; i < c; i++)
                {
                    result[i] = (h00 * a[i]) + (h10 * td * outTangent[i]) + (h01 * b[i]) + (h11 * td * inTangent[i]);
                }

                if (IsRotation)
                {
                    var q = Quaternion.Normalize(new Quaternion(result[0], result[1], result[2], result[3]));
                    result[0] = q.X;
                    result[1] = q.Y;
                    result[2] = q.Z;
                    result[3] = q.W;
                }

                return;
        }
    }
}

/// <summary>Connects a sampler to a node property.</summary>
public sealed class AnimationChannel
{
    /// <summary>Index into <see cref="AnimationClip.Samplers"/>.</summary>
    public int Sampler { get; internal set; }

    /// <summary>Index of the animated node.</summary>
    public int Node { get; internal set; }

    /// <summary>The animated property.</summary>
    public AnimationPath Path { get; internal set; }
}

/// <summary>An animation clip: channels that drive node properties over time.</summary>
public sealed class AnimationClip
{
    /// <summary>The clip's name, or empty.</summary>
    public string Name { get; internal set; } = "";

    /// <summary>The channels.</summary>
    public AnimationChannel[] Channels { get; internal set; } = [];

    /// <summary>The samplers the channels use.</summary>
    public AnimationSampler[] Samplers { get; internal set; } = [];

    /// <summary>The time of the last key of any sampler, in seconds.</summary>
    public float Duration { get; internal set; }

    /// <summary>Writes the animated properties at a time into a pose; other properties are left alone.</summary>
    /// <param name="pose">The pose to change, made by <see cref="GltfModel.CreateRestPose"/>.</param>
    /// <param name="time">Seconds; times outside the clip use its first or last values. Loop with <c>time % Duration</c>.</param>
    public void Apply(NodePose pose, float time)
    {
        ArgumentNullException.ThrowIfNull(pose);
        Span<float> v = stackalloc float[4];
        foreach (var ch in Channels)
        {
            var s = Samplers[ch.Sampler];
            if (ch.Path == AnimationPath.Weights)
            {
                var w = pose.Weights[ch.Node];
                if (w.Length != s.Components)
                {
                    continue;
                }

                s.Evaluate(time, w);
                continue;
            }

            s.Evaluate(time, v);
            switch (ch.Path)
            {
                case AnimationPath.Translation: pose.Translation[ch.Node] = new Vector3(v[0], v[1], v[2]); break;
                case AnimationPath.Rotation: pose.Rotation[ch.Node] = new Quaternion(v[0], v[1], v[2], v[3]); break;
                default: pose.Scale[ch.Node] = new Vector3(v[0], v[1], v[2]); break;
            }
        }
    }
}

/// <summary>The animatable state of every node: translation, rotation, scale and morph weights.</summary>
public sealed class NodePose
{
    internal NodePose(int count)
    {
        Translation = new Vector3[count];
        Rotation = new Quaternion[count];
        Scale = new Vector3[count];
        Weights = new float[count][];
    }

    /// <summary>Local translation per node.</summary>
    public Vector3[] Translation { get; }

    /// <summary>Local rotation per node.</summary>
    public Quaternion[] Rotation { get; }

    /// <summary>Local scale per node.</summary>
    public Vector3[] Scale { get; }

    /// <summary>Morph weights per node (empty when the node has no morph targets).</summary>
    public float[][] Weights { get; }

    /// <summary>Computes every node's transform to the model's root space.</summary>
    /// <param name="model">The model this pose came from.</param>
    /// <param name="world">Receives one matrix per node (row-vector convention: child = local * parent).</param>
    public void ComputeWorld(GltfModel model, Span<Matrix4x4> world)
    {
        ArgumentNullException.ThrowIfNull(model);
        var nodes = model.Nodes;
        if (world.Length < nodes.Length || Translation.Length != nodes.Length)
        {
            throw new ArgumentException("The pose or the destination does not match the model's node count.");
        }

        foreach (var i in model.ParentFirst)
        {
            var n = nodes[i];
            var local = n.FromMatrix
                ? n.Local
                : Matrix4x4.CreateScale(Scale[i]) * Matrix4x4.CreateFromQuaternion(Quaternion.Normalize(Rotation[i])) * Matrix4x4.CreateTranslation(Translation[i]);
            world[i] = n.Parent < 0 ? local : local * world[n.Parent];
        }
    }
}
