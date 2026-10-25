namespace OneGame.Sync;

/// <summary>Limits on what one authority transfer may carry. A peer's offer is checked against them before anything is allocated.</summary>
public static class TransferLimits
{
    /// <summary>Most entities in one transfer.</summary>
    public const int MaxEntities = 256;

    /// <summary>Most bytes in the serialized entities of one transfer.</summary>
    public const int MaxBundleBytes = 48 * 1024;

    /// <summary>Most unfinished outgoing transfers a scope's log keeps.</summary>
    public const int MaxOpen = 256;

    /// <summary>Most finished incoming transfer ids a scope's log remembers (the oldest are forgotten first).</summary>
    public const int MaxDone = 1024;
}

/// <summary>
/// The state of a scope's authority transfers, kept in the world so that a save, a snapshot-based restart and a replay carry it: the
/// bundles that left this scope and are not confirmed yet, and the ids of the transfers this scope has installed. Add it to every world
/// that sends or receives transfers with <see cref="AddTo"/>, in the same order on every node. It is a hidden table: subscribers never get it.
/// </summary>
public sealed class TransferLog : IHotTable
{
    /// <summary>The table's name in snapshots.</summary>
    public const string TableName = "og.transfers";

    private readonly List<OutRecord> _open = [];
    private readonly List<DoneRecord> _done = [];

    /// <summary>The next transfer number this scope hands out.</summary>
    internal ulong NextNumber { get; set; } = 1;

    internal IReadOnlyList<OutRecord> Open => _open;

    internal IReadOnlyList<DoneRecord> Done => _done;

    /// <inheritdoc />
    public string Name => TableName;

    /// <inheritdoc />
    public int Count => _open.Count + _done.Count;

    /// <inheritdoc />
    public Visibility Visibility => Visibility.Hidden;

    /// <summary>Adds a transfer log to a world and returns it.</summary>
    /// <param name="world">The world; add the table before any save is loaded.</param>
    public static TransferLog AddTo(World world) => world.AddTable(new TransferLog());

    /// <summary>The world's transfer log, or null if it has none.</summary>
    public static TransferLog? Of(World world)
    {
        // Indexed on purpose: the scope looks this up every tick, and an enumerator over the interface would allocate.
        var tables = world.Tables;
        for (var i = 0; i < tables.Count; i++)
        {
            if (tables[i] is TransferLog log)
            {
                return log;
            }
        }

        return null;
    }

    /// <summary>Number of outgoing transfers that are not confirmed yet.</summary>
    public int OpenCount => _open.Count;

    /// <summary>Whether this scope has installed the transfer with this id.</summary>
    public bool IsDone(ulong id) => FindDone(id) is not null;

    internal OutRecord? FindOpen(ulong id)
    {
        foreach (var r in _open)
        {
            if (r.Id == id)
            {
                return r;
            }
        }

        return null;
    }

    internal DoneRecord? FindDone(ulong id)
    {
        foreach (var r in _done)
        {
            if (r.Id == id)
            {
                return r;
            }
        }

        return null;
    }

    internal void AddOpen(OutRecord record)
    {
        _open.Add(record);
        var number = record.Id & 0xFFFF_FFFFUL;
        if (number >= NextNumber)
        {
            NextNumber = number + 1;
        }
    }

    internal bool RemoveOpen(ulong id)
    {
        var r = FindOpen(id);
        return r is not null && _open.Remove(r);
    }

    internal void AddDone(DoneRecord record)
    {
        _done.Add(record);
        if (_done.Count > TransferLimits.MaxDone)
        {
            _done.RemoveAt(0);
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        _open.Clear();
        _done.Clear();
        NextNumber = 1;
    }

    /// <inheritdoc />
    public void Write(SyncWriter writer)
    {
        writer.WriteVarUInt(NextNumber);
        writer.WriteVarUInt((ulong)_open.Count);
        foreach (var r in _open)
        {
            writer.WriteVarUInt(r.Id);
            writer.WriteVarUInt(r.DestScope);
            writer.WriteByte(r.Committed ? (byte)2 : (byte)1);
            writer.WriteVarInt(r.Tick);
            writer.WriteVarUInt((ulong)r.OldIds.Length);
            foreach (var id in r.OldIds)
            {
                writer.WriteVarUInt(id);
            }

            writer.WriteBytes(r.Bundle);
        }

        writer.WriteVarUInt((ulong)_done.Count);
        foreach (var d in _done)
        {
            writer.WriteVarUInt(d.Id);
            writer.WriteVarUInt((ulong)d.NewIds.Length);
            foreach (var id in d.NewIds)
            {
                writer.WriteVarUInt(id);
            }
        }
    }

    /// <inheritdoc />
    public void Read(ref SyncReader reader)
    {
        var next = reader.ReadVarUInt();
        var openCount = reader.ReadCount(TransferLimits.MaxOpen);
        var open = new List<OutRecord>(Math.Min(openCount, 16));
        for (var i = 0; i < openCount && reader.Ok; i++)
        {
            var id = reader.ReadVarUInt();
            var dest = reader.ReadVarUInt();
            var state = reader.ReadByte();
            var tick = reader.ReadVarInt();
            var idCount = reader.ReadCount(TransferLimits.MaxEntities);
            var ids = new ulong[idCount];
            for (var k = 0; k < idCount && reader.Ok; k++)
            {
                ids[k] = reader.ReadVarUInt();
            }

            var bundle = reader.ReadBytes(TransferLimits.MaxBundleBytes);
            if (!reader.Ok)
            {
                break;
            }

            if (dest > uint.MaxValue || state is not (1 or 2))
            {
                reader.Fail(SyncError.InvalidValue);
                break;
            }

            open.Add(new OutRecord(id, (uint)dest, state == 2, tick, ids, bundle.ToArray()));
        }

        var doneCount = reader.ReadCount(TransferLimits.MaxDone);
        var done = new List<DoneRecord>(Math.Min(doneCount, 16));
        for (var i = 0; i < doneCount && reader.Ok; i++)
        {
            var id = reader.ReadVarUInt();
            var idCount = reader.ReadCount(TransferLimits.MaxEntities);
            var ids = new ulong[idCount];
            for (var k = 0; k < idCount && reader.Ok; k++)
            {
                ids[k] = reader.ReadVarUInt();
            }

            if (reader.Ok)
            {
                done.Add(new DoneRecord(id, ids));
            }
        }

        if (!reader.Ok)
        {
            return;
        }

        NextNumber = next == 0 ? 1 : next;
        _open.Clear();
        _open.AddRange(open);
        _done.Clear();
        _done.AddRange(done);
    }

    /// <inheritdoc />
    public void WriteJson(JsonOut json)
    {
        json.BeginObject();
        json.Name("next");
        json.Value(NextNumber);
        json.Name("open");
        json.BeginArray();
        foreach (var r in _open)
        {
            json.BeginObject();
            json.Name("id");
            json.Value(r.Id);
            json.Name("dest");
            json.Value((ulong)r.DestScope);
            json.Name("committed");
            json.Value(r.Committed);
            json.Name("entities");
            json.Value((ulong)r.OldIds.Length);
            json.EndObject();
        }

        json.EndArray();
        json.Name("done");
        json.BeginArray();
        foreach (var d in _done)
        {
            json.Value(d.Id);
        }

        json.EndArray();
        json.EndObject();
    }

    /// <summary>An outgoing transfer that left the world and is not confirmed yet.</summary>
    internal sealed class OutRecord(ulong id, uint destScope, bool committed, long tick, ulong[] oldIds, byte[] bundle)
    {
        public ulong Id { get; } = id;

        public uint DestScope { get; } = destScope;

        public bool Committed { get; set; } = committed;

        public long Tick { get; } = tick;

        public ulong[] OldIds { get; } = oldIds;

        public byte[] Bundle { get; } = bundle;
    }

    /// <summary>An incoming transfer that was installed, with the ids the entities got here.</summary>
    internal sealed class DoneRecord(ulong id, ulong[] newIds)
    {
        public ulong Id { get; } = id;

        public ulong[] NewIds { get; } = newIds;
    }
}
