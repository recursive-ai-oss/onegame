using System.Net;
using System.Net.Sockets;
using OneGame.Core;
using OneGame.Host;
using OneGame.Net;

namespace OneGame.Server.Tests;

/// <summary>
/// A refusal that nobody authenticated (a Reject datagram anyone on the path can write) must not end a client's session for good.
/// A UDP relay between the client and a real server plays the path attacker: it answers the client's first requests with forged
/// rejections and then lets traffic through.
/// </summary>
public class ForgedRefusalTests : IDisposable
{
    private static readonly (string File, PeerKey Public) ServerKey = MakeKey();
    private readonly List<string> _folders = [];

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        foreach (var f in _folders)
        {
            try
            {
                Directory.Delete(f, true);
            }
            catch (IOException)
            {
            }
        }
    }

    private static (string, PeerKey) MakeKey()
    {
        var keys = KeyStore.Create(Path.Combine(Path.GetTempPath(), "onegame-refusal-tests-" + Guid.NewGuid().ToString("N")), "server");
        return (keys.PrivateFile, keys.PublicKey);
    }

    private static ServerConfig Config() =>
        ServerConfig.Parse($"name: net\nhashHistory: 512\nscopes: [{{ id: 1, seed: 7 }}]\nlisten:\n  - {{ transport: udp, port: 0, address: 127.0.0.1 }}\nsecurity:\n  key: {ServerKey.File}\n  game: net/1\n");

    // Sits between a client and a server. Answers the first few requests it sees with a forged Reject, forwards the rest both ways.
    private sealed class Relay : IDisposable
    {
        private readonly Socket _front = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        private readonly Socket _back = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        private readonly IPEndPoint _server;
        private EndPoint? _client;
        private readonly byte[] _buffer = new byte[2048];
        private int _forgeries;

        public Relay(int serverPort, int forgeries, byte reason)
        {
            _front.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            _back.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            _server = new IPEndPoint(IPAddress.Loopback, serverPort);
            _forgeries = forgeries;
            _reason = reason;
        }

        private readonly byte _reason;

        public int Port => ((IPEndPoint)_front.LocalEndPoint!).Port;

        public int Forged { get; private set; }

        public void Poll()
        {
            while (_front.Available > 0)
            {
                EndPoint from = new IPEndPoint(IPAddress.Any, 0);
                var n = _front.ReceiveFrom(_buffer, ref from);
                _client = from;
                var kind = _buffer[0] & 0xF;
                if (kind == 0 && _forgeries > 0)
                {
                    // Request: the nonce is visible in it, which is all a Reject needs.
                    _forgeries--;
                    Forged++;
                    var reject = new byte[10];
                    reject[0] = (byte)((_buffer[0] & 0xF0) | 4);
                    Array.Copy(_buffer, 1, reject, 1, 8);
                    reject[9] = _reason;
                    _ = _front.SendTo(reject, from);
                    continue;
                }

                if (_forgeries == 0 || kind != 0)
                {
                    _ = _back.SendTo(_buffer.AsSpan(0, n).ToArray(), _server);
                }
            }

            while (_back.Available > 0)
            {
                EndPoint from = new IPEndPoint(IPAddress.Any, 0);
                var n = _back.ReceiveFrom(_buffer, ref from);
                if (_client is not null)
                {
                    _ = _front.SendTo(_buffer.AsSpan(0, n).ToArray(), _client);
                }
            }
        }

        public void Dispose()
        {
            _front.Dispose();
            _back.Dispose();
        }
    }

    [Fact]
    public void ForgedRejectsDoNotEndTheSessionAndTheRealServerStillGetsThrough()
    {
        var host = new ServerHost(Config());
        host.Start();
        var serverPort = host.Endpoints.First(e => e.Transport == "udp").Port;
        using var relay = new Relay(serverPort, forgeries: 2, reason: (byte)DisconnectReason.HandshakeFailed);
        var world = host.CreateReplicaWorld(new ScopeId(1));
        var session = new RemoteSession(new ConnectOptions { Address = $"udp://127.0.0.1:{relay.Port}", Security = NetSecurity.ForClient([ServerKey.Public], "net/1") }, world);

        var closedEarly = false;
        var deadline = Environment.TickCount64 + 15_000;
        while (Environment.TickCount64 < deadline && session.Replica.State != ReplicaState.Live)
        {
            relay.Poll();
            host.PumpNetwork();
            host.StepOnce();
            session.Pump();
            closedEarly |= session.IsClosed;
            Thread.Sleep(2);
        }

        Assert.Equal(2, relay.Forged);
        Assert.False(closedEarly, "two forged rejections ended the session");
        Assert.Equal(ReplicaState.Live, session.Replica.State);
        Assert.Equal(1, session.Reconnects);
        host.Stop();
    }

    [Fact]
    public void TheSameRefusalOnThreeFreshAttemptsClosesTheSessionForAPause()
    {
        var host = new ServerHost(Config());
        host.Start();
        var serverPort = host.Endpoints.First(e => e.Transport == "udp").Port;
        using var relay = new Relay(serverPort, forgeries: int.MaxValue, reason: (byte)DisconnectReason.HandshakeFailed);
        var session = new RemoteSession(new ConnectOptions { Address = $"udp://127.0.0.1:{relay.Port}", Security = NetSecurity.ForClient([ServerKey.Public], "net/1") }, host.CreateReplicaWorld(new ScopeId(1)));

        var deadline = Environment.TickCount64 + 15_000;
        while (Environment.TickCount64 < deadline && !session.IsClosed)
        {
            relay.Poll();
            session.Pump();
            Assert.True(relay.Forged <= 3, "the session kept trying after the third refusal");
            Thread.Sleep(2);
        }

        Assert.True(session.IsClosed);
        Assert.Equal(3, relay.Forged);
        Assert.False(session.IsReconnecting);

        // And it stays quiet during the pause.
        var until = Environment.TickCount64 + 1500;
        while (Environment.TickCount64 < until)
        {
            relay.Poll();
            session.Pump();
            Thread.Sleep(2);
        }

        Assert.Equal(3, relay.Forged);
        host.Stop();
    }

    [Fact]
    public void ARefusalOverAnAuthenticatedLinkIsFinal()
    {
        // The server finishes the handshake, learns who the client is, and then declines to admit it: said inside the encrypted link.
        var host = new ServerHost(Config()) { Authenticate = _ => null };
        host.Start();
        var port = host.Endpoints.First(e => e.Transport == "udp").Port;
        var session = new RemoteSession(new ConnectOptions { Address = $"udp://127.0.0.1:{port}", Security = NetSecurity.ForClient([ServerKey.Public], "net/1") }, host.CreateReplicaWorld(new ScopeId(1)));
        for (var i = 0; i < 500 && !session.IsClosed; i++)
        {
            host.PumpNetwork();
            host.StepOnce();
            session.Pump();
            Thread.Sleep(2);
        }

        Assert.True(session.IsClosed, "a refusal over the authenticated link should be final");
        Assert.Equal(DisconnectReason.Rejected, session.Connection.CloseInfo.Reason);
        Assert.Equal(0, session.Reconnects);
        Assert.False(session.IsReconnecting);
        host.Stop();
    }
}
