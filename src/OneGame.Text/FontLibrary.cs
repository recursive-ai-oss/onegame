using OneFont;

namespace OneGame.Text;

/// <summary>A font could not be found: the library holds no faces, or a face id is unknown.</summary>
public sealed class FontNotFoundException : Exception
{
    /// <summary>Creates the exception.</summary>
    /// <param name="message">What was asked for.</param>
    public FontNotFoundException(string message)
        : base(message)
    {
    }
}

/// <summary>A text or font request broke a budget: nothing was shaped or allocated for it.</summary>
public sealed class TextLimitException : Exception
{
    /// <summary>Creates the exception.</summary>
    /// <param name="message">Which limit and by how much.</param>
    public TextLimitException(string message)
        : base(message)
    {
    }
}

/// <summary>The faces a resolved <c>font-family</c> list stands for, in lookup order, with the metrics of the first.</summary>
internal sealed class FontStack
{
    public FontStack(FontLibrary library, int[] faceIds)
    {
        FaceIds = faceIds;
        var faces = new FontFace[faceIds.Length];
        for (var i = 0; i < faces.Length; i++)
        {
            faces[i] = library.Face(faceIds[i]);
        }

        Faces = faces;
        Chain = new FontFallbackChain(faces);
        var m = faces[0].Metrics;
        var upem = (float)faces[0].UnitsPerEm;
        Ascent = m.Ascender / upem;
        Descent = -m.Descender / upem;
        LineGap = m.LineGap / upem;
    }

    public int[] FaceIds { get; }

    public FontFace[] Faces { get; }

    public FontFallbackChain Chain { get; }

    /// <summary>Ascent, descent (positive) and line gap of the first face, in em.</summary>
    public float Ascent { get; }

    public float Descent { get; }

    public float LineGap { get; }
}

/// <summary>
/// The fonts of a game: faces loaded from asset bytes, grouped by family, resolved per <c>font-family</c> list, weight and slant into a
/// fallback chain. Not thread safe: use it from the thread that runs the UI.
/// </summary>
public sealed class FontLibrary
{
    /// <summary>The largest font file accepted, in bytes.</summary>
    public const int MaxFontBytes = 64 * 1024 * 1024;

    /// <summary>The most faces a library holds.</summary>
    public const int MaxFaces = 256;

    private readonly List<FontFace> _faces = [];
    private readonly Dictionary<string, List<int>> _families = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _aliases = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<int> _fallbacks = [];
    private readonly Dictionary<(string Family, int Weight, bool Italic), FontStack> _stacks = [];
    private FontStack? _default;
    private string? _firstFamily;

    /// <summary>Gets how many faces are loaded.</summary>
    public int FaceCount => _faces.Count;

    /// <summary>Loads a font file (TrueType, OpenType/CFF or a collection's first face) and adds its face.</summary>
    /// <param name="fontData">The file's bytes. The library keeps them.</param>
    /// <param name="family">The family name to register the face under; the font's own name when <see langword="null"/>.</param>
    /// <param name="faceIndex">Which face of a collection.</param>
    /// <returns>The face id used by glyph atlas keys and positioned glyphs.</returns>
    /// <exception cref="TextLimitException">The file or the library is over its budget.</exception>
    /// <exception cref="FontFormatException">The file is not a usable font.</exception>
    public int Add(byte[] fontData, string? family = null, int faceIndex = 0)
    {
        ArgumentNullException.ThrowIfNull(fontData);
        if (fontData.Length > MaxFontBytes)
        {
            throw new TextLimitException($"A font file of {fontData.Length} bytes is over the limit of {MaxFontBytes}.");
        }

        if (_faces.Count >= MaxFaces)
        {
            throw new TextLimitException($"The library already holds {MaxFaces} faces.");
        }

        var face = FontFace.Load(fontData, faceIndex);
        var id = _faces.Count;
        _faces.Add(face);
        var name = string.IsNullOrWhiteSpace(family) ? face.FamilyName : family.Trim();
        if (!_families.TryGetValue(name, out var list))
        {
            _families[name] = list = [];
        }

        list.Add(id);
        _firstFamily ??= name;
        Changed();
        return id;
    }

    /// <summary>Makes a loaded face a last resort for every stack, after the families the style names (for scripts the main font lacks).</summary>
    /// <param name="faceId">A face id returned by <see cref="Add"/>.</param>
    public void AddFallback(int faceId)
    {
        _ = Face(faceId);
        if (!_fallbacks.Contains(faceId))
        {
            _fallbacks.Add(faceId);
            Changed();
        }
    }

    /// <summary>Lets a generic name such as <c>sans-serif</c> or a theme's font name stand for a loaded family.</summary>
    /// <param name="name">The name used in <c>font-family</c>.</param>
    /// <param name="family">A loaded family.</param>
    public void SetAlias(string name, string family)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(family);
        _aliases[name.Trim()] = family.Trim();
        Changed();
    }

    /// <summary>Returns a loaded face.</summary>
    /// <param name="faceId">A face id.</param>
    /// <returns>The face.</returns>
    /// <exception cref="FontNotFoundException">No such face.</exception>
    public FontFace Face(int faceId) =>
        (uint)faceId < (uint)_faces.Count ? _faces[faceId] : throw new FontNotFoundException($"No face with id {faceId}.");

    internal FontStack Resolve(string family, int weight, bool italic)
    {
        if (_faces.Count == 0)
        {
            throw new FontNotFoundException("The font library holds no faces; add a font before laying out text.");
        }

        if (string.IsNullOrWhiteSpace(family))
        {
            return _default ??= Build(string.Empty, 400, false);
        }

        weight = Math.Clamp(weight, 1, 1000);
        var key = (family, weight, italic);
        if (!_stacks.TryGetValue(key, out var stack))
        {
            if (_stacks.Count > 512)
            {
                _stacks.Clear();
            }

            _stacks[key] = stack = Build(family, weight, italic);
        }

        return stack;
    }

    /// <summary>Gets a number that grows whenever faces, fallbacks or aliases change; caches built from the library check it.</summary>
    internal int Version { get; private set; }

    private void Changed()
    {
        Version++;
        _stacks.Clear();
        _default = null;
    }

    private FontStack Build(string familyList, int weight, bool italic)
    {
        var ids = new List<int>();
        foreach (var raw in familyList.Split(','))
        {
            var name = raw.Trim().Trim('"', '\'').Trim();
            if (name.Length == 0)
            {
                continue;
            }

            if (_aliases.TryGetValue(name, out var alias))
            {
                name = alias;
            }

            if (_families.TryGetValue(name, out var candidates))
            {
                AddUnique(ids, Closest(candidates, weight, italic));
            }
        }

        if (ids.Count == 0)
        {
            // Nothing named is loaded: the first family added is the default.
            AddUnique(ids, Closest(_families[_firstFamily!], weight, italic));
        }

        foreach (var f in _fallbacks)
        {
            AddUnique(ids, f);
        }

        if (ids.Count > FontFallbackChain.MaxFaces)
        {
            ids.RemoveRange(FontFallbackChain.MaxFaces, ids.Count - FontFallbackChain.MaxFaces);
        }

        return new FontStack(this, [.. ids]);
    }

    private static void AddUnique(List<int> ids, int id)
    {
        if (!ids.Contains(id))
        {
            ids.Add(id);
        }
    }

    // CSS Fonts 4 §5.2 in short: prefer the requested slant, then the nearest weight (ties go to the heavier face for weights above 400).
    private int Closest(List<int> candidates, int weight, bool italic)
    {
        var best = candidates[0];
        var bestScore = long.MaxValue;
        foreach (var id in candidates)
        {
            var f = _faces[id];
            long score = (f.IsItalic == italic ? 0 : 100_000) + (Math.Abs(f.Weight - weight) * 2) + (f.Weight < weight && weight > 400 ? 1 : 0);
            if (score < bestScore)
            {
                bestScore = score;
                best = id;
            }
        }

        return best;
    }
}
