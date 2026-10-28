namespace OneGame.Gltf;

/// <summary>
/// A model's skeleton for animation: its rest pose, node lookup by name and clip lookup by name. Everything that samples or blends poses
/// starts from one of these. A rig is read-only and can be shared by any number of animators.
/// </summary>
public sealed class Rig
{
    private readonly NodePose _rest;
    private readonly Dictionary<string, int> _nodes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _clips = new(StringComparer.Ordinal);

    /// <summary>Creates the rig of a model.</summary>
    /// <param name="model">An imported model.</param>
    public Rig(GltfModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        Model = model;
        _rest = model.CreateRestPose();
        for (var i = 0; i < model.Nodes.Length; i++)
        {
            _nodes.TryAdd(model.Nodes[i].Name, i);
        }

        for (var i = 0; i < model.Animations.Length; i++)
        {
            _clips.TryAdd(model.Animations[i].Name, i);
        }

    }

    /// <summary>The model.</summary>
    public GltfModel Model { get; }

    /// <summary>The number of nodes in a pose.</summary>
    public int NodeCount => Model.Nodes.Length;

    // One buffer per thread, so a rig can be shared (and tests can run side by side) without two callers overwriting each other's matrices.
    [ThreadStatic]
    private static Matrix4x4[]? t_worlds;

    /// <summary>Makes a pose set to the rest pose. Poses are plain buffers: make the ones you need once and reuse them.</summary>
    public NodePose CreatePose() => Model.CreateRestPose();

    /// <summary>Sets every node of a pose to its rest value, without allocating.</summary>
    /// <param name="pose">A pose made by <see cref="CreatePose"/> (or by this model's <see cref="GltfModel.CreateRestPose"/>).</param>
    public void Reset(NodePose pose) => pose.CopyFrom(_rest);

    /// <summary>The rest value of a node's translation.</summary>
    public Vector3 RestTranslation(int node) => _rest.Translation[node];

    /// <summary>Finds a node by name.</summary>
    /// <param name="name">The node's name in the file.</param>
    /// <returns>The node index.</returns>
    /// <exception cref="KeyNotFoundException">There is no such node; the message lists the names.</exception>
    public int Node(string name)
        => _nodes.TryGetValue(name, out var i) ? i : throw new KeyNotFoundException($"No node named '{name}'. Nodes: {string.Join(", ", _nodes.Keys)}.");

    /// <summary>Finds a clip by name.</summary>
    /// <param name="name">The animation's name in the file.</param>
    /// <exception cref="KeyNotFoundException">There is no such clip; the message lists the names.</exception>
    public AnimationClip Clip(string name)
        => _clips.TryGetValue(name, out var i) ? Model.Animations[i] : throw new KeyNotFoundException($"No animation named '{name}'. Animations: {string.Join(", ", _clips.Keys)}.");

    /// <summary>Samples a clip at a time into a pose, from the rest pose, so properties the clip does not animate are at rest.</summary>
    /// <param name="clip">The clip.</param>
    /// <param name="seconds">Time in the clip; outside it the first or last values hold.</param>
    /// <param name="pose">Receives the result.</param>
    public void Sample(AnimationClip clip, float seconds, NodePose pose)
    {
        Reset(pose);
        clip.Apply(pose, seconds);
    }

    /// <summary>Computes every node's transform in the model's root space for a pose, into a buffer owned by the calling thread (valid until this thread's next call, on any rig).</summary>
    /// <param name="pose">The pose.</param>
    public ReadOnlySpan<Matrix4x4> ComputeWorld(NodePose pose)
    {
        var n = Model.Nodes.Length;
        if (t_worlds is null || t_worlds.Length < n)
        {
            t_worlds = new Matrix4x4[n];
        }

        pose.ComputeWorld(Model, t_worlds);
        return t_worlds.AsSpan(0, n);
    }
}
