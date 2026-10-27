using System.Numerics;
using OneGame.Core;
using OneGame.Host;
using OneGame.Render2D;
using OneGame.UI;

namespace Draw2DGame;

/// <summary>A hero that walks left and right. Position and speed are synced fixed-point state; the animation is only in the view.</summary>
[Entity]
public partial class Hero : Entity, IView2D
{
    [Synced] public partial Fixed X { get; private set; }

    [Synced] public partial Fixed Speed { get; private set; }

    /// <summary>Sets the speed in world units per tick; negative walks left, zero stands still.</summary>
    [Command]
    public void Go(Fixed speed) => Speed = speed;

    protected override void OnTick() => X += Speed;

    private SpriteSheet? _sheet;

    [View]
    public void Draw(Draw2D d)
    {
        _sheet ??= Art.HeroSheet(d.Renderer!);
        var at = new Vector2(X.ToSingle(), 0);
        d.Camera.Zoom = 2;
        d.Follow(at);
        d.Sprite(_sheet, Speed == Fixed.Zero ? "idle" : "walk", at, scale: 2, flip: Speed < Fixed.Zero ? SpriteFlip.Horizontal : SpriteFlip.None, z: 1);
        d.Text("hero", at - new Vector2(0, 24), 12, Color.White, new Vector2(0.5f, 1));
    }
}

/// <summary>The floor, a marker line and a coin.</summary>
[Entity]
public partial class Backdrop : Entity, IView2D
{
    [View]
    public void Draw(Draw2D d)
    {
        d.FillRect(new Rect(-400, 16, 800, 24), Color.Rgb(0x35506b));
        d.StrokeRect(new Rect(-400, 16, 800, 24), Color.Rgb(0x8fb8de), 2);
        d.Line(new Vector2(-120, 16), new Vector2(-120, -40), Color.Rgb(0xd0d0d0), 2);
        d.FillCircle(new Vector2(60, -8), 8, Color.Rgb(0xf2c14e));
        d.StrokeCircle(new Vector2(60, -8), 12, Color.Rgb(0xf2c14e), 1.5f);
        d.ScreenText("walk with Go", new Vector2(8, 8), 14, Color.Rgb(0xdddddd));
    }
}

internal static class Art
{
    /// <summary>A 4x2 grid of 16x16 frames drawn in code: four walking frames then two idle ones.</summary>
    public static SpriteSheet HeroSheet(Renderer2D renderer)
    {
        const int cell = 16;
        var px = new byte[4 * cell * 2 * cell * 4];
        for (var f = 0; f < 8; f++)
        {
            var ox = (f % 4) * cell;
            var oy = (f / 4) * cell;
            var walking = f < 4;
            var bob = walking ? (f % 2) : 0;
            for (var y = 0; y < cell; y++)
            {
                for (var x = 0; x < cell; x++)
                {
                    var body = x is >= 4 and < 12 && y >= 3 + bob && y < 12;
                    var head = x is >= 5 and < 11 && y is >= 1 and < 5;
                    var leg = y >= 12 && ((x is >= 4 and < 7 && (walking ? f % 4 < 2 : true)) || (x is >= 9 and < 12 && (walking ? f % 4 >= 2 || f == 1 : true)));
                    if (!(body || head || leg))
                    {
                        continue;
                    }

                    var i = ((((oy + y) * 4 * cell) + ox + x)) * 4;
                    px[i] = (byte)(head ? 240 : 70 + (f * 12));
                    px[i + 1] = (byte)(head ? 200 : 140);
                    px[i + 2] = (byte)(head ? 160 : 230);
                    px[i + 3] = 255;
                }
            }
        }

        var sheet = SpriteSheet.Grid(renderer.CreateTexture(4 * cell, 2 * cell, px, TextureFilter.Nearest), cell, cell);
        sheet.AddClip("walk", 0, 4, 8);
        sheet.AddClip("idle", 4, 2, 2);
        return sheet;
    }
}
