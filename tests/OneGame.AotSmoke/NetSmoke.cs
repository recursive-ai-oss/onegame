using System.Net;
using OneGame.Core;
using OneGame.Net;

namespace OneGame.AotSmoke;

/// <summary>Both transports over loopback: a UDP handshake and message round trip through the conditioner, and a WebSocket round trip.</summary>
internal static class NetSmoke
{
    public static string? Run()
    {
        return Udp() ?? WebSocket();
    }

    private static string? Udp()
    {
        var clock = new ManualNetClock();
        var conditions = new NetworkConditions { LatencyMs = 20, JitterMs = 5, LossRate = 0.1, Seed = 3 };
        using var server = UdpTransport.Listen(new ConditionedDatagramSocket(new SystemDatagramSocket(new IPEndPoint(IPAddress.Loopback, 0)), conditions, clock), Plain.Udp, clock);
        var client = UdpTransport.Connect((IPEndPoint)server.LocalEndPoint, Plain.Udp, clock);
        INetConnection? accepted = null;
        var payload = new byte[5000];
        new Random(1).NextBytes(payload);
        client.Send(payload);
        var echoed = false;
        for (var i = 0; i < 4000 && !echoed; i++)
        {
            clock.Advance(5);
            server.Pump();
            client.Pump();
            accepted ??= server.TryAccept(out var a) ? a : null;
            while (accepted is not null && accepted.TryReceive(out var m))
            {
                accepted.Send(m);
            }

            while (client.TryReceive(out var m))
            {
                echoed = m.SequenceEqual(payload);
            }

            Thread.Sleep(0);
        }

        client.Disconnect();
        return echoed ? null : "udp echo failed";
    }

    private static string? WebSocket()
    {
        // Encrypted: the records, the Noise handshake and the key types all have to survive trimming and AOT.
        var key = NetKeyPair.Generate();
        using var server = WebSocketTransport.Listen(new IPEndPoint(IPAddress.Loopback, 0), new WebSocketOptions { Security = NetSecurity.ForServer(key, "smoke") });
        var client = WebSocketTransport.Connect((IPEndPoint)server.LocalEndPoint, new WebSocketOptions { Security = NetSecurity.ForClient([key.PublicKey], "smoke") });
        INetConnection? accepted = null;
        var payload = new byte[70_000];
        new Random(2).NextBytes(payload);
        client.Send(payload);
        var echoed = false;
        var deadline = Environment.TickCount64 + 10_000;
        while (!echoed && Environment.TickCount64 < deadline)
        {
            server.Pump();
            client.Pump();
            accepted ??= server.TryAccept(out var a) ? a : null;
            while (accepted is not null && accepted.TryReceive(out var m))
            {
                accepted.Send(m);
            }

            while (client.TryReceive(out var m))
            {
                echoed = m.SequenceEqual(payload);
            }

            Thread.Sleep(1);
        }

        client.Disconnect();
        return echoed ? null : "websocket echo failed";
    }
}
