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
    private const uint ViewSnapshotFormat = 2;

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
    private int _executing;
    private bool _randomIncluded = true;

    /// <summary>Creates an empty world. Build the same way (registry, systems, tables) on every node, then add entities or load a snapshot.</summary>
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

    /// <summary>
    /// Why the last command handler rejected its command (set by <see cref="CommandReply.Reject"/>), valid while the observer is called
    /// for that command. The sync layer reads it to answer the submitter.
    /// </summary>
    internal string? LastRejectReason { get; set; }

    /// <summary>The ticket the command being executed carried when it was submitted, or 0. The sync layer uses it to find who sent the command.</summary>
    internal long CurrentTicket { get; private set; }

    /// <summary>
    /// True when the random streams are part of this world's view hash: always on an authority, and on a replica unless its authority
    /// keeps the streams from it.
    /// </summary>
    internal bool RandomIncluded => _randomIncluded;

    internal SyncWriter Scratch { get; } = new(256);

    internal EntityRegistry Registry => _registry;

    /// <summary>Commands accepted and waiting for the next tick. Snapshots do not hold them, so a recording must not start while any wait.</summary>
    public int PendingCommandCount => _pending.Count;

    /// <summary>Called for every command an authority accepts from outside its tick (connections, tests, local input), with the tick it will run in. The command log hooks in here; commands the sim queues itself are left out because the sim recreates them.</summary>
    internal CommandObserver? InputObserver { get; set; }

    /// <summary>Moves a replica's tick to the one its authority reports. Frames carry the tick, replicas never count it themselves.</summary>
    internal void SetReplicaTick(long tick) => Tick = tick;

    /// <summary>
    /// Adds an entity under a fresh id (one above the highest used so far) and returns it, so <c>var ship = world.Add(new
    /// Ship());</c> works. Call it from setup code, from sim code and from tests; views never add entities.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">A new entity that is not in any world.</param>
    /// <exception cref="InvalidOperationException">The entity is already in a world.</exception>
    [MutatesState]
    public T Add<T>(T entity)
        where T : Entity
    {
        AddWithId(entity, new EntityId(_nextId));
        return entity;
    }

    /// <summary>Adds an entity under a given id; replicas use it to mirror the authority's ids.</summary>
    /// <param name="entity">A new entity that is not in any world.</param>
    /// <param name="id">The id to give it; not zero and not used by another entity.</param>
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
    /// <param name="id">The entity to remove.</param>
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

    /// <summary>Finds an entity by id. Returns null for an id that was never used, was removed, or whose entity this replica has not received (or may not see).</summary>
    /// <param name="id">The entity's id.</param>
    public Entity? Find(EntityId id)
    {
        var index = IndexOf(id);
        return index >= 0 && !_entities[index].IsRemoved ? _entities[index] : null;
    }

    /// <summary>Finds an entity by id and type. Returns false when there is no such entity or it is of another type.</summary>
    /// <typeparam name="T">The entity type to look for.</typeparam>
    /// <param name="id">The entity's id.</param>
    /// <param name="entity">The entity when found; otherwise null.</param>
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
    /// <param name="system">The system. It is sim code and is checked by the sim analyzers.</param>
    [MutatesState]
    public void AddSystem(ISimSystem system) => _systems.Add(system);

    /// <summary>Adds a hot table. Tables are snapshotted and hashed in the order added.</summary>
    /// <typeparam name="T">The generated hot table type.</typeparam>
    /// <param name="table">The table to add.</param>
    /// <returns>The table, for chaining.</returns>
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
                Execute(cmd.Entity, cmd.CommandId, cmd.Payload, cmd.Ticket);
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
    /// <param name="entity">The entity to command.</param>
    /// <param name="commandId">The command's id (generated; entities expose it through <c>Send</c>).</param>
    /// <param name="payload">The encoded arguments.</param>
    /// <returns><see cref="CommandStatus.Queued"/> on an authority, <see cref="CommandStatus.Forwarded"/> on a replica, or why it was refused.</returns>
    public CommandStatus Submit(EntityId entity, uint commandId, ReadOnlySpan<byte> payload) => Submit(entity, commandId, payload, 0);

    /// <summary>Like <see cref="Submit(EntityId, uint, ReadOnlySpan{byte})"/>, tagging the command so the observer can tell who sent it.</summary>
    internal CommandStatus Submit(EntityId entity, uint commandId, ReadOnlySpan<byte> payload, long ticket)
    {
        if (payload.Length > SyncLimits.MaxCommandBytes)
        {
            return CommandStatus.Malformed;
        }

        // A predicted world forwards what outside code sends, but queues what its own sim sends, exactly as an authority would.
        if (Role == WorldRole.Replica || (Role == WorldRole.Predicted && !_ticking && _executing == 0))
        {
            Observer?.Invoke(Tick, entity, commandId, payload, CommandStatus.Forwarded);
            return CommandStatus.Forwarded;
        }

        if (!_ticking && Role == WorldRole.Authority)
        {
            InputObserver?.Invoke(Tick, entity, commandId, payload, CommandStatus.Queued);
        }

        _pending.Enqueue(new PendingCommand(entity, commandId, payload.ToArray(), ticket));
        return CommandStatus.Queued;
    }

    /// <summary>Submits a command from its wire form (entity id, command id, arguments), as read from a log or a connection. Untrusted bytes are fine: bad input returns <see cref="CommandStatus.Malformed"/>.</summary>
    /// <param name="wire">The bytes written by <see cref="EncodeCommand"/>.</param>
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
    /// <param name="writer">Receives the bytes.</param>
    /// <param name="entity">The entity the command is for.</param>
    /// <param name="commandId">The command's id.</param>
    /// <param name="payload">The encoded arguments.</param>
    public static void EncodeCommand(SyncWriter writer, EntityId entity, uint commandId, ReadOnlySpan<byte> payload)
    {
        writer.WriteVarUInt(entity.Value);
        writer.WriteVarUInt(commandId);
        writer.WriteRaw(payload);
    }

    /// <summary>Runs a command on this world right now, as the first thing of the next tick would. A predicted world uses it to show a command's effect the moment it is sent.</summary>
    internal CommandStatus ExecuteNow(EntityId id, uint commandId, ReadOnlySpan<byte> payload) => Execute(id, commandId, payload, 0);

    private CommandStatus Execute(EntityId id, uint commandId, ReadOnlySpan<byte> payload, long ticket)
    {
        LastRejectReason = null;
        CurrentTicket = ticket;
        var status = CommandStatus.UnknownEntity;
        _executing++;
        try
        {
            if (Find(id) is { } entity)
            {
                var r = new SyncReader(payload);
                status = entity.RunCommand(commandId, ref r);
            }
        }
        finally
        {
            _executing--;
        }

        Observer?.Invoke(Tick, id, commandId, payload, status);
        CurrentTicket = 0;
        LastRejectReason = null;
        return status;
    }

    /// <summary>
    /// Makes this world's state equal to <paramref name="source"/>'s without allocating once the entity set and storage match: entities with
    /// the same id and type keep their objects and have their fields assigned, entities that are gone are dropped and new ones are created.
    /// Both worlds must have been built the same way (registry, tables in the same order).
    /// </summary>
    internal void CopyFrom(World source)
    {
        if (_tables.Count != source._tables.Count)
        {
            throw new InvalidOperationException("The two worlds do not have the same tables. Build both the same way.");
        }

        Tick = source.Tick;
        _nextId = source._nextId;
        _randomIncluded = source._randomIncluded;
        Rng.CopyFrom(source.Rng);
        for (var i = 0; i < _tables.Count; i++)
        {
            if (_tables[i].Name != source._tables[i].Name)
            {
                throw new InvalidOperationException($"Table {i} is '{_tables[i].Name}' here and '{source._tables[i].Name}' in the source. Add tables in the same order.");
            }

            _tables[i].CopyFrom(source._tables[i]);
        }

        _pending.Clear();
        var j = 0;
        for (var k = 0; k < source._entities.Count; k++)
        {
            var from = source._entities[k];
            if (from.IsRemoved)
            {
                continue;
            }

            while (j < _entities.Count && _entities[j].Id.Value < from.Id.Value)
            {
                Detach(j);
            }

            if (j < _entities.Count && _entities[j].Id == from.Id && _entities[j].TypeId == from.TypeId)
            {
                _entities[j].CopyStateFrom(from);
                j++;
                continue;
            }

            if (j < _entities.Count && _entities[j].Id == from.Id)
            {
                Detach(j);
            }

            var created = _registry.Create(from.TypeId) ?? throw new InvalidOperationException($"Entity type {from.TypeName} is not registered in this world's registry.");
            created.World = this;
            created.Id = from.Id;
            created.CopyStateFrom(from);
            _entities.Insert(j, created);
            j++;
        }

        while (j < _entities.Count)
        {
            Detach(j);
        }
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

    /// <summary>
    /// The hash of what this world holds as a subscriber: equal to <see cref="ComputeHash"/> on an authority and on a replica that
    /// receives everything, and the hash of the filtered view on a replica that does not. The authority computes the same number for
    /// the same subscriber with <see cref="ComputeAudienceHash"/>, which is how drift is checked when subscribers see different things.
    /// </summary>
    public ulong ComputeViewHash() => ViewHash(null, _randomIncluded);

    /// <summary>
    /// The hash of the view an audience gets from this world: tick, the random streams when <paramref name="includeRandom"/>, the tables
    /// that are not hidden, and each entity the audience may see with only the properties it may see.
    /// </summary>
    /// <param name="audience">The subscriber's player and team.</param>
    /// <param name="includeRandom">Whether the subscriber receives the random streams.</param>
    public ulong ComputeAudienceHash(in Audience audience, bool includeRandom = true) => ViewHash(audience, includeRandom);

    private ulong ViewHash(Audience? audience, bool includeRandom)
    {
        var h = StateHash.Combine(0, (ulong)Tick);
        if (includeRandom)
        {
            _hashWriter.Reset();
            Rng.Write(_hashWriter);
            h = StateHash.Combine(h, StateHash.Of(_hashWriter.Written));
        }

        foreach (var table in _tables)
        {
            if (table.Visibility == Visibility.Hidden)
            {
                continue;
            }

            _hashWriter.Reset();
            table.Write(_hashWriter);
            h = StateHash.Combine(h, StateHash.Of(table.Name));
            h = StateHash.Combine(h, StateHash.Of(_hashWriter.Written));
        }

        foreach (var e in _entities)
        {
            if (audience is null)
            {
                h = StateHash.Combine(h, e.ComputeViewHash());
            }
            else if (e.TryGetAudienceMask(audience.Value, out var mask))
            {
                h = StateHash.Combine(h, e.ComputeViewHash(mask));
            }
        }

        return h;
    }

    /// <summary>Writes the whole world: tick, id counter, random streams, entities (with their schema versions) and tables. This is a save: <see cref="ReadSnapshot"/> restores it, through the entities' migrations if their schema grew since.</summary>
    /// <param name="w">Receives the bytes.</param>
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
    /// Writes the snapshot an audience is allowed to hold: only the entities it may see, each with only the properties it may see,
    /// without hidden tables, and without the random streams unless <paramref name="includeRandom"/>. <see cref="ReadSnapshot"/> reads it.
    /// </summary>
    public void WriteViewSnapshot(SyncWriter w, in Audience audience, bool includeRandom)
    {
        w.WriteFixed32(SnapshotMagic);
        w.WriteVarUInt(ViewSnapshotFormat);
        w.WriteVarInt(Tick);
        w.WriteVarUInt(_nextId);
        w.WriteVarUInt(includeRandom ? 1UL : 0UL);
        if (includeRandom)
        {
            Rng.Write(w);
        }

        ulong visible = 0;
        foreach (var e in _entities)
        {
            if (e.TryGetAudienceMask(audience, out _))
            {
                visible++;
            }
        }

        w.WriteVarUInt(visible);
        foreach (var e in _entities)
        {
            if (!e.TryGetAudienceMask(audience, out var mask))
            {
                continue;
            }

            mask = e.NormalizeMask(mask);
            _snapshotScratch.Reset();
            if (mask == ulong.MaxValue)
            {
                e.WriteState(_snapshotScratch);
            }
            else
            {
                e.WriteMaskedDelta(_snapshotScratch, mask);
            }

            w.WriteFixed32(e.TypeId);
            w.WriteVarUInt(e.Id.Value);
            w.WriteVarUInt(e.SchemaVersion);
            w.WriteByte(mask == ulong.MaxValue ? EntityFormFull : EntityFormMasked);
            w.WriteBytes(_snapshotScratch.Written);
        }

        var shown = 0;
        foreach (var t in _tables)
        {
            if (t.Visibility != Visibility.Hidden)
            {
                shown++;
            }
        }

        w.WriteVarUInt((ulong)shown);
        foreach (var t in _tables)
        {
            if (t.Visibility == Visibility.Hidden)
            {
                continue;
            }

            _snapshotScratch.Reset();
            t.Write(_snapshotScratch);
            w.WriteString(t.Name);
            w.WriteBytes(_snapshotScratch.Written);
        }
    }

    /// <summary>Entity body forms in a view snapshot or frame: the whole state, or a mask followed by the properties in it.</summary>
    internal const byte EntityFormFull = 0;

    /// <summary>See <see cref="EntityFormFull"/>.</summary>
    internal const byte EntityFormMasked = 1;

    /// <summary>Removes every entity and table row, as a replica does when its authority stops sending to it.</summary>
    internal void ClearReplica()
    {
        foreach (var e in _entities)
        {
            e.World = null;
            e.IsRemoved = true;
        }

        _entities.Clear();
        foreach (var t in _tables)
        {
            t.Clear();
        }
    }

    /// <summary>
    /// Replaces this world's contents with a snapshot from untrusted bytes. Entities of older schema versions go through their
    /// migrations. Restore into a freshly built world with the same tables and systems: on an error the world is in an unspecified
    /// state and should be discarded.
    /// </summary>
    /// <param name="bytes">A snapshot from <see cref="WriteSnapshot"/>, a save file or a server. Untrusted input is fine: it is bounds-checked and never throws.</param>
    /// <returns><see cref="SyncError.None"/> on success.</returns>
    [MutatesState]
    public SyncError ReadSnapshot(ReadOnlySpan<byte> bytes)
    {
        var r = new SyncReader(bytes);
        var magic = r.ReadFixed32();
        var format = r.ReadVarUInt();
        if (magic != SnapshotMagic || (format != SnapshotFormat && format != ViewSnapshotFormat))
        {
            return r.Ok ? SyncError.BadHeader : r.Error;
        }

        var tick = r.ReadVarInt();
        var nextId = r.ReadVarUInt();
        var filtered = format == ViewSnapshotFormat;
        var randomIncluded = true;
        if (filtered)
        {
            var flags = r.ReadVarUInt();
            if (r.Ok && flags > 1)
            {
                return SyncError.InvalidValue;
            }

            randomIncluded = flags == 1;
        }

        if (!r.Ok)
        {
            return r.Error;
        }

        if (tick < 0 || nextId == 0)
        {
            return SyncError.InvalidValue;
        }

        if (randomIncluded)
        {
            Rng.Read(ref r);
        }

        var count = r.ReadCount(SyncLimits.MaxEntities);
        var fresh = new List<Entity>(Math.Min(count, 1024));
        ulong previousId = 0;
        for (var i = 0; i < count && r.Ok; i++)
        {
            var typeId = r.ReadFixed32();
            var id = r.ReadVarUInt();
            var schema = r.ReadVarUInt();
            var form = filtered ? r.ReadByte() : EntityFormFull;
            var body = r.ReadBytes(int.MaxValue);
            if (!r.Ok)
            {
                break;
            }

            if (id <= previousId || id >= nextId || schema > uint.MaxValue || form > EntityFormMasked)
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
            if (form == EntityFormFull)
            {
                entity.ReadState((uint)schema, ref sub);
            }
            else
            {
                // A masked body is a delta in the current layout: a mask, then the properties the subscriber may see.
                if (schema != entity.SchemaVersion)
                {
                    return SyncError.UnsupportedVersion;
                }

                var peek = new SyncReader(body);
                var visible = peek.ReadVarUInt();
                entity.ApplyDelta(ref sub);
                entity.ClearDirty();
                entity.ViewMask = entity.NormalizeMask(visible);
            }

            sub.ExpectEnd();
            if (!sub.Ok)
            {
                return sub.Error;
            }

            entity.Id = new EntityId(id);
            fresh.Add(entity);
        }

        var tableCount = r.ReadCount(SyncLimits.MaxNamed);
        var expectedTables = 0;
        foreach (var t in _tables)
        {
            if (!filtered || t.Visibility != Visibility.Hidden)
            {
                expectedTables++;
            }
        }

        if (r.Ok && tableCount != expectedTables)
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

            if (table is null || (filtered && table.Visibility == Visibility.Hidden))
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
        _randomIncluded = randomIncluded;
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

    private readonly record struct PendingCommand(EntityId Entity, uint CommandId, byte[] Payload, long Ticket);
}
