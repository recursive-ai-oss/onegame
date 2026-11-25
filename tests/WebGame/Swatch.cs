using System;
using System.Numerics;
using OneGame.Core;
using OneGame.Host;
using OneGame.Render2D;
using WebGame.Shaders;

namespace WebGame;

/// <summary>A green square drawn through the game's own material, the first draw of the frame.</summary>
[Entity]
public partial class Swatch : Entity, IView2D
{
    /// <summary>While set, drawing throws: the page has to survive a frame that fails.</summary>
    [Synced] public partial bool Broken { get; private set; }

    /// <summary>Makes drawing fail (or work again).</summary>
    [Command]
    public void Break(bool on) => Broken = on;

    /// <summary>While set, the screen shows a full-window panel that blurs everything behind it (backdrop blur).</summary>
    [Synced] public partial bool Glass { get; private set; }

    /// <summary>Shows or hides the blurring panel.</summary>
    [Command]
    public void SetGlass(bool on) => Glass = on;

    private Texture2D? _texture;
    private Material? _material;

    [View]
    public void Draw(Draw2D d)
    {
        if (Broken)
        {
            throw new InvalidOperationException("the swatch is broken on purpose");
        }

        var renderer = d.Renderer!;
        if (_texture is null)
        {
            var pixels = new byte[16 * 16 * 4];
            Array.Fill(pixels, (byte)255);
            _texture = renderer.CreateTexture(16, 16, pixels, TextureFilter.Nearest);
            _material = renderer.CreateMaterial(new ShaderCode(RecursiveAI.OneGpu.ShaderStage.Pixel, Tint.PSMain.EntryPoint, Tint.PSMain.SpirV.ToArray(), Tint.PSMain.Msl.ToArray(), Tint.PSMain.MslFunctionName, Tint.PSMain.Layout, Tint.PSMain.Wgsl.ToArray(), Tint.PSMain.WgslEntryPoint), "tint");
            _material.SetParameter(0, new Vector4(0.1f, 0.9f, 0.2f, 0));
        }

        d.Sprite(Sprite.Create(_texture, new Vector2(120, 100)) with { Scale = new Vector2(6, 6), Material = _material });
    }
}
