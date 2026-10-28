using System.Buffers.Binary;
using OneGame.Core.Tests.Fuzzing;

using OneGame.Net.Tests;

namespace OneGame.Net.Tests.Fuzzing;

/// <summary>
/// The transport parsers as functions of a byte array: the UDP packet parser and reassembler (alone and behind a listener), the
/// WebSocket frame parser fed in odd chunk sizes, and the HTTP upgrade parsers. Any input returns normally.
/// </summary>
public static class NetFuzzTargets
{
    public static IReadOnlyList<FuzzTargets.Target> All { get; } =
    [
        new FuzzTargets.Target("net-udp-packet", RunPacket, UdpSeeds),
        new FuzzTargets.Target("net-reassembler", RunStream, StreamSeeds),
        new FuzzTargets.Target("net-udp-endpoints", RunEndpoints, EndpointSeeds),
        new FuzzTargets.Target("net-udp-secure", RunSecureUdp, SecureUdpSeeds),
        new FuzzTargets.Target("net-ws-frames", RunFrames, FrameSeeds),
        new FuzzTargets.Target("net-ws-handshake", RunHandshake, HandshakeSeeds),
        new FuzzTargets.Target("net-ws-secure-handshake", RunSecureHandshake, SecureHandshakeSeeds),
        new FuzzTargets.Target("net-ws-secure-records", RunRawRecords, RawRecordSeeds),
        new FuzzTargets.Target("net-ws-secure-authenticated", RunAuthenticatedRecords, AuthenticatedSeeds),
    ];

    private static byte[] Pat(int length, int seed)
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

    private static readonly UdpOptions Small = new() { Security = NetSecurity.Insecure, MaxMessageBytes = 8192, MaxSendQueueBytes = 64 * 1024, MaxReceiveQueueBytes = 32 * 1024, ReceiveWindow = 16 };

    private static byte[] Packet(int messages, int size, int seed, out ReliableCore core)
    {
        core = new ReliableCore(Small, 0);
        for (var i = 0; i < messages; i++)
        {
            _ = core.Enqueue(Pat(size, seed + i));
        }

        core.EnqueueUnreliable([1, 2, 3]);
        var buf = new byte[Small.Mtu];
        return buf[..core.Build(buf, 7, 100)];
    }

    private static IEnumerable<byte[]> UdpSeeds()
    {
        yield return Packet(1, 10, 1, out _);
        yield return Packet(5, 150, 2, out _);
        yield return Packet(1, 3000, 3, out _);
        yield return new byte[ReliableCore.HeaderSize];
    }

    private static bool RunPacket(byte[] input)
    {
        var core = new ReliableCore(Small, 0);
        // Send something first so acknowledgements in the input have data to refer to.
        _ = core.Enqueue(new byte[100]);
        var scratch = new byte[Small.Mtu];
        _ = core.Build(scratch, 7, 1);
        var result = core.OnPacket(input, 5);
        while (core.TryReceive(out _))
        {
        }

        _ = core.Build(scratch, 7, 1000);
        core.Release();
        return result == PacketResult.Accepted;
    }

    // A stream of datagrams: u16 length, bytes, repeated.
    private static IEnumerable<byte[]> StreamSeeds()
    {
        for (var big = 0; big < 2; big++)
        {
            var sender = new ReliableCore(Small, 0);
            for (var i = 0; i < 4; i++)
            {
                _ = sender.Enqueue(Pat(big == 0 ? 50 : 5000, i));
            }

            var ms = new MemoryStream();
            var buf = new byte[Small.Mtu];
            for (var n = sender.Build(buf, 7, 0); n > 0; n = sender.Build(buf, 7, 0))
            {
                ms.Write(BitConverter.GetBytes((ushort)n));
                ms.Write(buf, 0, n);
            }

            yield return ms.ToArray();
        }
    }

    private static bool RunStream(byte[] input)
    {
        var core = new ReliableCore(Small, 0);
        var pos = 0;
        var accepted = 0;
        var t = 1L;
        while (pos + 2 <= input.Length)
        {
            var len = BinaryPrimitives.ReadUInt16LittleEndian(input.AsSpan(pos));
            pos += 2;
            len = (ushort)Math.Min(len, input.Length - pos);
            if (core.OnPacket(input.AsSpan(pos, len), t++) == PacketResult.Accepted)
            {
                accepted++;
            }

            pos += len;
            while (core.TryReceive(out var m))
            {
                if (m.Length > Small.MaxMessageBytes)
                {
                    throw new InvalidOperationException("message larger than the limit delivered");
                }
            }
        }

        core.Release();
        return accepted > 0;
    }

    // Datagrams thrown at a listener and at a connecting client, over the in-memory network.
    private static IEnumerable<byte[]> EndpointSeeds()
    {
        var req = new byte[Wire.MinHandshakeSize];
        req[0] = Wire.Header(Wire.KindRequest);
        yield return req;
        var resp = new byte[Wire.MinHandshakeSize];
        resp[0] = Wire.Header(Wire.KindResponse);
        yield return resp;
        yield return Packet(2, 40, 9, out _);
        yield return [Wire.Header(Wire.KindDisconnect), 1, 0, 0, 0, 1, 0];
    }

    private static bool RunEndpoints(byte[] input)
    {
        var clock = new ManualNetClock();
        var net = new MemoryDatagramNetwork();
        var serverSocket = net.CreateSocket();
        using var server = UdpTransport.Listen(serverSocket, Small, clock);
        var clientSocket = net.CreateSocket();
        var client = UdpTransport.Connect(clientSocket, serverSocket.LocalAddress, Small, clock);
        var attacker = net.CreateSocket();
        for (var i = 0; i < 20; i++)
        {
            clock.Advance(5);
            server.Pump();
            client.Pump();
        }

        // The same bytes to the server, to the client, and to the server again after patching in the real connection id.
        net.Inject(input, attacker.LocalAddress, serverSocket.LocalAddress);
        net.Inject(input, serverSocket.LocalAddress, clientSocket.LocalAddress);
        if (server.Connections.Count > 0 && input.Length >= 5)
        {
            var patched = (byte[])input.Clone();
            BinaryPrimitives.WriteUInt32LittleEndian(patched.AsSpan(1), server.Connections[0].ConnectionId);
            net.Inject(patched, clientSocket.LocalAddress, serverSocket.LocalAddress);
        }

        for (var i = 0; i < 20; i++)
        {
            clock.Advance(5);
            server.Pump();
            client.Pump();
            while (server.TryAccept(out var c))
            {
                while (c.TryReceive(out _))
                {
                }
            }

            while (client.TryReceive(out _))
            {
            }
        }

        client.Disconnect();

        // A seed may legitimately close the connection (the disconnect seed does); surviving the input is the pass condition.
        return true;
    }

    // ---- Secure UDP: a Required listener and a Required client, with datagrams injected at every stage of the handshake. ----

    private static readonly NetKeyPair SecureServerKey = NetKeyPair.FromPrivateKey(Pat(32, 71));
    private static readonly NetKeyPair SecurePlayerKey = NetKeyPair.FromPrivateKey(Pat(32, 72));

    private static UdpOptions SecureServerOptions() => new() { Security = new NetSecurity { GameId = "fuzz/1", ServerKeys = [SecureServerKey] }, MaxMessageBytes = 8192, ReceiveWindow = 16 };

    private static UdpOptions SecureClientOptions() => new() { Security = NetSecurity.ForClient([SecureServerKey.PublicKey], "fuzz/1", SecurePlayerKey), MaxMessageBytes = 8192, ReceiveWindow = 16 };

    // Layout: pre-steps (how far the handshake got before the first injection), flags (bit 0: the server stops answering, so the
    // client stays in Connecting), then items of selector, u16 length, body. Selector bit 0: send to the server (else to the client);
    // bit 1: from the client's own address (else from a stranger / from the server); bit 2: put the client's nonce in bytes 1..8;
    // bit 3: put the live connection id in bytes 1..4.
    private static byte[] SecureItem(byte selector, byte[] body)
    {
        var item = new byte[3 + body.Length];
        item[0] = selector;
        BinaryPrimitives.WriteUInt16LittleEndian(item.AsSpan(1), (ushort)body.Length);
        body.CopyTo(item, 3);
        return item;
    }

    private static IEnumerable<byte[]> SecureUdpSeeds()
    {
        // Real traffic of one handshake and a few data packets, recorded from both ends.
        var clock = new ManualNetClock();
        var net = new MemoryDatagramNetwork();
        var serverSocket = net.CreateSocket();
        var serverTap = new RecordingSocket(serverSocket);
        var clientTap = new RecordingSocket(net.CreateSocket());
        using var server = UdpTransport.Listen(serverTap, SecureServerOptions(), clock);
        var client = UdpTransport.Connect(clientTap, serverSocket.LocalAddress, SecureClientOptions(), clock);
        client.Send(Pat(40, 3));
        for (var i = 0; i < 60; i++)
        {
            clock.Advance(5);
            server.Pump();
            client.Pump();
        }

        client.Disconnect();
        var toServer = clientTap.Sent;
        var toClient = serverTap.Sent;
        foreach (var d in toServer.Take(6))
        {
            yield return [.. new byte[] { 0, 0 }, .. SecureItem(0b1101, d)];
        }

        foreach (var d in toClient.Take(6))
        {
            yield return [.. new byte[] { 0, 1 }, .. SecureItem(0b1100, d)];
            yield return [.. new byte[] { 6, 0 }, .. SecureItem(0b1100, d)];
        }

        yield return [.. new byte[] { 0, 1 }, .. SecureItem(0b0100, [Wire.Header(Wire.KindAccept, secure: true), 0, 0, 0, 0, 0, 0, 0, 0])];
        yield return [.. new byte[] { 0, 1 }, .. SecureItem(0b0100, [Wire.Header(Wire.KindReject, secure: true), 0, 0, 0, 0, 0, 0, 0, 0, (byte)DisconnectReason.HandshakeFailed])];
    }

    // Keeps a copy of every datagram sent through it, to make seeds from real traffic.
    private sealed class RecordingSocket(IDatagramSocket inner) : IDatagramSocket
    {
        public List<byte[]> Sent { get; } = [];

        public System.Net.SocketAddress LocalAddress => inner.LocalAddress;

        public void Send(ReadOnlySpan<byte> datagram, System.Net.SocketAddress destination)
        {
            Sent.Add(datagram.ToArray());
            inner.Send(datagram, destination);
        }

        public int Receive(Span<byte> buffer, System.Net.SocketAddress from) => inner.Receive(buffer, from);

        public void Dispose() => inner.Dispose();
    }

    private static bool RunSecureUdp(byte[] input)
    {
        if (input.Length < 2)
        {
            return false;
        }

        var clock = new ManualNetClock();
        var net = new MemoryDatagramNetwork();
        var serverSocket = net.CreateSocket();
        var clientSocket = net.CreateSocket();
        var stranger = net.CreateSocket();
        using var server = UdpTransport.Listen(serverSocket, SecureServerOptions(), clock);
        var client = UdpTransport.Connect(clientSocket, serverSocket.LocalAddress, SecureClientOptions(), clock);
        var silent = (input[1] & 1) != 0;

        void Step()
        {
            clock.Advance(5);
            if (!silent)
            {
                server.Pump();
            }

            client.Pump();
            while (server.TryAccept(out var c))
            {
                while (c.TryReceive(out _))
                {
                }
            }

            while (client.TryReceive(out _))
            {
            }
        }

        for (var i = 0; i < input[0] % 40; i++)
        {
            Step();
        }

        var pos = 2;
        var any = false;
        while (pos + 3 <= input.Length)
        {
            var selector = input[pos];
            var len = Math.Min(BinaryPrimitives.ReadUInt16LittleEndian(input.AsSpan(pos + 1)), input.Length - pos - 3);
            var datagram = input.AsSpan(pos + 3, len).ToArray();
            pos += 3 + len;
            any = true;
            if ((selector & 4) != 0 && datagram.Length >= 9)
            {
                BinaryPrimitives.WriteUInt64LittleEndian(datagram.AsSpan(1), client.Nonce);
            }

            if ((selector & 8) != 0 && datagram.Length >= 5)
            {
                var id = client.ConnectionId != 0 ? client.ConnectionId : server.Connections.Count > 0 ? server.Connections[0].ConnectionId : 0;
                BinaryPrimitives.WriteUInt32LittleEndian(datagram.AsSpan(1), id);
            }

            if ((selector & 1) != 0)
            {
                net.Inject(datagram, (selector & 2) != 0 ? clientSocket.LocalAddress : stranger.LocalAddress, serverSocket.LocalAddress);
            }
            else
            {
                net.Inject(datagram, serverSocket.LocalAddress, clientSocket.LocalAddress);
            }

            Step();
        }

        for (var i = 0; i < 20; i++)
        {
            Step();
        }

        client.Disconnect();
        return any;
    }

    private static IEnumerable<byte[]> FrameSeeds()
    {
        var buf = new byte[70_000];
        // First byte: chunk size. Second byte: 1 for a server's view (client frames masked), 0 for a client's view.
        foreach (var size in new[] { 0, 5, 200, 70 })
        {
            var n = WsFrames.Write(buf, WsFrames.OpBinary, true, Pat(size, size), [1, 2, 3, 4]);
            yield return [.. new byte[] { 7, 1 }, .. buf[..n]];
        }

        var frag = new List<byte> { 3, 0 };
        frag.AddRange(buf[..WsFrames.Write(buf, WsFrames.OpBinary, false, Pat(30, 1), default)]);
        frag.AddRange(buf[..WsFrames.Write(buf, WsFrames.OpPing, true, [1, 2], default)]);
        frag.AddRange(buf[..WsFrames.Write(buf, WsFrames.OpContinuation, true, Pat(30, 2), default)]);
        frag.AddRange(buf[..WsFrames.Write(buf, WsFrames.OpClose, true, [0x03, 0xE8, 0x68, 0x69], default)]);
        yield return [.. frag];
    }

    private static bool RunFrames(byte[] input)
    {
        if (input.Length < 2)
        {
            return false;
        }

        var chunk = Math.Max(1, (int)input[0]);
        var reader = new WsFrameReader(requireMask: (input[1] & 1) != 0, 100_000);
        var data = input.AsSpan(2);
        var messages = 0;
        while (data.Length > 0)
        {
            var piece = data[..Math.Min(chunk, data.Length)];
            data = data[piece.Length..];
            while (piece.Length > 0)
            {
                var e = reader.Feed(piece, out var used);
                piece = piece[used..];
                if (e == WsEvent.Message)
                {
                    NetBuffers.Return(reader.TakeMessage(out _));
                    messages++;
                }
                else if (e == WsEvent.Error)
                {
                    reader.Release();
                    return false;
                }
            }
        }

        reader.Release();
        return messages > 0;
    }

    private static IEnumerable<byte[]> HandshakeSeeds()
    {
        yield return "GET /chat HTTP/1.1\r\nHost: x\r\nUpgrade: websocket\r\nConnection: Upgrade\r\nSec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\nSec-WebSocket-Version: 13\r\n\r\n"u8.ToArray();
        yield return "HTTP/1.1 101 Switching Protocols\r\nUpgrade: websocket\r\nConnection: Upgrade\r\nSec-WebSocket-Accept: s3pPLMBiTxaQ9kYGzzhZRbK+xOo=\r\n\r\n"u8.ToArray();
    }

    private static bool RunHandshake(byte[] input)
    {
        Span<byte> key = stackalloc byte[24];
        var request = WsHandshake.ParseRequest(input, new WebSocketOptions { AllowedOrigins = ["https://a.example"] }, key);
        var response = WsHandshake.ParseResponse(input, "dGhlIHNhbXBsZSBub25jZQ=="u8);
        _ = WsHandshake.FindEnd(input);
        return request == WsHandshake.RequestResult.Ok || response;
    }

    // ---- Secure WebSocket: the handshake messages, raw records, and records that authenticate but may be malformed. ----

    private static byte[] Stream(params byte[][] messages)
    {
        var ms = new MemoryStream();
        foreach (var m in messages)
        {
            ms.Write(BitConverter.GetBytes((ushort)m.Length));
            ms.Write(m);
        }

        return ms.ToArray();
    }

    private static IEnumerable<byte[]> Messages(byte[] input)
    {
        var pos = 0;
        while (pos + 2 <= input.Length)
        {
            var len = Math.Min(BinaryPrimitives.ReadUInt16LittleEndian(input.AsSpan(pos)), input.Length - pos - 2);
            pos += 2;
            yield return input.AsSpan(pos, len).ToArray();
            pos += len;
        }
    }

    private static IEnumerable<byte[]> SecureHandshakeSeeds()
    {
        var h = new SecureRig.Hand();
        yield return Stream(h.First);
        yield return Stream(h.First, [1, 2, 3]);
        yield return Stream([SecureConnection.VersionByte], new byte[97]);
    }

    // Anything a socket can say to a server that has just sent its challenge: it must never open and never throw.
    private static bool RunSecureHandshake(byte[] input)
    {
        var (server, wire) = SecureRig.NewServer();
        var any = false;
        foreach (var m in Messages(input))
        {
            any = true;
            wire.Send(m);
            server.Pump();
            if (server.IsOpen)
            {
                // Only a genuine handshake and confirm can open it; random bytes never will.
                throw new InvalidOperationException("opened from fuzz input");
            }
        }

        return any;
    }

    private static IEnumerable<byte[]> RawRecordSeeds()
    {
        yield return Stream(new byte[40], new byte[17], new byte[65535]);
        yield return Stream([0]);
    }

    // Records from the peer that were not made with its key: all fail authentication, and the connection ends.
    private static bool RunRawRecords(byte[] input)
    {
        var h = new SecureRig.Hand();
        h.ConfirmNow();
        var any = false;
        foreach (var m in Messages(input))
        {
            any = true;
            h.Wire.Send(m);
            _ = h.Server.TryReceive(out _);
        }

        return any;
    }

    // Each item is flag, u16 length, body. They are sealed with the real key, so only the parser's own rules stand in the way.
    private static IEnumerable<byte[]> AuthenticatedSeeds()
    {
        yield return [0, 3, 0, 1, 2, 3];
        yield return [1, 2, 0, 9, 9, 0, 1, 0, 7];
        yield return [2, 1, 0, 11];
        yield return [1, 0, 0, 3, 0, 0];
    }

    private static bool RunAuthenticatedRecords(byte[] input)
    {
        var h = new SecureRig.Hand();
        h.ConfirmNow();
        var pos = 0;
        var any = false;
        var delivered = 0L;
        while (pos + 3 <= input.Length)
        {
            var flag = input[pos];
            var len = Math.Min(BinaryPrimitives.ReadUInt16LittleEndian(input.AsSpan(pos + 1)), input.Length - pos - 3);
            h.Record(flag, input.AsSpan(pos + 3, len));
            pos += 3 + len;
            any = true;
            while (h.Server.TryReceive(out var m))
            {
                delivered += m.Length;
            }
        }

        if (delivered > pos)
        {
            throw new InvalidOperationException("more bytes delivered than were sent");
        }

        return any;
    }
}
