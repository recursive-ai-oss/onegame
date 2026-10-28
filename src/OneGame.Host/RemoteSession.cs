using System.Net;
using OneGame.Core;
using OneGame.Net;
using OneGame.Sync;

namespace OneGame.Host;

/// <summary>Where a game connects instead of running its own world: a server address and how.</summary>
public sealed record ConnectOptions
{
    /// <summary><c>udp://host:port</c> (reliable UDP) or <c>ws://host:port</c> (WebSocket).</summary>
    public required string Address { get; init; }

    /// <summary>
    /// How the link is secured: <c>NetSecurity.ForClient(serverPublicKeys, gameId, identity)</c>, or <c>NetSecurity.Insecure</c> for tests and
    /// local tools. Required: a session opened with null throws <see cref="SecurityNotConfiguredException"/>. A client with no identity key gets one
    /// for the life of the session, so a reconnect is the same player; keep an identity file (<c>NetKeyPair.Load</c>) for the same player across runs.
    /// </summary>
    public NetSecurity? Security { get; init; }

    /// <summary>The scope to subscribe to.</summary>
    public uint Scope { get; init; } = 1;

    /// <summary>More scopes to follow over the same connection, for a server that serves several (its own, and others it relays). Their replicas are in <see cref="RemoteSession.Replicas"/> after the first.</summary>
    public IReadOnlyList<uint> AlsoScopes { get; init; } = [];

    /// <summary>The name this client has in reports.</summary>
    public string Name { get; init; } = "client";

    /// <summary>Network conditions to apply to this client's traffic (latency, loss, ...), or null for the real network as it is.</summary>
    public NetworkConditions? Conditions { get; init; }

    /// <summary>
    /// When the connection closes (the server died, restarted or timed out), keep trying to connect again and resubscribe to every followed
    /// scope; the replicas are rebuilt from fresh snapshots. On by default. A refusal the server made over the encrypted link (kicked, not
    /// admitted, wrong version) is final. A refusal that arrived before the link was authenticated (a Reject datagram, a failed handshake)
    /// could have been forged by anyone on the path, so it only counts when the next three fresh attempts are refused too; the session then
    /// reports <see cref="RemoteSession.IsClosed"/> and tries again after a minute.
    /// </summary>
    public bool Reconnect { get; init; } = true;

    /// <summary>How long to keep trying after the connection was lost before the session counts as closed for good. Attempts back off from 250 ms to 2 s.</summary>
    public int ReconnectWindowMs { get; init; } = 60_000;
}

/// <summary>One drift report: a replica whose state differed from the server's at the same tick.</summary>
/// <param name="Node">The node holding the replica.</param>
/// <param name="Scope">The scope.</param>
/// <param name="Tick">The tick at which the hashes differed.</param>
/// <param name="Expected">The server's hash (16 hex digits).</param>
/// <param name="Actual">The replica's hash.</param>
/// <param name="Differences">The first entities, tables or streams that differ, one line each.</param>
public sealed record DriftInfo(string Node, uint Scope, long Tick, string Expected, string Actual, string[] Differences);

/// <summary>One scope a client follows besides its main one (served by its server's relay), for <c>net_status</c>.</summary>
/// <param name="Scope">The scope id.</param>
/// <param name="Replica">Replica state: subscribing, live, resyncing, refused, failed.</param>
/// <param name="Tick">The replica's tick.</param>
/// <param name="Hash">The hash of what the replica holds, 16 hex digits.</param>
public sealed record FollowedScope(uint Scope, string Replica, long Tick, string Hash);

/// <summary>A snapshot of a client's connection, for <c>net_status</c>.</summary>
/// <param name="Address">The server address as given.</param>
/// <param name="Transport"><c>udp</c> or <c>ws</c>.</param>
/// <param name="Connection">Transport state: connecting, connected, closing, closed.</param>
/// <param name="CloseReason">Why the connection closed, or null while it is open.</param>
/// <param name="Scope">The subscribed scope.</param>
/// <param name="Replica">Replica state: subscribing, live, resyncing, refused, failed.</param>
/// <param name="Tick">The replica's tick.</param>
/// <param name="Hash">The hash of what the replica holds (its view of the scope) as 16 hex digits; the server's hash for this client's audience is equal at the same tick.</param>
/// <param name="FramesApplied">Frames applied so far.</param>
/// <param name="Resyncs">How often the replica re-subscribed after a gap or drift.</param>
/// <param name="Drift">Drift reports filed so far.</param>
/// <param name="RttMs">Smoothed round-trip time.</param>
/// <param name="Retransmits">Reliable fragments sent again.</param>
/// <param name="InboundLoss">Estimated share of the server's datagrams that never arrived.</param>
/// <param name="OutboundLoss">Share of our reliable transmissions that were repeats.</param>
/// <param name="MessagesSent">Messages sent to the server.</param>
/// <param name="MessagesReceived">Messages received from the server.</param>
/// <param name="Reports">The latest drift reports (at most 8), oldest first.</param>
/// <param name="Followed">The other scopes the client follows through its server, in subscription order.</param>
/// <param name="Reconnects">How many times the session connected again after losing the server.</param>
/// <param name="Reconnecting">True while the connection is lost and the session is trying to get it back.</param>
public sealed record NetStatus(
    string Address, string Transport, string Connection, string? CloseReason, uint Scope, string Replica, long Tick, string Hash,
    long FramesApplied, int Resyncs, int Drift, double RttMs, long Retransmits, double InboundLoss, double OutboundLoss, long MessagesSent, long MessagesReceived, DriftInfo[] Reports, FollowedScope[] Followed, int Reconnects = 0, bool Reconnecting = false);

/// <summary>
/// A game's connection to a server: a node holding one replica scope, reached through a transport connection
/// (<see cref="Node.ConnectVia"/>). <see cref="Pump"/> does all the work, on the caller's thread.
/// </summary>
public sealed class RemoteSession
{
    /// <summary>Connects an empty replica world to a server and subscribes to the scope. Pump it to make progress.</summary>
    /// <param name="options">Where to connect.</param>
    /// <param name="replicaWorld">An empty replica world with the server's entity types, tables and systems.</param>
    /// <param name="extraWorlds">Empty replica worlds, one for each of <see cref="ConnectOptions.AlsoScopes"/>, in order.</param>
    public RemoteSession(ConnectOptions options, World replicaWorld, IReadOnlyList<World>? extraWorlds = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(replicaWorld);
        Options = options;
        _security = WithIdentity(options.Security);
        if (OperatingSystem.IsBrowser())
        {
            // A page cannot resolve names or open sockets: the browser connects to the URL itself.
            Transport = "ws";
            _endpoint = new IPEndPoint(IPAddress.Loopback, 0);
            Connection = OpenInBrowser(options.Address, _security);
        }
        else
        {
            (Transport, var endpoint) = Parse(options.Address);
            _endpoint = endpoint;
            Connection = Open(Transport, endpoint, options.Conditions, _security);
        }

        Connection.Connected += OnConnected;
        Node = new Node(options.Name);
        var link = Node.ConnectVia(Connection);
        Link = link;
        Replica = Node.Subscribe(link, new ScopeId(options.Scope), replicaWorld);
        var all = new List<ReplicaScope> { Replica };
        for (var i = 0; i < options.AlsoScopes.Count; i++)
        {
            all.Add(Node.Subscribe(link, new ScopeId(options.AlsoScopes[i]), extraWorlds![i]));
        }

        Replicas = all;
    }

    /// <summary>The link to the server, for subscribing to more scopes later.</summary>
    public Link Link { get; }

    /// <summary>Every replica this session follows, the main one first.</summary>
    public IReadOnlyList<ReplicaScope> Replicas { get; }

    /// <summary>The options it was created with.</summary>
    public ConnectOptions Options { get; }

    /// <summary><c>udp</c> or <c>ws</c>.</summary>
    public string Transport { get; }

    /// <summary>The transport connection.</summary>
    public INetConnection Connection { get; private set; }

    /// <summary>How many times the session connected again after losing the server.</summary>
    public int Reconnects { get; private set; }

    /// <summary>True while the connection is lost and the session is trying to get it back (<see cref="ConnectOptions.Reconnect"/>).</summary>
    public bool IsReconnecting => Connection.State == ConnectionState.Closed && _lostAt >= 0 && !GaveUp && _refusedUntil == 0 && !_permanent;

    /// <summary>True once the transport connection has closed and the session is not trying (the server refused it, the window ran out, or reconnecting is off). After repeated unauthenticated refusals it is true for a minute, then the session tries again.</summary>
    public bool IsClosed => Connection.State == ConnectionState.Closed && (!Options.Reconnect || _permanent || GaveUp || _refusedUntil > 0);

    private bool GaveUp => _lostAt >= 0 && Environment.TickCount64 - _lostAt > Options.ReconnectWindowMs;

    /// <summary>The client's node.</summary>
    public Node Node { get; }

    /// <summary>The replica of the server's scope; its world is the game's world.</summary>
    public ReplicaScope Replica { get; }

    /// <summary>Drift reports the replica filed.</summary>
    public IReadOnlyList<DriftReport> DriftReports => Node.DriftReports;

    /// <summary>Moves messages both ways and applies what arrived. Cheap when nothing did.</summary>
    public void Pump()
    {
        if (Connection.State == ConnectionState.Closed)
        {
            Node.Pump(); // whatever arrived before the close is still applied
            TryReconnect();
            return;
        }

        Connection.Pump();
        Node.Pump();
        Connection.Pump();
    }

    // The old connection is closed. Open a new one with growing pauses; only once it is connected is it swapped in, because messages sent
    // into a connection that never completes its handshake are lost with it, and the swap is what resubscribes.
    private void TryReconnect()
    {
        if (!Options.Reconnect || _permanent)
        {
            return;
        }

        var now = Environment.TickCount64;
        if (_refusedUntil > 0)
        {
            if (now < _refusedUntil)
            {
                return;
            }

            // The pause is over: a refusal nobody could prove is not held against the server for ever.
            _refusedUntil = 0;
            _refusals = 0;
            _lostAt = now;
            _nextAttempt = now;
            _backoffMs = 250;
        }

        if (_lostAt < 0)
        {
            _lostAt = now;
            _nextAttempt = now;
            _backoffMs = 250;
            if (_closedByUs || (Refusal(Connection.CloseInfo.Reason) && (_wasConnected || !IsSecure)))
            {
                // Said over a link that proved who it was talking to (or a link that never claimed to): final.
                _permanent = true;
                return;
            }

            if (Refusal(Connection.CloseInfo.Reason))
            {
                NoteUnprovenRefusal(now);
                if (_refusedUntil > 0)
                {
                    return;
                }
            }
        }

        if (GaveUp)
        {
            return;
        }

        if (_candidate is null)
        {
            if (now >= _nextAttempt)
            {
                _candidate = OperatingSystem.IsBrowser() ? OpenInBrowser(Options.Address, _security) : Open(Transport, _endpoint, Options.Conditions, _security);
            }

            return;
        }

        _candidate.Pump();
        if (_candidate.State == ConnectionState.Connected)
        {
            var fresh = _candidate;
            _candidate = null;
            Connection = fresh;
            Connection.Connected += OnConnected;
            _wasConnected = true;
            _refusals = 0;
            Reconnects++;
            _lostAt = -1;
            Node.Reconnect(Link, fresh);
        }
        else if (_candidate.State == ConnectionState.Closed)
        {
            var reason = _candidate.CloseInfo.Reason;
            _candidate = null;
            _backoffMs = Math.Min(_backoffMs * 2, 2000);
            _nextAttempt = now + _backoffMs;
            if (Refusal(reason))
            {
                if (!IsSecure)
                {
                    _permanent = true;
                    return;
                }

                NoteUnprovenRefusal(now);
            }
        }
    }

    // Reasons a server gives when it will not have this client. Over an unauthenticated exchange (a Reject datagram, a bad handshake
    // reply) anyone on the path can forge them, so they only count once they repeat; see NoteUnprovenRefusal.
    private static bool Refusal(DisconnectReason reason) => reason is DisconnectReason.Rejected or DisconnectReason.VersionMismatch or DisconnectReason.Kicked or DisconnectReason.SecurityRequired or DisconnectReason.HandshakeFailed;

    private bool IsSecure => _security is { Mode: SecurityMode.Required };

    private void OnConnected(INetConnection connection) => _wasConnected = true;

    // The same refusal on three fresh attempts in a row means the server really does not accept this client (a wrong key, another game
    // version). The session then reports closed and stays quiet for a minute before it tries again, so a forged packet cannot end it for good.
    private void NoteUnprovenRefusal(long now)
    {
        if (++_refusals >= RefusalsBeforePause)
        {
            _refusedUntil = now + RefusalPauseMs;
        }
    }

    private const int RefusalsBeforePause = 3;
    private const int RefusalPauseMs = 60_000;
    private int _refusals;
    private long _refusedUntil;
    private bool _wasConnected;

    private readonly IPEndPoint _endpoint;
    private INetConnection? _candidate;
    private long _lostAt = -1;
    private long _nextAttempt;
    private int _backoffMs = 250;
    private bool _permanent;
    private readonly NetSecurity? _security;

    // A client without an identity key gets one now and keeps it for every reconnect of this session: the server names the player by it.
    private static NetSecurity? WithIdentity(NetSecurity? security) =>
        security is { Mode: SecurityMode.Required, Identity: null, TrustedServerKeys.Count: > 0 }
            ? new NetSecurity { GameId = security.GameId, TrustedServerKeys = security.TrustedServerKeys, Identity = NetKeyPair.Generate(), PerServerIdentity = security.PerServerIdentity, ServerId = security.ServerId }
            : security;
    private bool _closedByUs;

    /// <summary>Describes the connection and the replica now.</summary>
    public NetStatus Status()
    {
        var s = Connection.Stats;
        var close = Connection.CloseInfo;
        return new NetStatus(
            Options.Address, Transport, Connection.State.ToString().ToLowerInvariant(), close.Reason == DisconnectReason.None ? null : close.Reason.ToString(), Options.Scope,
            Replica.State.ToString().ToLowerInvariant(), Replica.World.Tick, Replica.ViewHash.ToString("x16", CultureInfo.InvariantCulture),
            Replica.FramesApplied, Replica.Resyncs, Node.DriftCount, Math.Round(s.RttMs, 1), s.Retransmits, Math.Round(s.InboundLoss, 4), Math.Round(s.OutboundLoss, 4),
            s.MessagesSent, s.MessagesReceived, DriftInfos(),
            [.. Replicas.Skip(1).Select(r => new FollowedScope(r.Id.Value, r.State.ToString().ToLowerInvariant(), r.World.Tick, r.ViewHash.ToString("x16", CultureInfo.InvariantCulture)))],
            Reconnects, IsReconnecting);
    }

    private DriftInfo[] DriftInfos()
    {
        var reports = Node.DriftReports;
        var list = new List<DriftInfo>();
        for (var i = Math.Max(0, reports.Count - 8); i < reports.Count; i++)
        {
            var r = reports[i];
            var lines = new List<string>();
            foreach (var d in r.Differences)
            {
                if (lines.Count >= 6)
                {
                    break;
                }

                lines.Add($"{d.Change} {d.TypeName ?? d.Name ?? d.Kind.ToString()}{(d.Kind == DigestKind.Entity ? "#" + d.EntityId.ToString(CultureInfo.InvariantCulture) : "")}");
            }

            list.Add(new DriftInfo(r.ReplicaNode, r.Scope.Value, r.Tick, r.AuthorityHash.ToString("x16", CultureInfo.InvariantCulture), r.ReplicaHash.ToString("x16", CultureInfo.InvariantCulture), [.. lines]));
        }

        return [.. list];
    }

    /// <summary>Closes the connection, telling the server.</summary>
    internal void Close()
    {
        _closedByUs = true;
        _permanent = true;
        _candidate?.Disconnect(DisconnectReason.Normal);
        Connection.Disconnect(DisconnectReason.Normal, flush: true);
    }

    private static (string Transport, IPEndPoint Endpoint) Parse(string address)
    {
        var scheme = address.IndexOf("://", StringComparison.Ordinal);
        if (scheme < 0 || !Uri.TryCreate(address, UriKind.Absolute, out var uri) || uri.Port <= 0 || address[..scheme] is not ("udp" or "ws"))
        {
            throw new FormatException($"'{address}' is not a server address. Use udp://host:port or ws://host:port.");
        }

        IPAddress? ip = null;
        if (!IPAddress.TryParse(uri.Host, out ip))
        {
            foreach (var candidate in Dns.GetHostAddresses(uri.Host))
            {
                if (candidate.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    ip = candidate;
                    break;
                }
            }
        }

        return ip is null ? throw new FormatException($"Cannot resolve the host '{uri.Host}' in '{address}'.") : (address[..scheme], new IPEndPoint(ip, uri.Port));
    }

    [System.Runtime.Versioning.SupportedOSPlatform("browser")]
    private static INetConnection OpenInBrowser(string address, NetSecurity? security)
    {
        if (!address.StartsWith("ws://", StringComparison.Ordinal) && !address.StartsWith("wss://", StringComparison.Ordinal))
        {
            throw new FormatException("A web build connects over WebSocket: use ws://host:port or wss://host:port.");
        }

        // The same encrypted records as the desktop client, inside the browser's WebSocket messages (and inside TLS on a wss:// page).
        return WebSocketTransport.Secure(() => new BrowserWebSocketConnection(address), security);
    }

    private static INetConnection Open(string transport, IPEndPoint endpoint, NetworkConditions? conditions, NetSecurity? security)
    {
        var clock = NetClock.System;
        var udpOptions = new UdpOptions { Security = security };
        if (transport == "udp")
        {
            if (conditions is null)
            {
                return UdpTransport.Connect(endpoint, udpOptions, clock);
            }

            var bind = new IPEndPoint(endpoint.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any, 0);
            return UdpTransport.Connect(new ConditionedDatagramSocket(new SystemDatagramSocket(bind), conditions, clock), endpoint.Serialize(), udpOptions, clock);
        }

        var ws = WebSocketTransport.Connect(endpoint, new WebSocketOptions { Security = security }, clock);
        return conditions is null ? ws : new ConditionedConnection(ws, conditions, clock);
    }

    /// <summary>Reads <c>latency=100,jitter=10,loss=0.02,duplicate=0.01,reorder=0.05,bandwidth=125000,seed=7</c>.</summary>
    /// <exception cref="FormatException">The text is not a list of known settings.</exception>
    public static NetworkConditions ParseConditions(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        int latency = 0, jitter = 0, bandwidth = 0, seed = 1;
        double loss = 0, duplicate = 0, reorder = 0;
        foreach (var part in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var eq = part.IndexOf('=', StringComparison.Ordinal);
            if (eq <= 0)
            {
                throw new FormatException($"'{part}' is not name=value. Settings: latency, jitter (ms), loss, duplicate, reorder (0-1), bandwidth (bytes per second), seed.");
            }

            var key = part[..eq];
            var value = part[(eq + 1)..];
            bool Whole(out int v) => int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out v);
            bool Rate(out double v) => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out v) && v is >= 0 and <= 1;
            var ok = key switch
            {
                "latency" => Whole(out latency),
                "jitter" => Whole(out jitter),
                "bandwidth" => Whole(out bandwidth),
                "seed" => Whole(out seed),
                "loss" => Rate(out loss),
                "duplicate" => Rate(out duplicate),
                "reorder" => Rate(out reorder),
                _ => throw new FormatException($"Unknown network setting '{key}'. Settings: latency, jitter, loss, duplicate, reorder, bandwidth, seed."),
            };
            if (!ok)
            {
                throw new FormatException($"'{value}' is not a valid value for {key}.");
            }
        }

        return new NetworkConditions { LatencyMs = latency, JitterMs = jitter, LossRate = loss, DuplicateRate = duplicate, ReorderRate = reorder, BandwidthBytesPerSecond = bandwidth, Seed = seed };
    }
}
