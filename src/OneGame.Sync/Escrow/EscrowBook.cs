namespace OneGame.Sync;

/// <summary>
/// The game's side of an escrow: how value leaves and enters its holdings. Both calls run inside a scope's simulation (a command on the
/// <see cref="EscrowDesk"/>), so they must be deterministic and must change only that scope's state.
/// </summary>
public interface IEscrowHandler
{
    /// <summary>Takes <paramref name="amount"/> of <paramref name="kind"/> out of <paramref name="owner"/>'s holdings. Returns false, changing nothing, if the owner does not have it.</summary>
    bool TryDebit(World world, uint owner, uint kind, long amount);

    /// <summary>Adds <paramref name="amount"/> of <paramref name="kind"/> to <paramref name="owner"/>'s holdings. Returns false, changing nothing, if there is no such owner here.</summary>
    bool Credit(World world, uint owner, uint kind, long amount);
}

/// <summary>Where one trade stands on the coordinator.</summary>
public enum EscrowState : byte
{
    /// <summary>Waiting for both givers' values to be taken and held.</summary>
    Collecting = 1,

    /// <summary>Both values are held; the coordinator decided to deliver them to the counterparties.</summary>
    Committed = 2,

    /// <summary>The trade failed or timed out; whatever was taken is being returned to its giver.</summary>
    Cancelled = 3,

    /// <summary>Both values were delivered. Final.</summary>
    Settled = 4,

    /// <summary>Everything taken was returned. Final.</summary>
    Refunded = 5,
}

/// <summary>Whether a leg's value has been taken from its giver.</summary>
public enum EscrowHold : byte
{
    /// <summary>Not known yet.</summary>
    Pending = 0,

    /// <summary>Taken from the giver and held (the participant keeps a receipt).</summary>
    Held = 1,

    /// <summary>Never taken, and never will be.</summary>
    Failed = 2,
}

/// <summary>Whether a leg's value has been handed on (to the counterparty after a commit, back to the giver after a cancel).</summary>
public enum EscrowDelivery : byte
{
    /// <summary>Nothing to deliver (yet, or never).</summary>
    None = 0,

    /// <summary>The coordinator wants it delivered; the credit has not been confirmed.</summary>
    Pending = 1,

    /// <summary>The credit was applied exactly once.</summary>
    Delivered = 2,
}

/// <summary>One direction of a trade: <see cref="Giver"/> on <see cref="Scope"/> gives <see cref="Amount"/> of <see cref="Kind"/> to <see cref="Receiver"/> on the other leg's scope.</summary>
public sealed class EscrowLeg
{
    /// <summary>The scope that holds the giver.</summary>
    public ScopeId Scope { get; set; }

    /// <summary>Who gives.</summary>
    public uint Giver { get; set; }

    /// <summary>Who receives, on the other leg's scope.</summary>
    public uint Receiver { get; set; }

    /// <summary>What kind of value (the game's own numbering).</summary>
    public uint Kind { get; set; }

    /// <summary>How much; positive.</summary>
    public long Amount { get; set; }

    /// <summary>Whether the value was taken from the giver.</summary>
    public EscrowHold Hold { get; internal set; }

    /// <summary>Whether the value was handed on.</summary>
    public EscrowDelivery Delivery { get; internal set; }

    internal EscrowLeg Copy() => (EscrowLeg)MemberwiseClone();
}

/// <summary>A trade between two scopes, as the coordinator records it.</summary>
public sealed class EscrowTrade
{
    internal EscrowTrade(ulong tx, long openedTick, EscrowLeg a, EscrowLeg b)
    {
        Tx = tx;
        OpenedTick = openedTick;
        StateTick = openedTick;
        State = EscrowState.Collecting;
        Legs = [a, b];
    }

    /// <summary>The transaction id, chosen by whoever opened the trade; unique.</summary>
    public ulong Tx { get; }

    /// <summary>Where the trade stands.</summary>
    public EscrowState State { get; internal set; }

    /// <summary>The coordinator tick the trade was opened in.</summary>
    public long OpenedTick { get; }

    /// <summary>The coordinator tick of the last change. Hosts act on a decision only once a save at or after this tick exists.</summary>
    public long StateTick { get; internal set; }

    /// <summary>The two legs.</summary>
    public IReadOnlyList<EscrowLeg> Legs { get; }

    /// <summary>True once the trade can no longer change.</summary>
    public bool IsFinal => State is EscrowState.Settled or EscrowState.Refunded;

    /// <summary>The scope and owner a leg's value goes to: the counterparty after a commit, the giver after a cancel.</summary>
    public (ScopeId Scope, uint Owner) DeliveryTarget(int leg)
        => State == EscrowState.Committed || State == EscrowState.Settled
            ? (Legs[1 - leg].Scope, Legs[leg].Receiver)
            : (Legs[leg].Scope, Legs[leg].Giver);
}

/// <summary>What a participant's step did.</summary>
public enum EscrowStep : byte
{
    /// <summary>Taking the giver's value and keeping a receipt.</summary>
    Withdraw = 1,

    /// <summary>Making sure a withdraw that has not happened never will.</summary>
    Abort = 2,

    /// <summary>Crediting the receiver.</summary>
    Credit = 3,
}

/// <summary>
/// The escrow records of a scope, kept in its world (so saves, snapshots and replays carry them): on the coordinator, the trades; on every
/// participant, a receipt for each step it took, keyed by transaction and leg. A step that finds its receipt does nothing and answers the
/// same as the first time, which is what makes retries, duplicates and reordering harmless. Add it with <see cref="Escrow.Install"/>.
/// </summary>
public sealed class EscrowBook : IHotTable
{
    /// <summary>The table's name in snapshots.</summary>
    public const string TableName = "og.escrow";

    /// <summary>Ticks the coordinator waits for both values to be held before it cancels. Default 300.</summary>
    public const int DefaultCollectTicks = 300;

    private const int MaxTrades = 4096;
    private const int MaxReceipts = 32768;

    private readonly List<EscrowTrade> _trades = [];
    private readonly List<Receipt> _receipts = [];
    private readonly World _world;

    internal EscrowBook(World world, IEscrowHandler handler)
    {
        _world = world;
        Handler = handler;
    }

    /// <summary>How the game debits and credits its holdings.</summary>
    public IEscrowHandler Handler { get; }

    /// <summary>Ticks the coordinator waits for both values before cancelling.</summary>
    public int CollectTicks { get; set; } = DefaultCollectTicks;

    /// <inheritdoc />
    public string Name => TableName;

    /// <inheritdoc />
    public int Count => _trades.Count + _receipts.Count;

    /// <inheritdoc />
    public Visibility Visibility => Visibility.Hidden;

    /// <summary>The trades this scope coordinates, oldest first.</summary>
    public IReadOnlyList<EscrowTrade> Trades => _trades;

    /// <summary>The book of a world, or null.</summary>
    public static EscrowBook? Of(World world)
    {
        var tables = world.Tables;
        for (var i = 0; i < tables.Count; i++)
        {
            if (tables[i] is EscrowBook book)
            {
                return book;
            }
        }

        return null;
    }

    /// <summary>The trade with this id, or null.</summary>
    public EscrowTrade? FindTrade(ulong tx)
    {
        foreach (var t in _trades)
        {
            if (t.Tx == tx)
            {
                return t;
            }
        }

        return null;
    }

    // ---- participant steps -------------------------------------------------------------------------------------------

    /// <summary>Takes a giver's value and keeps a receipt. Repeats answer ok without taking anything again.</summary>
    public EscrowReply Withdraw(ulong tx, int leg, uint owner, uint kind, long amount)
    {
        if (FindReceipt(tx, leg, EscrowStep.Withdraw) is { } r)
        {
            return r.Result == ReceiptResult.Held ? EscrowReply.Ok : EscrowReply.No("aborted");
        }

        if (amount <= 0 || leg is < 0 or > 1)
        {
            return EscrowReply.No("invalid");
        }

        if (!Handler.TryDebit(_world, owner, kind, amount))
        {
            return EscrowReply.No("insufficient");
        }

        AddReceipt(new Receipt(tx, (byte)leg, EscrowStep.Withdraw, ReceiptResult.Held, kind, owner, amount));
        return EscrowReply.Ok;
    }

    /// <summary>Makes sure a withdraw that has not happened never will. Answers ok if it never happened, "held" if the value was already taken.</summary>
    public EscrowReply Abort(ulong tx, int leg)
    {
        if (FindReceipt(tx, leg, EscrowStep.Withdraw) is { } r)
        {
            return r.Result == ReceiptResult.Aborted ? EscrowReply.Ok : EscrowReply.No("held");
        }

        if (leg is < 0 or > 1)
        {
            return EscrowReply.No("invalid");
        }

        AddReceipt(new Receipt(tx, (byte)leg, EscrowStep.Withdraw, ReceiptResult.Aborted, 0, 0, 0));
        return EscrowReply.Ok;
    }

    /// <summary>Credits a receiver once. Repeats answer ok without crediting again.</summary>
    public EscrowReply Credit(ulong tx, int leg, uint owner, uint kind, long amount)
    {
        if (FindReceipt(tx, leg, EscrowStep.Credit) is not null)
        {
            return EscrowReply.Ok;
        }

        if (amount <= 0 || leg is < 0 or > 1)
        {
            return EscrowReply.No("invalid");
        }

        if (!Handler.Credit(_world, owner, kind, amount))
        {
            return EscrowReply.No("no such owner");
        }

        AddReceipt(new Receipt(tx, (byte)leg, EscrowStep.Credit, ReceiptResult.Credited, kind, owner, amount));
        return EscrowReply.Ok;
    }

    private Receipt? FindReceipt(ulong tx, int leg, EscrowStep step)
    {
        for (var i = _receipts.Count - 1; i >= 0; i--)
        {
            var r = _receipts[i];
            if (r.Tx == tx && r.Leg == leg && r.Step == step)
            {
                return r;
            }
        }

        return null;
    }

    private void AddReceipt(Receipt r)
    {
        _receipts.Add(r);
        if (_receipts.Count > MaxReceipts)
        {
            _receipts.RemoveAt(0);
        }
    }

    // ---- coordinator -------------------------------------------------------------------------------------------------

    /// <summary>Opens a trade. A repeat of an open transaction id is accepted and changes nothing.</summary>
    public EscrowReply Open(ulong tx, long tick, EscrowLeg a, EscrowLeg b)
    {
        if (FindTrade(tx) is not null)
        {
            return EscrowReply.Ok;
        }

        if (a.Amount <= 0 || b.Amount <= 0 || a.Scope == b.Scope || a.Giver == 0 || b.Giver == 0 || a.Receiver == 0 || b.Receiver == 0)
        {
            return EscrowReply.No("invalid trade");
        }

        if (_trades.Count >= MaxTrades && !EvictFinal())
        {
            return EscrowReply.No("too many open trades");
        }

        _trades.Add(new EscrowTrade(tx, tick, a.Copy(), b.Copy()));
        return EscrowReply.Ok;
    }

    private bool EvictFinal()
    {
        for (var i = 0; i < _trades.Count; i++)
        {
            if (_trades[i].IsFinal)
            {
                _trades.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    /// <summary>A participant answered a withdraw (<paramref name="held"/>: the value is held, otherwise it is not and will not be) or an abort (<paramref name="held"/> false: stopped; true: it was already taken).</summary>
    public void ReportHold(ulong tx, int leg, bool held, long tick)
    {
        if (FindTrade(tx) is not { } t || leg is < 0 or > 1)
        {
            return;
        }

        var l = t.Legs[leg];
        if (l.Hold != EscrowHold.Pending)
        {
            return; // already known; a repeated answer changes nothing
        }

        l.Hold = held ? EscrowHold.Held : EscrowHold.Failed;
        if (held && t.State == EscrowState.Cancelled)
        {
            l.Delivery = EscrowDelivery.Pending; // it was taken after the trade was cancelled: give it back
        }

        Touch(t, tick);
        Advance(t, tick);
    }

    /// <summary>A participant confirmed a credit.</summary>
    public void ReportDelivered(ulong tx, int leg, long tick)
    {
        if (FindTrade(tx) is not { } t || leg is < 0 or > 1)
        {
            return;
        }

        var l = t.Legs[leg];
        if (l.Delivery != EscrowDelivery.Pending)
        {
            return;
        }

        l.Delivery = EscrowDelivery.Delivered;
        Touch(t, tick);
        Advance(t, tick);
    }

    /// <summary>Moves every trade along: timeouts, decisions, completion. Runs once per tick on the coordinator's desk.</summary>
    public void Advance(long tick)
    {
        for (var i = 0; i < _trades.Count; i++)
        {
            if (!_trades[i].IsFinal)
            {
                Advance(_trades[i], tick);
            }
        }
    }

    private static void Touch(EscrowTrade t, long tick) => t.StateTick = tick;

    private void Advance(EscrowTrade t, long tick)
    {
        switch (t.State)
        {
            case EscrowState.Collecting:
                if (t.Legs[0].Hold == EscrowHold.Failed || t.Legs[1].Hold == EscrowHold.Failed || tick - t.OpenedTick >= CollectTicks)
                {
                    t.State = EscrowState.Cancelled;
                    foreach (var l in t.Legs)
                    {
                        if (l.Hold == EscrowHold.Held)
                        {
                            l.Delivery = EscrowDelivery.Pending;
                        }
                    }

                    Touch(t, tick);
                }
                else if (t.Legs[0].Hold == EscrowHold.Held && t.Legs[1].Hold == EscrowHold.Held)
                {
                    t.State = EscrowState.Committed;
                    t.Legs[0].Delivery = EscrowDelivery.Pending;
                    t.Legs[1].Delivery = EscrowDelivery.Pending;
                    Touch(t, tick);
                }

                break;
            case EscrowState.Committed:
                if (t.Legs[0].Delivery == EscrowDelivery.Delivered && t.Legs[1].Delivery == EscrowDelivery.Delivered)
                {
                    t.State = EscrowState.Settled;
                    Touch(t, tick);
                }

                break;
        }

        if (t.State == EscrowState.Cancelled)
        {
            var done = true;
            foreach (var l in t.Legs)
            {
                done &= l.Hold == EscrowHold.Failed || (l.Hold == EscrowHold.Held && l.Delivery == EscrowDelivery.Delivered);
            }

            if (done)
            {
                t.State = EscrowState.Refunded;
                Touch(t, tick);
            }
        }
    }

    // ---- audit -------------------------------------------------------------------------------------------------------

    /// <summary>
    /// The value that has left a giver and not yet arrived, by kind, over the books of all the scopes: a withdraw receipt with no credit
    /// receipt for the same transaction and leg anywhere. Holdings plus this sum is constant at every tick; a test asserts that.
    /// </summary>
    /// <param name="books">The books of every participating scope.</param>
    public static SortedDictionary<uint, long> InFlight(IEnumerable<EscrowBook> books)
    {
        var list = books.ToList();
        var credited = new HashSet<(ulong, byte)>();
        foreach (var b in list)
        {
            foreach (var r in b._receipts)
            {
                if (r.Step == EscrowStep.Credit)
                {
                    credited.Add((r.Tx, r.Leg));
                }
            }
        }

        var total = new SortedDictionary<uint, long>();
        foreach (var b in list)
        {
            foreach (var r in b._receipts)
            {
                if (r.Step == EscrowStep.Withdraw && r.Result == ReceiptResult.Held && !credited.Contains((r.Tx, r.Leg)))
                {
                    total[r.Kind] = total.GetValueOrDefault(r.Kind) + r.Amount;
                }
            }
        }

        return total;
    }

    /// <summary>How many receipts of a step this book holds (for tests and dashboards).</summary>
    public int ReceiptCount(EscrowStep step)
    {
        var n = 0;
        foreach (var r in _receipts)
        {
            if (r.Step == step)
            {
                n++;
            }
        }

        return n;
    }

    // ---- state ---------------------------------------------------------------------------------------------------------

    /// <inheritdoc />
    public void Clear()
    {
        _trades.Clear();
        _receipts.Clear();
    }

    /// <inheritdoc />
    public void Write(SyncWriter writer)
    {
        writer.WriteVarUInt((ulong)_trades.Count);
        foreach (var t in _trades)
        {
            writer.WriteVarUInt(t.Tx);
            writer.WriteByte((byte)t.State);
            writer.WriteVarInt(t.OpenedTick);
            writer.WriteVarInt(t.StateTick);
            foreach (var l in t.Legs)
            {
                writer.WriteVarUInt(l.Scope.Value);
                writer.WriteVarUInt(l.Giver);
                writer.WriteVarUInt(l.Receiver);
                writer.WriteVarUInt(l.Kind);
                writer.WriteVarInt(l.Amount);
                writer.WriteByte((byte)l.Hold);
                writer.WriteByte((byte)l.Delivery);
            }
        }

        writer.WriteVarUInt((ulong)_receipts.Count);
        foreach (var r in _receipts)
        {
            writer.WriteVarUInt(r.Tx);
            writer.WriteByte(r.Leg);
            writer.WriteByte((byte)r.Step);
            writer.WriteByte((byte)r.Result);
            writer.WriteVarUInt(r.Kind);
            writer.WriteVarUInt(r.Owner);
            writer.WriteVarInt(r.Amount);
        }
    }

    /// <inheritdoc />
    public void Read(ref SyncReader reader)
    {
        var tradeCount = reader.ReadCount(MaxTrades);
        var trades = new List<EscrowTrade>(Math.Min(tradeCount, 64));
        for (var i = 0; i < tradeCount && reader.Ok; i++)
        {
            var tx = reader.ReadVarUInt();
            var state = reader.ReadByte();
            var opened = reader.ReadVarInt();
            var changed = reader.ReadVarInt();
            var legs = new EscrowLeg[2];
            for (var k = 0; k < 2 && reader.Ok; k++)
            {
                var scope = reader.ReadVarUInt();
                var giver = reader.ReadVarUInt();
                var receiver = reader.ReadVarUInt();
                var kind = reader.ReadVarUInt();
                var amount = reader.ReadVarInt();
                var hold = reader.ReadByte();
                var delivery = reader.ReadByte();
                if (!reader.Ok)
                {
                    break;
                }

                if (scope > uint.MaxValue || giver > uint.MaxValue || receiver > uint.MaxValue || kind > uint.MaxValue || amount <= 0 || hold > (byte)EscrowHold.Failed || delivery > (byte)EscrowDelivery.Delivered)
                {
                    reader.Fail(SyncError.InvalidValue);
                    break;
                }

                legs[k] = new EscrowLeg { Scope = new ScopeId((uint)scope), Giver = (uint)giver, Receiver = (uint)receiver, Kind = (uint)kind, Amount = amount, Hold = (EscrowHold)hold, Delivery = (EscrowDelivery)delivery };
            }

            if (!reader.Ok)
            {
                break;
            }

            if (state is < (byte)EscrowState.Collecting or > (byte)EscrowState.Refunded)
            {
                reader.Fail(SyncError.InvalidValue);
                break;
            }

            trades.Add(new EscrowTrade(tx, opened, legs[0], legs[1]) { State = (EscrowState)state, StateTick = changed });
        }

        var receiptCount = reader.ReadCount(MaxReceipts);
        var receipts = new List<Receipt>(Math.Min(receiptCount, 256));
        for (var i = 0; i < receiptCount && reader.Ok; i++)
        {
            var tx = reader.ReadVarUInt();
            var leg = reader.ReadByte();
            var step = reader.ReadByte();
            var result = reader.ReadByte();
            var kind = reader.ReadVarUInt();
            var owner = reader.ReadVarUInt();
            var amount = reader.ReadVarInt();
            if (!reader.Ok)
            {
                break;
            }

            if (leg > 1 || step is < (byte)EscrowStep.Withdraw or > (byte)EscrowStep.Credit || result is < 1 or > 3 || kind > uint.MaxValue || owner > uint.MaxValue || amount < 0)
            {
                reader.Fail(SyncError.InvalidValue);
                break;
            }

            receipts.Add(new Receipt(tx, leg, (EscrowStep)step, (ReceiptResult)result, (uint)kind, (uint)owner, amount));
        }

        if (!reader.Ok)
        {
            return;
        }

        _trades.Clear();
        _trades.AddRange(trades);
        _receipts.Clear();
        _receipts.AddRange(receipts);
    }

    /// <inheritdoc />
    public void WriteJson(JsonOut json)
    {
        json.BeginObject();
        json.Name("trades");
        json.BeginArray();
        foreach (var t in _trades)
        {
            json.BeginObject();
            json.Name("tx");
            json.Value(t.Tx);
            json.Name("state");
            json.Value(t.State.ToString());
            json.EndObject();
        }

        json.EndArray();
        json.Name("receipts");
        json.Value((ulong)_receipts.Count);
        json.EndObject();
    }

    internal enum ReceiptResult : byte
    {
        Held = 1,
        Aborted = 2,
        Credited = 3,
    }

    internal sealed record Receipt(ulong Tx, byte Leg, EscrowStep Step, ReceiptResult Result, uint Kind, uint Owner, long Amount);
}

/// <summary>The answer to an escrow step: accepted (first time or repeat), or declined with a short reason.</summary>
/// <param name="Accepted">True when the step is in effect.</param>
/// <param name="Reason">Why not, when declined.</param>
public readonly record struct EscrowReply(bool Accepted, string? Reason)
{
    /// <summary>The step is in effect.</summary>
    public static EscrowReply Ok => new(true, null);

    /// <summary>The step was declined.</summary>
    public static EscrowReply No(string reason) => new(false, reason);
}
