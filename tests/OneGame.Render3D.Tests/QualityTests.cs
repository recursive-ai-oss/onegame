using OneGame.Gpu;
using OneGame.Gpu.Graph;

namespace OneGame.Render3D.Tests;

public class QualityTests
{
    private static Scene3D Make(Rig rig)
    {
        var model = new Shapes()
            .Ground(0f, 30f, Shapes.Matte(0.7f, 0.7f, 0.7f))
            .Box(new Vector3(0, 1f, 0), new Vector3(2, 2, 2), Shapes.Matte(0.8f, 0.4f, 0.3f))
            .Upload(rig);
        var scene = new Scene3D { Camera = Camera3D.LookAt(new Vector3(3, 6, 9), new Vector3(-1, 0.5f, 0), Vector3.UnitY) };
        scene.Add(model);
        scene.Lights.Add(new Light3D { Kind = LightKind.Directional, Direction = Vector3.Normalize(new Vector3(-0.8f, -1f, 0.2f)), Intensity = 2.5f, CastShadows = true });
        scene.Environment.Intensity = 0.4f;
        scene.Ssao.Enabled = true;
        return scene;
    }

    private static ResourceReport Resource(Rig rig, string name) => rig.Target.Graph.Describe().Resources.First(r => r.Name == name);

    [Fact]
    public void LevelsGrowInCostAndQuality()
    {
        var levels = new[] { QualityLevel.Low, QualityLevel.Medium, QualityLevel.High, QualityLevel.Ultra }.Select(QualitySettings.For).ToArray();
        for (var i = 1; i < levels.Length; i++)
        {
            Assert.True(levels[i].ShadowTileSize >= levels[i - 1].ShadowTileSize);
            Assert.True(levels[i].MaxCascades >= levels[i - 1].MaxCascades);
            Assert.True(levels[i].MaxSamples >= levels[i - 1].MaxSamples);
            Assert.True(levels[i].EnvironmentSize >= levels[i - 1].EnvironmentSize);
            Assert.True(levels[i].SsaoSamples >= levels[i - 1].SsaoSamples);
        }

        Assert.False(levels[0].Ssao);
        Assert.False(levels[0].Bloom);
        Assert.True(QualitySettings.TryParse("ULTRA", out var parsed) && parsed == QualityLevel.Ultra);
        Assert.False(QualitySettings.TryParse("extreme", out _));
    }

    [Fact]
    public void SwitchingAtRuntimeChangesTheWorkTheGraphDoes()
    {
        using var rig = new Rig(240, 180);
        var scene = Make(rig);
        var tileSizes = new Dictionary<QualityLevel, string>();
        var passes = new Dictionary<QualityLevel, string[]>();
        foreach (var level in new[] { QualityLevel.Low, QualityLevel.Medium, QualityLevel.High, QualityLevel.Low })
        {
            scene.Quality = level;
            rig.Target.Render(scene);
            Assert.Equal(level, rig.Renderer.LastStats.Quality);
            tileSizes[level] = Resource(rig, "shadow_atlas").Size;
            passes[level] = [.. rig.Target.Graph.Describe().Passes.Where(p => !p.Culled).Select(p => p.Name)];
        }

        // Low: two cascades of 512 side by side. Medium: three cascades of 1024 in a 2x2 atlas. High: four of 2048 in 2x2.
        Assert.Equal("1024x512", tileSizes[QualityLevel.Low]);
        Assert.Equal("2048x2048", tileSizes[QualityLevel.Medium]);
        Assert.Equal("4096x4096", tileSizes[QualityLevel.High]);

        Assert.DoesNotContain("ssao", passes[QualityLevel.Low]);
        Assert.DoesNotContain("bloom_down_0", passes[QualityLevel.Low]);
        Assert.Contains("ssao", passes[QualityLevel.Medium]);
        Assert.Contains("bloom_down_0", passes[QualityLevel.Medium]);
        Assert.Contains("shadows", passes[QualityLevel.High]);
    }

    [Fact]
    public void MultisamplingFollowsTheLevelAndTheSceneCanLowerIt()
    {
        using var rig = new Rig(160, 120);
        var scene = Make(rig);
        scene.Quality = QualityLevel.Low;
        rig.Target.Render(scene);
        Assert.DoesNotContain(rig.Target.Graph.Describe().Resources, r => r.Name == "scene_hdr_ms");
        scene.Quality = QualityLevel.High;
        rig.Target.Render(scene);
        Assert.Contains(rig.Target.Graph.Describe().Resources, r => r.Name == "scene_hdr_ms");
        scene.Samples = 1;
        rig.Target.Render(scene);
        Assert.DoesNotContain(rig.Target.Graph.Describe().Resources, r => r.Name == "scene_hdr_ms");
    }

    [Fact]
    public void EnvironmentsWithoutAnExplicitSizeFollowTheLevel()
    {
        using var rig = new Rig(160, 120);
        var scene = Make(rig);
        var env = rig.Own(rig.Renderer.CreateEnvironment(HdrImage.CreateSky(64, 32, new Vector3(0.5f), new Vector3(0.7f), new Vector3(0.2f))));
        var fixedSize = rig.Own(rig.Renderer.CreateEnvironment(HdrImage.CreateSky(64, 32, new Vector3(0.5f), new Vector3(0.7f), new Vector3(0.2f)), 128));
        Assert.Equal(256, env.Size);
        scene.Environment.Map = env;
        scene.Quality = QualityLevel.Low;
        rig.Target.Render(scene);
        Assert.Equal(64, env.Size);
        Assert.Equal(7, env.SpecularMipCount);
        scene.Quality = QualityLevel.Medium;
        rig.Target.Render(scene);
        Assert.Equal(128, env.Size);
        scene.Environment.Map = fixedSize;
        scene.Quality = QualityLevel.Low;
        rig.Target.Render(scene);
        Assert.Equal(128, fixedSize.Size);
    }

    [Fact]
    public void LevelCaptures()
    {
        using var rig = new Rig(240, 180);
        var scene = Make(rig);
        var env = rig.Own(rig.Renderer.CreateEnvironment(HdrImage.CreateSky(128, 64, new Vector3(0.6f, 0.7f, 0.9f), new Vector3(0.7f), new Vector3(0.2f))));
        scene.Environment.Map = env;
        foreach (var level in new[] { QualityLevel.Low, QualityLevel.High })
        {
            scene.Quality = level;
            var px = rig.Capture(scene, "quality_" + level.ToString().ToLowerInvariant());
            Reference.Compare("quality_" + level.ToString().ToLowerInvariant(), px, 240, 180, meanTolerance: 0.6, maxTolerance: 16);
        }
    }
}
