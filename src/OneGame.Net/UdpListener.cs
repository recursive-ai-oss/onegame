using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using OneGame.Core;

namespace OneGame.Net;

/// <summary>
/// The server end of the reliable-UDP transport. A client's first datagram only earns it a small cookie challenge; the server keeps
/// no state for it until the client echoes the cookie, which proves it can receive at the address it claims. Only then does a
/// connection exist, so a flood of spoofed requests costs the server a few hash computations and no memory.
/// </summary>
public sealed class UdpListener : INetListener
{
    private const long CookieEpochMs = 10_000;

    private readonly IDatagramSocket _socket;
    private readonly bool _ownsSocket;
    private readonly UdpOptions _o;
    private readonly NetClock _clock;
    private readonly byte[] _secret = new byte[32];
    private readonly Dictionary<uint, UdpConnection> _byId = [];
    private readonly Dictionary<HandshakeKey, UdpConnection> _byHandshake = [];
    private readonly List<UdpConnection> _connections = [];
    private readonly Queue<UdpConnection> _accepted = new();
    private readonly byte[] _rx = new byte[2048];
    private readonly byte[] _reply = new byte[Wire.ChallengeSize];
    private readonly SocketAddress _from;

    internal UdpListener(IDatagramSocket socket, bool ownsSocket, UdpOptions options, NetClock clock)
    {
        options.Validate();
        _socket = socket;
        _ownsSocket = ownsSocket;
        _o = options;
        _clock = clock;
        _from = new SocketAddress(socket.LocalAddress.Family, socket.LocalAddress.Size);
        Entropy.Fill(_secret);
    }

    /// <inheritdoc />
    public EndPoint LocalEndPoint => new IPEndPoint(IPAddress.Any, 0).Create(_socket.LocalAddress);

    /// <inheritdoc />
    public int ConnectionCount => _connections.Count;

    /// <inheritdoc />
    public int PendingHandshakes => 0;

    /// <summary>Datagrams ignored: wrong version, too short, unknown connection, wrong source, or a bad cookie.</summary>
    public long DatagramsIgnored { get; private set; }

    /// <summary>Cookie challenges sent.</summary>
    public long ChallengesSent { get; private set; }

    /// <summary>The live connections, oldest first. Do not change the list.</summary>
    public IReadOnlyList<UdpConnection> Connections => _connections;

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
        var now = _clock.NowMs;
        for (var i = 0; i < _o.MaxDatagramsPerPump; i++)
        {
            var n = _socket.Receive(_rx, _from);
            if (n < 0)
            {
                break;
            }

            Dispatch(_rx.AsSpan(0, n), now);
        }

        for (var i = 0; i < _connections.Count; i++)
        {
            var c = _connections[i];
            c.Tick(now);
            if (c.State == ConnectionState.Closed)
            {
                _ = _byId.Remove(c.ConnectionId);
                _ = _byHandshake.Remove(new HandshakeKey(c.Nonce, c.Remote));
                _connections.RemoveAt(i--);
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var c in _connections.ToArray())
        {
            c.Disconnect(DisconnectReason.Shutdown);
        }

        _connections.Clear();
        _byId.Clear();
        _byHandshake.Clear();
        if (_ownsSocket)
        {
            _socket.Dispose();
        }
    }

    private void Dispatch(ReadOnlySpan<byte> d, long now)
    {
        if (d.Length < 1)
        {
            DatagramsIgnored++;
            return;
        }

        var kind = d[0] & 0xF;
        if (d[0] >> 4 != Wire.Version)
        {
            if (kind == Wire.KindRequest && d.Length >= Wire.MinHandshakeSize)
            {
                SendReject(BinaryPrimitives.ReadUInt64LittleEndian(d[1..]), DisconnectReason.VersionMismatch);
            }
            else
            {
                DatagramsIgnored++;
            }

            return;
        }

        switch (kind)
        {
            case Wire.KindRequest when d.Length >= Wire.MinHandshakeSize:
                SendChallenge(BinaryPrimitives.ReadUInt64LittleEndian(d[1..]), now);
                break;
            case Wire.KindResponse when d.Length >= Wire.MinHandshakeSize:
                OnResponse(d, now);
                break;
            case Wire.KindData when d.Length >= ReliableCore.HeaderSize:
            case Wire.KindDisconnect when d.Length >= Wire.DisconnectSize:
                var id = BinaryPrimitives.ReadUInt32LittleEndian(d[1..]);
                if (_byId.TryGetValue(id, out var c) && c.Remote.Equals(_from))
                {
                    c.HandleDatagram(d, now);
                }
                else
                {
                    DatagramsIgnored++;
                    if (kind == Wire.KindData && c is null)
                    {
                        // Nobody here has this connection: this server started again since the sender connected. Say so, or the sender
                        // keeps a dead connection open until its timeout. The reply is smaller than what triggered it, and a disconnect
                        // is never answered, so two peers cannot bounce it back and forth.
                        SendUnknownConnection(id);
                    }
                }

                break;
            default:
                DatagramsIgnored++;
                break;
        }
    }

    private void SendChallenge(ulong nonce, long now)
    {
        _reply[0] = Wire.Header(Wire.KindChallenge);
        BinaryPrimitives.WriteUInt64LittleEndian(_reply.AsSpan(1), nonce);
        Wire.Cookie(_secret, _from, nonce, now / CookieEpochMs, _reply.AsSpan(9, Wire.CookieSize));
        _socket.Send(_reply, _from);
        ChallengesSent++;
    }

    private void SendUnknownConnection(uint connectionId)
    {
        Span<byte> r = stackalloc byte[Wire.DisconnectSize];
        r[0] = Wire.Header(Wire.KindDisconnect);
        BinaryPrimitives.WriteUInt32LittleEndian(r[1..], connectionId);
        r[5] = (byte)DisconnectReason.Shutdown;
        r[6] = 0;
        _socket.Send(r, _from);
    }

    private void SendReject(ulong nonce, DisconnectReason reason)
    {
        Span<byte> r = stackalloc byte[Wire.RejectSize];
        r[0] = Wire.Header(Wire.KindReject);
        BinaryPrimitives.WriteUInt64LittleEndian(r[1..], nonce);
        r[9] = (byte)reason;
        _socket.Send(r, _from);
    }

    private void OnResponse(ReadOnlySpan<byte> d, long now)
    {
        var nonce = BinaryPrimitives.ReadUInt64LittleEndian(d[1..]);
        var presented = d.Slice(9, Wire.CookieSize);
        Span<byte> expected = stackalloc byte[Wire.CookieSize];
        var valid = false;
        var epoch = now / CookieEpochMs;
        for (var e = epoch; e >= epoch - 1 && !valid; e--)
        {
            Wire.Cookie(_secret, _from, nonce, e, expected);
            valid = HmacSha256.FixedTimeEquals(presented, expected);
        }

        if (!valid)
        {
            DatagramsIgnored++;
            return;
        }

        if (_byHandshake.TryGetValue(new HandshakeKey(nonce, _from), out var existing))
        {
            existing.SendAccept(); // the client missed our accept; say it again
            return;
        }

        if (_connections.Count >= _o.MaxConnections)
        {
            SendReject(nonce, DisconnectReason.ServerFull);
            return;
        }

        uint id;
        Span<byte> r = stackalloc byte[4];
        do
        {
            Entropy.Fill(r);
            id = BinaryPrimitives.ReadUInt32LittleEndian(r);
        }
        while (id == 0 || _byId.ContainsKey(id));

        var remote = Wire.Clone(_from);
        var conn = new UdpConnection(_socket, remote, id, nonce, _o, _clock);
        _byId.Add(id, conn);
        _byHandshake.Add(new HandshakeKey(nonce, remote), conn);
        _connections.Add(conn);
        _accepted.Enqueue(conn);
        conn.SendAccept();
    }

    private readonly struct HandshakeKey(ulong nonce, SocketAddress address) : IEquatable<HandshakeKey>
    {
        private readonly ulong _nonce = nonce;
        private readonly SocketAddress _address = address;

        public bool Equals(HandshakeKey other) => _nonce == other._nonce && _address.Equals(other._address);

        public override bool Equals(object? obj) => obj is HandshakeKey k && Equals(k);

        public override int GetHashCode() => HashCode.Combine(_nonce, _address.GetHashCode());
    }
}

/// <summary>
/// Creates reliable-UDP listeners and connections. Nothing runs on its own: call <see cref="INetConnection.Pump"/> on connections you
/// opened and <see cref="INetListener.Pump"/> on listeners, from the thread that owns them (a game loop).
/// </summary>
/// <example>
/// <code>
/// using var listener = UdpTransport.Listen(new IPEndPoint(IPAddress.Loopback, 0));
/// var port = ((IPEndPoint)listener.LocalEndPoint).Port;
/// using var client = (UdpConnection)UdpTransport.Connect(new IPEndPoint(IPAddress.Loopback, port));
/// client.Send("hello"u8);
/// INetConnection? server = null;
/// while (server is null)
/// {
///     client.Pump();
///     listener.Pump();
///     if (listener.TryAccept(out var accepted)) { server = accepted; }
/// }
///
/// listener.Pump();
/// if (server.TryReceive(out var message)) { Console.WriteLine(System.Text.Encoding.UTF8.GetString(message)); }
/// </code>
/// </example>
public static class UdpTransport
{
    /// <summary>Starts listening on an operating-system socket.</summary>
    /// <param name="bind">The local address and port.</param>
    /// <param name="options">Protocol settings; defaults if null.</param>
    /// <param name="clock">Time source; the real clock if null.</param>
    public static UdpListener Listen(IPEndPoint bind, UdpOptions? options = null, NetClock? clock = null)
        => new(new SystemDatagramSocket(bind), true, options ?? new UdpOptions(), clock ?? NetClock.System);

    /// <summary>Starts listening on a given socket (a memory or conditioned one). The listener does not dispose it.</summary>
    /// <param name="socket">The datagram socket, for example a <see cref="ConditionedDatagramSocket"/> that adds latency and loss in a test.</param>
    /// <param name="options">Protocol settings; defaults if null.</param>
    /// <param name="clock">Time source; the real clock if null.</param>
    public static UdpListener Listen(IDatagramSocket socket, UdpOptions? options = null, NetClock? clock = null)
        => new(socket, false, options ?? new UdpOptions(), clock ?? NetClock.System);

    /// <summary>Connects to a listener. The connection starts in <see cref="ConnectionState.Connecting"/>; messages sent now go out when it connects.</summary>
    /// <param name="remote">The server's address.</param>
    /// <param name="options">Protocol settings; defaults if null.</param>
    /// <param name="clock">Time source; the real clock if null.</param>
    public static UdpConnection Connect(IPEndPoint remote, UdpOptions? options = null, NetClock? clock = null)
    {
        ArgumentNullException.ThrowIfNull(remote);
        var bind = new IPEndPoint(remote.AddressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any, 0);
        return new UdpConnection(new SystemDatagramSocket(bind), true, remote.Serialize(), options ?? new UdpOptions(), clock ?? NetClock.System);
    }

    /// <summary>Connects through a given socket (a memory or conditioned one). The connection disposes it when it closes.</summary>
    /// <param name="socket">The datagram socket to send through.</param>
    /// <param name="remote">The server's address.</param>
    /// <param name="options">Protocol settings; defaults if null.</param>
    /// <param name="clock">Time source; the real clock if null.</param>
    public static UdpConnection Connect(IDatagramSocket socket, SocketAddress remote, UdpOptions? options = null, NetClock? clock = null)
    {
        ArgumentNullException.ThrowIfNull(socket);
        ArgumentNullException.ThrowIfNull(remote);
        return new UdpConnection(socket, true, remote, options ?? new UdpOptions(), clock ?? NetClock.System);
    }
}
