using System.Numerics;
using Draw2DGame;
using OneGame.Atlas;
using OneGame.Core;
using OneGame.Host;
using OneGame.Render2D;
using OneGame.Text;
using OneGame.UI;
using RecursiveAI.OneGpu;

namespace OneGame.Draw2D.Tests;

public class RenderTargetTests
{
    [Fact]
    public void SteadyFramesWithARenderLayerALayerChainAndAFrameChainAllocateNothing()
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

        PostChain? frameChain = null;
        PostChain? layerChain = null;
        var spins = 0;
        stage.Draw.Describer = (_, d) =>
        {
            frameChain ??= d.CreatePostChain().AddBloom().AddGrade(exposure: 0.1f);
            layerChain ??= d.CreatePostChain().AddBlur(1f);
            var map = d.RenderTexture("map", 64, 64);
            var mapLayer = d.RenderLayer("map", map, new Color(20, 40, 30));
            var world = d.GetLayer("world");
            world.Post = layerChain;
            d.Post = frameChain;

            d.Layer = mapLayer;
            d.FillCircle(new Vector2(32, 32), 10 + (spins % 5), Color.White);
            d.Text("map", new Vector2(32, 52), 12, Color.White, new Vector2(0.5f, 0.5f));
            d.Layer = world;
            d.Sprite(map.Texture, new Vector2(40 + (spins % 30), 0), rotation: spins * 0.05f);
            d.Text("world", new Vector2(0, 60), 14, Color.White, new Vector2(0.5f, 0.5f));
            d.Layer = d.ScreenLayer;
            d.ScreenText("hud", new Vector2(4, 4), 12, Color.White);
            spins++;
        };

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
        for (var i = 0; i < 60; i++)
        {
            world.Step();
            var before = GC.GetAllocatedBytesForCurrentThread();
            stage.Render(device, target, new FrameInfo(320, 180, 1, world.Tick, list, world, Fixed.Half));
            total += GC.GetAllocatedBytesForCurrentThread() - before;
        }

        device.WaitIdle();
        var messages = device.TakeValidationMessages();
        Assert.True(messages.Length == 0, string.Join("\n", messages));
        Assert.True(total == 0, $"60 steady frames allocated {total} bytes");
    }
}
