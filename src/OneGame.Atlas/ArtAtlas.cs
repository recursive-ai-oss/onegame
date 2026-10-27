using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using OneImage;
using OneImage.Svg;

namespace OneGame.Atlas;

/// <summary>Insets, in pixels, that cut a frame into nine parts: the corners keep their size, the edges stretch along one axis, the middle in both.</summary>
/// <param name="Left">Width of the left column.</param>
/// <param name="Top">Height of the top row.</param>
/// <param name="Right">Width of the right column.</param>
/// <param name="Bottom">Height of the bottom row.</param>
public readonly record struct NineSlice(float Left, float Top, float Right, float Bottom)
{
    /// <summary>Gets whether all four insets are zero (the image is not sliced).</summary>
    public bool IsZero => Left == 0 && Top == 0 && Right == 0 && Bottom == 0;

    /// <summary>Returns the insets scaled per axis.</summary>
    /// <param name="sx">Horizontal factor.</param>
    /// <param name="sy">Vertical factor.</param>
    /// <returns>The scaled insets.</returns>
    public NineSlice Scaled(float sx, float sy) => new(Left * sx, Top * sy, Right * sx, Bottom * sy);

    /// <summary>Writes the insets as <c>left top right bottom</c>, the form <see cref="TryParse"/> reads.</summary>
    /// <returns>The text form.</returns>
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Left} {Top} {Right} {Bottom}");

    /// <summary>Reads <c>left top right bottom</c> (or one number for all four, or two for horizontal and vertical). Negative or non-finite numbers are refused.</summary>
    /// <param name="text">The text.</param>
    /// <param name="slice">The insets.</param>
    /// <returns>False when the text is not valid.</returns>
    public static bool TryParse(string text, out NineSlice slice)
    {
        slice = default;
        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length is not (1 or 2 or 4))
        {
            return false;
        }

        Span<float> v = stackalloc float[4];
        for (var i = 0; i < parts.Length; i++)
        {
            if (!float.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out v[i]) || !float.IsFinite(v[i]) || v[i] < 0)
            {
                return false;
            }
        }

        slice = parts.Length switch
        {
            1 => new NineSlice(v[0], v[0], v[0], v[0]),
            2 => new NineSlice(v[0], v[1], v[0], v[1]),
            _ => new NineSlice(v[0], v[1], v[2], v[3]),
        };
        return true;
    }
}

/// <summary>The outcome of a lookup that may rasterize.</summary>
public enum AtlasStatus
{
    /// <summary>The entry is in the atlas.</summary>
    Ok,

    /// <summary>No resource with that id was registered.</summary>
    UnknownResource,

    /// <summary>The request exceeds the size budget; nothing was decoded or allocated for it.</summary>
    TooLarge,

    /// <summary>It fits a page, but every page is in use this frame.</summary>
    Full,

    /// <summary>The asset could not be rasterized (it was damaged, or a limit inside the decoder stopped it). The failure is remembered.</summary>
    Invalid,
}

/// <summary>Where one rasterized SVG or image sits in the atlas.</summary>
/// <param name="Page">Page index.</param>
/// <param name="X">Left edge in page pixels.</param>
/// <param name="Y">Top edge in page pixels.</param>
/// <param name="Width">Width in pixels.</param>
/// <param name="Height">Height in pixels.</param>
/// <param name="U0">Left texture coordinate (X / page width).</param>
/// <param name="V0">Top texture coordinate (Y / page height).</param>
/// <param name="U1">Right texture coordinate.</param>
/// <param name="V1">Bottom texture coordinate.</param>
/// <param name="Slice">Nine-slice insets in pixels of this raster (the registered insets scaled to this size); zero when not sliced.</param>
/// <param name="Standalone">Zero when the raster sits on page <paramref name="Page"/>. Otherwise the handle of a standalone texture (see <see cref="ArtAtlas.TryGetStandalone"/>): <paramref name="Page"/> is -1, the rectangle is the whole texture and the texture coordinates run 0 to 1.</param>
public readonly record struct ArtRegion(int Page, int X, int Y, int Width, int Height, float U0, float V0, float U1, float V1, NineSlice Slice, int Standalone = 0);

/// <summary>
/// A raster too big for the shared pages: it has a texture of its own, at full resolution. The renderer uploads <see cref="Pixels"/> once,
/// calls <see cref="MarkUploaded"/>, and drops the texture when the atlas reports the handle as released.
/// </summary>
public sealed class StandaloneArt
{
    internal StandaloneArt(int handle, int width, int height, byte[] pixels)
    {
        Handle = handle;
        Width = width;
        Height = height;
        _pixels = pixels;
    }

    private byte[]? _pixels;

    /// <summary>Gets the handle carried by <see cref="ArtRegion.Standalone"/>.</summary>
    public int Handle { get; }

    /// <summary>Gets the width in pixels.</summary>
    public int Width { get; }

    /// <summary>Gets the height in pixels.</summary>
    public int Height { get; }

    /// <summary>Gets the premultiplied RGBA rows, tightly packed, until <see cref="MarkUploaded"/>; empty afterwards.</summary>
    public ReadOnlySpan<byte> Pixels => _pixels;

    /// <summary>Gets whether the pixels still wait for the upload.</summary>
    public bool NeedsUpload => _pixels is not null;

    /// <summary>Gets the bytes this texture takes on the GPU.</summary>
    public long Bytes => (long)Width * Height * 4;

    internal long LastUsedFrame { get; set; }

    /// <summary>Drops the CPU copy once it is on the GPU, so each large raster is held in memory once.</summary>
    public void MarkUploaded() => _pixels = null;
}

/// <summary>Progress of one <see cref="ArtAtlas.Preload(IEnumerable{string}, float)"/> call. Safe to read from any thread.</summary>
public sealed class ArtPreload
{
    private int _total;
    private int _finished;
    private int _failed;
    private int _sealed;

    /// <summary>Gets how many rasters were asked for.</summary>
    public int Total => Volatile.Read(ref _total);

    /// <summary>Gets how many are finished: in the atlas, already there, or given up on.</summary>
    public int Finished => Volatile.Read(ref _finished);

    /// <summary>Gets how many finished without art in the atlas (unknown id, too large, damaged, or no room). A draw of those behaves as without preload.</summary>
    public int Failed => Volatile.Read(ref _failed);

    /// <summary>Gets whether every requested raster is finished.</summary>
    public bool IsComplete => Volatile.Read(ref _sealed) == 1 && Finished >= Total;

    internal void Add(int n) => Interlocked.Add(ref _total, n);

    internal void Seal() => Volatile.Write(ref _sealed, 1);

    internal void Finish(bool failed)
    {
        if (failed)
        {
            Interlocked.Increment(ref _failed);
        }

        Interlocked.Increment(ref _finished);
    }
}

/// <summary>A registered asset was refused: it is damaged or breaks a size budget.</summary>
public class AtlasAssetException : Exception
{
    /// <summary>Creates the exception.</summary>
    /// <param name="message">What was refused.</param>
    /// <param name="inner">The decoder's exception, if any.</param>
    public AtlasAssetException(string message, Exception? inner = null)
        : base(message, inner)
    {
    }
}

/// <summary>Budgets that bound what a hostile SVG or image can make the atlas do.</summary>
public sealed class ArtLimits
{
    /// <summary>Gets the largest SVG file in bytes. Default 4 MiB.</summary>
    public int MaxSvgBytes { get; init; } = 4 * 1024 * 1024;

    /// <summary>Gets the largest raster image file in bytes. Default 32 MiB.</summary>
    public int MaxImageBytes { get; init; } = 32 * 1024 * 1024;

    /// <summary>Gets how many resources may be registered. Default 4096.</summary>
    public int MaxResources { get; init; } = 4096;

    /// <summary>Gets the largest width or height of one raster in pixels, and so the largest natural size of a raster image. Default 8192.</summary>
    public int MaxRasterDimension { get; init; } = 8192;

    /// <summary>Gets the largest pixel count of one raster. Default 16,777,216 (4096 × 4096, 64 MiB of texels).</summary>
    public long MaxRasterPixels { get; init; } = 4096L * 4096;

    /// <summary>Gets the width or height above which a raster gets a texture of its own instead of a place on a shared page. Default 512; never more than a page can hold.</summary>
    public int StandaloneThreshold { get; init; } = 512;

    /// <summary>
    /// Gets the most texel memory in bytes that standalone textures may hold at once. When a new one would not fit, the least recently used ones that were not drawn
    /// this frame are released first; if the frame itself needs more, the lookup returns <see cref="AtlasStatus.Full"/>. Default 256 MiB.
    /// </summary>
    public long MaxStandaloneBytes { get; init; } = 256L * 1024 * 1024;

    /// <summary>Gets the natural size limit of a registered SVG per side. The natural size only seeds the scale; the raster limit applies when drawing. Default 16384.</summary>
    public int MaxSvgNaturalDimension { get; init; } = 16384;
}

/// <summary>
/// SVG and image atlas. Sources are registered once under an id (the id the UI's <c>Image</c> commands carry) and rasterized lazily at the
/// pixel size asked for: an SVG is rendered at exactly that size, so it is crisp at any UI scale and is re-rasterized when the scale
/// changes; a raster image is decoded at its own size. Pages are <see cref="AtlasFormat.Rgba8Premultiplied"/>.
/// </summary>
public sealed class ArtAtlas
{
    private sealed class Source(bool svg, byte[] data, int width, int height, NineSlice slice)
    {
        public readonly bool Svg = svg;
        public readonly byte[] Data = data;
        public readonly int Width = width;
        public readonly int Height = height;
        public readonly NineSlice Slice = slice;
    }

    private readonly record struct Key(string Id, int Width, int Height);

    private sealed record PreloadJob(Key Key, Source Source, ArtPreload Handle);

    private sealed record PreloadResult(PreloadJob Job, byte[]? Pixels, bool Invalid);

    private readonly record struct Entry(ArtRegion Region, bool Failed);

    private static readonly ImageConfiguration SvgConfig = new ImageConfiguration().Add(SvgFormat.Instance);

    private readonly ArtLimits _limits;
    private readonly AtlasPages _pages;
    private readonly Dictionary<string, Source> _sources = new(StringComparer.Ordinal);
    private readonly Dictionary<Key, Entry> _entries = [];
    private readonly List<Key> _scratchKeys = [];
    private readonly Dictionary<int, StandaloneArt> _standalones = [];
    private readonly List<int> _released = [];
    private int _nextHandle = 1;
    private long _syncRasterizations;
    private long _backgroundRasterizations;
    private readonly Lock _gate = new();
    private readonly Queue<PreloadJob> _queue = new();
    private readonly System.Collections.Concurrent.ConcurrentQueue<PreloadResult> _done = new();
    private int _workers;

    /// <summary>Creates an empty atlas.</summary>
    /// <param name="options">Page sizes; the defaults when <see langword="null"/>.</param>
    /// <param name="limits">Budgets; the defaults when <see langword="null"/>.</param>
    public ArtAtlas(AtlasOptions? options = null, ArtLimits? limits = null)
    {
        _limits = limits ?? new ArtLimits();
        _pages = new AtlasPages(AtlasFormat.Rgba8Premultiplied, options);
    }

    /// <summary>Gets the pages. The renderer uploads <see cref="AtlasPage.TakeDirty"/> of each and keeps one texture per page.</summary>
    public AtlasPages Pages => _pages;

    /// <summary>Gets how many standalone textures exist.</summary>
    public int StandaloneCount => _standalones.Count;

    /// <summary>Gets the GPU bytes the standalone textures take; never above <see cref="ArtLimits.MaxStandaloneBytes"/>.</summary>
    public long StandaloneBytes { get; private set; }

    /// <summary>Returns the standalone texture behind <see cref="ArtRegion.Standalone"/>.</summary>
    /// <param name="handle">The handle from a region.</param>
    /// <param name="art">The texture's pixels and size.</param>
    /// <returns>False when the handle is unknown or was released.</returns>
    public bool TryGetStandalone(int handle, out StandaloneArt art) => _standalones.TryGetValue(handle, out art!);

    /// <summary>Moves the handles released since the last call into <paramref name="handles"/> so the renderer can free their textures.</summary>
    /// <param name="handles">Receives the handles.</param>
    public void TakeReleased(List<int> handles)
    {
        ArgumentNullException.ThrowIfNull(handles);
        handles.AddRange(_released);
        _released.Clear();
    }

    /// <summary>Starts a frame; see <see cref="AtlasPages.BeginFrame"/>.</summary>
    /// <param name="frame">A number that grows with every frame.</param>
    public void BeginFrame(long frame)
    {
        _pages.BeginFrame(frame);
        PumpPreload();
    }

    /// <summary>Registers an SVG document. Nothing is rasterized yet; the size is read to seed scaling.</summary>
    /// <param name="id">The resource id used by display list <c>Image</c> commands.</param>
    /// <param name="data">The file.</param>
    /// <param name="slice">Nine-slice insets in the SVG's own pixels (its natural size), or zero.</param>
    /// <exception cref="AtlasAssetException">The file is too large, damaged, or its natural size is beyond the limits.</exception>
    public void AddSvg(string id, ReadOnlyMemory<byte> data, NineSlice slice = default) => Add(id, data, slice, svg: true);

    /// <summary>Registers a raster image (PNG, JPEG, WebP and the other formats OneImage reads).</summary>
    /// <param name="id">The resource id used by display list <c>Image</c> commands.</param>
    /// <param name="data">The file.</param>
    /// <param name="slice">Nine-slice insets in image pixels, or zero.</param>
    /// <exception cref="AtlasAssetException">The file is too large, damaged, or larger than <see cref="ArtLimits.MaxRasterDimension"/>.</exception>
    public void AddImage(string id, ReadOnlyMemory<byte> data, NineSlice slice = default) => Add(id, data, slice, svg: false);

    /// <summary>Returns the natural size of a registered resource: the SVG's own size, or the image's pixel size.</summary>
    /// <param name="id">The resource id.</param>
    /// <param name="width">Natural width.</param>
    /// <param name="height">Natural height.</param>
    /// <returns>False when the id is unknown.</returns>
    public bool TryGetNaturalSize(string id, out int width, out int height)
    {
        if (_sources.TryGetValue(id, out var s))
        {
            width = s.Width;
            height = s.Height;
            return true;
        }

        width = height = 0;
        return false;
    }

    /// <summary>Looks up (rasterizing on first use) the resource at its natural size times <paramref name="scale"/>, rounded to whole pixels.</summary>
    /// <param name="id">The resource id.</param>
    /// <param name="scale">Pixels per natural pixel: the display list's UI scale. A raster image ignores it.</param>
    /// <param name="region">The entry when the result is <see cref="AtlasStatus.Ok"/>.</param>
    /// <returns>The status.</returns>
    public AtlasStatus TryGetScaled(string id, float scale, out ArtRegion region)
    {
        region = default;
        if (!_sources.TryGetValue(id, out var s))
        {
            return AtlasStatus.UnknownResource;
        }

        if (!s.Svg)
        {
            return TryGet(id, s.Width, s.Height, out region);
        }

        if (!float.IsFinite(scale) || scale <= 0 || scale > 64)
        {
            return AtlasStatus.TooLarge;
        }

        return TryGet(id, Math.Max(1, (int)MathF.Round(s.Width * scale)), Math.Max(1, (int)MathF.Round(s.Height * scale)), out region);
    }

    private static bool IsFittedSize(Source s, float width, float height, float scale, int threshold) =>
        s.Svg && float.IsFinite(width) && float.IsFinite(height) && width >= 1 && height >= 1 && width <= int.MaxValue && height <= int.MaxValue
        && MathF.Max(width, height) > threshold
        && (MathF.Abs(width - (s.Width * scale)) > 1f || MathF.Abs(height - (s.Height * scale)) > 1f);

    /// <summary>
    /// Looks up the resource for drawing into a box of the given pixel size. An SVG whose box is larger than <see cref="ArtLimits.StandaloneThreshold"/> on a side is rasterized at the box's
    /// own size when that differs from its natural size times <paramref name="scale"/> by more than a pixel, so a backdrop stretched over a panel stays sharp. Smaller boxes (icons, cards)
    /// and raster images use <see cref="TryGetScaled"/>, which keeps the shared pages from filling with one raster per layout size.
    /// When the exact size is refused for size or memory, the scaled lookup is the fallback.
    /// </summary>
    /// <param name="id">The resource id.</param>
    /// <param name="width">Box width in pixels.</param>
    /// <param name="height">Box height in pixels.</param>
    /// <param name="scale">The display list's UI scale.</param>
    /// <param name="region">The entry when the result is <see cref="AtlasStatus.Ok"/>.</param>
    /// <returns>The status.</returns>
    public AtlasStatus TryGetFitted(string id, float width, float height, float scale, out ArtRegion region)
    {
        region = default;
        if (!_sources.TryGetValue(id, out var s))
        {
            return AtlasStatus.UnknownResource;
        }

        if (IsFittedSize(s, width, height, scale, Math.Min(_limits.StandaloneThreshold, _pages.MaxEntrySize)))
        {
            var status = TryGet(id, (int)MathF.Round(width), (int)MathF.Round(height), out region);
            if (status is not (AtlasStatus.TooLarge or AtlasStatus.Full))
            {
                return status;
            }
        }

        return TryGetScaled(id, scale, out region);
    }

    /// <summary>Looks up (rasterizing on first use) the resource at an exact pixel size. A raster image is always stored at its own size.</summary>
    /// <param name="id">The resource id.</param>
    /// <param name="width">Width in pixels.</param>
    /// <param name="height">Height in pixels.</param>
    /// <param name="region">The entry when the result is <see cref="AtlasStatus.Ok"/>.</param>
    /// <returns>The status. Sizes beyond the budget return <see cref="AtlasStatus.TooLarge"/> before anything is decoded or allocated.</returns>
    public AtlasStatus TryGet(string id, int width, int height, out ArtRegion region)
    {
        region = default;
        if (!_sources.TryGetValue(id, out var s))
        {
            return AtlasStatus.UnknownResource;
        }

        if (!s.Svg)
        {
            (width, height) = (s.Width, s.Height);
        }

        if (width <= 0 || height <= 0 || width > _limits.MaxRasterDimension || height > _limits.MaxRasterDimension
            || (long)width * height > _limits.MaxRasterPixels || (long)width * height * 4 > _limits.MaxStandaloneBytes)
        {
            return AtlasStatus.TooLarge;
        }

        var key = new Key(id, width, height);
        if (_entries.TryGetValue(key, out var e))
        {
            if (e.Failed)
            {
                return AtlasStatus.Invalid;
            }

            if (e.Region.Standalone != 0)
            {
                _standalones[e.Region.Standalone].LastUsedFrame = _pages.Frame;
            }
            else
            {
                _pages.Touch(e.Region.Page);
            }

            region = e.Region;
            return AtlasStatus.Ok;
        }

        byte[] pixels;
        if (!MakeRoomFor(width, height))
        {
            return AtlasStatus.Full;
        }

        try
        {
            Interlocked.Increment(ref _syncRasterizations);
            pixels = Rasterize(s, width, height);
        }
        catch (ImageException)
        {
            _entries[key] = new Entry(default, true);
            return AtlasStatus.Invalid;
        }

        return Store(key, s, pixels, out region);
    }

    private bool IsStandalone(int width, int height)
    {
        var limit = Math.Min(_limits.StandaloneThreshold, _pages.MaxEntrySize);
        return width > limit || height > limit;
    }

    private bool MakeRoomFor(int width, int height) => !IsStandalone(width, height) || MakeRoom((long)width * height * 4);

    // Puts finished pixels into a page or a standalone texture and records the entry. Frame thread only.
    private AtlasStatus Store(Key key, Source s, byte[] pixels, out ArtRegion region)
    {
        region = default;
        var width = key.Width;
        var height = key.Height;
        var scaled = s.Slice.Scaled((float)width / s.Width, (float)height / s.Height);
        if (IsStandalone(width, height))
        {
            var art = new StandaloneArt(_nextHandle++, width, height, pixels) { LastUsedFrame = _pages.Frame };
            _standalones[art.Handle] = art;
            StandaloneBytes += art.Bytes;
            region = new ArtRegion(-1, 0, 0, width, height, 0, 0, 1, 1, scaled, art.Handle);
            _entries[key] = new Entry(region, false);
            return AtlasStatus.Ok;
        }

        if (!_pages.TryAllocate(width, height, out var slot, out var cleared))
        {
            return AtlasStatus.Full;
        }

        if (cleared >= 0)
        {
            DropPage(cleared);
        }

        _pages.Upload(slot, width, height, pixels, width * 4);
        var ps = (float)_pages.PageSize;
        region = new ArtRegion(slot.Page, slot.X, slot.Y, width, height, slot.X / ps, slot.Y / ps, (slot.X + width) / ps, (slot.Y + height) / ps, scaled);
        _entries[key] = new Entry(region, false);
        return AtlasStatus.Ok;
    }

    // Releases the least recently used standalone textures that were not drawn this frame until `need` more bytes fit the budget.
    private bool MakeRoom(long need)
    {
        while (StandaloneBytes + need > _limits.MaxStandaloneBytes)
        {
            StandaloneArt? victim = null;
            foreach (var a in _standalones.Values)
            {
                if (a.LastUsedFrame < _pages.Frame && (victim is null || a.LastUsedFrame < victim.LastUsedFrame))
                {
                    victim = a;
                }
            }

            if (victim is null)
            {
                return false;
            }

            Release(victim);
        }

        return true;
    }

    private void Release(StandaloneArt art)
    {
        _standalones.Remove(art.Handle);
        StandaloneBytes -= art.Bytes;
        _released.Add(art.Handle);
        foreach (var kv in _entries)
        {
            if (kv.Value.Region.Standalone == art.Handle)
            {
                _scratchKeys.Add(kv.Key);
            }
        }

        foreach (var k in _scratchKeys)
        {
            _entries.Remove(k);
        }

        _scratchKeys.Clear();
    }

    /// <summary>Gets how many rasterizations ran inside a lookup (<see cref="TryGet"/> and its variants), that is on the caller's thread, because the art was not ready.</summary>
    public long SyncRasterizations => Interlocked.Read(ref _syncRasterizations);

    /// <summary>Gets how many rasterizations ran for <see cref="Preload(IEnumerable{string}, float)"/>, on a worker thread or in <see cref="PumpPreload"/>.</summary>
    public long PreloadRasterizations => Interlocked.Read(ref _backgroundRasterizations);

    /// <summary>
    /// Gets or sets whether preload may rasterize on worker threads. The default is true everywhere threads exist and false in the browser (WebAssembly without threads),
    /// where preload work is done a little at a time by <see cref="PumpPreload"/> instead.
    /// </summary>
    public bool BackgroundThreads { get; set; } = !OperatingSystem.IsBrowser();

    /// <summary>Gets or sets the time <see cref="BeginFrame"/> may spend rasterizing preload work when <see cref="BackgroundThreads"/> is false (at least one item is always done). Default 2 ms.</summary>
    public TimeSpan PreloadFrameBudget { get; set; } = TimeSpan.FromMilliseconds(2);

    /// <summary>
    /// Starts rasterizing resources ahead of their first draw, at their natural size times <paramref name="scale"/> (the display list's UI scale), the size <see cref="TryGetScaled"/> asks for.
    /// The decoding and rasterizing run on worker threads; the pages are only touched on the UI thread, in <see cref="BeginFrame"/> or <see cref="PumpPreload"/>.
    /// Until the result is stored, a draw behaves as without preload: it rasterizes on the spot. Unknown ids, sizes beyond the budgets and art already present count as finished.
    /// </summary>
    /// <param name="ids">The resource ids.</param>
    /// <param name="scale">The UI scale the art will be drawn at.</param>
    /// <returns>A handle to watch progress.</returns>
    public ArtPreload Preload(IEnumerable<string> ids, float scale)
    {
        ArgumentNullException.ThrowIfNull(ids);
        var handle = new ArtPreload();
        foreach (var id in ids)
        {
            if (_sources.TryGetValue(id, out var s))
            {
                if (!s.Svg)
                {
                    Enqueue(handle, id, s, s.Width, s.Height);
                }
                else if (float.IsFinite(scale) && scale > 0 && scale <= 64)
                {
                    Enqueue(handle, id, s, Math.Max(1, (int)MathF.Round(s.Width * scale)), Math.Max(1, (int)MathF.Round(s.Height * scale)));
                }
                else
                {
                    handle.Add(1);
                    handle.Finish(failed: true);
                }
            }
            else
            {
                handle.Add(1);
                handle.Finish(failed: true);
            }
        }

        handle.Seal();
        StartWorkers();
        return handle;
    }

    /// <summary>Starts rasterizing one resource for a box of the given pixel size, the size <see cref="TryGetFitted"/> would pick; see <see cref="Preload(IEnumerable{string}, float)"/>.</summary>
    /// <param name="id">The resource id.</param>
    /// <param name="width">Box width in pixels.</param>
    /// <param name="height">Box height in pixels.</param>
    /// <param name="scale">The display list's UI scale.</param>
    /// <returns>A handle to watch progress.</returns>
    public ArtPreload PreloadFitted(string id, float width, float height, float scale)
    {
        ArgumentNullException.ThrowIfNull(id);
        if (_sources.TryGetValue(id, out var s) && IsFittedSize(s, width, height, scale, Math.Min(_limits.StandaloneThreshold, _pages.MaxEntrySize)))
        {
            var handle = new ArtPreload();
            Enqueue(handle, id, s, (int)MathF.Round(width), (int)MathF.Round(height));
            handle.Seal();
            StartWorkers();
            return handle;
        }

        return Preload([id], scale);
    }

    private void Enqueue(ArtPreload handle, string id, Source s, int width, int height)
    {
        handle.Add(1);
        if (width <= 0 || height <= 0 || width > _limits.MaxRasterDimension || height > _limits.MaxRasterDimension
            || (long)width * height > _limits.MaxRasterPixels || (long)width * height * 4 > _limits.MaxStandaloneBytes)
        {
            handle.Finish(failed: true);
            return;
        }

        var key = new Key(id, width, height);
        if (_entries.TryGetValue(key, out var e))
        {
            handle.Finish(failed: e.Failed);
            return;
        }

        lock (_gate)
        {
            _queue.Enqueue(new PreloadJob(key, s, handle));
        }
    }

    private void StartWorkers()
    {
        if (!BackgroundThreads)
        {
            return;
        }

        lock (_gate)
        {
            var want = Math.Min(_queue.Count, Math.Clamp(Environment.ProcessorCount / 2, 1, 4));
            while (_workers < want)
            {
                _workers++;
                _ = Task.Run(Work);
            }
        }
    }

    // Worker loop: only reads the immutable source bytes and limits, never the atlas state.
    private void Work()
    {
        while (true)
        {
            PreloadJob? job;
            lock (_gate)
            {
                if (!_queue.TryDequeue(out job))
                {
                    _workers--;
                    return;
                }
            }

            _done.Enqueue(Run(job));
        }
    }

    private PreloadResult Run(PreloadJob job)
    {
        try
        {
            var pixels = Rasterize(job.Source, job.Key.Width, job.Key.Height);
            Interlocked.Increment(ref _backgroundRasterizations);
            return new PreloadResult(job, pixels, false);
        }
        catch (ImageException)
        {
            return new PreloadResult(job, null, true);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Not remembered: a draw will try again on its own thread and surface the problem there.
            return new PreloadResult(job, null, false);
        }
    }

    /// <summary>
    /// Stores finished preload results in the pages (a memory copy per item), and, when <see cref="BackgroundThreads"/> is false, rasterizes queued work for up to
    /// <see cref="PreloadFrameBudget"/> first. <see cref="BeginFrame"/> calls it; call it directly to make progress outside a frame. UI thread only.
    /// </summary>
    public void PumpPreload()
    {
        if (!BackgroundThreads)
        {
            var start = System.Diagnostics.Stopwatch.GetTimestamp();
            while (true)
            {
                PreloadJob? job;
                lock (_gate)
                {
                    if (!_queue.TryDequeue(out job))
                    {
                        break;
                    }
                }

                _done.Enqueue(Run(job));
                if (System.Diagnostics.Stopwatch.GetElapsedTime(start) >= PreloadFrameBudget)
                {
                    break;
                }
            }
        }

        while (_done.TryDequeue(out var r))
        {
            Apply(r);
        }
    }

    /// <summary>Finishes every queued preload (the queue on the calling thread, work already on a worker by waiting for it) and stores the results: a loading screen's last step. UI thread only.</summary>
    public void CompletePreload()
    {
        while (true)
        {
            PreloadJob? job;
            lock (_gate)
            {
                _queue.TryDequeue(out job);
            }

            if (job is null)
            {
                break;
            }

            _done.Enqueue(Run(job));
        }

        while (true)
        {
            int workers;
            lock (_gate)
            {
                workers = _workers;
            }

            // Drain after reading the count: a worker enqueues its result before it exits.
            while (_done.TryDequeue(out var r))
            {
                Apply(r);
            }

            if (workers == 0)
            {
                break;
            }

            Thread.Yield();
        }
    }

    private void Apply(PreloadResult r)
    {
        var job = r.Job;
        var failed = false;
        // Skip results for art replaced by AddSvg/AddImage since the job was queued, and for entries a draw already made.
        if (_sources.TryGetValue(job.Key.Id, out var current) && ReferenceEquals(current, job.Source) && !_entries.ContainsKey(job.Key))
        {
            if (r.Invalid)
            {
                _entries[job.Key] = new Entry(default, true);
                failed = true;
            }
            else if (r.Pixels is not null)
            {
                failed = !MakeRoomFor(job.Key.Width, job.Key.Height) || Store(job.Key, job.Source, r.Pixels, out _) != AtlasStatus.Ok;
            }
            else
            {
                failed = true;
            }
        }
        else if (_entries.TryGetValue(job.Key, out var e))
        {
            failed = e.Failed;
        }

        job.Handle.Finish(failed);
    }

    private void Add(string id, ReadOnlyMemory<byte> data, NineSlice slice, bool svg)
    {
        ArgumentNullException.ThrowIfNull(id);
        if (_sources.Count >= _limits.MaxResources && !_sources.ContainsKey(id))
        {
            throw new AtlasAssetException($"The atlas already holds {_limits.MaxResources} resources.");
        }

        var max = svg ? _limits.MaxSvgBytes : _limits.MaxImageBytes;
        if (data.Length == 0 || data.Length > max)
        {
            throw new AtlasAssetException($"'{id}' is {data.Length} bytes; the limit for {(svg ? "an SVG" : "an image")} is {max}.");
        }

        var copy = data.ToArray();
        ImageInfo info;
        try
        {
            info = Image.Identify(copy, LoadOptionsFor(svg));
        }
        catch (ImageException ex)
        {
            throw new AtlasAssetException($"'{id}' could not be read: {ex.Message}", ex);
        }

        var w = info.Width;
        var h = info.Height;
        if (w <= 0 || h <= 0)
        {
            throw new AtlasAssetException($"'{id}' has no size.");
        }

        if (!svg && (w > _limits.MaxRasterDimension || h > _limits.MaxRasterDimension || (long)w * h > _limits.MaxRasterPixels))
        {
            throw new AtlasAssetException($"'{id}' is {w}x{h} pixels; the limit is {_limits.MaxRasterDimension} per side and {_limits.MaxRasterPixels} pixels.");
        }

        _sources[id] = new Source(svg, copy, w, h, slice);
        List<StandaloneArt>? stale = null;
        foreach (var kv in _entries)
        {
            if (kv.Key.Id == id)
            {
                _scratchKeys.Add(kv.Key);
                if (kv.Value.Region.Standalone != 0)
                {
                    (stale ??= []).Add(_standalones[kv.Value.Region.Standalone]);
                }
            }
        }

        foreach (var k in _scratchKeys)
        {
            _entries.Remove(k);
        }

        _scratchKeys.Clear();
        if (stale is not null)
        {
            foreach (var a in stale)
            {
                Release(a);
            }
        }
    }

    private LoadOptions LoadOptionsFor(bool svg) => new()
    {
        Configuration = svg ? SvgConfig : ImageConfiguration.Default,
        Limits = new ImageLimits
        {
            MaxDimension = svg ? _limits.MaxSvgNaturalDimension : _limits.MaxRasterDimension,
            MaxPixelsPerFrame = svg ? Math.Max(_limits.MaxRasterPixels, (long)_limits.MaxSvgNaturalDimension * _limits.MaxSvgNaturalDimension) : _limits.MaxRasterPixels,
            MaxFrames = 256,
            MaxTotalPixels = _limits.MaxRasterPixels * 4,
        },
        AutoOrient = true,
    };

    private byte[] Rasterize(Source s, int width, int height)
    {
        var image = Image.Load(s.Data, LoadOptionsFor(s.Svg));
        if (s.Svg || image.Width != width || image.Height != height)
        {
            // An SVG is rendered directly at the target size, not scaled up from its natural size.
            image = image.Resize(width, height, Fit.Stretch);
        }

        // ToArray<Rgba32> hands back straight alpha; the pages hold premultiplied texels.
        var px = image.ToArray<Rgba32>();
        for (var i = 0; i < px.Length; i++)
        {
            var p = px[i];
            if (p.A != 255)
            {
                px[i] = new Rgba32((byte)(((p.R * p.A) + 127) / 255), (byte)(((p.G * p.A) + 127) / 255), (byte)(((p.B * p.A) + 127) / 255), p.A);
            }
        }

        return MemoryMarshal.AsBytes(px.AsSpan()).ToArray();
    }

    private void DropPage(int page)
    {
        foreach (var kv in _entries)
        {
            if (!kv.Value.Failed && kv.Value.Region.Page == page)
            {
                _scratchKeys.Add(kv.Key);
            }
        }

        foreach (var k in _scratchKeys)
        {
            _entries.Remove(k);
        }

        _scratchKeys.Clear();
    }
}
