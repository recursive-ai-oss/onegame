using System.Numerics;
using OneGame.UI;

namespace OneGame.Render2D.Tests;

public class HappyPathTests
{
    [Fact]
    public void ClipsPickFramesFromTimeAndLoopOrHold()
    {
        using var rig = new Rig(64, 64);
        var texture = rig.Renderer.CreateTexture(32, 16, new byte[32 * 16 * 4]);
        var sheet = SpriteSheet.Grid(texture, 8, 8);
        Assert.Equal(8, sheet.FrameCount);
        Assert.Equal(new PixelRect(8, 8, 8, 8), sheet.Frame(5));
        var run = sheet.AddClip("run", 0, 4, 10);
        Assert.Equal([0, 0, 1, 2, 3, 0, 1], new[] { run.FrameAt(-1), run.FrameAt(0.05f), run.FrameAt(0.1f), run.FrameAt(0.25f), run.FrameAt(0.39f), run.FrameAt(0.4f), run.FrameAt(0.55f) });
        var once = sheet.AddClip("once", [6, 7], 2, loop: false);
        Assert.Equal(7, once.FrameAt(99));
        Assert.True(once.IsDone(1) && !once.IsDone(0.9f) && !run.IsDone(99));
        Assert.Same(run, sheet.Clip("run"));
        var ex = Assert.Throws<UnknownClipException>(() => sheet.Clip("jump"));
        Assert.Contains("run", ex.Message);
        Assert.Throws<ArgumentOutOfRangeException>(() => sheet.AddClip("bad", 6, 4, 5));
        Assert.Throws<ArgumentException>(() => SpriteSheet.Grid(texture, 64, 64));
        Assert.Throws<ArgumentException>(() => SpriteSheet.Frames(texture, [new PixelRect(30, 0, 8, 8)]));
    }

    [Fact]
    public void TheCameraFollowsSmoothsAndStaysInsideItsBounds()
    {
        var size = new Vector2(200, 100);
        var cam = new Camera2DRig { Zoom = 2 };
        cam.Follow(new Vector2(10, 20), 0.016f);
        Assert.Equal(new Vector2(10, 20), cam.Position);

        // Screen and world convert both ways, with the camera centred on the follow point.
        Assert.Equal(new Vector2(100, 50), cam.WorldToScreen(new Vector2(10, 20), size));
        Assert.Equal(new Vector2(120, 50), cam.WorldToScreen(new Vector2(20, 20), size));
        var back = cam.ScreenToWorld(new Vector2(130, 70), size);
        Assert.Equal(25, back.X, 3);
        Assert.Equal(30, back.Y, 3);

        // Smoothing closes about 63% of the gap in one time constant, however the time is sliced.
        cam.Smoothing = 0.5f;
        cam.Follow(new Vector2(110, 20), 0.5f);
        Assert.Equal(10 + (100 * (1 - MathF.Exp(-1))), cam.Position.X, 3);
        var a = new Camera2DRig { Smoothing = 0.5f };
        var b = new Camera2DRig { Smoothing = 0.5f };
        a.Snap(default);
        b.Snap(default);
        a.Follow(new Vector2(100, 0), 1);
        for (var i = 0; i < 10; i++)
        {
            b.Follow(new Vector2(100, 0), 0.1f);
        }

        Assert.Equal(a.Position.X, b.Position.X, 2);

        // Bounds keep the whole view inside; a view bigger than the bounds is centred on them.
        var bounded = new Camera2DRig { Zoom = 1, Bounds = new Rect(0, 0, 500, 300) };
        bounded.Follow(new Vector2(-50, 1000), 0);
        Assert.Equal(new Vector2(100, 250), bounded.Current(size).Position);
        bounded.Bounds = new Rect(0, 0, 150, 60);
        Assert.Equal(new Vector2(75, 30), bounded.Current(size).Position);
    }
}
