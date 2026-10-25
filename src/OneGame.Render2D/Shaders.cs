using OneGame.Gpu;

namespace OneGame.Render2D;

/// <summary>One compiled shader stage: SPIR-V (and, for Metal, Metal Shading Language) and the name of its entry point.</summary>
/// <param name="Stage">The pipeline stage.</param>
/// <param name="EntryPoint">The entry point function name in the SPIR-V module.</param>
/// <param name="SpirV">The SPIR-V words as bytes. The renderer reads it while creating pipelines and does not keep it.</param>
/// <param name="Msl">UTF-8 Metal Shading Language source, needed on Metal. Empty for shaders that only run on Vulkan.</param>
/// <param name="MslFunctionName">The function to look up in <paramref name="Msl"/>; defaults to the entry point.</param>
/// <param name="Bindings">The resources of the shader file, with their Metal indices; needed on Metal.</param>
public readonly record struct ShaderCode(ShaderStage Stage, string EntryPoint, ReadOnlyMemory<byte> SpirV,
    ReadOnlyMemory<byte> Msl = default, string? MslFunctionName = null, OneShader.ResourceBinding[]? Bindings = null)
{
    /// <summary>The form the graphics layer takes to create a shader on any backend.</summary>
    public ShaderSource ToSource() => new(Stage, EntryPoint)
    {
        SpirV = SpirV.Span,
        Msl = Msl.Span,
        MslFunctionName = MslFunctionName,
        Bindings = Bindings,
    };
}

/// <summary>
/// Supplies the renderer's built-in shaders by program and entry point name. A game passes the source generated from the shader build output;
/// a test or tool can pass its own.
/// </summary>
public interface IShaderSource
{
    /// <summary>Returns the compiled entry point <paramref name="entryPoint"/> of the program <paramref name="program"/>.</summary>
    /// <param name="program">One of <see cref="ShaderNames"/>: the HLSL file name without its extension.</param>
    /// <param name="entryPoint">One of the entry point names in <see cref="ShaderNames"/>.</param>
    /// <returns>The compiled stage.</returns>
    ShaderCode Load(string program, string entryPoint);
}

/// <summary>The programs and entry points the renderer asks an <see cref="IShaderSource"/> for.</summary>
public static class ShaderNames
{
    /// <summary>Program <c>Quad</c> (Quad.hlsl): sprites, composites and post passes.</summary>
    public const string Quad = "Quad";

    /// <summary>Program <c>Tile</c> (Tile.hlsl): tilemap chunks.</summary>
    public const string Tile = "Tile";

    /// <summary>Program <c>Ui</c> (Ui.hlsl): UI primitives.</summary>
    public const string Ui = "Ui";

    /// <summary>Vertex entry point shared by every program.</summary>
    public const string Vertex = "VSMain";

    /// <summary>Quad pixel entry point: texture times tint.</summary>
    public const string Sprite = "PSSprite";

    /// <summary>Quad pixel entry point: separable blur.</summary>
    public const string Blur = "PSBlur";

    /// <summary>Quad pixel entry point: colour grade.</summary>
    public const string Grade = "PSGrade";

    /// <summary>Tile pixel entry point: tile ids from the near-zoom pool.</summary>
    public const string TileIds = "PSIds";

    /// <summary>Tile pixel entry point: flat colours from the far-zoom pool.</summary>
    public const string TileFlat = "PSFlat";

    /// <summary>Ui pixel entry point.</summary>
    public const string UiPixel = "PSMain";
}

/// <summary>The fixed resource layout every 2D program shares. Set 0 and set 1 are identical in all programs so bind groups made once are valid everywhere.</summary>
internal static class Layouts
{
    public const ShaderStage Both = ShaderStage.Vertex | ShaderStage.Pixel;

    /// <summary>Set 0 binding 0: viewport size. Binding 1: the clip table.</summary>
    public static readonly ResourceBinding[] Frame =
    [
        new(0, 0, 1, ResourceKind.ConstantBuffer, Both),
        new(0, 1, 1, ResourceKind.ReadOnlyStorageBuffer, Both),
    ];

    /// <summary>Set 1: a texture and its sampler.</summary>
    public static readonly ResourceBinding[] Texture =
    [
        new(1, 0, 1, ResourceKind.SampledTexture, ShaderStage.Pixel),
        new(1, 1, 1, ResourceKind.Sampler, ShaderStage.Pixel),
    ];

    /// <summary>Set 2 of materials, blur and grade: parameters.</summary>
    public static readonly ResourceBinding[] Params =
    [
        new(2, 0, 1, ResourceKind.ConstantBuffer, ShaderStage.Pixel),
    ];

    /// <summary>Set 2 of tile programs: parameters, id pool, colour pool.</summary>
    public static readonly ResourceBinding[] TilePools =
    [
        new(2, 0, 1, ResourceKind.ConstantBuffer, ShaderStage.Pixel),
        new(2, 1, 1, ResourceKind.SampledTexture, ShaderStage.Pixel),
        new(2, 2, 1, ResourceKind.SampledTexture, ShaderStage.Pixel),
    ];

    public static ResourceBinding[] Concat(params ResourceBinding[][] parts)
    {
        var list = new List<ResourceBinding>();
        foreach (var p in parts)
        {
            list.AddRange(p);
        }

        return [.. list];
    }

    /// <summary>The per-instance vertex layout of quads (48 bytes). Only the tile programs read the last word.</summary>
    public static VertexBufferLayout QuadVertex(bool withExtra = false) => new()
    {
        Stride = (uint)QuadInstance.Size,
        StepMode = VertexStepMode.Instance,
        Attributes = withExtra
            ? new VertexElement[]
            {
                new(0, VertexFormat.Float2, 0),
                new(1, VertexFormat.Float2, 8),
                new(2, VertexFormat.Float2, 16),
                new(3, VertexFormat.Float4, 24),
                new(4, VertexFormat.UNorm8x4, 40),
                new(5, VertexFormat.UInt1, 44),
            }
            : new VertexElement[]
            {
                new(0, VertexFormat.Float2, 0),
                new(1, VertexFormat.Float2, 8),
                new(2, VertexFormat.Float2, 16),
                new(3, VertexFormat.Float4, 24),
                new(4, VertexFormat.UNorm8x4, 40),
            },
    };

    /// <summary>The per-instance vertex layout of UI primitives (128 bytes).</summary>
    public static VertexBufferLayout UiVertex() => new()
    {
        Stride = (uint)UiInstance.Size,
        StepMode = VertexStepMode.Instance,
        Attributes = new VertexElement[]
        {
            new(0, VertexFormat.Float4, 0),
            new(1, VertexFormat.Float4, 16),
            new(2, VertexFormat.Float2, 32),
            new(3, VertexFormat.Float4, 40),
            new(4, VertexFormat.Float4, 56),
            new(5, VertexFormat.Float4, 72),
            new(6, VertexFormat.UNorm8x4, 88),
            new(7, VertexFormat.UNorm8x4, 92),
            new(8, VertexFormat.Float4, 96),
            new(9, VertexFormat.UInt4, 112),
        },
    };
}
