using System.Numerics;
using OneGame.Core;
using OneGame.Host;
using OneGame.Render2D;
using OneGame.UI;
using WebGame.Shaders;

namespace WebGame;

/// <summary>
/// Passes whose first draw is a material of the game's own: a layer that draws into a render texture, and a layer with a post chain.
/// Neither has a plain sprite before it, so each pass needs its frame bind group set before the material's draw.
/// </summary>
[Entity]
public partial class Layers : Entity, IView2D
{
    private Texture2D? _white;
    private Material? _tint;
    private RenderTexture2D? _target;
    private PostChain? _chain;

    [View]
    public void Draw(Draw2D d)
    {
        if (_tint is null)
        {
            _white = d.Renderer!.CreateTexture(1, 1, [255, 255, 255, 255], TextureFilter.Nearest);
            _tint = d.Renderer.CreateMaterial(new ShaderCode(RecursiveAI.OneGpu.ShaderStage.Pixel, Tint.PSMain.EntryPoint, Tint.PSMain.SpirV.ToArray(), Tint.PSMain.Msl.ToArray(), Tint.PSMain.MslFunctionName, Tint.PSMain.Layout, Tint.PSMain.Wgsl.ToArray(), Tint.PSMain.WgslEntryPoint), "layers tint");
            _tint.SetParameter(0, new Vector4(1f, 0.5f, 0f, 0));
            _target = d.RenderTexture("layers-target", 16, 16);
            _chain = d.CreatePostChain().AddGrade(exposure: 0f);
        }

        var into = d.RenderLayer("layers-target", _target!, new Color(0, 0, 0, 255));
        var post = d.GetLayer("layers-post");
        post.Post = _chain;
        var show = d.GetLayer("layers-show");
        d.Layer = into;
        d.Sprite(Sprite.Create(_white!, new Vector2(8, 8)) with { Scale = new Vector2(16, 16), Material = _tint });
        d.Layer = post;
        d.Sprite(Sprite.Create(_white!, new Vector2(60, -60)) with { Scale = new Vector2(20, 20), Material = _tint });
        d.Layer = show;
        d.Sprite(_target!.Texture, new Vector2(100, -60));
    }
}
