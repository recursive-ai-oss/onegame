using OneGame.Gpu.Graph;
using RecursiveAI.OneGpu;

namespace OneGame.Render3D.Tests;

/// <summary>
/// Drawing the scene at a fraction of the target. At 1.0 the frame is the one a renderer without the setting draws (no extra pass, no extra
/// image); below it the whole 3D frame runs smaller and one pass stretches it over the target.
/// </summary>
public class RenderScaleTests
{
    private const int W = 256;
    private const int H = 144;

    private static (Rig Rig, Scene3D Scene) Make(int width = W, int height = H)
    {
        var rig = new Rig(width, height);
        var model = new Shapes()
            .Ground(0f, 20f, Shapes.Matte(0.7f, 0.72f, 0.68f))
            .Box(new Vector3(-1.5f, 1f, 0f), new Vector3(2f, 2f, 2f), Shapes.Matte(0.8f, 0.3f, 0.25f))
            .Sphere(new Vector3(1.6f, 0.9f, 0.5f), 0.9f, Shapes.Matte(0.25f, 0.45f, 0.8f))
            .Upload(rig);
        var scene = new Scene3D { Camera = Camera3D.LookAt(new Vector3(5f, 4f, 7f), new Vector3(0, 0.8f, 0), Vector3.UnitY) };
        scene.Add(model);
        scene.Lights.Add(Light3D.Directional(new Vector3(-0.4f, -1f, -0.5f), new Vector3(1f, 0.96f, 0.9f), 3f));
        scene.Lights[0].CastShadows = true;
        return (rig, scene);
    }

    private static string[] LivePasses(Rig rig) => [.. rig.Target.Graph.Describe().Passes.Where(p => !p.Culled).Select(p => p.Name)];

    private static double MeanAbsDiff(byte[] a, byte[] b)
    {
        long sum = 0;
        for (var i = 0; i < a.Length; i++)
        {
            if (i % 4 != 3)
            {
                sum += Math.Abs(a[i] - b[i]);
            }
        }

        return sum / (a.Length * 0.75);
    }

    [Fact]
    public void FullScaleAddsNoPassAndSizesTheSceneImageToTheTarget()
    {
        var (rig, scene) = Make();
        using (rig)
        {
            Assert.Equal(1f, scene.RenderScale);
            rig.Capture(scene);
            Assert.DoesNotContain("scene_upscale", LivePasses(rig));
            Assert.Equal((W, H), (rig.Renderer.LastStats.RenderWidth, rig.Renderer.LastStats.RenderHeight));
            Assert.Equal((W, H), (rig.Renderer.SceneColorWidth, rig.Renderer.SceneColorHeight));

            // Values that mean "no scaling" draw the same frame.
            var reference = rig.Capture(scene);
            foreach (var same in new[] { 1f, 1.7f, float.NaN, float.PositiveInfinity })
            {
                scene.RenderScale = same;
                Assert.Equal(reference, rig.Capture(scene));
            }
        }
    }

    [Fact]
    public void HalfScaleDrawsASmallerImageAndStretchesItOverTheTarget()
    {
        var (rig, scene) = Make();
        using (rig)
        {
            var full = rig.Capture(scene, "scale-1.0");
            scene.RenderScale = 0.5f;
            var half = rig.Capture(scene, "scale-0.5");
            var stats = rig.Renderer.LastStats;
            Assert.Equal((W / 2, H / 2), (stats.RenderWidth, stats.RenderHeight));
            Assert.Equal((W / 2, H / 2), (rig.Renderer.SceneColorWidth, rig.Renderer.SceneColorHeight));
            Assert.Equal(W * H * 4, half.Length);
            var passes = LivePasses(rig);
            Assert.Contains("scene_upscale", passes);
            Assert.Equal("scene_upscale", passes[^1]);

            // The same picture, softer: the ground, box and sphere sit where they did, so the images differ only along edges.
            var diff = MeanAbsDiff(full, half);
            Assert.InRange(diff, 0.01, 6.0);
            Assert.NotEqual(full, half);

            // The graph's transient memory for the 3D frame shrinks with the pixel count.
            var halfBytes = rig.Target.Graph.Stats.TransientBytes;
            scene.RenderScale = 1f;
            rig.Capture(scene);
            Assert.True(halfBytes < rig.Target.Graph.Stats.TransientBytes, $"{halfBytes} bytes at 0.5 against {rig.Target.Graph.Stats.TransientBytes} at 1");

            // Back at 1.0 it is exactly the first frame again.
            Assert.Equal(full, rig.Capture(scene));
        }
    }

    [Fact]
    public void AScaleBelowTheLimitUsesTheLimitAndOddSizesRound()
    {
        var (rig, scene) = Make(255, 143);
        using (rig)
        {
            scene.RenderScale = 0.01f;
            rig.Capture(scene);
            var s = rig.Renderer.LastStats;
            Assert.Equal((64, 36), (s.RenderWidth, s.RenderHeight));      // 255 * 0.25 and 143 * 0.25, rounded
            scene.RenderScale = 0.5f;
            rig.Capture(scene);
            s = rig.Renderer.LastStats;
            Assert.Equal((128, 72), (s.RenderWidth, s.RenderHeight));
        }
    }

    [Theory]
    [InlineData(GpuFormat.Rgba8Unorm)]
    [InlineData(GpuFormat.Rgba16Float)]
    public void OtherTargetFormatsScaleToo(GpuFormat format)
    {
        using var rig = new Rig(128, 72, format);
        var model = new Shapes().Ground(0f, 20f, Shapes.Matte(0.7f, 0.7f, 0.7f)).Upload(rig);
        var scene = new Scene3D { Camera = Camera3D.LookAt(new Vector3(0, 4, 8), Vector3.Zero, Vector3.UnitY), RenderScale = 0.5f };
        scene.Add(model);
        rig.Target.Render(scene);
        Assert.Contains("scene_upscale", LivePasses(rig));
        Assert.Equal((64, 36), (rig.Renderer.LastStats.RenderWidth, rig.Renderer.LastStats.RenderHeight));
    }

    [Fact]
    public void ASampledViewScalesToo()
    {
        var (rig, scene) = Make();
        using (rig)
        {
            using var portal = new Offscreen3D(rig.Renderer, 128, 72, GpuFormat.Rgba8UnormSrgb, sampled: true);
            scene.RenderScale = 0.5f;
            portal.Render(scene);
            Assert.Equal((64, 36), (rig.Renderer.LastStats.RenderWidth, rig.Renderer.LastStats.RenderHeight));
            Assert.Contains(portal.Graph.Describe().Passes, p => p.Name == "scene_upscale" && !p.Culled);
        }
    }

    [Fact]
    public void SteadyFramesAtHalfScaleAllocateNothing()
    {
        var (rig, scene) = Make();
        using (rig)
        {
            scene.RenderScale = 0.5f;
            for (var i = 0; i < 12; i++)
            {
                rig.Target.Render(scene);
            }

            rig.Device.WaitIdle();
            rig.Target.Render(scene);
            long allocated = 0;
            for (var i = 0; i < 40; i++)
            {
                var a0 = GC.GetAllocatedBytesForCurrentThread();
                rig.Target.Render(scene);
                allocated += GC.GetAllocatedBytesForCurrentThread() - a0;
            }

            rig.Device.WaitIdle();
            Assert.True(allocated == 0, $"40 steady frames at 0.5 allocated {allocated} bytes");
        }
    }

    /// <summary>The scene image a later pass can read: a graph texture of the scaled size, valid until the frame executes.</summary>
    [Fact]
    public void TheSceneImageIsExposedToLaterPassesOfTheSameFrame()
    {
        var (rig, scene) = Make();
        using (rig)
        {
            scene.RenderScale = 0.5f;
            var graph = rig.Target.Graph;
            graph.BeginFrame();
            var target = graph.ImportTexture("target", rig.Target.Target);
            rig.Renderer.Render(graph, scene, target, W, H, rig.Target.Format);
            var sceneColor = rig.Renderer.SceneColor;
            Assert.True(sceneColor.IsValid);
            Assert.NotEqual(target, sceneColor);
            graph.Execute();
            Assert.Equal((W / 2, H / 2), (rig.Renderer.SceneColorWidth, rig.Renderer.SceneColorHeight));
        }
    }

    /// <summary>
    /// GPU time of a 1080p frame (4 cascades plus a point light with shadows, SSAO, 4x MSAA, bloom) at several scales, printed for the record.
    /// Runs only with ONEGAME_SCALE_TIMING=1: a timing run is for the 4070, not for every test pass.
    /// </summary>
    [Fact]
    public void GpuTimeFollowsThePixelCount()
    {
        if (Environment.GetEnvironmentVariable("ONEGAME_SCALE_TIMING") != "1")
        {
            return;
        }

        var (rig, scene) = Make(1920, 1080);
        using (rig)
        {
            if (!rig.Device.Capabilities.Timestamps)
            {
                return;
            }

            var shapes = new Shapes().Ground(0f, 40f, Shapes.Matte(0.6f, 0.6f, 0.6f));
            for (var i = 0; i < 400; i++)
            {
                shapes.Sphere(new Vector3((i % 20 * 2f) - 20f, 0.6f + (i / 20 % 3 * 0.5f), (i / 20 * 1.5f) - 14f), 0.5f, Shapes.Matte(0.3f + (i % 7 * 0.1f), 0.5f, 0.8f - (i % 5 * 0.1f)), 24);
            }

            scene.Add(shapes.Upload(rig));
            scene.Lights.Add(Light3D.Point(new Vector3(0, 6, 0), Vector3.One, 200f, 30f));
            scene.Lights[1].CastShadows = true;
            scene.Ssao.Enabled = true;
            scene.Samples = 4;
            var line = new System.Text.StringBuilder(rig.Device.Adapter.Name + " 1920x1080:");
            var medians = new Dictionary<float, double>();
            foreach (var scale in new[] { 1f, 0.75f, 0.5f, 0.25f })
            {
                scene.RenderScale = scale;
                var seen = new Dictionary<int, double>();
                for (var i = 0; i < 260; i++)
                {
                    rig.Target.Render(scene);
                    var s = rig.Renderer.LastStats;
                    if (i >= 20 && s.GpuFrameMs is { } ms && !seen.ContainsKey(s.GpuFrameIndex))
                    {
                        seen[s.GpuFrameIndex] = ms;
                    }
                }

                rig.Device.WaitIdle();
                var sorted = seen.Values.Order().ToArray();
                medians[scale] = sorted[sorted.Length / 2];
                line.Append(System.Globalization.CultureInfo.InvariantCulture, $" scale {scale}: {medians[scale]:F3} ms ({rig.Renderer.LastStats.RenderWidth}x{rig.Renderer.LastStats.RenderHeight})");
            }

            Console.WriteLine(line);
            if (Paths.CaptureDir is { } dir)
            {
                Directory.CreateDirectory(dir);
                File.AppendAllText(Path.Combine(dir, "scaletimes.txt"), line + "\n");
            }

            Assert.True(medians[0.5f] < medians[1f], line.ToString());
        }
    }
}
