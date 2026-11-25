using OneGame.Gltf;
using RecursiveAI.OneGpu;

namespace OneGame.Render3D.Tests;

// A crowd of one skinned model, every copy in its own pose, drawn by one batch.
public class SkinnedInstancingTests
{
    private const float Scale = 0.04f;

    private static Matrix4x4 Place(int i, int columns)
    {
        var x = ((i % columns) - ((columns - 1) / 2f)) * 4.5f;
        var z = -(i / columns) * 4.5f;
        return Matrix4x4.CreateScale(Scale) * Matrix4x4.CreateRotationY(i * 0.7f) * Matrix4x4.CreateTranslation(x, 0, z);
    }

    private static NodePose PoseOf(Model3D model, int i)
    {
        var pose = model.Source.CreateRestPose();
        var clip = model.Source.Animations[1 + (i % 2)];
        clip.Apply(pose, (i * 0.137f) % clip.Duration);
        return pose;
    }

    private static Scene3D Stage(Rig rig, int columns, int rows)
    {
        var ground = new Shapes().Ground(0f, 200f, Shapes.Matte(0.45f, 0.5f, 0.35f)).Upload(rig);
        rig.Own(ground);
        var scene = new Scene3D { Camera = Camera3D.LookAt(new Vector3(0, 7, 14), new Vector3(0, 1.2f, -(rows - 1) * 2.2f), Vector3.UnitY) };
        scene.Camera.Far = 300f;
        scene.Add(ground);
        scene.Lights.Add(new Light3D { Kind = LightKind.Directional, Direction = Vector3.Normalize(new Vector3(-0.5f, -1f, -0.4f)), Intensity = 3f, CastShadows = true });
        scene.Shadows.MaxDistance = 80f;
        scene.Environment.Intensity = 0.6f;
        _ = columns;
        return scene;
    }

    [Fact]
    public void PosedCopiesMatchSeparateAnimatedModelsAndCastShadows()
    {
        var path = Paths.Model("Fox/glTF/Fox.gltf");
        Assert.NotNull(path);
        const int columns = 4;
        const int count = 12;
        using var rig = new Rig(480, 320);
        var fox = rig.Load(path);

        var separate = Stage(rig, columns, count / columns);
        var crowd = Stage(rig, columns, count / columns);
        var batch = crowd.AddSkinnedInstanced(fox, count);
        Assert.True(batch.Posed);
        for (var i = 0; i < count; i++)
        {
            var pose = PoseOf(fox, i);
            var instance = separate.Add(fox, Place(i, columns));
            instance.Pose.CopyFrom(pose);
            instance.UpdateTransforms();
            batch.Add(Place(i, columns), pose);
        }

        var expected = rig.Capture(separate, "skinned-separate");
        var separateStats = rig.Renderer.LastStats;
        var actual = rig.Capture(crowd, "skinned-batch");
        var stats = rig.Renderer.LastStats;

        Assert.Equal(count, stats.Instances);
        Assert.True(stats.ShadowTiles > 0);
        Assert.True(stats.DrawCalls < separateStats.DrawCalls, $"{stats.DrawCalls} draws against {separateStats.DrawCalls}");
        var worst = 0;
        var off = 0;
        for (var i = 0; i < expected.Length; i++)
        {
            var d = Math.Abs(expected[i] - actual[i]);
            worst = Math.Max(worst, d);
            if (d > 1)
            {
                off++;
            }
        }

        Assert.True(worst <= 3 && off <= 40, $"worst channel difference {worst}, {off} channels off by more than 1");

        // Looking at it: foxes are tan, the ground green, and the shadows are darker green; the pose and the shadow must both be there.
        Reference.Compare("skinned_crowd", actual, 480, 320, meanTolerance: 0.5, maxTolerance: 12);
    }

    [Fact]
    public void FiveHundredAnimatedCharactersAreAFewDrawsAndAllocateNothing()
    {
        var path = Paths.Model("Fox/glTF/Fox.gltf");
        Assert.NotNull(path);
        using var rig = new Rig(320, 240);
        var fox = rig.Load(path);
        var scene = Stage(rig, 25, 20);
        scene.Camera = Camera3D.LookAt(new Vector3(0, 40, 60), new Vector3(0, 0, -40), Vector3.UnitY);
        var batch = scene.AddSkinnedInstanced(fox, 500);
        var poses = new NodePose[8];
        for (var p = 0; p < poses.Length; p++)
        {
            poses[p] = PoseOf(fox, p);
        }

        var meshes = fox.DrawNodes.Sum(n => fox.Meshes[fox.Source.Nodes[n].Mesh]!.Length);
        void Fill(int phase)
        {
            batch.Clear();
            for (var i = 0; i < 500; i++)
            {
                batch.Add(Place(i, 25), poses[(i + phase) % poses.Length]);
            }
        }

        for (var i = 0; i < 12; i++)
        {
            Fill(i);
            rig.Target.Render(scene);
        }

        rig.Device.WaitIdle();
        Fill(3);
        rig.Target.Render(scene);
        var stats = rig.Renderer.LastStats;
        Assert.Equal(500, stats.Instances);

        // The camera pass is one draw per primitive of the model (plus the ground); every shadow tile adds the same again.
        Assert.Equal(meshes + 1, stats.DrawCalls);

        long allocated = 0;
        for (var i = 0; i < 20; i++)
        {
            Fill(i);
            var before = GC.GetAllocatedBytesForCurrentThread();
            rig.Target.Render(scene);
            allocated += GC.GetAllocatedBytesForCurrentThread() - before;
        }

        rig.Device.WaitIdle();
        Assert.True(allocated == 0, $"20 steady frames of 500 animated copies allocated {allocated} bytes");
    }

    [Fact]
    public void CopiesOutOfViewAreCulledAndAPlainBatchIgnoresPoses()
    {
        var path = Paths.Model("Fox/glTF/Fox.gltf");
        Assert.NotNull(path);
        using var rig = new Rig(160, 120);
        var fox = rig.Load(path);
        var scene = new Scene3D { Camera = Camera3D.LookAt(new Vector3(0, 3, 10), new Vector3(0, 3, 0), Vector3.UnitY) };
        var batch = scene.AddSkinnedInstanced(fox);
        var pose = PoseOf(fox, 1);
        batch.Add(Matrix4x4.CreateScale(Scale), pose);
        batch.Add(Matrix4x4.CreateScale(Scale) * Matrix4x4.CreateTranslation(0, 0, 40), pose);       // behind the camera
        batch.Add(Matrix4x4.CreateScale(Scale) * Matrix4x4.CreateTranslation(500, 0, 0), pose);      // far to the side
        rig.Target.Render(scene);
        Assert.Equal(3, rig.Renderer.LastStats.Instances);
        Assert.Equal(1, rig.Renderer.LastStats.InstancesDrawn);

        batch.RemoveAt(0);
        rig.Target.Render(scene);
        Assert.Equal(0, rig.Renderer.LastStats.InstancesDrawn);

        var plain = new Scene3D().AddInstanced(fox);
        Assert.False(plain.Posed);
        Assert.Throws<InvalidOperationException>(() => plain.Add(Matrix4x4.Identity, pose));
    }

    // Indicative timing of a crowd on whatever device the environment selects; set ONEGAME_CROWD_TIMING=1. Not a pass/fail check.
    [Theory]
    [InlineData(500)]
    [InlineData(2000)]
    public void CrowdFrameTimes(int count)
    {
        Assert.SkipUnless(Environment.GetEnvironmentVariable("ONEGAME_CROWD_TIMING") == "1", "timing only runs on request");
        var path = Paths.Model("Fox/glTF/Fox.gltf");
        Assert.NotNull(path);
        using var rig = new Rig(1280, 720, validation: false);
        var device = rig.Device;
        var renderer = rig.Renderer;
        var target = rig.Target;
        var fox = rig.Load(path);
        var groundModel = new Shapes().Ground(0f, 400f, Shapes.Matte(0.45f, 0.5f, 0.35f)).Upload(rig);
        rig.Own(groundModel);
        var scene = new Scene3D { Camera = Camera3D.LookAt(new Vector3(0, 30, 60), new Vector3(0, 0, -30), Vector3.UnitY) };
        scene.Camera.Far = 500f;
        scene.Add(groundModel);
        scene.Lights.Add(new Light3D { Kind = LightKind.Directional, Direction = Vector3.Normalize(new Vector3(-0.5f, -1f, -0.4f)), Intensity = 3f, CastShadows = true });
        scene.Shadows.MaxDistance = 150f;
        var batch = scene.AddSkinnedInstanced(fox, count);
        var poses = new NodePose[16];
        for (var p = 0; p < poses.Length; p++)
        {
            poses[p] = PoseOf(fox, p);
        }

        var columns = (int)Math.Ceiling(Math.Sqrt(count) * 1.6);
        void Fill(int phase)
        {
            batch.Clear();
            for (var i = 0; i < count; i++)
            {
                var x = ((i % columns) - (columns / 2f)) * 2.2f;
                var z = -(i / columns) * 2.2f;
                batch.Add(Matrix4x4.CreateScale(Scale) * Matrix4x4.CreateRotationY(i * 0.7f) * Matrix4x4.CreateTranslation(x, 0, z), poses[(i + phase) % poses.Length]);
            }
        }

        var cpu = new List<double>();
        var gpu = new List<double>();
        var fill = new List<double>();
        for (var f = 0; f < 90; f++)
        {
            var t0 = System.Diagnostics.Stopwatch.GetTimestamp();
            Fill(f / 3);
            fill.Add(System.Diagnostics.Stopwatch.GetElapsedTime(t0).TotalMilliseconds);
            target.Render(scene);
            if (f >= 30)
            {
                var stats = renderer.LastStats;
                cpu.Add(stats.CpuFrameMs);
                if (stats.GpuFrameMs is { } g)
                {
                    gpu.Add(g);
                }
            }
        }

        device.WaitIdle();
        var last = renderer.LastStats;
        cpu.Sort();
        fill.Sort();
        gpu.Sort();
        Console.WriteLine($"CROWD {device.Adapter.Name} {count}: copies={last.Instances} drawn={last.InstancesDrawn} draws={last.DrawCalls} shadowTiles={last.ShadowTiles} cpu median={cpu[cpu.Count / 2]:F2} ms fill(add {count} posed copies) median={fill[fill.Count / 2]:F2} ms gpu median={(gpu.Count > 0 ? gpu[gpu.Count / 2] : double.NaN):F2} ms");
    }
}
