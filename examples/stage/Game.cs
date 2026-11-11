using System;
using System.Numerics;
using OneGame.Core;
using OneGame.Gpu;
using OneGame.Host;
using OneGame.Render2D;
using OneGame.Render3D;
using OneGame.UI;

namespace StageExample;

/// <summary>A counter the simulation advances; the stage below only draws.</summary>
[Entity]
public partial class Clock : Entity
{
    [Synced] public partial int Beats { get; private set; }

    protected override void OnTick() => Beats++;
}

public sealed class StageGame : GameDefinition
{
    public override string Name => "Stage";

    public override void RegisterEntities(EntityRegistry registry) => OneGame.Generated.StageExample.EntityTypes.Register(registry);

    public override void Setup(World world) => world.Add(new Clock());

    public override void DefineScreens(ScreenSet screens) => screens.Add("main", _ => Ui.Column(Ui.Label("main screen")));

    public override IRenderStage CreateRenderStage(HostResources resources) => new Stage(resources);
}

/// <summary>
/// A custom stage with two extras on top of the 2D renderer: an overlay of its own (text and rectangles above the UI and the debug panel's
/// neighbour, never replacing it) and a 3D view drawn into an offscreen target that sprites and the UI canvas show without a readback.
/// </summary>
public sealed class Stage : Render2DStage
{
    private readonly Assets _assets;
    private Renderer3D? _r3;
    private Offscreen3D? _view;
    private Asset<OneGame.Gltf.GltfModel>? _model;
    private Model3D? _box;
    private Texture2D? _viewTexture;
    private readonly Scene3D _scene = new() { Samples = 1 };
    private int _tick;

    public Stage(HostResources resources)
        : base(resources)
    {
        _assets = resources.Assets!;
        // Overlays draw after the UI, in the same batch, using the game's fonts. The debug panel has its own slot and stays on top.
        AddOverlay(new Banner(() => _tick));
    }

    protected override void DrawOverUi(Renderer2D renderer, in FrameInfo frame)
    {
        _tick = (int)frame.Tick;
        var model = _model ??= _assets.LoadModel("art/Box.gltf");
        if (!model.IsReady)
        {
            return;
        }

        if (_view is null)
        {
            _r3 = new Renderer3D(renderer.Device);
            _box = _r3.Upload(model.Value);
            _scene.Add(_box);
            _scene.Lights.Add(Light3D.Directional(new Vector3(-1, -1, -1), Vector3.One, 3f));
            _scene.Environment.Background = BackgroundMode.Color;
            _scene.Environment.BackgroundColor = new Vector3(0.05f, 0.15f, 0.3f);
            _scene.Post.Bloom = false;
            // Sampled targets can be drawn by 2D; WrapTexture shares the image instead of copying it.
            _view = new Offscreen3D(_r3, 96, 96, GpuFormat.Rgba8UnormSrgb, sampled: true);
            _viewTexture = renderer.WrapTexture(_view.Target);
        }

        // Turn the box with the tick, render it, then draw the result twice: as a sprite and as a UI image.
        _scene.Camera = Camera3D.LookAt(new Vector3(MathF.Sin(_tick * 0.05f) * 3, 1.5f, MathF.Cos(_tick * 0.05f) * 3), Vector3.Zero, Vector3.UnitY);
        _view.Render(_scene);
        var sprites = new Scene2D();
        sprites.Draw(sprites.DefaultLayer, Sprite.Create(_viewTexture!, new Vector2(24, 24)) with { Pivot = Vector2.Zero });
        renderer.DrawScene(sprites, new Camera2D { Position = new Vector2(frame.Width / 2f, frame.Height / 2f), Zoom = 1f });
        renderer.Ui.Image(new Rect(24 + 96 + 8, 24, 96, 96), _viewTexture!, Color.White);
    }

    public override void Dispose()
    {
        // The wrapper goes first; the image belongs to the Offscreen3D, which goes before the renderer it draws with.
        _viewTexture?.Dispose();
        _view?.Dispose();
        _box?.Dispose();
        _r3?.Dispose();
        base.Dispose();
    }

    /// <summary>Text and rectangles above everything the game drew, in framebuffer pixels.</summary>
    private sealed class Banner(Func<int> tick) : IFrameOverlay
    {
        public void Draw(OverlayCanvas canvas)
        {
            var w = canvas.Width;
            var h = canvas.Height;
            canvas.Fill(w - 150, h - 40, 140, 30, new Color(20, 20, 20, 200));
            canvas.Outline(w - 150, h - 40, 140, 30, 2, Color.Rgb(0xffcc33));
            canvas.Text("tick " + tick().ToString(System.Globalization.CultureInfo.InvariantCulture), w - 140, h - 33, 16, Color.White);
        }
    }
}
