using System.Numerics;
using OneGame.Render2D.Tests.Shaders;
using OneGame.UI;

namespace OneGame.Render2D.Tests;

/// <summary>Render textures, bloom, passes written by the game, and chains on one layer.</summary>
public class RenderTargetTests
{
    private static readonly Color Dark = new(10, 12, 20, 255);

    private static ShaderCode InvertShader() => new(RecursiveAI.OneGpu.ShaderStage.Pixel, Invert.PSMain.EntryPoint, Invert.PSMain.SpirV.ToArray(), Invert.PSMain.Msl.ToArray(), Invert.PSMain.MslFunctionName, Invert.PSMain.Layout);

    private static Texture2D White(Rig rig) => rig.Own(rig.Renderer.CreateTexture(1, 1, [255, 255, 255, 255], TextureFilter.Nearest));

    [Fact]
    public void ARenderTextureDrawnIntoShowsAsASprite()
    {
        using var rig = new Rig(128, 96);
        var white = White(rig);
        var map = rig.Own(rig.Renderer.CreateRenderTexture(32, 32));
        var px = rig.Render(() =>
        {
            rig.Renderer.BeginTarget(map, new Color(0, 0, 255, 255));
            var inner = new Scene2D();
            inner.Draw(inner.DefaultLayer, Sprite.Create(white, new Vector2(16, 16)) with { Scale = new Vector2(8, 8), Tint = new Color(255, 0, 0, 255) });
            rig.Renderer.DrawScene(inner, new Camera2D { Position = new Vector2(16, 16), Zoom = 1 });
            rig.Renderer.EndTarget();

            // Shown twice as large, left of centre: the red square in the middle of a blue field.
            var outer = new Scene2D();
            outer.Draw(outer.DefaultLayer, Sprite.Create(map.Texture, new Vector2(40, 48)) with { Scale = new Vector2(2, 2) });
            rig.Renderer.DrawScene(outer, new Camera2D { Position = new Vector2(64, 48), Zoom = 1 });
        }, Dark);
        Assert.Equal((255, 0, 0, 255), Pixels.At(px, 128, 40, 48));
        Assert.Equal((0, 0, 255, 255), Pixels.At(px, 128, 12, 48));
        Assert.Equal((10, 12, 20, 255), Pixels.At(px, 128, 100, 48));
    }

    [Fact]
    public void ARenderTextureThatWasOnlyClearedIsClearedBeforeItIsShown()
    {
        using var rig = new Rig(64, 48);
        var tex = rig.Own(rig.Renderer.CreateRenderTexture(16, 16));
        var px = rig.Render(() =>
        {
            rig.Renderer.BeginTarget(tex, new Color(255, 0, 0, 255));
            rig.Renderer.EndTarget();
            var scene = new Scene2D();
            scene.Draw(scene.DefaultLayer, Sprite.Create(tex.Texture, new Vector2(32, 24)));
            rig.Renderer.DrawScene(scene, new Camera2D { Position = new Vector2(32, 24), Zoom = 1 });
        }, Dark);
        Assert.Equal((255, 0, 0, 255), Pixels.At(px, 64, 32, 24));
    }

    [Fact]
    public void ALayerAimedAtARenderTextureDrawsThereAndKeepsItWhenIdle()
    {
        using var rig = new Rig(96, 64);
        var white = White(rig);
        var tex = rig.Own(rig.Renderer.CreateRenderTexture(16, 16));
        var scene = new Scene2D();
        var cache = scene.AddLayer("cache", Vector2.One);
        cache.Target = tex;
        cache.ScreenSpace = true;
        cache.TargetClear = new Color(0, 200, 0, 255);
        var main = scene.AddLayer("main", Vector2.One);
        var cam = new Camera2D { Position = new Vector2(48, 32), Zoom = 1 };

        byte[] Frame(bool redraw)
        {
            scene.Clear();
            if (redraw)
            {
                scene.Draw(cache, Sprite.Create(white, new Vector2(8, 8)) with { Scale = new Vector2(4, 4), Tint = new Color(255, 255, 0, 255) });
            }

            scene.Draw(main, Sprite.Create(tex.Texture, new Vector2(48, 32)));
            return rig.Render(() => rig.Renderer.DrawScene(scene, cam), Dark);
        }

        var first = Frame(true);
        Assert.Equal((255, 255, 0, 255), Pixels.At(first, 96, 48, 32));
        Assert.Equal((0, 200, 0, 255), Pixels.At(first, 96, 41, 25));
        Assert.Equal(2, rig.Stats.Passes);

        // Nothing drawn into the layer this frame: the texture is not touched, so the picture is the same and costs one pass.
        var second = Frame(false);
        Assert.Equal(first, second);
        Assert.Equal(1, rig.Stats.Passes);
    }

    [Fact]
    public void DrawingARenderTextureIntoItselfIsRefused()
    {
        using var rig = new Rig(64, 64);
        var tex = rig.Own(rig.Renderer.CreateRenderTexture(16, 16));
        rig.Renderer.BeginFrame(rig.Target);
        rig.Renderer.BeginTarget(tex);
        var scene = new Scene2D();
        scene.Draw(scene.DefaultLayer, Sprite.Create(tex.Texture, new Vector2(8, 8)));
        Assert.Throws<InvalidOperationException>(() => rig.Renderer.DrawScene(scene, Camera2D.Default));
        Assert.Throws<InvalidOperationException>(() => rig.Renderer.BeginTarget(tex));
        rig.Renderer.CancelFrame();
        Assert.Throws<InvalidOperationException>(() => rig.Renderer.EndTarget());
    }

    [Fact]
    public void BloomAddsAHaloAroundBrightThingsAndLeavesDimOnesAlone()
    {
        using var rig = new Rig(128, 96);
        var white = White(rig);
        var scene = new Scene2D();
        scene.Draw(scene.DefaultLayer, Sprite.Create(white, new Vector2(40, 48)) with { Scale = new Vector2(16, 16) });
        scene.Draw(scene.DefaultLayer, Sprite.Create(white, new Vector2(100, 48)) with { Scale = new Vector2(16, 16), Tint = new Color(40, 40, 60, 255) });
        var cam = new Camera2D { Position = new Vector2(64, 48), Zoom = 1 };
        var plain = rig.Render(() => rig.Renderer.DrawScene(scene, cam), Dark);
        using var chain = rig.Renderer.CreatePostChain().AddBloom();
        var lit = rig.Render(() => rig.Renderer.DrawScene(scene, cam, chain), Dark);
        var bloomPasses = rig.Stats.Passes;

        // Inside the bright square, and on the dim one, nothing changes; just outside the bright one the glow raises the dark background.
        Assert.Equal(Pixels.At(plain, 128, 40, 48), Pixels.At(lit, 128, 40, 48));
        Assert.Equal(Pixels.At(plain, 128, 100, 48), Pixels.At(lit, 128, 100, 48));
        var near = Pixels.At(lit, 128, 52, 48);
        Assert.True(near.Item1 > 40 && near.Item3 > 40, $"no halo beside the bright square: {near}");
        Assert.True(Pixels.At(lit, 128, 80, 48).Item1 < 20, "the halo reaches too far");
        Assert.Equal(Pixels.At(plain, 128, 112, 90), Pixels.At(lit, 128, 112, 90));
        Golden.Note($"bloom frame: {bloomPasses} render passes");
    }

    [Fact]
    public void AGamePassRunsOverTheFrameAndSeesItsParameters()
    {
        using var rig = new Rig(64, 48);
        var white = White(rig);
        var invert = rig.Own(rig.Renderer.CreateMaterial(InvertShader(), "invert"));
        invert.SetParameter(0, new Vector4(1, 0, 0, 0));
        using var chain = rig.Renderer.CreatePostChain().AddMaterial(invert);
        var scene = new Scene2D();
        scene.Draw(scene.DefaultLayer, Sprite.Create(white, new Vector2(32, 24)) with { Scale = new Vector2(10, 10), Tint = new Color(255, 0, 0, 255) });
        var cam = new Camera2D { Position = new Vector2(32, 24), Zoom = 1 };
        var px = rig.Render(() => rig.Renderer.DrawScene(scene, cam, chain), new Color(0, 0, 0, 255));
        Assert.Equal((0, 255, 255, 255), Pixels.At(px, 64, 32, 24));
        Assert.Equal((255, 255, 255, 255), Pixels.At(px, 64, 5, 5));

        invert.SetParameter(0, new Vector4(0, 0, 0, 0));
        px = rig.Render(() => rig.Renderer.DrawScene(scene, cam, chain), new Color(0, 0, 0, 255));
        Assert.Equal((255, 0, 0, 255), Pixels.At(px, 64, 32, 24));
        Assert.Equal((0, 0, 0, 255), Pixels.At(px, 64, 5, 5));
    }

    [Fact]
    public void AChainOnOneLayerLeavesTheLayersAboveItSharp()
    {
        using var rig = new Rig(64, 48);
        var white = White(rig);
        var invert = rig.Own(rig.Renderer.CreateMaterial(InvertShader(), "invert"));
        invert.SetParameter(0, new Vector4(1, 0, 0, 0));
        using var chain = rig.Renderer.CreatePostChain().AddMaterial(invert);
        var scene = new Scene2D();
        var below = scene.DefaultLayer;
        var above = scene.AddLayer("above", Vector2.One);
        below.Post = chain;
        scene.Draw(below, Sprite.Create(white, new Vector2(24, 24)) with { Scale = new Vector2(20, 20), Tint = new Color(255, 0, 0, 255) });
        scene.Draw(above, Sprite.Create(white, new Vector2(34, 24)) with { Scale = new Vector2(12, 12), Tint = new Color(0, 255, 0, 255) });
        var cam = new Camera2D { Position = new Vector2(32, 24), Zoom = 1 };
        var px = rig.Render(() => rig.Renderer.DrawScene(scene, cam), new Color(0, 0, 0, 255));

        // The lower layer's red square is inverted to cyan and sits on the untouched black (a layer's own image is transparent where nothing is
        // drawn, so the chain leaves the background alone); the upper layer's green square is not inverted.
        Assert.Equal((0, 255, 255, 255), Pixels.At(px, 64, 20, 24));
        Assert.Equal((0, 255, 0, 255), Pixels.At(px, 64, 36, 24));
        Assert.Equal((0, 0, 0, 255), Pixels.At(px, 64, 5, 5));
    }

    [Fact]
    public void NoTargetAndNoChainAddsNoPass()
    {
        using var rig = new Rig(64, 48);
        var white = White(rig);
        using var empty = rig.Renderer.CreatePostChain();
        var scene = new Scene2D();
        scene.Draw(scene.DefaultLayer, Sprite.Create(white, new Vector2(32, 24)) with { Scale = new Vector2(10, 10) });
        var cam = new Camera2D { Position = new Vector2(32, 24), Zoom = 1 };
        var plain = rig.Render(() => rig.Renderer.DrawScene(scene, cam), Dark);
        Assert.Equal(1, rig.Stats.Passes);
        var withEmpty = rig.Render(() => rig.Renderer.DrawScene(scene, cam, empty), Dark);
        Assert.Equal(1, rig.Stats.Passes);
        Assert.Equal(plain, withEmpty);
    }

    [Fact]
    public void SteadyFramesWithATargetAndATwoPassChainAllocateNothing()
    {
        using var rig = new Rig(160, 120);
        var white = White(rig);
        var tex = rig.Own(rig.Renderer.CreateRenderTexture(48, 48));
        var invert = rig.Own(rig.Renderer.CreateMaterial(InvertShader(), "invert"));
        invert.SetParameter(0, new Vector4(0.5f, 0, 0, 0));
        using var chain = rig.Renderer.CreatePostChain().AddBloom().AddMaterial(invert);
        var scene = new Scene2D();
        var cache = scene.AddLayer("cache", Vector2.One);
        cache.Target = tex;
        cache.ScreenSpace = true;
        var main = scene.AddLayer("main", Vector2.One);
        var cam = new Camera2D { Position = new Vector2(80, 60), Zoom = 1 };

        void Frame(int n)
        {
            scene.Clear();
            scene.Draw(cache, Sprite.Create(white, new Vector2(24, 24)) with { Scale = new Vector2(20, 20), Rotation = n * 0.05f });
            scene.Draw(main, Sprite.Create(tex.Texture, new Vector2(40 + (n % 20), 60)));
            rig.Renderer.BeginFrame(rig.Target, new FrameOptions { Clear = Dark });
            rig.Renderer.DrawScene(scene, cam, chain);
            var enc = rig.Device.BeginCommands();
            rig.Renderer.EndFrame(enc);
            rig.Device.Submit(enc);
        }

        for (var n = 0; n < 60; n++)
        {
            Frame(n);
        }

        rig.Device.WaitIdle();
        var before = GC.GetAllocatedBytesForCurrentThread();
        const int frames = 100;
        for (var n = 0; n < frames; n++)
        {
            Frame(60 + n);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        rig.Device.WaitIdle();
        Golden.Note($"{frames} frames with a render texture, bloom and a game pass: {allocated} bytes allocated; last frame {rig.Renderer.LastFrame}");
        if (rig.Device.Adapter.Kind == RecursiveAI.OneGpu.AdapterKind.Software || rig.Device.Adapter.VendorId == 0x10DE)
        {
            Assert.Equal(0, allocated);
        }
        else
        {
            Assert.True(allocated <= 16 * frames, $"{allocated} bytes over {frames} frames");
        }
    }
}
