namespace OneGame.UI;

/// <summary>The resolved font properties that decide how a piece of text is measured and wrapped.</summary>
/// <param name="Family">The <c>font-family</c> list.</param>
/// <param name="Size">Font size in pixels (after UI scale).</param>
/// <param name="Weight">Weight, 100..900.</param>
/// <param name="Italic">True for italic.</param>
/// <param name="LineHeight">Line pitch in pixels, or 0 for the font's own line height.</param>
/// <param name="LetterSpacing">Extra advance per character in pixels.</param>
/// <param name="WhiteSpace">Wrapping mode.</param>
public readonly record struct TextStyle(string Family, float Size, int Weight, bool Italic, float LineHeight, float LetterSpacing, WhiteSpace WhiteSpace);

/// <summary>The size of laid-out text.</summary>
/// <param name="Width">Width of the widest line in pixels.</param>
/// <param name="Height">Total height in pixels.</param>
/// <param name="LineCount">Number of lines.</param>
public readonly record struct TextSize(float Width, float Height, int LineCount);

/// <summary>One line of laid-out text.</summary>
/// <param name="Start">Index of the first UTF-16 code unit of the line in the source string.</param>
/// <param name="Length">Number of code units in the line, without trailing spaces or the line break.</param>
/// <param name="Width">Advance width of the line in pixels.</param>
/// <param name="Y">Top of the line relative to the first line, in pixels.</param>
/// <param name="Height">Height of the line in pixels.</param>
public readonly record struct TextLine(int Start, int Length, float Width, float Y, float Height);

/// <summary>
/// Measures and breaks text for layout. The UI never shapes text itself: the real implementation (shaping, bidi, fallback, line breaking)
/// lives with the text system, and the renderer shapes each returned line again at draw time.
/// </summary>
public interface ITextMeasurer
{
    /// <summary>
    /// Measures <paramref name="text"/> wrapped at <paramref name="maxWidth"/>. A <c>PositiveInfinity</c> width means no wrapping (max-content);
    /// zero means break at every opportunity (min-content, the widest unbreakable run). A run wider than the limit overflows on its own line.
    /// </summary>
    TextSize Measure(string text, in TextStyle style, float maxWidth);

    /// <summary>Breaks <paramref name="text"/> into lines at <paramref name="maxWidth"/>, appending to <paramref name="lines"/>; the lines agree with <see cref="Measure"/>.</summary>
    void Layout(string text, in TextStyle style, float maxWidth, List<TextLine> lines);
}

/// <summary>
/// A deterministic <see cref="ITextMeasurer"/> where every character is <c>0.6 × size + letter-spacing</c> wide and a line is <c>1.2 × size</c> tall
/// unless the style sets a line height. It breaks at spaces. Headless runs and tests use it; it needs no font.
/// </summary>
public sealed class MonospaceTextMeasurer : ITextMeasurer
{
    /// <inheritdoc/>
    public TextSize Measure(string text, in TextStyle style, float maxWidth) => Run(text, style, maxWidth, null);

    /// <inheritdoc/>
    public void Layout(string text, in TextStyle style, float maxWidth, List<TextLine> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        Run(text, style, maxWidth, lines);
    }

    private static TextSize Run(string text, in TextStyle st, float maxWidth, List<TextLine>? lines)
    {
        var cw = (st.Size * 0.6f) + st.LetterSpacing;
        var lh = st.LineHeight > 0 ? st.LineHeight : st.Size * 1.2f;
        var wrap = st.WhiteSpace == WhiteSpace.Normal && !float.IsPositiveInfinity(maxWidth);
        var pre = st.WhiteSpace == WhiteSpace.Pre;
        var n = text.Length;
        var i = 0;
        var lineStart = -1;
        var lineEnd = 0;
        var chars = 0;
        var pending = 0;
        var maxW = 0f;
        var count = 0;

        void Emit()
        {
            if (lineStart < 0)
            {
                return;
            }

            var w = chars * cw;
            maxW = MathF.Max(maxW, w);
            lines?.Add(new TextLine(lineStart, lineEnd - lineStart, w, count * lh, lh));
            count++;
            lineStart = -1;
            chars = 0;
            pending = 0;
        }

        while (i < n)
        {
            var c = text[i];
            if (c == '\n' && pre)
            {
                if (lineStart < 0)
                {
                    lineStart = i;
                    lineEnd = i;
                }

                Emit();
                i++;
                continue;
            }

            if (c == ' ' || c == '\t' || c == '\n')
            {
                if (pre)
                {
                    pending++;
                    if (lineStart < 0)
                    {
                        lineStart = i;
                        lineEnd = i;
                    }
                }
                else if (chars > 0)
                {
                    pending = 1;
                }

                i++;
                continue;
            }

            var j = i;
            while (j < n && text[j] is not (' ' or '\t' or '\n'))
            {
                j++;
            }

            var len = j - i;
            if (wrap && chars > 0 && (chars + pending + len) * cw > maxWidth + 0.001f)
            {
                Emit();
            }

            if (lineStart < 0)
            {
                lineStart = i;
            }

            chars += pending + len;
            pending = 0;
            lineEnd = j;
            i = j;
        }

        Emit();

        return new TextSize(maxW, count * lh, count);
    }
}
