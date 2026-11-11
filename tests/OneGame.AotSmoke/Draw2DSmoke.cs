using System.Numerics;
using OneGame.Atlas;
using OneGame.Core;
using OneGame.Gpu;
using OneGame.Host;
using OneGame.Render2D;
using OneGame.Text;
using OneGame.UI;

namespace OneGame.AotSmoke;

/// <summary>An entity that draws an animated sprite, shapes and text through the 2D happy path.</summary>
[Entity]
public partial class Mover2D : Entity, IView2D
{
    [Synced] public partial Fixed X { get; private set; }

    private SpriteSheet? _sheet;

    protected override void OnTick() => X += Fixed.One;

    [View]
    public void Draw(Draw2D d)
    {
        if (_sheet is null)
        {
            _sheet = SpriteSheet.Grid(d.Renderer!.CreateTexture(2, 1, [255, 0, 0, 255, 0, 0, 255, 255], TextureFilter.Nearest), 1, 1);
            _sheet.AddClip("blink", 0, 2, 4);
        }

        d.Follow(new Vector2(X.ToSingle(), 0));
        d.Sprite(_sheet, "blink", new Vector2(X.ToSingle(), 0), scale: 8);
        d.FillCircle(new Vector2(0, 20), 6, Color.Rgb(0x00ff00));
        d.Line(new Vector2(-30, -20), new Vector2(30, -20), Color.White, 2);
        d.Text("Silk", new Vector2(-40, 10), 14);
    }
}

/// <summary>The default render stage drawing <see cref="IView2D"/> entities under the native publish, offscreen and read back.</summary>
internal static class Draw2DSmoke
{
    public static string? Run(GpuDevice device, byte[] font)
    {
        var registry = new EntityRegistry();
        OneGame.Generated.OneGame_AotSmoke.EntityTypes.Register(registry);
        var world = new World(1, registry);
        world.Add(new Mover2D());
        var fonts = new FontLibrary();
        fonts.Add(font);
        using var stage = new Render2DStage(new HostResources(fonts, new FontTextMeasurer(fonts), new ArtAtlas(), GeneratedShaders.Instance));
        using var target = device.CreateTexture(new TextureDesc(96, 64, GpuFormat.Rgba8UnormSrgb, TextureUsage.RenderTarget | TextureUsage.CopySource));
        world.Step();
        stage.Render(device, target, new FrameInfo(96, 64, 1, world.Tick, new DisplayList(), world, Fixed.Zero));
        var px = new byte[96 * 64 * 4];
        device.ReadTexture(target, 0, 0, px);
        var green = 0;
        var white = 0;
        for (var i = 0; i < px.Length; i += 4)
        {
            green += px[i] < 60 && px[i + 1] > 200 && px[i + 2] < 60 ? 1 : 0;
            white += px[i] > 200 && px[i + 1] > 200 && px[i + 2] > 200 ? 1 : 0;
        }

        if (!(stage.Draw.SpriteCount == 3 && stage.Draw.TextCount == 1 && green > 20 && white > 20))
        {
            return $"sprites {stage.Draw.SpriteCount}, texts {stage.Draw.TextCount}, green px {green}, white px {white}";
        }

        // A stage overlay (text, outline and a wrapped GPU image over the UI) and a texture wrapped around a GPU image.
        using var r2 = Renderer2D.Create(device, GpuFormat.Rgba8UnormSrgb, GeneratedShaders.Instance);
        using var image = device.CreateTexture(new TextureDesc(8, 8, GpuFormat.Rgba8UnormSrgb, TextureUsage.RenderTarget | TextureUsage.Sampled));
        using var wrapped = r2.WrapTexture(image);
        r2.RebindTexture(wrapped, image);
        stage.AddOverlay(new SmokeOverlay(wrapped));
        stage.Render(device, target, new FrameInfo(96, 64, 1, world.Tick, new DisplayList(), world, Fixed.Zero));
        device.ReadTexture(target, 0, 0, px);
        var magenta = 0;
        for (var i = 0; i < px.Length; i += 4)
        {
            magenta += px[i] > 200 && px[i + 1] < 60 && px[i + 2] > 200 ? 1 : 0;
        }

        return magenta > 100 && wrapped.Width == 8 ? null : $"overlay: magenta px {magenta}, wrapped width {wrapped.Width}";
    }

    private sealed class SmokeOverlay(Texture2D texture) : IFrameOverlay
    {
        public void Draw(OverlayCanvas canvas)
        {
            canvas.Fill(0, 0, 20, 10, Color.Rgb(0xff00ff));
            canvas.Outline(30, 0, 20, 10, 1, Color.White);
            canvas.Text("ov", 2, 20, 10, Color.White);
            canvas.Image(60, 0, 8, 8, texture, Color.White);
        }
    }
}
