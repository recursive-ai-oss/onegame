namespace OneGame.Core;

/// <summary>
/// What happened to a command when it was sent. Only <see cref="Executed"/> and <see cref="Queued"/> mean the command is on its way to
/// running; a replica reports <see cref="Forwarded"/> and the real answer arrives later from the authority (for a reply with a reason, see <see cref="CommandReply"/>).
/// </summary>
public enum CommandStatus
{
    /// <summary>The handler ran and accepted the command.</summary>
    Executed = 0,

    /// <summary>The handler ran and refused the command (it returned false).</summary>
    Rejected,

    /// <summary>The command was queued to run at the start of the next tick.</summary>
    Queued,

    /// <summary>The command was passed on to the authority; this world is a replica.</summary>
    Forwarded,

    /// <summary>No entity has the command's entity id.</summary>
    UnknownEntity,

    /// <summary>The entity has no command with that id.</summary>
    UnknownCommand,

    /// <summary>The bytes were not a valid command: truncated, too large or with trailing data.</summary>
    Malformed,
}

/// <summary>Whether a world owns its state or mirrors another node's.</summary>
public enum WorldRole
{
    /// <summary>The world simulates and is the source of truth.</summary>
    Authority = 0,

    /// <summary>The world only applies state received from an authority. Setters and ticking throw <see cref="ReplicaWriteException"/>.</summary>
    Replica,

    /// <summary>
    /// A writable copy a replica simulates ahead of its authority (client prediction). It runs ticks and predicted commands like an authority,
    /// but commands sent to its entities from outside the sim are forwarded through <see cref="World.Observer"/> as on a replica.
    /// </summary>
    Predicted,
}

/// <summary>Thrown when code changes state on a replica. Only the authority changes state; replicas send commands.</summary>
public sealed class ReplicaWriteException : InvalidOperationException
{
    /// <summary>Creates the exception.</summary>
    public ReplicaWriteException(string message)
        : base(message)
    {
    }
}

/// <summary>Thrown when an entity that is not in a world is asked to do something that needs one.</summary>
public sealed class NotInWorldException : InvalidOperationException
{
    /// <summary>Creates the exception.</summary>
    public NotInWorldException(string message)
        : base(message)
    {
    }
}

/// <summary>Thrown when an entity type is registered twice or two types share an id.</summary>
public sealed class DuplicateTypeException : InvalidOperationException
{
    /// <summary>Creates the exception.</summary>
    public DuplicateTypeException(string message)
        : base(message)
    {
    }
}

/// <summary>Sim logic that is not tied to one entity. Systems tick after all entities, in the order they were added. Keep their state in entities or tables, never in fields, so snapshots and hashes cover it.</summary>
public interface ISimSystem
{
    /// <summary>Runs one tick.</summary>
    void Tick(World world);
}

/// <summary>A struct-of-arrays table the world snapshots and hashes. The generator emits one for each <see cref="HotDataAttribute"/> struct.</summary>
public interface IHotTable
{
    /// <summary>The table's name in snapshots; unique within a world.</summary>
    string Name { get; }

    /// <summary>The number of rows.</summary>
    int Count { get; }

    /// <summary>Whether subscribers receive the table: <see cref="Visibility.Everyone"/> or <see cref="Visibility.Hidden"/>.</summary>
    Visibility Visibility => Visibility.Everyone;

    /// <summary>Removes every row.</summary>
    void Clear();

    /// <summary>Writes all rows.</summary>
    void Write(SyncWriter writer);

    /// <summary>Replaces all rows. All rows are decoded before any is stored, so an error leaves the table unchanged.</summary>
    void Read(ref SyncReader reader);

    /// <summary>Writes the rows as a JSON array of objects.</summary>
    void WriteJson(JsonOut json);

    /// <summary>Sets one scalar cell from text (numbers, bool, string, Fixed, enums by number), for the harness. The change bypasses commands.</summary>
    /// <param name="row">The row index.</param>
    /// <param name="column">The column (field) name.</param>
    /// <param name="text">The new value.</param>
    /// <returns>False when the row or column does not exist, the column has no text form, or the text does not parse. Generated tables implement this; a hand-written table does not support it.</returns>
    bool TrySetFromText(int row, string column, string text) => false;

    /// <summary>Makes this table hold exactly the rows of <paramref name="source"/>, a table of the same type. Generated tables copy the columns in place without allocating once they have room; the default goes through the encoded form.</summary>
    void CopyFrom(IHotTable source)
    {
        var w = new SyncWriter();
        source.Write(w);
        var r = new SyncReader(w.Written);
        Read(ref r);
    }
}

/// <summary>Receives every command a world executes, in order, for logging and replay.</summary>
/// <param name="tick">The tick the command ran in.</param>
/// <param name="entity">The target entity.</param>
/// <param name="commandId">The command's id.</param>
/// <param name="payload">The encoded arguments; valid only during the call.</param>
/// <param name="status">The outcome.</param>
public delegate void CommandObserver(long tick, EntityId entity, uint commandId, ReadOnlySpan<byte> payload, CommandStatus status);

/// <summary>The entity types a world can create while decoding a snapshot. Generated code fills it: call <c>EntityTypes.Register(registry)</c> from your assembly's generated namespace.</summary>
public sealed class EntityRegistry
{
    private readonly Dictionary<uint, Func<Entity>> _factories = [];
    private readonly Dictionary<uint, string> _names = [];

    /// <summary>Registers an entity type.</summary>
    /// <exception cref="DuplicateTypeException">The id is already taken by another type.</exception>
    public void Add(uint typeId, string name, Func<Entity> factory)
    {
        if (_names.TryGetValue(typeId, out var existing))
        {
            if (existing == name)
            {
                return;
            }

            throw new DuplicateTypeException($"Entity types '{existing}' and '{name}' share type id {typeId}. Give one a different [Entity(Id = ...)].");
        }

        _names[typeId] = name;
        _factories[typeId] = factory;
    }

    internal Entity? Create(uint typeId) => _factories.TryGetValue(typeId, out var f) ? f() : null;
}
