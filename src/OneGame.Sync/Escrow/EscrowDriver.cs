namespace OneGame.Sync;

/// <summary>
/// Host code on the coordinator's node that carries out what the coordinator's <see cref="EscrowBook"/> says is still to do: it sends the
/// withdraw, abort and credit steps to the participants' desks as commands and feeds their answers back to the coordinator's desk. It keeps
/// no record of its own beyond which steps it sent recently, so a node restarted from its last save simply starts sending again, and the
/// idempotent steps make the repeats harmless. Call <see cref="Pump"/> once per cluster tick.
/// </summary>
public sealed class EscrowDriver
{
    private readonly AuthorityScope _coordinator;
    private readonly Func<ScopeId, ReplicaScope?> _participant;
    private readonly Dictionary<(ulong Tx, int Leg, EscrowStep Step), long> _sentAt = [];
    private readonly Dictionary<(ReplicaScope Replica, uint Sequence), (ulong Tx, int Leg, EscrowStep Step)> _waiting = [];
    private readonly HashSet<ReplicaScope> _watched = [];
    private long _now;

    /// <summary>Creates a driver.</summary>
    /// <param name="coordinator">The coordinator's scope; its world has an escrow book (<see cref="Escrow.Install"/>).</param>
    /// <param name="participant">Finds this node's replica of a participant's scope, or null while it is not connected. The replica's link must carry <see cref="Escrow.CoordinatorTrust"/>.</param>
    /// <param name="resendTicks">Ticks before a step that got no answer is sent again.</param>
    /// <exception cref="ArgumentException">The coordinator's world has no escrow book or desk.</exception>
    public EscrowDriver(AuthorityScope coordinator, Func<ScopeId, ReplicaScope?> participant, int resendTicks = 10)
    {
        if (EscrowBook.Of(coordinator.World) is null || Escrow.DeskOf(coordinator.World) is null)
        {
            throw new ArgumentException("The coordinator's world needs Escrow.Install.", nameof(coordinator));
        }

        _coordinator = coordinator;
        _participant = participant;
        ResendTicks = resendTicks;
    }

    /// <summary>Ticks before an unanswered step is sent again.</summary>
    public int ResendTicks { get; set; }

    /// <summary>Steps sent so far, repeats included.</summary>
    public int StepsSent { get; private set; }

    /// <summary>Sends what is due. Call it once per cluster tick, after the step.</summary>
    public void Pump()
    {
        _now++;
        var book = EscrowBook.Of(_coordinator.World)!;
        var desk = Escrow.DeskOf(_coordinator.World)!;
        var durable = _coordinator.Durability == Durability.AfterSave;
        foreach (var trade in book.Trades)
        {
            if (trade.IsFinal)
            {
                continue;
            }

            // A decision that a restart could undo is not acted on: the coordinator's save must hold it first.
            if (durable && trade.StateTick > _coordinator.SavedTick)
            {
                continue;
            }

            for (var leg = 0; leg < 2; leg++)
            {
                var l = trade.Legs[leg];
                if (trade.State == EscrowState.Collecting && l.Hold == EscrowHold.Pending)
                {
                    Send(trade, leg, EscrowStep.Withdraw, l.Scope, desk);
                }
                else if (trade.State == EscrowState.Cancelled && l.Hold == EscrowHold.Pending)
                {
                    Send(trade, leg, EscrowStep.Abort, l.Scope, desk);
                }
                else if (l.Delivery == EscrowDelivery.Pending)
                {
                    Send(trade, leg, EscrowStep.Credit, trade.DeliveryTarget(leg).Scope, desk);
                }
            }
        }
    }

    private void Send(EscrowTrade trade, int leg, EscrowStep step, ScopeId scope, EscrowDesk coordinatorDesk)
    {
        var key = (trade.Tx, leg, step);
        if (_sentAt.TryGetValue(key, out var at) && _now - at < ResendTicks)
        {
            return;
        }

        var replica = _participant(scope);
        if (replica is null || replica.State != ReplicaState.Live || Escrow.DeskOf(replica.World) is not { } desk)
        {
            return;
        }

        Watch(replica, coordinatorDesk);
        var l = trade.Legs[leg];
        var (_, owner) = trade.DeliveryTarget(leg);
        switch (step)
        {
            case EscrowStep.Withdraw:
                desk.Send.Withdraw(trade.Tx, leg, l.Giver, l.Kind, l.Amount);
                break;
            case EscrowStep.Abort:
                desk.Send.Abort(trade.Tx, leg);
                break;
            default:
                desk.Send.Credit(trade.Tx, leg, owner, l.Kind, l.Amount);
                break;
        }

        _sentAt[key] = _now;
        _waiting[(replica, replica.LastSequence)] = key;
        StepsSent++;
    }

    private void Watch(ReplicaScope replica, EscrowDesk coordinatorDesk)
    {
        if (!_watched.Add(replica))
        {
            return;
        }

        replica.CommandCompleted += result =>
        {
            if (!_waiting.Remove((replica, result.Sequence), out var key))
            {
                return;
            }

            // Executed: the participant did it (now or earlier). Rejected: it declined, and the reason is final for this step.
            // Anything else (refused, dropped) says nothing about the step, so it stays due and is sent again.
            if (result.Verdict is not (CommandVerdict.Executed or CommandVerdict.Rejected))
            {
                return;
            }

            var ok = result.Verdict == CommandVerdict.Executed;
            if (key.Step == EscrowStep.Credit && !ok)
            {
                return; // the receiver does not exist there (yet); try again later
            }

            coordinatorDesk.Send.Report(key.Tx, key.Leg, (int)key.Step, ok);
            _sentAt.Remove(key);
        };
    }
}
