using System.Buffers;
using System.Buffers.Binary;
using System.Text.Unicode;

namespace OneGame.Net;

/// <summary>What <see cref="WsFrameReader.Feed"/> found.</summary>
internal enum WsEvent : byte
{
    /// <summary>All input is used; more is needed.</summary>
    NeedMore,

    /// <summary>A whole data message is ready: <see cref="WsFrameReader.TakeMessage"/>.</summary>
    Message,

    /// <summary>A ping arrived; <see cref="WsFrameReader.ControlPayload"/> must be echoed in a pong.</summary>
    Ping,

    /// <summary>A pong arrived.</summary>
    Pong,

    /// <summary>A valid close frame arrived; see <see cref="WsFrameReader.CloseCode"/>.</summary>
    Close,

    /// <summary>The stream broke the protocol; <see cref="WsFrameReader.ErrorCode"/> is the close code to answer with.</summary>
    Error,
}

/// <summary>Constants and writers for RFC 6455 frames.</summary>
internal static class WsFrames
{
    public const byte OpContinuation = 0x0;
    public const byte OpText = 0x1;
    public const byte OpBinary = 0x2;
    public const byte OpClose = 0x8;
    public const byte OpPing = 0x9;
    public const byte OpPong = 0xA;

    public const int MaxControlPayload = 125;
    public const int MaxHeaderBytes = 14;

    /// <summary>Close codes from RFC 6455 §7.4.1.</summary>
    public const int CodeNormal = 1000;

    public const int CodeGoingAway = 1001;
    public const int CodeProtocolError = 1002;
    public const int CodeUnsupportedData = 1003;
    public const int CodeInvalidPayload = 1007;
    public const int CodePolicy = 1008;
    public const int CodeTooBig = 1009;
    public const int CodeInternal = 1011;

    /// <summary>Writes one frame and returns its length. With <paramref name="maskKey"/> (4 bytes) the payload is masked as a client must (§5.3).</summary>
    public static int Write(Span<byte> dst, byte opcode, bool fin, ReadOnlySpan<byte> payload, ReadOnlySpan<byte> maskKey)
    {
        var pos = 0;
        dst[pos++] = (byte)((fin ? 0x80 : 0) | opcode);
        var mask = maskKey.Length == 4 ? 0x80 : 0;
        if (payload.Length <= 125)
        {
            dst[pos++] = (byte)(mask | payload.Length);
        }
        else if (payload.Length <= ushort.MaxValue)
        {
            dst[pos++] = (byte)(mask | 126);
            BinaryPrimitives.WriteUInt16BigEndian(dst[pos..], (ushort)payload.Length);
            pos += 2;
        }
        else
        {
            dst[pos++] = (byte)(mask | 127);
            BinaryPrimitives.WriteUInt64BigEndian(dst[pos..], (ulong)payload.Length);
            pos += 8;
        }

        if (mask != 0)
        {
            maskKey.CopyTo(dst[pos..]);
            pos += 4;
            for (var i = 0; i < payload.Length; i++)
            {
                dst[pos + i] = (byte)(payload[i] ^ maskKey[i & 3]);
            }
        }
        else
        {
            payload.CopyTo(dst[pos..]);
        }

        return pos + payload.Length;
    }

    /// <summary>True for close codes that may appear on the wire (§7.4.1, §7.4.2).</summary>
    public static bool ValidCloseCode(int code) => code is (>= 1000 and <= 1003) or (>= 1007 and <= 1014) or (>= 3000 and <= 4999);
}

/// <summary>
/// An incremental parser for the frames of one WebSocket stream (RFC 6455 §5). It checks everything the RFC says a receiver must:
/// reserved bits, opcodes, control frame size and fragmentation, mask direction, minimal length encoding, continuation order,
/// close payloads and UTF-8 in close reasons. The size of a message is checked against the limit when its frame header arrives,
/// before any payload byte is stored.
/// </summary>
internal sealed class WsFrameReader
{
    private readonly bool _requireMask;
    private readonly int _maxMessage;
    private readonly byte[] _hdr = new byte[WsFrames.MaxHeaderBytes];
    private readonly byte[] _ctl = new byte[WsFrames.MaxControlPayload];
    private readonly byte[] _maskKey = new byte[4];
    private int _hdrLen;
    private bool _inPayload;
    private byte _opcode;
    private bool _fin;
    private bool _masked;
    private long _remaining;
    private int _maskPos;
    private bool _fragmenting;
    private byte[]? _asm;
    private int _asmLen;
    private int _ctlLen;
    private bool _delivered;

    /// <param name="requireMask">True on a server: frames from a client must be masked. False on a client: frames from a server must not be.</param>
    /// <param name="maxMessageBytes">Largest data message.</param>
    public WsFrameReader(bool requireMask, int maxMessageBytes)
    {
        _requireMask = requireMask;
        _maxMessage = maxMessageBytes;
    }

    /// <summary>Close code to answer a <see cref="WsEvent.Error"/> with.</summary>
    public int ErrorCode { get; private set; }

    /// <summary>Why the stream was rejected, for diagnostics.</summary>
    public string? ErrorDetail { get; private set; }

    /// <summary>Status code of the last <see cref="WsEvent.Close"/>; 1005 (none) if the frame had no payload.</summary>
    public int CloseCode { get; private set; }

    /// <summary>Payload of the last ping, pong or close event.</summary>
    public ReadOnlySpan<byte> ControlPayload => _ctl.AsSpan(0, _ctlLen);

    /// <summary>Length of the last <see cref="WsEvent.Message"/>.</summary>
    public int MessageLength => _asmLen;

    /// <summary>Hands out the finished message as a pool-rented array (its first <see cref="MessageLength"/> bytes); the caller returns it with <c>NetBuffers.Return</c>.</summary>
    public byte[] TakeMessage(out int length)
    {
        length = _asmLen;
        var b = _asm ?? NetBuffers.Rent(1);
        _asm = null;
        _asmLen = 0;
        _delivered = false;
        return b;
    }

    /// <summary>Returns buffers still held.</summary>
    public void Release()
    {
        if (_asm is not null)
        {
            NetBuffers.Return(_asm);
            _asm = null;
        }
    }

    /// <summary>Reads input until something happens. Call again with the rest (<c>input[consumed..]</c>) after anything but <see cref="WsEvent.NeedMore"/>.</summary>
    public WsEvent Feed(ReadOnlySpan<byte> input, out int consumed)
    {
        consumed = 0;
        if (ErrorCode != 0)
        {
            return WsEvent.Error;
        }

        if (_delivered)
        {
            _asmLen = 0; // the caller did not take the last message; drop it
            _delivered = false;
        }

        while (true)
        {
            if (!_inPayload)
            {
                while (!HeaderComplete())
                {
                    if (consumed == input.Length)
                    {
                        return WsEvent.NeedMore;
                    }

                    _hdr[_hdrLen++] = input[consumed++];
                }

                if (!ParseHeader())
                {
                    return WsEvent.Error;
                }
            }

            if (_remaining > 0)
            {
                var take = (int)Math.Min(_remaining, input.Length - consumed);
                if (take == 0)
                {
                    return WsEvent.NeedMore;
                }

                var chunk = input.Slice(consumed, take);
                Span<byte> dst;
                if (_opcode >= WsFrames.OpClose)
                {
                    dst = _ctl.AsSpan(_ctlLen, take);
                    _ctlLen += take;
                }
                else
                {
                    EnsureAssembly(_asmLen + take);
                    dst = _asm.AsSpan(_asmLen, take);
                    _asmLen += take;
                }

                if (_masked)
                {
                    for (var i = 0; i < take; i++)
                    {
                        dst[i] = (byte)(chunk[i] ^ _maskKey[(_maskPos + i) & 3]);
                    }

                    _maskPos = (_maskPos + take) & 3;
                }
                else
                {
                    chunk.CopyTo(dst);
                }

                consumed += take;
                _remaining -= take;
                if (_remaining > 0)
                {
                    return WsEvent.NeedMore;
                }
            }

            _inPayload = false;
            _hdrLen = 0;
            var e = FinishFrame();
            if (e != WsEvent.NeedMore)
            {
                return e;
            }
        }
    }

    private bool HeaderComplete()
    {
        if (_hdrLen < 2)
        {
            return false;
        }

        var l7 = _hdr[1] & 0x7F;
        var total = 2 + (l7 == 126 ? 2 : l7 == 127 ? 8 : 0) + ((_hdr[1] & 0x80) != 0 ? 4 : 0);
        return _hdrLen >= total;
    }

    private bool Fail(int code, string detail)
    {
        ErrorCode = code;
        ErrorDetail = detail;
        return false;
    }

    private bool ParseHeader()
    {
        var b0 = _hdr[0];
        var b1 = _hdr[1];
        _fin = (b0 & 0x80) != 0;
        if ((b0 & 0x70) != 0)
        {
            return Fail(WsFrames.CodeProtocolError, "reserved bits set without a negotiated extension");
        }

        _opcode = (byte)(b0 & 0xF);
        _masked = (b1 & 0x80) != 0;
        if (_masked != _requireMask)
        {
            return Fail(WsFrames.CodeProtocolError, _requireMask ? "client frame is not masked" : "server frame is masked");
        }

        var l7 = b1 & 0x7F;
        ulong length;
        var pos = 2;
        if (l7 < 126)
        {
            length = (ulong)l7;
        }
        else if (l7 == 126)
        {
            length = BinaryPrimitives.ReadUInt16BigEndian(_hdr.AsSpan(pos));
            pos += 2;
            if (length < 126)
            {
                return Fail(WsFrames.CodeProtocolError, "length not minimally encoded");
            }
        }
        else
        {
            length = BinaryPrimitives.ReadUInt64BigEndian(_hdr.AsSpan(pos));
            pos += 8;
            if ((length >> 63) != 0 || length <= ushort.MaxValue)
            {
                return Fail(WsFrames.CodeProtocolError, "bad 64-bit length");
            }
        }

        if (_masked)
        {
            Array.Copy(_hdr, pos, _maskKey, 0, 4);
            _maskPos = 0;
        }

        var control = _opcode >= WsFrames.OpClose;
        switch (_opcode)
        {
            case WsFrames.OpContinuation:
                if (!_fragmenting)
                {
                    return Fail(WsFrames.CodeProtocolError, "continuation without a message to continue");
                }

                break;
            case WsFrames.OpBinary:
                if (_fragmenting)
                {
                    return Fail(WsFrames.CodeProtocolError, "new message inside a fragmented one");
                }

                break;
            case WsFrames.OpText:
                return Fail(WsFrames.CodeUnsupportedData, "text frames are not accepted");
            case WsFrames.OpClose:
            case WsFrames.OpPing:
            case WsFrames.OpPong:
                if (!_fin || length > WsFrames.MaxControlPayload)
                {
                    return Fail(WsFrames.CodeProtocolError, "control frame fragmented or longer than 125 bytes");
                }

                break;
            default:
                return Fail(WsFrames.CodeProtocolError, "reserved opcode");
        }

        if (control)
        {
            _ctlLen = 0;
        }
        else
        {
            if (_opcode == WsFrames.OpBinary)
            {
                _asmLen = 0;
            }

            if ((ulong)_asmLen + length > (ulong)_maxMessage)
            {
                return Fail(WsFrames.CodeTooBig, "message larger than the limit");
            }
        }

        _remaining = (long)length;
        _inPayload = true;
        return true;
    }

    private WsEvent FinishFrame()
    {
        switch (_opcode)
        {
            case WsFrames.OpPing:
                return WsEvent.Ping;
            case WsFrames.OpPong:
                return WsEvent.Pong;
            case WsFrames.OpClose:
                if (_ctlLen == 1)
                {
                    _ = Fail(WsFrames.CodeProtocolError, "close payload of one byte");
                    return WsEvent.Error;
                }

                CloseCode = 1005;
                if (_ctlLen >= 2)
                {
                    CloseCode = BinaryPrimitives.ReadUInt16BigEndian(_ctl);
                    if (!WsFrames.ValidCloseCode(CloseCode))
                    {
                        _ = Fail(WsFrames.CodeProtocolError, "close code not allowed on the wire");
                        return WsEvent.Error;
                    }

                    if (!Utf8.IsValid(_ctl.AsSpan(2, _ctlLen - 2)))
                    {
                        _ = Fail(WsFrames.CodeInvalidPayload, "close reason is not UTF-8");
                        return WsEvent.Error;
                    }
                }

                return WsEvent.Close;
            default:
                if (!_fin)
                {
                    _fragmenting = true;
                    return WsEvent.NeedMore;
                }

                _fragmenting = false;
                _delivered = true;
                if (_asm is null)
                {
                    _asm = NetBuffers.Rent(1);
                }

                return WsEvent.Message;
        }
    }

    private void EnsureAssembly(int needed)
    {
        if (_asm is null)
        {
            _asm = NetBuffers.Rent(Math.Max(needed, 1024));
        }
        else if (_asm.Length < needed)
        {
            var bigger = NetBuffers.Rent((int)Math.Min(Math.Max(needed, (long)_asm.Length * 2), _maxMessage + 1024L));
            _asm.AsSpan(0, _asmLen).CopyTo(bigger);
            NetBuffers.Return(_asm);
            _asm = bigger;
        }
    }
}
