using System.Linq;
using System.Net;
using OneGame.Core;

namespace OneGame.Net;

/// <summary>
/// Bounds the key-agreement work a WebSocket listener does for sockets that have not proved anything yet. A source is an IPv4
/// address or an IPv6 /64, so a host with a whole prefix is one source. Sources that completed an authenticated session recently
/// have their own lane, so a flood of strangers cannot lock out returning players.
/// </summary>
internal sealed class HandshakeBudget
{
    private const long KnownTtlMs = 10 * 60 * 1000;
    private readonly NetClock _clock;
    private readonly int _perSecond;
    private readonly int _perSource;
    private readonly Dictionary<string, int> _pendingBySource = [];
    private readonly Dictionary<string, long> _known = [];
    private long _window;
    private int _strangers;
    private int _returning;

    public HandshakeBudget(NetClock clock, int perSecond, int perSource)
    {
        _clock = clock;
        _perSecond = perSecond;
        _perSource = perSource;
        _window = clock.NowMs;
    }

    /// <summary>The source key of a peer address: the IPv4 address, or the first 64 bits of an IPv6 address.</summary>
    public static string SourceOf(string remoteName)
    {
        if (!IPEndPoint.TryParse(remoteName, out var ep))
        {
            return remoteName;
        }

        var bytes = ep.Address.GetAddressBytes();
        return bytes.Length == 16 ? Convert.ToHexString(bytes.AsSpan(0, 8)) : ep.Address.ToString();
    }

    private void Roll()
    {
        var now = _clock.NowMs;
        if (now - _window >= 1000)
        {
            _window = now;
            _strangers = 0;
            _returning = 0;
        }
    }

    /// <summary>Called before a socket is given a handshake. False: close it unanswered.</summary>
    public bool TryOpen(string source)
    {
        Roll();
        _pendingBySource.TryGetValue(source, out var n);
        if (n >= _perSource)
        {
            return false;
        }

        _pendingBySource[source] = n + 1;
        return true;
    }

    /// <summary>Called when a handshake ends, either way.</summary>
    public void Close(string source)
    {
        if (_pendingBySource.TryGetValue(source, out var n))
        {
            if (n <= 1)
            {
                _ = _pendingBySource.Remove(source);
            }
            else
            {
                _pendingBySource[source] = n - 1;
            }
        }
    }

    /// <summary>Called before any DH work for a first handshake message. False: drop the connection without spending it.</summary>
    public bool TryCharge(string source)
    {
        Roll();
        if (_known.TryGetValue(source, out var seen) && _clock.NowMs - seen < KnownTtlMs)
        {
            return ++_returning <= _perSecond;
        }

        return ++_strangers <= _perSecond;
    }

    /// <summary>A session from this source proved its key.</summary>
    public void Confirmed(string source)
    {
        if (_known.Count > 4096 && !_known.ContainsKey(source))
        {
            var now = _clock.NowMs;
            foreach (var k in _known.Where(p => now - p.Value >= KnownTtlMs).Select(p => p.Key).ToList())
            {
                _ = _known.Remove(k);
            }

            if (_known.Count > 4096)
            {
                return;
            }
        }

        _known[source] = _clock.NowMs;
    }
}

/// <summary>
/// Noise inside WebSocket binary messages (see <c>transport-security.md</c>, WebSocket). The server speaks first with a
/// <em>challenge</em>: the version byte and 16 random bytes. The client's handshake message binds the challenge, the transport tag and
/// the version byte into the Noise prologue, so a captured first message is worthless on any other connection (there is no cookie
/// on TCP; this gives the same freshness without a round trip the client would not wait for anyway). The server does not surface
/// the connection, and sends nothing, until the client's first record (a confirm) decrypts: a replayed first message never gets an
/// application-level connection. After the handshake every message is one record <c>AEAD(flag ‖ chunk)</c> of at most 65535 bytes
/// with an implicit counter; any record that is lost, repeated, reordered or injected fails authentication and ends the connection.
/// Flags: 0 ends a logical message, 1 continues it (never empty), 2 is a graceful close carrying the reason (nothing may follow it,
/// and it may not interrupt a message), 3 is the client's confirm (once, first). A WebSocket Close frame alone proves nothing, so a
/// connection that ends without flag 2 reports an unauthenticated end (<see cref="DisconnectReason.TransportError"/>, code 1006).
/// </summary>
/// <remarks>It wraps any <see cref="INetConnection"/> that keeps message boundaries, so the desktop socket and the browser's WebSocket share it.</remarks>
internal sealed class SecureConnection : INetConnection
{
    internal const byte VersionByte = 0x21;
    internal const int NonceSize = 16;
    internal const int MaxRecord = 65535;
    internal const int MaxChunk = MaxRecord - 16 - 1;
    internal const byte FlagLast = 0;
    internal const byte FlagMore = 1;
    internal const byte FlagClose = 2;
    internal const byte FlagConfirm = 3;
    private const int MaxHandshakeMessage = 1 + SecureHandshake.FirstMessageOverhead;

    private readonly NetSecurity _security;
    private readonly NetClock _clock;
    private readonly bool _client;
    private readonly int _maxMessage;
    private readonly int _timeoutMs;
    private readonly Func<INetConnection>? _open;
    private readonly HandshakeBudget? _budget;
    private readonly string _source = "";
    private readonly Queue<byte[]> _early = new();
    private readonly Queue<byte[]> _backlog = new();
    private INetConnection _inner;
    private SecureHandshake? _hs;
    private NoiseCipherState? _send;
    private NoiseCipherState? _recv;
    private byte[]? _sbuf;
    private byte[]? _rbuf;
    private byte[]? _asm;
    private byte[] _nonce = new byte[NonceSize];
    private int _asmLen;
    private bool _asmDone;
    private long _start;
    private int _keyIndex;
    private int _keysTried;
    private ConnectionState _state = ConnectionState.Connecting;
    private Stage _stage;
    private bool _localClose;
    private bool _authClose;
    private bool _confirmed;
    private DisconnectReason _peerReason;
    private DisconnectReason _failReason;
    private PeerKey _remote;
    private long _messagesSent;
    private long _messagesReceived;

    private enum Stage : byte
    {
        ClientAwaitInner,
        ClientAwaitChallenge,
        ClientAwaitReply,
        ServerAwaitFirst,
        ServerAwaitConfirm,
        Open,
        Dead,
    }

    private SecureConnection(INetConnection inner, Func<INetConnection>? open, NetSecurity security, WebSocketOptions o, NetClock clock, HandshakeBudget? budget)
    {
        _inner = inner;
        _open = open;
        _security = security;
        _clock = clock;
        _client = open is not null;
        _maxMessage = o.MaxMessageBytes;
        _timeoutMs = o.HandshakeTimeoutMs;
        _budget = budget;
        _start = clock.NowMs;
        _stage = _client ? Stage.ClientAwaitInner : Stage.ServerAwaitFirst;
        _inner.Disconnected += OnInnerClosed;
        if (!_client)
        {
            _source = HandshakeBudget.SourceOf(inner.RemoteName);
            // The challenge is the only thing the server sends before the client has proved a key.
            Entropy.Fill(_nonce);
            var challenge = new byte[1 + NonceSize];
            challenge[0] = VersionByte;
            _nonce.CopyTo(challenge, 1);
            _inner.Send(challenge);
        }
    }

    /// <summary>Opens a connection with <paramref name="open"/> and secures it; a rejected key is retried on a fresh connection with the next trusted key.</summary>
    internal static SecureConnection ForClient(Func<INetConnection> open, NetSecurity security, WebSocketOptions o, NetClock clock) => new(open(), open, security, o, clock, null);

    /// <summary>Wraps a connection a listener accepted.</summary>
    internal static SecureConnection ForServer(INetConnection inner, NetSecurity security, WebSocketOptions o, NetClock clock, HandshakeBudget budget) => new(inner, null, security, o, clock, budget);

    /// <inheritdoc />
    public event Action<INetConnection>? Connected;

    /// <inheritdoc />
    public event Action<INetConnection, DisconnectInfo>? Disconnected;

    /// <inheritdoc />
    public ConnectionState State => _state;

    /// <inheritdoc />
    public DisconnectInfo CloseInfo { get; private set; }

    /// <inheritdoc />
    public string RemoteName => _inner.RemoteName;

    /// <inheritdoc />
    public bool IsEncrypted => true;

    /// <inheritdoc />
    public ReadOnlyMemory<byte> ChannelBinding { get; private set; }

    /// <inheritdoc />
    public PeerKey? RemoteKey => _remote.IsEmpty ? null : _remote;

    /// <inheritdoc />
    public NetStats Stats
    {
        get
        {
            var s = _inner.Stats;
            s.MessagesSent = _messagesSent;
            s.MessagesReceived = _messagesReceived;
            return s;
        }
    }

    /// <summary>True once the handshake finished and (on a server) the client's confirm arrived.</summary>
    internal bool IsOpen => _stage == Stage.Open;

    /// <inheritdoc />
    public void Send(ReadOnlySpan<byte> message)
    {
        if (message.Length > _maxMessage)
        {
            throw new MessageTooLargeException(message.Length, _maxMessage);
        }

        if (_state == ConnectionState.Connecting)
        {
            if (_stage != Stage.Dead && _early.Count < 1024)
            {
                _early.Enqueue(message.ToArray());
            }

            return;
        }

        if (_state != ConnectionState.Connected)
        {
            return;
        }

        var buf = _sbuf!;
        var offset = 0;
        do
        {
            var take = Math.Min(MaxChunk, message.Length - offset);
            var last = offset + take == message.Length;
            buf[0] = last ? FlagLast : FlagMore;
            message.Slice(offset, take).CopyTo(buf.AsSpan(1));
            SealAndSend(1 + take);
            offset += take;
        }
        while (offset < message.Length);
        _messagesSent++;
    }

    private void SealAndSend(int plainLength)
    {
        // Sealed in place: the record is the flag and chunk followed by the tag. The counter is implicit and advances per record.
        _send!.Encrypt(default, _sbuf.AsSpan(0, plainLength), _sbuf.AsSpan(0, plainLength + NoiseCipherState.TagSize));
        _inner.Send(_sbuf.AsSpan(0, plainLength + NoiseCipherState.TagSize));
    }

    /// <inheritdoc />
    public bool TryReceive(out ReadOnlySpan<byte> message)
    {
        if (_state == ConnectionState.Connecting)
        {
            Pump();
        }

        if (_backlog.TryDequeue(out var kept))
        {
            message = kept;
            return true;
        }

        return Read(out message);
    }

    // Takes the next whole message out of the inner link's records. Only this method decrypts what arrives after the handshake.
    // Authenticated peers still send untrusted bytes: every rule below holds after the tag verifies.
    private bool Read(out ReadOnlySpan<byte> message)
    {
        message = default;
        if (_asmDone)
        {
            _asmDone = false;
            _asmLen = 0;
        }

        while (_stage == Stage.Open && !_authClose && _inner.TryReceive(out var record))
        {
            if (record.Length is < 1 + NoiseCipherState.TagSize or > MaxRecord)
            {
                Fail(DisconnectReason.ProtocolViolation);
                return false;
            }

            var plainLength = record.Length - NoiseCipherState.TagSize;
            if (!_recv!.Decrypt(default, record, _rbuf.AsSpan(0, plainLength)))
            {
                // Tampered, repeated, injected, reordered or from another key: all the same to us, and fatal.
                Fail(DisconnectReason.ProtocolViolation);
                return false;
            }

            var flag = _rbuf![0];
            var body = _rbuf.AsSpan(1, plainLength - 1);
            switch (flag)
            {
                case FlagLast when _asmLen == 0:
                    _messagesReceived++;
                    message = body;
                    return true;
                case FlagLast or FlagMore:
                    // A continuation must carry bytes, so a stream of empty records cannot keep us busy without progress.
                    if (body.Length == 0 || (long)_asmLen + body.Length > _maxMessage)
                    {
                        Fail(body.Length == 0 ? DisconnectReason.ProtocolViolation : DisconnectReason.MessageTooLarge);
                        return false;
                    }

                    Append(body);
                    if (flag == FlagLast)
                    {
                        _messagesReceived++;
                        message = _asm.AsSpan(0, _asmLen);
                        _asmDone = true; // reset on the next call, once the caller is done with the span
                        return true;
                    }

                    break;
                case FlagClose when _asmLen == 0:
                    _authClose = true;
                    _peerReason = body.Length == 1 ? (DisconnectReason)body[0] : DisconnectReason.Normal;
                    _state = ConnectionState.Closing;
                    _inner.Disconnect(DisconnectReason.Normal);
                    return false;
                default:
                    // A close inside an unfinished message is a truncation; a repeated confirm or unknown flag is a violation.
                    Fail(DisconnectReason.ProtocolViolation);
                    return false;
            }
        }

        return false;
    }

    private void Append(ReadOnlySpan<byte> body)
    {
        var need = _asmLen + body.Length;
        if (_asm is null || _asm.Length < need)
        {
            var bigger = NetBuffers.Rent(Math.Max(need, 2 * MaxRecord));
            if (_asm is not null)
            {
                _asm.AsSpan(0, _asmLen).CopyTo(bigger);
                NetBuffers.Return(_asm);
            }

            _asm = bigger;
        }

        body.CopyTo(_asm.AsSpan(_asmLen));
        _asmLen += body.Length;
    }

    /// <inheritdoc />
    public void Disconnect(DisconnectReason reason = DisconnectReason.Normal, bool flush = false)
    {
        if (_state is ConnectionState.Closing or ConnectionState.Closed)
        {
            return;
        }

        _localClose = true;
        if (_stage == Stage.Open && _state == ConnectionState.Connected)
        {
            _sbuf![0] = FlagClose;
            _sbuf[1] = (byte)reason;
            SealAndSend(2);
            _state = ConnectionState.Closing;
        }

        _stage = Stage.Dead;
        _inner.Disconnect(reason, flush);
    }

    /// <inheritdoc />
    public void Pump()
    {
        if (_state == ConnectionState.Closed)
        {
            return;
        }

        _inner.Pump();
        if (_state == ConnectionState.Closed || _stage is Stage.Open or Stage.Dead)
        {
            return;
        }

        if (_clock.NowMs - _start >= _timeoutMs)
        {
            Fail(DisconnectReason.ConnectTimeout);
            return;
        }

        switch (_stage)
        {
            case Stage.ClientAwaitInner:
                if (_inner.State == ConnectionState.Connected)
                {
                    _stage = Stage.ClientAwaitChallenge;
                }

                break;
            case Stage.ClientAwaitChallenge:
                if (_inner.TryReceive(out var challenge))
                {
                    ClientChallenge(challenge);
                }

                break;
            case Stage.ClientAwaitReply:
                if (_inner.TryReceive(out var reply))
                {
                    ClientReply(reply);
                }

                break;
            case Stage.ServerAwaitFirst:
                if (_inner.TryReceive(out var first))
                {
                    ServerFirst(first);
                }

                break;
            case Stage.ServerAwaitConfirm:
                if (_inner.TryReceive(out var confirm))
                {
                    ServerConfirm(confirm);
                }

                break;
            default:
                break;
        }
    }

    // The prologue's transport-specific part: the transport tag, the version byte (so a flipped version or a replay on another
    // connection fails the handshake like a wrong key) and the server's challenge.
    private static byte[] Bind(ReadOnlySpan<byte> nonce)
    {
        var bind = new byte[2 + 1 + NonceSize];
        bind[0] = (byte)'w';
        bind[1] = (byte)'s';
        bind[2] = VersionByte;
        nonce.CopyTo(bind.AsSpan(3));
        return bind;
    }

    private void ClientChallenge(ReadOnlySpan<byte> m)
    {
        if (m.Length != 1 + NonceSize || m[0] != VersionByte)
        {
            // An Insecure server's first message is game data, not a challenge: refuse, having sent nothing in clear.
            Fail(DisconnectReason.SecurityRequired);
            return;
        }

        _hs = SecureHandshake.Client(_security, _keyIndex, Bind(m[1..]));
        var msg = new byte[MaxHandshakeMessage];
        msg[0] = VersionByte;
        var n = _hs.WriteFirst(default, msg.AsSpan(1));
        _inner.Send(msg.AsSpan(0, 1 + n));
        _stage = Stage.ClientAwaitReply;
    }

    private void ClientReply(ReadOnlySpan<byte> m)
    {
        if (m.Length < 1 || m[0] != VersionByte)
        {
            Fail(DisconnectReason.SecurityRequired);
            return;
        }

        if (m.Length > 1 + SecureHandshake.ReplyOverhead || !_hs!.ReadReply(m[1..], new byte[SecureHandshake.ReplyOverhead], out _))
        {
            Rejected(DisconnectReason.HandshakeFailed);
            return;
        }

        Complete();
        FlushEarly();
    }

    // The server did not accept the key we tried. With more trusted keys, start over on a fresh connection with the next one.
    private void Rejected(DisconnectReason reason)
    {
        if (_client && _keysTried + 1 < _security.TrustedServerKeys.Count)
        {
            _keysTried++;
            _keyIndex = (_keyIndex + 1) % _security.TrustedServerKeys.Count;
            _hs = null;
            var old = _inner;
            old.Disconnected -= OnInnerClosed;
            old.Disconnect(DisconnectReason.Normal);
            _inner = _open!();
            _inner.Disconnected += OnInnerClosed;
            _stage = Stage.ClientAwaitInner;
            _start = _clock.NowMs;
            return;
        }

        Fail(reason);
    }

    private void ServerFirst(ReadOnlySpan<byte> m1)
    {
        if (m1.Length < 1 || m1[0] != VersionByte)
        {
            // Not a secure handshake: a plaintext client or another version. Nothing is answered or read as game data.
            Fail(DisconnectReason.SecurityRequired);
            return;
        }

        // Cheap checks first; only then spend key agreement, and only within the budget.
        if (m1.Length != 1 + SecureHandshake.FirstMessageOverhead || !_budget!.TryCharge(_source))
        {
            Fail(m1.Length == 1 + SecureHandshake.FirstMessageOverhead ? DisconnectReason.ServerFull : DisconnectReason.HandshakeFailed);
            return;
        }

        _hs = SecureHandshake.Server(_security, Bind(_nonce));
        if (!_hs.ReadFirst(m1[1..], new byte[SecureHandshake.FirstMessageOverhead], out _))
        {
            Fail(DisconnectReason.HandshakeFailed);
            return;
        }

        var msg = new byte[1 + SecureHandshake.ReplyOverhead];
        msg[0] = VersionByte;
        var n = _hs.WriteReply(default, msg.AsSpan(1));
        _inner.Send(msg.AsSpan(0, 1 + n));
        Complete();
        _stage = Stage.ServerAwaitConfirm;
    }

    private void ServerConfirm(ReadOnlySpan<byte> record)
    {
        if (record.Length != 1 + NoiseCipherState.TagSize || !_recv!.Decrypt(default, record, _rbuf.AsSpan(0, 1)) || _rbuf![0] != FlagConfirm)
        {
            Fail(DisconnectReason.ProtocolViolation);
            return;
        }

        // Only now is the peer known to be live, so only now does the application hear of it.
        _confirmed = true;
        _stage = Stage.Open;
        _state = ConnectionState.Connected;
        _budget!.Close(_source);
        _budget.Confirmed(_source);
    }

    // Splits the handshake into the two record ciphers and records who the peer is. On a server the connection stays in its
    // handshake state until the confirm arrives.
    private void Complete()
    {
        _remote = _hs!.Remote;
        _hs.Finish(out _send, out _recv);
        ChannelBinding = _hs.ChannelBinding;
        _hs = null;
        _sbuf = NetBuffers.Rent(MaxRecord);
        _rbuf = NetBuffers.Rent(MaxRecord);
        if (_client)
        {
            // The confirm tells the server the key exchange came from a live client, so it can surface the connection. It goes
            // out before the application can send anything.
            _sbuf[0] = FlagConfirm;
            SealAndSend(1);
            _stage = Stage.Open;
            _state = ConnectionState.Connected;
            Connected?.Invoke(this);
        }
    }

    private void FlushEarly()
    {
        while (_early.Count > 0 && _state == ConnectionState.Connected)
        {
            Send(_early.Dequeue());
        }
    }

    private void Fail(DisconnectReason reason)
    {
        if (_stage == Stage.Dead)
        {
            return;
        }

        _failReason = reason;
        _stage = Stage.Dead;
        _hs = null;
        _inner.Disconnect(reason);
        if (_state != ConnectionState.Closed)
        {
            _state = ConnectionState.Closing;
        }
    }

    private void OnInnerClosed(INetConnection inner, DisconnectInfo info)
    {
        if (_state == ConnectionState.Closed || inner != _inner)
        {
            return;
        }

        // A server that refuses our key closes the socket during the handshake; with another key to try, try it.
        if (_client && _stage == Stage.ClientAwaitReply && _failReason == DisconnectReason.None && !_localClose && _keysTried + 1 < _security.TrustedServerKeys.Count)
        {
            Rejected(DisconnectReason.HandshakeFailed);
            return;
        }

        // Records that arrived before the close are still genuine; read them now so the authenticated close record (and its
        // reason) is seen, and keep the messages for the application's next TryReceive calls.
        if (_stage == Stage.Open)
        {
            while (Read(out var rest))
            {
                _backlog.Enqueue(rest.ToArray());
            }
        }

        var handshaking = _stage is Stage.ClientAwaitInner or Stage.ClientAwaitChallenge or Stage.ClientAwaitReply or Stage.ServerAwaitFirst or Stage.ServerAwaitConfirm;
        _state = ConnectionState.Closed;
        _stage = Stage.Dead;
        if (_failReason != DisconnectReason.None)
        {
            CloseInfo = new DisconnectInfo(_failReason, false);
        }
        else if (_authClose)
        {
            CloseInfo = new DisconnectInfo(_peerReason, true, info.Code);
        }
        else if (_localClose)
        {
            CloseInfo = info;
        }
        else if (handshaking && info.Reason is DisconnectReason.SecurityRequired or DisconnectReason.HandshakeFailed or DisconnectReason.ServerFull)
        {
            CloseInfo = info;
        }
        else
        {
            // Nothing authenticated ended this: a bare Close frame, a reset or a timeout. Game code treats it as a drop.
            CloseInfo = info.Reason is DisconnectReason.Timeout or DisconnectReason.ConnectTimeout or DisconnectReason.TransportError
                ? info
                : new DisconnectInfo(DisconnectReason.TransportError, false, 1006);
        }

        _hs = null;
        _send?.Clear();
        _recv?.Clear();
        if (!_confirmed)
        {
            _budget?.Close(_source);
        }

        if (_sbuf is not null)
        {
            NetBuffers.Return(_sbuf);
            _sbuf = null;
        }

        if (_rbuf is not null)
        {
            NetBuffers.Return(_rbuf);
            _rbuf = null;
        }

        if (_asm is not null)
        {
            NetBuffers.Return(_asm);
            _asm = null;
        }

        _early.Clear();
        Disconnected?.Invoke(this, CloseInfo);
    }
}

/// <summary>A listener whose connections complete the Noise handshake and the client's confirm before the application sees them.</summary>
internal sealed class SecureListener : INetListener
{
    private readonly INetListener _inner;
    private readonly NetSecurity _sec;
    private readonly WebSocketOptions _o;
    private readonly NetClock _clock;
    private readonly HandshakeBudget _budget;
    private readonly List<SecureConnection> _pending = [];
    private readonly List<SecureConnection> _open = [];
    private readonly Queue<SecureConnection> _accepted = new();

    internal SecureListener(INetListener inner, NetSecurity security, WebSocketOptions options, NetClock clock)
    {
        _inner = inner;
        _sec = security;
        _o = options;
        _clock = clock;
        _budget = new HandshakeBudget(clock, options.MaxHandshakesPerSecond, options.MaxPendingHandshakesPerSource);
    }

    /// <inheritdoc />
    public EndPoint LocalEndPoint => _inner.LocalEndPoint;

    /// <inheritdoc />
    public int ConnectionCount => _open.Count;

    /// <inheritdoc />
    public int PendingHandshakes => _pending.Count + _inner.PendingHandshakes;

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
        _inner.Pump();
        while (_inner.TryAccept(out var raw))
        {
            // Capacity is checked before anything is computed: unconfirmed sockets have their own small budget, per source and
            // in total, and do not count against MaxConnections.
            if (_pending.Count >= _o.MaxPendingHandshakes || !_budget.TryOpen(HandshakeBudget.SourceOf(raw.RemoteName)))
            {
                raw.Disconnect(DisconnectReason.ServerFull);
                continue;
            }

            _pending.Add(SecureConnection.ForServer(raw, _sec, _o, _clock, _budget));
        }

        for (var i = 0; i < _pending.Count; i++)
        {
            var c = _pending[i];
            c.Pump();
            if (c.IsOpen)
            {
                _pending.RemoveAt(i--);
                _open.Add(c);
                _accepted.Enqueue(c);
            }
            else if (c.State is ConnectionState.Closing or ConnectionState.Closed)
            {
                _pending.RemoveAt(i--);
            }
        }

        for (var i = 0; i < _open.Count; i++)
        {
            if (_open[i].State == ConnectionState.Closed)
            {
                _open.RemoveAt(i--);
            }
        }
    }

    /// <inheritdoc />
    public void Dispose() => _inner.Dispose();
}
