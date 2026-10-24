using System.Globalization;

namespace OneGame.Sync;

/// <summary>Message type bytes. The first byte of every message is one of these; the scope id (varint) follows.</summary>
internal static class Msg
{
    // Client to authority.
    public const byte Subscribe = 1;
    public const byte Unsubscribe = 2;
    public const byte Command = 3;
    public const byte DigestRequest = 4;

    // Authority to client.
    public const byte Snapshot = 16;
    public const byte Frame = 17;
    public const byte Digest = 18;
    public const byte CommandRefused = 19;
    public const byte SubscribeRefused = 20;
}

/// <summary>The client end of a connection to another node: where a node sends subscriptions and commands, and receives snapshots and frames.</summary>
public sealed class Link
{
    internal Link(Node owner, Node server, IMessageChannel channel)
    {
        Owner = owner;
        Server = server;
        Channel = channel;
    }

    /// <summary>The node that holds this end.</summary>
    public Node Owner { get; }

    /// <summary>The node at the other end.</summary>
    public Node Server { get; }

    /// <summary>The server's end of the same connection.</summary>
    public Connection Connection { get; internal set; } = null!;

    /// <summary>The channel. Tests may send raw bytes through it to play a misbehaving peer.</summary>
    public IMessageChannel Channel { get; }
}

/// <summary>The authority's end of a connection: what the peer is trusted to do, and the entry point for commands as that peer.</summary>
public sealed class Connection
{
    internal Connection(Node node, int id, string name, Trust trust, IMessageChannel channel)
    {
        Node = node;
        Id = id;
        Name = name;
        Trust = trust;
        Channel = channel;
    }

    /// <summary>The node this end belongs to (the one that holds the authority scopes).</summary>
    public Node Node { get; }

    /// <summary>Id unique within the node.</summary>
    public int Id { get; }

    /// <summary>Name for reports.</summary>
    public string Name { get; }

    /// <summary>What the peer may do. May be changed at any time; the next command or subscription is checked against the new record.</summary>
    public Trust Trust { get; set; }

    /// <summary>The channel to the peer. Tests may send raw bytes through it to play a misbehaving authority.</summary>
    public IMessageChannel Channel { get; }

    /// <summary>
    /// Submits a command as this connection, exactly as if it had arrived over the channel: the trust record is checked, a refusal is
    /// filed and sent back, and an accepted command runs at the start of the scope's next tick.
    /// </summary>
    /// <param name="scope">The scope to command.</param>
    /// <param name="entity">The target entity.</param>
    /// <param name="commandId">The command id (see the generated <c>Commands</c>).</param>
    /// <param name="payload">The encoded arguments.</param>
    public SubmitResult SubmitCommand(ScopeId scope, EntityId entity, uint commandId, ReadOnlySpan<byte> payload)
        => Node.AcceptCommand(this, scope, entity, commandId, payload);

    /// <summary>Submits a command in its wire form (entity id, command id, arguments), as read from a script or a log.</summary>
    public SubmitResult SubmitCommand(ScopeId scope, ReadOnlySpan<byte> wire)
    {
        var r = new SyncReader(wire);
        var entity = r.ReadVarUInt();
        var command = r.ReadVarUInt();
        if (!r.Ok || command > uint.MaxValue)
        {
            return Node.AcceptCommand(this, scope, EntityId.None, 0, default, malformed: true);
        }

        return Node.AcceptCommand(this, scope, new EntityId(entity), (uint)command, wire[r.Position..]);
    }
}

/// <summary>
/// A participant: it holds the scopes it is authority for, the replicas it subscribes to, and the connections that carry both.
/// Nothing runs on its own; <see cref="Pump"/> processes whatever has arrived, and <see cref="Cluster"/> calls it in a fixed order.
/// </summary>
public sealed class Node
{
    private const int MaxHistory = 256;

    private readonly List<AuthorityScope> _authority = [];
    private readonly List<ReplicaScope> _replicas = [];
    private readonly List<Connection> _connections = [];
    private readonly List<Link> _links = [];
    private readonly List<CommandRefusal> _refusals = [];
    private readonly List<DriftReport> _drift = [];
    private readonly List<ProtocolError> _protocolErrors = [];
    private int _nextConnection = 1;

    /// <summary>Creates a node.</summary>
    /// <param name="name">Name for reports.</param>
    /// <param name="options">Behavior switches; defaults if null.</param>
    public Node(string name, ClusterOptions? options = null)
    {
        Name = name;
        Options = options ?? new ClusterOptions();
    }

    /// <summary>The node's name.</summary>
    public string Name { get; }

    /// <summary>The options this node follows.</summary>
    public ClusterOptions Options { get; }

    /// <summary>Scopes this node is authority for.</summary>
    public IReadOnlyList<AuthorityScope> AuthorityScopes => _authority;

    /// <summary>Scopes this node subscribes to.</summary>
    public IReadOnlyList<ReplicaScope> ReplicaScopes => _replicas;

    /// <summary>Connections other nodes made to this one.</summary>
    public IReadOnlyList<Connection> Connections => _connections;

    /// <summary>The most recent refused commands and subscriptions (at most 256): those this node refused as an authority, and those it was told about as a submitter.</summary>
    public IReadOnlyList<CommandRefusal> Refusals => _refusals;

    /// <summary>Drift found by this node's replicas.</summary>
    public IReadOnlyList<DriftReport> DriftReports => _drift;

    /// <summary>Messages that could not be applied (at most 256 kept).</summary>
    public IReadOnlyList<ProtocolError> ProtocolErrors => _protocolErrors;

    /// <summary>Total drift reports ever filed (the list keeps only the latest 256).</summary>
    public int DriftCount { get; private set; }

    internal SyncWriter Out { get; } = new(1 << 12);

    internal SyncWriter Scratch { get; } = new(1 << 12);

    /// <summary>Makes this node the authority of a scope.</summary>
    /// <param name="id">The scope id; unique on this node.</param>
    /// <param name="world">A fully built authority world.</param>
    /// <exception cref="ArgumentException">The world is a replica, or the scope id is taken.</exception>
    public AuthorityScope AddScope(ScopeId id, World world)
    {
        if (world.Role != WorldRole.Authority)
        {
            throw new ArgumentException("An authority scope needs an authority world.", nameof(world));
        }

        if (FindAuthority(id) is not null)
        {
            throw new ArgumentException($"{id} already exists on node '{Name}'.", nameof(id));
        }

        var scope = new AuthorityScope(this, id, world);
        _authority.Add(scope);
        return scope;
    }

    /// <summary>Connects this node to <paramref name="server"/> over a loopback pair.</summary>
    /// <param name="server">The node that will hold the authority scopes.</param>
    /// <param name="trust">What this node is trusted to do on the server.</param>
    /// <param name="name">Name for reports; defaults to this node's name.</param>
    public Link ConnectTo(Node server, Trust trust, string? name = null)
    {
        var (a, b) = Loopback.CreatePair();
        var link = new Link(this, server, a);
        var connection = new Connection(server, server._nextConnection++, name ?? Name, trust, b);
        link.Connection = connection;
        server._connections.Add(connection);
        _links.Add(link);
        return link;
    }

    /// <summary>Subscribes to a scope on the other end of <paramref name="link"/>. The replica fills in as messages are pumped.</summary>
    /// <param name="link">A link made by <see cref="ConnectTo"/> on this node.</param>
    /// <param name="id">The scope.</param>
    /// <param name="world">An empty replica world with the same entity registry and hot tables as the authority's.</param>
    /// <exception cref="ArgumentException">The world is not a replica, or the link belongs to another node.</exception>
    public ReplicaScope Subscribe(Link link, ScopeId id, World world)
    {
        if (world.Role != WorldRole.Replica)
        {
            throw new ArgumentException("A replica scope needs a replica world.", nameof(world));
        }

        if (link.Owner != this)
        {
            throw new ArgumentException("The link belongs to another node.", nameof(link));
        }

        var replica = new ReplicaScope(this, link, id, world);
        _replicas.Add(replica);
        replica.RequestSnapshot();
        return replica;
    }

    /// <summary>The authority scope with this id, or null.</summary>
    public AuthorityScope? FindAuthority(ScopeId id)
    {
        foreach (var s in _authority)
        {
            if (s.Id == id)
            {
                return s;
            }
        }

        return null;
    }

    /// <summary>Handles every message that has arrived. Returns true if there was any.</summary>
    public bool Pump()
    {
        var any = false;
        for (var i = 0; i < _connections.Count; i++)
        {
            var c = _connections[i];
            while (c.Channel.TryReceive(out var message))
            {
                any = true;
                HandleServerMessage(c, message);
            }
        }

        for (var i = 0; i < _links.Count; i++)
        {
            var l = _links[i];
            while (l.Channel.TryReceive(out var message))
            {
                any = true;
                HandleClientMessage(l, message);
            }
        }

        return any;
    }

    internal void ReportProtocolError(ScopeId scope, long tick, string reason)
    {
        if (_protocolErrors.Count >= MaxHistory)
        {
            _protocolErrors.RemoveAt(0);
        }

        _protocolErrors.Add(new ProtocolError(scope, tick, reason));
    }

    internal void FileRefusal(CommandRefusal refusal)
    {
        if (_refusals.Count >= MaxHistory)
        {
            _refusals.RemoveAt(0);
        }

        _refusals.Add(refusal);
    }

    internal void FileDrift(DriftReport report)
    {
        if (_drift.Count >= MaxHistory)
        {
            _drift.RemoveAt(0);
        }

        _drift.Add(report);
        DriftCount++;
    }

    internal SubmitResult AcceptCommand(Connection from, ScopeId scope, EntityId entity, uint commandId, ReadOnlySpan<byte> payload, bool malformed = false)
    {
        if (malformed || payload.Length > SyncLimits.MaxCommandBytes)
        {
            Refuse(from, new CommandRefusal(scope, from.Id, entity, commandId, RefusalReason.Malformed));
            return SubmitResult.Malformed;
        }

        var authority = FindAuthority(scope);
        if (authority is null)
        {
            Refuse(from, new CommandRefusal(scope, from.Id, entity, commandId, RefusalReason.UnknownScope));
            return SubmitResult.UnknownScope;
        }

        if (!from.Trust.Command.Contains(scope))
        {
            Refuse(from, new CommandRefusal(scope, from.Id, entity, commandId, RefusalReason.NotTrustedToCommand));
            return SubmitResult.NotTrusted;
        }

        authority.World.Submit(entity, commandId, payload);
        return SubmitResult.Queued;
    }

    private void Refuse(Connection to, CommandRefusal refusal)
    {
        FileRefusal(refusal);
        var w = Out;
        w.Reset();
        w.WriteByte(Msg.CommandRefused);
        w.WriteVarUInt(refusal.Scope.Value);
        w.WriteVarUInt(refusal.Entity.Value);
        w.WriteVarUInt(refusal.CommandId);
        w.WriteByte((byte)refusal.Reason);
        to.Channel.Send(w.Written);
    }

    private void HandleServerMessage(Connection from, ReadOnlySpan<byte> message)
    {
        var r = new SyncReader(message);
        var type = r.ReadByte();
        var scope = new ScopeId(r.ReadUInt32());
        if (!r.Ok)
        {
            ReportProtocolError(default, 0, "truncated message header from " + from.Name);
            return;
        }

        switch (type)
        {
            case Msg.Subscribe:
            {
                var authority = FindAuthority(scope);
                if (authority is null || !from.Trust.Read.Contains(scope))
                {
                    var reason = authority is null ? RefusalReason.UnknownScope : RefusalReason.NotTrustedToRead;
                    FileRefusal(new CommandRefusal(scope, from.Id, EntityId.None, 0, reason));
                    Out.Reset();
                    Out.WriteByte(Msg.SubscribeRefused);
                    Out.WriteVarUInt(scope.Value);
                    Out.WriteByte((byte)reason);
                    from.Channel.Send(Out.Written);
                    return;
                }

                authority.Subscribe(from);
                return;
            }

            case Msg.Unsubscribe:
                FindAuthority(scope)?.Unsubscribe(from);
                return;

            case Msg.Command:
            {
                var entity = r.ReadVarUInt();
                var command = r.ReadVarUInt();
                if (!r.Ok || command > uint.MaxValue)
                {
                    AcceptCommand(from, scope, EntityId.None, 0, default, malformed: true);
                    return;
                }

                AcceptCommand(from, scope, new EntityId(entity), (uint)command, message[r.Position..]);
                return;
            }

            case Msg.DigestRequest:
            {
                var tick = r.ReadVarInt();
                if (r.Ok && from.Trust.Read.Contains(scope))
                {
                    FindAuthority(scope)?.SendDigest(from, tick);
                }

                return;
            }

            default:
                ReportProtocolError(scope, 0, "unknown message type " + type.ToString(CultureInfo.InvariantCulture) + " from " + from.Name);
                return;
        }
    }

    private void HandleClientMessage(Link link, ReadOnlySpan<byte> message)
    {
        var r = new SyncReader(message);
        var type = r.ReadByte();
        var scope = new ScopeId(r.ReadUInt32());
        if (!r.Ok)
        {
            ReportProtocolError(default, 0, "truncated message header");
            return;
        }

        ReplicaScope? replica = null;
        foreach (var candidate in _replicas)
        {
            if (candidate.Link == link && candidate.Id == scope)
            {
                replica = candidate;
            }
        }

        if (replica is null)
        {
            return; // a late message for a scope this node no longer follows
        }

        replica.Handle(type, ref r, message);
    }
}
