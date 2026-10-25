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
}
