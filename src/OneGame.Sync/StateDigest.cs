using System.Globalization;

namespace OneGame.Sync;

/// <summary>What a digest entry describes.</summary>
public enum DigestKind : byte
{
    /// <summary>The random streams.</summary>
    Rng = 0,

    /// <summary>A hot table.</summary>
    Table = 1,

    /// <summary>An entity.</summary>
    Entity = 2,
}

/// <summary>One line of a <see cref="StateDigest"/>.</summary>
/// <param name="Kind">What it describes.</param>
/// <param name="Id">The entity id (entities only).</param>
/// <param name="Name">The table name (tables only).</param>
/// <param name="Hash">Hash of its state.</param>
public readonly record struct DigestEntry(DigestKind Kind, ulong Id, string? Name, ulong Hash);

/// <summary>How a part of the state differs between two digests.</summary>
public enum DifferenceChange : byte
{
    /// <summary>Present on both sides with different state.</summary>
    Changed = 0,

    /// <summary>Present only in the expected state (the authority, or the recording).</summary>
    OnlyInExpected,

    /// <summary>Present only in the actual state (the replica, or the replay).</summary>
    OnlyInActual,
}

/// <summary>One entity, table or the random streams that differ between expected and actual state.</summary>
public sealed record StateDifference(DigestKind Kind, ulong EntityId, string? Name, string? TypeName, DifferenceChange Change, ulong ExpectedHash, ulong ActualHash)
{
    internal void WriteJson(JsonOut j)
    {
        j.BeginObject();
        j.Name("kind");
        j.Value(Kind.ToString());
        if (Kind == DigestKind.Entity)
        {
            j.Name("entity");
            j.Value(EntityId);
            j.Name("type");
            j.Value(TypeName);
        }
        else if (Kind == DigestKind.Table)
        {
            j.Name("table");
            j.Value(Name);
        }

        j.Name("change");
        j.Value(Change.ToString());
        j.Name("expected");
        j.Value(ExpectedHash.ToString("x16", CultureInfo.InvariantCulture));
        j.Name("actual");
        j.Value(ActualHash.ToString("x16", CultureInfo.InvariantCulture));
        j.EndObject();
    }

    internal static void WriteJsonArray(JsonOut j, IReadOnlyList<StateDifference> list)
    {
        j.BeginArray();
        foreach (var d in list)
        {
            d.WriteJson(j);
        }

        j.EndArray();
    }
}

/// <summary>
/// A per-part hash list of a world at one tick: the random streams, each table, each entity. Two digests can be diffed to name
/// exactly what drifted. Capturing reuses the digest's buffers.
/// </summary>
public sealed class StateDigest
{
    private readonly List<DigestEntry> _entries = [];

    /// <summary>The completed tick count the digest describes.</summary>
    public long Tick { get; private set; }

    /// <summary>The whole-world hash at that tick (<see cref="World.ComputeHash"/>).</summary>
    public ulong WorldHash { get; private set; }

    /// <summary>True when the world had more entities than the limit and the entries were left out.</summary>
    public bool Truncated { get; private set; }

    /// <summary>The entries: random streams, tables in world order, entities in id order.</summary>
    public IReadOnlyList<DigestEntry> Entries => _entries;

    /// <summary>Captures <paramref name="world"/> into <paramref name="into"/>, replacing its content.</summary>
    /// <param name="world">The world.</param>
    /// <param name="into">The digest to fill.</param>
    /// <param name="scratch">A writer to use for serializing parts.</param>
    /// <param name="entityLimit">Entities beyond this count are not listed.</param>
    public static void Capture(World world, StateDigest into, SyncWriter scratch, int entityLimit = int.MaxValue)
    {
        into._entries.Clear();
        into.Tick = world.Tick;
        into.WorldHash = world.ComputeHash();
        into.Truncated = world.Entities.Count > entityLimit;
        if (into.Truncated)
        {
            return;
        }

        scratch.Reset();
        world.Rng.Write(scratch);
        into._entries.Add(new DigestEntry(DigestKind.Rng, 0, null, StateHash.Of(scratch.Written)));
        var tables = world.Tables;
        for (var t = 0; t < tables.Count; t++)
        {
            var table = tables[t];
            scratch.Reset();
            table.Write(scratch);
            into._entries.Add(new DigestEntry(DigestKind.Table, 0, table.Name, StateHash.Combine(StateHash.Of(table.Name), StateHash.Of(scratch.Written))));
        }

        var entities = world.Entities;
        for (var n = 0; n < entities.Count; n++)
        {
            into._entries.Add(new DigestEntry(DigestKind.Entity, entities[n].Id.Value, null, entities[n].ComputeHash()));
        }
    }

    /// <summary>Lists what differs between two digests. Entity type names are looked up in <paramref name="expectedWorld"/> then <paramref name="actualWorld"/>.</summary>
    public static void Diff(StateDigest expected, StateDigest actual, World? expectedWorld, World? actualWorld, List<StateDifference> output)
    {
        var a = expected._entries;
        var b = actual._entries;
        int i = 0, j = 0;
        while (i < a.Count || j < b.Count)
        {
            int order;
            if (i >= a.Count)
            {
                order = 1;
            }
            else if (j >= b.Count)
            {
                order = -1;
            }
            else
            {
                order = Compare(a[i], b[j]);
            }

            if (order == 0)
            {
                if (a[i].Hash != b[j].Hash)
                {
                    output.Add(Make(a[i], DifferenceChange.Changed, a[i].Hash, b[j].Hash, expectedWorld, actualWorld));
                }

                i++;
                j++;
            }
            else if (order < 0)
            {
                output.Add(Make(a[i], DifferenceChange.OnlyInExpected, a[i].Hash, 0, expectedWorld, actualWorld));
                i++;
            }
            else
            {
                output.Add(Make(b[j], DifferenceChange.OnlyInActual, 0, b[j].Hash, expectedWorld, actualWorld));
                j++;
            }
        }
    }

    private static int Compare(DigestEntry x, DigestEntry y)
    {
        if (x.Kind != y.Kind)
        {
            return x.Kind.CompareTo(y.Kind);
        }

        return x.Kind switch
        {
            DigestKind.Entity => x.Id.CompareTo(y.Id),
            DigestKind.Table => string.CompareOrdinal(x.Name, y.Name),
            _ => 0,
        };
    }

    private static StateDifference Make(DigestEntry e, DifferenceChange change, ulong expected, ulong actual, World? w1, World? w2)
    {
        string? type = null;
        if (e.Kind == DigestKind.Entity)
        {
            type = w1?.Find(new EntityId(e.Id))?.TypeName ?? w2?.Find(new EntityId(e.Id))?.TypeName;
        }

        return new StateDifference(e.Kind, e.Id, e.Name, type, change, expected, actual);
    }

    /// <summary>Writes the digest.</summary>
    public void Write(SyncWriter w)
    {
        w.WriteVarInt(Tick);
        w.WriteFixed64(WorldHash);
        w.WriteBool(Truncated);
        w.WriteVarUInt((ulong)_entries.Count);
        foreach (var e in _entries)
        {
            w.WriteByte((byte)e.Kind);
            if (e.Kind == DigestKind.Entity)
            {
                w.WriteVarUInt(e.Id);
            }
            else if (e.Kind == DigestKind.Table)
            {
                w.WriteString(e.Name);
            }

            w.WriteFixed64(e.Hash);
        }
    }

    /// <summary>Reads a digest from untrusted bytes. On error the reader holds the reason and the digest is left empty.</summary>
    public void Read(ref SyncReader r)
    {
        _entries.Clear();
        Tick = r.ReadVarInt();
        WorldHash = r.ReadFixed64();
        Truncated = r.ReadBool();
        var count = r.ReadCount(SyncLimits.MaxEntities);
        for (var n = 0; n < count && r.Ok; n++)
        {
            var kind = (DigestKind)r.ReadByte();
            ulong id = 0;
            string? name = null;
            switch (kind)
            {
                case DigestKind.Entity: id = r.ReadVarUInt(); break;
                case DigestKind.Table: name = r.ReadString(); break;
                case DigestKind.Rng: break;
                default: r.Fail(SyncError.InvalidValue); break;
            }

            var hash = r.ReadFixed64();
            if (r.Ok)
            {
                _entries.Add(new DigestEntry(kind, id, name, hash));
            }
        }

        if (!r.Ok)
        {
            _entries.Clear();
        }
    }

    /// <summary>Copies another digest into this one.</summary>
    public void CopyFrom(StateDigest other)
    {
        _entries.Clear();
        _entries.AddRange(other._entries);
        Tick = other.Tick;
        WorldHash = other.WorldHash;
        Truncated = other.Truncated;
    }
}

/// <summary>A replica whose state differs from its authority's at the same tick.</summary>
public sealed class DriftReport
{
    internal DriftReport(ScopeId scope, string replicaNode, long tick, ulong authorityHash, ulong replicaHash, IReadOnlyList<StateDifference> differences, bool detailAvailable)
    {
        Scope = scope;
        ReplicaNode = replicaNode;
        Tick = tick;
        AuthorityHash = authorityHash;
        ReplicaHash = replicaHash;
        Differences = differences;
        DetailAvailable = detailAvailable;
    }

    /// <summary>The scope.</summary>
    public ScopeId Scope { get; }

    /// <summary>Name of the node holding the replica.</summary>
    public string ReplicaNode { get; }

    /// <summary>The completed tick count at which the hashes differed.</summary>
    public long Tick { get; }

    /// <summary>Hash on the authority.</summary>
    public ulong AuthorityHash { get; }

    /// <summary>Hash on the replica.</summary>
    public ulong ReplicaHash { get; }

    /// <summary>The entities, tables and streams that differ. Empty when <see cref="DetailAvailable"/> is false.</summary>
    public IReadOnlyList<StateDifference> Differences { get; }

    /// <summary>False when the authority could no longer supply the per-entity digest for that tick.</summary>
    public bool DetailAvailable { get; }

    /// <summary>One-line summary.</summary>
    public override string ToString()
    {
        var names = string.Join(", ", Differences.Take(8).Select(d => d.Kind == DigestKind.Entity ? $"{d.TypeName ?? "entity"} {d.EntityId}" : d.Kind == DigestKind.Table ? $"table {d.Name}" : "rng"));
        return $"drift in {Scope} on {ReplicaNode} at tick {Tick}: authority {AuthorityHash:x16}, replica {ReplicaHash:x16}; differs: {(names.Length == 0 ? "(no detail)" : names)}";
    }

    /// <summary>The report as JSON.</summary>
    public string ToJson()
    {
        var j = new JsonOut();
        j.BeginObject();
        j.Name("scope");
        j.Value(Scope.Value);
        j.Name("node");
        j.Value(ReplicaNode);
        j.Name("tick");
        j.Value(Tick);
        j.Name("authorityHash");
        j.Value(AuthorityHash.ToString("x16", CultureInfo.InvariantCulture));
        j.Name("replicaHash");
        j.Value(ReplicaHash.ToString("x16", CultureInfo.InvariantCulture));
        j.Name("detailAvailable");
        j.Value(DetailAvailable);
        j.Name("differences");
        StateDifference.WriteJsonArray(j, Differences);
        j.EndObject();
        return j.ToString();
    }
}

/// <summary>Thrown by <see cref="Cluster.Step"/> when <see cref="ClusterOptions.FatalDrift"/> is on and a replica drifted.</summary>
public sealed class DriftException : InvalidOperationException
{
    internal DriftException(DriftReport report)
        : base(report.ToString())
    {
        Report = report;
    }

    /// <summary>The drift report.</summary>
    public DriftReport Report { get; }
}

/// <summary>A message from a peer that could not be applied; the replica asked for a fresh snapshot.</summary>
/// <param name="Scope">The scope concerned.</param>
/// <param name="Tick">The replica's tick when it happened.</param>
/// <param name="Reason">What was wrong.</param>
public readonly record struct ProtocolError(ScopeId Scope, long Tick, string Reason);
