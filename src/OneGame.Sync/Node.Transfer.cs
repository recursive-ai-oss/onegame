namespace OneGame.Sync;

/// <summary>Where a node should subscribe to follow entities that moved to another scope: a link to the node that holds it and an empty replica world.</summary>
/// <param name="Link">A link from this node to the destination scope's authority (or a relay of it), already trusted to read it.</param>
/// <param name="World">An empty replica world with the same registry and tables as the destination's.</param>
public sealed record FollowTarget(Link Link, World World);

public sealed partial class Node
{
    private const int MaxForwards = 4096;

    private readonly List<AuthorityScope> _relays = [];
    private readonly Dictionary<ulong, ForwardEntry> _forwards = [];
    private ulong _nextForward;

    /// <summary>
    /// Called when a scope this node follows reports that entities moved to another scope: return where to subscribe to it, or null to stay
    /// out. Without a resolver the node does not follow by itself.
    /// </summary>
    public Func<ScopeId, FollowTarget?>? FollowResolver { get; set; }

    /// <summary>Raised when the node subscribed to a scope by itself, through <see cref="FollowResolver"/>.</summary>
    public event Action<ReplicaScope>? ReplicaAdded;

    /// <summary>The relays this node runs (see <see cref="Relay"/>).</summary>
    public IReadOnlyList<AuthorityScope> Relays => _relays;

    /// <summary>
    /// Serves a replica onward: other nodes subscribe to this node for the same scope id and receive what the replica receives, filtered for
    /// each of them, with their commands passed up. The connection the replica uses must carry <see cref="Trust.AsRelay"/>.
    /// </summary>
    /// <param name="upstream">A replica made by <see cref="Subscribe"/> on this node.</param>
    /// <exception cref="ArgumentException">The replica belongs to another node, or this node already serves that scope id.</exception>
    public AuthorityScope Relay(ReplicaScope upstream)
    {
        if (upstream.Node != this)
        {
            throw new ArgumentException("The replica belongs to another node.", nameof(upstream));
        }

        if (FindAuthority(upstream.Id) is not null)
        {
            throw new ArgumentException($"{upstream.Id} is already served by node '{Name}'.", nameof(upstream));
        }

        var scope = new AuthorityScope(this, upstream);
        _authority.Add(scope);
        _relays.Add(scope);
        return scope;
    }

    internal bool TryRegisterForward(AuthorityScope scope, Link link, AuthorityScope.Origin? origin, out ulong sequence)
    {
        sequence = 0;
        if (_forwards.Count >= MaxForwards)
        {
            return false;
        }

        sequence = ++_nextForward;
        if (origin is { } o)
        {
            _forwards[sequence] = new ForwardEntry(scope, link, o);
        }

        return true;
    }

    /// <summary>Names the node a drift report's replica follows, so the report can name the hop.</summary>
    private string? UpstreamOf(DriftReport report)
    {
        ReplicaScope? match = null;
        foreach (var r in _replicas)
        {
            if (r.Id != report.Scope)
            {
                continue;
            }

            if (r.ViewHash == report.ReplicaHash)
            {
                return r.Link.PeerName;
            }

            match ??= r;
        }

        return match?.Link.PeerName;
    }

    // ---- the node receiving entities ------------------------------------------------------------------------------------

    private void HandleTransferMessage(Connection from, byte type, ScopeId scope, ref SyncReader r, ReadOnlySpan<byte> message)
    {
        switch (type)
        {
            case Msg.XferOffer:
            case Msg.XferCommit:
            {
                var id = r.ReadVarUInt();
                var bundle = r.ReadBytes(TransferLimits.MaxBundleBytes);
                r.ExpectEnd();
                if (!r.Ok)
                {
                    ReportProtocolError(scope, 0, "bad transfer message from " + from.Name + ": " + r.Error);
                    return;
                }

                var authority = FindAuthority(scope);
                if (authority is null || authority.IsRelay)
                {
                    ReplyRefused(from, scope, id, RefusalReason.UnknownScope);
                    return;
                }

                if (!from.Trust.Own.Contains(scope))
                {
                    ReplyRefused(from, scope, id, RefusalReason.NotTrustedToOwn);
                    return;
                }

                if (TransferLog.Of(authority.World) is not { } log)
                {
                    ReplyRefused(from, scope, id, RefusalReason.TransferNotAccepted);
                    return;
                }

                if (log.FindDone(id) is { } done)
                {
                    ReplyDone(authority, from, scope, id, done.NewIds);
                    return;
                }

                if (type == Msg.XferOffer)
                {
                    var entities = new List<Entity>();
                    var ids = new List<ulong>();
                    if (TransferOps.ReadBundle(authority.World, bundle, entities, ids) != SyncError.None)
                    {
                        ReplyRefused(from, scope, id, RefusalReason.Malformed);
                        return;
                    }

                    if (authority.TransferGate is { } gate && !gate(new TransferOffer(id, from, entities)))
                    {
                        ReplyRefused(from, scope, id, RefusalReason.TransferNotAccepted);
                        return;
                    }

                    Out.Reset();
                    Out.WriteByte(Msg.XferStaged);
                    Out.WriteVarUInt(scope.Value);
                    Out.WriteVarUInt(id);
                    from.Channel.Send(Out.Written);
                    return;
                }

                if (!authority.TryInstall(id, bundle, out var newIds))
                {
                    ReplyRefused(from, scope, id, RefusalReason.Malformed);
                    return;
                }

                ReplyDone(authority, from, scope, id, newIds);
                return;
            }

            case Msg.XferAbort:
                return; // the offering node keeps all the state; there is nothing to undo here

            case Msg.Forward:
            {
                var sequence = r.ReadVarUInt();
                var entity = r.ReadVarUInt();
                var command = r.ReadVarUInt();
                if (!r.Ok || command > uint.MaxValue || message.Length - r.Position > SyncLimits.MaxCommandBytes)
                {
                    ReportProtocolError(scope, 0, "bad forwarded command from " + from.Name);
                    return;
                }

                if (sequence <= from.LastForward)
                {
                    return; // a repeat
                }

                from.LastForward = sequence;
                var authority = FindAuthority(scope);
                if (authority is null || authority.IsRelay || !from.Trust.Own.Contains(scope) || !from.Trust.Command.Contains(scope))
                {
                    var reason = authority is null || authority.IsRelay ? RefusalReason.UnknownScope : RefusalReason.NotTrustedToOwn;
                    FileRefusal(new CommandRefusal(scope, from.Id, new EntityId(entity), (uint)command, reason));
                    Out.Reset();
                    Out.WriteByte(Msg.ForwardResult);
                    Out.WriteVarUInt(scope.Value);
                    Out.WriteVarUInt(sequence);
                    Out.WriteVarUInt(entity);
                    Out.WriteVarUInt(command);
                    Out.WriteByte((byte)CommandVerdict.Refused);
                    Out.WriteByte((byte)reason);
                    Out.WriteString(string.Empty);
                    from.Channel.Send(Out.Written);
                    return;
                }

                authority.QueueForwarded(from, sequence, new EntityId(entity), (uint)command, message[r.Position..]);
                return;
            }
        }
    }

    private void ReplyRefused(Connection to, ScopeId scope, ulong id, RefusalReason reason)
    {
        FileRefusal(new CommandRefusal(scope, to.Id, EntityId.None, 0, reason));
        Out.Reset();
        Out.WriteByte(Msg.XferRefused);
        Out.WriteVarUInt(scope.Value);
        Out.WriteVarUInt(id);
        Out.WriteByte((byte)reason);
        to.Channel.Send(Out.Written);
    }

    private void ReplyDone(AuthorityScope authority, Connection to, ScopeId scope, ulong id, ulong[] newIds)
    {
        Out.Reset();
        Out.WriteByte(Msg.XferDone);
        Out.WriteVarUInt(scope.Value);
        Out.WriteVarUInt(id);
        Out.WriteVarUInt((ulong)newIds.Length);
        foreach (var n in newIds)
        {
            Out.WriteVarUInt(n);
        }

        // The installed entities must be on disk before the sender is allowed to forget them.
        authority.SendDurable(to.Channel, Out.Written);
    }

    // ---- the node handing entities over, and subscribers following -----------------------------------------------------------

    private AuthorityScope? SourceOf(ulong transferId) => FindAuthority(new ScopeId((uint)(transferId >> 32)));

    private void HandlePeerReply(Link link, byte type, ScopeId scope, ref SyncReader r)
    {
        switch (type)
        {
            case Msg.XferStaged:
            {
                var id = r.ReadVarUInt();
                if (r.Ok)
                {
                    SourceOf(id)?.OnStaged(link, scope, id);
                }

                return;
            }

            case Msg.XferRefused:
            {
                var id = r.ReadVarUInt();
                var reason = r.ReadByte();
                if (r.Ok)
                {
                    SourceOf(id)?.OnRefused(link, id, (RefusalReason)reason);
                }

                return;
            }

            case Msg.XferDone:
            {
                var id = r.ReadVarUInt();
                var count = r.ReadCount(TransferLimits.MaxEntities);
                var ids = new ulong[count];
                for (var i = 0; i < count && r.Ok; i++)
                {
                    ids[i] = r.ReadVarUInt();
                }

                if (r.Ok)
                {
                    SourceOf(id)?.OnDone(link, id, ids);
                }

                return;
            }

            case Msg.ForwardResult:
            {
                var sequence = r.ReadVarUInt();
                r.ReadVarUInt();
                r.ReadVarUInt();
                var verdict = r.ReadByte();
                var refusal = r.ReadByte();
                var reason = r.ReadString();
                if (r.Ok && verdict <= (byte)CommandVerdict.Dropped && _forwards.TryGetValue(sequence, out var entry) && entry.Link == link)
                {
                    _forwards.Remove(sequence);
                    entry.Scope.AnswerForward(entry.Origin, (CommandVerdict)verdict, (RefusalReason)refusal, reason.Length == 0 ? null : reason);
                }

                return;
            }

            case Msg.Moved:
            {
                var dest = r.ReadVarUInt();
                var count = r.ReadCount(TransferLimits.MaxEntities);
                for (var i = 0; i < count && r.Ok; i++)
                {
                    r.ReadVarUInt();
                    r.ReadVarUInt();
                }

                if (!r.Ok || dest > uint.MaxValue)
                {
                    return;
                }

                // Only a scope this node really follows over this link may steer it.
                var known = false;
                foreach (var replica in _replicas)
                {
                    known |= replica.Link == link && replica.Id == scope;
                }

                if (known)
                {
                    Follow(new ScopeId((uint)dest));
                }

                return;
            }

            default:
                ReportProtocolError(scope, 0, "unexpected message type " + type);
                return;
        }
    }

    private void Follow(ScopeId destination)
    {
        foreach (var replica in _replicas)
        {
            if (replica.Id == destination && replica.State != ReplicaState.Unsubscribed)
            {
                return;
            }
        }

        if (FollowResolver?.Invoke(destination) is not { } target)
        {
            return;
        }

        var added = Subscribe(target.Link, destination, target.World);
        ReplicaAdded?.Invoke(added);
    }

    private readonly record struct ForwardEntry(AuthorityScope Scope, Link Link, AuthorityScope.Origin Origin);
}
