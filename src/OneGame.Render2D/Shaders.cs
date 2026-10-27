using OneGame.Gpu;
using OneGame.Render2D.Shaders;
using EntryLayout = OneShader.Layouts.EntryLayout;
using VertexAttributeOverride = OneShader.Layouts.VertexAttributeOverride;
using VertexLayoutOverrides = OneShader.Layouts.VertexLayoutOverrides;

namespace OneGame.Render2D;

/// <summary>One compiled shader stage: SPIR-V (and, for Metal, Metal Shading Language) and the name of its entry point.</summary>
/// <param name="Stage">The pipeline stage.</param>
/// <param name="EntryPoint">The entry point function name in the SPIR-V module.</param>
/// <param name="SpirV">The SPIR-V words as bytes. The renderer reads it while creating pipelines and does not keep it.</param>
/// <param name="Msl">UTF-8 Metal Shading Language source, needed on Metal. Empty for shaders that only run on Vulkan.</param>
/// <param name="MslFunctionName">The function to look up in <paramref name="Msl"/>; defaults to the entry point.</param>
/// <param name="Wgsl">UTF-8 WGSL source, needed on browser WebGPU. Empty for shaders compiled without it.</param>
/// <param name="WgslEntryPoint">The entry point name in the WGSL; defaults to the entry point.</param>
/// <param name="Layout">The entry point's layout (<c>Shader.Entry.Layout</c> of the generated class); needed on Metal.</param>
public readonly record struct ShaderCode(ShaderStage Stage, string EntryPoint, ReadOnlyMemory<byte> SpirV,
    ReadOnlyMemory<byte> Msl = default, string? MslFunctionName = null, OneShader.Layouts.EntryLayout? Layout = null,
    ReadOnlyMemory<byte> Wgsl = default, string? WgslEntryPoint = null)
{
    /// <summary>The form the graphics layer takes to create a shader on any backend.</summary>
    public ShaderSource ToSource() => new(Stage, EntryPoint)
    {
        SpirV = SpirV.Span,
        Msl = Msl.Span,
        MslFunctionName = MslFunctionName,
        Layout = Layout,
        Wgsl = Wgsl.Span,
        WgslEntryPoint = WgslEntryPoint,
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

    /// <summary>Quad pixel entry point: texture times tint, added to the target instead of blended over it.</summary>
    public const string Additive = "PSAdditive";

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

/// <summary>
/// The resource layout every 2D program shares, derived from the shaders. Set 0 and set 1 are identical in all programs so bind groups made
/// once are valid everywhere; set 0 is therefore visible to both stages even where only one shader reads it.
/// </summary>
internal static class Layouts
{
    private static readonly (uint Set, ShaderStage Stages)[] Everywhere = [(0, ShaderStage.Vertex | ShaderStage.Pixel)];

    // Sets 0 and 1 are the same in every layout, so every layout starts from the entry points that between them use all of them.
    private static readonly EntryLayout[] Shared = [Quad.VSMain.Layout, Quad.PSSprite.Layout, Ui.VSMain.Layout, Ui.PSMain.Layout];

    /// <summary>Sprites and UI: frame constants and clip table, a texture and its sampler.</summary>
    public static ResourceBinding[] Plain() => ShaderLayouts.Bindings(Shared, widenSets: Everywhere);

    /// <summary>Materials, blur and grade: <see cref="Plain"/> plus a parameter block in set 2.</summary>
    public static ResourceBinding[] WithParams() =>
        ShaderLayouts.Bindings([.. Shared, Quad.PSBlur.Layout, Quad.PSGrade.Layout], widenSets: Everywhere);

    /// <summary>Tile programs: <see cref="Plain"/> plus parameters, the id pool and the colour pool in set 2.</summary>
    public static ResourceBinding[] WithPools() =>
        ShaderLayouts.Bindings([.. Shared, Tile.VSMain.Layout, Tile.PSIds.Layout, Tile.PSFlat.Layout], widenSets: Everywhere);

    // The engine packs colours as unorm8x4 and its instances are smaller than the shader's float-typed input struct.

    /// <summary>The per-instance vertex layout of quads (48 bytes). Only the tile programs read the last word.</summary>
    public static VertexBufferLayout QuadVertex(bool withExtra = false) => ShaderLayouts.Vertex(
        withExtra ? Tile.VSMain.Layout : Quad.VSMain.Layout, VertexStepMode.Instance,
        new VertexLayoutOverrides
        {
            Stride = (uint)QuadInstance.Size,
            Attributes = withExtra
                ? new Dictionary<uint, VertexAttributeOverride> { [4] = new(OneShader.VertexFormat.Unorm8x4, 40), [5] = new(null, 44) }
                : new Dictionary<uint, VertexAttributeOverride> { [4] = new(OneShader.VertexFormat.Unorm8x4, 40) },
        });

    /// <summary>The per-instance vertex layout of UI primitives (128 bytes).</summary>
    public static VertexBufferLayout UiVertex() => ShaderLayouts.Vertex(Ui.VSMain.Layout, VertexStepMode.Instance, new VertexLayoutOverrides
    {
        Stride = (uint)UiInstance.Size,
        Attributes = new Dictionary<uint, VertexAttributeOverride>
        {
            [6] = new(OneShader.VertexFormat.Unorm8x4, 88),
            [7] = new(OneShader.VertexFormat.Unorm8x4, 92),
            [8] = new(null, 96),
            [9] = new(null, 112),
        },
    });
}
