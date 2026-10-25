using OneGame.Core;
using OneGame.Gpu;
using OneGame.Gpu.Vulkan;
using OneGame.Render3D;
using Scene = OneGame.Render3D.Scene3D;
using Scene3DGame;

namespace OneGame.Scene3D.Tests;

public class AllocationTests
{
    [Fact]
    public void SteadyFramesBuiltFromAWorldAllocateNothing()
    {
        var definition = new Scene3DGameDefinition();
        var registry = new EntityRegistry();
        definition.RegisterEntities(registry);
        var world = new World(1, registry);
        definition.Setup(world);

        using var device = GpuBackends.CreateDevice(new GpuDeviceOptions { Validation = true, RequireValidation = true });
        var renderer = new Renderer3D(device);
        var target = new Offscreen3D(renderer, 320, 180);
        var scene = new Scene();
        scene.Samples = 1;
        var view = definition.View;

        void Frame(long tick)
        {
            view.Prepare(world, tick, Fixed.Half);
            view.Build(scene, renderer);
            target.Render(scene);
        }

        for (var i = 0; i < 12; i++)
        {
            world.Step();
            Frame(world.Tick);
        }

        // The first frame after the GPU drains collects the pass timings, which the graph's own tests measure.
        device.WaitIdle();
        world.Step();
        Frame(world.Tick);

        long frames = 0;
        long sim = 0;
        var where = new List<string>();
        for (var i = 0; i < 60; i++)
        {
            var a0 = GC.GetAllocatedBytesForCurrentThread();
            world.Step();
            var a1 = GC.GetAllocatedBytesForCurrentThread();
            Frame(world.Tick);
            var delta = GC.GetAllocatedBytesForCurrentThread() - a1;
            frames += delta;
            sim += a1 - a0;
            if (delta > 0)
            {
                where.Add($"frame {i}: {delta}");
            }
        }

        device.WaitIdle();
        Assert.Equal(4, scene.Models.Count);
        Assert.Equal(2, scene.Lights.Count);
        _ = sim;   // the sim step is the game's own code, measured apart from the frame
        view.Dispose();
        target.Dispose();
        renderer.Dispose();
        var messages = device.TakeValidationMessages();
        Assert.True(messages.Length == 0, string.Join("\n", messages));
        Assert.True(frames == 0, $"60 steady frames (describe, build, draw) allocated {frames} bytes ({string.Join(", ", where)})");
    }
}
