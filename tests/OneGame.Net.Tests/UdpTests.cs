using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;

namespace OneGame.Net.Tests;

public class UdpTests
{
    [Fact]
    public void HandshakeThenMessagesFlowBothWays()
    {
        var rig = new Rig();
        var (server, client, accepted) = rig.Connected();
        Assert.Equal(1, server.ConnectionCount);
        Assert.Equal(ConnectionState.Connected, accepted.State);

        client.Send([1, 2, 3]);
        accepted.Send([9, 8]);
        client.Send([]);
        var got = new List<byte[]>();
        var back = new List<byte[]>();
        rig.RunUntil(() =>
        {
            while (accepted.TryReceive(out var m))
            {
                got.Add(m.ToArray());
            }

            while (client.TryReceive(out var m))
            {
                back.Add(m.ToArray());
            }

            return got.Count == 2 && back.Count == 1;
        }, 2000);
        Assert.Equal([[1, 2, 3], []], got);
        Assert.Equal([[9, 8]], back);
        Assert.True(client.Stats.RttMs >= 0);
    }

    [Fact]
    public void MessagesSentBeforeTheHandshakeFinishesArriveInOrder()
    {
        var rig = new Rig();
        var serverSocket = rig.Net.CreateSocket();
        var server = UdpTransport.Listen(serverSocket, null, rig.Clock);
        var client = UdpTransport.Connect(rig.Net.CreateSocket(), serverSocket.LocalAddress, null, rig.Clock);
        rig.Pumps.Add(server.Pump);
        rig.Pumps.Add(client.Pump);
        for (var i = 0; i < 20; i++)
        {
            client.Send(Rig.Pattern(100 + i, i));
        }

        INetConnection? accepted = null;
        var n = 0;
        rig.RunUntil(() =>
        {
            accepted ??= server.TryAccept(out var a) ? a : null;
            while (accepted is not null && accepted.TryReceive(out var m))
            {
                Assert.True(m.SequenceEqual(Rig.Pattern(100 + n, n)));
                n++;
            }

            return n == 20;
        }, 5000);
        Assert.Equal(20, n);
    }

    [Fact]
    public void LargeMessagesArriveWholeUnderLossDuplicationAndReordering()
    {
        var rig = new Rig();
        var conditions = new NetworkConditions { LatencyMs = 40, JitterMs = 15, LossRate = 0.05, DuplicateRate = 0.02, ReorderRate = 0.05, Seed = 7 };
        var (_, client, accepted) = rig.Connected(conditions: conditions);
        var sizes = new[] { 1 << 20, 300_000, 1, 1200, 5000, 1 << 20 };
        for (var i = 0; i < sizes.Length; i++)
        {
            client.Send(Rig.Pattern(sizes[i], i));
        }

        var received = 0;
        rig.RunUntil(() =>
        {
            while (accepted.TryReceive(out var m))
            {
                Assert.Equal(sizes[received], m.Length);
                Assert.True(m.SequenceEqual(Rig.Pattern(sizes[received], received)), $"message {received} corrupted");
                received++;
            }

            return received == sizes.Length;
        }, 300_000, 5);
        Assert.Equal(sizes.Length, received);
        Assert.True(client.Stats.Retransmits > 0, "the conditioner should have forced repeats");
    }

    [Fact]
    public void OversizeSendsAreRefusedBeforeAnythingIsQueued()
    {
        var rig = new Rig();
        var (_, client, _) = rig.Connected(new UdpOptions { MaxMessageBytes = 4096 }, new UdpOptions { MaxMessageBytes = 4096 });
        var e = Assert.Throws<MessageTooLargeException>(() => client.Send(new byte[4097]));
        Assert.Equal(4097, e.Size);
        Assert.Equal(4096, e.Limit);
        Assert.Equal(0, client.Stats.SendQueueBytes);
        client.Send(new byte[4096]);
        Assert.Equal(ConnectionState.Connected, client.State);
        Assert.Throws<MessageTooLargeException>(() => client.SendUnreliable(new byte[client.MaxUnreliableBytes + 1]));
    }

    [Fact]
    public void AnOversizeMessageFromTheWireClosesTheConnectionWithoutBufferingIt()
    {
        var rig = new Rig();
        // The sender allows 2 MiB; the receiver only 64 KiB.
        var (server, client, accepted) = rig.Connected(new UdpOptions { MaxMessageBytes = 64 * 1024 }, new UdpOptions { MaxMessageBytes = 2 << 20 });
        DisconnectInfo? seen = null;
        accepted.Disconnected += (_, info) => seen = info;
        var before = GC.GetTotalMemory(true);
        client.Send(new byte[1 << 20]);
        rig.RunUntil(() => seen is not null, 10_000);
        Assert.Equal(DisconnectReason.ProtocolViolation, seen!.Value.Reason);
        Assert.False(seen.Value.ByRemote);
        rig.RunUntil(() => client.State == ConnectionState.Closed, 5000);
        Assert.Equal(DisconnectReason.ProtocolViolation, client.CloseInfo.Reason);
        Assert.True(client.CloseInfo.ByRemote);
        Assert.True(GC.GetTotalMemory(true) - before < 4 << 20, "the receiver must not hold the oversize message");
        Assert.Equal(0, server.ConnectionCount);
    }

    [Fact]
    public void SpoofedRequestsLeaveTheServerWithNoState()
    {
        var rig = new Rig();
        var serverSocket = rig.Net.CreateSocket();
        var server = UdpTransport.Listen(serverSocket, null, rig.Clock);
        var replies = rig.Net.CreateSocket(); // stands in for every spoofed address; it never answers
        var request = new byte[Wire.MinHandshakeSize];
        request[0] = Wire.Header(Wire.KindRequest);
        var response = new byte[Wire.MinHandshakeSize];
        response[0] = Wire.Header(Wire.KindResponse);
        var rng = new Random(3);
        for (var i = 0; i < 5000; i++)
        {
            var spoof = Rig.Address(1000 + (i % 60000));
            BinaryPrimitives.WriteUInt64LittleEndian(request.AsSpan(1), (ulong)rng.NextInt64());
            rig.Net.Inject(request, spoof, serverSocket.LocalAddress);
            // A response with a made-up cookie, as a blind attacker would send.
            BinaryPrimitives.WriteUInt64LittleEndian(response.AsSpan(1), (ulong)rng.NextInt64());
            rng.NextBytes(response.AsSpan(9, Wire.CookieSize));
            rig.Net.Inject(response, spoof, serverSocket.LocalAddress);
            if (i % 1000 == 999)
            {
                server.Pump();
            }
        }

        server.Pump();
        Assert.Equal(0, server.ConnectionCount);
        Assert.Equal(0, server.PendingHandshakes);
        Assert.Empty(server.Connections);
        Assert.False(server.TryAccept(out _));
        Assert.Equal(5000, server.ChallengesSent);
        Assert.Equal(5000, server.DatagramsIgnored);
        _ = replies;
    }

    [Fact]
    public void ADatagramForAConnectionTheServerNeverHadIsAnsweredWithADisconnectSoARestartIsNoticedAtOnce()
    {
        var rig = new Rig();
        var serverSocket = rig.Net.CreateSocket();
        var server = UdpTransport.Listen(serverSocket, null, rig.Clock);
        var probe = rig.Net.CreateSocket();
        var data = new byte[ReliableCore.HeaderSize + 40];
        data[0] = Wire.Header(Wire.KindData);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(1), 0xDEADBEEF);
        probe.Send(data, serverSocket.LocalAddress);

        // A disconnect for an unknown connection is not answered: two peers could otherwise bounce it forever.
        var bye = new byte[Wire.DisconnectSize];
        bye[0] = Wire.Header(Wire.KindDisconnect);
        BinaryPrimitives.WriteUInt32LittleEndian(bye.AsSpan(1), 0xDEADBEEF);
        probe.Send(bye, serverSocket.LocalAddress);
        server.Pump();

        var buf = new byte[2048];
        var from = new SocketAddress(AddressFamily.InterNetwork);
        var n = probe.Receive(buf, from);
        Assert.Equal(Wire.DisconnectSize, n);
        Assert.True(n < data.Length);
        Assert.Equal(Wire.Header(Wire.KindDisconnect), buf[0]);
        Assert.Equal(0xDEADBEEFu, BinaryPrimitives.ReadUInt32LittleEndian(buf.AsSpan(1)));
        Assert.Equal((byte)DisconnectReason.Shutdown, buf[5]);
        Assert.Equal(-1, probe.Receive(buf, from));
        Assert.Equal(2, server.DatagramsIgnored);
    }

    [Fact]
    public void AClientOfAServerThatStartedAgainClosesWithinAKeepAliveNotTheTimeout()
    {
        // Real sockets: a second listener binds the port the first one had.
        var firstSocket = new SystemDatagramSocket(new IPEndPoint(IPAddress.Loopback, 0));
        var first = UdpTransport.Listen(firstSocket);
        var port = ((IPEndPoint)new IPEndPoint(IPAddress.Loopback, 0).Create(firstSocket.LocalAddress)).Port;
        var options = new UdpOptions { KeepAliveMs = 100, TimeoutMs = 20_000 };
        var client = UdpTransport.Connect(new IPEndPoint(IPAddress.Loopback, port), options);
        var clock = System.Diagnostics.Stopwatch.StartNew();
        while (client.State != ConnectionState.Connected && clock.ElapsedMilliseconds < 5000)
        {
            first.Pump();
            client.Pump();
            Thread.Sleep(2);
        }

        Assert.Equal(ConnectionState.Connected, client.State);

        // The first server dies without a word (its connections are dropped, not disconnected), and a new one starts on the same port.
        firstSocket.Dispose();
        using var restarted = UdpTransport.Listen(new IPEndPoint(IPAddress.Loopback, port));
        clock.Restart();
        while (client.State != ConnectionState.Closed && clock.ElapsedMilliseconds < 5000)
        {
            restarted.Pump();
            client.Pump();
            Thread.Sleep(2);
        }

        Assert.Equal(ConnectionState.Closed, client.State);
        Assert.True(client.CloseInfo.ByRemote);
        Assert.True(clock.ElapsedMilliseconds < 2000, $"took {clock.ElapsedMilliseconds} ms");
    }

    [Fact]
    public void AChallengeIsSmallerThanTheRequestSoTheServerCannotBeUsedToAmplify()
    {
        Assert.True(Wire.ChallengeSize < Wire.MinHandshakeSize);
        Assert.True(Wire.AcceptSize < Wire.MinHandshakeSize);
        var rig = new Rig();
        var serverSocket = rig.Net.CreateSocket();
        var server = UdpTransport.Listen(serverSocket, null, rig.Clock);
        var probe = rig.Net.CreateSocket();
        var tooShort = new byte[Wire.MinHandshakeSize - 1];
        tooShort[0] = Wire.Header(Wire.KindRequest);
        probe.Send(tooShort, serverSocket.LocalAddress);
        var ok = new byte[Wire.MinHandshakeSize];
        ok[0] = Wire.Header(Wire.KindRequest);
        probe.Send(ok, serverSocket.LocalAddress);
        server.Pump();
        Assert.Equal(1, server.ChallengesSent);
        var buf = new byte[2048];
        var from = new SocketAddress(AddressFamily.InterNetwork);
        var n = probe.Receive(buf, from);
        Assert.Equal(Wire.ChallengeSize, n);
        Assert.Equal(-1, probe.Receive(buf, from));
    }

    [Fact]
    public void ConnectingToNobodyTimesOutWithAReason()
    {
        var rig = new Rig();
        var lonely = UdpTransport.Connect(rig.Net.CreateSocket(), Rig.Address(9), new UdpOptions { ConnectTimeoutMs = 1000 }, rig.Clock);
        DisconnectInfo? info = null;
        lonely.Disconnected += (_, i) => info = i;
        rig.Pumps.Add(lonely.Pump);
        rig.Run(1500);
        Assert.Equal(new DisconnectInfo(DisconnectReason.ConnectTimeout, false), info);
        Assert.Equal(ConnectionState.Closed, lonely.State);
    }

    [Fact]
    public void DisconnectReasonsReachBothEnds()
    {
        var rig = new Rig();
        var (server, client, accepted) = rig.Connected();
        DisconnectInfo? atServer = null;
        DisconnectInfo? atClient = null;
        accepted.Disconnected += (_, i) => atServer = i;
        client.Disconnected += (_, i) => atClient = i;
        client.Send([1]);
        accepted.Disconnect(DisconnectReason.Kicked, flush: true);
        rig.RunUntil(() => atClient is not null && atServer is not null, 3000);
        Assert.Equal(new DisconnectInfo(DisconnectReason.Kicked, false), atServer);
        Assert.Equal(DisconnectReason.Kicked, atClient!.Value.Reason);
        Assert.True(atClient.Value.ByRemote);
        rig.Run(50);
        Assert.Equal(0, server.ConnectionCount);
        client.Send([2]); // sending on a closed connection is ignored, never throws
    }

    [Fact]
    public void AnUnresponsivePeerTimesOutAndIsReported()
    {
        var rig = new Rig();
        var (server, client, accepted) = rig.Connected(new UdpOptions { TimeoutMs = 2000, KeepAliveMs = 200 }, new UdpOptions { TimeoutMs = 2000, KeepAliveMs = 200 });
        DisconnectInfo? atServer = null;
        accepted.Disconnected += (_, i) => atServer = i;
        // The client goes silent without a word (the process died).
        rig.Pumps.Remove(rig.Pumps[1]);
        rig.Run(3000);
        Assert.Equal(DisconnectReason.Timeout, atServer!.Value.Reason);
        Assert.False(atServer.Value.ByRemote);
        Assert.Equal(0, server.ConnectionCount);
        _ = client;
    }

    [Fact]
    public void KeepaliveHoldsAnIdleConnectionOpen()
    {
        var rig = new Rig();
        var (_, client, accepted) = rig.Connected(new UdpOptions { TimeoutMs = 2000, KeepAliveMs = 300 }, new UdpOptions { TimeoutMs = 2000, KeepAliveMs = 300 });
        rig.Run(10_000, 20);
        Assert.Equal(ConnectionState.Connected, client.State);
        Assert.Equal(ConnectionState.Connected, accepted.State);
    }

    [Fact]
    public void AFullServerRejectsWithAReason()
    {
        var rig = new Rig();
        var serverSocket = rig.Net.CreateSocket();
        var server = UdpTransport.Listen(serverSocket, new UdpOptions { MaxConnections = 1 }, rig.Clock);
        rig.Pumps.Add(server.Pump);
        var a = UdpTransport.Connect(rig.Net.CreateSocket(), serverSocket.LocalAddress, null, rig.Clock);
        var b = UdpTransport.Connect(rig.Net.CreateSocket(), serverSocket.LocalAddress, null, rig.Clock);
        rig.Pumps.Add(a.Pump);
        rig.Pumps.Add(b.Pump);
        rig.Run(500);
        var states = new[] { a.State, b.State };
        Assert.Contains(ConnectionState.Connected, states);
        var loser = a.State == ConnectionState.Closed ? a : b;
        Assert.Equal(ConnectionState.Closed, loser.State);
        Assert.Equal(DisconnectReason.ServerFull, loser.CloseInfo.Reason);
        Assert.Equal(1, server.ConnectionCount);
    }

    [Fact]
    public void AnUnknownProtocolVersionIsRejected()
    {
        var rig = new Rig();
        var serverSocket = rig.Net.CreateSocket();
        var server = UdpTransport.Listen(serverSocket, null, rig.Clock);
        var probe = rig.Net.CreateSocket();
        var request = new byte[Wire.MinHandshakeSize];
        request[0] = (byte)((Wire.Version + 1) << 4 | Wire.KindRequest);
        probe.Send(request, serverSocket.LocalAddress);
        server.Pump();
        var buf = new byte[64];
        var n = probe.Receive(buf, new SocketAddress(AddressFamily.InterNetwork));
        Assert.Equal(Wire.RejectSize, n);
        Assert.Equal(Wire.KindReject, buf[0] & 0xF);
        Assert.Equal((byte)DisconnectReason.VersionMismatch, buf[9]);
    }

    [Fact]
    public void UnreliableMessagesAreDeliveredAtMostOnce()
    {
        var rig = new Rig();
        var (_, client, accepted) = rig.Connected(conditions: new NetworkConditions { DuplicateRate = 0.5, Seed = 3 });
        var server = (UdpConnection)accepted;
        for (var i = 0; i < 50; i++)
        {
            client.SendUnreliable([(byte)i]);
            rig.Step(5);
        }

        var seen = new List<byte>();
        rig.RunUntil(() =>
        {
            while (server.TryReceiveUnreliable(out var m))
            {
                seen.Add(m[0]);
            }

            return false;
        }, 500);
        Assert.Equal(50, seen.Count);
        Assert.Equal(50, seen.Distinct().Count());
    }

    [Fact]
    public void AReplayedDatagramIsDroppedAndDoesNotRefreshTheTimeout()
    {
        var sender = new ReliableCore(new UdpOptions(), 0);
        Assert.True(sender.Enqueue([1]));
        var packet = new byte[1200];
        var len = sender.Build(packet, 0, 0);
        var receiver = new ReliableCore(new UdpOptions(), 0);
        Assert.Equal(PacketResult.Accepted, receiver.OnPacket(packet.AsSpan(0, len), 1));
        Assert.Equal(PacketResult.Dropped, receiver.OnPacket(packet.AsSpan(0, len), 2));
        Assert.Equal(1, receiver.LastReceive);
    }

    [Fact]
    public void RealSocketsOnLoopbackWork()
    {
        using var server = UdpTransport.Listen(new IPEndPoint(IPAddress.Loopback, 0));
        var port = ((IPEndPoint)server.LocalEndPoint).Port;
        var client = UdpTransport.Connect(new IPEndPoint(IPAddress.Loopback, port));
        INetConnection? accepted = null;
        var big = Rig.Pattern(500_000, 5);
        client.Send(big);
        var got = false;
        var deadline = Environment.TickCount64 + 10_000;
        while (!got && Environment.TickCount64 < deadline)
        {
            server.Pump();
            client.Pump();
            accepted ??= server.TryAccept(out var a) ? a : null;
            if (accepted is not null && accepted.TryReceive(out var m))
            {
                Assert.True(m.SequenceEqual(big));
                accepted.Send([42]);
                got = true;
            }

            Thread.Sleep(1);
        }

        Assert.True(got);
        var reply = false;
        while (!reply && Environment.TickCount64 < deadline)
        {
            server.Pump();
            client.Pump();
            reply = client.TryReceive(out var r) && r.Length == 1 && r[0] == 42;
            Thread.Sleep(1);
        }

        Assert.True(reply);
        client.Disconnect(flush: true);
        for (var i = 0; i < 50 && server.ConnectionCount > 0; i++)
        {
            server.Pump();
            client.Pump();
            Thread.Sleep(5);
        }

        Assert.Equal(0, server.ConnectionCount);
    }

    [Fact]
    public void SteadyStateSendAndReceiveAllocatesNothing()
    {
        var rig = new Rig();
        var (_, client, accepted) = rig.Connected();
        var payload = Rig.Pattern(200, 1);
        void Round()
        {
            for (var i = 0; i < 10; i++)
            {
                client.Send(payload);
                accepted.Send(payload);
            }

            rig.Step(5);
            while (accepted.TryReceive(out _))
            {
            }

            while (client.TryReceive(out _))
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
        Assert.True(allocated == 0, $"{allocated} bytes allocated over 400 rounds of 20 messages");
    }

    [Fact]
    public void RealSocketsSteadyStateAllocatesNothing()
    {
        using var server = UdpTransport.Listen(new IPEndPoint(IPAddress.Loopback, 0));
        var client = UdpTransport.Connect(new IPEndPoint(IPAddress.Loopback, ((IPEndPoint)server.LocalEndPoint).Port));
        INetConnection? accepted = null;
        var deadline = Environment.TickCount64 + 5000;
        while ((accepted is null || client.State != ConnectionState.Connected) && Environment.TickCount64 < deadline)
        {
            server.Pump();
            client.Pump();
            accepted ??= server.TryAccept(out var a) ? a : null;
        }

        Assert.NotNull(accepted);
        var payload = Rig.Pattern(100, 2);
        void Round()
        {
            client.Send(payload);
            accepted!.Send(payload);
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
        Assert.True(allocated == 0, $"{allocated} bytes allocated over 300 real-socket rounds");
        client.Disconnect();
    }

    [Fact]
    public void BackpressureStallsTheSenderInsteadOfGrowingTheReceiver()
    {
        var rig = new Rig();
        var (_, client, accepted) = rig.Connected(new UdpOptions { MaxReceiveQueueBytes = 100_000 });
        for (var i = 0; i < 50; i++)
        {
            client.Send(new byte[50_000]);
        }

        rig.Run(5000);
        // Nothing has been read, so the receiver holds only about its queue limit plus the reorder window.
        Assert.True(accepted.Stats.MessagesReceived < 8, $"receiver accepted {accepted.Stats.MessagesReceived} messages with an unread queue");
        var n = 0;
        rig.RunUntil(() =>
        {
            while (accepted.TryReceive(out _))
            {
                n++;
            }

            return n == 50;
        }, 60_000);
        Assert.Equal(50, n);
    }

    [Fact]
    public void ABloatedSendQueueClosesTheConnection()
    {
        var rig = new Rig();
        var (_, client, _) = rig.Connected(clientOptions: new UdpOptions { MaxMessageBytes = 1 << 20, MaxSendQueueBytes = 2 << 20 });
        DisconnectInfo? info = null;
        client.Disconnected += (_, i) => info = i;
        for (var i = 0; i < 5; i++)
        {
            client.Send(new byte[1 << 20]);
        }

        Assert.Equal(DisconnectReason.SendQueueFull, info!.Value.Reason);
        Assert.Equal(ConnectionState.Closed, client.State);
    }

    [Fact]
    public void BadOptionsAreRefused()
    {
        Assert.Throws<ArgumentException>(() => UdpTransport.Listen(new Rig().Net.CreateSocket(), new UdpOptions { ReceiveWindow = 100 }));
        Assert.Throws<ArgumentException>(() => UdpTransport.Listen(new Rig().Net.CreateSocket(), new UdpOptions { Mtu = 100 }));
    }
}
