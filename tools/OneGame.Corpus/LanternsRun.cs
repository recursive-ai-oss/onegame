using Lanterns;
using OneGame.Core;
using OneGame.Sync;

namespace OneGame.Corpus;

/// <summary>
/// Lanterns on three authority nodes (two islands and the master) and bot players, in one process. It is the same wiring the sample's servers
/// use (<c>Lanterns.Server.Realm</c>): servers follow each other over links that carry the relay trust, islands relay what they follow to their
/// players, an island hands a player who stops on the bridge to the other island, the master runs the swap escrow, and every scope saves itself
/// every other tick under <see cref="Durability.AfterSave"/>. Only the transport differs: messages travel on the loopback so a run needs no
/// sockets, threads or clock, and the bytes are those the sockets would carry.
/// </summary>
internal sealed class LanternsRun
{
    private const string Game = "lanterns";
    private const int SaveEvery = 40;
    private const int LateJoinEvery = 100;
    private const int RecordingTicks = 600;
    private const int SampleTicks = 3000;
    private const int MaxTicks = 400_000;

    private static readonly uint[] ScopeIds = [Rules.IslandA, Rules.IslandB, Rules.Master];

    private readonly Dictionary<uint, Server> _servers = [];
    private readonly List<Bot> _bots = [];
    private readonly List<uint> _players = [];
    private Cluster _cluster = null!;
    private Capture _capture = null!;
    private ulong _seed;

    public static void Run(CorpusOptions options, Sink sink) => new LanternsRun().Execute(options, sink);

    private static Trust PeerTrust => Trust.Owner(new ScopeId(Rules.IslandA), new ScopeId(Rules.IslandB), new ScopeId(Rules.Master)).As(Escrow.CoordinatorPlayer).AsRelay();

    private static Trust PlayerTrust(uint player) => Trust.Owner(new ScopeId(Rules.IslandA), new ScopeId(Rules.IslandB), new ScopeId(Rules.Master)).As(player);

    private static World NewWorld(ulong seed, WorldRole role)
    {
        var registry = new EntityRegistry();
        Worlds.Register(registry);
        return new World(seed, registry, role);
    }

    private void Execute(CorpusOptions options, Sink sink)
    {
        _seed = options.Seed;
        _cluster = new Cluster(new ClusterOptions { PieceBytes = options.PieceBytes, FatalDrift = true });
        _capture = new Capture(sink, Game, () => _cluster.Tick);
        var rng = new Rng(Rng.Mix(_seed, 0x1A47));

        foreach (var id in ScopeIds)
        {
            var world = NewWorld(Rng.Mix(_seed, id), WorldRole.Authority);
            Worlds.Build(world, new ScopeId(id));
            Worlds.Seed(world, new ScopeId(id));
            var node = _cluster.AddNode(id == Rules.Master ? "master" : id == Rules.IslandA ? "islandA" : "islandB");
            var scope = _cluster.AddScope(node, new ScopeId(id), world);
            scope.Durability = Durability.AfterSave;
            _servers[id] = new Server(id, node, scope);
        }

        // Every server follows the other two over its own link, as the sample's servers do; islands serve what they follow on to their players.
        foreach (var from in _servers.Values)
        {
            foreach (var to in _servers.Values)
            {
                if (from == to)
                {
                    continue;
                }

                var (clientEnd, serverEnd) = Loopback.CreatePair();
                var link = from.Node.ConnectTo(to.Node, PeerTrust, _capture.ClientEnd(clientEnd), _capture.ServerEnd(serverEnd), "peer " + to.Id);
                var replica = from.Node.Subscribe(link, new ScopeId(to.Id), NewWorld(Rng.Mix(_seed, to.Id), WorldRole.Replica));
                if (from.Id != Rules.Master)
                {
                    from.Node.Relay(replica);
                }

                from.Peers[to.Id] = new Peer(link, replica);
            }

            var self = from;
            self.Scope.TransferLinks = id => self.Peers.TryGetValue(id.Value, out var p) ? p.Link : null;
        }

        var master = _servers[Rules.Master];
        master.Driver = new EscrowDriver(master.Scope, id => master.Peers.TryGetValue(id.Value, out var p) && p.Replica.State == ReplicaState.Live ? p.Replica : null);
        _cluster.Settle();

        var targets = ScopeIds.Select(id => new Archivist.Target(_servers[id].Scope, () => NewWorld(Rng.Mix(_seed, id), WorldRole.Replica), r => PlayerTrust(_players.Count == 0 ? 0 : _players[r.Range(0, _players.Count)]), _servers[id].Node.Name)).ToList();
        var archivist = new Archivist(_cluster, _capture, new Rng(Rng.Mix(_seed, 0xA4C)), targets, options.Sample ? 400 : RecordingTicks);

        var joinAt = new List<long>();
        var next = 1L;
        for (var i = 0; i < options.Bots; i++)
        {
            joinAt.Add(next);
            next += 30 + rng.Range(0, 50);
        }

        var ticks = options.Ticks ?? (options.Sample ? SampleTicks : MaxTicks);
        for (var tick = 1; tick <= ticks; tick++)
        {
            while (_bots.Count < joinAt.Count && joinAt[_bots.Count] <= tick)
            {
                Join(_bots.Count, options.Sample);
            }

            foreach (var bot in _bots)
            {
                bot.Step();
            }

            _cluster.Step();
            if (tick % 200 == 0)
            {
                _capture.EntitySchema = _servers.Values.Max(s => SoloRun.MaxSchema(s.Scope.World));
            }

            // Before the servers queue their own commands, so a recording can start on a tick with none waiting.
            archivist.Recordings();
            foreach (var server in _servers.Values)
            {
                After(server, archivist, tick);
            }

            if (tick % LateJoinEvery == 0)
            {
                archivist.LateJoin();
            }

            if (options.Ticks is null && !options.Sample && sink.Full(Game, Kinds.Frames))
            {
                break;
            }
        }

        archivist.Finish();
        var arrivals = _servers.Values.Where(s => s.Id != Rules.Master).Sum(s => Island.Of(s.Scope.World)!.Arrivals);
        var settled = _servers[Rules.Master].Scope.World.Entities.OfType<Market>().Sum(m => m.Settled);
        Console.Error.WriteLine($"lanterns: {_cluster.Tick} ticks, {_bots.Count} bots, {arrivals} arrivals over the bridge, {settled} swaps settled");
    }

    // A bot arrives like a player: the island admits it (a Join command from server code) and it opens one connection to that island,
    // from which it follows both islands and the master.
    private void Join(int index, bool sample)
    {
        var island = _servers[index % 2 == 0 ? Rules.IslandA : Rules.IslandB];
        var player = (island.Id * 100_000) + ++island.Issued;
        Island.Of(island.Scope.World)!.Send.Join((int)player, "Player " + island.Issued);
        _players.Add(player);

        var node = _cluster.AddNode("bot" + index);
        var (clientEnd, serverEnd) = Loopback.CreatePair();
        var link = node.ConnectTo(island.Node, PlayerTrust(player), clientEnd, _capture.ServerEnd(serverEnd));
        var replicas = new Dictionary<uint, ReplicaScope>();
        foreach (var id in ScopeIds)
        {
            replicas[id] = node.Subscribe(link, new ScopeId(id), NewWorld(Rng.Mix(_seed, id), WorldRole.Replica));
        }

        _bots.Add(new Bot(player, replicas, new Rng(Rng.Mix(_seed, 0xB07 + player)), sample));
    }

    private static void After(Server server, Archivist archivist, long tick)
    {
        if (server.Id == Rules.Master)
        {
            Observe(server);
            server.Driver!.Pump();
        }
        else
        {
            HandOver(server);
        }

        if (server.Scope.World.Tick % 2 == 0)
        {
            var save = server.Scope.Save();
            if (tick % SaveEvery == 0)
            {
                archivist.StoreSave(save);
            }
        }
    }

    // A player who stopped on the bridge goes to the island on the other side, unless a swap is holding their lanterns.
    private static void HandOver(Server server)
    {
        var ids = new List<EntityId>();
        uint dest = 0;
        foreach (var e in server.Scope.World.Entities)
        {
            if (e is Player { Dest: not 0 } p && p.Dest != server.Id && p.Pending == 0 && !InTransfer(server.Scope, p.Id))
            {
                dest = p.Dest;
                ids.Add(p.Id);
            }
        }

        if (ids.Count > 0 && server.Peers.TryGetValue(dest, out var peer) && peer.Replica.State == ReplicaState.Live)
        {
            server.Scope.BeginTransfer(peer.Link, new ScopeId(dest), ids);
        }
    }

    private static bool InTransfer(AuthorityScope scope, EntityId id)
    {
        foreach (var t in scope.Transfers)
        {
            if (t.State is TransferState.Pending or TransferState.Offered or TransferState.Committed && t.Entities.Contains(id))
            {
                return true;
            }
        }

        return false;
    }

    // The master learns where players are, and how many lanterns they hold, by reading the islands like any other subscriber.
    private static void Observe(Server master)
    {
        Market? market = null;
        var accounts = new Dictionary<uint, Account>();
        foreach (var e in master.Scope.World.Entities)
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

        foreach (var peer in master.Peers.Values)
        {
            if (peer.Replica.State != ReplicaState.Live)
            {
                continue;
            }

            foreach (var e in peer.Replica.World.Entities)
            {
                if (e is Player p && p.Dest == 0 && (!accounts.TryGetValue(p.Owner, out var a) || a.Island != p.Island || a.Total != p.Total || a.Name != p.Name))
                {
                    market.Send.Observe((int)p.Owner, p.Name, (int)p.Island, p.Total);
                }
            }
        }
    }

    private sealed record Peer(Link Link, ReplicaScope Replica);

    private sealed class Server(uint id, Node node, AuthorityScope scope)
    {
        public uint Id { get; } = id;

        public Node Node { get; } = node;

        public AuthorityScope Scope { get; } = scope;

        public Dictionary<uint, Peer> Peers { get; } = [];

        public EscrowDriver? Driver { get; set; }

        public uint Issued { get; set; }
    }

    /// <summary>
    /// A player that gathers a few lanterns, walks over the bridge, gathers on the other island, and offers or accepts swaps. It uses what a
    /// player can: the replicas it follows, and commands. Its choices (how many to gather, which lantern, when to offer) come from its own seeded stream.
    /// </summary>
    private sealed class Bot(uint player, Dictionary<uint, ReplicaScope> replicas, Rng rng, bool sample)
    {
        private const int ProposeEvery = 300;
        private const int AcceptAgainTicks = 180;

        private uint _island;
        private int _baseline;
        private int _goal = 3;
        private long _lastPropose = -ProposeEvery;
        private long _lastAccept = -AcceptAgainTicks;
        private int _lastStatus;
        private EntityId _target;
        private int _lastDx = 2;
        private int _lastDy = 2;
        private long _steps;

        public void Step()
        {
            _steps++;
            var me = Me();
            if (me is null)
            {
                return;
            }

            if (me.Island != _island)
            {
                _island = me.Island;
                _baseline = me.Total;
                _goal = sample ? 2 : rng.Range(2, 6);
                _target = default;
            }

            Swaps(me);
            var (tx, ty) = Target(me);
            var dx = Math.Sign(tx - me.X) * (Math.Abs(tx - me.X) > Rules.Speed ? 1 : 0);
            var dy = Math.Sign(ty - me.Y) * (Math.Abs(ty - me.Y) > Rules.Speed ? 1 : 0);
            if (dx != _lastDx || dy != _lastDy || _steps % 12 == 0)
            {
                me.Send.Steer(dx, dy);
                (_lastDx, _lastDy) = (dx, dy);
            }
        }

        private Player? Me()
        {
            foreach (var scope in new[] { Rules.IslandA, Rules.IslandB })
            {
                var replica = replicas[scope];
                if (replica.State != ReplicaState.Live)
                {
                    continue;
                }

                foreach (var e in replica.World.Entities)
                {
                    if (e is Player p && p.Owner == player)
                    {
                        return p;
                    }
                }
            }

            return null;
        }

        private Account? Mine()
        {
            foreach (var e in replicas[Rules.Master].World.Entities)
            {
                if (e is Account a && a.Owner == player)
                {
                    return a;
                }
            }

            return null;
        }

        private (int X, int Y) Target(Player me)
        {
            var bridgeX = me.Island == Rules.IslandA ? Rules.Width + 8 : -8;
            var bridgeY = (Rules.BridgeTop + Rules.BridgeBottom) / 2;
            if (me.Total - _baseline >= _goal || me.Pending > 0)
            {
                return (bridgeX, bridgeY);
            }

            var here = replicas.TryGetValue(me.Island, out var r) ? r.World : null;
            if (here is null)
            {
                return (bridgeX, bridgeY);
            }

            Lantern? best = null;
            Lantern? kept = null;
            var bestDistance = int.MaxValue;
            var count = 0;
            foreach (var e in here.Entities)
            {
                if (e is not Lantern { Taken: false } l)
                {
                    continue;
                }

                count++;
                if (l.Id == _target)
                {
                    kept = l;
                }

                var d = Math.Abs(l.X - me.X) + Math.Abs(l.Y - me.Y);
                if (d < bestDistance || (d == bestDistance && l.Id.Value < best!.Id.Value))
                {
                    best = l;
                    bestDistance = d;
                }
            }

            // Mostly stay on the chosen lantern; sometimes change one's mind, as a person would.
            var pick = kept is not null && !rng.OneIn(40) ? kept : best;
            if (pick is not null && count > 1 && rng.OneIn(25))
            {
                var skip = rng.Range(0, count);
                foreach (var e in here.Entities)
                {
                    if (e is Lantern { Taken: false } l && skip-- == 0)
                    {
                        pick = l;
                        break;
                    }
                }
            }

            _target = pick?.Id ?? default;
            return pick is null ? (bridgeX, bridgeY) : (pick.X, pick.Y);
        }

        private void Swaps(Player me)
        {
            if (Mine() is not { } account)
            {
                return;
            }

            var now = replicas[Rules.Master].LastTick;
            if (account.Status == SwapStatus.Incoming && (_lastStatus != SwapStatus.Incoming || now < _lastAccept || now - _lastAccept >= AcceptAgainTicks))
            {
                _lastAccept = now;
                account.Send.Accept();
            }
            else if (account.Status == SwapStatus.None || account.Status >= SwapStatus.Done)
            {
                if (now - _lastPropose >= ProposeEvery + rng.Range(0, 100) && me.Total >= 2 && Colors(me) is var (most, least))
                {
                    Account? other = null;
                    foreach (var e in replicas[Rules.Master].World.Entities)
                    {
                        if (e is Account b && b.Owner != me.Owner && b.Island != 0 && b.Island != me.Island && (other is null || rng.OneIn(2)))
                        {
                            other = b;
                        }
                    }

                    if (other is not null)
                    {
                        _lastPropose = now;
                        account.Send.Propose((int)other.Owner, most, least);
                    }
                }
            }

            _lastStatus = account.Status;
        }

        private static (int Most, int Least)? Colors(Player me)
        {
            int most = 0, least = 0;
            for (var c = 1; c < Rules.Colors; c++)
            {
                most = me.Count(c) > me.Count(most) ? c : most;
                least = me.Count(c) < me.Count(least) ? c : least;
            }

            return most == least ? null : (most, least);
        }
    }
}
