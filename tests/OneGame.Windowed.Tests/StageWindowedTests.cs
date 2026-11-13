using System.Numerics;
using OneGame.Core;
using RecursiveAI.OneGpu;
using OneGame.Host;
using OneGame.Platform;
using OneGame.Platform.Tests;
using OneGame.Render2D;
using OneGame.Render3D;
using OneGame.Text;
using OneGame.UI;

namespace OneGame.Windowed.Tests;

/// <summary>A stage with an overlay and a sampled 3D target drawn as a sprite and a UI image, into a window whose swapchain format is not the target's.</summary>
public sealed class StageGame : GameDefinition
{
    public override string Name => "StageGame";

    public override void RegisterEntities(EntityRegistry registry)
    {
    }

    public override void Setup(World world)
    {
    }

    public override void DefineFonts(FontLibrary fonts) =>
        fonts.Add(File.ReadAllBytes(Path.Combine(Paths.RepoRoot, "testdata", "text", "fonts", "NotoSans-Subset-Regular.ttf")));

    public override void DefineScreens(ScreenSet screens) => screens.Add("main", _ => Ui.Column().Background(Color.Rgb(0x203040)));

    public override IRenderStage CreateRenderStage(HostResources resources) => new Stage(resources);

    private sealed class Stage : Render2DStage
    {
        private Renderer3D? _r3;
        private Offscreen3D? _view;
        private Texture2D? _tex;
        private readonly Scene3D _scene = new() { Samples = 1 };

        public Stage(HostResources resources)
            : base(resources) => AddOverlay(new Mark());

        protected override void DrawOverUi(Renderer2D renderer, in FrameInfo frame)
        {
            if (_view is null)
            {
                _r3 = new Renderer3D(renderer.Device);
                _scene.Environment.Background = BackgroundMode.Color;
                _scene.Environment.BackgroundColor = new Vector3(0.8f, 0.1f, 0.1f);
                _scene.Post.Bloom = false;
                _view = new Offscreen3D(_r3, 64, 64, GpuFormat.Rgba8UnormSrgb, sampled: true);
                _tex = renderer.WrapTexture(_view.Target);
            }

            _view.Render(_scene);
            var sprites = new Scene2D();
            sprites.Draw(sprites.DefaultLayer, Sprite.Create(_tex!, Vector2.Zero) with { Pivot = Vector2.Zero });
            renderer.DrawScene(sprites, new Camera2D { Position = new Vector2(frame.Width / 2f, frame.Height / 2f), Zoom = 1f });
            renderer.Ui.Image(new Rect(100, 0, 64, 64), _tex!, Color.White);
        }

        public override void Dispose()
        {
            _tex?.Dispose();
            _view?.Dispose();
            _r3?.Dispose();
            base.Dispose();
        }
    }

    private sealed class Mark : IFrameOverlay
    {
        public void Draw(OverlayCanvas canvas)
        {
            canvas.Fill(300, 300, 40, 20, new Color(255, 0, 255, 255));
            canvas.Text("stage", 200, 300, 16, Color.White);
        }
    }
}

/// <summary>The stage extras in a real window: the capture equals the presented frame, and validation stays silent.</summary>
public sealed class StageWindowedTests : IClassFixture<XvfbFixture>
{
    private readonly XvfbFixture _xvfb;

    public StageWindowedTests(XvfbFixture xvfb) => _xvfb = xvfb;

    [Fact]
    public void AnOverlayAndAWrappedTargetShowInTheWindowAndInACapture() => MainThread.Run(() =>
    {
        Assert.SkipUnless(_xvfb.Available, "Xvfb is not installed");
        using var s = new Session(PlatformBackendKind.X11, _xvfb.Display!, game: new StageGame());
        s.Call("overlay", """{"visible":true}""");
        s.StepUntil(() => s.Game.PresentedFrames >= 8);
        var shot = s.Game.Capture(s.Game.Window.Width, s.Game.Window.Height, s.Game.UiScale);
        var shown = s.Game.CapturePresented()!;
        Session.Save("stage-capture", shot);
        Session.Save("stage-presented", shown);

        uint At(CapturedFrame f, int x, int y) => BitConverter.ToUInt32(f.Rgba, ((y * f.Width) + x) * 4);
        foreach (var f in new[] { shot, shown })
        {
            Assert.Equal(0xFFFF00FFu, At(f, 320, 310));
            // The sprite and the UI image both show the 3D backdrop (red, tone mapped).
            for (var x = 30; x <= 130; x += 100)
            {
                var p = At(f, x, 30);
                Assert.InRange(p & 0xFF, 225u, 250u);
                Assert.InRange((p >> 8) & 0xFF, 50u, 80u);
            }
        }

        // The panel's timings and histories change from frame to frame, so the comparison leaves its corner out.
        var worst = 0;
        for (var y = 0; y < shot.Height; y++)
        {
            for (var x = 0; x < shot.Width; x++)
            {
                if (x >= 280 && y < 150)
                {
                    continue;
                }

                for (var c = 0; c < 4; c++)
                {
                    var i = (((y * shot.Width) + x) * 4) + c;
                    worst = Math.Max(worst, Math.Abs(shot.Rgba[i] - shown.Rgba[i]));
                }
            }
        }

        Assert.True(worst <= 2, $"capture differs from the presented frame by up to {worst}");
        Assert.Empty(s.Validation());
    });
}
