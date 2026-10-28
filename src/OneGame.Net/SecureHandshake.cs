using System.Text;
using OneGame.Core;

namespace OneGame.Net;

/// <summary>
/// One Noise IK handshake (<c>Noise_IK_25519_ChaChaPoly_SHA256</c>) between a client that trusts a set of server keys and a server
/// that answers as one of its key pairs, shared by every transport. It moves bytes only: the transport frames the two messages and
/// decides what the optional payloads carry. Message 1 (client to server) is <c>e, es, s, ss</c>: the client's ephemeral key, then
/// its static key encrypted to the server. Message 2 (server to client) is <c>e, ee, se</c>. After both, <see cref="Finish"/> gives
/// the two directional ciphers and <see cref="Remote"/> is the peer's authenticated static key.
/// <para>
/// The prologue is <c>"onegame/2" 0x00 gameId 0x00 bind</c>. <c>bind</c> is whatever the transport wants the handshake tied to
/// (UDP: the datagram version, the client's nonce and the server's cookie, so a captured first message is useless with any other
/// cookie and a changed version byte breaks the handshake; WebSocket: the transport's own version and pattern byte). Both ends must pass the same bytes, or the handshake fails like a wrong key would.
/// </para>
/// </summary>
internal sealed class SecureHandshake
{
    private readonly NetSecurity _security;
    private readonly byte[] _prologue;
    private NoiseHandshake? _noise;
    private NetKeyPair? _identity;
    private PeerKey _serverKey;

    private SecureHandshake(NetSecurity security, ReadOnlySpan<byte> bind)
    {
        _security = security;
        var game = Encoding.UTF8.GetBytes(security.GameId);
        _prologue = new byte[9 + 1 + game.Length + 1 + bind.Length];
        Encoding.ASCII.GetBytes("onegame/2").CopyTo(_prologue, 0);
        game.CopyTo(_prologue, 10);
        bind.CopyTo(_prologue.AsSpan(10 + game.Length + 1));
    }

    /// <summary>Size of message 1 without its payload: 32 + 48 + 16.</summary>
    public const int FirstMessageOverhead = 96;

    /// <summary>Size of message 2 without its payload: 32 + 16.</summary>
    public const int ReplyOverhead = 48;

    /// <summary>The client side. <paramref name="keyIndex"/> picks which trusted server key this attempt uses (the caller cycles it after a rejection).</summary>
    public static SecureHandshake Client(NetSecurity security, int keyIndex, ReadOnlySpan<byte> bind)
    {
        var h = new SecureHandshake(security, bind);
        h._identity = security.Identity ?? NetKeyPair.Generate();
        h._serverKey = security.TrustedServerKeys[keyIndex % security.TrustedServerKeys.Count];
        return h;
    }

    /// <summary>The server side.</summary>
    public static SecureHandshake Server(NetSecurity security, ReadOnlySpan<byte> bind) => new(security, bind);

    /// <summary>The peer's authenticated static public key: the client's on a server, the server's on a client. Empty until known.</summary>
    public PeerKey Remote { get; private set; }

    /// <summary>On a server, the key pair the client chose; null before <see cref="ReadFirst"/> succeeds.</summary>
    public NetKeyPair? UsedServerKey { get; private set; }

    /// <summary>Client: writes message 1 and returns its length. The buffer needs <c>FirstMessageOverhead + payload.Length</c> bytes.</summary>
    public int WriteFirst(ReadOnlySpan<byte> payload, Span<byte> output)
    {
        _noise = new NoiseHandshake(NoisePattern.IK, true, _prologue, _identity!.Private, _serverKey.Bytes, _identity.PublicKey.Bytes);
        return _noise.WriteMessage(payload, output);
    }

    /// <summary>
    /// Server: reads message 1, trying each of the server's key pairs (a client picks the one it trusts, and the server cannot tell
    /// which from outside). False for anything that no key pair authenticates.
    /// </summary>
    public bool ReadFirst(ReadOnlySpan<byte> message, Span<byte> payload, out int payloadLength)
    {
        payloadLength = 0;
        foreach (var key in _security.ServerKeys)
        {
            var noise = new NoiseHandshake(NoisePattern.IK, false, _prologue, key.Private, default, key.PublicKey.Bytes);
            if (noise.ReadMessage(message, payload, out payloadLength))
            {
                _noise = noise;
                UsedServerKey = key;
                Remote = new PeerKey(noise.RemoteStatic);
                return true;
            }

            noise.Destroy();
        }

        return false;
    }

    /// <summary>Server: writes message 2 and returns its length.</summary>
    public int WriteReply(ReadOnlySpan<byte> payload, Span<byte> output) => _noise!.WriteMessage(payload, output);

    /// <summary>Client: reads message 2. False if it does not authenticate (the server does not hold the key we trusted, or the message was altered).</summary>
    public bool ReadReply(ReadOnlySpan<byte> message, Span<byte> payload, out int payloadLength)
    {
        // Read on a fork: a datagram that fails to authenticate (forged or damaged) must not spoil the handshake the real reply needs.
        var fork = _noise!.Fork();
        if (!fork.ReadMessage(message, payload, out payloadLength))
        {
            fork.Destroy();
            return false;
        }

        _noise.Destroy();
        _noise = fork;
        Remote = _serverKey;
        return true;
    }

    /// <summary>The ciphers for this side: what it sends with and what it receives with. Call once, after both messages.</summary>
    /// <param name="send">Sends packets; refuses any number at or past <paramref name="packetLimit"/>.</param>
    /// <param name="receive">Opens packets and owns the replay window.</param>
    /// <param name="packetLimit">The most packets one key may send.</param>
    public void Finish(ulong packetLimit, out DatagramSender send, out DatagramReceiver receive)
    {
        _noise!.Split(out var s, out var r);
        send = new DatagramSender(s, packetLimit);
        receive = new DatagramReceiver(r);
    }

    /// <summary>
    /// The 16-byte token a server proves a restart with (in the style of RFC 9000 §10.3): an HMAC of the connection id under a key
    /// derived from its primary private key, so it survives the process and nobody else can compute it.
    /// </summary>
    public static void ResetToken(NetKeyPair primary, uint connectionId, Span<byte> token)
    {
        Span<byte> prk = stackalloc byte[32];
        Span<byte> key = stackalloc byte[32];
        Hkdf.Extract(default, primary.Private, prk);
        Hkdf.Expand(prk, "onegame reset token"u8, key);
        Span<byte> id = stackalloc byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(id, connectionId);
        Span<byte> mac = stackalloc byte[32];
        HmacSha256.Hash(key, id, mac);
        mac[..token.Length].CopyTo(token);
    }
}
