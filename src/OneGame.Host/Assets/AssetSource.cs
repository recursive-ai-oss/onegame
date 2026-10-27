namespace OneGame.Host;

/// <summary>Where an <see cref="Assets"/> service reads content from. The default reads files; a platform whose content is not a folder supplies its own.</summary>
public interface IAssetSource
{
    /// <summary>Reads a whole file.</summary>
    /// <param name="path">The path relative to the content folders, with <c>/</c> separators.</param>
    /// <param name="maxBytes">The most to read; a larger file must be refused before anything that size is allocated.</param>
    /// <returns>The bytes.</returns>
    /// <exception cref="AssetException"><c>not_found</c>, <c>bad_path</c> or <c>too_large</c>.</exception>
    byte[] Read(string path, long maxBytes);

    /// <summary>The file on disk behind a path, or null when there is none (or the content is not a file). Development reloads watch these.</summary>
    /// <param name="path">The path relative to the content folders.</param>
    /// <returns>An absolute path or null.</returns>
    string? PhysicalPath(string path);
}

/// <summary>
/// Reads content from folders. The default roots are the program's folder (an export keeps <c>art/</c> and <c>data/</c> next to the program),
/// <c>../Resources</c> (a macOS bundle) and the working directory (development, from the project folder), tried in that order.
/// Paths are relative to a root and may not leave it.
/// </summary>
public sealed class FileAssetSource : IAssetSource
{
    private readonly string[] _roots;

    /// <summary>Creates a source over the default roots.</summary>
    public FileAssetSource()
        : this(DefaultRoots())
    {
    }

    /// <summary>Creates a source over the given roots, tried in order.</summary>
    /// <param name="roots">Folders that hold the content folders.</param>
    public FileAssetSource(IEnumerable<string> roots)
    {
        ArgumentNullException.ThrowIfNull(roots);
        _roots = [.. roots.Select(r => Path.GetFullPath(r))];
    }

    /// <summary>The roots, in the order tried.</summary>
    public IReadOnlyList<string> Roots => _roots;

    private static string[] DefaultRoots() =>
    [
        AppContext.BaseDirectory,
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "Resources")),
        Directory.GetCurrentDirectory(),
    ];

    /// <inheritdoc />
    public string? PhysicalPath(string path)
    {
        var (rel, _) = Normalize(path);
        foreach (var root in _roots)
        {
            var full = Path.GetFullPath(Path.Combine(root, rel));
            if (IsInside(root, full) && File.Exists(full))
            {
                return full;
            }
        }

        return null;
    }

    /// <inheritdoc />
    public byte[] Read(string path, long maxBytes)
    {
        var full = PhysicalPath(path) ?? throw new AssetException("not_found", path, $"No such file under {string.Join(", ", _roots)}. Paths are relative to a content folder, like art/ship.png.");
        using var stream = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        if (stream.Length > maxBytes)
        {
            throw new AssetException("too_large", path, $"The file is {stream.Length} bytes; the limit for this kind is {maxBytes}.");
        }

        var buffer = new byte[(int)stream.Length];
        stream.ReadExactly(buffer);
        return buffer;
    }

    private static (string Rel, string Name) Normalize(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        var rel = path.Replace('\\', '/');
        if (rel.Length == 0 || Path.IsPathRooted(rel) || rel.Split('/').Any(p => p == ".."))
        {
            throw new AssetException("bad_path", path, "Use a path relative to the content folders, like art/ship.png, without '..'.");
        }

        return (rel, rel);
    }

    private static bool IsInside(string root, string full) =>
        full.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.Ordinal);
}
