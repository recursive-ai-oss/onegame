namespace OneGame.Core;

/// <summary>
/// Declares a partial struct as a one-shot sim event: something that happened on one tick ("wall completed at cell 12", "life lost") that
/// views, audio and the harness react to exactly once. Its public fields and properties are the payload, with the same rules as
/// <see cref="SyncedStructAttribute"/> (numbers, bool, string, enums, <c>Fixed</c> and the other synced value types; never floats).
/// Sim code emits it with <see cref="Entity.Emit{T}"/> or <see cref="World.Emit{T}"/>, and views read it through <see cref="World.Events"/>.
/// </summary>
/// <example>
/// <code>
/// [SimEvent] public partial struct WallDone { public int Cell { get; set; } }
///
/// // sim:  Emit(new WallDone { Cell = 12 });
/// // view: var events = world.Events.NewReader();   // once
/// //       while (events.Next&lt;WallDone&gt;(out var done, out var info)) PlayPop(done.Cell);
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class SimEventAttribute : Attribute
{
    /// <summary>
    /// Who may receive the event. <see cref="Visibility.Everyone"/> (default) follows the emitting entity's own visibility and interest;
    /// <see cref="Visibility.Owner"/> and <see cref="Visibility.Team"/> narrow it to the emitting entity's owner or team (they need an
    /// entity, so emit them with <see cref="Entity.Emit{T}"/>); <see cref="Visibility.Hidden"/> keeps the event on the authority.
    /// </summary>
    public Visibility Visibility { get; set; } = Visibility.Everyone;
}

/// <summary>A struct the generator made from <see cref="SimEventAttribute"/>: a synced value with a stable type id.</summary>
/// <typeparam name="TSelf">The event struct.</typeparam>
public interface ISimEvent<TSelf> : ISyncValue<TSelf>
    where TSelf : ISimEvent<TSelf>
{
    /// <summary>The event's id on the wire: a hash of its full type name.</summary>
    static abstract uint EventTypeId { get; }

    /// <summary>The event's name in harness output and scenarios (the type name).</summary>
    static abstract string EventName { get; }

    /// <summary>Who may receive the event.</summary>
    static abstract Visibility EventVisibility { get; }
}

/// <summary>Writes the JSON of an event payload, for the harness.</summary>
/// <param name="reader">Reads the payload.</param>
/// <param name="json">Receives the JSON value.</param>
public delegate void EventJsonWriter(ref SyncReader reader, JsonOut json);

/// <summary>Where and when an event happened.</summary>
/// <param name="Tick">The tick the sim was on when it emitted the event (the same number a command that ran then reports).</param>
/// <param name="Entity">The entity that emitted it, or <see cref="EntityId.None"/> for a world event. A replica may not hold that entity.</param>
/// <param name="Sequence">The event's position in this log; rises by one per event and is only comparable within one log.</param>
public readonly record struct EventInfo(long Tick, EntityId Entity, long Sequence);

/// <summary>One event as read from an <see cref="EventLog"/>. Valid until the reader's next call.</summary>
public readonly ref struct EventRecord
{
    internal EventRecord(long tick, EntityId entity, long sequence, uint typeId, ReadOnlySpan<byte> payload)
    {
        Tick = tick;
        Entity = entity;
        Sequence = sequence;
        TypeId = typeId;
        Payload = payload;
    }

    /// <summary>The tick the event happened on.</summary>
    public long Tick { get; }

    /// <summary>The emitting entity, or <see cref="EntityId.None"/>.</summary>
    public EntityId Entity { get; }

    /// <summary>The event's position in its log.</summary>
    public long Sequence { get; }

    /// <summary>The event type's id (<see cref="ISimEvent{TSelf}.EventTypeId"/>).</summary>
    public uint TypeId { get; }

    /// <summary>The encoded payload.</summary>
    public ReadOnlySpan<byte> Payload { get; }

    /// <summary>The position data without the payload, which can be kept after the reader moves on.</summary>
    public EventInfo Info => new(Tick, Entity, Sequence);

    /// <summary>True when the event is a <typeparamref name="T"/>.</summary>
    public bool Is<T>()
        where T : ISimEvent<T>
        => TypeId == T.EventTypeId;

    /// <summary>Decodes the payload when the event is a <typeparamref name="T"/>.</summary>
    /// <param name="value">The decoded event, or default.</param>
    /// <returns>False when the event is of another type or the payload does not decode.</returns>
    public bool TryRead<T>(out T value)
        where T : ISimEvent<T>
    {
        value = default!;
        if (TypeId != T.EventTypeId)
        {
            return false;
        }

        var r = new SyncReader(Payload);
        var v = T.Read(ref r);
        r.ExpectEnd();
        if (!r.Ok)
        {
            return false;
        }

        value = v;
        return true;
    }
}

/// <summary>
/// The events a world's sim emitted, as a bounded ring of the most recent ones. Authority worlds record what their sim emits; a replica world
/// records the events its authority sent it (only those its audience may see). Events are output of a tick, not state: they are not saved,
/// not snapshotted, not part of any state hash, and a restore or a late join starts with an empty log. Read them with an <see cref="EventReader"/>.
/// </summary>
public sealed class EventLog
{
    /// <summary>How many events a log keeps; a reader that falls further behind loses the oldest (<see cref="EventReader.Missed"/>).</summary>
    public const int Capacity = 512;

    /// <summary>The largest encoded payload of one event, in bytes. Emitting a bigger one throws.</summary>
    public const int MaxPayloadBytes = 512;

    private readonly EntityRegistry _registry;
    private readonly SyncWriter _scratch = new(64);
    private Slot[]? _slots;
    private long _count;
    private long _floor;
    private int _epoch;
    private int _perTickLimit = 128;
    private long _tickMark = long.MinValue;
    private int _tickCount;
    private ulong _hash;

    internal EventLog(EntityRegistry registry) => _registry = registry;

    /// <summary>Events appended since the log was created. The next event gets this as its <see cref="EventInfo.Sequence"/>.</summary>
    public long Count => _count;

    /// <summary>The sequence of the oldest event still held.</summary>
    public long FirstRetained => Math.Max(_floor, _count - Capacity);

    /// <summary>Events dropped because a tick emitted more than <see cref="EventsPerTickLimit"/>.</summary>
    public long Dropped { get; private set; }

    /// <summary>Counts every reset (snapshot restore, load): readers see it and start from the end.</summary>
    public int Epoch => _epoch;

    /// <summary>
    /// A hash of every event appended since the log was created or last reset (tick, type, entity and payload, in order). Not part of the
    /// world's state hash; tests use it to compare two runs' event streams cheaply.
    /// </summary>
    public ulong StreamHash => _hash;

    /// <summary>How many events one tick may emit before the rest are dropped and counted in <see cref="Dropped"/> (1 to <see cref="Capacity"/>, default 128). Deterministic: the same tick drops the same events everywhere.</summary>
    public int EventsPerTickLimit
    {
        get => _perTickLimit;
        set => _perTickLimit = Math.Clamp(value, 1, Capacity);
    }

    /// <summary>Starts a reader at the end of the log: it sees only events emitted from now on.</summary>
    public EventReader NewReader() => new(this);

    /// <summary>Starts a reader at the oldest event still held, so it sees the history first.</summary>
    public EventReader NewReaderFromStart()
    {
        var reader = new EventReader(this);
        reader.Rewind();
        return reader;
    }

    /// <summary>Reads one retained event by sequence.</summary>
    /// <param name="sequence">The event's <see cref="EventInfo.Sequence"/>.</param>
    /// <param name="record">The event; valid until the log changes.</param>
    /// <returns>False when the event is older than the ring holds or not emitted yet.</returns>
    public bool TryGet(long sequence, out EventRecord record)
    {
        if (sequence < FirstRetained || sequence >= _count || _slots is null)
        {
            record = default;
            return false;
        }

        ref var s = ref _slots[sequence % Capacity];
        record = new EventRecord(s.Tick, new EntityId(s.Entity), sequence, s.TypeId, new ReadOnlySpan<byte>(s.Data, 0, s.Length));
        return true;
    }

    /// <summary>Writes the retained events with a sequence of at least <paramref name="since"/> (at most the newest <paramref name="max"/>) as a JSON array: sequence, tick, entity (<c>Type#id</c> is not known here, so the raw id), type name and the payload fields.</summary>
    public void WriteJson(JsonOut json, long since = 0, int max = 256)
    {
        var first = Math.Max(Math.Max(since, FirstRetained), _count - Math.Max(max, 0));
        json.BeginArray();
        for (var n = first; n < _count; n++)
        {
            TryGet(n, out var e);
            json.BeginObject();
            json.Name("sequence");
            json.Value(e.Sequence);
            json.Name("tick");
            json.Value(e.Tick);
            json.Name("entity");
            json.Value((long)e.Entity.Value);
            json.Name("type");
            var known = _registry.TryGetEvent(e.TypeId, out var name, out var write);
            json.Value(known ? name : "0x" + e.TypeId.ToString("x8", System.Globalization.CultureInfo.InvariantCulture));
            json.Name("data");
            if (known)
            {
                var r = new SyncReader(e.Payload);
                write(ref r, json);
            }
            else
            {
                json.Value(Convert.ToHexString(e.Payload));
            }

            json.EndObject();
        }

        json.EndArray();
    }

    /// <summary>Forgets every event, as a snapshot restore does. Readers start from the end of the (now empty) log.</summary>
    internal void Reset()
    {
        _floor = _count;
        _epoch++;
        _hash = 0;
        _tickMark = long.MinValue;
        _tickCount = 0;
    }

    /// <summary>Encodes and appends an event emitted by sim code.</summary>
    internal bool Record<T>(long tick, in T ev, Entity? source)
        where T : struct, ISimEvent<T>
    {
        var visibility = T.EventVisibility;
        if (source is null && (visibility == Visibility.Owner || visibility == Visibility.Team))
        {
            throw new InvalidOperationException($"{T.EventName} is {visibility}-only, which needs the emitting entity to match against. Emit it with entity.Emit(...), or make it Everyone.");
        }

        _scratch.Reset();
        ev.Write(_scratch);
        if (_scratch.Length > MaxPayloadBytes)
        {
            throw new ArgumentException($"{T.EventName} encodes to {_scratch.Length} bytes; an event holds at most {MaxPayloadBytes}. Send big data as entity state and emit a small event that points at it.");
        }

        return Append(tick, T.EventTypeId, visibility, source?.Id.Value ?? 0, source, _scratch.Written, enforceLimit: true);
    }

    /// <summary>Appends an event a replica received.</summary>
    internal bool AppendReceived(long tick, uint typeId, ulong entity, ReadOnlySpan<byte> payload)
        => Append(tick, typeId, Visibility.Everyone, entity, null, payload, enforceLimit: false);

    private bool Append(long tick, uint typeId, Visibility visibility, ulong entity, Entity? source, ReadOnlySpan<byte> payload, bool enforceLimit)
    {
        if (tick != _tickMark)
        {
            _tickMark = tick;
            _tickCount = 0;
        }

        // A replica applies what its authority sent, whatever limit the authority ran with.
        if (enforceLimit && _tickCount >= _perTickLimit)
        {
            Dropped++;
            return false;
        }

        _tickCount++;
        _slots ??= new Slot[Capacity];
        ref var s = ref _slots[_count % Capacity];
        if (s.Data is null || s.Data.Length < payload.Length)
        {
            s.Data = new byte[Math.Max(16, (int)System.Numerics.BitOperations.RoundUpToPowerOf2((uint)payload.Length))];
        }

        payload.CopyTo(s.Data);
        s.Length = payload.Length;
        s.Tick = tick;
        s.TypeId = typeId;
        s.Entity = entity;
        s.Visibility = visibility;
        s.Source = source;
        _hash = StateHash.Combine(_hash, StateHash.Combine(StateHash.Combine((ulong)tick, typeId | ((ulong)entity << 32)), StateHash.Of(payload)));
        _count++;
        return true;
    }

    /// <summary>What the sync layer needs to decide who gets event <paramref name="sequence"/>.</summary>
    internal void Describe(long sequence, out Visibility visibility, out Entity? source, out EventRecord record)
    {
        ref var s = ref _slots![sequence % Capacity];
        visibility = s.Visibility;
        source = s.Source;
        record = new EventRecord(s.Tick, new EntityId(s.Entity), sequence, s.TypeId, new ReadOnlySpan<byte>(s.Data, 0, s.Length));
    }

    /// <summary>The sequence the next publish starts from after a reset, and where a fresh reader starts.</summary>
    internal long Floor => _floor;

    private struct Slot
    {
        public long Tick;
        public uint TypeId;
        public ulong Entity;
        public Visibility Visibility;
        public Entity? Source;
        public byte[]? Data;
        public int Length;
    }
}

/// <summary>
/// A view's place in an <see cref="EventLog"/>. Create one per consumer (a view, the audio code, a test) and call <see cref="Next(out EventRecord)"/>
/// each frame: every event is delivered once, in emit order, with the tick it happened on. A reader never sees events that were already in the
/// log when it was created, nor anything from before a snapshot restore. Create a new reader when the game's world is replaced.
/// </summary>
public sealed class EventReader
{
    private readonly EventLog _log;
    private long _next;
    private int _epoch;

    internal EventReader(EventLog log)
    {
        _log = log;
        _next = log.Count;
        _epoch = log.Epoch;
    }

    /// <summary>Events lost because the reader fell more than <see cref="EventLog.Capacity"/> behind.</summary>
    public long Missed { get; private set; }

    /// <summary>How many times the log was reset (a snapshot restore or resync) while this reader held a position; the events of the reset span are gone.</summary>
    public int Restarts { get; private set; }

    /// <summary>How many events wait for this reader.</summary>
    public long Pending
    {
        get
        {
            Sync();
            return _log.Count - _next;
        }
    }

    internal void Rewind()
    {
        _next = _log.FirstRetained;
        _epoch = _log.Epoch;
    }

    /// <summary>Moves to the end of the log, skipping whatever waits.</summary>
    public void SkipToEnd()
    {
        _next = _log.Count;
        _epoch = _log.Epoch;
    }

    private void Sync()
    {
        if (_epoch != _log.Epoch)
        {
            _epoch = _log.Epoch;
            _next = Math.Max(_next, _log.Floor);
            Restarts++;
        }

        var first = _log.FirstRetained;
        if (_next < first)
        {
            Missed += first - _next;
            _next = first;
        }
    }

    /// <summary>Takes the next event of any type.</summary>
    /// <param name="record">The event; valid until the next call.</param>
    /// <returns>False when none is waiting.</returns>
    public bool Next(out EventRecord record)
    {
        Sync();
        if (_next >= _log.Count)
        {
            record = default;
            return false;
        }

        _log.TryGet(_next++, out record);
        return true;
    }

    /// <summary>Takes the next event that is a <typeparamref name="T"/>, passing over (and consuming) events of other types. Use one reader per kind of consumer.</summary>
    /// <param name="value">The decoded event.</param>
    /// <param name="info">When and where it happened.</param>
    /// <returns>False when no <typeparamref name="T"/> is waiting.</returns>
    public bool Next<T>(out T value, out EventInfo info)
        where T : ISimEvent<T>
    {
        while (Next(out var record))
        {
            if (record.TryRead(out value))
            {
                info = record.Info;
                return true;
            }
        }

        value = default!;
        info = default;
        return false;
    }
}
