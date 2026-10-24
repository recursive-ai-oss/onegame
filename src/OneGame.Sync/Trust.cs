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

/// <summary>What a connection may do: read scopes (subscribe), command them, and own them (be their authority). The authority enforces it on every command and subscription.</summary>
/// <param name="Read">Scopes the peer may subscribe to.</param>
/// <param name="Command">Scopes the peer may send commands to.</param>
/// <param name="Own">Scopes the peer may act as authority for. Reserved for authority transfer; nothing checks it yet.</param>
public readonly record struct Trust(ScopeSet Read, ScopeSet Command, ScopeSet Own)
{
    /// <summary>May do nothing.</summary>
    public static Trust None => new(ScopeSet.None, ScopeSet.None, ScopeSet.None);

    /// <summary>May read, command and own everything. For the local player of a single-player game and for tests.</summary>
    public static Trust Full => new(ScopeSet.All, ScopeSet.All, ScopeSet.All);

    /// <summary>May read the listed scopes and nothing else.</summary>
    public static Trust Reader(params ScopeId[] scopes) => new(ScopeSet.Of(scopes), ScopeSet.None, ScopeSet.None);

    /// <summary>May read and command the listed scopes.</summary>
    public static Trust Player(params ScopeId[] scopes) => new(ScopeSet.Of(scopes), ScopeSet.Of(scopes), ScopeSet.None);
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
}

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

    /// <summary>Most entities a drift digest lists; above it the digest holds the scope hash only.</summary>
    public int DigestEntityLimit { get; init; } = 100_000;
}
