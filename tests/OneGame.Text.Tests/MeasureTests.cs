using OneFont;
using OneGame.UI;

namespace OneGame.Text.Tests;

public class MeasureTests
{
    private static readonly int[] StackIds = [0, 2, 3, 4]; // Noto Sans, then the Arabic, Hebrew and CJK fallbacks of Fonts.Library()

    private static readonly int[] HebrewVisual = [0x05DD, 0x05D5, 0x05DC, 0x05E9];

    private static TextStyle Style(float size = 16, WhiteSpace ws = WhiteSpace.Normal, float spacing = 0) =>
        new("Noto Sans", size, 400, false, 0, spacing, ws);

    private static (FontTextMeasurer Measurer, FontLibrary Lib) New()
    {
        var lib = Fonts.Library();
        return (new FontTextMeasurer(lib), lib);
    }

    /// <summary>Lays out, measures and places a text, and checks that all three agree. Returns the lines with their glyphs.</summary>
    private static List<(TextLine Line, List<PositionedGlyph> Glyphs)> LayOut(FontTextMeasurer m, string text, TextStyle style, float maxWidth)
    {
        var lines = new List<TextLine>();
        m.Layout(text, style, maxWidth, lines);
        var size = m.Measure(text, style, maxWidth);
        Assert.Equal(lines.Count, size.LineCount);
        Assert.Equal(lines.Count == 0 ? 0 : lines.Max(l => l.Width), size.Width, 0.001);
        Assert.Equal(lines.Sum(l => l.Height), size.Height, 0.001);
        var result = new List<(TextLine, List<PositionedGlyph>)>();
        var prevEnd = 0;
        for (var i = 0; i < lines.Count; i++)
        {
            var l = lines[i];
            Assert.True(l.Start >= prevEnd && l.Start + l.Length <= text.Length, $"line {i} range {l.Start}+{l.Length} outside the text or overlapping");
            prevEnd = l.Start + l.Length;
            Assert.Equal(i * l.Height, l.Y, 0.001);
            var glyphs = new List<PositionedGlyph>();
            m.Place(text, style, l.Start, l.Length, l.Height, glyphs);

            // The drawn extent is the sum of the glyph advances (plus letter spacing per character), which is what layout measured.
            var drawn = glyphs.Sum(g => g.Advance) + (style.LetterSpacing * text.AsSpan(l.Start, l.Length).Length);
            if (style.LetterSpacing == 0)
            {
                Assert.Equal(l.Width, drawn, 0.002);
            }

            result.Add((l, glyphs));
        }

        return result;
    }

    [Fact]
    public void LatinMatchesAnIndependentShapingOfTheLine()
    {
        var (m, lib) = New();
        const string text = "Silk 95g AVATAR fi";
        var style = Style(20);
        var line = Assert.Single(LayOut(m, text, style, float.PositiveInfinity));
        var face = lib.Face(0);
        var scale = 20f / face.UnitsPerEm;
        var x = 0f;
        var k = 0;
        foreach (var cluster in TextShaping.ShapeRun(text, face))
        {
            foreach (var g in cluster.Glyphs)
            {
                Assert.Equal(g.GlyphId, line.Glyphs[k].GlyphId);
                Assert.Equal(0, line.Glyphs[k].FaceId);
                Assert.Equal(x + (g.XOffset * scale), line.Glyphs[k].X, 0.001);
                x += g.XAdvance * scale;
                k++;
            }
        }

        Assert.Equal(k, line.Glyphs.Count);
        Assert.Equal(x, line.Line.Width, 0.001);
        var m2 = face.Metrics;
        Assert.Equal((m2.Ascender - m2.Descender + m2.LineGap) * scale, line.Line.Height, 0.001);
    }

    [Fact]
    public void FallbackFaceDrawsTheKanaAndAgreesWithTheChain()
    {
        var (m, lib) = New();
        const string text = "Quest: こんにちは!";
        var style = Style(18);
        var line = Assert.Single(LayOut(m, text, style, float.PositiveInfinity));
        var chain = new FontFallbackChain(StackIds.Select(lib.Face));
        var scale = 18f / 1000f;
        var x = 0f;
        var k = 0;
        foreach (var cluster in chain.ShapeRun(text))
        {
            foreach (var g in cluster.Glyphs)
            {
                Assert.Equal(g.GlyphId, line.Glyphs[k].GlyphId);
                Assert.Equal(StackIds[cluster.FaceIndex], line.Glyphs[k].FaceId);
                Assert.Equal(x + (g.XOffset * scale), line.Glyphs[k].X, 0.001);
                x += g.XAdvance * scale;
                k++;
            }
        }

        Assert.Equal(k, line.Glyphs.Count);
        Assert.Equal(5, line.Glyphs.Count(g => g.FaceId == 4)); // the five kana come from the CJK face
        Assert.Equal(x, line.Line.Width, 0.001);
    }

    [Fact]
    public void WrappingBreaksAtOpportunitiesAndKeepsLinesWithinTheWidth()
    {
        var (m, _) = New();
        const string text = "Trade goods across the harbour before the tide turns";
        var style = Style(16);
        foreach (var width in new[] { 90f, 150f, 260f })
        {
            var lines = LayOut(m, text, style, width);
            Assert.True(lines.Count > 1);
            foreach (var (l, glyphs) in lines)
            {
                var sub = text.Substring(l.Start, l.Length);
                Assert.True(l.Width <= width + 0.001f || !sub.Contains(' '), $"'{sub}' is {l.Width} wide in {width}");
                Assert.False(sub.StartsWith(' ') || sub.EndsWith(' '));
                Assert.True(glyphs.Count > 0);
            }

            Assert.Equal(text, string.Join(' ', lines.Select(l => text.Substring(l.Line.Start, l.Line.Length))));

            // A line could not have taken the first word of the next one: that is what makes the wrap greedy.
            for (var i = 0; i + 1 < lines.Count; i++)
            {
                var next = text.Substring(lines[i + 1].Line.Start, lines[i + 1].Line.Length).Split(' ')[0];
                var joined = string.Concat(text.AsSpan(lines[i].Line.Start, lines[i].Line.Length), " ", next);
                Assert.True(m.Measure(joined, style, float.PositiveInfinity).Width > width, $"'{next}' would have fitted on line {i}");
            }
        }

        // Min-content: one word per line, as wide as the longest word. Max-content: one line.
        var words = text.Split(' ');
        var min = LayOut(m, text, style, 0);
        Assert.Equal(words.Length, min.Count);
        Assert.Equal(words.Max(w => m.Measure(w, style, float.PositiveInfinity).Width), m.Measure(text, style, 0).Width, 0.001);
        Assert.Single(LayOut(m, text, style, float.PositiveInfinity));
    }

    [Fact]
    public void WrappedCjkAndLatinBreakInsideTheKanaRun()
    {
        var (m, _) = New();
        const string text = "こんにちはこんにちはこんにちは";
        var lines = LayOut(m, text, Style(20), 100);
        Assert.True(lines.Count >= 3);
        Assert.All(lines, l => Assert.True(l.Line.Width <= 100.001f));
        Assert.Equal(text, string.Concat(lines.Select(l => text.Substring(l.Line.Start, l.Line.Length))));
    }

    [Fact]
    public void HebrewRunIsReversedInsideLeftToRightText()
    {
        var (m, lib) = New();
        const string text = "abc \u05E9\u05DC\u05D5\u05DD def";
        var line = Assert.Single(LayOut(m, text, Style(18), float.PositiveInfinity));
        var hebrew = lib.Face(3);
        var ids = line.Glyphs.Where(g => g.FaceId == 3).Select(g => g.GlyphId).ToArray();
        Assert.Equal(HebrewVisual.Select(hebrew.GetGlyphIndex), ids);
        for (var i = 1; i < line.Glyphs.Count; i++)
        {
            Assert.True(line.Glyphs[i].X >= line.Glyphs[i - 1].X, "glyph x must not go backwards");
        }

        // abc, space, [ם ו ל ש], space, def
        Assert.Equal(3 + 1 + 4 + 1 + 3, line.Glyphs.Count);
        Assert.Equal(lib.Face(0).GetGlyphIndex('a'), line.Glyphs[0].GlyphId);
        Assert.Equal(lib.Face(0).GetGlyphIndex('f'), line.Glyphs[^1].GlyphId);
    }

    [Theory]
    [InlineData("Price \u0633\u0644\u0627\u0645", true)]   // left-to-right paragraph: the Arabic ends up on the right
    [InlineData("\u0633\u0644\u0627\u0645 Price", false)]  // right-to-left paragraph: the Latin word ends up on the left
    public void ArabicAndLatinAreOrderedByParagraphDirection(string text, bool latinFirst)
    {
        var (m, lib) = New();
        var line = Assert.Single(LayOut(m, text, Style(18), float.PositiveInfinity));
        var latin = line.Glyphs.Where(g => g.FaceId == 0 && g.GlyphId != lib.Face(0).GetGlyphIndex(' ')).ToArray();
        var arabic = line.Glyphs.Where(g => g.FaceId == 2 && g.GlyphId != lib.Face(2).GetGlyphIndex(' ')).ToArray();
        Assert.Equal(5, latin.Length);
        Assert.Equal(3, arabic.Length); // four letters, but lam + alef form one ligature glyph
        if (latinFirst)
        {
            Assert.True(latin.Max(g => g.X) < arabic.Min(g => g.X));
        }
        else
        {
            Assert.True(arabic.Min(g => g.X) > latin.Max(g => g.X));
        }

        // The letters took their joined forms: at least one glyph is not the isolated letter's cmap glyph.
        var arabicFace = lib.Face(2);
        var isolated = "\u0633\u0644\u0627\u0645".Select(c => arabicFace.GetGlyphIndex(c)).ToHashSet();
        Assert.Contains(arabic, g => !isolated.Contains(g.GlyphId));
    }

    [Fact]
    public void SpacesCollapseExceptInPreformattedText()
    {
        var (m, _) = New();
        var style = Style(16);
        Assert.Equal(m.Measure("a b", style, float.PositiveInfinity).Width, m.Measure("  a  \t b  ", style, float.PositiveInfinity).Width, 0.001);
        var pre = Style(16, WhiteSpace.Pre);
        var lines = LayOut(m, "one\n\ntwo  x\n", pre, 0);
        Assert.Equal(3, lines.Count);
        Assert.Equal(0, lines[1].Line.Length);
        Assert.True(lines[2].Line.Width > m.Measure("twox", pre, 0).Width);
        Assert.Equal(0, m.Measure("", style, 100).LineCount);
        Assert.Equal(0, m.Measure("   ", style, 100).LineCount);
        Assert.Single(LayOut(m, "x", Style(16, WhiteSpace.NoWrap), 1));
    }

    [Fact]
    public void LetterSpacingWidensEveryCharacter()
    {
        var (m, _) = New();
        var plain = m.Measure("abcd", Style(16), float.PositiveInfinity).Width;
        var spaced = m.Measure("abcd", Style(16, spacing: 2), float.PositiveInfinity).Width;
        Assert.Equal(plain + 8, spaced, 0.02);
    }

    [Fact]
    public void BoldAndSizeSelectTheirOwnFaceAndMetrics()
    {
        var (m, _) = New();
        var regular = m.Measure("Silk market", Style(16), float.PositiveInfinity).Width;
        var bold = m.Measure("Silk market", new TextStyle("Noto Sans", 16, 700, false, 0, 0, WhiteSpace.Normal), float.PositiveInfinity).Width;
        Assert.NotEqual(regular, bold);
        Assert.Equal(regular * 2, m.Measure("Silk market", Style(32), float.PositiveInfinity).Width, 0.05);
        Assert.Equal(40, m.Measure("x", new TextStyle("Noto Sans", 16, 400, false, 40, 0, WhiteSpace.Normal), 100).Height, 0.001);
    }

    [Fact]
    public void LimitsAndMissingFontsAreTypedErrors()
    {
        var (m, _) = New();
        Assert.Throws<TextLimitException>(() => m.Measure(new string('a', FontTextMeasurer.MaxTextLength + 1), Style(), 100));
        Assert.Throws<FontNotFoundException>(() => new FontTextMeasurer(new FontLibrary()).Measure("a", Style(), 100));
        Assert.Throws<FontFormatException>(() => new FontLibrary().Add(new byte[2000]));
        Assert.Throws<TextLimitException>(() => new FontLibrary().Add(new byte[FontLibrary.MaxFontBytes + 1]));
        Assert.Equal(0, m.Measure("hello", new TextStyle("Noto Sans", float.NaN, 400, false, 0, 0, WhiteSpace.Normal), 100).Width, 0.5);
    }
}
