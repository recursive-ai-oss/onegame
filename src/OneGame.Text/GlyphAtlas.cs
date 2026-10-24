using OneGame.Atlas;

namespace OneGame.Text;

/// <summary>Where one rasterized glyph sits in the atlas, and how to place it. A glyph with no ink (a space) has <see cref="Width"/> 0 and no atlas entry.</summary>
/// <param name="Page">Page index.</param>
/// <param name="X">Left edge in page pixels.</param>
/// <param name="Y">Top edge in page pixels.</param>
/// <param name="Width">Bitmap width in pixels.</param>
/// <param name="Height">Bitmap height in pixels.</param>
/// <param name="U0">Left texture coordinate.</param>
/// <param name="V0">Top texture coordinate.</param>
/// <param name="U1">Right texture coordinate.</param>
/// <param name="V1">Bottom texture coordinate.</param>
/// <param name="OffsetX">Distance from the whole-pixel pen position to the bitmap's left edge (the subpixel shift is already inside the bitmap).</param>
/// <param name="OffsetY">Distance from the baseline to the bitmap's top edge, y down (negative above the baseline).</param>
public readonly record struct GlyphInfo(int Page, int X, int Y, int Width, int Height, float U0, float V0, float U1, float V1, int OffsetX, int OffsetY);

/// <summary>Budgets for glyph rasterization.</summary>
public sealed class GlyphLimits
{
    /// <summary>Gets the largest font size that is rasterized, in pixels. Default 512.</summary>
    public float MaxFontSize { get; init; } = 512;

    /// <summary>Gets the largest glyph bitmap edge in pixels. Default 640.</summary>
    public int MaxBitmapDimension { get; init; } = 640;

    /// <summary>Gets the largest glyph bitmap in pixels. Default 262,144.</summary>
    public int MaxBitmapPixels { get; init; } = 640 * 410;
}

/// <summary>
/// Glyph coverage atlas. Glyphs are rasterized on first use at the exact pixel size and a quarter-pixel horizontal position, from the
/// face's outline, into <see cref="AtlasFormat.R8"/> pages. Keyed by face, glyph, size (to 1/64 px) and subpixel step; when the pages are
/// full the least recently used page is cleared as a whole.
/// </summary>
public sealed class GlyphAtlas
{
    /// <summary>The number of horizontal subpixel positions a glyph is rasterized at.</summary>
    public const int SubpixelSteps = 4;

    private readonly record struct Key(int FaceId, int GlyphId, int SizeKey, int Step);

    private readonly FontLibrary _library;
    private readonly GlyphLimits _limits;
    private readonly AtlasPages _pages;
    private readonly Dictionary<Key, GlyphInfo> _glyphs = [];
    private readonly GlyphRasterizer _raster = new();
    private readonly List<Key> _scratchKeys = [];
    private byte[] _bitmap = new byte[4096];

    /// <summary>Creates an empty atlas.</summary>
    /// <param name="library">The fonts glyphs come from.</param>
    /// <param name="options">Page sizes; the defaults when <see langword="null"/>.</param>
    /// <param name="limits">Budgets; the defaults when <see langword="null"/>.</param>
    public GlyphAtlas(FontLibrary library, AtlasOptions? options = null, GlyphLimits? limits = null)
    {
        ArgumentNullException.ThrowIfNull(library);
        _library = library;
        _limits = limits ?? new GlyphLimits();
        _pages = new AtlasPages(AtlasFormat.R8, options);
    }

    /// <summary>Gets the pages. The renderer uploads <see cref="AtlasPage.TakeDirty"/> of each and keeps one texture per page.</summary>
    public AtlasPages Pages => _pages;

    /// <summary>Starts a frame; see <see cref="AtlasPages.BeginFrame"/>.</summary>
    /// <param name="frame">A number that grows with every frame.</param>
    public void BeginFrame(long frame) => _pages.BeginFrame(frame);

    /// <summary>Looks up a glyph, rasterizing it on first use.</summary>
    /// <param name="faceId">Face id from a <see cref="PositionedGlyph"/>.</param>
    /// <param name="glyphId">Glyph index.</param>
    /// <param name="size">Font size in pixels (rounded to 1/64).</param>
    /// <param name="subpixelStep">Horizontal position within the pixel in quarters, 0 to <see cref="SubpixelSteps"/> - 1.</param>
    /// <param name="info">The glyph when the result is <see cref="AtlasStatus.Ok"/>.</param>
    /// <returns>The status. A size or bitmap beyond the budget returns <see cref="AtlasStatus.TooLarge"/> before any bitmap is allocated.</returns>
    public AtlasStatus TryGet(int faceId, int glyphId, float size, int subpixelStep, out GlyphInfo info)
    {
        info = default;
        if (!float.IsFinite(size) || size <= 0 || size > _limits.MaxFontSize || (uint)subpixelStep >= SubpixelSteps)
        {
            return AtlasStatus.TooLarge;
        }

        var sizeKey = (int)MathF.Round(size * 64);
        var key = new Key(faceId, glyphId, sizeKey, subpixelStep);
        if (_glyphs.TryGetValue(key, out info))
        {
            if (info.Width > 0)
            {
                _pages.Touch(info.Page);
            }

            return AtlasStatus.Ok;
        }

        var face = _library.Face(faceId);
        if ((uint)glyphId >= (uint)face.GlyphCount)
        {
            return AtlasStatus.UnknownResource;
        }

        var scale = (sizeKey / 64f) / face.UnitsPerEm;
        var shift = subpixelStep / (float)SubpixelSteps;
        var outline = face.GetOutline(glyphId);
        if (!GlyphRasterizer.Bounds(outline, scale, shift, out var left, out var top, out var w, out var h)
            || w > _limits.MaxBitmapDimension || h > _limits.MaxBitmapDimension || (long)w * h > _limits.MaxBitmapPixels
            || w > _pages.MaxEntrySize || h > _pages.MaxEntrySize)
        {
            return AtlasStatus.TooLarge;
        }

        if (outline.IsEmpty)
        {
            info = new GlyphInfo(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            _glyphs[key] = info;
            return AtlasStatus.Ok;
        }

        if (_bitmap.Length < w * h)
        {
            _bitmap = new byte[w * h];
        }

        if (!_pages.TryAllocate(w, h, out var slot, out var cleared))
        {
            return AtlasStatus.Full;
        }

        if (cleared >= 0)
        {
            DropPage(cleared);
        }

        _raster.Rasterize(outline, scale, shift, left, top, w, h, _bitmap);
        _pages.Upload(slot, w, h, _bitmap, w);
        var ps = (float)_pages.PageSize;
        info = new GlyphInfo(slot.Page, slot.X, slot.Y, w, h, slot.X / ps, slot.Y / ps, (slot.X + w) / ps, (slot.Y + h) / ps, left, top);
        _glyphs[key] = info;
        return AtlasStatus.Ok;
    }

    private void DropPage(int page)
    {
        foreach (var kv in _glyphs)
        {
            if (kv.Value.Width > 0 && kv.Value.Page == page)
            {
                _scratchKeys.Add(kv.Key);
            }
        }

        foreach (var k in _scratchKeys)
        {
            _glyphs.Remove(k);
        }

        _scratchKeys.Clear();
    }
}
