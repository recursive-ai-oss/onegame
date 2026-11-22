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

/// <summary>Layers with their own cameras, text in the draw order, the batch counts that follows, and the cost of screen text.</summary>
public class LayerTests
{
    private sealed class Rig : IDisposable
    {
        private readonly GpuDevice _device;
        private readonly GpuTexture _target;
        private readonly DisplayList _list = new();

        public Rig()
        {
            var definition = new Draw2DGameDefinition();
            var registry = new EntityRegistry();
            definition.RegisterEntities(registry);
            World = new World(1, registry);
            definition.Setup(World);
            var fonts = new FontLibrary();
            definition.DefineFonts(fonts);
            _device = GpuBackends.CreateDevice(new GpuDeviceOptions { Validation = true, RequireValidation = true });
            Stage = new Render2DStage(new HostResources(fonts, new FontTextMeasurer(fonts), new ArtAtlas(), GeneratedShaders.Instance));
            _target = _device.CreateTexture(new TextureDesc(320, 180, GpuFormat.Rgba8UnormSrgb, TextureUsage.RenderTarget | TextureUsage.CopySource));
        }

        public World World { get; }

        public Render2DStage Stage { get; }

        public T Get<T>() where T : Entity => (T)World.Entities.First(e => e is T);

        public FrameStats Frame()
        {
            World.Step();
            Stage.Render(_device, _target, new FrameInfo(320, 180, 1, World.Tick, _list, World, Fixed.Half));
            return Stage.Draw.Renderer!.LastFrame;
        }

        public void Settle()
        {
            for (var i = 0; i < 20; i++)
            {
                Frame();
            }

            _device.WaitIdle();
            Frame();
        }

        public string[] Validation() => _device.TakeValidationMessages().Select(m => m.ToString() ?? "").ToArray();

        public void Dispose()
        {
            _device.WaitIdle();
            Stage.Dispose();
            _target.Dispose();
            _device.Dispose();
        }
    }

    [Fact]
    public void ABatchOfSpritesAndTextThatDoNotInterleaveCostsTheSameDrawsAsBefore()
    {
        using var rig = new Rig();
        rig.Get<Hero>().Go(Fixed.One);
        rig.Settle();
        var hero = rig.Frame();

        // The hero scene is the one the earlier checks drew: sprites first, text last. These counts are what the renderer recorded before text
        // joined the draw order (5 draws, and still 5 with 100 more labels, which join the text batch).
        Console.WriteLine($"hero scene: {hero.DrawCalls} draws, {hero.Passes} passes, {hero.Sprites} sprites, {hero.UiInstances} glyphs");
        Assert.Equal(5, hero.DrawCalls);

        rig.Get<TextLoad>().Set(100, 0);
        rig.Settle();
        var load = rig.Frame();
        Console.WriteLine($"hero scene plus 100 screen texts: {load.DrawCalls} draws, {load.UiInstances} glyphs");
        Assert.Equal(5, load.DrawCalls);
        Assert.Empty(rig.Validation());
    }

    [Fact]
    public void TextAndSpritesThatInterleaveEachNeedTheirOwnDraw()
    {
        using var rig = new Rig();
        rig.Get<Showcase>().Show(2);
        rig.Settle();
        var order = rig.Frame();
        Console.WriteLine($"order scene: {order.DrawCalls} draws, {order.Sprites} sprites, {order.UiInstances} glyphs");

        // Back fill, then four rows: sprite and text, text and sprite, a sprite on a front layer after the lower layer's text, text over
        // a sprite by z; the screen text is the last.
        Assert.True(order.DrawCalls >= 8, $"{order.DrawCalls}");
        Assert.Empty(rig.Validation());
    }

    [Fact]
    public void RepeatedScreenTextOfUnchangedStringsAllocatesNothingAndWarmUpPaysTheGlyphsEarly()
    {
        using var rig = new Rig();
        rig.Get<TextLoad>().Set(100, 0);
        rig.Settle();

        long total = 0;
        for (var i = 0; i < 60; i++)
        {
            rig.World.Step();
            var before = GC.GetAllocatedBytesForCurrentThread();
            rig.Frame();
            total += GC.GetAllocatedBytesForCurrentThread() - before;
        }

        Assert.Equal(102, rig.Stage.Draw.TextCount);
        Assert.True(total == 0, $"60 frames of 100 unchanged screen texts allocated {total} bytes");
        Assert.Empty(rig.Validation());
    }

    [Fact]
    public void ALayerWithItsOwnCameraAndViewportMapsPointsThroughThem()
    {
        var scene = new Scene2D();
        var world = scene.GetLayer("world");
        Assert.Same(world, scene.GetLayer("world"));
        var shared = Camera2D.Default with { Position = new Vector2(100, 50), Zoom = 2 };
        var screen = new Vector2(320, 180);

        // Without a camera of its own a layer follows the scene's.
        var a = Vector2.Transform(new Vector2(100, 50), world.View(shared, screen));
        Assert.Equal(new Vector2(160, 90), a);

        // Its own camera wins, and a viewport centres it in the rectangle.
        world.Camera = Camera2D.Default with { Position = Vector2.Zero, Zoom = 4 };
        world.Viewport = new Rect(200, 10, 100, 80);
        var b = Vector2.Transform(new Vector2(1, 1), world.View(shared, screen));
        Assert.Equal(new Vector2(254, 54), b);

        // A screen-space layer is pixels, origin at the viewport's top-left when it has one.
        var hud = scene.GetLayer("hud");
        hud.ScreenSpace = true;
        Assert.Equal(new Vector2(12, 7), Vector2.Transform(new Vector2(12, 7), hud.View(shared, screen)));
        hud.Viewport = new Rect(200, 10, 100, 80);
        Assert.Equal(new Vector2(212, 17), Vector2.Transform(new Vector2(12, 7), hud.View(shared, screen)));
    }

    [Fact]
    public void WarmUpTextWorksInsideAFrameAndRefusesOutsideOne()
    {
        using var rig = new Rig();
        Assert.Throws<InvalidOperationException>(() => rig.Stage.Draw.WarmUpText("0123", 12));
        rig.Get<TextLoad>().Set(5, 3);
        rig.Settle();
        Assert.Equal(0, TextLoad.WarmRefused);
        Assert.True(TextLoad.Warmed);
        Assert.Empty(rig.Validation());
    }
}
