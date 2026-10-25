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

    // A session of several segments is a container: this magic, a version, a count, then each segment length-prefixed in the
    // single-recording format. A session of one segment is written as that plain recording, so older tools still read it.
    private const uint SessionMagic = 0x5352474F; // "OGRS" little-endian
    private const uint SessionVersion = 1;
    private const int MaxSegments = 1_000_000;

    /// <summary>Writes a session, creating the folder when needed.</summary>
    public static void Save(SessionRecording session, string path)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.Segments.Count == 1)
        {
            Save(session.Segments[0], path);
            return;
        }

        var dir = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        using var ms = new MemoryStream();
        Span<byte> word = stackalloc byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(word, SessionMagic);
        ms.Write(word);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(word, SessionVersion);
        ms.Write(word);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(word, (uint)session.Segments.Count);
        ms.Write(word);
        foreach (var segment in session.Segments)
        {
            var bytes = segment.ToBytes();
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(word, checked((uint)bytes.Length));
            ms.Write(word);
            ms.Write(bytes);
        }

        File.WriteAllBytes(path, ms.ToArray());
    }

    /// <summary>Reads a session file: either a session container or a plain single recording (one segment).</summary>
    /// <exception cref="FileNotFoundException">There is no such file.</exception>
    /// <exception cref="InvalidDataException">The file is too large, or is not a recording this build reads.</exception>
    public static SessionRecording LoadSession(string path)
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

        var data = File.ReadAllBytes(path);
        if (data.Length < 12 || System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(data) != SessionMagic)
        {
            return new SessionRecording([Load(path)]);
        }

        var version = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(4));
        var count = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(8));
        if (version != SessionVersion || count == 0 || count > MaxSegments)
        {
            throw new InvalidDataException($"{path} is not a readable recording: unsupported session header.");
        }

        var segments = new List<Recording>();
        var offset = 12;
        for (uint i = 0; i < count; i++)
        {
            if (data.Length - offset < 4)
            {
                throw new InvalidDataException($"{path} is not a readable recording: truncated at segment {i}.");
            }

            var length = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset));
            offset += 4;
            if (length > (uint)(data.Length - offset))
            {
                throw new InvalidDataException($"{path} is not a readable recording: segment {i} runs past the end of the file.");
            }

            var error = Recording.TryRead(data.AsSpan(offset, (int)length), out var recording);
            if (error != SyncError.None || recording is null)
            {
                throw new InvalidDataException($"{path} is not a readable recording: segment {i}: {error}.");
            }

            segments.Add(recording);
            offset += (int)length;
        }

        return offset == data.Length ? new SessionRecording(segments) : throw new InvalidDataException($"{path} is not a readable recording: trailing bytes.");
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
