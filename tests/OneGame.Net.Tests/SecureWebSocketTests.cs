using System.Net;
using System.Net.Sockets;
using System.Text;
using OneGame.Core;
using static OneGame.Net.Tests.SecureRig;

namespace OneGame.Net.Tests;

public class SecureWebSocketTests
{
    private static (SecureConnection Client, SecureConnection Server, MemConn ClientRaw, MemConn ServerRaw) Open(Action<MemConn, MemConn>? tap = null, NetSecurity? server = null, NetSecurity? client = null, NetClock? clock = null, int serverMax = 1 << 20)
    {
        clock ??= NetClock.System;
        var (a, b) = MemConn.Pair();
        tap?.Invoke(a, b);
        var s = SecureConnection.ForServer(b, server ?? ServerSec(), Opt(server ?? ServerSec(), serverMax), clock, new HandshakeBudget(clock, 200, 4));
        var c = SecureConnection.ForClient(() => a, client ?? ClientSec(), Opt(client ?? ClientSec()), clock);
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
    public void HandshakeAuthenticatesBothSidesAndMessagesFlowAroundChunkBoundaries()
    {
        var (c, s, _, _) = Open();
        Assert.True(c.IsOpen && s.IsOpen);
        Assert.True(c.IsEncrypted && s.IsEncrypted);
        Assert.Equal(ServerKey.PublicKey, c.RemoteKey);
        Assert.Equal(PlayerKey.PublicKey, s.RemoteKey);
        var sizes = new[] { 0, 1, 1000, SecureConnection.MaxChunk - 1, SecureConnection.MaxChunk, SecureConnection.MaxChunk + 1, 2 * SecureConnection.MaxChunk, 700_000 };
        for (var i = 0; i < sizes.Length; i++)
        {
            c.Send(SecureRig.Pattern(sizes[i], i));
            Assert.True(s.TryReceive(out var m));
            Assert.True(m.SequenceEqual(SecureRig.Pattern(sizes[i], i)), $"size {sizes[i]} client to server");
            s.Send(SecureRig.Pattern(sizes[i], 50 + i));
            Assert.True(c.TryReceive(out var r));
            Assert.True(r.SequenceEqual(SecureRig.Pattern(sizes[i], 50 + i)), $"size {sizes[i]} server to client");
        }
    }

    [Fact]
    public void AWrongPinnedKeyFailsWithHandshakeFailedAndNoDataIsSent()
    {
        var (c, s, a, _) = Open(client: ClientSec("test-game", OtherKey.PublicKey));
        c.Send([9, 9, 9]);
        Assert.False(c.IsOpen);
        Assert.False(s.IsOpen);
        Assert.Equal(ConnectionState.Closed, a.State);
        Assert.Equal(DisconnectReason.HandshakeFailed, s.CloseInfo.Reason);
        Assert.False(s.TryReceive(out _));
    }

    [Fact]
    public void AGameIdMismatchNeverCompletes()
    {
        var (c, s, _, _) = Open(client: ClientSec("other-game"));
        Assert.False(c.IsOpen || s.IsOpen);
        Assert.Equal(DisconnectReason.HandshakeFailed, s.CloseInfo.Reason);
    }

    [Fact]
    public void TheServerSendsOnlyTheChallengeAndTheReplyAndSurfacesNothingBeforeTheConfirm()
    {
        var h = new Hand();
        h.Server.Pump();
        Assert.False(h.Server.IsOpen);
        Assert.Equal(ConnectionState.Connecting, h.Server.State);
        Assert.Equal(0, h.Wire.PendingCount);
        h.Record(SecureConnection.FlagConfirm, default);
        h.Server.Pump();
        Assert.True(h.Server.IsOpen);
        Assert.Equal(PlayerKey.PublicKey, h.Server.RemoteKey);
    }

    [Fact]
    public void ACapturedFirstMessageReplayedOnAnotherConnectionFails()
    {
        var h = new Hand();
        var (a2, b2) = MemConn.Pair();
        b2.KeepSent = true;
        var clock = NetClock.System;
        var s2 = SecureConnection.ForServer(b2, ServerSec(), Opt(ServerSec()), clock, new HandshakeBudget(clock, 200, 4));
        a2.Send(h.First); // the attacker's replay: valid bytes, different challenge
        s2.Pump();
        Assert.False(s2.IsOpen);
        Assert.Equal(DisconnectReason.HandshakeFailed, s2.CloseInfo.Reason);
        Assert.Single(b2.Sent); // nothing but the challenge ever went out
    }

    [Fact]
    public void AWrongFirstRecordInsteadOfTheConfirmEndsTheConnection()
    {
        var h = new Hand();
        h.Record(SecureConnection.FlagLast, [1, 2, 3]);
        h.Server.Pump();
        Assert.False(h.Server.IsOpen);
        Assert.Equal(DisconnectReason.ProtocolViolation, h.Server.CloseInfo.Reason);
    }

    private static Hand Confirmed(int serverMax = 1 << 20)
    {
        var h = new Hand(serverMax: serverMax);
        h.Record(SecureConnection.FlagConfirm, default);
        h.Server.Pump();
        Assert.True(h.Server.IsOpen);
        return h;
    }

    [Fact]
    public void RecordRulesStayStrictAfterAuthentication()
    {
        // An empty continuation, an empty one after a partial message, a second confirm, an unknown flag, a close inside an
        // unfinished message, and a record after a close: each is refused although every tag is valid.
        foreach (var rule in new[] { "empty-more", "second-confirm", "unknown-flag", "close-inside", "empty-last-after-more", "short-record" })
        {
            var h = Confirmed();
            switch (rule)
            {
                case "empty-more":
                    h.Record(SecureConnection.FlagMore, default);
                    break;
                case "second-confirm":
                    h.Record(SecureConnection.FlagConfirm, default);
                    break;
                case "unknown-flag":
                    h.Record(9, [1]);
                    break;
                case "close-inside":
                    h.Record(SecureConnection.FlagMore, [1, 2]);
                    h.Record(SecureConnection.FlagClose, [1]);
                    break;
                case "empty-last-after-more":
                    h.Record(SecureConnection.FlagMore, [1, 2]);
                    h.Record(SecureConnection.FlagLast, default);
                    break;
                default:
                    h.Wire.Send(new byte[10]);
                    break;
            }

            Assert.False(h.Server.TryReceive(out _), rule);
            Assert.Equal(ConnectionState.Closed, h.Server.State);
            Assert.Equal(DisconnectReason.ProtocolViolation, h.Server.CloseInfo.Reason);
        }
    }

    [Fact]
    public void NothingIsReadAfterAnAuthenticatedClose()
    {
        var h = Confirmed();
        h.Record(SecureConnection.FlagClose, [(byte)DisconnectReason.Kicked]);
        h.Record(SecureConnection.FlagLast, [7]);
        Assert.False(h.Server.TryReceive(out _));
        Assert.Equal(DisconnectReason.Kicked, h.Server.CloseInfo.Reason);
        Assert.True(h.Server.CloseInfo.ByRemote);
    }

    [Fact]
    public void ARunningTotalOverTheLimitIsRefusedBeforeItIsBuffered()
    {
        var h = Confirmed(serverMax: 100_000);
        var chunk = new byte[SecureConnection.MaxChunk];
        h.Record(SecureConnection.FlagMore, chunk);
        h.Record(SecureConnection.FlagMore, chunk);
        Assert.False(h.Server.TryReceive(out _));
        Assert.Equal(DisconnectReason.MessageTooLarge, h.Server.CloseInfo.Reason);
        Assert.Throws<MessageTooLargeException>(() => h.Server.Send(new byte[100_001]));
    }

    [Fact]
    public void TamperedRepeatedAndInjectedRecordsAreRejected()
    {
        foreach (var attack in new[] { "flip", "repeat", "inject", "truncate", "reorder" })
        {
            var (c, s, a, _) = Open();
            Assert.True(c.IsOpen && s.IsOpen);
            var held = (byte[]?)null;
            a.Tap = m => attack switch
            {
                "flip" => [FlipLast(m)],
                "repeat" => [m, m],
                "inject" => [m, SecureRig.Pattern(m.Length, 5)],
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

            // Whatever got through before the first bad record is genuine; nothing after it is read, and the connection ends.
            Assert.All(got, g => Assert.True(Convert.ToHexString(g) is "010203" or "040506", attack));
            Assert.True(got.Count <= 1, attack);
            Assert.NotEqual(ConnectionState.Connected, s.State);
            Assert.Equal(DisconnectReason.ProtocolViolation, s.CloseInfo.Reason);
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
    public void AnAuthenticatedCloseCarriesItsReasonAndABareCloseIsReportedAbnormal()
    {
        var (c, s, _, _) = Open();
        s.Disconnect(DisconnectReason.Kicked);
        Assert.False(c.TryReceive(out _));
        Assert.Equal(ConnectionState.Closed, c.State);
        Assert.Equal(new DisconnectInfo(DisconnectReason.Kicked, true, 1000), c.CloseInfo);

        var (_, s2, a2, _) = Open();
        a2.Disconnect(DisconnectReason.Normal); // a Close frame or reset nobody authenticated
        Assert.Equal(ConnectionState.Closed, s2.State);
        Assert.Equal(new DisconnectInfo(DisconnectReason.TransportError, false, 1006), s2.CloseInfo);
    }

    [Fact]
    public void SteadyStateSendAndReceiveAllocateNothing()
    {
        var (c, s, _, _) = Open();
        var msg = SecureRig.Pattern(1000, 3);
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
        var clock = NetClock.System;
        var s = SecureConnection.ForServer(b, ServerSec(), Opt(ServerSec()), clock, new HandshakeBudget(clock, 200, 4));
        var c = SecureConnection.ForClient(() => a, ClientSec(), Opt(ClientSec()), clock);
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
        var clock = NetClock.System;
        var s = SecureConnection.ForServer(b, ServerSec(), Opt(ServerSec()), clock, new HandshakeBudget(clock, 200, 4));
        a.Send("plain game message"u8);
        s.Pump();
        Assert.Equal(ConnectionState.Closed, b.State);
        Assert.Equal(DisconnectReason.SecurityRequired, s.CloseInfo.Reason);
        Assert.False(s.TryReceive(out _));
    }

    [Fact]
    public void ARequiredClientRefusesAPlaintextServerAndSendsNothing()
    {
        var (a, b) = MemConn.Pair();
        a.KeepSent = true;
        var c = SecureConnection.ForClient(() => a, ClientSec(), Opt(ClientSec()), NetClock.System);
        c.Send("game data"u8);
        c.Pump();
        b.Send("plain reply"u8); // an Insecure server's first message
        c.Pump();
        Assert.Equal(ConnectionState.Closed, c.State);
        Assert.Equal(DisconnectReason.SecurityRequired, c.CloseInfo.Reason);
        Assert.Empty(a.Sent);
    }

    [Fact]
    public void ASilentPeerTimesOutTheHandshake()
    {
        var (a, _) = MemConn.Pair();
        var clock = new ManualNetClock();
        var c = SecureConnection.ForClient(() => a, ClientSec(), Opt(ClientSec()), clock);
        c.Pump();
        clock.Advance(2500);
        c.Pump();
        Assert.Equal(ConnectionState.Closed, c.State);
        Assert.Equal(DisconnectReason.ConnectTimeout, c.CloseInfo.Reason);
    }

    [Fact]
    public void TheBudgetBoundsSourcesAndKeyAgreementAndKeepsALaneForReturningPeers()
    {
        var clock = new ManualNetClock();
        var budget = new HandshakeBudget(clock, 3, 2);
        Assert.True(budget.TryOpen("a"));
        Assert.True(budget.TryOpen("a"));
        Assert.False(budget.TryOpen("a"));
        Assert.True(budget.TryOpen("b"));
        budget.Close("a");
        Assert.True(budget.TryOpen("a"));
        Assert.True(budget.TryCharge("x") && budget.TryCharge("x") && budget.TryCharge("x"));
        Assert.False(budget.TryCharge("x")); // strangers are out of budget
        budget.Confirmed("friend");
        Assert.True(budget.TryCharge("friend")); // the returning lane is separate
        clock.Advance(1000);
        Assert.True(budget.TryCharge("x"));
        Assert.Equal(HandshakeBudget.SourceOf("[2001:db8:1:2:aaaa:bbbb:cccc:dddd]:80"), HandshakeBudget.SourceOf("[2001:db8:1:2:1::1]:99"));
        Assert.NotEqual(HandshakeBudget.SourceOf("[2001:db8:1:3::1]:99"), HandshakeBudget.SourceOf("[2001:db8:1:2::1]:99"));
        Assert.Equal("10.0.0.1", HandshakeBudget.SourceOf("10.0.0.1:5000"));
    }

    private static (INetListener Listener, INetConnection Client) RealPair(NetSecurity server, NetSecurity client, out Func<INetConnection?> accept)
    {
        var listener = WebSocketTransport.Listen(new IPEndPoint(IPAddress.Loopback, 0), Opt(server));
        var c = WebSocketTransport.Connect((IPEndPoint)listener.LocalEndPoint, Opt(client));
        INetConnection? accepted = null;
        accept = () => accepted ??= listener.TryAccept(out var x) ? x : null;
        return (listener, c);
    }

    // The same code over real sockets: the desktop WebSocket underneath, listener and client both secured.
    [Fact]
    public void WorksOverRealWebSocketsWithAListenerAndNoAllocationInSteadyState()
    {
        var (l, client) = RealPair(ServerSec(), ClientSec(), out var acceptFn);
        using var lis = l;
        client.Send("hello"u8);
        Spin(() =>
        {
            l.Pump();
            client.Pump();
            return acceptFn() is not null && client.State == ConnectionState.Connected;
        });
        var accepted = acceptFn()!;
        Assert.Equal(PlayerKey.PublicKey, accepted.RemoteKey);
        Assert.True(accepted.IsEncrypted && client.IsEncrypted);
        Spin(() =>
        {
            l.Pump();
            client.Pump();
            return accepted.TryReceive(out var hello) && Encoding.ASCII.GetString(hello) == "hello";
        });
        var msg = SecureRig.Pattern(1200, 4);
        for (var i = 0; i < 100; i++)
        {
            Round(l, client, accepted, msg);
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 500; i++)
        {
            Round(l, client, accepted, msg);
        }

        var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.True(bytes <= 0, $"{bytes} bytes over 500 rounds");
        client.Disconnect(DisconnectReason.Normal, flush: true);
        Spin(() =>
        {
            l.Pump();
            client.Pump();
            return client.State == ConnectionState.Closed && accepted.State == ConnectionState.Closed;
        });
        Assert.Equal(DisconnectReason.Normal, accepted.CloseInfo.Reason);
        Assert.True(accepted.CloseInfo.ByRemote);
    }

    private static void Round(INetListener l, INetConnection client, INetConnection server, byte[] msg)
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
    public void ARotatedServerKeyIsFoundByTryingTheNextTrustedKey()
    {
        // The server now answers only as the new key; the client still lists the old one first.
        var (l, client) = RealPair(ServerSec("test-game", OtherKey), ClientSec("test-game", ServerKey.PublicKey, OtherKey.PublicKey), out var acceptFn);
        using var lis = l;
        Spin(() =>
        {
            l.Pump();
            client.Pump();
            return acceptFn() is not null && client.State == ConnectionState.Connected;
        });
        Assert.Equal(OtherKey.PublicKey, client.RemoteKey);

        var (l2, client2) = RealPair(ServerSec("test-game", OtherKey), ClientSec("test-game", ServerKey.PublicKey), out _);
        using var _2 = l2;
        Spin(() =>
        {
            l2.Pump();
            client2.Pump();
            return client2.State == ConnectionState.Closed;
        });
        Assert.Equal(DisconnectReason.HandshakeFailed, client2.CloseInfo.Reason);
    }

    [Fact]
    public void AnInsecureClientAgainstARequiredListenerIsClosedAndNeverAccepted()
    {
        using var l = WebSocketTransport.Listen(new IPEndPoint(IPAddress.Loopback, 0), Opt(ServerSec()));
        var raw = WebSocketTransport.Connect((IPEndPoint)l.LocalEndPoint, new WebSocketOptions { Security = NetSecurity.Insecure });
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
    public void InsecureStillWorksAndNoChoiceAtAllIsAnError()
    {
        var o = new WebSocketOptions { Security = NetSecurity.Insecure };
        using var l = WebSocketTransport.Listen(new IPEndPoint(IPAddress.Loopback, 0), o);
        var c = WebSocketTransport.Connect((IPEndPoint)l.LocalEndPoint, o);
        c.Send("plain"u8);
        INetConnection? acc = null;
        Spin(() =>
        {
            l.Pump();
            c.Pump();
            acc ??= l.TryAccept(out var x) ? x : null;
            return acc is not null && acc.TryReceive(out var m) && Encoding.ASCII.GetString(m) == "plain";
        });
        Assert.False(c.IsEncrypted);
        Assert.Throws<SecurityNotConfiguredException>(() => WebSocketTransport.Listen(new IPEndPoint(IPAddress.Loopback, 0)));
        Assert.Throws<SecurityNotConfiguredException>(() => WebSocketTransport.Connect(new IPEndPoint(IPAddress.Loopback, 1)));
    }

    [Fact]
    public void ASourceCannotHoldMoreThanItsShareOfPendingHandshakes()
    {
        var o = new WebSocketOptions { Security = ServerSec(), MaxPendingHandshakesPerSource = 2, HandshakeTimeoutMs = 5000 };
        using var l = WebSocketTransport.Listen(new IPEndPoint(IPAddress.Loopback, 0), o);
        var conns = new List<INetConnection>();
        for (var i = 0; i < 5; i++)
        {
            conns.Add(WebSocketTransport.Connect((IPEndPoint)l.LocalEndPoint, new WebSocketOptions { Security = NetSecurity.Insecure }));
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
        using var l = WebSocketTransport.Listen(new IPEndPoint(IPAddress.Loopback, 0), Opt(ServerSec()));
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
        var opt = Opt(ClientSec());
        var client = WebSocketTransport.Connect(new IPEndPoint(IPAddress.Loopback, relayPort), new WebSocketOptions { Security = opt.Security, Host = "relay" });
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
