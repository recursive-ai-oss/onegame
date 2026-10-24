namespace OneGame.Sync;

/// <summary>A scope whose authority is this node: it owns the simulating <see cref="World"/>, accepts commands, and publishes a snapshot then one frame per tick to each subscriber.</summary>
public sealed class AuthorityScope
{
    private const int OutcomeHistory = 256;
    private const int DigestRing = 4;

    private readonly List<Connection> _subscribers = [];
    private readonly CommandObserver? _chainedObserver;
    private readonly CommandOutcome[] _outcomes = new CommandOutcome[OutcomeHistory];
    private int _outcomeCount;

    // Frame building: what the subscribers have been told so far.
    private List<ulong> _known = [];
    private List<ulong> _nextKnown = [];
    private readonly List<Blob> _tableBlobs = [];
    private readonly Blob _rngBlob = new();
    private readonly SyncWriter _removed = new(256);
    private readonly SyncWriter _added = new(1024);
    private readonly SyncWriter _deltas = new(1024);
    private readonly SyncWriter _tables = new(1024);
    private readonly SyncWriter _entity = new(256);
    private readonly SyncWriter _frame = new(4096);
    private readonly SyncWriter _scratch = new(1024);
    private readonly StateDigest[] _digests = new StateDigest[DigestRing];
    private int _digestNext;

    private Recorder? _recorder;

    internal AuthorityScope(Node node, ScopeId id, World world)
    {
        Node = node;
        Id = id;
        World = world;
        _chainedObserver = world.Observer;
        world.Observer = OnCommandExecuted;
        world.InputObserver = OnInput;
        for (var i = 0; i < DigestRing; i++)
        {
            _digests[i] = new StateDigest();
        }
    }

    /// <summary>The scope id.</summary>
    public ScopeId Id { get; }

    /// <summary>The node that holds the authority.</summary>
    public Node Node { get; }

    /// <summary>The simulating world.</summary>
    public World World { get; }

    /// <summary>The connections currently subscribed.</summary>
    public IReadOnlyList<Connection> Subscribers => _subscribers;

    /// <summary>Total commands the world has run for this scope, counting every outcome.</summary>
    public long CommandsRun { get; private set; }

    /// <summary>The most recent command outcomes (up to 256), oldest first. Handler rejections show up here.</summary>
    public IEnumerable<CommandOutcome> Outcomes
    {
        get
        {
            var n = Math.Min(_outcomeCount, OutcomeHistory);
            for (var i = 0; i < n; i++)
            {
                yield return _outcomes[(_outcomeCount - n + i) % OutcomeHistory];
            }
        }
    }

    /// <summary>The recorder, while one is running.</summary>
    public Recorder? ActiveRecorder => _recorder;

    /// <summary>The scope's current hash.</summary>
    public ulong Hash => World.ComputeHash();

    /// <summary>Runs one tick and publishes the resulting frame.</summary>
    public void StepAndPublish()
    {
        World.Step();
        Publish();
    }

    /// <summary>Starts recording: takes the start snapshot now, then logs every command and the hash checkpoints.</summary>
    /// <exception cref="InvalidOperationException">A recording is already running.</exception>
    public Recorder StartRecording(RecordingOptions? options = null)
    {
        if (_recorder is not null)
        {
            throw new InvalidOperationException("A recording is already running on this scope. Stop it first.");
        }

        _recorder = new Recorder(this, options ?? new RecordingOptions());
        return _recorder;
    }

    internal void RecorderStopped() => _recorder = null;

    /// <summary>Writes a save file of the scope's current state.</summary>
    public byte[] Save() => SaveFile.Write(World);

    /// <summary>
    /// Replaces the scope's state with a save. Subscribers get a fresh snapshot. The world must have been built the same way as the one saved.
    /// </summary>
    /// <exception cref="InvalidOperationException">A recording is running.</exception>
    public LoadResult Load(ReadOnlySpan<byte> save)
    {
        if (_recorder is not null)
        {
            throw new InvalidOperationException("Stop the recording before loading a save: the log would no longer describe the run.");
        }

        var result = SaveFile.Load(save, World);
        if (result.Status == SaveStatus.Ok)
        {
            Rebaseline();
            foreach (var s in _subscribers)
            {
                SendSnapshot(s);
            }
        }

        return result;
    }

    internal void Subscribe(Connection connection)
    {
        if (!_subscribers.Contains(connection))
        {
            if (_subscribers.Count == 0)
            {
                Rebaseline();
            }

            _subscribers.Add(connection);
        }

        SendSnapshot(connection);
    }

    internal void Unsubscribe(Connection connection) => _subscribers.Remove(connection);

    private void OnInput(long tick, EntityId entity, uint commandId, ReadOnlySpan<byte> payload, CommandStatus status)
        => _recorder?.AddCommand(tick, entity, commandId, payload);

    private void OnCommandExecuted(long tick, EntityId entity, uint commandId, ReadOnlySpan<byte> payload, CommandStatus status)
    {
        CommandsRun++;
        _outcomes[_outcomeCount++ % OutcomeHistory] = new CommandOutcome(tick, entity, commandId, status);
        _chainedObserver?.Invoke(tick, entity, commandId, payload, status);
    }

    /// <summary>Forgets what subscribers were told and takes the current state as the baseline for the next frame.</summary>
    private void Rebaseline()
    {
        _known.Clear();
        foreach (var e in World.Entities)
        {
            _known.Add(e.Id.Value);
            if (e.DirtyMask != 0)
            {
                e.ClearDirty();
            }
        }

        _scratch.Reset();
        World.Rng.Write(_scratch);
        _rngBlob.Set(_scratch.Written);
        var tables = World.Tables;
        while (_tableBlobs.Count < tables.Count)
        {
            _tableBlobs.Add(new Blob());
        }

        for (var i = 0; i < tables.Count; i++)
        {
            _scratch.Reset();
            tables[i].Write(_scratch);
            _tableBlobs[i].Set(_scratch.Written);
        }
    }

    internal void Publish()
    {
        _recorder?.OnTickEnd();
        if (_subscribers.Count == 0)
        {
            return;
        }

        var tick = World.Tick;
        var interval = Node.Options.DriftCheckInterval;
        var hashed = interval > 0 && tick % interval == 0;

        // Entities: the sorted id list the subscribers know, merged against the world's, gives removals, additions and changes.
        _removed.Reset();
        _added.Reset();
        _deltas.Reset();
        _tables.Reset();
        _nextKnown.Clear();
        uint removedCount = 0, addedCount = 0, deltaCount = 0, tableCount = 0;
        var entities = World.Entities;
        int i = 0, j = 0;
        while (i < entities.Count || j < _known.Count)
        {
            var entityId = i < entities.Count ? entities[i].Id.Value : ulong.MaxValue;
            var knownId = j < _known.Count ? _known[j] : ulong.MaxValue;
            if (knownId < entityId)
            {
                _removed.WriteVarUInt(knownId);
                removedCount++;
                j++;
            }
            else if (entityId < knownId)
            {
                var e = entities[i];
                _entity.Reset();
                e.WriteState(_entity);
                _added.WriteFixed32(e.TypeId);
                _added.WriteVarUInt(e.Id.Value);
                _added.WriteVarUInt(e.SchemaVersion);
                _added.WriteBytes(_entity.Written);
                e.ClearDirty();
                _nextKnown.Add(entityId);
                addedCount++;
                i++;
            }
            else
            {
                var e = entities[i];
                if (e.DirtyMask != 0)
                {
                    _entity.Reset();
                    e.WriteDirtyDelta(_entity);
                    _deltas.WriteVarUInt(e.Id.Value);
                    _deltas.WriteBytes(_entity.Written);
                    e.ClearDirty();
                    deltaCount++;
                }

                _nextKnown.Add(entityId);
                i++;
                j++;
            }
        }

        (_known, _nextKnown) = (_nextKnown, _known);

        // Random streams and hot tables are sent whole when their bytes changed. Row-level deltas are a later refinement.
        _scratch.Reset();
        World.Rng.Write(_scratch);
        var rngChanged = !_rngBlob.Matches(_scratch.Written);
        if (rngChanged)
        {
            _rngBlob.Set(_scratch.Written);
        }

        var worldTables = World.Tables;
        for (var t = 0; t < worldTables.Count; t++)
        {
            _scratch.Reset();
            worldTables[t].Write(_scratch);
            if (!_tableBlobs[t].Matches(_scratch.Written))
            {
                _tableBlobs[t].Set(_scratch.Written);
                _tables.WriteString(worldTables[t].Name);
                _tables.WriteBytes(_scratch.Written);
                tableCount++;
            }
        }

        _frame.Reset();
        _frame.WriteByte(Msg.Frame);
        _frame.WriteVarUInt(Id.Value);
        _frame.WriteVarInt(tick);
        _frame.WriteByte((byte)((rngChanged ? 1 : 0) | (hashed ? 2 : 0)));
        if (rngChanged)
        {
            _frame.WriteBytes(_rngBlob.Span);
        }

        _frame.WriteVarUInt(removedCount);
        _frame.WriteRaw(_removed.Written);
        _frame.WriteVarUInt(addedCount);
        _frame.WriteRaw(_added.Written);
        _frame.WriteVarUInt(deltaCount);
        _frame.WriteRaw(_deltas.Written);
        _frame.WriteVarUInt(tableCount);
        _frame.WriteRaw(_tables.Written);
        if (hashed)
        {
            var digest = _digests[_digestNext++ % DigestRing];
            StateDigest.Capture(World, digest, _scratch, Node.Options.DigestEntityLimit);
            _frame.WriteFixed64(digest.WorldHash);
        }

        foreach (var s in _subscribers)
        {
            s.Channel.Send(_frame.Written);
        }
    }

    private void SendSnapshot(Connection to)
    {
        var w = Node.Out;
        w.Reset();
        w.WriteByte(Msg.Snapshot);
        w.WriteVarUInt(Id.Value);
        w.WriteVarInt(World.Tick);
        World.WriteSnapshot(w);
        to.Channel.Send(w.Written);
    }

    internal void SendDigest(Connection to, long tick)
    {
        var w = Node.Out;
        w.Reset();
        w.WriteByte(Msg.Digest);
        w.WriteVarUInt(Id.Value);
        w.WriteVarInt(tick);
        StateDigest? found = null;
        foreach (var d in _digests)
        {
            if (d.Tick == tick && d.WorldHash != 0)
            {
                found = d;
            }
        }

        w.WriteBool(found is not null);
        found?.Write(w);
        to.Channel.Send(w.Written);
    }

    /// <summary>A grow-only byte buffer that remembers the last bytes it was given, so unchanged data can be skipped without allocating.</summary>
    private sealed class Blob
    {
        private byte[] _data = new byte[64];
        private int _length = -1;

        public ReadOnlySpan<byte> Span => new(_data, 0, Math.Max(_length, 0));

        public bool Matches(ReadOnlySpan<byte> bytes) => _length == bytes.Length && bytes.SequenceEqual(Span);

        public void Set(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length > _data.Length)
            {
                _data = new byte[Math.Max(bytes.Length, _data.Length * 2)];
            }

            bytes.CopyTo(_data);
            _length = bytes.Length;
        }
    }
}

/// <summary>What happened to a command when the authority ran it.</summary>
/// <param name="Tick">The tick it ran in.</param>
/// <param name="Entity">The target.</param>
/// <param name="CommandId">The command id.</param>
/// <param name="Status">The outcome: executed, rejected by the handler, unknown entity or command, or malformed.</param>
public readonly record struct CommandOutcome(long Tick, EntityId Entity, uint CommandId, CommandStatus Status);
