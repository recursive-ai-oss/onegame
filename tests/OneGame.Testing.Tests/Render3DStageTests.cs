using System.Numerics;
using OneGame.Gltf;
using OneGame.Host;
using OneGame.Render3D;
using OneGame.Text;

namespace OneGame.Testing.Tests;

/// <summary>A game whose render stage draws a glTF box in 3D under the UI.</summary>
public sealed class BoxGame : ShopGame
{
    public override string Name => "BoxGame";

    public override void DefineFonts(FontLibrary fonts) =>
        fonts.Add(File.ReadAllBytes(Path.Combine(GpuPaths.RepoRoot, "testdata", "text", "fonts", "NotoSans-Subset-Regular.ttf")));

    public override IRenderStage CreateRenderStage(HostResources resources) => new Stage(resources);

    private sealed class Stage(HostResources resources) : Render3DStage(resources)
    {
        private Model3D? _model;

        protected override void RendererCreated(Renderer3D renderer)
        {
            var gltf = GltfLoader.LoadFile(Path.Combine(GpuPaths.RepoRoot, "testdata", "gltf", "Box", "glTF-Embedded", "Box.gltf"));
            _model = renderer.Upload(gltf);
            Scene.Add(_model);
            Scene.Lights.Add(Light3D.Directional(new Vector3(-1, -1, -1), Vector3.One, 3f));
            var b = gltf.ComputeWorldBounds()!.Value;
            Scene.Camera = Camera3D.Frame(b.Min, b.Max, 30, 20, 160 / 120f);
        }

        public override void Dispose()
        {
            _model?.Dispose();
            base.Dispose();
        }
    }
}

/// <summary>The 3D stage through the control layer, driven the way a live session does.</summary>
public class Render3DStageTests
{
    [Fact]
    public void TheGraphReportListsThePbrPassesAndTheCaptureShowsTheModel()
    {
        using var cluster = TestCluster.Start(new BoxGame(), new TestOptions { Width = 160, Height = 120, Gpu = GpuMode.Required });
        var c = cluster.Client;
        c.Step(3);
        var report = c.Call("render_graph");
        foreach (var pass in new[] { "forward", "tonemap", "brdf_lut" })
        {
            Assert.Contains("\"" + pass + "\"", report, StringComparison.Ordinal);
        }

        var frame = c.Capture("160x120");
        var corner = frame.Rgba[0];
        var different = 0;
        for (var i = 0; i < frame.Rgba.Length; i += 4)
        {
            if (frame.Rgba[i] != corner)
            {
                different++;
            }
        }

        Assert.True(different > 500, $"only {different} pixels differ from the corner");
    }
}

/// <summary>A 3D stage whose scene update, or its 2D drawing over the scene, throws while a flag is set.</summary>
public sealed class ThrowingSceneGame : ShopGame
{
    /// <summary>Where the stage throws; read by the stage on every frame.</summary>
    public static string Throw { get; set; } = "";

    public override string Name => "ThrowingSceneGame";

    public override void DefineFonts(FontLibrary fonts) =>
        fonts.Add(File.ReadAllBytes(Path.Combine(GpuPaths.RepoRoot, "testdata", "text", "fonts", "NotoSans-Subset-Regular.ttf")));

    public override IRenderStage CreateRenderStage(HostResources resources) => new Stage(resources);

    private sealed class Stage(HostResources resources) : Render3DStage(resources)
    {
        private Model3D? _model;

        protected override void RendererCreated(Renderer3D renderer)
        {
            var gltf = GltfLoader.LoadFile(Path.Combine(GpuPaths.RepoRoot, "testdata", "gltf", "Box", "glTF-Embedded", "Box.gltf"));
            _model = renderer.Upload(gltf);
            Scene.Add(_model);
            Scene.Lights.Add(Light3D.Directional(new Vector3(-1, -1, -1), Vector3.One, 3f));
            var b = gltf.ComputeWorldBounds()!.Value;
            Scene.Camera = Camera3D.Frame(b.Min, b.Max, 30, 20, 160 / 120f);
        }

        protected override void UpdateScene(Scene3D scene, in FrameInfo frame)
        {
            if (Throw == "scene")
            {
                throw new InvalidOperationException("the game's scene bug");
            }
        }

        protected override void DrawOverUi(OneGame.Render2D.Renderer2D renderer, in FrameInfo frame)
        {
            // The 3D passes are already in the graph and the 2D renderer is mid-frame when this runs.
            if (Throw == "over")
            {
                throw new InvalidOperationException("the game's overlay bug");
            }
        }

        public override void Dispose()
        {
            _model?.Dispose();
            base.Dispose();
        }
    }
}

public class Render3DStageRecoveryTests
{
    [Theory]
    [InlineData("scene", "scene bug")]
    [InlineData("over", "overlay bug")]
    public void AFrameThatThrowsReportsTheGamesErrorAndLaterFramesAndCapturesWork(string where, string message)
    {
        using var cluster = TestCluster.Start(new ThrowingSceneGame(), new TestOptions { Width = 160, Height = 120, Gpu = GpuMode.Required });
        var c = cluster.Client;
        c.Step(2);
        var good = c.Capture("160x120");
        ThrowingSceneGame.Throw = where;
        try
        {
            var ex = Record.Exception(() => c.Capture("160x120"));
            Assert.NotNull(ex);
            Assert.Contains(message, ex!.ToString(), StringComparison.Ordinal);
            // A second failing frame is the game's error again, not "the previous frame was never executed".
            var again = Record.Exception(() => c.Capture("160x120"));
            Assert.Contains(message, again!.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            ThrowingSceneGame.Throw = "";
        }

        c.Step(2);
        var after = c.Capture("160x120");
        Assert.Equal(good.Rgba, after.Rgba);
    }
}
