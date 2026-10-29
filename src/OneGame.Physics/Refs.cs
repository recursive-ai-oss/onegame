namespace OneGame.Physics;

/// <summary>
/// Names a body in a <see cref="PhysicsSpace2D"/>: a slot and a generation, so a reference to a removed body is detected instead of
/// silently pointing at the body that took its slot. It is a plain synced value, so an entity keeps it in a property:
/// <code>[Synced] public partial BodyRef Body { get; private set; }</code>
/// The default value is the null reference.
/// </summary>
public readonly struct BodyRef : ISyncValue<BodyRef>
{
    internal BodyRef(int index, int generation)
    {
        Index = index;
        Generation = generation;
    }

    /// <summary>The body's slot in the space.</summary>
    public int Index { get; }

    /// <summary>How many bodies have used the slot; 0 only in the null reference.</summary>
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
    public static BodyRef Read(ref SyncReader reader)
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

        return new BodyRef(index, generation);
    }

    /// <inheritdoc />
    public void WriteJson(JsonOut json)
    {
        ArgumentNullException.ThrowIfNull(json);
        json.Value(ToString());
    }

    /// <inheritdoc />
    public bool Equals(BodyRef other) => Index == other.Index && Generation == other.Generation;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is BodyRef other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Index, Generation);

    /// <summary>Two references are equal when they name the same body.</summary>
    public static bool operator ==(BodyRef left, BodyRef right) => left.Equals(right);

    /// <summary>Two references differ when they name different bodies.</summary>
    public static bool operator !=(BodyRef left, BodyRef right) => !left.Equals(right);

    /// <inheritdoc />
    public override string ToString() => IsNull ? "body:none" : string.Create(System.Globalization.CultureInfo.InvariantCulture, $"body:{Index}.{Generation}");
}

/// <summary>
/// Names a character in a <see cref="PhysicsSpace2D"/>, like <see cref="BodyRef"/> names a body. Keep it in a synced property.
/// </summary>
public readonly struct CharacterRef : ISyncValue<CharacterRef>
{
    internal CharacterRef(int index, int generation)
    {
        Index = index;
        Generation = generation;
    }

    /// <summary>The slot of the character's body in the space (a character is a kinematic body with a capsule).</summary>
    public int Index { get; }

    /// <summary>How many bodies have used the slot; 0 only in the null reference.</summary>
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
    public static CharacterRef Read(ref SyncReader reader)
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

        return new CharacterRef(index, generation);
    }

    /// <inheritdoc />
    public void WriteJson(JsonOut json)
    {
        ArgumentNullException.ThrowIfNull(json);
        json.Value(ToString());
    }

    /// <inheritdoc />
    public bool Equals(CharacterRef other) => Index == other.Index && Generation == other.Generation;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is CharacterRef other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Index, Generation);

    /// <summary>Two references are equal when they name the same character.</summary>
    public static bool operator ==(CharacterRef left, CharacterRef right) => left.Equals(right);

    /// <summary>Two references differ when they name different characters.</summary>
    public static bool operator !=(CharacterRef left, CharacterRef right) => !left.Equals(right);

    /// <inheritdoc />
    public override string ToString() => IsNull ? "character:none" : string.Create(System.Globalization.CultureInfo.InvariantCulture, $"character:{Index}.{Generation}");
}
