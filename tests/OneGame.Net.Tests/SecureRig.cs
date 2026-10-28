using System.Net;
using System.Net.Sockets;
using System.Text;
using OneGame.Core;

namespace OneGame.Net.Tests;

/// <summary>An in-memory message connection pair with a hook on every message, so tests can tamper, repeat, inject and drop.</summary>
internal sealed class MemConn : INetConnection
{
    private readonly Queue<byte[]> _in = new();
    private readonly Stack<byte[]> _pool = new();
    private byte[]? _lent;
    private MemConn? _peer;

    /// <summary>Sees each message this end sends and returns what the peer receives (zero, one or more messages).</summary>
    public Func<byte[], IEnumerable<byte[]>>? Tap { get; set; }

    public List<byte[]> Sent { get; } = [];

    public bool KeepSent { get; set; }

    public int PendingCount => _in.Count;

    public ConnectionState State { get; private set; } = ConnectionState.Connected;

    public DisconnectInfo CloseInfo { get; private set; }

    public string RemoteName => "mem";

    public NetStats Stats => default;

    public event Action<INetConnection>? Connected { add { } remove { } }

    public event Action<INetConnection, DisconnectInfo>? Disconnected;

    public static (MemConn A, MemConn B) Pair()
    {
        var a = new MemConn();
        var b = new MemConn();
        a._peer = b;
        b._peer = a;
        return (a, b);
    }

    public void Send(ReadOnlySpan<byte> message)
    {
        if (State != ConnectionState.Connected)
        {
            return;
        }

        if (Tap is null && !KeepSent)
        {
            var buf = _pool.Count > 0 ? _pool.Pop() : new byte[65536 + 16];
            message.CopyTo(buf);
            _peer!._in.Enqueue(buf);
            _peer._lens.Enqueue(message.Length);
            return;
        }

        var copy = message.ToArray();
        if (KeepSent)
        {
            Sent.Add(copy);
        }

        foreach (var m in Tap is null ? [copy] : Tap(copy))
        {
            _peer!._in.Enqueue(m);
            _peer._lens.Enqueue(-1);
        }
    }

    private readonly Queue<int> _lens = new();

    public bool TryReceive(out ReadOnlySpan<byte> message)
    {
        if (_lent is not null)
        {
            _pool.Push(_lent);
            _lent = null;
        }

        if (_in.Count == 0)
        {
            message = default;
            return false;
        }

        var m = _in.Dequeue();
        var len = _lens.Count > 0 ? _lens.Dequeue() : -1;
        if (len >= 0)
        {
            _lent = m;
            message = m.AsSpan(0, len);
        }
        else
        {
            message = m;
        }

        return true;
    }

    public void Pump()
    {
    }

    public void Disconnect(DisconnectReason reason = DisconnectReason.Normal, bool flush = false)
    {
        if (State == ConnectionState.Closed)
        {
            return;
        }

        State = ConnectionState.Closed;
        CloseInfo = new DisconnectInfo(reason, false);
        Disconnected?.Invoke(this, CloseInfo);
        _peer?.RemoteClosed(reason);
    }

    private void RemoteClosed(DisconnectReason reason)
    {
        if (State == ConnectionState.Closed)
        {
            return;
        }

        State = ConnectionState.Closed;
        CloseInfo = new DisconnectInfo(DisconnectReason.Normal, true, 1000);
        Disconnected?.Invoke(this, CloseInfo);
    }
}

internal static class SecureRig
{
    internal static readonly NetKeyPair ServerKey = NetKeyPair.FromPrivateKey(Pattern(32, 1));
    internal static readonly NetKeyPair PlayerKey = NetKeyPair.FromPrivateKey(Pattern(32, 2));
    internal static readonly NetKeyPair OtherKey = NetKeyPair.FromPrivateKey(Pattern(32, 3));

    internal static NetSecurity ServerSec(string game = "test-game", params NetKeyPair[] keys) => NetSecurity.ForServer(keys.Length == 0 ? ServerKey : keys[0], game, keys.Skip(1).ToArray());

    internal static NetSecurity ClientSec(string game = "test-game", params PeerKey[] trusted) => NetSecurity.ForClient(trusted.Length == 0 ? [ServerKey.PublicKey] : trusted, game, PlayerKey);

    internal static WebSocketOptions Opt(NetSecurity sec, int maxMessage = 1 << 20) => new() { Security = sec, HandshakeTimeoutMs = 2000, MaxMessageBytes = maxMessage };

    internal static (SecureConnection Server, MemConn Wire) NewServer()
    {
        var (a, b) = MemConn.Pair();
        var clock = NetClock.System;
        return (SecureConnection.ForServer(b, ServerSec(), Opt(ServerSec()), clock, new HandshakeBudget(clock, 1_000_000, 1_000_000)), a);
    }

    // A hand-driven client: speaks the handshake itself so tests can send exactly the records they want.
    internal sealed class Hand
    {
        public readonly MemConn Wire;
        public readonly SecureConnection Server;
        public NoiseCipherState Send = null!;
        public NoiseCipherState Recv = null!;
        public byte[] First = [];

        public Hand(NetSecurity? server = null, int serverMax = 1 << 20)
        {
            var (a, b) = MemConn.Pair();
            Wire = a;
            var clock = NetClock.System;
            var sec = server ?? ServerSec();
            Server = SecureConnection.ForServer(b, sec, Opt(sec, serverMax), clock, new HandshakeBudget(clock, 200, 4));
            b.KeepSent = true;
            Wire.TryReceive(out var challenge);
            var hs = SecureHandshake.Client(ClientSec(), 0, Bind(challenge[1..]));
            var msg = new byte[1 + SecureHandshake.FirstMessageOverhead];
            msg[0] = SecureConnection.VersionByte;
            var n = hs.WriteFirst(default, msg.AsSpan(1));
            First = msg[..(1 + n)];
            Wire.Send(First);
            Server.Pump();
            if (!Wire.TryReceive(out var reply) || !hs.ReadReply(reply[1..], new byte[64], out _))
            {
                throw new InvalidOperationException("the test server did not complete the handshake");
            }

            hs.Finish(out Send, out Recv);
        }

        public void ConfirmNow()
        {
            Record(SecureConnection.FlagConfirm, default);
            Server.Pump();
        }

        public static byte[] Bind(ReadOnlySpan<byte> nonce)
        {
            var bind = new byte[3 + nonce.Length];
            bind[0] = (byte)'w';
            bind[1] = (byte)'s';
            bind[2] = SecureConnection.VersionByte;
            nonce.CopyTo(bind.AsSpan(3));
            return bind;
        }

        public void Record(byte flag, ReadOnlySpan<byte> body)
        {
            var plain = new byte[1 + body.Length];
            plain[0] = flag;
            body.CopyTo(plain.AsSpan(1));
            var sealedRecord = new byte[plain.Length + 16];
            Send.Encrypt(default, plain, sealedRecord);
            Wire.Send(sealedRecord);
        }
    }


    public static byte[] Pattern(int length, int seed)
    {
        var b = new byte[length];
        var x = (uint)seed * 2654435761u;
        for (var i = 0; i < b.Length; i++)
        {
            x = (x * 1664525) + 1013904223;
            b[i] = (byte)(x >> 24);
        }

        return b;
    }
}
