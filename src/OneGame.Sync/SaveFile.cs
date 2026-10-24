using System.Globalization;

namespace OneGame.Sync;

/// <summary>The schema version a save holds for one entity type.</summary>
/// <param name="TypeId">The entity type id.</param>
/// <param name="TypeName">The type's name.</param>
/// <param name="Version">The schema version it was written with.</param>
public readonly record struct SchemaEntry(uint TypeId, string TypeName, uint Version);

/// <summary>The fixed part of a save file.</summary>
/// <param name="Format">The save container format version.</param>
/// <param name="Seed">The world's seed.</param>
/// <param name="Tick">The completed tick count.</param>
/// <param name="WorldHash">The world hash when saved.</param>
/// <param name="PayloadHash">Hash of the snapshot bytes, to catch damaged files.</param>
/// <param name="Schemas">Schema version of every entity type present, by type id.</param>
public sealed record SaveHeader(uint Format, ulong Seed, long Tick, ulong WorldHash, ulong PayloadHash, IReadOnlyList<SchemaEntry> Schemas);

/// <summary>How loading a save ended.</summary>
public enum SaveStatus
{
    /// <summary>Loaded.</summary>
    Ok = 0,

    /// <summary>The file is not a save, or is cut short.</summary>
    BadFile,

    /// <summary>The file's container format is newer than this build reads.</summary>
    UnsupportedFormat,

    /// <summary>The payload does not match its recorded hash: the file is damaged.</summary>
    Corrupt,

    /// <summary>The save was made with another world seed than the world it is loaded into.</summary>
    SeedMismatch,

    /// <summary>The snapshot could not be decoded (unknown type, a schema with no migration, bad data).</summary>
    RestoreFailed,

    /// <summary>The loaded state does not hash to the recorded hash.</summary>
    HashMismatch,
}

/// <summary>What <see cref="SaveFile.Load"/> found and did.</summary>
/// <param name="Status">The outcome.</param>
/// <param name="Detail">The decoder's error, when there was one.</param>
/// <param name="Header">The header, if it could be read.</param>
/// <param name="Migrated">True when at least one entity type was written with an older schema and went through its migration.</param>
/// <param name="HashVerified">True when the loaded world hashed to the recorded hash. Not checkable after a migration: the state layout changed.</param>
public sealed record LoadResult(SaveStatus Status, SyncError Detail, SaveHeader? Header, bool Migrated, bool HashVerified);

/// <summary>Save files: a header (format, seed, tick, hashes, schema versions) followed by a world snapshot.</summary>
public static class SaveFile
{
    private const uint Magic = 0x56534F47; // "OGSV" little-endian
    private const uint Format = 1;

    /// <summary>Serializes <paramref name="world"/>.</summary>
    public static byte[] Write(World world)
    {
        var payload = new SyncWriter(4096);
        world.WriteSnapshot(payload);

        var schemas = new List<SchemaEntry>();
        foreach (var e in world.Entities)
        {
            var found = false;
            foreach (var s in schemas)
            {
                found |= s.TypeId == e.TypeId;
            }

            if (!found)
            {
                schemas.Add(new SchemaEntry(e.TypeId, e.TypeName, e.SchemaVersion));
            }
        }

        schemas.Sort(static (a, b) => a.TypeId.CompareTo(b.TypeId));
        var w = new SyncWriter(payload.Length + 256);
        w.WriteFixed32(Magic);
        w.WriteVarUInt(Format);
        w.WriteFixed64(world.Seed);
        w.WriteVarInt(world.Tick);
        w.WriteFixed64(world.ComputeHash());
        w.WriteFixed64(StateHash.Of(payload.Written));
        w.WriteVarUInt((ulong)schemas.Count);
        foreach (var s in schemas)
        {
            w.WriteFixed32(s.TypeId);
            w.WriteString(s.TypeName);
            w.WriteVarUInt(s.Version);
        }

        w.WriteBytes(payload.Written);
        return w.ToArray();
    }

    /// <summary>Reads the header from untrusted bytes.</summary>
    /// <param name="bytes">The file.</param>
    /// <param name="header">The header, or null.</param>
    /// <param name="payload">The snapshot bytes (a view into <paramref name="bytes"/>).</param>
    /// <param name="detail">The decoder error behind a <see cref="SaveStatus.BadFile"/>.</param>
    public static SaveStatus TryReadHeader(ReadOnlySpan<byte> bytes, out SaveHeader? header, out ReadOnlySpan<byte> payload, out SyncError detail)
    {
        header = null;
        payload = default;
        detail = SyncError.None;
        var r = new SyncReader(bytes);
        var magic = r.ReadFixed32();
        var format = r.ReadVarUInt();
        if (!r.Ok || magic != Magic)
        {
            detail = r.Ok ? SyncError.BadHeader : r.Error;
            return SaveStatus.BadFile;
        }

        if (format != Format)
        {
            return SaveStatus.UnsupportedFormat;
        }

        var seed = r.ReadFixed64();
        var tick = r.ReadVarInt();
        var worldHash = r.ReadFixed64();
        var payloadHash = r.ReadFixed64();
        var count = r.ReadCount(SyncLimits.MaxNamed);
        var schemas = new List<SchemaEntry>(count);
        for (var i = 0; i < count && r.Ok; i++)
        {
            var typeId = r.ReadFixed32();
            var name = r.ReadString();
            var version = r.ReadVarUInt();
            if (version > uint.MaxValue)
            {
                r.Fail(SyncError.InvalidValue);
            }

            if (r.Ok)
            {
                schemas.Add(new SchemaEntry(typeId, name, (uint)version));
            }
        }

        var body = r.ReadBytes(int.MaxValue);
        r.ExpectEnd();
        if (!r.Ok || tick < 0)
        {
            detail = r.Ok ? SyncError.InvalidValue : r.Error;
            return SaveStatus.BadFile;
        }

        header = new SaveHeader(Format, seed, tick, worldHash, payloadHash, schemas);
        payload = body;
        return SaveStatus.Ok;
    }

    /// <summary>
    /// Replaces the contents of <paramref name="world"/> with a save, running migrations for entity types saved at an older schema.
    /// The world must be built like the saved one (same seed, tables and systems). After a failure the world is unspecified: discard it.
    /// </summary>
    public static LoadResult Load(ReadOnlySpan<byte> save, World world)
    {
        var status = TryReadHeader(save, out var header, out var payload, out var detail);
        if (status != SaveStatus.Ok)
        {
            return new LoadResult(status, detail, null, false, false);
        }

        if (StateHash.Of(payload) != header!.PayloadHash)
        {
            return new LoadResult(SaveStatus.Corrupt, SyncError.None, header, false, false);
        }

        if (header.Seed != world.Seed)
        {
            return new LoadResult(SaveStatus.SeedMismatch, SyncError.None, header, false, false);
        }

        var migrated = false;
        foreach (var s in header.Schemas)
        {
            var current = world.Registry.Create(s.TypeId);
            migrated |= current is not null && current.SchemaVersion != s.Version;
        }

        var error = world.ReadSnapshot(payload);
        if (error != SyncError.None)
        {
            return new LoadResult(SaveStatus.RestoreFailed, error, header, migrated, false);
        }

        if (!migrated && (world.Tick != header.Tick || world.ComputeHash() != header.WorldHash))
        {
            return new LoadResult(SaveStatus.HashMismatch, SyncError.None, header, false, false);
        }

        return new LoadResult(SaveStatus.Ok, SyncError.None, header, migrated, !migrated);
    }

    /// <summary>Loads a save into <paramref name="scratchWorld"/> and writes the header and the full world state as JSON, for inspection.</summary>
    public static string ToJson(ReadOnlySpan<byte> save, World scratchWorld)
    {
        var result = Load(save, scratchWorld);
        var j = new JsonOut();
        j.BeginObject();
        j.Name("status");
        j.Value(result.Status.ToString());
        if (result.Detail != SyncError.None)
        {
            j.Name("error");
            j.Value(result.Detail.ToString());
        }

        if (result.Header is { } h)
        {
            j.Name("header");
            j.BeginObject();
            j.Name("format");
            j.Value((ulong)h.Format);
            j.Name("seed");
            j.Value(h.Seed);
            j.Name("tick");
            j.Value(h.Tick);
            j.Name("worldHash");
            j.Value(h.WorldHash.ToString("x16", CultureInfo.InvariantCulture));
            j.Name("schemas");
            j.BeginArray();
            foreach (var s in h.Schemas)
            {
                j.BeginObject();
                j.Name("type");
                j.Value(s.TypeName);
                j.Name("typeId");
                j.Value((ulong)s.TypeId);
                j.Name("version");
                j.Value((ulong)s.Version);
                j.EndObject();
            }

            j.EndArray();
            j.EndObject();
            j.Name("migrated");
            j.Value(result.Migrated);
        }

        if (result.Status == SaveStatus.Ok)
        {
            j.Name("world");
            j.Raw(scratchWorld.DumpJson());
        }

        j.EndObject();
        return j.ToString();
    }
}
