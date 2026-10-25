using OneGame.Gltf;

namespace OneGame.Render3D.Tests;

/// <summary>
/// The Khronos sample models against reference renders. The references were drawn by the Khronos glTF Sample Viewer (a black box, driven by
/// tools/reference3d/shoot.mjs) with the same camera, the same environment panorama, Khronos PBR Neutral tone mapping and exposure 1.
/// A case is skipped when its model, the panorama or its reference image is not available.
/// </summary>
public class ReferenceTests
{
    // What "matches" means: on the pixels the model covers (edges excluded) the mean per-channel difference stays under 6 of 255, and 95 % of
    // pixels differ by under 28 in their worst channel. The viewer and this renderer filter their environments differently (sample counts, cube
    // resolution, mip selection) and use different anti-aliasing, so exact equality is not a goal; these limits are about 3x what a
    // renderer with the same material model measures against itself across drivers, and far below what a wrong BRDF term, a flipped normal
    // map or a missing texture transform produces.
    internal const double MaxMeanAbs = 6.0;
    internal const int MaxP95 = 28;

    public static IEnumerable<TheoryDataRow<string>> Names()
    {
        foreach (var c in ReferenceCase.LoadAll().Cases)
        {
            yield return new TheoryDataRow<string>(c.Name);
        }
    }

    private static string? ReferencePath(string name)
    {
        if (Environment.GetEnvironmentVariable("ONEGAME_RENDER3D_REFS") is { Length: > 0 } dir && File.Exists(Path.Combine(dir, name + ".png")))
        {
            return Path.Combine(dir, name + ".png");
        }

        var committed = Path.Combine(Paths.RepoRoot, "testdata", "render3d", "ref", name + ".png");
        return File.Exists(committed) ? committed : null;
    }

    internal static string? EnvPath(string name)
    {
        var dir = Environment.GetEnvironmentVariable("ONEGAME_ENV_DIR");
        return dir is { Length: > 0 } && File.Exists(Path.Combine(dir, name + "_1k.hdr")) ? Path.Combine(dir, name + "_1k.hdr") : null;
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void MatchesViewerReference(string name)
    {
        var (w, h, background, cases) = ReferenceCase.LoadAll();
        var c = cases.Single(x => x.Name == name);
        var modelPath = Paths.Model(c.Model);
        var refPath = ReferencePath(name);
        var envPath = c.Env is null ? null : EnvPath(c.Env);
        if (modelPath is null || refPath is null || (c.Env is not null && envPath is null))
        {
            return;
        }

        var (rw, rh, reference) = Png.Read(refPath);
        Assert.Equal((w, h), (rw, rh));

        using var rig = new Rig(w, h);
        var model = rig.Load(modelPath);
        var scene = new Scene3D { Camera = c.Camera };
        var instance = scene.Add(model);
        if (c.PoseAnimation is { } anim)
        {
            var index = Array.FindIndex(model.Source.Animations, a => a.Name == anim);
            Assert.True(index >= 0, $"{name}: no animation '{anim}'");
            instance.Animate(index, c.PoseTime, loop: false);
        }

        scene.Lights.AddRange(c.Lights);
        scene.Environment.Background = BackgroundMode.Color;
        scene.Environment.BackgroundColor = background;
        scene.Environment.Intensity = c.Ibl ? 1f : 0f;
        if (envPath is not null)
        {
            scene.Environment.Map = rig.Own(rig.Renderer.CreateEnvironment(HdrImage.Decode(File.ReadAllBytes(envPath))));
        }

        scene.Post.Bloom = false;
        if (Environment.GetEnvironmentVariable("ONEGAME_ENV_YAW") is { Length: > 0 } yawText)
        {
            scene.Environment.Yaw = float.Parse(yawText, System.Globalization.CultureInfo.InvariantCulture) * MathF.PI / 180f;
        }

        var actual = rig.Capture(scene);
        var diff = new byte[w * h * 4];
        var d = Compare.Measure(w, h, reference, actual, 2, diff);
        if (Paths.CaptureDir is { } dir)
        {
            Directory.CreateDirectory(dir);
            Png.Write(Path.Combine(dir, $"ref_{name}.png"), w * 3, h, Compare.Triptych(w, h, reference, actual, diff));
            File.AppendAllText(Path.Combine(dir, "metrics.txt"), $"{name} {d}\n");
        }

        Assert.True(d.Pixels > 0, $"{name}: the reference shows no model");
        Assert.True(d.MeanAbs <= MaxMeanAbs && d.P95 <= MaxP95, $"{name}: {d} (limits: meanAbs {MaxMeanAbs}, p95 {MaxP95})");
    }
}
