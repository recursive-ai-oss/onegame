using System.Numerics;
using OneGame.Core;
using OneGame.Host;
using OneGame.Render2D;
using OneGame.UI;
using WebGame.Shaders;

namespace WebGame;

/// <summary>
/// A bright square on a layer with a post chain (bloom, then the game's own tint pass) and a render texture drawn as a sprite, so a browser
/// run can check that targets and chains work on WebGPU: the square's halo, the tint, and the texture's colour.
/// </summary>
[Entity]
public partial class Glow : Entity, IView2D
{
    private RenderTexture2D? _badge;
    private RenderTexture2D? _probe;
    private Texture2D? _white;
    private Material? _tint;
    private PostChain? _chain;

    [View]
    public void Draw(Draw2D d)
    {
        if (_chain is null)
        {
            _tint = d.Renderer!.CreateMaterial(new ShaderCode(RecursiveAI.OneGpu.ShaderStage.Pixel, Tint.PSMain.EntryPoint, Tint.PSMain.SpirV.ToArray(), Tint.PSMain.Msl.ToArray(), Tint.PSMain.MslFunctionName, Tint.PSMain.Layout, Tint.PSMain.Wgsl.ToArray(), Tint.PSMain.WgslEntryPoint), "glow tint");
            _tint.SetParameter(0, new Vector4(0.5f, 1f, 1f, 0));
            _chain = d.CreatePostChain().AddBloom(threshold: 0.5f).AddMaterial(_tint);
            _badge = d.RenderTexture("badge", 32, 32);
            _probe = d.RenderTexture("probe", 320, 200);
        }

        // The layer that fills the texture comes first, then the layer with the chain, then the layer that shows the texture.
        var fill = d.RenderLayer("badge", _badge!, new Color(255, 0, 255, 255));
        var glow = d.GetLayer("glow");
        glow.Post = _chain;
        var show = d.GetLayer("badge-show");
        // A texture cleared straight on the renderer from view code, between the frame's other drawing: only the texture may be cleared.
        var r = d.Renderer!;
        _white ??= r.CreateTexture(1, 1, [255, 255, 255, 255], TextureFilter.Nearest);
        var cam = new Camera2D { Position = new Vector2(160, 100), Zoom = 1 };
        var before = new Scene2D();
        before.Draw(before.DefaultLayer, Sprite.Create(_white, new Vector2(20, 170)) with { Scale = new Vector2(20, 20), Tint = new Color(255, 0, 0, 255) });
        r.DrawScene(before, cam);
        r.BeginTarget(_probe!, new Color(0, 0, 255, 255));
        var inner = new Scene2D();
        inner.Draw(inner.DefaultLayer, Sprite.Create(_white, new Vector2(160, 100)) with { Scale = new Vector2(100, 100), Tint = new Color(255, 255, 0, 255) });
        r.DrawScene(inner, cam);
        r.EndTarget();
        var after = new Scene2D();
        after.Draw(after.DefaultLayer, Sprite.Create(_white, new Vector2(300, 170)) with { Scale = new Vector2(20, 20), Tint = new Color(0, 255, 0, 255) });
        r.DrawScene(after, cam);
        d.Layer = fill;
        d.FillRect(new Rect(0, 0, 32, 16), new Color(255, 0, 255, 255));
        d.Layer = glow;
        d.FillRect(new Rect(-80, -30, 40, 40), Color.White);
        d.Layer = show;
        d.Sprite(_badge!.Texture, new Vector2(40, -60));
        d.Sprite(_probe!.Texture, new Vector2(-40, -60), scale: 0.1f);
    }
}
