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
    private const int MaxPendingInterest = 1024;

    private readonly List<Connection> _subscribers = [];
    private readonly List<View> _views = [];
    private readonly CommandObserver? _chainedObserver;
    private readonly CommandOutcome[] _outcomes = new CommandOutcome[OutcomeHistory];
    private readonly Dictionary<long, Origin> _origins = [];
    private int _outcomeCount;
    private long _nextTicket;
    private Visibility _randomVisibility = Visibility.Everyone;
    private bool _requireInterest;
    private int _viewMembers = 1;
    private readonly Dictionary<uint, TypeTraffic> _traffic = [];
    private readonly Dictionary<Connection, InterestSet> _pendingInterest = [];

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

    /// <summary>Bytes of entity state sent to subscribers so far, by entity type: what each type costs on the wire (every connection that was sent it counts).</summary>
    public IReadOnlyCollection<TypeTraffic> Traffic => _traffic.Values;

    /// <summary>Entity additions sent to subscribers so far (an entity entering a connection's view counts once per connection).</summary>
    public long EntitiesSent { get; private set; }

    /// <summary>Entity removals sent to subscribers so far (entities deleted, hidden, or leaving a connection's interest).</summary>
    public long EntitiesRemoved { get; private set; }

    /// <summary>Bytes of frame messages sent to subscribers so far (each connection's copy counts).</summary>
    public long FrameBytes { get; private set; }

    /// <summary>Bytes of snapshot messages sent to subscribers so far.</summary>
    public long SnapshotBytes { get; private set; }

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
    public ulong ViewHashFor(Connection connection)
        => World.ComputeAudienceHash(connection.Trust.Who, RandomVisible, ViewOf(connection)?.Interest);

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
        // A connection keeps the interest it had (or was given before subscribing). Under RequireInterest a new one starts empty: it
        // receives only global entities until the game says where it is.
        var existing = ViewOf(connection);
        InterestSet? wanted = null;
        if (!_pendingInterest.Remove(connection, out wanted))
        {
            wanted = existing?.Wanted;
        }

        if (wanted is null && _requireInterest)
        {
            wanted = new InterestSet();
        }

        Detach(connection);
        if (_views.Count == 0)
        {
            RebaselineShared();
        }

        var view = wanted is null ? FindView(connection.Trust.Who) ?? NewView(connection.Trust.Who) : NewView(connection.Trust.Who, wanted);
        view.Members.Add(connection);
        _subscribers.Add(connection);
        SendSnapshot(connection, view);
    }

    internal void Unsubscribe(Connection connection)
    {
        _pendingInterest.Remove(connection);
        Detach(connection);
    }

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
            if (v.Who == who && v.Wanted is null)
            {
                return v;
            }
        }

        return null;
    }

    private View NewView(Audience who, InterestSet? wanted = null)
    {
        var view = new View(who);
        if (wanted is not null)
        {
            view.Wanted = wanted;
            view.Interest = new InterestSet();
            view.InterestDirty = true;
            ApplyInterest(view);
        }

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
            ApplyInterest(v);
            BuildKnown(v);
            foreach (var c in v.Members)
            {
                SendSnapshot(c, v);
            }
        }
    }

    // ---- interest -------------------------------------------------------------------------------------------------------

    /// <summary>
    /// When true, a connection receives only global entities (interest key 0 or no key) until the game gives it a set with
    /// <see cref="SetInterest"/>; without it a new subscriber gets the whole scope. Turning it on forces <see cref="RandomStreams"/> to
    /// <see cref="Visibility.Hidden"/> (a random stream advanced by events outside a connection's interest would reveal them), and it
    /// refuses a world that has a hot table which is not hidden, because tables are sent whole to everyone. Set it before the first subscriber.
    /// </summary>
    /// <exception cref="InvalidOperationException">Subscribers already exist, or the world has a table that is not <see cref="Visibility.Hidden"/>.</exception>
    public bool RequireInterest
    {
        get => _requireInterest;
        set
        {
            if (value == _requireInterest)
            {
                return;
            }

            if (value)
            {
                if (_subscribers.Count != 0)
                {
                    throw new InvalidOperationException("RequireInterest must be set before the first subscriber.");
                }

                foreach (var t in World.Tables)
                {
                    if (t.Visibility != Visibility.Hidden)
                    {
                        throw new InvalidOperationException($"Hot table '{t.Name}' is sent whole to every subscriber and cannot be filtered by interest. Mark it Hidden, or keep its rows as entities with an [InterestKey].");
                    }
                }

                RandomStreams = Visibility.Hidden;
            }

            _requireInterest = value;
        }
    }

    /// <summary>
    /// Sets which interest cells <paramref name="connection"/> receives in this scope (also for a peer node's connection or a relay's).
    /// Entities with a non-zero <see cref="InterestKeyAttribute"/> value outside the set are not sent; entities that enter or leave it are
    /// added to and removed from the connection's replica by the next frame, with no new snapshot. Call it between ticks; it takes effect
    /// in the next publish. Setting the same keys again does nothing and allocates nothing. The first call on a connection that had no
    /// interest set (and no <see cref="RequireInterest"/>) sends it one fresh snapshot, because it moves to a view of its own.
    /// May be called before the connection subscribes; the set is then used for its first snapshot.
    /// </summary>
    /// <param name="connection">A connection of this scope's node.</param>
    /// <param name="keys">The cells to receive; duplicates and 0 are ignored.</param>
    /// <returns>True when the set changed.</returns>
    public bool SetInterest(Connection connection, ReadOnlySpan<uint> keys)
    {
        ArgumentNullException.ThrowIfNull(connection);
        var view = ViewOf(connection);
        if (view?.Wanted is { } wanted)
        {
            var changed = wanted.Replace(keys);
            view.InterestDirty |= changed;
            return changed;
        }

        if (!_pendingInterest.TryGetValue(connection, out var pending))
        {
            if (_pendingInterest.Count >= MaxPendingInterest)
            {
                throw new InvalidOperationException("Too many connections were given an interest before they subscribed.");
            }

            pending = new InterestSet();
            _pendingInterest[connection] = pending;
        }

        pending.Replace(keys);
        if (view is not null)
        {
            Subscribe(connection); // moves to a view of its own and sends the filtered snapshot
        }

        return true;
    }

    /// <summary>Gives the connection back everything it may see. It receives one fresh snapshot.</summary>
    /// <param name="connection">A connection of this scope's node.</param>
    public void ClearInterest(Connection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _pendingInterest.Remove(connection);
        if (ViewOf(connection) is { Wanted: not null } && !_requireInterest)
        {
            var who = connection.Trust.Who;
            Detach(connection);
            if (_views.Count == 0)
            {
                RebaselineShared();
            }

            var view = FindView(who) ?? NewView(who);
            view.Members.Add(connection);
            _subscribers.Add(connection);
            SendSnapshot(connection, view);
        }
    }

    /// <summary>The interest keys the connection is set to receive (the ones the last frame was built with), or null when it receives every entity.</summary>
    /// <param name="connection">A connection of this scope's node.</param>
    public IReadOnlyCollection<uint>? InterestOf(Connection connection)
        => ViewOf(connection)?.Interest is { } set ? [.. set.Keys] : null;

    internal InterestSet? EffectiveInterest(Connection connection) => ViewOf(connection)?.Interest;

    private static void ApplyInterest(View view)
    {
        if (view.Wanted is not null && view.InterestDirty)
        {
            view.Interest!.CopyFrom(view.Wanted);
            view.InterestDirty = false;
        }
    }

    /// <summary>True when the connection has a restricted view that does not hold the entity. Such a command is answered as an unknown entity, so ids cannot be probed.</summary>
    internal bool OutsideInterest(Connection from, EntityId entity)
        => ViewOf(from) is { Interest: not null } view && view.KnownIds.BinarySearch(entity.Value) < 0;

    internal void AnswerUnknownEntity(Connection from, uint sequence, EntityId entity, uint commandId)
    {
        if (sequence != 0)
        {
            SendResult(new Origin(from, sequence, entity, commandId, false), CommandVerdict.UnknownEntity, RefusalReason.Malformed, entity, commandId, null);
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

        // "Dropped" says the command was not applied, which is true whatever was saved, so it never waits.
        if (verdict == CommandVerdict.Dropped)
        {
            to.From.Channel.Send(w.Written);
        }
        else
        {
            SendDurable(to.From.Channel, w.Written);
        }
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
            if (e.TryGetAudienceMask(view.Who, view.Interest, out var mask))
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
            ApplyInterest(view);
            _viewMembers = view.Members.Count;
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
                StateDigest.CaptureView(World, digest, _scratch, Node.Options.DigestEntityLimit, view.Who, randomVisible, view.Interest);
                _frame.WriteFixed64(digest.WorldHash);
            }

            FrameBytes += (long)_frame.Written.Length * view.Members.Count;
            EntitiesSent += addedCount * (long)view.Members.Count;
            EntitiesRemoved += removedCount * (long)view.Members.Count;
            foreach (var c in view.Members)
            {
                Node.SendMessage(c.Channel, _frame.Written);
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
            var visible = e.TryGetAudienceMask(view.Who, view.Interest, out var rawMask);
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
                    Count(e, _entity.Written.Length, delta: true);
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
        Count(e, _entity.Written.Length + 8, delta: false);
    }

    private void Count(Entity e, int bytes, bool delta)
    {
        if (!_traffic.TryGetValue(e.TypeId, out var t))
        {
            t = new TypeTraffic(e.TypeId, e.TypeName);
            _traffic[e.TypeId] = t;
        }

        var total = (long)bytes * _viewMembers;
        if (delta)
        {
            t.DeltaBytes += total;
            t.Deltas += _viewMembers;
        }
        else
        {
            t.AddedBytes += total;
            t.Additions += _viewMembers;
        }
    }

    private void SendSnapshot(Connection to, View view)
    {
        var w = Node.Out;
        w.Reset();
        w.WriteByte(Msg.Snapshot);
        w.WriteVarUInt(Id.Value);
        w.WriteVarInt(World.Tick);
        World.WriteViewSnapshot(w, view.Who, RandomVisible, view.Interest);
        SnapshotBytes += w.Written.Length;
        Node.SendMessage(to.Channel, w.Written);
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

        /// <summary>What the game last asked this connection to receive; null for a view shared by an audience (no interest restriction).</summary>
        public InterestSet? Wanted { get; set; }

        /// <summary>The set the last frame and snapshot of this view were built with. It follows <see cref="Wanted"/> at the start of each publish, so a view hash always matches the bytes the replica holds.</summary>
        public InterestSet? Interest { get; set; }

        public bool InterestDirty { get; set; }

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
