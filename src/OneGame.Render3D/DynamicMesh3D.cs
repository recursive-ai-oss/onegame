using System.Numerics;
using System.Runtime.InteropServices;
using OneGame.Gltf;
using OneGame.Gpu;

namespace OneGame.Render3D;

/// <summary>
/// A triangle mesh a game builds and changes while running, such as a streamed voxel chunk. Create it with <see cref="Renderer3D.CreateMesh"/>,
/// draw it by adding <see cref="Model"/> to a <see cref="Scene3D"/> (or through <c>ModelLibrary.SetMesh</c>), change it with <see cref="Update"/>
/// and free it with <see cref="Dispose"/>.
/// </summary>
/// <remarks>
/// <para>
/// Buffers are sized in classes (at most 12.5% slack), so a mesh that changes size a little keeps its buffers and an update is one copy through
/// the device's reused staging memory. A bigger or much smaller mesh gets new buffers and the old ones are released once the GPU has finished with
/// the frames that used them. Nothing waits for the GPU. The mesh uses one shared <see cref="Material3D"/>; it is not skinned, morphed or pickable
/// at the triangle level, and it is not thread-safe: call it from the thread that renders.
/// </para>
/// </remarks>
public sealed class DynamicMesh3D : IDisposable
{
    private readonly Renderer3D _renderer;
    private readonly Primitive3D _primitive;
    private ulong _vertexCapacity;
    private ulong _indexCapacity;
    private bool _disposed;

    internal DynamicMesh3D(Renderer3D renderer, Model3D model, Primitive3D primitive, Material3D material, string name, ulong vertexCapacity, ulong indexCapacity)
    {
        _renderer = renderer;
        Model = model;
        _primitive = primitive;
        Material = material;
        Name = name;
        _vertexCapacity = vertexCapacity;
        _indexCapacity = indexCapacity;
    }

    /// <summary>The name given at creation (used to label GPU buffers).</summary>
    public string Name { get; }

    /// <summary>The material the mesh is drawn with. Many meshes share one.</summary>
    public Material3D Material { get; }

    /// <summary>The drawable model: add it to a <see cref="Scene3D"/> with <c>Scene3D.Add</c>. It stays the same object across updates.</summary>
    public Model3D Model { get; }

    /// <summary>Number of vertices of the current contents.</summary>
    public int VertexCount { get; private set; }

    /// <summary>Number of indices of the current contents.</summary>
    public int IndexCount => (int)_primitive.IndexCount;

    /// <summary>Number of triangles of the current contents.</summary>
    public int TriangleCount => IndexCount / 3;

    /// <summary>Corner of the box around the vertices, in mesh space.</summary>
    public Vector3 BoundsMin => _primitive.BoundsMin;

    /// <summary>Opposite corner of the box around the vertices.</summary>
    public Vector3 BoundsMax => _primitive.BoundsMax;

    /// <summary>Bytes of GPU buffer the mesh holds now (vertex plus index capacity).</summary>
    public long GpuBytes => (long)(_vertexCapacity + _indexCapacity);

    /// <summary>Checks geometry the way <see cref="Update"/> and <see cref="Renderer3D.CreateMesh"/> do, without touching the GPU.</summary>
    /// <param name="vertices">The vertices.</param>
    /// <param name="indices">Triangle list indices.</param>
    /// <exception cref="ArgumentException">There are no vertices or indices, the index count is not a multiple of 3, or an index is out of range.</exception>
    public static void Validate(ReadOnlySpan<MeshVertex3D> vertices, ReadOnlySpan<uint> indices)
    {
        if (vertices.IsEmpty || indices.IsEmpty)
        {
            throw new ArgumentException("A mesh needs at least one triangle. Free the mesh instead of updating it to nothing.", nameof(vertices));
        }

        if (indices.Length % 3 != 0)
        {
            throw new ArgumentException($"The index count {indices.Length} is not a multiple of 3.", nameof(indices));
        }

        var count = (uint)vertices.Length;
        foreach (var i in indices)
        {
            if (i >= count)
            {
                throw new ArgumentException($"An index ({i}) is not below the vertex count ({count}).", nameof(indices));
            }
        }
    }

    /// <summary>Replaces the geometry. The next frame shows the new contents.</summary>
    /// <param name="vertices">The vertices.</param>
    /// <param name="indices">Triangle list indices into <paramref name="vertices"/>; a multiple of 3, each below the vertex count.</param>
    /// <exception cref="ArgumentException">There are no vertices or indices, the index count is not a multiple of 3, or an index is out of range.</exception>
    /// <exception cref="ObjectDisposedException">The mesh was freed.</exception>
    public void Update(ReadOnlySpan<MeshVertex3D> vertices, ReadOnlySpan<uint> indices)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _renderer.WriteMesh(this, _primitive, vertices, indices, ref _vertexCapacity, ref _indexCapacity);
        VertexCount = vertices.Length;
        Model.InvalidateBounds();
    }

    /// <summary>Frees the GPU buffers once the GPU has finished with the frames that drew them. Remove the model from scenes first.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Model.Dispose();
    }
}

/// <summary>How a material from <see cref="Renderer3D.CreateMaterial"/> treats alpha.</summary>
public enum MaterialAlpha3D
{
    /// <summary>Alpha is ignored.</summary>
    Opaque = 0,

    /// <summary>Pixels with alpha below the cutoff are dropped (leaves, fences); still drawn and shadowed as solid.</summary>
    Mask = 1,

    /// <summary>Alpha blends with what is behind (water, glass); drawn far to near, no depth write.</summary>
    Blend = 2,
}

[StructLayout(LayoutKind.Sequential)]
internal struct PackedVertex
{
    public Vector3 Position;
    public Vector3 Normal;
    public Vector4 Tangent;
    public Vector2 Uv0;
    public Vector2 Uv1;
    public Vector4 Color;
    public ulong Joints;
    public Vector4 Weights;
}

public sealed partial class Renderer3D
{
    private PackedVertex[] _packVertices = [];
    private ushort[] _packIndices16 = [];

    // A one-triangle glTF whose single node and mesh give every dynamic mesh the node graph a ModelInstance3D needs.
    private static GltfModel? s_template;

    private static GltfModel Template => s_template ??= GltfLoader.Load(System.Text.Encoding.ASCII.GetBytes(
        "{\"asset\":{\"version\":\"2.0\"},\"scene\":0,\"scenes\":[{\"nodes\":[0]}],\"nodes\":[{\"mesh\":0}],\"meshes\":[{\"primitives\":[{\"attributes\":{\"POSITION\":0}}]}],"
        + "\"accessors\":[{\"bufferView\":0,\"componentType\":5126,\"count\":3,\"type\":\"VEC3\",\"min\":[0,0,0],\"max\":[1,1,0]}],\"bufferViews\":[{\"buffer\":0,\"byteLength\":36}],"
        + "\"buffers\":[{\"byteLength\":36,\"uri\":\"data:application/octet-stream;base64,AAAAAAAAAAAAAAAAAACAPwAAAAAAAAAAAACAPwAAgD8AAAAA\"}]}"));

    /// <summary>
    /// Makes a material for <see cref="DynamicMesh3D"/>s without a glTF file. It starts as white, rough, non-metal; set its textures, colours and
    /// style through <see cref="Material3D"/>. Materials live as long as the renderer; make a few and share them.
    /// </summary>
    /// <param name="name">A name for tools.</param>
    /// <param name="alpha">How alpha is treated.</param>
    /// <param name="doubleSided">Draw back faces too.</param>
    /// <param name="baseColor">Linear RGBA multiplied into the base colour texture; null for white.</param>
    /// <param name="alphaCutoff">Threshold for <see cref="MaterialAlpha3D.Mask"/>.</param>
    /// <returns>The material.</returns>
    public Material3D CreateMaterial(string name, MaterialAlpha3D alpha = MaterialAlpha3D.Opaque, bool doubleSided = false, Vector4? baseColor = null, float alphaCutoff = 0.5f)
    {
        ArgumentNullException.ThrowIfNull(name);
        var owner = Own(new Model3D(Template, 0));
        var constants = BuildMaterial(new GltfMaterial());
        constants.BaseColor = baseColor ?? Vector4.One;
        constants.P1.X = alphaCutoff;
        constants.P1.Y = (float)(int)alpha;
        var buffer = owner.Own(Device.CreateBuffer(new BufferDesc((ulong)Marshal.SizeOf<MaterialConstants>(), BufferUsage.Uniform | BufferUsage.CopyDestination) { Label = "material " + name }));
        Device.WriteBuffer(buffer, 0, MemoryMarshal.AsBytes(new ReadOnlySpan<MaterialConstants>(in constants)));
        BindGroupEntry[] entries =
        [
            BindGroupEntry.ForBuffer(0, buffer),
            BindGroupEntry.ForTexture(1, _white),
            BindGroupEntry.ForTexture(2, _white),
            BindGroupEntry.ForTexture(3, _white),
            BindGroupEntry.ForTexture(4, _white),
            BindGroupEntry.ForTexture(5, _white),
            BindGroupEntry.ForSampler(6, _defaultSampler),
            BindGroupEntry.ForSampler(7, _defaultSampler),
            BindGroupEntry.ForSampler(8, _defaultSampler),
            BindGroupEntry.ForSampler(9, _defaultSampler),
            BindGroupEntry.ForSampler(10, _defaultSampler),
        ];
        var group = owner.Own(CreateMaterialGroup(entries));
        var material = new Material3D(this, owner, name, constants, buffer, group, entries, alpha == MaterialAlpha3D.Blend, doubleSided, alpha == MaterialAlpha3D.Mask);
        owner.Materials = [material];
        return material;
    }

    /// <summary>Creates a mesh. See <see cref="DynamicMesh3D"/>.</summary>
    /// <param name="material">The shared material (from <see cref="CreateMaterial"/> or a model's <see cref="Model3D.Materials"/>).</param>
    /// <param name="vertices">The vertices.</param>
    /// <param name="indices">Triangle list indices; a multiple of 3, each below the vertex count.</param>
    /// <param name="name">A label for GPU debugging tools.</param>
    /// <returns>The mesh. Dispose it before the renderer.</returns>
    /// <exception cref="ArgumentException">The geometry is empty or invalid.</exception>
    public DynamicMesh3D CreateMesh(Material3D material, ReadOnlySpan<MeshVertex3D> vertices, ReadOnlySpan<uint> indices, string name = "mesh")
    {
        ArgumentNullException.ThrowIfNull(material);
        ArgumentNullException.ThrowIfNull(name);
        DynamicMesh3D.Validate(vertices, indices);
        var model = new Model3D(Template, 0);
        try
        {
            var vertexCapacity = SizeClass((ulong)vertices.Length * GltfPrimitive.PackedVertexSize);
            var indexCapacity = SizeClass((ulong)indices.Length * (vertices.Length <= ushort.MaxValue ? 2UL : 4UL));
            var vb = model.Own(Device.CreateBuffer(new BufferDesc(vertexCapacity, BufferUsage.Vertex | BufferUsage.CopyDestination) { Label = "vertices " + name }));
            var ib = model.Own(Device.CreateBuffer(new BufferDesc(indexCapacity, BufferUsage.Index | BufferUsage.CopyDestination) { Label = "indices " + name }));
            var primitive = new Primitive3D(vb, ib, IndexFormat.Index16, 0, material, default, default);
            model.Meshes = [[primitive]];
            model.Materials = [material];
            model.DrawNodes = [0];
            var mesh = new DynamicMesh3D(this, model, primitive, material, name, vertexCapacity, indexCapacity);
            mesh.Update(vertices, indices);
            return mesh;
        }
        catch
        {
            model.Dispose();
            throw;
        }
    }

    // Capacity classes: the next multiple of an eighth of the power of two below, so slack stays under 12.5% and freed buffers fit later requests.
    internal static ulong SizeClass(ulong bytes)
    {
        if (bytes <= 4096)
        {
            return 4096;
        }

        var step = (1UL << (63 - System.Numerics.BitOperations.LeadingZeroCount(bytes - 1))) / 8;
        return (bytes + step - 1) / step * step;
    }

    internal void WriteMesh(DynamicMesh3D mesh, Primitive3D primitive, ReadOnlySpan<MeshVertex3D> vertices, ReadOnlySpan<uint> indices, ref ulong vertexCapacity, ref ulong indexCapacity)
    {
        DynamicMesh3D.Validate(vertices, indices);
        if (_packVertices.Length < vertices.Length)
        {
            _packVertices = new PackedVertex[Math.Max(vertices.Length, _packVertices.Length * 2)];
        }

        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        for (var i = 0; i < vertices.Length; i++)
        {
            ref readonly var v = ref vertices[i];
            _packVertices[i] = new PackedVertex { Position = v.Position, Normal = v.Normal, Tangent = new Vector4(0, 0, 0, 1), Uv0 = v.Uv, Color = v.Color, Weights = new Vector4(1, 0, 0, 0) };
            min = Vector3.Min(min, v.Position);
            max = Vector3.Max(max, v.Position);
        }

        var model = mesh.Model;
        var vertexBytes = MemoryMarshal.AsBytes(new ReadOnlySpan<PackedVertex>(_packVertices, 0, vertices.Length));
        var wide = vertices.Length > ushort.MaxValue;
        ReadOnlySpan<byte> indexBytes;
        if (wide)
        {
            indexBytes = MemoryMarshal.AsBytes(indices);
        }
        else
        {
            if (_packIndices16.Length < indices.Length)
            {
                _packIndices16 = new ushort[Math.Max(indices.Length, _packIndices16.Length * 2)];
            }

            for (var i = 0; i < indices.Length; i++)
            {
                _packIndices16[i] = (ushort)indices[i];
            }

            indexBytes = MemoryMarshal.AsBytes(new ReadOnlySpan<ushort>(_packIndices16, 0, indices.Length));
        }

        // Keep the buffer when the data fits and is not much smaller than it; otherwise swap in one of the right class. The old one is
        // destroyed by the device once the submissions that may still read it have finished.
        if ((ulong)vertexBytes.Length > vertexCapacity || (ulong)vertexBytes.Length * 2 < vertexCapacity)
        {
            vertexCapacity = SizeClass((ulong)vertexBytes.Length);
            var old = primitive.Vertices;
            primitive.Vertices = model.Own(Device.CreateBuffer(new BufferDesc(vertexCapacity, BufferUsage.Vertex | BufferUsage.CopyDestination) { Label = "vertices " + mesh.Name }));
            model.Disown(old);
            old.Dispose();
        }

        if ((ulong)indexBytes.Length > indexCapacity || (ulong)indexBytes.Length * 2 < indexCapacity)
        {
            indexCapacity = SizeClass((ulong)indexBytes.Length);
            var old = primitive.Indices;
            primitive.Indices = model.Own(Device.CreateBuffer(new BufferDesc(indexCapacity, BufferUsage.Index | BufferUsage.CopyDestination) { Label = "indices " + mesh.Name }));
            model.Disown(old);
            old.Dispose();
        }

        Device.WriteBuffer(primitive.Vertices, 0, vertexBytes);
        Device.WriteBuffer(primitive.Indices, 0, indexBytes);
        primitive.IndexFormat = wide ? IndexFormat.Index32 : IndexFormat.Index16;
        primitive.IndexCount = (uint)indices.Length;
        primitive.SetBounds(min, max);
    }
}
