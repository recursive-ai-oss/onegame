using System.Numerics;
using OneGame.Core;
using OneGame.Gltf;
using OneGame.Host;
using OneGame.Platform;
using OneGame.Platform.Tests;
using OneGame.Render3D;
using OneGame.UI;

namespace OneGame.Windowed.Tests;

/// <summary>A crowd of posed, skinned copies drawn through one batch, shadows on, in a real X11 window.</summary>
public sealed class CrowdGame : GameDefinition
{
    public SceneView3D View { get; } = CreateView();

    public override string Name => "CrowdWindowed";

    public override void RegisterEntities(EntityRegistry registry)
    {
    }

    public override void Setup(World world)
    {
    }

    public override void DefineFonts(OneGame.Text.FontLibrary fonts) =>
        fonts.Add(File.ReadAllBytes(Path.Combine(Paths.RepoRoot, "testdata", "text", "fonts", "NotoSans-Subset-Regular.ttf")));

    public override void DefineScreens(ScreenSet screens) => screens.Add("main", _ => Ui.Column());

    public override IRenderStage CreateRenderStage(HostResources resources) => new Stage(resources, View);

    private static SceneView3D CreateView()
    {
        var view = new SceneView3D();
        var fox = GltfLoader.LoadFile(Path.Combine(Paths.RepoRoot, "testdata", "gltf", "Fox", "glTF", "Fox.gltf"));
        view.Models.Add("fox", fox);
        var poses = new NodePose[8];
        for (var i = 0; i < poses.Length; i++)
        {
            poses[i] = fox.CreateRestPose();
            fox.Animations[1].Apply(poses[i], i * 0.11f);
        }

        view.Describer = (_, w) =>
        {
            w.DirectionalLight(new Vector3(-0.5f, -1f, -0.4f), Vector3.One, 3f, castShadows: true);
            for (var i = 0; i < 80; i++)
            {
                var at = new Vector3(((i % 10) - 4.5f) * 3f, 0, -(i / 10) * 3f);
                w.Instance("fox", Matrix4x4.CreateScale(0.04f) * Matrix4x4.CreateRotationY(i * 0.7f) * Matrix4x4.CreateTranslation(at), poses[i % poses.Length]);
            }
        };
        view.CameraOverride = new ViewCamera(
            Matrix4x4.Invert(Matrix4x4.CreateLookAt(new Vector3(0f, 9f, 16f), new Vector3(0f, 0f, -10f), Vector3.UnitY), out var world) ? world : Matrix4x4.Identity,
            50f * MathF.PI / 180f, 0f, 0.1f, 200f, default);
        return view;
    }

    private sealed class Stage(HostResources resources, SceneView3D view) : Render3DStage(resources, view)
    {
        protected override void UpdateScene(Scene3D scene, in FrameInfo frame)
        {
            scene.Samples = 1;
            scene.Environment.Intensity = 0.6f;
        }
    }
}

public sealed class CrowdWindowedTests : IClassFixture<XvfbFixture>
{
    private readonly XvfbFixture _xvfb;

    public CrowdWindowedTests(XvfbFixture xvfb) => _xvfb = xvfb;

    [Fact]
    public void ThePosedCrowdIsInThePresentedFrame() => MainThread.Run(() =>
    {
        Assert.SkipUnless(_xvfb.Available, "Xvfb is not installed");
        var game = new CrowdGame();
        using var s = new Session(PlatformBackendKind.X11, _xvfb.Display!, game: game);
        s.StepUntil(() => s.Game.PresentedFrames >= 30, 600);
        var shot = s.Game.Capture(s.Game.Window.Width, s.Game.Window.Height, s.Game.UiScale);
        var shown = s.Game.CapturePresented()!;
        Session.Save("crowd-capture", shot);
        Session.Save("crowd-presented", shown);
        Console.WriteLine("crowd window adapter: " + s.Call("info").GetProperty("adapter"));

        var worst = 0;
        for (var i = 0; i < shot.Rgba.Length; i++)
        {
            worst = Math.Max(worst, Math.Abs(shot.Rgba[i] - shown.Rgba[i]));
        }

        Assert.True(worst <= 2, $"capture differs from the presented frame by up to {worst}");

        // The foxes are orange: red well above blue.
        var orange = 0;
        for (var i = 0; i < shown.Rgba.Length; i += 4)
        {
            orange += shown.Rgba[i] > shown.Rgba[i + 2] + 60 ? 1 : 0;
        }

        Assert.True(orange > 2000, $"only {orange} orange pixels");
        Assert.Empty(s.Validation());
    });
}
