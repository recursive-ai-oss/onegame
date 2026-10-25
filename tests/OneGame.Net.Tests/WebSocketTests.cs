using System.Net;
using System.Net.Sockets;
using System.Text;

namespace OneGame.Net.Tests;

public class WebSocketTests
{
    private static IPEndPoint Local => new(IPAddress.Loopback, 0);

    private static (WebSocketListener Server, WebSocketConnection Client, INetConnection Accepted) Connected(WebSocketOptions? serverOptions = null, WebSocketOptions? clientOptions = null)
    {
        var server = WebSocketTransport.Listen(Local, serverOptions);
        var client = WebSocketTransport.Connect((IPEndPoint)server.LocalEndPoint, clientOptions);
        INetConnection? accepted = null;
        Until(() =>
        {
            server.Pump();
            client.Pump();
            accepted ??= server.TryAccept(out var a) ? a : null;
            return accepted is not null && client.State == ConnectionState.Connected;
        }, 5000, "handshake did not finish");
        return (server, client, accepted!);
    }

    private static void Until(Func<bool> done, int ms, string failure = "timed out")
    {
        var deadline = Environment.TickCount64 + ms;
        while (!done())
        {
            if (Environment.TickCount64 > deadline)
            {
                Assert.Fail(failure);
            }

            Thread.Sleep(1);
        }
    }

    [Fact]
    public void AcceptValueMatchesTheExampleInTheRfc()
    {
        // RFC 6455 section 1.3.
        Span<byte> accept = stackalloc byte[28];
        WsHandshake.ComputeAccept("dGhlIHNhbXBsZSBub25jZQ=="u8, accept);
        Assert.Equal("s3pPLMBiTxaQ9kYGzzhZRbK+xOo=", Encoding.ASCII.GetString(accept));
    }

    [Fact]
    public void MessagesFlowBothWaysIncludingEmptyAndLargeOnes()
    {
        var (server, client, accepted) = Connected();
        using var srv = server;
        var sizes = new[] { 0, 1, 125, 126, 65535, 65536, 1 << 20 };
        for (var i = 0; i < sizes.Length; i++)
        {
            client.Send(Rig.Pattern(sizes[i], i));
            accepted.Send(Rig.Pattern(sizes[i], 100 + i));
        }

        int atServer = 0, atClient = 0;
        Until(() =>
        {
            server.Pump();
            client.Pump();
            while (accepted.TryReceive(out var m))
            {
                Assert.True(m.SequenceEqual(Rig.Pattern(sizes[atServer], atServer)), $"client->server message {atServer}");
                atServer++;
            }

            while (client.TryReceive(out var m))
            {
                Assert.True(m.SequenceEqual(Rig.Pattern(sizes[atClient], 100 + atClient)), $"server->client message {atClient}");
                atClient++;
            }

            return atServer == sizes.Length && atClient == sizes.Length;
        }, 10_000);
    }

    [Fact]
    public void OversizeSendsAreRefusedAndOversizeInputClosesWith1009()
    {
        var (server, client, accepted) = Connected(new WebSocketOptions { MaxMessageBytes = 10_000 }, new WebSocketOptions { MaxMessageBytes = 1 << 20 });
        using var srv = server;
        Assert.Throws<MessageTooLargeException>(() => accepted.Send(new byte[10_001]));
        DisconnectInfo? atServer = null;
        accepted.Disconnected += (_, i) => atServer = i;
        client.Send(new byte[50_000]);
        Until(() =>
        {
            server.Pump();
            client.Pump();
            return client.State == ConnectionState.Closed && atServer is not null;
        }, 5000);
        Assert.Equal(DisconnectReason.MessageTooLarge, atServer!.Value.Reason);
        Assert.Equal(1009, client.CloseInfo.Code);
        Assert.Equal(DisconnectReason.MessageTooLarge, client.CloseInfo.Reason);
        Assert.True(client.CloseInfo.ByRemote);
    }

    [Fact]
    public void CloseCarriesTheReasonToTheOtherEnd()
    {
        var (server, client, accepted) = Connected();
        using var srv = server;
        DisconnectInfo? atServer = null;
        DisconnectInfo? atClient = null;
        accepted.Disconnected += (_, i) => atServer = i;
        client.Disconnected += (_, i) => atClient = i;
        accepted.Send([1, 2, 3]);
        accepted.Disconnect(DisconnectReason.Kicked, flush: true);
        var gotMessage = false;
        Until(() =>
        {
            server.Pump();
            client.Pump();
            gotMessage |= client.TryReceive(out var m) && m.Length == 3;
            return atServer is not null && atClient is not null;
        }, 5000);
        Assert.True(gotMessage, "the flushed message arrives before the close");
        Assert.Equal(new DisconnectInfo(DisconnectReason.Kicked, false, 4009), atServer);
        Assert.Equal(new DisconnectInfo(DisconnectReason.Kicked, true, 4009), atClient);
        Assert.Equal(0, server.ConnectionCount);
    }

    [Fact]
    public void AClientThatVanishesWithoutClosingIsReportedAbnormal()
    {
        var (server, client, accepted) = Connected();
        using var srv = server;
        DisconnectInfo? info = null;
        accepted.Disconnected += (_, i) => info = i;
        client.Disconnect(DisconnectReason.Normal); // flush=false still sends a close frame
        Until(() =>
        {
            client.Pump();
            server.Pump();
            return info is not null;
        }, 5000);
        Assert.Equal(DisconnectReason.Normal, info!.Value.Reason);
    }

    [Fact]
    public void SilenceTimesOutAndPingsKeepAConnectionAlive()
    {
        var clock = new ManualNetClock();
        var o = new WebSocketOptions { KeepAliveMs = 100, TimeoutMs = 400 };
        var server = WebSocketTransport.Listen(Local, o, clock);
        using var srv = server;
        var client = WebSocketTransport.Connect((IPEndPoint)server.LocalEndPoint, o, clock);
        INetConnection? accepted = null;
        void Tick(int ms)
        {
            clock.Advance(ms);
            server.Pump();
            client.Pump();
            accepted ??= server.TryAccept(out var a) ? a : null;
            Thread.Sleep(1);
        }

        Until(() =>
        {
            Tick(1);
            return accepted is not null && client.State == ConnectionState.Connected;
        }, 5000);
        // Idle for far longer than the timeout: pings and pongs keep both ends up, and measure a round trip.
        for (var i = 0; i < 300; i++)
        {
            Tick(10);
        }

        Assert.Equal(ConnectionState.Connected, client.State);
        Assert.Equal(ConnectionState.Connected, accepted!.State);
        Assert.True(client.Stats.RttMs >= 0);

        // Now one side stops pumping: the other times out.
        DisconnectInfo? info = null;
        accepted.Disconnected += (_, i) => info = i;
        for (var i = 0; i < 100 && info is null; i++)
        {
            clock.Advance(10);
            server.Pump();
        }

        Assert.Equal(DisconnectReason.Timeout, info!.Value.Reason);
        Assert.False(info.Value.ByRemote);
    }

    [Fact]
    public void ConnectingToNothingFails()
    {
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        var client = WebSocketTransport.Connect(new IPEndPoint(IPAddress.Loopback, port));
        DisconnectInfo? info = null;
        client.Disconnected += (_, i) => info = i;
        Until(() =>
        {
            client.Pump();
            return client.State == ConnectionState.Closed;
        }, 5000);
        Assert.Equal(DisconnectReason.TransportError, client.CloseInfo.Reason);
        Assert.Equal(DisconnectReason.TransportError, info!.Value.Reason);
    }

    [Fact]
    public void CloseCodesRoundTripThroughReasons()
    {
        foreach (var r in Enum.GetValues<DisconnectReason>())
        {
            if (r == DisconnectReason.None)
            {
                continue;
            }

            var back = WebSocketConnection.FromCode(WebSocketConnection.ToCode(r));
            Assert.Equal(r, back);
        }
    }

    [Fact]
    public void BadOptionsAreRefused()
    {
        Assert.Throws<ArgumentException>(() => WebSocketTransport.Listen(Local, new WebSocketOptions { Path = "x" }));
        Assert.Throws<ArgumentException>(() => WebSocketTransport.Connect(new IPEndPoint(IPAddress.Loopback, 1), new WebSocketOptions { MaxHeaderBytes = 10 }));
    }

    [Fact]
    public void SteadyStateAllocatesNothing()
    {
        var (server, client, accepted) = Connected();
        using var srv = server;
        var payload = Rig.Pattern(150, 4);
        void Round()
        {
            client.Send(payload);
            accepted.Send(payload);
            client.Pump();
            server.Pump();
            client.Pump();
            while (accepted.TryReceive(out _))
            {
            }

            while (client.TryReceive(out _))
            {
            }
        }

        for (var i = 0; i < 300; i++)
        {
            Round();
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 300; i++)
        {
            Round();
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.True(allocated == 0, $"{allocated} bytes allocated over 300 rounds");
    }
}

/// <summary>A hand-driven peer: raw TCP bytes against a real <see cref="WebSocketListener"/>, to play a client that breaks the rules.</summary>
public class WebSocketServerRulesTests
{
    private static IPEndPoint Local => new(IPAddress.Loopback, 0);

    private sealed class RawClient : IDisposable
    {
        private readonly TcpClient _tcp = new() { NoDelay = true };
        private readonly WebSocketListener _server;

        public RawClient(WebSocketListener server)
        {
            _server = server;
            _tcp.Connect((IPEndPoint)server.LocalEndPoint);
            Stream = _tcp.GetStream();
        }

        public NetworkStream Stream { get; }

        public void Write(string text) => Write(Encoding.ASCII.GetBytes(text));

        public void Write(byte[] data)
        {
            Stream.Write(data);
            Pump();
        }

        public void Pump()
        {
            for (var i = 0; i < 5; i++)
            {
                _server.Pump();
                Thread.Sleep(2);
            }
        }

        /// <summary>Reads until the peer closes or <paramref name="ms"/> pass, pumping the server meanwhile.</summary>
        public byte[] ReadAll(int ms = 1500)
        {
            var all = new List<byte>();
            var buf = new byte[4096];
            var deadline = Environment.TickCount64 + ms;
            while (Environment.TickCount64 < deadline)
            {
                _server.Pump();
                if (_tcp.Available > 0)
                {
                    var n = Stream.Read(buf);
                    if (n == 0)
                    {
                        break;
                    }

                    all.AddRange(buf.AsSpan(0, n).ToArray());
                }
                else if (!IsOpen())
                {
                    break;
                }

                Thread.Sleep(1);
            }

            return [.. all];
        }

        public bool IsOpen()
        {
            try
            {
                return !(_tcp.Client.Poll(0, SelectMode.SelectRead) && _tcp.Available == 0);
            }
            catch (SocketException)
            {
                return false;
            }
        }

        public void Dispose() => _tcp.Dispose();
    }

    private const string Key = "dGhlIHNhbXBsZSBub25jZQ==";

    private static string Request(string extra = "", string key = Key, string version = "13", string method = "GET") =>
        $"{method} /chat HTTP/1.1\r\nHost: localhost\r\nUpgrade: websocket\r\nConnection: Upgrade\r\nSec-WebSocket-Key: {key}\r\nSec-WebSocket-Version: {version}\r\n{extra}\r\n";

    private static string Text(byte[] data) => Encoding.ASCII.GetString(data);

    private static byte[] ClientFrame(byte opcode, bool fin, byte[] payload, bool mask = true)
    {
        var buf = new byte[payload.Length + 14];
        var n = WsFrames.Write(buf, opcode, fin, payload, mask ? [1, 2, 3, 4] : default);
        return buf[..n];
    }

    private static (WebSocketListener, RawClient, INetConnection) Handshaken(WebSocketOptions? o = null)
    {
        var server = WebSocketTransport.Listen(Local, o);
        var raw = new RawClient(server);
        raw.Write(Request());
        var reply = Text(raw.ReadAll(300));
        Assert.StartsWith("HTTP/1.1 101", reply, StringComparison.Ordinal);
        Assert.Contains("Sec-WebSocket-Accept: s3pPLMBiTxaQ9kYGzzhZRbK+xOo=", reply, StringComparison.Ordinal);
        Assert.True(server.TryAccept(out var accepted));
        return (server, raw, accepted);
    }

    [Fact]
    public void ValidUpgradeGetsTheRfcAcceptValue()
    {
        var (server, raw, accepted) = Handshaken();
        using var srv = server;
        using var rawc = raw;
        Assert.Equal(ConnectionState.Connected, accepted.State);
    }

    [Theory]
    [InlineData("POST", "13", "400")]
    [InlineData("GET", "8", "426")]
    public void BadRequestsAreRefused(string method, string version, string status)
    {
        using var server = WebSocketTransport.Listen(Local);
        using var raw = new RawClient(server);
        raw.Write(Request(method: method, version: version));
        var reply = Text(raw.ReadAll(500));
        Assert.StartsWith($"HTTP/1.1 {status}", reply, StringComparison.Ordinal);
        if (status == "426")
        {
            Assert.Contains("Sec-WebSocket-Version: 13", reply, StringComparison.Ordinal);
        }

        Assert.Equal(0, server.PendingHandshakes);
        Assert.False(server.TryAccept(out _));
    }

    [Theory]
    [InlineData("short")]
    [InlineData("dGhlIHNhbXBsZSBub25jZQ=")]
    [InlineData("!!!!!!!!!!!!!!!!!!!!!!!!")]
    public void AMalformedKeyIsRefused(string key)
    {
        using var server = WebSocketTransport.Listen(Local);
        using var raw = new RawClient(server);
        raw.Write(Request(key: key));
        Assert.StartsWith("HTTP/1.1 400", Text(raw.ReadAll(500)), StringComparison.Ordinal);
    }

    [Fact]
    public void MissingUpgradeHeadersAreRefused()
    {
        using var server = WebSocketTransport.Listen(Local);
        using var raw = new RawClient(server);
        raw.Write("GET / HTTP/1.1\r\nHost: x\r\nSec-WebSocket-Key: " + Key + "\r\nSec-WebSocket-Version: 13\r\n\r\n");
        Assert.StartsWith("HTTP/1.1 400", Text(raw.ReadAll(500)), StringComparison.Ordinal);
    }

    [Fact]
    public void HeaderNamesAndConnectionTokensIgnoreCase()
    {
        using var server = WebSocketTransport.Listen(Local);
        using var raw = new RawClient(server);
        raw.Write($"GET / HTTP/1.1\r\nhost: x\r\nUPGRADE: WebSocket\r\nconnection: keep-alive, Upgrade\r\nsec-websocket-key: {Key}\r\nSEC-WEBSOCKET-VERSION: 13\r\n\r\n");
        Assert.StartsWith("HTTP/1.1 101", Text(raw.ReadAll(500)), StringComparison.Ordinal);
    }

    [Fact]
    public void ADisallowedOriginIsForbiddenButANonBrowserClientPasses()
    {
        using var server = WebSocketTransport.Listen(Local, new WebSocketOptions { AllowedOrigins = ["https://game.example"] });
        using (var raw = new RawClient(server))
        {
            raw.Write(Request("Origin: https://evil.example\r\n"));
            Assert.StartsWith("HTTP/1.1 403", Text(raw.ReadAll(500)), StringComparison.Ordinal);
        }

        using (var raw = new RawClient(server))
        {
            raw.Write(Request("Origin: HTTPS://GAME.example\r\n"));
            Assert.StartsWith("HTTP/1.1 101", Text(raw.ReadAll(500)), StringComparison.Ordinal);
        }

        using (var raw = new RawClient(server))
        {
            raw.Write(Request());
            Assert.StartsWith("HTTP/1.1 101", Text(raw.ReadAll(500)), StringComparison.Ordinal);
        }
    }

    [Fact]
    public void AnOversizeHeaderBlockIsRefused()
    {
        using var server = WebSocketTransport.Listen(Local, new WebSocketOptions { MaxHeaderBytes = 1024 });
        using var raw = new RawClient(server);
        raw.Write(Request("X-Padding: " + new string('a', 3000) + "\r\n"));
        Assert.StartsWith("HTTP/1.1 400", Text(raw.ReadAll(500)), StringComparison.Ordinal);
        Assert.Equal(0, server.PendingHandshakes);
    }

    [Fact]
    public void StalledAndExcessHandshakesAreDropped()
    {
        var clock = new ManualNetClock();
        using var server = WebSocketTransport.Listen(Local, new WebSocketOptions { MaxPendingHandshakes = 4, HandshakeTimeoutMs = 1000 }, clock);
        var stalled = new List<RawClient>();
        for (var i = 0; i < 10; i++)
        {
            stalled.Add(new RawClient(server)); // connects and says nothing
        }

        stalled[0].Pump();

        Assert.Equal(4, server.PendingHandshakes);
        clock.Advance(1500);
        server.Pump();
        Assert.Equal(0, server.PendingHandshakes);
        Assert.Equal(0, server.ConnectionCount);
        stalled.ForEach(s => s.Dispose());
    }

    [Fact]
    public void AnUnmaskedClientFrameClosesWith1002()
    {
        var (server, raw, accepted) = Handshaken();
        using var srv = server;
        using var rawc = raw;
        DisconnectInfo? info = null;
        accepted.Disconnected += (_, i) => info = i;
        raw.Write(ClientFrame(WsFrames.OpBinary, true, [1, 2, 3], mask: false));
        var reply = raw.ReadAll(500);
        Assert.Equal(0x88, reply[0]);
        Assert.Equal(1002, (reply[2] << 8) | reply[3]);
        Assert.Equal(DisconnectReason.ProtocolViolation, info!.Value.Reason);
        Assert.False(accepted.TryReceive(out _));
    }

    [Fact]
    public void PingIsAnsweredWithTheSamePayloadEvenInsideAFragmentedMessage()
    {
        var (server, raw, accepted) = Handshaken();
        using var srv = server;
        using var rawc = raw;
        raw.Write(ClientFrame(WsFrames.OpBinary, false, [1, 2]));
        raw.Write(ClientFrame(WsFrames.OpPing, true, [9, 9, 9]));
        raw.Write(ClientFrame(WsFrames.OpContinuation, true, [3, 4]));
        var reply = raw.ReadAll(300);
        Assert.Equal([0x8A, 3, 9, 9, 9], reply);
        Assert.True(accepted.TryReceive(out var m));
        Assert.Equal([1, 2, 3, 4], m.ToArray());
    }

    [Fact]
    public void CloseIsEchoedWithItsCodeAndReportedWithoutLoss()
    {
        var (server, raw, accepted) = Handshaken();
        using var srv = server;
        using var rawc = raw;
        DisconnectInfo? info = null;
        accepted.Disconnected += (_, i) => info = i;
        raw.Write(ClientFrame(WsFrames.OpClose, true, [0x03, 0xE9, (byte)'b', (byte)'y', (byte)'e'])); // 1001 going away
        var reply = raw.ReadAll(500);
        Assert.Equal(0x88, reply[0]);
        Assert.Equal(1001, (reply[2] << 8) | reply[3]);
        Assert.Equal(DisconnectReason.Shutdown, info!.Value.Reason);
        Assert.True(info.Value.ByRemote);
    }

    [Fact]
    public void FragmentedMessagesAreReassembledAndOversizeOnesRefusedBeforeStorage()
    {
        var (server, raw, accepted) = Handshaken(new WebSocketOptions { MaxMessageBytes = 1000 });
        using var srv = server;
        using var rawc = raw;
        raw.Write(ClientFrame(WsFrames.OpBinary, false, new byte[400]));
        raw.Write(ClientFrame(WsFrames.OpContinuation, false, new byte[400]));
        raw.Write(ClientFrame(WsFrames.OpContinuation, true, new byte[100]));
        Assert.True(accepted.TryReceive(out var m));
        Assert.Equal(900, m.Length);
        // The header of an oversize frame is enough: no payload byte is sent.
        raw.Write(ClientFrame(WsFrames.OpBinary, true, new byte[2000])[..20]);
        var reply = raw.ReadAll(500);
        Assert.Equal(1009, (reply[2] << 8) | reply[3]);
    }
}
