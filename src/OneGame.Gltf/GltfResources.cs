namespace OneGame.Gltf;

/// <summary>Supplies the external files (buffers and images) a <c>.gltf</c> names.</summary>
public interface IGltfResources
{
    /// <summary>Reads a file. The uri has been checked by <see cref="GltfUri.CheckRelative"/> already.</summary>
    /// <param name="relativeUri">The decoded relative path, with forward slashes.</param>
    /// <param name="maxBytes">The most bytes the caller accepts; reading more must fail rather than allocate.</param>
    /// <returns>The bytes, or null when the file does not exist.</returns>
    byte[]? Read(string relativeUri, long maxBytes);
}

/// <summary>Reads external files from a folder and refuses to leave it.</summary>
public sealed class GltfFolderResources : IGltfResources
{
    private readonly string _root;

    /// <summary>Creates a resolver rooted at a folder.</summary>
    /// <param name="root">The folder that holds the .gltf file.</param>
    public GltfFolderResources(string root)
    {
        ArgumentNullException.ThrowIfNull(root);
        _root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root)) + Path.DirectorySeparatorChar;
    }

    /// <inheritdoc/>
    public byte[]? Read(string relativeUri, long maxBytes)
    {
        var full = Path.GetFullPath(Path.Combine(_root, relativeUri));
        if (!full.StartsWith(_root, StringComparison.Ordinal))
        {
            throw new GltfException("uri_not_allowed", $"'{relativeUri}' leaves the model's folder.");
        }

        var info = new FileInfo(full);
        if (!info.Exists)
        {
            return null;
        }

        if (info.Length > maxBytes)
        {
            throw new GltfException("limit_exceeded", $"'{relativeUri}' is {info.Length} bytes; the limit is {maxBytes}.");
        }

        return File.ReadAllBytes(full);
    }
}

/// <summary>The uri rules: data uris are decoded in memory, everything else must be a plain relative path.</summary>
public static class GltfUri
{
    /// <summary>Decodes percent escapes and refuses anything that could reach outside the model's folder.</summary>
    /// <param name="uri">The uri as written in the file.</param>
    /// <returns>The relative path with forward slashes.</returns>
    /// <exception cref="GltfException"><c>uri_not_allowed</c> for a scheme, an absolute path, a drive, a NUL, a backslash or a <c>..</c> segment.</exception>
    public static string CheckRelative(string uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        if (uri.Length == 0 || uri.Length > 1024)
        {
            throw new GltfException("uri_not_allowed", "The uri is empty or too long.");
        }

        string decoded;
        try
        {
            decoded = Uri.UnescapeDataString(uri);
        }
        catch (UriFormatException)
        {
            throw new GltfException("uri_not_allowed", $"'{Show(uri)}' is not a valid uri.");
        }

        // Escapes are decoded once; a second pass would turn %252e into a dot after the checks, so what is checked is what is used.
        if (decoded.Contains('\0', StringComparison.Ordinal) || decoded.Contains('\\', StringComparison.Ordinal)
            || decoded.Contains(':', StringComparison.Ordinal) || decoded[0] == '/' || decoded.Contains('?', StringComparison.Ordinal) || decoded.Contains('#', StringComparison.Ordinal))
        {
            throw new GltfException("uri_not_allowed", $"'{Show(uri)}' is not a plain relative path (no scheme, drive, absolute path, backslash, query or fragment).");
        }

        foreach (var segment in decoded.Split('/'))
        {
            if (segment == "..")
            {
                throw new GltfException("uri_not_allowed", $"'{Show(uri)}' climbs out of the model's folder.");
            }
        }

        return decoded;
    }

    internal static string Show(string s) => s.Length <= 80 ? s : s[..80] + "...";

    /// <summary>True when the text starts a data uri.</summary>
    internal static bool IsData(string uri) => uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase);

    /// <summary>Decodes a base64 data uri, checking the decoded size from the text length before decoding.</summary>
    internal static byte[] DecodeData(string uri, long maxBytes, Budget budget)
    {
        var comma = uri.IndexOf(',', StringComparison.Ordinal);
        if (comma < 0 || !uri.AsSpan(0, comma).EndsWith(";base64", StringComparison.OrdinalIgnoreCase))
        {
            throw new GltfException("uri_not_allowed", "Only base64 data uris are supported.");
        }

        var text = uri.AsSpan(comma + 1);
        var upper = ((long)text.Length / 4 * 3) + 3;
        if (upper > maxBytes + 6)
        {
            throw new GltfException("limit_exceeded", $"A data uri decodes to about {upper} bytes; the limit is {maxBytes}.");
        }

        budget.Charge(upper, "data uri");
        var buffer = new byte[(int)upper];
        if (!Convert.TryFromBase64Chars(text, buffer, out var written))
        {
            throw new GltfException("uri_not_allowed", "The data uri is not valid base64.");
        }

        if (written > maxBytes)
        {
            throw new GltfException("limit_exceeded", $"A data uri decodes to {written} bytes; the limit is {maxBytes}.");
        }

        return written == buffer.Length ? buffer : buffer.AsSpan(0, written).ToArray();
    }
}

/// <summary>Counts what the importer allocates and refuses to pass the limit.</summary>
internal sealed class Budget(long limit)
{
    private long _used;

    internal long Used => _used;

    internal void Charge(long bytes, string what)
    {
        if (bytes < 0 || _used + bytes > limit || _used + bytes < 0)
        {
            throw new GltfException("limit_exceeded", $"{what} would need {bytes} bytes; {_used} of the {limit} byte output limit are already used.");
        }

        _used += bytes;
    }
}
