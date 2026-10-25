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
/// <param name="Hash">The hash as 16 hex digits, or null when that tick is not (or no longer) recorded.</param>
public sealed record ScopeHashAt(uint Scope, long Tick, string? Hash);

/// <summary>
/// The server's side of its network clients. The sync layer's nodes talk through message channels; for every accepted connection this
/// makes a stub peer node joined to the server node by a loopback pair, and moves each message between that pair and the connection.
/// The server node then handles it like any other client.
/// </summary>
// Replace with Node.Accept(channel, trust) once the sync layer offers it: the stub node and the copy loops go away.
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

        for (var i = 0; i < _remotes.Count; i++)
        {
            var r = _remotes[i];
            while (r.Connection.TryReceive(out var message))
            {
                r.Link.Channel.Send(message);
            }
        }
    }

    /// <summary>Moves what the server node wrote to the connections and drops the ones that closed. Call after publishing.</summary>
    public void PumpOutbound()
    {
        for (var i = _remotes.Count - 1; i >= 0; i--)
        {
            var r = _remotes[i];
            try
            {
                while (r.Link.Channel.TryReceive(out var message))
                {
                    r.Connection.Send(message);
                }
            }
            catch (MessageTooLargeException ex)
            {
                _log($"client {r.Name} dropped: {ex.Message}");
                r.Connection.Disconnect(DisconnectReason.MessageTooLarge);
            }
        }

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
            list[i] = new ClientInfo(r.Link.Connection.Id, r.Name, r.Transport, r.Connection.RemoteName, r.Connection.State.ToString().ToLowerInvariant(), Math.Round(s.RttMs, 1), Math.Round(s.InboundLoss, 4), Math.Round(s.OutboundLoss, 4), s.Retransmits, s.MessagesSent, s.MessagesReceived);
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

        var stub = new Node("remote:" + request.Name);
        var link = stub.ConnectTo(_server, trust.Value, request.Name);
        _remotes.Add(new Remote(request.Name, transport, connection, link));
        _log($"client {request.Name} connected");
    }

    private void Drop(int index)
    {
        var r = _remotes[index];
        _remotes.RemoveAt(index);
        var info = r.Connection.CloseInfo;
        _log($"client {r.Name} left ({info.Reason}{(info.ByRemote ? ", by the client" : "")})");

        // Unsubscribe by the wire message (type 2, scope id) so the authority stops publishing to a dead connection.
        foreach (var scope in _server.AuthorityScopes)
        {
            var message = new byte[6];
            message[0] = 2;
            var n = 1;
            var v = scope.Id.Value;
            while (v >= 0x80)
            {
                message[n++] = (byte)(v | 0x80);
                v >>= 7;
            }

            message[n++] = (byte)v;
            r.Link.Channel.Send(message.AsSpan(0, n));
        }
    }

    private sealed class Remote(string name, string transport, INetConnection connection, Link link)
    {
        public string Name { get; } = name;

        public string Transport { get; } = transport;

        public INetConnection Connection { get; } = connection;

        public Link Link { get; } = link;
    }
}
