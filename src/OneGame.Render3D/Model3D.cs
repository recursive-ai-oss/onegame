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

/// <summary>A material on the GPU: constants and textures bound as one group.</summary>
internal sealed class Material3D
{
    public required GpuBuffer Constants { get; init; }

    public required GpuBindGroup Group { get; init; }

    public bool Blend { get; init; }

    public bool DoubleSided { get; init; }
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
