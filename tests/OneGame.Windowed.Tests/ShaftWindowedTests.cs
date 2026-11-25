using System.Numerics;
using OneGame.Core;
using OneGame.Host;
using OneGame.Platform;
using OneGame.Platform.Tests;
using OneGame.Render3D;
using OneGame.UI;
using WaterExample;

namespace OneGame.Windowed.Tests;

/// <summary>A sun low behind a tower, with light shafts on: the passes run on the window's device, the presented frame equals the capture, and validation stays silent.</summary>
public sealed class ShaftGame : GameDefinition
{
    /// <summary>Whether the stage asks for shafts; the test flips it.</summary>
    public bool Shafts { get; set; } = true;

    public SceneView3D View { get; } = CreateView();

    public override string Name => "ShaftWindowed";

    public override void RegisterEntities(EntityRegistry registry)
    {
    }

    public override void Setup(World world)
    {
    }

    public override void DefineFonts(OneGame.Text.FontLibrary fonts) =>
        fonts.Add(File.ReadAllBytes(Path.Combine(Paths.RepoRoot, "testdata", "text", "fonts", "NotoSans-Subset-Regular.ttf")));

    public override void DefineScreens(ScreenSet screens) => screens.Add("main", _ => Ui.Column());

    public override IRenderStage CreateRenderStage(HostResources resources) => new Stage(resources, View, this);

    private static SceneView3D CreateView()
    {
        var view = new SceneView3D();
        view.Models.Add("pond", PondModels.Scenery);
        view.Describer = (_, w) =>
        {
            w.Model("pond", Matrix4x4.Identity, pickable: false);
            w.DirectionalLight(new Vector3(0.25f, -0.18f, 1f), new Vector3(1f, 0.93f, 0.8f), 3f, castShadows: true);
        };
        view.CameraOverride = new ViewCamera(
            Matrix4x4.Invert(Matrix4x4.CreateLookAt(new Vector3(2f, 1.6f, 9f), new Vector3(-1f, 1.8f, -6f), Vector3.UnitY), out var world) ? world : Matrix4x4.Identity,
            50f * MathF.PI / 180f, 0f, 0.1f, 200f, default);
        return view;
    }

    private sealed class Stage(HostResources resources, SceneView3D view, ShaftGame game) : Render3DStage(resources, view)
    {
        protected override void UpdateScene(Scene3D scene, in FrameInfo frame)
        {
            scene.Samples = 1;
            scene.Environment.Intensity = 0.2f;
            scene.Environment.Background = BackgroundMode.Color;
            scene.Environment.BackgroundColor = new Vector3(0.02f, 0.03f, 0.05f);
            scene.Post.Bloom = false;
            scene.Volumetric.Enabled = game.Shafts;
            scene.Volumetric.Density = 0.012f;
            scene.Volumetric.Intensity = 2f;
            scene.Volumetric.MaxDistance = 50f;
            if (scene.Lights.Count > 0)
            {
                scene.Lights[0].Volumetric = true;
            }
        }
    }
}

public sealed class ShaftWindowedTests : IClassFixture<XvfbFixture>
{
    private readonly XvfbFixture _xvfb;

    public ShaftWindowedTests(XvfbFixture xvfb) => _xvfb = xvfb;

    [Fact]
    public void TheShaftsAreInThePresentedFrame() => MainThread.Run(() =>
    {
        Assert.SkipUnless(_xvfb.Available, "Xvfb is not installed");
        var game = new ShaftGame();
        using var s = new Session(PlatformBackendKind.X11, _xvfb.Display!, game: game);
        s.StepUntil(() => s.Game.PresentedFrames >= 30, 600);
        var shot = s.Game.Capture(s.Game.Window.Width, s.Game.Window.Height, s.Game.UiScale);
        var shown = s.Game.CapturePresented()!;
        Session.Save("shafts-capture", shot);
        Session.Save("shafts-presented", shown);
        Console.WriteLine("shaft window adapter: " + s.Call("info").GetProperty("adapter"));
        Assert.True(s.Call("volumetric").GetProperty("active").GetBoolean(), "the shaft passes ran");

        var worst = 0;
        for (var i = 0; i < shot.Rgba.Length; i++)
        {
            worst = Math.Max(worst, Math.Abs(shot.Rgba[i] - shown.Rgba[i]));
        }

        Assert.True(worst <= 2, $"capture differs from the presented frame by up to {worst}");

        // Switched off, the same frame is darker: the air no longer glows.
        game.Shafts = false;
        s.Game.Step(3);
        var off = s.Game.Capture(s.Game.Window.Width, s.Game.Window.Height, s.Game.UiScale);
        Session.Save("shafts-off", off);
        Assert.False(s.Call("volumetric").GetProperty("active").GetBoolean());
        long on = 0;
        long none = 0;
        for (var i = 0; i < shot.Rgba.Length; i += 4)
        {
            on += shot.Rgba[i] + shot.Rgba[i + 1] + shot.Rgba[i + 2];
            none += off.Rgba[i] + off.Rgba[i + 1] + off.Rgba[i + 2];
        }

        Assert.True(on > none * 1.05, $"shafts add light: {none} without, {on} with");
        Assert.Empty(s.Validation());
    });
}
