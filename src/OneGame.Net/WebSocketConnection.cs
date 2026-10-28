using System.Buffers;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using OneGame.Core;

namespace OneGame.Net;

/// <summary>
/// A WebSocket connection (RFC 6455) over a non-blocking TCP socket, client or server end. Messages are binary frames; long ones
/// go out as continuation frames so a ping or close can overtake them. There are no threads: <see cref="Pump"/> reads and writes
/// what the socket allows.
/// </summary>
public sealed class WebSocketConnection : INetConnection
{
    private readonly Socket _sock;
    private readonly bool _client;
    private readonly WebSocketOptions _o;
    private readonly NetClock _clock;
    private readonly string _remoteName;
    private readonly byte[] _rbuf;
    private readonly byte[] _wbuf;
    private readonly byte[] _key = new byte[24];
    private readonly byte[] _pong = new byte[WsFrames.MaxControlPayload];
    private readonly Queue<Msg> _out = new();
    private readonly Queue<Msg> _inbox = new();
    private readonly EndPoint? _connectTo;
    private WsFrameReader? _reader;
    private Phase _phase;
    private int _rlen;
    private int _wlen;
    private int _wpos;
    private int _outOffset;
    private int _outQueued;
    private int _inboxBytes;
    private byte[]? _lent;
    private bool _closeQueued;
    private bool _closeSent;
    private bool _closeReceived;
    private bool _localClose;
    private bool _failed;
    private bool _pongPending;
    private int _pongLen;
    private bool _pingPending;
    private int _closeCodeOut;
    private int _closeCodeIn;
    private DisconnectReason _localReason;
    private long _phaseStart;
    private long _closeDeadline;
    private long _lastRecv;
    private long _lastPing;
    private long _framesSent;
    private long _framesReceived;
    private long _bytesSent;
    private long _bytesReceived;
    private long _messagesSent;
    private long _messagesReceived;
    private double _srtt;

    private WebSocketConnection(Socket socket, bool client, EndPoint? connectTo, WebSocketOptions options, NetClock clock)
    {
        options.Validate();
        _sock = socket;
        _client = client;
        _connectTo = connectTo;
        _o = options;
        _clock = clock;
        _remoteName = (connectTo ?? socket.RemoteEndPoint)?.ToString() ?? "?";
        _rbuf = NetBuffers.Rent(options.ReceiveBufferBytes);
        _wbuf = NetBuffers.Rent(Math.Max(4096, options.SendFragmentBytes + WsFrames.MaxHeaderBytes));
        _phaseStart = clock.NowMs;
        _lastRecv = _phaseStart;
        _lastPing = _phaseStart;
        _phase = client ? Phase.TcpConnect : Phase.ServerHandshake;
    }

    private enum Phase : byte
    {
        TcpConnect,
        ClientHandshake,
        ServerHandshake,
        Open,
        Closing,
        Closed,
    }

    /// <inheritdoc />
    public event Action<INetConnection>? Connected;

    /// <inheritdoc />
    public event Action<INetConnection, DisconnectInfo>? Disconnected;

    /// <inheritdoc />
    public ConnectionState State => _phase switch
    {
        Phase.Open => ConnectionState.Connected,
        Phase.Closing => ConnectionState.Closing,
        Phase.Closed => ConnectionState.Closed,
        _ => ConnectionState.Connecting,
    };

    /// <inheritdoc />
    public DisconnectInfo CloseInfo { get; private set; }

    /// <inheritdoc />
    public string RemoteName => _remoteName;

    /// <inheritdoc />
    public NetStats Stats => new()
    {
        RttMs = _srtt,
        PacketsSent = _framesSent,
        PacketsReceived = _framesReceived,
        BytesSent = _bytesSent,
        BytesReceived = _bytesReceived,
        MessagesSent = _messagesSent,
        MessagesReceived = _messagesReceived,
        SendQueueBytes = _outQueued,
    };

    internal static WebSocketConnection ForClient(IPEndPoint remote, WebSocketOptions options, NetClock clock)
    {
        var socket = new Socket(remote.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { Blocking = false, NoDelay = true };
        var c = new WebSocketConnection(socket, true, remote, options, clock);
        try
        {
            socket.Connect(remote);
        }
        catch (SocketException e) when (e.SocketErrorCode is SocketError.WouldBlock or SocketError.InProgress)
        {
            // The usual answer to a non-blocking connect; Pump watches for completion.
        }
        catch (SocketException)
        {
            c.FinishClose(new DisconnectInfo(DisconnectReason.TransportError, false));
        }

        return c;
    }

    internal static WebSocketConnection ForAccepted(Socket socket, WebSocketOptions options, NetClock clock)
    {
        socket.Blocking = false;
        socket.NoDelay = true;
        return new WebSocketConnection(socket, false, null, options, clock);
    }

    /// <inheritdoc />
    /// <exception cref="MessageTooLargeException">The message exceeds <see cref="WebSocketOptions.MaxMessageBytes"/>.</exception>
    public void Send(ReadOnlySpan<byte> message)
    {
        if (message.Length > _o.MaxMessageBytes)
        {
            throw new MessageTooLargeException(message.Length, _o.MaxMessageBytes);
        }

        if (_phase is Phase.Closing or Phase.Closed || _closeQueued)
        {
            return;
        }

        if ((long)_outQueued + message.Length > _o.MaxSendQueueBytes)
        {
            Disconnect(DisconnectReason.SendQueueFull);
            return;
        }

        var buf = NetBuffers.Rent(Math.Max(1, message.Length));
        message.CopyTo(buf);
        _out.Enqueue(new Msg(buf, message.Length));
        _outQueued += message.Length;
        _messagesSent++;
    }

    /// <inheritdoc />
    public bool TryReceive(out ReadOnlySpan<byte> message)
    {
        if (_lent is not null)
        {
            NetBuffers.Return(_lent);
            _lent = null;
        }

        if (!_inbox.TryDequeue(out var m))
        {
            message = default;
            return false;
        }

        _inboxBytes -= m.Len;
        _lent = m.Buf;
        message = m.Buf.AsSpan(0, m.Len);
        return true;
    }

    /// <inheritdoc />
    public void Disconnect(DisconnectReason reason = DisconnectReason.Normal, bool flush = false)
    {
        if (_phase is Phase.Closing or Phase.Closed)
        {
            return;
        }

        if (_phase != Phase.Open)
        {
            FinishClose(new DisconnectInfo(reason, false));
            return;
        }

        _localClose = true;
        _localReason = reason;
        _closeCodeOut = ToCode(reason);
        _closeQueued = true;
        _phase = Phase.Closing;
        _closeDeadline = _clock.NowMs + _o.CloseTimeoutMs;
        if (!flush)
        {
            DropOutgoing();
        }
    }

    /// <inheritdoc />
    public void Pump()
    {
        if (_phase == Phase.Closed)
        {
            return;
        }

        var now = _clock.NowMs;
        switch (_phase)
        {
            case Phase.TcpConnect:
                PumpConnect(now);
                break;
            case Phase.ClientHandshake:
            case Phase.ServerHandshake:
                ReadSome(now);
                if (_phase is Phase.ClientHandshake or Phase.ServerHandshake)
                {
                    TryFinishHandshake(now);
                }

                if (_phase != Phase.Closed)
                {
                    FlushWrites(now);
                }

                if (_phase is Phase.ClientHandshake or Phase.ServerHandshake && now - _phaseStart >= _o.HandshakeTimeoutMs)
                {
                    FinishClose(new DisconnectInfo(DisconnectReason.ConnectTimeout, false));
                }

                break;
            default:
                ProcessFrames(now);
                if (_phase != Phase.Closed)
                {
                    ReadSome(now);
                }

                if (_phase != Phase.Closed)
                {
                    KeepAlive(now);
                    FlushWrites(now);
                }

                if (_phase == Phase.Closing && ((_closeSent && (_closeReceived || _failed) && _wpos == _wlen) || now >= _closeDeadline))
                {
                    FinishClose(ResolveInfo());
                }

                break;
        }
    }

    private void PumpConnect(long now)
    {
        if (_sock.Poll(0, SelectMode.SelectWrite) || _sock.Poll(0, SelectMode.SelectError))
        {
            var error = _sock.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Error);
            if (error is int e && e != 0)
            {
                FinishClose(new DisconnectInfo(DisconnectReason.TransportError, false));
                return;
            }

            WsHandshake.NewKey(_key);
            _wlen = WsHandshake.WriteRequest(_wbuf, _o.Path, _o.Host ?? _connectTo!.ToString()!, _key);
            _wpos = 0;
            _phase = Phase.ClientHandshake;
            FlushWrites(now);
        }
        else if (now - _phaseStart >= _o.HandshakeTimeoutMs)
        {
            FinishClose(new DisconnectInfo(DisconnectReason.ConnectTimeout, false));
        }
    }

    private void TryFinishHandshake(long now)
    {
        var end = WsHandshake.FindEnd(_rbuf.AsSpan(0, _rlen));
        if (end < 0)
        {
            if (_rlen >= _o.MaxHeaderBytes)
            {
                RefuseHandshake(WsHandshake.RequestResult.BadRequest);
            }

            return;
        }

        if (end > _o.MaxHeaderBytes)
        {
            RefuseHandshake(WsHandshake.RequestResult.BadRequest);
            return;
        }

        var head = _rbuf.AsSpan(0, end);
        if (_client)
        {
            if (!WsHandshake.ParseResponse(head, _key))
            {
                FinishClose(new DisconnectInfo(DisconnectReason.Rejected, false));
                return;
            }
        }
        else
        {
            var result = WsHandshake.ParseRequest(head, _o, _key);
            if (result != WsHandshake.RequestResult.Ok)
            {
                RefuseHandshake(result);
                return;
            }

            _wlen = WsHandshake.WriteAccept(_wbuf, _key);
            _wpos = 0;
        }

        Buffer.BlockCopy(_rbuf, end, _rbuf, 0, _rlen - end);
        _rlen -= end;
        _reader = new WsFrameReader(requireMask: !_client, _o.MaxMessageBytes);
        _phase = Phase.Open;
        _lastRecv = now;
        _lastPing = now;
        if (_client)
        {
            Connected?.Invoke(this);
        }
    }

    private void RefuseHandshake(WsHandshake.RequestResult why)
    {
        if (!_client)
        {
            var n = WsHandshake.WriteRefusal(_wbuf, why);
            _ = _sock.Send(_wbuf.AsSpan(0, n), SocketFlags.None, out _); // best effort; the socket closes right after
        }

        FinishClose(new DisconnectInfo(why == WsHandshake.RequestResult.UpgradeRequired ? DisconnectReason.VersionMismatch : DisconnectReason.Rejected, false));
    }

    private void ReadSome(long now)
    {
        for (var i = 0; i < 64 && _phase != Phase.Closed; i++)
        {
            if (_phase is Phase.Open or Phase.Closing && (_inboxBytes >= _o.MaxReceiveQueueBytes || _rlen == _rbuf.Length))
            {
                return; // the application is behind; leave the bytes in the socket so TCP slows the sender
            }

            if (_rlen == _rbuf.Length)
            {
                return;
            }

            var n = _sock.Receive(_rbuf.AsSpan(_rlen), SocketFlags.None, out var error);
            if (error == SocketError.WouldBlock)
            {
                return;
            }

            if (error != SocketError.Success)
            {
                OnSocketGone(true);
                return;
            }

            if (n == 0)
            {
                OnSocketGone(false);
                return;
            }

            _rlen += n;
            _bytesReceived += n;
            _lastRecv = now;
            if (_phase is Phase.Open or Phase.Closing)
            {
                ProcessFrames(now);
            }
            else
            {
                return; // let the handshake check run on what arrived
            }
        }
    }

    private void OnSocketGone(bool error)
    {
        // The peer closed TCP. After a close exchange that is the normal end; otherwise it is code 1006, "closed abnormally".
        if (_phase == Phase.Closing && (_closeSent || _closeReceived))
        {
            FinishClose(ResolveInfo());
        }
        else if (error)
        {
            FinishClose(new DisconnectInfo(DisconnectReason.TransportError, false));
        }
        else
        {
            FinishClose(new DisconnectInfo(DisconnectReason.TransportError, true, 1006));
        }
    }

    private void ProcessFrames(long now)
    {
        if (_reader is null || _rlen == 0 || _phase is not (Phase.Open or Phase.Closing))
        {
            return;
        }

        var pos = 0;
        while (pos < _rlen && _phase is Phase.Open or Phase.Closing && _inboxBytes < _o.MaxReceiveQueueBytes)
        {
            var e = _reader.Feed(_rbuf.AsSpan(pos, _rlen - pos), out var used);
            pos += used;
            if (e == WsEvent.NeedMore)
            {
                break;
            }

            _framesReceived++;
            switch (e)
            {
                case WsEvent.Message:
                    var buf = _reader.TakeMessage(out var len);
                    if (_closeReceived || _closeSent)
                    {
                        NetBuffers.Return(buf); // data after a close frame is ignored
                        break;
                    }

                    _inbox.Enqueue(new Msg(buf, len));
                    _inboxBytes += len;
                    _messagesReceived++;
                    break;
                case WsEvent.Ping:
                    // RFC 6455 §5.5.2: answer with a pong carrying the same data. A newer ping replaces an unsent pong (§5.5.3).
                    _pongLen = _reader.ControlPayload.Length;
                    _reader.ControlPayload.CopyTo(_pong);
                    _pongPending = true;
                    break;
                case WsEvent.Pong:
                    if (_reader.ControlPayload.Length == 8)
                    {
                        var sent = BinaryPrimitives.ReadInt64LittleEndian(_reader.ControlPayload);
                        var sample = Math.Max(0, now - sent);
                        _srtt = _srtt == 0 ? sample : (0.875 * _srtt) + (0.125 * sample);
                    }

                    break;
                case WsEvent.Close:
                    OnCloseFrame();
                    break;
                case WsEvent.Error:
                    if (!_closeQueued)
                    {
                        _localClose = true;
                        _localReason = _reader.ErrorCode == WsFrames.CodeTooBig ? DisconnectReason.MessageTooLarge : DisconnectReason.ProtocolViolation;
                        _closeCodeOut = _reader.ErrorCode;
                        _closeQueued = true;
                        _failed = true;
                        _phase = Phase.Closing;
                        _closeDeadline = now + _o.CloseTimeoutMs;
                        DropOutgoing();
                    }

                    pos = _rlen; // nothing more is read from a broken stream
                    break;
                default:
                    break;
            }
        }

        if (pos > 0)
        {
            Buffer.BlockCopy(_rbuf, pos, _rbuf, 0, _rlen - pos);
            _rlen -= pos;
        }
    }

    private void OnCloseFrame()
    {
        _closeReceived = true;
        _closeCodeIn = _reader!.CloseCode;
        if (!_closeQueued)
        {
            // Answer a close with a close (§5.5.1), echoing its status code.
            _closeCodeOut = _closeCodeIn == 1005 ? WsFrames.CodeNormal : _closeCodeIn;
            _closeQueued = true;
            DropOutgoing();
        }

        if (_phase == Phase.Open)
        {
            _phase = Phase.Closing;
            _closeDeadline = _clock.NowMs + _o.CloseTimeoutMs;
        }
    }

    private void KeepAlive(long now)
    {
        if (_phase != Phase.Open)
        {
            return;
        }

        if (now - _lastRecv >= _o.TimeoutMs)
        {
            FinishClose(new DisconnectInfo(DisconnectReason.Timeout, false));
        }
        else if (now - Math.Max(_lastRecv, _lastPing) >= _o.KeepAliveMs)
        {
            _pingPending = true;
            _lastPing = now;
        }
    }

    private void FlushWrites(long now)
    {
        _ = now;
        while (_phase != Phase.Closed)
        {
            if (_wpos < _wlen)
            {
                var n = _sock.Send(_wbuf.AsSpan(_wpos, _wlen - _wpos), SocketFlags.None, out var error);
                if (error == SocketError.WouldBlock)
                {
                    return;
                }

                if (error != SocketError.Success)
                {
                    OnSocketGone(true);
                    return;
                }

                _wpos += n;
                _bytesSent += n;
                if (_wpos < _wlen)
                {
                    return;
                }

                _wpos = 0;
                _wlen = 0;
            }

            if (_phase is Phase.ClientHandshake or Phase.ServerHandshake or Phase.TcpConnect)
            {
                return;
            }

            if (!BuildNextFrame())
            {
                return;
            }
        }
    }

    // Puts the next outgoing frame in the write buffer: close, then pong, then ping, then message data.
    private bool BuildNextFrame()
    {
        if (_closeSent)
        {
            return false; // nothing may follow a close frame (§5.5.1)
        }

        Span<byte> key = stackalloc byte[4];
        if (_client)
        {
            Entropy.Fill(key);
        }
        else
        {
            key = default;
        }

        if (_pongPending)
        {
            _wlen = WsFrames.Write(_wbuf, WsFrames.OpPong, true, _pong.AsSpan(0, _pongLen), key);
            _pongPending = false;
        }
        else if (_pingPending)
        {
            Span<byte> payload = stackalloc byte[8];
            BinaryPrimitives.WriteInt64LittleEndian(payload, _clock.NowMs);
            _wlen = WsFrames.Write(_wbuf, WsFrames.OpPing, true, payload, key);
            _pingPending = false;
        }
        else if (_closeQueued && _out.Count == 0)
        {
            Span<byte> payload = stackalloc byte[2];
            BinaryPrimitives.WriteUInt16BigEndian(payload, (ushort)_closeCodeOut);
            _wlen = WsFrames.Write(_wbuf, WsFrames.OpClose, true, payload, key);
            _closeSent = true;
        }
        else if (_out.Count > 0 && _phase is Phase.Open or Phase.Closing)
        {
            var m = _out.Peek();
            var remaining = m.Len - _outOffset;
            var take = Math.Min(remaining, _o.SendFragmentBytes);
            var last = take == remaining;
            _wlen = WsFrames.Write(_wbuf, _outOffset == 0 ? WsFrames.OpBinary : WsFrames.OpContinuation, last, m.Buf.AsSpan(_outOffset, take), key);
            if (last)
            {
                _ = _out.Dequeue();
                NetBuffers.Return(m.Buf);
                _outQueued -= m.Len;
                _outOffset = 0;
            }
            else
            {
                _outOffset += take;
            }
        }
        else
        {
            return false;
        }

        _wpos = 0;
        _framesSent++;
        return true;
    }

    private void DropOutgoing()
    {
        // A message already partly written stays whole on the wire only if we finish it; since a close follows, the rest is dropped.
        while (_out.TryDequeue(out var m))
        {
            NetBuffers.Return(m.Buf);
        }

        _outQueued = 0;
        _outOffset = 0;
    }

    private DisconnectInfo ResolveInfo()
    {
        if (_localClose)
        {
            return new DisconnectInfo(_localReason, false, (ushort)_closeCodeOut);
        }

        return new DisconnectInfo(FromCode(_closeCodeIn), true, (ushort)_closeCodeIn);
    }

    private void FinishClose(DisconnectInfo info)
    {
        if (_phase == Phase.Closed)
        {
            return;
        }

        _phase = Phase.Closed;
        CloseInfo = info;
        try
        {
            _sock.Shutdown(SocketShutdown.Both);
        }
        catch (SocketException)
        {
            // Already reset by the peer.
        }
        catch (ObjectDisposedException)
        {
            // Closed twice.
        }

        _sock.Dispose();
        DropOutgoing();
        _reader?.Release();
        NetBuffers.Return(_rbuf);
        NetBuffers.Return(_wbuf);
        Disconnected?.Invoke(this, info);
    }

    /// <summary>Maps our reasons to WebSocket close codes: the registered ones where one fits, the private range 4000 + value otherwise.</summary>
    internal static int ToCode(DisconnectReason reason) => reason switch
    {
        DisconnectReason.Normal => WsFrames.CodeNormal,
        DisconnectReason.Shutdown => WsFrames.CodeGoingAway,
        DisconnectReason.ProtocolViolation => WsFrames.CodeProtocolError,
        DisconnectReason.MessageTooLarge => WsFrames.CodeTooBig,
        _ => 4000 + (byte)reason,
    };

    internal static DisconnectReason FromCode(int code) => code switch
    {
        WsFrames.CodeGoingAway => DisconnectReason.Shutdown,
        WsFrames.CodeProtocolError or WsFrames.CodeUnsupportedData or WsFrames.CodeInvalidPayload or WsFrames.CodePolicy => DisconnectReason.ProtocolViolation,
        WsFrames.CodeTooBig => DisconnectReason.MessageTooLarge,
        >= 4000 and <= 4255 => (DisconnectReason)(code - 4000),
        _ => DisconnectReason.Normal,
    };

    private readonly record struct Msg(byte[] Buf, int Len);
}

/// <summary>The server end of the WebSocket transport.</summary>
public sealed class WebSocketListener : INetListener
{
    private readonly Socket _socket;
    private readonly WebSocketOptions _o;
    private readonly NetClock _clock;
    private readonly List<WebSocketConnection> _pending = [];
    private readonly List<WebSocketConnection> _open = [];
    private readonly Queue<WebSocketConnection> _accepted = new();

    internal WebSocketListener(IPEndPoint bind, WebSocketOptions options, NetClock clock)
    {
        options.Validate();
        _o = options;
        _clock = clock;
        _socket = new Socket(bind.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { Blocking = false };
        try
        {
            _socket.Bind(bind);
            _socket.Listen(backlog: 64);
        }
        catch
        {
            _socket.Dispose();
            throw;
        }
    }

    /// <inheritdoc />
    public EndPoint LocalEndPoint => _socket.LocalEndPoint!;

    /// <inheritdoc />
    public int ConnectionCount => _open.Count;

    /// <inheritdoc />
    public int PendingHandshakes => _pending.Count;

    /// <inheritdoc />
    public bool TryAccept(out INetConnection connection)
    {
        if (_accepted.TryDequeue(out var c))
        {
            connection = c;
            return true;
        }

        connection = null!;
        return false;
    }

    /// <inheritdoc />
    public void Pump()
    {
        for (var i = 0; i < 32 && _socket.Poll(0, SelectMode.SelectRead); i++)
        {
            Socket accepted;
            try
            {
                accepted = _socket.Accept();
            }
            catch (SocketException)
            {
                break; // the client gave up between the poll and the accept
            }

            if (_pending.Count >= _o.MaxPendingHandshakes || _pending.Count + _open.Count >= _o.MaxConnections + _o.MaxPendingHandshakes || _open.Count >= _o.MaxConnections)
            {
                accepted.Dispose();
                continue;
            }

            _pending.Add(WebSocketConnection.ForAccepted(accepted, _o, _clock));
        }

        for (var i = 0; i < _pending.Count; i++)
        {
            var c = _pending[i];
            c.Pump();
            if (c.State == ConnectionState.Connecting)
            {
                continue;
            }

            _pending.RemoveAt(i--);
            if (c.State == ConnectionState.Connected)
            {
                _open.Add(c);
                _accepted.Enqueue(c);
            }
        }

        for (var i = 0; i < _open.Count; i++)
        {
            _open[i].Pump();
            if (_open[i].State == ConnectionState.Closed)
            {
                _open.RemoveAt(i--);
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var c in _pending.ToArray())
        {
            c.Disconnect(DisconnectReason.Shutdown);
        }

        foreach (var c in _open.ToArray())
        {
            c.Disconnect(DisconnectReason.Shutdown);
            c.Pump();
        }

        _socket.Dispose();
    }
}

/// <summary>Creates WebSocket listeners and connections.</summary>
public static class WebSocketTransport
{
    /// <summary>Starts listening for WebSocket clients on a TCP port.</summary>
    /// <param name="bind">The local address and port.</param>
    /// <param name="options">Settings; <see cref="WebSocketOptions.Security"/> is required (<see cref="NetSecurity.ForServer"/>, or <see cref="NetSecurity.Insecure"/> for tests and local tools).</param>
    /// <param name="clock">Time source; the real clock if null.</param>
    /// <exception cref="SecurityNotConfiguredException">No security was chosen, or a Required listener has no server key.</exception>
    public static INetListener Listen(IPEndPoint bind, WebSocketOptions? options = null, NetClock? clock = null)
    {
        ArgumentNullException.ThrowIfNull(bind);
        options ??= new WebSocketOptions();
        var security = NetSecurity.RequireForListening(options.Security, nameof(WebSocketOptions));
        clock ??= NetClock.System;
        if (security.Mode == SecurityMode.Insecure)
        {
            return new WebSocketListener(bind, options, clock);
        }

        options.Validate();
        return new SecureListener(new WebSocketListener(bind, options.ForInner(), clock), security, options, clock);
    }

    /// <summary>Connects to a WebSocket server. The connection starts in <see cref="ConnectionState.Connecting"/>; messages sent now go out when it connects.</summary>
    /// <param name="remote">The server's address.</param>
    /// <param name="options">Settings; <see cref="WebSocketOptions.Security"/> is required (<see cref="NetSecurity.ForClient"/>, or <see cref="NetSecurity.Insecure"/> for tests and local tools).</param>
    /// <param name="clock">Time source; the real clock if null.</param>
    /// <exception cref="SecurityNotConfiguredException">No security was chosen, or a Required client trusts no server key.</exception>
    public static INetConnection Connect(IPEndPoint remote, WebSocketOptions? options = null, NetClock? clock = null)
    {
        ArgumentNullException.ThrowIfNull(remote);
        options ??= new WebSocketOptions();
        var security = NetSecurity.RequireForConnecting(options.Security, nameof(WebSocketOptions));
        clock ??= NetClock.System;
        if (security.Mode == SecurityMode.Insecure)
        {
            return WebSocketConnection.ForClient(remote, options, clock);
        }

        options.Validate();
        var inner = options.ForInner();
        return SecureConnection.ForClient(() => WebSocketConnection.ForClient(remote, inner, clock), security, options, clock);
    }

    /// <summary>Applies the security setting to a connection something else opens (the browser's WebSocket): Required wraps it in the encrypted records, Insecure returns it as it is.</summary>
    /// <param name="open">Opens a fresh connection; called again when the server rejects the key tried and another is trusted.</param>
    /// <param name="security">The client's security setting.</param>
    /// <param name="options">Limits and timeouts; defaults if null.</param>
    /// <param name="clock">Time source; the real clock if null.</param>
    /// <exception cref="SecurityNotConfiguredException">No security was chosen, or a Required client trusts no server key.</exception>
    public static INetConnection Secure(Func<INetConnection> open, NetSecurity? security, WebSocketOptions? options = null, NetClock? clock = null)
    {
        ArgumentNullException.ThrowIfNull(open);
        var sec = NetSecurity.RequireForConnecting(security, nameof(WebSocketOptions));
        if (sec.Mode == SecurityMode.Insecure)
        {
            return open();
        }

        options ??= new WebSocketOptions();
        options.Validate();
        return SecureConnection.ForClient(open, sec, options, clock ?? NetClock.System);
    }

    // Tests that need the concrete plain types.
    internal static WebSocketListener ListenPlain(IPEndPoint bind, WebSocketOptions? options = null, NetClock? clock = null) => new(bind, options ?? new WebSocketOptions(), clock ?? NetClock.System);

    internal static WebSocketConnection ConnectPlain(IPEndPoint remote, WebSocketOptions? options = null, NetClock? clock = null) => WebSocketConnection.ForClient(remote, options ?? new WebSocketOptions(), clock ?? NetClock.System);
}
