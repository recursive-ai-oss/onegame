using OneGame.Atlas;
using OneGame.UI;

namespace OneGame.Text;

/// <summary>One textured rectangle of a glyph, in screen pixels, ready for the renderer.</summary>
/// <param name="Page">Glyph atlas page to sample.</param>
/// <param name="X0">Left edge in screen pixels.</param>
/// <param name="Y0">Top edge in screen pixels.</param>
/// <param name="X1">Right edge in screen pixels.</param>
/// <param name="Y1">Bottom edge in screen pixels.</param>
/// <param name="U0">Left texture coordinate.</param>
/// <param name="V0">Top texture coordinate.</param>
/// <param name="U1">Right texture coordinate.</param>
/// <param name="V1">Bottom texture coordinate.</param>
/// <param name="Color">Text color (straight alpha); multiply its alpha by the sampled coverage.</param>
public readonly record struct GlyphQuad(int Page, float X0, float Y0, float X1, float Y1, float U0, float V0, float U1, float V1, Color Color);

/// <summary>
/// Turns the text runs of a display list into glyph quads. Shaping comes from the measurer's cache (the same shaping that sized the
/// lines), glyphs from the atlas. Quads sit on whole pixels vertically and on quarter pixels horizontally.
/// </summary>
public sealed class TextQuadBuilder
{
    private readonly FontTextMeasurer _measurer;
    private readonly GlyphAtlas _atlas;
    private readonly List<PositionedGlyph> _glyphs = [];

    /// <summary>Creates a builder.</summary>
    /// <param name="measurer">The measurer the UI laid out with. Using another measurer gives glyphs that do not match the lines.</param>
    /// <param name="atlas">The atlas to rasterize into.</param>
    public TextQuadBuilder(FontTextMeasurer measurer, GlyphAtlas atlas)
    {
        ArgumentNullException.ThrowIfNull(measurer);
        ArgumentNullException.ThrowIfNull(atlas);
        _measurer = measurer;
        _atlas = atlas;
    }

    /// <summary>Appends the quads of one text run.</summary>
    /// <param name="list">The display list.</param>
    /// <param name="runIndex">The <see cref="DrawCommand.TextRun"/> of a <c>Text</c> command.</param>
    /// <param name="quads">Receives the quads, in draw order.</param>
    /// <returns>How many glyphs were not drawn because the atlas refused them (too large, or every page in use this frame); 0 normally.</returns>
    public int Build(DisplayList list, int runIndex, List<GlyphQuad> quads)
    {
        ArgumentNullException.ThrowIfNull(list);
        ArgumentNullException.ThrowIfNull(quads);
        var run = list.TextRuns[runIndex];
        var refused = 0;
        var lines = list.Lines.Slice(run.FirstLine, run.LineCount);
        foreach (var line in lines)
        {
            _glyphs.Clear();
            var baseline = _measurer.Place(run.Text, run.Style, line.Start, line.Length, line.Height, _glyphs);
            var originY = MathF.Round(line.Y + baseline);
            foreach (var g in _glyphs)
            {
                var x = line.X + g.X;
                var ix = MathF.Floor(x);
                var step = Math.Clamp((int)((x - ix) * GlyphAtlas.SubpixelSteps), 0, GlyphAtlas.SubpixelSteps - 1);
                if (_atlas.TryGet(g.FaceId, g.GlyphId, run.Style.Size, step, out var info) != AtlasStatus.Ok)
                {
                    refused++;
                    continue;
                }

                if (info.Width == 0)
                {
                    continue;
                }

                var x0 = ix + info.OffsetX;
                var y0 = originY + MathF.Round(g.Y) + info.OffsetY;
                quads.Add(new GlyphQuad(info.Page, x0, y0, x0 + info.Width, y0 + info.Height, info.U0, info.V0, info.U1, info.V1, run.Color));
            }
        }

        return refused;
    }

    /// <summary>Appends the quads of every text run of a list, in run order.</summary>
    /// <param name="list">The display list.</param>
    /// <param name="quads">Receives the quads.</param>
    /// <returns>How many glyphs were refused.</returns>
    public int BuildAll(DisplayList list, List<GlyphQuad> quads)
    {
        var refused = 0;
        for (var i = 0; i < list.TextRuns.Length; i++)
        {
            refused += Build(list, i, quads);
        }

        return refused;
    }
}
