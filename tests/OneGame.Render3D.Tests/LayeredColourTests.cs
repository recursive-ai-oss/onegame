using RecursiveAI.OneGpu;

namespace OneGame.Render3D.Tests;

/// <summary>The same image drawn through an ordinary material and as a layer of a layered one must give the same pixels, lit, fogged and tinted.</summary>
public class LayeredColourTests
{
    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public void LayerPixelsEqualOrdinaryMaterialPixels(bool mipmaps, bool tinted)
    {
        const int w = 64, h = 32;
        using var rig = new Rig(w, h, GpuFormat.Rgba16Float);
        // A flat grass-like colour: the ordinary material filters linearly and the layered one nearest, so only a uniform image compares them.
        byte[] image = [90, 160, 60, 255, 90, 160, 60, 255, 90, 160, 60, 255, 90, 160, 60, 255];
        var tex = rig.Own(rig.Device.CreateTexture(new TextureDesc(2, 2, GpuFormat.Rgba8UnormSrgb, TextureUsage.Sampled | TextureUsage.CopyDestination) { Label = "plain" }));
        rig.Device.WriteTexture(tex, 0, 0, image);
        var plain = rig.Renderer.CreateMaterial("plain");
        plain.SetTexture(MaterialSlot3D.BaseColor, tex);
        var layered = rig.Renderer.CreateLayeredMaterial("layered", 2, 2, new ReadOnlyMemory<byte>[] { new byte[16], image }, mipmaps: mipmaps);
        var colour = tinted ? new Vector4(0.8f, 0.9f, 0.7f, 1f) : Vector4.One;

        MeshVertex3D[] Quad(float x, int layer) =>
        [
            new(new Vector3(x, 1, 0), Vector3.UnitZ, new Vector2(0, 0), colour, layer),
            new(new Vector3(x + 2, 1, 0), Vector3.UnitZ, new Vector2(1, 0), colour, layer),
            new(new Vector3(x + 2, -1, 0), Vector3.UnitZ, new Vector2(1, 1), colour, layer),
            new(new Vector3(x, -1, 0), Vector3.UnitZ, new Vector2(0, 1), colour, layer),
        ];
        uint[] idx = [0, 3, 2, 0, 2, 1];
        var scene = new Scene3D { Camera = Plane.TopDown(2), Samples = 1 };
        scene.Environment.Intensity = 0.3f;
        scene.Environment.Background = BackgroundMode.Color;
        scene.Environment.BackgroundColor = Vector3.Zero;
        scene.Post.Bloom = false;
        scene.Post.ToneMapper = ToneMapper.None;
        scene.Lights.Add(Light3D.Directional(new Vector3(0.3f, -0.4f, -1f), Vector3.One, 3f));
        scene.Fog.Enabled = true;
        scene.Fog.Color = new Vector3(0.5f, 0.6f, 0.8f);
        scene.Fog.Start = 5;
        scene.Fog.End = 30;
        scene.Add(rig.Own(rig.Renderer.CreateMesh(plain, Quad(-2, 0), idx, "a")).Model);
        scene.Add(rig.Own(rig.Renderer.CreateMesh(layered, Quad(0, 1), idx, "b")).Model);
        var px = Plane.ReadAfterRender(rig, scene);
        Vector3 At(int x, int y) { var i = ((y * w) + x) * 4; return new Vector3(px[i], px[i + 1], px[i + 2]); }
        var a = At(16, 16);
        var b = At(48, 16);
        TestContext.Current.SendDiagnosticMessage($"mips={mipmaps} tinted={tinted}: ordinary {a}, layered {b}");
        Assert.True(a.Length() > 0.05f);
        // A lit, rough non-metal grass colour has real diffuse light: the old fully metallic default read about a third as bright.
        Assert.True(a.Y > 0.15f, $"too dark: {a}");
        Assert.True((a - b).Length() < 0.004f, $"ordinary {a}, layered {b}");
    }
}
