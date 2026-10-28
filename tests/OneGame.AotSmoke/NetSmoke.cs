using System.Net;
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
        // The encrypted path: keys, the Noise handshake, AEAD packets and the identity the server learns, all under NativeAOT.
        var serverKey = OneGame.Core.NetKeyPair.Generate();
        var playerKey = OneGame.Core.NetKeyPair.Generate();
        var conditions = new NetworkConditions { LatencyMs = 20, JitterMs = 5, LossRate = 0.1, Seed = 3 };
        using var server = UdpTransport.Listen(new ConditionedDatagramSocket(new SystemDatagramSocket(new IPEndPoint(IPAddress.Loopback, 0)), conditions, clock), new UdpOptions { Security = NetSecurity.ForServer(serverKey, "smoke/1") }, clock);
        var client = UdpTransport.Connect((IPEndPoint)server.LocalEndPoint, new UdpOptions { Security = NetSecurity.ForClient([serverKey.PublicKey], "smoke/1", playerKey) }, clock);
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

        var known = accepted is { IsEncrypted: true } && accepted.RemoteKey == NetSecurity.ForClient([serverKey.PublicKey], "smoke/1", playerKey).IdentityKeyFor(serverKey.PublicKey) && client.RemoteKey == serverKey.PublicKey;
        client.Disconnect();
        return !echoed ? "udp echo failed" : known ? null : "the secure link did not authenticate both ends";
    }

    private static string? WebSocket()
    {
        using var server = WebSocketTransport.Listen(new IPEndPoint(IPAddress.Loopback, 0));
        var client = WebSocketTransport.Connect((IPEndPoint)server.LocalEndPoint);
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
