using OneGame.Atlas;
using OneRender.Painting;

namespace OneGame.Pages;

/// <summary>What <see cref="PageImages"/> knows about one image address.</summary>
internal sealed record PageImageEntry(string ArtId, bool IsSvg, int Width, int Height);

/// <summary>
/// The images a page can show. Pages name images by address (<c>&lt;img src="coin.png"&gt;</c>, <c>background-image: url(coin.png)</c>); the game registers
/// the bytes under that address and the images live in the game's <see cref="ArtAtlas"/> next to the UI's art. OneRender never sees pixels, only the
/// handle and size this class hands back.
/// </summary>
public sealed class PageImages : IImageSource
{
    /// <summary>The most images one set holds.</summary>
    public const int MaxImages = 4096;

    private readonly ArtAtlas _art;
    private readonly Dictionary<string, int> _handles = new(StringComparer.Ordinal);
    private readonly List<PageImageEntry> _entries = [];
    private readonly List<string> _addresses = [];

    /// <summary>Creates an empty set over the game's art atlas.</summary>
    /// <param name="art">The atlas the images are registered in.</param>
    public PageImages(ArtAtlas art)
    {
        ArgumentNullException.ThrowIfNull(art);
        _art = art;
    }

    /// <summary>How many addresses are registered.</summary>
    public int Count => _entries.Count;

    /// <summary>Registers an image or SVG file under an address.</summary>
    /// <param name="address">The address as the page writes it.</param>
    /// <param name="data">The file's bytes (PNG, JPEG, WebP, SVG...).</param>
    /// <returns>The handle pages use for it.</returns>
    /// <exception cref="AtlasAssetException">The file is damaged, too large, or the set is full.</exception>
    public ImageHandle Add(string address, ReadOnlyMemory<byte> data)
    {
        ArgumentException.ThrowIfNullOrEmpty(address);
        if (_handles.TryGetValue(address, out var existing))
        {
            Replace(address, data);
            return new ImageHandle(existing);
        }

        if (_entries.Count >= MaxImages)
        {
            throw new AtlasAssetException($"A page image set holds at most {MaxImages} images.");
        }

        var handle = _entries.Count + 1;
        _entries.Add(Register("page-img:" + handle, data));
        _addresses.Add(address);
        _handles[address] = handle;
        return new ImageHandle(handle);
    }

    /// <summary>
    /// Replaces the pixels behind an address. Pages that show it draw the new pixels from the next frame when the size is unchanged
    /// (call <c>PageSet.InvalidateImage</c>); a new size needs the page to lay out again.
    /// </summary>
    /// <param name="address">A registered address.</param>
    /// <param name="data">The new file.</param>
    /// <returns>The image's handle.</returns>
    /// <exception cref="KeyNotFoundException">The address is not registered.</exception>
    /// <exception cref="AtlasAssetException">The file is damaged or too large.</exception>
    public ImageHandle Replace(string address, ReadOnlyMemory<byte> data)
    {
        if (!_handles.TryGetValue(address, out var handle))
        {
            throw new KeyNotFoundException($"No image is registered under '{address}'.");
        }

        _entries[handle - 1] = Register("page-img:" + handle, data);
        return new ImageHandle(handle);
    }

    /// <summary>The handle of an address, if it is registered.</summary>
    /// <param name="address">The address.</param>
    /// <param name="handle">The handle.</param>
    /// <returns>True when registered.</returns>
    public bool TryGetHandle(string address, out ImageHandle handle)
    {
        if (_handles.TryGetValue(address, out var h))
        {
            handle = new ImageHandle(h);
            return true;
        }

        handle = default;
        return false;
    }

    /// <inheritdoc />
    public bool TryGetImage(string address, out ImageInfo image)
    {
        if (address is not null && _handles.TryGetValue(address, out var h))
        {
            var e = _entries[h - 1];
            image = new ImageInfo(new ImageHandle(h), e.Width, e.Height);
            return true;
        }

        image = default;
        return false;
    }

    internal PageImageEntry? Resolve(ImageHandle handle) =>
        (uint)(handle.Value - 1) < (uint)_entries.Count ? _entries[handle.Value - 1] : null;

    internal string? AddressOf(ImageHandle handle) =>
        (uint)(handle.Value - 1) < (uint)_addresses.Count ? _addresses[handle.Value - 1] : null;

    private PageImageEntry Register(string id, ReadOnlyMemory<byte> data)
    {
        var svg = LooksLikeSvg(data.Span);
        if (svg)
        {
            _art.AddSvg(id, data);
        }
        else
        {
            _art.AddImage(id, data);
        }

        _art.TryGetNaturalSize(id, out var w, out var h);
        return new PageImageEntry(id, svg, w, h);
    }

    private static bool LooksLikeSvg(ReadOnlySpan<byte> data)
    {
        // An SVG is text that holds an <svg element near the start; every raster format starts with binary magic.
        var n = Math.Min(data.Length, 1024);
        return data[..n].IndexOf("<svg"u8) >= 0;
    }
}
