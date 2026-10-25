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

    /// <summary>The scope to subscribe to.</summary>
    public uint Scope { get; init; } = 1;

    /// <summary>The name this client has in reports.</summary>
    public string Name { get; init; } = "client";

    /// <summary>Network conditions to apply to this client's traffic (latency, loss, ...), or null for the real network as it is.</summary>
    public NetworkConditions? Conditions { get; init; }
}

/// <summary>One drift report: a replica whose state differed from the server's at the same tick.</summary>
/// <param name="Node">The node holding the replica.</param>
/// <param name="Scope">The scope.</param>
/// <param name="Tick">The tick at which the hashes differed.</param>
/// <param name="Expected">The server's hash (16 hex digits).</param>
/// <param name="Actual">The replica's hash.</param>
/// <param name="Differences">The first entities, tables or streams that differ, one line each.</param>
public sealed record DriftInfo(string Node, uint Scope, long Tick, string Expected, string Actual, string[] Differences);

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
public sealed record NetStatus(
    string Address, string Transport, string Connection, string? CloseReason, uint Scope, string Replica, long Tick, string Hash,
    long FramesApplied, int Resyncs, int Drift, double RttMs, long Retransmits, double InboundLoss, double OutboundLoss, long MessagesSent, long MessagesReceived, DriftInfo[] Reports);

/// <summary>
/// A game's connection to a server: a node holding one replica scope, reached through a transport connection
/// (<see cref="Node.ConnectVia"/>). <see cref="Pump"/> does all the work, on the caller's thread.
/// </summary>
public sealed class RemoteSession
{
    /// <summary>Connects an empty replica world to a server and subscribes to the scope. Pump it to make progress.</summary>
    /// <param name="options">Where to connect.</param>
    /// <param name="replicaWorld">An empty replica world with the server's entity types, tables and systems.</param>
    public RemoteSession(ConnectOptions options, World replicaWorld)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(replicaWorld);
        Options = options;
        (Transport, var endpoint) = Parse(options.Address);
        Connection = Open(Transport, endpoint, options.Conditions);
        Node = new Node(options.Name);
        var link = Node.ConnectVia(Connection);
        Replica = Node.Subscribe(link, new ScopeId(options.Scope), replicaWorld);
    }

    /// <summary>The options it was created with.</summary>
    public ConnectOptions Options { get; }

    /// <summary><c>udp</c> or <c>ws</c>.</summary>
    public string Transport { get; }

    /// <summary>The transport connection.</summary>
    public INetConnection Connection { get; }

    /// <summary>True once the transport connection has closed (the server left, timed out or refused).</summary>
    public bool IsClosed => Connection.State == ConnectionState.Closed;

    /// <summary>The client's node.</summary>
    public Node Node { get; }

    /// <summary>The replica of the server's scope; its world is the game's world.</summary>
    public ReplicaScope Replica { get; }

    /// <summary>Drift reports the replica filed.</summary>
    public IReadOnlyList<DriftReport> DriftReports => Node.DriftReports;

    /// <summary>Moves messages both ways and applies what arrived. Cheap when nothing did.</summary>
    public void Pump()
    {
        Connection.Pump();
        Node.Pump();
        Connection.Pump();
    }

    /// <summary>Describes the connection and the replica now.</summary>
    public NetStatus Status()
    {
        var s = Connection.Stats;
        var close = Connection.CloseInfo;
        return new NetStatus(
            Options.Address, Transport, Connection.State.ToString().ToLowerInvariant(), close.Reason == DisconnectReason.None ? null : close.Reason.ToString(), Options.Scope,
            Replica.State.ToString().ToLowerInvariant(), Replica.World.Tick, Replica.ViewHash.ToString("x16", CultureInfo.InvariantCulture),
            Replica.FramesApplied, Replica.Resyncs, Node.DriftCount, Math.Round(s.RttMs, 1), s.Retransmits, Math.Round(s.InboundLoss, 4), Math.Round(s.OutboundLoss, 4),
            s.MessagesSent, s.MessagesReceived, DriftInfos());
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
    internal void Close() => Connection.Disconnect(DisconnectReason.Normal, flush: true);

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

    private static INetConnection Open(string transport, IPEndPoint endpoint, NetworkConditions? conditions)
    {
        var clock = NetClock.System;
        if (transport == "udp")
        {
            if (conditions is null)
            {
                return UdpTransport.Connect(endpoint, null, clock);
            }

            var bind = new IPEndPoint(endpoint.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any, 0);
            return UdpTransport.Connect(new ConditionedDatagramSocket(new SystemDatagramSocket(bind), conditions, clock), endpoint.Serialize(), null, clock);
        }

        var ws = WebSocketTransport.Connect(endpoint, null, clock);
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
