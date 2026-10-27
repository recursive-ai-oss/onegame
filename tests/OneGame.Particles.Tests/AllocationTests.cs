using OneGame.Atlas;
using OneGame.Core;
using OneGame.Gpu;
using OneGame.Gpu.Vulkan;
using OneGame.Host;
using OneGame.Render2D;
using OneGame.Render3D;
using OneGame.Text;
using OneGame.UI;
using ParticleGame;

namespace OneGame.Particles.Tests;

/// <summary>A steady frame with particles allocates nothing, in 2D and in 3D. The emitters are scaled up so around two thousand particles are alive.</summary>
public class AllocationTests
{
    private static World NewWorld(ParticleGameDefinition definition)
    {
        var registry = new EntityRegistry();
        definition.RegisterEntities(registry);
        var world = new World(1, registry);
        definition.Setup(world);
        return world;
    }

    [Fact]
    public void SteadyFramesOfParticlesInTheDraw2DStageAllocateNothing()
    {
        Fx.Scale = 20;
        try
        {
            var definition = new ParticleGameDefinition(false);
            var world = NewWorld(definition);
            var fonts = new FontLibrary();
            definition.DefineFonts(fonts);
            using var device = GpuBackends.CreateDevice(new GpuDeviceOptions { Validation = true, RequireValidation = true });
            using var stage = new Render2DStage(new HostResources(fonts, new FontTextMeasurer(fonts), new ArtAtlas(), GeneratedShaders.Instance));
            using var target = device.CreateTexture(new TextureDesc(480, 270, GpuFormat.Rgba8UnormSrgb, TextureUsage.RenderTarget | TextureUsage.CopySource));
            var list = new DisplayList();

            void Frame()
            {
                world.Step();
                stage.Render(device, target, new FrameInfo(480, 270, 1, world.Tick, list, world, Fixed.Half));
            }

            // Warm-up reaches the peak (storage grows to the most particles seen, once), including a pop.
            for (var i = 0; i < 90; i++)
            {
                if (i == 5)
                {
                    ((Spout)world.Entities.First(e => e is Spout)).Pop();
                }

                Frame();
            }

            device.WaitIdle();
            Frame();
            long total = 0;
            var where = new List<string>();
            for (var i = 0; i < 60; i++)
            {
                world.Step();
                if (i == 30)
                {
                    ((Spout)world.Entities.First(e => e is Spout)).Pop();
                }

                var before = GC.GetAllocatedBytesForCurrentThread();
                stage.Render(device, target, new FrameInfo(480, 270, 1, world.Tick, list, world, Fixed.Half));
                var delta = GC.GetAllocatedBytesForCurrentThread() - before;
                total += delta;
                if (delta > 0)
                {
                    where.Add($"frame {i}: {delta}");
                }
            }

            device.WaitIdle();
            Assert.InRange(stage.Draw.ParticleEmitters.Sum(e => e.Count), 1500, 100000);
            var messages = device.TakeValidationMessages();
            Assert.True(messages.Length == 0, string.Join("\n", messages));
            Assert.True(total == 0, $"60 steady frames with {stage.Draw.ParticleEmitters.Sum(e => e.Count)} particles allocated {total} bytes ({string.Join(", ", where.Take(8))})");
        }
        finally
        {
            Fx.Scale = 1;
        }
    }

    [Fact]
    public void SteadyFramesOfParticlesInThe3DSceneAllocateNothing()
    {
        Fx.Scale = 20;
        try
        {
            var definition = new ParticleGameDefinition(true);
            var world = NewWorld(definition);
            using var device = GpuBackends.CreateDevice(new GpuDeviceOptions { Validation = true, RequireValidation = true });
            var renderer = new Renderer3D(device);
            var target = new Offscreen3D(renderer, 320, 180);
            var scene = new Scene3D { Samples = 1 };
            var view = definition.View!;

            void Frame(long tick)
            {
                view.Prepare(world, tick, Fixed.Half);
                view.Build(scene, renderer);
                target.Render(scene);
            }

            for (var i = 0; i < 90; i++)
            {
                world.Step();
                if (i == 5)
                {
                    ((Spout3D)world.Entities.First(e => e is Spout3D)).Pop();
                }

                Frame(world.Tick);
            }

            device.WaitIdle();
            world.Step();
            Frame(world.Tick);
            long total = 0;
            var where = new List<string>();
            for (var i = 0; i < 60; i++)
            {
                world.Step();
                if (i == 30)
                {
                    ((Spout3D)world.Entities.First(e => e is Spout3D)).Pop();
                }

                var before = GC.GetAllocatedBytesForCurrentThread();
                Frame(world.Tick);
                var delta = GC.GetAllocatedBytesForCurrentThread() - before;
                total += delta;
                if (delta > 0)
                {
                    where.Add($"frame {i}: {delta}");
                }
            }

            device.WaitIdle();
            var live = view.ParticleEmitters.Sum(e => e.Count);
            Assert.InRange(live, 1500, 100000);
            Assert.Equal(live, renderer.LastStats.Billboards);
            view.Dispose();
            target.Dispose();
            renderer.Dispose();
            var messages = device.TakeValidationMessages();
            Assert.True(messages.Length == 0, string.Join("\n", messages));
            Assert.True(total == 0, $"60 steady frames with {live} particles allocated {total} bytes ({string.Join(", ", where.Take(8))})");
        }
        finally
        {
            Fx.Scale = 1;
        }
    }
}
