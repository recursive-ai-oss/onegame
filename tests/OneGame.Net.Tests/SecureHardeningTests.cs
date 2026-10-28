using System.Net;
using System.Net.Sockets;
using System.Text;
using OneGame.Core;

namespace OneGame.Net.Tests;

/// <summary>
/// Hostile-input and abuse cases for the encrypted transports: datagrams that no honest peer sends, forged control packets, key
/// aliases, and floods from one source. Each test says what an attacker tried.
/// </summary>
public class SecureHardeningTests
{
    private static readonly NetKeyPair ServerKey = NetKeyPair.FromPrivateKey(Rig.Pattern(32, 111));
    private static readonly NetKeyPair PlayerKey = NetKeyPair.FromPrivateKey(Rig.Pattern(32, 313));

    private static UdpOptions ServerOptions() => new() { Security = NetSecurity.ForServer(ServerKey, "hard/1") };

    private static UdpOptions ClientOptions(NetKeyPair? identity = null) => new() { Security = NetSecurity.ForClient([ServerKey.PublicKey], "hard/1", identity ?? PlayerKey) };

    private static byte[] Prologue(string game, byte[] bind)
    {
        var g = Encoding.UTF8.GetBytes(game);
        var p = new byte[9 + 1 + g.Length + 1 + bind.Length];
        Encoding.ASCII.GetBytes("onegame/2").CopyTo(p, 0);
        g.CopyTo(p, 10);
        bind.CopyTo(p, 10 + g.Length + 1);
        return p;
    }

    // ---- UDP: datagrams of every kind and length ----

    // A datagram shorter than its kind needs must be ignored, never read past its end. This walks every kind and length in every
    // state a connection passes through, on both ends, with and without encryption.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void NoDatagramOfAnyKindOrLengthThrowsInAnyConnectionState(bool secure)
    {
        var rig = new Rig();
        var serverSocket = rig.Net.CreateSocket();
        var serverOptions = secure ? ServerOptions() : new UdpOptions { Security = NetSecurity.Insecure };
        var clientOptions = secure ? ClientOptions() : new UdpOptions { Security = NetSecurity.Insecure };
        var server = UdpTransport.Listen(serverSocket, serverOptions, rig.Clock);
        var client = UdpTransport.Connect(rig.Net.CreateSocket(), serverSocket.LocalAddress, clientOptions, rig.Clock);

        void Throw(UdpConnection c, string state)
        {
            for (var version = 0; version < 4; version++)
            {
                for (var kind = 0; kind < 16; kind++)
                {
                    for (var length = 1; length <= 40; length++)
                    {
                        var d = new byte[length];
                        d[0] = (byte)((version << 4) | kind);
                        for (var i = 1; i < d.Length; i++)
                        {
                            d[i] = (byte)(i * 37);
                        }

                        var ex = Record.Exception(() => c.HandleDatagram(d, rig.Clock.NowMs));
                        Assert.True(ex is null, $"{state}, secure={secure}, version {version} kind {kind} length {length}: {ex}");
                    }
                }
            }
        }

        Assert.Equal(ConnectionState.Connecting, client.State);
        Throw(client, "client connecting");
        if (client.State == ConnectionState.Closed)
        {
            // A forged datagram may have ended the attempt (a Reject with the right nonce would); the checks below need a live one.
            client = UdpTransport.Connect(rig.Net.CreateSocket(), serverSocket.LocalAddress, clientOptions, rig.Clock);
        }

        rig.Pumps.Add(server.Pump);
        rig.Pumps.Add(client.Pump);
        INetConnection? accepted = null;
        Assert.True(rig.RunUntil(() => (accepted ??= server.TryAccept(out var a) ? a : null) is not null && client.State == ConnectionState.Connected, 20_000));
        Throw(client, "client connected");
        Throw((UdpConnection)accepted!, "server end");
    }

    // A peer that is not honest sends one byte of kind Accept to a client that is still connecting (the client reads the connection id
    // out of the datagram, so the length has to be checked first). A real UDP socket plays the server.
    [Fact]
    public void AOneByteAcceptFromTheServerAddressDoesNotCrashAConnectingClient()
    {
        using var fake = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        fake.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        var port = ((IPEndPoint)fake.LocalEndPoint!).Port;
        var client = UdpTransport.Connect(new IPEndPoint(IPAddress.Loopback, port), ClientOptions());
        EndPoint from = new IPEndPoint(IPAddress.Any, 0);
        var buffer = new byte[2048];
        client.Pump();
        fake.ReceiveTimeout = 2000;
        _ = fake.ReceiveFrom(buffer, ref from);
        foreach (var length in new[] { 1, 5, 8, 12 })
        {
            var d = new byte[length];
            d[0] = (2 << 4) | Wire.KindAccept;
            _ = fake.SendTo(d, from);
        }

        Thread.Sleep(50);
        client.Pump();
        Assert.Equal(ConnectionState.Connecting, client.State);
        client.Disconnect();
    }

    // ---- UDP: forged control datagrams ----

    private sealed class Lone
    {
        public Rig Rig { get; } = new();
        public TapSocket ServerTap { get; }
        public TapSocket ClientTap { get; }
        public UdpListener Server { get; }
        public UdpConnection Client { get; }
        public INetConnection? Accepted { get; set; }

        public Lone(UdpOptions? server = null, UdpOptions? client = null, bool pumpServer = true)
        {
            var serverSocket = Rig.Net.CreateSocket();
            ServerTap = new TapSocket(serverSocket);
            ClientTap = new TapSocket(Rig.Net.CreateSocket());
            Server = UdpTransport.Listen(ServerTap, server ?? ServerOptions(), Rig.Clock);
            Client = UdpTransport.Connect(ClientTap, serverSocket.LocalAddress, client ?? ClientOptions(), Rig.Clock);
            if (pumpServer)
            {
                Rig.Pumps.Add(Server.Pump);
            }

            Rig.Pumps.Add(Client.Pump);
        }

        public void Connect() => Assert.True(Rig.RunUntil(() => (Accepted ??= Server.TryAccept(out var a) ? a : null) is not null && Client.State == ConnectionState.Connected, 20_000));
    }

    private static byte[] Reject(ulong nonce, DisconnectReason reason)
    {
        var d = new byte[Wire.RejectSize];
        d[0] = (byte)((Wire.VersionSecure << 4) | Wire.KindReject);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(d.AsSpan(1), nonce);
        d[9] = (byte)reason;
        return d;
    }

    // Only a party that saw the client's first datagram knows its nonce. A Reject without it changes nothing; one with it can only end
    // that one attempt (the session above decides what to do next, see RemoteSession).
    [Fact]
    public void AForgedRejectNeedsTheClientsNonceAndOnlyEndsThatAttempt()
    {
        var p = new Lone(pumpServer: false);
        p.Rig.Step(5);
        var nonce = p.Client.Nonce;

        p.Client.HandleDatagram(Reject(nonce + 1, DisconnectReason.ServerFull), p.Rig.Clock.NowMs);
        Assert.Equal(ConnectionState.Connecting, p.Client.State);

        p.Client.HandleDatagram(Reject(nonce, DisconnectReason.ServerFull), p.Rig.Clock.NowMs);
        Assert.Equal(ConnectionState.Closed, p.Client.State);
        Assert.Equal(DisconnectReason.ServerFull, p.Client.CloseInfo.Reason);
    }

    // The Accept carries the connection id twice: in the clear header and inside the encrypted message. A path attacker who rewrites
    // the clear copy cannot steer the client to an id the server did not choose.
    [Fact]
    public void AnAcceptWhoseClearConnectionIdWasChangedIsNotBelievedAndDoesNotSpoilTheRealOne()
    {
        foreach (var alterAll in new[] { true, false })
        {
            var p = new Lone();
            var altered = 0;
            p.ServerTap.Attack = d =>
            {
                if ((d[0] & 0xF) != Wire.KindAccept || (!alterAll && altered++ > 0))
                {
                    return d;
                }

                var forged = (byte[])d.Clone();
                forged[9] ^= 0x55;
                return forged;
            };
            if (alterAll)
            {
                p.Rig.Run(3000);
                Assert.Equal(ConnectionState.Connecting, p.Client.State);
            }
            else
            {
                p.Connect(); // the first Accept is rewritten on the way; the repeat that follows is the real one
                Assert.Equal(ConnectionState.Connected, p.Client.State);
            }
        }
    }

    // The server tells strangers "I do not know that connection" only for ids it does not hold. For a live id it stays silent, or
    // anyone who could guess an id would get the token that closes that connection.
    [Fact]
    public void ALiveConnectionIdIsNeverAnsweredWithAResetTokenAndAnUnknownOneIs()
    {
        var p = new Lone();
        p.Connect();
        var stranger = p.Rig.Net.CreateSocket();
        var liveId = ((UdpConnection)p.Accepted!).ConnectionId;

        byte[] DataFor(uint id)
        {
            var d = new byte[Wire.SecureAadSize + 14 + Wire.TagSize + 8];
            d[0] = Wire.Header(Wire.KindData, secure: true);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(d.AsSpan(1), id);
            return d;
        }

        var buffer = new byte[2048];
        var from = new SocketAddress(AddressFamily.InterNetwork, p.ServerTap.LocalAddress.Size);

        p.Rig.Net.Inject(DataFor(liveId), stranger.LocalAddress, p.ServerTap.LocalAddress);
        p.Rig.Step(5);
        Assert.True(stranger.Receive(buffer, from) < 0, "a live connection id was answered");
        Assert.Equal(ConnectionState.Connected, p.Client.State);

        p.Rig.Net.Inject(DataFor(liveId ^ 0x1234u), stranger.LocalAddress, p.ServerTap.LocalAddress);
        p.Rig.Step(5);
        var n = stranger.Receive(buffer, from);
        Assert.Equal(Wire.ResetSize, n);
        Assert.Equal(Wire.KindReset, buffer[0] & 0xF);
    }

    // ---- UDP: handshakes that nobody follows up, and floods ----

    private static UdpOptions DropsAllData(out Func<byte[], byte[]?> attack)
    {
        attack = d => (d[0] & 0xF) == Wire.KindData ? null : d;
        return ClientOptions();
    }

    [Fact]
    public void AHandshakeNobodyFollowsUpIsDroppedAfterTheUnconfirmedTimeoutAndNeverSurfaces()
    {
        var p = new Lone(server: new UdpOptions { Security = NetSecurity.ForServer(ServerKey, "hard/1"), UnconfirmedTimeoutMs = 2000 }, client: DropsAllData(out var attack));
        p.ClientTap.Attack = attack;
        p.Rig.Run(500);
        Assert.Equal(1, p.Server.ConnectionCount); // the handshake itself worked: the server holds the connection back
        Assert.False(p.Server.TryAccept(out _));

        p.Rig.Run(3000);
        Assert.Equal(0, p.Server.ConnectionCount);
        Assert.False(p.Server.TryAccept(out _));
    }

    [Fact]
    public void UnconfirmedHandshakesAreCappedPerSourceAndInTotal()
    {
        foreach (var (perSource, total) in new[] { (1, 100), (100, 2) })
        {
            var rig = new Rig();
            var serverSocket = rig.Net.CreateSocket();
            var server = UdpTransport.Listen(serverSocket, new UdpOptions { Security = NetSecurity.ForServer(ServerKey, "hard/1"), MaxUnconfirmedPerSource = perSource, MaxUnconfirmed = total, MaxHandshakesPerSourcePerSecond = 100 }, rig.Clock);
            rig.Pumps.Add(server.Pump);
            for (var i = 0; i < 6; i++)
            {
                var tap = new TapSocket(rig.Net.CreateSocket()) { Attack = d => (d[0] & 0xF) == Wire.KindData ? null : d };
                var c = UdpTransport.Connect(tap, serverSocket.LocalAddress, ClientOptions(NetKeyPair.Generate()), rig.Clock);
                rig.Pumps.Add(c.Pump);
            }

            rig.Run(400);
            Assert.Equal(Math.Min(perSource, total), server.ConnectionCount);
            Assert.True(server.HandshakesRateLimited > 0);
        }
    }

    // The per-source rate stands on its own: with the unconfirmed caps out of the way, one address still cannot start more handshakes
    // in a second than its share, while its datagrams are honest and well formed.
    [Fact]
    public void OneAddressCannotStartMoreHandshakesPerSecondThanItsShare()
    {
        var rig = new Rig();
        var serverSocket = rig.Net.CreateSocket();
        var server = UdpTransport.Listen(serverSocket, new UdpOptions { Security = NetSecurity.ForServer(ServerKey, "hard/1"), MaxHandshakesPerSourcePerSecond = 2, MaxUnconfirmedPerSource = 100, MaxUnconfirmed = 100 }, rig.Clock);
        rig.Pumps.Add(server.Pump);
        var clients = Enumerable.Range(0, 8).Select(_ => UdpTransport.Connect(rig.Net.CreateSocket(), serverSocket.LocalAddress, ClientOptions(NetKeyPair.Generate()), rig.Clock)).ToList();
        foreach (var c in clients)
        {
            rig.Pumps.Add(c.Pump);
        }

        rig.Run(600);
        Assert.Equal(2, clients.Count(c => c.State == ConnectionState.Connected));
        Assert.True(server.HandshakesRateLimited > 0);
        Assert.True(rig.RunUntil(() => clients.All(c => c.State == ConnectionState.Connected), 20_000), "the rest get in over the next seconds");
    }

    // ---- Keys ----

    [Fact]
    public void OnlyTheCanonicalSpellingOfAPublicKeyIsAnIdentity()
    {
        // 2^255 - 19 is the field prime; values from it up (and anything with bit 255 set) name the same point as a smaller value.
        byte[] Key(byte first, byte middle, byte last)
        {
            var k = new byte[32];
            Array.Fill(k, middle);
            k[0] = first;
            k[31] = last;
            return k;
        }

        Assert.True(SecureHandshake.IsCanonical(PlayerKey.PublicKey.Bytes));
        Assert.True(SecureHandshake.IsCanonical(Key(0xec, 0xff, 0x7f))); // p - 1
        Assert.False(SecureHandshake.IsCanonical(Key(0xed, 0xff, 0x7f))); // p
        Assert.False(SecureHandshake.IsCanonical(Key(0xff, 0xff, 0x7f))); // 2^255 - 1
        Assert.False(SecureHandshake.IsCanonical(Key(0x00, 0x00, 0x80))); // bit 255
        Assert.True(SecureHandshake.IsCanonical(Key(0xff, 0x00, 0x7f)));
        Assert.False(SecureHandshake.IsCanonical(new byte[31]));
    }

    // X25519 ignores the top bit of a public key, so the same private key can spell its public key two ways and complete a handshake
    // either way. A registry keyed by the spelling would then count one player twice.
    [Fact]
    public void AClientKeyWithTheTopBitSetIsRefusedAndTheCanonicalOneIsNot()
    {
        var bind = Rig.Pattern(25, 7);
        var sec = new NetSecurity { GameId = "g", ServerKeys = [ServerKey] };
        var pub = PlayerKey.PublicKey.Bytes.ToArray();
        var alias = (byte[])pub.Clone();
        alias[31] |= 0x80;
        Assert.False(SecureHandshake.IsCanonical(alias));

        foreach (var (spelling, expected) in new[] { (pub, true), (alias, false) })
        {
            var client = new NoiseHandshake(NoisePattern.IK, true, Prologue("g", bind), PlayerKey.Private, ServerKey.PublicKey.Bytes, spelling);
            var m1 = new byte[SecureHandshake.FirstMessageOverhead];
            Assert.Equal(m1.Length, client.WriteMessage(default, m1));
            var server = SecureHandshake.Server(sec, bind);
            Assert.Equal(expected, server.ReadFirst(m1, new byte[96], out _));
            Assert.Equal(expected, !server.Remote.IsEmpty);
        }
    }

    // The channel binding is Noise's handshake hash: public to anyone who saw the two messages. It is unique to a session, not secret.
    [Fact]
    public void ThePassiveObserverCanComputeTheChannelBindingSoItIsNotASecret()
    {
        var bind = Rig.Pattern(25, 9);
        var sec = new NetSecurity { GameId = "g", ServerKeys = [ServerKey] };
        var client = SecureHandshake.Client(NetSecurity.ForClient([ServerKey.PublicKey], "g", PlayerKey), 0, bind);
        var m1 = new byte[96];
        _ = client.WriteFirst(default, m1);
        var server = SecureHandshake.Server(sec, bind);
        Assert.True(server.ReadFirst(m1, new byte[96], out _));
        var m2 = new byte[48];
        _ = server.WriteReply(default, m2);
        Assert.True(client.ReadReply(m2, new byte[48], out _));
        client.Finish(out _, out _);
        server.Finish(out _, out _);

        static byte[] H(params byte[][] parts)
        {
            var o = new byte[32];
            Sha256.Hash(parts.SelectMany(x => x).ToArray(), o);
            return o;
        }

        var h = Encoding.ASCII.GetBytes("Noise_IK_25519_ChaChaPoly_SHA256");
        h = H(h, Prologue("g", bind));
        h = H(h, ServerKey.PublicKey.Bytes.ToArray());
        h = H(h, m1[..32]);
        h = H(h, m1[32..80]);
        h = H(h, m1[80..96]);
        h = H(h, m2[..32]);
        h = H(h, m2[32..48]);
        Assert.True(h.AsSpan().SequenceEqual(server.ChannelBinding));
    }

    // A server that rotates its key keeps its name, and players keep the key that server knows them by.
    [Fact]
    public void AServerNameKeepsPlayersTheSameKeyWhenTheServerRotatesItsKey()
    {
        var rotated = NetKeyPair.FromPrivateKey(Rig.Pattern(32, 222));
        var byKey = NetSecurity.ForClient([ServerKey.PublicKey, rotated.PublicKey], "hard/1", PlayerKey);
        Assert.NotEqual(byKey.IdentityKeyFor(ServerKey.PublicKey), byKey.IdentityKeyFor(rotated.PublicKey)); // without a name, rotation resets players

        var named = NetSecurity.ForClient([ServerKey.PublicKey, rotated.PublicKey], "hard/1", PlayerKey, serverId: "eu-1.hard");
        Assert.Equal(named.IdentityKeyFor(ServerKey.PublicKey), named.IdentityKeyFor(rotated.PublicKey));
        var otherName = NetSecurity.ForClient([ServerKey.PublicKey], "hard/1", PlayerKey, serverId: "us-1.hard");
        Assert.NotEqual(named.IdentityKeyFor(ServerKey.PublicKey), otherName.IdentityKeyFor(ServerKey.PublicKey));
        Assert.NotEqual(PlayerKey.PublicKey, named.IdentityKeyFor(ServerKey.PublicKey));

        // End to end: the server sees the same key before and after it changes its key pair.
        var seen = new List<PeerKey>();
        foreach (var serverPair in new[] { ServerKey, rotated })
        {
            var p = new Lone(server: new UdpOptions { Security = NetSecurity.ForServer(serverPair, "hard/1") }, client: new UdpOptions { Security = named });
            p.Connect();
            seen.Add(p.Accepted!.RemoteKey!.Value);
        }

        Assert.Equal(seen[0], seen[1]);
        Assert.Equal(named.IdentityKeyFor(ServerKey.PublicKey), seen[0]);
    }

    // ---- WebSocket: handshake budget and pending sockets ----

    [Fact]
    public void OneWebSocketSourceCannotSpendTheSharedBudgetOrTheReturningLane()
    {
        var clock = new ManualNetClock();
        var budget = new HandshakeBudget(clock, 200, 4, 10);
        var spent = 0;
        for (var i = 0; i < 1000; i++)
        {
            if (budget.TryOpen("198.51.100.7"))
            {
                if (budget.TryCharge("198.51.100.7"))
                {
                    spent++;
                }

                budget.Close("198.51.100.7");
            }
        }

        Assert.Equal(10, spent);
        Assert.True(budget.TryOpen("203.0.113.9"));
        Assert.True(budget.TryCharge("203.0.113.9"), "another source still gets its handshake");

        // A source that completed a session is "returning"; being returning does not lift its own per-second cap.
        budget.Confirmed("198.51.100.7");
        clock.Advance(1500);
        spent = 0;
        for (var i = 0; i < 1000; i++)
        {
            if (budget.TryCharge("198.51.100.7"))
            {
                spent++;
            }
        }

        Assert.Equal(10, spent);
        Assert.True(budget.TryCharge("203.0.113.10"));
    }

    [Fact]
    public void AnIpv4PeerSeenThroughADualModeSocketKeepsItsOwnSource()
    {
        Assert.NotEqual(HandshakeBudget.SourceOf("[::ffff:1.2.3.4]:5000"), HandshakeBudget.SourceOf("[::ffff:5.6.7.8]:6000"));
        Assert.Equal(HandshakeBudget.SourceOf("1.2.3.4:5000"), HandshakeBudget.SourceOf("[::ffff:1.2.3.4]:6000"));
    }

    // Behind a reverse proxy every visitor has the proxy's address. Sockets that never speak must not keep real visitors out.
    [Fact]
    public void StalledSocketsFromOneSourceDoNotLockOutARealClient()
    {
        var o = new WebSocketOptions { Security = NetSecurity.ForServer(ServerKey, "hard/1") };
        using var listener = WebSocketTransport.Listen(new IPEndPoint(IPAddress.Loopback, 0), o);
        var endpoint = (IPEndPoint)listener.LocalEndPoint;
        var stalled = new List<INetConnection>();
        for (var i = 0; i < 4; i++)
        {
            stalled.Add(WebSocketTransport.Connect(endpoint, new WebSocketOptions { Security = NetSecurity.Insecure }));
        }

        for (var i = 0; i < 60; i++)
        {
            listener.Pump();
            foreach (var c in stalled)
            {
                c.Pump();
            }

            Thread.Sleep(5);
        }

        Assert.Equal(4, listener.PendingHandshakes);
        var real = WebSocketTransport.Connect(endpoint, new WebSocketOptions { Security = NetSecurity.ForClient([ServerKey.PublicKey], "hard/1", PlayerKey) });
        INetConnection? accepted = null;
        for (var i = 0; i < 400 && real.State != ConnectionState.Connected && real.State != ConnectionState.Closed; i++)
        {
            listener.Pump();
            real.Pump();
            foreach (var c in stalled)
            {
                c.Pump();
            }

            _ = accepted is not null || listener.TryAccept(out accepted);
            Thread.Sleep(5);
        }

        for (var i = 0; i < 20 && accepted is null; i++)
        {
            listener.Pump();
            real.Pump();
            _ = listener.TryAccept(out accepted);
            Thread.Sleep(5);
        }

        Assert.Equal(ConnectionState.Connected, real.State);
        Assert.NotNull(accepted);
        foreach (var c in stalled)
        {
            c.Disconnect();
        }

        real.Disconnect();
    }

    // A socket that does not send its first message in time is closed and stops counting against its source.
    [Fact]
    public void ASocketThatNeverSendsItsFirstMessageIsClosedAfterTheFirstMessageTimeout()
    {
        var clock = new ManualNetClock();
        var (a, b) = MemConn.Pair();
        var options = SecureRig.Opt(SecureRig.ServerSec());
        var budget = new HandshakeBudget(clock, 200, 4);
        Assert.True(budget.TryOpen("mem"));
        var server = SecureConnection.ForServer(b, SecureRig.ServerSec(), new WebSocketOptions { Security = options.Security, FirstMessageTimeoutMs = 700, HandshakeTimeoutMs = 5000 }, clock, budget);
        Assert.True(server.AwaitingFirstMessage);
        clock.Advance(600);
        server.Pump();
        Assert.True(server.AwaitingFirstMessage);
        clock.Advance(200);
        server.Pump();
        Assert.False(server.AwaitingFirstMessage);
        Assert.NotEqual(ConnectionState.Connected, server.State);

        // Its place in the source's queue was given back at once.
        for (var i = 0; i < 4; i++)
        {
            Assert.True(budget.TryOpen("mem"), "slot " + i);
        }

        GC.KeepAlive(a);
    }
}
