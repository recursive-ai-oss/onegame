using System.Net;
using OneGame.Core;
using OneGame.Net;

namespace OneGame.AotSmoke;

/// <summary>Both transports over loopback: a UDP handshake and message round trip through the conditioner, and a WebSocket round trip.</summary>
internal static class NetSmoke
{
    public static string? Run()
    {
        return Udp() ?? WebSocket() ?? Identities();
    }

    // Server names, the canonical-key rule and the player registry's keep-or-forget policy, which a game uses on every connection.
    private static string? Identities()
    {
        var root = NetKeyPair.Generate();
        var named = NetSecurity.ForClient([NetKeyPair.Generate().PublicKey, NetKeyPair.Generate().PublicKey], "smoke", root, serverId: "eu-1.smoke");
        if (named.IdentityKeyFor(named.TrustedServerKeys[0]) != named.IdentityKeyFor(named.TrustedServerKeys[1]) || root.DeriveFor("a").PublicKey == root.DeriveFor("b").PublicKey)
        {
            return "a server name did not give a stable identity";
        }

        var players = new OneGame.Sync.PlayerRegistry(maxPlayers: 3);
        var first = players.PlayerFor(root.PublicKey, out _);
        _ = players.Activate(first);
        for (var i = 0; i < 10; i++)
        {
            _ = players.PlayerFor(NetKeyPair.Generate().PublicKey, out _);
        }

        return players.Count == 3 && players.TryGetPlayer(root.PublicKey, out var again) && again == first && OneGame.Sync.PlayerRegistry.TryLoad(players.Save(), out var loaded, 3) && loaded.IsActive(first)
            ? null
            : "the player registry forgot an active player";
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
