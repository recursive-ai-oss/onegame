namespace OneGame.Gltf;

/// <summary>Upper bounds the importer enforces before it allocates. The defaults suit content from a server; raise them for trusted tools.</summary>
public sealed class GltfLimits
{
    /// <summary>The largest file (or GLB) read, in bytes (default 256 MiB).</summary>
    public long MaxFileBytes { get; init; } = 256L << 20;

    /// <summary>The largest JSON text, in bytes.</summary>
    public int MaxJsonBytes { get; init; } = 32 << 20;

    /// <summary>The deepest JSON nesting the reader accepts.</summary>
    public int MaxJsonDepth { get; init; } = 32;

    /// <summary>The largest declared size of one buffer.</summary>
    public long MaxBufferBytes { get; init; } = 256L << 20;

    /// <summary>The largest total of all buffers.</summary>
    public long MaxTotalBufferBytes { get; init; } = 512L << 20;

    /// <summary>The most buffers.</summary>
    public int MaxBuffers { get; init; } = 256;

    /// <summary>The most buffer views.</summary>
    public int MaxBufferViews { get; init; } = 100_000;

    /// <summary>The most accessors.</summary>
    public int MaxAccessors { get; init; } = 100_000;

    /// <summary>The most nodes in the file.</summary>
    public int MaxNodes { get; init; } = 100_000;

    /// <summary>The deepest node tree.</summary>
    public int MaxNodeDepth { get; init; } = 256;

    /// <summary>The most meshes.</summary>
    public int MaxMeshes { get; init; } = 100_000;

    /// <summary>The most primitives in all meshes together.</summary>
    public int MaxPrimitives { get; init; } = 100_000;

    /// <summary>The most materials.</summary>
    public int MaxMaterials { get; init; } = 100_000;

    /// <summary>The most textures.</summary>
    public int MaxTextures { get; init; } = 10_000;

    /// <summary>The most images.</summary>
    public int MaxImages { get; init; } = 1024;

    /// <summary>The most animations.</summary>
    public int MaxAnimations { get; init; } = 10_000;

    /// <summary>The most skins.</summary>
    public int MaxSkins { get; init; } = 10_000;

    /// <summary>The most scenes, samplers, cameras and lights (each).</summary>
    public int MaxSmallLists { get; init; } = 10_000;

    /// <summary>The most elements one accessor may declare.</summary>
    public int MaxElementsPerAccessor { get; init; } = 16 << 20;

    /// <summary>The most vertices in one primitive, after expansion.</summary>
    public int MaxVerticesPerPrimitive { get; init; } = 16 << 20;

    /// <summary>The most indices in one primitive, after expansion.</summary>
    public int MaxIndicesPerPrimitive { get; init; } = 64 << 20;

    /// <summary>The most morph targets in one mesh.</summary>
    public int MaxMorphTargets { get; init; } = 64;

    /// <summary>The most joints in one skin.</summary>
    public int MaxJointsPerSkin { get; init; } = 1024;

    /// <summary>The most keyframes in one animation sampler.</summary>
    public int MaxKeyframes { get; init; } = 4 << 20;

    /// <summary>The longest side of a decoded image, in pixels.</summary>
    public int MaxImageDimension { get; init; } = 16384;

    /// <summary>The most pixels in one image.</summary>
    public long MaxImagePixels { get; init; } = 64L << 20;

    /// <summary>The most bytes the importer allocates for the model (arrays and pixels), charged before each allocation.</summary>
    public long MaxOutputBytes { get; init; } = 1L << 30;
}
