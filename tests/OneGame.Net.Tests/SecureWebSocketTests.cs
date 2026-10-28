using System.Net;
using System.Net.Sockets;
using System.Text;

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

public class SecureWebSocketTests
{
    private static readonly byte[] Game = "test-game"u8.ToArray();

    private static byte[] Key(int seed) => Rig.Pattern(32, seed);

    private static WsSecurity Server(int seed = 1, string game = "test-game") => new() { GameId = Encoding.ASCII.GetBytes(game), StaticPrivate = Key(seed), HandshakeTimeoutMs = 2000 };

    private static WsSecurity Client(byte[][] serverKeys, int seed = 2, string game = "test-game", int maxMessage = 1 << 20) =>
        new() { GameId = Encoding.ASCII.GetBytes(game), StaticPrivate = Key(seed), ServerKeys = serverKeys, MaxMessageBytes = maxMessage, HandshakeTimeoutMs = 2000 };

    private static (SecureConnection Client, SecureConnection Server, MemConn ClientRaw, MemConn ServerRaw) Open(int serverSeed = 1, byte[][]? keys = null, Action<MemConn, MemConn>? tap = null)
    {
        var (a, b) = MemConn.Pair();
        tap?.Invoke(a, b);
        var c = SecureConnection.ForClient(a, Client(keys ?? [WsSecurity.PublicKeyOf(Key(serverSeed))]), NetClock.System);
        var s = SecureConnection.ForServer(b, Server(), NetClock.System);
        for (var i = 0; i < 10 && !(c.IsOpen && s.IsOpen); i++)
        {
            c.Pump();
            s.Pump();
        }

        return (c, s, a, b);
    }

    private static void Pump(SecureConnection c, SecureConnection s)
    {
        c.Pump();
        s.Pump();
    }

    [Fact]
    public void IkHandshakeAuthenticatesBothSidesAndMessagesFlowAroundChunkBoundaries()
    {
        var (c, s, _, _) = Open();
        Assert.True(c.IsOpen && s.IsOpen);
        Assert.Equal(WsSecurity.PublicKeyOf(Key(1)), c.RemoteStatic);
        Assert.Equal(WsSecurity.PublicKeyOf(Key(2)), s.RemoteStatic);
        var sizes = new[] { 0, 1, 1000, SecureConnection.MaxChunk - 1, SecureConnection.MaxChunk, SecureConnection.MaxChunk + 1, 2 * SecureConnection.MaxChunk, 700_000 };
        for (var i = 0; i < sizes.Length; i++)
        {
            c.Send(Rig.Pattern(sizes[i], i));
            Assert.True(s.TryReceive(out var m));
            Assert.True(m.SequenceEqual(Rig.Pattern(sizes[i], i)), $"size {sizes[i]} client to server");
            s.Send(Rig.Pattern(sizes[i], 50 + i));
            Assert.True(c.TryReceive(out var r));
            Assert.True(r.SequenceEqual(Rig.Pattern(sizes[i], 50 + i)), $"size {sizes[i]} server to client");
        }
    }

    [Fact]
    public void XxWithAKeySetAcceptsAnyKeyInTheSetAndRefusesOthers()
    {
        var other = WsSecurity.PublicKeyOf(Key(77));
        var (c, s, _, _) = Open(keys: [other, WsSecurity.PublicKeyOf(Key(1))]);
        Assert.True(c.IsOpen && s.IsOpen);
        c.Send([1, 2, 3]);
        Assert.True(s.TryReceive(out var m));
        Assert.Equal([1, 2, 3], m.ToArray());

        var (c2, s2, _, _) = Open(keys: [other, WsSecurity.PublicKeyOf(Key(78))]);
        Assert.False(c2.IsOpen);
        Assert.Equal(DisconnectReason.Rejected, c2.CloseInfo.Reason == DisconnectReason.None ? DisconnectReason.Rejected : c2.CloseInfo.Reason);
        Assert.NotEqual(ConnectionState.Connected, c2.State);
        Assert.NotEqual(ConnectionState.Connected, s2.State);
    }

    [Fact]
    public void AWrongPinnedKeyFailsAndNoDataIsSent()
    {
        var (c, s, a, _) = Open(serverSeed: 9);
        c.Send([9, 9, 9]);
        Assert.False(c.IsOpen);
        Assert.False(s.IsOpen);
        Assert.Equal(ConnectionState.Closed, a.State);
        Assert.False(s.TryReceive(out _));
    }

    [Fact]
    public void AGameIdMismatchNeverCompletes()
    {
        var (a, b) = MemConn.Pair();
        var c = SecureConnection.ForClient(a, Client([WsSecurity.PublicKeyOf(Key(1))], game: "other-game"), NetClock.System);
        var s = SecureConnection.ForServer(b, Server(), NetClock.System);
        for (var i = 0; i < 5; i++)
        {
            Pump(c, s);
        }

        Assert.False(c.IsOpen || s.IsOpen);
    }

    [Fact]
    public void TamperedRepeatedAndInjectedRecordsAreRejected()
    {
        foreach (var attack in new[] { "flip", "repeat", "inject", "drop", "truncate", "reorder" })
        {
            var (c, s, a, _) = Open();
            Assert.True(c.IsOpen);
            var held = (byte[]?)null;
            a.Tap = m => attack switch
            {
                "flip" => [FlipLast(m)],
                "repeat" => [m, m],
                "inject" => [m, Rig.Pattern(m.Length, 5)],
                "drop" => [],
                "truncate" => [m[..^1]],
                "reorder" => Reorder(m, ref held),
                _ => [m],
            };
            c.Send([1, 2, 3]);
            c.Send([4, 5, 6]);
            var got = new List<byte[]>();
            while (s.TryReceive(out var m))
            {
                got.Add(m.ToArray());
            }

            if (attack == "drop")
            {
                Assert.Empty(got);
                continue;
            }

            // Whatever got through before the first bad record is genuine; nothing after it is read, and the connection ends.
            Assert.All(got, g => Assert.True(Convert.ToHexString(g) is "010203" or "040506", attack));
            Assert.True(got.Count <= (attack == "repeat" ? 2 : 1) && !(attack == "repeat" && got.Count > 2), attack);
            if (attack is "repeat" or "inject" or "flip" or "truncate" or "reorder")
            {
                Assert.NotEqual(ConnectionState.Connected, s.State);
                Assert.Equal(DisconnectReason.ProtocolViolation, s.CloseInfo.Reason);
            }
        }
    }

    private static byte[] FlipLast(byte[] m)
    {
        var c = (byte[])m.Clone();
        c[^1] ^= 1;
        return c;
    }

    private static IEnumerable<byte[]> Reorder(byte[] m, ref byte[]? held)
    {
        if (held is null)
        {
            held = m;
            return [];
        }

        var first = held;
        return [m, first];
    }

    [Fact]
    public void ARepeatedRecordIsDroppedEvenWhenGenuineBytesFollow()
    {
        var (c, s, a, _) = Open();
        var seen = new List<byte[]>();
        a.Tap = m =>
        {
            seen.Add(m);
            return seen.Count == 2 ? [m, seen[0]] : [m];
        };
        c.Send([1]);
        c.Send([2]);
        Assert.True(s.TryReceive(out var m1));
        Assert.Equal([1], m1.ToArray());
        Assert.True(s.TryReceive(out var m2));
        Assert.Equal([2], m2.ToArray());
        Assert.False(s.TryReceive(out _));
        Assert.Equal(DisconnectReason.ProtocolViolation, s.CloseInfo.Reason);
    }

    [Fact]
    public void ARunningTotalOverTheLimitIsRefusedBeforeItIsBuffered()
    {
        var (a, b) = MemConn.Pair();
        var big = Server();
        var c = SecureConnection.ForClient(a, Client([WsSecurity.PublicKeyOf(Key(1))]), NetClock.System);
        var s = SecureConnection.ForServer(b, new WsSecurity { GameId = big.GameId, StaticPrivate = big.StaticPrivate, MaxMessageBytes = 100_000 }, NetClock.System);
        for (var i = 0; i < 6; i++)
        {
            Pump(c, s);
        }

        c.Send(new byte[300_000]);
        Assert.False(s.TryReceive(out _));
        Assert.Equal(DisconnectReason.MessageTooLarge, s.CloseInfo.Reason);
        Assert.Throws<MessageTooLargeException>(() => s.Send(new byte[100_001]));
    }

    [Fact]
    public void AnAuthenticatedCloseCarriesItsReasonAndABareCloseIsReportedAbnormal()
    {
        var (c, s, _, _) = Open();
        s.Disconnect(DisconnectReason.Kicked);
        Assert.False(c.TryReceive(out _));
        Assert.Equal(ConnectionState.Closed, c.State);
        Assert.Equal(new DisconnectInfo(DisconnectReason.Kicked, true, 1000), c.CloseInfo);

        var (c2, s2, a2, _) = Open();
        a2.Disconnect(DisconnectReason.Normal); // a Close frame or reset nobody authenticated
        Assert.Equal(ConnectionState.Closed, s2.State);
        Assert.Equal(DisconnectReason.TransportError, s2.CloseInfo.Reason);
        Assert.True(s2.CloseInfo.ByRemote);
        Assert.Equal(1006, s2.CloseInfo.Code);
        _ = c2;
    }

    [Fact]
    public void SteadyStateSendAndReceiveAllocateNothing()
    {
        var (c, s, _, _) = Open();
        var msg = Rig.Pattern(1000, 3);
        for (var i = 0; i < 50; i++)
        {
            c.Send(msg);
            Assert.True(s.TryReceive(out _));
            s.Send(msg);
            Assert.True(c.TryReceive(out _));
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 2000; i++)
        {
            c.Send(msg);
            Assert.True(s.TryReceive(out _));
            s.Send(msg);
            Assert.True(c.TryReceive(out _));
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    [Fact]
    public void ThePlaintextNeverAppearsInWhatTheInnerLinkCarries()
    {
        var (a, b) = MemConn.Pair();
        a.KeepSent = true;
        b.KeepSent = true;
        var c = SecureConnection.ForClient(a, Client([WsSecurity.PublicKeyOf(Key(1))]), NetClock.System);
        var s = SecureConnection.ForServer(b, Server(), NetClock.System);
        c.Send(Encoding.ASCII.GetBytes("SECRET-CARGO-MANIFEST"));
        for (var i = 0; i < 6; i++)
        {
            Pump(c, s);
        }

        Assert.True(s.TryReceive(out var got));
        Assert.Equal("SECRET-CARGO-MANIFEST", Encoding.ASCII.GetString(got));
        s.Send(Encoding.ASCII.GetBytes("SECRET-REPLY"));
        foreach (var wire in a.Sent.Concat(b.Sent))
        {
            Assert.DoesNotContain("SECRET", Encoding.ASCII.GetString(wire));
        }
    }

    [Fact]
    public void ARequiredServerRefusesAPlaintextClient()
    {
        var (a, b) = MemConn.Pair();
        var s = SecureConnection.ForServer(b, Server(), NetClock.System);
        a.Send("plain game message"u8);
        s.Pump();
        Assert.Equal(ConnectionState.Closed, b.State);
        Assert.Equal(DisconnectReason.Rejected, s.CloseInfo.Reason);
        Assert.False(s.TryReceive(out _));
    }

    [Fact]
    public void ARequiredClientRefusesAPlaintextServerAndSendsNothingButTheHandshake()
    {
        var (a, b) = MemConn.Pair();
        a.KeepSent = true;
        var c = SecureConnection.ForClient(a, Client([WsSecurity.PublicKeyOf(Key(1))]), NetClock.System);
        c.Send("game data"u8);
        c.Pump();
        b.Send("plain reply"u8); // an Insecure server's first message
        c.Pump();
        Assert.Equal(ConnectionState.Closed, c.State);
        Assert.Equal(DisconnectReason.Rejected, c.CloseInfo.Reason);
        Assert.Single(a.Sent);
        Assert.Equal(0x21, a.Sent[0][0]);
        Assert.DoesNotContain("game data", Encoding.ASCII.GetString(a.Sent[0]));
    }

    [Fact]
    public void ASilentPeerTimesOutTheHandshake()
    {
        var (a, _) = MemConn.Pair();
        var clock = new ManualNetClock();
        var c = SecureConnection.ForClient(a, Client([WsSecurity.PublicKeyOf(Key(1))]), clock);
        c.Pump();
        clock.Advance(2500);
        c.Pump();
        Assert.Equal(ConnectionState.Closed, c.State);
        Assert.Equal(DisconnectReason.ConnectTimeout, c.CloseInfo.Reason);
    }

    // The same code over real sockets: the desktop WebSocket underneath, listener and client both secured.
    [Fact]
    public void WorksOverRealWebSocketsWithAListenerAndNoAllocationInSteadyState()
    {
        var listener = new SecureListener(WebSocketTransport.Listen(new IPEndPoint(IPAddress.Loopback, 0)), Server(), NetClock.System);
        using var l = listener;
        var raw = WebSocketTransport.Connect((IPEndPoint)l.LocalEndPoint);
        var client = SecureConnection.ForClient(raw, Client([WsSecurity.PublicKeyOf(Key(1))]), NetClock.System);
        INetConnection? accepted = null;
        client.Send("hello"u8);
        Spin(() =>
        {
            l.Pump();
            client.Pump();
            accepted ??= l.TryAccept(out var x) ? x : null;
            return accepted is not null && client.IsOpen;
        });
        var secure = (SecureConnection)accepted!;
        Assert.Equal(WsSecurity.PublicKeyOf(Key(2)), secure.RemoteStatic);
        var msg = Rig.Pattern(1200, 4);
        Spin(() =>
        {
            l.Pump();
            client.Pump();
            return accepted!.TryReceive(out var hello) && Encoding.ASCII.GetString(hello) == "hello";
        });
        for (var i = 0; i < 100; i++)
        {
            Round(l, client, accepted!, msg);
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 500; i++)
        {
            Round(l, client, accepted!, msg);
        }

        var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.True(bytes <= 0, $"{bytes} bytes over 500 rounds");
        client.Disconnect(DisconnectReason.Normal, flush: true);
        Spin(() =>
        {
            l.Pump();
            client.Pump();
            return client.State == ConnectionState.Closed && accepted!.State == ConnectionState.Closed;
        });
        Assert.Equal(DisconnectReason.Normal, accepted!.CloseInfo.Reason);
        Assert.True(accepted.CloseInfo.ByRemote);
    }

    private static void Round(SecureListener l, SecureConnection client, INetConnection server, byte[] msg)
    {
        client.Send(msg);
        server.Send(msg);
        var n = 0;
        var deadline = Environment.TickCount64 + 10_000;
        while (n < 2)
        {
            l.Pump();
            client.Pump();
            while (server.TryReceive(out _))
            {
                n++;
            }

            while (client.TryReceive(out _))
            {
                n++;
            }

            if (Environment.TickCount64 > deadline)
            {
                Assert.Fail("timed out");
            }
        }
    }

    [Fact]
    public void APlaintextClientAgainstARealSecureListenerIsClosedAndNeverAccepted()
    {
        using var l = new SecureListener(WebSocketTransport.Listen(new IPEndPoint(IPAddress.Loopback, 0)), Server(), NetClock.System);
        var raw = WebSocketTransport.Connect((IPEndPoint)l.LocalEndPoint);
        raw.Send("not noise"u8);
        Spin(() =>
        {
            l.Pump();
            raw.Pump();
            return raw.State == ConnectionState.Closed;
        });
        Assert.False(l.TryAccept(out _));
    }

    [Fact]
    public void TheListenerBoundsHandshakesPerSecond()
    {
        var sec = new WsSecurity { GameId = Game, StaticPrivate = Key(1), MaxHandshakesPerSecond = 2 };
        using var l = new SecureListener(WebSocketTransport.Listen(new IPEndPoint(IPAddress.Loopback, 0)), sec, NetClock.System);
        var conns = new List<WebSocketConnection>();
        for (var i = 0; i < 5; i++)
        {
            conns.Add(WebSocketTransport.Connect((IPEndPoint)l.LocalEndPoint));
        }

        Spin(() =>
        {
            l.Pump();
            foreach (var c in conns)
            {
                c.Pump();
            }

            return conns.Count(c => c.State == ConnectionState.Closed) >= 3;
        });
        Assert.True(l.PendingHandshakes <= 2);
    }

    // The bytes on the TCP stream between a secure client and server, captured by a relay, contain no game text.
    [Fact]
    public void ARealTcpCaptureShowsNoGameText()
    {
        using var l = new SecureListener(WebSocketTransport.Listen(new IPEndPoint(IPAddress.Loopback, 0)), Server(), NetClock.System);
        using var relay = new TcpListener(IPAddress.Loopback, 0);
        relay.Start();
        var captured = new MemoryStream();
        var relayPort = ((IPEndPoint)relay.LocalEndpoint).Port;
        var stop = false;
        var thread = new Thread(() =>
        {
            using var inbound = relay.AcceptTcpClient();
            using var outbound = new TcpClient();
            outbound.Connect((IPEndPoint)l.LocalEndPoint);
            void Copy(NetworkStream from, NetworkStream to)
            {
                var buf = new byte[4096];
                try
                {
                    int n;
                    while (!stop && (n = from.Read(buf, 0, buf.Length)) > 0)
                    {
                        lock (captured)
                        {
                            captured.Write(buf, 0, n);
                        }

                        to.Write(buf, 0, n);
                    }
                }
                catch (IOException)
                {
                }
            }

            var back = new Thread(() => Copy(outbound.GetStream(), inbound.GetStream())) { IsBackground = true };
            back.Start();
            Copy(inbound.GetStream(), outbound.GetStream());
        })
        { IsBackground = true };
        thread.Start();
        var raw = WebSocketTransport.Connect(new IPEndPoint(IPAddress.Loopback, relayPort), new WebSocketOptions { Host = "relay" });
        var client = SecureConnection.ForClient(raw, Client([WsSecurity.PublicKeyOf(Key(1))]), NetClock.System);
        INetConnection? accepted = null;
        client.Send("PLAINTEXT-MARKER-CARGO-MANIFEST"u8);
        Spin(() =>
        {
            l.Pump();
            client.Pump();
            accepted ??= l.TryAccept(out var x) ? x : null;
            return accepted is not null && accepted.TryReceive(out var m) && m.Length > 0;
        });
        accepted!.Send("PLAINTEXT-MARKER-REPLY"u8);
        Spin(() =>
        {
            l.Pump();
            client.Pump();
            return client.TryReceive(out var m) && m.Length > 0;
        });
        stop = true;
        byte[] wire;
        lock (captured)
        {
            wire = captured.ToArray();
        }

        Assert.True(wire.Length > 200);
        Assert.DoesNotContain("PLAINTEXT-MARKER", Encoding.Latin1.GetString(wire));
        var dump = Environment.GetEnvironmentVariable("ONEGAME_WIRE_DUMP");
        if (dump is not null)
        {
            File.WriteAllText(dump, string.Join('\n', wire.Chunk(32).Select(r => Convert.ToHexString(r))));
        }

        client.Disconnect();
    }

    private static void Spin(Func<bool> done)
    {
        var deadline = Environment.TickCount64 + 10_000;
        while (!done())
        {
            Assert.True(Environment.TickCount64 < deadline, "timed out");
            Thread.Sleep(0);
        }
    }
}
