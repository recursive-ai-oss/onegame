namespace OneGame.Storage;

/// <summary>
/// Files held in memory only. Headless runs and tests use it so a run never reads or changes a player's own files, and a game can hand
/// one to a second <see cref="SettingsFile{T}"/> or slot list to model "a fresh process" by sharing the same instance.
/// </summary>
public sealed class MemoryByteStore : IByteStore
{
    private readonly Dictionary<string, byte[]> _files = new(StringComparer.Ordinal);

    /// <summary>Creates an empty store.</summary>
    public MemoryByteStore()
    {
    }

    /// <inheritdoc />
    public string Description => "memory";

    /// <summary>How many files the store holds.</summary>
    public int Count => _files.Count;

    /// <inheritdoc />
    public bool TryRead(string name, int maxBytes, out byte[] data)
    {
        StoreNames.Require(name);
        if (_files.TryGetValue(name, out var bytes) && bytes.Length <= maxBytes)
        {
            data = (byte[])bytes.Clone();
            return true;
        }

        data = [];
        return false;
    }

    /// <inheritdoc />
    public bool Write(string name, ReadOnlySpan<byte> data)
    {
        StoreNames.Require(name);
        _files[name] = data.ToArray();
        return true;
    }

    /// <inheritdoc />
    public bool Delete(string name)
    {
        StoreNames.Require(name);
        _files.Remove(name);
        return true;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> List(string prefix = "")
    {
        var names = new List<string>();
        foreach (var name in _files.Keys)
        {
            if (name.StartsWith(prefix, StringComparison.Ordinal))
            {
                names.Add(name);
            }
        }

        names.Sort(StringComparer.Ordinal);
        return names;
    }
}
