using OneGame.Stores;

namespace Solitaire;

/// <summary>Where the game keeps its small files: the profile (settings and statistics) and the saved game.</summary>
public interface IDataStore
{
    /// <summary>Where the files live, for the settings screen and logs.</summary>
    string Description { get; }

    /// <summary>Reads a file. Returns false when it does not exist, is unreadable or is larger than <paramref name="maxBytes"/>.</summary>
    bool TryRead(string name, int maxBytes, out byte[] data);

    /// <summary>Writes a file, replacing any earlier one. Returns false when it could not be written.</summary>
    bool Write(string name, ReadOnlySpan<byte> data);

    /// <summary>Deletes a file if it exists.</summary>
    void Delete(string name);
}

/// <summary>Files kept in a folder on disk. A write goes to a temporary file first, so a crash never leaves half a save.</summary>
public sealed class LocalFileStore : IDataStore
{
    private readonly string _directory;

    /// <summary>Creates a store in <paramref name="directory"/>; the folder is made on the first write.</summary>
    public LocalFileStore(string directory)
    {
        ArgumentException.ThrowIfNullOrEmpty(directory);
        _directory = directory;
    }

    /// <summary>The folder the game uses when nothing else is configured: <c>solitaire</c> under the user's local application data.</summary>
    public static string DefaultDirectory() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "solitaire");

    /// <inheritdoc />
    public string Description => "file " + _directory;

    /// <inheritdoc />
    public bool TryRead(string name, int maxBytes, out byte[] data)
    {
        data = [];
        try
        {
            var path = PathOf(name);
            var info = new FileInfo(path);
            if (!info.Exists || info.Length > maxBytes)
            {
                return false;
            }

            data = File.ReadAllBytes(path);
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
        try
        {
            Directory.CreateDirectory(_directory);
            var path = PathOf(name);
            var temp = path + ".tmp";
            File.WriteAllBytes(temp, data.ToArray());
            File.Move(temp, path, overwrite: true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public void Delete(string name)
    {
        try
        {
            File.Delete(PathOf(name));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Nothing to delete, or not ours to delete: either way the file is not going to be read as a save.
        }
    }

    private string PathOf(string name) => Path.Combine(_directory, name);
}

/// <summary>Files held in memory only. Headless runs use it so a test never reads or changes the player's own save.</summary>
public sealed class MemoryStore : IDataStore
{
    private readonly Dictionary<string, byte[]> _files = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public string Description => "memory";

    /// <inheritdoc />
    public bool TryRead(string name, int maxBytes, out byte[] data)
    {
        if (_files.TryGetValue(name, out var bytes) && bytes.Length <= maxBytes)
        {
            data = bytes;
            return true;
        }

        data = [];
        return false;
    }

    /// <inheritdoc />
    public bool Write(string name, ReadOnlySpan<byte> data)
    {
        _files[name] = data.ToArray();
        return true;
    }

    /// <inheritdoc />
    public void Delete(string name) => _files.Remove(name);
}

/// <summary>Files kept in the store's cloud saves (Steam Cloud when the game runs under Steam).</summary>
public sealed class CloudStore : IDataStore
{
    private readonly IStorePlatform _store;

    /// <summary>Creates a store over a platform whose cloud is available.</summary>
    public CloudStore(IStorePlatform store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
    }

    /// <inheritdoc />
    public string Description => _store.Name + " cloud";

    /// <inheritdoc />
    public bool TryRead(string name, int maxBytes, out byte[] data)
    {
        data = [];
        return _store.Cloud.Exists(name) && _store.Cloud.Read(name, out data, maxBytes) == StoreResult.Ok;
    }

    /// <inheritdoc />
    public bool Write(string name, ReadOnlySpan<byte> data) => _store.Cloud.Write(name, data) == StoreResult.Ok;

    /// <inheritdoc />
    public void Delete(string name) => _store.Cloud.Delete(name);
}

/// <summary>Chooses where the game keeps its files.</summary>
public static class DataStores
{
    /// <summary>
    /// Cloud saves when the store is running and its cloud is switched on; otherwise a folder on disk. A cloud write that fails
    /// is not retried locally: the next write tries the cloud again.
    /// </summary>
    public static IDataStore Choose(IStorePlatform? store, string localDirectory) =>
        store is { IsAvailable: true, Cloud.IsEnabled: true } ? new CloudStore(store) : new LocalFileStore(localDirectory);
}

/// <summary>The things the game makes a sound about.</summary>
public enum SoundEvent
{
    /// <summary>A card was placed.</summary>
    Card,

    /// <summary>Cards were turned from the stock.</summary>
    Draw,

    /// <summary>A move was refused.</summary>
    Refuse,

    /// <summary>The game was won.</summary>
    Win,

    /// <summary>A menu button was used.</summary>
    Click,
}

/// <summary>Where the game sends its sounds. An audio-backed implementation plays them; the default is silent.</summary>
public interface ISoundSink
{
    /// <summary>Plays a sound at a volume from 0 to 1.</summary>
    void Play(SoundEvent sound, float volume);

    /// <summary>Sets the music and effects volumes, 0 to 1.</summary>
    void SetVolumes(float music, float effects)
    {
    }
}

/// <summary>Makes no sound.</summary>
public sealed class SilentSounds : ISoundSink
{
    /// <inheritdoc />
    public void Play(SoundEvent sound, float volume)
    {
    }
}

/// <summary>Remembers what was played, for tests and for the harness.</summary>
public sealed class RecordingSounds : ISoundSink
{
    private readonly List<(SoundEvent Sound, float Volume)> _played = [];

    /// <summary>Every sound played so far, oldest first.</summary>
    public IReadOnlyList<(SoundEvent Sound, float Volume)> Played => _played;

    /// <inheritdoc />
    public void Play(SoundEvent sound, float volume) => _played.Add((sound, volume));
}
