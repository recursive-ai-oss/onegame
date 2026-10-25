namespace OneGame.Sync;

/// <summary>Where a replica is in its life.</summary>
public enum ReplicaState
{
    /// <summary>The subscription was sent; no snapshot has arrived yet.</summary>
    Subscribing = 0,

    /// <summary>Snapshot applied, frames arriving in order.</summary>
    Live,

    /// <summary>A frame, snapshot or drift made the replica ask for a fresh snapshot; frames are ignored until it arrives.</summary>
    Resyncing,

    /// <summary>The authority refused the subscription (see <see cref="Node.Refusals"/>).</summary>
    Refused,

    /// <summary>The authority kept sending data this replica could not apply, three times in a row. It stopped asking.</summary>
    Failed,

    /// <summary>The authority took the read right away. The replica's world was emptied; <see cref="Node.Resubscribe"/> asks again.</summary>
    Revoked,

    /// <summary>This node stopped following the scope (<see cref="Node.Unsubscribe"/>). The world keeps its last state and is no longer updated.</summary>
    Unsubscribed,
}

/// <summary>
/// This node's copy of a scope it does not own. Its <see cref="World"/> is a replica: it only applies what the authority sends, and
/// its entities reject writes. Views read it; input goes out as commands (<c>entity.Send.X(...)</c> on a replica entity).
/// </summary>
public sealed class ReplicaScope
{
    private const int MaxResyncAttempts = 3;
    private const int MaxPending = 1024;
    private const int MaxResults = 256;
    private const int MaxReasonChars = 256;

    private readonly StateDigest _localDigest = new();
    private readonly SyncWriter _digestScratch = new(1024);
    private bool _awaitingDigest;
    private long _pendingTick;
    private ulong _pendingAuthorityHash;
    private ulong _pendingLocalHash;
    private long _digestRequestedTick;
    private int _failures;
    private uint _nextSequence = 1;
    private readonly List<PendingSend> _pending = [];
    private readonly List<CommandResult> _results = [];

    internal ReplicaScope(Node node, Link link, ScopeId id, World world)
    {
        Node = node;
        Link = link;
        Id = id;
        World = world;
        world.Observer = Forward;
    }

    /// <summary>The scope id.</summary>
    public ScopeId Id { get; }

    /// <summary>The node holding this replica.</summary>
    public Node Node { get; }

    /// <summary>The link to the authority's node.</summary>
    public Link Link { get; }

    /// <summary>The replica world. Read it; never write it.</summary>
    public World World { get; }

    /// <summary>Lifecycle state.</summary>
    public ReplicaState State { get; private set; } = ReplicaState.Subscribing;

    /// <summary>The tick of the last snapshot or frame applied.</summary>
    public long LastTick { get; private set; }

    /// <summary>Frames applied so far.</summary>
    public long FramesApplied { get; private set; }

    /// <summary>How many times this replica asked for a fresh snapshot after the first.</summary>
    public int Resyncs { get; private set; }

    /// <summary>The hash of everything the replica world holds. Equals the authority's <see cref="AuthorityScope.Hash"/> only when this replica is allowed to see everything.</summary>
    public ulong Hash => World.ComputeHash();

    /// <summary>
    /// The hash of the view this replica holds: the authority computes the same value for the same audience (<see cref="AuthorityScope.ViewHashFor"/>),
    /// so equality at the same tick means nothing it may see has drifted, and nothing it may not see was sent.
    /// </summary>
    public ulong ViewHash => World.ComputeViewHash();

    /// <summary>The sequence number of the command most recently forwarded to the authority, or 0 if none yet. Read it right after <c>entity.Send.X(...)</c> to match the answer in <see cref="Results"/>.</summary>
    public uint LastSequence => _nextSequence - 1;

    /// <summary>The answers received for forwarded commands, oldest first (the latest 256).</summary>
    public IReadOnlyList<CommandResult> Results => _results;

    /// <summary>How many forwarded commands have not been answered yet.</summary>
    public int PendingCommands => _pending.Count;

    /// <summary>Raised for every answer to a forwarded command: executed, rejected with the handler's reason, refused, or dropped.</summary>
    public event Action<CommandResult>? CommandCompleted;

    internal void RequestSnapshot()
    {
        var w = Node.Out;
        w.Reset();
        w.WriteByte(Msg.Subscribe);
        w.WriteVarUInt(Id.Value);
        Link.Channel.Send(w.Written);
    }

    internal void MarkUnsubscribed()
    {
        var w = Node.Out;
        w.Reset();
        w.WriteByte(Msg.Unsubscribe);
        w.WriteVarUInt(Id.Value);
        Link.Channel.Send(w.Written);
        State = ReplicaState.Unsubscribed;
        DropPending();
    }

    internal void Restart()
    {
        _failures = 0;
        _awaitingDigest = false;
        State = ReplicaState.Subscribing;
        RequestSnapshot();
    }

    private void Forward(long tick, EntityId entity, uint commandId, ReadOnlySpan<byte> payload, CommandStatus status)
    {
        if (status != CommandStatus.Forwarded)
        {
            return;
        }

        var sequence = _nextSequence++;
        if (_pending.Count >= MaxPending)
        {
            Complete(new CommandResult(Id, _pending[0].Sequence, _pending[0].Entity, _pending[0].CommandId, CommandVerdict.Dropped, default, null));
        }

        _pending.Add(new PendingSend(sequence, entity, commandId));
        var w = Node.Out;
        w.Reset();
        w.WriteByte(Msg.Command);
        w.WriteVarUInt(Id.Value);
        w.WriteVarUInt(sequence);
        w.WriteVarUInt(entity.Value);
        w.WriteVarUInt(commandId);
        w.WriteRaw(payload);
        Link.Channel.Send(w.Written);
    }

    internal void Handle(byte type, ref SyncReader r, ReadOnlySpan<byte> message)
    {
        if (State == ReplicaState.Failed)
        {
            return;
        }

        switch (type)
        {
            case Msg.Snapshot:
                ApplySnapshot(ref r, message);
                break;
            case Msg.Frame:
                if (State == ReplicaState.Live)
                {
                    ApplyFrame(ref r);
                }

                break;
            case Msg.Digest:
                ApplyDigest(ref r);
                break;
            case Msg.CommandRefused:
            {
                var sequence = r.ReadVarUInt();
                var entity = r.ReadVarUInt();
                var command = r.ReadVarUInt();
                var reason = r.ReadByte();
                if (r.Ok && command <= uint.MaxValue && sequence <= uint.MaxValue)
                {
                    Node.FileRefusal(new CommandRefusal(Id, (Link.IsRemote ? 0 : Link.Connection.Id), new EntityId(entity), (uint)command, (RefusalReason)reason));
                    Answer((uint)sequence, new CommandResult(Id, (uint)sequence, new EntityId(entity), (uint)command, CommandVerdict.Refused, (RefusalReason)reason, null));
                }

                break;
            }

            case Msg.CommandResult:
            {
                var sequence = r.ReadVarUInt();
                var entity = r.ReadVarUInt();
                var command = r.ReadVarUInt();
                var verdict = r.ReadByte();
                var refusal = r.ReadByte();
                var reason = r.ReadString();
                if (r.Ok && command <= uint.MaxValue && sequence <= uint.MaxValue && verdict <= (byte)CommandVerdict.Dropped)
                {
                    if (reason.Length > MaxReasonChars)
                    {
                        reason = reason[..MaxReasonChars];
                    }

                    Answer((uint)sequence, new CommandResult(Id, (uint)sequence, new EntityId(entity), (uint)command, (CommandVerdict)verdict, (RefusalReason)refusal, reason.Length == 0 ? null : reason));
                }
                else
                {
                    Node.ReportProtocolError(Id, LastTick, "malformed command result");
                }

                break;
            }

            case Msg.Revoked:
            {
                var reason = r.ReadByte();
                if (r.Ok)
                {
                    State = ReplicaState.Revoked;
                    World.ClearReplica();
                    DropPending();
                    Node.FileRefusal(new CommandRefusal(Id, (Link.IsRemote ? 0 : Link.Connection.Id), EntityId.None, 0, (RefusalReason)reason));
                }

                break;
            }

            case Msg.SubscribeRefused:
            {
                var reason = r.ReadByte();
                State = ReplicaState.Refused;
                Node.FileRefusal(new CommandRefusal(Id, (Link.IsRemote ? 0 : Link.Connection.Id), EntityId.None, 0, (RefusalReason)reason));
                break;
            }

            default:
                Node.ReportProtocolError(Id, LastTick, "unexpected message type " + type);
                break;
        }
    }

    private void ApplySnapshot(ref SyncReader r, ReadOnlySpan<byte> message)
    {
        var tick = r.ReadVarInt();
        if (!r.Ok)
        {
            Desync("snapshot header: " + r.Error);
            return;
        }

        var error = World.ReadSnapshot(message[r.Position..]);
        if (error != SyncError.None)
        {
            Desync("snapshot: " + error);
            return;
        }

        if (World.Tick != tick)
        {
            Desync($"snapshot says tick {tick} but holds tick {World.Tick}");
            return;
        }

        LastTick = tick;
        State = ReplicaState.Live;
        _awaitingDigest = false;
    }

    private void ApplyFrame(ref SyncReader r)
    {
        var tick = r.ReadVarInt();
        var flags = r.ReadByte();
        if (!r.Ok || (flags & ~3) != 0)
        {
            Desync("frame header");
            return;
        }

        if (tick != LastTick + 1)
        {
            Desync($"frame for tick {tick} after tick {LastTick}");
            return;
        }

        if ((flags & 1) != 0)
        {
            var bytes = r.ReadBytes(int.MaxValue);
            var sub = new SyncReader(bytes);
            World.Rng.Read(ref sub);
            sub.ExpectEnd();
            if (!r.Ok || !sub.Ok)
            {
                Desync("frame random streams");
                return;
            }
        }

        var removed = r.ReadCount(SyncLimits.MaxEntities);
        for (var n = 0; n < removed && r.Ok; n++)
        {
            var id = r.ReadVarUInt();
            if (r.Ok)
            {
                World.Remove(new EntityId(id));
            }
        }

        var added = r.ReadCount(SyncLimits.MaxEntities);
        for (var n = 0; n < added && r.Ok; n++)
        {
            var typeId = r.ReadFixed32();
            var id = r.ReadVarUInt();
            var schema = r.ReadVarUInt();
            var form = r.ReadByte();
            var body = r.ReadBytes(int.MaxValue);
            if (!r.Ok)
            {
                break;
            }

            var entity = id == 0 || schema > uint.MaxValue || form > World.EntityFormMasked ? null : World.Registry.Create(typeId);
            if (entity is null)
            {
                r.Fail(id == 0 || schema > uint.MaxValue || form > World.EntityFormMasked ? SyncError.InvalidValue : SyncError.UnknownType);
                break;
            }

            var sub = new SyncReader(body);
            if (form == World.EntityFormFull)
            {
                entity.ReadState((uint)schema, ref sub);
            }
            else if (schema != entity.SchemaVersion)
            {
                r.Fail(SyncError.UnsupportedVersion);
                break;
            }
            else
            {
                // A masked body is a delta in the current layout holding only the properties this replica may see.
                var peek = new SyncReader(body);
                var visible = peek.ReadVarUInt();
                entity.ApplyDelta(ref sub);
                entity.ClearDirty();
                entity.ViewMask = entity.NormalizeMask(visible);
            }

            sub.ExpectEnd();
            if (!sub.Ok)
            {
                r.Fail(sub.Error);
                break;
            }

            // An entity the subscriber already has is replaced: a late subscriber's snapshot can already include it.
            World.Remove(new EntityId(id));
            World.AddWithId(entity, new EntityId(id));
        }

        var deltas = r.ReadCount(SyncLimits.MaxEntities);
        for (var n = 0; n < deltas && r.Ok; n++)
        {
            var id = r.ReadVarUInt();
            var body = r.ReadBytes(int.MaxValue);
            if (!r.Ok)
            {
                break;
            }

            var entity = World.Find(new EntityId(id));
            if (entity is null)
            {
                r.Fail(SyncError.Mismatch);
                break;
            }

            if (entity.ViewMask != ulong.MaxValue)
            {
                // A filtered entity only ever receives changes to the properties it holds.
                var peek = new SyncReader(body);
                if ((peek.ReadVarUInt() & ~entity.ViewMask) != 0)
                {
                    r.Fail(SyncError.InvalidValue);
                    break;
                }
            }

            var sub = new SyncReader(body);
            entity.ApplyDelta(ref sub);
            sub.ExpectEnd();
            if (!sub.Ok)
            {
                r.Fail(sub.Error);
                break;
            }
        }

        var tables = r.ReadCount(SyncLimits.MaxNamed);
        for (var n = 0; n < tables && r.Ok; n++)
        {
            // The name is matched as bytes: tables are applied every tick, and a string per tick is garbage for nothing.
            var nameBytes = r.ReadBytes(SyncLimits.MaxStringBytes);
            var body = r.ReadBytes(int.MaxValue);
            if (!r.Ok)
            {
                break;
            }

            var table = FindTable(nameBytes);
            if (table is null)
            {
                r.Fail(SyncError.Mismatch);
                break;
            }

            var sub = new SyncReader(body);
            table.Read(ref sub);
            sub.ExpectEnd();
            if (!sub.Ok)
            {
                r.Fail(sub.Error);
                break;
            }
        }

        ulong authorityHash = 0;
        if (r.Ok && (flags & 2) != 0)
        {
            authorityHash = r.ReadFixed64();
        }

        r.ExpectEnd();
        if (!r.Ok)
        {
            Desync("frame body: " + r.Error);
            return;
        }

        World.SetReplicaTick(tick);
        LastTick = tick;
        FramesApplied++;
        _failures = 0;
        if (_awaitingDigest && tick - _digestRequestedTick >= Node.Options.DigestTimeoutTicks)
        {
            // The authority never answered. Report the drift without detail rather than waiting forever.
            _awaitingDigest = false;
            Node.FileDrift(new DriftReport(Id, Node.Name, _pendingTick, _pendingAuthorityHash, _pendingLocalHash, [], false, timedOut: true));
            if (Node.Options.ResyncOnDrift)
            {
                Resync();
                return;
            }
        }

        if ((flags & 2) != 0)
        {
            var local = World.ComputeViewHash();
            if (local != authorityHash)
            {
                BeginDrift(tick, authorityHash, local);
            }
        }
    }

    private IHotTable? FindTable(ReadOnlySpan<byte> nameBytes)
    {
        Span<byte> buffer = stackalloc byte[256];
        var tables = World.Tables;
        for (var k = 0; k < tables.Count; k++)
        {
            if (System.Text.Encoding.UTF8.TryGetBytes(tables[k].Name, buffer, out var written) && buffer[..written].SequenceEqual(nameBytes))
            {
                return tables[k];
            }
        }

        return null;
    }

    private void BeginDrift(long tick, ulong authorityHash, ulong localHash)
    {
        if (_awaitingDigest)
        {
            return;
        }

        // Capture the replica side now: by the time the authority answers, newer frames may have changed it.
        StateDigest.CaptureView(World, _localDigest, _digestScratch, Node.Options.DigestEntityLimit, null, World.RandomIncluded);
        _awaitingDigest = true;
        _digestRequestedTick = LastTick;
        _pendingTick = tick;
        _pendingAuthorityHash = authorityHash;
        _pendingLocalHash = localHash;
        var w = Node.Out;
        w.Reset();
        w.WriteByte(Msg.DigestRequest);
        w.WriteVarUInt(Id.Value);
        w.WriteVarInt(tick);
        Link.Channel.Send(w.Written);
    }

    private void ApplyDigest(ref SyncReader r)
    {
        var tick = r.ReadVarInt();
        var present = r.ReadBool();
        if (!r.Ok || !_awaitingDigest || tick != _pendingTick)
        {
            return;
        }

        var differences = new List<StateDifference>();
        var detail = false;
        if (present)
        {
            var expected = new StateDigest();
            expected.Read(ref r);
            if (r.Ok)
            {
                detail = !expected.Truncated && !_localDigest.Truncated;
                if (detail)
                {
                    StateDigest.Diff(expected, _localDigest, null, World, differences);
                }
            }
            else
            {
                Node.ReportProtocolError(Id, LastTick, "digest: " + r.Error);
            }
        }

        _awaitingDigest = false;
        Node.FileDrift(new DriftReport(Id, Node.Name, _pendingTick, _pendingAuthorityHash, _pendingLocalHash, differences, detail));
        if (Node.Options.ResyncOnDrift)
        {
            Resync();
        }
    }

    private void Answer(uint sequence, CommandResult result)
    {
        for (var i = 0; i < _pending.Count; i++)
        {
            if (_pending[i].Sequence == sequence)
            {
                _pending.RemoveAt(i);
                Complete(result);
                return;
            }
        }

        // An answer to a command sent through the harness path, or one already given up on: nothing waits for it.
    }

    private void Complete(CommandResult result)
    {
        for (var i = 0; i < _pending.Count; i++)
        {
            if (_pending[i].Sequence == result.Sequence)
            {
                _pending.RemoveAt(i);
                break;
            }
        }

        if (_results.Count >= MaxResults)
        {
            _results.RemoveAt(0);
        }

        _results.Add(result);
        CommandCompleted?.Invoke(result);
    }

    private void DropPending()
    {
        while (_pending.Count > 0)
        {
            var p = _pending[0];
            Complete(new CommandResult(Id, p.Sequence, p.Entity, p.CommandId, CommandVerdict.Dropped, default, null));
        }
    }

    private readonly record struct PendingSend(uint Sequence, EntityId Entity, uint CommandId);

    private void Desync(string reason)
    {
        Node.ReportProtocolError(Id, LastTick, reason);
        _failures++;
        Resync();
    }

    private void Resync()
    {
        if (_failures > MaxResyncAttempts)
        {
            State = ReplicaState.Failed;
            return;
        }

        State = ReplicaState.Resyncing;
        Resyncs++;
        RequestSnapshot();
    }
}
