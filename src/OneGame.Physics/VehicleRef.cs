namespace OneGame.Physics;

/// <summary>
/// Names a vehicle in a <see cref="PhysicsSpace3D"/>, like <see cref="BodyRef"/> names a body. Keep it in a synced property.
/// </summary>
public readonly struct VehicleRef : ISyncValue<VehicleRef>
{
    internal VehicleRef(int index, int generation)
    {
        Index = index;
        Generation = generation;
    }

    /// <summary>The vehicle slot in the space (separate from its chassis body slot).</summary>
    public int Index { get; }

    /// <summary>How many vehicles have used the slot; 0 only in the null reference.</summary>
    public int Generation { get; }

    /// <summary>True for the default value, which names nothing.</summary>
    public bool IsNull => Generation == 0;

    /// <inheritdoc />
    public void Write(SyncWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteInt32(Index);
        writer.WriteInt32(Generation);
    }

    /// <inheritdoc />
    public static VehicleRef Read(ref SyncReader reader)
    {
        var index = reader.ReadInt32();
        var generation = reader.ReadInt32();
        if (!reader.Ok)
        {
            return default;
        }

        if (index < 0 || generation < 0 || (generation == 0 && index != 0))
        {
            reader.Fail(SyncError.InvalidValue);
            return default;
        }

        return new VehicleRef(index, generation);
    }

    /// <inheritdoc />
    public void WriteJson(JsonOut json)
    {
        ArgumentNullException.ThrowIfNull(json);
        json.Value(ToString());
    }

    /// <inheritdoc />
    public bool Equals(VehicleRef other) => Index == other.Index && Generation == other.Generation;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is VehicleRef other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Index, Generation);

    /// <summary>Two references are equal when they name the same vehicle.</summary>
    public static bool operator ==(VehicleRef left, VehicleRef right) => left.Equals(right);

    /// <summary>Two references differ when they name different vehicles.</summary>
    public static bool operator !=(VehicleRef left, VehicleRef right) => !left.Equals(right);

    /// <inheritdoc />
    public override string ToString() => IsNull ? "vehicle:none" : string.Create(System.Globalization.CultureInfo.InvariantCulture, $"vehicle:{Index}.{Generation}");
}
