using OneFont;
using OneGame.UI;

namespace OneGame.Text;

/// <summary>One glyph with its pen position, ready to look up in the glyph atlas.</summary>
/// <param name="FaceId">Face id in the <see cref="FontLibrary"/>.</param>
/// <param name="GlyphId">Glyph index in that face.</param>
/// <param name="X">Horizontal origin in pixels, from the left end of the line.</param>
/// <param name="Y">Vertical origin in pixels from the baseline, y down (shaper offsets only; usually 0).</param>
/// <param name="Advance">How far the pen moves after this glyph, in pixels (letter spacing is added once per character cluster, not included here).</param>
public readonly record struct PositionedGlyph(int FaceId, int GlyphId, float X, float Y, float Advance);

/// <summary>
/// One text string shaped with one style: bidi levels, clusters with advances and glyphs in logical order, and the break and whitespace
/// facts that wrapping needs. Wrapping at a width and placing a line read this; neither shapes again.
/// </summary>
internal sealed class ShapedText
{
    private const byte FSpace = 1;
    private const byte FCollapsed = 2;
    private const byte FBreakAfter = 4;
    private const byte FHard = 8;

    private readonly string _text;
    private readonly int _n;
    private readonly int[] _start;
    private readonly int[] _length;
    private readonly float[] _advance;
    private readonly byte[] _level;
    private readonly byte[] _flags;
    private readonly int[] _glyphFirst;
    private readonly int[] _glyphCount;
    private readonly int[] _glyphFace;
    private readonly int[] _glyphId;
    private readonly float[] _glyphAdvance;
    private readonly float[] _glyphX;
    private readonly float[] _glyphY;
    private readonly float _spacing;
    private readonly bool _wrapAllowed;
    private readonly bool _pre;
    private byte[] _levelScratch = [];
    private int[] _orderScratch = [];

    public ShapedText(string text, FontStack stack, float size, float lineHeight, float spacing, WhiteSpace whiteSpace, ShapedRun scratch)
    {
        _text = text;
        _spacing = spacing;
        _pre = whiteSpace == WhiteSpace.Pre;
        _wrapAllowed = whiteSpace == WhiteSpace.Normal;
        Stack = stack;
        Size = size;
        LineHeight = lineHeight > 0 ? lineHeight : (stack.Ascent + stack.Descent + stack.LineGap) * size;

        var start = new List<int>();
        var length = new List<int>();
        var advance = new List<float>();
        var level = new List<byte>();
        var flags = new List<byte>();
        var gFirst = new List<int>();
        var gCount = new List<int>();
        var gFace = new List<int>();
        var gId = new List<int>();
        var gAdv = new List<float>();
        var gX = new List<float>();
        var gY = new List<float>();

        void Synthetic(int at, byte f)
        {
            start.Add(at);
            length.Add(1);
            advance.Add(0);
            level.Add(0);
            flags.Add(f);
            gFirst.Add(gId.Count);
            gCount.Add(0);
        }

        var n = text.Length;
        var ps = 0;
        while (ps <= n)
        {
            var pe = _pre ? text.IndexOf('\n', ps) : -1;
            if (pe < 0)
            {
                pe = n;
            }

            if (pe > ps)
            {
                ShapeParagraph(ps, pe, scratch);
            }

            if (pe < n)
            {
                Synthetic(pe, FSpace | FCollapsed | FHard | FBreakAfter);
            }

            ps = pe + 1;
        }

        void ShapeParagraph(int from, int to, ShapedRun run)
        {
            var len = to - from;
            var buf = new char[len];
            for (var i = 0; i < len; i++)
            {
                var c = text[from + i];
                buf[i] = c is '\n' or '\t' or '\r' ? ' ' : c;
            }

            var levels = TextShaping.GetBidiLevels(buf, TextDirection.Auto, out _);
            var opp = TextSegmentation.GetLineBreakOpportunities(buf);
            var paraFirst = start.Count;
            for (var i = 0; i < len;)
            {
                var j = i + 1;
                while (j < len && levels[j] == levels[i])
                {
                    j++;
                }

                var rtl = (levels[i] & 1) == 1;
                stack.Chain.ShapeRun(buf.AsSpan(i, j - i), run, rtl);
                foreach (var c in run.Clusters)
                {
                    var faceIdx = c.FaceIndex;
                    var scale = size / stack.Faces[faceIdx].UnitsPerEm;
                    var cs = from + i + c.Start;
                    var firstG = gId.Count;
                    var adv = 0f;
                    foreach (var g in run.GlyphsOf(c))
                    {
                        var a = g.XAdvance * scale;
                        gFace.Add(stack.FaceIds[faceIdx]);
                        gId.Add(g.GlyphId);
                        gAdv.Add(a);
                        gX.Add(g.XOffset * scale);
                        gY.Add(-g.YOffset * scale);
                        adv += a;
                    }

                    var rel = i + c.Start;
                    var rawChar = text[cs];
                    byte f = 0;
                    if (c.Length == 1 && rawChar is ' ' or '\t' or '\n' or '\r')
                    {
                        f |= FSpace;
                    }

                    var endRel = rel + c.Length;
                    if (endRel < len && opp[endRel] != LineBreakOpportunity.None)
                    {
                        f |= FBreakAfter;
                    }

                    start.Add(cs);
                    length.Add(c.Length);
                    advance.Add(adv + spacing);
                    level.Add(levels[rel]);
                    flags.Add(f);
                    gFirst.Add(firstG);
                    gCount.Add(gId.Count - firstG);
                }

                i = j;
            }

            if (!_pre)
            {
                // CSS Text 3 §4.1.1: a run of spaces collapses to one, and the first space of a line is removed. Collapsed clusters keep
                // their place in the index but have no width and draw nothing.
                var prevSpace = true;
                for (var k = paraFirst; k < start.Count; k++)
                {
                    if ((flags[k] & FSpace) != 0)
                    {
                        if (prevSpace)
                        {
                            flags[k] |= FCollapsed;
                            advance[k] = 0;
                        }

                        prevSpace = true;
                    }
                    else
                    {
                        prevSpace = false;
                    }
                }
            }
        }

        _n = start.Count;
        _start = [.. start];
        _length = [.. length];
        _advance = [.. advance];
        _level = [.. level];
        _flags = [.. flags];
        _glyphFirst = [.. gFirst];
        _glyphCount = [.. gCount];
        _glyphFace = [.. gFace];
        _glyphId = [.. gId];
        _glyphAdvance = [.. gAdv];
        _glyphX = [.. gX];
        _glyphY = [.. gY];
    }

    public FontStack Stack { get; }

    public float Size { get; }

    /// <summary>The line pitch in pixels: the style's, or the font's own.</summary>
    public float LineHeight { get; }

    /// <summary>The distance from the top of a line box of <paramref name="lineHeight"/> to the baseline: the font's ascent plus half the leading.</summary>
    public float BaselineIn(float lineHeight)
    {
        var content = (Stack.Ascent + Stack.Descent) * Size;
        return ((lineHeight - content) / 2) + (Stack.Ascent * Size);
    }

    /// <summary>
    /// Breaks the text into lines at <paramref name="maxWidth"/> (CSS Text 3 §5: break at the opportunities UAX #14 gives; a word wider than the
    /// line overflows on a line of its own; spaces at the end of a line hang and are not counted).
    /// </summary>
    public TextSize Wrap(float maxWidth, List<TextLine>? lines)
    {
        var wrap = _wrapAllowed && !float.IsPositiveInfinity(maxWidth);
        var lh = LineHeight;
        var count = 0;
        var maxW = 0f;
        var lineFirst = -1;
        var lastContent = -1;
        var lineW = 0f;
        var pending = 0f;
        var hasContent = false;

        void Emit(int hardAt)
        {
            if (lineFirst < 0 && hardAt < 0)
            {
                return;
            }

            var s = lineFirst >= 0 && lastContent >= 0 ? _start[lineFirst] : (lineFirst >= 0 ? _start[lineFirst] : _start[hardAt]);
            var e = lastContent >= 0 ? _start[lastContent] + _length[lastContent] : s;
            maxW = MathF.Max(maxW, lineW);
            lines?.Add(new TextLine(s, Math.Max(0, e - s), lineW, count * lh, lh));
            count++;
            lineFirst = -1;
            lastContent = -1;
            lineW = 0;
            pending = 0;
            hasContent = false;
        }

        var i = 0;
        while (i < _n)
        {
            var e = i;
            var hard = -1;
            while (e < _n)
            {
                var f = _flags[e];
                e++;
                if ((f & FHard) != 0)
                {
                    hard = e - 1;
                    break;
                }

                if ((f & FBreakAfter) != 0)
                {
                    break;
                }
            }

            var tail = e;
            while (tail > i && (_flags[tail - 1] & FSpace) != 0)
            {
                tail--;
            }

            if (tail > i)
            {
                var contentW = 0f;
                for (var k = i; k < tail; k++)
                {
                    contentW += _advance[k];
                }

                if (hasContent && wrap && lineW + pending + contentW > maxWidth + 0.001f)
                {
                    Emit(-1);
                }

                if (lineFirst < 0)
                {
                    lineFirst = i;
                }

                lineW += pending + contentW;
                lastContent = tail - 1;
                hasContent = true;
                pending = 0;
            }
            else if (_pre && lineFirst < 0 && hard < 0)
            {
                lineFirst = i;
            }

            for (var k = tail; k < e; k++)
            {
                pending += _advance[k];
            }

            if (hard >= 0)
            {
                Emit(hard);
            }

            i = e;
        }

        Emit(-1);
        return new TextSize(maxW, count * lh, count);
    }

    /// <summary>
    /// Appends the glyphs of the line that starts at UTF-16 index <paramref name="start"/> and spans <paramref name="length"/> code units, in
    /// display order (UAX #9 rule L2), with x from the line's left end. Collapsed spaces draw nothing.
    /// </summary>
    public void Place(int start, int length, List<PositionedGlyph> into)
    {
        var a = LowerBound(start);
        var b = a;
        var end = start + length;
        while (b < _n && _start[b] < end)
        {
            b++;
        }

        var count = b - a;
        if (count <= 0)
        {
            return;
        }

        if (_levelScratch.Length < count)
        {
            _levelScratch = new byte[count * 2];
            _orderScratch = new int[count * 2];
        }

        for (var k = 0; k < count; k++)
        {
            _levelScratch[k] = _level[a + k];
        }

        TextShaping.GetVisualOrder(_levelScratch.AsSpan(0, count), _orderScratch);
        var pen = 0f;
        for (var k = 0; k < count; k++)
        {
            var c = a + _orderScratch[k];
            if ((_flags[c] & FCollapsed) != 0)
            {
                continue;
            }

            var first = _glyphFirst[c];
            var gc = _glyphCount[c];
            var rtl = (_level[c] & 1) == 1;
            for (var q = 0; q < gc; q++)
            {
                var g = first + (rtl ? gc - 1 - q : q);
                into.Add(new PositionedGlyph(_glyphFace[g], _glyphId[g], pen + _glyphX[g], _glyphY[g], _glyphAdvance[g]));
                pen += _glyphAdvance[g];
            }

            pen += _spacing;
        }
    }

    private int LowerBound(int start)
    {
        int lo = 0, hi = _n;
        while (lo < hi)
        {
            var mid = (lo + hi) >>> 1;
            if (_start[mid] < start)
            {
                lo = mid + 1;
            }
            else
            {
                hi = mid;
            }
        }

        return lo;
    }

    /// <summary>The source string, for cache validation.</summary>
    public string Text => _text;
}
