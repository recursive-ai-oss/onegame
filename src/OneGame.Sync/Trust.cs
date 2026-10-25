namespace OneGame.Sync;

/// <summary>Names a scope: a unit of state with exactly one authority node.</summary>
/// <param name="Value">The id.</param>
public readonly record struct ScopeId(uint Value)
{
    /// <inheritdoc />
    public override string ToString() => "scope#" + Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}

/// <summary>A set of scopes a connection is allowed to touch: all of them, none, or a listed few.</summary>
public readonly struct ScopeSet
{
    private readonly ScopeId[]? _ids;

    private ScopeSet(bool all, ScopeId[]? ids)
    {
        IsAll = all;
        _ids = ids;
    }

    /// <summary>Every scope.</summary>
    public static ScopeSet All { get; } = new(true, null);

    /// <summary>No scope.</summary>
    public static ScopeSet None { get; } = new(false, null);

    /// <summary>True for the set of every scope.</summary>
    public bool IsAll { get; }

    /// <summary>Exactly the listed scopes.</summary>
    public static ScopeSet Of(params ScopeId[] ids) => new(false, (ScopeId[])ids.Clone());

    /// <summary>Whether <paramref name="scope"/> is in the set.</summary>
    public bool Contains(ScopeId scope)
    {
        if (IsAll)
        {
            return true;
        }

        if (_ids is null)
        {
            return false;
        }

        foreach (var id in _ids)
        {
            if (id == scope)
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>
/// What a connection may do and who it is. <see cref="Read"/> decides which scopes it may subscribe to, <see cref="Command"/> which it
/// may send commands to, and <see cref="Own"/> in which it may send <c>OwnerOnly</c> commands. <see cref="Who"/> is the player and team
/// it speaks for, which decides what owner- and team-visible data it receives and which entities it owns. The authority enforces all
/// of it on every subscription, command and frame, and it can change at any time (see <see cref="Connection.Trust"/>).
/// </summary>
/// <param name="Read">Scopes the peer may subscribe to.</param>
/// <param name="Command">Scopes the peer may send commands to.</param>
/// <param name="Own">Scopes in which the peer may send <c>OwnerOnly</c> commands for entities it owns. Without it those commands are refused, even for the owner.</param>
/// <param name="Who">The player and team the peer speaks for; <see cref="Audience.Anonymous"/> by default.</param>
public readonly record struct Trust(ScopeSet Read, ScopeSet Command, ScopeSet Own, Audience Who = default)
{
    /// <summary>The same trust, speaking for a player and a team: they decide which owner- and team-visible data the peer receives.</summary>
    /// <param name="player">The player id; 0 for none.</param>
    /// <param name="team">The team id; 0 for none.</param>
    public Trust As(uint player, uint team = 0) => this with { Who = new Audience(player, team) };

    /// <summary>May do nothing.</summary>
    public static Trust None => new(ScopeSet.None, ScopeSet.None, ScopeSet.None);

    /// <summary>May read, command and own everything. For the local player of a single-player game and for tests.</summary>
    public static Trust Full => new(ScopeSet.All, ScopeSet.All, ScopeSet.All);

    /// <summary>May read the listed scopes and nothing else.</summary>
    public static Trust Reader(params ScopeId[] scopes) => new(ScopeSet.Of(scopes), ScopeSet.None, ScopeSet.None);

    /// <summary>May read and command the listed scopes.</summary>
    public static Trust Player(params ScopeId[] scopes) => new(ScopeSet.Of(scopes), ScopeSet.Of(scopes), ScopeSet.None);

    /// <summary>May read, command and own the listed scopes (and so send <c>OwnerOnly</c> commands there for entities it owns).</summary>
    public static Trust Owner(params ScopeId[] scopes) => new(ScopeSet.Of(scopes), ScopeSet.Of(scopes), ScopeSet.Of(scopes));
}

/// <summary>Why a command or subscription was refused.</summary>
public enum RefusalReason : byte
{
    /// <summary>The connection's trust does not allow commands to the scope.</summary>
    NotTrustedToCommand = 1,

    /// <summary>The connection's trust does not allow reading the scope.</summary>
    NotTrustedToRead,

    /// <summary>The authority node has no such scope.</summary>
    UnknownScope,

    /// <summary>The command bytes were not a valid command.</summary>
    Malformed,

    /// <summary>The command is <c>OwnerOnly</c> and the connection's trust does not own the scope.</summary>
    NotTrustedToOwn,

    /// <summary>The command is <c>OwnerOnly</c> and the connection's player is not the target entity's owner.</summary>
    NotOwner,

    /// <summary>The connection's trust no longer allows reading the scope, so the authority stopped sending it.</summary>
    ReadRevoked,
}

/// <summary>How a command sent from a replica ended.</summary>
public enum CommandVerdict : byte
{
    /// <summary>The authority ran the handler and it accepted.</summary>
    Executed = 0,

    /// <summary>The handler rejected the command; <see cref="CommandResult.Reason"/> says why when it gave a reason.</summary>
    Rejected,

    /// <summary>The authority refused the command before running it; <see cref="CommandResult.Refusal"/> says why.</summary>
    Refused,

    /// <summary>The target entity does not exist on the authority.</summary>
    UnknownEntity,

    /// <summary>The entity has no such command.</summary>
    UnknownCommand,

    /// <summary>The arguments were not valid for the command.</summary>
    Malformed,

    /// <summary>The authority never ran the command (its state was replaced by a load, or the replica stopped waiting).</summary>
    Dropped,
}

/// <summary>The answer to one command a replica sent, matched to the send by <see cref="Sequence"/>.</summary>
/// <param name="Scope">The scope.</param>
/// <param name="Sequence">The number <see cref="ReplicaScope.LastSequence"/> returned right after the send.</param>
/// <param name="Entity">The target entity.</param>
/// <param name="CommandId">The command id.</param>
/// <param name="Verdict">How it ended.</param>
/// <param name="Refusal">Why it was refused, when <paramref name="Verdict"/> is <see cref="CommandVerdict.Refused"/>.</param>
/// <param name="Reason">The handler's reason for rejecting, or null.</param>
public readonly record struct CommandResult(ScopeId Scope, uint Sequence, EntityId Entity, uint CommandId, CommandVerdict Verdict, RefusalReason Refusal, string? Reason);

/// <summary>What <c>Connection.SubmitCommand</c> did with a command.</summary>
public enum SubmitResult
{
    /// <summary>Accepted; it runs at the start of the scope's next tick.</summary>
    Queued = 0,

    /// <summary>The connection's trust does not allow commanding the scope; a <see cref="CommandRefusal"/> was filed.</summary>
    NotTrusted,

    /// <summary>The node has no authority scope with that id.</summary>
    UnknownScope,

    /// <summary>The bytes were too large to be a command.</summary>
    Malformed,
}

/// <summary>A command or subscription that an authority refused, or that this node was told was refused.</summary>
/// <param name="Scope">The scope concerned.</param>
/// <param name="Connection">Id of the connection it came from (on the authority) or went over (on the submitter).</param>
/// <param name="Entity">The target entity (zero for subscriptions).</param>
/// <param name="CommandId">The command id (zero for subscriptions).</param>
/// <param name="Reason">Why.</param>
public readonly record struct CommandRefusal(ScopeId Scope, int Connection, EntityId Entity, uint CommandId, RefusalReason Reason);

/// <summary>Cluster-wide behavior switches.</summary>
public sealed class ClusterOptions
{
    /// <summary>Ticks between hash comparisons of authority and replicas; 0 turns drift detection off. On by default.</summary>
    public int DriftCheckInterval { get; init; } = 30;

    /// <summary>When true, <see cref="Cluster.Step"/> throws <see cref="DriftException"/> as soon as a drift report is filed. Tests turn it on.</summary>
    public bool FatalDrift { get; init; }

    /// <summary>When true, a replica that found drift re-subscribes and takes a fresh snapshot.</summary>
    public bool ResyncOnDrift { get; init; } = true;

    /// <summary>Authority ticks a replica waits for the answer to a digest request before it reports the drift without detail.</summary>
    public int DigestTimeoutTicks { get; init; } = 60;

    /// <summary>Most entities a drift digest lists; above it the digest holds the scope hash only.</summary>
    public int DigestEntityLimit { get; init; } = 100_000;
}
