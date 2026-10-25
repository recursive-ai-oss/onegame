using OneGame.Gpu;

namespace OneGame.Render3D.Tests;

/// <summary>Ambient occlusion: a crease where two surfaces meet is darker with it on, an open plane is not.</summary>
public class SsaoTests
{
    private const int W = 160;
    private const int H = 120;

    private static (Rig Rig, Scene3D Scene) Make(GpuFormat format = GpuFormat.Rgba16Float)
    {
        var rig = new Rig(W, H, format);
        var model = new Shapes()
            .Ground(0f, 30f, Shapes.Matte(0.8f, 0.8f, 0.8f))
            .Wall(-4f, 30f, 12f, Shapes.Matte(0.8f, 0.8f, 0.8f))
            .Box(new Vector3(-2.5f, 0.75f, -2.5f), new Vector3(1.5f, 1.5f, 1.5f), Shapes.Matte(0.8f, 0.45f, 0.35f))
            .Box(new Vector3(2.2f, 0.5f, -1f), new Vector3(1f, 1f, 1f), Shapes.Matte(0.35f, 0.5f, 0.8f))
            .Upload(rig);
        var scene = new Scene3D { Camera = Camera3D.LookAt(new Vector3(0, 4, 6), new Vector3(0, 0.5f, -2), Vector3.UnitY), Samples = 1 };
        scene.Add(model);
        scene.Environment.Background = BackgroundMode.Color;
        scene.Environment.BackgroundColor = Vector3.Zero;
        scene.Post.Bloom = false;
        scene.Post.ToneMapper = ToneMapper.None;
        return (rig, scene);
    }

    // Average of the green channel over a small square of pixels around a world point, as seen by the scene's camera.
    private static float Sample(float[] px, Scene3D scene, Vector3 world, int radius = 1)
    {
        var clip = Vector4.Transform(new Vector4(world, 1f), scene.Camera.View * scene.Camera.Projection(W / (float)H));
        var x = (int)(((clip.X / clip.W) * 0.5f + 0.5f) * W);
        var y = (int)((0.5f - ((clip.Y / clip.W) * 0.5f)) * H);
        float sum = 0;
        var n = 0;
        for (var dy = -radius; dy <= radius; dy++)
        {
            for (var dx = -radius; dx <= radius; dx++)
            {
                sum += px[((((y + dy) * W) + x + dx) * 4) + 1];
                n++;
            }
        }

        return sum / n;
    }

    [Fact]
    public void CreaseIsDarkerAndOpenPlaneIsNot()
    {
        var (rig, scene) = Make();
        using (rig)
        {
            rig.Target.Render(scene);
            var off = Plane.ReadLinear(rig);
            Assert.False(rig.Renderer.LastStats.Ssao);

            scene.Ssao.Enabled = true;
            scene.Ssao.Radius = 1.0f;
            rig.Target.Render(scene);
            var on = Plane.ReadLinear(rig);
            Assert.True(rig.Renderer.LastStats.Ssao);

            // In the crease (ground 0.25 from the wall, and wall 0.25 above the ground) light drops by a good fraction.
            var creaseGround = Sample(on, scene, new Vector3(0, 0, -3.75f)) / Sample(off, scene, new Vector3(0, 0, -3.75f));
            var creaseWall = Sample(on, scene, new Vector3(0, 0.25f, -4f)) / Sample(off, scene, new Vector3(0, 0.25f, -4f));
            Assert.True(creaseGround < 0.8f, $"ground in the crease keeps {creaseGround:P0}");
            Assert.True(creaseWall < 0.8f, $"wall in the crease keeps {creaseWall:P0}");

            // Far from any crease, on the open ground and high on the wall, the image is unchanged.
            var openGround = Sample(on, scene, new Vector3(0, 0, 0.5f)) / Sample(off, scene, new Vector3(0, 0, 0.5f));
            var openWall = Sample(on, scene, new Vector3(0, 3f, -4f)) / Sample(off, scene, new Vector3(0, 3f, -4f));
            Assert.InRange(openGround, 0.98f, 1.01f);
            Assert.InRange(openWall, 0.98f, 1.01f);

            // The occlusion fades smoothly away from the crease: further out it is weaker.
            var near = Sample(on, scene, new Vector3(0, 0, -3.5f)) / Sample(off, scene, new Vector3(0, 0, -3.5f));
            var mid = Sample(on, scene, new Vector3(0, 0, -2.5f)) / Sample(off, scene, new Vector3(0, 0, -2.5f));
            Assert.True(near < mid && mid < 0.999f + 0.01f, $"near {near:F3}, mid {mid:F3}");
        }
    }

    [Fact]
    public void LowQualityTurnsItOffAndPassesAppearOnlyWhenOn()
    {
        var (rig, scene) = Make();
        using (rig)
        {
            scene.Ssao.Enabled = true;
            scene.Quality = QualityLevel.Low;
            rig.Target.Render(scene);
            Assert.False(rig.Renderer.LastStats.Ssao);
            Assert.DoesNotContain(rig.Target.Graph.Describe().Passes, p => p.Name == "ssao" && !p.Culled);

            scene.Quality = QualityLevel.Medium;
            rig.Target.Render(scene);
            var names = rig.Target.Graph.Describe().Passes.Where(p => !p.Culled).Select(p => p.Name).ToArray();
            Assert.Contains("depth_normal_prepass", names);
            Assert.Contains("ssao", names);
            Assert.Contains("ssao_blur", names);
        }
    }

    [Fact]
    public void OffAndOnCaptures()
    {
        var (rig, scene) = Make(GpuFormat.Rgba8UnormSrgb);
        using (rig)
        {
            scene.Post.ToneMapper = ToneMapper.PbrNeutral;
            scene.Environment.Intensity = 1.5f;
            var off = rig.Capture(scene, "ssao_off");
            scene.Ssao.Enabled = true;
            scene.Ssao.Radius = 1.0f;
            var on = rig.Capture(scene, "ssao_on");
            Reference.Compare("ssao_on", on, W, H, meanTolerance: 0.6, maxTolerance: 12);
            Reference.Compare("ssao_off", off, W, H, meanTolerance: 0.6, maxTolerance: 12);
        }
    }
}
