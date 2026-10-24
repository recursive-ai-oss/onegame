namespace OneGame.Atlas;

/// <summary>How an atlas is sized.</summary>
public sealed class AtlasOptions
{
    /// <summary>The smallest page edge accepted.</summary>
    public const int MinPageSize = 64;

    /// <summary>The largest page edge accepted.</summary>
    public const int MaxPageSize = 4096;

    /// <summary>Gets the page edge in pixels (pages are square). Default 1024.</summary>
    public int PageSize { get; init; } = 1024;

    /// <summary>Gets how many pages may exist. When all are full the least recently used page is cleared and reused. Default 4.</summary>
    public int MaxPages { get; init; } = 4;

    /// <summary>Gets the transparent border kept around every entry so bilinear sampling never reads a neighbour. Default 1.</summary>
    public int Padding { get; init; } = 1;

    internal void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(PageSize, MinPageSize);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(PageSize, MaxPageSize);
        ArgumentOutOfRangeException.ThrowIfLessThan(MaxPages, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(MaxPages, 64);
        ArgumentOutOfRangeException.ThrowIfLessThan(Padding, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(Padding, 8);
    }
}

/// <summary>A rectangle reserved on a page.</summary>
/// <param name="Page">Page index.</param>
/// <param name="X">Left edge of the usable rectangle, inside the padding.</param>
/// <param name="Y">Top edge of the usable rectangle.</param>
public readonly record struct AtlasSlot(int Page, int X, int Y);

/// <summary>
/// A growing set of equal-sized pages with a shelf packer. Entries are never moved. When every page is full, the page that was used
/// least recently is cleared as a whole (every entry on it is gone, <see cref="Epoch"/> grows, and the owner drops them); pages in use
/// during the current frame are never cleared, so a request that cannot fit is refused instead.
/// </summary>
public sealed class AtlasPages
{
    private sealed class Shelf(int y, int height)
    {
        public readonly int Y = y;
        public readonly int Height = height;
        public int Cursor;
    }

    private sealed class PageState(AtlasPage page)
    {
        public readonly AtlasPage Page = page;
        public readonly List<Shelf> Shelves = [];
        public int NextY;
        public long LastUsedFrame = -1;
    }

    private readonly AtlasOptions _options;
    private readonly List<PageState> _pages = [];

    /// <summary>Creates an empty atlas; pages are allocated when the first entry needs them.</summary>
    /// <param name="format">Pixel layout of every page.</param>
    /// <param name="options">Sizes; the defaults when <see langword="null"/>.</param>
    public AtlasPages(AtlasFormat format, AtlasOptions? options = null)
    {
        _options = options ?? new AtlasOptions();
        _options.Validate();
        Format = format;
    }

    /// <summary>Gets the pixel layout of every page.</summary>
    public AtlasFormat Format { get; }

    /// <summary>Gets the page edge in pixels.</summary>
    public int PageSize => _options.PageSize;

    /// <summary>Gets the largest entry edge that can ever fit: the page minus both paddings.</summary>
    public int MaxEntrySize => _options.PageSize - (2 * _options.Padding);

    /// <summary>Gets a counter that grows whenever a page is cleared. Quads built from earlier lookups must be rebuilt when it changes.</summary>
    public int Epoch { get; private set; }

    /// <summary>Gets the pages created so far, in index order.</summary>
    public int PageCount => _pages.Count;

    /// <summary>Gets the current frame number, as set by <see cref="BeginFrame"/>.</summary>
    public long Frame { get; private set; }

    /// <summary>Returns a page.</summary>
    /// <param name="index">Page index below <see cref="PageCount"/>.</param>
    /// <returns>The page.</returns>
    public AtlasPage this[int index] => _pages[index].Page;

    /// <summary>Starts a frame. Pages touched in this frame are protected from clearing until the next call.</summary>
    /// <param name="frame">A number that grows with every frame.</param>
    public void BeginFrame(long frame) => Frame = frame;

    /// <summary>Marks a page as used in the current frame; call it for every entry looked up.</summary>
    /// <param name="page">Page index.</param>
    public void Touch(int page) => _pages[page].LastUsedFrame = Frame;

    /// <summary>Reserves a rectangle.</summary>
    /// <param name="width">Usable width, at most <see cref="MaxEntrySize"/>.</param>
    /// <param name="height">Usable height, at most <see cref="MaxEntrySize"/>.</param>
    /// <param name="slot">Where it went.</param>
    /// <param name="clearedPage">The page that had to be cleared to make room, or -1. The owner removes every entry on that page.</param>
    /// <returns>False when the request is too large or nothing can be cleared now.</returns>
    public bool TryAllocate(int width, int height, out AtlasSlot slot, out int clearedPage)
    {
        clearedPage = -1;
        slot = default;
        if (width <= 0 || height <= 0 || width > MaxEntrySize || height > MaxEntrySize)
        {
            return false;
        }

        var pad = _options.Padding;
        var w = width + (2 * pad);
        var h = height + (2 * pad);
        foreach (var p in _pages)
        {
            if (TryPlace(p, w, h, pad, out slot))
            {
                p.LastUsedFrame = Frame;
                return true;
            }
        }

        if (_pages.Count < _options.MaxPages)
        {
            var np = new PageState(new AtlasPage(_pages.Count, _options.PageSize, Format));
            _pages.Add(np);
            np.LastUsedFrame = Frame;
            return TryPlace(np, w, h, pad, out slot);
        }

        PageState? victim = null;
        foreach (var p in _pages)
        {
            if (p.LastUsedFrame < Frame && (victim is null || p.LastUsedFrame < victim.LastUsedFrame))
            {
                victim = p;
            }
        }

        if (victim is null)
        {
            return false;
        }

        victim.Shelves.Clear();
        victim.NextY = 0;
        victim.Page.Clear();
        victim.LastUsedFrame = Frame;
        Epoch++;
        clearedPage = victim.Page.Index;
        return TryPlace(victim, w, h, pad, out slot);
    }

    /// <summary>Copies pixels into a reserved slot and marks the region dirty.</summary>
    /// <param name="slot">A slot from <see cref="TryAllocate"/>.</param>
    /// <param name="width">Width of the pixels to copy.</param>
    /// <param name="height">Height of the pixels to copy.</param>
    /// <param name="source">Source rows.</param>
    /// <param name="sourceStride">Bytes per source row.</param>
    public void Upload(AtlasSlot slot, int width, int height, ReadOnlySpan<byte> source, int sourceStride) =>
        _pages[slot.Page].Page.Blit(slot.X, slot.Y, width, height, source, sourceStride);

    private static bool TryPlace(PageState p, int w, int h, int pad, out AtlasSlot slot)
    {
        var size = p.Page.Width;
        Shelf? best = null;
        foreach (var s in p.Shelves)
        {
            if (s.Height >= h && s.Height <= h + (h / 2) + 2 && s.Cursor + w <= size && (best is null || s.Height < best.Height))
            {
                best = s;
            }
        }

        if (best is null && p.NextY + h <= size)
        {
            // A little headroom lets glyphs of nearly equal height share a shelf.
            var sh = Math.Min(size - p.NextY, h + (h / 8));
            best = new Shelf(p.NextY, sh);
            p.Shelves.Add(best);
            p.NextY += sh;
        }

        if (best is null)
        {
            slot = default;
            return false;
        }

        slot = new AtlasSlot(p.Page.Index, best.Cursor + pad, best.Y + pad);
        best.Cursor += w;
        return true;
    }
}
