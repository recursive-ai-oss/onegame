using System.Numerics;
using OneFont;
using OneGame.Atlas;
using OneGame.UI;
using OneImage;
using OneImage.Drawing;
using DrawPath = OneImage.Drawing.Path;

namespace OneGame.Text.Tests;

public class GlyphAtlasTests
{
    /// <summary>Replays an outline into a OneImage path, as an independent rasterizer's input.</summary>
    private sealed class PathSink(float scale, float dx, float dy) : IOutlineSink
    {
        private readonly PathBuilder _b = new();

        public DrawPath Build() => _b.Build();

        public void MoveTo(float x, float y) => _b.MoveTo((x * scale) + dx, (-y * scale) + dy);

        public void LineTo(float x, float y) => _b.LineTo((x * scale) + dx, (-y * scale) + dy);

        public void QuadTo(float cx, float cy, float x, float y) => _b.QuadraticBezierTo((cx * scale) + dx, (-cy * scale) + dy, (x * scale) + dx, (-y * scale) + dy);

        public void CubicTo(float c1x, float c1y, float c2x, float c2y, float x, float y) =>
            _b.CubicBezierTo((c1x * scale) + dx, (-c1y * scale) + dy, (c2x * scale) + dx, (-c2y * scale) + dy, (x * scale) + dx, (-y * scale) + dy);

        public void Close() => _b.Close();
    }

    [Theory]
    [InlineData(0, 'g', 17, 0)]
    [InlineData(0, 'S', 23, 1)]
    [InlineData(0, '@', 31, 3)]
    [InlineData(2, 0x0633, 20, 2)]  // Arabic seen
    [InlineData(4, 0x65E5, 24, 0)]  // a CJK ideograph if the subset has it, else the missing-glyph box
    [InlineData(0, 'W', 11, 2)]
    public void CoverageMatchesAnIndependentExactAreaRasterizer(int faceId, int codePoint, float size, int step)
    {
        var lib = Fonts.Library();
        var atlas = new GlyphAtlas(lib);
        var face = lib.Face(faceId);
        var gid = face.GetGlyphIndex(codePoint);
        Assert.Equal(AtlasStatus.Ok, atlas.TryGet(faceId, gid, size, step, out var info));
        Assert.True(info.Width > 0 && info.Height > 0);

        // Reference: the same outline filled by OneImage's rasterizer on a canvas, pen at (pad + shift, baseline).
        const int pad = 4;
        var scale = size / face.UnitsPerEm;
        var w = info.Width + (2 * pad);
        var h = info.Height + (2 * pad);
        var shift = step / 4f;
        var sink = new PathSink(scale, pad - info.OffsetX + shift, pad - info.OffsetY);
        face.GetOutline(gid).Write(sink);
        using var canvas = new Canvas(w, h, PixelFormat.Rgba8);
        canvas.Fill(new Rgba32(0, 0, 0, 255), sink.Build());

        var page = atlas.Pages[info.Page];
        var maxDiff = 0;
        long sum = 0;
        long ink = 0;
        for (var y = 0; y < info.Height; y++)
        {
            for (var x = 0; x < info.Width; x++)
            {
                var mine = page.Pixels[((info.Y + y) * page.Stride) + info.X + x];
                var theirs = canvas[x + pad, y + pad].A;
                var d = Math.Abs(mine - theirs);
                maxDiff = Math.Max(maxDiff, d);
                sum += d;
                ink += theirs;
            }
        }

        Assert.True(ink > 200, "the reference glyph has no ink; the case tests nothing");
        var mean = (double)sum / (info.Width * info.Height);
        Assert.True(maxDiff <= 12 && mean < 0.6, $"max {maxDiff}, mean {mean:F3}");
    }

    [Fact]
    public void LookupsAreCachedPaddedAndDirtyRegionsAreReported()
    {
        var lib = Fonts.Library();
        var atlas = new GlyphAtlas(lib, new AtlasOptions { PageSize = 256 });
        var face = lib.Face(0);
        var gid = face.GetGlyphIndex('A');
        Assert.Equal(AtlasStatus.Ok, atlas.TryGet(0, gid, 16, 0, out var a));
        Assert.Equal(AtlasStatus.Ok, atlas.TryGet(0, gid, 16, 0, out var again));
        Assert.Equal(a, again);
        Assert.Equal(AtlasStatus.Ok, atlas.TryGet(0, gid, 16, 1, out var shifted));
        Assert.NotEqual(a, shifted);
        Assert.Equal(AtlasStatus.Ok, atlas.TryGet(0, gid, 17, 0, out var bigger));
        Assert.True(bigger.Height > a.Height);

        // A new page starts fully dirty; taking the dirty region cleans it; a later glyph dirties only its own rectangle.
        var page = atlas.Pages[0];
        Assert.Equal(new AtlasRect(0, 0, 256, 256), page.TakeDirty());
        Assert.True(page.TakeDirty().IsEmpty);
        Assert.Equal(AtlasStatus.Ok, atlas.TryGet(0, face.GetGlyphIndex('B'), 16, 0, out var b));
        var dirty = page.TakeDirty();
        Assert.Equal(new AtlasRect(b.X, b.Y, b.Width, b.Height), dirty);

        // Every pixel of the one-pixel border around an entry is still zero, so bilinear sampling never picks up a neighbour.
        for (var x = a.X - 1; x <= a.X + a.Width; x++)
        {
            Assert.Equal(0, page.Pixels[((a.Y - 1) * page.Stride) + x]);
            Assert.Equal(0, page.Pixels[((a.Y + a.Height) * page.Stride) + x]);
        }

        // Texture coordinates address the same texels.
        Assert.Equal(a.X / 256f, a.U0);
        Assert.Equal((a.Y + a.Height) / 256f, a.V1);

        // A space has no ink and no entry.
        Assert.Equal(AtlasStatus.Ok, atlas.TryGet(0, face.GetGlyphIndex(' '), 16, 0, out var space));
        Assert.Equal(0, space.Width);
    }

    [Fact]
    public void FullPagesClearTheLeastRecentlyUsedPageAndNeverOneInUse()
    {
        var lib = Fonts.Library();
        var atlas = new GlyphAtlas(lib, new AtlasOptions { PageSize = 128, MaxPages = 2 });
        var face = lib.Face(0);
        var all = Enumerable.Range('!', 94).Select(c => face.GetGlyphIndex(c)).ToArray();

        // Frame 1 fills both pages with 30 px glyphs.
        atlas.BeginFrame(1);
        var placed = new List<(int Glyph, GlyphInfo Info)>();
        var epoch = atlas.Pages.Epoch;
        foreach (var g in all)
        {
            if (atlas.TryGet(0, g, 30, 0, out var info) != AtlasStatus.Ok)
            {
                break;
            }

            placed.Add((g, info));
        }

        // Inside one frame nothing may be cleared, so the atlas refuses once the two pages are full.
        Assert.True(placed.Count < all.Length);
        Assert.Equal(2, atlas.Pages.PageCount);
        Assert.Equal(epoch, atlas.Pages.Epoch);
        Assert.Equal(AtlasStatus.Full, atlas.TryGet(0, all[placed.Count], 30, 0, out _));

        // The next frame may clear a page; entries on the surviving page are untouched and still resolve to the same place.
        atlas.BeginFrame(2);
        var keep = placed[0];
        Assert.Equal(AtlasStatus.Ok, atlas.TryGet(0, keep.Glyph, 30, 0, out var kept));
        Assert.Equal(keep.Info, kept);
        var next = all[placed.Count];
        Assert.Equal(AtlasStatus.Ok, atlas.TryGet(0, next, 30, 0, out var fresh));
        Assert.NotEqual(kept.Page, fresh.Page);
        Assert.True(atlas.Pages.Epoch > epoch);
        Assert.Equal(AtlasStatus.Ok, atlas.TryGet(0, keep.Glyph, 30, 0, out kept));
        Assert.Equal(keep.Info, kept);
    }

    [Fact]
    public void OversizedRequestsAreRefusedWithoutAllocating()
    {
        var lib = Fonts.Library();
        var atlas = new GlyphAtlas(lib);
        var gid = lib.Face(0).GetGlyphIndex('W');
        Assert.Equal(AtlasStatus.Ok, atlas.TryGet(0, gid, 16, 0, out _)); // warm up
        var before = GC.GetAllocatedBytesForCurrentThread();
        Assert.Equal(AtlasStatus.TooLarge, atlas.TryGet(0, gid, 100_000, 0, out _));
        Assert.Equal(AtlasStatus.TooLarge, atlas.TryGet(0, gid, 513, 0, out _));
        Assert.Equal(AtlasStatus.TooLarge, atlas.TryGet(0, gid, float.NaN, 0, out _));
        Assert.Equal(AtlasStatus.TooLarge, atlas.TryGet(0, gid, 16, 9, out _));
        Assert.True(GC.GetAllocatedBytesForCurrentThread() - before < 4096);

        // A bitmap over the pixel budget (not just the size budget) is refused before the bitmap exists.
        var tight = new GlyphAtlas(lib, limits: new GlyphLimits { MaxBitmapPixels = 100 });
        before = GC.GetAllocatedBytesForCurrentThread();
        Assert.Equal(AtlasStatus.TooLarge, tight.TryGet(0, gid, 40, 0, out _));
        Assert.True(GC.GetAllocatedBytesForCurrentThread() - before < 4096);
        Assert.Equal(AtlasStatus.UnknownResource, atlas.TryGet(0, 999_999, 16, 0, out _));
        Assert.Throws<FontNotFoundException>(() => atlas.TryGet(77, 1, 16, 0, out _));
    }

    private const string Heading = "Gold 1,200 \u00B7 Silk 95g";
    private const string Mixed = "Port \u05E9\u05DC\u05D5\u05DD \u0633\u0644\u0627\u0645 AV";
    private const string Kana = "\u3053\u3093\u306B\u3061\u306F fi";

    private static (CpuCanvas Canvas, DisplayList List) Render(float scale, GlyphAtlas atlas, FontTextMeasurer measurer)
    {
        var ui = new UiTree(measurer);
        ui.SetViewport(240 * scale, 90 * scale, scale);
        ui.AddStyleSheet(new StyleSheet().Rule(":root", s => s.FontSize(Length.Ui(14)).TextColor(Color.Hex("#101820"))).Rule(".box", s => s.Padding(Length.Ui(6))));
        ui.Root.With(Ui.Column(Ui.Label(Heading), Ui.Label(Mixed), Ui.Label(Kana)).Class("box"));
        var list = ui.Frame();
        atlas.BeginFrame(list.Version);
        var quads = new List<GlyphQuad>();
        Assert.Equal(0, new TextQuadBuilder(measurer, atlas).BuildAll(list, quads));
        var canvas = new CpuCanvas((int)(240 * scale), (int)(90 * scale));
        canvas.Draw(quads, atlas);
        return (canvas, list);
    }

    [Theory]
    [InlineData(1f, "text-1x.png")]
    [InlineData(2f, "text-2x.png")]
    public void ComposedUiTextMatchesTheCommittedReferenceAtTwoScales(float scale, string reference)
    {
        var lib = Fonts.Library();
        var measurer = new FontTextMeasurer(lib);
        var atlas = new GlyphAtlas(lib);
        var (canvas, list) = Render(scale, atlas, measurer);
        Assert.Equal(3, list.TextRuns.Length);
        Assert.Equal(14 * scale, list.TextRuns[0].Style.Size);
        Golden.Check(reference, canvas.Width, canvas.Height, canvas.Pixels);
        var ink = canvas.Pixels.Count(p => p.R < 128);
        Assert.True(ink > 400 * scale * scale, $"only {ink} dark pixels");
        if (Environment.GetEnvironmentVariable("ONEGAME_SAVE_FRAMES") is { Length: > 0 } dir)
        {
            canvas.Save(System.IO.Path.Combine(dir, reference));
        }
    }

    [Fact]
    public void GlyphsLandWhereTheLinesSay()
    {
        var lib = Fonts.Library();
        var measurer = new FontTextMeasurer(lib);
        var atlas = new GlyphAtlas(lib);
        var (canvas, list) = Render(1, atlas, measurer);

        // Each line's ink lies inside its line box horizontally (from the line's x to its measured width) and vertically
        // between the top of the box and a little below its bottom (descenders).
        for (var r = 0; r < list.TextRuns.Length; r++)
        {
            var run = list.TextRuns[r];
            var quads = new List<GlyphQuad>();
            new TextQuadBuilder(measurer, atlas).Build(list, r, quads);
            foreach (var q in quads)
            {
                var inLine = false;
                for (var i = 0; i < run.LineCount; i++)
                {
                    var l = list.Lines[run.FirstLine + i];
                    inLine |= q.X0 >= l.X - 2 && q.X1 <= l.X + l.Width + 2 && q.Y0 >= l.Y - 2 && q.Y1 <= l.Y + l.Height + 4;
                }

                Assert.True(inLine, $"glyph quad {q} is outside every line of run {r}");
            }
        }

        Assert.NotNull(canvas);
    }
}
