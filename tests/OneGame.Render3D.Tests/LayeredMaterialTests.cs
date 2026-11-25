using RecursiveAI.OneGpu;

namespace OneGame.Render3D.Tests;

/// <summary>Layered materials: a texture array whose layer each vertex picks, drawn nearest and repeating inside the layer.</summary>
public class LayeredMaterialTests
{
    private const int Cell = 16;               // pixels per texel on screen
    private const int Quads = 4;
    private const int Width = Quads * 2 * Cell;
    private const int Height = 2 * Cell;

    // Layer l is a 2x2 image of four distinct sRGB colours.
    private static byte[] LayerImage(int layer)
    {
        var px = new byte[16];
        for (var t = 0; t < 4; t++)
        {
            px[(t * 4) + 0] = (byte)(40 + (layer * 50) + (t * 7));
            px[(t * 4) + 1] = (byte)(220 - (layer * 40) + (t * 5));
            px[(t * 4) + 2] = (byte)(30 + (t * 60));
            px[(t * 4) + 3] = 255;
        }

        return px;
    }

    private static float Linear(byte v)
    {
        var c = v / 255f;
        return c <= 0.04045f ? c / 12.92f : MathF.Pow((c + 0.055f) / 1.055f, 2.4f);
    }

    private static (MeshVertex3D[] Vertices, uint[] Indices) Row()
    {
        var v = new List<MeshVertex3D>();
        var i = new List<uint>();
        for (var q = 0; q < Quads; q++)
        {
            var x = (q * 2) - 3f;
            var b = (uint)v.Count;

            // Coordinates run 0..2 so the 2x2 image shows twice across and down; v = 0 is the top edge.
            v.Add(new MeshVertex3D(new Vector3(x - 1, 1, 0), Vector3.UnitZ, new Vector2(0, 0), Vector4.One, q));
            v.Add(new MeshVertex3D(new Vector3(x + 1, 1, 0), Vector3.UnitZ, new Vector2(2, 0), Vector4.One, q));
            v.Add(new MeshVertex3D(new Vector3(x + 1, -1, 0), Vector3.UnitZ, new Vector2(2, 2), Vector4.One, q));
            v.Add(new MeshVertex3D(new Vector3(x - 1, -1, 0), Vector3.UnitZ, new Vector2(0, 2), Vector4.One, q));
            i.AddRange([b, b + 3, b + 2, b, b + 2, b + 1]);
        }

        return (v.ToArray(), i.ToArray());
    }

    private static float[] Render(Rig rig, Material3D material, out Scene3D scene, MeshVertex3D[]? vertices = null, uint[]? indices = null)
    {
        var (v, i) = Row();
        var mesh = rig.Own(rig.Renderer.CreateMesh(material, vertices ?? v, indices ?? i, "row"));
        scene = new Scene3D { Camera = Plane.TopDown(2), Samples = 1 };
        scene.Environment.Intensity = 0f;
        scene.Environment.Background = BackgroundMode.Color;
        scene.Environment.BackgroundColor = Vector3.Zero;
        scene.Post.Bloom = false;
        scene.Post.ToneMapper = ToneMapper.None;
        scene.Add(mesh.Model);
        return Plane.ReadAfterRender(rig, scene);
    }

    private static Vector3 At(float[] px, int x, int y)
    {
        var i = ((y * Width) + x) * 4;
        return new Vector3(px[i], px[i + 1], px[i + 2]);
    }

    private static Material3D Make(Rig rig, bool mipmaps = true)
    {
        var layers = new ReadOnlyMemory<byte>[Quads];
        for (var l = 0; l < Quads; l++)
        {
            layers[l] = LayerImage(l);
        }

        var m = rig.Renderer.CreateLayeredMaterial("blocks", 2, 2, layers, mipmaps: mipmaps);
        m.Style = MaterialStyle3D.Flat;
        return m;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EachVertexPicksItsLayerNearestAndRepeatingInsideIt(bool mipmaps)
    {
        using var rig = new Rig(Width, Height, GpuFormat.Rgba16Float);
        var material = Make(rig, mipmaps);
        Assert.Equal(Quads, material.LayerCount);
        var px = Render(rig, material, out _);
        for (var q = 0; q < Quads; q++)
        {
            var image = LayerImage(q);
            for (var cy = 0; cy < 4; cy++)
            {
                for (var cx = 0; cx < 4; cx++)
                {
                    var t = ((cy % 2) * 2) + (cx % 2);
                    var expected = new Vector3(Linear(image[(t * 4) + 0]), Linear(image[(t * 4) + 1]), Linear(image[(t * 4) + 2]));
                    var got = At(px, (q * 2 * Cell) + (cx * (Cell / 2)) + (Cell / 4), (cy * (Cell / 2)) + (Cell / 4));
                    Assert.True((expected - got).Length() < 0.01f, $"quad {q} cell {cx},{cy}: expected {expected}, got {got}");
                }
            }
        }
    }

    [Fact]
    public void EdgesBetweenTexelsAndBetweenLayersAreHardAndNeverBlend()
    {
        using var rig = new Rig(Width, Height, GpuFormat.Rgba16Float);
        var px = Render(rig, Make(rig), out _);
        var allowed = new List<Vector3>();
        for (var l = 0; l < Quads; l++)
        {
            var image = LayerImage(l);
            for (var t = 0; t < 4; t++)
            {
                allowed.Add(new Vector3(Linear(image[(t * 4) + 0]), Linear(image[(t * 4) + 1]), Linear(image[(t * 4) + 2])));
            }
        }

        // Every pixel of the frame is exactly one texel colour of some layer: no filtering across texels, repeats or quads.
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                var c = At(px, x, y);
                Assert.True(allowed.Exists(a => (a - c).Length() < 0.01f), $"pixel {x},{y} = {c} is not a texel colour");
            }
        }
    }

    [Fact]
    public void OneMaterialIsOneDrawWhateverTheLayerCount()
    {
        using var rig = new Rig(Width, Height, GpuFormat.Rgba16Float);
        Render(rig, Make(rig), out _);
        Assert.Equal(1, rig.Renderer.LastStats.DrawCalls);
    }

    [Fact]
    public void BadInputIsRefusedWithATypedException()
    {
        using var rig = new Rig(Width, Height, GpuFormat.Rgba16Float);
        Assert.Throws<ArgumentException>(() => rig.Renderer.CreateLayeredMaterial("none", 2, 2, []));
        Assert.Throws<ArgumentException>(() => rig.Renderer.CreateLayeredMaterial("size", 0, 2, new ReadOnlyMemory<byte>[] { new byte[16] }));
        Assert.Throws<ArgumentException>(() => rig.Renderer.CreateLayeredMaterial("bytes", 2, 2, new ReadOnlyMemory<byte>[] { new byte[15] }));
    }

    [Fact]
    public void LayersWorkWithInstancingAndShadowsAndFog()
    {
        using var rig = new Rig(Width, Height, GpuFormat.Rgba16Float);
        var material = Make(rig);
        var (v, i) = Row();
        var mesh = rig.Own(rig.Renderer.CreateMesh(material, v, i, "row"));
        var scene = new Scene3D { Camera = Plane.TopDown(2), Samples = 1 };
        scene.Environment.Intensity = 0f;
        scene.Environment.Background = BackgroundMode.Color;
        scene.Post.Bloom = false;
        scene.Post.ToneMapper = ToneMapper.None;
        var light = Light3D.Directional(new Vector3(0, 0, -1), Vector3.One, 3f);
        light.CastShadows = true;
        scene.Lights.Add(light);
        scene.Fog.Enabled = true;
        scene.Fog.Color = new Vector3(0.5f);
        scene.Fog.Start = 100;
        scene.Fog.End = 200;
        scene.Add(mesh.Model);
        scene.AddInstanced(mesh.Model).Add(Matrix4x4.CreateTranslation(0, 10, 0));
        var px = Plane.ReadAfterRender(rig, scene);
        Assert.True(At(px, 8, 8).Length() > 0.05f);
    }
}
