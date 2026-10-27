using Draw2DGame;
using OneGame.Atlas;
using OneGame.Core;
using OneGame.Gpu;
using OneGame.Host;
using OneGame.Render2D;
using OneGame.Text;
using OneGame.UI;

namespace OneGame.Draw2D.Tests;

public class AllocationTests
{
    [Fact]
    public void SteadyFramesOfSpritesShapesAndTextAllocateNothing()
    {
        var definition = new Draw2DGameDefinition();
        var registry = new EntityRegistry();
        definition.RegisterEntities(registry);
        var world = new World(1, registry);
        definition.Setup(world);
        var fonts = new FontLibrary();
        definition.DefineFonts(fonts);

        using var device = GpuBackends.CreateDevice(new GpuDeviceOptions { Validation = true, RequireValidation = true });
        using var stage = new Render2DStage(new HostResources(fonts, new FontTextMeasurer(fonts), new ArtAtlas(), GeneratedShaders.Instance));
        using var target = device.CreateTexture(new TextureDesc(320, 180, GpuFormat.Rgba8UnormSrgb, TextureUsage.RenderTarget | TextureUsage.CopySource));
        var list = new DisplayList();
        var hero = (Hero)world.Entities.First(e => e is Hero);
        hero.Go(Fixed.One);

        void Frame()
        {
            world.Step();
            stage.Render(device, target, new FrameInfo(320, 180, 1, world.Tick, list, world, Fixed.Half));
        }

        for (var i = 0; i < 20; i++)
        {
            Frame();
        }

        device.WaitIdle();
        Frame();

        long total = 0;
        var where = new List<string>();
        for (var i = 0; i < 60; i++)
        {
            world.Step();
            var before = GC.GetAllocatedBytesForCurrentThread();
            stage.Render(device, target, new FrameInfo(320, 180, 1, world.Tick, list, world, Fixed.Half));
            var delta = GC.GetAllocatedBytesForCurrentThread() - before;
            total += delta;
            if (delta > 0)
            {
                where.Add($"frame {i}: {delta}");
            }
        }

        device.WaitIdle();
        Assert.True(stage.Draw.SpriteCount >= 24 && stage.Draw.TextCount == 2);
        var messages = device.TakeValidationMessages();
        Assert.True(messages.Length == 0, string.Join("\n", messages));
        Assert.True(total == 0, $"60 steady frames allocated {total} bytes ({string.Join(", ", where.Take(8))})");
    }
}
