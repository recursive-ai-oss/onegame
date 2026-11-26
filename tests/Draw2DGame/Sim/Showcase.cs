using System;
using System.Numerics;
using OneGame.Core;
using OneGame.Host;
using OneGame.Render2D;
using OneGame.UI;

namespace Draw2DGame;

/// <summary>
/// Scenes that show how layers, cameras and text order work. Mode 0 draws nothing (the other entities' frame is unchanged); mode 1 draws a
/// zoomed and panned world with a zoom-1 HUD over it and a second camera in a corner; mode 2 draws sprites and text that cover one another.
/// </summary>
[Entity]
public partial class Showcase : Entity, IView2D
{
    /// <summary>Which scene is on: 0 none, 1 world and screen layers, 2 text and sprite order.</summary>
    [Synced] public partial int Mode { get; private set; }

    /// <summary>Switches scene.</summary>
    [Command]
    public void Show(int mode) => Mode = mode;

    [View]
    public void Draw(Draw2D d)
    {
        if (Mode == 0)
        {
            return;
        }

        ShowcaseScenes.Draw(d, Mode);
    }
}

/// <summary>What the <see cref="Showcase"/> entity draws; kept out of the entity because it is view code.</summary>
internal static class ShowcaseScenes
{
    public static void Draw(Draw2D d, int mode)
    {
        // An opaque backdrop on the screen so the hero and floor of the other entities do not show through.
        var back = d.GetLayer("showcase-back");
        back.ScreenSpace = true;
        d.Layer = back;
        d.FillRect(new Rect(0, 0, d.Width, d.Height), Color.Rgb(0x1b2430));

        if (mode == 9)
        {
            Fluid(d);
        }
        else if (mode == 1)
        {
            Layers(d);
        }
        else
        {
            Order(d);
        }
    }

    private static readonly Vector2[] FluidPoints = new Vector2[400];
    private static int _fluidFrame;
    private static FluidSurface2D? _fluid;

    // A few hundred particles on a drifting lattice, drawn as one liquid surface through its post chain.
    private static void Fluid(Draw2D d)
    {
        _fluid ??= new FluidSurface2D("showcase-fluid");
        _fluid.Style.ParticleRadius = 6;
        _fluid.Style.Blur = 4;
        var t = _fluidFrame++ * 0.05f;
        for (var i = 0; i < FluidPoints.Length; i++)
        {
            FluidPoints[i] = new Vector2(((i % 20) * 5) - 50 + (MathF.Sin(t + i) * 2), ((i / 20) * 3) - 30 + (MathF.Cos(t * 1.3f + i) * 2));
        }

        _fluid.Draw(d, FluidPoints, 1, false);
    }

    private static void World(Draw2D d)
    {
        // A 6x4 board of squares around the origin with a label; the same content is drawn for every camera that looks at it.
        for (var y = 0; y < 4; y++)
        {
            for (var x = 0; x < 6; x++)
            {
                var c = ((x + y) & 1) == 0 ? Color.Rgb(0x3d6b8f) : Color.Rgb(0x8f6b3d);
                d.FillRect(new Rect((x * 20) - 60, (y * 20) - 40, 18, 18), c);
            }
        }

        d.FillCircle(new Vector2(0, 0), 9, Color.Rgb(0xf2c14e));
        d.Text("world", new Vector2(0, -44), 8, Color.White, new Vector2(0.5f, 1));
    }

    private static void Layers(Draw2D d)
    {
        // The world, zoomed 3x and panned off-centre, through a camera of its own.
        var world = d.GetLayer("showcase-world");
        world.Camera = new Camera2D { Position = new Vector2(20, 0), Zoom = 3 };
        d.Layer = world;
        World(d);

        // A second view of the same world in a corner: a quarter of the zoom, confined to its own rectangle.
        var mini = d.GetLayer("showcase-mini");
        mini.Camera = new Camera2D { Position = Vector2.Zero, Zoom = 0.6f };
        mini.Viewport = new Rect(356, 12, 112, 80);
        d.Layer = mini;
        d.FillRect(new Rect(-400, -400, 800, 800), Color.Rgb(0x0c1118), 0, -1);
        World(d);

        // The HUD, in pixels, over both: zoom 1 whatever the cameras do.
        var hud = d.ScreenLayer;
        d.Layer = hud;
        d.FillRect(new Rect(8, 8, 150, 34), Color.Rgb(0x000000) with { A = 170 });
        d.Text("HUD at zoom 1", new Vector2(14, 14), 16, Color.White);
        d.StrokeRect(new Rect(356, 12, 112, 80), Color.Rgb(0xd0d0d0), 1);
    }

    private static void Order(Draw2D d)
    {
        var rows = d.GetLayer("showcase-rows");
        rows.ScreenSpace = true;
        var front = d.GetLayer("showcase-front");
        front.ScreenSpace = true;
        d.Layer = rows;

        // 1. A text drawn after a sprite is over it.
        d.FillRect(new Rect(20, 16, 300, 30), Color.Rgb(0xd9822b));
        d.Text("text over an earlier sprite", new Vector2(28, 22), 18, Color.Rgb(0x10151c));

        // 2. A sprite drawn after a text covers it.
        d.Text("a later sprite covers this text", new Vector2(28, 72), 18, Color.White);
        d.FillRect(new Rect(180, 66, 140, 30), Color.Rgb(0x4fa35a));

        // 3. A sprite on a later layer covers text on an earlier one, however the calls are ordered.
        d.Layer = front;
        d.FillRect(new Rect(180, 116, 140, 30), Color.Rgb(0x8e5ac8));
        d.Layer = rows;
        d.Text("text on the lower layer", new Vector2(28, 122), 18, Color.White);

        // 4. An explicit z puts text over a sprite that is drawn after it.
        d.Text("z 5 over a z 1 sprite", new Vector2(28, 172), 18, Color.White, z: 5);
        d.FillRect(new Rect(28, 168, 290, 30), Color.Rgb(0xc4473b), 0, 1);

        d.ScreenText("screen text, last", new Vector2(28, 220), 16, Color.Rgb(0xdddddd));
    }
}

/// <summary>Draws a load of screen text: the same labels every frame, or labels that change every frame. A test or a browser script sets how many.</summary>
[Entity]
public partial class TextLoad : Entity, IView2D
{
    /// <summary>How many lines of screen text are drawn each frame.</summary>
    [Synced] public partial int Lines { get; private set; }

    /// <summary>0 draws the same string objects at one size; 1 draws numbers that change every frame; 2 cycles through eight sizes; 4 draws at a size that is new every frame; 3 warms the glyphs first.</summary>
    [Synced] public partial int Churn { get; private set; }

    /// <summary>Sets how much text is drawn and how it changes.</summary>
    [Command]
    public void Set(int lines, int churn)
    {
        Lines = lines;
        Churn = churn;
    }

    /// <summary>Set once the warm-up of churn mode 3 has run.</summary>
    public static bool Warmed { get; private set; }

    /// <summary>What the warm-up reported refused.</summary>
    public static int WarmRefused { get; private set; }

    private readonly string[] _labels = BuildLabels();

    private static string[] BuildLabels()
    {
        var labels = new string[200];
        for (var i = 0; i < labels.Length; i++)
        {
            labels[i] = "label " + i.ToString("000", System.Globalization.CultureInfo.InvariantCulture);
        }

        return labels;
    }

    [View]
    public void Draw(Draw2D d)
    {
        var n = Math.Min(Lines, _labels.Length);
        for (var i = 0; i < n; i++)
        {
            var at = new Vector2(8 + ((i / 20) * 90), 8 + ((i % 20) * 12));
            switch (Churn)
            {
                case 1:
                    d.ScreenText("n " + ((d.Tick * 7) + i).ToString(System.Globalization.CultureInfo.InvariantCulture), at, 11, Color.White);
                    break;
                case 3:
                    if (!Warmed)
                    {
                        WarmRefused = d.WarmUpText("label 0123456789", 11);
                        Warmed = true;
                    }

                    d.ScreenText(_labels[i], at, 11, Color.White);
                    break;
                case 4:
                    // A size that never repeats: a label that pulses smoothly.
                    d.ScreenText(_labels[i], at, 10 + ((((d.Tick * 7) + i) % 20000) / 2000f), Color.White);
                    break;
                case 2:
                    d.ScreenText(_labels[i], at, 9 + ((i + (int)d.Tick) % 8), Color.White);
                    break;
                default:
                    d.ScreenText(_labels[i], at, 11, Color.White);
                    break;
            }
        }
    }
}
