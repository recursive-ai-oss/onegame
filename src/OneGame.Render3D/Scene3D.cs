using System.Runtime.InteropServices;
using OneGame.Gltf;
using OneGame.Gpu;

namespace OneGame.Render3D;

/// <summary>The curve that maps scene-referred HDR light to display values.</summary>
public enum ToneMapper
{
    /// <summary>Khronos PBR Neutral: keeps base colours as authored, compresses only bright highlights. The default.</summary>
    PbrNeutral = 0,

    /// <summary>A filmic curve (the ACES rational fit) with more contrast and a stronger highlight roll-off.</summary>
    Aces = 1,

    /// <summary>No curve: values are clamped to 0..1 after exposure.</summary>
    None = 2,
}

/// <summary>What fills the pixels no model covers.</summary>
public enum BackgroundMode
{
    /// <summary>A flat colour (<see cref="Environment3D.BackgroundColor"/>).</summary>
    Color,

    /// <summary>The environment itself, seen from the camera.</summary>
    Environment,
}

/// <summary>Image-based lighting and backdrop of a scene.</summary>
public sealed class Environment3D
{
    /// <summary>The environment to light the scene with; null uses a soft built-in sky.</summary>
    public EnvironmentMap? Map { get; set; }

    /// <summary>Scale on the light the environment contributes; 0 turns image-based lighting off.</summary>
    public float Intensity { get; set; } = 1f;

    /// <summary>Turns the environment about the +Y axis, in radians.</summary>
    public float Yaw { get; set; }

    /// <summary>What fills the pixels no model covers.</summary>
    public BackgroundMode Background { get; set; } = BackgroundMode.Environment;

    /// <summary>Linear colour for <see cref="BackgroundMode.Color"/>.</summary>
    public Vector3 BackgroundColor { get; set; } = new(0.05f, 0.05f, 0.06f);

    /// <summary>How blurred the environment backdrop is, 0 (sharp) to 1 (fully blurred).</summary>
    public float BackgroundBlur { get; set; } = 0.3f;
}

/// <summary>Bloom, exposure and tone mapping.</summary>
public sealed class PostSettings
{
    /// <summary>The tone mapping curve.</summary>
    public ToneMapper ToneMapper { get; set; } = ToneMapper.PbrNeutral;

    /// <summary>Linear multiplier applied to the HDR image before tone mapping (2^EV).</summary>
    public float Exposure { get; set; } = 1f;

    /// <summary>Whether bright areas glow.</summary>
    public bool Bloom { get; set; } = true;

    /// <summary>How much of the blurred bright image is added back (linear light).</summary>
    public float BloomStrength { get; set; } = 0.04f;
}

/// <summary>One placed copy of a <see cref="Model3D"/>, with its own pose.</summary>
public sealed class ModelInstance3D : IDisposable
{
    private readonly Matrix4x4[] _world;
    private readonly Dictionary<(int Node, int Primitive), MorphState> _morphs = [];

    internal ModelInstance3D(Model3D model)
    {
        Model = model;
        Pose = model.Source.CreateRestPose();
        _world = new Matrix4x4[model.Source.Nodes.Length];
        UpdateTransforms();
    }

    /// <summary>The model this instance draws.</summary>
    public Model3D Model { get; }

    /// <summary>Placement of the whole model in the world.</summary>
    public Matrix4x4 Transform { get; set; } = Matrix4x4.Identity;

    /// <summary>Whether the instance is drawn.</summary>
    public bool Visible { get; set; } = true;

    /// <summary>Per-node translation, rotation, scale and morph weights. Change them, then call <see cref="UpdateTransforms"/>.</summary>
    public NodePose Pose { get; }

    /// <summary>World matrix of every node of the model (before <see cref="Transform"/>), as of the last <see cref="UpdateTransforms"/>.</summary>
    public ReadOnlySpan<Matrix4x4> NodeWorlds => _world;

    /// <summary>Returns the pose to the model's rest pose.</summary>
    public void ResetPose()
    {
        var rest = Model.Source.CreateRestPose();
        for (var i = 0; i < _world.Length; i++)
        {
            Pose.Translation[i] = rest.Translation[i];
            Pose.Rotation[i] = rest.Rotation[i];
            Pose.Scale[i] = rest.Scale[i];
            Pose.Weights[i] = rest.Weights[i];
        }

        UpdateTransforms();
    }

    /// <summary>Evaluates an animation at a time (seconds) into the pose and updates the node transforms.</summary>
    /// <param name="clip">Index into <see cref="GltfModel.Animations"/>.</param>
    /// <param name="seconds">Time since the animation started.</param>
    /// <param name="loop">Wrap the time around the clip's duration.</param>
    public void Animate(int clip, float seconds, bool loop = true)
    {
        var c = Model.Source.Animations[clip];
        if (loop && c.Duration > 0)
        {
            seconds %= c.Duration;
        }

        c.Apply(Pose, seconds);
        UpdateTransforms();
    }

    /// <summary>Recomputes the node world matrices from <see cref="Pose"/>.</summary>
    public void UpdateTransforms() => Pose.ComputeWorld(Model.Source, _world);

    /// <summary>Releases the vertex buffers of morphed primitives. Call it when an instance of a model with morph targets is discarded.</summary>
    public void Dispose()
    {
        foreach (var m in _morphs.Values)
        {
            m.Buffer.Dispose();
        }

        _morphs.Clear();
    }

    private sealed class MorphState
    {
        public required GpuBuffer Buffer { get; init; }

        public required byte[] Scratch { get; init; }

        public float[] Weights { get; set; } = [];
    }

    // Blends the morph targets of one primitive on the CPU (glTF 2.0 section 3.7.2.2) into a per-instance vertex buffer. The buffer is
    // rewritten only when the weights change.
    internal GpuBuffer MorphedVertices(GpuDevice device, int node, int primitiveIndex, Primitive3D primitive, float[] weights)
    {
        var key = (node, primitiveIndex);
        if (!_morphs.TryGetValue(key, out var state))
        {
            var baseBytes = primitive.BaseVertices!;
            state = new MorphState
            {
                Buffer = device.CreateBuffer(new BufferDesc((ulong)baseBytes.Length, BufferUsage.Vertex | BufferUsage.CopyDestination) { Label = "morphed vertices" }),
                Scratch = new byte[baseBytes.Length],
                Weights = [],
            };
            _morphs[key] = state;
        }

        if (!weights.AsSpan().SequenceEqual(state.Weights))
        {
            Blend(primitive.Source, primitive.BaseVertices!, state.Scratch, weights);
            device.WriteBuffer(state.Buffer, 0, state.Scratch);
            state.Weights = (float[])weights.Clone();
        }

        return state.Buffer;
    }

    private static void Blend(GltfPrimitive p, byte[] baseBytes, byte[] scratch, float[] weights)
    {
        const int floatsPerVertex = GltfPrimitive.PackedVertexSize / 4;
        baseBytes.CopyTo(scratch, 0);
        var f = MemoryMarshal.Cast<byte, float>(scratch.AsSpan());
        for (var t = 0; t < p.MorphTargets.Length && t < weights.Length; t++)
        {
            var w = weights[t];
            if (w == 0f)
            {
                continue;
            }

            var target = p.MorphTargets[t];
            for (var v = 0; v < p.Positions.Length; v++)
            {
                var o = v * floatsPerVertex;
                if (target.Positions is { } dp)
                {
                    f[o] += w * dp[v].X;
                    f[o + 1] += w * dp[v].Y;
                    f[o + 2] += w * dp[v].Z;
                }

                if (target.Normals is { } dn)
                {
                    f[o + 3] += w * dn[v].X;
                    f[o + 4] += w * dn[v].Y;
                    f[o + 5] += w * dn[v].Z;
                }

                if (target.Tangents is { } dt)
                {
                    f[o + 6] += w * dt[v].X;
                    f[o + 7] += w * dt[v].Y;
                    f[o + 8] += w * dt[v].Z;
                }
            }
        }

        for (var v = 0; v < p.Positions.Length; v++)
        {
            var o = (v * floatsPerVertex) + 3;
            var n = new Vector3(f[o], f[o + 1], f[o + 2]);
            if (n.LengthSquared() > 0f)
            {
                n = Vector3.Normalize(n);
                f[o] = n.X;
                f[o + 1] = n.Y;
                f[o + 2] = n.Z;
            }
        }
    }
}

/// <summary>Everything one frame draws: camera, models, lights, environment and post-processing.</summary>
public sealed class Scene3D
{
    /// <summary>The camera.</summary>
    public Camera3D Camera { get; set; } = new();

    /// <summary>Models to draw.</summary>
    public List<ModelInstance3D> Models { get; } = [];

    /// <summary>Punctual lights. At most <see cref="Renderer3D.MaxLights"/> are used.</summary>
    public List<Light3D> Lights { get; } = [];

    /// <summary>Image-based lighting and backdrop.</summary>
    public Environment3D Environment { get; } = new();

    /// <summary>Bloom, exposure and tone mapping.</summary>
    public PostSettings Post { get; } = new();

    /// <summary>Copies of models drawn in bulk (see <see cref="AddInstanced"/>).</summary>
    public List<InstanceBatch3D> Batches { get; } = [];

    /// <summary>Multisampling: 1 (off), 4 or 8 (a request for 2 uses 4), limited by the device and by <see cref="Quality"/>. 0 (the default) takes the level's own count.</summary>
    public int Samples { get; set; }

    /// <summary>The quality level: bounds shadow map size, cascades, ambient occlusion, multisampling, bloom and environment size. Change it any time.</summary>
    public QualityLevel Quality { get; set; } = QualityLevel.High;

    /// <summary>Shadow controls; a light casts shadows when its <see cref="Light3D.CastShadows"/> is set.</summary>
    public ShadowSettings Shadows { get; } = new();

    /// <summary>Screen-space ambient occlusion.</summary>
    public SsaoSettings Ssao { get; } = new();

    /// <summary>Adds a copy of a model and returns it.</summary>
    public ModelInstance3D Add(Model3D model, Matrix4x4? transform = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        var instance = new ModelInstance3D(model) { Transform = transform ?? Matrix4x4.Identity };
        Models.Add(instance);
        return instance;
    }

    /// <summary>Adds a batch that draws many copies of a static model with per-copy transforms and colours.</summary>
    /// <param name="model">The model.</param>
    /// <param name="capacity">Copies to reserve room for; the batch grows as needed.</param>
    public InstanceBatch3D AddInstanced(Model3D model, int capacity = 64)
    {
        ArgumentNullException.ThrowIfNull(model);
        var batch = new InstanceBatch3D(model, capacity);
        Batches.Add(batch);
        return batch;
    }

    /// <summary>Adds the lights the model's glTF file defines (KHR_lights_punctual) at their current positions, through the instance transform.</summary>
    public void AddModelLights(ModelInstance3D instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        var nodes = instance.Model.Source.Nodes;
        for (var i = 0; i < nodes.Length; i++)
        {
            if (nodes[i].Light >= 0)
            {
                Lights.Add(Light3D.FromGltf(instance.Model.Source.Lights[nodes[i].Light], instance.NodeWorlds[i] * instance.Transform));
            }
        }
    }

    /// <summary>The camera a node of the model carries, placed through the instance's transform.</summary>
    public static Camera3D? CameraOf(ModelInstance3D instance, int cameraIndex = 0)
    {
        ArgumentNullException.ThrowIfNull(instance);
        var nodes = instance.Model.Source.Nodes;
        var seen = 0;
        for (var i = 0; i < nodes.Length; i++)
        {
            if (nodes[i].Camera >= 0)
            {
                if (seen++ == cameraIndex)
                {
                    return Camera3D.FromGltf(instance.Model.Source.Cameras[nodes[i].Camera], instance.NodeWorlds[i] * instance.Transform);
                }
            }
        }

        return null;
    }
}
