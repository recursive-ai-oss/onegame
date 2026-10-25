using OneGame.Gltf;
using OneGame.Gpu;

namespace OneGame.Render3D;

/// <summary>A drawable triangle list of a mesh, with its GPU buffers and material.</summary>
public sealed class Primitive3D
{
    internal Primitive3D(GltfPrimitive source, GpuBuffer vertices, GpuBuffer indices, IndexFormat format, Material3D material)
    {
        Source = source;
        Vertices = vertices;
        Indices = indices;
        IndexFormat = format;
        IndexCount = (uint)source.Indices.Length;
        Material = material;
        Center = (source.Bounds.Min + source.Bounds.Max) * 0.5f;
        Radius = Vector3.Distance(source.Bounds.Min, source.Bounds.Max) * 0.5f;
    }

    /// <summary>The imported primitive.</summary>
    public GltfPrimitive Source { get; }

    internal GpuBuffer Vertices { get; }

    internal GpuBuffer Indices { get; }

    internal IndexFormat IndexFormat { get; }

    internal uint IndexCount { get; }

    internal Material3D Material { get; }

    /// <summary>Centre of the bounding sphere, in mesh space.</summary>
    internal Vector3 Center { get; }

    internal float Radius { get; }

    /// <summary>The packed rest-pose vertices, kept only for primitives with morph targets.</summary>
    internal byte[]? BaseVertices { get; set; }
}

/// <summary>
/// A material on the GPU: constants and textures bound as one group. Its <see cref="Style"/> picks physically based, cel-shaded or flat
/// lighting, and its outline settings add a silhouette line; changes apply to the next frame.
/// </summary>
public sealed class Material3D
{
    private readonly GpuDevice _device;
    private MaterialConstants _constants;
    private MaterialStyle3D _style;
    private int _bands = 3;
    private float _softness = 0.04f;
    private float _highlight = 0.5f;
    private float _outlineWidth;
    private Vector3 _outlineColor = new(0.02f, 0.02f, 0.03f);

    internal Material3D(GpuDevice device, string name, in MaterialConstants constants, GpuBuffer buffer, GpuBindGroup group, bool blend, bool doubleSided, bool mask)
    {
        _device = device;
        _constants = constants;
        Name = name;
        Constants = buffer;
        Group = group;
        Blend = blend;
        DoubleSided = doubleSided;
        Mask = mask;
    }

    /// <summary>The material's name in the glTF file (empty for the default material).</summary>
    public string Name { get; }

    internal GpuBuffer Constants { get; }

    internal GpuBindGroup Group { get; }

    internal bool Blend { get; }

    internal bool DoubleSided { get; }

    /// <summary>Alpha-tested: the shadow and depth passes need the base colour texture to cut holes.</summary>
    internal bool Mask { get; }

    /// <summary>The lighting model.</summary>
    public MaterialStyle3D Style { get => _style; set => Set(ref _style, value); }

    /// <summary>Toon: number of light steps (2 to 8).</summary>
    public int ToonBands { get => _bands; set => Set(ref _bands, Math.Clamp(value, 2, 8)); }

    /// <summary>Toon: width of the transition between two steps, as a fraction of one step (0 is a hard edge).</summary>
    public float ToonSoftness { get => _softness; set => Set(ref _softness, Math.Clamp(value, 0f, 0.5f)); }

    /// <summary>Toon: strength of the hard specular highlight (0 none).</summary>
    public float ToonHighlight { get => _highlight; set => Set(ref _highlight, Math.Max(value, 0f)); }

    /// <summary>Outline thickness in pixels of the render target (0 draws none). Works with every style; it needs smooth vertex normals.</summary>
    public float OutlineWidth { get => _outlineWidth; set => Set(ref _outlineWidth, Math.Clamp(value, 0f, 32f)); }

    /// <summary>Outline colour, linear RGB.</summary>
    public Vector3 OutlineColor { get => _outlineColor; set => Set(ref _outlineColor, value); }

    private void Set<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        var c = _constants;
        c.StyleA = new Vector4((float)(int)_style, _bands, _softness, _highlight);
        c.StyleB = new Vector4(_outlineWidth, 0, 0, 0);
        c.OutlineColor = new Vector4(_outlineColor, 1f);
        _device.WriteBuffer(Constants, 0, System.Runtime.InteropServices.MemoryMarshal.AsBytes(new ReadOnlySpan<MaterialConstants>(in c)));
    }
}

/// <summary>A glTF model uploaded to the GPU: meshes, materials and textures. Create it with <see cref="Renderer3D.Upload"/>.</summary>
public sealed class Model3D : IDisposable
{
    private readonly List<IDisposable> _owned = [];

    internal Model3D(GltfModel source, int scene)
    {
        Source = source;
        Scene = scene;
    }

    /// <summary>The imported model.</summary>
    public GltfModel Source { get; }

    /// <summary>The glTF scene being drawn.</summary>
    public int Scene { get; }

    /// <summary>The materials of the model, in glTF order (the last one is the default material primitives without a material use).</summary>
    public IReadOnlyList<Material3D> Materials { get; internal set; } = [];

    /// <summary>Sets the style of every material of the model.</summary>
    /// <param name="style">The lighting model.</param>
    public void SetStyle(MaterialStyle3D style)
    {
        foreach (var m in Materials)
        {
            m.Style = style;
        }
    }

    private Matrix4x4[]? _restWorlds;
    private (Vector3 Min, Vector3 Max)? _restBounds;

    /// <summary>World matrix of every node in the rest pose.</summary>
    internal Matrix4x4[] RestWorlds
    {
        get
        {
            if (_restWorlds is null)
            {
                var worlds = new Matrix4x4[Source.Nodes.Length];
                Source.CreateRestPose().ComputeWorld(Source, worlds);
                _restWorlds = worlds;
            }

            return _restWorlds;
        }
    }

    /// <summary>Box around every drawn primitive in the rest pose.</summary>
    internal (Vector3 Min, Vector3 Max) RestBounds
    {
        get
        {
            if (_restBounds is null)
            {
                var min = new Vector3(float.MaxValue);
                var max = new Vector3(float.MinValue);
                foreach (var n in DrawNodes)
                {
                    foreach (var p in Meshes[Source.Nodes[n].Mesh]!)
                    {
                        var b = p.Source.Bounds;
                        for (var c = 0; c < 8; c++)
                        {
                            var corner = new Vector3((c & 1) == 0 ? b.Min.X : b.Max.X, (c & 2) == 0 ? b.Min.Y : b.Max.Y, (c & 4) == 0 ? b.Min.Z : b.Max.Z);
                            var w = Vector3.Transform(corner, RestWorlds[n]);
                            min = Vector3.Min(min, w);
                            max = Vector3.Max(max, w);
                        }
                    }
                }

                _restBounds = min.X > max.X ? (Vector3.Zero, Vector3.Zero) : (min, max);
            }

            return _restBounds.Value;
        }
    }

    internal Primitive3D[]?[] Meshes { get; set; } = [];

    internal int[] DrawNodes { get; set; } = [];

    /// <summary>Number of triangles drawn per frame at rest.</summary>
    public int TriangleCount
    {
        get
        {
            var n = 0;
            foreach (var node in DrawNodes)
            {
                foreach (var p in Meshes[Source.Nodes[node].Mesh]!)
                {
                    n += p.Source.Indices.Length / 3;
                }
            }

            return n;
        }
    }

    internal T Own<T>(T resource)
        where T : IDisposable
    {
        _owned.Add(resource);
        return resource;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var o in _owned)
        {
            o.Dispose();
        }

        _owned.Clear();
    }
}
