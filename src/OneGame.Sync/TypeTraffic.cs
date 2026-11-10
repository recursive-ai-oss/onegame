namespace OneGame.Sync;

/// <summary>What one entity type has cost on the wire in a scope: bytes of state in additions (a whole entity entering a view) and in deltas, counted once per connection they were sent to.</summary>
public sealed class TypeTraffic
{
    internal TypeTraffic(uint typeId, string typeName)
    {
        TypeId = typeId;
        TypeName = typeName;
    }

    /// <summary>The entity type's id.</summary>
    public uint TypeId { get; }

    /// <summary>The entity type's name.</summary>
    public string TypeName { get; }

    /// <summary>Entities of this type sent whole to a connection (new, or entering its interest).</summary>
    public long Additions { get; internal set; }

    /// <summary>Bytes of those additions.</summary>
    public long AddedBytes { get; internal set; }

    /// <summary>Deltas sent for entities of this type.</summary>
    public long Deltas { get; internal set; }

    /// <summary>Bytes of those deltas.</summary>
    public long DeltaBytes { get; internal set; }

    /// <summary>Additions plus deltas, in bytes.</summary>
    public long Bytes => AddedBytes + DeltaBytes;
}
