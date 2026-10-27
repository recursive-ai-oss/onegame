using OneFont;
using OneGame.Text;
using OneRender.Layout;
using OneRender.Style;
using OneRender.Text;

namespace OneGame.Pages;

/// <summary>
/// The fonts of a page, shared with the game's <see cref="FontLibrary"/>: each face is loaded once and the same <see cref="FontFace"/> object sits in
/// OneRender's catalog and in the library, so a page and a <c>OneGame.UI</c> screen shape and draw with identical faces and glyph bitmaps.
/// </summary>
public sealed class PageFonts
{
    private readonly FontLibrary _library;

    /// <summary>Creates an empty set of page fonts over the game's library.</summary>
    /// <param name="library">The library faces are shared with.</param>
    public PageFonts(FontLibrary library)
    {
        ArgumentNullException.ThrowIfNull(library);
        _library = library;
        Catalog = new FontCatalog();
        Provider = new TextFontProvider(Catalog);
    }

    /// <summary>OneRender's font catalog.</summary>
    public FontCatalog Catalog { get; }

    /// <summary>The font provider a page is created with.</summary>
    public IFontProvider Provider { get; }

    /// <summary>Loads a font file and registers its face for pages and for the game's text.</summary>
    /// <param name="fontData">The file's bytes (TrueType or OpenType).</param>
    /// <param name="family">The family name used in <c>font-family</c>; the font's own name when null.</param>
    /// <param name="faceIndex">Which face of a collection.</param>
    /// <returns>The face id in the game's <see cref="FontLibrary"/>.</returns>
    /// <exception cref="FontFormatException">The file is not a usable font.</exception>
    /// <exception cref="TextLimitException">The library is full or the file is over its limit.</exception>
    public int Add(byte[] fontData, string? family = null, int faceIndex = 0)
    {
        ArgumentNullException.ThrowIfNull(fontData);
        if (fontData.Length > FontLibrary.MaxFontBytes)
        {
            throw new TextLimitException($"A font file of {fontData.Length} bytes is over the limit of {FontLibrary.MaxFontBytes}.");
        }

        var face = FontFace.Load(fontData, faceIndex);
        var name = string.IsNullOrWhiteSpace(family) ? face.FamilyName : family.Trim();
        var id = _library.Add(face, name);
        Catalog.AddFont(face, name, face.Weight, face.IsItalic ? FontStyleKind.Italic : FontStyleKind.Normal);
        return id;
    }
}
