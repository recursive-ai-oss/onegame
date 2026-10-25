using OneGame.Gpu;
using OneGame.Render2D.Shaders;

namespace OneGame.Render2D.Tests;

/// <summary>The hand-written pipeline layouts must agree with what the shader build found in the HLSL.</summary>
public class GeneratedShaderTests
{
    private static readonly ResourceBinding[] Plain = Layouts.Concat(Layouts.Frame, Layouts.Texture);
    private static readonly ResourceBinding[] WithParams = Layouts.Concat(Layouts.Frame, Layouts.Texture, Layouts.Params);
    private static readonly ResourceBinding[] WithPools = Layouts.Concat(Layouts.Frame, Layouts.Texture, Layouts.TilePools);

    private static void Agree(string program, ReadOnlySpan<OneShader.ResourceBinding> found, ResourceBinding[] table)
    {
        var all = found.ToArray();
        Assert.NotEmpty(all);
        foreach (var f in all)
        {
            var match = table.Where(t => t.Set == f.Set && t.Binding == f.Binding).ToArray();
            Assert.True(match.Length == 1, $"{program}: set {f.Set} binding {f.Binding} is in the HLSL but not in the layout table");
            Assert.Equal(f.Count, match[0].Count);
            Assert.Equal(f.Kind.ToString(), match[0].Kind.ToString());
        }
    }

    [Fact]
    public void QuadBindingsAgreeWithTheLayouts() => Agree("Quad", Quad.Bindings.All, WithParams);

    [Fact]
    public void TileBindingsAgreeWithTheLayouts() => Agree("Tile", Tile.Bindings.All, WithPools);

    [Fact]
    public void UiBindingsAgreeWithTheLayouts() => Agree("Ui", Ui.Bindings.All, Plain);

    // The layouts read packed data (UNorm8x4 colours) that the shader sees as float4, and the generated stride is that of the shader's own
    // float-typed input struct, so only the locations and whether each input is integer or float are comparable.
    private static void VertexAgrees(ReadOnlySpan<OneShader.VertexAttribute> inputs, VertexBufferLayout layout, string what)
    {
        var found = inputs.ToArray();
        var attributes = layout.Attributes.ToArray();
        Assert.Equal(attributes.Length, found.Length);
        foreach (var a in attributes)
        {
            var f = found.Single(x => x.Location == a.Location);
            var integerInLayout = a.Format is VertexFormat.UInt1 or VertexFormat.UInt2 or VertexFormat.UInt4;
            Assert.True(integerInLayout == f.Format.ToString().StartsWith("Uint", StringComparison.Ordinal), $"{what}: location {a.Location} is {f.Format} in the HLSL but {a.Format} in the layout");
        }
    }

    [Fact]
    public void QuadVertexInputsAgreeWithTheLayout() => VertexAgrees(Quad.VSMain.Inputs, Layouts.QuadVertex(), "Quad");

    [Fact]
    public void TileVertexInputsAgreeWithTheLayout() => VertexAgrees(Tile.VSMain.Inputs, Layouts.QuadVertex(true), "Tile");

    [Fact]
    public void UiVertexInputsAgreeWithTheLayout() => VertexAgrees(Ui.VSMain.Inputs, Layouts.UiVertex(), "Ui");
}
