using System.Globalization;

namespace OneGame.Core;

/// <summary>
/// Identifies an entity in a world. Ids are assigned in increasing order and never reused, so an id that refers to a removed
/// entity cannot later point at a different one. Zero means "no entity".
/// </summary>
public readonly struct EntityId : ISyncValue<EntityId>, IComparable<EntityId>
{
    /// <summary>Wraps a raw id.</summary>
    public EntityId(ulong value) => Value = value;

    /// <summary>The "no entity" id.</summary>
    public static EntityId None => default;

    /// <summary>The raw number.</summary>
    public ulong Value { get; }

    /// <summary>True for any id other than <see cref="None"/>.</summary>
    public bool IsValid => Value != 0;

    /// <summary>Equality of the numbers.</summary>
    public static bool operator ==(EntityId a, EntityId b) => a.Value == b.Value;

    /// <summary>Inequality of the numbers.</summary>
    public static bool operator !=(EntityId a, EntityId b) => a.Value != b.Value;

    /// <summary>True if <paramref name="a"/> is the smaller id.</summary>
    public static bool operator <(EntityId a, EntityId b) => a.Value < b.Value;

    /// <summary>True if <paramref name="a"/> is the larger id.</summary>
    public static bool operator >(EntityId a, EntityId b) => a.Value > b.Value;

    /// <summary>True if <paramref name="a"/> is not larger.</summary>
    public static bool operator <=(EntityId a, EntityId b) => a.Value <= b.Value;

    /// <summary>True if <paramref name="a"/> is not smaller.</summary>
    public static bool operator >=(EntityId a, EntityId b) => a.Value >= b.Value;

    /// <inheritdoc />
    public bool Equals(EntityId other) => Value == other.Value;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is EntityId e && e.Value == Value;

    /// <inheritdoc />
    public override int GetHashCode() => Value.GetHashCode();

    /// <inheritdoc />
    public int CompareTo(EntityId other) => Value.CompareTo(other.Value);

    /// <inheritdoc />
    public void Write(SyncWriter writer) => writer.WriteVarUInt(Value);

    /// <inheritdoc />
    public static EntityId Read(ref SyncReader reader) => new(reader.ReadVarUInt());

    /// <inheritdoc />
    public void WriteJson(JsonOut json) => json.Value(Value);

    /// <inheritdoc />
    public override string ToString() => "#" + Value.ToString(CultureInfo.InvariantCulture);
}
