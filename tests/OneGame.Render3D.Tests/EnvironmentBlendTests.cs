using OneGame.Gltf;
using OneGame.Gpu;

namespace OneGame.Render3D.Tests;

/// <summary>Environment crossfade: weight 0 is the first map, 1 the second, in between a mix, and a keyframe swap changes nothing.</summary>
public class EnvironmentBlendTests
{
    private const int W = 240;
    private const int H = 120;

    private static HdrImage Day() => HdrImage.CreateSky(128, 64, new Vector3(0.25f, 0.5f, 1.2f), new Vector3(0.9f, 0.95f, 1.1f), new Vector3(0.25f, 0.23f, 0.2f), new Vector3(0.3f, 1f, 0.2f), new Vector3(30f));

    private static HdrImage Dusk() => HdrImage.CreateSky(128, 64, new Vector3(0.12f, 0.08f, 0.3f), new Vector3(1.6f, 0.55f, 0.2f), new Vector3(0.1f, 0.05f, 0.04f), new Vector3(-1f, 0.1f, 0.3f), new Vector3(12f, 4f, 1.5f));

    private static HdrImage Night() => HdrImage.CreateSky(128, 64, new Vector3(0.01f, 0.01f, 0.04f), new Vector3(0.03f, 0.03f, 0.08f), new Vector3(0.005f), null);

    private static (Rig Rig, Scene3D Scene, EnvironmentMap A, EnvironmentMap B, EnvironmentMap C) Make()
    {
        var rig = new Rig(W, H);
        var a = rig.Own(rig.Renderer.CreateEnvironment(Day(), 64));
        var b = rig.Own(rig.Renderer.CreateEnvironment(Dusk(), 64));
        var c = rig.Own(rig.Renderer.CreateEnvironment(Night(), 64));
        var sphere = new Shapes().Sphere(new Vector3(-1.6f, 0, 0), 1.4f, "{\"pbrMetallicRoughness\":{\"baseColorFactor\":[0.95,0.9,0.8,1],\"metallicFactor\":1,\"roughnessFactor\":0.25}}", 48)
            .Sphere(new Vector3(1.6f, 0, 0), 1.4f, Shapes.Matte(0.7f, 0.7f, 0.7f), 48).Upload(rig);
        foreach (var env in new[] { a, b, c })
        {
            rig.Renderer.PrepareEnvironment(env);
        }

        var scene = new Scene3D { Camera = Camera3D.LookAt(new Vector3(0, 0.3f, 7), Vector3.Zero, Vector3.UnitY), Samples = 1 };
        scene.Add(sphere);
        scene.Environment.Map = a;
        scene.Environment.Background = BackgroundMode.Environment;
        scene.Environment.BackgroundBlur = 0.2f;
        scene.Post.Bloom = false;
        return (rig, scene, a, b, c);
    }

    // The target reuses its capture buffer, so a capture that is compared later must be copied.
    private static byte[] Shot(Rig rig, Scene3D scene, string? name = null) => rig.Capture(scene, name).ToArray();

    private static Vector3 Mean(byte[] px, int x0, int y0, int w, int h)
    {
        var sum = Vector3.Zero;
        for (var y = y0; y < y0 + h; y++)
        {
            for (var x = x0; x < x0 + w; x++)
            {
                var i = ((y * W) + x) * 4;
                sum += new Vector3(px[i], px[i + 1], px[i + 2]);
            }
        }

        return sum / (w * h);
    }

    private static int MaxDiff(byte[] a, byte[] b)
    {
        var worst = 0;
        for (var i = 0; i < a.Length; i++)
        {
            worst = Math.Max(worst, Math.Abs(a[i] - b[i]));
        }

        return worst;
    }

    [Fact]
    public void EndsMatchTheSingleMapsAndTheMiddleMixesBoth()
    {
        var (rig, scene, a, b, _) = Make();
        using (rig)
        {
            var onlyA = Shot(rig, scene);
            scene.Environment.Map = b;
            var onlyB = Shot(rig, scene);
            scene.Environment.Map = a;
            scene.Environment.NextMap = b;

            scene.Environment.Blend = 0f;
            var w0 = Shot(rig, scene, "env_blend_0");
            scene.Environment.Blend = 0.5f;
            var w5 = Shot(rig, scene, "env_blend_05");
            scene.Environment.Blend = 1f;
            var w1 = Shot(rig, scene, "env_blend_1");

            Assert.True(MaxDiff(onlyA, w0) <= 1, $"weight 0 differs from the first map alone by {MaxDiff(onlyA, w0)}");
            Assert.True(MaxDiff(onlyB, w1) <= 1, $"weight 1 differs from the second map alone by {MaxDiff(onlyB, w1)}");

            // Backdrop (top-left corner) and the metal sphere's reflection change with the weight, the middle sits between the ends.
            foreach (var (x, y, w, h, name) in new[] { (2, 2, 20, 10, "sky"), (40, 40, 30, 30, "metal sphere") })
            {
                var m0 = Mean(w0, x, y, w, h);
                var m1 = Mean(w1, x, y, w, h);
                var mh = Mean(w5, x, y, w, h);
                Assert.True((m0 - m1).Length() > 15f, $"{name} should change visibly: {m0} -> {m1}");
                Assert.True((mh - m0).Length() > 3f && (mh - m1).Length() > 3f, $"{name} middle {mh} should differ from both ends {m0}, {m1}");
                var along = Vector3.Dot(mh - m0, m1 - m0) / (m1 - m0).LengthSquared();
                Assert.InRange(along, 0.2f, 0.8f);
            }

            Reference.Compare("env_blend_0", w0, W, H, meanTolerance: 0.6, maxTolerance: 16);
            Reference.Compare("env_blend_05", w5, W, H, meanTolerance: 0.6, maxTolerance: 16);
            Reference.Compare("env_blend_1", w1, W, H, meanTolerance: 0.6, maxTolerance: 16);
        }
    }

    [Fact]
    public void SwappingKeyframesAtTheBoundaryChangesNothing()
    {
        var (rig, scene, a, b, c) = Make();
        using (rig)
        {
            scene.Environment.Map = a;
            scene.Environment.NextMap = b;
            scene.Environment.Blend = 1f;
            var before = Shot(rig, scene);

            // The cycle moves on: b becomes the first map, c the next, and the weight starts over.
            scene.Environment.Map = b;
            scene.Environment.NextMap = c;
            scene.Environment.Blend = 0f;
            var after = Shot(rig, scene);
            Assert.True(MaxDiff(before, after) <= 1, $"swap changed the picture by {MaxDiff(before, after)}");

            // And a weight just past the boundary is just past it, not a jump.
            scene.Environment.Blend = 0.01f;
            var next = Shot(rig, scene);
            Assert.True(MaxDiff(after, next) < 12, $"1% of a step moved the picture by {MaxDiff(after, next)}");
        }
    }

    [Fact]
    public void ChangingTheWeightEveryFrameAllocatesNothingAndRefiltersNothing()
    {
        var (rig, scene, a, b, _) = Make();
        using (rig)
        {
            scene.Environment.NextMap = b;
            scene.Fog.Enabled = true;
            scene.Fog.Color = new Vector3(0.5f);
            scene.Fog.Start = 5f;
            scene.Fog.End = 50f;
            for (var i = 0; i < 12; i++)
            {
                scene.Environment.Blend = i / 8f;
                rig.Target.Render(scene);
            }

            rig.Device.WaitIdle();
            rig.Target.Render(scene);
            Assert.True(a.IsBuilt && b.IsBuilt);
            long allocated = 0;
            for (var i = 0; i < 40; i++)
            {
                scene.Environment.Blend = i / 40f;
                scene.Fog.End = 50f + i;
                var a0 = GC.GetAllocatedBytesForCurrentThread();
                rig.Target.Render(scene);
                allocated += GC.GetAllocatedBytesForCurrentThread() - a0;
            }

            rig.Device.WaitIdle();
            Assert.True(allocated == 0, $"changing the weight every frame allocated {allocated} bytes");
        }
    }
}
