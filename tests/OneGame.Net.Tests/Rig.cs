using System.Net;
using System.Net.Sockets;

namespace OneGame.Net.Tests;

/// <summary>A virtual-time rig: a manual clock, an in-memory datagram network, and a list of things to pump each step.</summary>
internal sealed class Rig
{
    public ManualNetClock Clock { get; } = new();

    public MemoryDatagramNetwork Net { get; } = new();

    public List<Action> Pumps { get; } = [];

    public void Step(int ms = 5)
    {
        Clock.Advance(ms);
        foreach (var p in Pumps)
        {
            p();
        }
    }

    public void Run(int ms, int step = 5)
    {
        for (var t = 0; t < ms; t += step)
        {
            Step(step);
        }
    }

    public bool RunUntil(Func<bool> condition, int maxMs, int step = 5)
    {
        for (var t = 0; t < maxMs; t += step)
        {
            if (condition())
            {
                return true;
            }

            Step(step);
        }

        return condition();
    }

    /// <summary>A listener on the memory network, and a client connected to it.</summary>
    public (UdpListener Server, UdpConnection Client, INetConnection Accepted) Connected(UdpOptions? serverOptions = null, UdpOptions? clientOptions = null, NetworkConditions? conditions = null)
    {
        var serverSocket = Net.CreateSocket();
        var server = UdpTransport.Listen(Maybe(serverSocket, conditions), serverOptions, Clock);
        var client = UdpTransport.Connect(Maybe(Net.CreateSocket(), conditions), serverSocket.LocalAddress, clientOptions, Clock);
        Pumps.Add(server.Pump);
        Pumps.Add(client.Pump);
        INetConnection? accepted = null;
        Assert.True(RunUntil(() => (accepted ??= server.TryAccept(out var a) ? a : null) is not null && client.State == ConnectionState.Connected, 20_000), "handshake did not finish");
        return (server, client, accepted!);
    }

    private IDatagramSocket Maybe(IDatagramSocket s, NetworkConditions? c) => c is null ? s : new ConditionedDatagramSocket(s, c, Clock);

    public static SocketAddress Address(int port) => new IPEndPoint(IPAddress.Loopback, port).Serialize();

    public static byte[] Pattern(int length, int seed)
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
}
