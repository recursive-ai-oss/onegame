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
    private readonly NetSecurity _security;
    private readonly bool _secure;
    private long _handshakeWindow = long.MinValue / 2;
    private int _handshakesInWindow;
    private int _reservedInWindow;
    private readonly Dictionary<SourceKey, int> _perSource = [];

    // Sources that finished a session lately keep a reserved share of the handshake budget (value: when they last confirmed).
    private readonly Dictionary<SourceKey, long> _knownSources = [];

    // A closed or timed-out handshake stays on this list for as long as its cookie could still be replayed, so a captured first
    // message cannot start a second handshake (value: when the entry may go).
    private readonly Dictionary<HandshakeKey, long> _tombstones = [];
    private int _unconfirmed;
    private long _lastSweep;
    private long _resetWindow;
    private int _resetsInWindow;
    private readonly Dictionary<SourceKey, int> _unconfirmedBySource = [];
    private const int MaxTombstones = 65_536;
    private const int MaxKnownSources = 4096;
    private const int MaxSourcesPerWindow = 8192;

    internal UdpListener(IDatagramSocket socket, bool ownsSocket, UdpOptions options, NetClock clock)
    {
        options.Validate();
        _security = NetSecurity.RequireForListening(options.Security, nameof(UdpOptions));
        _secure = _security.Mode == SecurityMode.Required;
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

    /// <summary>Secure handshakes dropped because more than <see cref="UdpOptions.MaxHandshakesPerSecond"/> arrived in a second.</summary>
    public long HandshakesRateLimited { get; private set; }

    /// <summary>Secure handshakes that failed to authenticate (a key the server does not hold, another game id, damage).</summary>
    public long HandshakesFailed { get; private set; }

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
                var key = new HandshakeKey(c.Nonce, c.Remote);
                _ = _byHandshake.Remove(key);
                _connections.RemoveAt(i--);
                if (_secure)
                {
                    ReleaseUnconfirmed(c);
                    if (_tombstones.Count < MaxTombstones)
                    {
                        _tombstones[key] = now + (2 * CookieEpochMs) + 1000;
                    }
                }
            }
        }

        if (_secure && now - _lastSweep >= 1000)
        {
            _lastSweep = now;
            SweepTables(now);
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
        var version = d[0] >> 4;
        if (version != (_secure ? Wire.VersionSecure : Wire.Version))
        {
            if (kind == Wire.KindRequest && d.Length >= Wire.MinHandshakeSize)
            {
                // Plaintext to a Required server and the reverse are the same mistake: say so, in the version the peer speaks.
                var known = version is Wire.Version or Wire.VersionSecure;
                SendReject(BinaryPrimitives.ReadUInt64LittleEndian(d[1..]), known ? DisconnectReason.SecurityRequired : DisconnectReason.VersionMismatch, known ? (byte)version : Wire.Version);
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
            case Wire.KindData when d.Length >= (_secure ? Wire.SecureAadSize + 14 + Wire.TagSize : ReliableCore.HeaderSize):
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
        _reply[0] = Wire.Header(Wire.KindChallenge, _secure);
        BinaryPrimitives.WriteUInt64LittleEndian(_reply.AsSpan(1), nonce);
        Wire.Cookie(_secret, _from, nonce, now / CookieEpochMs, _reply.AsSpan(9, Wire.CookieSize));
        _socket.Send(_reply, _from);
        ChallengesSent++;
    }

    private void SendUnknownConnection(uint connectionId)
    {
        if (_secure)
        {
            // Each reset costs an HMAC and goes to an address nobody has proven, so they are limited per second (they are only a hint).
            var second = _clock.NowMs / 1000;
            if (second != _resetWindow)
            {
                _resetWindow = second;
                _resetsInWindow = 0;
            }

            if (++_resetsInWindow > 200)
            {
                return;
            }

            // The reset token proves this is the server the connection was made with, and only that server can compute it.
            Span<byte> reset = stackalloc byte[Wire.ResetSize];
            reset[0] = Wire.Header(Wire.KindReset, secure: true);
            BinaryPrimitives.WriteUInt32LittleEndian(reset[1..], connectionId);
            SecureHandshake.ResetToken(_security.ServerKeys[0], connectionId, reset[5..]);
            _socket.Send(reset, _from);
            return;
        }

        Span<byte> r = stackalloc byte[Wire.DisconnectSize];
        r[0] = Wire.Header(Wire.KindDisconnect);
        BinaryPrimitives.WriteUInt32LittleEndian(r[1..], connectionId);
        r[5] = (byte)DisconnectReason.Shutdown;
        r[6] = 0;
        _socket.Send(r, _from);
    }

    private void SendReject(ulong nonce, DisconnectReason reason, byte version = 0)
    {
        Span<byte> r = stackalloc byte[Wire.RejectSize];
        r[0] = (byte)(((version == 0 ? (_secure ? Wire.VersionSecure : Wire.Version) : version) << 4) | Wire.KindReject);
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

        if (_secure && _tombstones.ContainsKey(new HandshakeKey(nonce, _from)))
        {
            DatagramsIgnored++; // a replay of a first message whose handshake already ended
            return;
        }

        if (_connections.Count - _unconfirmed >= _o.MaxConnections)
        {
            SendReject(nonce, DisconnectReason.ServerFull);
            return;
        }

        if (_secure && (_unconfirmed >= _o.MaxUnconfirmed || _unconfirmedBySource.GetValueOrDefault(SourceKey.Of(_from)) >= _o.MaxUnconfirmedPerSource))
        {
            HandshakesRateLimited++; // too many handshakes waiting for their first packet; the client repeats
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
        var source = SourceKey.Of(_from);
        UdpConnection.ServerSecrets? secrets = null;
        if (_secure)
        {
            // Each handshake costs several X25519 operations, so every budget is checked before any of them. Dropped, not queued: the client repeats.
            if (!SpendHandshakeBudget(source, now))
            {
                HandshakesRateLimited++;
                return;
            }

            secrets = Handshake(d, nonce, id, presented);
            if (secrets is null)
            {
                HandshakesFailed++;
                SendReject(nonce, DisconnectReason.HandshakeFailed);
                return;
            }
        }

        var conn = new UdpConnection(_socket, remote, id, nonce, _o, _clock, secrets);
        if (_secure)
        {
            _unconfirmed++;
            _unconfirmedBySource[source] = _unconfirmedBySource.GetValueOrDefault(source) + 1;
        }

        _byId.Add(id, conn);
        _byHandshake.Add(new HandshakeKey(nonce, remote), conn);
        _connections.Add(conn);
        if (secrets is null)
        {
            _accepted.Enqueue(conn);
        }
        else
        {
            // Handed out once the client's first authenticated packet arrives (a replayed first message never gets that far).
            conn.Confirmed += c =>
            {
                ReleaseUnconfirmed(c);
                if (_knownSources.Count < MaxKnownSources || _knownSources.ContainsKey(source))
                {
                    _knownSources[source] = _clock.NowMs;
                }

                _accepted.Enqueue(c);
            };
        }

        conn.SendAccept();
    }

    // Reads message 1 of a secure Response and builds the Accept datagram (with message 2) the connection will repeat if asked.
    private UdpConnection.ServerSecrets? Handshake(ReadOnlySpan<byte> d, ulong nonce, uint id, ReadOnlySpan<byte> cookie)
    {
        var length = BinaryPrimitives.ReadUInt16LittleEndian(d[(9 + Wire.CookieSize)..]);
        if (length < SecureHandshake.FirstMessageOverhead || length > d.Length - Wire.SecureResponseHeader)
        {
            return null;
        }

        Span<byte> bind = stackalloc byte[Wire.BindSize];
        Wire.Bind(nonce, cookie, bind);
        var hs = SecureHandshake.Server(_security, bind);
        var hello = new byte[length];
        if (!hs.ReadFirst(d.Slice(Wire.SecureResponseHeader, length), hello, out _))
        {
            return null;
        }

        // The reply's encrypted payload is the reset token and the connection id: the id is also in the clear header, and the client
        // compares the two, so a path attacker cannot steer the client to a connection id the server did not choose (review L6).
        var accept = new byte[Wire.SecureAcceptHeader + SecureHandshake.ReplyOverhead + 20];
        Span<byte> payload = stackalloc byte[20];
        SecureHandshake.ResetToken(_security.ServerKeys[0], id, payload[..16]);
        BinaryPrimitives.WriteUInt32LittleEndian(payload[16..], id);
        var n = hs.WriteReply(payload, accept.AsSpan(Wire.SecureAcceptHeader));
        accept[0] = Wire.Header(Wire.KindAccept, secure: true);
        BinaryPrimitives.WriteUInt64LittleEndian(accept.AsSpan(1), nonce);
        BinaryPrimitives.WriteUInt32LittleEndian(accept.AsSpan(9), id);
        BinaryPrimitives.WriteUInt16LittleEndian(accept.AsSpan(13), (ushort)n);
        hs.Finish((ulong)_o.MaxPacketsPerKey + 1, out var send, out var receive);
        return new UdpConnection.ServerSecrets(send, receive, hs.Remote, accept.AsSpan(0, Wire.SecureAcceptHeader + n).ToArray());
    }

    // Budgets: one per source (an IPv4 address or an IPv6 /64), one shared by strangers, and one reserved for sources that completed a
    // session lately. All counters restart every second; the per-source table is capped so a spray of spoofed sources cannot grow it.
    private bool SpendHandshakeBudget(SourceKey source, long now)
    {
        var second = now / 1000;
        if (second != _handshakeWindow)
        {
            _handshakeWindow = second;
            _handshakesInWindow = 0;
            _reservedInWindow = 0;
            _perSource.Clear();
        }

        var mine = _perSource.GetValueOrDefault(source);
        if (mine >= _o.MaxHandshakesPerSourcePerSecond || (mine == 0 && _perSource.Count >= MaxSourcesPerWindow))
        {
            return false;
        }

        if (_handshakesInWindow < _o.MaxHandshakesPerSecond)
        {
            _handshakesInWindow++;
        }
        else if (_knownSources.ContainsKey(source) && _reservedInWindow < _o.ReservedHandshakesPerSecond)
        {
            _reservedInWindow++;
        }
        else
        {
            return false;
        }

        _perSource[source] = mine + 1;
        return true;
    }

    private void ReleaseUnconfirmed(UdpConnection c)
    {
        if (!c.IsUnconfirmed)
        {
            return;
        }

        c.IsUnconfirmed = false;
        _unconfirmed--;
        var source = SourceKey.Of(c.Remote);
        if (_unconfirmedBySource.TryGetValue(source, out var n))
        {
            if (n <= 1)
            {
                _ = _unconfirmedBySource.Remove(source);
            }
            else
            {
                _unconfirmedBySource[source] = n - 1;
            }
        }
    }

    private void SweepTables(long now)
    {
        List<HandshakeKey>? old = null;
        foreach (var (key, until) in _tombstones)
        {
            if (until <= now)
            {
                (old ??= []).Add(key);
            }
        }

        if (old is not null)
        {
            foreach (var key in old)
            {
                _ = _tombstones.Remove(key);
            }
        }

        List<SourceKey>? stale = null;
        foreach (var (key, seen) in _knownSources)
        {
            if (now - seen > 10 * 60_000)
            {
                (stale ??= []).Add(key);
            }
        }

        if (stale is not null)
        {
            foreach (var key in stale)
            {
                _ = _knownSources.Remove(key);
            }
        }
    }

    private readonly record struct SourceKey(ulong High, ulong Low)
    {
        // The network a peer sits on: all of an IPv4 address, the first 64 bits of an IPv6 one (the part one subscriber controls).
        public static SourceKey Of(SocketAddress a)
        {
            var ip = ((IPEndPoint)new IPEndPoint(IPAddress.Any, 0).Create(a)).Address;
            Span<byte> bytes = stackalloc byte[16];
            if (!ip.TryWriteBytes(bytes, out var n))
            {
                return default;
            }

            return n == 4
                ? new SourceKey(0, BinaryPrimitives.ReadUInt32BigEndian(bytes))
                : new SourceKey(BinaryPrimitives.ReadUInt64BigEndian(bytes), 0);
        }
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
/// var client = UdpTransport.Connect(new IPEndPoint(IPAddress.Loopback, port));
/// client.Send("hello"u8);
/// INetConnection? server = null;
/// while (server is null)
/// {
///     client.Pump();
///     listener.Pump();
///     if (listener.TryAccept(out var accepted)) { server = accepted; }
/// }
///
/// ReadOnlySpan&lt;byte&gt; message;
/// while (!server.TryReceive(out message))
/// {
///     client.Pump();
///     listener.Pump();
/// }
///
/// Console.WriteLine(System.Text.Encoding.UTF8.GetString(message));   // hello
///
/// client.Disconnect();
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
