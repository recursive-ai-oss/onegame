using System.Net;
using Lanterns;
using OneGame.Core;
using OneGame.Net;
using OneGame.Server;
using OneGame.Sync;

namespace Lanterns.Server;

/// <summary>
/// What makes a server an island or the master. The scope it hosts decides: 1 and 2 are islands, 3 is the master.
/// <list type="bullet">
/// <item>Islands accept players on the public port, hand a player who walks onto the bridge to the other island, and relay the other island and the master to their players, so a player needs only one connection.</item>
/// <item>The master follows both islands, runs the swap escrow between them, and keeps every player's account.</item>
/// <item>Servers talk to each other on a second, private UDP port (<c>--option peers=PORT</c>) and find each other with <c>--option peer.N=udp://host:port</c>. The transport does not authenticate yet, so that port must not be reachable by players.</item>
/// <item>Every scope saves itself every other tick (<c>--option data=DIR</c> keeps the file) and answers peers only about what the save already holds, so a server that is killed and started again from its file never contradicts what it told the others.</item>
/// </list>
/// </summary>
public sealed class Realm
{
    private const int SaveEveryTicks = 2;

    private readonly Dictionary<uint, Peer> _peers = [];
    private readonly List<PeerConnection> _accepted = [];
    private readonly List<EntityId> _ids = [];
    private ServerHost _host = null!;
    private AuthorityScope _scope = null!;
    private UdpListener? _listener;
    private string? _saveFile;
    private EscrowDriver? _driver;
    private uint _scopeNo;
    private uint _issued;

    /// <summary>The setup a server program (or a test) passes to the server host. Each call makes an independent server.</summary>
    public static ServerSetup Setup() => new Realm().Create();

    /// <summary>Saves written so far.</summary>
    public int Saves { get; private set; }

    /// <summary>The escrow driver on the master, or null.</summary>
    public EscrowDriver? Driver => _driver;

    /// <summary>The setup for this one server (a test makes the realm itself to reach it afterwards).</summary>
    public ServerSetup Create() => new()
    {
        RegisterEntities = Worlds.Register,
        BuildWorld = Worlds.Build,
        SeedWorld = Worlds.Seed,
        Started = Start,
        BeforeTick = _ => PumpNetwork(),
        AfterTick = After,
        Pump = _ => PumpNetwork(),
    };

    /// <summary>Closes the private port and the links to other servers, as a stopped process would. Tests call it before starting the server again from its save.</summary>
    public void Close()
    {
        _listener?.Dispose();
        _listener = null;
        foreach (var p in _peers.Values)
        {
            p.Channel.Close();
        }
    }

    /// <summary>A line about this server's replica of another scope: its state, tick, resubscriptions and the link, for logs and tests.</summary>
    public string PeerState(uint scope) => _peers.TryGetValue(scope, out var p)
        ? $"{p.Replica.State} tick {p.Replica.LastTick} resyncs {p.Replica.Resyncs} link {(p.Channel.IsConnected ? "up" : "down")}"
        : "none";

    private bool IsMaster => _scopeNo == Rules.Master;

    private static Trust PeerTrust => Trust.Owner(new ScopeId(Rules.IslandA), new ScopeId(Rules.IslandB), new ScopeId(Rules.Master)).As(Escrow.CoordinatorPlayer).AsRelay();

    private void Start(ServerHost host)
    {
        _host = host;
        _scope = host.Scopes[0].Authority;
        _scopeNo = _scope.Id.Value;
        _scope.Durability = Durability.AfterSave;
        var options = host.Config.Options;
        string? Option(string key) => options.FirstOrDefault(o => o.Key == key).Value;

        if (Option("data") is { Length: > 0 } dir)
        {
            Directory.CreateDirectory(dir);
            _saveFile = Path.Combine(dir, "scope" + _scopeNo.ToString(CultureInfo.InvariantCulture) + ".save");
            if (File.Exists(_saveFile))
            {
                var result = _scope.Load(File.ReadAllBytes(_saveFile));
                host.Log.Info($"restored scope {_scopeNo} from {_saveFile}: {result.Status}");
            }
        }

        if (Option("peers") is { } port)
        {
            _listener = UdpTransport.Listen(new IPEndPoint(IPAddress.Parse(Option("peers.address") ?? "127.0.0.1"), int.Parse(port, CultureInfo.InvariantCulture)));
        }

        foreach (var (key, value) in options)
        {
            if (key.StartsWith("peer.", StringComparison.Ordinal))
            {
                var scope = uint.Parse(key[5..], CultureInfo.InvariantCulture);
                if (scope != _scopeNo)
                {
                    AddPeer(scope, value);
                }
            }
        }

        _scope.TransferLinks = id => _peers.TryGetValue(id.Value, out var p) ? p.Link : null;
        if (IsMaster)
        {
            _driver = new EscrowDriver(_scope, id => _peers.TryGetValue(id.Value, out var p) && p.Replica.State == ReplicaState.Live ? p.Replica : null);
            host.Authenticate = _ => null;
        }
        else
        {
            _issued = Island.Of(_scope.World)?.NextPlayer ?? 0;
            host.Authenticate = AdmitPlayer;
        }
    }

    private void AddPeer(uint scope, string address)
    {
        var uri = new Uri(address);
        var channel = new PeerChannel(new IPEndPoint(IPAddress.Parse(uri.Host), uri.Port));
        var link = _host.Node.ConnectVia(channel, "peer " + scope.ToString(CultureInfo.InvariantCulture));
        var id = new ScopeId(scope);
        var registry = new EntityRegistry();
        Worlds.Register(registry);
        var world = new World(1, registry, WorldRole.Replica);
        var replica = _host.Node.Subscribe(link, id, world);
        if (!IsMaster)
        {
            // Players reach the other island and the master through this server.
            _host.Node.Relay(replica);
        }

        // Messages sent while the link was down are gone; asking again gives a fresh snapshot.
        channel.Opened += () => _host.Node.Resubscribe(replica);
        _peers[scope] = new Peer(channel, link, replica);
    }

    private Trust? AdmitPlayer(ClientRequest request)
    {
        var serial = ++_issued;
        var player = (_scopeNo * 100_000) + serial;
        var island = Island.Of(_scope.World);
        if (island is null)
        {
            return null;
        }

        _host.SendCommand(_scope.Id, island.Id, "Join", player.ToString(CultureInfo.InvariantCulture), "Player " + serial.ToString(CultureInfo.InvariantCulture));
        _host.Log.Info($"player {player} joined from {request.Address}");
        return Trust.Owner(new ScopeId(Rules.IslandA), new ScopeId(Rules.IslandB), new ScopeId(Rules.Master)).As(player);
    }

    private void PumpNetwork()
    {
        if (_listener is not null)
        {
            _listener.Pump();
            while (_listener.TryAccept(out var connection))
            {
                var peer = new PeerConnection(connection);
                _accepted.Add(peer);
                _host.Node.Accept(peer, PeerTrust, "peer " + connection.RemoteName);
            }

            for (var i = _accepted.Count - 1; i >= 0; i--)
            {
                if (_accepted[i].IsClosed)
                {
                    _accepted[i].Inject(_host.Node.AuthorityScopes.Select(s => s.Id));
                    _accepted.RemoveAt(i);
                }
            }
        }

        foreach (var p in _peers.Values)
        {
            p.Channel.Pump();
        }
    }

    private void After(ServerHost host)
    {
        if (IsMaster)
        {
            Observe();
            _driver!.Pump();
        }
        else
        {
            HandOver();
        }

        if (host.Tick % SaveEveryTicks == 0)
        {
            Save();
        }

        PumpNetwork();
    }

    // A player who stopped on the bridge goes to the island on the other side, unless a swap is holding their lanterns.
    private void HandOver()
    {
        _ids.Clear();
        uint dest = 0;
        foreach (var e in _scope.World.Entities)
        {
            if (e is Player { Dest: not 0 } p && p.Dest != _scopeNo && p.Pending == 0 && !InTransfer(p.Id))
            {
                dest = p.Dest;
                _ids.Add(p.Id);
            }
        }

        if (_ids.Count > 0 && _peers.TryGetValue(dest, out var peer) && peer.Channel.IsConnected && peer.Replica.State == ReplicaState.Live)
        {
            _scope.BeginTransfer(peer.Link, new ScopeId(dest), _ids);
        }
    }

    private bool InTransfer(EntityId id)
    {
        foreach (var t in _scope.Transfers)
        {
            if (t.State is TransferState.Pending or TransferState.Offered or TransferState.Committed && t.Entities.Contains(id))
            {
                return true;
            }
        }

        return false;
    }

    // The master learns where players are, and how many lanterns they hold, by reading the islands like any other subscriber.
    private void Observe()
    {
        var world = _scope.World;
        Market? market = null;
        var accounts = new Dictionary<uint, Account>();
        foreach (var e in world.Entities)
        {
            if (e is Market m)
            {
                market = m;
            }
            else if (e is Account a)
            {
                accounts[a.Owner] = a;
            }
        }

        if (market is null)
        {
            return;
        }

        foreach (var peer in _peers.Values)
        {
            if (peer.Replica.State != ReplicaState.Live)
            {
                continue;
            }

            foreach (var e in peer.Replica.World.Entities)
            {
                if (e is Player p && p.Dest == 0 && (!accounts.TryGetValue(p.Owner, out var a) || a.Island != p.Island || a.Total != p.Total || a.Name != p.Name))
                {
                    _host.SendCommand(_scope.Id, market.Id, "Observe", p.Owner.ToString(CultureInfo.InvariantCulture), p.Name, p.Island.ToString(CultureInfo.InvariantCulture), p.Total.ToString(CultureInfo.InvariantCulture));
                }
            }
        }
    }

    private void Save()
    {
        var bytes = _scope.Save();
        Saves++;
        if (_saveFile is not null)
        {
            var temp = _saveFile + ".tmp";
            File.WriteAllBytes(temp, bytes);
            File.Move(temp, _saveFile, true);
        }
    }

    private sealed record Peer(PeerChannel Channel, Link Link, ReplicaScope Replica);

    // A connection another server opened to us. When it closes we tell the node, as a polite client would have, so it stops publishing to it.
    private sealed class PeerConnection(INetConnection inner) : IMessageChannel
    {
        private readonly Queue<byte[]> _injected = [];
        private byte[] _current = [];

        public bool IsClosed => inner.State == ConnectionState.Closed;

        public void Send(ReadOnlySpan<byte> message) => inner.Send(message);

        public bool TryReceive(out ReadOnlySpan<byte> message)
        {
            if (inner.TryReceive(out message))
            {
                return true;
            }

            if (_injected.TryDequeue(out var next))
            {
                _current = next;
                message = _current;
                return true;
            }

            message = default;
            return false;
        }

        public void Inject(IEnumerable<ScopeId> scopes)
        {
            foreach (var scope in scopes)
            {
                // Unsubscribe is message type 2 followed by the scope id as a varint; ours are all below 128.
                _injected.Enqueue([2, (byte)scope.Value]);
            }
        }
    }
}
