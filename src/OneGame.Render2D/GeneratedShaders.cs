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
        (ShaderNames.Quad, ShaderNames.Vertex) => new(Map(Quad.VSMain.Stage), Quad.VSMain.EntryPoint, Quad.VSMain.SpirV.ToArray()),
        (ShaderNames.Quad, ShaderNames.Sprite) => new(Map(Quad.PSSprite.Stage), Quad.PSSprite.EntryPoint, Quad.PSSprite.SpirV.ToArray()),
        (ShaderNames.Quad, ShaderNames.Blur) => new(Map(Quad.PSBlur.Stage), Quad.PSBlur.EntryPoint, Quad.PSBlur.SpirV.ToArray()),
        (ShaderNames.Quad, ShaderNames.Grade) => new(Map(Quad.PSGrade.Stage), Quad.PSGrade.EntryPoint, Quad.PSGrade.SpirV.ToArray()),
        (ShaderNames.Tile, ShaderNames.Vertex) => new(Map(Tile.VSMain.Stage), Tile.VSMain.EntryPoint, Tile.VSMain.SpirV.ToArray()),
        (ShaderNames.Tile, ShaderNames.TileIds) => new(Map(Tile.PSIds.Stage), Tile.PSIds.EntryPoint, Tile.PSIds.SpirV.ToArray()),
        (ShaderNames.Tile, ShaderNames.TileFlat) => new(Map(Tile.PSFlat.Stage), Tile.PSFlat.EntryPoint, Tile.PSFlat.SpirV.ToArray()),
        (ShaderNames.Ui, ShaderNames.Vertex) => new(Map(Ui.VSMain.Stage), Ui.VSMain.EntryPoint, Ui.VSMain.SpirV.ToArray()),
        (ShaderNames.Ui, ShaderNames.UiPixel) => new(Map(Ui.PSMain.Stage), Ui.PSMain.EntryPoint, Ui.PSMain.SpirV.ToArray()),
        _ => throw new ArgumentException($"The renderer has no shader {program}.{entryPoint}.", nameof(entryPoint)),
    };

    private static OneGame.Gpu.ShaderStage Map(OneShader.ShaderStage stage) => stage switch
    {
        OneShader.ShaderStage.Vertex => OneGame.Gpu.ShaderStage.Vertex,
        OneShader.ShaderStage.Pixel => OneGame.Gpu.ShaderStage.Pixel,
        _ => OneGame.Gpu.ShaderStage.Compute,
    };
}
