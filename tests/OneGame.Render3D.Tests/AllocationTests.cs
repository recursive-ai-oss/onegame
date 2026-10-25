using OneGame.Gpu;

namespace OneGame.Render3D.Tests;

public class AllocationTests
{
    [Fact]
    public void SteadyFramesAllocateNoManagedMemory()
    {
        var path = Paths.Model("Fox/glTF/Fox.gltf");
        Assert.NotNull(path);
        using var rig = new Rig(320, 240);
        var model = rig.Load(path);
        var scene = new Scene3D();
        var instance = scene.Add(model);
        scene.Add(model, Matrix4x4.CreateTranslation(60, 0, 0));
        var b = model.Source.ComputeWorldBounds()!.Value;
        scene.Camera = Camera3D.Frame(b.Min, b.Max, 30, 15, 320 / 240f);
        scene.Lights.Add(Light3D.Directional(new Vector3(-1, -1, -1), Vector3.One, 2));
        scene.Lights.Add(Light3D.Point(new Vector3(40, 50, 50), Vector3.One, 20000, 300));
        scene.Lights.Add(Light3D.Spot(new Vector3(0, 80, 40), new Vector3(0, -1, 0), Vector3.One, 40000, 0.2f, 0.6f, 300));
        scene.Environment.Map = rig.Own(rig.Renderer.CreateEnvironment(HdrImage.CreateSky(64, 32, new Vector3(0.5f), new Vector3(0.7f), new Vector3(0.2f), new Vector3(0.3f, 1, 0.2f), new Vector3(20f))));

        // The animated instance exercises skinning every frame; the warm-up covers pipeline creation and the environment filtering.
        for (var i = 0; i < 12; i++)
        {
            instance.Animate(1, i * 0.05f);
            rig.Target.Render(scene);
        }

        // The first frame after the GPU has drained collects the pass timings (the graph's own bookkeeping, measured by its tests), so one
        // more frame follows the drain before counting starts.
        rig.Device.WaitIdle();
        rig.Target.Render(scene);
        long allocated = 0;
        var where = new List<string>();
        long animating = 0;
        for (var i = 0; i < 60; i++)
        {
            var a0 = GC.GetAllocatedBytesForCurrentThread();
            instance.Animate(1, 0.3f + (i * 0.02f));
            var a1 = GC.GetAllocatedBytesForCurrentThread();
            rig.Target.Render(scene);
            var delta = GC.GetAllocatedBytesForCurrentThread() - a1;
            allocated += delta;
            if (delta > 0)
            {
                where.Add($"frame {i}: {delta}");
            }

            animating += a1 - a0;
        }

        rig.Device.WaitIdle();
        _ = animating;   // evaluating the animation is the importer's code, measured apart from the renderer
        Assert.True(allocated == 0, $"60 steady frames allocated {allocated} bytes ({string.Join(", ", where)})");
    }
}
