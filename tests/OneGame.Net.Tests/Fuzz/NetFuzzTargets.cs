using System.Buffers.Binary;
using OneGame.Core.Tests.Fuzzing;

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
        new FuzzTargets.Target("net-ws-frames", RunFrames, FrameSeeds),
        new FuzzTargets.Target("net-ws-handshake", RunHandshake, HandshakeSeeds),
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
}
