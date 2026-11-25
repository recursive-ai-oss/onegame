using System.Runtime.CompilerServices;
using OneGame.Gltf;

namespace OneGame.Render3D;

/// <summary>One copy of a model in an <see cref="InstanceBatch3D"/>: where it stands and a colour that multiplies the material's base colour.</summary>
public struct InstanceData3D
{
    /// <summary>Placement in the world. Use rotation, uniform scale and translation; mirrored or non-uniformly scaled copies light wrongly.</summary>
    public Matrix4x4 Transform { get; set; }

    /// <summary>Linear RGBA multiplied into the base colour (white leaves it unchanged).</summary>
    public Vector4 Color { get; set; }
}

/// <summary>
/// Many copies of one static model (trees, crowds, rubble), drawn with one instanced draw per primitive. Every frame the renderer culls
/// the copies against the camera and each shadow map and uploads only the visible ones, so tens of thousands are cheap. The model's
/// skin, animation and morph targets are ignored: the batch draws it in its rest pose. Create it with <see cref="Scene3D.AddInstanced"/>.
/// A batch made with <see cref="Scene3D.AddSkinnedInstanced"/> instead gives every copy its own pose (see <see cref="Add(in Matrix4x4, NodePose, Vector4?)"/>).
/// </summary>
public sealed class InstanceBatch3D
{
    private InstanceData3D[] _items;
    private bool _boundsDirty = true;
    private Vector3 _boundsMin;
    private Vector3 _boundsMax;

    private Vector4[] _palette = [];
    private int[] _skinOffsets = [];
    private Vector4[]? _restPalette;
    private Matrix4x4[]? _worldScratch;
    private float _boundsMargin = 0.5f;

    internal InstanceBatch3D(Model3D model, int capacity, bool posed = false)
    {
        Model = model;
        _items = new InstanceData3D[Math.Max(capacity, 4)];
        var bounds = model.RestBounds;
        LocalCenter = (bounds.Min + bounds.Max) * 0.5f;
        LocalRadius = Vector3.Distance(bounds.Min, bounds.Max) * 0.5f;
        RestWorlds = model.RestWorlds;
        if (posed)
        {
            // One palette per copy: for each skinned node of the model, in draw order, one matrix per joint of its skin.
            var nodes = model.Source.Nodes;
            _skinOffsets = new int[model.DrawNodes.Length];
            var joints = 0;
            for (var k = 0; k < model.DrawNodes.Length; k++)
            {
                var skin = nodes[model.DrawNodes[k]].Skin;
                if (skin >= 0 && skin < model.Source.Skins.Length)
                {
                    _skinOffsets[k] = joints;
                    joints += model.Source.Skins[skin].Joints.Length;
                }
            }

            Posed = joints > 0;
            PaletteVectors = joints * 4;
            _palette = new Vector4[_items.Length * PaletteVectors];
        }
    }

    /// <summary>
    /// True when each copy carries its own pose: the batch came from <see cref="Scene3D.AddSkinnedInstanced"/> and the model has at least one
    /// skin. Copies of other batches, and copies added without a pose, are drawn at the model's rest pose.
    /// </summary>
    public bool Posed { get; }

    /// <summary>
    /// How much of the rest-pose bounding radius is added to a posed copy's culling sphere, so limbs that reach out in an animation are not
    /// cut off at the screen edge or lose their shadow. 0.5 by default; ignored by batches that are not <see cref="Posed"/>.
    /// </summary>
    public float BoundsMargin { get => _boundsMargin; set => _boundsMargin = float.IsFinite(value) ? Math.Max(value, 0f) : 0f; }

    internal int PaletteVectors { get; }

    internal int PaletteOffsetOf(int drawNodeIndex) => _skinOffsets[drawNodeIndex];

    internal void CopyPalette(int index, Span<Vector4> destination) => _palette.AsSpan(index * PaletteVectors, PaletteVectors).CopyTo(destination);

    /// <summary>The model every copy draws.</summary>
    public Model3D Model { get; }

    /// <summary>Whether the batch is drawn.</summary>
    public bool Visible { get; set; } = true;

    /// <summary>Whether the copies cast shadows (when lights do).</summary>
    public bool CastShadows { get; set; } = true;

    /// <summary>Number of copies.</summary>
    public int Count { get; private set; }

    /// <summary>The copies, <see cref="Count"/> of them. Edit in place, then call <see cref="Touch"/> if bounds changed.</summary>
    public Span<InstanceData3D> Items => _items.AsSpan(0, Count);

    internal Vector3 LocalCenter { get; }

    internal float LocalRadius { get; }

    internal Matrix4x4[] RestWorlds { get; }

    internal InstanceData3D[] Raw => _items;

    /// <summary>Adds a copy and returns its index.</summary>
    /// <param name="transform">Placement in the world.</param>
    /// <param name="color">Tint; null for white.</param>
    public int Add(in Matrix4x4 transform, Vector4? color = null)
    {
        Grow();
        _items[Count] = new InstanceData3D { Transform = transform, Color = color ?? Vector4.One };
        _boundsDirty = true;
        if (Posed)
        {
            RestPalette().CopyTo(_palette.AsSpan(Count * PaletteVectors, PaletteVectors));
        }

        return Count++;
    }

    /// <summary>
    /// Adds a copy in its own pose. The pose is read now, so the caller may change it or reuse the object straight away; each call costs one
    /// pass over the model's nodes and skins, so a crowd animated by a few shared clips can evaluate each clip once per frame and add many copies.
    /// </summary>
    /// <param name="transform">Placement in the world.</param>
    /// <param name="pose">Node pose of this copy, for example from an <c>Animator</c> or <see cref="AnimationClip.Apply"/>. It must belong to this batch's model.</param>
    /// <param name="color">Tint; null for white.</param>
    /// <returns>The copy's index.</returns>
    /// <exception cref="InvalidOperationException">The batch is not <see cref="Posed"/>.</exception>
    public int Add(in Matrix4x4 transform, NodePose pose, Vector4? color = null)
    {
        ArgumentNullException.ThrowIfNull(pose);
        if (!Posed)
        {
            throw new InvalidOperationException("Only a batch made with Scene3D.AddSkinnedInstanced for a skinned model takes poses.");
        }

        var index = Add(transform, color);
        SetPose(index, pose);
        return index;
    }

    /// <summary>Replaces one copy and its pose.</summary>
    /// <param name="index">Copy index.</param>
    /// <param name="transform">Placement in the world.</param>
    /// <param name="pose">Node pose of this copy.</param>
    /// <param name="color">Tint; null for white.</param>
    /// <exception cref="InvalidOperationException">The batch is not <see cref="Posed"/>.</exception>
    public void Set(int index, in Matrix4x4 transform, NodePose pose, Vector4? color = null)
    {
        Set(index, transform, color);
        SetPose(index, pose);
    }

    /// <summary>Gives one copy a new pose and leaves its transform and tint alone.</summary>
    /// <param name="index">Copy index.</param>
    /// <param name="pose">Node pose of this copy.</param>
    /// <exception cref="InvalidOperationException">The batch is not <see cref="Posed"/>.</exception>
    public void SetPose(int index, NodePose pose)
    {
        ArgumentNullException.ThrowIfNull(pose);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)Count);
        if (!Posed)
        {
            throw new InvalidOperationException("Only a batch made with Scene3D.AddSkinnedInstanced for a skinned model takes poses.");
        }

        var source = Model.Source;
        _worldScratch ??= new Matrix4x4[source.Nodes.Length];
        pose.ComputeWorld(source, _worldScratch);
        FillPalette(_worldScratch, _palette.AsSpan(index * PaletteVectors, PaletteVectors));
    }

    private void Grow()
    {
        if (Count == _items.Length)
        {
            Array.Resize(ref _items, _items.Length * 2);
            if (Posed)
            {
                Array.Resize(ref _palette, _items.Length * PaletteVectors);
            }
        }
    }

    private Vector4[] RestPalette()
    {
        if (_restPalette is null)
        {
            _restPalette = new Vector4[PaletteVectors];
            FillPalette(RestWorlds, _restPalette);
        }

        return _restPalette;
    }

    // Joint world * inverse bind for every skinned node's joints (glTF 2.0 section 3.7.3.3 without the mesh node's inverse, which the
    // shader's identity object matrix makes unnecessary), stored as four row vectors per matrix like the regular joint upload.
    private void FillPalette(ReadOnlySpan<Matrix4x4> world, Span<Vector4> destination)
    {
        var source = Model.Source;
        var nodes = source.Nodes;
        var o = 0;
        for (var k = 0; k < Model.DrawNodes.Length; k++)
        {
            var skinIndex = nodes[Model.DrawNodes[k]].Skin;
            if (skinIndex < 0 || skinIndex >= source.Skins.Length)
            {
                continue;
            }

            var skin = source.Skins[skinIndex];
            for (var j = 0; j < skin.Joints.Length; j++)
            {
                var m = skin.InverseBindMatrices[j] * world[skin.Joints[j]];
                destination[o++] = new Vector4(m.M11, m.M12, m.M13, m.M14);
                destination[o++] = new Vector4(m.M21, m.M22, m.M23, m.M24);
                destination[o++] = new Vector4(m.M31, m.M32, m.M33, m.M34);
                destination[o++] = new Vector4(m.M41, m.M42, m.M43, m.M44);
            }
        }
    }

    /// <summary>Replaces one copy.</summary>
    /// <param name="index">Copy index.</param>
    /// <param name="transform">Placement in the world.</param>
    /// <param name="color">Tint; null for white.</param>
    public void Set(int index, in Matrix4x4 transform, Vector4? color = null)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)Count);
        _items[index] = new InstanceData3D { Transform = transform, Color = color ?? Vector4.One };
        _boundsDirty = true;
    }

    /// <summary>Removes a copy by moving the last one into its place (order is not kept).</summary>
    /// <param name="index">Copy index.</param>
    public void RemoveAt(int index)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)Count);
        _items[index] = _items[--Count];
        if (Posed && index != Count)
        {
            _palette.AsSpan(Count * PaletteVectors, PaletteVectors).CopyTo(_palette.AsSpan(index * PaletteVectors, PaletteVectors));
        }

        _boundsDirty = true;
    }

    /// <summary>Removes every copy.</summary>
    public void Clear()
    {
        Count = 0;
        _boundsDirty = true;
    }

    /// <summary>Tells the batch its <see cref="Items"/> were edited in place, so it recomputes its bounds.</summary>
    public void Touch() => _boundsDirty = true;

    /// <summary>World-space box around every copy.</summary>
    internal (Vector3 Min, Vector3 Max) WorldBounds()
    {
        if (_boundsDirty)
        {
            _boundsDirty = false;
            _boundsMin = new Vector3(float.MaxValue);
            _boundsMax = new Vector3(float.MinValue);
            for (var i = 0; i < Count; i++)
            {
                var m = _items[i].Transform;
                SphereOf(in m, out var c, out var r);
                _boundsMin = Vector3.Min(_boundsMin, c - new Vector3(r));
                _boundsMax = Vector3.Max(_boundsMax, c + new Vector3(r));
            }
        }

        return (_boundsMin, _boundsMax);
    }

    /// <summary>The bounding sphere of one copy.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SphereOf(in Matrix4x4 m, out Vector3 center, out float radius)
    {
        center = Vector3.Transform(LocalCenter, m);
        var sx = (m.M11 * m.M11) + (m.M12 * m.M12) + (m.M13 * m.M13);
        var sy = (m.M21 * m.M21) + (m.M22 * m.M22) + (m.M23 * m.M23);
        var sz = (m.M31 * m.M31) + (m.M32 * m.M32) + (m.M33 * m.M33);
        radius = LocalRadius * (Posed ? 1f + _boundsMargin : 1f) * MathF.Sqrt(MathF.Max(sx, MathF.Max(sy, sz)));
    }
}
