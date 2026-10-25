namespace OneGame.Gltf;

/// <summary>When tangents are generated.</summary>
public enum TangentMode
{
    /// <summary>For primitives whose material has a normal texture and that ship no tangents. The default.</summary>
    WhenNormalMapped,

    /// <summary>For every triangle primitive with UVs, replacing shipped tangents.</summary>
    Always,

    /// <summary>Never; only shipped tangents are kept.</summary>
    Never,
}

/// <summary>How a file is imported.</summary>
public sealed class GltfLoadOptions
{
    /// <summary>The limits to enforce. Null means the defaults.</summary>
    public GltfLimits? Limits { get; init; }

    /// <summary>Decode image pixels. When false only each image's size is read.</summary>
    public bool DecodeImages { get; init; } = true;

    /// <summary>When to generate tangents.</summary>
    public TangentMode Tangents { get; init; } = TangentMode.WhenNormalMapped;

    /// <summary>Generate flat normals for triangle primitives that have none.</summary>
    public bool GenerateNormals { get; init; } = true;
}
