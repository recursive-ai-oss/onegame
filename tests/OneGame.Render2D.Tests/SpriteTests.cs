using System.Numerics;
using System.Runtime.CompilerServices;
using OneGame.Render2D.Tests.Shaders;
using OneGame.UI;

namespace OneGame.Render2D.Tests;

/// <summary>Sprites, layers, parallax, materials and post-processing, drawn offscreen with the validation layer on.</summary>
public class SpriteTests
{
    [Fact]
    public void InstanceStructsMatchTheVertexLayouts()
    {
        Assert.Equal(48, Unsafe.SizeOf<QuadInstance>());
        Assert.Equal(128, Unsafe.SizeOf<UiInstance>());
    }

    [Fact]
    public void OneSpriteLandsWhereTheCameraSaysAndTintsLinearly()
    {
        using var rig = new Rig(64, 64);
        var tex = rig.Own(rig.Renderer.CreateTexture(8, 8, Pixels.Solid(8, 8, new Color(255, 255, 255, 255)), TextureFilter.Nearest));
        var scene = new Scene2D();
        // Centre of the screen is the camera position; an 8x8 sprite at (10, 0) covers screen x 38..46, y 28..36.
        scene.Draw(Sprite.Create(tex, new Vector2(10, 0)) with { Tint = new Color(255, 0, 0, 255) });
        var px = rig.Render(() => rig.Renderer.DrawScene(scene, Camera2D.Default), new Color(0, 0, 0, 255));
        Assert.Equal((255, 0, 0, 255), Pixels.At(px, 64, 41, 31));
        Assert.Equal((0, 0, 0, 255), Pixels.At(px, 64, 37, 31));
        Assert.Equal((0, 0, 0, 255), Pixels.At(px, 64, 41, 27));
    }

    [Fact]
    public void HalfTransparentWhiteOverBlackIsTheLinearMidpointEncoded()
    {
        using var rig = new Rig(16, 16);
        var tex = rig.Own(rig.Renderer.CreateTexture(1, 1, [255, 255, 255, 255], TextureFilter.Nearest));
        var scene = new Scene2D();
        scene.Draw(Sprite.Create(tex, Vector2.Zero) with { Scale = new Vector2(16, 16), Tint = new Color(255, 255, 255, 128) });
        var px = rig.Render(() => rig.Renderer.DrawScene(scene, Camera2D.Default), new Color(0, 0, 0, 255));
        // 128/255 of linear white is 0.502, which encodes to sRGB 188: blending in linear light, not in encoded values.
        var (r, _, _, a) = Pixels.At(px, 16, 8, 8);
        Assert.InRange(r, 186, 190);
        Assert.Equal(255, a);
    }

    private static Scene2D BuildSpriteScene(Rig rig)
    {
        var disc = rig.Own(rig.Renderer.CreateTexture(32, 32, Pixels.Disc(32, new Color(240, 180, 40, 255), ring: 3)));
        var square = rig.Own(rig.Renderer.CreateTexture(32, 32, Pixels.Checker(32, 8, new Color(60, 160, 240, 255), new Color(20, 60, 120, 255)), TextureFilter.Nearest));
        var scene = new Scene2D();
        var back = scene.DefaultLayer;
        var front = scene.AddLayer("front", Vector2.One);
        // The checker is drawn after the discs but has a lower z, so it must still be behind them.
        scene.Draw(back, Sprite.Create(disc, new Vector2(-40, -20)) with { Scale = new Vector2(2, 2), Z = 1 });
        scene.Draw(back, Sprite.Create(square, new Vector2(-20, -10)) with { Scale = new Vector2(2, 2), Rotation = 0.5f, Z = 0 });
        scene.Draw(back, Sprite.Create(square, new Vector2(60, 30)) with { Rotation = -0.3f, Tint = new Color(255, 255, 255, 140), Flip = SpriteFlip.Horizontal });
        scene.Draw(front, Sprite.Create(disc, new Vector2(10, 20)) with { Scale = new Vector2(1.5f, 1.5f), Tint = new Color(255, 120, 120, 200) });
        scene.Draw(front, Sprite.Create(disc, new Vector2(30, 10)) with { Source = new PixelRect(0, 0, 16, 32), Scale = new Vector2(2, 2), Pivot = Vector2.Zero });
        return scene;
    }

    [Fact]
    public void SpriteSceneWithLayersRotationAndAlphaMatchesTheReference()
    {
        using var rig = new Rig(256, 192);
        var scene = BuildSpriteScene(rig);
        var px = rig.Render(() => rig.Renderer.DrawScene(scene, Camera2D.Default), new Color(24, 28, 40, 255));
        Golden.Check("sprites", 256, 192, px);
        Assert.Equal(5, rig.Stats.Sprites);
        // Two textures, interleaved by z: disc(z1) is after square(z0), so the batches are square, disc, square, disc, disc: no merging across textures.
        Assert.True(rig.Stats.DrawCalls <= 5);
    }

    [Fact]
    public void TenThousandSpritesFromOneTextureTakeOneDrawCall()
    {
        using var rig = new Rig(512, 512);
        var atlas = rig.Own(rig.Renderer.CreateTexture(64, 64, Pixels.Checker(64, 16, new Color(250, 200, 60, 255), new Color(200, 80, 40, 255)), TextureFilter.Nearest));
        var scene = new Scene2D();
        for (var i = 0; i < 10_000; i++)
        {
            var src = new PixelRect((i % 4) * 16, ((i / 4) % 4) * 16, 16, 16);
            scene.Draw(Sprite.Create(atlas, new Vector2((i % 100 * 5) - 250, (i / 100 * 5) - 250)) with { Source = src, Scale = new Vector2(0.3f, 0.3f), Z = i % 3 });
        }

        var px = rig.Render(() => rig.Renderer.DrawScene(scene, Camera2D.Default), new Color(0, 0, 0, 255));
        Assert.Equal(10_000, rig.Stats.Sprites);
        Assert.Equal(1, rig.Stats.DrawCalls);
        Assert.Equal(1, rig.Stats.Passes);
        Assert.NotEqual((0, 0, 0, 255), Pixels.At(px, 512, 256, 256));
        // A second scene where z alternates between two textures keeps order, so it needs one draw per run of equal texture.
        var other = rig.Own(rig.Renderer.CreateTexture(8, 8, Pixels.Solid(8, 8, new Color(10, 200, 90, 255))));
        scene.Clear();
        for (var i = 0; i < 10; i++)
        {
            scene.Draw(Sprite.Create(i % 2 == 0 ? atlas : other, new Vector2(i * 10, 0)));
        }

        rig.Render(() => rig.Renderer.DrawScene(scene, Camera2D.Default));
        Assert.Equal(10, rig.Stats.DrawCalls);
    }

    [Fact]
    public void OffscreenSpritesAreCulled()
    {
        using var rig = new Rig(64, 64);
        var tex = rig.Own(rig.Renderer.CreateTexture(8, 8, Pixels.Solid(8, 8, Color.White)));
        var scene = new Scene2D();
        scene.Draw(Sprite.Create(tex, new Vector2(1000, 0)));
        scene.Draw(Sprite.Create(tex, new Vector2(0, 0)));
        rig.Render(() => rig.Renderer.DrawScene(scene, Camera2D.Default));
        Assert.Equal(1, rig.Stats.Sprites);
        Assert.Equal(1, rig.Stats.SpritesCulled);
    }

    /// <summary>Finds the centre of the pixels that are close to a colour.</summary>
    private static Vector2 Centroid(byte[] px, int w, int h, (byte R, byte G, byte B) c)
    {
        double sx = 0, sy = 0, n = 0;
        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                var (r, g, b, _) = Pixels.At(px, w, x, y);
                if (Math.Abs(r - c.R) < 8 && Math.Abs(g - c.G) < 8 && Math.Abs(b - c.B) < 8)
                {
                    sx += x + 0.5;
                    sy += y + 0.5;
                    n++;
                }
            }
        }

        return n == 0 ? new Vector2(float.NaN) : new Vector2((float)(sx / n), (float)(sy / n));
    }

    [Fact]
    public void ParallaxLayersFollowTheCameraByTheirFactor()
    {
        using var rig = new Rig(200, 100);
        var marker = rig.Own(rig.Renderer.CreateTexture(10, 10, Pixels.Solid(10, 10, Color.White), TextureFilter.Nearest));
        var scene = new Scene2D();
        var far = scene.DefaultLayer;
        far.Parallax = new Vector2(0.25f, 0.25f);
        var mid = scene.AddLayer("mid", new Vector2(0.5f, 0.5f));
        var near = scene.AddLayer("near", Vector2.One);
        scene.Draw(far, Sprite.Create(marker, new Vector2(0, 0)) with { Tint = new Color(255, 0, 0, 255) });
        scene.Draw(mid, Sprite.Create(marker, new Vector2(0, 20)) with { Tint = new Color(0, 255, 0, 255) });
        scene.Draw(near, Sprite.Create(marker, new Vector2(0, -20)) with { Tint = new Color(0, 0, 255, 255) });
        var camera = Camera2D.Default with { Position = new Vector2(40, 0) };
        var px = rig.Render(() => rig.Renderer.DrawScene(scene, camera), new Color(0, 0, 0, 255));
        Golden.Check("parallax", 200, 100, px);

        // Screen x = 100 + (world x - camera x * factor): far at 100 - 10, mid at 100 - 20, near at 100 - 40.
        Assert.Equal(90f, Centroid(px, 200, 100, (255, 0, 0)).X, 0.01f);
        Assert.Equal(80f, Centroid(px, 200, 100, (0, 255, 0)).X, 0.01f);
        Assert.Equal(60f, Centroid(px, 200, 100, (0, 0, 255)).X, 0.01f);
    }

    [Fact]
    public void CameraZoomAndRotationMoveSpritesAsDocumented()
    {
        using var rig = new Rig(200, 200);
        var marker = rig.Own(rig.Renderer.CreateTexture(4, 4, Pixels.Solid(4, 4, Color.White), TextureFilter.Nearest));
        var scene = new Scene2D();
        scene.Draw(Sprite.Create(marker, new Vector2(20, 0)) with { Tint = new Color(255, 0, 0, 255) });
        // Zoom 2 doubles the distance from the centre: x = 100 + 40.
        var px = rig.Render(() => rig.Renderer.DrawScene(scene, Camera2D.Default with { Zoom = 2 }), new Color(0, 0, 0, 255));
        Assert.Equal(140f, Centroid(px, 200, 200, (255, 0, 0)).X, 0.01f);
        // A camera turned a quarter turn clockwise sees the world turned counter-clockwise: a point to the right appears above the centre.
        px = rig.Render(() => rig.Renderer.DrawScene(scene, Camera2D.Default with { Rotation = MathF.PI / 2 }), new Color(0, 0, 0, 255));
        var c = Centroid(px, 200, 200, (255, 0, 0));
        Assert.Equal(100f, c.X, 0.05f);
        Assert.Equal(80f, c.Y, 0.05f);
    }

    [Fact]
    public void PixelSnapKeepsSpritesOnWholePixels()
    {
        using var rig = new Rig(64, 64);
        var tex = rig.Own(rig.Renderer.CreateTexture(4, 4, Pixels.Solid(4, 4, Color.White), TextureFilter.Linear));
        var scene = new Scene2D();
        scene.Draw(Sprite.Create(tex, new Vector2(0.4f, 0.4f)));
        var px = rig.Render(() => rig.Renderer.DrawScene(scene, Camera2D.Default with { PixelSnap = true }), new Color(0, 0, 0, 255));
        // The sprite covers x,y 30..34 exactly: no half-covered edge pixels.
        Assert.Equal((255, 255, 255, 255), Pixels.At(px, 64, 30, 30));
        Assert.Equal((0, 0, 0, 255), Pixels.At(px, 64, 29, 30));
        Assert.Equal((255, 255, 255, 255), Pixels.At(px, 64, 33, 33));
        Assert.Equal((0, 0, 0, 255), Pixels.At(px, 64, 34, 33));
    }

    [Fact]
    public void CustomMaterialShadesSpritesAndParametersChangeTheResult()
    {
        using var rig = new Rig(128, 96);
        var tex = rig.Own(rig.Renderer.CreateTexture(32, 32, Pixels.Checker(32, 4, new Color(255, 255, 255, 255), new Color(90, 90, 200, 255)), TextureFilter.Linear));
        var ripple = rig.Own(rig.Renderer.CreateMaterial(new ShaderCode(OneGame.Gpu.ShaderStage.Pixel, Ripple.PSMain.EntryPoint, Ripple.PSMain.SpirV.ToArray(), Ripple.PSMain.Msl.ToArray(), Ripple.PSMain.MslFunctionName, Ripple.PSMain.Layout), "ripple"));
        ripple.SetParameter(0, new Vector4(0.06f, 3f, 0.25f, 0));
        ripple.SetParameter(1, new Vector4(0.9f, 0.2f, 0.1f, 0));
        var scene = new Scene2D();
        scene.Draw(Sprite.Create(tex, new Vector2(-32, 0)) with { Scale = new Vector2(2, 2) });
        scene.Draw(Sprite.Create(tex, new Vector2(32, 0)) with { Scale = new Vector2(2, 2), Material = ripple });
        var px = rig.Render(() => rig.Renderer.DrawScene(scene, Camera2D.Default), new Color(20, 20, 20, 255));
        Golden.Check("material", 128, 96, px);
        var plain = Pixels.At(px, 128, 20, 20);
        var shaded = Pixels.At(px, 128, 84, 20);
        Assert.NotEqual(plain, shaded);

        ripple.SetParameter(2, new Vector4(0, 0, 0, 0));
        ripple.SetParameter(0, new Vector4(0, 3, 0, 0));
        var again = rig.Render(() => rig.Renderer.DrawScene(scene, Camera2D.Default), new Color(20, 20, 20, 255));
        // With no wave and no mix the material draws what the plain shader does: the two halves are mirror-equal.
        Assert.Equal(Pixels.At(again, 128, 20, 20), Pixels.At(again, 128, 84, 20));
    }

    [Fact]
    public void BlurAndGradePassesChangeTheFrame()
    {
        using var rig = new Rig(160, 120);
        var scene = BuildSpriteScene(rig);
        using var chain = rig.Renderer.CreatePostChain().AddBlur(2f).AddGrade(exposure: 0.2f, contrast: 1.2f, saturation: 0.6f, vignette: 0.5f, tint: new Color(255, 220, 180, 255), tintAmount: 0.4f);
        var sharp = rig.Render(() => rig.Renderer.DrawScene(scene, Camera2D.Default with { Zoom = 0.6f }), new Color(24, 28, 40, 255));
        var post = rig.Render(() => rig.Renderer.DrawScene(scene, Camera2D.Default with { Zoom = 0.6f }, chain), new Color(24, 28, 40, 255));
        Golden.Check("post", 160, 120, post);
        // Scene, two blur passes, grade.
        Assert.Equal(4, rig.Stats.Passes);
        // Blur lowers the local contrast: the sum of absolute neighbour differences, relative to brightness, drops.
        using var blurOnly = rig.Renderer.CreatePostChain().AddBlur(2f);
        var blurred = rig.Render(() => rig.Renderer.DrawScene(scene, Camera2D.Default with { Zoom = 0.6f }, blurOnly), new Color(24, 28, 40, 255));
        Assert.True(Roughness(blurred) < Roughness(sharp) * 0.8, $"{Roughness(blurred)} vs {Roughness(sharp)}");
    }

    private static double Roughness(byte[] px)
    {
        double sum = 0;
        double lum = 0;
        for (var i = 0; i + 8 < px.Length; i += 4)
        {
            sum += Math.Abs(px[i] - px[i + 4]) + Math.Abs(px[i + 1] - px[i + 5]) + Math.Abs(px[i + 2] - px[i + 6]);
            lum += px[i] + px[i + 1] + px[i + 2];
        }

        return sum / lum;
    }
}
