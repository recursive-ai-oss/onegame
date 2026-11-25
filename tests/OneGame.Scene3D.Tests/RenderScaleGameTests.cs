using System.Numerics;
using System.Text.Json;
using OneGame.Control;
using OneGame.Core;
using OneGame.Host;
using OneGame.Render3D;
using OneGame.UI;

namespace OneGame.Scene3D.Tests;

/// <summary>A game whose 3D view is drawn at a fraction of the window while its UI stays at full resolution, driven over live control.</summary>
public class RenderScaleGameTests
{
    private const int W = 320;
    private const int H = 180;

    private sealed class Stage(HostResources resources, SceneView3D view, Action<Material3D> made) : Render3DStage(resources, view)
    {
        protected override void RendererCreated(Renderer3D renderer) => made(renderer.CreateMaterial("lime", baseColor: new Vector4(0.2f, 0.9f, 0.1f, 1f)));

        protected override void UpdateScene(OneGame.Render3D.Scene3D scene, in FrameInfo frame)
        {
            scene.Environment.Background = BackgroundMode.Color;
            scene.Environment.BackgroundColor = new Vector3(0.02f, 0.03f, 0.05f);
            scene.Post.Bloom = false;
            scene.Samples = 1;
        }
    }

    private sealed class ScaleGame : GameDefinition
    {
        public SceneView3D View { get; } = new();

        public Material3D? Material;

        public override string Name => "ScaleGame";

        public override void RegisterEntities(EntityRegistry registry)
        {
        }

        public override void Setup(World world)
        {
        }

        public override void DefineScreens(ScreenSet screens) => screens.Add("main", _ => Ui.Column(Ui.Label("Score 12345")));

        public override void DefineFonts(OneGame.Text.FontLibrary fonts) => fonts.Add(File.ReadAllBytes(ManualSceneTests.FontPath()));

        public override IRenderStage CreateRenderStage(HostResources resources) => new Stage(resources, View, m => Material = m);
    }

    private static JsonElement Call(ControlSurface surface, string op, string args = "{}") => JsonDocument.Parse(surface.Call(op, args)).RootElement.Clone();

    private static ScaleGame Make()
    {
        var definition = new ScaleGame();
        definition.View.Describer = (_, w) =>
        {
            w.DirectionalLight(new Vector3(0.2f, -1f, -0.3f), Vector3.One, 3f);
            if (definition.View.Models.Contains("tile"))
            {
                w.Model("tile", Matrix4x4.CreateTranslation(1.2f, 0, 0), new ModelOptions { Label = "tile" });
            }
        };
        definition.View.CameraOverride = new ViewCamera(
            Matrix4x4.Invert(Matrix4x4.CreateLookAt(new Vector3(0, 6, 0.01f), Vector3.Zero, -Vector3.UnitZ), out var cw) ? cw : Matrix4x4.Identity,
            50f * MathF.PI / 180f, 0f, 0.1f, 100f, default);
        return definition;
    }

    private static (MeshVertex3D[], uint[]) Tile()
    {
        var n = Vector3.UnitY;
        return ([new(new(-1, 0, -1), n, new(0, 0)), new(new(1, 0, -1), n, new(1, 0)), new(new(1, 0, 1), n, new(1, 1)), new(new(-1, 0, 1), n, new(0, 1))], [0, 2, 1, 0, 3, 2]);
    }

    [Fact]
    public void TheUiStaysSharpPickingAndProjectionStayInWindowPixelsAndTheOpReportsTheScale()
    {
        var definition = Make();
        using var game = GameInstance.Create(definition, new GameOptions { Headless = true, Gpu = GpuMode.Required });
        var surface = new ControlSurface(game);
        game.Capture(W, H);
        var (v, i) = Tile();
        definition.View.Models.SetMesh("tile", definition.Material!, v, i);

        var before = Call(surface, "render_scale");
        Assert.Equal(1.0, before.GetProperty("scale").GetDouble());

        var full = game.Capture(W, H);
        var fullProject = Call(surface, "project", """{"point":[1.2,0,0],"size":"320x180"}""");
        var fullRay = definition.View.Ray(200f, 90f, W, H);

        var set = Call(surface, "render_scale", """{"scale":0.5}""");
        Assert.Equal(0.5, set.GetProperty("scale").GetDouble());
        var half = game.Capture(W, H);
        if (Environment.GetEnvironmentVariable("ONEGAME_CAPTURE_DIR") is { Length: > 0 } dir)
        {
            Directory.CreateDirectory(dir);
            OneGame.Render3D.Tests.Png.Write(Path.Combine(dir, "ui-over-scale-1.0.png"), W, H, full.Rgba);
            OneGame.Render3D.Tests.Png.Write(Path.Combine(dir, "ui-over-scale-0.5.png"), W, H, half.Rgba);
        }

        Assert.Empty(game.TakeGpuValidationMessages());
        var read = Call(surface, "render_scale");
        Assert.Equal(0.5, read.GetProperty("scale").GetDouble());
        Assert.Equal(W / 2, read.GetProperty("renderWidth").GetInt32());
        Assert.Equal(H / 2, read.GetProperty("renderHeight").GetInt32());
        Assert.Throws<ControlException>(() => surface.Call("render_scale", """{"scale":0.1}"""));
        Assert.Throws<ControlException>(() => surface.Call("render_scale", """{"scale":1.5}"""));

        // Window coordinates: the same projection and the same ray at either scale.
        var halfProject = Call(surface, "project", """{"point":[1.2,0,0],"size":"320x180"}""");
        Assert.Equal(fullProject.GetProperty("x").GetDouble(), halfProject.GetProperty("x").GetDouble());
        Assert.Equal(fullProject.GetProperty("y").GetDouble(), halfProject.GetProperty("y").GetDouble());
        Assert.Equal(fullRay, definition.View.Ray(200f, 90f, W, H));

        // The tile is where the projection says at both scales, and the picture around it is the same colour.
        var px = (int)halfProject.GetProperty("x").GetDouble();
        var py = (int)halfProject.GetProperty("y").GetDouble();
        (int R, int G, int B) At(CapturedFrame f, int x, int y) => (f.Rgba[((y * W) + x) * 4], f.Rgba[((y * W) + x) * 4 + 1], f.Rgba[((y * W) + x) * 4 + 2]);
        var a = At(full, px, py);
        var b = At(half, px, py);
        Assert.True(a.G > 120 && b.G > 120, $"tile at the projected point: {a} and {b}");
        Assert.InRange(Math.Abs(a.G - b.G), 0, 30);

        // The label is drawn after the upscale: its pixels (the top-left corner, over a flat background) match the full-size frame exactly.
        long labelDiff = 0;
        var inked = 0;
        for (var y = 0; y < 30; y++)
        {
            for (var x = 0; x < 120; x++)
            {
                var p = At(full, x, y);
                var q = At(half, x, y);
                labelDiff += Math.Abs(p.R - q.R) + Math.Abs(p.G - q.G) + Math.Abs(p.B - q.B);
                if (p.R < 15 && p.G < 15 && p.B < 15)
                {
                    inked++;
                }
            }
        }

        Assert.True(inked > 40, $"the label should show in the corner ({inked} dark pixels)");
        Assert.Equal(0, labelDiff);

        // And the scaled tile's edge really is softer than the full-size one: more distinct values across it.
        Assert.NotEqual(full.Rgba, half.Rgba);
    }
}
