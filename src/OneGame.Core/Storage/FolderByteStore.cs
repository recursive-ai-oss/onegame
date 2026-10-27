using System.IO;

namespace OneGame.Storage;

/// <summary>
/// Files kept in a folder on disk: the store for desktop, Android and iOS (where the folder is the app's own storage). A write goes to
/// a temporary file in the same folder, is flushed to the device, and then replaces the real file in one rename, so a crash or a full
/// disk never leaves half a file under the real name.
/// </summary>
public sealed class FolderByteStore : IByteStore
{
    private readonly string _directory;

    /// <summary>Creates a store in <paramref name="directory"/>; the folder is made on the first write.</summary>
    public FolderByteStore(string directory)
    {
        ArgumentException.ThrowIfNullOrEmpty(directory);
        _directory = directory;
    }

    /// <summary>
    /// The folder a game uses by default: <paramref name="appName"/> under the user's local application data
    /// (<c>~/.local/share</c>, <c>%LocalAppData%</c>, <c>~/Library/Application Support</c>, or the app's sandbox on a phone).
    /// </summary>
    public static string DefaultDirectory(string appName) =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), StoreNames.Sanitize(appName));

    /// <summary>The folder holding the files.</summary>
    public string Folder => _directory;

    /// <summary>A test seam: called after the temporary file is complete and before it replaces the real one (where a crash would land).</summary>
    internal Action<string>? BeforeCommit { get; set; }

    /// <inheritdoc />
    public string Description => "file " + _directory;

    /// <inheritdoc />
    public bool TryRead(string name, int maxBytes, out byte[] data)
    {
        StoreNames.Require(name);
        data = [];
        try
        {
            var path = Path.Combine(_directory, name);
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            var length = stream.Length;
            if (length > maxBytes || length > Array.MaxLength)
            {
                return false;
            }

            var bytes = new byte[(int)length];
            stream.ReadExactly(bytes);
            data = bytes;
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public bool Write(string name, ReadOnlySpan<byte> data)
    {
        StoreNames.Require(name);
        var temp = Path.Combine(_directory, name + StoreNames.TempSuffix);
        try
        {
            System.IO.Directory.CreateDirectory(_directory);
            using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(data);
                stream.Flush(flushToDisk: true);
            }

            BeforeCommit?.Invoke(temp);
            File.Move(temp, Path.Combine(_directory, name), overwrite: true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            TryDelete(temp);
            return false;
        }
    }

    /// <inheritdoc />
    public bool Delete(string name)
    {
        StoreNames.Require(name);
        try
        {
            File.Delete(Path.Combine(_directory, name));
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<string> List(string prefix = "")
    {
        var names = new List<string>();
        try
        {
            if (System.IO.Directory.Exists(_directory))
            {
                foreach (var path in System.IO.Directory.EnumerateFiles(_directory))
                {
                    var name = Path.GetFileName(path);
                    if (StoreNames.IsValid(name) && name.StartsWith(prefix, StringComparison.Ordinal))
                    {
                        names.Add(name);
                    }
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // An unreadable folder lists as empty: nothing in it can be read either.
        }

        names.Sort(StringComparer.Ordinal);
        return names;
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A leftover temporary file is never read or listed; the next write replaces it.
        }
    }
}
