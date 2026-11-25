using OneGame.Gpu.Graph;
using RecursiveAI.OneGpu;

namespace OneGame.Render3D.Tests;

/// <summary>Sampled offscreen views share one shadow atlas, and a view that goes away leaves nothing behind.</summary>
public class SharedAtlasTests
{
    private const int Size = 64;

    // A directional light (four cascades) and a point light (six faces): ten tiles at High quality, the most expensive atlas a scene asks for.
    private static (Rig Rig, Scene3D Scene) Make()
    {
        var rig = new Rig(Size, Size);
        var model = new Shapes()
            .Ground(0f, 20f, Shapes.Matte(0.7f, 0.72f, 0.68f))
            .Box(new Vector3(-1.5f, 1f, 0f), new Vector3(2f, 2f, 2f), Shapes.Matte(0.8f, 0.3f, 0.25f))
            .Upload(rig);
        var scene = new Scene3D { Camera = Camera3D.LookAt(new Vector3(5f, 4f, 7f), new Vector3(0, 0.8f, 0), Vector3.UnitY), Samples = 1 };
        scene.Add(model);
        scene.Lights.Add(Light3D.Directional(new Vector3(-0.4f, -1f, -0.5f), new Vector3(1f, 0.96f, 0.9f), 3f));
        scene.Lights.Add(Light3D.Point(new Vector3(0f, 4f, 2f), Vector3.One, 80f, 14f));
        scene.Lights[0].CastShadows = true;
        scene.Lights[1].CastShadows = true;
        return (rig, scene);
    }

    private static ulong Reserved(Rig rig)
    {
        rig.Device.WaitIdle();
        return rig.Device.MemoryStats.ReservedBytes;
    }

    [Fact]
    public void SixteenSampledViewsWithShadowsReserveAboutOneAtlas()
    {
        var (rig, scene) = Make();
        using (rig)
        {
            var views = new List<Offscreen3D>();
            try
            {
                var before = Reserved(rig);
                views.Add(new Offscreen3D(rig.Renderer, Size, Size, GpuFormat.Rgba8UnormSrgb, sampled: true));
                views[0].Render(scene);
                var one = Reserved(rig);
                var atlas = (ulong)rig.Renderer.ShadowAtlasBytes;
                Assert.True(atlas > 100UL << 20, $"expected the High quality atlas to be large, got {atlas >> 20} MiB");
                Assert.True(one - before >= atlas, "the first view reserves its atlas");

                for (var i = 1; i < 16; i++)
                {
                    views.Add(new Offscreen3D(rig.Renderer, Size, Size, GpuFormat.Rgba8UnormSrgb, sampled: true));
                    views[i].Render(scene);
                }

                var sixteen = Reserved(rig);
                Console.WriteLine($"shadow atlas {atlas >> 20} MiB; reserved {before >> 20} MiB before, {one >> 20} MiB with one view, {sixteen >> 20} MiB with sixteen");

                // Fifteen more views cost their own small targets, not fifteen more atlases (about 2.9 GiB here).
                Assert.True(sixteen - one < atlas / 4, $"15 more views reserved {(sixteen - one) >> 20} MiB; one atlas is {atlas >> 20} MiB");

                // Every view drew the same picture as the first.
                var first = Read(rig, views[0]);
                Assert.Contains(first, b => b != 0);
                for (var i = 1; i < 16; i++)
                {
                    Assert.Equal(first, Read(rig, views[i]));
                }

                // The shadows are really there: the same scene without them differs.
                scene.Lights[0].CastShadows = false;
                scene.Lights[1].CastShadows = false;
                views[0].Render(scene);
                Assert.NotEqual(first, Read(rig, views[0]));
            }
            finally
            {
                foreach (var v in views)
                {
                    v.Dispose();
                }
            }
        }
    }

    private static byte[] Read(Rig rig, Offscreen3D view)
    {
        var bytes = new byte[view.Width * view.Height * 4];
        rig.Device.ReadTexture(view.Target, 0, 0, bytes);
        return bytes;
    }

    [Fact]
    public void ViewsCreatedAndDisposedInALoopLeaveReservedMemoryFlat()
    {
        var (rig, scene) = Make();
        using (rig)
        {
            ulong Round()
            {
                using var view = new Offscreen3D(rig.Renderer, Size, Size, GpuFormat.Rgba8UnormSrgb, sampled: true);
                view.Render(scene);
                view.Render(scene);
                return 0;
            }

            for (var i = 0; i < 3; i++)
            {
                Round();
                rig.Target.Render(scene);
            }

            var start = Reserved(rig);
            for (var i = 0; i < 40; i++)
            {
                Round();
                rig.Target.Render(scene);
            }

            var end = Reserved(rig);
            Console.WriteLine($"dispose loop: reserved {start >> 20} MiB -> {end >> 20} MiB over 40 views");
            Assert.True(end <= start + (8UL << 20), $"reserved memory grew from {start >> 20} to {end >> 20} MiB");
        }
    }

    [Fact]
    public void TheAtlasIsReleasedAfterAWhileWithoutShadowsAndComesBack()
    {
        var (rig, scene) = Make();
        using (rig)
        {
            rig.Target.Render(scene);
            Assert.True(rig.Renderer.ShadowAtlasBytes > 0);
            scene.Lights[0].CastShadows = false;
            scene.Lights[1].CastShadows = false;
            for (var i = 0; i < 260; i++)
            {
                rig.Target.Render(scene);
            }

            Assert.Equal(0, rig.Renderer.ShadowAtlasBytes);
            scene.Lights[0].CastShadows = true;
            rig.Target.Render(scene);
            Assert.True(rig.Renderer.ShadowAtlasBytes > 0);
        }
    }

    [Fact]
    public void ASmallerFrameAfterABiggerOneKeepsTheAtlasInsteadOfReallocating()
    {
        var (rig, scene) = Make();
        using (rig)
        {
            rig.Target.Render(scene);
            var big = rig.Renderer.ShadowAtlasBytes;
            scene.Lights[1].CastShadows = false;     // only the cascades now: fewer tiles
            rig.Target.Render(scene);
            Assert.Equal(big, rig.Renderer.ShadowAtlasBytes);
            Assert.Equal(4, rig.Renderer.LastStats.ShadowTiles);
        }
    }
}
