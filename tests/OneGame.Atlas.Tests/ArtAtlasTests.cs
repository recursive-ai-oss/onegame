using OneImage;

namespace OneGame.Atlas.Tests;

public class ArtAtlasTests
{
    private static readonly NineSlice FrameSlice = new(8, 8, 8, 8);

    private static ArtAtlas Loaded(AtlasOptions? options = null, ArtLimits? limits = null)
    {
        var atlas = new ArtAtlas(options, limits);
        atlas.AddSvg("coin", Fixtures.Svg("coin.svg"));
        atlas.AddSvg("arrow", Fixtures.Svg("arrow.svg"));
        atlas.AddSvg("frame", Fixtures.Svg("frame.svg"), FrameSlice);
        return atlas;
    }

    /// <summary>Lays the three icons side by side on white, blending the premultiplied texels the way a renderer's blit shader does.</summary>
    private static (int W, int H, Rgba32[] Pixels) Sheet(ArtAtlas atlas, float scale)
    {
        var w = (int)(100 * scale);
        var h = (int)(48 * scale);
        var px = new Rgba32[w * h];
        Array.Fill(px, new Rgba32(255, 255, 255, 255));
        var x = (int)(4 * scale);
        foreach (var id in new[] { "coin", "arrow", "frame" })
        {
            Assert.Equal(AtlasStatus.Ok, atlas.TryGetScaled(id, scale, out var r));
            var page = atlas.Pages[r.Page];
            for (var yy = 0; yy < r.Height; yy++)
            {
                for (var xx = 0; xx < r.Width; xx++)
                {
                    var o = ((r.Y + yy) * page.Stride) + ((r.X + xx) * 4);
                    var s = page.Pixels.Slice(o, 4);
                    ref var d = ref px[((4 + yy) * w) + x + xx];
                    var inv = 255 - s[3];
                    d = new Rgba32((byte)(s[0] + ((d.R * inv + 127) / 255)), (byte)(s[1] + ((d.G * inv + 127) / 255)), (byte)(s[2] + ((d.B * inv + 127) / 255)), 255);
                }
            }

            x += r.Width + (int)(6 * scale);
        }

        return (w, h, px);
    }

    private static void CheckGolden(string name, int w, int h, Rgba32[] actual)
    {
        var path = Path.Combine(Fixtures.Dir(), "ref", name);
        if (Environment.GetEnvironmentVariable("ONEGAME_UPDATE_GOLDEN") == "1")
        {
            Image.FromPixels<Rgba32>(actual, w, h).Save(path);
        }

        var exp = Image.Load(path).ConvertTo(PixelFormat.Rgba8).ToArray<Rgba32>();
        Assert.Equal(exp.Length, actual.Length);
        var worst = 0;
        long total = 0;
        for (var i = 0; i < exp.Length; i++)
        {
            var d = Math.Max(Math.Max(Math.Abs(exp[i].R - actual[i].R), Math.Abs(exp[i].G - actual[i].G)), Math.Abs(exp[i].B - actual[i].B));
            worst = Math.Max(worst, d);
            total += d;
        }

        Assert.True(worst <= 3 && (double)total / exp.Length < 0.05, $"{name}: max channel difference {worst}, mean {(double)total / exp.Length:F4}");
        if (Environment.GetEnvironmentVariable("ONEGAME_SAVE_FRAMES") is { Length: > 0 } dir)
        {
            Image.FromPixels<Rgba32>(actual, w, h).Save(Path.Combine(dir, name));
        }
    }

    [Theory]
    [InlineData(1f, "svg-1x.png")]
    [InlineData(2f, "svg-2x.png")]
    public void SvgIconsMatchTheCommittedReferenceAtTwoScales(float scale, string reference)
    {
        var atlas = Loaded();
        var (w, h, px) = Sheet(atlas, scale);
        CheckGolden(reference, w, h, px);
    }

    [Fact]
    public void ScaleChangeRasterizesAgainAtTheNewSizeAndKeepsBothCached()
    {
        var atlas = Loaded();
        Assert.Equal(AtlasStatus.Ok, atlas.TryGetScaled("coin", 1, out var one));
        Assert.Equal(AtlasStatus.Ok, atlas.TryGetScaled("coin", 2, out var two));
        Assert.Equal((24, 24), (one.Width, one.Height));
        Assert.Equal((48, 48), (two.Width, two.Height));
        Assert.NotEqual((one.X, one.Y), (two.X, two.Y));
        Assert.Equal(AtlasStatus.Ok, atlas.TryGetScaled("coin", 1, out var again));
        Assert.Equal(one, again);

        // The 2x raster is drawn at 48 px, not a stretched 24 px one: the star's sharp tip reaches the top edge row at 2x with full colour.
        var page = atlas.Pages[two.Page];
        var versionBefore = page.Version;
        Assert.Equal(AtlasStatus.Ok, atlas.TryGetScaled("coin", 2, out _));
        Assert.Equal(versionBefore, page.Version); // a cache hit writes nothing
        var centre = page.Pixels.Slice(((two.Y + 24) * page.Stride) + ((two.X + 24) * 4), 4);
        Assert.True(centre[3] == 255 && centre[0] > 200 && centre[2] > 150, "the middle of the coin is the cream star");
        var rim = page.Pixels.Slice(((two.Y + 24) * page.Stride) + ((two.X + 3) * 4), 4);
        Assert.True(rim[3] == 255 && rim[0] > 90 && rim[0] < 150, "the left rim is the dark stroke");
    }

    [Fact]
    public void PagesArePremultipliedAndSizesFollowTheNaturalSize()
    {
        var atlas = Loaded();
        Assert.True(atlas.TryGetNaturalSize("frame", out var w, out var h));
        Assert.Equal((32, 32), (w, h));
        Assert.Equal(AtlasStatus.Ok, atlas.TryGetScaled("arrow", 1.5f, out var r));
        Assert.Equal((24, 24), (r.Width, r.Height));
        var page = atlas.Pages[r.Page];
        Assert.Equal(AtlasFormat.Rgba8Premultiplied, page.Format);
        var translucent = 0;
        for (var y = 0; y < r.Height; y++)
        {
            for (var x = 0; x < r.Width; x++)
            {
                var p = page.Pixels.Slice(((r.Y + y) * page.Stride) + ((r.X + x) * 4), 4);
                Assert.True(p[0] <= p[3] && p[1] <= p[3] && p[2] <= p[3], "colour channels must not exceed alpha");
                translucent += p[3] is > 0 and < 255 ? 1 : 0;
            }
        }

        Assert.True(translucent > 20, "the arrow's edges are anti-aliased");
        Assert.Equal(AtlasStatus.UnknownResource, atlas.TryGetScaled("nope", 1, out _));
    }

    [Fact]
    public void NineSliceMetadataRoundTripsAndScalesWithTheRaster()
    {
        var atlas = Loaded();
        Assert.Equal(AtlasStatus.Ok, atlas.TryGetScaled("frame", 1, out var one));
        Assert.Equal(FrameSlice, one.Slice);
        Assert.Equal(AtlasStatus.Ok, atlas.TryGetScaled("frame", 2, out var two));
        Assert.Equal(new NineSlice(16, 16, 16, 16), two.Slice);
        Assert.Equal(AtlasStatus.Ok, atlas.TryGetScaled("coin", 1, out var plain));
        Assert.True(plain.Slice.IsZero);

        Assert.True(NineSlice.TryParse(FrameSlice.ToString(), out var parsed));
        Assert.Equal(FrameSlice, parsed);
        Assert.True(NineSlice.TryParse("2.5 4", out parsed));
        Assert.Equal(new NineSlice(2.5f, 4, 2.5f, 4), parsed);
        Assert.True(NineSlice.TryParse("7", out parsed));
        Assert.Equal(new NineSlice(7, 7, 7, 7), parsed);
        Assert.False(NineSlice.TryParse("1 2 3", out _));
        Assert.False(NineSlice.TryParse("-1 0 0 0", out _));
        Assert.False(NineSlice.TryParse("NaN 0 0 0", out _));
    }

    [Fact]
    public void RasterImagesKeepTheirOwnSizeAndPremultiply()
    {
        var px = new Rgba32[20 * 10];
        for (var i = 0; i < px.Length; i++)
        {
            px[i] = new Rgba32(200, 100, 50, 128);
        }

        var file = Path.Combine(Path.GetTempPath(), "art-" + Guid.NewGuid().ToString("N") + ".png");
        try
        {
            Image.FromPixels<Rgba32>(px, 20, 10).Save(file, cancellationToken: TestContext.Current.CancellationToken);
            var atlas = new ArtAtlas();
            atlas.AddImage("panel", File.ReadAllBytes(file), new NineSlice(3, 3, 3, 3));
            Assert.Equal(AtlasStatus.Ok, atlas.TryGetScaled("panel", 3, out var r)); // scale is ignored for rasters
            Assert.Equal((20, 10), (r.Width, r.Height));
            Assert.Equal(new NineSlice(3, 3, 3, 3), r.Slice);
            var p = atlas.Pages[r.Page].Pixels.Slice((r.Y * atlas.Pages[r.Page].Stride) + (r.X * 4), 4);
            Assert.Equal(128, p[3]);
            Assert.InRange(p[0], 99, 101);
            Assert.InRange(p[1], 49, 51);
            Assert.InRange(p[2], 24, 26);
        }
        finally
        {
            File.Delete(file);
        }
    }

    private static byte[] PngHeaderClaiming(int width, int height)
    {
        using var ms = new MemoryStream();
        ms.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        Chunk(ms, "IHDR", [.. Be(width), .. Be(height), 8, 6, 0, 0, 0]);
        Chunk(ms, "IDAT", [0x78, 0x9C, 0x03, 0x00, 0x00, 0x00, 0x00, 0x01]);
        Chunk(ms, "IEND", []);
        return ms.ToArray();
    }

    private static byte[] Be(int v) => [(byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8), (byte)v];

    private static void Chunk(MemoryStream ms, string type, byte[] data)
    {
        ms.Write(Be(data.Length));
        var body = System.Text.Encoding.ASCII.GetBytes(type).Concat(data).ToArray();
        ms.Write(body);
        uint crc = 0xFFFFFFFF;
        foreach (var b in body)
        {
            crc ^= b;
            for (var k = 0; k < 8; k++)
            {
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
            }
        }

        ms.Write(Be((int)~crc));
    }

    [Fact]
    public void HostileAssetsAreRefusedWithoutBigAllocations()
    {
        var atlas = Loaded();
        var huge = new byte[5 * 1024 * 1024];
        var hugeNatural = System.Text.Encoding.UTF8.GetBytes("""<svg xmlns="http://www.w3.org/2000/svg" width="100000" height="100000"><rect width="5" height="5"/></svg>""");
        var bigPng = PngHeaderClaiming(60000, 60000);
        var moderatePng = PngHeaderClaiming(5000, 5000);
        var garbage = new byte[100];

        var before = GC.GetAllocatedBytesForCurrentThread();
        Assert.Throws<AtlasAssetException>(() => atlas.AddSvg("big-file", huge));
        Assert.Throws<AtlasAssetException>(() => atlas.AddSvg("big-natural", hugeNatural));
        Assert.Throws<AtlasAssetException>(() => atlas.AddImage("big-png", bigPng));
        Assert.Throws<AtlasAssetException>(() => atlas.AddImage("moderate-png", moderatePng)); // 25 megapixels is over the raster budget
        Assert.Throws<AtlasAssetException>(() => atlas.AddImage("garbage", garbage));
        Assert.Throws<AtlasAssetException>(() => atlas.AddSvg("empty", ReadOnlyMemory<byte>.Empty));
        var refused = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.True(refused < 2 * 1024 * 1024, $"refusing hostile assets allocated {refused} bytes");
        Assert.False(atlas.TryGetNaturalSize("big-natural", out _, out _));

        // A request beyond the raster budget never reaches the SVG renderer.
        before = GC.GetAllocatedBytesForCurrentThread();
        Assert.Equal(AtlasStatus.TooLarge, atlas.TryGet("frame", 4097, 4097, out _));      // over 16 megapixels
        Assert.Equal(AtlasStatus.TooLarge, atlas.TryGet("frame", 8193, 10, out _));       // over the side limit
        Assert.Equal(AtlasStatus.TooLarge, atlas.TryGet("frame", 5000, 5000, out _));
        Assert.Equal(AtlasStatus.TooLarge, atlas.TryGet("frame", int.MaxValue, int.MaxValue, out _));
        Assert.Equal(AtlasStatus.TooLarge, atlas.TryGetScaled("frame", float.NaN, out _));
        Assert.Equal(AtlasStatus.TooLarge, atlas.TryGet("frame", 0, 10, out _));
        refused = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.True(refused < 64 * 1024, $"refusing oversized requests allocated {refused} bytes");

        var small = new ArtAtlas(new AtlasOptions { PageSize = 128 });
        small.AddSvg("coin", Fixtures.Svg("coin.svg"));
        Assert.Equal(AtlasStatus.Ok, small.TryGetScaled("coin", 6, out var big)); // 144 px does not fit a 128 px page, so it gets a texture of its own
        Assert.NotEqual(0, big.Standalone);

        var few = new ArtAtlas(limits: new ArtLimits { MaxResources = 2 });
        few.AddSvg("a", Fixtures.Svg("coin.svg"));
        few.AddSvg("b", Fixtures.Svg("coin.svg"));
        Assert.Throws<AtlasAssetException>(() => few.AddSvg("c", Fixtures.Svg("coin.svg")));
    }

    [Fact]
    public void ReplacingAResourceDropsItsOldRasters()
    {
        var atlas = Loaded();
        Assert.Equal(AtlasStatus.Ok, atlas.TryGetScaled("coin", 1, out var before));
        atlas.AddSvg("coin", Fixtures.Svg("arrow.svg"));
        Assert.Equal(AtlasStatus.Ok, atlas.TryGetScaled("coin", 1, out var after));
        Assert.Equal((16, 16), (after.Width, after.Height));
        Assert.NotEqual(before, after);
    }

    [Fact]
    public void PackerKeepsEntriesDisjointInsideThePageAndClearsOnlyIdlePages()
    {
        var pages = new AtlasPages(AtlasFormat.R8, new AtlasOptions { PageSize = 256, MaxPages = 2 });
        var rng = new Random(7);
        var slots = new List<(AtlasSlot Slot, int W, int H)>();
        pages.BeginFrame(1);
        while (true)
        {
            var w = rng.Next(4, 40);
            var h = rng.Next(4, 40);
            if (!pages.TryAllocate(w, h, out var s, out var cleared))
            {
                break;
            }

            Assert.Equal(-1, cleared);
            slots.Add((s, w, h));
        }

        Assert.Equal(2, pages.PageCount);
        Assert.True(slots.Count > 60);
        for (var i = 0; i < slots.Count; i++)
        {
            var a = slots[i];
            Assert.True(a.Slot.X >= 1 && a.Slot.Y >= 1 && a.Slot.X + a.W + 1 <= 256 && a.Slot.Y + a.H + 1 <= 256);
            for (var j = i + 1; j < slots.Count; j++)
            {
                var b = slots[j];
                if (a.Slot.Page != b.Slot.Page)
                {
                    continue;
                }

                // Padded footprints (one pixel on each side) never overlap.
                var apart = a.Slot.X + a.W + 1 <= b.Slot.X - 1 || b.Slot.X + b.W + 1 <= a.Slot.X - 1
                    || a.Slot.Y + a.H + 1 <= b.Slot.Y - 1 || b.Slot.Y + b.H + 1 <= a.Slot.Y - 1;
                Assert.True(apart, $"entries {i} and {j} overlap");
            }
        }

        // Same frame: all pages are in use, so the request is refused. Next frame: one idle page is cleared.
        Assert.False(pages.TryAllocate(30, 30, out _, out _));
        pages.BeginFrame(2);
        pages.Touch(1);
        Assert.True(pages.TryAllocate(30, 30, out var fresh, out var clearedPage));
        Assert.Equal(0, clearedPage);
        Assert.Equal(0, fresh.Page);
        Assert.False(pages.TryAllocate(300, 10, out _, out _));
    }
}
