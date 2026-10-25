namespace OneGame.Sync;

/// <summary>
/// A scope whose authority is this node: it owns the simulating <see cref="World"/>, accepts commands, and publishes a snapshot then
/// one frame per tick to each subscriber. Subscribers are grouped by <see cref="Audience"/>; each group gets the view its audience is
/// allowed to see (see <see cref="Visibility"/>), so private data never reaches a connection that may not have it.
/// </summary>
public sealed partial class AuthorityScope
{
    private const int OutcomeHistory = 256;
    private const int DigestRing = 4;
    private const int MaxReasonChars = 200;

    private readonly List<Connection> _subscribers = [];
    private readonly List<View> _views = [];
    private readonly CommandObserver? _chainedObserver;
    private readonly CommandOutcome[] _outcomes = new CommandOutcome[OutcomeHistory];
    private readonly Dictionary<long, Origin> _origins = [];
    private int _outcomeCount;
    private long _nextTicket;
    private Visibility _randomVisibility = Visibility.Everyone;

    // Frame building. Random streams and tables are the same for every audience, so they are compared once per tick.
    private readonly List<Blob> _tableBlobs = [];
    private readonly Blob _rngBlob = new();
    private bool _rngChanged;
    private uint _tableCount;
    private readonly SyncWriter _removed = new(256);
    private readonly SyncWriter _added = new(1024);
    private readonly SyncWriter _deltas = new(1024);
    private readonly SyncWriter _tables = new(1024);
    private readonly SyncWriter _entity = new(256);
    private readonly SyncWriter _frame = new(4096);
    private readonly SyncWriter _scratch = new(1024);

    private Recorder? _recorder;
    private readonly List<(IMessageChannel Channel, byte[] Message)> _deferred = [];
    private readonly Dictionary<uint, Origin> _relayPending = [];
    private long _relayTick = -1;

    internal AuthorityScope(Node node, ScopeId id, World world)
    {
        Node = node;
        Id = id;
        World = world;
        _chainedObserver = world.Observer;
        world.Observer = OnCommandExecuted;
        world.InputObserver = OnInput;
    }

    /// <summary>A relay: serves <paramref name="upstream"/>'s replica onward to this node's own subscribers.</summary>
    internal AuthorityScope(Node node, ReplicaScope upstream)
    {
        Node = node;
        Id = upstream.Id;
        World = upstream.World;
        Upstream = upstream;
        _chainedObserver = upstream.World.Observer;
        upstream.World.Observer = OnCommandExecuted;
        upstream.CommandCompleted += OnUpstreamResult;
    }

    /// <summary>For a relay, the replica it serves from; null for a scope this node is authority of.</summary>
    public ReplicaScope? Upstream { get; }

    /// <summary>True when this scope serves a replica onward instead of simulating (see <see cref="Node.Relay"/>).</summary>
    public bool IsRelay => Upstream is not null;

    /// <summary>
    /// When command answers and transfer confirmations may leave this scope. <see cref="Durability.Immediate"/> (default) sends them as they happen;
    /// <see cref="Durability.AfterSave"/> holds them until <see cref="Save"/> has captured the state they describe, so a node restored from its last
    /// save never contradicts something it already told a peer.
    /// </summary>
    public Durability Durability
    {
        get => _durability;
        set
        {
            _durability = value;
            if (value == Durability.Immediate)
            {
                ReleaseDeferred();
            }
        }
    }

    private Durability _durability;

    /// <summary>The world tick of the last <see cref="Save"/> or <see cref="Load"/>; -1 before either.</summary>
    public long SavedTick { get; private set; } = -1;

    private bool RandomVisible => _randomVisibility == Visibility.Everyone && World.RandomIncluded;

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

    /// <summary>
    /// Whether subscribers receive the random streams. <see cref="Visibility.Everyone"/> (the default) sends them, which a replica needs to
    /// predict or to match the authority's hash; <see cref="Visibility.Hidden"/> keeps them on the authority, for games where the next
    /// shuffle must stay secret. Changing it gives every subscriber a fresh snapshot.
    /// </summary>
    /// <exception cref="ArgumentException">The value is neither Everyone nor Hidden.</exception>
    public Visibility RandomStreams
    {
        get => _randomVisibility;
        set
        {
            if (value is not (Visibility.Everyone or Visibility.Hidden))
            {
                throw new ArgumentException("The random streams are either sent to everyone or kept hidden.", nameof(value));
            }

            if (value == _randomVisibility)
            {
                return;
            }

            _randomVisibility = value;
            ResendSnapshots();
        }
    }

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

    /// <summary>The scope's current hash (everything, including what no subscriber may see).</summary>
    public ulong Hash => World.ComputeHash();

    /// <summary>
    /// The hash of the view this connection holds of the scope: what its replica's <see cref="ReplicaScope.ViewHash"/> must equal at the same
    /// tick. For a connection that sees everything it equals <see cref="Hash"/>.
    /// </summary>
    public ulong ViewHashFor(Connection connection) => World.ComputeAudienceHash(connection.Trust.Who, RandomVisible);

    /// <summary>Runs one tick and publishes the resulting frame.</summary>
    public void StepAndPublish()
    {
        if (IsRelay)
        {
            return; // a relay publishes when its upstream frame arrives
        }

        World.Step();
        _recorder?.OnTickEnd();
        RunTransfers();
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
    public byte[] Save()
    {
        if (IsRelay)
        {
            throw new InvalidOperationException("A relay holds a copy of another scope's state; save the authority.");
        }

        var bytes = SaveFile.Write(World);
        SavedTick = World.Tick;
        ReleaseDeferred();
        return bytes;
    }

    /// <summary>
    /// Replaces the scope's state with a save. Subscribers get a fresh snapshot. The world must have been built the same way as the one saved.
    /// </summary>
    /// <exception cref="InvalidOperationException">A recording is running.</exception>
    public LoadResult Load(ReadOnlySpan<byte> save)
    {
        if (IsRelay)
        {
            throw new InvalidOperationException("A relay holds a copy of another scope's state; load the authority.");
        }

        if (_recorder is not null)
        {
            throw new InvalidOperationException("Stop the recording before loading a save: the log would no longer describe the run.");
        }

        var result = SaveFile.Load(save, World);
        if (result.Status == SaveStatus.Ok)
        {
            // Commands that were queued are gone with the old state; tell their senders.
            foreach (var (_, origin) in _origins)
            {
                SendResult(origin, CommandVerdict.Dropped, RefusalReason.Malformed, origin.Entity, origin.CommandId, null);
            }

            _origins.Clear();
            _deferred.Clear();
            SavedTick = World.Tick;
            ResetTransfersAfterLoad();
            ResendSnapshots();
        }

        return result;
    }

    // ---- subscriptions ----------------------------------------------------------------------------------------------

    internal void Subscribe(Connection connection)
    {
        Detach(connection);
        if (_views.Count == 0)
        {
            RebaselineShared();
        }

        var view = FindView(connection.Trust.Who) ?? NewView(connection.Trust.Who);
        view.Members.Add(connection);
        _subscribers.Add(connection);
        SendSnapshot(connection, view);
    }

    internal void Unsubscribe(Connection connection) => Detach(connection);

    /// <summary>The connection's trust was replaced: stop sending if it may no longer read, and move it to the view its new audience gets.</summary>
    internal void TrustChanged(Connection connection)
    {
        if (ViewOf(connection) is null)
        {
            return;
        }

        if (!connection.Trust.Read.Contains(Id))
        {
            Detach(connection);
            var w = Node.Out;
            w.Reset();
            w.WriteByte(Msg.Revoked);
            w.WriteVarUInt(Id.Value);
            w.WriteByte((byte)RefusalReason.ReadRevoked);
            connection.Channel.Send(w.Written);
            Node.FileRefusal(new CommandRefusal(Id, connection.Id, EntityId.None, 0, RefusalReason.ReadRevoked));
            return;
        }

        if (ViewOf(connection)!.Who != connection.Trust.Who)
        {
            Subscribe(connection);
        }
    }

    private View? ViewOf(Connection connection)
    {
        foreach (var v in _views)
        {
            if (v.Members.Contains(connection))
            {
                return v;
            }
        }

        return null;
    }

    private View? FindView(Audience who)
    {
        foreach (var v in _views)
        {
            if (v.Who == who)
            {
                return v;
            }
        }

        return null;
    }

    private View NewView(Audience who)
    {
        var view = new View(who);
        BuildKnown(view);
        _views.Add(view);
        return view;
    }

    private void Detach(Connection connection)
    {
        var view = ViewOf(connection);
        if (view is null)
        {
            return;
        }

        view.Members.Remove(connection);
        _subscribers.Remove(connection);
        if (view.Members.Count == 0)
        {
            _views.Remove(view);
        }
    }

    /// <summary>Gives every subscriber a snapshot of the current state and makes it the baseline for the next frame.</summary>
    internal void ResendSnapshots()
    {
        if (_views.Count == 0)
        {
            return;
        }

        RebaselineShared();
        foreach (var v in _views)
        {
            BuildKnown(v);
            foreach (var c in v.Members)
            {
                SendSnapshot(c, v);
            }
        }
    }

    // ---- commands -----------------------------------------------------------------------------------------------------

    internal void QueueExternal(Connection from, uint sequence, EntityId entity, uint commandId, ReadOnlySpan<byte> payload)
    {
        if (IsRelay)
        {
            // Pass the command up. The replica sends it with its own sequence number; remember whose it was so the answer can go back.
            var before = Upstream!.LastSequence;
            World.Submit(entity, commandId, payload);
            if (sequence != 0 && Upstream.LastSequence != before)
            {
                _relayPending[Upstream.LastSequence] = new Origin(from, sequence, entity, commandId, false);
            }

            return;
        }

        long ticket = 0;
        if (sequence != 0)
        {
            ticket = ++_nextTicket;
            _origins[ticket] = new Origin(from, sequence, entity, commandId, false);
        }

        World.Submit(entity, commandId, payload, ticket);
    }

    private void OnInput(long tick, EntityId entity, uint commandId, ReadOnlySpan<byte> payload, CommandStatus status)
        => _recorder?.AddCommand(tick, entity, commandId, payload);

    /// <summary>Queues a command a peer node forwarded here for an entity that moved to this scope; the answer goes back as a <c>ForwardResult</c>.</summary>
    internal void QueueForwarded(Connection from, ulong forwardSequence, EntityId entity, uint commandId, ReadOnlySpan<byte> payload)
    {
        var ticket = ++_nextTicket;
        _origins[ticket] = new Origin(from, (uint)Math.Min(forwardSequence, uint.MaxValue), entity, commandId, true) { ForwardSequence = forwardSequence };
        World.Submit(entity, commandId, payload, ticket);
    }

    private void OnUpstreamResult(CommandResult result)
    {
        if (_relayPending.Remove(result.Sequence, out var origin))
        {
            SendResult(origin, result.Verdict, result.Refusal, origin.Entity, origin.CommandId, result.Reason);
        }
    }

    private void OnCommandExecuted(long tick, EntityId entity, uint commandId, ReadOnlySpan<byte> payload, CommandStatus status)
    {
        if (IsRelay)
        {
            _chainedObserver?.Invoke(tick, entity, commandId, payload, status);
            return;
        }

        CommandsRun++;
        _outcomes[_outcomeCount++ % OutcomeHistory] = new CommandOutcome(tick, entity, commandId, status);
        var ticket = World.CurrentTicket;
        if (ticket != 0 && _origins.Remove(ticket, out var origin))
        {
            var verdict = status switch
            {
                CommandStatus.Executed => CommandVerdict.Executed,
                CommandStatus.Rejected => CommandVerdict.Rejected,
                CommandStatus.UnknownEntity => CommandVerdict.UnknownEntity,
                CommandStatus.UnknownCommand => CommandVerdict.UnknownCommand,
                _ => CommandVerdict.Malformed,
            };
            SendResult(origin, verdict, RefusalReason.Malformed, entity, commandId, status == CommandStatus.Rejected ? World.LastRejectReason : null);
        }

        _chainedObserver?.Invoke(tick, entity, commandId, payload, status);
    }

    private void SendResult(Origin to, CommandVerdict verdict, RefusalReason refusal, EntityId entity, uint commandId, string? reason)
    {
        if (reason is { Length: > MaxReasonChars })
        {
            reason = reason[..MaxReasonChars];
        }

        var w = Node.Out;
        w.Reset();
        w.WriteByte(to.Forward ? Msg.ForwardResult : Msg.CommandResult);
        w.WriteVarUInt(Id.Value);
        w.WriteVarUInt(to.Forward ? to.ForwardSequence : to.Sequence);
        w.WriteVarUInt(entity.Value);
        w.WriteVarUInt(commandId);
        w.WriteByte((byte)verdict);
        w.WriteByte((byte)refusal);
        w.WriteString(reason ?? string.Empty);
        SendDurable(to.From.Channel, w.Written);
    }

    /// <summary>Sends now, or holds the message until the next <see cref="Save"/> when <see cref="Durability"/> asks for that.</summary>
    internal void SendDurable(IMessageChannel channel, ReadOnlySpan<byte> message)
    {
        if (_durability == Durability.AfterSave)
        {
            _deferred.Add((channel, message.ToArray()));
            return;
        }

        channel.Send(message);
    }

    private void ReleaseDeferred()
    {
        foreach (var (channel, message) in _deferred)
        {
            channel.Send(message);
        }

        _deferred.Clear();
    }

    // ---- frames -------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Takes the current random streams and tables as what subscribers have been told, and forgets stale changes. Called when the
    /// first subscriber arrives and whenever every subscriber gets a new snapshot.
    /// </summary>
    private void RebaselineShared()
    {
        foreach (var e in World.Entities)
        {
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

    /// <summary>Records which entities, and which of their properties, the view's audience holds now.</summary>
    private void BuildKnown(View view)
    {
        view.KnownIds.Clear();
        view.KnownMasks.Clear();
        foreach (var e in World.Entities)
        {
            if (e.TryGetAudienceMask(view.Who, out var mask))
            {
                view.KnownIds.Add(e.Id.Value);
                view.KnownMasks.Add(e.NormalizeMask(mask));
            }
        }
    }

    internal void Publish()
    {
        if (_views.Count == 0)
        {
            return;
        }

        var tick = World.Tick;
        var interval = Node.Options.DriftCheckInterval;
        var hashed = interval > 0 && tick % interval == 0;

        // Random streams and hot tables are sent whole when their bytes changed. Row-level deltas are a later refinement.
        _scratch.Reset();
        World.Rng.Write(_scratch);
        var randomVisible = RandomVisible;
        _rngChanged = !_rngBlob.Matches(_scratch.Written);
        if (_rngChanged)
        {
            _rngBlob.Set(_scratch.Written);
        }

        _tables.Reset();
        _tableCount = 0;
        var worldTables = World.Tables;
        for (var t = 0; t < worldTables.Count; t++)
        {
            _scratch.Reset();
            worldTables[t].Write(_scratch);
            if (!_tableBlobs[t].Matches(_scratch.Written))
            {
                _tableBlobs[t].Set(_scratch.Written);
                if (worldTables[t].Visibility != Visibility.Hidden)
                {
                    _tables.WriteString(worldTables[t].Name);
                    _tables.WriteBytes(_scratch.Written);
                    _tableCount++;
                }
            }
        }

        foreach (var view in _views)
        {
            BuildEntityChanges(view, out var removedCount, out var addedCount, out var deltaCount);
            _frame.Reset();
            _frame.WriteByte(Msg.Frame);
            _frame.WriteVarUInt(Id.Value);
            _frame.WriteVarInt(tick);
            var sendRandom = _rngChanged && randomVisible;
            _frame.WriteByte((byte)((sendRandom ? 1 : 0) | (hashed ? 2 : 0)));
            if (sendRandom)
            {
                _frame.WriteBytes(_rngBlob.Span);
            }

            _frame.WriteVarUInt(removedCount);
            _frame.WriteRaw(_removed.Written);
            _frame.WriteVarUInt(addedCount);
            _frame.WriteRaw(_added.Written);
            _frame.WriteVarUInt(deltaCount);
            _frame.WriteRaw(_deltas.Written);
            _frame.WriteVarUInt(_tableCount);
            _frame.WriteRaw(_tables.Written);
            if (hashed)
            {
                var digest = view.Digests[view.DigestNext++ % DigestRing];
                StateDigest.CaptureView(World, digest, _scratch, Node.Options.DigestEntityLimit, view.Who, randomVisible);
                _frame.WriteFixed64(digest.WorldHash);
            }

            foreach (var c in view.Members)
            {
                c.Channel.Send(_frame.Written);
            }
        }

        // Every audience has been told; the changes are now history.
        var all = World.Entities;
        for (var n = 0; n < all.Count; n++)
        {
            if (all[n].DirtyMask != 0)
            {
                all[n].ClearDirty();
            }
        }
    }

    /// <summary>
    /// Merges the sorted ids the audience knows against the world's to get removals, additions and changes, showing the audience only
    /// the entities and properties it may see. An entity whose visible properties changed in kind (its owner changed, say) is sent
    /// again whole; the replica replaces what it had.
    /// </summary>
    private void BuildEntityChanges(View view, out uint removedCount, out uint addedCount, out uint deltaCount)
    {
        _removed.Reset();
        _added.Reset();
        _deltas.Reset();
        view.NextIds.Clear();
        view.NextMasks.Clear();
        removedCount = addedCount = deltaCount = 0;
        var entities = World.Entities;
        var knownIds = view.KnownIds;
        var knownMasks = view.KnownMasks;
        int i = 0, j = 0;
        while (i < entities.Count || j < knownIds.Count)
        {
            var entityId = i < entities.Count ? entities[i].Id.Value : ulong.MaxValue;
            var knownId = j < knownIds.Count ? knownIds[j] : ulong.MaxValue;
            if (knownId < entityId)
            {
                _removed.WriteVarUInt(knownId);
                removedCount++;
                j++;
                continue;
            }

            var e = entities[i];
            var visible = e.TryGetAudienceMask(view.Who, out var rawMask);
            var mask = e.NormalizeMask(rawMask);
            if (entityId < knownId)
            {
                if (visible)
                {
                    WriteAdded(e, mask);
                    addedCount++;
                    view.NextIds.Add(entityId);
                    view.NextMasks.Add(mask);
                }

                i++;
                continue;
            }

            // Known and still in the world.
            if (!visible)
            {
                _removed.WriteVarUInt(entityId);
                removedCount++;
            }
            else if (mask != knownMasks[j])
            {
                WriteAdded(e, mask);
                addedCount++;
                view.NextIds.Add(entityId);
                view.NextMasks.Add(mask);
            }
            else
            {
                var dirty = mask == ulong.MaxValue ? e.DirtyMask : e.DirtyMask & mask;
                if (dirty != 0)
                {
                    _entity.Reset();
                    if (mask == ulong.MaxValue)
                    {
                        e.WriteDirtyDelta(_entity);
                    }
                    else
                    {
                        e.WriteMaskedDelta(_entity, dirty);
                    }

                    _deltas.WriteVarUInt(entityId);
                    _deltas.WriteBytes(_entity.Written);
                    deltaCount++;
                }

                view.NextIds.Add(entityId);
                view.NextMasks.Add(mask);
            }

            i++;
            j++;
        }

        (view.KnownIds, view.NextIds) = (view.NextIds, view.KnownIds);
        (view.KnownMasks, view.NextMasks) = (view.NextMasks, view.KnownMasks);
    }

    private void WriteAdded(Entity e, ulong mask)
    {
        _entity.Reset();
        if (mask == ulong.MaxValue)
        {
            e.WriteState(_entity);
        }
        else
        {
            e.WriteMaskedDelta(_entity, mask);
        }

        _added.WriteFixed32(e.TypeId);
        _added.WriteVarUInt(e.Id.Value);
        _added.WriteVarUInt(e.SchemaVersion);
        _added.WriteByte(mask == ulong.MaxValue ? World.EntityFormFull : World.EntityFormMasked);
        _added.WriteBytes(_entity.Written);
    }

    private void SendSnapshot(Connection to, View view)
    {
        var w = Node.Out;
        w.Reset();
        w.WriteByte(Msg.Snapshot);
        w.WriteVarUInt(Id.Value);
        w.WriteVarInt(World.Tick);
        World.WriteViewSnapshot(w, view.Who, RandomVisible);
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
        if (ViewOf(to) is { } view)
        {
            foreach (var d in view.Digests)
            {
                if (d.Tick == tick && d.WorldHash != 0)
                {
                    found = d;
                }
            }
        }

        w.WriteBool(found is not null);
        found?.Write(w);
        to.Channel.Send(w.Written);
    }

    /// <summary>The subscribers that share one audience, and what that audience has been told.</summary>
    private sealed class View
    {
        public View(Audience who)
        {
            Who = who;
            for (var i = 0; i < DigestRing; i++)
            {
                Digests[i] = new StateDigest();
            }
        }

        public Audience Who { get; }

        public List<Connection> Members { get; } = [];

        public List<ulong> KnownIds { get; set; } = [];

        public List<ulong> NextIds { get; set; } = [];

        public List<ulong> KnownMasks { get; set; } = [];

        public List<ulong> NextMasks { get; set; } = [];

        public StateDigest[] Digests { get; } = new StateDigest[DigestRing];

        public int DigestNext { get; set; }
    }

    internal readonly record struct Origin(Connection From, uint Sequence, EntityId Entity, uint CommandId, bool Forward)
    {
        public ulong ForwardSequence { get; init; }
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
