namespace OneGame.Gltf;

/// <summary>A scene: the root nodes drawn together.</summary>
public sealed class GltfScene
{
    /// <summary>The scene's name, or empty.</summary>
    public string Name { get; internal set; } = "";

    /// <summary>Indices of the root nodes.</summary>
    public int[] Roots { get; internal set; } = [];
}

/// <summary>A node in the transform tree.</summary>
public sealed class GltfNode
{
    /// <summary>The node's name, or empty.</summary>
    public string Name { get; internal set; } = "";

    /// <summary>Index of the parent node, or -1 for a root.</summary>
    public int Parent { get; internal set; } = -1;

    /// <summary>Indices of the children.</summary>
    public int[] Children { get; internal set; } = [];

    /// <summary>Local translation (the rest pose).</summary>
    public Vector3 Translation { get; internal set; }

    /// <summary>Local rotation (the rest pose).</summary>
    public Quaternion Rotation { get; internal set; } = Quaternion.Identity;

    /// <summary>Local scale (the rest pose).</summary>
    public Vector3 Scale { get; internal set; } = Vector3.One;

    /// <summary>The exact local transform, in <c>System.Numerics</c> row-vector convention.</summary>
    public Matrix4x4 Local { get; internal set; } = Matrix4x4.Identity;

    /// <summary>True when the file gave a <c>matrix</c>; such a node cannot be animated.</summary>
    public bool FromMatrix { get; internal set; }

    /// <summary>The rest-pose transform from the node to the model's root space.</summary>
    public Matrix4x4 World { get; internal set; } = Matrix4x4.Identity;

    /// <summary>Distance from a root (a root is 0).</summary>
    public int Depth { get; internal set; }

    /// <summary>Index of the mesh drawn at this node, or -1.</summary>
    public int Mesh { get; internal set; } = -1;

    /// <summary>Index of the skin applied to the mesh, or -1.</summary>
    public int Skin { get; internal set; } = -1;

    /// <summary>Index of the camera at this node, or -1.</summary>
    public int Camera { get; internal set; } = -1;

    /// <summary>Index of the punctual light at this node, or -1.</summary>
    public int Light { get; internal set; } = -1;

    /// <summary>Default morph weights that override the mesh's, or null.</summary>
    public float[]? Weights { get; internal set; }
}

/// <summary>An axis-aligned box.</summary>
/// <param name="Min">The smallest corner.</param>
/// <param name="Max">The largest corner.</param>
public readonly record struct GltfBounds(Vector3 Min, Vector3 Max);

/// <summary>A skin: the joints that deform a mesh.</summary>
public sealed class GltfSkin
{
    /// <summary>The skin's name, or empty.</summary>
    public string Name { get; internal set; } = "";

    /// <summary>Node indices of the joints, in the order the vertex <c>JOINTS_0</c> values refer to.</summary>
    public int[] Joints { get; internal set; } = [];

    /// <summary>One inverse bind matrix per joint (identity when the file has none).</summary>
    public Matrix4x4[] InverseBindMatrices { get; internal set; } = [];

    /// <summary>Index of the skeleton root node, or -1.</summary>
    public int Skeleton { get; internal set; } = -1;

    /// <summary>Computes the matrices the vertex shader multiplies a vertex by: <c>inverse(meshNodeWorld)</c>, the joint's world, then its inverse bind matrix (glTF §Skins).</summary>
    /// <param name="world">World matrices of all nodes, for example from <see cref="NodePose.ComputeWorld"/>.</param>
    /// <param name="meshNode">The node that carries the mesh and the skin.</param>
    /// <param name="destination">Receives one matrix per joint; at least <see cref="Joints"/> long.</param>
    public void ComputeJointMatrices(ReadOnlySpan<Matrix4x4> world, int meshNode, Span<Matrix4x4> destination)
    {
        if (destination.Length < Joints.Length)
        {
            throw new ArgumentException("The destination is shorter than the joint list.", nameof(destination));
        }

        if ((uint)meshNode >= (uint)world.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(meshNode));
        }

        // glTF writes jointMatrix = inverse(meshWorld) * jointWorld * inverseBind with column vectors; row vectors reverse the order.
        var hasInverse = Matrix4x4.Invert(world[meshNode], out var inverse);
        if (!hasInverse)
        {
            inverse = Matrix4x4.Identity;
        }

        for (var i = 0; i < Joints.Length; i++)
        {
            destination[i] = InverseBindMatrices[i] * world[Joints[i]] * inverse;
        }
    }
}

/// <summary>A perspective or orthographic camera.</summary>
public sealed class GltfCamera
{
    /// <summary>The camera's name, or empty.</summary>
    public string Name { get; internal set; } = "";

    /// <summary>True for a perspective camera.</summary>
    public bool Perspective { get; internal set; }

    /// <summary>Vertical field of view in radians (perspective).</summary>
    public float YFov { get; internal set; }

    /// <summary>Width over height, or null to use the viewport's (perspective).</summary>
    public float? AspectRatio { get; internal set; }

    /// <summary>Half the view width (orthographic).</summary>
    public float XMag { get; internal set; }

    /// <summary>Half the view height (orthographic).</summary>
    public float YMag { get; internal set; }

    /// <summary>Near plane distance.</summary>
    public float ZNear { get; internal set; }

    /// <summary>Far plane distance, or null for an infinite projection (perspective only).</summary>
    public float? ZFar { get; internal set; }
}

/// <summary>The kind of a punctual light.</summary>
public enum GltfLightType
{
    /// <summary>Parallel rays along the node's -Z axis.</summary>
    Directional,

    /// <summary>Emits in all directions from the node.</summary>
    Point,

    /// <summary>A cone along the node's -Z axis.</summary>
    Spot,
}

/// <summary>A KHR_lights_punctual light.</summary>
public sealed class GltfLight
{
    /// <summary>The light's name, or empty.</summary>
    public string Name { get; internal set; } = "";

    /// <summary>The kind of light.</summary>
    public GltfLightType Type { get; internal set; }

    /// <summary>Linear RGB colour.</summary>
    public Vector3 Color { get; internal set; } = Vector3.One;

    /// <summary>Lux for directional lights, candela for point and spot lights.</summary>
    public float Intensity { get; internal set; } = 1;

    /// <summary>Distance beyond which the light has no effect, or null for unlimited.</summary>
    public float? Range { get; internal set; }

    /// <summary>Spot inner cone angle in radians.</summary>
    public float InnerConeAngle { get; internal set; }

    /// <summary>Spot outer cone angle in radians.</summary>
    public float OuterConeAngle { get; internal set; } = MathF.PI / 4;
}

/// <summary>An imported glTF file.</summary>
public sealed class GltfModel
{
    /// <summary>The <c>asset.generator</c> text, or empty.</summary>
    public string Generator { get; internal set; } = "";

    /// <summary>The <c>asset.copyright</c> text, or empty.</summary>
    public string Copyright { get; internal set; } = "";

    /// <summary>The scenes.</summary>
    public GltfScene[] Scenes { get; internal set; } = [];

    /// <summary>Index of the default scene (0 when the file names none and has scenes), or -1 when there are no scenes.</summary>
    public int DefaultScene { get; internal set; } = -1;

    /// <summary>The nodes.</summary>
    public GltfNode[] Nodes { get; internal set; } = [];

    /// <summary>The meshes.</summary>
    public GltfMesh[] Meshes { get; internal set; } = [];

    /// <summary>The materials.</summary>
    public GltfMaterial[] Materials { get; internal set; } = [];

    /// <summary>The textures.</summary>
    public GltfTexture[] Textures { get; internal set; } = [];

    /// <summary>The images.</summary>
    public GltfImage[] Images { get; internal set; } = [];

    /// <summary>The samplers.</summary>
    public GltfSampler[] Samplers { get; internal set; } = [];

    /// <summary>The skins.</summary>
    public GltfSkin[] Skins { get; internal set; } = [];

    /// <summary>The animation clips.</summary>
    public AnimationClip[] Animations { get; internal set; } = [];

    /// <summary>The cameras.</summary>
    public GltfCamera[] Cameras { get; internal set; } = [];

    /// <summary>The punctual lights.</summary>
    public GltfLight[] Lights { get; internal set; } = [];

    /// <summary><c>extensionsUsed</c> as written.</summary>
    public string[] ExtensionsUsed { get; internal set; } = [];

    /// <summary><c>extensionsRequired</c> as written. All of them are supported, or the load failed.</summary>
    public string[] ExtensionsRequired { get; internal set; } = [];

    /// <summary>Things that loaded but were degraded or ignored.</summary>
    public GltfWarning[] Warnings { get; internal set; } = [];

    /// <summary>Node indices with every parent before its children.</summary>
    internal int[] ParentFirst { get; set; } = [];

    /// <summary>True when the file was a binary GLB.</summary>
    public bool IsBinary { get; internal set; }

    /// <summary>Bytes the importer allocated for the model, as counted against <see cref="GltfLimits.MaxOutputBytes"/>.</summary>
    public long OutputBytes { get; internal set; }

    /// <summary>The axis-aligned box of every mesh in a scene, in the model's root space at the rest pose.</summary>
    /// <param name="scene">Scene index, or -1 for the default scene (all meshes when the file has no scenes).</param>
    /// <returns>The box, or null when the scene draws nothing.</returns>
    public GltfBounds? ComputeWorldBounds(int scene = -1)
    {
        if (scene < 0)
        {
            scene = DefaultScene;
        }

        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(scene, Scenes.Length);

        // Walk down from the scene's roots without recursion; with no scene, every node counts.
        var inScene = new bool[Nodes.Length];
        if (scene < 0)
        {
            Array.Fill(inScene, true);
        }
        else
        {
            foreach (var i in ParentFirst)
            {
                inScene[i] = Nodes[i].Parent >= 0 ? inScene[Nodes[i].Parent] : Array.IndexOf(Scenes[scene].Roots, i) >= 0;
            }
        }

        var min = new Vector3(float.PositiveInfinity);
        var max = new Vector3(float.NegativeInfinity);
        for (var i = 0; i < Nodes.Length; i++)
        {
            if (!inScene[i] || Nodes[i].Mesh < 0)
            {
                continue;
            }

            var b = Meshes[Nodes[i].Mesh].Bounds;
            for (var corner = 0; corner < 8; corner++)
            {
                var c = new Vector3((corner & 1) == 0 ? b.Min.X : b.Max.X, (corner & 2) == 0 ? b.Min.Y : b.Max.Y, (corner & 4) == 0 ? b.Min.Z : b.Max.Z);
                var w = Vector3.Transform(c, Nodes[i].World);
                min = Vector3.Min(min, w);
                max = Vector3.Max(max, w);
            }
        }

        return float.IsFinite(min.X) && float.IsFinite(max.X) ? new GltfBounds(min, max) : null;
    }

    /// <summary>Creates the rest pose: every node's own translation, rotation and scale, and the default morph weights.</summary>
    /// <returns>A pose to animate.</returns>
    public NodePose CreateRestPose()
    {
        var pose = new NodePose(Nodes.Length);
        for (var i = 0; i < Nodes.Length; i++)
        {
            var n = Nodes[i];
            pose.Translation[i] = n.Translation;
            pose.Rotation[i] = n.Rotation;
            pose.Scale[i] = n.Scale;
            pose.Weights[i] = n.Weights ?? (n.Mesh >= 0 ? (float[])Meshes[n.Mesh].DefaultWeights.Clone() : []);
        }

        return pose;
    }
}
