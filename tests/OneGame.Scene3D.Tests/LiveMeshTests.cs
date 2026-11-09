using System.Numerics;
using System.Text.Json;
using OneGame.Control;
using OneGame.Core;
using OneGame.Gpu;
using OneGame.Host;
using OneGame.Render3D;
using OneGame.UI;

namespace OneGame.Scene3D.Tests;

/// <summary>A game that shows meshes built at run time through a scene view, driven over live control.</summary>
public class LiveMeshTests
{
    private sealed class MeshStage(HostResources resources, SceneView3D view, Action<Material3D> made) : Render3DStage(resources, view)
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

    private sealed class MeshGame : GameDefinition
    {
        public SceneView3D View { get; } = new();

        public Material3D? Material;

        public override string Name => "MeshGame";

        public override void RegisterEntities(EntityRegistry registry)
        {
        }

        public override void Setup(World world)
        {
        }

        public override void DefineScreens(ScreenSet screens) => screens.Add("main", _ => Ui.Column(Ui.Label("x")));

        public override void DefineFonts(OneGame.Text.FontLibrary fonts) => fonts.Add(File.ReadAllBytes(ManualSceneTests.FontPath()));

        public override IRenderStage CreateRenderStage(HostResources resources) => new MeshStage(resources, View, m => Material = m);
    }

    private static JsonElement Call(ControlSurface surface, string op, string args = "{}") => JsonDocument.Parse(surface.Call(op, args)).RootElement.Clone();

    [Fact]
    public void MeshesShowInSceneModelsCaptureAndProjectOverLiveControl()
    {
        var definition = new MeshGame();
        definition.View.Describer = (_, w) =>
        {
            w.DirectionalLight(new Vector3(0.2f, -1f, -0.3f), Vector3.One, 3f);
            if (definition.View.Models.Contains("chunk a"))
            {
                w.Model("chunk a", Matrix4x4.CreateTranslation(-1.5f, 0, 0), new ModelOptions { Label = "chunk a" });
            }

            if (definition.View.Models.Contains("chunk b"))
            {
                w.Model("chunk b", Matrix4x4.CreateTranslation(1.5f, 0, 0), new ModelOptions { Label = "chunk b", Opacity = 0.5f });
            }
        };
        definition.View.CameraOverride = new ViewCamera(
            Matrix4x4.Invert(Matrix4x4.CreateLookAt(new Vector3(0, 6, 0.01f), Vector3.Zero, -Vector3.UnitZ), out var cw) ? cw : Matrix4x4.Identity,
            50f * MathF.PI / 180f, 0f, 0.1f, 100f, default);
        using var game = GameInstance.Create(definition, new GameOptions { Headless = true, Gpu = GpuMode.Required });
        var surface = new ControlSurface(game);
        game.Capture(160, 90);   // creates the renderer and its material

        var (v, i) = Tile();
        definition.View.Models.SetMesh("chunk a", definition.Material!, v, i);
        definition.View.Models.SetMesh("chunk b", definition.Material!, v, i);

        var listing = Call(surface, "scene_models");
        Assert.Equal(2, listing.GetProperty("count").GetInt32());
        Assert.Equal(2, listing.GetProperty("dynamic").GetInt32());
        var models = listing.GetProperty("models").EnumerateArray().ToArray();
        Assert.Equal("chunk a", models[0].GetProperty("label").GetString());
        Assert.Equal(1.0, models[0].GetProperty("opacity").GetDouble());
        Assert.Equal(0.5, models[1].GetProperty("opacity").GetDouble());
        Assert.Equal(2, models[0].GetProperty("triangles").GetInt32());
        Assert.True(models[0].GetProperty("dynamic").GetBoolean());

        // The next frame shows both tiles: opaque lime on the left, half faded on the right.
        var frame = game.Capture(160, 90);
        Assert.Empty(game.TakeGpuValidationMessages());
        (int R, int G, int B) At(int x, int y) => (frame.Rgba[((y * 160) + x) * 4], frame.Rgba[((y * 160) + x) * 4 + 1], frame.Rgba[((y * 160) + x) * 4 + 2]);
        var projected = Call(surface, "project", """{"point":[-1.5,0,0],"size":"160x90"}""");
        var (px, py) = ((int)projected.GetProperty("x").GetDouble(), (int)projected.GetProperty("y").GetDouble());
        var left = At(px, py);
        var right = At(160 - px, py);
        Assert.True(left.G > 120 && left.R < 80, $"solid tile {left}");
        Assert.True(right.G > 40 && right.G < left.G, $"faded tile {right} against {left}");

        // Removing a mesh takes it out of the listing and the frame.
        definition.View.Models.Remove("chunk b");
        Assert.Equal(1, Call(surface, "scene_models").GetProperty("count").GetInt32());
        frame = game.Capture(160, 90);
        Assert.True(At(160 - px, py).G < 60, "the removed tile is gone");
        Assert.Empty(game.TakeGpuValidationMessages());
    }

    [Fact]
    public void TheEnvironmentOpReadsAndSetsFogAndBlendLive()
    {
        var definition = new MeshGame();
        definition.View.Describer = (_, w) =>
        {
            w.DirectionalLight(new Vector3(0.2f, -1f, -0.3f), Vector3.One, 3f);
            if (definition.View.Models.Contains("chunk"))
            {
                w.Model("chunk", Matrix4x4.Identity, new ModelOptions { Label = "chunk" });
            }
        };
        definition.View.CameraOverride = new ViewCamera(
            Matrix4x4.Invert(Matrix4x4.CreateLookAt(new Vector3(0, 6, 0.01f), Vector3.Zero, -Vector3.UnitZ), out var cw) ? cw : Matrix4x4.Identity,
            50f * MathF.PI / 180f, 0f, 0.1f, 100f, default);
        using var game = GameInstance.Create(definition, new GameOptions { Headless = true, Gpu = GpuMode.Required });
        var surface = new ControlSurface(game);
        game.Capture(160, 90);
        var (v, i) = Tile();
        definition.View.Models.SetMesh("chunk", definition.Material!, v, i);

        var before = Call(surface, "environment");
        Assert.False(before.GetProperty("fogEnabled").GetBoolean());
        Assert.False(before.GetProperty("hasNextMap").GetBoolean());

        var set = Call(surface, "environment", """{"fog":true,"fogColor":[0.9,0.1,0.1],"fogStart":1,"fogEnd":2,"fogMaxDensity":1,"blend":0.25}""");
        Assert.True(set.GetProperty("fogEnabled").GetBoolean());
        Assert.Equal(2.0, set.GetProperty("fogEnd").GetDouble());
        Assert.Equal(0.25, set.GetProperty("blend").GetDouble());

        var frame = game.Capture(160, 90);
        Assert.Empty(game.TakeGpuValidationMessages());
        var projected = Call(surface, "project", """{"point":[0,0,0],"size":"160x90"}""");
        var o = ((((int)projected.GetProperty("y").GetDouble()) * 160) + (int)projected.GetProperty("x").GetDouble()) * 4;
        Assert.True(frame.Rgba[o] > 150 && frame.Rgba[o + 1] < 90, $"the tile is fog red: {frame.Rgba[o]},{frame.Rgba[o + 1]},{frame.Rgba[o + 2]}");

        Assert.Throws<ControlException>(() => surface.Call("environment", """{"fogEnd":0.5}"""));
        Assert.Throws<ControlException>(() => surface.Call("environment", """{"blend":2}"""));
        Call(surface, "environment", """{"fog":false}""");
        frame = game.Capture(160, 90);
        Assert.True(frame.Rgba[o + 1] > 120, "fog off again shows the lime tile");
    }

    private static (MeshVertex3D[], uint[]) Tile()
    {
        var n = Vector3.UnitY;
        return ([new(new(-1, 0, -1), n, new(0, 0)), new(new(1, 0, -1), n, new(1, 0)), new(new(1, 0, 1), n, new(1, 1)), new(new(-1, 0, 1), n, new(0, 1))], [0, 2, 1, 0, 3, 2]);
    }
}
