using System.Text;

namespace OneGame.Atlas.Tests;

/// <summary>Rasters too big for a shared page get a texture of their own, inside a bounded amount of memory.</summary>
public class StandaloneArtTests
{
    private static byte[] Svg(int w, int h) =>
        Encoding.UTF8.GetBytes($"""<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="0 0 {w} {h}"><rect width="{w}" height="{h}" fill="#336699"/><circle cx="{w / 2}" cy="{h / 2}" r="{h / 3}" fill="#ffcc00"/></svg>""");

    [Fact]
    public void ARasterOverTheThresholdGetsItsOwnTextureAtFullSize()
    {
        var atlas = new ArtAtlas();
        atlas.AddSvg("wall", Svg(1000, 600));
        Assert.Equal(AtlasStatus.Ok, atlas.TryGet("wall", 1920, 1080, out var r));
        Assert.Equal(-1, r.Page);
        Assert.NotEqual(0, r.Standalone);
        Assert.Equal((1920, 1080), (r.Width, r.Height));
        Assert.Equal((0f, 0f, 1f, 1f), (r.U0, r.V0, r.U1, r.V1));
        Assert.Equal(0, atlas.Pages.PageCount);
        Assert.True(atlas.TryGetStandalone(r.Standalone, out var art));
        Assert.Equal(1920 * 1080 * 4, art.Pixels.Length);
        Assert.Equal(1920L * 1080 * 4, atlas.StandaloneBytes);

        // The same request is the same texture; a small one still goes to a page.
        Assert.Equal(AtlasStatus.Ok, atlas.TryGet("wall", 1920, 1080, out var again));
        Assert.Equal(r.Standalone, again.Standalone);
        Assert.Equal(AtlasStatus.Ok, atlas.TryGet("wall", 64, 40, out var small));
        Assert.Equal(0, small.Standalone);

        // Once uploaded, the CPU copy goes away; the byte count is about the GPU copy.
        art.MarkUploaded();
        Assert.Empty(art.Pixels.ToArray());
        Assert.False(art.NeedsUpload);
        Assert.Equal(1920L * 1080 * 4, atlas.StandaloneBytes);
    }

    [Fact]
    public void FittedLookupsRasterizeAnSvgAtTheBoxSizeAndKeepIconsOnTheirNaturalRaster()
    {
        var atlas = new ArtAtlas();
        atlas.AddSvg("wall", Svg(1024, 576));
        atlas.AddSvg("icon", Svg(24, 24));
        Assert.Equal(AtlasStatus.Ok, atlas.TryGetFitted("wall", 1920, 1080, 1.5f, out var wall));
        Assert.Equal((1920, 1080), (wall.Width, wall.Height));
        // 24 × 1.5 = 36: a 36.4 px box is the same raster, not a new one.
        Assert.Equal(AtlasStatus.Ok, atlas.TryGetFitted("icon", 36.4f, 36f, 1.5f, out var icon));
        Assert.Equal((36, 36), (icon.Width, icon.Height));
        // A small box keeps the natural raster too, however far off; only boxes over the threshold get an exact one.
        Assert.Equal(AtlasStatus.Ok, atlas.TryGetFitted("icon", 90f, 60f, 1.5f, out var wide));
        Assert.Equal((36, 36), (wide.Width, wide.Height));
        Assert.Equal(AtlasStatus.Ok, atlas.TryGetFitted("icon", 700f, 90f, 1.5f, out var strip));
        Assert.Equal((700, 90), (strip.Width, strip.Height));
        // A box beyond the limits falls back to the natural raster instead of failing.
        Assert.Equal(AtlasStatus.Ok, atlas.TryGetFitted("icon", 20000f, 20000f, 1.5f, out var huge));
        Assert.Equal((36, 36), (huge.Width, huge.Height));
    }

    [Fact]
    public void StandaloneMemoryNeverPassesTheLimitAndTheLeastRecentlyUsedGoFirst()
    {
        const long each = 800L * 600 * 4;
        var atlas = new ArtAtlas(limits: new ArtLimits { MaxStandaloneBytes = (2 * each) + 100 });
        foreach (var id in new[] { "a", "b", "c" })
        {
            atlas.AddSvg(id, Svg(800, 600));
        }

        atlas.BeginFrame(1);
        Assert.Equal(AtlasStatus.Ok, atlas.TryGet("a", 800, 600, out var a));
        Assert.Equal(AtlasStatus.Ok, atlas.TryGet("b", 800, 600, out var b));
        // Both were drawn this frame, so neither may be given up: the third lookup is refused, and nothing was rasterized for it.
        Assert.Equal(AtlasStatus.Full, atlas.TryGet("c", 800, 600, out _));
        Assert.Equal(2 * each, atlas.StandaloneBytes);

        atlas.BeginFrame(2);
        Assert.Equal(AtlasStatus.Ok, atlas.TryGet("b", 800, 600, out _));   // b is in use again, a is the oldest
        Assert.Equal(AtlasStatus.Ok, atlas.TryGet("c", 800, 600, out var c));
        Assert.Equal(2, atlas.StandaloneCount);
        Assert.False(atlas.TryGetStandalone(a.Standalone, out _));
        Assert.True(atlas.TryGetStandalone(b.Standalone, out _));
        Assert.True(atlas.TryGetStandalone(c.Standalone, out _));
        var released = new List<int>();
        atlas.TakeReleased(released);
        Assert.Equal([a.Standalone], released);
        Assert.True(atlas.StandaloneBytes <= (2 * each) + 100);

        // A single image bigger than the whole budget is refused outright.
        var tight = new ArtAtlas(limits: new ArtLimits { MaxStandaloneBytes = each - 1 });
        tight.AddSvg("a", Svg(800, 600));
        Assert.Equal(AtlasStatus.TooLarge, tight.TryGet("a", 800, 600, out _));
    }

    [Fact]
    public void ReplacingAResourceReleasesItsStandaloneTextures()
    {
        var atlas = new ArtAtlas();
        atlas.AddSvg("wall", Svg(800, 600));
        Assert.Equal(AtlasStatus.Ok, atlas.TryGet("wall", 800, 600, out var r));
        atlas.AddSvg("wall", Svg(400, 300));
        Assert.Equal(0, atlas.StandaloneCount);
        Assert.Equal(0, atlas.StandaloneBytes);
        var released = new List<int>();
        atlas.TakeReleased(released);
        Assert.Equal([r.Standalone], released);
    }

    [Fact]
    public void RasterLimitsStillRefuseBeforeAllocating()
    {
        var atlas = new ArtAtlas();
        atlas.AddSvg("wall", Svg(800, 600));
        var before = GC.GetAllocatedBytesForCurrentThread();
        Assert.Equal(AtlasStatus.TooLarge, atlas.TryGet("wall", 8192, 8192, out _));
        Assert.Equal(AtlasStatus.TooLarge, atlas.TryGet("wall", 9000, 10, out _));
        Assert.True(GC.GetAllocatedBytesForCurrentThread() - before < 64 * 1024);
        Assert.Equal(0, atlas.StandaloneCount);
    }
}
