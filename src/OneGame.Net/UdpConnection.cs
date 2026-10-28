using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using OneGame.Core;

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

    // Security (null for an Insecure connection).
    private readonly NetSecurity _security;
    private readonly bool _secure;
    private readonly byte[]? _plain;
    private readonly byte[]? _msg1;
    private int _msg1Length;
    private int _keyIndex;
    private int _keysRejected;
    private SecureHandshake? _handshake;
    private DatagramSender? _send;
    private DatagramReceiver? _receive;
    private readonly byte[]? _resetToken;
    private byte[]? _acceptDatagram;
    private bool _confirmed = true;

    // Listener bookkeeping: true from the secure handshake until the first authenticated packet (or the connection's end).
    internal bool IsUnconfirmed { get; set; }

    // Client end.
    internal UdpConnection(IDatagramSocket socket, bool ownsSocket, SocketAddress remote, UdpOptions options, NetClock clock)
    {
        options.Validate();
        _security = NetSecurity.RequireForConnecting(options.Security, nameof(UdpOptions));
        _secure = _security.Mode == SecurityMode.Required;
        _o = options;
        _clock = clock;
        _socket = socket;
        _ownsSocket = ownsSocket;
        _remote = Wire.Clone(remote);
        _created = clock.NowMs;
        _core = new ReliableCore(options, _created, _secure ? Wire.TagSize : 0);
        _tx = new byte[options.Mtu];
        _rx = new byte[2048];
        if (_secure)
        {
            _plain = new byte[options.Mtu];
            _msg1 = new byte[SecureHandshake.FirstMessageOverhead];
            _resetToken = new byte[16];
        }

        _from = new SocketAddress(remote.Family, remote.Size);
        Span<byte> n = stackalloc byte[8];
        Entropy.Fill(n);
        _nonce = BinaryPrimitives.ReadUInt64LittleEndian(n);
        RemoteName = Wire.Describe(remote);
        State = ConnectionState.Connecting;
    }

    // Server end, created once the client has proved its address.
    internal UdpConnection(IDatagramSocket socket, SocketAddress remote, uint connectionId, ulong nonce, UdpOptions options, NetClock clock, ServerSecrets? secrets = null)
    {
        _security = options.Security!;
        _secure = secrets is not null;
        _o = options;
        _clock = clock;
        _socket = socket;
        _remote = remote;
        _connectionId = connectionId;
        _nonce = nonce;
        _created = clock.NowMs;
        _core = new ReliableCore(options, _created, _secure ? Wire.TagSize : 0);
        _tx = new byte[options.Mtu];
        RemoteName = Wire.Describe(remote);
        State = ConnectionState.Connected;
        if (secrets is not null)
        {
            _plain = new byte[options.Mtu];
            _send = secrets.Send;
            _receive = secrets.Receive;
            _remoteKey = secrets.RemoteKey;
            ChannelBinding = secrets.ChannelBinding;
            _acceptDatagram = secrets.AcceptDatagram;
            _confirmed = false; // handed out only after the first authenticated packet
            IsUnconfirmed = true;
        }
    }

    // What a listener hands a server end once the handshake succeeded.
    internal sealed record ServerSecrets(DatagramSender Send, DatagramReceiver Receive, PeerKey RemoteKey, byte[] AcceptDatagram, byte[] ChannelBinding);

    private PeerKey? _remoteKey;

    /// <inheritdoc />
    public bool IsEncrypted => _secure;

    /// <inheritdoc />
    public PeerKey? RemoteKey => _remoteKey;

    /// <inheritdoc />
    public ReadOnlyMemory<byte> ChannelBinding { get; private set; }

    // Server end: raised once, when the first authenticated packet shows the client holds its keys.
    internal event Action<UdpConnection>? Confirmed;

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
        if (d.Length < 1 || State == ConnectionState.Closed)
        {
            return;
        }

        var version = d[0] >> 4;
        var kind = d[0] & 0xF;

        // A Reject comes in the version of the request it answers, so a Required client can learn that a plaintext server is not for it.
        if (kind == Wire.KindReject)
        {
            if (_from is not null && State == ConnectionState.Connecting && d.Length >= Wire.RejectSize && BinaryPrimitives.ReadUInt64LittleEndian(d[1..]) == _nonce)
            {
                OnReject((DisconnectReason)d[9], now);
            }

            return;
        }

        if (version != (_secure ? Wire.VersionSecure : Wire.Version))
        {
            return;
        }

        switch (kind)
        {
            case Wire.KindChallenge when _from is not null && State == ConnectionState.Connecting && d.Length >= Wire.ChallengeSize && BinaryPrimitives.ReadUInt64LittleEndian(d[1..]) == _nonce:
                if (!d.Slice(9, Wire.CookieSize).SequenceEqual(_cookie))
                {
                    d.Slice(9, Wire.CookieSize).CopyTo(_cookie);
                    _msg1Length = 0; // the first message is bound to the cookie
                }

                _haveCookie = true;
                SendHandshake(now);
                break;
            case Wire.KindAccept when _from is not null && State == ConnectionState.Connecting && BinaryPrimitives.ReadUInt64LittleEndian(d[1..]) == _nonce:
                if (_secure ? d.Length >= Wire.SecureAcceptHeader && OnSecureAccept(d) : d.Length >= Wire.AcceptSize)
                {
                    _connectionId = BinaryPrimitives.ReadUInt32LittleEndian(d[9..]);
                    State = ConnectionState.Connected;
                    _core.Touch(now);
                    if (_secure)
                    {
                        _core.ForceSend(); // the server holds this connection back until it hears from us
                    }

                    Connected?.Invoke(this);
                }

                break;
            case Wire.KindData when !_secure && d.Length >= ReliableCore.HeaderSize && State is ConnectionState.Connected or ConnectionState.Closing && BinaryPrimitives.ReadUInt32LittleEndian(d[1..]) == _connectionId:
                if (_core.OnPacket(d, now) == PacketResult.Violation)
                {
                    Close(DisconnectReason.ProtocolViolation, byRemote: false, 0, notify: true);
                }

                break;
            case Wire.KindData when _secure && d.Length >= Wire.SecureAadSize + 14 + Wire.TagSize && d.Length <= _o.Mtu && State is ConnectionState.Connected or ConnectionState.Closing && BinaryPrimitives.ReadUInt32LittleEndian(d[1..]) == _connectionId:
                OnSecureData(d, now);
                break;
            case Wire.KindDisconnect when !_secure && d.Length >= Wire.DisconnectSize && State != ConnectionState.Connecting && BinaryPrimitives.ReadUInt32LittleEndian(d[1..]) == _connectionId:
                Close((DisconnectReason)d[5], byRemote: true, d[6], notify: false);
                break;
            case Wire.KindDisconnect when _secure && d.Length == 5 + 2 + Wire.TagSize && State != ConnectionState.Connecting && BinaryPrimitives.ReadUInt32LittleEndian(d[1..]) == _connectionId:
            {
                // Only the holder of the session key can close it; a forged or replayed-from-elsewhere datagram fails here.
                Span<byte> body = stackalloc byte[2];
                if (_receive!.OpenClose(d[..5], d[5..], body))
                {
                    Close((DisconnectReason)body[0], byRemote: true, body[1], notify: false);
                }

                break;
            }

            case Wire.KindReset when _secure && d.Length == Wire.ResetSize && State != ConnectionState.Connecting && BinaryPrimitives.ReadUInt32LittleEndian(d[1..]) == _connectionId:
                if (HmacSha256.FixedTimeEquals(d[5..], _resetToken))
                {
                    Close(DisconnectReason.Shutdown, byRemote: true, 0, notify: false);
                }

                break;
            default:
                break;
        }
    }

    // Reject is unauthenticated. It can only end a connect attempt, which a party that can drop packets could do anyway.
    private void OnReject(DisconnectReason reason, long now)
    {
        if (_secure && reason == DisconnectReason.HandshakeFailed)
        {
            // The server does not know the key we tried; the next trusted key gets a turn.
            if (++_keysRejected < _security.TrustedServerKeys.Count)
            {
                _keyIndex++;
                _msg1Length = 0;
                if (_haveCookie)
                {
                    SendHandshake(now);
                }

                return;
            }
        }

        Close(reason, byRemote: true, (byte)reason, notify: false);
    }

    // Client: reads the server's message 2 and takes the session keys. Anything that does not authenticate is ignored.
    private bool OnSecureAccept(ReadOnlySpan<byte> d)
    {
        var length = BinaryPrimitives.ReadUInt16LittleEndian(d[13..]);
        if (_handshake is null || _msg1Length == 0 || length > d.Length - Wire.SecureAcceptHeader)
        {
            return false;
        }

        Span<byte> payload = stackalloc byte[length];
        if (!_handshake.ReadReply(d.Slice(Wire.SecureAcceptHeader, length), payload, out var n) || n != 20 || BinaryPrimitives.ReadUInt32LittleEndian(payload[16..]) != BinaryPrimitives.ReadUInt32LittleEndian(d[9..]))
        {
            return false;
        }

        payload[..16].CopyTo(_resetToken);
        _handshake.Finish((ulong)_o.MaxPacketsPerKey + 1, out _send, out _receive);
        _remoteKey = _handshake.Remote;
        ChannelBinding = _handshake.ChannelBinding;
        _handshake = null;
        return true;
    }

    // Authenticate first, decrypt into the scratch buffer, and only then let the reliability layer see the packet.
    private void OnSecureData(ReadOnlySpan<byte> d, long now)
    {
        var pkt = BinaryPrimitives.ReadUInt32LittleEndian(d[5..]);
        if (!_receive!.IsFresh(pkt))
        {
            return; // a replay or a very old packet: no decryption spent
        }

        var bodyLength = d.Length - Wire.SecureAadSize - Wire.TagSize;
        var plain = _plain!;
        d[..Wire.SecureAadSize].CopyTo(plain);
        if (!_receive!.Open(pkt, d[..Wire.SecureAadSize], d[Wire.SecureAadSize..], plain.AsSpan(Wire.SecureAadSize, bodyLength)))
        {
            return;
        }

        if (!_confirmed)
        {
            _confirmed = true;
            Confirmed?.Invoke(this);
        }

        if (_core.OnPacket(plain.AsSpan(0, Wire.SecureAadSize + bodyLength), now) == PacketResult.Violation)
        {
            Close(DisconnectReason.ProtocolViolation, byRemote: false, 0, notify: true);
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
                if (!_confirmed)
                {
                    // A handshake nobody followed up (a replayed first message, a vanished client): drop it without a word.
                    if (now - _created >= _o.UnconfirmedTimeoutMs)
                    {
                        Close(DisconnectReason.ConnectTimeout, byRemote: false, 0, notify: false);
                    }

                    break;
                }

                if (now - _core.LastReceive >= _o.TimeoutMs)
                {
                    Close(DisconnectReason.Timeout, byRemote: false, 0, notify: true);
                    break;
                }

                for (var i = 0; i < SendBurst; i++)
                {
                    if (_secure && _core.TxPackets >= _o.MaxPacketsPerKey)
                    {
                        Close(DisconnectReason.PacketLimit, byRemote: false, 0, notify: true);
                        break;
                    }

                    var n = _core.Build(_tx, _connectionId, now);
                    if (n == 0)
                    {
                        break;
                    }

                    if (_secure)
                    {
                        n = Seal(n);
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

    // Turns the datagram the core built into the secure form in place: version 2 header, encrypted body, tag.
    private int Seal(int n)
    {
        _tx[0] = Wire.Header(Wire.KindData, secure: true);
        var pkt = BinaryPrimitives.ReadUInt32LittleEndian(_tx.AsSpan(5));
        _send!.Seal(pkt, _tx.AsSpan(0, Wire.SecureAadSize), _tx.AsSpan(Wire.SecureAadSize, n - Wire.SecureAadSize), _tx.AsSpan(Wire.SecureAadSize, n - Wire.SecureAadSize + Wire.TagSize));
        return n + Wire.TagSize;
    }

    // The server-side half of the handshake: the listener calls this after it validated the cookie, and again for a repeated response.
    internal void SendAccept()
    {
        if (_acceptDatagram is not null)
        {
            _socket.Send(_acceptDatagram, _remote);
            return;
        }

        Span<byte> a = stackalloc byte[Wire.AcceptSize];
        a[0] = Wire.Header(Wire.KindAccept);
        BinaryPrimitives.WriteUInt64LittleEndian(a[1..], _nonce);
        BinaryPrimitives.WriteUInt32LittleEndian(a[9..], _connectionId);
        _socket.Send(a, _remote);
    }

    private void SendHandshake(long now)
    {
        _lastHandshake = now;
        var d = _tx.AsSpan(0, Wire.MinHandshakeSize);
        d.Clear();
        d[0] = Wire.Header(_haveCookie ? Wire.KindResponse : Wire.KindRequest, _secure);
        BinaryPrimitives.WriteUInt64LittleEndian(d[1..], _nonce);
        if (_haveCookie)
        {
            _cookie.CopyTo(d[9..]);
            if (_secure)
            {
                if (_msg1Length == 0)
                {
                    Span<byte> bind = stackalloc byte[Wire.BindSize];
                    Wire.Bind(_nonce, _cookie, bind);
                    _handshake = SecureHandshake.Client(_security, _keyIndex, bind);
                    _msg1Length = _handshake.WriteFirst(default, _msg1!);
                }

                BinaryPrimitives.WriteUInt16LittleEndian(d[(9 + Wire.CookieSize)..], (ushort)_msg1Length);
                _msg1.AsSpan(0, _msg1Length).CopyTo(d[Wire.SecureResponseHeader..]);
            }
        }

        _socket.Send(d, _remote);
    }

    private void Close(DisconnectReason reason, bool byRemote, ushort code, bool notify)
    {
        if (State == ConnectionState.Closed)
        {
            return;
        }

        if (notify && (!_secure || _send is not null))
        {
            // Three copies: a single lost datagram would otherwise leave the peer waiting for its timeout.
            Span<byte> d = stackalloc byte[Wire.DisconnectSize + Wire.TagSize];
            d[0] = Wire.Header(Wire.KindDisconnect, _secure);
            BinaryPrimitives.WriteUInt32LittleEndian(d[1..], _connectionId);
            var length = Wire.DisconnectSize;
            if (_secure)
            {
                Span<byte> body = stackalloc byte[2];
                body[0] = (byte)reason;
                body[1] = (byte)code;
                _send!.SealClose(d[..5], body, d.Slice(5, 2 + Wire.TagSize));
                length = 5 + 2 + Wire.TagSize;
            }
            else
            {
                d[5] = (byte)reason;
                d[6] = (byte)code;
            }

            for (var i = 0; i < 3; i++)
            {
                _socket.Send(d[..length], _remote);
            }
        }

        State = ConnectionState.Closed;
        CloseInfo = new DisconnectInfo(reason, byRemote, code);
        _send?.Clear();
        _receive?.Clear();
        if (_ownsSocket)
        {
            _socket.Dispose();
        }

        Disconnected?.Invoke(this, CloseInfo);
    }
}
