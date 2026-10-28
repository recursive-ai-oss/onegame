namespace OneGame.Core;

/// <summary>
/// Base class of every entity. The generator fills in everything abstract here for a class marked <see cref="EntityAttribute"/>;
/// authors override <see cref="OnTick"/> for per-tick sim logic.
/// </summary>
/// <remarks>
/// Game code seldom calls the members of this class directly. Declare synced state with <see cref="SyncedAttribute"/>, change it from
/// <see cref="OnTick"/> and from <see cref="CommandAttribute"/> handlers (sim code), and read it from <see cref="ViewAttribute"/> members.
/// Setters throw <see cref="ReplicaWriteException"/> on a replica; a view that wants a change sends a command through the generated
/// <c>Send</c> property. The members here are the surface the sync layer, saves and the test harness use.
/// </remarks>
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

    /// <summary>The entity type's name as written in code, used in harness output, scenarios and snapshots' JSON dump.</summary>
    public abstract string TypeName { get; }

    /// <summary>The schema version of the state this build writes.</summary>
    public abstract uint SchemaVersion { get; }

    /// <summary>The number of <see cref="SyncedAttribute"/> properties, which is also the width of the bit masks used by <see cref="DirtyMask"/> and visibility.</summary>
    public abstract int PropertyCount { get; }

    /// <summary>The name of synced property <paramref name="index"/> (declaration order).</summary>
    public abstract string PropertyName(int index);

    /// <summary>Bit i is set when synced property i changed since <see cref="ClearDirty"/>.</summary>
    public abstract ulong DirtyMask { get; }

    /// <summary>Forgets which properties changed; the sync layer calls it after sending a delta.</summary>
    public abstract void ClearDirty();

    /// <summary>Writes all synced state in the current schema.</summary>
    /// <param name="writer">Receives the bytes.</param>
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

    /// <summary>Writes a delta holding exactly the properties in <paramref name="mask"/>, whatever their dirty state. With the visible mask of an audience it is that audience's full view of the entity.</summary>
    public abstract void WriteMaskedDelta(SyncWriter writer, ulong mask);

    /// <summary>The value of the <see cref="OwnerKeyAttribute"/> property, or 0.</summary>
    public virtual ulong OwnerKey => 0;

    /// <summary>The value of the <see cref="TeamKeyAttribute"/> property, or 0.</summary>
    public virtual ulong TeamKey => 0;

    /// <summary>
    /// Which of this entity's properties (bit i = property i) an audience may receive. Returns false when the entity as a whole is
    /// hidden from it. Entities without visibility rules show everything to everyone.
    /// </summary>
    public virtual bool TryGetVisibleMask(in Audience audience, out ulong mask)
    {
        mask = FullMask;
        return true;
    }

    /// <summary>
    /// The mask an audience gets of this copy: what <see cref="TryGetVisibleMask"/> allows, cut down to what this copy holds (a replica of a
    /// filtered entity cannot pass on what it never received). A relay audience is treated as the entity's owner and team.
    /// </summary>
    internal bool TryGetAudienceMask(in Audience audience, out ulong mask)
    {
        var who = audience.Relay ? new Audience((uint)OwnerKey, (uint)TeamKey) : audience;
        if (!TryGetVisibleMask(who, out mask))
        {
            return false;
        }

        if (ViewMask != ulong.MaxValue)
        {
            mask &= ViewMask;
        }

        return true;
    }

    /// <summary>True when the command is marked <c>OwnerOnly</c>.</summary>
    /// <param name="commandId">The command's generated id.</param>
    public virtual bool IsOwnerOnlyCommand(uint commandId) => false;

    /// <summary>True when the command is marked <c>Predicted</c>.</summary>
    public virtual bool IsPredictedCommand(uint commandId) => false;

    /// <summary>True when the command is marked <c>Predicted</c> with <c>UsesRandom</c>.</summary>
    public virtual bool PredictedCommandUsesRandom(uint commandId) => false;

    /// <summary>Makes this entity's synced state equal to <paramref name="source"/>'s (same type). The generator overrides it to assign fields directly; the default goes through the encoded form.</summary>
    protected virtual void CopyStateCore(Entity source)
    {
        var w = new SyncWriter();
        source.WriteState(w);
        var r = new SyncReader(w.Written);
        ReadState(ref r);
    }

    internal void CopyStateFrom(Entity source)
    {
        CopyStateCore(source);
        ViewMask = source.ViewMask;
    }

    /// <summary>The mask with a bit for every synced property.</summary>
    public ulong FullMask => PropertyCount >= 64 ? ulong.MaxValue : (1UL << PropertyCount) - 1;

    /// <summary>
    /// Which properties this copy actually holds. A replica of a filtered entity holds only what its authority was allowed to send;
    /// the rest stay at their defaults. <see cref="ulong.MaxValue"/> means all of them.
    /// </summary>
    internal ulong ViewMask { get; set; } = ulong.MaxValue;

    /// <summary>Collapses a mask that covers every property to the all-properties marker, so the same view always hashes the same way.</summary>
    internal ulong NormalizeMask(ulong mask) => (mask & FullMask) == FullMask ? ulong.MaxValue : mask & FullMask;

    /// <summary>Writes the entity as a JSON object.</summary>
    public abstract void WriteJson(JsonOut json);

    /// <summary>Sets a scalar synced property from text, for the state-patch harness. Returns false if the name is unknown, the type is not a scalar, or the text does not parse.</summary>
    /// <param name="propertyName">The property's name.</param>
    /// <param name="text">The new value in invariant text form (for example <c>12.5</c> for a <see cref="Fixed"/>).</param>
    public abstract bool TrySetFromText(string propertyName, string text);

    /// <summary>Signatures of the commands that <see cref="SendFromText"/> accepts, such as <c>Buy(int itemId, int count)</c>, for the harness.</summary>
    public virtual string[] CommandSignatures => [];

    /// <summary>Encodes and sends a command given its name and text arguments (the state-patch harness uses this). Returns <see cref="CommandStatus.UnknownCommand"/> for an unknown name and <see cref="CommandStatus.Malformed"/> when the argument count or a value is wrong.</summary>
    /// <param name="commandName">The command method's name.</param>
    /// <param name="arguments">One text per parameter, in declaration order.</param>
    public virtual CommandStatus SendFromText(string commandName, string[] arguments) => CommandStatus.UnknownCommand;

    /// <summary>Runs the handler for a command. Returns <see cref="CommandStatus.Executed"/>, <see cref="CommandStatus.Rejected"/>, <see cref="CommandStatus.UnknownCommand"/> or <see cref="CommandStatus.Malformed"/>.</summary>
    protected abstract CommandStatus ExecuteCommand(uint commandId, ref SyncReader arguments);

    /// <summary>
    /// Per-tick sim logic: override it to move, age or react. It runs once per fixed simulation tick on the authority (and on a predicting
    /// replica's predicted copy), in entity id order, after the commands queued for that tick, and never on a plain replica. It is sim code:
    /// fixed-point numbers, named random streams (<see cref="World.Rng"/>), no clock, threads or I/O. An entity removed during a
    /// tick is gone when the tick ends.
    /// </summary>
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

    /// <summary>
    /// Hash of what an audience sees of this entity: with every property visible it equals <see cref="ComputeHash"/>, otherwise it
    /// covers the type, the id and only the visible properties. A replica computes the same value with <see cref="ComputeViewHash()"/>.
    /// </summary>
    public ulong ComputeViewHash(ulong visibleMask)
    {
        var mask = NormalizeMask(visibleMask);
        if (mask == ulong.MaxValue)
        {
            return ComputeHash();
        }

        var scratch = World?.Scratch ?? new SyncWriter();
        scratch.Reset();
        WriteMaskedDelta(scratch, mask);
        var h = StateHash.Combine(TypeId, Id.Value);
        return StateHash.Combine(h, StateHash.Of(scratch.Written));
    }

    /// <summary>The hash of what this copy holds: <see cref="ComputeHash"/> for a complete entity, the filtered form for a replica of a filtered one.</summary>
    public ulong ComputeViewHash() => ComputeViewHash(ViewMask);

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

    /// <summary>Records why the command handler running now rejected the command; the authority sends the text back to the submitter.</summary>
    /// <param name="reason">Short plain text for the player, or null to clear it.</param>
    protected void SetRejectReason(string? reason)
    {
        if (World is { } world)
        {
            world.LastRejectReason = reason;
        }
    }

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
