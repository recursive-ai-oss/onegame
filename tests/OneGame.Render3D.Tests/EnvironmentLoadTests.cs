using System.Diagnostics;
using RecursiveAI.OneGpu;

namespace OneGame.Render3D.Tests;

/// <summary>Asynchronous environments: same pixels as the synchronous path, bounded work per frame, clean cancellation.</summary>
public class EnvironmentLoadTests
{
    private const int W = 200;
    private const int H = 120;

    private static HdrImage Sky(int width) => HdrImage.CreateSky(width, width / 2, new Vector3(0.25f, 0.5f, 1.2f), new Vector3(0.9f, 0.95f, 1.1f), new Vector3(0.25f, 0.23f, 0.2f), new Vector3(0.3f, 1f, 0.2f), new Vector3(30f));

    private static (Scene3D Scene, Model3D Model) Scene(Rig rig)
    {
        var model = new Shapes().Sphere(new Vector3(-1.4f, 0, 0), 1.2f, "{\"pbrMetallicRoughness\":{\"baseColorFactor\":[0.95,0.9,0.8,1],\"metallicFactor\":1,\"roughnessFactor\":0.3}}", 40)
            .Sphere(new Vector3(1.4f, 0, 0), 1.2f, Shapes.Matte(0.7f, 0.7f, 0.7f), 40).Upload(rig);
        var scene = new Scene3D { Camera = Camera3D.LookAt(new Vector3(0, 0.3f, 7), Vector3.Zero, Vector3.UnitY), Samples = 1 };
        scene.Add(model);
        scene.Environment.Background = BackgroundMode.Environment;
        scene.Post.Bloom = false;
        return (scene, model);
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

    private static int Pump(Rig rig, Scene3D scene, EnvironmentLoad load, int limit = 400)
    {
        var frames = 0;
        while (load.IsRunning && frames < limit)
        {
            rig.Target.Render(scene);
            frames++;
        }

        rig.Device.WaitIdle();
        return frames;
    }

    [Fact]
    public void AsyncResultMatchesTheSynchronousPath()
    {
        using var rig = new Rig(W, H);
        var (scene, _) = Scene(rig);
        var sync = rig.Own(rig.Renderer.CreateEnvironment(Sky(256), 128));
        rig.Renderer.PrepareEnvironment(sync);
        scene.Environment.Map = sync;
        var expected = rig.Capture(scene, "env-sync");

        // A small budget makes the load span many frames; the old map keeps drawing meanwhile.
        using var load = rig.Renderer.CreateEnvironmentAsync(Sky(256), new EnvironmentLoadOptions { Size = 128, GpuBudget = 1_000_000, UploadBytesPerFrame = 40_000 });
        var during = rig.Capture(scene);
        Assert.Equal(0, MaxDiff(expected, during));
        var frames = Pump(rig, scene, load);
        Assert.True(load.IsReady, load.Error?.ToString());
        Assert.True(frames > 8, "the load should spread over many frames, took " + frames);
        Assert.Equal(1.0, load.Progress);

        var map = rig.Own(load.Take());
        Assert.True(map.IsBuilt);
        scene.Environment.Map = map;
        var actual = rig.Capture(scene, "env-async");
        Assert.True(MaxDiff(expected, actual) <= 1, "max diff " + MaxDiff(expected, actual));
    }

    [Fact]
    public void FollowsQualityAndOddPanoramaSizes()
    {
        using var rig = new Rig(W, H);
        var (scene, _) = Scene(rig);
        var image = new HdrImage(100, 37, Sky(100).Rgb.AsSpan(0, 100 * 37 * 3).ToArray());
        var sync = rig.Own(rig.Renderer.CreateEnvironment(image));
        rig.Renderer.PrepareEnvironment(sync);
        scene.Environment.Map = sync;
        var expected = rig.Capture(scene);
        using var load = rig.Renderer.CreateEnvironmentAsync(image, new EnvironmentLoadOptions { UploadBytesPerFrame = 3000 });
        Pump(rig, scene, load);
        Assert.True(load.IsReady, load.Error?.ToString());
        scene.Environment.Map = rig.Own(load.Take());
        Assert.True(MaxDiff(expected, rig.Capture(scene)) <= 1);
    }

    [Fact]
    public void QualityChangeMidLoadRestartsAtTheNewSize()
    {
        using var rig = new Rig(W, H);
        var (scene, _) = Scene(rig);
        scene.Quality = QualityLevel.Low;
        using var load = rig.Renderer.CreateEnvironmentAsync(Sky(256), new EnvironmentLoadOptions { GpuBudget = 1_000_000 });
        for (var i = 0; i < 6; i++)
        {
            rig.Target.Render(scene);
        }

        Assert.True(load.IsRunning);
        scene.Quality = QualityLevel.High;
        Pump(rig, scene, load);
        Assert.True(load.IsReady, load.Error?.ToString());
        var map = rig.Own(load.Take());
        Assert.Equal(QualitySettings.For(QualityLevel.High).EnvironmentSize, map.Size);
        scene.Environment.Map = map;
        rig.Capture(scene);
        Assert.Equal(QualitySettings.For(QualityLevel.High).EnvironmentSize, map.Size);
    }

    [Fact]
    public void NoFrameOfTheLoadTakesLongOnTheRenderThread()
    {
        using var rig = new Rig(W, H);
        var (scene, _) = Scene(rig);
        scene.Environment.Map = rig.Own(rig.Renderer.CreateEnvironment(Sky(128), 64));
        for (var i = 0; i < 12; i++)
        {
            rig.Target.Render(scene);
        }

        rig.Device.WaitIdle();
        var baseline = new List<double>();
        for (var i = 0; i < 20; i++)
        {
            var t0 = Stopwatch.GetTimestamp();
            rig.Target.Render(scene);
            baseline.Add(Stopwatch.GetElapsedTime(t0).TotalMilliseconds);
        }

        rig.Device.WaitIdle();
        var panorama = Sky(2048);
        var sync0 = Stopwatch.GetTimestamp();
        var reference = rig.Renderer.CreateEnvironment(panorama, 256);
        rig.Renderer.PrepareEnvironment(reference);
        var syncMs = Stopwatch.GetElapsedTime(sync0).TotalMilliseconds;
        reference.Dispose();

        using var load = rig.Renderer.CreateEnvironmentAsync(panorama, new EnvironmentLoadOptions { Size = 256 });
        var times = new List<double>();
        var frames = 0;
        while (load.IsRunning && frames < 600)
        {
            var t0 = Stopwatch.GetTimestamp();
            rig.Target.Render(scene);
            times.Add(Stopwatch.GetElapsedTime(t0).TotalMilliseconds);
            frames++;
            Thread.Sleep(2);
        }

        Assert.True(load.IsReady, load.Error?.ToString());
        baseline.Sort();
        times.Sort();
        var worst = times[^1];
        var secondWorst = times[^2];
        var line = $"{rig.Device.Adapter.Name}: sync create+prepare {syncMs:F1} ms; async {frames} frames, worst frame {worst:F2} ms, second worst {secondWorst:F2} ms (steady median {baseline[baseline.Count / 2]:F2} ms)";
        var dir = Paths.CaptureDir ?? Path.GetTempPath();
        Directory.CreateDirectory(dir);
        File.AppendAllText(Path.Combine(dir, "envload-times.txt"), line + "\n");
        Assert.True(frames > 4);
        if (!rig.Device.Adapter.Name.Contains("llvmpipe", StringComparison.OrdinalIgnoreCase))
        {
            // One frame in a run can lose its slice to the OS on a shared machine; the budget is about the rest.
            Assert.True(secondWorst < baseline[baseline.Count / 2] + 4.0, line);
        }

        rig.Own(load.Take());
    }

    [Fact]
    public void SteadyFramesBeforeAndAfterAllocateNothing()
    {
        using var rig = new Rig(W, H);
        var (scene, _) = Scene(rig);
        scene.Environment.Map = rig.Own(rig.Renderer.CreateEnvironment(Sky(128), 64));
        for (var i = 0; i < 12; i++)
        {
            rig.Target.Render(scene);
        }

        var before = Steady(rig, scene);
        using var load = rig.Renderer.CreateEnvironmentAsync(Sky(256), new EnvironmentLoadOptions { Size = 64 });
        Pump(rig, scene, load);
        Assert.True(load.IsReady, load.Error?.ToString());
        scene.Environment.NextMap = rig.Own(load.Take());
        scene.Environment.Blend = 0.5f;
        for (var i = 0; i < 12; i++)
        {
            rig.Target.Render(scene);
        }

        var after = Steady(rig, scene);
        var allowed = rig.Device.Adapter.Name.Contains("RADV", StringComparison.Ordinal) ? 8192 : 0;
        Assert.True(before <= allowed, $"before: {before} B");
        Assert.True(after <= allowed, $"after: {after} B");
    }

    private static long Steady(Rig rig, Scene3D scene)
    {
        rig.Device.WaitIdle();
        rig.Target.Render(scene);
        long total = 0;
        for (var i = 0; i < 20; i++)
        {
            var a = GC.GetAllocatedBytesForCurrentThread();
            rig.Target.Render(scene);
            total += GC.GetAllocatedBytesForCurrentThread() - a;
        }

        return total;
    }

    [Fact]
    public void CancelAndDisposeMidLoadFreeEverythingWithoutValidationMessages()
    {
        using var rig = new Rig(W, H);
        var (scene, _) = Scene(rig);
        scene.Environment.Map = rig.Own(rig.Renderer.CreateEnvironment(Sky(128), 64));
        rig.Capture(scene);

        // Cancelled while converting (before the first frame), while filtering, and after it was ready but never taken.
        var early = rig.Renderer.CreateEnvironmentAsync(Sky(512), new EnvironmentLoadOptions { Size = 128 });
        early.Cancel();
        Assert.Equal(EnvironmentLoadState.Canceled, early.State);
        early.Dispose();

        var mid = rig.Renderer.CreateEnvironmentAsync(Sky(512), new EnvironmentLoadOptions { Size = 128, GpuBudget = 1_000_000 });
        var frames = 0;
        while (mid.State != EnvironmentLoadState.Filtering || mid.Progress < 0.3)
        {
            rig.Target.Render(scene);
            Assert.True(++frames < 300, "never reached filtering");
        }

        mid.Cancel();
        Assert.Equal(EnvironmentLoadState.Canceled, mid.State);
        Assert.Throws<InvalidOperationException>(() => mid.Environment);
        rig.Target.Render(scene);
        rig.Target.Render(scene);
        mid.Dispose();

        var done = rig.Renderer.CreateEnvironmentAsync(Sky(256), new EnvironmentLoadOptions { Size = 64 });
        Pump(rig, scene, done);
        Assert.True(done.IsReady);
        done.Dispose();
        Assert.Throws<InvalidOperationException>(() => done.Environment);
        rig.Target.Render(scene);

        // A load still running when the renderer goes away (the rig disposes the renderer next) must not leave anything behind or throw.
        _ = rig.Renderer.CreateEnvironmentAsync(Sky(512), new EnvironmentLoadOptions { Size = 128, GpuBudget = 1_000_000 });
        rig.Target.Render(scene);
        rig.Target.Render(scene);
        Assert.Equal(1, rig.Renderer.RunningEnvironmentLoads);
    }

    [Fact]
    public void BadInputsThrowAtOnce()
    {
        using var rig = new Rig(W, H);
        Assert.Throws<ArgumentOutOfRangeException>(() => rig.Renderer.CreateEnvironmentAsync(Sky(64), new EnvironmentLoadOptions { Size = 100 }));
        Assert.Throws<ArgumentNullException>(() => rig.Renderer.CreateEnvironmentAsync(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => rig.Renderer.CreateEnvironmentAsync(Sky(64), new EnvironmentLoadOptions { GpuBudget = 0 }));
    }
}
