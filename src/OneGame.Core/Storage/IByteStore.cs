namespace OneGame.Storage;

/// <summary>
/// A flat folder of named byte blobs: the low level under save slots and settings. A store is one place a game keeps its files for a
/// player: a folder on disk, the browser's IndexedDB, an in-memory map for tests. Names are plain (see <see cref="StoreNames"/>).
/// Stores never throw for a missing, unreadable or unwritable file; they answer false so the game can carry on and say so.
/// </summary>
public interface IByteStore
{
    /// <summary>Where the files live, for settings screens and logs.</summary>
    string Description { get; }

    /// <summary>
    /// Reads a file. Returns false when it does not exist, is unreadable or is larger than <paramref name="maxBytes"/>. The size is
    /// checked before the bytes are copied, so a huge or hostile file costs no memory.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid store name.</exception>
    bool TryRead(string name, int maxBytes, out byte[] data);

    /// <summary>
    /// Writes a file, replacing any earlier one. The replacement is atomic: after a crash or a failure the file holds either the
    /// whole old content or the whole new content, never a mix. Returns false when it could not be written (the old file stays).
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid store name.</exception>
    bool Write(string name, ReadOnlySpan<byte> data);

    /// <summary>Deletes a file. Returns true when the file is gone afterwards (including when it never existed).</summary>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid store name.</exception>
    bool Delete(string name);

    /// <summary>The names of the files that start with <paramref name="prefix"/>, in ordinal order. Half-written leftovers are not listed.</summary>
    IReadOnlyList<string> List(string prefix = "");
}

/// <summary>The rules for file names in an <see cref="IByteStore"/>: ASCII letters, digits, <c>.</c>, <c>_</c> and <c>-</c>, up to 96 characters.</summary>
public static class StoreNames
{
    /// <summary>The longest name.</summary>
    public const int MaxLength = 96;

    /// <summary>The ending the folder store uses for a file being written; no valid name ends with it.</summary>
    internal const string TempSuffix = ".tmp";

    /// <summary>True when <paramref name="name"/> is a valid file name: no path separators, no leading dot, not a temporary name.</summary>
    public static bool IsValid(string? name)
    {
        if (string.IsNullOrEmpty(name) || name.Length > MaxLength || name[0] == '.' || name.EndsWith(TempSuffix, StringComparison.Ordinal))
        {
            return false;
        }

        foreach (var c in name)
        {
            if (!(char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or '-'))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Throws <see cref="ArgumentException"/> unless <paramref name="name"/> is valid.</summary>
    public static void Require(string? name)
    {
        if (!IsValid(name))
        {
            throw new ArgumentException($"'{name}' is not a valid file name for a store: use up to {MaxLength} ASCII letters, digits, '.', '_' or '-', not starting with '.' and not ending in '{TempSuffix}'.", nameof(name));
        }
    }

    /// <summary>Turns any text into a valid name part by replacing other characters with <c>_</c> (for a game's name in a folder).</summary>
    /// <param name="text">Any non-empty text.</param>
    /// <returns>The text with each disallowed character replaced.</returns>
    public static string Sanitize(string text)
    {
        ArgumentException.ThrowIfNullOrEmpty(text);
        return string.Create(text.Length, text, static (span, source) =>
        {
            for (var i = 0; i < span.Length; i++)
            {
                var c = source[i];
                span[i] = char.IsAsciiLetterOrDigit(c) || c is '_' or '-' ? c : '_';
            }
        });
    }
}
