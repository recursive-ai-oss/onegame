using OneGame.Core;
using OneGame.Gpu;
using OneGame.Gpu.Vulkan;
using OneGame.Render3D;
using Scene = OneGame.Render3D.Scene3D;

namespace OneGame.Animation.Tests;

public class FrameTests
{
    [Fact]
    public void SteadyFramesOfAnAnimatedCharacterAllocateNothingAndTheValidationLayerStaysSilent()
    {
        var definition = new CharacterGame();
        var registry = new EntityRegistry();
        definition.RegisterEntities(registry);
        var world = new World(1, registry);
        definition.Setup(world);
        var hero = world.Entities.OfType<Hero>().Single();

        using var device = GpuBackends.CreateDevice(new GpuDeviceOptions { Validation = true, RequireValidation = true });
        var renderer = new Renderer3D(device);
        var target = new Offscreen3D(renderer, 320, 180);
        var scene = new Scene { Samples = 1 };
        var view = definition.View;

        void Frame()
        {
            world.Step();
            view.Prepare(world, world.Tick, Fixed.Half);
            view.Build(scene, renderer);
            target.Render(scene);
        }

        // Walk, run, jump and stop on a schedule that repeats, so every kind of blend and fade is warm before the measured frames.
        void Drive(long tick)
        {
            switch (tick % 300)
            {
                case 1: hero.Send.SetGait(1); break;
                case 80: hero.Send.SetGait(2); break;
                case 150: hero.Send.Jump(); break;
                case 230: hero.Send.SetGait(0); break;
            }
        }

        for (var i = 0; i < 400; i++)
        {
            Drive(world.Tick);
            Frame();
        }

        device.WaitIdle();
        Drive(world.Tick);
        Frame();

        long frames = 0;
        for (var i = 0; i < 300; i++)
        {
            Drive(world.Tick);
            world.Step();
            var before = GC.GetAllocatedBytesForCurrentThread();
            view.Prepare(world, world.Tick, Fixed.Half);
            view.Build(scene, renderer);
            target.Render(scene);
            frames += GC.GetAllocatedBytesForCurrentThread() - before;
        }

        device.WaitIdle();
        Assert.Equal(2, scene.Models.Count);
        view.Dispose();
        target.Dispose();
        renderer.Dispose();
        var messages = device.TakeValidationMessages();
        Assert.True(messages.Length == 0, string.Join("\n", messages));
        Assert.True(frames == 0, $"300 steady frames (describe, animate, build, draw) allocated {frames} bytes");
    }
}
