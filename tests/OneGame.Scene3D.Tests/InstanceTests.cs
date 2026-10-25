using OneGame.Core;
using OneGame.Gpu;
using OneGame.Gpu.Vulkan;
using OneGame.Render3D;
using Scene = OneGame.Render3D.Scene3D;
using Scene3DGame;

namespace OneGame.Scene3D.Tests;

public class InstanceTests
{
    [Fact]
    public void ViewsDrawManyCopiesThroughOneBatchWithoutAllocating()
    {
        var definition = new Scene3DGameDefinition();
        var registry = new EntityRegistry();
        definition.RegisterEntities(registry);
        var world = new World(1, registry);
        definition.Setup(world);

        using var device = GpuBackends.CreateDevice(new GpuDeviceOptions { Validation = true, RequireValidation = true });
        var renderer = new Renderer3D(device);
        var target = new Offscreen3D(renderer, 320, 180);
        var scene = new Scene { Samples = 1 };
        var view = definition.View;

        view.Prepare(world, 0, Fixed.Zero);
        view.Build(scene, renderer);
        var before = target.Capture(scene);
        Assert.Empty(scene.Batches);

        world.Add(new Grove()).Init(new FVec3(-5, Fixed.FromRatio(1, 8), -4), 20);
        view.Prepare(world, 0, Fixed.Zero);
        view.Build(scene, renderer);
        Assert.Single(scene.Batches);
        Assert.Equal(400, scene.Batches[0].Count);
        var after = target.Capture(scene);
        Assert.NotEqual(before, after);

        // Copies come and go with the described frame; the batch is reused.
        view.Prepare(world, 1, Fixed.Zero);
        view.Build(scene, renderer);
        Assert.Single(scene.Batches);
        Assert.Equal(400, scene.Batches[0].Count);

        for (var i = 0; i < 12; i++)
        {
            view.Prepare(world, i, Fixed.Half);
            view.Build(scene, renderer);
            target.Render(scene);
        }

        device.WaitIdle();
        view.Prepare(world, 20, Fixed.Half);
        view.Build(scene, renderer);
        target.Render(scene);
        long frames = 0;
        for (var i = 0; i < 40; i++)
        {
            var a0 = GC.GetAllocatedBytesForCurrentThread();
            view.Prepare(world, 30 + i, Fixed.Half);
            view.Build(scene, renderer);
            target.Render(scene);
            frames += GC.GetAllocatedBytesForCurrentThread() - a0;
        }

        device.WaitIdle();
        view.Dispose();
        target.Dispose();
        renderer.Dispose();
        var messages = device.TakeValidationMessages();
        Assert.True(messages.Length == 0, string.Join("\n", messages));
        Assert.True(frames == 0, $"40 steady frames with 400 instances allocated {frames} bytes");
    }
}
