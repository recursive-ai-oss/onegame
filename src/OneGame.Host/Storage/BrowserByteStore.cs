using System.Runtime.Versioning;
using OneGame.Platform;
using OneGame.Storage;

namespace OneGame.Host;

/// <summary>
/// The browser's IndexedDB as an <see cref="IByteStore"/>. The page loads every saved file before the program starts, so reads and
/// writes here are synchronous; a write reaches IndexedDB in one atomic transaction right after (<see cref="Pending"/>).
/// </summary>
[SupportedOSPlatform("browser")]
public sealed class BrowserByteStore : IByteStore
{
    private readonly string _prefix;

    /// <summary>Creates the store for a game; its files are kept apart from other games served from the same address.</summary>
    public BrowserByteStore(string appName) => _prefix = StoreNames.Sanitize(appName) + "/";

    /// <inheritdoc />
    public string Description => BrowserFiles.Kind == "indexeddb" ? "browser storage" : "memory (browser storage unavailable: " + BrowserFiles.Why + ")";

    /// <summary>Writes the browser has not committed yet. A save is safe across a reload once this is 0.</summary>
    public static int Pending => BrowserFiles.Pending;

    /// <inheritdoc />
    public bool TryRead(string name, int maxBytes, out byte[] data)
    {
        StoreNames.Require(name);
        data = [];
        var size = BrowserFiles.Size(_prefix + name);
        if (size < 0 || size > maxBytes)
        {
            return false;
        }

        var bytes = new byte[size];
        if (!BrowserFiles.Read(_prefix + name, bytes))
        {
            return false;
        }

        data = bytes;
        return true;
    }

    /// <inheritdoc />
    public bool Write(string name, ReadOnlySpan<byte> data)
    {
        StoreNames.Require(name);
        BrowserFiles.Write(_prefix + name, data);
        return true;
    }

    /// <inheritdoc />
    public bool Delete(string name)
    {
        StoreNames.Require(name);
        BrowserFiles.Delete(_prefix + name);
        return true;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> List(string prefix = "")
    {
        var names = new List<string>();
        foreach (var key in BrowserFiles.List(_prefix + prefix))
        {
            names.Add(key[_prefix.Length..]);
        }

        return names;
    }
}
