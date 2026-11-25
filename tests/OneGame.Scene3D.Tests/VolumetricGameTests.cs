using System.Numerics;
using System.Text.Json;
using OneGame.Control;
using OneGame.Core;
using OneGame.Host;
using OneGame.Render3D;

namespace OneGame.Scene3D.Tests;

/// <summary>Light shafts of a game driven over live control: the op reads and sets the scene's settings and one light's opt-in.</summary>
public class VolumetricGameTests
{
    private const int W = 160;
    private const int H = 90;

    private sealed class Stage(HostResources resources, SceneView3D view) : Render3DStage(resources, view)
    {
        protected override void UpdateScene(OneGame.Render3D.Scene3D scene, in FrameInfo frame)
        {
            scene.Environment.Background = BackgroundMode.Color;
            scene.Environment.BackgroundColor = new Vector3(0.02f, 0.03f, 0.05f);
            scene.Post.Bloom = false;
            scene.Samples = 1;
        }
    }

    private sealed class ShaftGame : GameDefinition
    {
        public SceneView3D View { get; } = new();

        public override string Name => "ShaftGame";

        public override void RegisterEntities(EntityRegistry registry)
        {
        }

        public override void Setup(World world)
        {
        }

        public override void DefineScreens(ScreenSet screens) => screens.Add("main", _ => OneGame.UI.Ui.Column());

        public override void DefineFonts(OneGame.Text.FontLibrary fonts) => fonts.Add(File.ReadAllBytes(ManualSceneTests.FontPath()));

        public override IRenderStage CreateRenderStage(HostResources resources) => new Stage(resources, View);
    }

    private static JsonElement Call(ControlSurface surface, string op, string args = "{}") => JsonDocument.Parse(surface.Call(op, args)).RootElement.Clone();

    private static double Mean(CapturedFrame f)
    {
        long sum = 0;
        for (var i = 0; i < f.Rgba.Length; i += 4)
        {
            sum += f.Rgba[i] + f.Rgba[i + 1] + f.Rgba[i + 2];
        }

        return sum / (double)(f.Rgba.Length / 4);
    }

    [Fact]
    public void TheVolumetricOpReadsAndSetsTheSettingsAndTheLightsOptIn()
    {
        var definition = new ShaftGame();
        definition.View.Describer = (_, w) => w.DirectionalLight(new Vector3(0f, -0.2f, -1f), new Vector3(1f, 0.95f, 0.85f), 3f);
        definition.View.CameraOverride = new ViewCamera(Matrix4x4.CreateTranslation(0, 1, 5), 50f * MathF.PI / 180f, 0f, 0.1f, 100f, default);
        using var game = GameInstance.Create(definition, new GameOptions { Headless = true, Gpu = GpuMode.Required });
        var surface = new ControlSurface(game);
        var plain = game.Capture(W, H);

        var before = Call(surface, "volumetric");
        Assert.False(before.GetProperty("enabled").GetBoolean());
        Assert.False(before.GetProperty("active").GetBoolean());
        Assert.True(before.GetProperty("allowed").GetBoolean());
        Assert.Equal(1, before.GetProperty("lights").GetArrayLength());
        Assert.False(before.GetProperty("lights")[0].GetProperty("volumetric").GetBoolean());

        // Enabled, but no light has opted in: still the plain frame and no passes.
        Call(surface, "volumetric", """{"enabled":true,"density":0.05,"intensity":8,"anisotropy":0.3,"maxDistance":50}""");
        Assert.Equal(plain.Rgba, game.Capture(W, H).Rgba);
        Assert.False(Call(surface, "volumetric").GetProperty("active").GetBoolean());

        var set = Call(surface, "volumetric", """{"light":0,"lightEnabled":true,"lightIntensity":2}""");
        Assert.Equal(0.05, set.GetProperty("density").GetDouble(), 6);
        Assert.Equal(0.3, set.GetProperty("anisotropy").GetDouble(), 6);
        Assert.True(set.GetProperty("lights")[0].GetProperty("volumetric").GetBoolean());
        Assert.Equal(2.0, set.GetProperty("lights")[0].GetProperty("intensity").GetDouble());
        var lit = game.Capture(W, H);
        Assert.Empty(game.TakeGpuValidationMessages());
        Assert.True(Call(surface, "volumetric").GetProperty("active").GetBoolean());
        Assert.True(Mean(lit) > Mean(plain) + 5, $"the air in front of the camera glows: {Mean(plain):F1} to {Mean(lit):F1}");
        Assert.Contains("light_shafts", Call(surface, "quality").GetProperty("passes").EnumerateArray().Select(p => p.GetString()));

        // The level bounds it: Low has none.
        Call(surface, "quality", """{"level":"low"}""");
        game.Capture(W, H);
        var low = Call(surface, "volumetric");
        Assert.False(low.GetProperty("allowed").GetBoolean());
        Assert.False(low.GetProperty("active").GetBoolean());
        Call(surface, "quality", """{"level":"high"}""");

        Assert.Throws<ControlException>(() => surface.Call("volumetric", """{"density":-1}"""));
        Assert.Throws<ControlException>(() => surface.Call("volumetric", """{"anisotropy":2}"""));
        Assert.Throws<ControlException>(() => surface.Call("volumetric", """{"maxDistance":0}"""));
        Assert.Throws<ControlException>(() => surface.Call("volumetric", """{"light":5,"lightEnabled":true}"""));
    }
}
