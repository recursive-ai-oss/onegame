using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using OneGame.Core;

namespace OneGame.Net;

/// <summary>Constants and helpers for the datagram formats. See docs/interfaces/transport.md.</summary>
internal static class Wire
{
    /// <summary>Protocol version, in the high nibble of every datagram's first byte. The low nibble is the kind.</summary>
    public const byte Version = 1;

    /// <summary>Version nibble of secure datagrams (encrypted and authenticated).</summary>
    public const byte VersionSecure = 2;

    public const byte KindRequest = 0;
    public const byte KindChallenge = 1;
    public const byte KindResponse = 2;
    public const byte KindAccept = 3;
    public const byte KindReject = 4;
    public const byte KindData = 5;
    public const byte KindDisconnect = 6;

    /// <summary>A server that does not know a connection proves it restarted: <c>b0, connId u32, token[16]</c>.</summary>
    public const byte KindReset = 7;

    /// <summary>Client handshake datagrams are padded to at least this size, so a reply is always smaller than the request that caused it.</summary>
    public const int MinHandshakeSize = 256;

    public const int CookieSize = 16;
    public const int ChallengeSize = 1 + 8 + CookieSize;
    public const int AcceptSize = 1 + 8 + 4;
    public const int RejectSize = 1 + 8 + 1;
    public const int DisconnectSize = 1 + 4 + 1 + 1;

    public static byte Header(byte kind) => (byte)((Version << 4) | kind);

    public static byte Header(byte kind, bool secure) => (byte)(((secure ? VersionSecure : Version) << 4) | kind);

    /// <summary>Authentication tag added to every secure datagram body.</summary>
    public const int TagSize = 16;

    /// <summary>Bytes of a secure data datagram that stay in the clear and are authenticated: kind, connection id, packet number.</summary>
    public const int SecureAadSize = 9;

    /// <summary>Counter used for the authenticated disconnect; no packet number can reach it.</summary>
    public const ulong DisconnectNonce = 1UL << 32;

    /// <summary>Secure Response: kind, nonce, cookie, noise length.</summary>
    public const int SecureResponseHeader = 1 + 8 + CookieSize + 2;

    /// <summary>Secure Accept: kind, nonce, connection id, noise length.</summary>
    public const int SecureAcceptHeader = 1 + 8 + 4 + 2;

    public const int ResetSize = 1 + 4 + 16;

    /// <summary>The handshake binding: the client nonce and the cookie, so a captured first message only works with the cookie it was made for.</summary>
    public static void Bind(ulong nonce, ReadOnlySpan<byte> cookie, Span<byte> bind)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(bind, nonce);
        cookie[..CookieSize].CopyTo(bind[8..]);
    }

    /// <summary>Copies a socket address so it can be kept.</summary>
    public static SocketAddress Clone(SocketAddress a)
    {
        var copy = new SocketAddress(a.Family, a.Size);
        a.Buffer.Span.CopyTo(copy.Buffer.Span);
        return copy;
    }

    /// <summary>The cookie a client must echo: a keyed hash of its address, its nonce and a ten-second epoch, so the server stores nothing.</summary>
    public static void Cookie(ReadOnlySpan<byte> secret, SocketAddress from, ulong nonce, long epoch, Span<byte> cookie)
    {
        Span<byte> input = stackalloc byte[64];
        var addr = from.Buffer.Span;
        var n = Math.Min(addr.Length, 40);
        addr[..n].CopyTo(input);
        BinaryPrimitives.WriteUInt64LittleEndian(input[n..], nonce);
        BinaryPrimitives.WriteInt64LittleEndian(input[(n + 8)..], epoch);
        Span<byte> mac = stackalloc byte[32];
        HmacSha256.Hash(secret, input[..(n + 16)], mac);
        mac[..CookieSize].CopyTo(cookie);
    }

    public static string Describe(SocketAddress a) => a.Family is AddressFamily.InterNetwork or AddressFamily.InterNetworkV6 ? new IPEndPoint(IPAddress.Any, 0).Create(a).ToString() ?? "?" : a.ToString();
}
