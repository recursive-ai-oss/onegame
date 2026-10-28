using System.Net;
using OneGame.Core;

namespace OneGame.Net;

/// <summary>
/// What a secure message link needs: this side's static private key, the game it belongs to (bound into the handshake, so peers of
/// different games never complete one) and, on a client, the server keys it accepts.
/// </summary>
internal sealed class WsSecurity
{
    /// <summary>Names the game and its wire version; part of the handshake prologue.</summary>
    public required byte[] GameId { get; init; }

    /// <summary>This side's static private key (32 bytes).</summary>
    public required byte[] StaticPrivate { get; init; }

    /// <summary>Client only: the server public keys it will talk to. One key uses the one-round-trip IK pattern, several use XX and check the server's key against the set.</summary>
    public IReadOnlyList<byte[]> ServerKeys { get; init; } = [];

    /// <summary>The public key for a private key.</summary>
    internal static byte[] PublicKeyOf(byte[] privateKey)
    {
        var pub = new byte[32];
        X25519.PublicKey(privateKey, pub);
        return pub;
    }

    /// <summary>Largest logical message, checked on the running total of chunks before anything is buffered.</summary>
    public int MaxMessageBytes { get; init; } = 1 << 20;

    /// <summary>The Noise handshake must finish within this long of the connection starting.</summary>
    public int HandshakeTimeoutMs { get; init; } = 5000;

    /// <summary>Listener only: sockets allowed to sit in the Noise handshake at once.</summary>
    public int MaxPendingHandshakes { get; init; } = 32;

    /// <summary>Listener only: handshakes started per second across all sources; more are closed unanswered, so the key agreement work stays bounded.</summary>
    public int MaxHandshakesPerSecond { get; init; } = 200;
}

/// <summary>
/// Noise inside WebSocket binary messages. The first message each way is the handshake; after it every WebSocket message is one
/// record, <c>AEAD(flag ‖ chunk)</c> with at most 65535 bytes, the counter implicit (TCP keeps order, so any lost, repeated,
/// reordered or injected record fails authentication and ends the connection). Flag 0 ends a logical message, 1 continues it, 2 is
/// an authenticated close carrying the reason. A WebSocket Close frame alone proves nothing, so a connection that ends without
/// flag 2 reports an abnormal end (<see cref="DisconnectReason.TransportError"/>, code 1006).
/// </summary>
/// <remarks>It wraps any <see cref="INetConnection"/> that keeps message boundaries, so the desktop socket and the browser's WebSocket share it.</remarks>
internal sealed class SecureConnection : INetConnection
{
    internal const byte VersionNibble = 0x20;
    internal const byte PatternIk = 1;
    internal const byte PatternXx = 2;
    internal const int MaxRecord = 65535;
    internal const int MaxChunk = MaxRecord - 16 - 1;
    internal const byte FlagLast = 0;
    internal const byte FlagMore = 1;
    internal const byte FlagClose = 2;
    private const int MaxHandshakeMessage = 1024;

    private readonly INetConnection _inner;
    private readonly WsSecurity _sec;
    private readonly NetClock _clock;
    private readonly bool _client;
    private readonly long _start;
    private readonly Queue<byte[]> _early = new();
    private NoiseHandshake? _hs;
    private NoiseCipherState? _send;
    private NoiseCipherState? _recv;
    private byte[]? _sbuf;
    private byte[]? _rbuf;
    private byte[]? _asm;
    private int _asmLen;
    private bool _asmDone;
    private readonly Queue<byte[]> _backlog = new();
    private ConnectionState _state = ConnectionState.Connecting;
    private Stage _stage;
    private byte _pattern;
    private bool _localClose;
    private bool _authClose;
    private DisconnectReason _peerReason;
    private DisconnectReason _failReason;
    private long _messagesSent;
    private long _messagesReceived;

    private enum Stage : byte
    {
        ClientAwaitInner,
        ClientAwaitMsg2,
        ServerAwaitMsg1,
        ServerAwaitMsg3,
        Open,
        Dead,
    }

    private SecureConnection(INetConnection inner, WsSecurity security, NetClock clock, bool client)
    {
        _inner = inner;
        _sec = security;
        _clock = clock;
        _client = client;
        _start = clock.NowMs;
        _stage = client ? Stage.ClientAwaitInner : Stage.ServerAwaitMsg1;
        if (security.StaticPrivate.Length != 32)
        {
            throw new ArgumentException("The static private key must be 32 bytes.", nameof(security));
        }

        if (client && security.ServerKeys.Count == 0)
        {
            throw new ArgumentException("A client needs at least one server public key.", nameof(security));
        }

        _inner.Disconnected += OnInnerClosed;
        if (_inner.State == ConnectionState.Closed)
        {
            OnInnerClosed(_inner, _inner.CloseInfo);
        }
    }

    /// <summary>Wraps a connection this side opened.</summary>
    internal static SecureConnection ForClient(INetConnection inner, WsSecurity security, NetClock clock) => new(inner, security, clock, true);

    /// <summary>Wraps a connection a listener accepted.</summary>
    internal static SecureConnection ForServer(INetConnection inner, WsSecurity security, NetClock clock) => new(inner, security, clock, false);

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

    /// <summary>The peer's static public key, authenticated by the handshake; empty until it completes.</summary>
    internal byte[] RemoteStatic { get; private set; } = [];

    /// <summary>True once the handshake finished and records flow.</summary>
    internal bool IsOpen => _stage == Stage.Open;

    /// <inheritdoc />
    public void Send(ReadOnlySpan<byte> message)
    {
        if (message.Length > _sec.MaxMessageBytes)
        {
            throw new MessageTooLargeException(message.Length, _sec.MaxMessageBytes);
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
    private bool Read(out ReadOnlySpan<byte> message)
    {
        message = default;
        if (_asmDone)
        {
            _asmDone = false;
            _asmLen = 0;
        }

        if (_stage != Stage.Open)
        {
            return false;
        }

        while (_inner.TryReceive(out var record))
        {
            if (record.Length is < 1 + NoiseCipherState.TagSize or > MaxRecord)
            {
                Fail(DisconnectReason.ProtocolViolation);
                return false;
            }

            var plainLength = record.Length - NoiseCipherState.TagSize;
            if (!_recv!.Decrypt(default, record, _rbuf.AsSpan(0, plainLength)))
            {
                // Tampered, replayed, injected, reordered or from another key: all the same to us, and fatal.
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
                    if ((long)_asmLen + body.Length > _sec.MaxMessageBytes)
                    {
                        Fail(DisconnectReason.MessageTooLarge);
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
                case FlagClose:
                    _authClose = true;
                    _state = ConnectionState.Closing;
                    _peerReason = body.Length == 1 ? (DisconnectReason)body[0] : DisconnectReason.Normal;
                    _inner.Disconnect(DisconnectReason.Normal);
                    return false;
                default:
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
        if (_state == ConnectionState.Closed || _stage == Stage.Open || _stage == Stage.Dead)
        {
            return;
        }

        if (_clock.NowMs - _start >= _sec.HandshakeTimeoutMs)
        {
            Fail(DisconnectReason.ConnectTimeout);
            return;
        }

        switch (_stage)
        {
            case Stage.ClientAwaitInner:
                if (_inner.State == ConnectionState.Connected)
                {
                    StartClient();
                }

                break;
            case Stage.ClientAwaitMsg2:
                if (_inner.TryReceive(out var m2))
                {
                    ClientMsg2(m2);
                }

                break;
            case Stage.ServerAwaitMsg1:
                if (_inner.TryReceive(out var m1))
                {
                    ServerMsg1(m1);
                }

                break;
            case Stage.ServerAwaitMsg3:
                if (_inner.TryReceive(out var m3))
                {
                    ServerMsg3(m3);
                }

                break;
            default:
                break;
        }
    }

    private byte[] Prologue(byte pattern)
    {
        var tag = "onegame/2/ws"u8;
        var p = new byte[tag.Length + 1 + 1 + _sec.GameId.Length];
        tag.CopyTo(p);
        p[tag.Length] = pattern;
        p[tag.Length + 1] = (byte)_sec.GameId.Length;
        _sec.GameId.CopyTo(p, tag.Length + 2);
        return p;
    }

    private void StartClient()
    {
        // One pinned key: IK, a single round trip. A set: XX, and the key the server proves is checked against the set.
        _pattern = _sec.ServerKeys.Count == 1 ? PatternIk : PatternXx;
        _hs = _pattern == PatternIk
            ? new NoiseHandshake(NoisePattern.IK, true, Prologue(_pattern), _sec.StaticPrivate, _sec.ServerKeys[0])
            : new NoiseHandshake(NoisePattern.XX, true, Prologue(_pattern), _sec.StaticPrivate, default);
        var msg = new byte[1 + MaxHandshakeMessage];
        msg[0] = (byte)(VersionNibble | _pattern);
        var n = _hs.WriteMessage(default, msg.AsSpan(1));
        _inner.Send(msg.AsSpan(0, 1 + n));
        _stage = Stage.ClientAwaitMsg2;
    }

    private void ClientMsg2(ReadOnlySpan<byte> m2)
    {
        if (m2.Length > MaxHandshakeMessage || !_hs!.ReadMessage(m2, new byte[MaxHandshakeMessage], out _))
        {
            // A plaintext reply from an Insecure server lands here too, which is the downgrade refusal: nothing was ever sent in clear.
            Fail(DisconnectReason.Rejected);
            return;
        }

        if (_pattern == PatternXx)
        {
            var remote = _hs.RemoteStatic;
            var known = false;
            foreach (var k in _sec.ServerKeys)
            {
                known |= remote.SequenceEqual(k);
            }

            if (!known)
            {
                Fail(DisconnectReason.Rejected);
                return;
            }

            var m3 = new byte[MaxHandshakeMessage];
            var n = _hs.WriteMessage(default, m3);
            _inner.Send(m3.AsSpan(0, n));
        }

        Complete();
    }

    private void ServerMsg1(ReadOnlySpan<byte> m1)
    {
        if (m1.Length < 1 || (m1[0] & 0xF0) != VersionNibble)
        {
            // Not a secure handshake: a plaintext client, or an older one. The server never answers or reads it as game data.
            Fail(m1.Length >= 1 ? DisconnectReason.Rejected : DisconnectReason.ProtocolViolation);
            return;
        }

        _pattern = (byte)(m1[0] & 0x0F);
        if (_pattern is not (PatternIk or PatternXx) || m1.Length > 1 + MaxHandshakeMessage)
        {
            Fail(DisconnectReason.ProtocolViolation);
            return;
        }

        _hs = new NoiseHandshake(_pattern == PatternIk ? NoisePattern.IK : NoisePattern.XX, false, Prologue(_pattern), _sec.StaticPrivate, default);
        if (!_hs.ReadMessage(m1[1..], new byte[MaxHandshakeMessage], out _))
        {
            Fail(DisconnectReason.Rejected);
            return;
        }

        var msg = new byte[MaxHandshakeMessage];
        var n = _hs.WriteMessage(default, msg);
        _inner.Send(msg.AsSpan(0, n));
        if (_hs.IsComplete)
        {
            Complete();
        }
        else
        {
            _stage = Stage.ServerAwaitMsg3;
        }
    }

    private void ServerMsg3(ReadOnlySpan<byte> m3)
    {
        if (m3.Length > MaxHandshakeMessage || !_hs!.ReadMessage(m3, new byte[MaxHandshakeMessage], out _))
        {
            Fail(DisconnectReason.Rejected);
            return;
        }

        Complete();
    }

    private void Complete()
    {
        // The peer's static key is authenticated now: only its holder could finish the key agreements that mix it in.
        RemoteStatic = _hs!.RemoteStatic.ToArray();
        _hs.Split(out _send, out _recv);
        _hs = null;
        _sbuf = NetBuffers.Rent(MaxRecord);
        _rbuf = NetBuffers.Rent(MaxRecord);
        _stage = Stage.Open;
        _state = ConnectionState.Connected;
        if (_client)
        {
            Connected?.Invoke(this);
        }

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
        _hs?.Destroy();
        _hs = null;
        _inner.Disconnect(reason);
        if (_state != ConnectionState.Closed)
        {
            _state = ConnectionState.Closing;
        }
    }

    private void OnInnerClosed(INetConnection inner, DisconnectInfo info)
    {
        if (_state == ConnectionState.Closed)
        {
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
        else
        {
            // Nothing authenticated ended this: a bare Close frame, a reset or a timeout. Game code treats it as a drop.
            CloseInfo = info.Reason is DisconnectReason.Timeout or DisconnectReason.ConnectTimeout or DisconnectReason.TransportError
                ? info
                : new DisconnectInfo(DisconnectReason.TransportError, true, 1006);
        }

        _hs?.Destroy();
        _hs = null;
        _send?.Clear();
        _recv?.Clear();
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

/// <summary>A listener whose connections complete the Noise handshake before the application sees them.</summary>
internal sealed class SecureListener : INetListener
{
    private readonly INetListener _inner;
    private readonly WsSecurity _sec;
    private readonly NetClock _clock;
    private readonly List<SecureConnection> _pending = [];
    private readonly List<SecureConnection> _open = [];
    private readonly Queue<SecureConnection> _accepted = new();
    private long _windowStart;
    private int _windowCount;

    internal SecureListener(INetListener inner, WsSecurity security, NetClock clock)
    {
        _inner = inner;
        _sec = security;
        _clock = clock;
        _windowStart = clock.NowMs;
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
        var now = _clock.NowMs;
        if (now - _windowStart >= 1000)
        {
            _windowStart = now;
            _windowCount = 0;
        }

        while (_inner.TryAccept(out var raw))
        {
            // The budget bounds DH work an attacker can ask for with fresh sockets; over it, the socket is closed without a reply.
            if (_pending.Count >= _sec.MaxPendingHandshakes || _windowCount >= _sec.MaxHandshakesPerSecond)
            {
                raw.Disconnect(DisconnectReason.ServerFull);
                continue;
            }

            _windowCount++;
            _pending.Add(SecureConnection.ForServer(raw, _sec, _clock));
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
