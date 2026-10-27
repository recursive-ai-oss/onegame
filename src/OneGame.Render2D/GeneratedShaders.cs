using OneGame.Gpu;
using OneGame.Render2D.Shaders;

namespace OneGame.Render2D;

/// <summary>
/// The renderer's own shaders, compiled from <c>Shaders/*.hlsl</c> during the build. This is the default <see cref="IShaderSource"/>;
/// the SPIR-V is embedded in the assembly, so nothing is read from disk at run time.
/// </summary>
public sealed class GeneratedShaders : IShaderSource
{
    /// <summary>The shared instance.</summary>
    public static GeneratedShaders Instance { get; } = new();

    /// <inheritdoc />
    public ShaderCode Load(string program, string entryPoint) => (program, entryPoint) switch
    {
        (ShaderNames.Quad, ShaderNames.Vertex) => Code(Quad.VSMain.Stage, Quad.VSMain.EntryPoint, Quad.VSMain.SpirV, Quad.VSMain.Msl, Quad.VSMain.MslFunctionName, Quad.VSMain.Layout, Quad.VSMain.Wgsl, Quad.VSMain.WgslEntryPoint),
        (ShaderNames.Quad, ShaderNames.Sprite) => Code(Quad.PSSprite.Stage, Quad.PSSprite.EntryPoint, Quad.PSSprite.SpirV, Quad.PSSprite.Msl, Quad.PSSprite.MslFunctionName, Quad.PSSprite.Layout, Quad.PSSprite.Wgsl, Quad.PSSprite.WgslEntryPoint),
        (ShaderNames.Quad, ShaderNames.Blur) => Code(Quad.PSBlur.Stage, Quad.PSBlur.EntryPoint, Quad.PSBlur.SpirV, Quad.PSBlur.Msl, Quad.PSBlur.MslFunctionName, Quad.PSBlur.Layout, Quad.PSBlur.Wgsl, Quad.PSBlur.WgslEntryPoint),
        (ShaderNames.Quad, ShaderNames.Grade) => Code(Quad.PSGrade.Stage, Quad.PSGrade.EntryPoint, Quad.PSGrade.SpirV, Quad.PSGrade.Msl, Quad.PSGrade.MslFunctionName, Quad.PSGrade.Layout, Quad.PSGrade.Wgsl, Quad.PSGrade.WgslEntryPoint),
        (ShaderNames.Tile, ShaderNames.Vertex) => Code(Tile.VSMain.Stage, Tile.VSMain.EntryPoint, Tile.VSMain.SpirV, Tile.VSMain.Msl, Tile.VSMain.MslFunctionName, Tile.VSMain.Layout, Tile.VSMain.Wgsl, Tile.VSMain.WgslEntryPoint),
        (ShaderNames.Tile, ShaderNames.TileIds) => Code(Tile.PSIds.Stage, Tile.PSIds.EntryPoint, Tile.PSIds.SpirV, Tile.PSIds.Msl, Tile.PSIds.MslFunctionName, Tile.PSIds.Layout, Tile.PSIds.Wgsl, Tile.PSIds.WgslEntryPoint),
        (ShaderNames.Tile, ShaderNames.TileFlat) => Code(Tile.PSFlat.Stage, Tile.PSFlat.EntryPoint, Tile.PSFlat.SpirV, Tile.PSFlat.Msl, Tile.PSFlat.MslFunctionName, Tile.PSFlat.Layout, Tile.PSFlat.Wgsl, Tile.PSFlat.WgslEntryPoint),
        (ShaderNames.Ui, ShaderNames.Vertex) => Code(Ui.VSMain.Stage, Ui.VSMain.EntryPoint, Ui.VSMain.SpirV, Ui.VSMain.Msl, Ui.VSMain.MslFunctionName, Ui.VSMain.Layout, Ui.VSMain.Wgsl, Ui.VSMain.WgslEntryPoint),
        (ShaderNames.Ui, ShaderNames.UiPixel) => Code(Ui.PSMain.Stage, Ui.PSMain.EntryPoint, Ui.PSMain.SpirV, Ui.PSMain.Msl, Ui.PSMain.MslFunctionName, Ui.PSMain.Layout, Ui.PSMain.Wgsl, Ui.PSMain.WgslEntryPoint),
        _ => throw new ArgumentException($"The renderer has no shader {program}.{entryPoint}.", nameof(entryPoint)),
    };

    private static ShaderCode Code(OneShader.ShaderStage stage, string entry, ReadOnlySpan<byte> spirv, ReadOnlySpan<byte> msl, string mslName, OneShader.Layouts.EntryLayout layout, ReadOnlySpan<byte> wgsl, string wgslName) =>
        new(Map(stage), entry, spirv.ToArray(), msl.ToArray(), mslName, layout, wgsl.ToArray(), wgslName);

    private static OneGame.Gpu.ShaderStage Map(OneShader.ShaderStage stage) => stage switch
    {
        OneShader.ShaderStage.Vertex => OneGame.Gpu.ShaderStage.Vertex,
        OneShader.ShaderStage.Pixel => OneGame.Gpu.ShaderStage.Pixel,
        _ => OneGame.Gpu.ShaderStage.Compute,
    };
}
