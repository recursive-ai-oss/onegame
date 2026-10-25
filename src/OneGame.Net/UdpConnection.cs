using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace OneGame.Net;

/// <summary>
/// One reliable-UDP connection, either the client end made by <see cref="UdpTransport.Connect(IPEndPoint, UdpOptions?, NetClock?)"/>
/// or a server end handed out by a <see cref="UdpListener"/>. Messages are whole, ordered and reliable; unreliable messages are
/// available beside them.
/// </summary>
public sealed class UdpConnection : INetConnection
{
    private const int SendBurst = 512;

    private readonly UdpOptions _o;
    private readonly NetClock _clock;
    private readonly IDatagramSocket _socket;
    private readonly bool _ownsSocket;
    private readonly SocketAddress _remote;
    private readonly ReliableCore _core;
    private readonly byte[] _tx;
    private readonly byte[]? _rx;
    private readonly SocketAddress? _from;
    private readonly ulong _nonce;
    private readonly long _created;
    private readonly byte[] _cookie = new byte[Wire.CookieSize];
    private bool _haveCookie;
    private long _lastHandshake = long.MinValue / 2;
    private long _lingerUntil;
    private DisconnectReason _closingReason;
    private uint _connectionId;

    // Client end.
    internal UdpConnection(IDatagramSocket socket, bool ownsSocket, SocketAddress remote, UdpOptions options, NetClock clock)
    {
        options.Validate();
        _o = options;
        _clock = clock;
        _socket = socket;
        _ownsSocket = ownsSocket;
        _remote = Wire.Clone(remote);
        _created = clock.NowMs;
        _core = new ReliableCore(options, _created);
        _tx = new byte[options.Mtu];
        _rx = new byte[2048];
        _from = new SocketAddress(remote.Family, remote.Size);
        Span<byte> n = stackalloc byte[8];
        RandomNumberGenerator.Fill(n);
        _nonce = BinaryPrimitives.ReadUInt64LittleEndian(n);
        RemoteName = Wire.Describe(remote);
        State = ConnectionState.Connecting;
    }

    // Server end, created once the client has proved its address.
    internal UdpConnection(IDatagramSocket socket, SocketAddress remote, uint connectionId, ulong nonce, UdpOptions options, NetClock clock)
    {
        _o = options;
        _clock = clock;
        _socket = socket;
        _remote = remote;
        _connectionId = connectionId;
        _nonce = nonce;
        _created = clock.NowMs;
        _core = new ReliableCore(options, _created);
        _tx = new byte[options.Mtu];
        RemoteName = Wire.Describe(remote);
        State = ConnectionState.Connected;
    }

    /// <inheritdoc />
    public event Action<INetConnection>? Connected;

    /// <inheritdoc />
    public event Action<INetConnection, DisconnectInfo>? Disconnected;

    /// <inheritdoc />
    public ConnectionState State { get; private set; }

    /// <inheritdoc />
    public DisconnectInfo CloseInfo { get; private set; }

    /// <inheritdoc />
    public string RemoteName { get; }

    /// <inheritdoc />
    public NetStats Stats
    {
        get
        {
            var s = default(NetStats);
            _core.FillStats(ref s);
            return s;
        }
    }

    /// <summary>Largest message <see cref="SendUnreliable"/> accepts.</summary>
    public int MaxUnreliableBytes => _core.MaxUnreliableBytes;

    internal SocketAddress Remote => _remote;

    internal uint ConnectionId => _connectionId;

    internal ulong Nonce => _nonce;

    /// <inheritdoc />
    /// <exception cref="MessageTooLargeException">The message exceeds <see cref="UdpOptions.MaxMessageBytes"/>.</exception>
    public void Send(ReadOnlySpan<byte> message)
    {
        if (State is ConnectionState.Closing or ConnectionState.Closed)
        {
            return;
        }

        if (!_core.Enqueue(message))
        {
            Close(DisconnectReason.SendQueueFull, byRemote: false, 0, notify: true);
        }
    }

    /// <summary>
    /// Sends a message that may be lost, duplicated-and-dropped, or arrive out of order. It must fit one datagram
    /// (<see cref="MaxUnreliableBytes"/>) and is sent once, never repeated.
    /// </summary>
    /// <exception cref="MessageTooLargeException">The message does not fit one datagram.</exception>
    public void SendUnreliable(ReadOnlySpan<byte> message)
    {
        if (State is ConnectionState.Connected or ConnectionState.Connecting)
        {
            _core.EnqueueUnreliable(message);
        }
    }

    /// <inheritdoc />
    public bool TryReceive(out ReadOnlySpan<byte> message)
    {
        if (_core.TryReceive(out message))
        {
            return true;
        }

        if (State == ConnectionState.Closed)
        {
            _core.Release(); // everything that arrived has been read; give the buffers back
        }

        return false;
    }

    /// <summary>Takes the next unreliable message. The span is valid until the next call.</summary>
    public bool TryReceiveUnreliable(out ReadOnlySpan<byte> message) => _core.TryReceiveUnreliable(out message);

    /// <inheritdoc />
    public void Pump()
    {
        if (_ownsSocket && State != ConnectionState.Closed)
        {
            var now = _clock.NowMs;
            for (var i = 0; i < _o.MaxDatagramsPerPump; i++)
            {
                var n = _socket.Receive(_rx, _from!);
                if (n < 0)
                {
                    break;
                }

                if (_from!.Equals(_remote))
                {
                    HandleDatagram(_rx.AsSpan(0, n), now);
                }
            }
        }

        Tick(_clock.NowMs);
    }

    /// <inheritdoc />
    public void Disconnect(DisconnectReason reason = DisconnectReason.Normal, bool flush = false)
    {
        if (State is ConnectionState.Closed or ConnectionState.Closing)
        {
            return;
        }

        if (flush && State == ConnectionState.Connected && !_core.Drained)
        {
            State = ConnectionState.Closing;
            _closingReason = reason;
            _lingerUntil = _clock.NowMs + _o.LingerMs;
            return;
        }

        Close(reason, byRemote: false, 0, notify: State == ConnectionState.Connected);
    }

    internal void HandleDatagram(ReadOnlySpan<byte> d, long now)
    {
        if (d.Length < 1 || d[0] >> 4 != Wire.Version || State == ConnectionState.Closed)
        {
            return;
        }

        switch (d[0] & 0xF)
        {
            case Wire.KindChallenge when _from is not null && State == ConnectionState.Connecting && d.Length >= Wire.ChallengeSize && BinaryPrimitives.ReadUInt64LittleEndian(d[1..]) == _nonce:
                d.Slice(9, Wire.CookieSize).CopyTo(_cookie);
                _haveCookie = true;
                SendHandshake(now);
                break;
            case Wire.KindAccept when _from is not null && State == ConnectionState.Connecting && d.Length >= Wire.AcceptSize && BinaryPrimitives.ReadUInt64LittleEndian(d[1..]) == _nonce:
                _connectionId = BinaryPrimitives.ReadUInt32LittleEndian(d[9..]);
                State = ConnectionState.Connected;
                _core.Touch(now);
                Connected?.Invoke(this);
                break;
            case Wire.KindReject when _from is not null && State == ConnectionState.Connecting && d.Length >= Wire.RejectSize && BinaryPrimitives.ReadUInt64LittleEndian(d[1..]) == _nonce:
                Close((DisconnectReason)d[9], byRemote: true, d[9], notify: false);
                break;
            case Wire.KindData when d.Length >= ReliableCore.HeaderSize && State is ConnectionState.Connected or ConnectionState.Closing && BinaryPrimitives.ReadUInt32LittleEndian(d[1..]) == _connectionId:
                if (_core.OnPacket(d, now) == PacketResult.Violation)
                {
                    Close(DisconnectReason.ProtocolViolation, byRemote: false, 0, notify: true);
                }

                break;
            case Wire.KindDisconnect when d.Length >= Wire.DisconnectSize && State != ConnectionState.Connecting && BinaryPrimitives.ReadUInt32LittleEndian(d[1..]) == _connectionId:
                Close((DisconnectReason)d[5], byRemote: true, d[6], notify: false);
                break;
            default:
                break;
        }
    }

    internal void Tick(long now)
    {
        switch (State)
        {
            case ConnectionState.Connecting:
                if (now - _created >= _o.ConnectTimeoutMs)
                {
                    Close(DisconnectReason.ConnectTimeout, byRemote: false, 0, notify: false);
                }
                else if (_ownsSocket && now - _lastHandshake >= _o.ConnectRetryMs)
                {
                    SendHandshake(now);
                }

                break;
            case ConnectionState.Connected:
            case ConnectionState.Closing:
                if (now - _core.LastReceive >= _o.TimeoutMs)
                {
                    Close(DisconnectReason.Timeout, byRemote: false, 0, notify: true);
                    break;
                }

                for (var i = 0; i < SendBurst; i++)
                {
                    var n = _core.Build(_tx, _connectionId, now);
                    if (n == 0)
                    {
                        break;
                    }

                    _socket.Send(_tx.AsSpan(0, n), _remote);
                }

                if (State == ConnectionState.Closing && (_core.Drained || now >= _lingerUntil))
                {
                    Close(_closingReason, byRemote: false, 0, notify: true);
                }

                break;
            default:
                break;
        }
    }

    // The server-side half of the handshake: the listener calls this after it validated the cookie, and again for a repeated response.
    internal void SendAccept()
    {
        Span<byte> a = stackalloc byte[Wire.AcceptSize];
        a[0] = Wire.Header(Wire.KindAccept);
        BinaryPrimitives.WriteUInt64LittleEndian(a[1..], _nonce);
        BinaryPrimitives.WriteUInt32LittleEndian(a[9..], _connectionId);
        _socket.Send(a, _remote);
    }

    internal void Kick(DisconnectReason reason) => Disconnect(reason);

    private void SendHandshake(long now)
    {
        _lastHandshake = now;
        var d = _tx.AsSpan(0, Wire.MinHandshakeSize);
        d.Clear();
        d[0] = Wire.Header(_haveCookie ? Wire.KindResponse : Wire.KindRequest);
        BinaryPrimitives.WriteUInt64LittleEndian(d[1..], _nonce);
        if (_haveCookie)
        {
            _cookie.CopyTo(d[9..]);
        }

        _socket.Send(d, _remote);
    }

    private void Close(DisconnectReason reason, bool byRemote, ushort code, bool notify)
    {
        if (State == ConnectionState.Closed)
        {
            return;
        }

        if (notify)
        {
            // Three copies: a single lost datagram would otherwise leave the peer waiting for its timeout.
            Span<byte> d = stackalloc byte[Wire.DisconnectSize];
            d[0] = Wire.Header(Wire.KindDisconnect);
            BinaryPrimitives.WriteUInt32LittleEndian(d[1..], _connectionId);
            d[5] = (byte)reason;
            d[6] = (byte)code;
            for (var i = 0; i < 3; i++)
            {
                _socket.Send(d, _remote);
            }
        }

        State = ConnectionState.Closed;
        CloseInfo = new DisconnectInfo(reason, byRemote, code);
        if (_ownsSocket)
        {
            _socket.Dispose();
        }

        Disconnected?.Invoke(this, CloseInfo);
    }
}
