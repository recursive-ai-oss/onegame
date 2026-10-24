namespace OneGame.Atlas;

/// <summary>The pixel layout of an atlas page.</summary>
public enum AtlasFormat
{
    /// <summary>One byte per pixel: coverage (0 = outside, 255 = fully inside). Glyph pages use it; the shader multiplies it into the text color.</summary>
    R8,

    /// <summary>Four bytes per pixel, R G B A, with the color channels already multiplied by alpha. SVG and image pages use it; blend with (One, OneMinusSrcAlpha).</summary>
    Rgba8Premultiplied,
}

/// <summary>An integer rectangle in page pixels, origin top-left.</summary>
/// <param name="X">Left edge.</param>
/// <param name="Y">Top edge.</param>
/// <param name="Width">Width in pixels.</param>
/// <param name="Height">Height in pixels.</param>
public readonly record struct AtlasRect(int X, int Y, int Width, int Height)
{
    /// <summary>Gets whether the rectangle covers no pixels.</summary>
    public bool IsEmpty => Width <= 0 || Height <= 0;

    /// <summary>Gets the smallest rectangle holding both rectangles; an empty one is ignored.</summary>
    /// <param name="other">The other rectangle.</param>
    /// <returns>The union.</returns>
    public AtlasRect Union(AtlasRect other)
    {
        if (IsEmpty)
        {
            return other;
        }

        if (other.IsEmpty)
        {
            return this;
        }

        var x0 = Math.Min(X, other.X);
        var y0 = Math.Min(Y, other.Y);
        var x1 = Math.Max(X + Width, other.X + other.Width);
        var y1 = Math.Max(Y + Height, other.Y + other.Height);
        return new AtlasRect(x0, y0, x1 - x0, y1 - y0);
    }
}

/// <summary>
/// One texture-sized page of pixels in system memory. The atlas writes into it; the renderer uploads the dirty region to a texture and
/// samples it. Rows are tightly packed (<see cref="Stride"/> = width × bytes per pixel), top row first.
/// </summary>
public sealed class AtlasPage
{
    private readonly byte[] _pixels;
    private AtlasRect _dirty;

    internal AtlasPage(int index, int size, AtlasFormat format)
    {
        Index = index;
        Width = size;
        Height = size;
        Format = format;
        BytesPerPixel = format == AtlasFormat.R8 ? 1 : 4;
        Stride = size * BytesPerPixel;
        _pixels = new byte[checked(Stride * size)];
        _dirty = new AtlasRect(0, 0, size, size);
        Version = 1;
    }

    /// <summary>Gets the page number within its atlas. The renderer keeps one texture per index.</summary>
    public int Index { get; }

    /// <summary>Gets the width in pixels.</summary>
    public int Width { get; }

    /// <summary>Gets the height in pixels.</summary>
    public int Height { get; }

    /// <summary>Gets the pixel layout.</summary>
    public AtlasFormat Format { get; }

    /// <summary>Gets the bytes per pixel (1 or 4).</summary>
    public int BytesPerPixel { get; }

    /// <summary>Gets the bytes per row.</summary>
    public int Stride { get; }

    /// <summary>Gets a counter that grows with every write or clear. Equal values mean equal pixels.</summary>
    public int Version { get; private set; }

    /// <summary>Gets all pixels of the page.</summary>
    public ReadOnlySpan<byte> Pixels => _pixels;

    /// <summary>Gets the part of the page changed since the last <see cref="TakeDirty"/>. A new page starts fully dirty.</summary>
    public AtlasRect Dirty => _dirty;

    /// <summary>Returns the bytes of one row of <paramref name="rect"/> (its width × bytes per pixel), for uploading a dirty region row by row.</summary>
    /// <param name="rect">A rectangle inside the page.</param>
    /// <param name="row">Row within the rectangle, 0-based.</param>
    /// <returns>The row's bytes.</returns>
    public ReadOnlySpan<byte> RowOf(AtlasRect rect, int row) =>
        new(_pixels, ((rect.Y + row) * Stride) + (rect.X * BytesPerPixel), rect.Width * BytesPerPixel);

    /// <summary>Returns the dirty region and marks the page clean. Call it once per upload.</summary>
    /// <returns>The changed rectangle, empty when nothing changed.</returns>
    public AtlasRect TakeDirty()
    {
        var d = _dirty;
        _dirty = default;
        return d;
    }

    internal void MarkDirty(AtlasRect rect)
    {
        _dirty = _dirty.Union(rect);
        Version++;
    }

    internal void Blit(int x, int y, int w, int h, ReadOnlySpan<byte> src, int srcStride)
    {
        var bpp = BytesPerPixel;
        for (var r = 0; r < h; r++)
        {
            src.Slice(r * srcStride, w * bpp).CopyTo(_pixels.AsSpan(((y + r) * Stride) + (x * bpp)));
        }

        MarkDirty(new AtlasRect(x, y, w, h));
    }

    internal void Clear()
    {
        Array.Clear(_pixels);
        _dirty = new AtlasRect(0, 0, Width, Height);
        Version++;
    }
}
