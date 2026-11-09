using System.Text;

namespace OneGame.Atlas.Tests;

/// <summary>A frame that needs more art than the pages hold still draws all of it; an image that truly cannot fit is reported, not painted magenta.</summary>
public class AtlasOverflowTests
{
    private static byte[] Portrait(int w, int h, int index) =>
        Encoding.UTF8.GetBytes($"""<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="0 0 {w} {h}"><rect width="{w}" height="{h}" fill="rgb({index * 2 % 256},{index * 7 % 256},{255 - index})"/></svg>""");

    private static (byte R, byte G, byte B) CentrePixel(ArtAtlas atlas, ArtRegion r)
    {
        ReadOnlySpan<byte> px;
        int stride;
        int x = r.Width / 2, y = r.Height / 2;
        if (r.Standalone != 0)
        {
            Assert.True(atlas.TryGetStandalone(r.Standalone, out var art));
            px = art.Pixels;
            stride = art.Width * 4;
        }
        else
        {
            px = atlas.Pages[r.Page].Pixels;
            stride = atlas.Pages[r.Page].Stride;
            x += r.X;
            y += r.Y;
        }

        var o = (y * stride) + (x * 4);
        return (px[o], px[o + 1], px[o + 2]);
    }

    [Fact]
    public void OneFrameNeedingMoreThanTheFourPagesDrawsEveryImage()
    {
        const int Count = 120;
        var atlas = new ArtAtlas();
        for (var i = 0; i < Count; i++)
        {
            atlas.AddSvg("p" + i, Portrait(256, 320, i));
        }

        // 120 x 256 x 320 x 4 bytes is about 39 MB; the four 1024 px pages hold 16 MiB.
        atlas.BeginFrame(1);
        var regions = new ArtRegion[Count];
        for (var i = 0; i < Count; i++)
        {
            Assert.Equal(AtlasStatus.Ok, atlas.TryGet("p" + i, 256, 320, out regions[i]));
        }

        Assert.True(atlas.Overflowed > 0);
        Assert.Equal(0, atlas.Refusals);
        Assert.Equal(4, atlas.Pages.PageCount);
        for (var i = 0; i < Count; i++)
        {
            Assert.Equal(((byte)(i * 2 % 256), (byte)(i * 7 % 256), (byte)(255 - i)), CentrePixel(atlas, regions[i]));
        }

        Assert.True(atlas.StandaloneBytes <= atlas.StandaloneLimit);
    }

    [Fact]
    public void ManyFramesOfSlidingWindowsKeepEveryDrawnImageResident()
    {
        const int Count = 120;
        var atlas = new ArtAtlas();
        for (var i = 0; i < Count; i++)
        {
            atlas.AddSvg("p" + i, Portrait(256, 320, i));
        }

        for (var frame = 1; frame <= 60; frame++)
        {
            atlas.BeginFrame(frame);
            for (var k = 0; k < 30; k++)
            {
                var i = ((frame * 7) + (k * 3)) % Count;
                Assert.Equal(AtlasStatus.Ok, atlas.TryGet("p" + i, 256, 320, out var r));
                Assert.Equal(((byte)(i * 2 % 256), (byte)(i * 7 % 256), (byte)(255 - i)), CentrePixel(atlas, r));
            }
        }

        Assert.Equal(0, atlas.Refusals);
    }

    [Fact]
    public void AnImageThatTrulyCannotFitIsRefusedCountedAndWarnedAbout()
    {
        // Pages and textures are both tiny, so the second portrait has nowhere to go within one frame.
        var atlas = new ArtAtlas(new AtlasOptions { PageSize = 256, MaxPages = 1 }, new ArtLimits { MaxStandaloneBytes = 200 * 200 * 4 + 16 });
        var warnings = new List<string>();
        atlas.Warning = warnings.Add;
        atlas.AddSvg("a", Portrait(200, 200, 1));
        atlas.AddSvg("b", Portrait(200, 200, 2));
        atlas.AddSvg("c", Portrait(200, 200, 3));
        atlas.BeginFrame(1);
        Assert.Equal(AtlasStatus.Ok, atlas.TryGet("a", 200, 200, out _)); // the page
        Assert.Equal(AtlasStatus.Ok, atlas.TryGet("b", 200, 200, out _)); // its own texture, using the whole budget
        Assert.Equal(AtlasStatus.Full, atlas.TryGet("c", 200, 200, out _));
        Assert.Equal(1, atlas.Refusals);
        Assert.Contains("'c'", atlas.LastRefusal);
        Assert.Single(warnings);

        // The same lookup again is counted but warned about once.
        Assert.Equal(AtlasStatus.Full, atlas.TryGet("c", 200, 200, out _));
        Assert.Equal(2, atlas.Refusals);
        Assert.Single(warnings);

        // Next frame the page can be cleared, so it fits again.
        atlas.BeginFrame(2);
        Assert.Equal(AtlasStatus.Ok, atlas.TryGet("c", 200, 200, out _));
    }
}
