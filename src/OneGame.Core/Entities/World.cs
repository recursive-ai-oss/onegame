namespace OneGame.Core;

/// <summary>
/// The simulation: entities in a deterministic order, hot tables, systems, the named random streams and the tick counter.
/// One <see cref="Step"/> runs queued commands, then each entity's <see cref="Entity.OnTick"/> in id order, then each system in
/// the order added. Nothing here reads a clock, a thread or a hash-ordered collection, so the same seed and the same commands give
/// the same state, tick for tick, on every platform.
/// </summary>
public sealed class World
{
    private const uint SnapshotMagic = 0x4E53474F; // "OGSN" little-endian
    private const uint SnapshotFormat = 1;

    private readonly EntityRegistry _registry;
    private readonly List<Entity> _entities = [];
    private readonly List<ISimSystem> _systems = [];
    private readonly List<IHotTable> _tables = [];
    private readonly Queue<PendingCommand> _pending = new();
    private readonly List<Entity> _removedDuringTick = [];
    private readonly SyncWriter _hashWriter = new(4096);
    private readonly SyncWriter _snapshotScratch = new(1024);
    private ulong _nextId = 1;
    private bool _ticking;

    /// <summary>Creates an empty world.</summary>
    /// <param name="seed">Seeds every random stream.</param>
    /// <param name="registry">The entity types snapshots may contain.</param>
    /// <param name="role">Authority, or replica of another node's world.</param>
    public World(ulong seed, EntityRegistry registry, WorldRole role = WorldRole.Authority)
    {
        Seed = seed;
        Role = role;
        _registry = registry;
        Rng = new RngStreams(seed);
    }

    /// <summary>The seed the random streams derive from.</summary>
    public ulong Seed { get; }

    /// <summary>Whether this world is the authority or a replica.</summary>
    public WorldRole Role { get; }

    /// <summary>The number of completed ticks. Replicas take it from the snapshots they apply.</summary>
    public long Tick { get; private set; }

    /// <summary>The named random streams.</summary>
    public RngStreams Rng { get; }

    /// <summary>Entities in id order.</summary>
    public IReadOnlyList<Entity> Entities => _entities;

    /// <summary>The hot tables, in the order added.</summary>
    public IReadOnlyList<IHotTable> Tables => _tables;

    /// <summary>Called for every command this world executes, queues-then-runs on an authority or forwards on a replica. The command log hooks in here.</summary>
    public CommandObserver? Observer { get; set; }

    internal SyncWriter Scratch { get; } = new(256);

    internal EntityRegistry Registry => _registry;

    /// <summary>Commands accepted and waiting for the next tick. Snapshots do not hold them, so a recording must not start while any wait.</summary>
    internal int PendingCommandCount => _pending.Count;

    /// <summary>Called for every command an authority accepts from outside its tick (connections, tests, local input), with the tick it will run in. The command log hooks in here; commands the sim queues itself are left out because the sim recreates them.</summary>
    internal CommandObserver? InputObserver { get; set; }

    /// <summary>Moves a replica's tick to the one its authority reports. Frames carry the tick, replicas never count it themselves.</summary>
    internal void SetReplicaTick(long tick) => Tick = tick;

    /// <summary>Adds an entity under a fresh id and returns it.</summary>
    /// <exception cref="InvalidOperationException">The entity is already in a world.</exception>
    [MutatesState]
    public T Add<T>(T entity)
        where T : Entity
    {
        AddWithId(entity, new EntityId(_nextId));
        return entity;
    }

    /// <summary>Adds an entity under a given id; replicas use it to mirror the authority's ids.</summary>
    /// <exception cref="InvalidOperationException">The entity is already in a world, or the id is zero or taken.</exception>
    [MutatesState]
    public void AddWithId(Entity entity, EntityId id)
    {
        if (entity.World is not null)
        {
            throw new InvalidOperationException($"{entity.TypeName} is already in a world.");
        }

        if (!id.IsValid || IndexOf(id) >= 0)
        {
            throw new InvalidOperationException($"Entity id {id} is zero or already in use.");
        }

        entity.World = this;
        entity.Id = id;
        _entities.Insert(~IndexOf(id), entity);
        if (id.Value >= _nextId)
        {
            _nextId = id.Value + 1;
        }
    }

    /// <summary>Removes an entity. During a tick the removal takes effect when the tick ends.</summary>
    /// <returns>False if no such entity exists.</returns>
    [MutatesState]
    public bool Remove(EntityId id)
    {
        var index = IndexOf(id);
        if (index < 0 || _entities[index].IsRemoved)
        {
            return false;
        }

        var entity = _entities[index];
        entity.IsRemoved = true;
        if (_ticking)
        {
            _removedDuringTick.Add(entity);
        }
        else
        {
            Detach(index);
        }

        return true;
    }

    private void Detach(int index)
    {
        var entity = _entities[index];
        _entities.RemoveAt(index);
        entity.World = null;
        entity.IsRemoved = true;
    }

    /// <summary>Finds an entity by id, or null.</summary>
    public Entity? Find(EntityId id)
    {
        var index = IndexOf(id);
        return index >= 0 && !_entities[index].IsRemoved ? _entities[index] : null;
    }

    /// <summary>Finds an entity of a given type by id.</summary>
    public bool TryGet<T>(EntityId id, out T entity)
        where T : Entity
    {
        if (Find(id) is T found)
        {
            entity = found;
            return true;
        }

        entity = null!;
        return false;
    }

    private int IndexOf(EntityId id)
    {
        int lo = 0, hi = _entities.Count - 1;
        while (lo <= hi)
        {
            var mid = (lo + hi) >>> 1;
            var c = _entities[mid].Id.Value.CompareTo(id.Value);
            if (c == 0)
            {
                return mid;
            }

            if (c < 0)
            {
                lo = mid + 1;
            }
            else
            {
                hi = mid - 1;
            }
        }

        return ~lo;
    }

    /// <summary>Adds a system. Systems tick in the order added, so add them in the same order on every node.</summary>
    [MutatesState]
    public void AddSystem(ISimSystem system) => _systems.Add(system);

    /// <summary>Adds a hot table. Tables are snapshotted and hashed in the order added.</summary>
    /// <exception cref="DuplicateTypeException">A table with this name exists.</exception>
    [MutatesState]
    public T AddTable<T>(T table)
        where T : IHotTable
    {
        foreach (var t in _tables)
        {
            if (t.Name == table.Name)
            {
                throw new DuplicateTypeException($"A table named '{table.Name}' is already in this world.");
            }
        }

        _tables.Add(table);
        return table;
    }

    /// <summary>Advances the simulation by one tick.</summary>
    /// <exception cref="ReplicaWriteException">This world is a replica.</exception>
    [MutatesState]
    public void Step()
    {
        if (Role == WorldRole.Replica)
        {
            throw new ReplicaWriteException("A replica world does not simulate. Apply snapshots and deltas from its authority instead.");
        }

        _ticking = true;
        try
        {
            // Commands queued before this tick run first, in arrival order. Commands the sim queues now wait for the next tick.
            for (var n = _pending.Count; n > 0; n--)
            {
                var cmd = _pending.Dequeue();
                Execute(cmd.Entity, cmd.CommandId, cmd.Payload);
            }

            // Entities added during the tick start next tick, and so the loop stops at the count it began with.
            var count = _entities.Count;
            for (var i = 0; i < count; i++)
            {
                var e = _entities[i];
                if (!e.IsRemoved)
                {
                    e.RunTick();
                }
            }

            for (var i = 0; i < _systems.Count; i++)
            {
                _systems[i].Tick(this);
            }
        }
        finally
        {
            _ticking = false;
            foreach (var e in _removedDuringTick)
            {
                var idx = IndexOf(e.Id);
                if (idx >= 0)
                {
                    Detach(idx);
                }
            }

            _removedDuringTick.Clear();
        }

        Tick++;
    }

    /// <summary>
    /// Sends a command to an entity. An authority queues it for the next tick; a replica passes it on through
    /// <see cref="Observer"/> for the networking layer to deliver.
    /// </summary>
    public CommandStatus Submit(EntityId entity, uint commandId, ReadOnlySpan<byte> payload)
    {
        if (payload.Length > SyncLimits.MaxCommandBytes)
        {
            return CommandStatus.Malformed;
        }

        if (Role == WorldRole.Replica)
        {
            Observer?.Invoke(Tick, entity, commandId, payload, CommandStatus.Forwarded);
            return CommandStatus.Forwarded;
        }

        if (!_ticking)
        {
            InputObserver?.Invoke(Tick, entity, commandId, payload, CommandStatus.Queued);
        }

        _pending.Enqueue(new PendingCommand(entity, commandId, payload.ToArray()));
        return CommandStatus.Queued;
    }

    /// <summary>Submits a command from its wire form (entity id, command id, arguments), as read from a log or a connection. Untrusted bytes are fine: bad input returns <see cref="CommandStatus.Malformed"/>.</summary>
    public CommandStatus SubmitEncoded(ReadOnlySpan<byte> wire)
    {
        var r = new SyncReader(wire);
        var entity = r.ReadVarUInt();
        var command = r.ReadVarUInt();
        if (!r.Ok || command > uint.MaxValue)
        {
            return CommandStatus.Malformed;
        }

        return Submit(new EntityId(entity), (uint)command, wire[r.Position..]);
    }

    /// <summary>The wire form of a command, the inverse of <see cref="SubmitEncoded"/>.</summary>
    public static void EncodeCommand(SyncWriter writer, EntityId entity, uint commandId, ReadOnlySpan<byte> payload)
    {
        writer.WriteVarUInt(entity.Value);
        writer.WriteVarUInt(commandId);
        writer.WriteRaw(payload);
    }

    private void Execute(EntityId id, uint commandId, byte[] payload)
    {
        var status = CommandStatus.UnknownEntity;
        if (Find(id) is { } entity)
        {
            var r = new SyncReader(payload);
            status = entity.RunCommand(commandId, ref r);
        }

        Observer?.Invoke(Tick, id, commandId, payload, status);
    }

    /// <summary>
    /// A 64-bit hash of everything that defines the simulation: tick, random streams, tables and every entity's state. Two nodes
    /// with equal hashes at the same tick have equal state. It allocates nothing once the internal buffers have grown.
    /// </summary>
    public ulong ComputeHash()
    {
        var h = StateHash.Combine(0, (ulong)Tick);
        _hashWriter.Reset();
        Rng.Write(_hashWriter);
        h = StateHash.Combine(h, StateHash.Of(_hashWriter.Written));
        foreach (var table in _tables)
        {
            _hashWriter.Reset();
            table.Write(_hashWriter);
            h = StateHash.Combine(h, StateHash.Of(table.Name));
            h = StateHash.Combine(h, StateHash.Of(_hashWriter.Written));
        }

        foreach (var e in _entities)
        {
            h = StateHash.Combine(h, e.ComputeHash());
        }

        return h;
    }

    /// <summary>Writes the whole world: tick, id counter, random streams, entities (with their schema versions) and tables.</summary>
    public void WriteSnapshot(SyncWriter w)
    {
        w.WriteFixed32(SnapshotMagic);
        w.WriteVarUInt(SnapshotFormat);
        w.WriteVarInt(Tick);
        w.WriteVarUInt(_nextId);
        Rng.Write(w);
        w.WriteVarUInt((ulong)_entities.Count);
        foreach (var e in _entities)
        {
            _snapshotScratch.Reset();
            e.WriteState(_snapshotScratch);
            w.WriteFixed32(e.TypeId);
            w.WriteVarUInt(e.Id.Value);
            w.WriteVarUInt(e.SchemaVersion);
            w.WriteBytes(_snapshotScratch.Written);
        }

        w.WriteVarUInt((ulong)_tables.Count);
        foreach (var t in _tables)
        {
            _snapshotScratch.Reset();
            t.Write(_snapshotScratch);
            w.WriteString(t.Name);
            w.WriteBytes(_snapshotScratch.Written);
        }
    }

    /// <summary>
    /// Replaces this world's contents with a snapshot from untrusted bytes. Entities of older schema versions go through their
    /// migrations. Restore into a freshly built world with the same tables and systems: on an error the world is in an unspecified
    /// state and should be discarded.
    /// </summary>
    /// <returns><see cref="SyncError.None"/> on success.</returns>
    [MutatesState]
    public SyncError ReadSnapshot(ReadOnlySpan<byte> bytes)
    {
        var r = new SyncReader(bytes);
        if (r.ReadFixed32() != SnapshotMagic || r.ReadVarUInt() != SnapshotFormat)
        {
            return r.Ok ? SyncError.BadHeader : r.Error;
        }

        var tick = r.ReadVarInt();
        var nextId = r.ReadVarUInt();
        if (!r.Ok)
        {
            return r.Error;
        }

        if (tick < 0 || nextId == 0)
        {
            return SyncError.InvalidValue;
        }

        Rng.Read(ref r);
        var count = r.ReadCount(SyncLimits.MaxEntities);
        var fresh = new List<Entity>(Math.Min(count, 1024));
        ulong previousId = 0;
        for (var i = 0; i < count && r.Ok; i++)
        {
            var typeId = r.ReadFixed32();
            var id = r.ReadVarUInt();
            var schema = r.ReadVarUInt();
            var body = r.ReadBytes(int.MaxValue);
            if (!r.Ok)
            {
                break;
            }

            if (id <= previousId || id >= nextId || schema > uint.MaxValue)
            {
                r.Fail(SyncError.InvalidValue);
                break;
            }

            previousId = id;
            var entity = _registry.Create(typeId);
            if (entity is null)
            {
                r.Fail(SyncError.UnknownType);
                break;
            }

            var sub = new SyncReader(body);
            entity.ReadState((uint)schema, ref sub);
            sub.ExpectEnd();
            if (!sub.Ok)
            {
                return sub.Error;
            }

            entity.Id = new EntityId(id);
            fresh.Add(entity);
        }

        var tableCount = r.ReadCount(SyncLimits.MaxNamed);
        if (r.Ok && tableCount != _tables.Count)
        {
            return SyncError.Mismatch;
        }

        for (var i = 0; i < tableCount && r.Ok; i++)
        {
            var name = r.ReadString();
            var body = r.ReadBytes(int.MaxValue);
            if (!r.Ok)
            {
                break;
            }

            IHotTable? table = null;
            foreach (var t in _tables)
            {
                if (t.Name == name)
                {
                    table = t;
                }
            }

            if (table is null)
            {
                return SyncError.Mismatch;
            }

            var sub = new SyncReader(body);
            table.Read(ref sub);
            sub.ExpectEnd();
            if (!sub.Ok)
            {
                return sub.Error;
            }
        }

        r.ExpectEnd();
        if (!r.Ok)
        {
            return r.Error;
        }

        foreach (var old in _entities)
        {
            old.World = null;
            old.IsRemoved = true;
        }

        _entities.Clear();
        foreach (var e in fresh)
        {
            e.World = this;
            _entities.Add(e);
        }

        _nextId = nextId;
        Tick = tick;
        _pending.Clear();
        return SyncError.None;
    }

    /// <summary>The world as a JSON document, for inspection: tick, seed, every entity and every table.</summary>
    public string DumpJson()
    {
        var j = new JsonOut();
        j.BeginObject();
        j.Name("tick");
        j.Value(Tick);
        j.Name("seed");
        j.Value(Seed);
        j.Name("hash");
        j.Value(ComputeHash().ToString("x16", System.Globalization.CultureInfo.InvariantCulture));
        j.Name("entities");
        j.BeginArray();
        foreach (var e in _entities)
        {
            e.WriteJson(j);
        }

        j.EndArray();
        j.Name("tables");
        j.BeginObject();
        foreach (var t in _tables)
        {
            j.Name(t.Name);
            t.WriteJson(j);
        }

        j.EndObject();
        j.EndObject();
        return j.ToString();
    }

    private readonly record struct PendingCommand(EntityId Entity, uint CommandId, byte[] Payload);
}
