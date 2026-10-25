using System.Buffers.Text;
using OneGame.Core;
using System.Text;

namespace OneGame.Net;

/// <summary>The HTTP opening handshake of RFC 6455 §4, parsed and written without allocating.</summary>
internal static class WsHandshake
{
    /// <summary>RFC 6455 §1.3: the fixed string appended to the client's key before hashing.</summary>
    private static ReadOnlySpan<byte> Guid => "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"u8;

    private static ReadOnlySpan<byte> Crlf => "\r\n"u8;

    public enum RequestResult : byte
    {
        Ok,
        BadRequest,
        UpgradeRequired,
        Forbidden,
    }

    /// <summary>Index just past the blank line that ends the header block, or -1 if it has not arrived.</summary>
    public static int FindEnd(ReadOnlySpan<byte> data)
    {
        var i = data.IndexOf("\r\n\r\n"u8);
        return i < 0 ? -1 : i + 4;
    }

    /// <summary>Sec-WebSocket-Accept for a key (§4.2.2 step 5.4): base64(SHA-1(key + GUID)).</summary>
    public static void ComputeAccept(ReadOnlySpan<byte> key, Span<byte> accept28)
    {
        Span<byte> input = stackalloc byte[24 + 36];
        key[..24].CopyTo(input);
        Guid.CopyTo(input[24..]);
        Span<byte> hash = stackalloc byte[20];
        Sha1.Hash(input, hash);
        _ = Base64.EncodeToUtf8(hash, accept28, out _, out _);
    }

    /// <summary>A fresh random key as 24 base64 characters (§4.1: a 16-byte nonce).</summary>
    public static void NewKey(Span<byte> key24)
    {
        Span<byte> nonce = stackalloc byte[16];
        Entropy.Fill(nonce);
        _ = Base64.EncodeToUtf8(nonce, key24, out _, out _);
    }

    public static int WriteRequest(Span<byte> dst, string path, string host, ReadOnlySpan<byte> key24)
    {
        var pos = 0;
        Append(dst, ref pos, "GET "u8);
        pos += Encoding.ASCII.GetBytes(path, dst[pos..]);
        Append(dst, ref pos, " HTTP/1.1\r\nHost: "u8);
        pos += Encoding.ASCII.GetBytes(host, dst[pos..]);
        Append(dst, ref pos, "\r\nUpgrade: websocket\r\nConnection: Upgrade\r\nSec-WebSocket-Key: "u8);
        Append(dst, ref pos, key24);
        Append(dst, ref pos, "\r\nSec-WebSocket-Version: 13\r\n\r\n"u8);
        return pos;
    }

    public static int WriteAccept(Span<byte> dst, ReadOnlySpan<byte> key24)
    {
        var pos = 0;
        Append(dst, ref pos, "HTTP/1.1 101 Switching Protocols\r\nUpgrade: websocket\r\nConnection: Upgrade\r\nSec-WebSocket-Accept: "u8);
        ComputeAccept(key24, dst.Slice(pos, 28));
        pos += 28;
        Append(dst, ref pos, "\r\n\r\n"u8);
        return pos;
    }

    public static int WriteRefusal(Span<byte> dst, RequestResult why)
    {
        var pos = 0;
        switch (why)
        {
            case RequestResult.UpgradeRequired:
                Append(dst, ref pos, "HTTP/1.1 426 Upgrade Required\r\nSec-WebSocket-Version: 13\r\nConnection: close\r\nContent-Length: 0\r\n\r\n"u8);
                break;
            case RequestResult.Forbidden:
                Append(dst, ref pos, "HTTP/1.1 403 Forbidden\r\nConnection: close\r\nContent-Length: 0\r\n\r\n"u8);
                break;
            default:
                Append(dst, ref pos, "HTTP/1.1 400 Bad Request\r\nConnection: close\r\nContent-Length: 0\r\n\r\n"u8);
                break;
        }

        return pos;
    }

    /// <summary>Checks a client's upgrade request (§4.2.1). <paramref name="head"/> is the header block including the blank line.</summary>
    public static RequestResult ParseRequest(ReadOnlySpan<byte> head, WebSocketOptions options, Span<byte> key24)
    {
        var rest = head;
        if (!NextLine(ref rest, out var line))
        {
            return RequestResult.BadRequest;
        }

        // Request line: GET <target> HTTP/1.1
        var sp1 = line.IndexOf((byte)' ');
        if (sp1 <= 0 || !line[..sp1].SequenceEqual("GET"u8))
        {
            return RequestResult.BadRequest;
        }

        var afterMethod = line[(sp1 + 1)..];
        var sp2 = afterMethod.IndexOf((byte)' ');
        if (sp2 <= 0 || !afterMethod[(sp2 + 1)..].SequenceEqual("HTTP/1.1"u8))
        {
            return RequestResult.BadRequest;
        }

        bool host = false, upgrade = false, connection = false, key = false, version = false, versionOther = false;
        string? origin = null;
        Span<byte> probe = stackalloc byte[24];
        var originSeen = false;
        while (NextLine(ref rest, out line))
        {
            if (line.Length == 0)
            {
                break;
            }

            if (!SplitHeader(line, out var name, out var value))
            {
                return RequestResult.BadRequest;
            }

            if (Ascii.EqualsIgnoreCase(name, "host"u8))
            {
                host = true;
            }
            else if (Ascii.EqualsIgnoreCase(name, "upgrade"u8))
            {
                upgrade |= HasToken(value, "websocket"u8);
            }
            else if (Ascii.EqualsIgnoreCase(name, "connection"u8))
            {
                connection |= HasToken(value, "upgrade"u8);
            }
            else if (Ascii.EqualsIgnoreCase(name, "sec-websocket-key"u8))
            {
                if (key || value.Length != 24)
                {
                    return RequestResult.BadRequest;
                }

                if (Base64.DecodeFromUtf8(value, probe, out _, out var written) != System.Buffers.OperationStatus.Done || written != 16)
                {
                    return RequestResult.BadRequest;
                }

                value.CopyTo(key24);
                key = true;
            }
            else if (Ascii.EqualsIgnoreCase(name, "sec-websocket-version"u8))
            {
                if (HasToken(value, "13"u8))
                {
                    version = true;
                }
                else
                {
                    versionOther = true;
                }
            }
            else if (Ascii.EqualsIgnoreCase(name, "origin"u8))
            {
                originSeen = true;
                origin = options.AllowedOrigins is null ? null : Encoding.ASCII.GetString(value);
            }
        }

        if (!host || !upgrade || !connection || !key)
        {
            return RequestResult.BadRequest;
        }

        if (!version)
        {
            return versionOther ? RequestResult.UpgradeRequired : RequestResult.BadRequest;
        }

        if (options.AllowedOrigins is not null && originSeen)
        {
            var allowed = false;
            foreach (var o in options.AllowedOrigins)
            {
                allowed |= string.Equals(o, origin, StringComparison.OrdinalIgnoreCase);
            }

            if (!allowed)
            {
                return RequestResult.Forbidden;
            }
        }

        return RequestResult.Ok;
    }

    /// <summary>Checks the server's reply (§4.2.2 / §4.1 step 2): status 101, the right upgrade headers, the right accept hash, no extension or subprotocol we did not ask for.</summary>
    public static bool ParseResponse(ReadOnlySpan<byte> head, ReadOnlySpan<byte> key24)
    {
        var rest = head;
        if (!NextLine(ref rest, out var line) || !line.StartsWith("HTTP/1.1 101"u8) || (line.Length > 12 && line[12] != (byte)' '))
        {
            return false;
        }

        Span<byte> expected = stackalloc byte[28];
        ComputeAccept(key24, expected);
        bool upgrade = false, connection = false, accept = false;
        while (NextLine(ref rest, out line))
        {
            if (line.Length == 0)
            {
                break;
            }

            if (!SplitHeader(line, out var name, out var value))
            {
                return false;
            }

            if (Ascii.EqualsIgnoreCase(name, "upgrade"u8))
            {
                upgrade |= HasToken(value, "websocket"u8);
            }
            else if (Ascii.EqualsIgnoreCase(name, "connection"u8))
            {
                connection |= HasToken(value, "upgrade"u8);
            }
            else if (Ascii.EqualsIgnoreCase(name, "sec-websocket-accept"u8))
            {
                if (accept)
                {
                    return false;
                }

                accept = value.SequenceEqual(expected);
                if (!accept)
                {
                    return false;
                }
            }
            else if (Ascii.EqualsIgnoreCase(name, "sec-websocket-extensions"u8) || Ascii.EqualsIgnoreCase(name, "sec-websocket-protocol"u8))
            {
                return false; // we offered none
            }
        }

        return upgrade && connection && accept;
    }

    // CRLF-terminated lines only; a bare LF is not a line break in HTTP/1.1 messages we accept.
    private static bool NextLine(ref ReadOnlySpan<byte> rest, out ReadOnlySpan<byte> line)
    {
        var i = rest.IndexOf(Crlf);
        if (i < 0)
        {
            line = default;
            return false;
        }

        line = rest[..i];
        rest = rest[(i + 2)..];
        return true;
    }

    private static bool SplitHeader(ReadOnlySpan<byte> line, out ReadOnlySpan<byte> name, out ReadOnlySpan<byte> value)
    {
        var colon = line.IndexOf((byte)':');
        name = default;
        value = default;
        if (colon <= 0)
        {
            return false;
        }

        name = line[..colon];
        foreach (var b in name)
        {
            if (b <= 0x20 || b >= 0x7F)
            {
                return false; // no whitespace or control bytes in a header name (RFC 9112 §5.1)
            }
        }

        value = Trim(line[(colon + 1)..]);
        return true;
    }

    private static ReadOnlySpan<byte> Trim(ReadOnlySpan<byte> s)
    {
        while (s.Length > 0 && (s[0] == (byte)' ' || s[0] == (byte)'\t'))
        {
            s = s[1..];
        }

        while (s.Length > 0 && (s[^1] == (byte)' ' || s[^1] == (byte)'\t'))
        {
            s = s[..^1];
        }

        return s;
    }

    // A comma-separated header value contains a token, compared ignoring case.
    private static bool HasToken(ReadOnlySpan<byte> value, ReadOnlySpan<byte> token)
    {
        while (true)
        {
            var comma = value.IndexOf((byte)',');
            var item = Trim(comma < 0 ? value : value[..comma]);
            if (Ascii.EqualsIgnoreCase(item, token))
            {
                return true;
            }

            if (comma < 0)
            {
                return false;
            }

            value = value[(comma + 1)..];
        }
    }

    private static void Append(Span<byte> dst, ref int pos, ReadOnlySpan<byte> s)
    {
        s.CopyTo(dst[pos..]);
        pos += s.Length;
    }
}
