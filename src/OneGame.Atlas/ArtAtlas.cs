using System.Globalization;
using System.Runtime.InteropServices;
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
public readonly record struct ArtRegion(int Page, int X, int Y, int Width, int Height, float U0, float V0, float U1, float V1, NineSlice Slice);

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

    /// <summary>Gets the largest width or height of one raster in pixels, and so the largest natural size of a raster image. Default 2048.</summary>
    public int MaxRasterDimension { get; init; } = 2048;

    /// <summary>Gets the largest pixel count of one raster. Default 4,194,304 (2048 × 2048).</summary>
    public long MaxRasterPixels { get; init; } = 2048L * 2048;

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

    private readonly record struct Entry(ArtRegion Region, bool Failed);

    private static readonly ImageConfiguration SvgConfig = new ImageConfiguration().Add(SvgFormat.Instance);

    private readonly ArtLimits _limits;
    private readonly AtlasPages _pages;
    private readonly Dictionary<string, Source> _sources = new(StringComparer.Ordinal);
    private readonly Dictionary<Key, Entry> _entries = [];
    private readonly List<Key> _scratchKeys = [];

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

    /// <summary>Starts a frame; see <see cref="AtlasPages.BeginFrame"/>.</summary>
    /// <param name="frame">A number that grows with every frame.</param>
    public void BeginFrame(long frame) => _pages.BeginFrame(frame);

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
            || (long)width * height > _limits.MaxRasterPixels || width > _pages.MaxEntrySize || height > _pages.MaxEntrySize)
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

            _pages.Touch(e.Region.Page);
            region = e.Region;
            return AtlasStatus.Ok;
        }

        byte[] pixels;
        try
        {
            pixels = Rasterize(s, width, height);
        }
        catch (ImageException)
        {
            _entries[key] = new Entry(default, true);
            return AtlasStatus.Invalid;
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
        region = new ArtRegion(slot.Page, slot.X, slot.Y, width, height, slot.X / ps, slot.Y / ps, (slot.X + width) / ps, (slot.Y + height) / ps,
            s.Slice.Scaled((float)width / s.Width, (float)height / s.Height));
        _entries[key] = new Entry(region, false);
        return AtlasStatus.Ok;
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
        foreach (var k in _entries.Keys)
        {
            if (k.Id == id)
            {
                _scratchKeys.Add(k);
            }
        }

        foreach (var k in _scratchKeys)
        {
            _entries.Remove(k);
        }

        _scratchKeys.Clear();
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
