using System;
using System.Numerics;
using OneGame.Core;
using OneGame.Host;
using OneGame.UI;

namespace WebGame;

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
