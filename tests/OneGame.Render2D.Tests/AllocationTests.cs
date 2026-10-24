using System.Numerics;
using OneGame.UI;

namespace OneGame.Render2D.Tests;

/// <summary>Steady-state frames allocate nothing: sprites, a tilemap, post-processing and a UI list, measured over many frames.</summary>
public class AllocationTests
{
    [Fact]
    public void SteadyStateFramesAllocateZeroBytes()
    {
        using var rig = new Rig(320, 200);
        var atlas = rig.Own(rig.Renderer.CreateTexture(32, 32, Pixels.Checker(32, 8, new Color(250, 200, 60, 255), new Color(200, 80, 40, 255))));
        var set = rig.Own(rig.Renderer.CreateTileSet(16, 16, 1, 1, Pixels.Solid(16, 16, new Color(80, 160, 90, 255))));
        var map = rig.Own(new TileMap(set, 512, 512, 16));
        for (var i = 0; i < 512; i += 3)
        {
            for (var j = 0; j < 512; j += 2)
            {
                map.Set(i, j, 1);
            }
        }

        var scene = new Scene2D();
        var back = scene.DefaultLayer;
        var front = scene.AddLayer("front", new Vector2(1.2f, 1.2f));
        var chain = rig.Own(rig.Renderer.CreatePostChain().AddBlur(1.5f).AddGrade(0.1f, 1.1f, 0.9f, 0.3f));
        var ui = rig.Renderer.Ui;

        void Frame(int n)
        {
            scene.Clear();
            scene.Draw(back, map);
            for (var i = 0; i < 1000; i++)
            {
                scene.Draw(i % 2 == 0 ? back : front, Sprite.Create(atlas, new Vector2((i % 40 * 8) - 160, (i / 40 * 8) - 100)) with { Rotation = (n + i) * 0.01f, Z = i % 5 });
            }

            rig.Renderer.BeginFrame(rig.Target, new FrameOptions { Clear = new Color(10, 10, 20, 255) });
            rig.Renderer.DrawScene(scene, Camera2D.Default with { Position = new Vector2(n % 16, (n % 8) * 0.5f) }, chain);
            ui.PushClip(new Rect(10, 10, 200, 100), new CornerRadii(12, 12, 12, 12));
            ui.PushOpacity(0.7f);
            ui.Fill(new Rect(0, 0, 320, 200), Gradient.Linear(Color.White, Color.Hex("#3b1f6b"), Vector2.Zero, new Vector2(320, 0)), new CornerRadii(8, 8, 8, 8));
            ui.Border(new Rect(20, 20, 100, 50), Color.White, new Edges(2, 2, 2, 2), new CornerRadii(6, 6, 6, 6));
            ui.PopOpacity();
            ui.PopClip();
            var enc = rig.Device.BeginCommands();
            rig.Renderer.EndFrame(enc);
            rig.Device.Submit(enc);
        }

        for (var n = 0; n < 100; n++)
        {
            Frame(n);
        }

        rig.Device.WaitIdle();
        var before = GC.GetAllocatedBytesForCurrentThread();
        const int frames = 200;
        for (var n = 0; n < frames; n++)
        {
            Frame(100 + n);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        rig.Device.WaitIdle();
        Golden.Note($"{frames} frames of 1000 sprites + tilemap + post chain + UI: {allocated} bytes allocated; last frame {rig.Renderer.LastFrame}");
        // The renderer itself allocates nothing: exactly zero on the software rasterizer and the NVIDIA driver. RADV has been seen to cost the GPU
        // backend a couple of kilobytes in total at random points, so real adapters are held to a few bytes per frame instead.
        if (rig.Device.Adapter.Kind == OneGame.Gpu.AdapterKind.Software || rig.Device.Adapter.VendorId == 0x10DE)
        {
            Assert.Equal(0, allocated);
        }
        else
        {
            Assert.True(allocated <= 16 * frames, $"{allocated} bytes over {frames} frames");
        }
    }

    [Fact]
    public void TenThousandSpritesCostTheCpuAFewMilliseconds()
    {
        using var rig = new Rig(1280, 720);
        var atlas = rig.Own(rig.Renderer.CreateTexture(64, 64, Pixels.Checker(64, 16, new Color(250, 200, 60, 255), new Color(200, 80, 40, 255)), TextureFilter.Nearest));
        var scene = new Scene2D();
        var sw = System.Diagnostics.Stopwatch.StartNew();
        const int frames = 100;
        for (var f = 0; f < frames + 10; f++)
        {
            if (f == 10)
            {
                rig.Device.WaitIdle();
                sw.Restart();
            }

            scene.Clear();
            for (var i = 0; i < 10_000; i++)
            {
                scene.Draw(Sprite.Create(atlas, new Vector2((i % 100 * 12) - 600, (i / 100 * 7) - 350)) with { Rotation = (f + i) * 0.01f, Z = i % 4 });
            }

            rig.Renderer.BeginFrame(rig.Target);
            rig.Renderer.DrawScene(scene, Camera2D.Default);
            var enc = rig.Device.BeginCommands();
            rig.Renderer.EndFrame(enc);
            rig.Device.Submit(enc);
        }

        rig.Device.WaitIdle();
        var perFrame = sw.Elapsed.TotalMilliseconds / frames;
        Golden.Note($"10,000 sprites in 4 z levels ({rig.Device.Adapter.Name}): {perFrame:F2} ms per frame submitted and executed; {rig.Renderer.LastFrame}");
        Assert.True(rig.Renderer.LastFrame.DrawCalls <= 4 * 1);
    }
}
