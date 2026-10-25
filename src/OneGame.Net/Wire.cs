using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace OneGame.Net;

/// <summary>Constants and helpers for the datagram formats. See docs/interfaces/transport.md.</summary>
internal static class Wire
{
    /// <summary>Protocol version, in the high nibble of every datagram's first byte. The low nibble is the kind.</summary>
    public const byte Version = 1;

    public const byte KindRequest = 0;
    public const byte KindChallenge = 1;
    public const byte KindResponse = 2;
    public const byte KindAccept = 3;
    public const byte KindReject = 4;
    public const byte KindData = 5;
    public const byte KindDisconnect = 6;

    /// <summary>Client handshake datagrams are padded to at least this size, so a reply is always smaller than the request that caused it.</summary>
    public const int MinHandshakeSize = 256;

    public const int CookieSize = 16;
    public const int ChallengeSize = 1 + 8 + CookieSize;
    public const int AcceptSize = 1 + 8 + 4;
    public const int RejectSize = 1 + 8 + 1;
    public const int DisconnectSize = 1 + 4 + 1 + 1;

    public static byte Header(byte kind) => (byte)((Version << 4) | kind);

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
        _ = HMACSHA256.HashData(secret, input[..(n + 16)], mac);
        mac[..CookieSize].CopyTo(cookie);
    }

    public static string Describe(SocketAddress a) => a.Family is AddressFamily.InterNetwork or AddressFamily.InterNetworkV6 ? new IPEndPoint(IPAddress.Any, 0).Create(a).ToString() ?? "?" : a.ToString();
}
