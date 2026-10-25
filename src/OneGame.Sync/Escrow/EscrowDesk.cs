namespace OneGame.Sync;

/// <summary>
/// The command surface of an escrow in a scope. Participants answer the coordinator's steps through it; the coordinator's desk also takes
/// trades (<see cref="Open"/>) and the participants' answers (<see cref="Report"/>). Every command is <c>OwnerOnly</c> with the coordinator
/// identity (<see cref="Escrow.CoordinatorPlayer"/>) as the owner, so an ordinary player cannot call any of them. Created by <see cref="Escrow.Install"/>.
/// </summary>
[Entity]
public partial class EscrowDesk : Entity
{
    /// <summary>The identity that owns the desk: <see cref="Escrow.CoordinatorPlayer"/>.</summary>
    [Synced, OwnerKey] public partial uint Coordinator { get; set; }

    /// <summary>Takes a giver's value into escrow. Declined when the giver has too little, or when the withdraw was aborted.</summary>
    [Command(OwnerOnly = true)]
    public CommandReply Withdraw(ulong tx, int leg, uint owner, uint kind, long amount) => Answer(Book()?.Withdraw(tx, leg, owner, kind, amount));

    /// <summary>Makes sure a withdraw that has not happened never will. Declined with "held" when the value was already taken.</summary>
    [Command(OwnerOnly = true)]
    public CommandReply Abort(ulong tx, int leg) => Answer(Book()?.Abort(tx, leg));

    /// <summary>Credits a receiver, once per transaction and leg.</summary>
    [Command(OwnerOnly = true)]
    public CommandReply Credit(ulong tx, int leg, uint owner, uint kind, long amount) => Answer(Book()?.Credit(tx, leg, owner, kind, amount));

    /// <summary>Coordinator: opens a trade between two scopes. A repeat of an open transaction id is accepted and ignored.</summary>
    [Command(OwnerOnly = true)]
    public CommandReply Open(
        ulong tx,
        uint scopeA, uint giverA, uint receiverA, uint kindA, long amountA,
        uint scopeB, uint giverB, uint receiverB, uint kindB, long amountB)
    {
        var a = new EscrowLeg { Scope = new ScopeId(scopeA), Giver = giverA, Receiver = receiverA, Kind = kindA, Amount = amountA };
        var b = new EscrowLeg { Scope = new ScopeId(scopeB), Giver = giverB, Receiver = receiverB, Kind = kindB, Amount = amountB };
        return Answer(Book()?.Open(tx, World!.Tick, a, b));
    }

    /// <summary>Coordinator: records a participant's answer. <paramref name="step"/> is an <see cref="EscrowStep"/>; <paramref name="ok"/> is the participant's verdict on it.</summary>
    [Command(OwnerOnly = true)]
    public CommandReply Report(ulong tx, int leg, int step, bool ok)
    {
        var book = Book();
        if (book is null)
        {
            return CommandReply.Reject("no escrow book");
        }

        switch ((EscrowStep)step)
        {
            case EscrowStep.Withdraw:
                book.ReportHold(tx, leg, ok, World!.Tick);
                break;
            case EscrowStep.Abort:
                // Stopped (ok) means the value was never taken; declined means it was already held.
                book.ReportHold(tx, leg, !ok, World!.Tick);
                break;
            case EscrowStep.Credit:
                if (ok)
                {
                    book.ReportDelivered(tx, leg, World!.Tick);
                }

                break;
            default:
                return CommandReply.Reject("unknown step");
        }

        return CommandReply.Ok;
    }

    /// <inheritdoc />
    protected override void OnTick() => Book()?.Advance(World!.Tick);

    private EscrowBook? Book() => World is null ? null : EscrowBook.Of(World);

    private static CommandReply Answer(EscrowReply? reply)
        => reply is null ? CommandReply.Reject("no escrow book") : reply.Value.Accepted ? CommandReply.Ok : CommandReply.Reject(reply.Value.Reason ?? "declined");
}

/// <summary>Installing escrow into a game's worlds, and the identity the coordinator acts as.</summary>
public static class Escrow
{
    /// <summary>The player id the coordinator acts as. Connections of the coordinator carry <c>Trust.Owner(scope).As(CoordinatorPlayer)</c>; nobody else should.</summary>
    public const uint CoordinatorPlayer = uint.MaxValue - 1;

    /// <summary>Registers the escrow entity types with a registry (the one every world of the game shares).</summary>
    public static void Register(EntityRegistry registry) => registry.Add(new EscrowDesk().TypeId, nameof(EscrowDesk), static () => new EscrowDesk());

    /// <summary>
    /// Adds the escrow book and the desk entity to an authority world. Call it right after building the world, in the same order on every
    /// node. Replica worlds need only the entity type registered (the book is hidden and never sent).
    /// </summary>
    /// <param name="world">An authority world.</param>
    /// <param name="handler">How the game debits and credits its own holdings.</param>
    public static EscrowBook Install(World world, IEscrowHandler handler)
    {
        var book = world.AddTable(new EscrowBook(world, handler));
        world.Add(new EscrowDesk { Coordinator = CoordinatorPlayer });
        return book;
    }

    /// <summary>The desk entity of a world (authority or replica), or null.</summary>
    public static EscrowDesk? DeskOf(World world)
    {
        var entities = world.Entities;
        for (var i = 0; i < entities.Count; i++)
        {
            if (entities[i] is EscrowDesk desk)
            {
                return desk;
            }
        }

        return null;
    }

    /// <summary>The trust a coordinator's node needs on a participant's or its own scope.</summary>
    public static Trust CoordinatorTrust(ScopeId scope) => Trust.Owner(scope).As(CoordinatorPlayer);
}
