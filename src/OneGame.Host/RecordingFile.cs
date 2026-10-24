using OneGame.Core;
using OneGame.Sync;

namespace OneGame.Host;

/// <summary>Reads and writes recording files. A recording file is untrusted input when read.</summary>
public static class RecordingFile
{
    private const long MaxFileBytes = 256L * 1024 * 1024;

    /// <summary>Writes a recording, creating the folder when needed.</summary>
    public static void Save(Recording recording, string path)
    {
        ArgumentNullException.ThrowIfNull(recording);
        var dir = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllBytes(path, recording.ToBytes());
    }

    /// <summary>Reads a recording file.</summary>
    /// <exception cref="FileNotFoundException">There is no such file.</exception>
    /// <exception cref="InvalidDataException">The file is too large, or is not a recording this build reads.</exception>
    public static Recording Load(string path)
    {
        var info = new FileInfo(path);
        if (!info.Exists)
        {
            throw new FileNotFoundException($"No recording at {path}.", path);
        }

        if (info.Length > MaxFileBytes)
        {
            throw new InvalidDataException("The recording file is too large.");
        }

        var error = Recording.TryRead(File.ReadAllBytes(path), out var recording);
        return error == SyncError.None && recording is not null
            ? recording
            : throw new InvalidDataException($"{path} is not a readable recording: {error}.");
    }
}
