using OneGame.Gltf;
using OneGame.Gpu;

namespace OneGame.Render3D.Tests;

/// <summary>Tone mapping, bloom, alpha modes, culling and mirrored transforms on generated planes.</summary>
public class PostTests
{
    private static float[] Neutral(float[] c)
    {
        // The equations of the Khronos PBR Neutral specification.
        const double f90 = 0.04, ks = 0.8 - f90, kd = 0.15;
        var x = Math.Min(c[0], Math.Min(c[1], c[2]));
        var f = x <= 2 * f90 ? x - (x * x / (4 * f90)) : f90;
        var p = Math.Max(c[0] - f, Math.Max(c[1] - f, c[2] - f));
        if (p <= ks)
        {
            return [(float)(c[0] - f), (float)(c[1] - f), (float)(c[2] - f)];
        }

        var pn = 1 - ((1 - ks) * (1 - ks) / (p + 1 - (2 * ks)));
        var g = 1 / ((kd * (p - pn)) + 1);
        return [.. c.Select(v => (float)((((v - f) * pn / p) * g) + (pn * (1 - g))))];
    }

    private static float[] Aces(float[] c) => [.. c.Select(v =>
    {
        var x = v * 0.6;
        return (float)Math.Clamp(x * ((2.51 * x) + 0.03) / ((x * ((2.43 * x) + 0.59)) + 0.14), 0, 1);
    })];

    private static readonly float[] Colour = [0.2f, 0.4f, 0.6f];

    private static float Srgb(float v) => v <= 0.0031308f ? v * 12.92f : (1.055f * MathF.Pow(v, 1 / 2.4f)) - 0.055f;

    [Theory]
    [InlineData(ToneMapper.PbrNeutral)]
    [InlineData(ToneMapper.Aces)]
    [InlineData(ToneMapper.None)]
    public void ToneMappersMatchTheirDefinitions(ToneMapper mapper)
    {
        var (rig, scene) = Plane.Setup(Plane.Unlit(0.9f, 0.5f, 0.1f));
        using (rig)
        {
            scene.Post.ToneMapper = mapper;
            scene.Post.Exposure = 3f;   // scene value (2.7, 1.5, 0.3)
            rig.Target.Render(scene);
            var px = Plane.ReadLinear(rig);
            var i = ((32 * 64) + 32) * 4;
            float[] hdr = [2.7f, 1.5f, 0.3f];
            var want = mapper switch { ToneMapper.PbrNeutral => Neutral(hdr), ToneMapper.Aces => Aces(hdr), _ => [1f, 1f, 0.3f] };
            for (var c = 0; c < 3; c++)
            {
                Assert.True(Math.Abs(px[i + c] - want[c]) < 0.004, $"{mapper} channel {c}: {px[i + c]} against {want[c]}");
            }
        }
    }

    [Fact]
    public void SrgbTargetsAreEncodedByTheHardwareAndUnormTargetsByTheShader()
    {
        byte[] Render(GpuFormat format)
        {
            using var rig = new Rig(32, 32, format);
            var model = rig.Own(rig.Renderer.Upload(GltfLoader.Load(Plane.Unlit(0.2f, 0.4f, 0.6f))));
            var scene = new Scene3D { Camera = Plane.TopDown(8), Samples = 1 };
            scene.Add(model);
            scene.Environment.Background = BackgroundMode.Color;
            scene.Post.Bloom = false;
            scene.Post.ToneMapper = ToneMapper.None;
            return rig.Capture(scene);
        }

        var a = Render(GpuFormat.Rgba8UnormSrgb);
        var b = Render(GpuFormat.Rgba8Unorm);
        var i = ((16 * 32) + 16) * 4;
        for (var c = 0; c < 3; c++)
        {
            var want = (int)MathF.Round(Srgb(Colour[c]) * 255);
            Assert.InRange(a[i + c], want - 1, want + 1);
            Assert.InRange(b[i + c], want - 1, want + 1);
        }
    }

    [Fact]
    public void BloomSpillsLightAroundABrightObject()
    {
        float Around(bool bloom)
        {
            var (rig, scene) = Plane.Setup(Plane.Gltf("""{"pbrMetallicRoughness":{"baseColorFactor":[1,1,1,1]},"extensions":{"KHR_materials_unlit":{}}}""", "\"KHR_materials_unlit\"", 0.4f), 64, 8f);
            using (rig)
            {
                scene.Post.Exposure = 8f;
                scene.Post.Bloom = bloom;
                scene.Post.BloomStrength = 0.5f;
                rig.Target.Render(scene);
                var px = Plane.ReadLinear(rig);

                // One world unit (8 pixels) beyond the 0.8-unit square's edge.
                return px[((32 * 64) + 32 + 12) * 4];
            }
        }

        Assert.Equal(0f, Around(false));
        Assert.True(Around(true) > 0.01f);
    }

    [Fact]
    public void AlphaMaskDiscardsAndBlendMixes()
    {
        var (rig, scene) = Plane.Setup(Plane.Gltf("""{"alphaMode":"MASK","alphaCutoff":0.5,"pbrMetallicRoughness":{"baseColorFactor":[1,1,1,0.3]},"extensions":{"KHR_materials_unlit":{}}}""", "\"KHR_materials_unlit\""));
        using (rig)
        {
            Assert.Equal(0f, Plane.ReadAfterRender(rig, scene)[((32 * 64) + 32) * 4]);
        }

        (rig, scene) = Plane.Setup(Plane.Gltf("""{"alphaMode":"MASK","alphaCutoff":0.5,"pbrMetallicRoughness":{"baseColorFactor":[1,1,1,0.9]},"extensions":{"KHR_materials_unlit":{}}}""", "\"KHR_materials_unlit\""));
        using (rig)
        {
            Assert.Equal(1f, Plane.ReadAfterRender(rig, scene)[((32 * 64) + 32) * 4]);
        }

        (rig, scene) = Plane.Setup(Plane.Gltf("""{"alphaMode":"BLEND","pbrMetallicRoughness":{"baseColorFactor":[1,0.5,0,0.25]},"extensions":{"KHR_materials_unlit":{}}}""", "\"KHR_materials_unlit\""));
        using (rig)
        {
            scene.Environment.BackgroundColor = new Vector3(0.4f, 0.4f, 0.4f);
            var px = Plane.ReadAfterRender(rig, scene);
            var i = ((32 * 64) + 32) * 4;
            Assert.InRange(px[i], (1 * 0.25) + (0.4 * 0.75) - 0.004, (1 * 0.25) + (0.4 * 0.75) + 0.004);
            Assert.InRange(px[i + 1], (0.5 * 0.25) + (0.4 * 0.75) - 0.004, (0.5 * 0.25) + (0.4 * 0.75) + 0.004);
            Assert.InRange(px[i + 2], (0.4 * 0.75) - 0.004, (0.4 * 0.75) + 0.004);
        }
    }

    [Fact]
    public void SingleSidedSurfacesAreCulledFromBehindAndDoubleSidedOnesAreNot()
    {
        float Centre(string material)
        {
            var (rig, scene) = Plane.Setup(Plane.Gltf(material, "\"KHR_materials_unlit\""));
            using (rig)
            {
                scene.Camera.World = Matrix4x4.CreateRotationY(MathF.PI) * Matrix4x4.CreateTranslation(0, 0, -10);
                return Plane.ReadAfterRender(rig, scene)[((32 * 64) + 32) * 4];
            }
        }

        Assert.Equal(0f, Centre("""{"pbrMetallicRoughness":{"baseColorFactor":[1,1,1,1]},"extensions":{"KHR_materials_unlit":{}}}"""));
        Assert.Equal(1f, Centre("""{"doubleSided":true,"pbrMetallicRoughness":{"baseColorFactor":[1,1,1,1]},"extensions":{"KHR_materials_unlit":{}}}"""));
    }

    [Fact]
    public void MirroredNodesStillShowTheirFrontFaces()
    {
        var (rig, scene) = Plane.Setup(Plane.Gltf("""{"pbrMetallicRoughness":{"baseColorFactor":[1,1,1,1]},"extensions":{"KHR_materials_unlit":{}}}""", "\"KHR_materials_unlit\"", 5f, ",\"scale\":[-1,1,1]"));
        using (rig)
        {
            Assert.Equal(1f, Plane.ReadAfterRender(rig, scene)[((32 * 64) + 32) * 4]);
        }
    }
}
