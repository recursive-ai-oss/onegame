using OneGame.Atlas;
using OneGame.Render2D;
using OneGame.Text;
using OneGame.UI;

namespace OneGame.Host;

/// <summary>
/// Something drawn on top of the finished frame, after the game's UI and sprites: the development overlay is the one user. It sees the
/// frame only through an <see cref="OverlayCanvas"/>, so it can draw text and rectangles and nothing else.
/// </summary>
public interface IFrameOverlay
{
    /// <summary>Draws this frame's overlay.</summary>
    /// <param name="canvas">Where to draw; valid only during the call.</param>
    void Draw(OverlayCanvas canvas);
}

/// <summary>The few drawing calls an <see cref="IFrameOverlay"/> has: filled rectangles and one-line text, in framebuffer pixels.</summary>
public sealed class OverlayCanvas
{
    private Renderer2D? _renderer;
    private TextQuadBuilder? _text;
    private GlyphAtlas? _glyphs;
    private FontTextMeasurer? _measurer;

    internal OverlayCanvas()
    {
    }

    /// <summary>Width of the target in pixels.</summary>
    public int Width { get; private set; }

    /// <summary>Height of the target in pixels.</summary>
    public int Height { get; private set; }

    /// <summary>The UI scale of the frame; sizes given in points are multiplied by it.</summary>
    public float Scale { get; private set; }

    internal void Begin(Renderer2D renderer, TextQuadBuilder text, GlyphAtlas glyphs, FontTextMeasurer measurer, in FrameInfo frame)
    {
        _renderer = renderer;
        _text = text;
        _glyphs = glyphs;
        _measurer = measurer;
        Width = frame.Width;
        Height = frame.Height;
        Scale = frame.Scale;
    }

    internal void End() => _renderer = null;

    /// <summary>Fills a rectangle.</summary>
    /// <param name="x">Left edge in pixels.</param>
    /// <param name="y">Top edge in pixels.</param>
    /// <param name="width">Width in pixels.</param>
    /// <param name="height">Height in pixels.</param>
    /// <param name="color">Fill colour, straight alpha.</param>
    public void Fill(float x, float y, float width, float height, Color color) =>
        Renderer().Ui.Fill(new Rect(x, y, width, height), color);

    /// <summary>Draws one line of text in the game's first font.</summary>
    /// <param name="line">The text; newlines are not broken.</param>
    /// <param name="x">Left edge in pixels.</param>
    /// <param name="y">Top edge in pixels.</param>
    /// <param name="size">Font size in pixels.</param>
    /// <param name="color">Text colour, straight alpha.</param>
    public void Text(string line, float x, float y, float size, Color color)
    {
        var r = Renderer();
        r.Ui.Text(_text!, _glyphs!, line, Style(size), new System.Numerics.Vector2(x, y), color);
    }

    /// <summary>The width of a line of text when drawn by <see cref="Text"/>.</summary>
    /// <param name="line">The text.</param>
    /// <param name="size">Font size in pixels.</param>
    /// <returns>Pixels.</returns>
    public float Measure(string line, float size) => _measurer!.Measure(line, Style(size), float.PositiveInfinity).Width;

    private static TextStyle Style(float size) => new(string.Empty, size, 400, false, 0, 0, WhiteSpace.NoWrap);

    private Renderer2D Renderer() => _renderer ?? throw new InvalidOperationException("An overlay can only draw while its frame is being rendered.");
}
