using OneGame.Storage;
using OneGame.Stores;

namespace Solitaire;

/// <summary>Files kept in the store's cloud saves (Steam Cloud when the game runs under Steam).</summary>
public sealed class CloudStore : IByteStore
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
        StoreNames.Require(name);
        data = [];
        return _store.Cloud.Exists(name) && _store.Cloud.Read(name, out data, maxBytes) == StoreResult.Ok;
    }

    /// <inheritdoc />
    public bool Write(string name, ReadOnlySpan<byte> data)
    {
        StoreNames.Require(name);
        return _store.Cloud.Write(name, data) == StoreResult.Ok;
    }

    /// <inheritdoc />
    public bool Delete(string name)
    {
        StoreNames.Require(name);
        return _store.Cloud.Delete(name) is StoreResult.Ok or StoreResult.NotFound;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> List(string prefix = "")
    {
        var names = new List<string>();
        foreach (var file in _store.Cloud.List())
        {
            if (StoreNames.IsValid(file.Name) && file.Name.StartsWith(prefix, StringComparison.Ordinal))
            {
                names.Add(file.Name);
            }
        }

        names.Sort(StringComparer.Ordinal);
        return names;
    }
}

/// <summary>Chooses where the game keeps its files.</summary>
public static class DataStores
{
    /// <summary>
    /// Cloud saves when the store is running and its cloud is switched on; otherwise a folder on disk. A cloud write that fails
    /// is not retried locally: the next write tries the cloud again.
    /// </summary>
    public static IByteStore Choose(IStorePlatform? store, string localDirectory) => Choose(store, new FolderByteStore(localDirectory));

    /// <summary>As above, with any store as the local one (a folder, the browser's storage).</summary>
    public static IByteStore Choose(IStorePlatform? store, IByteStore local) =>
        store is { IsAvailable: true, Cloud.IsEnabled: true } ? new CloudStore(store) : local;
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
