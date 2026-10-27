using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;

namespace OneGame.Platform;

/// <summary>
/// The browser's persistent file area, a flat map of names to bytes kept in IndexedDB. The page script loads it into memory before the
/// program starts (<c>storageOpen</c>), so every call here is synchronous. A write is visible at once and reaches IndexedDB in one
/// atomic transaction; <see cref="Pending"/> counts the ones still on their way. Games use it through <c>OneGame.Storage.IByteStore</c>.
/// </summary>
[SupportedOSPlatform("browser")]
public static partial class BrowserFiles
{
    private const string Module = "onegame-platform";

    /// <summary>"indexeddb" when files survive a reload, "memory" when the page could not open IndexedDB (private windows, blocked storage).</summary>
    public static string Kind => Status().Kind;

    /// <summary>Why IndexedDB is not in use, or the last transaction failure; empty when nothing went wrong.</summary>
    public static string Why => Status().Why;

    /// <summary>Writes that have not been committed to IndexedDB yet. Wait for 0 before closing the page when a save must survive.</summary>
    public static int Pending => Status().Pending;

    /// <summary>Writes IndexedDB refused (quota, a closed database). The files stay readable until the page reloads.</summary>
    public static int Failed => Status().Failed;

    /// <summary>The size of a file, or -1 when there is none.</summary>
    public static int Size(string key) => KvSize(key);

    /// <summary>Copies a file into <paramref name="destination"/>, which must be exactly <see cref="Size"/> bytes long.</summary>
    public static bool Read(string key, Span<byte> destination) => KvRead(key, destination) == destination.Length;

    /// <summary>Stores a file, replacing any earlier one.</summary>
    public static void Write(string key, ReadOnlySpan<byte> data) => KvWrite(key, data.ToArray());

    /// <summary>Removes a file.</summary>
    public static void Delete(string key) => KvDelete(key);

    /// <summary>The keys that start with <paramref name="prefix"/>, in ordinal order.</summary>
    public static string[] List(string prefix)
    {
        var text = KvList(prefix);
        return text.Length == 0 ? [] : text.Split('\n');
    }

    private static (string Kind, int Pending, int Failed, string Why) Status()
    {
        using var doc = JsonDocument.Parse(Encoding.UTF8.GetBytes(KvStatus()));
        var r = doc.RootElement;
        return (r.GetProperty("kind").GetString() ?? "memory", r.GetProperty("pending").GetInt32(), r.GetProperty("failed").GetInt32(), r.GetProperty("why").GetString() ?? string.Empty);
    }

    [JSImport("kvSize", Module)]
    private static partial int KvSize(string key);

    [JSImport("kvRead", Module)]
    private static partial int KvRead(string key, [JSMarshalAs<JSType.MemoryView>] Span<byte> destination);

    [JSImport("kvWrite", Module)]
    private static partial void KvWrite(string key, [JSMarshalAs<JSType.MemoryView>] Span<byte> data);

    [JSImport("kvDelete", Module)]
    private static partial void KvDelete(string key);

    [JSImport("kvList", Module)]
    private static partial string KvList(string prefix);

    [JSImport("kvStatus", Module)]
    private static partial string KvStatus();
}
