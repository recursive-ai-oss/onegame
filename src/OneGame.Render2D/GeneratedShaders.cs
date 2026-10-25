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
        (ShaderNames.Quad, ShaderNames.Vertex) => Code(Quad.VSMain.Stage, Quad.VSMain.EntryPoint, Quad.VSMain.SpirV, Quad.VSMain.Msl, Quad.VSMain.MslFunctionName, Quad.Bindings.All),
        (ShaderNames.Quad, ShaderNames.Sprite) => Code(Quad.PSSprite.Stage, Quad.PSSprite.EntryPoint, Quad.PSSprite.SpirV, Quad.PSSprite.Msl, Quad.PSSprite.MslFunctionName, Quad.Bindings.All),
        (ShaderNames.Quad, ShaderNames.Blur) => Code(Quad.PSBlur.Stage, Quad.PSBlur.EntryPoint, Quad.PSBlur.SpirV, Quad.PSBlur.Msl, Quad.PSBlur.MslFunctionName, Quad.Bindings.All),
        (ShaderNames.Quad, ShaderNames.Grade) => Code(Quad.PSGrade.Stage, Quad.PSGrade.EntryPoint, Quad.PSGrade.SpirV, Quad.PSGrade.Msl, Quad.PSGrade.MslFunctionName, Quad.Bindings.All),
        (ShaderNames.Tile, ShaderNames.Vertex) => Code(Tile.VSMain.Stage, Tile.VSMain.EntryPoint, Tile.VSMain.SpirV, Tile.VSMain.Msl, Tile.VSMain.MslFunctionName, Tile.Bindings.All),
        (ShaderNames.Tile, ShaderNames.TileIds) => Code(Tile.PSIds.Stage, Tile.PSIds.EntryPoint, Tile.PSIds.SpirV, Tile.PSIds.Msl, Tile.PSIds.MslFunctionName, Tile.Bindings.All),
        (ShaderNames.Tile, ShaderNames.TileFlat) => Code(Tile.PSFlat.Stage, Tile.PSFlat.EntryPoint, Tile.PSFlat.SpirV, Tile.PSFlat.Msl, Tile.PSFlat.MslFunctionName, Tile.Bindings.All),
        (ShaderNames.Ui, ShaderNames.Vertex) => Code(Ui.VSMain.Stage, Ui.VSMain.EntryPoint, Ui.VSMain.SpirV, Ui.VSMain.Msl, Ui.VSMain.MslFunctionName, Ui.Bindings.All),
        (ShaderNames.Ui, ShaderNames.UiPixel) => Code(Ui.PSMain.Stage, Ui.PSMain.EntryPoint, Ui.PSMain.SpirV, Ui.PSMain.Msl, Ui.PSMain.MslFunctionName, Ui.Bindings.All),
        _ => throw new ArgumentException($"The renderer has no shader {program}.{entryPoint}.", nameof(entryPoint)),
    };

    private static ShaderCode Code(OneShader.ShaderStage stage, string entry, ReadOnlySpan<byte> spirv, ReadOnlySpan<byte> msl, string mslName, ReadOnlySpan<OneShader.ResourceBinding> bindings) =>
        new(Map(stage), entry, spirv.ToArray(), msl.ToArray(), mslName, bindings.ToArray());

    private static OneGame.Gpu.ShaderStage Map(OneShader.ShaderStage stage) => stage switch
    {
        OneShader.ShaderStage.Vertex => OneGame.Gpu.ShaderStage.Vertex,
        OneShader.ShaderStage.Pixel => OneGame.Gpu.ShaderStage.Pixel,
        _ => OneGame.Gpu.ShaderStage.Compute,
    };
}
