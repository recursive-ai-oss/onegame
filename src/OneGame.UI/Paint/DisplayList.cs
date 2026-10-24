using System.Globalization;
using System.Numerics;
using System.Text;

namespace OneGame.UI;

/// <summary>The kind of a <see cref="DrawCommand"/>.</summary>
public enum DrawKind
{
    /// <summary>Fill <see cref="DrawCommand.Bounds"/> with <see cref="DrawCommand.Color"/>, rounded by <see cref="DrawCommand.Radii"/>.</summary>
    Rect,
    /// <summary>Stroke the inside edge of <see cref="DrawCommand.Bounds"/>, <see cref="DrawCommand.Widths"/> thick per side, rounded by <see cref="DrawCommand.Radii"/>.</summary>
    Border,
    /// <summary>A blurred shadow of the rounded box <see cref="DrawCommand.Bounds"/>, moved by <see cref="DrawCommand.Offset"/>, grown by <see cref="DrawCommand.Spread"/>, blurred by <see cref="DrawCommand.Value"/>. It is not drawn inside the box itself.</summary>
    Shadow,
    /// <summary>Draw the image or SVG named <see cref="DrawCommand.Resource"/> into <see cref="DrawCommand.Bounds"/>, multiplied by <see cref="DrawCommand.Color"/>; <see cref="DrawCommand.Slice"/> gives nine-slice insets in pixels (zero: stretch).</summary>
    Image,
    /// <summary>Draw text run number <see cref="DrawCommand.TextRun"/>.</summary>
    Text,
    /// <summary>Intersect the clip with the rounded rectangle <see cref="DrawCommand.Bounds"/> until the matching <see cref="PopClip"/>.</summary>
    PushClip,
    /// <summary>Restore the clip from before the matching <see cref="PushClip"/>.</summary>
    PopClip,
    /// <summary>Apply <see cref="DrawCommand.Transform"/> to everything until the matching <see cref="PopTransform"/>.</summary>
    PushTransform,
    /// <summary>Remove the transform pushed by the matching <see cref="PushTransform"/>.</summary>
    PopTransform,
    /// <summary>Draw everything until the matching <see cref="PopOpacity"/> as a group, then blend it with <see cref="DrawCommand.Value"/> opacity.</summary>
    PushOpacity,
    /// <summary>End the group started by the matching <see cref="PushOpacity"/>.</summary>
    PopOpacity,
}

/// <summary>One drawing instruction. Which fields matter depends on <see cref="Kind"/>; the others are default.</summary>
public struct DrawCommand
{
    /// <summary>What to draw.</summary>
    public DrawKind Kind { get; init; }

    /// <summary>The rectangle in screen pixels, in the coordinates in effect before the current transform.</summary>
    public Rect Bounds { get; init; }

    /// <summary>The fill, stroke, tint, shadow or text color.</summary>
    public Color Color { get; init; }

    /// <summary>Corner radii of <see cref="Bounds"/>.</summary>
    public CornerRadii Radii { get; init; }

    /// <summary>Border thickness per side.</summary>
    public Edges Widths { get; init; }

    /// <summary>Nine-slice insets of an image.</summary>
    public Edges Slice { get; init; }

    /// <summary>The asset id of an image or SVG.</summary>
    public string? Resource { get; init; }

    /// <summary>Shadow offset.</summary>
    public Vector2 Offset { get; init; }

    /// <summary>Shadow spread in pixels.</summary>
    public float Spread { get; init; }

    /// <summary>Shadow blur radius in pixels, or the opacity (0..1) of a group.</summary>
    public float Value { get; init; }

    /// <summary>The 2D transform of <see cref="DrawKind.PushTransform"/>, applied to row vectors (x, y, 1).</summary>
    public Matrix3x2 Transform { get; init; }

    /// <summary>Index into <see cref="DisplayList.TextRuns"/>.</summary>
    public int TextRun { get; init; }

    /// <summary>The element that produced the command, for debugging and capture annotations. Renderers ignore it.</summary>
    public Element? Source { get; init; }
}

/// <summary>A line of text with its final position.</summary>
/// <param name="Start">Index of the first UTF-16 code unit in the run's text.</param>
/// <param name="Length">Number of code units.</param>
/// <param name="X">Left edge in screen pixels (text alignment already applied).</param>
/// <param name="Y">Top edge in screen pixels.</param>
/// <param name="Width">Advance width in pixels.</param>
/// <param name="Height">Line height in pixels.</param>
public readonly record struct PlacedLine(int Start, int Length, float X, float Y, float Width, float Height);

/// <summary>A paragraph of text to draw: the string, its font, color and where each line goes. The renderer shapes each line segment.</summary>
/// <param name="Text">The source string; lines index into it.</param>
/// <param name="Style">The font properties, with sizes in pixels.</param>
/// <param name="Color">The text color.</param>
/// <param name="FirstLine">Index of the first line in <see cref="DisplayList.Lines"/>.</param>
/// <param name="LineCount">Number of lines.</param>
public readonly record struct TextRun(string Text, TextStyle Style, Color Color, int FirstLine, int LineCount);

/// <summary>
/// What the UI hands the renderer each frame: draw commands in paint order, plus the text they refer to.
/// The same instance is reused; <see cref="Version"/> changes when the content does.
/// </summary>
public sealed class DisplayList
{
    private DrawCommand[] _commands = new DrawCommand[256];
    private int _count;
    private TextRun[] _runs = new TextRun[16];
    private int _runCount;
    private PlacedLine[] _lines = new PlacedLine[64];
    private int _lineCount;

    /// <summary>Increases whenever the commands change; a renderer can skip re-recording when it is unchanged.</summary>
    public int Version { get; internal set; }

    /// <summary>Screen width in pixels.</summary>
    public float Width { get; internal set; }

    /// <summary>Screen height in pixels.</summary>
    public float Height { get; internal set; }

    /// <summary>Pixels per UI unit; SVG art is rasterized at this scale.</summary>
    public float Scale { get; internal set; } = 1;

    /// <summary>The commands in paint order.</summary>
    public ReadOnlySpan<DrawCommand> Commands => new(_commands, 0, _count);

    /// <summary>The text runs that <see cref="DrawKind.Text"/> commands refer to.</summary>
    public ReadOnlySpan<TextRun> TextRuns => new(_runs, 0, _runCount);

    /// <summary>The lines of all text runs.</summary>
    public ReadOnlySpan<PlacedLine> Lines => new(_lines, 0, _lineCount);

    internal void Clear()
    {
        _count = 0;
        _runCount = 0;
        _lineCount = 0;
    }

    internal void Add(in DrawCommand c)
    {
        if (_count == _commands.Length)
        {
            Array.Resize(ref _commands, _count * 2);
        }

        _commands[_count++] = c;
    }

    internal int AddRun(in TextRun r)
    {
        if (_runCount == _runs.Length)
        {
            Array.Resize(ref _runs, _runCount * 2);
        }

        _runs[_runCount] = r;
        return _runCount++;
    }

    internal int AddLine(in PlacedLine l)
    {
        if (_lineCount == _lines.Length)
        {
            Array.Resize(ref _lines, _lineCount * 2);
        }

        _lines[_lineCount] = l;
        return _lineCount++;
    }

    /// <summary>One line per command in a stable text form, for tests and for reading in a terminal.</summary>
    public string ToText()
    {
        var sb = new StringBuilder();
        var depth = 0;
        foreach (var c in Commands)
        {
            if (c.Kind is DrawKind.PopClip or DrawKind.PopTransform or DrawKind.PopOpacity)
            {
                depth--;
            }

            sb.Append(' ', depth * 2);
            sb.Append(c.Kind);
            switch (c.Kind)
            {
                case DrawKind.Rect:
                    sb.Append(' ').Append(Fmt(c.Bounds)).Append(' ').Append(c.Color);
                    Radii(sb, c.Radii);
                    break;
                case DrawKind.Border:
                    sb.Append(' ').Append(Fmt(c.Bounds)).Append(' ').Append(c.Color)
                      .Append(" widths=").Append(N(c.Widths.Left)).Append(',').Append(N(c.Widths.Top)).Append(',').Append(N(c.Widths.Right)).Append(',').Append(N(c.Widths.Bottom));
                    Radii(sb, c.Radii);
                    break;
                case DrawKind.Shadow:
                    sb.Append(' ').Append(Fmt(c.Bounds)).Append(' ').Append(c.Color).Append(" offset=").Append(N(c.Offset.X)).Append(',').Append(N(c.Offset.Y))
                      .Append(" blur=").Append(N(c.Value)).Append(" spread=").Append(N(c.Spread));
                    break;
                case DrawKind.Image:
                    sb.Append(' ').Append(Fmt(c.Bounds)).Append(" '").Append(c.Resource).Append('\'');
                    if (!c.Slice.IsZero)
                    {
                        sb.Append(" slice=").Append(N(c.Slice.Left));
                    }

                    break;
                case DrawKind.Text:
                    var run = _runs[c.TextRun];
                    sb.Append(" '").Append(run.Text).Append("' ").Append(run.Color).Append(" size=").Append(N(run.Style.Size));
                    for (var i = 0; i < run.LineCount; i++)
                    {
                        var l = _lines[run.FirstLine + i];
                        sb.Append(" [").Append(N(l.X)).Append(',').Append(N(l.Y)).Append(' ').Append(run.Text, l.Start, l.Length).Append(']');
                    }

                    break;
                case DrawKind.PushClip:
                    sb.Append(' ').Append(Fmt(c.Bounds));
                    break;
                case DrawKind.PushTransform:
                    sb.Append(' ').Append(string.Create(CultureInfo.InvariantCulture, $"[{c.Transform.M11:0.###} {c.Transform.M12:0.###} {c.Transform.M21:0.###} {c.Transform.M22:0.###} {c.Transform.M31:0.###} {c.Transform.M32:0.###}]"));
                    break;
                case DrawKind.PushOpacity:
                    sb.Append(' ').Append(N(c.Value));
                    break;
                default:
                    break;
            }

            sb.Append('\n');
            if (c.Kind is DrawKind.PushClip or DrawKind.PushTransform or DrawKind.PushOpacity)
            {
                depth++;
            }
        }

        return sb.ToString();
    }

    private static void Radii(StringBuilder sb, CornerRadii r)
    {
        if (!r.IsZero)
        {
            sb.Append(" radii=").Append(N(r.TopLeft)).Append(',').Append(N(r.TopRight)).Append(',').Append(N(r.BottomRight)).Append(',').Append(N(r.BottomLeft));
        }
    }

    private static string Fmt(Rect r) => $"({N(r.X)},{N(r.Y)} {N(r.Width)}x{N(r.Height)})";

    private static string N(float v) => Num.Format(v);
}
