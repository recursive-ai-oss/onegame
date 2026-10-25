using System.Buffers.Binary;

namespace OneGame.Gltf;

/// <summary>How a primitive's indices form shapes. Strips, fans and loops are expanded to lists on import.</summary>
public enum GltfTopology
{
    /// <summary>One point per index.</summary>
    Points,

    /// <summary>Two indices per line.</summary>
    Lines,

    /// <summary>Three indices per triangle, counter-clockwise front faces.</summary>
    Triangles,
}

/// <summary>Four joint indices of a skinned vertex.</summary>
/// <param name="A">First joint.</param>
/// <param name="B">Second joint.</param>
/// <param name="C">Third joint.</param>
/// <param name="D">Fourth joint.</param>
public readonly record struct JointIndices(ushort A, ushort B, ushort C, ushort D);

/// <summary>Per-vertex deltas of one morph target. Absent streams are null.</summary>
public sealed class MorphTarget
{
    /// <summary>Position deltas, one per vertex, or null.</summary>
    public Vector3[]? Positions { get; internal set; }

    /// <summary>Normal deltas, or null.</summary>
    public Vector3[]? Normals { get; internal set; }

    /// <summary>Tangent deltas (xyz only), or null.</summary>
    public Vector3[]? Tangents { get; internal set; }
}

/// <summary>The index width chosen by <see cref="GltfPrimitive.PackIndices"/>.</summary>
public enum IndexWidth
{
    /// <summary>Two bytes per index.</summary>
    U16,

    /// <summary>Four bytes per index.</summary>
    U32,
}

/// <summary>One drawable piece of a mesh: vertex streams, indices and a material.</summary>
public sealed class GltfPrimitive
{
    /// <summary>Bytes per vertex of <see cref="PackVertices"/>.</summary>
    public const int PackedVertexSize = 96;

    /// <summary>How <see cref="Indices"/> form shapes.</summary>
    public GltfTopology Topology { get; internal set; } = GltfTopology.Triangles;

    /// <summary>Index of the material, or -1 for the glTF default material.</summary>
    public int Material { get; internal set; } = -1;

    /// <summary>Vertex positions.</summary>
    public Vector3[] Positions { get; internal set; } = [];

    /// <summary>Vertex normals; always present (flat for generated ones, or (0,0,1) for points and lines without any).</summary>
    public Vector3[] Normals { get; internal set; } = [];

    /// <summary>True when the normals were generated flat, which also split shared vertices.</summary>
    public bool NormalsGenerated { get; internal set; }

    /// <summary>True when the file supplied normals or they were generated (false for points and lines without normals).</summary>
    public bool HasNormals { get; internal set; }

    /// <summary>Tangents (xyz and a sign in w), or null.</summary>
    public Vector4[]? Tangents { get; internal set; }

    /// <summary>True when the tangents were generated.</summary>
    public bool TangentsGenerated { get; internal set; }

    /// <summary>First texture coordinate set, or null.</summary>
    public Vector2[]? Uv0 { get; internal set; }

    /// <summary>Second texture coordinate set, or null.</summary>
    public Vector2[]? Uv1 { get; internal set; }

    /// <summary>Vertex colours (RGBA), or null.</summary>
    public Vector4[]? Colors { get; internal set; }

    /// <summary>Joint indices, or null when the primitive is not skinned.</summary>
    public JointIndices[]? Joints { get; internal set; }

    /// <summary>Joint weights, or null when the primitive is not skinned.</summary>
    public Vector4[]? Weights { get; internal set; }

    /// <summary>The shape indices into the vertex streams; always present.</summary>
    public uint[] Indices { get; internal set; } = [];

    /// <summary>Morph targets (deltas), all with one entry per vertex.</summary>
    public MorphTarget[] MorphTargets { get; internal set; } = [];

    /// <summary>The position bounds of the base mesh (morph targets not applied).</summary>
    public GltfBounds Bounds { get; internal set; }

    /// <summary>The number of vertices.</summary>
    public int VertexCount => Positions.Length;

    /// <summary>Packs the vertices into the fixed 96-byte little-endian layout documented in the interface description.</summary>
    /// <returns>VertexCount times <see cref="PackedVertexSize"/> bytes.</returns>
    public byte[] PackVertices()
    {
        var data = new byte[checked(Positions.Length * PackedVertexSize)];
        for (var i = 0; i < Positions.Length; i++)
        {
            var s = data.AsSpan(i * PackedVertexSize, PackedVertexSize);
            Put(s, 0, Positions[i]);
            Put(s, 12, Normals[i]);
            Put(s, 24, Tangents is null ? new Vector4(0, 0, 0, 1) : Tangents[i]);
            Put(s, 40, Uv0 is null ? Vector2.Zero : Uv0[i]);
            Put(s, 48, Uv1 is null ? Vector2.Zero : Uv1[i]);
            Put(s, 56, Colors is null ? Vector4.One : Colors[i]);
            if (Joints is not null)
            {
                var j = Joints[i];
                BinaryPrimitives.WriteUInt16LittleEndian(s[72..], j.A);
                BinaryPrimitives.WriteUInt16LittleEndian(s[74..], j.B);
                BinaryPrimitives.WriteUInt16LittleEndian(s[76..], j.C);
                BinaryPrimitives.WriteUInt16LittleEndian(s[78..], j.D);
            }

            Put(s, 80, Weights is null ? new Vector4(1, 0, 0, 0) : Weights[i]);
        }

        return data;
    }

    /// <summary>Packs the indices as 16-bit when every vertex index fits, else 32-bit, little-endian.</summary>
    /// <param name="width">The chosen width.</param>
    /// <returns>The index bytes.</returns>
    public byte[] PackIndices(out IndexWidth width)
    {
        width = Positions.Length <= ushort.MaxValue ? IndexWidth.U16 : IndexWidth.U32;
        var size = width == IndexWidth.U16 ? 2 : 4;
        var data = new byte[checked(Indices.Length * size)];
        for (var i = 0; i < Indices.Length; i++)
        {
            if (size == 2)
            {
                BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(i * 2), (ushort)Indices[i]);
            }
            else
            {
                BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(i * 4), Indices[i]);
            }
        }

        return data;
    }

    private static void Put(Span<byte> s, int at, Vector2 v)
    {
        BinaryPrimitives.WriteSingleLittleEndian(s[at..], v.X);
        BinaryPrimitives.WriteSingleLittleEndian(s[(at + 4)..], v.Y);
    }

    private static void Put(Span<byte> s, int at, Vector3 v)
    {
        Put(s, at, new Vector2(v.X, v.Y));
        BinaryPrimitives.WriteSingleLittleEndian(s[(at + 8)..], v.Z);
    }

    private static void Put(Span<byte> s, int at, Vector4 v)
    {
        Put(s, at, new Vector3(v.X, v.Y, v.Z));
        BinaryPrimitives.WriteSingleLittleEndian(s[(at + 12)..], v.W);
    }
}

/// <summary>A mesh: one or more primitives sharing a node and morph weights.</summary>
public sealed class GltfMesh
{
    /// <summary>The mesh's name, or empty.</summary>
    public string Name { get; internal set; } = "";

    /// <summary>The primitives.</summary>
    public GltfPrimitive[] Primitives { get; internal set; } = [];

    /// <summary>The number of morph targets (the same for every primitive).</summary>
    public int MorphTargetCount { get; internal set; }

    /// <summary>Default morph weights, one per target (zeros when the file gives none).</summary>
    public float[] DefaultWeights { get; internal set; } = [];

    /// <summary>The union of the primitives' bounds.</summary>
    public GltfBounds Bounds { get; internal set; }
}
