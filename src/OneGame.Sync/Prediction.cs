namespace OneGame.Sync;

/// <summary>Settings for a replica's prediction (<see cref="ReplicaScope.EnablePrediction"/>).</summary>
public sealed class PredictionOptions
{
    /// <summary>
    /// How many ticks the predicted world runs ahead of the last confirmed tick, at least. A command sent now reaches the authority after
    /// about half a round trip and its effect comes back after the rest, so a lead near the round trip in ticks makes the predicted world
    /// show what the authority will show when the command's effect arrives. With <see cref="AutoLead"/> this is only the starting value.
    /// </summary>
    public int LeadTicks { get; init; }

    /// <summary>
    /// Measure the lead instead of trusting <see cref="LeadTicks"/> alone. When the authority answers a command, the tick it ran in is
    /// known exactly; if the predicted world ran that command earlier or later than the authority did, the lead moves by the difference
    /// (it follows the 75th percentile of the last 32 answers: up at once, down one tick per 8 answers). <see cref="LeadTicks"/> is where it starts.
    /// </summary>
    public bool AutoLead { get; init; } = true;

    /// <summary>The most the measured lead may grow to.</summary>
    public int MaxLeadTicks { get; init; } = 60;

    /// <summary>The predicted world stops advancing when it is this many ticks ahead of the confirmed state (a stalled link must not run the prediction away).</summary>
    public int MaxAheadTicks { get; init; } = 120;

    /// <summary>The most unanswered predicted commands kept for resimulation; older ones are given up on (the authority's state still arrives).</summary>
    public int MaxUnconfirmed { get; init; } = 256;

    /// <summary>
    /// Hash the predicted world before and after each reconciliation to count <see cref="Prediction.Rollbacks"/> (reconciliations that changed
    /// what a view was showing). Costs two world hashes per reconciliation; allocation-free.
    /// </summary>
    public bool DetectRollbacks { get; init; } = true;
}

/// <summary>
/// Client-side prediction for one replica. The replica's own world stays exactly the authority's confirmed state. Prediction keeps a second,
/// writable world beside it: commands marked <c>[Command(Predicted = true)]</c> run on it the moment they are sent, and whenever
/// authoritative state arrives it is copied over the predicted world, the commands the authority has not yet applied are run again, and the
/// predicted world is simulated forward to where it was (a rollback and resimulation). Views draw <see cref="World"/>; sim state is never
/// blended, so any smoothing of a visible correction belongs in view code (see <see cref="Reconciling"/> and <see cref="Reconciled"/>).
/// </summary>
/// <remarks>
/// Reading <see cref="World"/> (or calling <see cref="Advance"/>) first brings the prediction up to date with whatever the replica has
/// received since, so call them from the thread that pumps the node.
/// </remarks>
public sealed class Prediction
{
    private readonly ReplicaScope _owner;
    private readonly World _world;
    private readonly PredictionOptions _options;
    private readonly List<Slot> _slots = [];
    private readonly Stack<Slot> _free = new();
    private bool _dirty = true;
    private bool _reconciling;
    private long _tick;
    private int _lead;
    private int _stall;
    private int _below;
    private readonly int[] _samples = new int[32];
    private int _sampleNext;
    private int _sampleCount;

    internal Prediction(ReplicaScope owner, World world, PredictionOptions options)
    {
        _owner = owner;
        _world = world;
        _options = options;
        world.Observer = owner.Forward;
        _lead = Math.Clamp(options.LeadTicks, 0, options.MaxLeadTicks);
    }

    /// <summary>The predicted world: what the authority will most likely show once the commands in flight are applied. Read it; send commands through its entities or the replica's.</summary>
    public World World
    {
        get
        {
            Reconcile();
            return _world;
        }
    }

    /// <summary>The tick the predicted world is at.</summary>
    public long Tick
    {
        get
        {
            Reconcile();
            return _tick;
        }
    }

    /// <summary>How many ticks ahead of the confirmed state the predicted world is rebuilt to when a frame arrives: <see cref="PredictionOptions.LeadTicks"/>, then what the measured round trips asked for.</summary>
    public int LeadTicks => _lead;

    /// <summary>Predicted commands the authority has not yet applied to the confirmed state.</summary>
    public int Unconfirmed => _slots.Count;

    /// <summary>Predicted commands run so far.</summary>
    public long CommandsPredicted { get; private set; }

    /// <summary>Predicted commands that were sent without being predicted because they use a random stream this replica does not receive.</summary>
    public long CommandsSkipped { get; private set; }

    /// <summary>How many times the predicted world was rebuilt from confirmed state and simulated forward.</summary>
    public long Reconciliations { get; private set; }

    /// <summary>Reconciliations that changed what the view was showing at the same tick: a command the authority rejected, or state it changed that the prediction missed.</summary>
    public long Rollbacks { get; private set; }

    /// <summary>Ticks simulated again during reconciliations.</summary>
    public long ResimulatedTicks { get; private set; }

    /// <summary>Raised before a reconciliation overwrites the predicted world: it still holds what the view has been showing. A view records the positions it draws here.</summary>
    public event Action<Prediction>? Reconciling;

    /// <summary>Raised after a reconciliation, with the corrected predicted world. A view compares with what it recorded in <see cref="Reconciling"/> and blends the difference in over a few frames.</summary>
    public event Action<Prediction>? Reconciled;

    /// <summary>
    /// Simulates one tick on the predicted world, one per local frame, so the view moves while the authority's frames are still on their way.
    /// Returns false (and does nothing) while the replica has no state yet, or when the predicted world is already <see cref="PredictionOptions.MaxAheadTicks"/> ahead.
    /// </summary>
    public bool Advance()
    {
        if (_owner.State != ReplicaState.Live)
        {
            return false;
        }

        Reconcile();
        if (_stall > 0)
        {
            // The lead shrank: hold the predicted clock for a tick so the confirmed one catches up with it.
            _stall--;
            return false;
        }

        if (_tick - _owner.World.Tick >= _options.MaxAheadTicks)
        {
            return false;
        }

        _world.Step();
        _tick = _world.Tick;
        return true;
    }

    /// <summary>Brings the predicted world up to date now. Reading <see cref="World"/> does it already.</summary>
    public void Reconcile()
    {
        if (!_dirty || _reconciling)
        {
            return;
        }

        _dirty = false;
        _reconciling = true;
        try
        {
            Reconciling?.Invoke(this);
            var before = _options.DetectRollbacks ? _world.ComputeViewHash() : 0UL;
            var beforeTick = _world.Tick;
            var confirmed = _owner.World;
            var confirmedTick = confirmed.Tick;
            _world.CopyFrom(confirmed);

            var target = Math.Max(_tick, confirmedTick + _lead);
            target = Math.Max(target, confirmedTick);
            var next = 0;
            for (var k = confirmedTick; ; k++)
            {
                // A command the authority has not applied runs at the first tick the confirmed state has not passed: where it was sent, or now.
                while (next < _slots.Count && Math.Max(_slots[next].SendTick, confirmedTick) <= k)
                {
                    var slot = _slots[next++];
                    _ = _world.ExecuteNow(slot.Entity, slot.CommandId, slot.Payload.AsSpan(0, slot.Length));
                }

                if (k >= target)
                {
                    break;
                }

                _world.Step();
                ResimulatedTicks++;
            }

            _tick = _world.Tick;
            Reconciliations++;
            if (_options.DetectRollbacks && beforeTick == _tick && before != _world.ComputeViewHash())
            {
                Rollbacks++;
            }

            Reconciled?.Invoke(this);
        }
        finally
        {
            _reconciling = false;
        }
    }

    // ---- fed by the replica -------------------------------------------------------------------------------------------

    internal void OnSent(uint sequence, EntityId entity, uint commandId, ReadOnlySpan<byte> payload)
    {
        if (_reconciling || _owner.State != ReplicaState.Live)
        {
            return;
        }

        var target = _world.Find(entity) ?? _owner.World.Find(entity);
        if (target is null || !target.IsPredictedCommand(commandId))
        {
            return;
        }

        if (target.PredictedCommandUsesRandom(commandId) && !_owner.World.RandomIncluded)
        {
            CommandsSkipped++;
            return;
        }

        Reconcile();
        if (_slots.Count >= _options.MaxUnconfirmed)
        {
            Release(0);
        }

        var slot = _free.Count > 0 ? _free.Pop() : new Slot();
        if (slot.Payload.Length < payload.Length)
        {
            slot.Payload = new byte[Math.Max(payload.Length, 32)];
        }

        payload.CopyTo(slot.Payload);
        slot.Length = payload.Length;
        slot.Sequence = sequence;
        slot.Entity = entity;
        slot.CommandId = commandId;
        slot.SendTick = _tick;
        slot.LeadAtSend = _lead;
        slot.ExecutedTick = 0;
        _slots.Add(slot);
        CommandsPredicted++;
        _ = _world.ExecuteNow(entity, commandId, payload);
    }

    internal void OnResult(CommandResult result)
    {
        for (var i = 0; i < _slots.Count; i++)
        {
            if (_slots[i].Sequence != result.Sequence)
            {
                continue;
            }

            if (result.Verdict == CommandVerdict.Executed)
            {
                // Results travel in order with frames: the frame of the tick that ran this command is the next one to arrive.
                _slots[i].ExecutedTick = _owner.LastTick + 1;
                if (_options.AutoLead)
                {
                    Retune(_slots[i]);
                }
            }
            else
            {
                // Rejected, refused or dropped: the effect the prediction showed will never happen.
                Release(i);
                _dirty = true;
            }

            return;
        }
    }

    private void Retune(Slot slot)
    {
        // The predicted world ran the command in the tick after SendTick; the authority ran it in ExecutedTick. What the lead should have
        // been is the lead at send time plus that difference. A lost packet makes a few answers late, so the lead follows the 75th
        // percentile of recent answers: spikes do not drag it up, a lasting change does.
        var desired = (int)Math.Clamp(slot.LeadAtSend + (slot.ExecutedTick - (slot.SendTick + 1)), 0, _options.MaxLeadTicks);
        _samples[_sampleNext++ % _samples.Length] = desired;
        _sampleCount = Math.Min(_sampleCount + 1, _samples.Length);
        var target = desired;
        if (_sampleCount >= 8)
        {
            Span<int> sorted = stackalloc int[_samples.Length];
            _samples.AsSpan(0, _sampleCount).CopyTo(sorted);
            sorted = sorted[.._sampleCount];
            sorted.Sort();
            target = sorted[(_sampleCount * 3) / 4];
        }

        if (target > _lead)
        {
            _lead = target;
            _below = 0;
            _dirty = true;
        }
        else if (target < _lead)
        {
            if (++_below >= 8)
            {
                _lead--;
                _stall++;
                _below = 0;
                _dirty = true;
            }
        }
        else
        {
            _below = 0;
        }
    }

    /// <summary>The confirmed state changed: a frame or snapshot arrived, or the replica was emptied.</summary>
    internal void OnConfirmed(long tick, bool snapshot = false)
    {
        if (snapshot)
        {
            // A new baseline: the confirmed tick may have jumped, so the predicted clock starts over from it.
            _tick = 0;
        }

        for (var i = _slots.Count - 1; i >= 0; i--)
        {
            if (_slots[i].ExecutedTick != 0 && _slots[i].ExecutedTick <= tick)
            {
                Release(i);
            }
        }

        _dirty = true;
    }

    internal void Clear()
    {
        while (_slots.Count > 0)
        {
            Release(_slots.Count - 1);
        }

        _dirty = true;
    }

    private void Release(int index)
    {
        _free.Push(_slots[index]);
        _slots.RemoveAt(index);
    }

    private sealed class Slot
    {
        public byte[] Payload = [];
        public int Length;
        public uint Sequence;
        public EntityId Entity;
        public uint CommandId;
        public long SendTick;
        public long ExecutedTick;
        public int LeadAtSend;
    }
}
