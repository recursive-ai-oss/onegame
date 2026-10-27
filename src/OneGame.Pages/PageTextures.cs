using OneGame.Atlas;
using OneGame.Gpu;
using OneGame.Text;

namespace OneGame.Pages;

/// <summary>
/// The textures behind the game's glyph and art atlases, in the formats a page layer wants: art pages as <c>Rgba8Unorm</c> (the atlas stores
/// premultiplied sRGB-encoded bytes, which is the space pages blend in), glyph pages as <c>R8Unorm</c> coverage. They are copies of the pages the
/// UI renderer keeps, uploaded when a page changes.
/// </summary>
internal sealed class PageTextures : IDisposable
{
    private sealed class PageTexture
    {
        public required GpuTexture Texture;
        public int Version = -1;
    }

    private readonly PageGpu _gpu;
    private readonly List<PageTexture?> _art = [];
    private readonly List<PageTexture?> _glyph = [];
    private readonly Dictionary<int, PageTexture> _standalone = [];
    private readonly List<int> _released = [];

    public PageTextures(PageGpu gpu, GlyphAtlas glyphs, ArtAtlas art)
    {
        _gpu = gpu;
        Glyphs = glyphs;
        Art = art;
    }

    public GlyphAtlas Glyphs { get; }

    public ArtAtlas Art { get; }

    /// <summary>Uploads what changed in the atlases since the last frame.</summary>
    public void Sync()
    {
        SyncPages(Glyphs.Pages, _glyph, GpuFormat.R8Unorm);
        SyncPages(Art.Pages, _art, GpuFormat.Rgba8Unorm);
        Art.TakeReleased(_released);
        foreach (var handle in _released)
        {
            if (_standalone.Remove(handle, out var tex))
            {
                // A frame in flight may still sample it; the next frames' bind groups no longer name it.
                _gpu.Device.WaitIdle();
                tex.Texture.Dispose();
            }
        }

        _released.Clear();
    }

    public GpuBindGroup GlyphGroup(int page) => _gpu.Sampled(Page(Glyphs.Pages, _glyph, page, GpuFormat.R8Unorm).Texture);

    /// <summary>The bind group of the texture an art region lives in (its shared page, or the texture of its own that a large raster gets).</summary>
    public GpuBindGroup ArtGroup(in ArtRegion region, bool nearest, out int width, out int height)
    {
        if (region.Standalone == 0)
        {
            var info = Art.Pages[region.Page];
            width = info.Width;
            height = info.Height;
            return _gpu.Sampled(Page(Art.Pages, _art, region.Page, GpuFormat.Rgba8Unorm).Texture, null, nearest);
        }

        if (!Art.TryGetStandalone(region.Standalone, out var data))
        {
            throw new InvalidOperationException("A standalone art texture was released while the frame still draws it.");
        }

        if (!_standalone.TryGetValue(region.Standalone, out var tex))
        {
            var gpu = _gpu.Device.CreateTexture(new TextureDesc((uint)data.Width, (uint)data.Height, GpuFormat.Rgba8Unorm, TextureUsage.Sampled | TextureUsage.CopyDestination) { Label = "page art" });
            tex = new PageTexture { Texture = gpu, Version = 0 };
            _standalone[region.Standalone] = tex;
        }

        if (data.NeedsUpload)
        {
            _gpu.Device.WriteTexture(tex.Texture, 0, 0, data.Pixels);
            data.MarkUploaded();
        }

        width = data.Width;
        height = data.Height;
        return _gpu.Sampled(tex.Texture, null, nearest);
    }

    public int ArtPageCount => _art.Count(p => p is not null);

    public int GlyphPageCount => _glyph.Count(p => p is not null);

    public int StandaloneCount => _standalone.Count;

    public void Dispose()
    {
        _gpu.Device.WaitIdle();
        foreach (var t in _standalone.Values)
        {
            t.Texture.Dispose();
        }

        foreach (var list in new[] { _art, _glyph })
        {
            foreach (var p in list)
            {
                p?.Texture.Dispose();
            }

            list.Clear();
        }

        _standalone.Clear();
    }

    private PageTexture Page(AtlasPages pages, List<PageTexture?> list, int index, GpuFormat format)
    {
        while (list.Count <= index)
        {
            list.Add(null);
        }

        var page = list[index];
        if (page is null)
        {
            var info = pages[index];
            var gpu = _gpu.Device.CreateTexture(new TextureDesc((uint)info.Width, (uint)info.Height, format, TextureUsage.Sampled | TextureUsage.CopyDestination) { Label = "page atlas" });
            page = new PageTexture { Texture = gpu };
            list[index] = page;
        }

        return page;
    }

    private void SyncPages(AtlasPages pages, List<PageTexture?> list, GpuFormat format)
    {
        for (var i = 0; i < pages.PageCount; i++)
        {
            var p = pages[i];
            var tex = Page(pages, list, i, format);
            // The version, not the page's dirty rectangle: the UI renderer consumes that for its own copy.
            if (tex.Version == p.Version)
            {
                continue;
            }

            _gpu.Device.WriteTexture(tex.Texture, 0, 0, p.Pixels);
            tex.Version = p.Version;
        }
    }
}
