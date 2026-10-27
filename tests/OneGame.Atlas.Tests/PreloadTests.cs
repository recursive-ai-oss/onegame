using System.Text;

namespace OneGame.Atlas.Tests;

/// <summary>Preloaded art is rasterized off the lookup path, looks the same as a cold draw, and survives concurrent use.</summary>
public class PreloadTests
{
    private static byte[] Svg(int w, int h, int tint) =>
        Encoding.UTF8.GetBytes($"""<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="0 0 {w} {h}"><rect width="{w}" height="{h}" rx="6" fill="#{tint & 0xFFFFFF:x6}"/><circle cx="{w / 2}" cy="{h / 2}" r="{h / 3}" fill="#ffcc00" fill-opacity="0.6"/></svg>""");

    private static ArtAtlas Make(int count, bool threads, int size = 40)
    {
        var atlas = new ArtAtlas { BackgroundThreads = threads };
        for (var i = 0; i < count; i++)
        {
            atlas.AddSvg("a" + i, Svg(size, size, 0x203040 + (i * 997)));
        }

        atlas.AddSvg("wall", Svg(900, 500, 0x336699));
        return atlas;
    }

    private static string[] Ids(int count) => [.. Enumerable.Range(0, count).Select(i => "a" + i), "wall"];

    private static void Wait(ArtAtlas atlas, ArtPreload p)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (!p.IsComplete)
        {
            Assert.True(DateTime.UtcNow < deadline, "preload did not finish");
            atlas.PumpPreload();
            Thread.Sleep(1);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AfterPreloadTheFirstLookupDoesNoRasterizeWork(bool threads)
    {
        var atlas = Make(12, threads);
        atlas.BeginFrame(1);
        var p = atlas.Preload(Ids(12), 2f);
        Wait(atlas, p);
        Assert.Equal(0, p.Failed);
        Assert.Equal(13, p.Total);
        Assert.Equal(13, atlas.PreloadRasterizations);
        Assert.Equal(0, atlas.SyncRasterizations);

        atlas.BeginFrame(2);
        foreach (var id in Ids(12))
        {
            Assert.Equal(AtlasStatus.Ok, atlas.TryGetScaled(id, 2f, out _));
        }

        Assert.Equal(0, atlas.SyncRasterizations);
        Assert.Equal(13, atlas.PreloadRasterizations);
    }

    [Fact]
    public void ADrawBeforeTheResultArrivesRasterizesOnTheSpotAndTheLateResultIsDropped()
    {
        var atlas = Make(2, threads: false);
        atlas.BeginFrame(1);
        var p = atlas.Preload(["a0"], 1f);
        Assert.False(p.IsComplete);
        Assert.Equal(AtlasStatus.Ok, atlas.TryGetScaled("a0", 1f, out var cold));
        Assert.Equal(1, atlas.SyncRasterizations);
        Wait(atlas, p);
        Assert.Equal(AtlasStatus.Ok, atlas.TryGetScaled("a0", 1f, out var again));
        Assert.Equal(cold, again);
        Assert.Equal(1, atlas.SyncRasterizations);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void APreloadedDrawIsPixelEqualToAColdOne(bool threads)
    {
        var warm = Make(6, threads);
        var cold = Make(6, false);
        warm.BeginFrame(1);
        cold.BeginFrame(1);
        warm.Preload(Ids(6), 1.5f);
        warm.CompletePreload();
        foreach (var id in Ids(6))
        {
            Assert.Equal(AtlasStatus.Ok, warm.TryGetScaled(id, 1.5f, out var w));
            Assert.Equal(AtlasStatus.Ok, cold.TryGetScaled(id, 1.5f, out var c));
            Assert.Equal((w.Width, w.Height), (c.Width, c.Height));
            if (w.Standalone != 0)
            {
                Assert.True(warm.TryGetStandalone(w.Standalone, out var wa));
                Assert.True(cold.TryGetStandalone(c.Standalone, out var ca));
                Assert.True(wa.Pixels.SequenceEqual(ca.Pixels));
                continue;
            }

            Assert.Equal(c.Slice, w.Slice);
            var wp = warm.Pages[w.Page];
            var cp = cold.Pages[c.Page];
            for (var y = 0; y < w.Height; y++)
            {
                Assert.True(wp.Pixels.Slice(((w.Y + y) * wp.Stride) + (w.X * 4), w.Width * 4).SequenceEqual(cp.Pixels.Slice(((c.Y + y) * cp.Stride) + (c.X * 4), c.Width * 4)));
            }
        }
    }

    [Fact]
    public void WithoutThreadsWorkIsSpreadOverFramesByTheBudget()
    {
        var atlas = Make(30, threads: false, size: 200);
        atlas.PreloadFrameBudget = TimeSpan.Zero; // one item per frame
        atlas.BeginFrame(1);
        var p = atlas.Preload(Ids(30), 1f);
        var frames = 0;
        while (!p.IsComplete)
        {
            atlas.BeginFrame(2 + frames++);
            Assert.True(frames < 100);
        }

        Assert.True(frames >= 20, $"finished in {frames} frames");
        Assert.Equal(0, atlas.SyncRasterizations);
    }

    [Fact]
    public void ConcurrentPreloadWhileDrawingKeepsEveryResultCorrect()
    {
        var atlas = Make(40, threads: true);
        var reference = Make(40, false);
        reference.BeginFrame(1);
        var ids = Ids(40);
        var p = atlas.Preload(ids, 1f);
        var frame = 1L;
        var order = new Random(7);
        var deadline = DateTime.UtcNow.AddSeconds(60);
        while (!p.IsComplete)
        {
            Assert.True(DateTime.UtcNow < deadline);
            atlas.BeginFrame(++frame);
            for (var i = 0; i < 10; i++)
            {
                Assert.Equal(AtlasStatus.Ok, atlas.TryGetScaled(ids[order.Next(ids.Length)], 1f, out _));
            }
        }

        atlas.BeginFrame(++frame);
        foreach (var id in ids)
        {
            Assert.Equal(AtlasStatus.Ok, atlas.TryGetScaled(id, 1f, out var r));
            Assert.Equal(AtlasStatus.Ok, reference.TryGetScaled(id, 1f, out var e));
            if (r.Standalone != 0)
            {
                Assert.True(atlas.TryGetStandalone(r.Standalone, out var a));
                Assert.True(reference.TryGetStandalone(e.Standalone, out var b));
                Assert.True(a.Pixels.SequenceEqual(b.Pixels));
            }
            else
            {
                Assert.Equal(e.Width, r.Width);
                var ap = atlas.Pages[r.Page];
                var bp = reference.Pages[e.Page];
                for (var y = 0; y < r.Height; y++)
                {
                    Assert.True(ap.Pixels.Slice(((r.Y + y) * ap.Stride) + (r.X * 4), r.Width * 4).SequenceEqual(bp.Pixels.Slice(((e.Y + y) * bp.Stride) + (e.X * 4), e.Width * 4)), id);
                }
            }
        }

        Assert.Equal(0, p.Failed);
    }

    [Fact]
    public void ReplacingArtDuringPreloadNeverStoresTheOldRaster()
    {
        var atlas = Make(1, threads: true);
        atlas.BeginFrame(1);
        var p = atlas.Preload(["a0"], 1f);
        atlas.AddSvg("a0", Svg(40, 40, 0xff0000));
        atlas.CompletePreload();
        Assert.True(p.IsComplete);
        Assert.Equal(AtlasStatus.Ok, atlas.TryGetScaled("a0", 1f, out var r));
        var page = atlas.Pages[r.Page];
        // The red rectangle's corner-adjacent pixel (inside the rounded corners, outside the circle) is red.
        var px = page.Pixels.Slice(((r.Y + 20) * page.Stride) + ((r.X + 2) * 4), 4);
        Assert.Equal(255, px[0]);
        Assert.Equal(0, px[2]);
    }

    [Fact]
    public void UnknownTooLargeAndDamagedArtFinishAsFailedAndDrawAsBefore()
    {
        var atlas = Make(1, threads: true);
        atlas.BeginFrame(1);
        var p = atlas.Preload(["nope", "a0", "wall"], 100f);
        atlas.CompletePreload();
        Assert.True(p.IsComplete);
        Assert.Equal(3, p.Failed);
        Assert.Equal(AtlasStatus.UnknownResource, atlas.TryGetScaled("nope", 1f, out _));
        Assert.Equal(AtlasStatus.TooLarge, atlas.TryGetScaled("a0", 100f, out _));
    }
}
