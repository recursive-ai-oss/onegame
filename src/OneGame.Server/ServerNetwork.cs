using System.Net;
using OneGame.Net;

namespace OneGame.Server;

/// <summary>A client that has just connected, as the authentication hook sees it.</summary>
/// <param name="Name">The name the connection will have in reports (transport and remote address).</param>
/// <param name="Transport"><c>udp</c> or <c>ws</c>.</param>
/// <param name="Address">The client's remote address as text.</param>
public sealed record ClientRequest(string Name, string Transport, string Address);

/// <summary>A bound listening address.</summary>
/// <param name="Transport"><c>udp</c> or <c>ws</c>.</param>
/// <param name="Address">The address text the listener bound.</param>
/// <param name="Port">The port; for a configured port of 0, the one the system picked.</param>
public sealed record ListenEndpoint(string Transport, string Address, int Port);

/// <summary>One connected client, for reports.</summary>
/// <param name="Id">Id of the connection on the server node.</param>
/// <param name="Name">Name in reports.</param>
/// <param name="Transport"><c>udp</c> or <c>ws</c>.</param>
/// <param name="Address">Remote address.</param>
/// <param name="State">Transport state.</param>
/// <param name="RttMs">Smoothed round-trip time.</param>
/// <param name="InboundLoss">Estimated share of the client's datagrams that never arrived.</param>
/// <param name="OutboundLoss">Share of our reliable transmissions that were repeats.</param>
/// <param name="Retransmits">Reliable fragments sent again.</param>
/// <param name="MessagesSent">Messages sent to the client.</param>
/// <param name="MessagesReceived">Messages received from the client.</param>
public sealed record ClientInfo(int Id, string Name, string Transport, string Address, string State, double RttMs, double InboundLoss, double OutboundLoss, long Retransmits, long MessagesSent, long MessagesReceived);

/// <summary>The recorded hash of one scope at one tick.</summary>
/// <param name="Scope">The scope id.</param>
/// <param name="Tick">The tick asked for.</param>
/// <param name="Hash">The hash of the whole scope as 16 hex digits, or null when that tick is not (or no longer) recorded.</param>
/// <param name="Views">The hash of what each connected client's replica must hold at that tick (its audience's view of the scope); equal to <c>Hash</c> when everything is visible to everyone.</param>
public sealed record ScopeHashAt(uint Scope, long Tick, string? Hash, string[] Views);

/// <summary>
/// The server's side of its network clients: listeners, the authentication decision, and <see cref="Node.Accept"/> for every accepted
/// connection, so the server node handles it like any other client. Closed connections are unsubscribed from every scope.
/// </summary>
internal sealed class ServerNetwork
{
    private readonly Node _server;
    private readonly List<INetListener> _listeners = [];
    private readonly List<string> _transports = [];
    private readonly List<ListenEndpoint> _endpoints = [];
    private readonly List<Remote> _remotes = [];
    private readonly Func<ClientRequest, Trust?> _authenticate;
    private readonly Action<string> _log;

    public ServerNetwork(Node server, Func<ClientRequest, Trust?> authenticate, Action<string> log)
    {
        _server = server;
        _authenticate = authenticate;
        _log = log;
    }

    public IReadOnlyList<ListenEndpoint> Endpoints => _endpoints;

    /// <summary>Called for a client whose connection closed, after its pending messages were handled and before the node forgets it.</summary>
    public Action<Connection>? Left { get; set; }

    public int Count => _remotes.Count;

    public void Listen(ListenConfig config)
    {
        if (!IPAddress.TryParse(config.Address, out var address))
        {
            throw new ServerConfigException($"listen address '{config.Address}' is not an IP address", null, 0);
        }

        var bind = new IPEndPoint(address, config.Port);
        INetListener listener = config.Transport == "udp" ? UdpTransport.Listen(bind) : WebSocketTransport.Listen(bind);
        _listeners.Add(listener);
        _transports.Add(config.Transport);
        var port = listener.LocalEndPoint is IPEndPoint bound ? bound.Port : config.Port;
        _endpoints.Add(new ListenEndpoint(config.Transport, config.Address, port));
        _log($"listening on {config.Transport}://{config.Address}:{port}");
    }

    /// <summary>Accepts new connections and moves messages toward the server node. Call before the node is pumped.</summary>
    public void PumpInbound()
    {
        for (var i = 0; i < _listeners.Count; i++)
        {
            _listeners[i].Pump();
            while (_listeners[i].TryAccept(out var connection))
            {
                Admit(connection, _transports[i]);
            }
        }

    }

    /// <summary>Moves what the server node wrote to the connections and drops the ones that closed. Call after publishing.</summary>
    public void PumpOutbound()
    {
        for (var i = 0; i < _listeners.Count; i++)
        {
            _listeners[i].Pump();
        }

        for (var i = _remotes.Count - 1; i >= 0; i--)
        {
            if (_remotes[i].Connection.State == ConnectionState.Closed)
            {
                Drop(i);
            }
        }
    }

    public ClientInfo[] Clients()
    {
        var list = new ClientInfo[_remotes.Count];
        for (var i = 0; i < list.Length; i++)
        {
            var r = _remotes[i];
            var s = r.Connection.Stats;
            list[i] = new ClientInfo(r.Accepted.Id, r.Name, r.Transport, r.Connection.RemoteName, r.Connection.State.ToString().ToLowerInvariant(), Math.Round(s.RttMs, 1), Math.Round(s.InboundLoss, 4), Math.Round(s.OutboundLoss, 4), s.Retransmits, s.MessagesSent, s.MessagesReceived);
        }

        return list;
    }

    /// <summary>Closes every connection (telling the clients why) and the listeners.</summary>
    public void Shutdown()
    {
        PumpOutbound();
        foreach (var r in _remotes)
        {
            r.Connection.Disconnect(DisconnectReason.Shutdown, flush: true);
        }

        for (var round = 0; round < 50 && _remotes.Exists(r => r.Connection.State != ConnectionState.Closed); round++)
        {
            foreach (var l in _listeners)
            {
                l.Pump();
            }

            Thread.Sleep(2);
        }

        foreach (var l in _listeners)
        {
            l.Dispose();
        }

        _listeners.Clear();
        _remotes.Clear();
    }

    private void Admit(INetConnection connection, string transport)
    {
        var request = new ClientRequest(transport + "://" + connection.RemoteName, transport, connection.RemoteName);
        var trust = _authenticate(request);
        if (trust is null)
        {
            _log($"client {request.Name} refused by the authentication hook");
            connection.Disconnect(DisconnectReason.Rejected);
            return;
        }

        var accepted = _server.Accept(connection, trust.Value, request.Name);
        _remotes.Add(new Remote(request.Name, transport, connection, accepted));
        _log($"client {request.Name} connected");
    }

    private void Drop(int index)
    {
        var r = _remotes[index];
        _remotes.RemoveAt(index);
        var info = r.Connection.CloseInfo;
        _log($"client {r.Name} left ({info.Reason}{(info.ByRemote ? ", by the client" : "")})");

        // Messages that arrived before the close are handled first, then the node stops publishing to the dead connection and forgets it.
        _server.Pump();
        Left?.Invoke(r.Accepted);
        _server.Detach(r.Accepted);
    }

    private sealed class Remote(string name, string transport, INetConnection connection, Connection accepted)
    {
        public string Name { get; } = name;

        public string Transport { get; } = transport;

        public INetConnection Connection { get; } = connection;

        public Connection Accepted { get; } = accepted;
    }
}
