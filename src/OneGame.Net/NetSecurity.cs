using OneGame.Core;

namespace OneGame.Net;

/// <summary>Whether a transport encrypts and authenticates its links.</summary>
public enum SecurityMode
{
    /// <summary>Every link runs a Noise IK handshake and is encrypted and authenticated. The default, and the only mode for a game that is played over a network.</summary>
    Required = 0,

    /// <summary>
    /// Plaintext, unauthenticated links as in earlier releases. For tests, loopback tools and local development. A peer in
    /// <see cref="Required"/> mode refuses to talk to it, and the other way round: there is no negotiation and no fallback.
    /// </summary>
    Insecure = 1,
}

/// <summary>A transport was opened without saying how its links are secured, or with keys that do not fit what it was asked to do.</summary>
public sealed class SecurityNotConfiguredException : InvalidOperationException
{
    /// <summary>Creates the exception.</summary>
    /// <param name="message">What is missing and how to supply it.</param>
    public SecurityNotConfiguredException(string message) : base(message)
    {
    }
}

/// <summary>
/// How a transport secures its links, passed as <c>UdpOptions.Security</c> or <c>WebSocketOptions.Security</c>. A transport opened
/// without one throws <see cref="SecurityNotConfiguredException"/>: there is no silent plaintext default.
/// <list type="bullet">
/// <item><description><b>Server:</b> <see cref="ForServer"/> with the server's key pair. Clients pin its <see cref="NetKeyPair.PublicKey"/>.</description></item>
/// <item><description><b>Client:</b> <see cref="ForClient"/> with the server public keys it trusts (a set, so a server can rotate keys) and optionally a key pair that names this player.</description></item>
/// <item><description><b>Tests and local tools:</b> <see cref="Insecure"/>.</description></item>
/// </list>
/// The handshake is Noise IK (<c>Noise_IK_25519_ChaChaPoly_SHA256</c>): the client already knows the server's key, so it is one
/// round trip, and the client's key is sent encrypted. See <c>docs/interfaces/transport-security.md</c>.
/// </summary>
public sealed class NetSecurity
{
    private static readonly NetSecurity InsecureValue = new() { Mode = SecurityMode.Insecure };

    /// <summary>Plaintext links, for tests and local tools. Search for it to find every place a game opts out.</summary>
    public static NetSecurity Insecure => InsecureValue;

    /// <summary>Required or Insecure.</summary>
    public SecurityMode Mode { get; init; } = SecurityMode.Required;

    /// <summary>
    /// Names the game and version of the protocol the peers speak. It is mixed into the handshake, so peers with different values
    /// cannot complete one; use the same text on both ends (for example <c>"my-game/3"</c>). Default: empty.
    /// </summary>
    public string GameId { get; init; } = "";

    /// <summary>
    /// Server (listener) side: the key pairs this server answers as. The first is its primary key. More than one lets a server
    /// rotate: add the new pair, ship clients the new public key, and remove the old pair later.
    /// </summary>
    public IReadOnlyList<NetKeyPair> ServerKeys { get; init; } = [];

    /// <summary>
    /// Client (connecting) side: the server public keys this client accepts, in order of preference. A connection attempt starts with
    /// the first and tries the next after a rejection, so a rotation can overlap.
    /// </summary>
    public IReadOnlyList<PeerKey> TrustedServerKeys { get; init; } = [];

    /// <summary>
    /// Client side: the key pair that names this player. The server sees its public key after the handshake and can map it to a
    /// player (<see cref="OneGame.Sync.PlayerRegistry"/>), which is how a player stays the same across reconnects. Keep it in the
    /// game's save storage. If null, each connecting transport makes a fresh key pair, so identity lasts one connection object.
    /// </summary>
    public NetKeyPair? Identity { get; init; }

    /// <summary>
    /// Client side: when true (the default of <see cref="ForClient"/>), the key a server sees is derived from <see cref="Identity"/> and
    /// that server's public key (<see cref="NetKeyPair.DeriveFor"/>), so two servers cannot tell they are talking to the same player by
    /// comparing keys. Server-to-server links turn it off, because peers name each other by their real keys.
    /// </summary>
    public bool PerServerIdentity { get; init; }

    /// <summary>The key a server with public key <paramref name="serverKey"/> sees for this client: what that server's player registry is keyed by. Empty when there is no <see cref="Identity"/> yet.</summary>
    /// <param name="serverKey">The server's public key.</param>
    public PeerKey IdentityKeyFor(PeerKey serverKey) =>
        Identity is null ? default : PerServerIdentity ? Identity.DeriveFor(serverKey).PublicKey : Identity.PublicKey;

    /// <summary>Settings for a listening server.</summary>
    /// <param name="key">The server's key pair (its private half is a secret).</param>
    /// <param name="gameId">The game and protocol name both ends use.</param>
    /// <param name="more">Further key pairs the server also answers as, for rotation.</param>
    public static NetSecurity ForServer(NetKeyPair key, string gameId = "", params NetKeyPair[] more)
    {
        ArgumentNullException.ThrowIfNull(key);
        return new NetSecurity { GameId = gameId, ServerKeys = [key, .. more] };
    }

    /// <summary>Settings for a connecting client.</summary>
    /// <param name="serverKeys">The server public keys this client trusts, preferred first.</param>
    /// <param name="gameId">The game and protocol name both ends use.</param>
    /// <param name="identity">This player's key pair, kept between runs; null for a fresh one per connection.</param>
    public static NetSecurity ForClient(IEnumerable<PeerKey> serverKeys, string gameId = "", NetKeyPair? identity = null)
    {
        ArgumentNullException.ThrowIfNull(serverKeys);
        return new NetSecurity { GameId = gameId, TrustedServerKeys = [.. serverKeys], Identity = identity, PerServerIdentity = true };
    }

    /// <summary>The same settings for a server that also connects to other servers (a game's own servers talking to each other).</summary>
    /// <param name="trusted">Public keys of the servers this one connects to.</param>
    public NetSecurity AlsoTrusting(IEnumerable<PeerKey> trusted) => new() { Mode = Mode, GameId = GameId, ServerKeys = ServerKeys, Identity = Identity, PerServerIdentity = PerServerIdentity, TrustedServerKeys = [.. TrustedServerKeys, .. trusted] };

    internal static NetSecurity RequireForListening(NetSecurity? security, string owner)
    {
        if (security is null)
        {
            throw new SecurityNotConfiguredException($"{owner}.Security is not set. Pass NetSecurity.ForServer(key, gameId) to encrypt links (make a key with 'onegame keys' or NetKeyPair.Generate()), or NetSecurity.Insecure for tests and local tools.");
        }

        if (security.Mode == SecurityMode.Required && security.ServerKeys.Count == 0)
        {
            throw new SecurityNotConfiguredException($"{owner}.Security has no server key. A listening server needs NetSecurity.ForServer(key, gameId).");
        }

        return security;
    }

    internal static NetSecurity RequireForConnecting(NetSecurity? security, string owner)
    {
        if (security is null)
        {
            throw new SecurityNotConfiguredException($"{owner}.Security is not set. Pass NetSecurity.ForClient(serverPublicKeys, gameId) to encrypt links, or NetSecurity.Insecure for tests and local tools.");
        }

        if (security.Mode == SecurityMode.Required && security.TrustedServerKeys.Count == 0)
        {
            throw new SecurityNotConfiguredException($"{owner}.Security trusts no server key. A client needs the server's public key: NetSecurity.ForClient(serverPublicKeys, gameId).");
        }

        return security;
    }
}
