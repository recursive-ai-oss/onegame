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

/// <summary>How a file is imported. Every property has a default, so <c>null</c> or <c>new GltfLoadOptions()</c> is the normal choice; set <see cref="Limits"/> when loading content from a server and <see cref="DecodeImages"/> to false when only the geometry matters.</summary>
public sealed class GltfLoadOptions
{
    /// <summary>The limits to enforce. Null means the defaults, which suit untrusted content: a file that exceeds one is refused with a <see cref="GltfException"/> before anything large is allocated.</summary>
    public GltfLimits? Limits { get; init; }

    /// <summary>Decode image pixels (PNG and JPEG, through the image library). When false only each image's size is read, which is much faster for a tool that lists or measures models.</summary>
    public bool DecodeImages { get; init; } = true;

    /// <summary>When to generate tangents.</summary>
    public TangentMode Tangents { get; init; } = TangentMode.WhenNormalMapped;

    /// <summary>Generate flat (per-face) normals for triangle primitives that have none, as the glTF specification requires. Turn off to keep the data as authored.</summary>
    public bool GenerateNormals { get; init; } = true;
}
