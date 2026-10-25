namespace OneGame.Core;

/// <summary>
/// Who a subscriber is, for visibility: the player it speaks for and the team it plays on. The authority matches it against the
/// <see cref="OwnerKeyAttribute"/> and <see cref="TeamKeyAttribute"/> values of each entity to decide what to send. Zero means
/// "nobody" and never matches, so <see cref="Anonymous"/> sees only what is visible to everyone.
/// </summary>
/// <param name="Player">The player id the connection owns entities as; 0 for none.</param>
/// <param name="Team">The team id; 0 for none.</param>
public readonly record struct Audience(uint Player, uint Team = 0)
{
    /// <summary>No player and no team: sees only what everyone sees.</summary>
    public static Audience Anonymous => default;

    /// <summary>True when this audience is the owner for an entity whose owner key is <paramref name="ownerKey"/>.</summary>
    public bool IsOwnerOf(ulong ownerKey) => Player != 0 && Player == ownerKey;

    /// <summary>True when this audience is the owner of, or on the same team as, an entity with these keys.</summary>
    public bool IsTeamOf(ulong ownerKey, ulong teamKey) => IsOwnerOf(ownerKey) || (Team != 0 && Team == teamKey);
}

/// <summary>
/// What a command handler answers: accepted, or rejected with a reason that travels back to whoever sent the command. A handler
/// that returns <see cref="bool"/> converts to this, with no reason.
/// </summary>
public readonly struct CommandReply
{
    private CommandReply(bool accepted, string? reason)
    {
        Accepted = accepted;
        Reason = reason;
    }

    /// <summary>True when the handler accepted the command.</summary>
    public bool Accepted { get; }

    /// <summary>Why the handler rejected the command, or null. Not part of the synced state; only the sender sees it.</summary>
    public string? Reason { get; }

    /// <summary>The command was accepted.</summary>
    public static CommandReply Ok => new(true, null);

    /// <summary>The command is refused; <paramref name="reason"/> is sent to the submitter (long text is cut at 200 characters).</summary>
    public static CommandReply Reject(string reason) => new(false, reason);

    /// <summary><c>true</c> accepts and <c>false</c> rejects without a reason.</summary>
    public static implicit operator CommandReply(bool accepted) => new(accepted, null);
}
