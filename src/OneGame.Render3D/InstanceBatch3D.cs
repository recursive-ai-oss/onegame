using System.Runtime.CompilerServices;

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
/// </summary>
public sealed class InstanceBatch3D
{
    private InstanceData3D[] _items;
    private bool _boundsDirty = true;
    private Vector3 _boundsMin;
    private Vector3 _boundsMax;

    internal InstanceBatch3D(Model3D model, int capacity)
    {
        Model = model;
        _items = new InstanceData3D[Math.Max(capacity, 4)];
        var bounds = model.RestBounds;
        LocalCenter = (bounds.Min + bounds.Max) * 0.5f;
        LocalRadius = Vector3.Distance(bounds.Min, bounds.Max) * 0.5f;
        RestWorlds = model.RestWorlds;
    }

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
        if (Count == _items.Length)
        {
            Array.Resize(ref _items, _items.Length * 2);
        }

        _items[Count] = new InstanceData3D { Transform = transform, Color = color ?? Vector4.One };
        _boundsDirty = true;
        return Count++;
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
        radius = LocalRadius * MathF.Sqrt(MathF.Max(sx, MathF.Max(sy, sz)));
    }
}
