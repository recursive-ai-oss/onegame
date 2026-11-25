using System.Diagnostics;
using RecursiveAI.OneGpu;
using OneGame.Gpu.Graph;

namespace OneGame.Render3D.Tests;

/// <summary>CPU and GPU frame time in <see cref="FrameStats3D"/>: present, late but never waited for, null when the device cannot time, and free of allocation.</summary>
public class FrameTimingTests
{
    private static (Rig Rig, Scene3D Scene) Pond(int width = 320, int height = 180) => WaterReflectionTests.Pond(width, height);

    [Fact]
    public void CpuAndGpuTimesArePositiveAndTheGpuOneArrivesWithinAFewFrames()
    {
        var (rig, scene) = Pond();
        using (rig)
        {
            if (!rig.Device.Capabilities.Timestamps)
            {
                return;
            }

            // A device that finishes work at submit (a software rasterizer) has the frame's time at once; a real GPU is one or two frames behind.
            // Either way the value belongs to a frame that has been rendered, and none is ever waited for.
            rig.Target.Render(scene);
            var first = rig.Renderer.LastStats;
            Assert.True(first.CpuFrameMs > 0);
            Assert.Equal(first.GpuFrameMs is null, first.GpuFrameIndex == 0);

            var firstSeen = first.GpuFrameMs is null ? -1 : 0;
            for (var i = 1; i < 12 && firstSeen < 0; i++)
            {
                rig.Target.Render(scene);
                if (rig.Renderer.LastStats.GpuFrameMs is not null)
                {
                    firstSeen = i;
                }
            }

            Assert.InRange(firstSeen, 0, 4);
            for (var i = 0; i < 10; i++)
            {
                rig.Target.Render(scene);
            }

            var s = rig.Renderer.LastStats;
            Assert.True(s.GpuFrameMs > 0);
            Assert.True(s.CpuFrameMs > 0);
            Assert.InRange(rig.Target.Graph.Frame - s.GpuFrameIndex, 0, 3);

            var passes = new List<PassTime>();
            var passFrame = rig.Renderer.ReadGpuPassTimes(passes);
            Assert.True(passFrame > 0);
            Assert.Contains(passes, p => p.Name == "forward" && p.Microseconds > 0);
        }
    }

    /// <summary>
    /// Many frames, read every frame the way an overlay does: no pass or frame time is ever a wrapped subtraction (about 1.8e13 ms), and once
    /// the first frame has finished there is always a frame time, however far the GPU lags. ONEGAME_TIMING_FRAMES sets the length (default 600).
    /// </summary>
    [Fact]
    public void LongRunsNeverWrapAndNeverLoseTheFrameTime()
    {
        var frames = int.TryParse(Environment.GetEnvironmentVariable("ONEGAME_TIMING_FRAMES"), out var n) && n > 0 ? n : 600;
        // ONEGAME_TIMING_SIZE (for example 3840x2160) makes the GPU the slow side, which is when finished frames pile up unread.
        var size = Environment.GetEnvironmentVariable("ONEGAME_TIMING_SIZE")?.Split('x');
        var (rig, scene) = size is { Length: 2 } ? Pond(int.Parse(size[0]), int.Parse(size[1])) : Pond();
        using (rig)
        {
            if (!rig.Device.Capabilities.Timestamps)
            {
                return;
            }

            var passes = new List<PassTime>(64);
            var missing = 0;
            var worst = 0.0;
            var worstPass = 0.0;
            var lastIndex = 0;
            for (var i = 0; i < frames; i++)
            {
                rig.Target.Render(scene);
                var s = rig.Renderer.LastStats;
                if (i >= 8 && s.GpuFrameMs is null)
                {
                    missing++;
                }

                if (s.GpuFrameMs is { } ms)
                {
                    worst = Math.Max(worst, ms);
                    Assert.True(s.GpuFrameIndex >= lastIndex, $"frame time went back from {lastIndex} to {s.GpuFrameIndex}");
                    lastIndex = s.GpuFrameIndex;
                }

                rig.Renderer.ReadGpuPassTimes(passes);
                foreach (var p in passes)
                {
                    worstPass = Math.Max(worstPass, p.Microseconds / 1000.0);
                }
            }

            rig.Device.WaitIdle();
            Console.WriteLine($"{rig.Device.Adapter.Name}: {frames} frames, longest frame {worst:F3} ms, longest pass {worstPass:F3} ms, frames without a time {missing}");
            // A wrapped subtraction reads as about 1.8e13 ms. A software rasterizer on a busy machine can really take seconds per frame,
            // so the bound sits far above any real frame (ten minutes) and far below a wrap.
            const double WrapBoundMs = 600_000;
            Assert.True(worst < WrapBoundMs, $"a frame took {worst} ms");
            Assert.True(worstPass < WrapBoundMs, $"a pass took {worstPass} ms");
            Assert.Equal(0, missing);
        }
    }

    [Fact]
    public void ReadingNeverWaitsForTheGpu()
    {
        var (rig, scene) = Pond();
        using (rig)
        {
            for (var i = 0; i < 8; i++)
            {
                rig.Target.Render(scene);
            }

            // Reads are polls: thousands of them cost far less than one frame's GPU time on any device.
            var sw = Stopwatch.StartNew();
            for (var i = 0; i < 2000; i++)
            {
                _ = rig.Renderer.LastStats;
            }

            Assert.True(sw.Elapsed.TotalMilliseconds < 500, $"2000 reads took {sw.Elapsed.TotalMilliseconds:F1} ms");
        }
    }

    [Fact]
    public void WithTimingOffTheGpuTimeIsNullAndTheCpuTimeStillThere()
    {
        var (rig, scene) = Pond();
        using (rig)
        {
            using var graph = new RenderGraph(rig.Device, new RenderGraphOptions { Timing = false });
            var target = rig.Target;
            for (var i = 0; i < 6; i++)
            {
                graph.BeginFrame();
                var t = graph.ImportTexture("target", target.Target);
                rig.Renderer.Render(graph, scene, t, target.Width, target.Height, target.Format);
                graph.Execute();
            }

            rig.Device.WaitIdle();
            var s = rig.Renderer.LastStats;
            Assert.False(graph.TimingEnabled);
            Assert.Null(s.GpuFrameMs);
            Assert.Equal(0, s.GpuFrameIndex);
            Assert.True(s.CpuFrameMs > 0);
            Assert.Equal(0, rig.Renderer.ReadGpuPassTimes([]));
            Assert.Null(default(FrameStats3D).GpuFrameMs);
        }
    }

    [Fact]
    public void SteadyFramesAllocateNothingWhetherOrNotTheStatsAreRead()
    {
        var (rig, scene) = Pond();
        using (rig)
        {
            var passes = new List<PassTime>(64);
            for (var i = 0; i < 12; i++)
            {
                rig.Target.Render(scene);
                _ = rig.Renderer.LastStats;
                rig.Renderer.ReadGpuPassTimes(passes);
            }

            rig.Device.WaitIdle();
            rig.Target.Render(scene);
            foreach (var read in new[] { false, true })
            {
                long allocated = 0;
                for (var i = 0; i < 40; i++)
                {
                    var a0 = GC.GetAllocatedBytesForCurrentThread();
                    rig.Target.Render(scene);
                    if (read)
                    {
                        var s = rig.Renderer.LastStats;
                        _ = s.GpuFrameMs;
                        rig.Renderer.ReadGpuPassTimes(passes);
                    }

                    allocated += GC.GetAllocatedBytesForCurrentThread() - a0;
                }

                rig.Device.WaitIdle();
                Assert.True(allocated == 0, $"40 steady frames (stats read: {read}) allocated {allocated} bytes");
            }
        }
    }

    /// <summary>Median GPU time of the water scene with the planar reflection off and on. Printed for the record; asserts only that the pass shows up with a time (a software rasterizer under load is too noisy to order the totals).</summary>
    [Fact]
    public void ReflectionShowsInTheGpuTime()
    {
        var (rig, scene) = Pond(1920, 1080);
        using (rig)
        {
            if (!rig.Device.Capabilities.Timestamps)
            {
                return;
            }

            var water = Water(scene);
            var passes = new List<PassTime>(64);
            var off = Median(rig, scene, water, false, passes, out _);
            var on = Median(rig, scene, water, true, passes, out var reflection);
            var line = $"{rig.Device.Adapter.Name}: reflection off {off:F3} ms, on {on:F3} ms (+{on - off:F3}), planar_reflection pass {reflection:F3} ms, {rig.Target.Width}x{rig.Target.Height}";
            if (Paths.CaptureDir is { } dir)
            {
                Directory.CreateDirectory(dir);
                File.AppendAllText(Path.Combine(dir, "frametimes.txt"), line + "\n");
            }

            Assert.True(reflection > 0, line);
        }
    }

    private static Material3D Water(Scene3D scene)
    {
        foreach (var m in scene.Models)
        {
            if (m.Model.FindMaterial("Water") is { } w)
            {
                return w;
            }
        }

        throw new InvalidOperationException("no water material in the scene");
    }

    private static double Median(Rig rig, Scene3D scene, Material3D water, bool reflect, List<PassTime> passes, out double reflectionMs)
    {
        water.PlanarReflection = reflect;
        const int frames = 200;
        var seen = new Dictionary<int, double>();
        var reflections = new Dictionary<int, double>();
        for (var i = 0; i < frames + 8; i++)
        {
            rig.Target.Render(scene);
            if (i < 8)
            {
                continue;
            }

            var s = rig.Renderer.LastStats;
            if (s.GpuFrameMs is { } ms && s.GpuFrameIndex > 0 && !seen.ContainsKey(s.GpuFrameIndex))
            {
                seen[s.GpuFrameIndex] = ms;
                rig.Renderer.ReadGpuPassTimes(passes);
                foreach (var p in passes)
                {
                    if (p.Name == "planar_reflection")
                    {
                        reflections[s.GpuFrameIndex] = p.Microseconds / 1000.0;
                    }
                }
            }
        }

        rig.Device.WaitIdle();
        reflectionMs = reflections.Count == 0 ? 0 : Mid(reflections.Values);
        return Mid(seen.Values);
    }

    private static double Mid(IEnumerable<double> values)
    {
        var a = values.Order().ToArray();
        return a[a.Length / 2];
    }
}
