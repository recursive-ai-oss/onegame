using System.Net;
using System.Net.Sockets;
using OneGame.Core;

namespace OneGame.Net.Tests;

/// <summary>A socket that shows a path attacker everything sent through it and lets them alter, drop or add to it.</summary>
internal sealed class TapSocket(IDatagramSocket inner) : IDatagramSocket
{
    /// <summary>Every datagram handed to Send, before the attacker touched it.</summary>
    public List<byte[]> Sent { get; } = [];

    /// <summary>Called for each datagram; return the bytes to actually send, or null to drop them.</summary>
    public Func<byte[], byte[]?>? Attack { get; set; }

    public SocketAddress LocalAddress => inner.LocalAddress;

    /// <summary>When false the tap passes datagrams through without keeping copies (for allocation measurements).</summary>
    public bool Record { get; set; } = true;

    public void Send(ReadOnlySpan<byte> datagram, SocketAddress destination)
    {
        if (!Record && Attack is null)
        {
            inner.Send(datagram, destination);
            return;
        }

        var copy = datagram.ToArray();
        Sent.Add(copy);
        var outgoing = Attack is null ? copy : Attack(copy);
        if (outgoing is not null)
        {
            inner.Send(outgoing, destination);
        }
    }

    public int Receive(Span<byte> buffer, SocketAddress from) => inner.Receive(buffer, from);

    public void Dispose() => inner.Dispose();
}

public class SecureUdpTests
{
    private static readonly NetKeyPair ServerKey = NetKeyPair.FromPrivateKey(Rig.Pattern(32, 101));
    private static readonly NetKeyPair OtherKey = NetKeyPair.FromPrivateKey(Rig.Pattern(32, 202));
    private static readonly NetKeyPair PlayerKey = NetKeyPair.FromPrivateKey(Rig.Pattern(32, 303));

    private static UdpOptions Server(params NetKeyPair[] keys) => new() { Security = new NetSecurity { GameId = "test/1", ServerKeys = keys.Length == 0 ? [ServerKey] : keys } };

    private static UdpOptions Client(NetKeyPair? identity = null, params PeerKey[] trusted) => new() { Security = NetSecurity.ForClient(trusted.Length == 0 ? [ServerKey.PublicKey] : trusted, "test/1", identity ?? PlayerKey) };

    private sealed class Pair
    {
        public Rig Rig { get; } = new();
        public TapSocket ServerTap { get; set; } = null!;
        public TapSocket ClientTap { get; set; } = null!;
        public UdpListener Server { get; set; } = null!;
        public UdpConnection Client { get; set; } = null!;
        public INetConnection? Accepted { get; set; }

        public static Pair Make(UdpOptions? server = null, UdpOptions? client = null, bool connect = true)
        {
            var p = new Pair();
            var serverSocket = p.Rig.Net.CreateSocket();
            p.ServerTap = new TapSocket(serverSocket);
            p.ClientTap = new TapSocket(p.Rig.Net.CreateSocket());
            p.Server = UdpTransport.Listen(p.ServerTap, server ?? Server(), p.Rig.Clock);
            p.Client = UdpTransport.Connect(p.ClientTap, serverSocket.LocalAddress, client ?? Client(), p.Rig.Clock);
            p.Rig.Pumps.Add(p.Server.Pump);
            p.Rig.Pumps.Add(p.Client.Pump);
            if (connect)
            {
                Assert.True(p.Rig.RunUntil(() => (p.Accepted ??= p.Server.TryAccept(out var a) ? a : null) is not null && p.Client.State == ConnectionState.Connected, 20_000), "the secure handshake did not finish");
            }

            return p;
        }

        public void Exchange(byte[] payload)
        {
            Client.Send(payload);
            Accepted!.Send(payload);
            Rig.Step(5);
        }
    }

    // Review M7: hosts cannot link a player across servers by their key, and a server still sees the same key every time.
    [Fact]
    public void AClientShowsEachServerADifferentKeyThatStaysTheSameForThatServer()
    {
        var other = NetKeyPair.FromPrivateKey(Rig.Pattern(32, 404));
        var security = NetSecurity.ForClient([ServerKey.PublicKey, other.PublicKey], "test/1", PlayerKey);
        Assert.NotEqual(security.IdentityKeyFor(ServerKey.PublicKey), security.IdentityKeyFor(other.PublicKey));
        Assert.Equal(security.IdentityKeyFor(ServerKey.PublicKey), security.IdentityKeyFor(ServerKey.PublicKey));
        var p = Pair.Make();
        var q = Pair.Make(server: Server(other), client: new UdpOptions { Security = NetSecurity.ForClient([other.PublicKey], "test/1", PlayerKey) });
        Assert.NotEqual(p.Accepted!.RemoteKey, q.Accepted!.RemoteKey);
        Assert.Equal(security.IdentityKeyFor(other.PublicKey), q.Accepted.RemoteKey);
    }

    [Fact]
    public void BothEndsOfASessionShareAChannelBindingNobodyElseHas()
    {
        var p = Pair.Make();
        Assert.Equal(32, p.Client.ChannelBinding.Length);
        Assert.True(p.Client.ChannelBinding.Span.SequenceEqual(p.Accepted!.ChannelBinding.Span));
        var q = Pair.Make();
        Assert.False(p.Client.ChannelBinding.Span.SequenceEqual(q.Client.ChannelBinding.Span));
    }

    [Fact]
    public void ASecureConnectionKnowsWhoIsOnTheOtherEnd()
    {
        var p = Pair.Make();
        Assert.True(p.Client.IsEncrypted);
        Assert.True(p.Accepted!.IsEncrypted);
        Assert.Equal(Client().Security!.IdentityKeyFor(ServerKey.PublicKey), p.Accepted.RemoteKey);
        Assert.NotEqual(PlayerKey.PublicKey, p.Accepted.RemoteKey); // the server sees a key derived for it, not the player's root key
        Assert.Equal(ServerKey.PublicKey, p.Client.RemoteKey);

        var message = Rig.Pattern(3000, 5);
        p.Client.Send(message);
        p.Accepted.Send(message);
        Assert.True(p.Rig.RunUntil(() => p.Accepted.TryReceive(out var m) && m.SequenceEqual(message), 5000));
        Assert.True(p.Rig.RunUntil(() => p.Client.TryReceive(out var m) && m.SequenceEqual(message), 5000));
        p.Client.SendUnreliable(message.AsSpan(0, 100));
        Assert.True(p.Rig.RunUntil(() => p.Accepted.State == ConnectionState.Connected && ((UdpConnection)p.Accepted).TryReceiveUnreliable(out var u) && u.Length == 100, 2000));
        Assert.Equal(1200 - 23 - 3 - 16, p.Client.MaxUnreliableBytes);
    }

    [Fact]
    public void NothingOnTheWireIsPlaintext()
    {
        var p = Pair.Make();
        var secret = System.Text.Encoding.ASCII.GetBytes("TOP-SECRET-LANTERN-COUNT-4242-TOP-SECRET");
        for (var i = 0; i < 20; i++)
        {
            p.Client.Send(secret);
            p.Accepted!.Send(secret);
            p.Rig.Step(5);
        }

        p.Rig.Run(200);
        var all = p.ClientTap.Sent.Concat(p.ServerTap.Sent).ToList();
        Assert.True(all.Count > 40);
        var needles = new[] { secret[..16], PlayerKey.PublicKey.Bytes.ToArray(), Client().Security!.IdentityKeyFor(ServerKey.PublicKey).Bytes.ToArray(), ServerKey.PublicKey.Bytes.ToArray() };
        foreach (var datagram in all)
        {
            foreach (var needle in needles)
            {
                Assert.False(datagram.AsSpan().IndexOf(needle) >= 0, "a datagram carried plaintext or a static key");
            }
        }

        // The handshake messages are the only ones with unencrypted structure; show one Data datagram as a byte dump.
        var data = p.ClientTap.Sent.Last(d => (d[0] & 0xF) == Wire.KindData);
        Assert.Equal(Wire.VersionSecure, data[0] >> 4);
        Console.WriteLine("secure data datagram (" + data.Length + " bytes): " + Convert.ToHexString(data.AsSpan(0, Math.Min(64, data.Length))).ToLowerInvariant());
    }

    [Fact]
    public void FlippedBitsAreNeverDelivered()
    {
        var p = Pair.Make();
        var rng = new Random(7);
        p.ClientTap.Attack = d =>
        {
            if ((d[0] & 0xF) == Wire.KindData && rng.Next(3) == 0)
            {
                var copy = (byte[])d.Clone();
                copy[rng.Next(copy.Length)] ^= (byte)(1 << rng.Next(8));
                return copy;
            }

            return d;
        };
        var sent = new List<byte[]>();
        for (var i = 0; i < 60; i++)
        {
            var m = Rig.Pattern(500 + i, i);
            sent.Add(m);
            p.Client.Send(m);
        }

        var received = new List<byte[]>();
        Assert.True(p.Rig.RunUntil(() =>
        {
            while (p.Accepted!.TryReceive(out var m))
            {
                received.Add(m.ToArray());
            }

            return received.Count == sent.Count;
        }, 60_000));
        for (var i = 0; i < sent.Count; i++)
        {
            Assert.Equal(sent[i], received[i]);
        }

        Assert.Equal(ConnectionState.Connected, p.Client.State);
    }

    [Fact]
    public void ReplayedAndInjectedDatagramsChangeNothing()
    {
        var p = Pair.Make();
        var message = Rig.Pattern(100, 1);
        p.Client.Send(message);
        p.Rig.Run(100);
        Assert.True(p.Accepted!.TryReceive(out _));
        var statsBefore = p.Accepted.Stats.MessagesReceived;

        // Replay every Data datagram the client ever sent, from the client's own address, many times.
        var clientAddress = p.ClientTap.LocalAddress;
        foreach (var d in p.ClientTap.Sent.Where(d => (d[0] & 0xF) == Wire.KindData).ToList())
        {
            for (var i = 0; i < 5; i++)
            {
                p.Rig.Net.Inject(d, clientAddress, p.ServerTap.LocalAddress);
            }
        }

        // Forge data and disconnects with the right connection id and random bodies.
        var id = ((UdpConnection)p.Accepted).ConnectionId;
        var forgeRng = new Random(3);
        for (var i = 0; i < 200; i++)
        {
            var forged = new byte[40 + forgeRng.Next(200)];
            forgeRng.NextBytes(forged);
            forged[0] = Wire.Header(i % 2 == 0 ? Wire.KindData : Wire.KindDisconnect, secure: true);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(forged.AsSpan(1), id);
            if (i % 2 == 1)
            {
                forged = forged[..23];
            }

            p.Rig.Net.Inject(forged, clientAddress, p.ServerTap.LocalAddress);
        }

        // Forge a reset for the client with a wrong token.
        var reset = new byte[Wire.ResetSize];
        reset[0] = Wire.Header(Wire.KindReset, secure: true);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(reset.AsSpan(1), p.Client.ConnectionId);
        p.Rig.Net.Inject(reset, p.ServerTap.LocalAddress, clientAddress);
        p.Rig.Run(300);

        Assert.False(p.Accepted.TryReceive(out _));
        Assert.Equal(statsBefore, p.Accepted.Stats.MessagesReceived);
        Assert.Equal(ConnectionState.Connected, p.Accepted.State);
        Assert.Equal(ConnectionState.Connected, p.Client.State);
        p.Exchange(message);
        Assert.True(p.Rig.RunUntil(() => p.Accepted.TryReceive(out var m) && m.Length == 100, 2000));
    }

    [Fact]
    public void ADatagramReorderedLongAgoIsRejectedByTheWindowNotDecrypted()
    {
        var p = Pair.Make();
        var held = new List<byte[]>();
        p.ClientTap.Attack = d =>
        {
            if ((d[0] & 0xF) == Wire.KindData && held.Count == 0)
            {
                held.Add(d);
                return null; // the attacker holds the first datagram back
            }

            return d;
        };
        for (var i = 0; i < 300; i++)
        {
            p.Client.Send(Rig.Pattern(100, i));
            p.Rig.Step(5);
        }

        p.ClientTap.Attack = null;
        p.Rig.Net.Inject(held[0], p.ClientTap.LocalAddress, p.ServerTap.LocalAddress);
        p.Rig.Run(50);
        Assert.Equal(ConnectionState.Connected, p.Accepted!.State);
    }

    [Fact]
    public void AClientThatRequiresSecurityRefusesAPlaintextServer()
    {
        var rig = new Rig();
        var serverSocket = rig.Net.CreateSocket();
        var server = UdpTransport.Listen(serverSocket, Plain.Udp, rig.Clock);
        var client = UdpTransport.Connect(rig.Net.CreateSocket(), serverSocket.LocalAddress, Client(), rig.Clock);
        rig.Pumps.Add(server.Pump);
        rig.Pumps.Add(client.Pump);
        client.Send([1, 2, 3]);
        Assert.True(rig.RunUntil(() => client.State == ConnectionState.Closed, 5000));
        Assert.Equal(DisconnectReason.SecurityRequired, client.CloseInfo.Reason);
        Assert.False(server.TryAccept(out _));
        Assert.Equal(0, server.ConnectionCount);
    }

    [Fact]
    public void APlaintextClientIsRefusedByARequiredServer()
    {
        var rig = new Rig();
        var serverSocket = rig.Net.CreateSocket();
        var server = UdpTransport.Listen(serverSocket, Server(), rig.Clock);
        var client = UdpTransport.Connect(rig.Net.CreateSocket(), serverSocket.LocalAddress, Plain.Udp, rig.Clock);
        rig.Pumps.Add(server.Pump);
        rig.Pumps.Add(client.Pump);
        Assert.True(rig.RunUntil(() => client.State == ConnectionState.Closed, 5000));
        Assert.Equal(DisconnectReason.SecurityRequired, client.CloseInfo.Reason);
        Assert.Equal(0, server.ConnectionCount);
    }

    [Fact]
    public void AForgedPlaintextAcceptCannotDowngradeASecureClient()
    {
        var rig = new Rig();
        var serverSocket = rig.Net.CreateSocket();
        var clientSocket = rig.Net.CreateSocket();
        var client = UdpTransport.Connect(clientSocket, serverSocket.LocalAddress, Client(), rig.Clock);
        rig.Pumps.Add(client.Pump);
        var request = new byte[Wire.MinHandshakeSize];
        Assert.True(rig.RunUntil(() => serverSocket.Receive(request, new SocketAddress(AddressFamily.InterNetwork)) > 0, 1000));
        var nonce = System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(request.AsSpan(1));
        var accept = new byte[Wire.AcceptSize];
        accept[0] = Wire.Header(Wire.KindAccept); // version 1
        System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(accept.AsSpan(1), nonce);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(accept.AsSpan(9), 77);
        rig.Net.Inject(accept, serverSocket.LocalAddress, clientSocket.LocalAddress);
        rig.Run(50);
        Assert.Equal(ConnectionState.Connecting, client.State);
    }

    // Review L1/L7: a datagram that looks like the reply but is not authentic must not kill the handshake the real reply is waiting for.
    [Fact]
    public void AForgedAuthenticLookingAcceptDoesNotSpoilTheRealOne()
    {
        var p = Pair.Make(connect: false);
        var injected = 0;
        p.ServerTap.Attack = d =>
        {
            if ((d[0] & 0xF) == Wire.KindAccept && injected++ == 0)
            {
                var forged = (byte[])d.Clone();
                forged[^1] ^= 1;
                p.Rig.Net.Inject(forged, p.ServerTap.LocalAddress, p.ClientTap.LocalAddress);
            }

            return d;
        };
        Assert.True(p.Rig.RunUntil(() => (p.Accepted ??= p.Server.TryAccept(out var a) ? a : null) is not null && p.Client.State == ConnectionState.Connected, 20_000));
        Assert.Equal(1, injected);
        Assert.Equal(ServerKey.PublicKey, p.Client.RemoteKey);
    }

    // Review M2: one source cannot use the shared handshake budget, and the others still get in.
    [Fact]
    public void OneSourceHasItsOwnHandshakeBudget()
    {
        var rig = new Rig();
        var serverSocket = rig.Net.CreateSocket();
        var server = UdpTransport.Listen(serverSocket, new UdpOptions { Security = NetSecurity.ForServer(ServerKey, "test/1"), MaxHandshakesPerSourcePerSecond = 2 }, rig.Clock);
        rig.Pumps.Add(server.Pump);
        var clients = Enumerable.Range(0, 8).Select(_ => UdpTransport.Connect(rig.Net.CreateSocket(), serverSocket.LocalAddress, Client(NetKeyPair.Generate()), rig.Clock)).ToList();
        foreach (var c in clients)
        {
            rig.Pumps.Add(c.Pump);
        }

        rig.Run(300);
        Assert.True(server.HandshakesRateLimited > 0);
        Assert.True(rig.RunUntil(() => clients.All(c => c.State == ConnectionState.Connected), 20_000), "clients retry and all get in");
    }

    [Fact]
    public void NoKeysIsATypedErrorAndInsecureIsExplicit()
    {
        var rig = new Rig();
        var socket = rig.Net.CreateSocket();
        var e = Assert.Throws<SecurityNotConfiguredException>(() => UdpTransport.Listen(socket, new UdpOptions(), rig.Clock));
        Assert.Contains("NetSecurity.ForServer", e.Message);
        Assert.Throws<SecurityNotConfiguredException>(() => UdpTransport.Listen(socket, null, rig.Clock));
        Assert.Throws<SecurityNotConfiguredException>(() => UdpTransport.Listen(socket, new UdpOptions { Security = new NetSecurity() }, rig.Clock));
        Assert.Throws<SecurityNotConfiguredException>(() => UdpTransport.Connect(rig.Net.CreateSocket(), socket.LocalAddress, new UdpOptions(), rig.Clock));
        Assert.Throws<SecurityNotConfiguredException>(() => UdpTransport.Connect(rig.Net.CreateSocket(), socket.LocalAddress, new UdpOptions { Security = new NetSecurity() }, rig.Clock));
        var insecure = Pair.Make(Plain.Udp, Plain.Udp);
        Assert.False(insecure.Client.IsEncrypted);
        Assert.Null(insecure.Client.RemoteKey);
        Assert.Null(insecure.Accepted!.RemoteKey);
    }

    [Fact]
    public void AWrongKeyOrGameIdFailsTheHandshake()
    {
        var wrongKey = Pair.Make(Server(), Client(null, OtherKey.PublicKey), connect: false);
        Assert.True(wrongKey.Rig.RunUntil(() => wrongKey.Client.State == ConnectionState.Closed, 5000));
        Assert.Equal(DisconnectReason.HandshakeFailed, wrongKey.Client.CloseInfo.Reason);
        Assert.Equal(0, wrongKey.Server.ConnectionCount);
        Assert.True(wrongKey.Server.HandshakesFailed >= 1);

        var otherGame = new UdpOptions { Security = NetSecurity.ForClient([ServerKey.PublicKey], "another/9", PlayerKey) };
        var wrongGame = Pair.Make(Server(), otherGame, connect: false);
        Assert.True(wrongGame.Rig.RunUntil(() => wrongGame.Client.State == ConnectionState.Closed, 5000));
        Assert.Equal(DisconnectReason.HandshakeFailed, wrongGame.Client.CloseInfo.Reason);
    }

    [Fact]
    public void KeyRotationOverlapsOnBothSides()
    {
        // A client whose first trusted key is stale falls through to the next; a server holding both pairs answers either.
        var staleFirst = Pair.Make(Server(ServerKey), Client(null, OtherKey.PublicKey, ServerKey.PublicKey));
        Assert.Equal(ServerKey.PublicKey, staleFirst.Client.RemoteKey);

        var oldClient = Pair.Make(Server(OtherKey, ServerKey), Client(null, ServerKey.PublicKey));
        Assert.Equal(ServerKey.PublicKey, oldClient.Client.RemoteKey);
        var newClient = Pair.Make(Server(OtherKey, ServerKey), Client(null, OtherKey.PublicKey));
        Assert.Equal(OtherKey.PublicKey, newClient.Client.RemoteKey);
    }

    [Fact]
    public void ARestartedServerResetsTheClientAndOnlyItsOwnTokenWorks()
    {
        var p = Pair.Make();
        p.Rig.Pumps.Remove(p.Server.Pump); // the old process is gone without a word

        // The same server comes back at the same address with the same key and no memory of the connection.
        var restarted = UdpTransport.Listen(p.ServerTap, Server(), p.Rig.Clock);
        p.Rig.Pumps.Add(restarted.Pump);
        p.Client.Send([1]);
        Assert.True(p.Rig.RunUntil(() => p.Client.State == ConnectionState.Closed, 5000));
        Assert.Equal(DisconnectReason.Shutdown, p.Client.CloseInfo.Reason);
        Assert.True(p.Client.CloseInfo.ByRemote);

        // A different server key cannot reset it.
        var q = Pair.Make();
        q.Rig.Pumps.Remove(q.Server.Pump);
        var impostor = UdpTransport.Listen(q.ServerTap, Server(OtherKey), q.Rig.Clock);
        q.Rig.Pumps.Add(impostor.Pump);
        q.Client.Send([1]);
        q.Rig.Run(2000);
        Assert.Equal(ConnectionState.Connected, q.Client.State);
    }

    [Fact]
    public void AReplayedFirstMessageNeverSurfacesAConnection()
    {
        var p = Pair.Make();
        var response = p.ClientTap.Sent.First(d => (d[0] & 0xF) == Wire.KindResponse);
        p.Client.Disconnect();
        p.Rig.Run(100);
        Assert.Equal(0, p.Server.ConnectionCount);

        // An attacker replays the captured Response from the client's address while the cookie is still valid.
        p.Rig.Net.Inject(response, p.ClientTap.LocalAddress, p.ServerTap.LocalAddress);
        p.Server.Pump();
        Assert.Equal(0, p.Server.ConnectionCount); // the ended handshake is remembered, so the replay does not even start one
        Assert.False(p.Server.TryAccept(out _));
        p.Rig.Run(6000);
        Assert.False(p.Server.TryAccept(out _));
        Assert.Equal(0, p.Server.ConnectionCount);
    }

    [Fact]
    public void HandshakesAreRateLimitedBeforeAnyKeyWork()
    {
        var rig = new Rig();
        var serverSocket = rig.Net.CreateSocket();
        var server = UdpTransport.Listen(serverSocket, new UdpOptions { Security = NetSecurity.ForServer(ServerKey, "test/1"), MaxHandshakesPerSecond = 3, ReservedHandshakesPerSecond = 0 }, rig.Clock);
        rig.Pumps.Add(server.Pump);
        var clients = Enumerable.Range(0, 12).Select(_ => UdpTransport.Connect(rig.Net.CreateSocket(), serverSocket.LocalAddress, Client(NetKeyPair.Generate()), rig.Clock)).ToList();
        foreach (var c in clients)
        {
            rig.Pumps.Add(c.Pump);
        }

        rig.Run(300);
        Assert.True(server.HandshakesRateLimited > 0);
        Assert.True(clients.Count(c => c.State == ConnectionState.Connected) <= 3);
        Assert.True(rig.RunUntil(() => clients.All(c => c.State == ConnectionState.Connected), 20_000), "clients retry and get in over a few seconds");
    }

    [Fact]
    public void ThePacketLimitClosesBothEndsAndAFreshConnectionWorks()
    {
        var o = new UdpOptions { Security = NetSecurity.ForClient([ServerKey.PublicKey], "test/1", PlayerKey), MaxPacketsPerKey = 1024 };
        var p = Pair.Make(null, o);
        for (var i = 0; i < 3000 && p.Client.State == ConnectionState.Connected; i++)
        {
            p.Client.Send(Rig.Pattern(100, i));
            p.Rig.Step(5);
            while (p.Accepted!.TryReceive(out _))
            {
            }
        }

        Assert.Equal(ConnectionState.Closed, p.Client.State);
        Assert.Equal(DisconnectReason.PacketLimit, p.Client.CloseInfo.Reason);
        Assert.True(p.Rig.RunUntil(() => p.Accepted!.State == ConnectionState.Closed, 2000));
        Assert.Equal(DisconnectReason.PacketLimit, p.Accepted!.CloseInfo.Reason);
        Assert.True(p.Accepted.CloseInfo.ByRemote);
    }

    [Fact]
    public void ASecureConnectionClosesGracefullyWithItsReason()
    {
        var p = Pair.Make();
        p.Client.Disconnect(DisconnectReason.Kicked);
        Assert.True(p.Rig.RunUntil(() => p.Accepted!.State == ConnectionState.Closed, 2000));
        Assert.Equal(DisconnectReason.Kicked, p.Accepted!.CloseInfo.Reason);
        Assert.True(p.Accepted.CloseInfo.ByRemote);
    }

    [Fact]
    public void SecureSteadyStateAllocatesNothing()
    {
        var p = Pair.Make();
        p.ClientTap.Record = false;
        p.ServerTap.Record = false;
        var payload = Rig.Pattern(200, 1);
        void Round()
        {
            for (var i = 0; i < 10; i++)
            {
                p.Client.Send(payload);
                p.Accepted!.Send(payload);
            }

            p.Rig.Step(5);
            while (p.Accepted!.TryReceive(out _))
            {
            }

            while (p.Client.TryReceive(out _))
            {
            }
        }

        for (var i = 0; i < 400; i++)
        {
            Round();
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 400; i++)
        {
            Round();
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.True(allocated == 0, $"{allocated} bytes allocated over 400 rounds of 20 encrypted messages");
    }
}

