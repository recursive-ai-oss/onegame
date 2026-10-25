namespace OneGame.Sync;

/// <summary>When a scope lets answers and confirmations leave relative to saving its state.</summary>
public enum Durability
{
    /// <summary>Send as soon as it happens.</summary>
    Immediate = 0,

    /// <summary>Hold command answers and transfer confirmations until <see cref="AuthorityScope.Save"/> has captured the state they describe.</summary>
    AfterSave,
}

/// <summary>Where an authority transfer stands. See "Transfer, relaying and escrow" in the sync interface document.</summary>
public enum TransferState
{
    /// <summary>Requested; starts after the next tick.</summary>
    Pending = 0,

    /// <summary>The entities left the world and were offered to the other node.</summary>
    Offered,

    /// <summary>The other node accepted and this node decided to commit; waiting for it to confirm the install.</summary>
    Committed,

    /// <summary>The other node installed the entities. They are its authority now.</summary>
    Complete,

    /// <summary>The transfer did not happen; the entities are back here, unchanged.</summary>
    Aborted,
}

/// <summary>A running or finished move of entities from one scope to a scope on another node (<see cref="AuthorityScope.BeginTransfer"/>).</summary>
public sealed class Transfer
{
    internal Transfer(Link link, ScopeId destination, IReadOnlyList<EntityId> entities)
    {
        Link = link;
        Destination = destination;
        Entities = entities;
    }

    /// <summary>The transfer id (source scope in the high 32 bits); 0 until the transfer starts.</summary>
    public ulong Id { get; internal set; }

    /// <summary>Where the transfer stands.</summary>
    public TransferState State { get; internal set; }

    /// <summary>Why the transfer was aborted, or a note on the last problem; null otherwise.</summary>
    public string? Reason { get; internal set; }

    /// <summary>The entities being moved, by their ids in the source scope.</summary>
    public IReadOnlyList<EntityId> Entities { get; }

    /// <summary>The ids the entities got in the destination scope, in the order of <see cref="Entities"/>; empty until <see cref="TransferState.Complete"/>.</summary>
    public IReadOnlyList<EntityId> NewIds { get; internal set; } = [];

    /// <summary>The scope the entities go to.</summary>
    public ScopeId Destination { get; }

    /// <summary>The link to the destination node.</summary>
    public Link Link { get; }

    internal long OfferTick { get; set; }

    internal long LastSend { get; set; }

    internal byte[] Bundle { get; set; } = [];

    internal List<Held> Held { get; } = [];

    internal List<Connection> Watchers { get; } = [];

    internal Dictionary<ulong, Entity> Departed { get; } = [];
}

/// <summary>A command held back while its entity is in transit.</summary>
internal sealed record Held(Connection From, uint Sequence, EntityId Entity, uint CommandId, byte[] Payload);

/// <summary>An offer another node made to hand entities to a scope on this node; <see cref="AuthorityScope.TransferGate"/> may veto it.</summary>
/// <param name="Id">The transfer id.</param>
/// <param name="From">The connection the offer came on.</param>
/// <param name="Entities">The entities, decoded and not yet in the world (read their state, do not keep them).</param>
public sealed record TransferOffer(ulong Id, Connection From, IReadOnlyList<Entity> Entities);

public sealed partial class AuthorityScope
{
    private const int MaxHeld = 1024;
    private const int MaxMoved = 4096;

    private readonly List<Transfer> _transfers = [];
    private readonly Dictionary<ulong, Transfer> _inTransit = [];
    private readonly Dictionary<ulong, Moved> _moved = [];
    private readonly Queue<ulong> _movedOrder = new();

    /// <summary>Ticks an offer may go unanswered before the transfer is aborted and the entities come back. Default 120.</summary>
    public int TransferOfferTicks { get; set; } = 120;

    /// <summary>Ticks between resends of an offer or commit that has not been answered. Default 10.</summary>
    public int TransferResendTicks { get; set; } = 10;

    /// <summary>Called on the receiving scope for each offer; return false to refuse. Without a gate, any peer with <c>Trust.Own</c> on this scope may hand entities in.</summary>
    public Func<TransferOffer, bool>? TransferGate { get; set; }

    /// <summary>
    /// Finds the link to a destination scope's node for transfers left unfinished by a restart. After <see cref="Load"/>, a committed transfer
    /// in the log has no connection until this returns one.
    /// </summary>
    public Func<ScopeId, Link?>? TransferLinks { get; set; }

    /// <summary>Raised when this scope decided to commit a transfer, before the commit message is sent. With <see cref="Durability.AfterSave"/> the message waits for the next <see cref="Save"/>.</summary>
    public event Action<Transfer>? TransferCommitted;

    /// <summary>Raised when a transfer reaches <see cref="TransferState.Complete"/> or <see cref="TransferState.Aborted"/>.</summary>
    public event Action<Transfer>? TransferFinished;

    /// <summary>Raised on the receiving scope when a transfer was installed, with the transfer id and the ids the entities got here. A repeated commit does not raise it again.</summary>
    public event Action<ulong, IReadOnlyList<EntityId>>? TransferInstalled;

    /// <summary>Transfers that have not finished.</summary>
    public IReadOnlyList<Transfer> Transfers => _transfers;

    /// <summary>
    /// Starts moving entities to a scope on another node. Call it between ticks from host code, never from sim code. The entities leave the
    /// world after the next tick and arrive in the destination scope under new ids; see the sync interface document for the protocol.
    /// </summary>
    /// <param name="link">This node's link to the destination node; that node must trust it with <c>Own</c> on <paramref name="destination"/>.</param>
    /// <param name="destination">The scope on the other node.</param>
    /// <param name="entities">Ids of entities in this scope. Entities may not refer to each other or to anything left behind by id.</param>
    /// <exception cref="InvalidOperationException">This scope is a relay, or its world has no <see cref="TransferLog"/>.</exception>
    /// <exception cref="ArgumentException">The list is empty or too long, an entity does not exist or is already moving, or the link belongs to another node.</exception>
    public Transfer BeginTransfer(Link link, ScopeId destination, IEnumerable<EntityId> entities)
    {
        if (IsRelay || TransferLog.Of(World) is null)
        {
            throw new InvalidOperationException("Transfers need an authority world with a TransferLog table: call TransferLog.AddTo(world) when building it.");
        }

        if (link.Owner != Node)
        {
            throw new ArgumentException("The link belongs to another node.", nameof(link));
        }

        var list = new List<EntityId>(entities);
        if (list.Count == 0 || list.Count > TransferLimits.MaxEntities)
        {
            throw new ArgumentException($"A transfer carries 1 to {TransferLimits.MaxEntities} entities.", nameof(entities));
        }

        var seen = new HashSet<ulong>();
        foreach (var id in list)
        {
            if (World.Find(id) is null)
            {
                throw new ArgumentException($"Entity {id} is not in {Id}.", nameof(entities));
            }

            if (!seen.Add(id.Value) || _inTransit.ContainsKey(id.Value) || _transfers.Exists(t => t.State == TransferState.Pending && t.Entities.Contains(id)))
            {
                throw new ArgumentException($"Entity {id} is listed twice or is already being moved.", nameof(entities));
            }
        }

        var transfer = new Transfer(link, destination, list);
        _transfers.Add(transfer);
        return transfer;
    }

    // ---- source side ------------------------------------------------------------------------------------------------

    private void RunTransfers()
    {
        if (TransferLog.Of(World) is not { } log)
        {
            return;
        }

        Recover(log);
        var tick = World.Tick;
        for (var i = 0; i < _transfers.Count; i++)
        {
            var t = _transfers[i];
            switch (t.State)
            {
                case TransferState.Pending:
                    StartOffer(log, t);
                    break;
                case TransferState.Offered:
                    if (tick - t.OfferTick >= TransferOfferTicks)
                    {
                        Abort(t, $"the other node did not accept within {TransferOfferTicks} ticks");
                    }
                    else if (tick - t.LastSend >= TransferResendTicks)
                    {
                        t.LastSend = tick;
                        SendOffer(t);
                    }

                    break;
                case TransferState.Committed:
                    if (tick - t.LastSend >= TransferResendTicks)
                    {
                        t.LastSend = tick;
                        SendCommit(t);
                    }

                    break;
            }
        }

        for (var i = _transfers.Count - 1; i >= 0; i--)
        {
            if (_transfers[i].State is TransferState.Complete or TransferState.Aborted)
            {
                _transfers.RemoveAt(i);
            }
        }
    }

    /// <summary>After a load, picks up records the log holds but no transfer object represents: unanswered offers are undone, committed ones resume.</summary>
    private void Recover(TransferLog log)
    {
        if (log.OpenCount == 0)
        {
            return;
        }

        foreach (var record in log.Open.ToArray())
        {
            if (_transfers.Exists(t => t.Id == record.Id))
            {
                continue;
            }

            if (!record.Committed)
            {
                ApplyOp(TransferOps.Abort, TransferOps.IdPayload(record.Id));
                continue;
            }

            var link = TransferLinks?.Invoke(new ScopeId(record.DestScope));
            if (link is null)
            {
                continue;
            }

            var entities = new List<Entity>();
            var ids = new List<ulong>();
            TransferOps.ReadBundle(World, record.Bundle, entities, ids);
            var t = new Transfer(link, new ScopeId(record.DestScope), ids.ConvertAll(static i => new EntityId(i)))
            {
                Id = record.Id,
                State = TransferState.Committed,
                Bundle = record.Bundle,
                OfferTick = record.Tick,
                LastSend = long.MinValue / 2,
            };
            for (var i = 0; i < entities.Count; i++)
            {
                t.Departed[ids[i]] = entities[i];
                _inTransit[ids[i]] = t;
            }

            _transfers.Add(t);
        }
    }

    private void ResetTransfersAfterLoad()
    {
        foreach (var t in _transfers)
        {
            if (t.State is not (TransferState.Complete or TransferState.Aborted))
            {
                t.State = TransferState.Aborted;
                t.Reason = "the scope loaded a save";
            }
        }

        _transfers.Clear();
        _inTransit.Clear();
        _moved.Clear();
        _movedOrder.Clear();
    }

    private void StartOffer(TransferLog log, Transfer t)
    {
        var ids = new List<ulong>(t.Entities.Count);
        foreach (var id in t.Entities)
        {
            if (World.Find(id) is null)
            {
                t.State = TransferState.Aborted;
                t.Reason = $"entity {id} left the scope before the transfer started";
                TransferFinished?.Invoke(t);
                return;
            }

            ids.Add(id.Value);
        }

        // Remember who has seen these entities so that only they are told where they went.
        foreach (var view in _views)
        {
            foreach (var id in ids)
            {
                if (view.KnownIds.BinarySearch(id) >= 0)
                {
                    foreach (var c in view.Members)
                    {
                        if (!t.Watchers.Contains(c))
                        {
                            t.Watchers.Add(c);
                        }
                    }

                    break;
                }
            }
        }

        foreach (var id in ids)
        {
            t.Departed[id] = World.Find(new EntityId(id))!;
        }

        var tid = ((ulong)Id.Value << 32) | log.NextNumber;
        var payload = TransferOps.BeginPayload(tid, t.Destination.Value, ids);
        if (!ApplyOp(TransferOps.Begin, payload))
        {
            t.State = TransferState.Aborted;
            t.Reason = "the entities could not be taken out of the world";
            t.Departed.Clear();
            TransferFinished?.Invoke(t);
            return;
        }

        t.Id = tid;
        t.Bundle = log.FindOpen(tid)!.Bundle;
        t.State = TransferState.Offered;
        t.OfferTick = t.LastSend = World.Tick;
        foreach (var id in ids)
        {
            _inTransit[id] = t;
        }

        SendOffer(t);
    }

    private bool ApplyOp(uint op, byte[] payload, List<ulong>? newIds = null)
    {
        var ok = TransferOps.Apply(World, op, payload, newIds);
        if (ok)
        {
            _recorder?.AddCommand(World.Tick, EntityId.None, op, payload);
        }

        return ok;
    }

    private void SendOffer(Transfer t)
    {
        var w = Node.Scratch;
        w.Reset();
        w.WriteByte(Msg.XferOffer);
        w.WriteVarUInt(t.Destination.Value);
        w.WriteVarUInt(t.Id);
        w.WriteBytes(t.Bundle);
        t.Link.Channel.Send(w.Written);
    }

    private void SendCommit(Transfer t)
    {
        var w = Node.Scratch;
        w.Reset();
        w.WriteByte(Msg.XferCommit);
        w.WriteVarUInt(t.Destination.Value);
        w.WriteVarUInt(t.Id);
        w.WriteBytes(t.Bundle);
        SendDurable(t.Link.Channel, w.Written);
    }

    private void Abort(Transfer t, string reason)
    {
        ApplyOp(TransferOps.Abort, TransferOps.IdPayload(t.Id));
        t.State = TransferState.Aborted;
        t.Reason = reason;
        foreach (var id in t.Departed.Keys)
        {
            _inTransit.Remove(id);
        }

        t.Departed.Clear();
        var w = Node.Scratch;
        w.Reset();
        w.WriteByte(Msg.XferAbort);
        w.WriteVarUInt(t.Destination.Value);
        w.WriteVarUInt(t.Id);
        t.Link.Channel.Send(w.Written);

        // The commands that waited run here now, in the order they arrived.
        var held = t.Held.ToArray();
        t.Held.Clear();
        foreach (var h in held)
        {
            QueueExternal(h.From, h.Sequence, h.Entity, h.CommandId, h.Payload);
        }

        TransferFinished?.Invoke(t);
    }

    private Transfer? FindTransfer(ulong id) => _transfers.Find(t => t.Id == id && t.State != TransferState.Pending);

    /// <summary>The other node accepted the offer: decide, then tell it.</summary>
    internal void OnStaged(Link link, ScopeId destination, ulong id)
    {
        var t = FindTransfer(id);
        if (t is null)
        {
            // Finished or aborted here already. Tell the other node so it forgets the offer.
            var w = Node.Scratch;
            w.Reset();
            w.WriteByte(Msg.XferAbort);
            w.WriteVarUInt(destination.Value);
            w.WriteVarUInt(id);
            link.Channel.Send(w.Written);
            return;
        }

        if (t.Link != link || t.Destination != destination || t.State != TransferState.Offered)
        {
            return;
        }

        ApplyOp(TransferOps.Commit, TransferOps.IdPayload(id));
        t.State = TransferState.Committed;
        t.LastSend = World.Tick;
        TransferCommitted?.Invoke(t);
        SendCommit(t);
    }

    internal void OnRefused(Link link, ulong id, RefusalReason reason)
    {
        var t = FindTransfer(id);
        if (t is null || t.Link != link)
        {
            return;
        }

        if (t.State == TransferState.Offered)
        {
            Abort(t, "the other node refused: " + reason);
        }
        else if (t.State == TransferState.Committed)
        {
            t.Reason = "the other node refused the commit: " + reason;
        }
    }

    internal void OnDone(Link link, ulong id, ulong[] newIds)
    {
        var t = FindTransfer(id);
        if (t is null || t.Link != link || t.State != TransferState.Committed || newIds.Length != t.Entities.Count)
        {
            return;
        }

        ApplyOp(TransferOps.Done, TransferOps.IdPayload(id));
        t.State = TransferState.Complete;
        t.NewIds = Array.ConvertAll(newIds, static i => new EntityId(i));
        for (var i = 0; i < newIds.Length; i++)
        {
            var old = t.Entities[i].Value;
            _inTransit.Remove(old);
            _moved[old] = new Moved(t, newIds[i], t.Departed[old]);
            _movedOrder.Enqueue(old);
            while (_movedOrder.Count > MaxMoved)
            {
                _moved.Remove(_movedOrder.Dequeue());
            }
        }

        var held = t.Held.ToArray();
        t.Held.Clear();
        foreach (var h in held)
        {
            ForwardHeld(t, h, new EntityId(_moved[h.Entity.Value].NewId));
        }

        if (t.Watchers.Count > 0)
        {
            var w = Node.Scratch;
            w.Reset();
            w.WriteByte(Msg.Moved);
            w.WriteVarUInt(Id.Value);
            w.WriteVarUInt(t.Destination.Value);
            w.WriteVarUInt((ulong)newIds.Length);
            for (var i = 0; i < newIds.Length; i++)
            {
                w.WriteVarUInt(t.Entities[i].Value);
                w.WriteVarUInt(newIds[i]);
            }

            foreach (var c in t.Watchers)
            {
                c.Channel.Send(w.Written);
            }
        }

        TransferFinished?.Invoke(t);
    }

    // ---- commands for entities that left -------------------------------------------------------------------------------

    /// <summary>The entity object of an entity that is in transit or has moved, for owner checks on commands that still name it; null otherwise.</summary>
    internal Entity? FindDeparted(EntityId id)
    {
        if (_inTransit.TryGetValue(id.Value, out var t))
        {
            return t.Departed.GetValueOrDefault(id.Value);
        }

        return _moved.TryGetValue(id.Value, out var m) ? m.Entity : null;
    }

    /// <summary>Holds a command for an entity in transit, or forwards it for one that moved. Returns false when the entity is here.</summary>
    internal bool TryDivert(Connection from, uint sequence, EntityId entity, uint commandId, ReadOnlySpan<byte> payload)
    {
        if (_inTransit.TryGetValue(entity.Value, out var t))
        {
            if (t.Held.Count >= MaxHeld)
            {
                // Not applied and not going to be: say so.
                if (sequence != 0)
                {
                    SendResult(new Origin(from, sequence, entity, commandId, false), CommandVerdict.Dropped, RefusalReason.Malformed, entity, commandId, null);
                }

                return true;
            }

            t.Held.Add(new Held(from, sequence, entity, commandId, payload.ToArray()));
            return true;
        }

        if (_moved.TryGetValue(entity.Value, out var m))
        {
            ForwardHeld(m.Transfer, new Held(from, sequence, entity, commandId, payload.ToArray()), new EntityId(m.NewId));
            return true;
        }

        return false;
    }

    private void ForwardHeld(Transfer t, Held h, EntityId newId)
    {
        var origin = h.Sequence == 0 ? default(Origin?) : new Origin(h.From, h.Sequence, h.Entity, h.CommandId, false);
        if (!Node.TryRegisterForward(this, t.Link, origin, out var sequence))
        {
            if (origin is { } o)
            {
                SendResult(o, CommandVerdict.Dropped, RefusalReason.Malformed, h.Entity, h.CommandId, null);
            }

            return;
        }

        var w = Node.Scratch;
        w.Reset();
        w.WriteByte(Msg.Forward);
        w.WriteVarUInt(t.Destination.Value);
        w.WriteVarUInt(sequence);
        w.WriteVarUInt(newId.Value);
        w.WriteVarUInt(h.CommandId);
        w.WriteRaw(h.Payload);
        t.Link.Channel.Send(w.Written);
    }

    /// <summary>The destination answered a forwarded command: pass the answer to whoever sent it.</summary>
    internal void AnswerForward(Origin origin, CommandVerdict verdict, RefusalReason refusal, string? reason)
        => SendResult(origin, verdict, refusal, origin.Entity, origin.CommandId, reason);

    // ---- destination side -------------------------------------------------------------------------------------------

    /// <summary>Installs a bundle the source committed. Returns the ids the entities have here; a repeat of a finished transfer returns the same ids and changes nothing.</summary>
    internal bool TryInstall(ulong id, ReadOnlySpan<byte> bundle, out ulong[] ids)
    {
        ids = [];
        if (TransferLog.Of(World) is not { } log)
        {
            return false;
        }

        if (log.FindDone(id) is { } done)
        {
            ids = done.NewIds;
            return true;
        }

        var fresh = new List<ulong>();
        if (!ApplyOp(TransferOps.Install, TransferOps.InstallPayload(id, bundle), fresh))
        {
            return false;
        }

        ids = fresh.ToArray();
        TransferInstalled?.Invoke(id, Array.ConvertAll(ids, static i => new EntityId(i)));
        return true;
    }

    private sealed record Moved(Transfer Transfer, ulong NewId, Entity Entity);
}
