namespace OneGame.Core;

/// <summary>
/// Base class of every entity. The generator fills in everything abstract here for a class marked <see cref="EntityAttribute"/>;
/// authors override <see cref="OnTick"/> for per-tick sim logic.
/// </summary>
public abstract class Entity
{
    private int _restoring;

    /// <summary>The entity's id, or <see cref="EntityId.None"/> before it is added to a world.</summary>
    public EntityId Id { get; internal set; }

    /// <summary>The world this entity belongs to, or null.</summary>
    public World? World { get; internal set; }

    /// <summary>True once the entity has been removed from its world.</summary>
    public bool IsRemoved { get; internal set; }

    /// <summary>The stable id of this entity type, used in snapshots.</summary>
    public abstract uint TypeId { get; }

    /// <summary>The type's name.</summary>
    public abstract string TypeName { get; }

    /// <summary>The schema version of the state this build writes.</summary>
    public abstract uint SchemaVersion { get; }

    /// <summary>The number of synced properties.</summary>
    public abstract int PropertyCount { get; }

    /// <summary>The name of synced property <paramref name="index"/> (declaration order).</summary>
    public abstract string PropertyName(int index);

    /// <summary>Bit i is set when synced property i changed since <see cref="ClearDirty"/>.</summary>
    public abstract ulong DirtyMask { get; }

    /// <summary>Forgets which properties changed; the sync layer calls it after sending a delta.</summary>
    public abstract void ClearDirty();

    /// <summary>Writes all synced state in the current schema.</summary>
    public abstract void WriteState(SyncWriter writer);

    /// <summary>Replaces all synced state from the current schema's layout. Check <paramref name="reader"/> afterwards; on error the entity is unchanged.</summary>
    public abstract void ReadState(ref SyncReader reader);

    /// <summary>Replaces all synced state from a given schema version, running the author's migration for older versions. On error the reader holds the reason.</summary>
    public abstract void ReadState(uint schemaVersion, ref SyncReader reader);

    /// <summary>Writes a delta holding the properties changed since <see cref="ClearDirty"/>.</summary>
    public abstract void WriteDirtyDelta(SyncWriter writer);

    /// <summary>Writes a delta that turns <paramref name="baseline"/> into this entity's state.</summary>
    /// <returns>False if <paramref name="baseline"/> is not the same entity type; nothing is written then.</returns>
    public abstract bool WriteDeltaFrom(Entity baseline, SyncWriter writer);

    /// <summary>Applies a delta. All values are decoded before any is stored, so an error leaves the entity unchanged. Applied properties become dirty, so a relay can forward them.</summary>
    public abstract void ApplyDelta(ref SyncReader reader);

    /// <summary>Writes the entity as a JSON object.</summary>
    public abstract void WriteJson(JsonOut json);

    /// <summary>Sets a scalar synced property from text, for the state-patch harness. Returns false if the name is unknown, the type is not a scalar, or the text does not parse.</summary>
    public abstract bool TrySetFromText(string propertyName, string text);

    /// <summary>Signatures of the commands that <see cref="SendFromText"/> accepts, such as <c>Buy(int itemId, int count)</c>, for the harness.</summary>
    public virtual string[] CommandSignatures => [];

    /// <summary>Encodes and sends a command given its name and text arguments (the state-patch harness uses this). Returns <see cref="CommandStatus.UnknownCommand"/> for an unknown name and <see cref="CommandStatus.Malformed"/> when the argument count or a value is wrong.</summary>
    public virtual CommandStatus SendFromText(string commandName, string[] arguments) => CommandStatus.UnknownCommand;

    /// <summary>Runs the handler for a command. Returns <see cref="CommandStatus.Executed"/>, <see cref="CommandStatus.Rejected"/>, <see cref="CommandStatus.UnknownCommand"/> or <see cref="CommandStatus.Malformed"/>.</summary>
    protected abstract CommandStatus ExecuteCommand(uint commandId, ref SyncReader arguments);

    /// <summary>Per-tick sim logic. Runs on the authority, in entity id order, after queued commands.</summary>
    protected virtual void OnTick()
    {
    }

    internal void RunTick() => OnTick();

    internal CommandStatus RunCommand(uint commandId, ref SyncReader arguments) => ExecuteCommand(commandId, ref arguments);

    /// <summary>Hash of this entity's type, id and full state, for drift detection.</summary>
    public ulong ComputeHash()
    {
        var scratch = World?.Scratch ?? new SyncWriter();
        scratch.Reset();
        WriteState(scratch);
        var h = StateHash.Combine(TypeId, Id.Value);
        return StateHash.Combine(h, StateHash.Of(scratch.Written));
    }

    /// <summary>Throws if this entity belongs to a replica world. Generated setters call it.</summary>
    /// <exception cref="ReplicaWriteException">The entity is a replica; only the authority changes state.</exception>
    protected void AssertWritable()
    {
        if (_restoring == 0 && World is { Role: WorldRole.Replica })
        {
            throw new ReplicaWriteException(
                $"{TypeName} {Id} is a replica; only its authority changes state. Send a command instead.");
        }
    }

    /// <summary>Lets generated migration code assign properties on a replica.</summary>
    protected void BeginRestore() => _restoring++;

    /// <summary>Ends <see cref="BeginRestore"/>.</summary>
    protected void EndRestore() => _restoring--;

    /// <summary>Starts a command message: returns the world's scratch writer for the arguments.</summary>
    /// <exception cref="NotInWorldException">The entity is not in a world.</exception>
    protected SyncWriter BeginCommand()
    {
        var world = World ?? throw new NotInWorldException($"{TypeName} is not in a world, so it cannot send commands. Add it with World.Add first.");
        var w = world.Scratch;
        w.Reset();
        return w;
    }

    /// <summary>Sends the command whose arguments are in the writer from <see cref="BeginCommand"/>.</summary>
    protected CommandStatus SendCommand(uint commandId, SyncWriter arguments)
        => World!.Submit(Id, commandId, arguments.Written);
}
