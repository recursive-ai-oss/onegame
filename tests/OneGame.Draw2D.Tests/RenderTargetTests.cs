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

    // Drawn straight on the renderer from a view: red, then a texture cleared and drawn into, then green and the texture shown. Only the texture may be
    // cleared; the frame keeps what was drawn before and after.
    [Fact]
    public void ABeginTargetInsideAViewClearsOnlyTheTextureAndKeepsTheFrame()
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
        using var target = device.CreateTexture(new TextureDesc(128, 96, GpuFormat.Rgba8UnormSrgb, TextureUsage.RenderTarget | TextureUsage.CopySource));
        var list = new DisplayList();
        Texture2D? white = null;
        stage.Draw.Describer = (_, d) =>
        {
            var r = d.Renderer!;
            white ??= r.CreateTexture(1, 1, [255, 255, 255, 255], TextureFilter.Nearest);
            var tex = d.RenderTexture("t", 128, 96);
            var cam = new Camera2D { Position = new Vector2(64, 48), Zoom = 1 };
            var a = new Scene2D();
            a.Draw(a.DefaultLayer, Sprite.Create(white, new Vector2(20, 48)) with { Scale = new Vector2(40, 96), Tint = new Color(255, 0, 0, 255) });
            r.DrawScene(a, cam);
            r.BeginTarget(tex, new Color(0, 0, 255, 255));
            var inner = new Scene2D();
            inner.Draw(inner.DefaultLayer, Sprite.Create(white, new Vector2(64, 48)) with { Scale = new Vector2(60, 40), Tint = new Color(255, 255, 0, 255) });
            r.DrawScene(inner, cam);
            r.EndTarget();
            var b = new Scene2D();
            b.Draw(b.DefaultLayer, Sprite.Create(white, new Vector2(108, 48)) with { Scale = new Vector2(40, 96), Tint = new Color(0, 255, 0, 255) });
            b.Draw(b.DefaultLayer, Sprite.Create(tex.Texture, new Vector2(20, 70)) with { Scale = new Vector2(0.25f, 0.25f) });
            r.DrawScene(b, cam);
        };

        world.Step();
        stage.Render(device, target, new FrameInfo(128, 96, 1, world.Tick, list, world, Fixed.Half));
        var px = new byte[128 * 96 * 4];
        device.ReadTexture(target, 0, 0, px);
        (int, int, int) At(int x, int y) => (px[((y * 128) + x) * 4], px[(((y * 128) + x) * 4) + 1], px[(((y * 128) + x) * 4) + 2]);
        Assert.Equal((255, 0, 0), At(10, 40));
        Assert.Equal((0, 255, 0), At(118, 50));
        Assert.Equal((255, 255, 0), At(20, 70));
        Assert.Equal((0, 0, 255), At(8, 62));
        var messages = device.TakeValidationMessages();
        Assert.True(messages.Length == 0, string.Join("\n", messages));
    }
}
