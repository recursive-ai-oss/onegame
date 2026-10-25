using OneGame.Gpu;

namespace OneGame.Render3D.Tests;

public class InstancingTests
{
    private static Model3D Tree(Rig rig) => new Shapes()
        .Box(new Vector3(0, 0.75f, 0), new Vector3(0.4f, 1.5f, 0.4f), Shapes.Matte(0.35f, 0.22f, 0.1f))
        .Box(new Vector3(0, 2.6f, 0), new Vector3(1.8f, 2.2f, 1.8f), Shapes.Matte(0.2f, 0.5f, 0.15f))
        .Upload(rig);

    private static InstanceBatch3D Forest(Scene3D scene, Model3D tree, int side, float spacing)
    {
        var batch = scene.AddInstanced(tree, side * side);
        var rng = new Random(12345);
        for (var z = 0; z < side; z++)
        {
            for (var x = 0; x < side; x++)
            {
                var scale = 0.7f + (float)rng.NextDouble() * 0.9f;
                var pos = new Vector3(((x - (side / 2f)) * spacing) + ((float)rng.NextDouble() - 0.5f) * spacing * 0.6f, 0, ((z - (side / 2f)) * spacing) + ((float)rng.NextDouble() - 0.5f) * spacing * 0.6f);
                var m = Matrix4x4.CreateScale(scale) * Matrix4x4.CreateRotationY((float)rng.NextDouble() * 6.28f) * Matrix4x4.CreateTranslation(pos);
                var tint = new Vector4(0.8f + (float)rng.NextDouble() * 0.4f, 0.8f + (float)rng.NextDouble() * 0.4f, 0.8f + (float)rng.NextDouble() * 0.4f, 1f);
                batch.Add(m, tint);
            }
        }

        return batch;
    }

    internal static Scene3D ForestScene(Rig rig, int side, float spacing, out Model3D tree)
    {
        tree = Tree(rig);
        var ground = new Shapes().Ground(0f, 600f, Shapes.Matte(0.45f, 0.5f, 0.35f)).Upload(rig);
        var scene = new Scene3D { Camera = Camera3D.LookAt(new Vector3(0, 18, 70), new Vector3(0, 2, 0), Vector3.UnitY) };
        scene.Camera.Far = 400f;
        scene.Add(ground);
        Forest(scene, tree, side, spacing);
        scene.Lights.Add(new Light3D { Kind = LightKind.Directional, Direction = Vector3.Normalize(new Vector3(-0.5f, -1f, -0.4f)), Intensity = 2.5f, CastShadows = true });
        scene.Shadows.MaxDistance = 150f;
        scene.Environment.Intensity = 0.6f;
        return scene;
    }

    [Fact]
    public void TenThousandInstancesDrawInAFewCallsAndAreCulled()
    {
        using var rig = new Rig(480, 320);
        var scene = ForestScene(rig, 100, 6f, out _);
        var pixels = rig.Capture(scene, "forest");
        var stats = rig.Renderer.LastStats;
        Assert.Equal(10000, stats.Instances);
        Assert.InRange(stats.InstancesDrawn, 500, 9999);

        // One draw per primitive of the tree (trunk, crown) plus the ground: the camera pass is three draws for ten thousand trees.
        Assert.Equal(3, stats.DrawCalls);
        Assert.Equal(4, stats.ShadowTiles);
        Assert.True(stats.Triangles > 12L * 2 * 500, $"{stats.Triangles} triangles");
        Assert.Contains(Enumerable.Range(0, 480 * 320), i => pixels[i * 4 + 1] > pixels[i * 4] + 20 && pixels[i * 4 + 1] > pixels[i * 4 + 2] + 20);
        Reference.Compare("forest", pixels, 480, 320, meanTolerance: 0.8, maxTolerance: 20);
    }

    [Fact]
    public void CullingDropsWhatIsBehindTheCameraOrOutOfSight()
    {
        using var rig = new Rig(160, 120);
        var tree = Tree(rig);
        var scene = new Scene3D { Camera = Camera3D.LookAt(new Vector3(0, 3, 10), new Vector3(0, 3, 0), Vector3.UnitY) };
        var batch = scene.AddInstanced(tree);
        batch.Add(Matrix4x4.CreateTranslation(0, 0, 0));              // in front
        batch.Add(Matrix4x4.CreateTranslation(0, 0, 40));             // behind the camera
        batch.Add(Matrix4x4.CreateTranslation(500, 0, 0));            // far to the side
        batch.Add(Matrix4x4.CreateTranslation(-6, 0, -3));            // in view
        rig.Target.Render(scene);
        Assert.Equal(4, rig.Renderer.LastStats.Instances);
        Assert.Equal(2, rig.Renderer.LastStats.InstancesDrawn);
        Assert.Equal(2, rig.Renderer.LastStats.DrawCalls);

        batch.Visible = false;
        rig.Target.Render(scene);
        Assert.Equal(0, rig.Renderer.LastStats.Instances);
        Assert.Equal(0, rig.Renderer.LastStats.DrawCalls);
    }

    [Fact]
    public void InstancedDrawMatchesAPlainPlacementAndAppliesTheTint()
    {
        var placement = Matrix4x4.CreateScale(1.3f) * Matrix4x4.CreateRotationY(0.7f) * Matrix4x4.CreateTranslation(0.5f, 0.2f, -0.3f);
        float[] Render(bool instanced, Vector4 tint)
        {
            using var rig = new Rig(96, 72, GpuFormat.Rgba16Float);
            var tree = Tree(rig);
            var scene = new Scene3D { Camera = Camera3D.LookAt(new Vector3(5, 5, 8), new Vector3(0, 2, 0), Vector3.UnitY), Samples = 1 };
            if (instanced)
            {
                scene.AddInstanced(tree).Add(placement, tint);
            }
            else
            {
                scene.Add(tree, placement);
            }

            scene.Lights.Add(Light3D.Directional(new Vector3(-1, -1.5f, -0.5f), Vector3.One, 3f));
            scene.Environment.Background = BackgroundMode.Color;
            scene.Environment.BackgroundColor = Vector3.Zero;
            scene.Post.Bloom = false;
            scene.Post.ToneMapper = ToneMapper.None;
            return Plane.ReadAfterRender(rig, scene);
        }

        var plain = Render(false, Vector4.One);
        var batched = Render(true, Vector4.One);
        var worst = 0f;
        for (var i = 0; i < plain.Length; i++)
        {
            worst = MathF.Max(worst, MathF.Abs(plain[i] - batched[i]));
        }

        Assert.True(worst < 0.01f, $"largest difference {worst}");

        // A tint of (1, 0.5, 0.25) scales the base colour the same way, so the lit colour scales by it in linear light (diffuse and specular).
        var tinted = Render(true, new Vector4(1f, 0.5f, 0.25f, 1f));
        float Sum(float[] px, int channel)
        {
            double s = 0;
            for (var i = channel; i < px.Length; i += 4)
            {
                s += px[i];
            }

            return (float)s;
        }

        Assert.InRange(Sum(tinted, 1) / Sum(batched, 1), 0.45f, 0.55f);
        Assert.InRange(Sum(tinted, 2) / Sum(batched, 2), 0.2f, 0.3f);
    }

    [Fact]
    public void SteadyFramesAllocateNoManagedMemory()
    {
        using var rig = new Rig(320, 240);
        var scene = ForestScene(rig, 100, 6f, out _);
        scene.Ssao.Enabled = true;
        scene.Lights.Add(Light3D.Spot(new Vector3(0, 30, 40), -Vector3.UnitY, Vector3.One, 40000f, 0.3f, 0.7f, 200f));
        scene.Lights[1].CastShadows = true;
        for (var i = 0; i < 12; i++)
        {
            scene.Camera = Camera3D.LookAt(new Vector3(i, 18, 70), new Vector3(0, 2, 0), Vector3.UnitY);
            rig.Target.Render(scene);
        }

        rig.Device.WaitIdle();
        rig.Target.Render(scene);
        long allocated = 0;
        var where = new List<string>();
        for (var i = 0; i < 60; i++)
        {
            // The camera is built outside the measured region; the frames see a different view each time, so culling varies.
            scene.Camera = Camera3D.LookAt(new Vector3(i * 0.5f, 18, 70), new Vector3(0, 2, 0), Vector3.UnitY);
            var a = GC.GetAllocatedBytesForCurrentThread();
            rig.Target.Render(scene);
            var delta = GC.GetAllocatedBytesForCurrentThread() - a;
            allocated += delta;
            if (delta > 0)
            {
                where.Add($"frame {i}: {delta}");
            }
        }

        rig.Device.WaitIdle();

        // The RADV driver itself allocates about 2 KB now and then (seen with the 2D renderer too); everywhere else the frames allocate nothing.
        var allowed = rig.Device.Adapter.Name.Contains("RADV", StringComparison.Ordinal) ? 8192 : 0;
        Assert.True(allocated <= allowed, $"60 steady frames allocated {allocated} bytes ({string.Join(", ", where.Take(8))})");
    }
}
