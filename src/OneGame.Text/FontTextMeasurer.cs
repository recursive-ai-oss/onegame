using OneFont;
using OneGame.UI;

namespace OneGame.Text;

/// <summary>
/// The UI's text measurer, backed by the font library: shapes with OneFont (bidi, fallback, ligatures, kerning), breaks lines at the
/// opportunities of UAX #14 and measures with the same advances the glyphs are drawn with. Results are cached per string and style, so
/// repeated layout of unchanged text shapes nothing and allocates nothing. Not thread safe.
/// </summary>
public sealed class FontTextMeasurer : ITextMeasurer
{
    /// <summary>The longest string accepted, in UTF-16 code units.</summary>
    public const int MaxTextLength = 1 << 20;

    /// <summary>The largest font size accepted, in pixels.</summary>
    public const float MaxFontSize = 4096;

    private const int MaxCached = 4096;

    private readonly record struct Key(string Text, string Family, int SizeKey, int Weight, bool Italic, int SpacingKey, int LineKey, WhiteSpace WhiteSpace);

    private readonly FontLibrary _library;
    private readonly Dictionary<Key, ShapedText> _cache = [];
    private readonly ShapedRun _scratch = new();
    private string? _lastText;
    private TextStyle _lastStyle;
    private ShapedText? _last;
    private int _version;

    /// <summary>Creates a measurer over a library.</summary>
    /// <param name="library">The fonts to use.</param>
    public FontTextMeasurer(FontLibrary library)
    {
        ArgumentNullException.ThrowIfNull(library);
        _library = library;
    }

    /// <summary>Gets the font library this measurer shapes with.</summary>
    public FontLibrary Library => _library;

    /// <inheritdoc/>
    /// <exception cref="TextLimitException">The text is longer than <see cref="MaxTextLength"/>.</exception>
    /// <exception cref="FontNotFoundException">The library holds no faces.</exception>
    public TextSize Measure(string text, in TextStyle style, float maxWidth) => Shape(text, style).Wrap(maxWidth, null);

    /// <inheritdoc/>
    /// <exception cref="TextLimitException">The text is longer than <see cref="MaxTextLength"/>.</exception>
    /// <exception cref="FontNotFoundException">The library holds no faces.</exception>
    public void Layout(string text, in TextStyle style, float maxWidth, List<TextLine> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        Shape(text, style).Wrap(maxWidth, lines);
    }

    /// <summary>
    /// Appends the glyphs of one laid-out line, in display order, with x measured from the line's left end and y from its baseline.
    /// </summary>
    /// <param name="text">The text the line came from.</param>
    /// <param name="style">The style it was laid out with.</param>
    /// <param name="start">The line's <c>Start</c>.</param>
    /// <param name="length">The line's <c>Length</c>.</param>
    /// <param name="into">Receives the glyphs.</param>
    /// <returns>The baseline's distance from the top of a line box of the style's line height.</returns>
    public float Place(string text, in TextStyle style, int start, int length, List<PositionedGlyph> into) =>
        Place(text, style, start, length, 0, into);

    /// <summary>Like <see cref="Place(string, in TextStyle, int, int, List{PositionedGlyph})"/>, for a line box of a given height.</summary>
    /// <param name="text">The text the line came from.</param>
    /// <param name="style">The style it was laid out with.</param>
    /// <param name="start">The line's <c>Start</c>.</param>
    /// <param name="length">The line's <c>Length</c>.</param>
    /// <param name="lineHeight">The line's <c>Height</c>; 0 for the style's own.</param>
    /// <param name="into">Receives the glyphs.</param>
    /// <returns>The baseline's distance from the top of the line box.</returns>
    public float Place(string text, in TextStyle style, int start, int length, float lineHeight, List<PositionedGlyph> into)
    {
        ArgumentNullException.ThrowIfNull(into);
        var shaped = Shape(text, style);
        shaped.Place(start, length, into);
        return shaped.BaselineIn(lineHeight > 0 ? lineHeight : shaped.LineHeight);
    }

    private ShapedText Shape(string text, in TextStyle style)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (_version != _library.Version)
        {
            _cache.Clear();
            _last = null;
            _version = _library.Version;
        }

        if (ReferenceEquals(text, _lastText) && style == _lastStyle && _last is not null)
        {
            return _last;
        }

        if (text.Length > MaxTextLength)
        {
            throw new TextLimitException($"A text of {text.Length} code units is over the limit of {MaxTextLength}.");
        }

        var size = float.IsFinite(style.Size) ? Math.Clamp(style.Size, 0.01f, MaxFontSize) : 0.01f;
        var spacing = float.IsFinite(style.LetterSpacing) ? Math.Clamp(style.LetterSpacing, -MaxFontSize, MaxFontSize) : 0;
        var line = float.IsFinite(style.LineHeight) ? Math.Clamp(style.LineHeight, 0, MaxFontSize * 4) : 0;

        // Sizes are rounded to 1/64 px so layout and the glyph atlas agree on one size.
        var sizeKey = (int)MathF.Round(size * 64);
        var key = new Key(text, style.Family ?? string.Empty, sizeKey, style.Weight, style.Italic, (int)MathF.Round(spacing * 64), (int)MathF.Round(line * 64), style.WhiteSpace);
        if (!_cache.TryGetValue(key, out var shaped))
        {
            if (_cache.Count >= MaxCached)
            {
                _cache.Clear();
            }

            var stack = _library.Resolve(style.Family ?? string.Empty, style.Weight, style.Italic);
            shaped = new ShapedText(text, stack, sizeKey / 64f, key.LineKey / 64f, key.SpacingKey / 64f, style.WhiteSpace, _scratch);
            _cache[key] = shaped;
        }

        _lastText = text;
        _lastStyle = style;
        _last = shaped;
        return shaped;
    }
}
