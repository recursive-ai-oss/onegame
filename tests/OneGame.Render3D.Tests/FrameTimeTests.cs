using System.Diagnostics;

namespace OneGame.Render3D.Tests;

/// <summary>Frame times of the reference scenes at 1080p. Runs only when ONEGAME_FRAME_TIMES is set, because the numbers depend on the device.</summary>
public class FrameTimeTests
{
    [Theory]
    [InlineData("sponza")]
    [InlineData("helmet")]
    public void ReportsFrameTime(string name)
    {
        if (Environment.GetEnvironmentVariable("ONEGAME_FRAME_TIMES") is not { Length: > 0 })
        {
            return;
        }

        var (_, _, _, cases) = ReferenceCase.LoadAll();
        var c = cases.Single(x => x.Name == name);
        var modelPath = Paths.Model(c.Model);
        var envPath = c.Env is null ? null : ReferenceTests.EnvPath(c.Env);
        if (modelPath is null || (c.Env is not null && envPath is null))
        {
            return;
        }

        using var rig = new Rig(1920, 1080);
        var scene = new Scene3D { Camera = c.Camera };
        scene.Add(rig.Load(modelPath));
        scene.Lights.AddRange(c.Lights);
        scene.Environment.Intensity = c.Ibl ? 1f : 0f;
        if (envPath is not null)
        {
            scene.Environment.Map = rig.Own(rig.Renderer.CreateEnvironment(HdrImage.Decode(File.ReadAllBytes(envPath))));
        }

        for (var i = 0; i < 5; i++)
        {
            rig.Target.Render(scene);
        }

        rig.Device.WaitIdle();
        const int frames = 60;
        var sw = Stopwatch.StartNew();
        for (var i = 0; i < frames; i++)
        {
            rig.Target.Render(scene);
        }

        rig.Device.WaitIdle();
        var ms = sw.Elapsed.TotalMilliseconds / frames;
        File.AppendAllText(Path.Combine(Paths.CaptureDir ?? Path.GetTempPath(), "frametimes.txt"), $"{name} 1920x1080 {rig.Device.Adapter.Name}: {ms:F2} ms/frame, {rig.Renderer.LastStats}\n");
    }

    /// <summary>The 10,000-tree forest at 1080p at each quality level with shadows and ambient occlusion on, with the GPU time of each pass.</summary>
    [Fact]
    public void ReportsForestFrameTime()
    {
        if (Environment.GetEnvironmentVariable("ONEGAME_FRAME_TIMES") is not { Length: > 0 })
        {
            return;
        }

        using var rig = new Rig(1920, 1080);
        var scene = InstancingTests.ForestScene(rig, 100, 6f, out _);
        scene.Ssao.Enabled = true;
        scene.Ssao.Radius = 1f;
        foreach (var level in new[] { QualityLevel.Low, QualityLevel.Medium, QualityLevel.High, QualityLevel.Ultra })
        {
            scene.Quality = level;
            for (var i = 0; i < 6; i++)
            {
                rig.Target.Render(scene);
            }

            rig.Device.WaitIdle();
            const int frames = 60;
            var sw = Stopwatch.StartNew();
            for (var i = 0; i < frames; i++)
            {
                rig.Target.Render(scene);
            }

            rig.Device.WaitIdle();
            var ms = sw.Elapsed.TotalMilliseconds / frames;
            rig.Target.Render(scene);
            rig.Device.WaitIdle();
            rig.Target.Render(scene);
            var passes = string.Join(", ", rig.Target.Graph.Describe().Passes.Where(p => !p.Culled && p.GpuMicroseconds is > 0).Select(p => $"{p.Name} {p.GpuMicroseconds / 1000.0:F2}"));
            var s = rig.Renderer.LastStats;
            File.AppendAllText(Path.Combine(Paths.CaptureDir ?? Path.GetTempPath(), "frametimes.txt"),
                $"forest {level} 1920x1080 {rig.Device.Adapter.Name}: {ms:F2} ms/frame wall, {s.Instances} instances, {s.InstancesDrawn} drawn, {s.ShadowTiles} shadow tiles, {s.Triangles} triangles; GPU ms: {passes}\n");
        }
    }
}
