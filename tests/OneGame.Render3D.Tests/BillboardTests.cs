using OneGame.Gpu;

namespace OneGame.Render3D.Tests;

/// <summary>Camera-facing quads: placed where they say, hidden by nearer surfaces, sorted when blended, order independent when additive.</summary>
public class BillboardTests
{
    private const int W = 160;
    private const int H = 120;

    private static (Rig Rig, Scene3D Scene) Make(bool box)
    {
        var rig = new Rig(W, H, GpuFormat.Rgba16Float);
        var scene = new Scene3D { Camera = Camera3D.LookAt(new Vector3(0, 0, 6), Vector3.Zero, Vector3.UnitY), Samples = 1 };
        if (box)
        {
            // A wall across the middle of the view, 2 x 2 at z = -3, lit from the front.
            scene.Add(new Shapes().Box(new Vector3(0, 0, -3), new Vector3(2, 2, 0.2f), Shapes.Matte(0.5f, 0.5f, 0.5f)).Upload(rig));
            scene.Lights.Add(Light3D.Directional(new Vector3(0, 0, -1), Vector3.One, 3f));
        }

        scene.Environment.Intensity = 0f;
        scene.Environment.Background = BackgroundMode.Color;
        scene.Environment.BackgroundColor = Vector3.Zero;
        scene.Post.Bloom = false;
        scene.Post.ToneMapper = ToneMapper.None;
        return (rig, scene);
    }

    private static Vector3 Pixel(float[] px, Scene3D scene, Vector3 world)
    {
        var clip = Vector4.Transform(new Vector4(world, 1f), scene.Camera.View * scene.Camera.Projection(W / (float)H));
        var x = (int)(((clip.X / clip.W) * 0.5f + 0.5f) * W);
        var y = (int)((0.5f - ((clip.Y / clip.W) * 0.5f)) * H);
        return At(px, x, y);
    }

    private static Vector3 At(float[] px, int x, int y)
    {
        var i = ((y * W) + x) * 4;
        return new Vector3(px[i], px[i + 1], px[i + 2]);
    }

    private static BillboardBatch3D Batch(Scene3D scene)
    {
        var b = new BillboardBatch3D();
        scene.Billboards.Add(b);
        return b;
    }

    private static Billboard3D Quad(Vector3 at, float size, Vector4 colour, bool additive) =>
        new() { Position = at, Size = new Vector2(size), Color = colour, Additive = additive };

    [Fact]
    public void AdditiveQuadAddsItsColourWhereItStandsAndHDRValuesSurvive()
    {
        var (rig, scene) = Make(false);
        using (rig)
        {
            scene.Post.Exposure = 0.25f;
            var b = Batch(scene);
            b.Add(Quad(new Vector3(0, 0, 0), 2f, new Vector4(2f, 1f, 0f, 1f), true));
            b.Add(Quad(new Vector3(3, 0, 0), 2f, new Vector4(3f, 0f, 0f, 1f), true));
            var px = Plane.ReadAfterRender(rig, scene);
            var c = Pixel(px, scene, Vector3.Zero);
            Assert.InRange(c.X, 0.45f, 0.51f);
            Assert.InRange(c.Y, 0.22f, 0.26f);
            Assert.InRange(c.Z, 0f, 0.01f);
            // Brighter than the display range: colour 3 at exposure 1/4 reads 0.75, where a clamped colour would read 0.25.
            var hot = Pixel(px, scene, new Vector3(3, 0, 0));
            Assert.InRange(hot.X, 0.7f, 0.77f);
            // Outside the quads nothing was drawn.
            Assert.Equal(Vector3.Zero, At(px, 2, 2));
            Assert.Equal(2, rig.Renderer.LastStats.Billboards);
        }
    }

    [Fact]
    public void QuadsBehindASurfaceAreHiddenAndQuadsInFrontAreNot()
    {
        var (rig, scene) = Make(true);
        using (rig)
        {
            var bare = Plane.ReadAfterRender(rig, scene);
            var b = Batch(scene);
            b.Add(Quad(new Vector3(0, 0, -6), 1f, new Vector4(1, 0, 0, 1), true));
            var behind = Plane.ReadAfterRender(rig, scene);
            Assert.True(Vector3.Distance(Pixel(behind, scene, new Vector3(0, 0, -3)), Pixel(bare, scene, new Vector3(0, 0, -3))) < 1e-3f, "a quad behind the wall shows through it");

            b.Clear();
            b.Add(Quad(new Vector3(0, 0, -2), 1f, new Vector4(1, 0, 0, 1), true));
            var front = Plane.ReadAfterRender(rig, scene);
            Assert.True(Pixel(front, scene, new Vector3(0, 0, -2)).X - Pixel(bare, scene, new Vector3(0, 0, -3)).X > 0.4f, "a quad in front of the wall is not drawn");
        }
    }

    [Fact]
    public void BlendedQuadsAreDrawnFarToNearWhateverTheOrderTheyWereAdded()
    {
        Vector3 Centre(bool farFirst)
        {
            var (rig, scene) = Make(false);
            using (rig)
            {
                var b = Batch(scene);
                var far = Quad(new Vector3(0, 0, -1), 2f, new Vector4(1, 0, 0, 0.5f), false);
                var near = Quad(new Vector3(0, 0, 1), 2f, new Vector4(0, 1, 0, 0.5f), false);
                if (farFirst)
                {
                    b.Add(far);
                    b.Add(near);
                }
                else
                {
                    b.Add(near);
                    b.Add(far);
                }

                return Pixel(Plane.ReadAfterRender(rig, scene), scene, Vector3.Zero);
            }
        }

        var a = Centre(true);
        var c = Centre(false);
        Assert.True(Vector3.Distance(a, c) < 1e-3f, $"{a} vs {c}");
        // Near green over far red at half alpha each: green 0.5, red 0.25.
        Assert.InRange(a.Y, 0.48f, 0.52f);
        Assert.InRange(a.X, 0.23f, 0.27f);
    }

    [Fact]
    public void AdditiveQuadsGiveTheSameImageInAnyOrder()
    {
        Vector3 Centre(bool flip)
        {
            var (rig, scene) = Make(false);
            using (rig)
            {
                var b = Batch(scene);
                var q1 = Quad(new Vector3(0, 0, -1), 2f, new Vector4(0.3f, 0, 0, 1), true);
                var q2 = Quad(new Vector3(0, 0, 1), 2f, new Vector4(0, 0.4f, 0, 1), true);
                b.Add(flip ? q2 : q1);
                b.Add(flip ? q1 : q2);
                return Pixel(Plane.ReadAfterRender(rig, scene), scene, Vector3.Zero);
            }
        }

        var a = Centre(false);
        Assert.Equal(a, Centre(true));
        Assert.InRange(a.X, 0.28f, 0.31f);
        Assert.InRange(a.Y, 0.38f, 0.41f);
    }

    [Fact]
    public void TextureRectanglesAndRotationAreHonoured()
    {
        var (rig, scene) = Make(false);
        using (rig)
        {
            // A 2 x 1 image: left pixel red, right pixel green.
            var tex = rig.Own(rig.Renderer.CreateBillboardTexture(2, 1, [255, 0, 0, 255, 0, 255, 0, 255]));
            var b = Batch(scene);
            b.Texture = tex;
            b.Add(new Billboard3D { Position = new Vector3(-1.5f, 0, 0), Size = new Vector2(1f), Color = Vector4.One, Uv = new Vector4(0.5f, 0, 0.5f, 1), Additive = true });
            // A wide bar turned a quarter: it stands upright.
            Batch(scene).Add(new Billboard3D { Position = new Vector3(1.5f, 0, 0), Size = new Vector2(2f, 0.4f), Rotation = MathF.PI / 2f, Color = Vector4.One, Additive = true });
            var px = Plane.ReadAfterRender(rig, scene);
            var green = Pixel(px, scene, new Vector3(-1.5f, 0, 0));
            Assert.True(green.Y > 0.9f && green.X < 0.03f, $"right half of the image should be green, got {green}");
            Assert.True(Pixel(px, scene, new Vector3(1.5f, 0.5f, 0)).X > 0.1f, "the rotated bar does not reach upward");
            Assert.True(Pixel(px, scene, new Vector3(2.1f, 0, 0)).X < 0.01f, "the rotated bar still reaches sideways");
        }
    }

    [Fact]
    public void ThousandsOfQuadsAllocateNothingInSteadyFrames()
    {
        var (rig, scene) = Make(true);
        using (rig)
        {
            var tex = rig.Own(rig.Renderer.CreateBillboardTexture(2, 2, new byte[16]));
            var a = Batch(scene);
            var b = Batch(scene);
            b.Texture = tex;
            const int quads = 2000;
            void Fill(int frame)
            {
                a.Clear();
                b.Clear();
                for (var i = 0; i < quads; i++)
                {
                    var t = (i * 0.37f) + (frame * 0.01f);
                    var q = Quad(new Vector3(MathF.Sin(t) * 2f, MathF.Cos(t * 1.3f), MathF.Sin(t * 0.7f) * 2f), 0.1f, new Vector4(0.2f, 0.3f, 0.5f, 0.5f), i % 3 == 0);
                    (i % 2 == 0 ? a : b).Add(q);
                }
            }

            for (var i = 0; i < 12; i++)
            {
                Fill(i);
                rig.Target.Render(scene);
            }

            rig.Device.WaitIdle();
            Fill(99);
            rig.Target.Render(scene);
            long total = 0;
            var where = new List<string>();
            for (var i = 0; i < 60; i++)
            {
                Fill(i);
                var before = GC.GetAllocatedBytesForCurrentThread();
                rig.Target.Render(scene);
                var delta = GC.GetAllocatedBytesForCurrentThread() - before;
                total += delta;
                if (delta > 0)
                {
                    where.Add($"frame {i}: {delta}");
                }
            }

            rig.Device.WaitIdle();
            Assert.Equal(quads, rig.Renderer.LastStats.Billboards);
            Assert.True(total == 0, $"60 steady frames of {quads} quads allocated {total} bytes ({string.Join(", ", where.Take(6))})");
        }
    }
}
