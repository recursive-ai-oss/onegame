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
        SetBounds(source.Bounds.Min, source.Bounds.Max);
    }

    // A primitive of a dynamic mesh: its data lives in the buffers only, and a later update may replace them.
    internal Primitive3D(GpuBuffer vertices, GpuBuffer indices, IndexFormat format, uint indexCount, Material3D material, Vector3 min, Vector3 max)
    {
        Source = Empty;
        Vertices = vertices;
        Indices = indices;
        IndexFormat = format;
        IndexCount = indexCount;
        Material = material;
        SetBounds(min, max);
    }

    private static readonly GltfPrimitive Empty = new();

    /// <summary>The imported primitive (an empty one for a <see cref="DynamicMesh3D"/>).</summary>
    public GltfPrimitive Source { get; }

    internal GpuBuffer Vertices { get; set; }

    internal GpuBuffer Indices { get; set; }

    internal IndexFormat IndexFormat { get; set; }

    internal uint IndexCount { get; set; }

    internal Vector3 BoundsMin { get; private set; }

    internal Vector3 BoundsMax { get; private set; }

    internal void SetBounds(Vector3 min, Vector3 max)
    {
        BoundsMin = min;
        BoundsMax = max;
        Center = (min + max) * 0.5f;
        Radius = Vector3.Distance(min, max) * 0.5f;
    }

    internal Material3D Material { get; }

    /// <summary>Centre of the bounding sphere, in mesh space.</summary>
    internal Vector3 Center { get; private set; }

    internal float Radius { get; private set; }

    /// <summary>The packed rest-pose vertices, kept only for primitives with morph targets.</summary>
    internal byte[]? BaseVertices { get; set; }
}

/// <summary>The texture slots of a <see cref="Material3D"/> that <see cref="Material3D.SetTexture"/> can replace.</summary>
public enum MaterialSlot3D
{
    /// <summary>Base colour (sRGB colour; multiplied by the base colour factor).</summary>
    BaseColor = 0,

    /// <summary>Metallic (blue) and roughness (green).</summary>
    MetallicRoughness = 1,

    /// <summary>Tangent-space normal map.</summary>
    Normal = 2,

    /// <summary>Occlusion (red).</summary>
    Occlusion = 3,

    /// <summary>Emissive colour (multiplied by <see cref="Material3D.Emissive"/>).</summary>
    Emissive = 4,
}

/// <summary>
/// A material on the GPU: constants and textures bound as one group. Its <see cref="Style"/> picks physically based, cel-shaded or flat
/// lighting, and its outline settings add a silhouette line; changes apply to the next frame. Emissive light, opacity and textures can be
/// changed at any time without reloading the model.
/// </summary>
public sealed class Material3D
{
    private readonly GpuDevice _device;
    private readonly Renderer3D _renderer;
    private readonly Model3D _owner;
    private readonly BindGroupEntry[] _entries;
    private readonly bool _blendAtLoad;
    private GpuBindGroup _group;
    private MaterialConstants _constants;
    private MaterialStyle3D _style;
    private int _bands = 3;
    private float _softness = 0.04f;
    private float _highlight = 0.5f;
    private float _outlineWidth;
    private Vector3 _outlineColor = new(0.02f, 0.02f, 0.03f);
    private Vector3 _emissive;
    private float _emissiveStrength = 1f;
    private float _opacity = 1f;

    internal Material3D(Renderer3D renderer, Model3D owner, string name, in MaterialConstants constants, GpuBuffer buffer, GpuBindGroup group, BindGroupEntry[] entries, bool blend, bool doubleSided, bool mask)
    {
        _device = renderer.Device;
        _renderer = renderer;
        _owner = owner;
        _constants = constants;
        _entries = entries;
        _emissive = new Vector3(constants.Emissive.X, constants.Emissive.Y, constants.Emissive.Z);
        Name = name;
        Constants = buffer;
        _group = group;
        _blendAtLoad = blend;
        DoubleSided = doubleSided;
        Mask = mask;
    }

    /// <summary>The material's name in the glTF file (empty for the default material).</summary>
    public string Name { get; }

    internal GpuBuffer Constants { get; }

    internal GpuBindGroup Group => _group;

    /// <summary>Drawn in the blended pass (far to near, no depth write): a glTF BLEND material, or any material made translucent with <see cref="Opacity"/>.</summary>
    internal bool Blend => _blendAtLoad || _opacity < 1f;

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

    /// <summary>Emitted colour, linear RGB (the file's emissive factor times its strength at load). It is multiplied by the emissive texture and by <see cref="EmissiveStrength"/>.</summary>
    public Vector3 Emissive { get => _emissive; set => Set(ref _emissive, Vector3.Max(value, Vector3.Zero)); }

    /// <summary>A multiplier on <see cref="Emissive"/> (1 by default), for pulsing or fading a glow without touching its colour.</summary>
    public float EmissiveStrength { get => _emissiveStrength; set => Set(ref _emissiveStrength, Math.Max(value, 0f)); }

    /// <summary>
    /// 1 (default) draws the material as the file says; below 1 it multiplies the alpha and moves the material to the blended pass, which sorts
    /// far to near and writes no depth, as for a glTF BLEND material. Translucent materials cast no shadows and have no outline.
    /// </summary>
    public float Opacity { get => _opacity; set => Set(ref _opacity, Math.Clamp(value, 0f, 1f)); }

    /// <summary>
    /// Replaces one texture of this material, for example with the target of an <see cref="Offscreen3D"/> built with <c>sampled: true</c>, so a
    /// portal window shows another view. The texture keeps being used until <see cref="ResetTexture"/>; it must outlive the frames that draw it.
    /// Rendered targets are tone-mapped already: show one through <see cref="MaterialSlot3D.Emissive"/> (base colour black) or the
    /// <see cref="MaterialStyle3D.Flat"/> style on the base colour slot. Use an <c>Rgba8UnormSrgb</c> target so sampling returns linear light.
    /// </summary>
    /// <param name="slot">Which texture to replace.</param>
    /// <param name="texture">A texture with <see cref="TextureUsage.Sampled"/>.</param>
    public void SetTexture(MaterialSlot3D slot, GpuTexture texture)
    {
        ArgumentNullException.ThrowIfNull(texture);
        Rebind(slot, texture);
    }

    /// <summary>Puts back the texture the file gave (or the plain white one) in a slot.</summary>
    /// <param name="slot">Which texture to restore.</param>
    public void ResetTexture(MaterialSlot3D slot) => Rebind(slot, null);

    private void Rebind(MaterialSlot3D slot, GpuTexture? texture)
    {
        var binding = (uint)slot + 1;
        var index = Array.FindIndex(_entries, e => e.Binding == binding);
        _originalTextures ??= new GpuTexture?[5];
        if (texture is null)
        {
            if (_originalTextures[(int)slot] is not { } original)
            {
                return;
            }

            _entries[index] = BindGroupEntry.ForTexture(binding, original);
            _originalTextures[(int)slot] = null;
        }
        else
        {
            _originalTextures[(int)slot] ??= _entries[index].Texture;
            _entries[index] = BindGroupEntry.ForTexture(binding, texture);
        }

        var made = _renderer.CreateMaterialGroup(_entries);
        _renderer.Retire(_group);
        _owner.Disown(_group);
        _owner.Own(made);
        _group = made;
    }

    private GpuTexture?[]? _originalTextures;

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
        c.Emissive = new Vector4(_emissive * _emissiveStrength, 1f - _opacity);
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

    internal void InvalidateBounds() => _restBounds = null;

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
                        var b = (Min: p.BoundsMin, Max: p.BoundsMax);
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
                    n += (int)p.IndexCount / 3;
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

    internal void Disown(IDisposable resource) => _owned.Remove(resource);

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
